using Unity.Entities;
using Unity.Mathematics;

// Címkék
public struct PlayerTag : IComponentData { }
public struct NpcTag : IComponentData { }

// Beállítások
public struct MoveSettings : IComponentData
{
    public float MaxSpeed;
    public float Accel;
    public float Decel;
    public float AirControl;   // levegõben mennyi kontroll
    public float RotateSpeed;  // slerp forgatás
}

// Aktuális sebesség
public struct Velocity : IComponentData
{
    public float3 Value; // XZ: vízszintes, Y: függõleges
}

// Talaj-állapot + ritkítás
public struct GroundState : IComponentData
{
    public float FootOffset;   // félmagasság (lábtól a középpontig)
    public byte IsGrounded;   // 1=földön
    public float GroundDist;   // pos.y - hitY (utolsó mérés)
    public float LastHitY;     // utolsó talaj Y
    public int FrameCounter; // hány frame óta nem raycasteltünk
    public int Interval;     // hányadik frame-enként raycasteljünk
}

// Játékos input (WASD + Space)
public struct ControlInput : IComponentData
{
    public float2 Move;        // -1..1 (x=AD, y=WS)
    public byte Jump;        // 1 ha ebben a frame-ben Space
    public byte CameraRelative; // 1 = kamera szerint
}

// NPC-k vezérlése (AI írja)
public struct DesiredMove : IComponentData
{
    public float3 WorldDir;    // normalizált irány világban
    public byte Jump;        // 1 ha ugrás
}
