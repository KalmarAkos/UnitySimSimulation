using Unity.Entities;
using Unity.Mathematics;

// C�mk�k
public struct PlayerTag : IComponentData { }
public struct NpcTag : IComponentData { }

// Be�ll�t�sok
public struct MoveSettings : IComponentData
{
    public float MaxSpeed;
    public float Accel;
    public float Decel;
    public float AirControl;   // leveg�ben mennyi kontroll
    public float RotateSpeed;  // slerp forgat�s
}

// Aktu�lis sebess�g
public struct Velocity : IComponentData
{
    public float3 Value; // XZ: v�zszintes, Y: f�gg�leges
}

// Talaj-�llapot + ritk�t�s
public struct GroundState : IComponentData
{
    public float FootOffset;   // f�lmagass�g (l�bt�l a k�z�ppontig)
    public byte IsGrounded;   // 1=f�ld�n
    public float GroundDist;   // pos.y - hitY (utols� m�r�s)
    public float LastHitY;     // utols� talaj Y
    public int FrameCounter; // h�ny frame �ta nem raycastelt�nk
    public int Interval;     // h�nyadik frame-enk�nt raycastelj�nk
}

// J�t�kos input (WASD + Space)
public struct ControlInput : IComponentData
{
    public float2 Move;        // -1..1 (x=AD, y=WS)
    public byte Jump;        // 1 ha ebben a frame-ben Space
    public byte CameraRelative; // 1 = kamera szerint
}

// NPC-k vez�rl�se (AI �rja)
public struct DesiredMove : IComponentData
{
    public float3 WorldDir;    // normaliz�lt ir�ny vil�gban
    public byte Jump;        // 1 ha ugr�s
}
