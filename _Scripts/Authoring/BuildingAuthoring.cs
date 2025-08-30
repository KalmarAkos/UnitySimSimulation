using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.City
{
    public struct Building : IComponentData
    {
        public float3 DoorPos;        // ajt� k�zepe (vil�g)
        public float3 DoorNormal;     // kifel� mutat� egys�gvektor
        public float DoorHalfWidth;  // ajt� f�l-sz�less�g
        public float3 InsideAnchor;   // bent hova �rkezzen
    }

    public class BuildingAuthoring : MonoBehaviour
    {
        [Header("Door")]
        public Transform door;           // ajt� k�zep�t jel�l� transform
        public Vector3 doorNormal = Vector3.forward; // kifel� mutat� ir�ny (local)
        public float doorWidth = 1.5f;

        [Header("Inside Anchor")]
        public Transform insideAnchor;   // bent egy pont (el�szoba)

        class Baker : Baker<BuildingAuthoring>
        {
            public override void Bake(BuildingAuthoring a)
            {
                // Static helyett: None (ha nem mozog az �p�let)
                var e = GetEntity(TransformUsageFlags.None);

                Vector3 doorPosW = a.door ? a.door.position : a.transform.position;
                Vector3 nW = a.door ? a.door.TransformDirection(a.doorNormal.normalized)
                                    : a.transform.TransformDirection(a.doorNormal.normalized);
                if (nW.sqrMagnitude < 1e-6f) nW = Vector3.forward;

                Vector3 insideW = a.insideAnchor ? a.insideAnchor.position
                                                 : (doorPosW - nW * 1.0f);

                AddComponent(e, new Building
                {
                    DoorPos = (float3)doorPosW,
                    DoorNormal = math.normalize((float3)nW),
                    DoorHalfWidth = math.max(0.1f, a.doorWidth * 0.5f),
                    InsideAnchor = (float3)insideW
                });
            }
        }
    }
}
