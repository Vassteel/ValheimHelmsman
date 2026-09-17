using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Helmsman;

// Refine only private visual copies. UVs, skinning and submeshes survive, so the
// native flight animation and world materials remain authoritative.
internal static class GullSurfaceRefinement
{
    internal static Mesh Create(Mesh source,bool perched)
    {
        var result=UnityEngine.Object.Instantiate(source);
        result.name="Detailed private gull surface";
        if(!source.isReadable||source.blendShapeCount!=0)return result;
        try
        {
            if(result.normals.Length!=result.vertexCount)result.RecalculateNormals();
            if(perched)RoundPlumage(result);
            for(int pass=0;pass<2;pass++)Refine(result);
            if(perched&&result.subMeshCount==1)Plumage(result);
            if(result.uv.Length==result.vertexCount)result.RecalculateTangents();
            result.RecalculateBounds();return result;
        }
        catch {UnityEngine.Object.Destroy(result);throw;}
    }
    private static void RoundPlumage(Mesh mesh)
    {
        var v=mesh.vertices;var originalNormals=mesh.normals;var source=(Vector3[])v.Clone();var ids=new Dictionary<(int,int,int),List<int>>();
        var neighbors=new HashSet<int>[v.Length];for(int i=0;i<v.Length;i++)neighbors[i]=new HashSet<int>();
        for(int i=0;i<v.Length;i++)
        {var p=v[i];var key=((int)Math.Round(p.x*100000),(int)Math.Round(p.y*100000),(int)Math.Round(p.z*100000));if(!ids.TryGetValue(key,out var group)){group=new List<int>();ids.Add(key,group);}group.Add(i);}
        var triangles=mesh.triangles;
        for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++)
        {int a=triangles[i+j];neighbors[a].Add(triangles[i+(j+1)%3]);neighbors[a].Add(triangles[i+(j+2)%3]);}
        foreach(var group in ids.Values)
        {
            var p=source[group[0]];
            // Feet, eyes, bill and helmet fitting points retain their native shape.
            if(p.y<.38f||p.y>1.09f||p.z<-.38f)continue;
            var adjacent=new HashSet<int>();foreach(int i in group)adjacent.UnionWith(neighbors[i]);
            if(adjacent.Count==0)continue;var average=Vector3.zero;foreach(int i in adjacent)average+=source[i];
            average*=1f/adjacent.Count;foreach(int i in group)v[i]=Vector3.Lerp(p,average,.18f);
        }
        mesh.vertices=v;mesh.RecalculateNormals();var normals=mesh.normals;
        foreach(var group in ids.Values)
        {
            var p=v[group[0]];if(p.y<.38f||p.y>1.09f||p.z<-.38f){foreach(int i in group)normals[i]=originalNormals[i];continue;}
            var sum=Vector3.zero;foreach(int i in group)sum+=normals[i];
            if(sum.sqrMagnitude>.001f)foreach(int i in group)normals[i]=sum.normalized;
        }
        mesh.normals=normals;
    }
    private static void Refine(Mesh mesh)
    {
        var vertices=new List<Vector3>(mesh.vertices);var normals=new List<Vector3>(mesh.normals);
        var uv=new List<Vector2>(mesh.uv);var colors=new List<Color>(mesh.colors);
        var weights=new List<BoneWeight>(mesh.boneWeights);
        bool hasUv=uv.Count==vertices.Count,hasColors=colors.Count==vertices.Count,skinned=weights.Count==vertices.Count;
        var edges=new Dictionary<(int,int),int>();var submeshes=new List<int[]>();
        int Midpoint(int a,int b)
        {
            var key=a<b?(a,b):(b,a);if(edges.TryGetValue(key,out int cached))return cached;
            var edge=vertices[b]-vertices[a];var normal=(normals[a]+normals[b]).normalized;
            // Cubic Hermite midpoint rounds broad plumage surfaces while retaining
            // sharp beak/foot transitions and every original silhouette vertex.
            var correction=(normals[b]*Vector3.Dot(normals[b],edge)-normals[a]*Vector3.Dot(normals[a],edge))*.125f;
            if(Vector3.Dot(normals[a],normals[b])<.35f)correction=Vector3.zero;
            int at=vertices.Count;vertices.Add((vertices[a]+vertices[b])*.5f+correction);normals.Add(normal.sqrMagnitude>.1f?normal:normals[a]);
            if(hasUv)uv.Add((uv[a]+uv[b])*.5f);
            if(hasColors)colors.Add(Color.Lerp(colors[a],colors[b],.5f));
            if(skinned)weights.Add(Blend(weights[a],weights[b]));
            edges.Add(key,at);return at;
        }
        for(int sub=0;sub<mesh.subMeshCount;sub++)
        {
            var original=mesh.GetTriangles(sub);var triangles=new List<int>(original.Length*4);
            for(int i=0;i<original.Length;i+=3)
            {
                int a=original[i],b=original[i+1],c=original[i+2],ab=Midpoint(a,b),bc=Midpoint(b,c),ca=Midpoint(c,a);
                triangles.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});
            }
            submeshes.Add(triangles.ToArray());
        }
        if(vertices.Count>60000)throw new InvalidOperationException("Gull refinement exceeds mesh budget.");
        mesh.vertices=vertices.ToArray();mesh.normals=normals.ToArray();
        if(hasUv)mesh.uv=uv.ToArray();if(hasColors)mesh.colors=colors.ToArray();if(skinned)mesh.boneWeights=weights.ToArray();
        for(int sub=0;sub<submeshes.Count;sub++)mesh.SetTriangles(submeshes[sub],sub);
    }
    private static BoneWeight Blend(BoneWeight a,BoneWeight b)
    {
        var combined=new Dictionary<int,float>();
        void Add(int bone,float weight){if(weight>0)combined[bone]=combined.TryGetValue(bone,out float old)?old+weight*.5f:weight*.5f;}
        Add(a.boneIndex0,a.weight0);Add(a.boneIndex1,a.weight1);Add(a.boneIndex2,a.weight2);Add(a.boneIndex3,a.weight3);
        Add(b.boneIndex0,b.weight0);Add(b.boneIndex1,b.weight1);Add(b.boneIndex2,b.weight2);Add(b.boneIndex3,b.weight3);
        var values=combined.OrderByDescending(p=>p.Value).Take(4).ToArray();float sum=values.Sum(p=>p.Value);
        int Index(int i)=>i<values.Length?values[i].Key:0;float Weight(int i)=>i<values.Length?values[i].Value/sum:0;
        return new BoneWeight{boneIndex0=Index(0),boneIndex1=Index(1),boneIndex2=Index(2),boneIndex3=Index(3),weight0=Weight(0),weight1=Weight(1),weight2=Weight(2),weight3=Weight(3)};
    }
    private static void Plumage(Mesh mesh)
    {
        var source=mesh.vertices;var sourceUv=mesh.uv;var vertices=new List<Vector3>(source);var normals=new List<Vector3>(mesh.normals);
        var uv=new List<Vector2>(sourceUv);var indices=new List<int>(mesh.triangles);var colors=new List<Color>(mesh.colors);bool colored=colors.Count==source.Length;
        if(sourceUv.Length!=source.Length)return;
        // Intersect the actual refined wing surface in its Y/Z plane. Feather
        // roots follow that surface, avoiding detached flat sheets beside the bird.
        var sourceTriangles=mesh.triangles;
        float Surface(float y,float z,int side)
        {
            float outer=0;
            for(int i=0;i<sourceTriangles.Length;i+=3)
            {
                var a=source[sourceTriangles[i]];var b=source[sourceTriangles[i+1]];var c=source[sourceTriangles[i+2]];
                float den=(b.z-c.z)*(a.y-c.y)+(c.y-b.y)*(a.z-c.z);if(Math.Abs(den)<1e-9f)continue;
                float u=((b.z-c.z)*(y-c.y)+(c.y-b.y)*(z-c.z))/den;
                float w=((c.z-a.z)*(y-c.y)+(a.y-c.y)*(z-c.z))/den;float t=1-u-w;
                if(u<0||w<0||t<0)continue;outer=Math.Max(outer,side*(u*a.x+w*b.x+t*c.x));
            }
            return side*(outer+.002f);
        }
        for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++)for(int feather=0;feather<5;feather++)
        {
            float z=-.025f+feather*.055f+row*.04f,y=.695f-row*.085f-feather*.024f;
            var root=new Vector3(0,y,z);var axis=new Vector3(0,-.145f,.12f);var across=new Vector3(0,.013f,.016f);var n=new Vector3(side,0,0);
            var points=new[]{root,root+axis*.3f+across,root+axis*.7f+across*.7f,root+axis,root+axis*.7f-across*.7f,root+axis*.3f-across,root+axis*.45f,root+axis*.45f};
            for(int i=0;i<points.Length;i++){var p=points[i];p.x=Surface(p.y,p.z,side)+(i==6?side*.006f:i==7?-side*.001f:0);points[i]=p;}
            int start=vertices.Count;var featherIndices=new List<int>();
            for(int i=0;i<6;i++)
            {
                int a=i,b=(i+1)%6,c=6,d=7;
                if(Vector3.Dot(Vector3.Cross(points[b]-points[a],points[c]-points[a]),n)<0){int swap=a;a=b;b=swap;}
                featherIndices.AddRange(new[]{a,b,c,b,a,d});
            }
            var featherNormals=new Vector3[points.Length];
            for(int i=0;i<featherIndices.Count;i+=3)
            {int a=featherIndices[i],b=featherIndices[i+1],c=featherIndices[i+2];var fn=Vector3.Cross(points[b]-points[a],points[c]-points[a]);featherNormals[a]+=fn;featherNormals[b]+=fn;featherNormals[c]+=fn;}
            for(int index=0;index<points.Length;index++)
            {
                var p=points[index];int nearest=0;float distance=float.MaxValue;
                for(int i=0;i<source.Length;i++){float d=(source[i]-p).sqrMagnitude;if(d<distance){distance=d;nearest=i;}}
                vertices.Add(p);normals.Add(featherNormals[index].sqrMagnitude>1e-14f?featherNormals[index].normalized:n);uv.Add(sourceUv[nearest]);if(colored)colors.Add(colors[nearest]);
            }
            indices.AddRange(featherIndices.Select(i=>i+start));
        }
        mesh.vertices=vertices.ToArray();mesh.normals=normals.ToArray();mesh.uv=uv.ToArray();if(colored)mesh.colors=colors.ToArray();mesh.triangles=indices.ToArray();
    }
}
