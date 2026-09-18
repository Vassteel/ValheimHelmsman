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
    private Cloth[] nativeCloth=System.Array.Empty<Cloth>();
    private Transform? sail;
    private Vector3 previousScale;
    private void Start()
    {
        if(ZNet.instance&&ZNet.instance.IsDedicated())return;
        var ship=GetComponent<Ship>();if(!ship||!ship.m_sailObject)return;
        sail=ship.m_sailObject.transform;previousScale=sail.localScale;
        nativeCloth=sail.GetComponentsInChildren<Cloth>(true);
        foreach(var c in nativeCloth)
        {
            // Retain the working legacy cloth simulation, but tether it to its rig.
            // The source used acceleration 100, no tethers and up to 2m travel.
            var driver=c.GetComponent<GlobalWind>();if(driver){driver.CancelInvoke();driver.enabled=false;}
            c.useTethers=true;c.useGravity=false;c.damping=.35f;
            var coefficients=c.coefficients;
            float travel=.35f/Mathf.Max(.1f,Mathf.Max(sail.lossyScale.x,sail.lossyScale.z));
            for(int i=0;i<coefficients.Length;i++)coefficients[i].maxDistance=Mathf.Min(coefficients[i].maxDistance,travel);
            c.coefficients=coefficients;
        }
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
        if(sail)
        {
            bool resizing=(sail.localScale-previousScale).sqrMagnitude>.000001f;
            foreach(var c in nativeCloth)
            {
                if(!c)continue;
                bool resume=!c.enabled&&!resizing;c.enabled=!resizing;
                if(resume)c.ClearTransformMotion();
                var direction=EnvMan.instance?EnvMan.instance.GetWindDir():transform.forward;
                c.externalAcceleration=direction*(4+8*wind);
                c.randomAcceleration=Vector3.one*(1+2*wind);
            }
            previousScale=sail.localScale;
        }
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
