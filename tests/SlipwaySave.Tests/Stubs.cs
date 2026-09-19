using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace UnityEngine;
public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
// Reproduce the observed live save failure on both serialization directions.
public static class JsonUtility {
 public static bool DropNested=true;
 public static string ToJson(object value){var j=JObject.FromObject(value);if(DropNested&&value is Helmsman.SlipwayOrder)j.Remove("order");return j.ToString(Formatting.None);}
 public static T FromJson<T>(string raw){var j=JObject.Parse(raw);if(DropNested&&typeof(T)==typeof(Helmsman.SlipwayOrder))j.Remove("order");return j.ToObject<T>()!;}
}
