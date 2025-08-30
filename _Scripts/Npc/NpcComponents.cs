using Unity.Entities;
using Unity.Mathematics;

namespace Game.NPC
{
    public struct NpcTag : IComponentData { }

    public struct NpcMoveSettings : IComponentData
    {
        public float MaxSpeed;
        public float Accel;
        public float Decel;
        public float RotateSpeed;
    }

    public struct NpcVelocity : IComponentData
    {
        public float3 Value; // XZ-t haszn�ljuk
    }

    public struct NpcWanderState : IComponentData
    {
        public float3 Dir;        // norm�lt ir�ny
        public float NextDirT;   // k�vetkez� ir�nyv�lt�s ideje
        public float PauseUntil; // meddig �lljon
    }

    public struct NpcAvoidanceSettings : IComponentData
    {
        public float DetectRadius;
        public float PauseMin;
        public float PauseMax;
        public float DirChangeMin;
        public float DirChangeMax;
    }

    public struct NpcRandom : IComponentData
    {
        public Unity.Mathematics.Random Rng;
    }

    public struct NpcSpatialHashConfig : IComponentData
    {
        public float CellSize; // DetectRadius ~ 1�2�
    }
}
