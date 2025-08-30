using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Game.City;

namespace Game.Citizens
{
    // A te mozgásrendszered NpcTarget.Position-t fogja követni (dir = cél felé).
    public struct NpcTarget : IComponentData
    {
        public float3 Position;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct HomeDoorGoalSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NpcCitizenHome>();
            state.RequireForUpdate<Building>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bLookup = SystemAPI.GetComponentLookup<Building>(true);

            foreach (var (home, target, lt) in
                     SystemAPI.Query<RefRO<NpcCitizenHome>, RefRW<NpcTarget>, RefRO<Unity.Transforms.LocalTransform>>())
            {
                if (home.ValueRO.Building == Entity.Null) continue;
                if (!bLookup.HasComponent(home.ValueRO.Building)) continue;

                var B = bLookup[home.ValueRO.Building];
                float3 pos = lt.ValueRO.Position;

                // Kapu síkja: (X - DoorPos)·DoorNormal = 0
                float side = math.dot(pos - B.DoorPos, B.DoorNormal);

                if (side > 0f)
                {
                    // KINT vagyunk → irány az ajtó közepe
                    target.ValueRW.Position = B.DoorPos;
                }
                else
                {
                    // BENT vagyunk → irány a belső horgony
                    target.ValueRW.Position = B.InsideAnchor;
                }
            }
        }
    }
}
