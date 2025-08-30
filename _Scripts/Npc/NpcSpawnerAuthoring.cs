using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.NPC
{
    public class NpcSpawnerAuthoring : MonoBehaviour
    {
        [Header("Prefab + darabszám")]
        public GameObject npcPrefab;
        public int count = 500;

        [Header("Szórási terület (középpont körül, XZ)")]
        public Vector2 areaSize = new Vector2(80, 80);
        public float yLevel = 0f;

        class Baker : Baker<NpcSpawnerAuthoring>
        {
            public override void Bake(NpcSpawnerAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                var prefabE = GetEntity(a.npcPrefab, TransformUsageFlags.Dynamic);

                AddComponent(e, new NpcSpawner
                {
                    Prefab = prefabE,
                    Count = math.max(0, a.count),
                    Area = a.areaSize,
                    Y = a.yLevel
                });
            }
        }
    }

    public struct NpcSpawner : IComponentData
    {
        public Entity Prefab;
        public int Count;
        public float2 Area;
        public float Y;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct NpcSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<NpcSpawner>();

        public void OnUpdate(ref SystemState state)
        {
            var sp = SystemAPI.GetSingleton<NpcSpawner>();
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            var rng = new Unity.Mathematics.Random(0xBADD_F00D);

            for (int i = 0; i < sp.Count; i++)
            {
                var e = ecb.Instantiate(sp.Prefab);
                float x = rng.NextFloat(-sp.Area.x * 0.5f, sp.Area.x * 0.5f);
                float z = rng.NextFloat(-sp.Area.y * 0.5f, sp.Area.y * 0.5f);

                ecb.SetComponent(e, new LocalTransform
                {
                    Position = new float3(x, sp.Y, z),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                ecb.SetComponent(e, new NpcRandom { Rng = new Unity.Mathematics.Random(rng.NextUInt()) });
            }

            ecb.RemoveComponent<NpcSpawner>(SystemAPI.GetSingletonEntity<NpcSpawner>());
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
