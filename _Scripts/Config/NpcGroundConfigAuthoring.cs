// Game.NPC n�vt�r � adj egy �j f�jlt: NpcGroundConfigAuthoring.cs
using Unity.Entities;
using UnityEngine;

namespace Game.NPC
{
    public struct NpcGroundConfig : IComponentData
    {
        public float GroundY;
        public float FootOffset; // f�l magass�g (pl. 1.0f)
    }

    public class NpcGroundConfigAuthoring : MonoBehaviour
    {
        public float groundY = 0f;
        public float footOffset = 1f;

        class Baker : Baker<NpcGroundConfigAuthoring>
        {
            public override void Bake(NpcGroundConfigAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new NpcGroundConfig { GroundY = a.groundY, FootOffset = a.footOffset });
            }
        }
    }
}
