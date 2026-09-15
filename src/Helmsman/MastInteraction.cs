using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

internal static class MastInteraction
{
    internal const float HoldSeconds=.6f;
    private static bool usingHoldFast, called;
    private static Chair? pendingChair;
    private static Player? pendingPlayer;
    private static float pressedAt;
    internal static bool Pending=>pendingChair || pendingPlayer;
    internal static bool IsMast(Chair chair)=>chair && chair.m_inShip && ShipRules.IsMast(chair.m_attachAnimation) &&
        ShipProfile.Supports(chair.GetComponentInParent<Ship>());
    internal static bool Intercept(Chair chair,Humanoid user,bool hold,bool alt,ref bool result)
    {
        if(usingHoldFast || user!=Player.m_localPlayer)return true;
        if(alt || !IsMast(chair)){Cancel();return true;}
        // Delay the ordinary action until release so a long press never attaches the player first.
        result=false;
        if(!hold && !Pending)
        {
            pendingChair=chair;pendingPlayer=Player.m_localPlayer;
            pressedAt=Time.unscaledTime;called=false;
        }
        return false;
    }
    internal static void Tick(float now,bool pressed,Player? player,Chair? hovered,bool acceptsInput)
    {
        if(!Pending)return;
        var chair=pendingChair;
        if(!acceptsInput || !player || player!=pendingPlayer || player.IsDead() || !IsMast(chair!) ||
            hovered!=chair || !chair!.m_attachPoint ||
            Vector3.Distance(player.transform.position,chair.m_attachPoint.position)>=chair.m_useDistance)
        {Cancel();return;}
        bool longPress=now-pressedAt>=HoldSeconds;
        if(!pressed)
        {
            bool shouldCall=!called && longPress;
            bool shouldAttach=!called && !longPress;
            Cancel();
            if(shouldCall)GullCall.Call(chair.GetComponentInParent<Ship>());
            else if(shouldAttach)HoldFast(chair);
            return;
        }
        if(longPress && !called)
        {
            called=true;
            GullCall.Call(chair.GetComponentInParent<Ship>());
        }
    }
    internal static void Cancel(){pendingChair=null;pendingPlayer=null;called=false;}
    internal static void HoldFast(Chair chair)
    {
        if(!chair || !Player.m_localPlayer)return;
        // Vanilla retains its range, occupancy, encumbrance and attachment cooldown checks.
        usingHoldFast=true;
        try {chair.Interact(Player.m_localPlayer,false,false);}
        finally {usingHoldFast=false;}
    }
}
