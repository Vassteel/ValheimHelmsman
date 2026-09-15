using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
public class Object {
 public bool destroyed; public static implicit operator bool(Object o)=>o!=null&&!o.destroyed;
 public static List<Object> All=new();public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T:Object=>All.OfType<T>().Where(o=>o).ToArray();
 public static void Destroy(Object o){o.destroyed=true;}
}
public enum FindObjectsSortMode {None}
public static class Time {public static float unscaledTime;}
public class Transform:Object {public Vector3 position,forward=new(0,0,1);}
public struct Vector3 {
 public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
 public static Vector3 up=>new(0,1,0);
 public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
 public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
 public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
 public static float Distance(Vector3 a,Vector3 b){var d=a-b;return (float)Math.Sqrt(d.x*d.x+d.y*d.y+d.z*d.z);}
}
public class GameObject:Object {
 public Transform transform=new();
 public T AddComponent<T>() where T:MonoBehaviour,new(){var c=new T{gameObject=this};All.Add(c);return c;}
}
public class MonoBehaviour:Object {public GameObject gameObject=new();public Transform transform=>gameObject.transform;}
}
public class Humanoid:UnityEngine.MonoBehaviour {}
public class Player:Humanoid {public static Player m_localPlayer=new();public bool Dead;public bool IsDead()=>Dead;}
public class Ship:UnityEngine.MonoBehaviour {public bool Owned=true,Aboard=true,Supported=true;public bool IsOwner()=>Owned;public bool IsPlayerInBoat(Player p)=>Aboard;}
public class Chair:UnityEngine.MonoBehaviour {
 public UnityEngine.Transform m_attachPoint=new();public float m_useDistance=2;public Ship Ship;public bool m_inShip=true;public string m_attachAnimation="attach_mast";public int VanillaUses;public bool Throw;
 public T GetComponentInParent<T>() where T:class=>Ship as T;
 public bool Interact(Humanoid user,bool hold,bool alt){bool result=false;if(!Helmsman.MastInteraction.Intercept(this,user,hold,alt,ref result))return result;VanillaUses++;if(Throw)throw new InvalidOperationException();return true;}
}
namespace Helmsman {
internal static class ShipProfile {internal static bool Supports(Ship s)=>s&&s.Supported;}
public class DockRecord {}
public class SummonRequest:UnityEngine.MonoBehaviour {}
public class DockMarker:UnityEngine.MonoBehaviour {public bool Ready=true;public int Id=1;public GullGuide Guide;internal GullGuide CallGuide(Ship ship){Guide.ReserveDock(Id);Guide.Visit(ship);return Guide;}}
public class GullGuide:UnityEngine.MonoBehaviour {
 public Ship Ship;public bool Landed;public int FlyAwayCount;public static int Created;public static Dictionary<int,GullGuide> Homes=new();
 internal static GullGuide Traveller(int id)=>Homes.TryGetValue(id,out var g)&&g?g:null;
 internal void ReserveDock(int id)=>Homes[id]=this;
 internal static GullGuide Create(UnityEngine.Vector3 start,DockMarker dock,Voyage voyage){Created++;return new GullGuide();}
 internal void Visit(Ship ship){Ship=ship;Landed=false;}
 internal bool ReadyOn(Ship ship)=>Ship==ship&&Landed;
 internal void FlyAway(){FlyAwayCount++;}
}
public class Voyage:UnityEngine.MonoBehaviour {
 public Ship Ship;public bool Unattended,GullReady;public static bool Allow=true;public static GullGuide Transferred;
 internal static bool BeginAboard(Ship ship,DockRecord to,GullGuide guide,out string reason){reason=Allow?"Sailing":"Blocked";if(!Allow)return false;Transferred=guide;Plugin.Instance.Voyage=new Voyage{Ship=ship};return true;}
}
public class HelmsmanUI {public int MastOpens,OrdersOpens,VisitOpens;internal void OpenMast(Chair chair,Ship ship)=>MastOpens++;internal void OpenVoyage()=>OrdersOpens++;internal void OpenCalledGull(GullCall call)=>VisitOpens++;}
public class Plugin {public static Plugin Instance=new();public static bool Solo=true;public HelmsmanUI UI=new();public Voyage Voyage;public SummonRequest Summon;public GullCall CalledGull;public static string LastMessage;internal static void Message(string text)=>LastMessage=text;internal void Error(Exception error){} }
}
