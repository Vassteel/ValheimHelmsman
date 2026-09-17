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
int characterTicks=0;
for(int i=0;i<50;i++) {if(ImportedSailCompatibility.Prefix(legacy,.02f))throw new NullReferenceException("Old ship has no current cloth rig");characterTicks++;}
Check(characterTicks==50,"Compatibility path allows later character updates to execute every tick");
Console.WriteLine($"PASS: {count} legacy sail regression checks using production code with game doubles.");
