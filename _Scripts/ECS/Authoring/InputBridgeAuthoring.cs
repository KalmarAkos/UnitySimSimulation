// ---------------------------------------------
// FILE: Scripts/ECS/Authoring/InputBridgeAuthoring.cs
// ---------------------------------------------
using Unity.Entities;
using UnityEngine;
using DOTSGame.Components;

namespace DOTSGame.Authoring
{
    /// <summary>
    /// Egyszerû híd: MonoBehaviour beírja az ECS InputSingleton-t (WASD + SPACE).
    /// </summary>
    public class InputBridgeAuthoring : MonoBehaviour
    {
        void OnEnable()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;

            // KÉSZÍTS QUERY-T ÉS TÍPUSSAL HÍVD A HasSingleton-T
            var qInput = em.CreateEntityQuery(ComponentType.ReadOnly<InputSingleton>());
            if (!qInput.HasSingleton<InputSingleton>())
            {
                var e = em.CreateEntity(typeof(InputSingleton));
                em.SetComponentData(e, new InputSingleton { Move = default, Jump = 0 });
            }
        }

        void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            var em = world.EntityManager;

            var q = em.CreateEntityQuery(ComponentType.ReadWrite<InputSingleton>());
            if (q.HasSingleton<InputSingleton>())
            {
                var e = q.GetSingletonEntity();
                var move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                byte jump = (byte)(Input.GetKeyDown(KeyCode.Space) ? 1 : 0);

                em.SetComponentData(e, new InputSingleton
                {
                    Move = move,
                    Jump = jump
                });
            }
        }
    }
}
