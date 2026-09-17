public class CraftingStation {public string gameObject,m_name;public bool m_showBasicRecipies=true,m_upgrader=true;public static implicit operator bool(CraftingStation s)=>s!=null;}
public class ItemDrop {public string gameObject;public static implicit operator bool(ItemDrop i)=>i!=null;}
public class Recipe {public CraftingStation m_craftingStation;public ItemDrop m_item;public static implicit operator bool(Recipe r)=>r!=null;}
public class Player {public CraftingStation Station;public CraftingStation GetCurrentCraftingStation()=>Station;public void GetAvailableRecipes(ref System.Collections.Generic.List<Recipe> available){} }
public static class Utils {public static string GetPrefabName(string name)=>name.Replace("(Clone)","");}
namespace Helmsman {internal static class ImportedHulls {internal const string TablePrefab="CarpentersTable";}}
