using Unity.Entities;
using UnityEngine;

// r� kell h�zni minden olyan GameObject prefabra, amit az NPC-knek ker�lgetni�k kell

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
