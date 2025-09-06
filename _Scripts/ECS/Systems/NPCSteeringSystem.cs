
// ---------------------------------------------
// FILE: Scripts/ECS/Systems/NPCSteeringSystem.cs
// ---------------------------------------------
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using DOTSGame.Components;

namespace DOTSGame.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NPCWanderSystem))]
    public partial struct NPCSteeringSystem : ISystem
    {
        struct AgentSnapshot { public float3 pos; public float3 fwd; }
        const float CellSize = 3.0f;
        static int3 CellOf(float3 p) => new int3((int)math.floor(p.x / CellSize), 0, (int)math.floor(p.z / CellSize));
        static int Hash(int3 c) { unchecked { return c.x * 73856093 ^ c.y * 19349663 ^ c.z * 83492791; } }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        { state.RequireForUpdate<AgentRegistry>(); state.RequireForUpdate<PhysicsWorldSingleton>(); }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            int count = SystemAPI.GetSingleton<AgentRegistry>().Count; if (count <= 0) return;

            var snapshots = new NativeArray<AgentSnapshot>(count, Allocator.TempJob);
            var map = new NativeParallelMultiHashMap<int, int>(count * 2, Allocator.TempJob);

            int idx = 0;
            foreach (var (lt, steer, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Steering>>().WithAll<NPCTag>().WithEntityAccess())
            {
                if (idx >= count) break;
                float3 fwd = math.normalizesafe(steer.ValueRO.LastVelocity, new float3(0, 0, 1));
                var snap = new AgentSnapshot { pos = lt.ValueRO.Position, fwd = fwd };
                snapshots[idx] = snap;
                var cell = CellOf(snap.pos); map.Add(Hash(cell), idx);
                idx++;
            }
            int used = math.min(idx, count);
            float2 half = SystemAPI.HasSingleton<WorldBounds>() ? SystemAPI.GetSingleton<WorldBounds>().Size * 0.5f : new float2(500, 500);

            var snapshotsRO = snapshots; var mapRO = map;
            foreach (var (lt, ms, av, tgt, steer) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRO<AvoidanceSettings>, RefRO<AgentTarget>, RefRW<Steering>>().WithAll<NPCTag>())
            {
                float3 pos = lt.ValueRO.Position;

                float3 toTarget = tgt.ValueRO.Position - pos; toTarget.y = 0;
                float3 seekDir = math.normalizesafe(toTarget);
                float3 seekVel = seekDir * ms.ValueRO.Value;

                float3 sep = float3.zero; int3 cell = CellOf(pos);
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int key = Hash(new int3(cell.x + dx, 0, cell.z + dz));
                        if (mapRO.TryGetFirstValue(key, out int nIdx, out var it))
                        {
                            do
                            {
                                if (nIdx < used)
                                {
                                    float3 other = snapshotsRO[nIdx].pos;
                                    float3 d = pos - other; d.y = 0;
                                    float dist2 = math.lengthsq(d);
                                    float rr = av.ValueRO.NeighborRadius * av.ValueRO.NeighborRadius;
                                    if (dist2 > 1e-6f && dist2 < rr)
                                    {
                                        float inv = 1f / math.max(0.0001f, dist2);
                                        sep += d * inv;
                                    }
                                }
                            } while (mapRO.TryGetNextValue(out nIdx, ref it));
                        }
                    }
                sep = math.normalizesafe(sep) * ms.ValueRO.Value;

                float3 forward = math.normalizesafe(steer.ValueRO.LastVelocity, seekDir);
                float3 origin = pos + new float3(0, 0.5f, 0);
                float3 hitAvoid = float3.zero;
                var ray = new Unity.Physics.RaycastInput { Start = origin, End = origin + forward * av.ValueRO.LookAhead, Filter = CollisionFilter.Default };
                if (physicsWorld.CastRay(ray, out var hit))
                {
                    float3 n = hit.SurfaceNormal; n.y = 0; n = math.normalizesafe(n);
                    float3 refl = math.normalizesafe(math.reflect(forward, n));
                    hitAvoid = refl * ms.ValueRO.Value;
                }

                float3 desired = seekVel * av.ValueRO.SeekWeight + sep * av.ValueRO.SeparationWeight + hitAvoid * av.ValueRO.AvoidWeight;
                float m = math.length(desired); if (m > av.ValueRO.MaxForce) desired *= (av.ValueRO.MaxForce / m);
                if (!math.all(desired == float3.zero)) desired = math.normalizesafe(desired) * ms.ValueRO.Value;

                float3 newPos = pos + desired * dt;
                newPos.x = math.clamp(newPos.x, -half.x + 0.5f, half.x - 0.5f);
                newPos.z = math.clamp(newPos.z, -half.y + 0.5f, half.y - 0.5f);

                lt.ValueRW.Position = newPos;
                steer.ValueRW.LastVelocity = desired;
                steer.ValueRW.DesiredVelocity = desired;
            }

            snapshots.Dispose();
            map.Dispose();
        }
    }
}