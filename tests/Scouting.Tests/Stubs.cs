using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
namespace UnityEngine {
 public class Object { public static implicit operator bool(Object o)=>o!=null; }
 public class MonoBehaviour:Object {
  public Transform transform=new(); public List<IEnumerator> Routines=new();
  public void StartCoroutine(IEnumerator e){if(e.MoveNext())Routines.Add(e);}
  public void StopAllCoroutines()=>Routines.Clear();
  public void Tick(){foreach(var e in Routines.ToArray())if(!e.MoveNext())Routines.Remove(e);}
  public T GetComponent<T>()=>this is T t?t:default;
 }
 public class Transform {public Vector3 position;}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>new();[JsonIgnore]public float sqrMagnitude=>x*x+y*y+z*z;
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
 }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float sqrMagnitude=>x*x+y*y;}
 public class GameObject:MonoBehaviour {public object Component;public new T GetComponent<T>()=>Component is T t?t:default;}
 public static class Time {public static float unscaledTime;}
 public static class Mathf {public static float Abs(float f)=>Math.Abs(f);public static int CeilToInt(float f)=>(int)Math.Ceiling(f);public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);}
 public class Texture2D {public int Applies;public void Apply()=>Applies++;}
 public static class JsonUtility {
  static readonly JsonSerializerOptions options=new(){IncludeFields=true};
  public static string ToJson(object o)=>JsonSerializer.Serialize(o,options);
  public static T FromJson<T>(string s)=>JsonSerializer.Deserialize<T>(s,options);
 }
}
namespace HarmonyLib {
 public static class AccessTools {
  public delegate ref F FieldRef<T,F>(T instance);
  public static MethodInfo Method(Type type,string name,Type[] args)=>type.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,args,null);
  public static T MethodDelegate<T>(MethodInfo method) where T:Delegate=>(T)Delegate.CreateDelegate(typeof(T),method);
  public static FieldRef<T,F> FieldRefAccess<T,F>(string name)=> (T instance)=>ref Field<T,F>(instance,name);
  static ref F Field<T,F>(T instance,string name) {
   var map=(Minimap)(object)instance;
   if(name=="m_fogTexture")return ref Unsafe.As<UnityEngine.Texture2D,F>(ref map.m_fogTexture);
   if(name=="m_pins")return ref Unsafe.As<List<Minimap.PinData>,F>(ref map.m_pins);
   throw new Exception(name);
  }
 }
}
namespace BepInEx {public static class Paths {public static string ConfigPath="";}}
namespace BepInEx.Configuration {
 public class ConfigEntry<T> {public T Value;public ConfigEntry(T t){Value=t;}}
 public class ConfigFile {public ConfigEntry<T> Bind<T>(string s,string k,T value,ConfigDescription d)=>new(value);}
 public class ConfigDescription {public ConfigDescription(string s,object o,object tag){}}
 public class AcceptableValueRange<T> {public AcceptableValueRange(T a,T b){}}
}
public readonly record struct ZDOID(long UserID,uint ID) {public static ZDOID None=>new();}
public class ZDO {public ZDOID m_uid;public long PlayerID;public UnityEngine.Vector3 Position;public int Prefab=1;public long GetLong(int key)=>PlayerID;public UnityEngine.Vector3 GetPosition()=>Position;public int GetPrefab()=>Prefab;}
public static class ZDOVars {public const int s_playerID=1;}
public class ZDOMan {public static ZDOMan instance=new();public Dictionary<ZDOID,ZDO> Data=new();public ZDO GetZDO(ZDOID id)=>Data.GetValueOrDefault(id);}
public class ZNetView:UnityEngine.Object {public ZDO Data;public bool IsValid()=>Data!=null;public ZDO GetZDO()=>Data;}
public class ZNetPeer {public long m_uid;public ZDOID m_characterID;public bool IsReady()=>true;}
public class ZNet:UnityEngine.Object {
 public static ZNet instance=new();public bool Server=true;public long World=7;public double Clock;
 public Dictionary<long,ZNetPeer> Peers=new();public bool IsServer()=>Server;public long GetWorldUID()=>World;public static long GetUID()=>42;
 public double GetTimeSeconds()=>Clock;public ZNetPeer GetPeer(long uid)=>Peers.GetValueOrDefault(uid);public ZNetPeer GetServerPeer()=>Server?null:new(){m_uid=999};
}
public class Player:UnityEngine.MonoBehaviour {public static Player m_localPlayer=new();public long ID=10;public bool Dead;public Dictionary<string,string> m_customData=new();public long GetPlayerID()=>ID;public bool IsDead()=>Dead;}
public class MapTable:UnityEngine.MonoBehaviour {public ZNetView View;public new T GetComponent<T>()=>View is T t?t:default;}
public class ZNetScene:UnityEngine.Object {
 public static ZNetScene instance=new();public Dictionary<ZDOID,UnityEngine.GameObject> Live=new();public Dictionary<int,UnityEngine.GameObject> Prefabs=new();
 public UnityEngine.GameObject GetPrefab(int id)=>Prefabs.GetValueOrDefault(id);public UnityEngine.GameObject FindInstance(ZDOID id)=>Live.GetValueOrDefault(id);
}
public class WorldGenerator {public static WorldGenerator instance=new();public Func<float,float,float> Height=(x,z)=>Math.Abs(x)<=24&&Math.Abs(z)<=24?35:20;public float GetHeight(float x,float z)=>Height(x,z);}
public class ZoneSystem:UnityEngine.Object {
 public static ZoneSystem instance=new();public Dictionary<int,LocationInstance> m_locationInstances=new();
 public class LocationInstance {public ZoneLocation m_location;public UnityEngine.Vector3 m_position;}
 public class ZoneLocation {public string m_name;}
}
public class PrivateArea {public static bool Access=true;public static bool CheckAccess(UnityEngine.Vector3 p,float r,bool f,bool w)=>Access;}
public class ZPackage {
 readonly MemoryStream stream=new();readonly BinaryWriter writer;readonly BinaryReader reader;
 public ZPackage(){writer=new(stream,Encoding.UTF8,true);reader=new(stream,Encoding.UTF8,true);}
 public void SetPos(int pos)=>stream.Position=pos;
 public void Write(string s)=>writer.Write(s);public void Write(long n)=>writer.Write(n);
 public void Write(ZDOID id){writer.Write(id.UserID);writer.Write(id.ID);}
 public void Write(byte[] bytes){writer.Write(bytes.Length);writer.Write(bytes);}
 public string ReadString()=>reader.ReadString();public long ReadLong()=>reader.ReadInt64();public ZDOID ReadZDOID()=>new(reader.ReadInt64(),reader.ReadUInt32());public byte[] ReadByteArray()=>reader.ReadBytes(reader.ReadInt32());
}
public class ZRoutedRpc {
 public static ZRoutedRpc instance=new();public Dictionary<string,Action<long,ZPackage>> Handlers=new();public List<(long Peer,string Name,ZPackage Data)> Sent=new();
 public void Register<T>(string n,Action<long,T> a)=>Handlers[n]=(p,q)=>a(p,(T)(object)q);
 public void InvokeRoutedRPC(long peer,string name,ZPackage pkg){pkg.SetPos(0);Sent.Add((peer,name,pkg));}
}
public class Minimap:UnityEngine.Object {
 public static Minimap instance=new();public int m_textureSize=128;public float m_pixelSize=4;
 public UnityEngine.Texture2D m_fogTexture=new();public List<PinData> m_pins=new();public HashSet<(int,int)> Explored=new();public int Saves;
 public enum PinType {Icon3} public class PinData {public string m_name;public UnityEngine.Vector3 m_pos;public bool m_save;}
 private bool Explore(int x,int y)=>Explored.Add((x,y));public void SaveMapData()=>Saves++;
 public void AddPin(UnityEngine.Vector3 p,PinType t,string n,bool save,bool check)=>m_pins.Add(new(){m_name=n,m_pos=p,m_save=save});
}
public class Game:UnityEngine.Object {public static Game instance=new();}
namespace Helmsman {
 public class ConfigurationManagerAttributes {public bool IsAdminOnly;}
 public class Plugin:UnityEngine.Object {
  public static Plugin Instance=new();public BepInEx.Configuration.ConfigFile Config=new();public UnityEngine.Object Voyage,Summon,CalledGull;public IslandScouting Scout;
  public List<Exception> Errors=new();public static List<string> Messages=new();public void Error(Exception e)=>Errors.Add(e);public static void Message(string s)=>Messages.Add(s);
  public T GetComponent<T>()=>Scout is T t?t:default;
 }
 public class DockMarker:UnityEngine.MonoBehaviour {public bool Ready=true;public ZDOID Id;}
 public class DockInfo {public UnityEngine.Vector3 MarkerPosition;public Berth Berth=new();}
 public class Berth {public string name="Test island";}
 public static class DockDirectory {public static Dictionary<ZDOID,DockInfo> Docks=new();public static DockInfo Resolve(ZDOID id)=>Docks.GetValueOrDefault(id);}
 public static class WaterChart {public const float Sea=30;}
 public static class Shipyard {public static string FormatDuration(double seconds)=>seconds.ToString();}
 public static class ScoutAccess {public static long DeniedOwner;public static bool Allowed(long owner,UnityEngine.Vector3 p)=>owner!=DeniedOwner;}
 public static class ScoutGullPresentation {public static int Departures;internal static void Depart(ScoutJob job)=>Departures++;}
}
