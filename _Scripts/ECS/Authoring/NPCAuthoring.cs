// ---------------------------------------------
// FILE: Scripts/ECS/Authoring/NPCAuthoring.cs
// ---------------------------------------------
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DOTSGame.Components;

namespace DOTSGame.Authoring
{
    public class NPCAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float maxSpeed = 6f;

        [Header("Steering Weights")]
        public float seekWeight = 1.0f;
        public float separationWeight = 1.5f;
        public float avoidWeight = 2.0f;
        public float maxForce = 10f;

        [Header("Neighborhood / Obstacle")]
        public float neighborRadius = 2.5f;
        public float lookAhead = 3.0f;

        [Header("Targeting")]
        public float arriveRadius = 2.0f;
        public float repathCooldown = 3.0f;

        [Header("Grounding")]
        public float groundRay = 3f;
        public float groundOffset = 0.9f;

        class Baker : Baker<NPCAuthoring>
        {
            public override void Bake(NPCAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<NPCTag>(e);
                AddComponent(e, new MoveSpeed { Value = a.moveSpeed });
                AddComponent(e, new Steering { DesiredVelocity = float3.zero, LastVelocity = float3.zero, MaxSpeed = a.maxSpeed });
                AddComponent(e, new AvoidanceSettings
                {
                    LookAhead = a.lookAhead,
                    AvoidWeight = a.avoidWeight,
                    NeighborRadius = a.neighborRadius,
                    SeparationWeight = a.separationWeight,
                    SeekWeight = a.seekWeight,
                    MaxForce = a.maxForce
                });
                AddComponent(e, new AgentTarget
                {
                    Position = float3.zero,
                    Radius = a.arriveRadius,
                    RepathCooldown = a.repathCooldown,
                    RepathTimer = 0f,
                    Seed = 1u
                });
                AddComponent(e, new Grounding { RayLength = a.groundRay, Offset = a.groundOffset });
                AddComponent<JumpData>(e); // NPC-knél is ragaszkodunk a talajhoz
            }
        }
    }
}