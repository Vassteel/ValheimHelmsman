using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Helmsman;

// Presentation follows synchronized native speed settings on every observing client.
public sealed class ShipOarAnimation : MonoBehaviour
{
    private Ship ship=null!;
    private Animator[] oars=Array.Empty<Animator>();
    private bool rowing;
    private IEnumerator Start()
    {
        if(ZNet.instance&&ZNet.instance.IsDedicated())yield break;
        if(GetComponent<FinalShipPresentation>())yield break;
        ship=GetComponent<Ship>();
        oars=GetComponentsInChildren<Animator>(true).Where(a=>a.name=="Remos"||a.transform.parent&&a.transform.parent.name=="Remos").ToArray();
        foreach(var animator in oars){animator.Rebind();animator.Update(0);animator.enabled=false;}
        yield break;
    }
    private void Update()
    {
        if(!ship||oars.Length==0)return;
        var speed=ship.GetSpeedSetting();bool next=speed==Ship.Speed.Slow||speed==Ship.Speed.Back;
        if(next==rowing)return;rowing=next;
        foreach(var animator in oars)
        {
            if(!animator)continue;
            animator.enabled=true;
            if(!rowing){animator.Rebind();animator.Update(0);animator.enabled=false;}
        }
    }
}

// Rowing-only hulls use the game's forward rowing gear. Higher sail settings
// would otherwise leave these boats motionless because they have no sail force.
[HarmonyLib.HarmonyPatch(typeof(Ship),nameof(Ship.CustomFixedUpdate))]
internal static class CanoeRowingGear
{
    private static void Prefix(Ship __instance,ref Ship.Speed ___m_speed)
    {
        if(!__instance.GetComponent<ShipOarAnimation>()||ShipProfile.CanSail(__instance))return;
        if(___m_speed==Ship.Speed.Half||___m_speed==Ship.Speed.Full)___m_speed=Ship.Speed.Slow;
    }
}
