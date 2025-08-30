using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;

public class CharacterAuthoring : MonoBehaviour
{
    [Header("Type")]
    public bool isPlayer = true;         // ha false, NPC lesz

    [Header("Speed")]
    public float maxSpeed = 5f;
    public float acceleration = 12f;
    public float deceleration = 16f;
    public float airControl = 0.4f;
    public float rotateSpeed = 14f;

    [Header("Jump / Gravity")]
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    [Header("Grounding")]
    public float footOffset = 1f;   // kapszula Height/2
    public int raycastInterval = 3;   // ritk�t�s (1 = minden frame)

    [Header("Input")]
    public bool cameraRelative = true;  // j�t�kosn�l �rv�nyes

    class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(e, new MoveSettings
            {
                MaxSpeed = a.maxSpeed,
                Accel = a.acceleration,
                Decel = a.deceleration,
                AirControl = a.airControl,
                RotateSpeed = a.rotateSpeed
            });

            AddComponent(e, new Velocity { Value = default });

            AddComponent(e, new GroundState
            {
                FootOffset = a.footOffset,
                IsGrounded = 0,
                GroundDist = float.MaxValue,
                LastHitY = 0,
                FrameCounter = 999,              // hogy az els� frame-ben raycasteljen
                Interval = math.max(1, a.raycastInterval)
            });

            if (a.isPlayer)
            {
                AddComponent<PlayerTag>(e);
                AddComponent(e, new ControlInput
                {
                    Move = default,
                    Jump = 0,
                    CameraRelative = (byte)(a.cameraRelative ? 1 : 0)
                });
            }
            else
            {
                AddComponent<NpcTag>(e);
                AddComponent(e, new DesiredMove { WorldDir = default, Jump = 0 });
            }

            // ugr�shoz/gravit�ci�hoz � k�l�n komponens nem kell; gravity/jumpForce-t k�zvetlen�l itt nem t�roljuk,
            // a MovementSystem-ben konstansk�nt �ll�tjuk be (vagy ha szeretn�d, kitehetj�k MoveSettingsbe).
            AddComponent(e, new PlayerJumpData { JumpForce = a.jumpForce, Gravity = a.gravity });
        }
    }
}

// opcion�lis kis adatcsomag a gravit�ci�/ugr�shoz
public struct PlayerJumpData : IComponentData
{
    public float JumpForce;
    public float Gravity;
}
