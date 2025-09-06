using Unity.Entities;
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
    public float RayLength;
}


public struct JumpData : IComponentData
{
    public float Gravity;
    public float JumpImpulse;
    public float VerticalSpeed;
    public byte IsGrounded;
}


public struct WorldBounds : IComponentData
{
    public float2 Size; // pl. (1000,1000)
    public float BaseY; // alapszint (pl. Terrain baseline)
}


public struct InputSingleton : IComponentData
{
    public float2 Move; // x=Horizontal (A/D), y=Vertical (W/S)
    public byte Jump; // egyszeri leütés
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