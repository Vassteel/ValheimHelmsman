using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Jotunn.Managers;
using Object=UnityEngine.Object;

namespace Helmsman;

// Source hierarchy, network identity, inventories and animation components stay in
// place. Ship physics is preserved; rebuilt shore structures receive matching solids.
internal static class BoatyardModels
{
    private sealed class Target
    {
        internal int[] Route=Array.Empty<int>();
        internal string Name="";
        internal Transform Resolve(Transform root)
        {
            foreach(int index in Route)
            {
                if(index<0 || index>=root.childCount)throw new InvalidDataException("Boatyard hierarchy changed: "+Name);
                root=root.GetChild(index);
            }
            if(root.name!=Name)throw new InvalidDataException("Boatyard model expected "+Name+", found "+root.name);
            return root;
        }
    }
    private sealed class Shape
    {
        internal Target Target=new();
        internal int Expected;
        internal bool Custom;
        internal Mesh Mesh=null!;
    }
    private sealed class Design
    {
        internal readonly List<Shape> Replace=new();
        internal readonly List<Target> Hide=new();
        internal Shape? Extra;
    }
    private static readonly Dictionary<string,Design> designs=new(StringComparer.Ordinal);
    private static readonly List<Mesh> meshes=new();
    private static Material[]? palette;
    private static readonly List<Sprite> icons=new();
    private static int Count(BinaryReader reader,int maximum)
    {
        int n=reader.ReadInt32();if(n<0||n>maximum)throw new InvalidDataException("Invalid boatyard model count.");return n;
    }
    private static string Text(BinaryReader reader)
    {
        int size=Count(reader,4096);var bytes=reader.ReadBytes(size);
        if(bytes.Length!=size)throw new EndOfStreamException();return Encoding.UTF8.GetString(bytes);
    }
    private static Target ReadTarget(BinaryReader reader)
    {
        var target=new Target{Route=new int[Count(reader,64)]};
        for(int i=0;i<target.Route.Length;i++)target.Route[i]=Count(reader,100000);
        target.Name=Text(reader);return target;
    }
    private static float Number(BinaryReader reader)
    {
        float n=reader.ReadSingle();if(float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>100000)throw new InvalidDataException("Invalid boatyard coordinate.");return n;
    }
    private static Shape ReadShape(BinaryReader reader)
    {
        var shape=new Shape{Target=ReadTarget(reader),Expected=Count(reader,2000000),Custom=Count(reader,1)==1};
        int count=Count(reader,2000000);var vertices=new Vector3[count];var uv=new Vector2[count];
        for(int i=0;i<count;i++)vertices[i]=new Vector3(Number(reader),Number(reader),Number(reader));
        for(int i=0;i<count;i++)uv[i]=new Vector2(Number(reader),Number(reader));
        int groups=Count(reader,32);var triangles=new int[groups][];
        for(int g=0;g<groups;g++)
        {
            int n=Count(reader,12000000);if(n%3!=0)throw new InvalidDataException("Invalid boatyard triangle count.");
            triangles[g]=new int[n];for(int i=0;i<n;i++){int v=reader.ReadInt32();if(v<0||v>=count)throw new InvalidDataException("Invalid boatyard vertex index.");triangles[g][i]=v;}
        }
        var mesh=new Mesh{name="Helmsman boatyard "+shape.Target.Name,indexFormat=count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
        meshes.Add(mesh);mesh.vertices=vertices;mesh.uv=uv;mesh.subMeshCount=groups;
        for(int g=0;g<groups;g++)mesh.SetTriangles(triangles[g],g);
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();shape.Mesh=mesh;return shape;
    }
    private static void Load()
    {
        if(designs.Count>0)return;
        using var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Helmsman.Ships.boatyard")??throw new InvalidOperationException("Missing boatyard models.");
        try
        {
            using var gzip=new GZipStream(stream,CompressionMode.Decompress);using var reader=new BinaryReader(gzip);
            if(Encoding.ASCII.GetString(reader.ReadBytes(4))!="HMD1")throw new InvalidDataException("Unknown boatyard model format.");
            int count=Count(reader,128);
            for(int i=0;i<count;i++)
            {
                string name=Text(reader);var design=new Design();int replacements=Count(reader,2048);
                for(int j=0;j<replacements;j++)design.Replace.Add(ReadShape(reader));
                int hides=Count(reader,2048);for(int j=0;j<hides;j++)design.Hide.Add(ReadTarget(reader));
                if(Count(reader,1)==1)design.Extra=ReadShape(reader);
                designs.Add(name,design);
            }
            if(reader.BaseStream.ReadByte()!=-1)throw new InvalidDataException("Trailing boatyard model data.");
        }
        catch{Release();throw;}
    }
    private static Material[] Palette()
    {
        if(palette!=null)return palette;
        var colors=new[]{new Color(.43f,.29f,.16f),new Color(.58f,.41f,.23f),new Color(.16f,.19f,.19f),new Color(.52f,.44f,.29f),new Color(.74f,.69f,.52f),new Color(.24f,.35f,.38f),new Color(.45f,.52f,.31f)};
        palette=colors.Select((c,i)=>ImportedShipMaterials.BoatyardMaterial(c,i<2)).ToArray();return palette;
    }
    internal static void Apply(GameObject prefab,string source)
    {
        Load();if(!designs.TryGetValue(source,out var design))throw new InvalidOperationException("No boatyard design for "+source);
        // Validate every target before changing anything. Mismatched bundles fail registration
        // explicitly instead of making only part of a ship invisible.
        var replacements=design.Replace.Select(s=>(Shape:s,Node:s.Target.Resolve(prefab.transform))).ToArray();
        var hidden=design.Hide.Select(t=>t.Resolve(prefab.transform).GetComponent<Renderer>()).ToArray();
        foreach(var pair in replacements)
        {
            var filter=pair.Node.GetComponent<MeshFilter>();var renderer=pair.Node.GetComponent<MeshRenderer>();
            if(!filter||!filter.sharedMesh||!renderer)throw new InvalidDataException("Boatyard mesh missing: "+pair.Shape.Target.Name);
            if(pair.Shape.Expected>0 && filter.sharedMesh.vertexCount!=pair.Shape.Expected)throw new InvalidDataException("Boatyard source mesh changed: "+pair.Shape.Target.Name);
        }
        if(hidden.Any(r=>!r))throw new InvalidDataException("Boatyard renderer is missing for "+source);
        var materials=Palette();
        foreach(var pair in replacements)
        {
            pair.Node.GetComponent<MeshFilter>().sharedMesh=pair.Shape.Mesh;
            if(pair.Shape.Custom)pair.Node.GetComponent<MeshRenderer>().sharedMaterials=materials;
        }
        foreach(var renderer in hidden){renderer.enabled=false;renderer.forceRenderingOff=true;}
        if(design.Extra!=null)
        {
            var visual=new GameObject("Helmsman boatyard design");visual.transform.SetParent(prefab.transform,false);visual.layer=prefab.layer;
            visual.AddComponent<MeshFilter>().sharedMesh=design.Extra.Mesh;
            var renderer=visual.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;
            renderer.receiveShadows=true;renderer.shadowCastingMode=ShadowCastingMode.On;
            // Rebuilt shore structures need matching solids; retaining the source
            // treadwheel/frame colliders would leave invisible obstacles. Triggers
            // and the fishing chest keep their own interaction/collision components.
            if(prefab.GetComponent<Piece>()&&!prefab.GetComponent<Ship>())
            {
                var solid=visual.AddComponent<MeshCollider>();solid.sharedMesh=design.Extra.Mesh;
                foreach(var collider in prefab.GetComponentsInChildren<Collider>(true))
                    if(collider!=solid&&!collider.isTrigger&&!collider.GetComponentInParent<Container>())Object.DestroyImmediate(collider);
                // Native placement ignores non-convex meshes. Supply a preview-only
                // solid without filling the finished crane/press with an invisible box.
                prefab.AddComponent<ShoreBuildBounds>().Bounds=design.Extra.Mesh.bounds;
            }
        }
    }
    internal static void RefreshIcon(GameObject prefab)
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
        try
        {
            var icon=RenderManager.Instance.Render(new RenderManager.RenderRequest(prefab){Rotation=RenderManager.IsometricRotation,Width=128,Height=128,UseCache=true,TargetPlugin=Plugin.Instance.Info.Metadata,ParticleSimulationTime=-1});
            if(!icon)return;
            icons.Add(icon);
            var item=prefab.GetComponent<ItemDrop>();if(item)item.m_itemData.m_shared.m_icons=new[]{icon};
            var piece=prefab.GetComponent<Piece>();if(piece)piece.m_icon=icon;
        }
        catch(Exception ex){Plugin.Instance.Record("Boatyard icon unavailable for "+prefab.name+": "+ex.Message);}
    }
    internal static void Release()
    {
        foreach(var mesh in meshes)if(mesh)Object.Destroy(mesh);meshes.Clear();designs.Clear();
        foreach(var icon in icons)if(icon){if(icon.texture)Object.Destroy(icon.texture);Object.Destroy(icon);}icons.Clear();
        if(palette!=null)foreach(var material in palette)if(material)Object.Destroy(material);palette=null;
    }
}
