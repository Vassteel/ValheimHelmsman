using Helmsman;
using UnityEngine;
int count=0;
void Check(bool v,string message){if(!v)throw new Exception(message);count++;}
Ship Make(bool imported=true){var go=new GameObject();var ship=go.AddComponent<Ship>();ship.m_sailObject=new();ship.m_mastObject=new();if(imported)go.AddComponent<ImportedSailAnimation>();return ship;}
var native=Make(false);Check(ImportedSailCompatibility.Prefix(native,.02f),"Native/unrelated ships retain the game's sail update");
var legacy=Make();legacy.m_sailObject.transform.localScale=new Vector3(2,3,4);
foreach(var entry in new[]{(Ship.Speed.Stop,.1f),(Ship.Speed.Half,.5f),(Ship.Speed.Full,1f),(Ship.Speed.Slow,.1f),(Ship.Speed.Back,.1f)})
{
 legacy.Setting=entry.Item1;
 for(int i=0;i<100;i++)Check(!ImportedSailCompatibility.Prefix(legacy,.02f),"Imported ships must never enter missing cloth-rig references");
 var s=legacy.m_sailObject.transform.localScale;
 Check(Math.Abs(s.y-entry.Item2*3)<.001f&&s.x==2&&s.z==4,"Furl amount matches native speed without deforming sail width/depth");
 Check(float.IsFinite(legacy.m_mastObject.transform.rotation.Q.W),"Mast animation stays finite");
}
legacy.Setting=Ship.Speed.Full;var before=legacy.m_sailObject.transform.localScale.y;ImportedSailCompatibility.Prefix(legacy,.02f);Check(legacy.m_sailObject.transform.localScale.y>before&&legacy.m_sailObject.transform.localScale.y<3,"Sail unfolds gradually");
legacy.m_sailObject=null;legacy.m_mastObject=null;Check(!ImportedSailCompatibility.Prefix(legacy,.02f),"Missing legacy visual objects cannot interrupt shared physics");
var rowing=Make();rowing.m_hasSail=false;Check(!ImportedSailCompatibility.Prefix(rowing,.02f)&&rowing.m_sailObject.transform.localScale.y==1,"Rowing-only hulls bypass unused sail animation");
var noEnvironment=Make();EnvMan.instance=null;Check(!ImportedSailCompatibility.Prefix(noEnvironment,.02f),"Scene startup without environment cannot interrupt shared physics");
EnvMan.instance=new();EnvMan.instance.Wind=new Vector3();noEnvironment.Setting=Ship.Speed.Full;Check(!ImportedSailCompatibility.Prefix(noEnvironment,.02f),"Zero projected wind leaves a valid mast rotation");
var fixedRig=Make();fixedRig.GetComponent<ImportedSailAnimation>().FixedMast=true;
fixedRig.Setting=Ship.Speed.Stop;
for(int i=0;i<100;i++)ImportedSailCompatibility.Prefix(fixedRig,.02f);
Check(Math.Abs(fixedRig.m_sailObject.transform.localScale.y-.1f)<.001f,"Final fleet sails still furl on fixed standing rigging");
int characterTicks=0;
for(int i=0;i<50;i++) {if(ImportedSailCompatibility.Prefix(legacy,.02f))throw new NullReferenceException("Old ship has no current cloth rig");characterTicks++;}
Check(characterTicks==50,"Compatibility path allows later character updates to execute every tick");
Console.WriteLine($"PASS: {count} legacy sail regression checks using production code with game doubles.");

