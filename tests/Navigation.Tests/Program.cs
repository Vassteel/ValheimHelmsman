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
Console.WriteLine($"PASS: {checks} production navigation and local-detour checks (host physics doubles).");
