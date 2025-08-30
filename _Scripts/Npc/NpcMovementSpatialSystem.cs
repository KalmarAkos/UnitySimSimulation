using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.NPC
{
    // Ha még nincs külön fájlod hozzá, ideiglenesen itt is definiálhatod.
    // Ha már létrehoztad máshol, ezt a structot töröld innen, hogy ne legyen duplikáció.
    //public struct ObstacleTag : IComponentData { }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct NpcMovementSpatialSystem : ISystem
    {
        // Burst-barát cellakulcs XZ síkra
        static int CellKey(float x, float z, float cell)
        {
            int2 c = (int2)math.floor(new float2(x, z) / cell);
            return unchecked((int)math.hash(new int2(c.x * 73856093, c.y * 19349663)));
        }

        // Saját + 8 szomszéd cella
        static readonly int2[] Neigh =
        {
            new int2(-1,-1), new int2(0,-1), new int2(1,-1),
            new int2(-1, 0), new int2(0, 0), new int2(1, 0),
            new int2(-1, 1), new int2(0, 1), new int2(1, 1),
        };

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NpcSpatialHashConfig>();
            state.RequireForUpdate<NpcTag>(); // csak akkor fut, ha van legalább egy NPC
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            float time = (float)SystemAPI.Time.ElapsedTime;
            float cell = SystemAPI.GetSingleton<NpcSpatialHashConfig>().CellSize;

            // — NPC-k (mozgatandók)
            var qNpc = SystemAPI.QueryBuilder().WithAll<NpcTag, LocalTransform>().Build();
            var ents = qNpc.ToEntityArray(Allocator.TempJob);
            var poses = qNpc.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            int nNpc = ents.Length;

            // — Akadályok (player + bármi, amin ObstacleTag van)
            var qObs = SystemAPI.QueryBuilder().WithAll<ObstacleTag, LocalTransform>().Build();
            var entsObs = qObs.ToEntityArray(Allocator.TempJob);
            var posesObs = qObs.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            int nObs = entsObs.Length;

            int nAll = nNpc + nObs;
            if (nAll == 0) { ents.Dispose(); poses.Dispose(); entsObs.Dispose(); posesObs.Dispose(); return; }

            // Entity -> npcIndex (csak NPC-khez kell)
            var indexOf = new NativeHashMap<Entity, int>(nNpc, Allocator.TempJob);
            for (int i = 0; i < nNpc; i++) indexOf.TryAdd(ents[i], i);

            // Spatial hash (cellKey -> összes index [0..nNpc) NPC, [nNpc..nNpc+nObs) OBSTACLE)
            var grid = new NativeParallelMultiHashMap<int, int>(nAll, Allocator.TempJob);
            for (int i = 0; i < nNpc; i++)
            {
                float3 p = poses[i].Position;
                grid.Add(CellKey(p.x, p.z, cell), i);
            }
            for (int j = 0; j < nObs; j++)
            {
                float3 p = posesObs[j].Position;
                grid.Add(CellKey(p.x, p.z, cell), nNpc + j);
            }

            // — Frissítés (csak NPC-ket mozgatunk)
            foreach (var (lt, vel, ms, ws, av, rng, e) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<NpcVelocity>, RefRO<NpcMoveSettings>,
                                     RefRW<NpcWanderState>, RefRO<NpcAvoidanceSettings>, RefRW<NpcRandom>>()
                              .WithAll<NpcTag>()
                              .WithEntityAccess())
            {
                if (!indexOf.TryGetValue(e, out int selfNpcIdx)) continue;

                float3 pos = lt.ValueRO.Position;

                // Wander alapirány idõzítve
                var r = rng.ValueRO.Rng;
                if (time >= ws.ValueRO.NextDirT || math.lengthsq(ws.ValueRO.Dir) < 1e-6f)
                {
                    float ang = r.NextFloat(0, math.PI * 2f);
                    ws.ValueRW.Dir = math.normalize(new float3(math.cos(ang), 0, math.sin(ang)));
                    float wait = r.NextFloat(av.ValueRO.DirChangeMin, av.ValueRO.DirChangeMax);
                    ws.ValueRW.NextDirT = time + wait;
                }
                float3 dir = ws.ValueRO.Dir;

                // Szomszédok felkutatása (NPC + Obstacle)
                float detectR2 = av.ValueRO.DetectRadius * av.ValueRO.DetectRadius;
                float3 push = float3.zero; bool any = false;

                int2 c = (int2)math.floor(new float2(pos.x, pos.z) / cell);
                for (int k = 0; k < Neigh.Length; k++)
                {
                    int2 nc = c + Neigh[k];
                    int key = unchecked((int)math.hash(new int2(nc.x * 73856093, nc.y * 19349663)));

                    NativeParallelMultiHashMapIterator<int> it;
                    int idx;
                    if (grid.TryGetFirstValue(key, out idx, out it))
                    {
                        do
                        {
                            // saját NPC index kihagyása
                            if (idx == selfNpcIdx) continue;

                            // pozíció lekérdezése: NPC vagy Obstacle
                            float3 op = (idx < nNpc) ? poses[idx].Position
                                                      : posesObs[idx - nNpc].Position;

                            float3 dVec = op - pos; dVec.y = 0;
                            float d2 = math.lengthsq(dVec);
                            if (d2 > 1e-6f && d2 < detectR2)
                            {
                                any = true;
                                push -= dVec; // el a szomszédtól / akadálytól
                            }
                        }
                        while (grid.TryGetNextValue(out idx, ref it));
                    }
                }

                // — Steering tangens irányba + közeledés csillapítása (nincs pause, nincs nudge)
                if (any)
                {
                    float3 sepDir = push;
                    if (math.lengthsq(sepDir) < 1e-6f)
                    {
                        float a0 = r.NextFloat(0, math.PI * 2f);
                        sepDir = new float3(math.cos(a0), 0, math.sin(a0));
                    }
                    sepDir = math.normalize(sepDir);

                    // Tangens (90°-os kerülõ), véletlen oldal
                    float3 tangent = math.normalize(new float3(-sepDir.z, 0, sepDir.x));
                    if (r.NextFloat() < 0.5f) tangent = -tangent;

                    // Fokozatos kormányzás
                    const float steer = 0.65f;
                    float blend = math.saturate(8f * dt);
                    float3 wantedDir = math.normalize(math.lerp(dir, tangent, steer));
                    dir = math.normalize(math.lerp(dir, wantedDir, blend));

                    // Befelé mutató sebességkomponens visszavétele
                    float3 vNow = vel.ValueRO.Value;
                    float towards = math.dot(vNow, -sepDir);
                    if (towards > 0f)
                        vel.ValueRW.Value = vNow - (-sepDir) * (towards * 0.6f);

                    ws.ValueRW.Dir = dir;
                }

                // Sebesség integráció (MoveTowards-szerû)
                float3 desiredVel = dir * ms.ValueRO.MaxSpeed;
                float3 velNow = vel.ValueRO.Value;
                float3 dv = desiredVel - velNow;
                float dist = math.length(dv);
                float maxDv = ((math.lengthsq(desiredVel) > 0 && math.dot(velNow, desiredVel) < 0)
                                      ? ms.ValueRO.Decel : ms.ValueRO.Accel) * dt;

                if (dist > 1e-5f)
                    velNow = (dist > maxDv) ? (velNow + (dv / dist) * maxDv) : desiredVel;

                // Pozíció frissítés
                float3 newPos = pos + velNow * dt;

                // Forgatás a mozgás irányába
                float vlen2 = math.lengthsq(velNow);
                if (vlen2 > 1e-6f && math.all(math.isfinite(velNow)))
                {
                    float3 fwd = velNow / math.sqrt(vlen2);
                    var target = quaternion.LookRotationSafe(fwd, math.up());
                    lt.ValueRW.Rotation = math.slerp(lt.ValueRO.Rotation, target, ms.ValueRO.RotateSpeed * dt);
                }

                // Visszaírás
                vel.ValueRW.Value = velNow;
                lt.ValueRW.Position = newPos;
                rng.ValueRW.Rng = r; // RNG vissza
            }

            grid.Dispose();
            indexOf.Dispose();
            ents.Dispose();
            poses.Dispose();
            entsObs.Dispose();
            posesObs.Dispose();
        }
    }
}
