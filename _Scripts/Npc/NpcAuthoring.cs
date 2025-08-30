using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.NPC
{
    public class NpcAuthoring : MonoBehaviour
    {
        [Header("Move")]
        public float maxSpeed = 3f;
        public float acceleration = 8f;
        public float deceleration = 10f;
        public float rotateSpeed = 8f;

        [Header("Wander / Avoid")]
        public float detectRadius = 1.2f;
        public float pauseMin = 0.5f;
        public float pauseMax = 2.0f;
        public float dirChangeMin = 2.0f;
        public float dirChangeMax = 5.0f;

        class Baker : Baker<NpcAuthoring>
        {
            public override void Bake(NpcAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<NpcTag>(e);
                AddComponent(e, new NpcMoveSettings
                {
                    MaxSpeed = a.maxSpeed,
                    Accel = a.acceleration,
                    Decel = a.deceleration,
                    RotateSpeed = a.rotateSpeed
                });
                AddComponent(e, new NpcVelocity { Value = float3.zero });
                AddComponent(e, new NpcWanderState { Dir = float3.zero, NextDirT = 0, PauseUntil = 0 });
                AddComponent(e, new NpcAvoidanceSettings
                {
                    DetectRadius = a.detectRadius,
                    PauseMin = a.pauseMin,
                    PauseMax = a.pauseMax,
                    DirChangeMin = a.dirChangeMin,
                    DirChangeMax = a.dirChangeMax
                });

                uint seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
                AddComponent(e, new NpcRandom { Rng = new Unity.Mathematics.Random(seed) });
            }
        }
    }
}
