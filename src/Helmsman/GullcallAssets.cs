using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Helmsman;

// Original mesh and matching orthographic icon, embedded so an installed DLL is self-contained.
internal static class GullcallAssets
{
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
        // Decode and validate every mesh before modifying the cloned item.
        using var modelStream=new MemoryStream(Read("model.bin"));
        var model=Helmsman.Core.GullcallModel.Read(modelStream);
        // Capture the cloned BoneFragments material before replacing its renderers.
        // Its actual shader reference works even when Shader.Find cannot find bundled shaders.
        Material? native=null;
        foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            foreach(var material in renderer.sharedMaterials)
                if(material && material.shader){native=material;break;}
        if(!native)throw new InvalidOperationException("BoneFragments material is not loaded yet.");
        prefab.transform.localScale=Vector3.one;
        // Retain ItemDrop/network/physics from the cloned material item; replace only its visuals.
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true)) UnityEngine.Object.DestroyImmediate(renderer);
        foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true)) UnityEngine.Object.DestroyImmediate(filter);
        foreach(var light in prefab.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(light);
        foreach(var collider in prefab.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        var first=model[0].Vertices;
        var bounds=new Bounds(new Vector3(first[0],first[1],first[2]),Vector3.zero);
        var root=new GameObject("Gullcall carved model");root.transform.SetParent(prefab.transform,false);
        foreach(var part in model)
        {
            var vertices=new Vector3[part.Vertices.Length/3];
            for(int i=0;i<vertices.Length;i++)
            {vertices[i]=new Vector3(part.Vertices[i*3],part.Vertices[i*3+1],part.Vertices[i*3+2]);bounds.Encapsulate(vertices[i]);}
            var mesh=new Mesh {name="Gullcall "+part.Name,vertices=vertices,triangles=part.Triangles};
            mesh.RecalculateNormals();mesh.RecalculateBounds();assets.Add(mesh);
            var material=new Material(native!) {name="Gullcall "+part.Name,color=new Color(part.Color[0],part.Color[1],part.Color[2],part.Color[3])};
            if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",Texture2D.whiteTexture);
            foreach(var property in new[]{"_EmissionColor","_NoiseGlowColor"})
                if(material.HasProperty(property))material.SetColor(property,Color.black);
            foreach(var property in new[]{"_Glossiness","_Metallic","_ValueNoise","_ValueNoiseVertex","_AddRain","_AddSnow","_NoiseGlowEnabled"})
                if(material.HasProperty(property))material.SetFloat(property,0);
            if(material.HasProperty("_MoveableObject"))material.SetFloat("_MoveableObject",1);
            material.DisableKeyword("_EMISSION");material.DisableKeyword("NOISEGLOW");
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;assets.Add(material);
            var go=new GameObject(part.Name);go.transform.SetParent(root.transform,false);
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
