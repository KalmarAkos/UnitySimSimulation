// ---------------------------------------------
// FILE: Scripts/ECS/Systems/NPCWanderSystem.cs
// ---------------------------------------------
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DOTSGame.Components;

namespace DOTSGame.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct NPCWanderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        { state.RequireForUpdate<WorldBounds>(); }

        static float3 RandomDir(uint seed)
        {
            var r = Unity.Mathematics.Random.CreateFromIndex(seed);
            var d = math.normalize(new float3(r.NextFloat(-1, 1), 0, r.NextFloat(-1, 1)));
            if (!math.all(math.isfinite(d)) || math.lengthsq(d) < 1e-6f) d = new float3(0, 0, 1);
            return d;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var bounds = SystemAPI.GetSingleton<WorldBounds>();
            var half = bounds.Size * 0.5f;

            foreach (var (lt, tgt) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<AgentTarget>>().WithAll<NPCTag>())
            {
                var at = tgt.ValueRW; at.RepathTimer -= dt;
                bool needNew = false;
                var pos = lt.ValueRO.Position; var to = at.Position - pos; to.y = 0;
                if (math.lengthsq(to) <= at.Radius * at.Radius) needNew = true;
                if (at.RepathTimer <= 0f) needNew = true;

                if (needNew)
                {
                    at.Seed += 17u;
                    float3 dir = RandomDir(at.Seed);
                    float dist = 8f + (at.Seed % 13u);
                    float3 candidate = pos + dir * dist;
                    candidate.x = math.clamp(candidate.x, -half.x + 1f, half.x - 1f);
                    candidate.z = math.clamp(candidate.z, -half.y + 1f, half.y - 1f);
                    candidate.y = bounds.BaseY;
                    at.Position = candidate;
                    at.RepathTimer = at.RepathCooldown;
                }
                tgt.ValueRW = at;
            }
        }
    }
}