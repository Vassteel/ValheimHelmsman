using System;
using Helmsman.Core;

int passed=0;
void Check(bool condition,string label){if(!condition)throw new Exception("FAIL: "+label);passed++;Console.WriteLine("PASS: "+label);}
var gate=new BoardingGate(0);
Check(!gate.Update(9,true),"Early boarding does not shorten grace period");
Check(gate.Update(10,true),"Boarded player can leave after grace");
gate=new BoardingGate(0);
Check(!gate.Update(10,false),"No departure when countdown expires without player");
Check(!gate.Update(100,false),"Waiting indefinitely never grants boarding");
Check(!gate.Update(101,true),"Late boarding starts final countdown");
Check(!gate.Update(103,true),"Final countdown lasts three seconds");
Check(!gate.Update(103.5,false),"Stepping off resets final countdown");
Check(!gate.Update(104,true),"Reboarding restarts full countdown");
Check(!gate.Update(106.9,true),"Reset countdown cannot depart early");
Check(gate.Update(107,true),"Reboarded player departs after fresh countdown");
Check(!gate.Update(108,false),"Boarding condition checked even after prior readiness");

bool AroundIsland(Point a,Point b)
{
    int count=(int)Math.Ceiling(a.Distance(b)*4)+1;
    for(int i=0;i<=count;i++)
    {
        double t=(double)i/count;double x=a.X+(b.X-a.X)*t,y=a.Y+(b.Y-a.Y)*t;
        if(x>=8 && x<=22 && Math.Abs(y)<=7)return false;
    }
    return true;
}
var route=new RouteSearch(new Point(0,0),new Point(30,0),AroundIsland,4,1000);
while(route.State==SearchState.Searching)route.Step(3);
Check(route.State==SearchState.Found,"A* routes around island");
Check(route.Route.Exists(p=>Math.Abs(p.Y)>7),"Island route leaves direct heading");
bool clear=true;for(int i=1;i<route.Route.Count;i++)clear &= AroundIsland(route.Route[i-1],route.Route[i]);
Check(clear,"Every returned route edge avoids island");
Check(route.Route[0].Distance(new Point(0,0))==0 && route.Route[^1].Distance(new Point(30,0))==0,"Route preserves exact endpoints");
route=new RouteSearch(new Point(0,0),new Point(30,0),(a,b)=>false,4,100);
while(route.State==SearchState.Searching)route.Step();
Check(route.State==SearchState.NoRoute,"Blocked start returns no route");
route=new RouteSearch(new Point(0,0),new Point(1000,1000),(a,b)=>true,4,2);
while(route.State==SearchState.Searching)route.Step();
Check(route.State==SearchState.BudgetExceeded,"Unbounded exploration has a hard budget");
route=new RouteSearch(new Point(0,0),new Point(1,1),(a,b)=>b.X!=1 || b.Y!=1,4,100);
while(route.State==SearchState.Searching)route.Step();
Check(route.State!=SearchState.Found,"Goal connector cannot skip blocked destination");
Check(HelmMath.StoppingDistance(5,.25,1.5,4)>HelmMath.StoppingDistance(2,.25,1.5,4),"Stopping margin grows with speed");
Check(HelmMath.StoppingDistance(-5,.25,1.5,4)==HelmMath.StoppingDistance(5,.25,1.5,4),"Reverse momentum gets same stopping margin");
Check(HelmMath.Rudder(30,true)==-HelmMath.Rudder(30,false),"Reverse steering sign is inverted");
Check(Math.Abs(HelmMath.Rudder(180,false))<1,"Hard turns retain some paddling thrust");
Check(!HelmMath.NeedsSlowTurn(45,false),"Ordinary course corrections preserve sailing mode");
Check(!HelmMath.NeedsSlowTurn(-75,false),"Wide turns in either direction can retain sail");
bool slowing=HelmMath.NeedsSlowTurn(90,false);
Check(slowing,"Sharp turns request reduced propulsion");
slowing=HelmMath.NeedsSlowTurn(79,slowing);
Check(slowing,"Small heading fluctuations do not immediately restore sail");
slowing=HelmMath.NeedsSlowTurn(-60,slowing);
Check(slowing,"Slow-turn hysteresis is symmetric for port and starboard");
slowing=HelmMath.NeedsSlowTurn(54,slowing);
Check(!slowing,"Sailing resumes once the sharp turn has opened out");
Check(!HelmMath.NeedsSlowTurn(60,slowing),"Returning toward the exit threshold does not drop sail again");
var mood=new GullMoodState();
Check(mood.Update(0,false,false,false,false)==GullMood.Calm,"Gull starts calm");
Check(mood.Update(1,false,false,false,true)==GullMood.Calm,"Brief rough-water signal does not flicker the expression");
Check(mood.Update(4,false,false,false,true)==GullMood.RoughSeas,"Sustained rough water makes the gull brace");
Check(mood.Update(5,false,false,true,true)==GullMood.RoughSeas,"Weather changes wait to settle");
Check(mood.Update(8,false,false,true,true)==GullMood.Fog,"Fog takes priority over rough water");
mood.Update(9,false,true,true,true);
Check(mood.Update(12,false,true,true,true)==GullMood.Storm,"Storm takes priority over fog and rough water");
Check(mood.Update(12.1,true,true,true,true)==GullMood.Combat,"Combat interrupts weather expressions immediately");
Check(mood.Update(17,false,false,false,false)==GullMood.Combat,"Combat expression lingers after the threat disappears");
Check(mood.Update(19,false,false,false,false)==GullMood.Combat,"Returning to calm also waits for stable conditions");
Check(mood.Update(20,false,false,false,false)==GullMood.Combat,"Repeated samples do not shorten the settling interval");
Check(mood.Update(22,false,false,false,false)==GullMood.Calm,"Gull settles back to calm after combat");
Check(GullPerformance.Sample(GullMood.Calm,3.825).HeadPitch>35,"Calm sequence contains a full peck gesture");
Check(GullPerformance.Sample(GullMood.Calm,10).HeadYaw>70,"Calm sequence contains a separate preening turn");
Check(GullPerformance.Sample(GullMood.RoughSeas,0,0,0,12).BodyRoll<0,"Rough-sea posture counters ship roll");
Check(GullPerformance.Sample(GullMood.RoughSeas,6.125).Hop>.1,"Rough-sea sequence contains a wing-assisted balance hop");
Check(GullPerformance.Sample(GullMood.Storm,1).Crouch>.1,"Storm sequence tucks down against wind");
Check(Math.Abs(GullPerformance.Sample(GullMood.Storm,3).BodyRoll)>3,"Storm sequence includes a vigorous feather shake");
Check(GullPerformance.Sample(GullMood.Fog,2).HeadYaw< -50 && GullPerformance.Sample(GullMood.Fog,6).HeadYaw>50,"Fog sequence searches both horizons with held looks");
Check(GullPerformance.Sample(GullMood.Combat,.805).Hop>.39,"Combat opens with a visible alarm hop and wingbeat");
Check(GullPerformance.Sample(GullMood.Combat,4.55).Hop>.24,"Combat contains a second distinct wingbeat hop");
Check(GullPerformance.Sample(GullMood.Combat,2,60).BodyYaw>55,"Combat directs the body toward the detected threat");
var neutral=new GullPose();var alert=GullPerformance.Sample(GullMood.Combat,2,60);
var blended=GullPerformance.Blend(neutral,alert,.5);
Check(blended.BodyYaw>0 && blended.BodyYaw<alert.BodyYaw,"Pose blending eases mood changes without snapping");
bool bounded=true;
foreach(GullMood state in Enum.GetValues(typeof(GullMood)))
for(int tick=0;tick<600;tick++)
{
    var p=GullPerformance.Sample(state,tick*.05,120,-120,30,-20);
    bounded &= !double.IsNaN(p.HeadPitch+p.BodyRoll+p.Hop+p.Crouch) && p.Hop>=0 && p.Hop<=.401 &&
        Math.Abs(p.BodyYaw)<=80 && Math.Abs(p.HeadYaw)<=90 && Math.Abs(p.BodyRoll)<=20;
}
Check(bounded,"All five sequences remain finite and bounded across repeated cycles");
Check(ShipText.CleanName("  Sea Wolf  ")=="Sea Wolf","Ship names trim surrounding whitespace");
Check(ShipText.CleanName("<color=red>Sea Wolf</color>")=="Sea Wolf","Ship names cannot inject rich text into menus");
Check(ShipText.CleanName(new string('x',60)).Length==48,"Ship names respect the saved-name limit");
Check(ShipText.CleanName("Sea\nWolf\t")=="SeaWolf","Ship names remove control characters");
Check(ShipText.CleanName(" <b></b> ")=="","Formatting-only ship names are rejected as empty");
string[] bearings={"N","NE","E","SE","S","SW","W","NW"};
for(int i=0;i<8;i++)
{
    double angle=i*Math.PI/4;
    Check(ShipText.Bearing(Math.Sin(angle)*100,Math.Cos(angle)*100)==bearings[i],"Summon bearing "+bearings[i]);
}
Check(ShipText.Bearing(0,0)=="Here","Zero-distance ship bearing is explicit");
// Installed OdinShip 0.7.9 prefab metadata: seated helm counts, beds and hold-fast points do not.
var odinShips=new[]{
    (Name:"BigCargoShip",Force:.04f,Seats:5,Expected:true),
    (Name:"CargoShip",Force:.04f,Seats:5,Expected:true),
    (Name:"DoubleRowingCanoe",Force:0f,Seats:2,Expected:true),
    (Name:"LittleBoat",Force:.03f,Seats:0,Expected:true),
    (Name:"MercantShip",Force:.05f,Seats:6,Expected:true),
    (Name:"RowingCanoe",Force:0f,Seats:1,Expected:true),
    (Name:"WarShip",Force:.06f,Seats:13,Expected:true)
};
foreach(var ship in odinShips)
    Check(ShipRules.Eligible(ShipRules.CanSail(ship.Name,true,ship.Force),ship.Seats)==ship.Expected,"OdinShip eligibility: "+ship.Name);
