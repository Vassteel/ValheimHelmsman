namespace HarmonyLib {
[System.AttributeUsage(System.AttributeTargets.Class)]
public class HarmonyPatch:System.Attribute {public HarmonyPatch(System.Type type,string method){} }
}
public class Player {}
public class Piece { public bool m_noClipping,m_waterPiece; }
namespace UnityEngine {
public class Object {public static implicit operator bool(Object value)=>value!=null;}
public class MonoBehaviour:Object {}
public record struct Vector3(float x,float y,float z) {
 public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);
 public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
}
public struct Bounds {public Vector3 center,size;public Vector3 extents=>new(size.x/2,size.y/2,size.z/2);}
public class BoxCollider:MonoBehaviour {public Vector3 center,size;}
public class Transform:Object {
 public string name;
 public Vector3 lossyScale=new(1,1,1);
 public Transform Parent;
 public T GetComponent<T>()=>gameObject.GetComponent<T>();
 public void SetSiblingIndex(int i){Parent.Children.Remove(this);Parent.Children.Insert(Math.Min(i,Parent.Children.Count),this);}
 public readonly List<Transform> Children=new();
 public GameObject gameObject;
 public bool WorldPositionStays;
 public void SetParent(Transform parent,bool worldPositionStays){parent.Children.Add(this);Parent=parent;WorldPositionStays=worldPositionStays;}
}
public class GameObject:Object {
 public int layer;
 public readonly Transform transform;
 private readonly Dictionary<System.Type,object> components=new();
 public GameObject(string name){transform=new Transform{gameObject=this,name=name};}
 public T GetComponent<T>()=>components.TryGetValue(typeof(T),out var value)?(T)value:default;
 public T AddComponent<T>() where T:new(){var value=new T();components[typeof(T)]=value;return value;}
}
}

namespace Helmsman {public class Slipway:UnityEngine.MonoBehaviour {}}
namespace HarmonyLib {
 public class CodeInstruction {
  public System.Reflection.Emit.OpCode opcode;public object operand;
  public CodeInstruction(CodeInstruction o){opcode=o.opcode;operand=o.operand;}
  public CodeInstruction(System.Reflection.Emit.OpCode c,object o=null){opcode=c;operand=o;}
 }
 public static class AccessTools {public static System.Reflection.MethodInfo Method(System.Type t,string name)=>t.GetMethod(name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);}
}
