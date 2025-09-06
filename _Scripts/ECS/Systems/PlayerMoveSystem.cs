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
            state.RequireForUpdate<InputSingleton>();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var input = SystemAPI.GetSingleton<InputSingleton>();


            // Világ-tengely relatív WASD (Z elõre, X oldal)
            float3 worldForward = new float3(0, 0, 1);
            float3 worldRight = new float3(1, 0, 0);


            foreach (var (lt, speed, steer, jump) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>, RefRW<Steering>, RefRW<JumpData>>().WithAll<PlayerTag>())
            {
                float3 wish = worldForward * input.Move.y + worldRight * input.Move.x;
                if (!math.all(wish == float3.zero)) wish = math.normalize(wish);


                float3 desired = wish * speed.ValueRO.Value;


                var jd = jump.ValueRW;
                if (input.Jump == 1 && jd.IsGrounded == 1)
                {
                    jd.VerticalSpeed = jd.JumpImpulse;
                    jd.IsGrounded = 0;
                }
                jd.VerticalSpeed += jd.Gravity * dt;
                desired.y = jd.VerticalSpeed;


                lt.ValueRW.Position += desired * dt;


                steer.ValueRW.LastVelocity = desired;
                jump.ValueRW = jd;
            }
        }
    }
}