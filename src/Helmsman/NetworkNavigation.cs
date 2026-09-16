using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Helmsman;

// Voyages run on the connected requesting peer, just as native boat physics does.
// The server reserves distant ships and streams one additional area per requester.
// No voyage survives a disconnect, and the player's normal loading area is retained.
public sealed class NetworkNavigation : MonoBehaviour
{
    private sealed class Lease
    {
        internal ZDOID Ship;
        internal Vector3 Center,CallingShore;
        internal float Until,NextReply;
    }
    private readonly Dictionary<long,Lease> leases=new();
    private readonly Dictionary<long,float> requests=new();
    private readonly List<ZDO> objects=new();
    private readonly HashSet<ZDOID> expected=new();
    private static readonly MethodInfo Poke=AccessTools.Method(typeof(ZoneSystem),"PokeLocalZone");
    internal static NetworkNavigation Instance=null!;
    private ZRoutedRpc? registered;
    private float nextRequest,receivedAt;
    private Vector2s receivedZone;
    private ZDOID requestedShip;
    private long world;
    private bool granted;
    internal static bool Ready(Vector3 center)=>ZNet.instance&&ZNet.instance.IsServer()||Instance&&Instance.granted&&
        Time.unscaledTime-Instance.receivedAt<6&&ZoneSystem.GetZone(center)==Instance.receivedZone&&
        Instance.expected.All(id=>ZDOMan.instance.GetZDO(id)!=null);
    private void Awake(){Instance=this;StartCoroutine(Catalog());}
    private void Register()
    {
        if(registered==ZRoutedRpc.instance)return;
        registered=ZRoutedRpc.instance;if(registered==null)return;
        leases.Clear();requests.Clear();expected.Clear();granted=false;requestedShip=ZDOID.None;
        registered.Register<ZDOID,Vector3>("HelmsmanRemoteArea",Request);
        registered.Register<ZPackage>("HelmsmanRemoteReply",Reply);
    }
    private void Update()
    {
        if(!ZNet.instance||ZDOMan.instance==null||!ZoneSystem.instance)return;
        Register();
        long current=ZNet.instance.GetWorldUID();
        if(world!=current){world=current;leases.Clear();requests.Clear();expected.Clear();granted=false;requestedShip=ZDOID.None;}
        if(ZNet.IsSinglePlayer)return;
        var summon=Plugin.Instance.Summon;
        if(!ZNet.instance.IsServer()&&Player.m_localPlayer&&Time.unscaledTime>=nextRequest)
        {
            nextRequest=Time.unscaledTime+1;
            if(summon)
            {
                requestedShip=summon.ShipId;
                registered!.InvokeRoutedRPC((ZNet.instance.GetServerPeer()?.m_uid??ZNet.GetUID()),"HelmsmanRemoteArea",requestedShip,summon.SimulationCenter);
            }
            else if(requestedShip!=ZDOID.None)
            {
                registered!.InvokeRoutedRPC((ZNet.instance.GetServerPeer()?.m_uid??ZNet.GetUID()),"HelmsmanRemoteArea",ZDOID.None,Vector3.zero);
                requestedShip=ZDOID.None;granted=false;expected.Clear();
            }
        }
        if(!ZNet.instance.IsServer())return;
        foreach(var pair in leases.ToArray())
        {
            var lease=pair.Value;var ship=ZDOMan.instance.GetZDO(lease.Ship);
            if(Time.unscaledTime>lease.Until||ZNet.instance.GetPeer(pair.Key)==null||ship==null)
            {leases.Remove(pair.Key);continue;}
            var zone=ZoneSystem.GetZone(lease.Center);bool created=false;
            for(int x=-2;x<=2&&!created;x++)for(int z=-2;z<=2;z++)
                if((bool)Poke.Invoke(ZoneSystem.instance,new object[]{new Vector2s(zone.x+x,zone.y+z)})){created=true;break;}
            if(Time.unscaledTime<lease.NextReply)continue;
            bool loaded=true;
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
                if(!ZoneSystem.instance.IsZoneLoaded(new Vector2s(zone.x+x,zone.y+z)))loaded=false;
            if(!loaded)continue;
            lease.NextReply=Time.unscaledTime+1;
            objects.Clear();ZDOMan.instance.FindSectorObjects(zone,new SimulationDistance(2,0,true),objects);
            var reply=new ZPackage();reply.Write(lease.Ship);reply.Write(lease.Center);reply.Write("");
            var usable=objects.Where(o=>ZNetScene.instance.GetPrefab(o.GetPrefab())).ToArray();
            reply.Write(usable.Length);
            foreach(var data in usable){ZDOMan.instance.ForceSendZDO(pair.Key,data.m_uid);reply.Write(data.m_uid);}
            registered!.InvokeRoutedRPC(pair.Key,"HelmsmanRemoteReply",reply);
        }
    }
    private void Reject(long sender,ZDOID ship,string reason)
    {
        var response=new ZPackage();response.Write(ship);response.Write(Vector3.zero);response.Write(reason);response.Write(0);
        registered!.InvokeRoutedRPC(sender,"HelmsmanRemoteReply",response);
    }
    private void Request(long sender,ZDOID id,Vector3 center)
    {
        if(!ZNet.instance.IsServer()||ZNet.IsSinglePlayer)return;
        var peer=ZNet.instance.GetPeer(sender);if(peer==null||!peer.IsReady())return;
        if(id==ZDOID.None){leases.Remove(sender);return;}
        if(requests.TryGetValue(sender,out var at)&&Time.unscaledTime-at<.5f)return;requests[sender]=Time.unscaledTime;
        var ship=ZDOMan.instance.GetZDO(id);
        if(ship==null||ShipDirectory.SavedName(ship).Length==0)
        {Reject(sender,id,"The named ship is no longer available.");return;}
        var prefab=ZNetScene.instance.GetPrefab(ship.GetPrefab());
        if(!prefab||!ShipProfile.Supports(prefab.GetComponent<Ship>())){Reject(sender,id,"This vessel cannot be sailed by the gull.");return;}
        if(!Finite(center)||(center-ship.GetPosition()).sqrMagnitude>128*128&&(center-peer.GetRefPos()).sqrMagnitude>128*128&&
            !(leases.TryGetValue(sender,out var previous)&&previous.Ship==id&&(center-previous.CallingShore).sqrMagnitude<1))
        {Reject(sender,id,"The requested loading area is no longer beside the ship or calling shore.");return;}
        if(leases.Any(p=>p.Key!=sender&&p.Value.Ship==id&&p.Value.Until>Time.unscaledTime))
        {Reject(sender,id,"Another Viking has already called this ship.");return;}
        // Inspect both live colliders and remote player ZDOs. The conservative margin
        // also prevents taking an unloaded boat out from beneath another player.
        var live=ZNetScene.instance.FindInstance(id)?.GetComponent<Ship>();
        float radius=ShipProfile.For(prefab.GetComponent<Ship>()).Length*.6f+3;
        if(live&&(live.HasPlayerOnboard()||live.m_shipControlls.HaveValidUser())||
            ZNet.instance.GetPeers().Any(p=>p.m_uid!=sender&&(p.GetRefPos()-ship.GetPosition()).sqrMagnitude<radius*radius))
        {leases.Remove(sender);Reject(sender,id,"Ship call stopped: another Viking is aboard or beside the boat.");return;}
        if(!leases.TryGetValue(sender,out var lease)||lease.Ship!=id)
        {
            lease=new Lease {Ship=id,CallingShore=center};leases[sender]=lease;
            ship.SetOwner(sender);ZDOMan.instance.ForceSendZDO(sender,id);
        }
        // Once another player takes ownership, never take it back for autopilot.
        if(ship.GetOwner()!=sender){leases.Remove(sender);Reject(sender,id,"Ship call stopped: someone else has taken control.");return;}
        lease.Center=center;lease.Until=Time.unscaledTime+8;
    }
    private static bool Finite(Vector3 value)=>!(float.IsNaN(value.x)||float.IsNaN(value.y)||float.IsNaN(value.z)||
        float.IsInfinity(value.x)||float.IsInfinity(value.y)||float.IsInfinity(value.z));
    private void Reply(long sender,ZPackage reply)
    {
        if(sender!=(ZNet.instance.GetServerPeer()?.m_uid??ZNet.GetUID()))return;
        var id=reply.ReadZDOID();var center=reply.ReadVector3();string reason=reply.ReadString();int count=reply.ReadInt();
        if(id!=requestedShip||!Plugin.Instance.Summon)return;
        if(reason.Length>0){granted=false;Plugin.Instance.Summon.Cancel(reason);return;}
        if(count<0||count>100000){granted=false;return;}
        expected.Clear();for(int i=0;i<count;i++)expected.Add(reply.ReadZDOID());
        receivedZone=ZoneSystem.GetZone(center);receivedAt=Time.unscaledTime;granted=true;
    }
    internal static bool InArea(Vector3 point,long peer)
        =>HostSummonArea(point,peer)||Instance&&Instance.leases.TryGetValue(peer,out var lease)&&lease.Until>Time.unscaledTime&&
            Mathf.Abs(point.x-lease.Center.x)<128&&Mathf.Abs(point.z-lease.Center.z)<128;
    private static bool HostSummonArea(Vector3 point,long peer)
    {
        var summon=Plugin.Instance.Summon;
        return ZNet.instance&&ZNet.instance.IsServer()&&peer==ZNet.GetUID()&&summon&&summon.Loading&&
            Mathf.Abs(point.x-summon.SimulationCenter.x)<128&&Mathf.Abs(point.z-summon.SimulationCenter.z)<128;
    }
    private IEnumerator Catalog()
    {
        while(true)
        {
            if(ZNet.instance&&ZNet.instance.IsServer()&&!ZNet.IsSinglePlayer&&ZNetScene.instance&&ZDOMan.instance!=null)
            {
                long catalogWorld=ZNet.instance.GetWorldUID();var manager=ZDOMan.instance;
                var names=ZNetScene.instance.GetPrefabNames().Where(n=>ZNetScene.instance.GetPrefab(n).GetComponent<Ship>()).Concat(new[]{Plugin.DockPrefab}).ToArray();
                foreach(string name in names)
                {
                    if(!ZNet.instance||!ZNet.instance.IsServer()||ZDOMan.instance!=manager||ZNet.instance.GetWorldUID()!=catalogWorld)break;
                    var list=new List<ZDO>();int cursor=0;
                    while(ZDOMan.instance==manager&&ZNet.instance&&ZNet.instance.GetWorldUID()==catalogWorld&&!manager.GetAllZDOsWithPrefabIterative(name,list,ref cursor))yield return null;
                    if(ZDOMan.instance!=manager||!ZNet.instance||ZNet.instance.GetWorldUID()!=catalogWorld)break;
                    foreach(var data in list.Where(d=>d.IsValid()&&(name==Plugin.DockPrefab||ShipDirectory.SavedName(d).Length>0)))
                        foreach(var peer in ZNet.instance.GetPeers())if(peer.IsReady())ZDOMan.instance.ForceSendZDO(peer.m_uid,data.m_uid);
                    yield return null;
                }
            }
            yield return new WaitForSeconds(5);
        }
    }
}

[HarmonyPatch(typeof(ZDOMan),"IsInPeerActiveArea")]
internal static class RemoteNavigationOwnership
{
    private static void Postfix(Vector3 point,long uid,ref bool __result)
    {if(!__result)__result=NetworkNavigation.InArea(point,uid);}
}
