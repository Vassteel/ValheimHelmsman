using System.Collections;
using System.Reflection;
namespace UnityEngine {
 public class Object {public static implicit operator bool(Object o)=>o!=null;}
 public class MonoBehaviour:Object {public void StartCoroutine(IEnumerator e){} }
 public readonly record struct Vector3(float x,float y,float z) {
  public static Vector3 zero=>new();public float sqrMagnitude=>x*x+y*y+z*z;
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
 }
 public class GameObject:Object {public Ship Ship;public T GetComponent<T>()=>Ship is T t?t:default;}
 public static class Time {public static float unscaledTime;}
 public static class Mathf {public static float Abs(float n)=>Math.Abs(n);}
 public class WaitForSeconds {public WaitForSeconds(float n){} }
}
namespace HarmonyLib {
 [AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute {public HarmonyPatch(Type t,string n){} }
 public static class AccessTools {public static MethodInfo Method(Type t,string n)=>t.GetMethod(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);}
}
public readonly record struct ZDOID(int ID) {public static ZDOID None=>new();}
public readonly record struct Vector2s(int x,int y);
public class ZDO {
 public ZDOID m_uid;public long Owner;public bool Named=true;public UnityEngine.Vector3 Position;
 public bool IsValid()=>true;public UnityEngine.Vector3 GetPosition()=>Position;public int GetPrefab()=>1;
 public long GetOwner()=>Owner;public void SetOwner(long peer)=>Owner=peer;
}
public class ZDOMan {
 public static ZDOMan instance=new();public Dictionary<ZDOID,ZDO> Data=new();
 public ZDO GetZDO(ZDOID id)=>Data.GetValueOrDefault(id);
 public void ForceSendZDO(long peer,ZDOID id){}
 public void FindSectorObjects(Vector2s zone,SimulationDistance distance,List<ZDO> output)=>output.AddRange(Data.Values);
 public bool GetAllZDOsWithPrefabIterative(string name,List<ZDO> output,ref int cursor)=>true;
 private bool IsInPeerActiveArea(UnityEngine.Vector3 point,long uid)=>false;
}
public class ZNetPeer {public long m_uid;public UnityEngine.Vector3 Position;public bool IsReady()=>true;public UnityEngine.Vector3 GetRefPos()=>Position;}
public class ZNet:UnityEngine.Object {
 public static ZNet instance=new();public static bool IsSinglePlayer;public bool Server=true;public long World=1;
 public List<ZNetPeer> Peers=new();public bool IsServer()=>Server;public long GetWorldUID()=>World;public static long GetUID()=>42;
 public ZNetPeer GetPeer(long uid)=>Peers.FirstOrDefault(p=>p.m_uid==uid);
 public List<ZNetPeer> GetPeers()=>Peers;public ZNetPeer GetServerPeer()=>new(){m_uid=999};
}
public class Player:UnityEngine.Object {public static Player m_localPlayer;}
public class ZNetScene:UnityEngine.Object {
 public static ZNetScene instance=new();public UnityEngine.GameObject Prefab=new(){Ship=new()};public UnityEngine.GameObject Live;
 public UnityEngine.GameObject GetPrefab(int id)=>Prefab;public UnityEngine.GameObject GetPrefab(string id)=>Prefab;
 public UnityEngine.GameObject FindInstance(ZDOID id)=>Live;public List<string> GetPrefabNames()=>new(){"Ship"};
}
public class ZPackage {
 private readonly List<object> data=new();private int index;
 public void Write(object obj)=>data.Add(obj);public ZDOID ReadZDOID()=>(ZDOID)data[index++];
 public UnityEngine.Vector3 ReadVector3()=>(UnityEngine.Vector3)data[index++];public string ReadString()=>(string)data[index++];public int ReadInt()=>(int)data[index++];
}
public class ZRoutedRpc {
 public static ZRoutedRpc instance=new();public List<(long Peer,string Method,object[] Values)> Sent=new();
 public void Register<T,U>(string name,Action<long,T,U> callback){}public void Register<T>(string name,Action<long,T> callback){}
 public void InvokeRoutedRPC(long peer,string method,params object[] values)=>Sent.Add((peer,method,values));
}
public class SimulationDistance {public SimulationDistance(int a,int b,bool classic){} }
public class ZoneSystem:UnityEngine.Object {
 public static ZoneSystem instance=new();public bool Loaded=true;
 public static Vector2s GetZone(UnityEngine.Vector3 point)=>new((int)Math.Floor(point.x/64),(int)Math.Floor(point.z/64));
 public bool IsZoneLoaded(Vector2s zone)=>Loaded;public bool PokeLocalZone(Vector2s zone)=>false;
}
public class Ship:UnityEngine.Object {public bool Occupied;public ShipControlls m_shipControlls=new();public bool HasPlayerOnboard()=>Occupied;}
public class ShipControlls {public bool User;public bool HaveValidUser()=>User;}
namespace Helmsman {
 public class SummonRequest:UnityEngine.Object {public ZDOID ShipId;public UnityEngine.Vector3 SimulationCenter;public bool Loading;public string Cancelled;public void Cancel(string reason)=>Cancelled=reason;}
 public class Plugin:UnityEngine.Object {public const string DockPrefab="Dock";public static Plugin Instance=new();public SummonRequest Summon;}
 public class ShipProfile {public float Length=10;public static bool Supports(Ship ship)=>ship;public static ShipProfile For(Ship ship)=>new();}
 public static class ShipDirectory {public static string SavedName(ZDO data)=>data.Named?"Gully":"";}
}
