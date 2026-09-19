using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Helmsman;

internal static class WorkshopModels
{
    private static readonly List<UnityEngine.Object> owned=new();
    private static int Count(BinaryReader r,int max){int n=r.ReadInt32();if(n<0||n>max)throw new InvalidDataException("Invalid workshop asset count");return n;}
    private static float Number(BinaryReader r){float n=r.ReadSingle();if(float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>100000)throw new InvalidDataException("Invalid workshop coordinate");return n;}
    private static Vector3 V(BinaryReader r)=>new(Number(r),Number(r),Number(r));
    private static string Text(BinaryReader r){int n=Count(r,16384);var bytes=r.ReadBytes(n);if(bytes.Length!=n)throw new EndOfStreamException();return Encoding.UTF8.GetString(bytes);}
    internal static void Apply(GameObject root,string key)
    {
        foreach(var r in root.GetComponentsInChildren<Renderer>(true)){r.enabled=false;r.forceRenderingOff=true;}
        // Native workbench EffectAreas require the colliders replaced below.
        foreach(var area in root.GetComponentsInChildren<EffectArea>(true))UnityEngine.Object.DestroyImmediate(area);
        foreach(var c in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var l in root.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(l);
        foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.CompareTag("snappoint"))UnityEngine.Object.DestroyImmediate(t.gameObject);
        using var resource=typeof(Plugin).Assembly.GetManifestResourceStream("Helmsman.Workshop."+key+".bin.gz")??throw new InvalidDataException("Missing workshop "+key);
        using var zip=new GZipStream(resource,CompressionMode.Decompress);using var reader=new BinaryReader(zip);
        string format=Encoding.ASCII.GetString(reader.ReadBytes(4));
        if(format!="HMW1"&&format!="HMW2"&&format!="HMW3")throw new InvalidDataException("Unknown workshop asset");
        HarborRigSpec? rig=format=="HMW3"?JsonUtility.FromJson<HarborRigSpec>(Text(reader)):null;
        var materials=new Material[Count(reader,64)];
        for(int i=0;i<materials.Length;i++){string name=Text(reader);var color=new Color(Number(reader),Number(reader),Number(reader),Number(reader));materials[i]=Surface(name,color);owned.Add(materials[i]);}
        int parts=Count(reader,128);var bounds=new Bounds();bool first=true;
        for(int p=0;p<parts;p++)
        {
            string tag=format!="HMW1"?Text(reader):"static";
            int mi=Count(reader,materials.Length-1),n=Count(reader,1000000);var vertices=new Vector3[n];var normals=new Vector3[n];var uv=new Vector2[n];var weights=rig!=null?new Vector2[n]:null;
            for(int i=0;i<n;i++){vertices[i]=V(reader);normals[i]=V(reader);uv[i]=new Vector2(Number(reader),Number(reader));if(weights!=null)weights[i]=new Vector2(Number(reader),0);}
            int nt=Count(reader,3000000);if(nt%3!=0)throw new InvalidDataException("Invalid workshop triangles");var indices=new int[nt];
            for(int i=0;i<nt;i++)indices[i]=Count(reader,n-1);
            var mesh=new Mesh{name="Helmsman "+key,indexFormat=n>65535?IndexFormat.UInt32:IndexFormat.UInt16};owned.Add(mesh);
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;if(weights!=null)mesh.uv2=weights;mesh.triangles=indices;mesh.RecalculateBounds();mesh.RecalculateTangents();
            if(first){bounds=mesh.bounds;first=false;}else bounds.Encapsulate(mesh.bounds);
            var go=new GameObject("Authored "+key+" "+tag+" "+p);go.layer=root.layer;go.transform.SetParent(root.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[mi];
            // Stationary solids use the authored surface, leaving the launch corridor open.
            if(!root.GetComponent<ItemDrop>()&&tag=="static")go.AddComponent<MeshCollider>().sharedMesh=mesh;
            if(rig!=null&&tag.StartsWith("roller",StringComparison.Ordinal))
            {
                var solid=new GameObject("Stationary roller surface");solid.layer=root.layer;solid.transform.SetParent(root.transform,false);
                solid.AddComponent<MeshCollider>().sharedMesh=mesh;
            }
        }
        int snaps=Count(reader,512);
        var snapTransforms=new List<Transform>();
        for(int i=0;i<snaps;i++){var go=new GameObject(Text(reader));go.transform.SetParent(root.transform,false);go.transform.localPosition=V(reader);go.tag="snappoint";snapTransforms.Add(go.transform);}
        if(key=="slipway")SlipwaySnapping.OrderCorners(snapTransforms);
        if(reader.BaseStream.ReadByte()!=-1)throw new InvalidDataException("Trailing workshop data");
        if(root.GetComponent<ItemDrop>()){var box=root.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;return;}
        if(rig!=null)root.AddComponent<HarborMachinery>().Spec=rig;
        var proxy=root.GetComponent<ShoreBuildBounds>()??root.AddComponent<ShoreBuildBounds>();proxy.Bounds=bounds;proxy.RelaxedDockPlacement=key=="slipway";
    }
    private static Material Surface(string name,Color color)
    {
        string n=name.ToLowerInvariant();
        bool plain=n.Contains("iron")||n.Contains("bronze")||n.Contains("clay")||n.Contains("stoneware")||n.Contains("pigment")||n.Contains("fish")||n.Contains("resin")||n.Contains("leather");
        var material=plain?ImportedShipMaterials.BoatyardMaterial(color.gamma,false):ImportedShipMaterials.FleetMaterial(name,color);
        material.name="Helmsman workshop "+name;
        if(n.StartsWith("workshop ")&&!plain)material.color=Color.Lerp(Color.white,color.gamma,.65f);
        if(n.Contains("weathered dock timber"))material.color=Color.Lerp(Color.white,color.gamma,.35f);
        // Pots, buckles and fish must not inherit the hull's wood grain normal map.
        if(plain&&material.HasProperty("_BumpMap"))material.SetTexture("_BumpMap",null);
        // The sewn demonstration canvas is a single sheet, visible from either side.
        if(n.Contains("canvas")&&material.HasProperty("_Cull"))material.SetFloat("_Cull",0);
        return material;
    }
    internal static void Release(){foreach(var o in owned)if(o)UnityEngine.Object.Destroy(o);owned.Clear();}
}
