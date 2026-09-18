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
    private ImportedSailAnimation? animationState;
    private sealed class Cloth {internal Mesh Mesh=null!;internal Vector3[] Rest=null!,Live=null!;}
    private readonly List<Cloth> cloth=new();
    private float nextTick;
    private float halfWidth=1;
    private float bottom;
    private void Start()
    {
        if(ZNet.instance&&ZNet.instance.IsDedicated())return;
        animationState=GetComponent<ImportedSailAnimation>();
        bottom=Top;
        foreach(var filter in Sail.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh)continue;
            var mesh=Instantiate(filter.sharedMesh);mesh.MarkDynamic();filter.sharedMesh=mesh;
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
                float wave=(Mathf.Sin(time*2.3f+p.y*1.5f+p.x)*.07f+Mathf.Sin(time*4.1f-p.y*2+p.z)*.025f)*(.25f+wind)*amount*FleetSailShape.Weight(Kind,p,Top,bottom,halfWidth);
                if(Kind=="falkusa"||Kind=="currach")result.x+=wave;
                else result.z+=wave;
                part.Live[i]=result;
            }
            part.Mesh.vertices=part.Live;part.Mesh.RecalculateNormals();part.Mesh.RecalculateBounds();
        }
    }
    private void OnDestroy(){foreach(var part in cloth)if(part.Mesh)Destroy(part.Mesh);cloth.Clear();}
}
