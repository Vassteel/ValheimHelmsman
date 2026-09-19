#nullable disable
namespace Helmsman.Structures
{
    public static class PrefabPolicy
    {
        // A Piece component denotes hammer construction, not whether scenery can be imported.
        // Trees, furniture and Pickable decorations may be ordinary persistent networked objects.
        public static string Rejection(bool networked,bool creature,bool spawner,bool terrain,bool looseItem)
        {
            if(!networked)return "no persistent network component";
            if(creature)return "creature";
            if(spawner)return "creature spawner";
            if(terrain)return "independent terrain operation";
            if(looseItem)return "loose inventory item";
            return null;
        }
    }
}
