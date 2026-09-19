using System.Reflection;
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object x)=>x!=null;}
 public class GameObject:Object {public string name;public object component;public T AddComponent<T>() where T:new()=>new();public T GetComponent<T>()=>component is T t?t:default;}
 public class Transform {public Vector3 position;}
 public class Component:Object {public Transform transform=new();public GameObject gameObject=new();public Dictionary<Type,object> components=new();public T GetComponent<T>()=>components.TryGetValue(typeof(T),out var t)?(T)t:default;}
 public struct Vector3 {public static Vector3 up=>new();public static Vector3 operator +(Vector3 a,Vector3 b)=>new();}
 public struct Quaternion {public static Quaternion identity=>new();}
}
public class ZDOID {public static readonly ZDOID None=new();}
public class ZDO {public string Saved="order";public ZDOID Receipt=ZDOID.None;public int InUse;public ZDOID GetZDOID(string k)=>Receipt;public int GetInt(int k)=>InUse;public void Set(int k,string s)=>Saved=s;}
public class ZNetView:UnityEngine.Component {public bool Owner=true;public ZDO Data=new();public bool IsValid()=>true;public bool IsOwner()=>Owner;public ZDO GetZDO()=>Data;}
public static class ZDOVars {public const int s_inUse=1;}
public class Player:UnityEngine.Component {public static Player m_localPlayer;public long Id=1;public Inventory Items=new();public long GetPlayerID()=>Id;public Inventory GetInventory()=>Items;}
public class Inventory {public List<ItemDrop.ItemData> Items=new();public List<ItemDrop.ItemData> GetAllItems()=>Items;public bool RemoveItem(ItemDrop.ItemData i,int n){if(!Items.Contains(i)||n>i.m_stack)return false;i.m_stack-=n;if(i.m_stack==0)Items.Remove(i);return true;}}
public class ItemDrop:UnityEngine.Component {
 public ItemData m_itemData=new();public static int Dropped;public static Action BeforeDrop;
 public static void DropItem(ItemData i,int n,UnityEngine.Vector3 p,UnityEngine.Quaternion q){BeforeDrop?.Invoke();Dropped+=n;}
 public class Shared {public string m_name;public int m_maxStackSize=50;}
 public class ItemData {public Shared m_shared=new();public int m_stack,m_worldLevel;public UnityEngine.GameObject m_dropPrefab;public ItemData Clone()=>(ItemData)MemberwiseClone();}
}
public class ObjectDB:UnityEngine.Object {public static ObjectDB instance=new();public Dictionary<string,UnityEngine.GameObject> Items=new();public UnityEngine.GameObject GetItemPrefab(string id)=>Items.GetValueOrDefault(id);}
public class Localization {public static Localization instance=new();public string Localize(string s)=>s;}
public class Container:UnityEngine.Component {public string m_name;public int m_width,m_height;public object m_bkg,m_privacy,m_openEffects,m_closeEffects,m_destroyedLootPrefab;public bool m_checkGuardStone,m_autoDestroyEmpty,Open,Access=true;public Inventory Items=new();public bool IsInUse()=>Open;public bool CheckAccess(long id)=>Access;public Inventory GetInventory()=>Items;}
public class CraftingStation:UnityEngine.Component {public string GetHoverText()=>"";}
public static class PrivateArea {public static bool Allowed=true;public static bool CheckAccess(UnityEngine.Vector3 p,int r,bool a,bool b)=>Allowed;}
public static class Game {public static int m_worldLevel=1;}
namespace Jotunn.Managers {public class PrefabManager {public static PrefabManager Instance=new();public UnityEngine.GameObject GetPrefab(string id)=>null;}}
namespace HarmonyLib {public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string n){}}public static class AccessTools {public static MethodInfo Method(Type t,string n)=>t.GetMethod(n);}}
namespace Helmsman {
 public class Shipyard:UnityEngine.Component {public string Identity="bench";public bool Nearby=true;public bool Near(Player p)=>Nearby;public string GetHoverText()=>"";}
 public class WorkstationLease {public bool Delay;public Func<string> Pending;public string Run(Func<string> a){if(Delay){Pending=a;return "Waiting";}return a();}}
 public class ConstructionOrder {public string recipe="Wood:20",name="Ottar";public long started=42,creator=1;public bool freeBuild;}
 public class SlipwayOrder {public string bench="bench",supplied="Wood:7";public bool awaitingMaterials=true;public long launchStarted;public ConstructionOrder order=new();}
 public class Ghost {public int Cleared;public void Dispose()=>Cleared++;}
 public class Crew {public int Leaving;public void Tick(object state,int dt)=>Leaving++;}
 public sealed partial class Slipway:UnityEngine.Component {
  public ZNetView view=new();public bool Ready=true,Allowed=true;public SlipwayOrder State=new();private int OrderKey=1;private string Receipt="receipt",cachedRaw="";private SlipwayOrder cachedOrder;
  public Ghost ghost=new();public Crew crew=new();public SlipwayOrder Order=>view.Data.Saved.Length>0?State:null;
  public bool CanOrder(Player p)=>Allowed;
 }
 internal static class QuartermasterWorkshop {public static List<Container> Chests=new();public static List<Container> Supplies(Player p,UnityEngine.Vector3 at)=>Chests;public static bool UsableSupply(Container c)=>!c.Open&&c.Access&&c.GetComponent<ZNetView>().IsOwner()&&PrivateArea.Allowed;}
}
