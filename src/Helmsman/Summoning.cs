using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Helmsman.Core;
using TMPro;
using UnityEngine;

namespace Helmsman;

public sealed class SummonRequest : MonoBehaviour
{
    private static readonly MethodInfo LoadZones=AccessTools.Method(typeof(ZoneSystem),"PokeLocalZone");
    internal ZDOID Home,ShipId;
    internal string Status {get;private set;}="Sending the gull";
    internal Vector3 Center {get;private set;}
    internal bool Loading {get;private set;}
    private GullGuide? gull;
    private Voyage? voyage;
    private Player requester=null!;
    private float nextUpdate,started;
    private bool finishing,areaReady;
    private readonly List<ZDO> checkedObjects=new List<ZDO>();
    internal static bool Begin(DockMarker ward,ShipRecord record,out string reason)
    {
        if(!Plugin.Solo){reason="Ship summoning currently supports solo worlds.";return false;}
        if(!UnattendedShipPhysics.Installed){reason="Empty-ship control is incompatible with this game/mod setup.";return false;}
        if(Plugin.Instance.Voyage || Plugin.Instance.Summon || Plugin.Instance.CalledGull){reason="Finish or cancel the current voyage or gull visit first.";return false;}
        if(!ward.Settings.configured || DockDirectory.Resolve(ward.Id)==null){reason="Configure this dock's arrival berth first.";return false;}
        if(GullGuide.Traveller(ward.Id)){reason="The gull is finishing its last trip.";return false;}
        var zdo=ZDOMan.instance.GetZDO(record.Id);
        if(zdo==null || zdo.GetString(ShipDirectory.NameKey,"").Length==0){reason="That named ship no longer exists.";return false;}
        var prefab=ZNetScene.instance.GetPrefab(zdo.GetPrefab());
        if(!prefab || !ShipProfile.Supports(prefab.GetComponent<Ship>())){reason="This ship needs a custom movement adapter.";return false;}
        var loaded=record.Loaded;
        if(loaded && (loaded.HasPlayerOnboard() || loaded.m_shipControlls.HaveValidUser())){reason="The ship is occupied. Only empty ships can be summoned.";return false;}
        if(Vector3.Distance(WaterChart.AtSea(zdo.GetPosition()),WaterChart.AtSea(ward.Settings.position))<6){reason="That ship is already at this dock.";return false;}
        var request=Plugin.Instance.gameObject.AddComponent<SummonRequest>();
        request.Home=ward.Id;request.ShipId=record.Id;request.requester=Player.m_localPlayer;request.Center=zdo.GetPosition();request.started=Time.time;
        Plugin.Instance.Summon=request;
        request.gull=ward.FetchGuide(request.Center+Vector3.up*3);
        request.gull.ReserveDock(ward.Id);
        if(!ward.GetComponent<SummonBeacon>())ward.gameObject.AddComponent<SummonBeacon>();
        reason="The gull is fetching "+record.Name+".";return true;
    }
    internal bool ReadyArea=>Loading && areaReady;
    private void KeepAreaLoaded()
    {
        var center=ZoneSystem.GetZone(Center);
        bool created=false;
        for(int x=-2;x<=2 && !created;x++)for(int z=-2;z<=2;z++)
            if((bool)LoadZones.Invoke(ZoneSystem.instance,new object[]{new Vector2s(center.x+x,center.y+z)})) {created=true;break;}
        areaReady=true;
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            if(!ZoneSystem.instance.IsZoneLoaded(Center+new Vector3(x*64,0,z*64)))areaReady=false;
        if(!areaReady)return;
        checkedObjects.Clear();
        ZDOMan.instance.FindSectorObjects(center,new SimulationDistance(2,0,true),checkedObjects);
        foreach(var zdo in checkedObjects)
            if(Vector3.Distance(WaterChart.AtSea(zdo.GetPosition()),WaterChart.AtSea(Center))<80 &&
                ZNetScene.instance.GetPrefab(zdo.GetPrefab()) && !ZNetScene.instance.HaveInstance(zdo))
            {areaReady=false;break;}
    }
    private void Update()
    {
        if(finishing || Time.time<nextUpdate)return;nextUpdate=Time.time+.1f;
        try {Advance();}catch(Exception e){Plugin.Instance.Error(e);Cancel("Summon stopped after an error.");}
    }
    private void Advance()
    {
        if(!Plugin.Solo || !requester || requester.IsDead()){Cancel("Summon stopped: player unavailable.");return;}
        var home=DockDirectory.Resolve(Home);var zdo=ZDOMan.instance.GetZDO(ShipId);
        if(home==null || zdo==null){Cancel("Summon stopped: dock or ship removed.");return;}
        var go=ZNetScene.instance.FindInstance(ShipId);var ship=go ? go.GetComponent<Ship>() : null;
        Center=ship ? ship.transform.position : zdo.GetPosition();
        if(!gull && !voyage){Cancel("Summon guide unavailable.");return;}
        if(!voyage && gull)gull.FetchTarget(Center+Vector3.up*3);
        if(voyage || (gull && Vector3.Distance(gull.transform.position,Center)<100))Loading=true;
        if(Loading)KeepAreaLoaded();
        if(voyage){Status=voyage.Status;return;}
        Status=Loading ? "Gull finding the ship" : "Gull flying to the ship";
        if(Time.time-started>1800){Cancel("The gull could not reach that ship in time.");return;}
        if(!ReadyArea || !ship || Vector3.Distance(gull!.transform.position,Center)>8)return;
        if(ship.HasPlayerOnboard() || ship.m_shipControlls.HaveValidUser()){Cancel("Summon stopped: ship is occupied.");return;}
        var view=ship.GetComponent<ZNetView>();view.ClaimOwnership();
        if(!view.IsOwner())return;
        if(Voyage.BeginSummoned(ship,home,gull,out var reason)){voyage=Plugin.Instance.Voyage;Status="Bringing the ship to the dock";}
        else Cancel(reason);
    }
    internal void Complete(string reason)
    {
        if(finishing)return;finishing=true;Loading=false;Status=reason;
        if(Plugin.Instance.Summon==this)Plugin.Instance.Summon=null;
        Plugin.Message(reason);Destroy(this);
    }
    internal void Cancel(string reason)
    {
        if(finishing)return;finishing=true;
        if(voyage)voyage.Cancel(reason);else if(gull)gull.FlyAway();
        finishing=false;Complete(reason);
    }
    private void OnDestroy(){if(!finishing)Cancel("Summon ended.");}
}

