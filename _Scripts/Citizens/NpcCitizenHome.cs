using Unity.Entities;

namespace Game.Citizens
{
    // Jelöli, melyik épület az NPC otthona
    public struct NpcCitizenHome : IComponentData
    {
        public Entity Building;
    }
}
