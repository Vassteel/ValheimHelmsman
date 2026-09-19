using System.Text.Json;
using UnityEngine;
using VikingBirds;
using Helmsman.Core;
internal static class CrewExport
{
 internal static void Run(string destination)
 {
  Directory.CreateDirectory(destination);
  using var bird=new PerchedBird(new GameObject("Circus preview").transform,new Material(new Shader()),false);
  var filters=bird.Root.GetComponentsInChildren<MeshFilter>(true);
  float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
  var parts=filters.Select(f=>new {name=f.gameObject.name,vertices=f.sharedMesh.vertices.Select(V),normals=f.sharedMesh.normals.Select(V),triangles=f.sharedMesh.triangles,uv=f.sharedMesh.uv.Select(v=>new[]{v.x,v.y}),material=f.GetComponent<MeshRenderer>().sharedMaterial.name,color=new[]{f.GetComponent<MeshRenderer>().sharedMaterial.color.r,f.GetComponent<MeshRenderer>().sharedMaterial.color.g,f.GetComponent<MeshRenderer>().sharedMaterial.color.b}}).ToArray();
  object Pose(int index,double time,bool flight,bool carry,bool waiting=false)
  {
   bird.Root.transform.localPosition=Vector3.zero;bird.Root.transform.localRotation=Quaternion.identity;
   var job=PuffinCrewPlan.Job(index);var p=flight||waiting?default:PuffinCrewPlan.Work(job,time);
   if(carry)p=PuffinCrewPlan.Work(job,time);
   bird.SetTool(p.Tool);bird.Pose(p.Pitch,p.HeadYaw,p.Roll,p.BodyPitch,p.Crouch,p.Wing);bird.CloseEyes(0);
   if(flight)bird.Fly((float)time,1,0);else if(carry)bird.Waddle((float)time*6,1);
   return filters.Select(f=>{var m=f.transform.Matrix;bool visible=true;for(var t=f.transform;t!=null;t=t.parent)visible&=t.gameObject.activeSelf;return new{visible,matrix=new[]{m.M11,m.M12,m.M13,m.M14,m.M21,m.M22,m.M23,m.M24,m.M31,m.M32,m.M33,m.M34,m.M41,m.M42,m.M43,m.M44}};}).ToArray();
  }
  const int fps=12;var frames=new List<object>();
  for(int frame=0;frame<360;frame++)
  {
   float time=frame/(float)fps;var agents=new List<object>();
   for(int i=0;i<PuffinCrewPlan.Count;i++)
   {
    int side=i%2==0?-1:1;float z=new[]{5.2f,1.8f,-1.8f}[i/2];float y=1.212f+(10+z)*.075f;
    var work=new Vector3(side*3.05f,y,z);float delay=PuffinCrewPlan.ArrivalDelay(723456789,i);var dir=PuffinNavigationFree(PuffinCrewPlan.Departure(723456789,i));
    bool leaving=time>=12;float arrive=SlipwayMotion.Ease((time-delay)/4),leave=SlipwayMotion.Ease((time-12-i*.22)/6);
    dir.x=side*Math.Max(.4f,Math.Abs(dir.x));
    Vector3 position=work+(dir*17+Vector3.up*4)*(1-arrive);
    float yaw=side<0?90:-90;bool carrying=i>=4;
    if(arrive==1&&carrying&&!leaving&&time>=8.5f)
    {
     float trip=(time-8.5f)%8;float walk=SlipwayMotion.Ease((trip%4)/2.7);float along=trip<4?-2.3f*walk:-2.3f*(1-walk);
     position=work+new Vector3(0,along*.075f,along);yaw=trip<4?180:0;carrying=trip>=4;
    }
    if(leaving){var d=PuffinNavigationFree(PuffinCrewPlan.Departure(723456789,i+6));float lift=SlipwayMotion.Ease((time-12-i*.22)/2.5);float outwards=SlipwayMotion.Ease((time-14.5-i*.22)/3.5);position=work+Vector3.up*13*lift+(d*22+Vector3.up*4)*outwards;yaw=(float)(Math.Atan2(d.x,d.z)*180/Math.PI);}
    else if(arrive<1)yaw=(float)(Math.Atan2(-dir.x,-dir.z)*180/Math.PI);
    agents.Add(new{index=i,visible=time>=delay&&leave<1,position=V(position),yaw,propsVisible=arrive>=1&&!leaving,parts=Pose(i,time+PuffinCrewPlan.Variation(723456789,i)*17,arrive<1||leaving,carrying&&!leaving&&(time>=8.5f||arrive<1),time<8.5f),beak=V(bird.BeakWorld)});
   }
   double launch=Math.Max(0,time-12);
   frames.Add(new{time,progress=Math.Clamp((time-8.5f)/3.5f,0,1),travel=SlipwayMotion.Travel(launch),level=SlipwayMotion.Level(launch),cradle=time<22?SlipwayMotion.Cradle(launch,21.92f):11*SlipwayMotion.Reset(time-22),agents});
  }
  var roles=Enumerable.Range(0,6).Select(i=>new {index=i,job=PuffinCrewPlan.Job(i).ToString(),parts=Pose(i,i==0?.74:1.1,false,i>=4),beak=V(bird.BeakWorld)}).ToArray();
  File.WriteAllText(Path.Combine(destination,"crew.json"),JsonSerializer.Serialize(new{fps,parts,frames,roles}));
  Console.WriteLine("Exported circus production geometry, tool poses and open-slipway review choreography.");
 }
 private static Vector3 PuffinNavigationFree(PuffinPoint p)=>new Vector3(p.X,p.Y,p.Z);
}
