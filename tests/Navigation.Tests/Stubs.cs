using N=System.Numerics;
namespace UnityEngine {
 public class Object {public string name="object";public static implicit operator bool(Object o)=>o!=null;}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponentInParent<T>()where T:Component=>gameObject.GetComponent<T>()??transform.parent?.GetComponentInParent<T>();public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>gameObject.GetComponentsInChildren<T>();}
 public class GameObject:Object {public bool activeSelf=true;public Transform transform;readonly List<Component> parts=new();public GameObject(){transform=new Transform{gameObject=this};parts.Add(transform);}public T AddComponent<T>()where T:Component,new(){var p=new T{gameObject=this};parts.Add(p);return p;}public T GetComponent<T>()where T:Component=>parts.OfType<T>().FirstOrDefault();public T[] GetComponentsInChildren<T>()where T:Component=>parts.OfType<T>().Concat(transform.children.SelectMany(t=>t.gameObject.GetComponentsInChildren<T>())).ToArray();}
 public class Transform:Component {public Transform root=>parent?parent.root:this;public Transform parent;public List<Transform> children=new();public Vector3 localPosition,localScale=Vector3.one;public Quaternion localRotation=Quaternion.identity;public Vector3 position=>TransformPoint(Vector3.zero);public Quaternion rotation=>parent?parent.rotation*localRotation:localRotation;public void SetParent(Transform t){parent=t;t.children.Add(this);}public Vector3 TransformPoint(Vector3 p){p=localPosition+localRotation*Vector3.Scale(p,localScale);return parent?parent.TransformPoint(p):p;}public bool IsChildOf(Transform t)=>this==t || parent&&parent.IsChildOf(t);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>MathF.Sqrt(sqrMagnitude);public Vector3 normalized=>this*(1/magnitude);public static Vector3 up=>new(0,1,0);public static Vector3 forward=>new(0,0,1);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public float this[int i]{get=>i==0?x:i==1?y:z;set{if(i==0)x=value;else if(i==1)y=value;else z=value;}}public static Vector3 zero=>new(0,0,0);public static Vector3 one=>new(1,1,1);public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator *(Vector3 a,float s)=>new(a.x*s,a.y*s,a.z*s);}
 public struct Quaternion {public N.Quaternion q;public static Quaternion LookRotation(Vector3 v)=>Euler(0,MathF.Atan2(v.x,v.z)*180/MathF.PI,0);public static Quaternion identity=>new(){q=N.Quaternion.Identity};public static Quaternion Inverse(Quaternion a)=>new(){q=N.Quaternion.Inverse(a.q)};public static Quaternion Euler(float x,float y,float z)=>new(){q=N.Quaternion.CreateFromYawPitchRoll(y*MathF.PI/180,x*MathF.PI/180,z*MathF.PI/180)};public static Quaternion operator *(Quaternion a,Quaternion b)=>new(){q=a.q*b.q};public static Vector3 operator *(Quaternion a,Vector3 b){var r=N.Vector3.Transform(new(b.x,b.y,b.z),a.q);return new(r.X,r.Y,r.Z);}}
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 extents=>size*.5f;public void Encapsulate(Vector3 p){var lo=center-extents;var hi=center+extents;for(int i=0;i<3;i++){lo[i]=Math.Min(lo[i],p[i]);hi[i]=Math.Max(hi[i],p[i]);}center=(lo+hi)*.5f;size=hi-lo;}}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>MathF.Sqrt(x*x+y*y);}
 public struct RaycastHit {public Collider collider;}
 public class Collider:Component {public bool enabled=true,isTrigger;}
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
  public static Collider[] Nearby=Array.Empty<Collider>();public static Vector3 QueryCenter,QueryHalf,PartPosition;public static Quaternion PartRotation;public static int Calls;public static Func<Collider,Collider,float> Penetration=(a,b)=>0;
  public static Collider[] OverlapBox(Vector3 center,Vector3 half,Quaternion rotation,int mask,QueryTriggerInteraction q){QueryCenter=center;QueryHalf=half;return Nearby;}
  public static bool ComputePenetration(Collider a,Vector3 p,Quaternion r,Collider b,Vector3 bp,Quaternion br,out Vector3 direction,out float depth){Calls++;PartPosition=p;PartRotation=r;direction=Vector3.zero;depth=Penetration(a,b);return depth>0;}
 }
}
public class Ship:UnityEngine.Component {}
public class Character:UnityEngine.Component {}
public class Heightmap:UnityEngine.Component {public static float Ground=25;public static bool Available=true;public static bool GetHeight(UnityEngine.Vector3 p,out float h){h=Ground;return Available;}}
public class ZoneSystem:UnityEngine.Object {public static ZoneSystem instance=new();public float m_waterLevel=30;public Func<UnityEngine.Vector3,bool> Loaded=p=>true;public bool IsZoneLoaded(UnityEngine.Vector3 p)=>Loaded(p);}
public class Fish:UnityEngine.Component {}
public class Piece:UnityEngine.Component {}
public class WorldGenerator {public static WorldGenerator instance=new();public float GetHeight(UnityEngine.Vector3 p)=>Heightmap.Ground;}
namespace Helmsman {
 public static class RockClearing {public static HashSet<UnityEngine.Collider> Eligible=new();public static bool CanPlanThrough(UnityEngine.Collider c)=>Eligible.Contains(c);}
 public class GullGuide:UnityEngine.Component {}
 public class ShipProfile {public float Width=4,Length=10,Draft=1,Margin=.2f,AirHeight=12,TurnRadius=16;public UnityEngine.Vector3 Center,MastCenter;public static ShipProfile For(Ship ship)=>new();}
 public class Berth {public bool ValidData=true;public UnityEngine.Vector3 Approach,position,Exit;public float heading;}
}

public class ItemDrop:UnityEngine.Component {}
