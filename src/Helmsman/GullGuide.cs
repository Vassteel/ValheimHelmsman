using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Helmsman;

internal static class Visuals
{
    private static bool overlayWarning;
    internal static Material PreviewMaterial(Color color)
    {
        // Unity's built-in colored shader exposes the depth-test state needed for an editing overlay.
        var shader=Shader.Find("Hidden/Internal-Colored");
        var material=new Material(shader ? shader : Shader.Find("Sprites/Default"));
        if(material.HasProperty("_Color"))material.SetColor("_Color",color);
        if(material.HasProperty("_ZTest"))
        {
            material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite",0);
            material.SetInt("_ZTest",(int)CompareFunction.Always);
            material.SetInt("_Cull",(int)CullMode.Off);
            material.renderQueue=4000;
        }
        else if(!overlayWarning)
        {
            overlayWarning=true;
            Plugin.Instance.Record("Preview overlay shader unavailable; using ordinary transparency. Adjust view remains available.");
        }
        return material;
    }
    internal static GameObject? Clone(GameObject source, Transform parent, bool ghost)
    {
        if (!source) return null;
        var staging=new GameObject("Helmsman visual staging"); staging.SetActive(false);
        var clone=Object.Instantiate(source,staging.transform,false);
        foreach(var b in clone.GetComponentsInChildren<MonoBehaviour>(true).OrderBy(b=>b is ZNetView ? 1 : 0)) Object.DestroyImmediate(b);
        foreach(var c in clone.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach(var joint in clone.GetComponentsInChildren<Joint>(true)) Object.DestroyImmediate(joint);
        foreach(var rb in clone.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
        foreach(var sound in clone.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(sound);
        foreach(var lod in clone.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(lod);
        foreach(var particles in clone.GetComponentsInChildren<ParticleSystem>(true)) particles.gameObject.SetActive(false);
        foreach(var light in clone.GetComponentsInChildren<Light>(true)) light.enabled=false;
        if(ghost)
        {
            var lifetime=clone.AddComponent<VisualLifetime>();
            foreach(var animator in clone.GetComponentsInChildren<Animator>(true)) animator.enabled=false;
            foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.allowOcclusionWhenDynamic=false;
                    var old=renderer.sharedMaterials; var mats=new Material[old.Length];
                    for(int i=0;i<mats.Length;i++)
                    {
                        mats[i]=PreviewMaterial(new Color(.3f,.8f,1,.18f));
                        lifetime.Materials.Add(mats[i]);
                    }
                    renderer.sharedMaterials=mats;
                }
        }
        clone.transform.SetParent(parent,false);
        clone.SetActive(true);
        Object.Destroy(staging);
        return clone;
    }

    internal static LineRenderer Line(Transform parent, Color color, float width=.15f, bool overlay=false)
    {
        var go=new GameObject("Helmsman preview line"); go.transform.SetParent(parent,false);
        var line=go.AddComponent<LineRenderer>(); line.useWorldSpace=true;
        if(overlay)line.allowOcclusionWhenDynamic=false;
        line.sharedMaterial=overlay ? PreviewMaterial(Color.white) : new Material(Shader.Find("Sprites/Default"));
        line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;
        return line;
    }

    internal static void DestroyMaterials(GameObject root)
    {
        foreach(var line in root.GetComponentsInChildren<LineRenderer>(true)) Object.Destroy(line.sharedMaterial);
    }
}

public sealed class VisualLifetime : MonoBehaviour
{
    internal readonly List<Material> Materials=new List<Material>();
    private void OnDestroy(){foreach(var material in Materials) if(material) Destroy(material);}
}
