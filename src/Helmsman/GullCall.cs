using System.Linq;
using UnityEngine;

namespace Helmsman;

// A visit owns the gull only until the player chooses a voyage or dismisses him.
public sealed class GullCall : MonoBehaviour
{
    internal Ship Ship=null!;
    private Player requester=null!;
    private GullGuide? guide;
    private bool finished, announced;
    internal bool Ready=>!finished && guide && guide.ReadyOn(Ship);
    internal string Status=>Ready ? "The gull has landed. Speak to him to choose a dock." : "The gull is flying aboard.";

    internal static void Call(Ship ship)
    {
        if(!Plugin.Solo){Plugin.Message("Calling the gull currently supports solo worlds.");return;}
        if(!ShipProfile.Supports(ship) || !ship.IsOwner() || Player.m_localPlayer.IsDead() || !ship.IsPlayerInBoat(Player.m_localPlayer))
        {Plugin.Message("Board a locally owned supported ship to call the gull.");return;}
        var voyage=Plugin.Instance.Voyage;
        if(voyage)
        {
            if(voyage.Ship!=ship || voyage.Unattended){Plugin.Message("Finish the current voyage first.");return;}
            if(voyage.GullReady)Plugin.Instance.UI.OpenVoyage();
            else Plugin.Message("Wait for the gull to land.");
            return;
        }
        if(Plugin.Instance.Summon){Plugin.Message("Finish or cancel the ship summon first.");return;}
        var existing=Plugin.Instance.CalledGull;
        if(existing)
        {
            if(existing.Ship!=ship){Plugin.Message("The gull is waiting on your other ship.");return;}
            if(existing.Ready)Plugin.Instance.UI.OpenCalledGull(existing);
            else Plugin.Message(existing.Status);
            return;
        }
        var call=ship.gameObject.AddComponent<GullCall>();
        call.Ship=ship;call.requester=Player.m_localPlayer;Plugin.Instance.CalledGull=call;
        var home=FindObjectsByType<DockMarker>(FindObjectsSortMode.None)
            .Where(d=>d.Ready && !GullGuide.Traveller(d.Id) && Vector3.Distance(d.transform.position,ship.transform.position)<35)
            .OrderBy(d=>Vector3.Distance(d.transform.position,ship.transform.position)).FirstOrDefault();
        try
        {
            call.guide=home ? home.CallGuide(ship) : GullGuide.Create(ship.transform.position+Vector3.up*12-ship.transform.forward*10,null,null);
            if(!home)call.guide.Visit(ship);
            Plugin.Message(call.Status);
        }
        catch(System.Exception error){Plugin.Instance.Error(error);call.Dismiss("The gull could not reach the ship.");}
    }

    internal bool Sail(DockRecord destination,out string reason)
    {
        if(!Ready){reason="Wait for the gull to land.";return false;}
        if(!Voyage.BeginAboard(Ship,destination,guide!,out reason))return false;
        // The voyage now owns exactly the same actor. OnDestroy must not dismiss him.
        guide=null;finished=true;
        if(Plugin.Instance.CalledGull==this)Plugin.Instance.CalledGull=null;
        Destroy(this);return true;
    }
    private void Update()
    {
        if(finished)return;
        if(!Plugin.Solo || !Ship || !Ship.IsOwner() || !requester || requester.IsDead() ||
            !Ship.IsPlayerInBoat(requester) || !guide)
        {Dismiss("Gull visit ended.");return;}
        if(Ready && !announced){announced=true;Plugin.Message(Status);}
    }
    internal void Dismiss(string reason)
    {
        if(finished)return;finished=true;
        if(guide){guide.FlyAway();guide=null;}
        if(Plugin.Instance.CalledGull==this)Plugin.Instance.CalledGull=null;
        Plugin.Message(reason);Destroy(this);
    }
    private void OnDestroy(){if(!finished)Dismiss("Gull visit ended.");}
}
