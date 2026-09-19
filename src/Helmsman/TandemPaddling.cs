using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace Helmsman;

internal static class TandemPaddling
{
    internal const string InputKey="helmsman_paddle_input",TimeKey="helmsman_paddle_input_time";
    internal static int Input(ZDO data)
    {
        long now=ZNet.instance?ZNet.instance.GetTime().Ticks:0;
        long stamp=data.GetLong(TimeKey);
        return stamp>0&&now>=stamp&&now-stamp<TimeSpan.TicksPerSecond*2?Math.Sign(data.GetInt(InputKey)):0;
    }
    internal static bool Front(Vector3 seat)=>seat.z>.5f;
    internal static void Clear(ZDO data){if(data.GetInt(InputKey)!=0)data.Set(InputKey,0);}
}

// The forward chair remains a passenger seat: movement supplies paddle effort,
// while jump and interaction retain the normal exit behavior. No steering writes.
[HarmonyPatch(typeof(Player),nameof(Player.SetControls))]
internal static class TandemPaddleInput
{
    private static void Prefix(Player __instance,ref Vector3 movedir,bool jump)
    {
        var view=__instance.GetComponent<ZNetView>();if(!view||!view.IsValid()||!view.IsOwner())return;
        var data=view.GetZDO();var anchor=__instance.GetAttachPoint();var rig=anchor?anchor.GetComponentInParent<PaddleCraftRig>():null;
        if(!rig||!TandemPaddling.Front(rig.transform.InverseTransformPoint(anchor!.position))||jump||__instance.IsDead())
        {TandemPaddling.Clear(data);return;}
        int input=movedir.z>.1f?1:movedir.z<-.1f?-1:0;
        long now=ZNet.instance.GetTime().Ticks;
        if(input!=data.GetInt(TandemPaddling.InputKey)||input!=0&&now-data.GetLong(TandemPaddling.TimeKey)>TimeSpan.TicksPerSecond/2)
        {data.Set(TandemPaddling.InputKey,input);data.Set(TandemPaddling.TimeKey,now);}
        movedir=Vector3.zero; // Do not leave the chair when pressing W/S or A/D.
    }
}

[HarmonyPatch(typeof(Ship),nameof(Ship.CustomFixedUpdate))]
internal static class TandemPaddleThrust
{
    private static void Postfix(Ship __instance,float fixedDeltaTime,List<Player> ___m_players,ZNetView ___m_nview,Rigidbody ___m_body,ref WaterVolume ___m_previousCenter)
    {
        if(!___m_nview||!___m_nview.IsValid()||!___m_nview.IsOwner()||!__instance.GetComponent<PaddleCraftRig>())return;
        var id=___m_nview.GetZDO().m_uid;
        foreach(var player in ___m_players)
        {
            if(!player||player.IsDead())continue;
            var view=player.GetComponent<ZNetView>();if(!view||!view.IsValid())continue;
            var data=view.GetZDO();var seat=data.GetVec3("helmsman_paddle_seat",Vector3.zero);
            if(data.GetZDOID("helmsman_paddle_ship")!=id||!TandemPaddling.Front(seat))continue;
            if((player.transform.position-__instance.transform.TransformPoint(seat)).sqrMagnitude>4)continue;
            int effort=TandemPaddling.Input(data);if(effort==0)continue;
            float water=Floating.GetWaterLevel(___m_body.worldCenterOfMass,ref ___m_previousCenter);
            if(___m_body.worldCenterOfMass.y-water-__instance.m_waterLevelOffset>__instance.m_disableLevel)return;
            // A second paddler adds 60% thrust (~26% steady speed under quadratic
            // drag). Applying at the centre avoids giving the front seat steering.
            ___m_body.AddForce(__instance.transform.forward*(effort*__instance.m_backwardForce*.60f*___m_body.mass*fixedDeltaTime),ForceMode.Impulse);
            break; // One forward cockpit, never stack stale/duplicate occupants.
        }
    }
}
