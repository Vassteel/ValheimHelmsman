global using Object=UnityEngine.Object;
using System.Text.Json;
using System.Globalization;
using UnityEngine;
using Helmsman;
var input=args[0];var output=args[1];
var positions=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
foreach(var line in File.ReadLines(input))
{
 var s=line.Split(' ',StringSplitOptions.RemoveEmptyEntries);if(s.Length<2)continue;
 // UnityPy's OBJ exporter mirrors X and reverses winding; undo both.
 if(s[0]=="v")positions.Add(new(-F(s[1]),F(s[2]),F(s[3])));
 if(s[0]=="vn")normals.Add(new(-F(s[1]),F(s[2]),F(s[3])));
 if(s[0]=="vt")uv.Add(new(F(s[1]),F(s[2])));
 if(s[0]=="f")indices.AddRange(s.Skip(1).Reverse().Select(v=>int.Parse(v.Split('/')[0])-1));
}
var mesh=new Mesh{vertices=positions.ToArray(),normals=normals.ToArray(),uv=uv.ToArray(),triangles=indices.ToArray()};
if(mesh.normals.Length!=mesh.vertexCount)mesh.RecalculateNormals();
int checks=0;void Check(bool test,string name){checks++;if(!test)throw new Exception(name);}
var detailed=GullSurfaceRefinement.Create(mesh,true);
Check(mesh.vertexCount==positions.Count,"native mesh not modified");
Check(detailed.triangles.Length==mesh.triangles.Length*16+20*12*3,"subdivision and layered feathers");
Check(detailed.vertexCount<12000,"perched vertex budget");
foreach(int i in detailed.triangles)Check(i>=0&&i<detailed.vertexCount,"valid index");
for(int i=0;i<detailed.vertexCount;i++){Check(float.IsFinite(detailed.vertices[i].x),"finite geometry");Check(detailed.normals[i].sqrMagnitude>.7f,"unit normals");Check(float.IsFinite(detailed.uv[i].x),"preserved UV");}
// Two materials and four bone influences are retained on a synthetic flight patch.
var skin=new Mesh{vertices=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(1,1,0),new Vector3(0,1,0)},normals=Enumerable.Repeat(new Vector3(0,0,1),4).ToArray(),uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)},subMeshCount=2,boneWeights=Enumerable.Range(0,4).Select(i=>new BoneWeight{boneIndex0=i,weight0=1}).ToArray()};
skin.SetTriangles(new[]{0,1,2},0);skin.SetTriangles(new[]{0,2,3},1);
var flight=GullSurfaceRefinement.Create(skin,false);Check(flight.subMeshCount==2,"flight submeshes");
foreach(var w in flight.boneWeights)Check(Math.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<1e-5,"normalized skin weights");
Check(flight.boneWeights.Length==flight.vertexCount,"all vertices skinned");
var root=new GameObject("Gull");root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=new Material(new Shader()){color=new Color(.78f,.8f,.8f)};
var rig=GullMeshRig.Create(root);var scenes=new List<object>();
object Export(string name,Mesh m,Transform transform,Color color)=>new{name,vertices=m.vertices.Select(v=>transform.TransformPoint(v)).Select(v=>new[]{v.x,v.y,v.z}).ToArray(),normals=m.normals.Select(v=>transform.localRotation*v).Select(v=>new[]{v.x,v.y,v.z}).ToArray(),uv=m.uv.Select(v=>new[]{v.x,v.y}).ToArray(),triangles=(int[])m.triangles.Clone(),color=new[]{color.r,color.g,color.b}};
foreach(var posed in new[]{false,true})
{
 rig.Apply(posed?new Helmsman.Core.GullPose{HeadPitch=30,HeadYaw=35,BodyPitch=18,Crouch=.04,TailFan=.1}:new Helmsman.Core.GullPose());
 var parts=new List<object>();
 foreach(var filter in root.GetComponentsInChildren<MeshFilter>())parts.Add(Export(filter.gameObject.name,filter.sharedMesh,filter.transform,filter.GetComponent<MeshRenderer>().sharedMaterial.color));
 scenes.Add(new{name=posed?"gull-peck":"gull-idle",parts});
 Check(float.IsFinite(rig.BeakWorld.y),"posed beak attachment");
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);File.WriteAllText(output,JsonSerializer.Serialize(scenes));
Console.WriteLine($"Gull: {checks:N0} checks passed; {detailed.vertexCount:N0} vertices, {detailed.triangles.Length/3:N0} triangles plus fitted helmet.");
