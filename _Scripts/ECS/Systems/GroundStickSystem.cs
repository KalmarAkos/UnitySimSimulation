// ---------------------------------------------
// FILE: Scripts/ECS/Systems/GroundStickSystem.cs
// ---------------------------------------------
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
        { state.RequireForUpdate<PhysicsWorldSingleton>(); }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            foreach (var (lt, ground, jump) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Grounding>, RefRW<JumpData>>())
            {
                float3 origin = lt.ValueRO.Position + new float3(0, ground.ValueRO.RayLength * 0.5f, 0);
                var input = new RaycastInput
                {
                    Start = origin,
                    End = origin + new float3(0, -ground.ValueRO.RayLength, 0),
                    Filter = CollisionFilter.Default
                };

                var jd = jump.ValueRW;
                if (physicsWorld.CastRay(input, out var hit))
                {
                    var p = lt.ValueRO.Position;
                    p.y = hit.Position.y + ground.ValueRO.Offset; // kapszula félmagasság
                    if (jd.VerticalSpeed <= 0f)
                    { jd.VerticalSpeed = 0f; jd.IsGrounded = 1; }
                    lt.ValueRW.Position = p;
                }
                else
                { jd.IsGrounded = 0; }

                jump.ValueRW = jd;
            }
        }
    }
}