var spawned=Make();var authored=spawned.GetComponent<ImportedSailAnimation>();authored.ConfiguredScale=true;authored.RestScale=new Vector3(1,1,1);
spawned.m_sailObject.transform.localScale=new Vector3(1,.1f,1);spawned.Setting=Ship.Speed.Full;
for(int i=0;i<100;i++)ImportedSailCompatibility.Prefix(spawned,.02f);
Check(Math.Abs(spawned.m_sailObject.transform.localScale.y-1)<.0001f,"A ship spawned with a furled preview must still unfold to its authored size");
authored.MeshFurl=true;spawned.Setting=Ship.Speed.Stop;
for(int i=0;i<100;i++)ImportedSailCompatibility.Prefix(spawned,.02f);
Check(Math.Abs(authored.FurlAmount-.1f)<.0001f&&spawned.m_sailObject.transform.localScale.y==1,"Diagonal sails furl through mesh anchors without collapsing their transform vertically");
spawned.Setting=Ship.Speed.Full;
for(int i=0;i<100;i++)ImportedSailCompatibility.Prefix(spawned,.02f);
Check(Math.Abs(authored.FurlAmount-1)<.0001f,"Anchored sails reopen fully after reefing");
Console.WriteLine($"PASS: {count} sail regression checks including furled spawn recovery and mesh furling.");

void Close(Vector3 a,Vector3 b,string message)=>Check((a-b).sqrMagnitude<.000001f,message);
var lateenA=new Vector3(.45f,1.95f,3.4f);var lateenB=new Vector3(.45f,8.65f,-3.25f);var clew=new Vector3(.45f,1.75f,-3.45f);
Close(FleetSailShape.Anchor("falkusa",lateenA,9),lateenA,"Lateen tack stays on diagonal yard");
Close(FleetSailShape.Anchor("falkusa",lateenB,9),lateenB,"Lateen peak stays on diagonal yard");
var reef=Vector3.Lerp(FleetSailShape.Anchor("falkusa",clew,9),clew,.1f);
Check(reef.y>4.8f&&reef.y<5,"Lateen clew reefs upward toward the diagonal spar rather than flattening horizontally");
Check(FleetSailShape.Weight("falkusa",lateenA,9,0,4)<.001f,"Spar pin does not flutter away from its attachment");
Check(FleetSailShape.Weight("falkusa",new Vector3(.7f,4.1166667f,-1.1f),9,0,4)>.9f,"Lateen interior has a free billowing area");
var jibA=new Vector3(-.25f,1.48f,5.45f);Close(FleetSailShape.Anchor("falkusa",jibA,9),jibA,"Jib reefs to its own stay, independently of the main");
var currachA=new Vector3(.125f,4.4f,1.12f);var currachC=new Vector3(.135f,.95f,1.12f);
Close(FleetSailShape.Anchor("currach",currachA,5),currachA,"Currach sailhead stays on mast");Close(FleetSailShape.Anchor("currach",currachC,5),currachC,"Currach tack stays on mast");
foreach(var kind in new[]{"ottar","freighter","snekkja","ceol"})
{
 var head=new Vector3(2,8,.3f);Close(FleetSailShape.Anchor(kind,head,8),head,"Square sailhead stays on its yard");
 Check(FleetSailShape.Weight(kind,head,8,2,2)<.001f,"Square sail pinned edge does not flutter");
 Check(FleetSailShape.Weight(kind,new Vector3(0,5,.3f),8,2,2)>.99f,"Square sail middle billows");
}
Console.WriteLine($"PASS: {count} production sail checks including all six anchored sail shapes.");

foreach(var kind in new[]{"ottar","freighter","snekkja","ceol","currach","falkusa"})
{
 var middle=kind=="currach"?new Vector3(.2f,2.1066667f,.23f):kind=="falkusa"?new Vector3(.7f,4.1166667f,-1.1f):new Vector3(0,5,.3f);
 foreach(float amount in new[]{.1f,.5f,1f})
 {
  var samples=Enumerable.Range(0,100).Select(i=>FleetSailShape.Flutter(kind,middle,8,2,2,i*.05f,.6f,amount)).ToArray();
  Check(samples.Max()-samples.Min()>.04f,kind+" has visible cloth motion even reefed");
  Check(samples.All(v=>float.IsFinite(v)&&Math.Abs(v)<.33f),kind+" cloth motion stays bounded");
 }
}
Console.WriteLine($"PASS: {count} sail checks including visible and bounded flutter at every reef setting.");
