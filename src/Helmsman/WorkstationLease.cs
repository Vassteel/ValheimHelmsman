using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Helmsman;

// Follow the native container ownership handoff, but reserve the workshop first.
// Two players cannot both pay for the same free construction slot.
public sealed class WorkstationLease : MonoBehaviour
{
    private static readonly int PeerKey="helmsman_work_peer_v1".GetStableHashCode();
    private static readonly int UntilKey="helmsman_work_until_v1".GetStableHashCode();
    private ZNetView view=null!;
    private Func<string>? pending;
    private float deadline;
    private long replyPeer;
    private bool initialized;
    private long Now=>ZNet.instance.GetTime().Ticks;
    internal bool Held=>view&&view.IsValid()&&view.IsOwner()&&view.GetZDO().GetLong(PeerKey)==ZNet.GetUID()&&view.GetZDO().GetLong(UntilKey)>Now;
    private IEnumerator Start()
    {
        view=GetComponent<ZNetView>();
        while(view&&!view.IsValid())yield return null;
        if(!view)yield break;
        view.Register<long>("HelmsmanWorkRequest",Request);
        view.Register<bool>("HelmsmanWorkReply",Reply);
        initialized=true;
    }
    internal Func<Player,bool>? AccessRange;
    private bool Near(Player player)=>player&&!player.IsDead()&&(AccessRange!=null?AccessRange(player):(player.transform.position-transform.position).sqrMagnitude<64);
    internal string Run(Func<string> action)
    {
        if(!initialized||!Near(Player.m_localPlayer)||!PrivateArea.CheckAccess(transform.position,0,false,true))return "Stand beside an accessible workshop.";
        if(pending!=null)return "Waiting for workshop access.";
        if(Held)return action();
        if(view.IsOwner())
        {
            if(!Reserve(ZNet.GetUID()))return "Another Viking is using this workshop.";
            return action();
        }
        pending=action;deadline=Time.unscaledTime+10;replyPeer=view.GetZDO().GetOwner();
        view.InvokeRPC("HelmsmanWorkRequest",Player.m_localPlayer.GetPlayerID());
        return "Checking workshop access…";
    }
    private bool Reserve(long peer)
    {
        var data=view.GetZDO();long holder=data.GetLong(PeerKey);
        if(holder!=0&&holder!=peer&&data.GetLong(UntilKey)>Now)return false;
        data.Set(PeerKey,peer);data.Set(UntilKey,Now+TimeSpan.FromSeconds(10).Ticks);return true;
    }
    private void Request(long sender,long playerId)
    {
        if(!view||!view.IsValid()||!view.IsOwner())return;
        var player=Player.GetAllPlayers().FirstOrDefault(p=>p&&p.GetPlayerID()==playerId);
        var playerView=player?player.GetComponent<ZNetView>():null;
        if(!Near(player!)||!playerView||!playerView.IsValid()||playerView.GetZDO().GetOwner()!=sender||!Reserve(sender))
        {view.InvokeRPC(sender,"HelmsmanWorkReply",false);return;}
        view.GetZDO().SetOwner(sender);
        ZDOMan.instance.ForceSendZDO(sender,view.GetZDO().m_uid);
        view.InvokeRPC(sender,"HelmsmanWorkReply",true);
    }
    private void Reply(long sender,bool granted)
    {
        if(pending==null||sender!=replyPeer)return;
        if(!granted){pending=null;Plugin.Message("Another Viking is using this workshop. Try again shortly.");}
        // The reply and ZDO may arrive separately. Update waits for the actual lease.
    }
    private void Update()
    {
        if(pending==null)return;
        if(!Near(Player.m_localPlayer)||!PrivateArea.CheckAccess(transform.position,0,false,true)||Time.unscaledTime>deadline)
        {pending=null;Plugin.Message("Workshop request expired; no materials were taken.");return;}
        if(!Held)return;
        var action=pending;pending=null;
        try{Plugin.Message(action());}catch(Exception error){Plugin.Instance.Error(error);Plugin.Message("Workshop action failed; check its saved status.");}
    }
}