Check(!ShipRules.Eligible(false,0),"Seatless rowing boats are excluded");
Check(ShipRules.Eligible(false,1),"Single-seat canoe can be recalled by its gull");
Check(ShipRules.Eligible(false,2),"Two-seat rowing boats remain eligible");
Check(ShipRules.CanSail("VikingShip",true,.1f),"Longship remains eligible for sailing");
Check(ShipRules.CanSail("CustomSailingShip",true,.1f),"Sailing ships do not need a prefab whitelist");
Check(!ShipRules.CanSail("DoubleRowingCanoe",true,0),"Canoe dummy sails never select wind propulsion");
Check(!ShipRules.CanSail("Mod_Row_Boat",true,.1f),"Named rowboats with template sail settings still row");
Check(!ShipRules.CanSail("CustomBoat",false,.1f),"Missing sail forces rowing mode");
Check(ShipRules.CanSail("Karve",false,.03f,true),"Installed Karve cloth sail works without the retired sail object");
Check(ShipRules.CanSail("Raft",false,.05f,true),"Installed Raft cloth sail remains eligible");
Check(!ShipRules.CanSail("DoubleRowingCanoe",true,0,true),"Cloth or legacy flags do not turn a powerless canoe into a sailing boat");
Check(!ShipRules.CanSail("Mod_Row_Boat",false,.1f,true),"Rowboat exclusions also apply to cloth sails");
Check(!ShipRules.IsSeat("attach_mast") && !ShipRules.IsSeat("attach_dragon"),"Hold-fast points do not inflate seat counts");
Check(!ShipRules.IsSeat("attach_bed") && !ShipRules.IsSeat(""),"Beds and standing helms do not count as seats");
Check(ShipRules.IsSeat("attach_sitship") && ShipRules.IsSeat("attach_chair"),"Passenger and seated helm animations count");
Check(ShipRules.IsSeat("attach_lox"),"Odin single-canoe seated helm is recognized");
ShorelineTests.Run(Check);
GullcallModelTests.Run(Check);
GuardedStepsTests.Run(Check);
ShipConstructionTests.Run(Check);
        ShipwrightRulesTests.Run(Check);
Console.WriteLine($"{passed} checks passed.");
