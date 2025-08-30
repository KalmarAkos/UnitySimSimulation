using Unity.Entities;
using UnityEngine;

namespace Game.NPC
{
    public class NpcSpatialConfigAuthoring : MonoBehaviour
    {
        [Tooltip("R�cs cella m�ret (m�ter). J�: DetectRadius � 1�2.")]
        public float cellSize = 2.0f;

        class Baker : Baker<NpcSpatialConfigAuthoring>
        {
            public override void Bake(NpcSpatialConfigAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new NpcSpatialHashConfig { CellSize = Mathf.Max(0.1f, a.cellSize) });
            }
        }
    }
}
