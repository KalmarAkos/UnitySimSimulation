using Unity.Entities;

namespace Game.Citizens
{
    // Jel�li, melyik �p�let az NPC otthona
    public struct NpcCitizenHome : IComponentData
    {
        public Entity Building;
    }
}
