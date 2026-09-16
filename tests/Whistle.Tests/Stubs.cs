// Host doubles for the production registration and use hook; no Unity graphics simulation.
namespace UnityEngine {
public class Object { public string name; public static implicit operator bool(Object value)=>value!=null; }
public class GameObject:Object { }
public class Sprite:Object { }
}
public class Inventory { public readonly List<ItemDrop.ItemData> Items=new(); public bool ContainsItem(ItemDrop.ItemData item)=>Items.Contains(item); }
public class Humanoid:UnityEngine.Object { public void UseItem(Inventory inventory,ItemDrop.ItemData item,bool fromInventoryGui){} }
public class Player:Humanoid {
 public static Player m_localPlayer; public bool Dead,Attacking,Dodging; public readonly Inventory Inventory=new();
 public bool IsDead()=>Dead;public bool InAttack()=>Attacking;public bool InDodge()=>Dodging;public Inventory GetInventory()=>Inventory;
}
public class InventoryGui:UnityEngine.Object { public static InventoryGui instance=new();public int Hides;public void Hide()=>Hides++; }
public class ItemDrop {
 public ItemData m_itemData=new();
 public class ItemData { public UnityEngine.GameObject m_dropPrefab;public SharedData m_shared=new();public int m_stack=1;public enum ItemType {Misc,Consumable} }
 public class SharedData {public ItemData.ItemType m_itemType;public int m_maxQuality,m_value;public bool m_useDurability,m_questItem,m_teleportable;}
}
namespace HarmonyLib { [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch:Attribute { public HarmonyPatch(Type type,string method){} } }
namespace Jotunn.Configs {
public class RequirementConfig {public string Item;public int Amount;public RequirementConfig(string item,int amount){Item=item;Amount=amount;}}
public class ItemConfig {public string Name,Description,CraftingStation;public int Amount,StackSize,MinStationLevel;public float Weight;public UnityEngine.Sprite Icon;public RequirementConfig[] Requirements;}
}
namespace Jotunn.Entities {
public class CustomItem {
 public UnityEngine.GameObject ItemPrefab;public ItemDrop ItemDrop=new();public Jotunn.Configs.ItemConfig Config;
 public CustomItem(string name,string source,Jotunn.Configs.ItemConfig config){ItemPrefab=new(){name=name};ItemDrop.m_itemData.m_dropPrefab=ItemPrefab;Config=config;}
}
}
namespace Jotunn.Managers {public class ItemManager {public static ItemManager Instance=new();public Jotunn.Entities.CustomItem Added;public bool AddItem(Jotunn.Entities.CustomItem item){Added=item;return true;}}}
namespace Helmsman {
internal static class GullcallAssets {public static UnityEngine.GameObject Attached;public static UnityEngine.Sprite Icon()=>new();public static void Attach(UnityEngine.GameObject obj)=>Attached=obj;}
public class HelmsmanUI {public int Opens;public ItemDrop.ItemData Last;internal void OpenWhistle(ItemDrop.ItemData item){Opens++;Last=item;}}
public class Plugin {public static Plugin Instance=new();public static bool LocalSession=true;public HelmsmanUI UI=new();public static string LastMessage;internal static void Message(string text)=>LastMessage=text;internal void Record(string text){} }
}
