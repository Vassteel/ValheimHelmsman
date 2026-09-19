using System;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;
internal static class SlipwayPlacement
{
    internal static ShipBlueprint? Plan(GameObject go)=>go?ShipConstruction.Blueprints.FirstOrDefault(b=>b.Prefab==go.name.Replace("(Clone)","")):null;
}
[HarmonyPatch(typeof(Player),"TryPlacePiece")]
internal static class SlipwayHammerOrder
{
    private static bool Prefix(Player __instance,Piece piece,bool ___m_noPlacementCost,ref bool __result)
    {
        var plan=SlipwayPlacement.Plan(piece.gameObject);if(plan==null||!plan.UsesSlipway)return true;
        __result=false; // This callback owns payment; vanilla must not charge a second time.
        var slip=Slipway.Find(__instance);
        if(!slip){Plugin.Message(QuartermasterWorkshop.Available?"Place a free slipway and shipwright bench in the same Quartermaster base zone.":"Place a free slipway and stand within 30 m of its shipwright bench.");return false;}
        bool free=___m_noPlacementCost||(ZoneSystem.instance&&ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()));
        try
        {
            var result=slip.Request(__instance,piece,plan,free);
            Plugin.Instance.Record("Slipway placement "+plan.Id+": "+result);Plugin.Message(result);
        }
        catch(Exception error){Plugin.Instance.Error(error);Plugin.Message("Ship order could not be saved. No construction started; check the slipway status.");}
        return false;
    }
}
// Large ship previews are docked to a slipway, so native free-placement physics,
// snappoint scans and collider nearest-point work are unnecessary every frame.
[HarmonyPatch(typeof(Player),"SetupPlacementGhost")]
internal static class SlipwayPreviewSetup
{
    private static void Postfix(GameObject ___m_placementGhost)
    {
        var motion=___m_placementGhost?___m_placementGhost.GetComponent<FleetSailMotion>():null;
        if(motion)motion.FreezePreview();
    }
}
[HarmonyPatch(typeof(Player),"UpdatePlacementGhost")]
internal static class SlipwayHammerPreview
{
    private static GameObject? previous;
    private static ShipBlueprint? plan;
    private static Slipway? selected;
    private static float nextSearch;
    private static Vector3 stage;
    private static readonly System.Reflection.MethodInfo Valid=AccessTools.Method(typeof(Player),"SetPlacementGhostValid");
    private static bool Prefix(Player __instance,GameObject ___m_placementGhost,GameObject ___m_placementMarkerInstance)
    {
        var ghost=___m_placementGhost;
        if(ghost!=previous){previous=ghost;plan=SlipwayPlacement.Plan(ghost);selected=null;nextSearch=0;}
        if(!ghost||plan==null||!plan.UsesSlipway)return true;
        // A newly freed slipway must not immediately show the hammer preview
        // over the just-cancelled construction while its workshop menu is open.
        if(Plugin.Instance.UI.IsOpen){ghost.SetActive(false);return false;}
        if(___m_placementMarkerInstance)___m_placementMarkerInstance.SetActive(false);
        if(Time.time>=nextSearch)
        {
            nextSearch=Time.time+.25f;selected=Slipway.Find(__instance);
            var source=ZNetScene.instance?ZNetScene.instance.GetPrefab(plan.Prefab):null;
            var ship=source?source.GetComponent<Ship>():null;
            if(selected&&ship)stage=selected.Stage(ship);else selected=null;
            Valid.Invoke(__instance,new object[]{selected!=null});
        }
        ghost.SetActive(selected!=null&&!selected.Busy);
        if(selected)ghost.transform.SetPositionAndRotation(stage,selected.StageRotation);
        return false;
    }
}
[HarmonyPatch(typeof(Player),"HaveRequirements",new[]{typeof(Piece),typeof(Player.RequirementMode)})]
internal static class SlipwayBlueprintRequirements
{
    private static void Postfix(Piece piece,Player.RequirementMode mode,ref bool __result)
    {
        if(mode==Player.RequirementMode.CanBuild && piece && SlipwayPlacement.Plan(piece.gameObject)?.UsesSlipway==true)
            __result=true; // Authoritative workshop/access checks run when reserving the order.
    }
}
// Native station queries also support instant ships and hammer ingredient checks.
// Quartermaster's regular coverage setting may be disabled, so enforce the requested
// Helmsman-specific base-zone rule independently for this one station type.
[HarmonyPatch(typeof(CraftingStation),"HaveBuildStationInRange")]
internal static class ShipwrightStationCoverage
{
    private static void Prefix(Vector3 point,out Vector3 __state)=>__state=point;
    private static void Postfix(string name,Vector3 __state,ref CraftingStation __result)
    {
        if(name!="Puffin shipwright"||!Player.m_localPlayer)return;
        if(QuartermasterWorkshop.Available)
        {
            var bench=WorkshopRange.Bench(Player.m_localPlayer,__state);
            __result=bench?bench.GetComponent<CraftingStation>():null!;
        }
    }
}

[HarmonyPatch(typeof(CraftingStation),nameof(CraftingStation.Interact))]
internal static class ShipwrightBenchInteraction
{
    private static bool Prefix(CraftingStation __instance,Humanoid user,bool repeat,bool alt,ref bool __result)
    {
        var yard=__instance.GetComponent<Shipyard>();if(!yard)return true;
        __result=yard.Interact(user,repeat,alt);return false;
    }
}
