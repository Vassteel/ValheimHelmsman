using System.Text.Json;
using UnityEngine;
using VikingBirds;
using Helmsman.Core;
var output=args.Length>0?args[0]:"output/bird-geometry.json";
var scenes=new List<object>();
bool performance=args.Contains("--performance");
foreach(var species in performance?new[]{"Puffin","Pelican"}:new[]{"Puffin","Burrowing owl","Pelican"})
{
 bool owl=species=="Burrowing owl";
 using var bird=new PerchedBird(new GameObject("root").transform,new Material(new Shader()),owl,species=="Pelican");
 foreach(var pose in performance?(species=="Puffin"?new[]{"anticipate","hop","hammer","stitch"}:new[]{"glance","preen"}):new[]{"idle","sleep"})
 {
  bird.Pose(pose=="work"?25:0,0,owl&&pose=="work"?40:0,pose=="work"?30:0,pose=="work"?.04f:0);
  bird.SetTool(!owl&&pose=="work"?1:0);
  for(int frame=0;frame<90;frame++){bird.Pose(0,0,0,0,0);bird.Rest(pose=="sleep",0,1f/60);}
  if(performance)
  {
   if(species=="Puffin")
   {
    double t=pose=="anticipate"?.25:pose=="hop"?.775:pose=="hammer"?2.24:26.3;
    var work=PuffinPerformance.Sample(true,t);
    bird.SetTool(work.Tool);bird.Pose(work.Pitch,work.HeadYaw,work.Roll,work.BodyPitch,work.Crouch,work.Wing);
    bird.Stage(new Vector3(work.X,work.Hop,work.Z),work.Yaw,work.Hop*.3f);
   }
   else {bird.Pose(0,0,0,0,0);bird.Idle(pose=="glance"?3.5f:17.4f);}
  }
  var parts=new List<object>();
  foreach(var filter in bird.Root.GetComponentsInChildren<MeshFilter>())
  {
   var mesh=filter.sharedMesh;var color=filter.GetComponent<MeshRenderer>().sharedMaterial.color;
   if(mesh.triangles.Any(i=>i<0||i>=mesh.vertices.Length))throw new Exception("Invalid mesh index");
   var vertices=mesh.vertices.Select(v=>filter.transform.TransformPoint(v)).Select(v=>new[]{v.x,v.y,v.z}).ToArray();
   if(vertices.SelectMany(v=>v).Any(v=>!float.IsFinite(v)))throw new Exception("Non-finite geometry");
   parts.Add(new{name=filter.gameObject.name,vertices,triangles=mesh.triangles,color=new[]{color.r,color.g,color.b}});
  }
  scenes.Add(new{name=species+" — "+pose,parts});
 }
}
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
File.WriteAllText(output,JsonSerializer.Serialize(scenes));
Console.WriteLine("Exported poses from production bird geometry: "+output);
