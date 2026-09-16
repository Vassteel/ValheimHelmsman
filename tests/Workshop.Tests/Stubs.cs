namespace UnityEngine {
 public class Object {public static implicit operator bool(Object value)=>value!=null;}
 public class Transform {public Vector3 position;}
 public class MonoBehaviour:Object {public Transform transform=new();public ZNetView View;public T GetComponent<T>()=>View is T t?t:default;}
 public readonly record struct Vector3(float x,float y,float z) {public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);}
 public static class Time {public static float unscaledTime;}
}
public class ZDO {
 public int m_uid=7;public long Owner=42;public Dictionary<int,long> Values=new();
 public long GetLong(int key)=>Values.GetValueOrDefault(key);public void Set(int key,long value)=>Values[key]=value;
 public long GetOwner()=>Owner;public void SetOwner(long owner)=>Owner=owner;
}
public class ZNetView:UnityEngine.Object {
 public ZDO Data=new();public bool Valid=true;public List<(long Peer,string Method,object[] Args)> Sent=new();
 public bool IsValid()=>Valid;public bool IsOwner()=>Data.Owner==ZNet.GetUID();public ZDO GetZDO()=>Data;
 public void Register<T>(string name,Action<long,T> method){}
 public void InvokeRPC(string name,params object[] args)=>InvokeRPC(Data.Owner,name,args);
 public void InvokeRPC(long peer,string name,params object[] args)=>Sent.Add((peer,name,args));
}
public class Player:UnityEngine.MonoBehaviour {
 public static Player m_localPlayer;public static List<Player> All=new();public long Id;public bool Dead;
 public bool IsDead()=>Dead;public long GetPlayerID()=>Id;public static List<Player> GetAllPlayers()=>All;
}
public class ZNet {public static ZNet instance=new();public static long Peer=42;public static long GetUID()=>Peer;public DateTime GetTime()=>new DateTime(2026,1,1).AddSeconds(UnityEngine.Time.unscaledTime);}
public class ZDOMan {public static ZDOMan instance=new();public void ForceSendZDO(long peer,int id){} }
public static class PrivateArea {public static bool Allowed=true;public static bool CheckAccess(UnityEngine.Vector3 p,float r,bool a,bool b)=>Allowed;}
public static class Hash {public static int GetStableHashCode(this string value)=>value.GetHashCode();}
namespace Helmsman {public class Plugin {public static Plugin Instance=new();public static List<string> Messages=new();public static void Message(string text)=>Messages.Add(text);public void Error(Exception error){} }}
