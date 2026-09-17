namespace HarmonyLib {
[System.AttributeUsage(System.AttributeTargets.Class)]
public class HarmonyPatch:System.Attribute {public HarmonyPatch(System.Type type,string method){} }
}
public class Player {}
public class Piece { public bool m_noClipping,m_waterPiece; }
namespace UnityEngine {
public class Object {public static implicit operator bool(Object value)=>value!=null;}
public class MonoBehaviour:Object {}
public record struct Vector3(float x,float y,float z);
public struct Bounds {public Vector3 center,size;}
public class BoxCollider:MonoBehaviour {public Vector3 center,size;}
public class Transform {
 public readonly List<Transform> Children=new();
 public GameObject gameObject;
 public bool WorldPositionStays;
 public void SetParent(Transform parent,bool worldPositionStays){parent.Children.Add(this);WorldPositionStays=worldPositionStays;}
}
public class GameObject:Object {
 public int layer;
 public readonly Transform transform;
 private readonly Dictionary<System.Type,object> components=new();
 public GameObject(string name){transform=new Transform{gameObject=this};}
 public T GetComponent<T>()=>components.TryGetValue(typeof(T),out var value)?(T)value:default;
 public T AddComponent<T>() where T:new(){var value=new T();components[typeof(T)]=value;return value;}
}
}
