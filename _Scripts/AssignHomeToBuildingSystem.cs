using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.City;

namespace Game.Citizens
{
    // A NpcCitizenHome.Building-et a legközelebbi Building entitásra állítja be.
    // Ezt csak egyszer kell lefuttatni, amikor a polgár létrejön.
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AssignHomeToBuildingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NpcCitizenHome>();
            state.RequireForUpdate<Building>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bQuery = SystemAPI.QueryBuilder().WithAll<Building>().Build();
            var bEnts = bQuery.ToEntityArray(Allocator.Temp);
            var bData = bQuery.ToComponentDataArray<Building>(Allocator.Temp);

            if (bEnts.Length == 0) { bEnts.Dispose(); bData.Dispose(); return; }

            foreach (var (lt, home, e) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<NpcCitizenHome>>()
                              .WithEntityAccess())
            {
                if (home.ValueRO.Building != Entity.Null) continue;

                float3 p = lt.ValueRO.Position;
                float best = float.MaxValue;
                Entity bestB = Entity.Null;

                for (int i = 0; i < bEnts.Length; i++)
                {
                    float d2 = math.lengthsq(bData[i].DoorPos - p);
                    if (d2 < best) { best = d2; bestB = bEnts[i]; }
                }

                home.ValueRW.Building = bestB;
            }

            bEnts.Dispose();
            bData.Dispose();
        }
    }
}
