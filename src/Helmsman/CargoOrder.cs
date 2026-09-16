using System;
using UnityEngine;

namespace Helmsman;

public sealed class CargoOrder : MonoBehaviour
{
    private Ship ship=null!;
    private GullGuide gull=null!;
    private Player requester=null!;
    private object request=null!;
    private bool ended;
    internal string Status { get; private set; }="";
    internal static bool Start(Ship ship, GullGuide gull, out string reason)
    {
        reason="Speak to the landed gull aboard a stopped boat with Quartermaster in range.";
        if(!Plugin.Solo || !gull || !gull.ReadyOn(ship) || !QuartermasterBridge.Available) return false;
        if(Plugin.Instance.Cargo) { reason="Finish or stop the current cargo request first.";return false; }
        try
        {
            reason=QuartermasterBridge.Check(ship);if(reason.Length>0) return false;
            var token=QuartermasterBridge.BeginUnload(ship);
            var order=ship.gameObject.AddComponent<CargoOrder>();
            order.ship=ship;order.gull=gull;order.requester=Player.m_localPlayer;order.request=token;
            Plugin.Instance.Cargo=order;order.Status="Unloading cargo into base storage…";reason=order.Status;return true;
        }
        catch(Exception ex) { reason=ex.GetBaseException().Message;return false; }
    }
    private void Update()
    {
        if(ended) return;
        if(!Plugin.Solo || !QuartermasterBridge.Available || !ship || !gull || !gull.ReadyOn(ship) ||
            !requester || requester!=Player.m_localPlayer || requester.IsDead() || !ship.IsPlayerInBoat(requester))
        { Stop("Cargo request ended.");return; }
        try
        {
            var invalid=QuartermasterBridge.Check(ship);
            if(invalid.Length>0) { Stop(invalid);return; }
            if(gull.SortingCargo) return;
            if(QuartermasterBridge.Finished(request)) { Stop(QuartermasterBridge.Status(request),true);return; }
            var item=QuartermasterBridge.UnloadNext(request);
            Status=QuartermasterBridge.Status(request);
            if(item!=null) gull.SortCargo(item);
            else if(QuartermasterBridge.Finished(request)) Stop(Status,true);
        }
        catch(Exception ex) { Plugin.Instance.Error(ex);Stop("Cargo handling stopped: "+ex.GetBaseException().Message); }
    }
    internal void Stop(string reason,bool completed=false)
    {
        if(ended) return;ended=true;
        if(request!=null && QuartermasterBridge.Available) QuartermasterBridge.Cancel(request);
        if(gull) gull.EndCargoSort(!completed);
        if(Plugin.Instance && Plugin.Instance.Cargo==this) Plugin.Instance.Cargo=null;
        Status=reason;
        if(completed && gull)gull.Speak(reason,true);
        Plugin.Message(reason);Destroy(this);
    }
    private void OnDestroy() { if(!ended) Stop("Cargo request ended."); }
}
