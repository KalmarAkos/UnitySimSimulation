using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using DOTSGame.Components;


namespace DOTSGame.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    public partial struct GroundStickSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;


            foreach (var (lt, ground, jump) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Grounding>, RefRW<JumpData>>())
            {
                var origin = lt.ValueRO.Position + new float3(0, ground.ValueRO.RayLength * 0.5f, 0);
                var input = new RaycastInput
                {
                    Start = origin,
                    End = origin + new float3(0, -ground.ValueRO.RayLength, 0),
                    Filter = CollisionFilter.Default
                };
                if (physicsWorld.CastRay(input, out var hit))
                {
                    var p = lt.ValueRO.Position;
                    p.y = hit.Position.y;
                    var jd = jump.ValueRW;
                    if (jd.VerticalSpeed <= 0f)
                    {
                        jd.VerticalSpeed = 0f;
                        jd.IsGrounded = 1;
                    }
                    jump.ValueRW = jd;
                    lt.ValueRW.Position = p;
                }
                else
                {
                    var jd = jump.ValueRW; jd.IsGrounded = 0; jump.ValueRW = jd;
                }
            }
        }
    }
}