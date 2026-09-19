using System;
using System.Linq;
using Helmsman.Core;
int checks=0;void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
var instant=new[]{"dugout","kayak","tandem_kayak","little_boat","currach"};
Check(ShipConstruction.Blueprints.Where(b=>!b.UsesSlipway).Select(b=>b.Id).SequenceEqual(instant),"All five requested instant-build exceptions remain instant");
Check(ShipConstruction.Blueprints.Count(b=>b.UsesSlipway)==5,"Every other registered hull requires a slipway");
Check(ShipConstruction.Blueprints.Where(b=>b.UsesSlipway).All(b=>ShipConstruction.ValidDuration(b.BuildSeconds*.75)),"Tool reduction preserves valid saved-order durations");
Check(WorkshopCoverage.Contains(100,60,0,80),"Exact base boundary is included");
Check(!WorkshopCoverage.Contains(100,60,1,80),"Quartermaster radius is three-dimensional");
Check(!WorkshopCoverage.Contains(30,31,0,0),"Standalone workshop radius is bounded");
Check(WorkshopCoverage.Contains(100,31,0,0),"Quartermaster coverage extends beyond the local bench radius");
Check(!WorkshopCoverage.Contains(float.NaN,0,0,0)&&!WorkshopCoverage.Contains(100,float.NaN,0,0)&&!WorkshopCoverage.Contains(float.PositiveInfinity,0,0,0),"Invalid coverage cannot grant build access");
Check(CommissionCosts.Refund(ShipConstruction.Find("big_cargo")!.Recipe,true).Count==0,"Free construction never refunds paid materials");
Check(CommissionCosts.Refund(ShipConstruction.Find("big_cargo")!.Recipe,false)["IronNails"]==150,"Cancelled paid construction returns its blueprint costs");
Console.WriteLine($"PASS: {checks} slipway fleet, coverage and cost checks.");
// Recipe expansion must conserve the raw ingredients of the retired supplies.
var supplies=new System.Collections.Generic.Dictionary<string,(string recipe,int amount)> {
 ["ResinWood"]=("RoundLog:10,Resin:10",10),["CaulkedWood"]=("FineWood:10,Resin:10,Coal:10",10),
 ["ClothShip"]=("DeerHide:5",1),["ShipRope"]=("LeatherScraps:5",1)
};
var historical=new System.Collections.Generic.Dictionary<string,string> {
 ["hercule"]="ClothShip:2,ResinWood:30,BronzeNails:100,ShipRope:2",
 ["merchant"]="ClothShip:4,ResinWood:40,IronNails:100,ShipRope:2",
 ["big_cargo"]="ClothShip:4,CaulkedWood:50,IronNails:150,ShipRope:4",
 ["warship"]="ClothShip:4,CaulkedWood:50,IronNails:120,ShipRope:2",
};
foreach(var old in historical)
{
 var expanded=new System.Collections.Generic.Dictionary<string,int>();
 foreach(var cost in ShipwrightRules.Costs(old.Value))
 {
  var parts=supplies.TryGetValue(cost.Key,out var supply)?ShipwrightRules.Costs(supply.recipe):ShipwrightRules.Costs(cost.Key+":1");
  int batch=supplies.ContainsKey(cost.Key)?supply.amount:1;
  foreach(var part in parts)expanded[part.Key]=(expanded.TryGetValue(part.Key,out int n)?n:0)+part.Value*cost.Value/batch;
 }
 var current=ShipConstruction.Find(old.Key)!.Recipe;
 var costs=ShipwrightRules.Costs(current);
 Check(costs.Count==expanded.Count&&expanded.All(p=>costs.TryGetValue(p.Key,out int n)&&n==p.Value),old.Key+" preserves raw ingredient cost");
 Check(ShipConstruction.ValidRecipe(old.Key,old.Value)&&ShipConstruction.ValidRecipe(old.Key,current),"Existing paid and new orders both remain valid");
 Check(!ShipConstruction.ValidRecipe(old.Key,"Wood:1")&&!ShipConstruction.ValidRecipe(old.Key,old.Value+",Wood:1"),"Modified order costs are rejected");
 Check(CommissionCosts.Refund(old.Value,false).ContainsKey("ClothShip"),"Old refunds return the supplies actually paid");
}
Check(!ShipConstruction.ValidRecipe("unknown","Wood:1"),"Unknown blueprints cannot validate an order");
Check(ShipConstruction.Blueprints.All(b=>!ShipwrightRules.Costs(b.Recipe).Keys.Any(supplies.ContainsKey)),"Every ship builds without custom supplies");
Check(HarborCatalog.Items.Where(i=>supplies.ContainsKey(i.Prefab)).All(i=>i.Recipe.Length==0),"Deferred supplies retain inventory prefabs without crafting recipes");
long start=DateTime.UtcNow.Ticks;
Check(SlipwayMotion.Travel(1.39)==0&&SlipwayMotion.Travel(10)==1,"Brake releases before travel and launch finishes exactly");
float prior=0;for(double t=0;t<12;t+=.01){float p=SlipwayMotion.Travel(t);Check(p>=prior&&p<=1,"Launch travel is monotone and bounded");prior=p;}
Check(SlipwayMotion.Level(4)<SlipwayMotion.Travel(4),"Hull remains ramp aligned before water entry");
Check(SlipwayMotion.Reset(0)==1&&SlipwayMotion.Reset(6)==0,"Cradle resets to the exact original position");
long pause=start+4*TimeSpan.TicksPerSecond;
Check(SlipwayMotion.LaunchElapsed(start+20*TimeSpan.TicksPerSecond,start,pause,0)==4,"Obstructed launch retains its sampled pose");
Check(SlipwayMotion.LaunchElapsed(start+21*TimeSpan.TicksPerSecond,start,0,16*TimeSpan.TicksPerSecond)==5,"Resume excludes blocked time");
Check(SlipwayMotion.PartStart("mast 2",.5f)<SlipwayMotion.PartStart("sail 1",.5f)&&SlipwayMotion.PartStart("deck 0",0)<SlipwayMotion.PartStart("mast 0",0),"Deck precedes mast, mast precedes rigging");
var stages=new[]{"deck","mast","rigging","sail","cargo"}.Select(n=>SlipwayMotion.PartStart(n+" 0 wood sail shared material",0)).ToArray();
Check(stages.Zip(stages.Skip(1),(a,b)=>b-a>=.05f).All(v=>v),"Authored stages stay distinct even when materials contain other stage names");
Check(stages[0]>.06f+5*.082f,"Main deck waits for the highest hull strakes");
foreach(YardSound sound in Enum.GetValues(typeof(YardSound)))
{
 var samples=ShipyardAudioSynth.Create(sound);
 Check(samples.Length>1000&&samples.All(n=>float.IsFinite(n)&&Math.Abs(n)<.95f),sound+" samples are finite and have headroom");
 Check(Math.Abs(samples[0])<.001&&Math.Abs(samples[^1])<.001,sound+" has click-free endpoints");
 Check(samples.Select(n=>(double)n*n).Average()>.000005,sound+" is audible");
}
bool Wall(PuffinPoint a,PuffinPoint b)
{
 for(int i=0;i<=100;i++){float t=i/100f,x=a.X+(b.X-a.X)*t,y=a.Y+(b.Y-a.Y)*t,z=a.Z+(b.Z-a.Z)*t;if(x>1&&x<2&&Math.Abs(z)<2&&y<2)return false;}return true;
}
var path=new PuffinFlightPath(new(0,0,0),new(4,0,0),Wall);
for(int i=0;i<200&&path.State==PuffinFlightPath.Result.Searching;i++)path.Step(24);
Check(path.State==PuffinFlightPath.Result.Found&&path.Expanded<=4096,"Puffin finds bounded detour around a wall");
Check(path.Points.Zip(path.Points.Skip(1),(a,b)=>Wall(a,b)).All(v=>v),"Every smoothed flight segment remains clear");
var ground=new PuffinGroundPath(new(0,0,0),new(4,0,0),p=>new PuffinPoint(p.X,0,p.Z),Wall);
for(int i=0;i<100&&ground.State==PuffinFlightPath.Result.Searching;i++)ground.Step(24);
Check(ground.State==PuffinFlightPath.Result.Found&&ground.Points.All(p=>p.Y==0),"Ground detour stays on supporting floor");
var unreachable=new PuffinFlightPath(new(0,0,0),new(300,0,0),(_,_)=>true);
Check(unreachable.State==PuffinFlightPath.Result.Unreachable,"Impossible trip is rejected without unbounded searching");
for(int tool=1;tool<=4;tool++)
{
 for(double t=0;t<60;t+=.01){var p=PuffinPerformance.Station(tool,t);Check(p.Hop==0&&p.X==0&&p.Z==0&&Math.Abs(p.Pitch)<70&&Math.Abs(p.BodyPitch)<70,"Station poses stay planted and anatomically bounded");}
 int contacts=0;for(double t=0;t<10;t+=.01)if(PuffinPerformance.Strike(tool,t,t+.01))contacts++;
 Check(contacts>=3&&contacts<=8,"One sound per tool contact, independent of frame rate");
}
Console.WriteLine($"PASS: {checks} checks including animation, pause/resume, navigation, station poses and audio.");
for(double t=1.4;t<10;t+=.1)
 Check(Math.Abs(SlipwayMotion.Cradle(t,21)-Math.Min(11,SlipwayMotion.Travel(t)*21))<.00001,"Cradle stays under the sliding hull until the release end");
