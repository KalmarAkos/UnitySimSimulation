using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;   // URPMaterialPropertyBaseColor

namespace Game.NPC
{
    // Találat idõzítõ – ennyi ideig maradjon „piros”
    public struct NpcHitTimer : IComponentData
    {
        public float TimeLeft;
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))] // a szimuláció után kapjuk meg az eseményeket
    public partial struct NpcCollisionColorSystem : ISystem
    {
        ComponentLookup<NpcTag> _npcLookupRO;
        ComponentLookup<ObstacleTag> _obsLookupRO;
        ComponentLookup<URPMaterialPropertyBaseColor> _colorRW;
        ComponentLookup<NpcHitTimer> _hitRW;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();  // Unity Physics
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _npcLookupRO = state.GetComponentLookup<NpcTag>(isReadOnly: true);
            _obsLookupRO = state.GetComponentLookup<ObstacleTag>(isReadOnly: true);
            _colorRW = state.GetComponentLookup<URPMaterialPropertyBaseColor>(isReadOnly: false);
            _hitRW = state.GetComponentLookup<NpcHitTimer>(isReadOnly: false);
        }

        [BurstCompile]
        struct CollisionJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentLookup<NpcTag> Npc;
            [ReadOnly] public ComponentLookup<ObstacleTag> Obstacle;

            public ComponentLookup<URPMaterialPropertyBaseColor> Color;
            public ComponentLookup<NpcHitTimer> Hit;

            public EntityCommandBuffer ECB; // EndSimulation ECB

            public void Execute(CollisionEvent ce)
            {
                Entity a = ce.EntityA;
                Entity b = ce.EntityB;

                // „NPC ütközött fallal?”
                bool aNpc_bObs = Npc.HasComponent(a) && Obstacle.HasComponent(b);
                bool bNpc_aObs = Npc.HasComponent(b) && Obstacle.HasComponent(a);
                if (!aNpc_bObs && !bNpc_aObs) return;

                Entity npc = aNpc_bObs ? a : b;

                // Pirosra váltás
                if (Color.HasComponent(npc))
                {
                    Color[npc] = new URPMaterialPropertyBaseColor { Value = new float4(1, 0, 0, 1) };
                }

                // Idõzítõ frissítés/hozzáadás
                if (Hit.HasComponent(npc))
                {
                    var h = Hit[npc];
                    h.TimeLeft = 0.35f;
                    Hit[npc] = h;
                }
                else
                {
                    ECB.AddComponent(npc, new NpcHitTimer { TimeLeft = 0.35f });
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Lookups frissítése
            _npcLookupRO.Update(ref state);
            _obsLookupRO.Update(ref state);
            _colorRW.Update(ref state);
            _hitRW.Update(ref state);

            var sim = SystemAPI.GetSingleton<SimulationSingleton>();
            var ecb =
                SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>()
                         .ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            var job = new CollisionJob
            {
                Npc = _npcLookupRO,
                Obstacle = _obsLookupRO,
                Color = _colorRW,
                Hit = _hitRW,
                ECB = ecb
            };

            state.Dependency = job.Schedule(sim, state.Dependency);

            // a piros szín visszaalakítása idõzítõ alapján
            float dt = SystemAPI.Time.DeltaTime;
            foreach (var (hit, color, e) in SystemAPI.Query<RefRW<NpcHitTimer>, RefRW<URPMaterialPropertyBaseColor>>().WithAll<NpcTag>().WithEntityAccess())
            {
                hit.ValueRW.TimeLeft -= dt;
                if (hit.ValueRO.TimeLeft <= 0f)
                {
                    color.ValueRW = new URPMaterialPropertyBaseColor { Value = new float4(0.6f, 0.6f, 0.6f, 1f) }; // vissza szürkére
                    state.EntityManager.RemoveComponent<NpcHitTimer>(e);
                }
            }
        }
    }
}
