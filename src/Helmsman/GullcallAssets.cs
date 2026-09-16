using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Helmsman;

// Original mesh and matching orthographic icon, embedded so an installed DLL is self-contained.
internal static class GullcallAssets
{
    [Serializable] private sealed class Model { public Part[] parts=Array.Empty<Part>(); }
    [Serializable] private sealed class Part
    {
        public string name="";
        public Color color=Color.white;
        public Vector3[] vertices=Array.Empty<Vector3>();
        public int[] triangles=Array.Empty<int>();
    }
    private static readonly List<UnityEngine.Object> assets=new List<UnityEngine.Object>();
    internal static byte[] Read(string name)
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Helmsman.Gullcall."+name)
            ?? throw new InvalidOperationException("Missing Gullcall asset: "+name);
        using var output=new MemoryStream();stream.CopyTo(output);return output.ToArray();
    }
    internal static Sprite Icon()
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false) {name="Gullcall Whistle icon"};
        if(!ImageConversion.LoadImage(texture,Read("icon.png")))
        {UnityEngine.Object.Destroy(texture);throw new InvalidOperationException("Cannot decode Gullcall icon.");}
        texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;assets.Add(texture);
        var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));
        assets.Add(sprite);return sprite;
    }
    internal static void Attach(GameObject prefab)
    {
        prefab.transform.localScale=Vector3.one;
        // Retain ItemDrop/network/physics from the cloned material item; replace only its visuals.
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)) UnityEngine.Object.DestroyImmediate(renderer);
        foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true)) UnityEngine.Object.DestroyImmediate(filter);
        foreach(var light in prefab.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
        foreach(var collider in prefab.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        var shader=Shader.Find("Custom/Piece");
        if(!shader || !shader.isSupported)shader=Shader.Find("Standard");
        if(!shader || !shader.isSupported)throw new InvalidOperationException("No supported lit shader for Gullcall Whistle.");
        var model=JsonUtility.FromJson<Model>(System.Text.Encoding.UTF8.GetString(Read("model.json")));
        var bounds=new Bounds(model.parts[0].vertices[0],Vector3.zero);
        var root=new GameObject("Gullcall carved model");root.transform.SetParent(prefab.transform,false);
        foreach(var part in model.parts)
        {
            foreach(var vertex in part.vertices)bounds.Encapsulate(vertex);
            var mesh=new Mesh {name="Gullcall "+part.name,vertices=part.vertices,triangles=part.triangles};
            mesh.RecalculateNormals();mesh.RecalculateBounds();assets.Add(mesh);
            var material=new Material(shader) {name="Gullcall "+part.name,color=part.color};
            foreach(var property in new[]{"_EmissionColor","_NoiseGlowColor"})
                if(material.HasProperty(property))material.SetColor(property,Color.black);
            foreach(var property in new[]{"_Glossiness","_Metallic","_ValueNoise","_ValueNoiseVertex","_AddRain","_AddSnow","_NoiseGlowEnabled"})
                if(material.HasProperty(property))material.SetFloat(property,0);
            if(material.HasProperty("_MoveableObject"))material.SetFloat("_MoveableObject",1);
            material.DisableKeyword("_EMISSION");material.DisableKeyword("NOISEGLOW");
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;assets.Add(material);
            var go=new GameObject(part.name);go.transform.SetParent(root.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.receiveShadows=true;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        }
        var box=prefab.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
        var body=prefab.GetComponent<Rigidbody>();if(body)body.mass=.2f;
    }
    internal static void Release()
    {foreach(var asset in assets)if(asset)UnityEngine.Object.Destroy(asset);assets.Clear();}
}
