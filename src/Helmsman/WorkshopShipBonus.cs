using System.Collections;
using HarmonyLib;
using UnityEngine;
namespace Helmsman;
public sealed class WorkshopShipBonus:MonoBehaviour,IPlaced
{
    internal const string HullKey="helmsman_workshop_hull_v1",SailKey="helmsman_workshop_sail_v1";
    public void OnPlaced()
    {
        var view=GetComponent<ZNetView>();var player=Player.m_localPlayer;
        if(!view||!view.IsValid()||!view.IsOwner()||!player||SlipwayPlacement.Plan(gameObject)==null)return;
        var bench=WorkshopRange.Bench(player,transform.position);if(!bench)return;
        var wear=GetComponent<WearNTear>();if(!wear)return;
        Stamp(view.GetZDO(),WorkshopRange.Upgrade(bench,player,"caulking",transform.position)?1.15f:1,WorkshopRange.Upgrade(bench,player,"rigging",transform.position)?1.1f:1,wear.m_health);
    }
    internal static void Stamp(ZDO data,float hull,float sail,float baseHealth)
    {
        data.Set(HullKey,hull);data.Set(SailKey,sail);
        if(!data.GetBool("helmsman_workshop_initial_health_v1"))
        {
            data.Set("health",baseHealth*hull);
            data.Set("helmsman_workshop_initial_health_v1",true);
        }
    }
    private IEnumerator Start()
    {
        var view=GetComponent<ZNetView>();while(view&&!view.IsValid())yield return null;if(!view)yield break;
        var ship=GetComponent<Ship>();var wear=GetComponent<WearNTear>();var data=view.GetZDO();
        if(wear)wear.m_health*=Mathf.Clamp(data.GetFloat(HullKey,1),1,1.15f);
        if(ship)ship.m_sailForceFactor*=Mathf.Clamp(data.GetFloat(SailKey,1),1,1.1f);
    }
}
[HarmonyPatch(typeof(Ship),"Awake")]
internal static class WorkshopShipBonusAttach
{
    private static void Postfix(Ship __instance){if(!__instance.GetComponent<WorkshopShipBonus>())__instance.gameObject.AddComponent<WorkshopShipBonus>();}
}
