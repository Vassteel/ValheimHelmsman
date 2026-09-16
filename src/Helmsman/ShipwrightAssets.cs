using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;

namespace Helmsman;

internal static class ShipwrightAssets
{
    private static readonly Dictionary<string,Texture2D> textures=new();
    internal static string StyleFolder(string kind)=>Path.Combine(Paths.ConfigPath,"Helmsman","ShipStyles",kind);
    internal static string[] Styles(string kind)
    {
        var dir=StyleFolder(kind);Directory.CreateDirectory(dir);
        return new[]{"Sea flax","Vanilla"}.Concat(Directory.GetFiles(dir,"*.png")
            .Select(Path.GetFileNameWithoutExtension).Where(n=>n!="Sea flax" && n!="Vanilla").OrderBy(n=>n,StringComparer.Ordinal)).ToArray()!;
    }
    internal static Texture2D? Texture(string name,string? kind=null)
    {
        if(name=="Vanilla")return null;
        string key=(kind??"embedded")+"/"+name;
        if(textures.TryGetValue(key,out var existing) && existing)return existing;
        byte[] bytes;
        if(kind!=null && name!="Sea flax")
        {
            if(name!=Path.GetFileName(name))return null;
            var file=Path.Combine(StyleFolder(kind),name+".png");
            if(!File.Exists(file) || new FileInfo(file).Length>8*1024*1024)return null;
            bytes=File.ReadAllBytes(file);
        }
        else
        {
            var file=name=="Sea flax" ? (kind=="sails"?"sail-sea-flax":"canopy-sea-flax") : name;
            using var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Helmsman.Shipwright."+file+".png");
            if(stream==null)throw new InvalidOperationException("Missing ship texture: "+file);
            using var data=new MemoryStream();stream.CopyTo(data);bytes=data.ToArray();
        }
        var loaded=new Texture2D(2,2,TextureFormat.RGBA32,true){name="Helmsman "+key,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
        if(!loaded.LoadImage(bytes) || loaded.width>4096 || loaded.height>4096){UnityEngine.Object.Destroy(loaded);return null;}
        // Match the native ships' texel density without requiring GPU work on a dedicated server.
        if(kind==null || name=="Sea flax")
        {
            int size=kind=="sails"?128:256;
            int width=Math.Max(1,size*loaded.width/Math.Max(loaded.width,loaded.height)),height=Math.Max(1,size*loaded.height/Math.Max(loaded.width,loaded.height));
            var pixels=loaded.GetPixels32();var small=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                small[y*width+x]=pixels[Math.Min(loaded.height-1,y*loaded.height/height)*loaded.width+Math.Min(loaded.width-1,x*loaded.width/width)];
            var compact=new Texture2D(width,height,TextureFormat.RGBA32,true){name=loaded.name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            compact.SetPixels32(small);compact.Apply();UnityEngine.Object.Destroy(loaded);loaded=compact;
        }
        textures[key]=loaded;return loaded;
    }
    internal static void Release(){foreach(var t in textures.Values)if(t)UnityEngine.Object.Destroy(t);textures.Clear();}
}

internal sealed class ShipwrightSkin : IDisposable
{
    private readonly List<(Renderer Renderer,Material[] Original,Material[] Owned)> bindings=new();
    internal void Paint(GameObject root,Texture2D texture)
    {
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if(!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))continue;
            var source=renderer.sharedMaterials;var owned=new Material[source.Length];
            for(int i=0;i<source.Length;i++)
            {
                if(!source[i])continue;
                owned[i]=new Material(source[i]){name=source[i].name+" [Helmsman shipwright]"};
                if(owned[i].HasProperty("_MainTex"))owned[i].SetTexture("_MainTex",texture);
            }
            bindings.Add((renderer,source,owned));renderer.sharedMaterials=owned;
        }
    }
    public void Dispose()
    {
        foreach(var b in bindings)
        {
            if(b.Renderer)
            {
                var current=b.Renderer.sharedMaterials;
                for(int i=0;i<current.Length && i<b.Owned.Length;i++)if(current[i]==b.Owned[i])current[i]=b.Original[i];
                b.Renderer.sharedMaterials=current;
            }
            foreach(var material in b.Owned)if(material)UnityEngine.Object.Destroy(material);
        }
        bindings.Clear();
    }
}
