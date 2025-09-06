using Unity.Entities;
using UnityEngine;
using DOTSGame.Components;


namespace DOTSGame.Authoring
{
    // Egyszerû híd: MonoBehaviour beírja az ECS InputSingleton-t (WASD + SPACE).
    public class InputBridgeAuthoring : MonoBehaviour
    {
        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var em = world.EntityManager;
                if (!em.CreateEntityQuery(typeof(InputSingleton)).HasSingleton)
                {
                    var e = em.CreateEntity(typeof(InputSingleton));
                    em.SetComponentData(e, new InputSingleton { Move = default, Jump = 0 });
                }
            }
        }


        void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            var em = world.EntityManager;


            var q = em.CreateEntityQuery(typeof(InputSingleton));
            if (q.HasSingleton)
            {
                var e = q.GetSingletonEntity();
                var move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                byte jump = (byte)(Input.GetKeyDown(KeyCode.Space) ? 1 : 0);
                em.SetComponentData(e, new InputSingleton { Move = move, Jump = jump });
            }
        }
    }
}