Console.WriteLine("PASS: cradle follows the ship's actual launch distance.");
float immersion=9.81f*2/(50*1);
Check(Math.Abs(SlipwayMotion.FloatOffset(.25f-.6f+immersion,.25f,2,1,-9.81f)+.6f)<.000001,"Launch and native buoyancy agree on the resting waterline");
for(int member=0;member<PuffinCrewPlan.Count;member++)
{
 var direction=PuffinCrewPlan.Departure(123456789,member);
 Check(Math.Abs(direction.X*direction.X+direction.Z*direction.Z-1)<.0001&&direction.Y>.3f,"Crew departures travel outward and rise above the worksite");
 Check(PuffinCrewPlan.ArrivalDelay(123456789,member)>=0&&PuffinCrewPlan.ArrivalDelay(123456789,member)<5,"Arrivals are staggered within a bounded window");
 for(int other=0;other<member;other++)
 {var b=PuffinCrewPlan.Departure(123456789,other);Check(direction.X*b.X+direction.Z*b.Z<.95,"Puffins depart on visibly different bearings");}
 var changed=PuffinCrewPlan.Departure(987654321,member);
 Check(Math.Abs(direction.X-changed.X)+Math.Abs(direction.Z-changed.Z)>.01,"Each construction order changes the departure directions");
 for(double t=0;t<30;t+=.1)
 {
  var pose=PuffinCrewPlan.Work(PuffinCrewPlan.Job(member),t);
  Check(pose.Hop==0&&pose.Crouch>=0&&pose.Crouch<.1&&Math.Abs(pose.BodyPitch)<60,"Crew work stays grounded and within pose bounds");
 }
}
Check(Enumerable.Range(0,PuffinCrewPlan.Count).Select(PuffinCrewPlan.Job).Distinct().Count()==5,"Circus covers hammer, plane, rope, timber and supplies jobs");
Check(!ShipyardAudioSynth.Create(YardSound.Creak,0).SequenceEqual(ShipyardAudioSynth.Create(YardSound.Creak,1)),"Loaded timber uses varied creaks");
var slide=ShipyardAudioSynth.Create(YardSound.WoodSlide);
Check(slide.Length>=ShipyardAudioSynth.Rate*4&&slide[0]==0&&Math.Abs(slide[^1])<.001,"Sliding bed is sustained with click-free loop edges");
Console.WriteLine("PASS: circus work poses, staggered arrivals, dispersed departures and sustained timber audio.");
var hullMin=new PuffinPoint(-2,0,-6);var hullMax=new PuffinPoint(2,3,6);
Check(!PuffinCrewSpace.Clear(new(-4,1,0),new(4,1,0),hullMin,hullMax),"Workers cannot fly through collider-free construction hulls");
Check(!PuffinCrewSpace.Clear(new(0,1,0),new(0,8,0),hullMin,hullMax),"A route cannot start inside the construction hull");
Check(!PuffinCrewSpace.Clear(new(2.1f,1,-9),new(2.1f,1,9),hullMin,hullMax),"Wing clearance inflates construction obstacles");
Check(PuffinCrewSpace.Clear(new(-4,4,0),new(4,4,0),hullMin,hullMax),"Workers can pass above the completed hull");
Check(PuffinCrewSpace.Clear(new(3,1,-9),new(3,1,9),hullMin,hullMax),"Side-platform supply routes remain usable");
Console.WriteLine("PASS: construction hull and mast clearance includes cosmetic geometry.");

