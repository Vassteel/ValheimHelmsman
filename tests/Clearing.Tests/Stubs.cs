using N=System.Numerics;
namespace UnityEngine {
 public class Object {public static void Destroy(Object o){} public string name="object";public static implicit operator bool(Object o)=>o!=null;}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T GetComponentInParent<T>()where T:Component=>gameObject.GetComponent<T>()??transform.parent?.GetComponentInParent<T>();public T[] GetComponentsInChildren<T>(bool inactive=false)where T:Component=>gameObject.GetComponentsInChildren<T>();}
 public class MonoBehaviour:Component {}
 public static class Time {public static float time;}
 public class GameObject:Object {public bool activeSelf=true;public Transform transform;readonly List<Component> parts=new();public GameObject(){transform=new Transform{gameObject=this};parts.Add(transform);}public T AddComponent<T>()where T:Component,new(){var p=new T{gameObject=this};parts.Add(p);return p;}public T GetComponent<T>()where T:Component=>parts.OfType<T>().FirstOrDefault();public T[] GetComponentsInChildren<T>()where T:Component=>parts.OfType<T>().Concat(transform.children.SelectMany(t=>t.gameObject.GetComponentsInChildren<T>())).ToArray();}
 public class Transform:Component {public Transform root=>parent?parent.root:this;public Transform parent;public List<Transform> children=new();public Vector3 localPosition,localScale=Vector3.one;public Quaternion localRotation=Quaternion.identity;public Vector3 position=>TransformPoint(Vector3.zero);public Vector3 forward=>rotation*Vector3.forward;public Quaternion rotation=>parent?parent.rotation*localRotation:localRotation;public void SetParent(Transform t){parent=t;t.children.Add(this);}public Vector3 TransformPoint(Vector3 p){p=localPosition+localRotation*Vector3.Scale(p,localScale);return parent?parent.TransformPoint(p):p;}public bool IsChildOf(Transform t)=>this==t || parent&&parent.IsChildOf(t);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>MathF.Sqrt(sqrMagnitude);public Vector3 normalized=>this*(1/magnitude);public static Vector3 up=>new(0,1,0);public static Vector3 forward=>new(0,0,1);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public float this[int i]{get=>i==0?x:i==1?y:z;set{if(i==0)x=value;else if(i==1)y=value;else z=value;}}public static Vector3 zero=>new(0,0,0);public static Vector3 one=>new(1,1,1);public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float s)=>new(a.x*s,a.y*s,a.z*s);}
 public struct Quaternion {public N.Quaternion q;public static Quaternion LookRotation(Vector3 v)=>Euler(0,MathF.Atan2(v.x,v.z)*180/MathF.PI,0);public static Quaternion identity=>new(){q=N.Quaternion.Identity};public static Quaternion Inverse(Quaternion a)=>new(){q=N.Quaternion.Inverse(a.q)};public static Quaternion Euler(float x,float y,float z)=>new(){q=N.Quaternion.CreateFromYawPitchRoll(y*MathF.PI/180,x*MathF.PI/180,z*MathF.PI/180)};public static Quaternion operator *(Quaternion a,Quaternion b)=>new(){q=a.q*b.q};public static Vector3 operator *(Quaternion a,Vector3 b){var r=N.Vector3.Transform(new(b.x,b.y,b.z),a.q);return new(r.X,r.Y,r.Z);}}
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 extents=>size*.5f;public void Expand(float n){size+=Vector3.one*n;}public void Encapsulate(Vector3 p){var lo=center-extents;var hi=center+extents;for(int i=0;i<3;i++){lo[i]=Math.Min(lo[i],p[i]);hi[i]=Math.Max(hi[i],p[i]);}center=(lo+hi)*.5f;size=hi-lo;}}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>MathF.Sqrt(x*x+y*y);}
 public struct RaycastHit {public Collider collider;}
 public class Collider:Component {public bool enabled=true,isTrigger;public Bounds bounds=>new(transform.position,Vector3.one);public Vector3 ClosestPoint(Vector3 p)=>transform.position;}
 public class BoxCollider:Collider {public Vector3 center,size=Vector3.one;}
 public class SphereCollider:Collider {public Vector3 center;public float radius;}
 public class CapsuleCollider:Collider {public Vector3 center;public int direction;public float radius,height;}
 public class MeshCollider:Collider {public bool convex=true;public Mesh sharedMesh;}
 public class Mesh:Object {public Bounds bounds;}
 public static class Mathf {public const float PI=MathF.PI;public static int RoundToInt(float f)=>(int)MathF.Round(f);public static int CeilToInt(float f)=>(int)MathF.Ceiling(f);public static int Max(int a,int b)=>Math.Max(a,b);public static float Sin(float a)=>MathF.Sin(a);public static float Cos(float a)=>MathF.Cos(a);public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class LayerMask {public static int GetMask(params string[] names)=>1;}
 public enum QueryTriggerInteraction {Ignore}
 public static class Physics {
  public static RaycastHit[] Cast=Array.Empty<RaycastHit>();public static RaycastHit[] BoxCastAll(Vector3 p,Vector3 h,Vector3 d,Quaternion r,float distance,int mask,QueryTriggerInteraction q)=>Cast;
  public static HashSet<(Collider,Collider)> Ignored=new();public static bool GetIgnoreCollision(Collider a,Collider b)=>Ignored.Contains((a,b));public static void IgnoreCollision(Collider a,Collider b,bool yes){if(yes)Ignored.Add((a,b));else Ignored.Remove((a,b));}
  public static Collider[] Nearby=Array.Empty<Collider>();public static Vector3 QueryCenter,QueryHalf,PartPosition;public static Quaternion PartRotation;public static int Calls;public static Func<Collider,Collider,float> Penetration=(a,b)=>0;
  public static Collider[] OverlapBox(Vector3 center,Vector3 half,Quaternion rotation,int mask,QueryTriggerInteraction q){QueryCenter=center;QueryHalf=half;return Nearby;}
  public static bool ComputePenetration(Collider a,Vector3 p,Quaternion r,Collider b,Vector3 bp,Quaternion br,out Vector3 direction,out float depth){Calls++;PartPosition=p;PartRotation=r;direction=Vector3.zero;depth=Penetration(a,b);return depth>0;}
 }
}
public class Ship:UnityEngine.Component {public bool Owner=true,Aboard=true;public UnityEngine.Collider m_floatCollider;public ShipControlls m_shipControlls=new();public bool IsOwner()=>Owner;public bool IsPlayerInBoat(Player p)=>Aboard;}
public class ShipControlls {public bool User;public bool HaveValidUser()=>User;}
public class Character:UnityEngine.Component {}
public class Heightmap:UnityEngine.Component {public static float Ground=25;public static bool Available=true;public static bool GetHeight(UnityEngine.Vector3 p,out float h){h=Ground;return Available;}}
public class ZoneSystem:UnityEngine.Object {public static ZoneSystem instance=new();public float m_waterLevel=30;public Func<UnityEngine.Vector3,bool> Loaded=p=>true;public bool IsZoneLoaded(UnityEngine.Vector3 p)=>Loaded(p);}
public class Fish:UnityEngine.Component {}
public class Piece:UnityEngine.Component {}
public class WorldGenerator {public static WorldGenerator instance=new();public float GetHeight(UnityEngine.Vector3 p)=>Heightmap.Ground;}

namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string name,Type[] types){} }
 public static class AccessTools {
  public static System.Reflection.MethodInfo Method(Type t,string name)=>t.GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
  public static T MethodDelegate<T>(System.Reflection.MethodInfo m)where T:Delegate=>(T)Delegate.CreateDelegate(typeof(T),m);
 }
}
public class Player:Character {public static Player m_localPlayer;public bool Dead;public bool IsDead()=>Dead;public long GetPlayerID()=>10;}
public class PrivateArea {public static bool Allowed=true;public static bool CheckAccess(UnityEngine.Vector3 p,float r,bool f,bool w)=>Allowed;}
public class ZDO {public Dictionary<string,bool> Bools=new();public int InUse,Stack;public bool GetBool(string key)=>Bools.GetValueOrDefault(key);public int GetInt(int key)=>InUse;public void Set(string key,bool value)=>Bools[key]=value;}
public class ZNetView:UnityEngine.Component {public bool Owner=true,Valid=true,Destroyed,FailDestroy;public ZDO Data=new();public bool IsValid()=>Valid;public bool IsOwner()=>Owner;public ZDO GetZDO()=>Data;public void Destroy(){if(FailDestroy)throw new Exception("Destroy delayed");Destroyed=true;}}
public static class ZNet {public static long GetUID()=>42;}
public static class ZDOVars {public static int s_inUse=1;}
public class Game {public static int m_worldLevel=3;}
public class HitData {public UnityEngine.Collider m_hitCollider;public UnityEngine.Vector3 m_point,m_dir;public short m_toolTier;public Damage m_damage=new();public class Damage {public float m_pickaxe;}public void SetAttacker(Player p){} }
public class DropTable {public List<DropData> m_drops=new();public struct DropData {public UnityEngine.GameObject m_item;}}
public class ItemDrop:UnityEngine.Component {
 public ItemData m_itemData=new();public bool CanPickup(bool delay)=>GetComponent<ZNetView>().IsOwner();
 public static void SaveToZDO(ItemData item,ZDO data)=>data.Stack=item.m_stack;
 public static void OnCreateNew(ItemDrop item,bool cheated=false){item.m_itemData.m_worldLevel=(byte)Game.m_worldLevel;item.m_itemData.m_cheated=cheated;Helmsman.RockClearing.Capture(item);}
 public class SharedData {public string m_name="Stone";public int m_maxStackSize=50;}
 public class ItemData {public UnityEngine.GameObject m_dropPrefab;public int m_stack=1,m_quality=1;public byte m_worldLevel=3;public bool m_cheated;public SharedData m_shared=new();public ItemData Clone()=>(ItemData)MemberwiseClone();}
}
public class Inventory {
 public List<ItemDrop.ItemData> Items=new();public int Slots=1;public bool FailPartial;
 public int GetEmptySlots()=>Slots-Items.Count;public List<ItemDrop.ItemData> GetAllItems()=>Items;
 public bool CanAddItem(ItemDrop.ItemData item,int count)=>Items.Where(i=>i.m_shared.m_name==item.m_shared.m_name&&i.m_quality==item.m_quality&&i.m_worldLevel==item.m_worldLevel).Sum(i=>i.m_shared.m_maxStackSize-i.m_stack)+GetEmptySlots()*item.m_shared.m_maxStackSize>=count;
 public bool AddItem(ItemDrop.ItemData item){var existing=Items.FirstOrDefault(i=>i.m_stack<50&&i.m_shared.m_name==item.m_shared.m_name&&i.m_quality==item.m_quality&&i.m_worldLevel==item.m_worldLevel);if(existing!=null)existing.m_stack+=item.m_stack;else Items.Add(item);return !FailPartial;}
 public void Save(ZPackage p)=>p.Items=Items.Select(i=>i.Clone()).ToList();public void Load(ZPackage p,bool b)=>Items=p.Items.Select(i=>i.Clone()).ToList();
}
public class ZPackage {static int id;static Dictionary<int,List<ItemDrop.ItemData>> Saved=new();public List<ItemDrop.ItemData> Items=new();public ZPackage(){}public ZPackage(byte[] bytes){Items=Saved[BitConverter.ToInt32(bytes)];}public byte[] GetArray(){Saved[++id]=Items;return BitConverter.GetBytes(id);}}
public class Container:UnityEngine.Component {public ZNetView m_rootObjectOverride;public bool InUse,Owner=true,Access=true,m_checkGuardStone;public Inventory Inventory=new();private bool CheckAccess(long id)=>Access;public bool IsOwner()=>Owner;public bool IsInUse()=>InUse;public Inventory GetInventory()=>Inventory;}
public class DropOnDestroyed:UnityEngine.Component {public DropTable m_dropWhenDestroyed;}
public static class NativeRock {
 public static int Hits;public static ItemDrop LastDrop;public static bool FailAfterDrop;
 public static void Hit(DropTable table){Hits++;var prefab=table.m_drops[0].m_item;var go=new UnityEngine.GameObject();go.AddComponent<ZNetView>();LastDrop=go.AddComponent<ItemDrop>();LastDrop.m_itemData=prefab.GetComponent<ItemDrop>().m_itemData.Clone();LastDrop.m_itemData.m_dropPrefab=prefab;ItemDrop.OnCreateNew(LastDrop,true);if(FailAfterDrop)throw new Exception("Native hit failed after creating a drop");}
}
public class MineRock:UnityEngine.Component {public DropTable m_dropItems;private int GetAreaIndex(UnityEngine.Collider c)=>0;private void RPC_Hit(long sender,HitData hit,int index)=>NativeRock.Hit(m_dropItems);}
public class MineRock5:UnityEngine.Component {public DropTable m_dropItems;private int GetAreaIndex(UnityEngine.Collider c)=>0;private void RPC_Damage(long sender,HitData hit,int index)=>NativeRock.Hit(m_dropItems);}
public class Destructible:UnityEngine.Component {public UnityEngine.GameObject m_spawnWhenDestroyed,m_spawnWhenDamaged;private void RPC_Damage(long sender,HitData hit)=>NativeRock.Hit(GetComponent<DropOnDestroyed>().m_dropWhenDestroyed);}
namespace Helmsman {
 public class Setting {public bool Value;}
 public class Voyage:UnityEngine.Component {public Ship Ship;public bool CanControl=true;}
 public class Plugin {public static Plugin Instance=new();public Setting FishPassThrough=new(){Value=true};public Voyage Voyage;public List<Exception> Errors=new();public void Error(Exception e)=>Errors.Add(e);}
 public class ShipHoldScope:UnityEngine.Component {public bool MigrationBlocked;public int Key(int key)=>key;}
 public class ShipProfile {public float Length=10,Margin=.2f;public static ShipProfile For(Ship s)=>new();}
 public class WaterChart {public static UnityEngine.Vector3 AtSea(UnityEngine.Vector3 p)=>new(p.x,30,p.z);}
}
