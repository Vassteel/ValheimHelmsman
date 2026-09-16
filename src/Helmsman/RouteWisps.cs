using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Helmsman;

// Cosmetic world-space ribbons. Never feed their height or sway back into navigation.
public sealed class RouteWisps : MonoBehaviour
{
    private const int StrandCount=8, MaximumPoints=768;
    private readonly LineRenderer[] strands=new LineRenderer[StrandCount];
    private Vector3[] centers=new Vector3[0], sides=new Vector3[0], positions=new Vector3[0];
    private float[] distances=new float[0];
    private Material? material;
    private Texture2D? texture;
    private float nextFrame;
    private bool visible;

    internal static RouteWisps Create(Transform parent)
    {
        var root=new GameObject("Helmsman route wisps");root.transform.SetParent(parent,false);
        var effect=root.AddComponent<RouteWisps>();effect.Initialize();return effect;
    }

    private void Initialize()
    {
        var shader=Shader.Find("Sprites/Default");
        if(!shader || !shader.isSupported)
        {Plugin.Instance.Record("Route wisps shader unavailable; route visual disabled.");return;}
        // Seamless soft ribbon: transparent edges and uneven drifting opacity, no hard blue rail.
        texture=new Texture2D(128,32,TextureFormat.RGBA32,false);
        texture.name="Helmsman wisp falloff";texture.wrapMode=TextureWrapMode.Repeat;
        texture.filterMode=FilterMode.Bilinear;
        var pixels=new Color[128*32];
        for(int y=0;y<32;y++)for(int x=0;x<128;x++)
        {
            float u=x/128f*Mathf.PI*2, v=Mathf.Abs((y/31f-.5f)*2);
            float edge=Mathf.Pow(1-v*v,3);
            float fog=.55f+.25f*Mathf.Sin(u)+.14f*Mathf.Sin(u*3+.8f);
            pixels[y*128+x]=new Color(1,1,1,edge*fog);
        }
        texture.SetPixels(pixels);texture.Apply(false,true);
        material=new Material(shader){name="Helmsman blue mist",mainTexture=texture};
        material.mainTextureScale=new Vector2(.09f,1);
        for(int s=0;s<StrandCount;s++)
        {
            var go=new GameObject("Route wisp "+s);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();strands[s]=line;
            line.useWorldSpace=true;line.sharedMaterial=material;line.textureMode=LineTextureMode.Tile;
            line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            line.numCornerVertices=3;line.numCapVertices=3;
            bool mist=s>=6;
            line.startWidth=line.endWidth=mist ? 1.35f : .09f+s*.022f;
            var color=mist ? new Color(.10f,.48f,1,.11f) : new Color(.08f,.57f,1,.48f);
            var fade=new Gradient();
            fade.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(color,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(color.a,.06f),
                    new GradientAlphaKey(color.a,.94f),new GradientAlphaKey(0,1)});
            line.colorGradient=fade;line.enabled=false;
        }
    }

    internal void Clear()
    {
        centers=new Vector3[0];visible=false;
        foreach(var line in strands)if(line){line.enabled=false;line.positionCount=0;}
    }

    internal void SetRoute(IReadOnlyList<Vector3> route)
    {
        Clear();if(route.Count<2 || !material)return;
        var lengths=new float[route.Count];
        for(int i=1;i<route.Count;i++)lengths[i]=lengths[i-1]+Vector3.Distance(route[i-1],route[i]);
        float total=lengths[lengths.Length-1];if(total<.01f)return;
        int count=Mathf.Clamp(Mathf.CeilToInt(total/1.5f)+1,2,MaximumPoints);
        centers=new Vector3[count];sides=new Vector3[count];positions=new Vector3[count];distances=new float[count];
        int segment=1;
        for(int i=0;i<count;i++)
        {
            float distance=total*i/(count-1);distances[i]=distance;
            while(segment<route.Count-1 && lengths[segment]<distance)segment++;
            float length=lengths[segment]-lengths[segment-1];
            centers[i]=Vector3.Lerp(route[segment-1],route[segment],length>.001f ? (distance-lengths[segment-1])/length : 0)+Vector3.up*4;
            var forward=route[segment]-route[segment-1];
            sides[i]=Vector3.Cross(Vector3.up,forward).normalized;
        }
        foreach(var line in strands)if(line)line.positionCount=count;
        nextFrame=0;
    }

    private void LateUpdate()
    {
        bool show=Plugin.Instance && Plugin.Instance.DebugRoute.Value && centers.Length>1 && material;
        if(show!=visible)
        {
            visible=show;foreach(var line in strands)if(line)line.enabled=show;
            nextFrame=0;
        }
        if(!show || Time.time<nextFrame)return;
        nextFrame=Time.time+1f/30;
        float time=Time.time;
        material!.mainTextureOffset=new Vector2(-time*.08f,0);
        for(int s=0;s<StrandCount;s++)
        {
            float fan=(s-2.5f)/5;
            for(int i=0;i<centers.Length;i++)
            {
                float phase=distances[i]*.13f-time*.7f;
                float taper=Mathf.Sin(Mathf.PI*i/(centers.Length-1));
                float side=(Mathf.Sin(phase)*fan*.85f+Mathf.Sin(phase*.53f+s*.45f)*.13f)*taper;
                float lift=(Mathf.Cos(phase*.8f+s*.32f)*.23f+fan*Mathf.Sin(phase)*.30f)*taper;
                positions[i]=centers[i]+sides[i]*side+Vector3.up*lift;
            }
            strands[s].SetPositions(positions);
        }
    }

    private void OnDestroy()
    {
        if(material)Destroy(material);
        if(texture)Destroy(texture);
    }
}