long called=TimeSpan.TicksPerDay,landed=called+27*TimeSpan.TicksPerSecond;
Check(PuffinCrewPlan.ConstructionElapsed(called,0,called,called+600*TimeSpan.TicksPerSecond,30)==0,"Unlanded crew cannot spend construction time, even after the minimum build duration");
Check(!PuffinCrewPlan.CanBeginConstruction(called,0,landed,true,false),"A nearby owner waits for every worker to land");
Check(PuffinCrewPlan.CanBeginConstruction(called,0,landed,true,true),"Actual crew readiness releases construction");
Check(!PuffinCrewPlan.CanBeginConstruction(called,landed,landed+10,true,true),"Reload and ownership changes cannot restart a saved construction clock");
Check(PuffinCrewPlan.ConstructionElapsed(called,landed,called,landed,30)==0&&PuffinCrewPlan.ConstructionElapsed(called,landed,called,landed+9*TimeSpan.TicksPerSecond,30)==9,"Build duration begins at landing, with no catch-up jump");
Check(!PuffinCrewPlan.CanBeginConstruction(called,0,called+19*TimeSpan.TicksPerSecond,false,false)&&PuffinCrewPlan.CanBeginConstruction(called,0,called+20*TimeSpan.TicksPerSecond,false,false),"Dedicated/unobserved orders retain a gathering interval without cosmetic NPCs");
Check(PuffinCrewPlan.ConstructionElapsed(0,0,called,landed,30)==27,"Already-running saved orders retain their original elapsed time");
Console.WriteLine("PASS: crew arrival gates construction; saved start time, ownership continuity and headless gathering.");

