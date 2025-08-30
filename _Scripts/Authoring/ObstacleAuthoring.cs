using Unity.Entities;
using UnityEngine;

// rá kell húzni minden olyan GameObject prefabra, amit az NPC-knek kerülgetniük kell

namespace Game.NPC
{
    public class ObstacleAuthoring : MonoBehaviour
    {
        class Baker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ObstacleTag>(e);
            }
        }
    }

    public struct ObstacleTag : IComponentData { }
}
