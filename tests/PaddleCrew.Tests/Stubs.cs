namespace HarmonyLib { public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string n){}} }
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object? o)=>o!=null;}
 public class Component:Object {public Dictionary<Type,object> Components=new(); public Transform transform=new();public T? GetComponent<T>() where T:class=>Components.GetValueOrDefault(typeof(T)) as T;}
 public class Transform:Object {public Vector3 position;public Vector3 forward=new(0,0,1);public object? Parent;public T? GetComponentInParent<T>() where T:class=>Parent as T;public Vector3 InverseTransformPoint(Vector3 p)=>p-position;public Vector3 TransformPoint(Vector3 p)=>p+position;}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new();public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float s)=>new(a.x*s,a.y*s,a.z*s);}
 public enum ForceMode {Impulse}
 public class Rigidbody:Component {public Vector3 worldCenterOfMass,Impulse;public float mass=100;public int Calls;public void AddForce(Vector3 f,ForceMode m){Impulse+=f;Calls++;}}
}
public record struct ZDOID(int Value);
public class ZDO {public ZDOID m_uid=new(1);private Dictionary<string,object> d=new();public int GetInt(string k)=>d.TryGetValue(k,out var x)?(int)x:0;public long GetLong(string k)=>d.TryGetValue(k,out var x)?(long)x:0;public ZDOID GetZDOID(string k)=>d.TryGetValue(k,out var x)?(ZDOID)x:new();public UnityEngine.Vector3 GetVec3(string k,UnityEngine.Vector3 v)=>d.TryGetValue(k,out var x)?(UnityEngine.Vector3)x:v;public void Set(string k,object v)=>d[k]=v;}
public class ZNet:UnityEngine.Object {public static ZNet instance=new();public DateTime Now=new(2026,9,17);public DateTime GetTime()=>Now;}
public class ZNetView:UnityEngine.Component {public bool Owner=true,Valid=true;public ZDO Data=new();public bool IsValid()=>Valid;public bool IsOwner()=>Owner;public ZDO GetZDO()=>Data;}
public class Player:UnityEngine.Component {public bool Dead;public UnityEngine.Transform? Anchor;public bool IsDead()=>Dead;public UnityEngine.Transform? GetAttachPoint()=>Anchor;public void SetControls(){}}
public class Ship:UnityEngine.Component {public float m_backwardForce=.65f,m_waterLevelOffset=0,m_disableLevel=-.5f;public void CustomFixedUpdate(){}}
public class WaterVolume {}
public static class Floating {public static float Level=1;public static float GetWaterLevel(UnityEngine.Vector3 p,ref WaterVolume v)=>Level;}
namespace Helmsman {public class PaddleCraftRig:UnityEngine.Component {}}
