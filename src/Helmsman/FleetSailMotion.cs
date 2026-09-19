using System;
using System.Collections.Generic;
using UnityEngine;

namespace Helmsman;

// Furl toward each sail's actual spar/luff, not the ship's world-up axis.
// Every instance owns its deforming meshes; the prefab and neighbouring ships are immutable.
public sealed class FleetSailMotion : MonoBehaviour
{
    public string Kind="";
    public Transform Sail=null!;
    public float Top;
    public Mesh[] AuthoredMeshes=Array.Empty<Mesh>();
    private ImportedSailAnimation? animationState;
    private sealed class Cloth {internal Mesh Mesh=null!;internal Vector3[] Rest=null!,Live=null!;}
    private readonly List<Cloth> cloth=new();
    private float nextTick;
    private float halfWidth=1;
    private float bottom;
    private bool frozenPreview;
    internal void FreezePreview()
    {
        if(frozenPreview)return;frozenPreview=true;
        enabled=false;
        var rig=GetComponent<FinalShipPresentation>();if(rig)rig.enabled=false;
        int index=0;
        foreach(var filter in Sail.GetComponentsInChildren<MeshFilter>(true))
        {
            var source=index<AuthoredMeshes.Length?AuthoredMeshes[index++]:filter.sharedMesh;
            if(!source)continue;
            var mesh=Instantiate(source);var vertices=source.vertices;
            for(int i=0;i<vertices.Length;i++)vertices[i]=Vector3.Lerp(FleetSailShape.Anchor(Kind,vertices[i],Top),vertices[i],.1f);
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();filter.sharedMesh=mesh;
            cloth.Add(new Cloth{Mesh=mesh});
        }
    }
    private void Start()
    {
        if(ZNet.instance&&ZNet.instance.IsDedicated())return;
        animationState=GetComponent<ImportedSailAnimation>();
        bottom=Top;
        int index=0;
        foreach(var filter in Sail.GetComponentsInChildren<MeshFilter>(true))
        {
            // Icon previews and other instances can already have deformed meshes.
            // Always start from the immutable fully deployed resource assigned at registration.
            var source=index<AuthoredMeshes.Length?AuthoredMeshes[index++]:filter.sharedMesh;
            if(!source)continue;
            var mesh=Instantiate(source);mesh.MarkDynamic();filter.sharedMesh=mesh;
            var rest=mesh.vertices;
            foreach(var v in rest){halfWidth=Mathf.Max(halfWidth,Mathf.Abs(v.x));bottom=Mathf.Min(bottom,v.y);}
            cloth.Add(new Cloth{Mesh=mesh,Rest=rest,Live=new Vector3[rest.Length]});
        }
    }
    public Vector3 Furl(Vector3 p)
    {
        float amount=animationState?animationState.FurlAmount:1;
        return Vector3.Lerp(FleetSailShape.Anchor(Kind,p,Top),p,amount);
    }
    private void LateUpdate()
    {
        if(Time.time<nextTick)return;nextTick=Time.time+.05f;
        float time=Time.time,amount=animationState?animationState.FurlAmount:1;
        float wind=EnvMan.instance?EnvMan.instance.GetWindIntensity():0;
        foreach(var part in cloth)
        {
            for(int i=0;i<part.Rest.Length;i++)
            {
                var p=part.Rest[i];var result=Furl(p);
                float wave=FleetSailShape.Flutter(Kind,p,Top,bottom,halfWidth,time,wind,amount);
                if(Kind=="falkusa"||Kind=="currach")result.x+=wave;
                else result.z+=wave;
                part.Live[i]=result;
            }
            part.Mesh.vertices=part.Live;part.Mesh.RecalculateNormals();part.Mesh.RecalculateBounds();
        }
    }
    private void OnDestroy(){foreach(var part in cloth)if(part.Mesh)Destroy(part.Mesh);cloth.Clear();}
}
