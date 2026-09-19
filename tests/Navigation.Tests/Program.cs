using Helmsman;
using Helmsman.Core;
using UnityEngine;
int checks=0;
void Check(bool value,string label){if(!value)throw new Exception("FAIL: "+label);checks++;Console.WriteLine("PASS: "+label);}
Collider Part<T>()where T:Component,new(){var go=new GameObject();go.AddComponent<T>();var child=new GameObject();child.transform.SetParent(go.transform);return child.AddComponent<BoxCollider>();}
var own=new GameObject().AddComponent<Ship>();var chart=new WaterChart(own);
bool Clear(bool relaxed=false)=>chart.HullSegment(new(0,30,0),new(0,30,10),Quaternion.identity,true,out _,relaxed);
Physics.Nearby=new[]{Part<Fish>()};Check(Clear(),"Fish beneath hull do not stop navigation");
Physics.Cast=new[]{new RaycastHit{collider=Physics.Nearby[0]}};Physics.Nearby=Array.Empty<Collider>();Check(Clear(),"Fish in forward sweep do not force replanning");
Physics.Cast=Array.Empty<RaycastHit>();Physics.Nearby=new[]{Part<GullGuide>()};Check(Clear(),"Guide gull is never a navigational obstacle");
Physics.Nearby=new[]{Part<ItemDrop>()};Check(Clear(),"Loose stone and resource drops do not re-block the cleared route");
Physics.Nearby=new[]{Part<Character>()};Check(Clear(),"Existing character exclusion preserved");
var ownPart=new GameObject();ownPart.transform.SetParent(own.transform);Physics.Nearby=new[]{ownPart.AddComponent<BoxCollider>()};Check(Clear(),"Own hull remains excluded");
Physics.Nearby=new[]{Part<Ship>()};Check(!Clear(true),"Other boats still block relaxed dock passage");
Physics.Nearby=new[]{Part<Piece>()};Check(!Clear()&&Clear(true),"Dock-piece relaxation remains scoped to dock maneuvers");
Physics.Nearby=new[]{new GameObject().AddComponent<BoxCollider>()};Check(!Clear(true),"Submerged rocks still block unsafe passages");
RockClearing.Eligible.Add(Physics.Nearby[0]);chart.PlanRockClearing=true;
Check(chart.RegionalSegment(new(0,0),new(0,10)),"Enabled route planning can target a passage through clearable rock");
Check(!Clear(),"Live hull clearance still stops the boat before the planned rock");
chart.PlanRockClearing=false;Check(!chart.RegionalSegment(new(0,0),new(0,10)),"Disabled rock clearing keeps rocks blocked during planning");
RockClearing.Eligible.Clear();
Physics.Cast=new[]{new RaycastHit{collider=Physics.Nearby[0]}};Physics.Nearby=Array.Empty<Collider>();Check(!Clear(),"Rock along the swept path remains an obstacle");
Physics.Cast=Array.Empty<RaycastHit>();Heightmap.Ground=29.5f;Check(!Clear(),"Shallow terrain remains blocked");Heightmap.Ground=25;
ZoneSystem.instance.Loaded=p=>false;Check(!Clear(),"Local detours cannot treat unloaded terrain as safe");ZoneSystem.instance.Loaded=p=>true;
var prior=new[]{new Point(0,0),new Point(24,0),new Point(72,0),new Point(240,0),new Point(500,0)};
// A narrow navigable channel has an obstruction in its middle. A regional 24 m
// step cannot enter its side passage; a hull-clear six-metre detour can.
bool Passage(Point a,Point b){int samples=(int)Math.Ceiling(a.Distance(b)*2)+1;for(int i=0;i<=samples;i++){double t=(double)i/samples,x=a.X+(b.X-a.X)*t,y=a.Y+(b.Y-a.Y)*t;if(Math.Abs(y)>15||x<0||x>90||x>=22&&x<=38&&Math.Abs(y)<7)return false;}return true;}
var coarse=new RouteSearch(prior[0],prior[2],Passage);while(coarse.State==SearchState.Searching)coarse.Step();Check(coarse.State==SearchState.NoRoute,"Regional grid reproduces missed narrow passage");
var detour=LocalDetour.Create(prior[0],prior,1,Passage)!;while(detour.Search.State==SearchState.Searching)detour.Search.Step();
Check(detour.Search.State==SearchState.Found,"Local search finds a passage around the rock");
var result=detour.Splice(prior);Check(result[^1].X==500&&result[^2].X==240,"Local repair preserves distant waypoints and destination");
Check(detour.Search.Route.Zip(detour.Search.Route.Skip(1),(a,b)=>Passage(a,b)).All(v=>v),"Every detour edge passes clearance, including rejoin");
Check(detour.Search.Route.Any(p=>Math.Abs(p.Y)>=7),"Detour travels around the obstruction");
Check(LocalDetour.Create(prior[0],prior,4,Passage)==null,"Distant rejoin falls back to regional planning");
Check(LocalDetour.Create(prior[0],prior,9,Passage)==null,"Missing waypoint cannot fabricate a repair");
var blocked=LocalDetour.Create(prior[0],prior,1,(a,b)=>false)!;while(blocked.Search.State==SearchState.Searching)blocked.Search.Step();Check(blocked.Search.State==SearchState.NoRoute,"No passage stays blocked instead of bypassing safety");
bool rejected=false;try{blocked.Splice(prior);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Failed detour cannot replace the existing route");
var river=new AdaptiveRouteSearch(prior[0],prior[2],Passage);
while(river.State==SearchState.Searching)river.Step();
Check(river.Fine&&river.State==SearchState.Found,"Initial course retries a channel missed by the regional grid");
Check(river.Route.Zip(river.Route.Skip(1),Passage).All(v=>v),"Fine initial course checks every edge through the river bends");
var open=new AdaptiveRouteSearch(new(0,0),new(48,0),(a,b)=>true);while(open.State==SearchState.Searching)open.Step();
Check(!open.Fine&&open.State==SearchState.Found,"Open water retains the cheaper regional search");
var closed=new AdaptiveRouteSearch(new(0,0),new(48,0),(a,b)=>false);while(closed.State==SearchState.Searching)closed.Step();
Check(closed.Fine&&closed.State==SearchState.NoRoute&&closed.Route.Count==0,"Fine fallback cannot create a passage through blocked water");

var berthPoint=new Point(0,0);var forward=new Point(0,1);
bool Straight(Point a,Point b)=>a.Y>=-30&&b.Y>=-30;
Check(!Straight(new(0,-55),new(0,-35)),"Fixed 55 metre lead-in reproduces riverbank rejection");
Check(DockApproach.TrySelect(berthPoint,forward,10,Straight,Straight,out var entry,out var lead),"Short ship fits a checked final corridor before the river bend");
Check(Straight(lead,entry)&&Straight(entry,berthPoint)&&entry.Y<=-12.5&&entry.Y-lead.Y>=7.5,"Adaptive approach preserves hull-sized alignment and final-heading segments");
Check(DockApproach.TrySelect(berthPoint,forward,10,(a,b)=>true,(a,b)=>true,out entry,out lead)&&entry.Y==-35&&lead.Y==-55,"Open dock keeps its original approach length");
Check(!DockApproach.TrySelect(berthPoint,forward,30,Straight,Straight,out _,out _),"Large hull cannot borrow the short ship's cramped approach");
Check(!DockApproach.TrySelect(berthPoint,forward,10,(a,b)=>false,(a,b)=>true,out _,out _),"Blocked final berth remains rejected");
Check(!DockApproach.TrySelect(berthPoint,forward,10,(a,b)=>true,(a,b)=>false,out _,out _),"Clear berth without alignment space remains rejected");
Check(DockApproach.TrySelect(new(100,50),new(1,0),10,(a,b)=>a.X>=70,(a,b)=>a.X>=70,out entry,out lead)&&entry.Y==50&&lead.X<entry.X&&entry.X<100,"Corridor shortening preserves a rotated and translated berth heading");

Physics.Nearby=Array.Empty<Collider>();Physics.Cast=Array.Empty<RaycastHit>();
var dock=new Berth{position=new(0,30,0),Approach=new(0,30,-35)};
Check(chart.ValidateArrival(dock,new(0,30,-15),out _,false),"Chosen approach is revalidated with live hull clearance before docking");
ZoneSystem.instance.Loaded=p=>p.z>=-25;
Check(chart.ValidateArrival(dock,new(0,30,-15),out _,false)&&!chart.ValidateArrival(dock,out _,false),"Arrival revalidates the selected corridor rather than the obsolete 35 metre point");
ZoneSystem.instance.Loaded=p=>false;
Check(!chart.ValidateArrival(dock,new(0,30,-15),out _,false),"Adaptive arrival still waits for loaded terrain");
ZoneSystem.instance.Loaded=p=>true;Heightmap.Ground=29.5f;
Check(!chart.ValidateArrival(dock,new(0,30,-15),out _,false),"Adaptive arrival still rejects insufficient water depth");Heightmap.Ground=25;
Physics.Nearby=new[]{Part<Ship>()};
Check(!chart.ValidateArrival(dock,new(0,30,-15),out _,true),"Adaptive arrival never ignores another boat even with relaxed docks");
Console.WriteLine($"PASS: {checks} production navigation and local-detour checks (host physics doubles).");
