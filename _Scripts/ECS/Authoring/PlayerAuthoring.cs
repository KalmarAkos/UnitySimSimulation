// ---------------------------------------------
// FILE: Scripts/ECS/Authoring/PlayerAuthoring.cs
// ---------------------------------------------
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DOTSGame.Components;

namespace DOTSGame.Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 8f;
        public float maxSpeed = 8f;
        public float turnSpeed = 720f;

        [Header("Jump/Gravity")]
        public float gravity = -25f;       // NEGATÍV!
        public float jumpImpulse = 8f;

        [Header("Grounding")]
        public float groundRay = 3f;
        public float groundOffset = 0.9f;  // kapszula félmagasság

        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(e);
                AddComponent<ControlInput>(e); // << input komponens
                AddComponent(e, new MoveSpeed { Value = a.moveSpeed });
                AddComponent(e, new TurnSpeed { Value = a.turnSpeed });
                AddComponent(e, new Steering { DesiredVelocity = float3.zero, LastVelocity = float3.zero, MaxSpeed = a.maxSpeed });
                AddComponent(e, new JumpData { Gravity = a.gravity, JumpImpulse = a.jumpImpulse, VerticalSpeed = 0f, IsGrounded = 0 });
                AddComponent(e, new Grounding { RayLength = a.groundRay, Offset = a.groundOffset });
            }
        }
    }
}