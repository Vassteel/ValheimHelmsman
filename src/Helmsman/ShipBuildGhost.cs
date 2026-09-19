using System;
using System.Collections.Generic;
using System.Linq;
using Helmsman.Core;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Helmsman;

internal sealed class ShipBuildGhost : IDisposable
{
    private GameObject? visual;
    private Material? material;
    private string blueprint="";
    private readonly List<Mesh> ownedMeshes=new();
    private readonly List<(MeshRenderer renderer,Material[] materials,float start)> parts=new();
    internal void Update(ConstructionOrder? order,float progress,Vector3? position=null,Quaternion? rotation=null)
    {
        if(order==null){Dispose();return;}
        if(!visual || blueprint!=order.blueprint)
        {
            Dispose();
            var plan=Helmsman.Core.ShipConstruction.Find(order.blueprint);
            var prefab=plan!=null?ShipDirectory.FindPrefab(plan.Prefab):null;
            if(!prefab)return;
            try
            {
                visual=ShipMenuPreviews.CreateVisual(prefab,"Ship under construction");blueprint=order.blueprint;
                var furl=prefab.GetComponent<FleetSailMotion>();
                if(furl)foreach(var filter in visual.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("sail ",StringComparison.Ordinal)))
                {
                    var mesh=Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;
                    for(int i=0;i<vertices.Length;i++)vertices[i]=Vector3.Lerp(FleetSailShape.Anchor(furl.Kind,vertices[i],furl.Top),vertices[i],.1f);
                    mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();filter.sharedMesh=mesh;ownedMeshes.Add(mesh);
                }
                material=Visuals.PreviewMaterial(new Color(.46f,.78f,.85f,.16f));
                // World preview: respect walls/terrain and never cast solid shadows.
                if(material.HasProperty("_ZTest"))material.SetInt("_ZTest",(int)CompareFunction.LessEqual);
                if(material.HasProperty("_Cull"))material.SetInt("_Cull",(int)CullMode.Back);
                material.renderQueue=3000;
                foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var slots=renderer.sharedMaterials;
                    if(Array.Exists(slots,m=>m&&m.shader&&(m.shader.name.Contains("WaterMask")||m.shader.name.Contains("ShadowBlob")))){renderer.enabled=false;continue;}
                    if(renderer.name.StartsWith("hull ",StringComparison.OrdinalIgnoreCase)||(plan!.Source=="VikingShip"&&renderer.bounds.size.y<4&&Mathf.Max(renderer.bounds.size.x,renderer.bounds.size.z)>4))
                    {SplitHull(renderer,slots);renderer.enabled=false;continue;}
                    float height=Mathf.InverseLerp(-1,6,visual.transform.InverseTransformPoint(renderer.bounds.center).y);
                    parts.Add((renderer,(Material[])slots.Clone(),SlipwayMotion.PartStart(renderer.name+" "+string.Join(" ",slots.Where(m=>m).Select(m=>m.name)),height)));
                    for(int i=0;i<slots.Length;i++)slots[i]=material;
                    renderer.sharedMaterials=slots;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                }
                visual.SetActive(true);
            }
            catch{Dispose();throw;}
        }
        visual.transform.position=position??WaterChart.AtSea(order.position);
        visual.transform.rotation=rotation??Quaternion.Euler(0,order.heading,0);
        foreach(var part in parts)
        {
            bool built=progress>=part.start;
            if(built==(part.renderer.shadowCastingMode==ShadowCastingMode.On))continue;
            var slots=(Material[])part.materials.Clone();
            if(!built)for(int i=0;i<slots.Length;i++)slots[i]=material!;
            part.renderer.sharedMaterials=slots;
            part.renderer.shadowCastingMode=built?ShadowCastingMode.On:ShadowCastingMode.Off;
            part.renderer.receiveShadows=built;
        }
        material!.color=new Color(.46f,.78f,.85f,.065f);
    }
    private void SplitHull(MeshRenderer original,Material[] materials)
    {
        var source=original.GetComponent<MeshFilter>().sharedMesh;var vertices=source.vertices;
        var heights=vertices.Select(v=>visual!.transform.InverseTransformPoint(original.transform.TransformPoint(v)).y).ToArray();float low=heights.Min(),high=heights.Max();
        var bins=new List<int>[6,source.subMeshCount];
        for(int i=0;i<6;i++)for(int j=0;j<source.subMeshCount;j++)bins[i,j]=new List<int>();
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            var indices=source.GetTriangles(sub);
            for(int j=0;j<indices.Length;j+=3)
            {
                float height=(heights[indices[j]]+heights[indices[j+1]]+heights[indices[j+2]])/3;
                int band=Mathf.Min(5,(int)(Mathf.InverseLerp(low,high,height)*6));
                bins[band,sub].AddRange(new[]{indices[j],indices[j+1],indices[j+2]});
            }
        }
        var normals=source.normals;var uv=source.uv;var tangents=source.tangents;
        for(int band=0;band<6;band++)
        {
            if(!Enumerable.Range(0,source.subMeshCount).Any(s=>bins[band,s].Count>0))continue;
            var used=Enumerable.Range(0,source.subMeshCount).SelectMany(sub=>bins[band,sub]).Distinct().ToArray();
            var map=used.Select((v,i)=>(v,i)).ToDictionary(pair=>pair.v,pair=>pair.i);
            var mesh=new Mesh{name="Construction hull strakes "+band,indexFormat=source.indexFormat,vertices=used.Select(i=>vertices[i]).ToArray(),subMeshCount=source.subMeshCount};
            if(normals.Length==vertices.Length)mesh.normals=used.Select(i=>normals[i]).ToArray();
            if(uv.Length==vertices.Length)mesh.uv=used.Select(i=>uv[i]).ToArray();
            if(tangents.Length==vertices.Length)mesh.tangents=used.Select(i=>tangents[i]).ToArray();
            for(int sub=0;sub<source.subMeshCount;sub++)mesh.SetTriangles(bins[band,sub].Select(i=>map[i]).ToArray(),sub);
            mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            var node=new GameObject(mesh.name);node.transform.SetParent(original.transform,false);
            node.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=node.AddComponent<MeshRenderer>();
            renderer.sharedMaterials=materials.Select(_=>material!).ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            parts.Add((renderer,materials,.06f+band*.082f));
        }
    }
    public void Dispose()
    {if(visual)Object.Destroy(visual);if(material)Object.Destroy(material);visual=null;material=null;blueprint="";parts.Clear();foreach(var mesh in ownedMeshes)Object.Destroy(mesh);ownedMeshes.Clear();}
}
