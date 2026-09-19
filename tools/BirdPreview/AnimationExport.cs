using System.Text.Json;
using UnityEngine;
using VikingBirds;
using Helmsman.Core;
internal static class AnimationExport
{
 internal static void Run(string destination)
 {
  Directory.CreateDirectory(destination);
  using var bird=new PerchedBird(new GameObject("Preview root").transform,new Material(new Shader()),false);
  var filters=bird.Root.GetComponentsInChildren<MeshFilter>(true);
  float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
  var parts=filters.Select(f=>new {name=f.gameObject.name,vertices=f.sharedMesh.vertices.Select(V),normals=f.sharedMesh.normals.Select(V),triangles=f.sharedMesh.triangles,uv=f.sharedMesh.uv.Select(v=>new[]{v.x,v.y}),material=f.GetComponent<MeshRenderer>().sharedMaterial.name,color=new[]{f.GetComponent<MeshRenderer>().sharedMaterial.color.r,f.GetComponent<MeshRenderer>().sharedMaterial.color.g,f.GetComponent<MeshRenderer>().sharedMaterial.color.b}}).ToArray();
  var frames=new List<object>();const int fps=12;
  for(int frame=0;frame<fps*24;frame++)
  {
   float t=frame/(float)fps;int chapter=(int)(t/4);float time=t%4;
   bird.Root.transform.localPosition=Vector3.zero;bird.Root.transform.localRotation=Quaternion.identity;
   bird.SetTool(0);bird.Pose(0,0,0,0,0);bird.CloseEyes(0);
   if(chapter<4)
   {
    var p=PuffinPerformance.Station(chapter+1,time);bird.SetTool(p.Tool);bird.Pose(p.Pitch,p.HeadYaw,p.Roll,p.BodyPitch,p.Crouch,p.Wing);
   }
   else if(chapter==4){bird.Waddle(time*.46f/.24f*Mathf.PI*2,1);bird.Root.transform.localRotation=Quaternion.Euler(0,time*22,0);}
   else {bird.Fly(time,SlipwayMotion.Ease(time/.4)*SlipwayMotion.Ease((4-time)/.6),0);bird.Root.transform.localPosition=new Vector3(0,.15f*SlipwayMotion.Ease(time/.5)*SlipwayMotion.Ease((4-time)/.6),0);}
   object Transform(MeshFilter f)
   {
    var m=f.transform.Matrix;bool visible=true;for(var p=f.transform;p!=null;p=p.parent)visible&=p.gameObject.activeSelf;
    return new{visible,matrix=new[]{m.M11,m.M12,m.M13,m.M14,m.M21,m.M22,m.M23,m.M24,m.M31,m.M32,m.M33,m.M34,m.M41,m.M42,m.M43,m.M44}};
   }
   frames.Add(new{label=new[]{"Timber preparation","Caulking","Rigging and sailcloth","Paint and brushes","Walking and turning","Flight and settling"}[chapter],parts=filters.Select(Transform).ToArray()});
  }
  File.WriteAllText(Path.Combine(destination,"animation.json"),JsonSerializer.Serialize(new{fps,parts,frames}));
  foreach(YardSound sound in Enum.GetValues<YardSound>())
  for(int variant=0;variant<(sound==YardSound.Creak?4:1);variant++)
  {
   var samples=ShipyardAudioSynth.Create(sound,variant);using var writer=new BinaryWriter(File.Create(Path.Combine(destination,sound+(variant==0?"":variant.ToString())+".wav")));
   writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(ShipyardAudioSynth.Rate);writer.Write(ShipyardAudioSynth.Rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);foreach(float v in samples)writer.Write((short)(Math.Clamp(v,-1,1)*32767));
  }
  using var packed=new System.IO.Compression.GZipStream(File.OpenRead("assets/ships/final/MercantShip.bin.gz"),System.IO.Compression.CompressionMode.Decompress);
  using var reader=new BinaryReader(packed);reader.ReadBytes(4);using var spec=JsonDocument.Parse(reader.ReadBytes(reader.ReadInt32()));
  float launchDistance=14+spec.RootElement.GetProperty("length").GetSingle()*.5f;
  var slip=Enumerable.Range(0,241).Select(frame=>{float t=frame/12f;double launch=Math.Max(0,t-4);return new{time=t,progress=Math.Min(1,t/4),travel=SlipwayMotion.Travel(launch),level=SlipwayMotion.Level(launch),cradle=t<14?SlipwayMotion.Cradle(launch,launchDistance):11*SlipwayMotion.Reset(t-14)};});
  File.WriteAllText(Path.Combine(destination,"slipway-timeline.json"),JsonSerializer.Serialize(slip));
  File.WriteAllText(Path.Combine(destination,"construction-stages.json"),JsonSerializer.Serialize(new[]{"fixed","deck","mast","rigging","sail","cargo","decoration"}.ToDictionary(g=>g,g=>SlipwayMotion.PartStart(g+" 0",.5f))));
  Console.WriteLine("Exported production poses, launch/reset timeline and original sound samples: "+destination);
 }
}