Check(HarborMotion.Travel(0)==0&&HarborMotion.Travel(4)==1&&HarborMotion.Travel(6)==1&&HarborMotion.Travel(10)==0&&HarborMotion.Travel(12)==0,"Harbor lift, hold, lower and reset have exact endpoints");
for(int i=0;i<120;i++)
{
 double t=i/10.0;float value=HarborMotion.Travel(t);
 Check(value>=0&&value<=1,"Harbor motion cannot exceed rig clearance");
 if(t<4)Check(value<=HarborMotion.Travel(t+.1),"Hoist rises without reversing");
 if(t>=6&&t<10)Check(value>=HarborMotion.Travel(t+.1),"Hoist lowers without reversing");
}
Check(HarborMotion.Travel(double.NaN)==0&&HarborMotion.Travel(double.PositiveInfinity)==0&&HarborMotion.Travel(-1)==0,"Invalid motion samples stay at rest");
Check(HarborMotion.Busy(start,start+11*TimeSpan.TicksPerSecond)&&!HarborMotion.Busy(start,start+12*TimeSpan.TicksPerSecond),"Settling completes before another interaction");
Check(!HarborMotion.Busy(0,start)&&HarborMotion.Elapsed(0,start)==0,"Unused equipment stays idle");
Check(HarborMotion.Elapsed(start,start+long.MaxValue/2)==HarborMotion.Duration,"Late joins and unloaded equipment finish at rest");
if(args.Length==2&&args[0]=="--harbor-motion")
 System.IO.File.WriteAllText(args[1],System.Text.Json.JsonSerializer.Serialize(Enumerable.Range(0,97).Select(i=>new {seconds=i/8.0,travel=HarborMotion.Travel(i/8.0)})));
Console.WriteLine("PASS: harbor machinery timeline, clearance bounds, repeat interaction and network-time restoration.");
