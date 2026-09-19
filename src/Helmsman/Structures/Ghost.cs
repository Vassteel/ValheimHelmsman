#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Helmsman.Structures
{
    internal sealed class Ghost:IDisposable
    {
        private sealed class Batch
        {
            internal Mesh Mesh;internal int Submesh;
            internal readonly List<Matrix4x4> Local=new List<Matrix4x4>();
            internal Matrix4x4[] World;
        }
        private readonly List<Batch> batches=new List<Batch>();
        private readonly Material material;
        private readonly Material terrainMaterial;
        private readonly bool instanced;
        private Mesh terrain;
        private float nextTerrain;
        internal Ghost(Blueprint blueprint,Dictionary<string,GameObject> prefabs)
        {
            var shader=Shader.Find("Standard")??Shader.Find("Legacy Shaders/Diffuse");
            var lineShader=Shader.Find("Hidden/Internal-Colored")??Shader.Find("Sprites/Default");
            if(!shader||!lineShader)throw new InvalidOperationException("No supported shaded preview shader found.");
            instanced=SystemInfo.supportsInstancing&&shader.name=="Standard";
            // Opaque, matte clay material: depth occludes the back walls instead of layering cyan surfaces.
            material=new Material(shader){name="POI shaded greybox",color=new Color(.65f,.65f,.65f,1f),renderQueue=2000,enableInstancing=instanced};
            material.mainTexture=Texture2D.whiteTexture;
            if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",0f);
            if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0f);
            if(material.HasProperty("_Mode"))material.SetFloat("_Mode",0f);
            material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.Zero);material.SetInt("_ZWrite",1);
            material.SetOverrideTag("RenderType","Opaque");
            material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_ALPHABLEND_ON");material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            // A subtle ambient floor keeps the model readable at dusk without removing directional shading.
            if(material.HasProperty("_EmissionColor")){material.SetColor("_EmissionColor",new Color(.055f,.055f,.055f));material.EnableKeyword("_EMISSION");}
            terrainMaterial=new Material(lineShader){name="POI terrain grid",color=new Color(.2f,.85f,.95f,.75f),renderQueue=3000};
            terrainMaterial.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);terrainMaterial.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            terrainMaterial.SetInt("_ZWrite",0);terrainMaterial.SetInt("_Cull",(int)CullMode.Off);
            var current=new Dictionary<(Mesh,int),Batch>();int total=0;
            try
            {
                foreach(var piece in blueprint.Pieces)
                {
                    var prefab=prefabs[piece.Prefab];
                    var matrix=Matrix4x4.TRS(new Vector3(piece.X,piece.Y,piece.Z),new Quaternion(piece.Qx,piece.Qy,piece.Qz,piece.Qw),Scale(piece,prefab));
                    foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var renderer=filter.GetComponent<MeshRenderer>();
                        if(!renderer||!renderer.enabled||!filter.sharedMesh||!Visible(filter.transform,prefab.transform))continue;
                        var name=filter.name.ToLowerInvariant();
                        if(name.Contains("shadow")||name.Contains("lod1")||name.Contains("lod2")||name.Contains("lod3"))continue;
                        var mesh=filter.sharedMesh;
                        for(int sub=0;sub<mesh.subMeshCount;sub++)
                        {
                            var key=(mesh,sub);
                            if(!current.TryGetValue(key,out var batch)||batch.Local.Count==1023)
                            {batch=new Batch{Mesh=mesh,Submesh=sub};current[key]=batch;batches.Add(batch);}
                            batch.Local.Add(matrix*prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix);
                            total+=mesh.vertexCount;
                            if(total>8_000_000)throw new InvalidOperationException("Build exceeds the 8-million-vertex preview budget.");
                        }
                    }
                }
                if(batches.Count==0)throw new InvalidOperationException("This structure contains no supported preview meshes.");
                foreach(var b in batches)b.World=new Matrix4x4[b.Local.Count];
            }
            catch{Dispose();throw;}
        }
        private static Vector3 Scale(BuildPiece piece,GameObject prefab)=>piece.HasScale?new Vector3(piece.ScaleX,piece.ScaleY,piece.ScaleZ):prefab.transform.localScale;
        internal static void FitFoundation(Blueprint blueprint,Dictionary<string,GameObject> prefabs)
        {
            var points=new List<Point>();
            foreach(var piece in blueprint.Pieces)
            {
                if(piece.Y>2f)continue;
                var prefab=prefabs[piece.Prefab];
                var root=Matrix4x4.TRS(new Vector3(piece.X,piece.Y,piece.Z),new Quaternion(piece.Qx,piece.Qy,piece.Qz,piece.Qw),Scale(piece,prefab));
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!filter.sharedMesh||!Visible(filter.transform,prefab.transform))continue;
                    var bounds=filter.sharedMesh.bounds;var matrix=root*prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                    for(int i=0;i<8;i++)
                    {
                        var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                        var p=matrix.MultiplyPoint3x4(corner);
                        // Small edge clearance for foundation thickness and slightly projecting planks.
                        points.Add(new Point(p.x-.5f,p.z-.5f));points.Add(new Point(p.x+.5f,p.z+.5f));
                        points.Add(new Point(p.x-.5f,p.z+.5f));points.Add(new Point(p.x+.5f,p.z-.5f));
                        blueprint.Radius=Mathf.Max(blueprint.Radius,new Vector2(p.x,p.z).magnitude+1);
                    }
                }
            }
            if(blueprint.Radius>120)throw new InvalidOperationException("The actual model footprint is too large for one loaded placement.");
            if(points.Count>=3)blueprint.Hull=Geometry.Hull(points);
        }
        private static bool Visible(Transform t,Transform root)
        {
            while(t!=root){if(!t.gameObject.activeSelf)return false;t=t.parent;}return true;
        }
        internal void Draw(Vector3 position,Quaternion rotation,bool valid)
        {
            material.color=valid?new Color(.65f,.65f,.65f,1f):new Color(.62f,.38f,.36f,1f);
            var matrix=Matrix4x4.TRS(position,rotation,Vector3.one);
            foreach(var batch in batches)
            {
                for(int i=0;i<batch.Local.Count;i++)batch.World[i]=matrix*batch.Local[i];
                // Draw directly from GPU meshes: no Read/Write import flag and no live prefab clones needed.
                if(instanced)Graphics.DrawMeshInstanced(batch.Mesh,batch.Submesh,material,batch.World,batch.World.Length,null,ShadowCastingMode.Off,false);
                else foreach(var world in batch.World)Graphics.DrawMesh(batch.Mesh,world,material,0,null,batch.Submesh,null,ShadowCastingMode.Off,false);
            }
        }
        internal void DrawTerrain(Blueprint blueprint,Vector3 origin,Quaternion rotation,float width,float terrainHeight)
        {
            if(Time.unscaledTime>=nextTerrain)
            {
                nextTerrain=Time.unscaledTime+.2f;
                if(terrain)UnityEngine.Object.Destroy(terrain);
                float extent=blueprint.Radius+width;
                float step=Mathf.Max(2f,extent/50f);int n=Mathf.CeilToInt(2*extent/step)+1;
                var vertices=new Vector3[n*n];var active=new bool[n*n];var lines=new List<int>();
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    float lx=x*step-extent,lz=z*step-extent;var point=origin+rotation*new Vector3(lx,0,lz);
                    float distance=Geometry.Distance(new Point(lx,lz),blueprint.Hull);
                    if(!Heightmap.GetHeight(point,out var old))continue;
                    point.y=Geometry.FoundationHeight(old,origin.y+terrainHeight,distance,width)+.08f;
                    int i=z*n+x;vertices[i]=point;active[i]=distance<width;
                    if(x>0&&active[i]&&active[i-1])lines.AddRange(new[]{i-1,i});
                    if(z>0&&active[i]&&active[i-n])lines.AddRange(new[]{i-n,i});
                }
                terrain=new Mesh{name="Curved terrain preview",indexFormat=IndexFormat.UInt32};
                terrain.vertices=vertices;terrain.SetIndices(lines.ToArray(),MeshTopology.Lines,0);terrain.RecalculateBounds();
            }
            if(terrain)Graphics.DrawMesh(terrain,Matrix4x4.identity,terrainMaterial,0,null,0,null,ShadowCastingMode.Off,false);
        }
        public void Dispose(){if(terrain)UnityEngine.Object.Destroy(terrain);batches.Clear();if(material)UnityEngine.Object.Destroy(material);if(terrainMaterial)UnityEngine.Object.Destroy(terrainMaterial);}
    }
}
