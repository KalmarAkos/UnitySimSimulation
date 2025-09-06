// ---------------------------------------------
// FILE: Scripts/ECS/Systems/SpawnerSystem.cs
// ---------------------------------------------
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using DOTSGame.Components;

namespace DOTSGame.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        { state.RequireForUpdate<NPCSpawner>(); }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var spawner in SystemAPI.Query<RefRW<NPCSpawner>>())
            {
                if (spawner.ValueRO.Done == 1) continue;

                int count = spawner.ValueRO.Count;
                float2 half = spawner.ValueRO.AreaSize * 0.5f;

                for (int i = 0; i < count; i++)
                {
                    var e = ecb.Instantiate(spawner.ValueRO.Prefab);
                    float x = math.lerp(-half.x, half.x, (i % 1000) / 999f);
                    float z = math.lerp(-half.y, half.y, (i / 1000) / math.max(1, (count - 1) / 1000f));
                    var r = Unity.Mathematics.Random.CreateFromIndex((uint)(i + 1));
                    x += r.NextFloat(-2f, 2f);
                    z += r.NextFloat(-2f, 2f);

                    ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(new float3(x, spawner.ValueRO.SpawnY, z), quaternion.identity, 1f));

                    var tgt = new float3(x, spawner.ValueRO.SpawnY, z) + math.normalize(new float3(r.NextFloat(-1, 1), 0, r.NextFloat(-1, 1))) * r.NextFloat(5f, 20f);
                    ecb.SetComponent(e, new AgentTarget { Position = tgt, Radius = 2f, RepathCooldown = 3f, RepathTimer = 0f, Seed = (uint)(i + 1) });
                }

                spawner.ValueRW.Done = 1;
                if (SystemAPI.HasSingleton<AgentRegistry>())
                {
                    var reg = SystemAPI.GetSingletonRW<AgentRegistry>();
                    reg.ValueRW.Count = count;
                }
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}