using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Game.NPC
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct NpcMovementSpatialSystem : ISystem
    {
        // ---- Spatial hash key (XZ) ----
        [BurstCompile]
        static int CellKey(float x, float z, float cell)
        {
            int2 c = (int2)math.floor(new float2(x, z) / cell);
            return unchecked((int)math.hash(new int2(c.x * 73856093, c.y * 19349663)));
        }

        static readonly int2[] Neigh =
        {
            new int2(-1,-1), new int2(0,-1), new int2(1,-1),
            new int2(-1, 0), new int2(0, 0), new int2(1, 0),
            new int2(-1, 1), new int2(0, 1), new int2(1, 1),
        };

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NpcSpatialHashConfig>();
            state.RequireForUpdate<NpcTag>();
        }

        [BurstCompile]
        public unsafe void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            float time = (float)SystemAPI.Time.ElapsedTime;
            float cell = SystemAPI.GetSingleton<NpcSpatialHashConfig>().CellSize;

            // --- Physics world for wall avoidance ---
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            var collisionWorld = physicsWorld.CollisionWorld;

            // --- Gather NPCs (positions for the grid) ---
            var q = SystemAPI.QueryBuilder().WithAll<NpcTag, LocalTransform>().Build();
            var ents = q.ToEntityArray(Allocator.TempJob);
            var poses = q.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            int count = ents.Length;
            if (count == 0) { ents.Dispose(); poses.Dispose(); return; }

            var indexOf = new NativeHashMap<Entity, int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++) indexOf.TryAdd(ents[i], i);

            var grid = new NativeParallelMultiHashMap<int, int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                float3 p = poses[i].Position;
                grid.Add(CellKey(p.x, p.z, cell), i);
            }

            // --- Main update loop ---
            foreach (var (lt, vel, ms, ws, av, rng, e) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<NpcVelocity>, RefRO<NpcMoveSettings>,
                                     RefRW<NpcWanderState>, RefRO<NpcAvoidanceSettings>, RefRW<NpcRandom>>()
                              .WithAll<NpcTag>()
                              .WithEntityAccess())
            {
                if (!indexOf.TryGetValue(e, out int self)) continue;

                float3 pos = lt.ValueRO.Position;

                // Pause phase: bleed velocity, skip steering
                if (time < ws.ValueRO.PauseUntil)
                {
                    float3 velPause = vel.ValueRO.Value * math.max(0f, 1f - ms.ValueRO.Decel * dt);
                    vel.ValueRW.Value = velPause;
                    continue;
                }

                // Wander baseline direction
                var r = rng.ValueRO.Rng;
                if (time >= ws.ValueRO.NextDirT || math.lengthsq(ws.ValueRO.Dir) < 1e-6f)
                {
                    float ang = r.NextFloat(0, math.PI * 2f);
                    ws.ValueRW.Dir = math.normalize(new float3(math.cos(ang), 0, math.sin(ang)));
                    float wait = r.NextFloat(av.ValueRO.DirChangeMin, av.ValueRO.DirChangeMax);
                    ws.ValueRW.NextDirT = time + wait;
                }

                float3 dir = ws.ValueRO.Dir;

                // Neighbour avoidance (spatial hash)
                float detectR2 = av.ValueRO.DetectRadius * av.ValueRO.DetectRadius;
                float3 push = float3.zero; bool any = false;

                int2 ccell = (int2)math.floor(new float2(pos.x, pos.z) / cell);
                for (int k = 0; k < Neigh.Length; k++)
                {
                    int2 nc = ccell + Neigh[k];
                    int key = unchecked((int)math.hash(new int2(nc.x * 73856093, nc.y * 19349663)));

                    NativeParallelMultiHashMapIterator<int> it;
                    int idx;
                    if (grid.TryGetFirstValue(key, out idx, out it))
                    {
                        do
                        {
                            if (idx == self) continue;
                            float3 op = poses[idx].Position;
                            float3 dv3 = op - pos; dv3.y = 0;
                            float d2 = math.lengthsq(dv3);
                            if (d2 > 1e-6f && d2 < detectR2)
                            {
                                any = true;
                                push -= dv3; // push away
                            }
                        }
                        while (grid.TryGetNextValue(out idx, ref it));
                    }
                }

                // Break symmetry when crowded
                if (any)
                {
                    float3 away = math.lengthsq(push) > 1e-6f ? math.normalize(push) : new float3(1, 0, 0);
                    float ang = r.NextFloat(-0.5f, 0.5f); // ±~30°
                    float s = math.sin(ang);
                    float c2 = math.cos(ang);
                    dir = math.normalize(new float3(
                        away.x * c2 - away.z * s, 0,
                        away.x * s + away.z * c2
                    ));
                    ws.ValueRW.Dir = dir;

                    float pause = r.NextFloat(av.ValueRO.PauseMin, av.ValueRO.PauseMax);
                    ws.ValueRW.PauseUntil = time + pause;
                    ws.ValueRW.NextDirT = math.max(ws.ValueRO.NextDirT, ws.ValueRO.PauseUntil + 0.3f);
                }

                // Velocity integration
                float3 desiredVel = dir * ms.ValueRO.MaxSpeed;
                float3 velNow = vel.ValueRO.Value;
                float3 dv = desiredVel - velNow;
                float dist = math.length(dv);
                float maxDv = ((math.lengthsq(desiredVel) > 0 && math.dot(velNow, desiredVel) < 0)
                                        ? ms.ValueRO.Decel : ms.ValueRO.Accel) * dt;

                if (dist > 1e-5f)
                    velNow = (dist > maxDv) ? (velNow + (dv / dist) * maxDv) : desiredVel;

                // Proposed new position
                float3 newPos = pos + velNow * dt;

                // -----------------------------
                // Wall avoidance (Unity.Physics)
                // -----------------------------
                {
                    float3 step = newPos - pos;
                    float stepLen = math.length(step);
                    if (stepLen > 1e-4f)
                    {
                        const float skin = 0.03f;
                        const float agentRadius = 0.35f;

                        SphereGeometry geom = new SphereGeometry
                        {
                            Center = float3.zero,
                            Radius = agentRadius
                        };
                        var material = Unity.Physics.Material.Default;
                        var filter = CollisionFilter.Default;
                        // (optional) exclude ground layer here by setting filter.CollidesWith

                        using (var sphereRef = SphereCollider.Create(geom, filter, material))
                        {
                            float3 castStart = pos + new float3(0, skin, 0);
                            float3 castEnd = newPos + new float3(0, skin, 0);

                            var castInput = new ColliderCastInput
                            {
                                Collider = (Unity.Physics.Collider*)sphereRef.GetUnsafePtr(),
                                Orientation = quaternion.identity,
                                Start = castStart,
                                End = castEnd
                            };

                            if (collisionWorld.CastCollider(castInput, out ColliderCastHit hit) && hit.Fraction > 1e-4f)
                            {
                                var hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                                bool obstacle = SystemAPI.HasComponent<ObstacleTag>(hitEntity);
                                if (obstacle)
                                {
                                    float3 nrm = math.normalizesafe(hit.SurfaceNormal, new float3(0, 1, 0));

                                    // slide velocity along the wall
                                    velNow = velNow - nrm * math.dot(velNow, nrm);

                                    // stop just before the wall + keep a small skin
                                    float3 hitPos = math.lerp(castStart, castEnd, hit.Fraction);
                                    newPos = hitPos - new float3(0, skin, 0) + nrm * skin;

                                    // if almost zero tangentially, give a little side nudge
                                    if (math.lengthsq(velNow) < 1e-4f)
                                    {
                                        float3 side = math.normalize(math.cross(nrm, new float3(0, 1, 0)));
                                        if (!math.any(math.isfinite(side))) side = new float3(1, 0, 0);
                                        velNow = side * 0.5f * ms.ValueRO.MaxSpeed;
                                        newPos += velNow * dt;
                                    }
                                }
                            }
                        }
                    }
                }

                // Face movement direction
                float vlen2 = math.lengthsq(velNow);
                if (vlen2 > 1e-6f && math.all(math.isfinite(velNow)))
                {
                    float3 fwd = velNow / math.sqrt(vlen2);
                    var target = quaternion.LookRotationSafe(fwd, math.up());
                    lt.ValueRW.Rotation = math.slerp(lt.ValueRO.Rotation, target, ms.ValueRO.RotateSpeed * dt);
                }

                vel.ValueRW.Value = velNow;
                lt.ValueRW.Position = newPos;

                rng.ValueRW.Rng = r; // persist RNG
            }

            grid.Dispose();
            indexOf.Dispose();
            ents.Dispose();
            poses.Dispose();
        }
    }
}
