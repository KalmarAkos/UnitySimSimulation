// ---------------------------------------------
// FILE: Scripts/ECS/Systems/PlayerMoveSystem.cs
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
    public partial struct PlayerMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<ControlInput>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            float3 worldF = new float3(0, 0, 1);
            float3 worldR = new float3(1, 0, 0);

            foreach (var (lt, speed, steer, jump, input) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRW<Steering>, RefRW<JumpData>, RefRO<ControlInput>>()
                         .WithAll<PlayerTag>())
            {
                var jd = jump.ValueRW;
                jd.Gravity = -math.abs(jd.Gravity); // garantáltan lefelé

                float2 mv = input.ValueRO.Move;
                byte jumpNow = input.ValueRO.Jump;

                float3 wish = worldF * mv.y + worldR * mv.x;
                if (math.lengthsq(wish) > 1e-6f) wish = math.normalize(wish);
                float3 desired = wish * speed.ValueRO.Value;

                if (jumpNow == 1 && jd.IsGrounded == 1)
                { jd.VerticalSpeed = jd.JumpImpulse; jd.IsGrounded = 0; }

                jd.VerticalSpeed += jd.Gravity * dt;
                desired.y = jd.VerticalSpeed;

                lt.ValueRW.Position += desired * dt;
                steer.ValueRW.LastVelocity = desired;
                jump.ValueRW = jd;
            }
        }
    }
}
