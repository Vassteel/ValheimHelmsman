using System.Collections;
using System.Reflection;
using Helmsman;
using UnityEngine;
int count=0,charges=0;WorkstationLease lease=null!;ZNetView view=null!;
object Call(string name,params object[] args)=>typeof(WorkstationLease).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(lease,args)!;
void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
string Pay(){charges++;return "Paid once";}
Player NewPlayer(long id,long peer)=>new(){Id=id,View=new(){Data=new(){Owner=peer}}};
void Reset(long owner=42)
{
 Time.unscaledTime=0;ZNet.Peer=42;charges=0;PrivateArea.Allowed=true;Plugin.Messages.Clear();
 Player.m_localPlayer=NewPlayer(10,42);Player.All=new(){Player.m_localPlayer,NewPlayer(20,22)};
 view=new(){Data=new(){Owner=owner}};lease=new(){View=view};((IEnumerator)Call("Start")).MoveNext();
}
Reset();Check(lease.Run(Pay)=="Paid once"&&charges==1&&lease.Held,"Local owner reserves before payment");
Call("Request",22L,20L);Check(view.Data.Owner==42&&charges==1&&!(bool)view.Sent.Last().Args[0],"Second player cannot displace a live reservation");
Time.unscaledTime=11;Call("Request",22L,20L);Check(view.Data.Owner==22,"Expired workshop reservation can pass to another nearby player");
Reset(22);Check(lease.Run(Pay).Contains("Checking")&&charges==0,"Remote request never pays before authority arrives");
Check(lease.Run(Pay).Contains("Waiting")&&charges==0,"Repeated clicks cannot queue duplicate purchases");
Call("Reply",99L,false);Call("Update");Check(charges==0,"Forged reply cannot approve a purchase");
Call("Reply",22L,true);Call("Update");Check(charges==0,"Approval packet alone cannot spend materials");
view.Data.Owner=42;Call("Update");Check(charges==0,"Ownership alone cannot spend without the matching reservation");
view.Data.Set("helmsman_work_peer_v1".GetStableHashCode(),42);
view.Data.Set("helmsman_work_until_v1".GetStableHashCode(),ZNet.instance.GetTime().AddSeconds(10).Ticks);
Call("Update");Call("Update");Check(charges==1,"Ownership plus valid lease executes queued payment exactly once");
Reset(22);lease.Run(Pay);Player.m_localPlayer.transform.position=new(100,0,0);Call("Update");Check(charges==0&&Plugin.Messages.Last().Contains("no materials"),"Walking away cancels without charging");
Reset(22);lease.Run(Pay);Time.unscaledTime=11;Call("Update");Check(charges==0,"Timed-out authority request takes no materials");
Reset();PrivateArea.Allowed=false;Check(lease.Run(Pay).Contains("accessible")&&charges==0,"Ward restriction blocks local payment");
Reset();Call("Request",22L,10L);Check(view.Data.Owner==42,"Remote request cannot impersonate another player");
Console.WriteLine($"PASS: {count} production workshop authority and single-payment checks.");