public sealed class SummonBeacon : MonoBehaviour
{
    private TMP_Text? label;
    private float next;
    private void Update()
    {
        var request=Plugin.Instance.Summon;var ward=GetComponent<DockMarker>();
        bool show=request && ward && ward.Ready && request.Home==ward.Id && Player.m_localPlayer &&
            Vector3.Distance(Player.m_localPlayer.transform.position,transform.position)<50;
        if(!show){if(label)label.gameObject.SetActive(false);return;}
        if(!label)
        {
            var font=MenuTheme.FindFont();if(!font)return;
            var go=new GameObject("Summoned ship status");go.transform.SetParent(transform,false);
            label=go.AddComponent<TextMeshPro>();label.font=font;label.fontSize=3;label.richText=false;
            label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(8,2);label.raycastTarget=false;
        }
        label.gameObject.SetActive(true);label.transform.position=transform.position+Vector3.up*3.5f;
        if(Camera.main)label.transform.rotation=Camera.main.transform.rotation;
        if(Time.time<next)return;next=Time.time+.3f;
        var delta=request!.Center-transform.position;
        var zdo=ZDOMan.instance.GetZDO(request.ShipId);
        label.text=(zdo?.GetString(ShipDirectory.NameKey,"Ship") ?? "Ship")+"\n"+
            new Vector2(delta.x,delta.z).magnitude.ToString("0")+" m · "+ShipText.Bearing(delta.x,delta.z)+"\n"+request.Status;
    }
}

// Extend the loaded world around one summoned ship, retaining the player's normal active area.
[HarmonyPatch(typeof(ZNetScene),"CreateObjects")]
internal static class SummonSceneObjects
{
    private static readonly List<ZDO> remote=new List<ZDO>();
    internal static void Prefix(List<ZDO> currentNearObjects)
    {
        var request=Plugin.Instance.Summon;if(!request || !request.Loading || !Plugin.Solo)return;
        remote.Clear();
        ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(request.Center),new SimulationDistance(2,0,true),remote);
        var seen=new HashSet<ZDO>(currentNearObjects);
        foreach(var zdo in remote)
            if(ZoneSystem.instance.IsZoneLoaded(zdo.GetPosition()) && seen.Add(zdo))currentNearObjects.Add(zdo);
    }
}
[HarmonyPatch(typeof(ZNetScene),"PointInsideActiveArea")]
internal static class SummonActiveArea
{
    internal static void Postfix(Vector3 point,ref bool __result)
    {
        var request=Plugin.Instance.Summon;
        if(request && request.Loading && Plugin.Solo && Mathf.Abs(point.x-request.Center.x)<128 && Mathf.Abs(point.z-request.Center.z)<128)__result=true;
    }
}
[HarmonyPatch(typeof(Ship),nameof(Ship.CustomFixedUpdate))]
internal static class UnattendedShipPhysics
{
    internal static bool Installed;
    // Do not add a fake player to the boat. Only the two empty-boat physics branches get a virtual crew count.
    internal static int CrewCount(List<Player> crew,Ship ship)
    {
        var voyage=Plugin.Instance.Voyage;
        return crew.Count+(voyage && voyage.Ship==ship && voyage.Unattended && voyage.CanControl ? 1 : 0);
    }
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var count=AccessTools.PropertyGetter(typeof(List<Player>),"Count");var helper=AccessTools.Method(typeof(UnattendedShipPhysics),nameof(CrewCount));
        var original=new List<CodeInstruction>(instructions);
        var result=new List<CodeInstruction>();int replaced=0;
        foreach(var code in original)if(code.Calls(count))replaced++;
        Installed=replaced==2;
        if(!Installed)return original;
        foreach(var code in original)
        {
            if(code.Calls(count))
            {
                var ship=new CodeInstruction(OpCodes.Ldarg_0);ship.labels.AddRange(code.labels);code.labels.Clear();
                result.Add(ship);code.opcode=OpCodes.Call;code.operand=helper;
            }
            result.Add(code);
        }
        return result;
    }
}
