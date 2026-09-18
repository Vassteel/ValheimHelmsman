using System.Collections.Generic;
using UnityEngine;

namespace Helmsman;

// Retained imported hulls keep their authored mast and furl animation. Add gentle
// cloth deformation in mesh-local space without touching their source meshes.
public sealed class ImportedSailFlutter : MonoBehaviour
{
    private sealed class Panel
    {
        internal Mesh Mesh=null!;internal Vector3[] Rest=null!,Normals=null!,Live=null!;internal Bounds Bounds;internal Vector3 Direction;
    }
    private readonly List<Panel> panels=new();
    private float nextTick;
    private void Start()
    {
        if(ZNet.instance&&ZNet.instance.IsDedicated())return;
        var ship=GetComponent<Ship>();if(!ship||!ship.m_sailObject)return;
        foreach(var filter in ship.m_sailObject.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!filter.sharedMesh||!filter.sharedMesh.isReadable)continue;
            var mesh=Instantiate(filter.sharedMesh);mesh.MarkDynamic();filter.sharedMesh=mesh;
            panels.Add(new Panel{Mesh=mesh,Rest=mesh.vertices,Normals=mesh.normals,Live=new Vector3[mesh.vertexCount],Bounds=mesh.bounds,Direction=mesh.bounds.size.x<mesh.bounds.size.z?Vector3.right:Vector3.forward});
        }
    }
    private void LateUpdate()
    {
        if(Time.time<nextTick)return;nextTick=Time.time+.05f;
        float wind=EnvMan.instance?EnvMan.instance.GetWindIntensity():0;
        foreach(var p in panels)
        {
            for(int i=0;i<p.Rest.Length;i++)
            {
                var v=p.Rest[i];float t=Mathf.Clamp01((v.y-p.Bounds.min.y)/Mathf.Max(.01f,p.Bounds.size.y));
                float wave=Mathf.Sin(Mathf.PI*t)*(.025f+.055f*wind)*Mathf.Sin(Time.time*2.6f+v.x*1.8f+v.y*2);
                // Opposite faces must move together rather than separating along opposite normals.
                var n=i<p.Normals.Length?p.Normals[i]:Vector3.forward;if(Vector3.Dot(n,p.Direction)<0)n=-n;
                p.Live[i]=v+n*wave;
            }
            p.Mesh.vertices=p.Live;p.Mesh.RecalculateNormals();p.Mesh.RecalculateBounds();
        }
    }
    private void OnDestroy(){foreach(var p in panels)if(p.Mesh)Destroy(p.Mesh);panels.Clear();}
}
