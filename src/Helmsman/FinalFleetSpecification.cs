using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Helmsman;

internal static partial class FinalFleetModels
{
    [Serializable] internal sealed class Point {public string kind="";public float[] position=Array.Empty<float>(),exit=Array.Empty<float>(),facing=Array.Empty<float>();}
    [Serializable] internal sealed class Solid {public float[] position=Array.Empty<float>(),size=Array.Empty<float>();}
    [Serializable] internal sealed class HullSolid {public float[] vertices=Array.Empty<float>();public int[] triangles=Array.Empty<int>();}
    [Serializable] internal sealed class Sheet {public float[] head=Array.Empty<float>(),foot=Array.Empty<float>();}
    [Serializable] internal sealed class Spec
    {
        public string prefab="",name="";
        public float length=0,beam=0,walkHeight=0,waterline=0,airHeight=0,cargoHeight=0;
        public float[] sailPivot=Array.Empty<float>(),rudderPivot=Array.Empty<float>();
        public HullSolid[] hullSolids=Array.Empty<HullSolid>(),cargoSolids=Array.Empty<HullSolid>();
        public Point[] points=Array.Empty<Point>();public Solid[] colliders=Array.Empty<Solid>();public Sheet[] sheets=Array.Empty<Sheet>();
    }
    // Decode authored metadata with the game's managed serializer, so runtime
    // loading and the standalone resource tests exercise the same parser.
    internal static Spec ReadSpecification(string json,string expected)
    {
        var spec=JsonConvert.DeserializeObject<Spec>(json)??throw new InvalidDataException("Missing final ship specification: "+expected);
        int helms=spec.points?.Count(p=>p!=null&&p.kind=="helm")??0;
        if(spec.prefab!=expected||!Finite(spec.length)||spec.length<4||spec.length>30||!Finite(spec.beam)||spec.beam<1||spec.beam>10||helms!=1)
            throw new InvalidDataException($"Invalid final ship specification for {expected}: prefab={spec.prefab}, length={spec.length}, beam={spec.beam}, helms={helms}");
        spec.cargoSolids??=Array.Empty<HullSolid>();
        if(spec.hullSolids==null||spec.colliders==null||spec.sheets==null)
            throw new InvalidDataException("Missing final ship geometry: "+expected);
        return spec;
    }
    private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
