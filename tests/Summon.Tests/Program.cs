using System.Reflection;
using Helmsman;
using UnityEngine;
int checks=0;
void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
void Advance(SummonRequest request)
{
 typeof(SummonRequest).GetMethod("Advance",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(request,null);
 foreach(var routine in request.Routines.ToArray())if(!routine.MoveNext())request.Routines.Remove(routine);
}
var ship=new Ship();ship.transform.position=new Vector3(1000,0,0);ship.gameObject.Components[typeof(Ship)]=ship;ship.gameObject.AddComponent<ZNetView>();
ZNetScene.instance.Boat=ship.gameObject;ZDOMan.instance.Ship.Position=ship.transform.position;
UnattendedShipPhysics.Installed=true;
var record=new ShipRecord{Loaded=ship};
Plugin.LocalSession=false;Check(!SummonRequest.BeginShoreline(record,out _)&&!Plugin.Instance.Summon,"No world session cannot create a summon");Plugin.LocalSession=true;
ship.Occupied=true;Check(!SummonRequest.BeginShoreline(record,out _),"Occupied ships are rejected before loading terrain");ship.Occupied=false;
Player.m_localPlayer.transform.position=new Vector3(10,0,20);
Check(SummonRequest.BeginShoreline(record,out _),"Shoreline request starts");var request=Plugin.Instance.Summon;
Check(!SummonRequest.BeginShoreline(record,out _),"Second request cannot replace an active summon");
Check(request.Loading && request.SimulationCenter.x==10,"Shore search pins the original calling area");
Player.m_localPlayer.transform.position=new Vector3(5000,0,-5000);
Advance(request);
Check(Plugin.Instance.Summon==request && ShorelineArrival.LastOrigin.x==10 && ShorelineArrival.LastOrigin.z==20,"Leaving before the first shoreline check preserves the call location");
Check(ZoneSystem.instance.Poked.TrueForAll(p=>Math.Abs(p.x)<=2&&Math.Abs(p.y)<=2),"Shore search loads the original area, not the teleported player's area");
Advance(request);Advance(request);
Check(!request.Destroyed && !request.Loading && request.SimulationCenter.x==1000,"Accepted landing switches simulation toward the requested ship");
Check(ShorelineArrival.Attempts==3,"Shoreline search still checks clearance incrementally");
GullGuide.Last.transform.position=ship.transform.position;
Advance(request);
Check(Plugin.Instance.Voyage && Voyage.Arrival.Temporary,"Empty ship begins sailing after the guide arrives");
Check(Voyage.Arrival.MarkerPosition.x==10 && Voyage.Arrival.MarkerPosition.z==20,"Sailing retains the original shoreline reference");
var landing=Voyage.Arrival.Berth.position;
Player.m_localPlayer.transform.position=new Vector3(-5000,0,5000);ship.transform.position=new Vector3(900,0,0);
Advance(request);
Check(!request.Destroyed && Voyage.Arrival.Berth.position.x==landing.x && Voyage.Arrival.Berth.position.z==landing.z,"Moving again while sailing neither cancels nor retargets the request");
Check(request.SimulationCenter.x==900,"Remote simulation follows the ship during its journey");
request.Cancel("Player cancelled");
Check(request.Destroyed && !Plugin.Instance.Summon && !request.Loading,"Cancellation clears the active job and remote loading");
Player.m_localPlayer.transform.position=new Vector3(10,0,20);ShorelineArrival.Attempts=0;
Check(SummonRequest.BeginShoreline(record,out _),"New order can follow cancellation");request=Plugin.Instance.Summon;
ZNet.instance.World++;Advance(request);
Check(request.Destroyed && !request.Loading,"Changing worlds stops the request even during shoreline search");
Console.WriteLine($"PASS: {checks} production summon lifetime checks (host terrain/ship doubles; no Unity physics simulation).");
