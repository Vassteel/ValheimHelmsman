using System.Collections.Generic;
using UnityEngine;

namespace VikingGull;

// Original procedural accessory. Each actor owns its meshes/materials; vanilla assets stay untouched.
public sealed class GullHelmet : MonoBehaviour
{
    private Material template=null!;
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Material> materials = new List<Material>();
    internal static readonly Vector3 Crown = new Vector3(0, 1.216f, -.345f);
    internal static GullHelmet Create(Transform parent, Vector3 position, Material? template = null)
    {
        var root = new GameObject("Viking gull helmet");
        root.transform.SetParent(parent, false); root.transform.localPosition = position;
        var helmet = root.AddComponent<GullHelmet>();
        var nativeRenderer=parent.GetComponent<Renderer>();
        helmet.template=template ? template : nativeRenderer ? nativeRenderer.sharedMaterial : null!;
        var iron = helmet.Material(new Color(.27f, .30f, .32f), .65f);
        var bronze = helmet.Material(new Color(.57f, .35f, .13f), .55f);
        var horn = helmet.Material(new Color(.82f, .75f, .54f), .05f);
        helmet.Surface("Iron cap", iron, 32, 12, (u,v) => {
            float a = u * Mathf.PI * 2, e = v * Mathf.PI * .5f;
            return new Vector3(Mathf.Cos(a) * .079f * Mathf.Cos(e), .094f * Mathf.Sin(e), Mathf.Sin(a) * .103f * Mathf.Cos(e));
        });
        helmet.Surface("Bronze brow band", bronze, 32, 2, (u,v) => {
            float a = u * Mathf.PI * 2;
            return new Vector3(Mathf.Cos(a)*.082f, -.006f + v*.024f, Mathf.Sin(a)*.106f);
        });
        for (int side = -1; side <= 1; side += 2)
        {
            int s = side;
            helmet.Surface("Curved horn", horn, 16, 14, (u,v) => {
                float a = u*Mathf.PI*2, r = .031f*(1-v);
                // Short outward sweep, then upturned tips like the gull in the package artwork.
                return new Vector3(s*(.067f+.143f*v)+r*Mathf.Cos(a)*.5f,
                    .046f+.032f*v+.135f*v*v+r*Mathf.Sin(a), .008f+.042f*v+r*Mathf.Cos(a));
            });
        }
        // Rolled bronze edge, fitted crown seam and small individual rivets.
        helmet.Surface("Rolled helmet rim",bronze,40,8,(u,v)=>{
            float a=u*Mathf.PI*2,b=v*Mathf.PI*2;
            return new Vector3(Mathf.Cos(a)*(.082f+.003f*Mathf.Cos(b)),.006f+.003f*Mathf.Sin(b),Mathf.Sin(a)*(.106f+.003f*Mathf.Cos(b)));
        });
        helmet.Surface("Crown reinforcing strip",bronze,4,24,(u,v)=>{
            float a=v*Mathf.PI;
            return new Vector3((u-.5f)*.011f,.096f*Mathf.Sin(a),.105f*Mathf.Cos(a));
        });
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI*2/10;var center=new Vector3(.085f*Mathf.Cos(a),.011f,.109f*Mathf.Sin(a));
            helmet.Surface("Brow rivet",bronze,8,6,(u,v)=>{
                float t=u*Mathf.PI*2,e=v*Mathf.PI;
                return center+new Vector3(Mathf.Cos(t)*Mathf.Sin(e),Mathf.Cos(e),Mathf.Sin(t)*Mathf.Sin(e))*.0038f;
            });
        }
        return helmet;
    }
    private Material Material(Color color, float metallic)
    {
        var material = Helmsman.GullMaterials.Create(template, color, metallic);
        if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",Texture2D.whiteTexture);
        materials.Add(material); return material;
    }
    private void Surface(string label, Material material, int sides, int rings, System.Func<float,float,Vector3> point)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        // Indexed grids keep curved metal and horn surfaces smooth. Backfaces
        // use separate vertices so their normals never cancel the visible shell.
        int stride=(sides+1)*(rings+1);
        for(int face=0;face<2;face++)for(int j=0;j<=rings;j++)for(int i=0;i<=sides;i++)
            vertices.Add(point((float)i/sides,(float)j/rings));
        for(int face=0;face<2;face++)for(int j=0;j<rings;j++)for(int i=0;i<sides;i++)
        {
            int a=face*stride+j*(sides+1)+i,b=a+1,c=a+sides+1,d=c+1;
            if(face==0)triangles.AddRange(new[]{a,c,b,b,c,d});else triangles.AddRange(new[]{a,b,c,b,d,c});
        }
        var mesh=new Mesh{name=label};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var part=new GameObject(label); part.transform.SetParent(transform,false);
        part.AddComponent<MeshFilter>().sharedMesh=mesh; part.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    internal void FollowStanding(Quaternion head, Quaternion body, Vector3 neck, Vector3 hips, float crouch)
    {
        var p=neck+head*(Crown-neck);
        transform.localPosition=hips+body*(p-hips)-Vector3.up*crouch;
        transform.localRotation=body*head;
    }
    internal static void FitFlying(GameObject model, Transform actor)
    {
        var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
        if(!skin) return;
        // The vanilla flight rig has body/wing bones, but no separate head bone.
        // Measure the posed head in actor metres, then follow its body bone (not the wing rootBone).
        Transform body=skin.transform; bool foundBody=false;
        foreach(var bone in skin.bones) if(bone && bone.name=="Bone") { body=bone; foundBody=true; break; }
        if(!foundBody) return;
        var baked=new Mesh();
        try
        {
            skin.BakeMesh(baked);
            var points=baked.vertices; float front=float.NegativeInfinity, back=float.PositiveInfinity;
            for(int i=0;i<points.Length;i++)
            {
                points[i]=actor.InverseTransformPoint(skin.transform.TransformPoint(points[i]));
                front=Mathf.Max(front,points[i].z); back=Mathf.Min(back,points[i].z);
            }
            var top=new Vector3(0,float.NegativeInfinity,0);
            foreach(var p in points)
                if(Mathf.Abs(p.x)<.09f && p.z>Mathf.Lerp(back,front,.68f) && p.y>top.y) top=p;
            if(float.IsInfinity(top.y)) return;
            var helmet=Create(actor,new Vector3(0,top.y-.02f*model.transform.localScale.y,top.z),skin.sharedMaterial);
            helmet.transform.localScale=model.transform.localScale;
            helmet.transform.localRotation=Quaternion.Euler(0,180,0);
            helmet.transform.SetParent(body,true);
            if(skin.sharedMesh&&skin.sharedMesh.isReadable)
            {
                var detailed=Helmsman.GullSurfaceRefinement.Create(skin.sharedMesh,false);
                skin.sharedMesh=detailed;helmet.meshes.Add(detailed);
            }
        }
        finally { Object.Destroy(baked); }
    }
    private void OnDestroy()
    {
        foreach(var mesh in meshes) if(mesh) Destroy(mesh);
        foreach(var material in materials) if(material) Destroy(material);
    }
}
