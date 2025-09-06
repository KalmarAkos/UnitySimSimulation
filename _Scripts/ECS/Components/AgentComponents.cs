using Unity.Entities;
using Unity.Mathematics;

namespace DOTSGame.Components
{
    public struct PlayerTag : IComponentData { }
    public struct NPCTag : IComponentData { }

    // ÚJ: az inputot ez a komponens tartalmazza
    public struct ControlInput : IComponentData
    {
        public float2 Move; // x=AD, y=WS
        public byte Jump; // 1 = most megnyomva
    }

    public struct MoveSpeed : IComponentData { public float Value; }
    public struct TurnSpeed : IComponentData { public float Value; }

    public struct Steering : IComponentData
    {
        public float3 DesiredVelocity;
        public float3 LastVelocity;
        public float MaxSpeed;
    }

    public struct AvoidanceSettings : IComponentData
    {
        public float LookAhead;
        public float AvoidWeight;
        public float NeighborRadius;
        public float SeparationWeight;
        public float SeekWeight;
        public float MaxForce;
    }

    public struct AgentTarget : IComponentData
    {
        public float3 Position;
        public float Radius;
        public float RepathCooldown;
        public float RepathTimer;
        public uint Seed;
    }

    public struct Grounding : IComponentData
    {
        public float RayLength; // lefelé ray hossza
        public float Offset;    // talajtól való függõleges offset (kapszula félmagasság)
    }

    public struct JumpData : IComponentData
    {
        public float Gravity;       // NEGATÍV érték!
        public float JumpImpulse;   // kezdõ fel sebesség
        public float VerticalSpeed; // aktuális Y sebesség
        public byte IsGrounded;    // 0/1
    }

    public struct WorldBounds : IComponentData
    {
        public float2 Size;   // pl. (1000,1000)
        public float BaseY;  // alapszint
    }

    public struct NPCSpawner : IComponentData
    {
        public Entity Prefab;
        public int Count;
        public float2 AreaSize;
        public float SpawnY;
        public byte Done; // 0 = még nem spawnolt
    }

    public struct AgentRegistry : IComponentData
    {
        public int Count; // összes NPC
    }
}