// ---------------------------------------------
// FILE: Scripts/ECS/Systems/PlayerInputSystem.cs
// ---------------------------------------------
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using DOTSGame.Components;

namespace DOTSGame.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }
        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            var kb = Keyboard.current;
            float2 move = float2.zero;
            byte jumpNow = 0;

            if (kb != null)
            {
                if (kb.aKey.isPressed) move.x -= 1f;
                if (kb.dKey.isPressed) move.x += 1f;
                if (kb.sKey.isPressed) move.y -= 1f;
                if (kb.wKey.isPressed) move.y += 1f;
                if (math.lengthsq(move) > 1f) move = math.normalize(move);
                jumpNow = (byte)((kb.spaceKey != null && kb.spaceKey.wasPressedThisFrame) ? 1 : 0);
            }

            foreach (var input in SystemAPI.Query<RefRW<ControlInput>>().WithAll<PlayerTag>())
            {
                input.ValueRW.Move = move;
                input.ValueRW.Jump = jumpNow;
            }
        }
    }
}