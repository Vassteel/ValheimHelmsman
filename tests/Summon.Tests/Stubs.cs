// Execute the production request lifetime. Terrain, Unity and sailing are host doubles;
// collision geometry and sailing performance still require in-game checks.
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
namespace UnityEngine {
public class Object {
 public bool Destroyed;public static implicit operator bool(Object value)=>value!=null&&!value.Destroyed;
 public static void Destroy(Object value){if(value!=null)value.Destroyed=true;}
}
public class Transform:Object {public Vector3 position;public object rotation;public void SetParent(Transform other,bool preserve){} }
public class GameObject:Object {
 public GameObject(){}public GameObject(string name){}
 public bool Active;public Transform transform=new();public Dictionary<Type,object> Components=new();
 public void SetActive(bool value)=>Active=value;
 public T AddComponent<T>() where T:MonoBehaviour,new(){var value=new T{gameObject=this};Components[typeof(T)]=value;return value;}
 public T GetComponent<T>()=>Components.TryGetValue(typeof(T),out var value)?(T)value:default;
}
public class MonoBehaviour:Object {
 public GameObject gameObject=new();public Transform transform=>gameObject.transform;
 public readonly List<IEnumerator> Routines=new();
 public void StartCoroutine(IEnumerator routine){Routines.Add(routine);routine.MoveNext();}
 public T GetComponent<T>()=>gameObject.GetComponent<T>();
}
public class Camera:Object {public static Camera main;public Transform transform=new();}
public struct Vector3 {
 public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
 public static Vector3 up=>new(0,1,0);
 public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
 public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
 public static Vector3 operator *(Vector3 a,float n)=>new(a.x*n,a.y*n,a.z*n);
 public static float Distance(Vector3 a,Vector3 b){var d=a-b;return (float)Math.Sqrt(d.x*d.x+d.y*d.y+d.z*d.z);}
}
public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
public static class Time {public static float time;}
public static class Mathf {public static float Abs(float x)=>Math.Abs(x);}
}
namespace TMPro {
public enum TextAlignmentOptions {Center}
public class TextRect {public UnityEngine.Vector2 sizeDelta;}
public class TMP_Text:UnityEngine.MonoBehaviour {public object font;public float fontSize;public bool richText,raycastTarget;public string text;public TextAlignmentOptions alignment;public TextRect rectTransform=new();}
public class TextMeshPro:TMP_Text {}
}
namespace HarmonyLib {
[AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch:Attribute {public HarmonyPatch(Type type,string name){} }
public static class AccessTools {public static MethodInfo Method(Type t,string name)=>t.GetMethod(name);public static MethodInfo PropertyGetter(Type t,string name)=>t.GetProperty(name).GetMethod;}
public class CodeInstruction {public OpCode opcode;public object operand;public List<Label> labels=new();public CodeInstruction(OpCode op){opcode=op;}public bool Calls(MethodInfo method)=>Equals(operand,method);}
}
public readonly record struct ZDOID(int Value);
public readonly record struct Vector2s(int x,int y);
public class SimulationDistance {public SimulationDistance(int a,int b,bool c){} }
public class ZDO {public UnityEngine.Vector3 Position;public string Name="Ship";public UnityEngine.Vector3 GetPosition()=>Position;public string GetString(int key,string fallback)=>Name;public int GetPrefab()=>1;}
public class ZDOMan {
 public static ZDOMan instance=new();public ZDO Ship=new();
 public ZDO GetZDO(ZDOID id)=>Ship;
 public void FindSectorObjects(Vector2s center,SimulationDistance distance,List<ZDO> result){}
}
public class ZNet:UnityEngine.Object {public static ZNet instance=new();public long World=1;public bool IsServer()=>true;public long GetWorldUID()=>World;}
public class ZNetView:UnityEngine.MonoBehaviour {public bool Owned=true;public void ClaimOwnership(){}public bool IsOwner()=>Owned;}
public class ZoneSystem:UnityEngine.Object {
 public static ZoneSystem instance=new();public List<Vector2s> Poked=new();
 public static Vector2s GetZone(UnityEngine.Vector3 p)=>new((int)(p.x/64),(int)(p.z/64));
 public bool PokeLocalZone(Vector2s point){Poked.Add(point);return false;}
 public bool IsZoneLoaded(UnityEngine.Vector3 point)=>true;
}
public class ZNetScene:UnityEngine.Object {
 public static ZNetScene instance=new();public UnityEngine.GameObject Boat;
 public UnityEngine.GameObject GetPrefab(int id)=>Boat;
 public UnityEngine.GameObject FindInstance(ZDOID id)=>Boat;
 public bool HaveInstance(ZDO obj)=>true;
}
public class Player:UnityEngine.MonoBehaviour {public static Player m_localPlayer=new();public bool Dead;public bool IsDead()=>Dead;}
public class ShipControlls {public bool Occupied;public bool HaveValidUser()=>Occupied;}
public class Ship:UnityEngine.MonoBehaviour {
 public bool Occupied;public ShipControlls m_shipControlls=new();public bool HasPlayerOnboard()=>Occupied;
 public void CustomFixedUpdate(float dt){}
}
namespace Helmsman.Core {public static class ShipText {public static string Bearing(float x,float z)=>"N";} }
namespace Helmsman {
public static class NetworkNavigation {public static bool Loaded=true;public static bool Ready(UnityEngine.Vector3 center)=>Loaded;}
public class Config<T> {public T Value;}
public class Plugin:UnityEngine.MonoBehaviour {
 public static Plugin Instance=new();public static bool LocalSession=true;public SummonRequest Summon;public Voyage Voyage;public GullGuide CalledGull;
 public static string LastMessage;public static void Message(string text)=>LastMessage=text;public void Error(Exception error)=>throw new Exception("Unexpected request failure",error);
}
public class Berth {public string name;public bool configured;public UnityEngine.Vector3 position;}
public class DockRecord {public bool Temporary;public ZDOID Id;public Berth Berth;public UnityEngine.Vector3 MarkerPosition;}
public class DockMarker:UnityEngine.MonoBehaviour {public bool Ready=true;public ZDOID Id=new(9);public GullGuide FetchGuide(UnityEngine.Vector3 target){var g=GullGuide.Create(target,this,null);g.Fetch(target);return g;} }
public static class DockDirectory {public static DockRecord Home;public static DockRecord Resolve(ZDOID id)=>Home;}
public class ShipRecord {public ZDOID Id=new(1);public string Name="Test boat",Prefab="Karve";public Ship Loaded;}
public static class ShipDirectory {public const int NameKey=7;public static string SavedName(ZDO data)=>data.Name;}
public class ShipProfile {public static ShipProfile For(Ship ship)=>new();public static bool Supports(Ship ship)=>ship;}
public class WaterChart {
 public WaterChart(Ship ship,ShipProfile profile){}
 public static UnityEngine.Vector3 AtSea(UnityEngine.Vector3 p)=>new(p.x,0,p.z);
}
public static class ShorelineArrival {
 public static UnityEngine.Vector3 LastOrigin;public static int Attempts;public static int AcceptAfter=3;
 public static bool CallerReady(Player player,out string reason){reason="";return true;}
 public static IEnumerable<Berth> Candidates(UnityEngine.Vector3 origin,ShipProfile profile,string prefab){LastOrigin=origin;for(int i=0;i<5;i++)yield return new Berth{position=origin+new UnityEngine.Vector3(0,0,10+i),configured=true};}
 public static bool Validate(UnityEngine.Vector3 origin,Berth berth,WaterChart chart,out string reason){LastOrigin=origin;reason="";return ++Attempts>=AcceptAfter;}
}
public class GullGuide:UnityEngine.MonoBehaviour {
 public static GullGuide Last;public bool FlewAway;public UnityEngine.Vector3 Target;
 public static GullGuide Create(UnityEngine.Vector3 pos,DockMarker dock,Voyage voyage){Last=new();Last.transform.position=pos;return Last;}
 public static GullGuide Traveller(ZDOID id)=>null;
 public void Fetch(UnityEngine.Vector3 pos)=>Target=pos;
 public void FetchTarget(UnityEngine.Vector3 pos)=>Target=pos;
 public void ReserveDock(ZDOID id){}public void FlyAway()=>FlewAway=true;
}
public class Voyage:UnityEngine.MonoBehaviour {
 public string Status="Sailing";public Ship Ship;public bool Unattended=true,CanControl=true;public static DockRecord Arrival;
 public static bool BeginSummoned(Ship ship,DockRecord home,GullGuide gull,out string reason){Arrival=home;Plugin.Instance.Voyage=new(){Ship=ship};reason="";return true;}
 public void Cancel(string reason){if(Plugin.Instance.Summon)Plugin.Instance.Summon.Complete(reason);Plugin.Instance.Voyage=null;}
}
public class MenuTheme {public static UnityEngine.Object FindFont()=>new();}
}
public static class PrivateArea {public static bool Allowed=true;public static bool CheckAccess(UnityEngine.Vector3 point,float radius,bool flash,bool ward)=>Allowed;}
