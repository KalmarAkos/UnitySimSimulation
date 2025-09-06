// ---------------------------------------------
// FILE: Scripts/ECS/Systems/RotationSystem.cs
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
    [UpdateAfter(typeof(PlayerMoveSystem))]
    [UpdateAfter(typeof(NPCSteeringSystem))]
    public partial struct RotationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var up = new float3(0, 1, 0);
            foreach (var (lt, steer) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Steering>>())
            {
                var v = steer.ValueRO.LastVelocity; v.y = 0;
                if (math.lengthsq(v) > 1e-4f)
                {
                    var rot = quaternion.LookRotationSafe(v, up);
                    var cur = lt.ValueRO;
                    cur.Rotation = rot;
                    lt.ValueRW = cur;
                }
            }
        }
    }
}