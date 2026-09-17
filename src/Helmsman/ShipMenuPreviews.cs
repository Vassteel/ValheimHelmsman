using System;
using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Helmsman;

internal static class ShipMenuPreviews
{
    private static readonly Dictionary<string,Sprite?> cache=new(StringComparer.Ordinal);
    private static readonly List<Sprite> owned=new();

    internal static Sprite? Get(string prefabName)
    {
        if(cache.TryGetValue(prefabName,out var cached))return cached;
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return null;
        var prefab=ShipDirectory.FindPrefab(prefabName);
        if(!prefab)return null;
        Sprite? result=null;
        GameObject? visual=null;
        try
        {
            // Copy only visible meshes. No ship, inventory or network components
            // are instantiated when browsing the carousel.
            visual=CreateVisual(prefab,"Helmsman menu preview "+prefabName);
            var rendered=RenderManager.Instance.Render(new RenderManager.RenderRequest(visual) {
                Width=768,Height=768,Rotation=Quaternion.Euler(15,120,0),
                // Jotunn's disk cache is keyed by prefab, not resolution. Keep
                // these large carousel pictures separate from the 128px icons.
                UseCache=false,ParticleSimulationTime=-1
            });
            if(rendered)
            {
                result=rendered;owned.Add(rendered);
                // Remove transparent margins while preserving the ship's proportions.
                var texture=rendered.texture;var pixels=texture.GetPixels32();
                int left=texture.width,bottom=texture.height,right=-1,top=-1;
                for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
                    if(pixels[y*texture.width+x].a>8)
                    {left=Math.Min(left,x);bottom=Math.Min(bottom,y);right=Math.Max(right,x);top=Math.Max(top,y);}
                if(right>=left && top>=bottom)
                {
                    left=Math.Max(0,left-8);bottom=Math.Max(0,bottom-8);
                    right=Math.Min(texture.width-1,right+8);top=Math.Min(texture.height-1,top+8);
                    result=Sprite.Create(texture,new Rect(left,bottom,right-left+1,top-bottom+1),new Vector2(.5f,.5f));
                    owned[owned.Count-1]=result;Object.Destroy(rendered);
                }
            }
        }
        catch(Exception error){Plugin.Instance.Record("Ship menu picture unavailable for "+prefabName+": "+error.Message);}
        finally{if(visual)Object.Destroy(visual);}
        if(!result)result=prefab.GetComponent<Piece>()?.m_icon;
        cache[prefabName]=result;return result;
    }

    internal static GameObject CreateVisual(GameObject prefab,string name)
    {
        var visual=new GameObject(name);visual.SetActive(false);
        try
        {
            visual.transform.localScale=prefab.transform.localScale;
            var lowerLods=new HashSet<Renderer>();var firstLods=new HashSet<Renderer>();
            foreach(var group in prefab.GetComponentsInChildren<LODGroup>(true))
            {
                var levels=group.GetLODs();
                for(int i=0;i<levels.Length;i++)foreach(var renderer in levels[i].renderers)
                    if(renderer)(i==0?firstLods:lowerLods).Add(renderer);
            }
            lowerLods.ExceptWith(firstLods);CopyMeshes(prefab.transform,visual.transform,lowerLods);
            return visual;
        }
        catch{Object.Destroy(visual);throw;}
    }
    private static void CopyMeshes(Transform source,Transform target,HashSet<Renderer> lowerLods)
    {
        var renderer=source.GetComponent<MeshRenderer>();var filter=source.GetComponent<MeshFilter>();
        if(renderer && renderer.enabled && !renderer.forceRenderingOff && !lowerLods.Contains(renderer) && filter && filter.sharedMesh)
        {
            target.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
            target.gameObject.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
        }
        for(int i=0;i<source.childCount;i++)
        {
            var child=source.GetChild(i);if(!child.gameObject.activeSelf)continue;
            var copy=new GameObject(child.name).transform;copy.SetParent(target,false);
            copy.localPosition=child.localPosition;copy.localRotation=child.localRotation;copy.localScale=child.localScale;
            CopyMeshes(child,copy,lowerLods);
        }
    }

    internal static void Release()
    {
        foreach(var sprite in owned)if(sprite){if(sprite.texture)Object.Destroy(sprite.texture);Object.Destroy(sprite);}
        owned.Clear();cache.Clear();
    }
}
