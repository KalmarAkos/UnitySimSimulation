using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PlayerInputSystem : ISystem
{
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
