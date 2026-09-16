using System.Reflection;
using Helmsman;
using UnityEngine;
int count=0;
void Check(bool ok,string name){count++;if(!ok)throw new Exception(name);}
var flags=BindingFlags.NonPublic|BindingFlags.Instance;
NetworkNavigation net=null!;ZDO ship=null!;
void Call(string method,params object[] args)=>typeof(NetworkNavigation).GetMethod(method,flags)!.Invoke(net,args);
void Reset()
{
 Time.unscaledTime=0;Player.m_localPlayer=null;ZNet.instance=new();ZDOMan.instance=new();ZNetScene.instance=new();ZoneSystem.instance=new();ZRoutedRpc.instance=new();Plugin.Instance=new();
 ZNet.instance.Peers.AddRange(new[]{new ZNetPeer{m_uid=1,Position=new(0,0,0)},new ZNetPeer{m_uid=2,Position=new(5000,0,0)}});
 ship=new(){m_uid=new(7),Position=new(1000,0,0),Owner=999};ZDOMan.instance.Data[ship.m_uid]=ship;
 net=new();Call("Awake");Call("Update");
}
void Request(long peer,Vector3 center)=>Call("Request",peer,ship.m_uid,center);
string LastReason()
{
 var message=ZRoutedRpc.instance.Sent.Last();var pkg=(ZPackage)message.Values[0];pkg.ReadZDOID();pkg.ReadVector3();return pkg.ReadString();
}
Reset();Request(1,ship.Position);
Check(ship.Owner==1,"Server grants a named empty ship to requesting peer");
Check(NetworkNavigation.InArea(ship.Position,1),"Reservation protects ownership in the distant ship area");
Request(2,ship.Position);Check(ship.Owner==1&&LastReason().Contains("already called"),"Concurrent caller cannot steal a reserved ship");
Time.unscaledTime=1;Call("Update");Check(ZRoutedRpc.instance.Sent.Last().Method=="HelmsmanRemoteReply","Loaded server area returns a ZDO manifest");
var approval=(ZPackage)ZRoutedRpc.instance.Sent.Last().Values[0];
ZNet.instance.Server=false;Player.m_localPlayer=new();Plugin.Instance.Summon=new(){ShipId=ship.m_uid,SimulationCenter=ship.Position};Call("Update");Call("Reply",999L,approval);
Check(NetworkNavigation.Ready(ship.Position),"Client begins only after the server reply and every manifest object are present");
ZDOMan.instance.Data.Remove(ship.m_uid);Check(!NetworkNavigation.Ready(ship.Position),"Missing ship data holds the client loading barrier");ZDOMan.instance.Data[ship.m_uid]=ship;
Time.unscaledTime+=7;Check(!NetworkNavigation.Ready(ship.Position),"Stale server readiness cannot keep a remote boat sailing");
Reset();Request(1,new(10,0,10));ZNet.instance.Peers[0].Position=new(-5000,0,-5000);Time.unscaledTime=1;Request(1,new(10,0,10));
Check(NetworkNavigation.InArea(new(10,0,10),1),"Moving after calling preserves the accepted shoreline instead of imposing a player distance limit");
Time.unscaledTime=10;Call("Update");Check(!NetworkNavigation.InArea(new(10,0,10),1),"Heartbeat expiry releases the remote reservation");
Reset();Request(1,ship.Position);ZNet.instance.Peers.RemoveAt(0);Call("Update");Check(!NetworkNavigation.InArea(ship.Position,1),"Disconnected peer loses its extra loading area");
Reset();ship.Named=false;Request(1,ship.Position);Check(ship.Owner==999&&LastReason().Contains("no longer available"),"Unnamed ships cannot be requested by a forged RPC");
Reset();Request(1,new(float.NaN,0,0));Check(ship.Owner==999,"Non-finite centers cannot enter zone calculations");
Reset();Request(1,new(99999,0,99999));Check(ship.Owner==999,"Request cannot load an unrelated remote region");
Reset();ZNetScene.instance.Live=new(){Ship=new(){Occupied=true}};Request(1,ship.Position);Check(ship.Owner==999,"Loaded occupied ship cannot be taken for a summon");
Reset();ZNet.instance.Peers[1].Position=ship.Position;Request(1,ship.Position);Check(ship.Owner==999,"Nearby remote player protects an unloaded occupied boat");
Reset();Request(1,ship.Position);ship.Owner=2;Time.unscaledTime=1;Request(1,ship.Position);Check(ship.Owner==2&&LastReason().Contains("taken control"),"Manual ownership takeover is never stolen back by a heartbeat");
Reset();Request(1,ship.Position);Call("Request",1L,ZDOID.None,Vector3.zero);Check(!NetworkNavigation.InArea(ship.Position,1),"Explicit cancellation releases the reservation");
Reset();ZoneSystem.instance.Loaded=false;Request(1,ship.Position);Call("Update");Check(ZRoutedRpc.instance.Sent.Count==0,"Server does not approve an ungenerated or still-loading shore");
Console.WriteLine($"PASS: {count} production server reservation, remote loading and takeover checks (host network doubles).");
