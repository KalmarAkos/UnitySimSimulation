using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct GroundingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (lt, ground) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<GroundState>>())
        {
            ground.ValueRW.FrameCounter++;
            if (ground.ValueRO.FrameCounter < ground.ValueRO.Interval)
                continue;

            ground.ValueRW.FrameCounter = 0;

            float3 pos = lt.ValueRO.Position;
            Vector3 rayStart = (Vector3)pos + new Vector3(0, 0.05f, 0);
            float rayLen = ground.ValueRO.FootOffset + 0.25f;

            if (Physics.Raycast(rayStart, Vector3.down, out var hit, rayLen))
            {
                ground.ValueRW.IsGrounded = 1;
                ground.ValueRW.LastHitY = hit.point.y;
                ground.ValueRW.GroundDist = pos.y - hit.point.y;
            }
            else
            {
                ground.ValueRW.IsGrounded = 0;
                ground.ValueRW.GroundDist = float.MaxValue;
            }
        }
    }
}
