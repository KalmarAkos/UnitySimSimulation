using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.City
{
    public struct Building : IComponentData
    {
        public float3 DoorPos;        // ajtó közepe (világ)
        public float3 DoorNormal;     // kifelé mutató egységvektor
        public float DoorHalfWidth;  // ajtó fél-szélesség
        public float3 InsideAnchor;   // bent hova érkezzen
    }

    public class BuildingAuthoring : MonoBehaviour
    {
        [Header("Door")]
        public Transform door;           // ajtó közepét jelölõ transform
        public Vector3 doorNormal = Vector3.forward; // kifelé mutató irány (local)
        public float doorWidth = 1.5f;

        [Header("Inside Anchor")]
        public Transform insideAnchor;   // bent egy pont (elõszoba)

        class Baker : Baker<BuildingAuthoring>
        {
            public override void Bake(BuildingAuthoring a)
            {
                // Static helyett: None (ha nem mozog az épület)
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
