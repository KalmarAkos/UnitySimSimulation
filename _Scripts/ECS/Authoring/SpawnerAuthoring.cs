using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using DOTSGame.Components;


namespace DOTSGame.Authoring
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        [Header("NPC Prefab (Entity)")]
        public GameObject npcPrefab;


        [Header("Count & Area")]
        public int count = 1000;
        public float2 areaSize = new float2(1000, 1000);
        public float spawnY = 1f;


        class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                var prefab = GetEntity(a.npcPrefab, TransformUsageFlags.Dynamic);
                AddComponent(e, new NPCSpawner
                {
                    Prefab = prefab,
                    Count = math.max(1, a.count),
                    AreaSize = a.areaSize,
                    SpawnY = a.spawnY,
                    Done = 0
                });
                AddComponent(e, new WorldBounds { Size = a.areaSize, BaseY = 0f });
                AddComponent(e, new AgentRegistry { Count = a.count });
            }
        }
    }
}