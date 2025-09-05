using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
namespace Game.NPC
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct NpcHitFadeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb =
                SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>()
                         .ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (color, timer, entity) in
                     SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, RefRW<NpcHitTimer>>()
                              .WithEntityAccess())
            {
                float t = timer.ValueRO.TimeLeft - dt;
                timer.ValueRW.TimeLeft = t;

                // egyszerű visszafestés zöld felé (opcionális lerp)
                if (t <= 0f)
                {
                    color.ValueRW.Value = new float4(0, 1, 0, 1); // zöld
                    ecb.RemoveComponent<NpcHitTimer>(entity);
                }
                else
                {
                    // lassan halványodhat nálad, ha szeretnéd:
                    float k = math.saturate(t / 0.35f); // 0..1
                    color.ValueRW.Value = math.lerp(new float4(0, 1, 0, 1), new float4(1, 0, 0, 1), k);
                }
            }
        }
    }
}
