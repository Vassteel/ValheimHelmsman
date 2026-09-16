using System;
using System.Collections;
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
    internal Vector3 SimulationCenter=>searchingShore ? shoreOrigin : Center;
    internal bool Loading {get;private set;}
    private GullGuide? gull;
    private Voyage? voyage;
    private Player requester=null!;
    private float nextUpdate,started;
    private bool finishing,areaReady,shoreline,searchingShore;
    private Vector3 shoreOrigin;
    private DockRecord? shoreDestination;
    private long world;
    private readonly List<ZDO> checkedObjects=new List<ZDO>();
    internal static bool Begin(DockMarker ward,ShipRecord record,out string reason)
    {
        var home=ward && ward.Ready ? DockDirectory.Resolve(ward.Id) : null;
        if(home==null){reason="Configure this dock's arrival berth first.";return false;}
        return Begin(home,record,ward,out reason);
    }
    internal static bool BeginShoreline(ShipRecord record,out string reason)
    {
        if(!CanRequest(record,out var zdo,out var ship,out reason))return false;
        if(!ShorelineArrival.CallerReady(Player.m_localPlayer,out reason))return false;
        var request=Plugin.Instance.gameObject.AddComponent<SummonRequest>();
        request.shoreline=true;request.searchingShore=true;request.Loading=true;request.shoreOrigin=Player.m_localPlayer.transform.position;
        request.ShipId=record.Id;request.requester=Player.m_localPlayer;request.Center=zdo.GetPosition();request.started=Time.time;
        request.world=ZNet.instance.GetWorldUID();request.Status="Finding safe water near your shoreline";
        Plugin.Instance.Summon=request;
        request.StartCoroutine(request.FindShoreline(record,ship));
        reason="The gull is checking for a safe landing. He'll sail the ship to this calling spot; you can move on.";return true;
    }
    private IEnumerator FindShoreline(ShipRecord record,Ship ship)
    {
        var profile=ShipProfile.For(ship);var chart=new WaterChart(record.Loaded,profile);
        int checkedSpots=0;
        foreach(var berth in ShorelineArrival.Candidates(shoreOrigin,profile,record.Prefab))
        {
            // Keep the original calling area loaded while checking it, including if
            // the player teleports away before this incremental search completes.
            while(!ReadyArea && !finishing)yield return null;
            if(finishing)yield break;
            bool clear;
            try {clear=ShorelineArrival.Validate(shoreOrigin,berth,chart,out _);}
            catch(Exception e){Plugin.Instance.Error(e);Cancel("Could not check the shoreline safely.");yield break;}
            if(clear)
            {
                if(Vector3.Distance(WaterChart.AtSea(Center),WaterChart.AtSea(berth.position))<6)
                {Cancel("That ship is already beside this shoreline.");yield break;}
                shoreDestination=new DockRecord {Temporary=true,Berth=berth,MarkerPosition=shoreOrigin};
                searchingShore=false;Loading=false;areaReady=false;
                try
                {
                    gull=GullGuide.Create(shoreOrigin+Vector3.up*3,null,null);
                    if(!gull){Cancel("The gull could not answer.");yield break;}
                    gull.Fetch(Center+Vector3.up*3);
                    Status="The gull is fetching "+record.Name+" to your shoreline";
                    Plugin.Message(Status);
                }
                catch(Exception e){Plugin.Instance.Error(e);Cancel("The gull could not answer.");}
                yield break;
            }
            Status="Checking shoreline clearance — "+(++checkedSpots)+" spots";
            yield return null;
        }
        Cancel("No safe landing nearby for that ship. Try a more open shoreline with deeper water.");
    }
    private static bool CanRequest(ShipRecord record,out ZDO zdo,out Ship ship,out string reason)
    {
        zdo=null!;ship=null!;
        if(!Plugin.LocalSession){reason="Join a world before calling a ship.";return false;}
        if(!UnattendedShipPhysics.Installed){reason="Empty-ship control is incompatible with this game/mod setup.";return false;}
        if(Plugin.Instance.Voyage || Plugin.Instance.Summon || Plugin.Instance.CalledGull){reason="Finish or cancel the current voyage or gull visit first.";return false;}
        zdo=ZDOMan.instance.GetZDO(record.Id);
        if(zdo==null || ShipDirectory.SavedName(zdo).Length==0){reason="That named ship no longer exists.";return false;}
        var prefab=ZNetScene.instance.GetPrefab(zdo.GetPrefab());
        if(!prefab || !ShipProfile.Supports(prefab.GetComponent<Ship>())){reason="This ship needs a custom movement adapter.";return false;}
        var loaded=record.Loaded;
        if(loaded && (loaded.HasPlayerOnboard() || loaded.m_shipControlls.HaveValidUser())){reason="The ship is occupied. Only empty ships can be summoned.";return false;}
        ship=loaded ? loaded : prefab.GetComponent<Ship>();reason="";return true;
    }
    private static bool Begin(DockRecord home,ShipRecord record,DockMarker? ward,out string reason)
    {
        if(!CanRequest(record,out var zdo,out _,out reason))return false;
        if(GullGuide.Traveller(home.Id)){reason="The gull is finishing its last trip.";return false;}
        if(Vector3.Distance(WaterChart.AtSea(zdo.GetPosition()),WaterChart.AtSea(home.Berth.position))<6){reason="That ship is already at this dock.";return false;}
        var request=Plugin.Instance.gameObject.AddComponent<SummonRequest>();
        request.Home=home.Id;request.ShipId=record.Id;request.requester=Player.m_localPlayer;request.Center=zdo.GetPosition();request.started=Time.time;request.world=ZNet.instance.GetWorldUID();
        Plugin.Instance.Summon=request;
        if(ward)request.gull=ward.FetchGuide(request.Center+Vector3.up*3);
        else
        {
            request.gull=GullGuide.Create(Player.m_localPlayer.transform.position+Vector3.up*3,null,null);
            if(!request.gull){request.Cancel("The gull could not answer.");reason="The gull could not answer.";return false;}
            request.gull.Fetch(request.Center+Vector3.up*3);
        }
        request.gull.ReserveDock(home.Id);
        if(ward && !ward.GetComponent<SummonBeacon>())ward.gameObject.AddComponent<SummonBeacon>();
        reason="The gull is fetching "+record.Name+".";return true;
    }
    internal bool ReadyArea=>Loading && areaReady;
    private void KeepAreaLoaded()
    {
        var simulationCenter=SimulationCenter;
        var center=ZoneSystem.GetZone(simulationCenter);
        bool created=false;
        for(int x=-2;x<=2 && !created;x++)for(int z=-2;z<=2;z++)
            if((bool)LoadZones.Invoke(ZoneSystem.instance,new object[]{new Vector2s(center.x+x,center.y+z)})) {created=true;break;}
        areaReady=NetworkNavigation.Ready(simulationCenter);
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            if(!ZoneSystem.instance.IsZoneLoaded(simulationCenter+new Vector3(x*64,0,z*64)))areaReady=false;
        if(!areaReady)return;
        checkedObjects.Clear();
        ZDOMan.instance.FindSectorObjects(center,new SimulationDistance(2,0,true),checkedObjects);
        foreach(var zdo in checkedObjects)
            if(Vector3.Distance(WaterChart.AtSea(zdo.GetPosition()),WaterChart.AtSea(simulationCenter))<80 &&
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
        if(!Plugin.LocalSession || !requester || requester!=Player.m_localPlayer || requester.IsDead() || ZNet.instance.GetWorldUID()!=world){Cancel("Summon stopped: player unavailable.");return;}
        if(!voyage && Time.time-started>1800){Cancel("The gull could not prepare that ship's journey in time.");return;}
        if(searchingShore){KeepAreaLoaded();return;}
        var home=shoreline ? shoreDestination : DockDirectory.Resolve(Home);var zdo=ZDOMan.instance.GetZDO(ShipId);
        if(home==null || zdo==null){Cancel("Summon stopped: dock or ship removed.");return;}
        var go=ZNetScene.instance.FindInstance(ShipId);var ship=go ? go.GetComponent<Ship>() : null;
        Center=ship ? ship.transform.position : zdo.GetPosition();
        if(!gull && !voyage){Cancel("Summon guide unavailable.");return;}
        if(!voyage && gull)gull.FetchTarget(Center+Vector3.up*3);
        if(voyage || (gull && Vector3.Distance(gull.transform.position,Center)<100))Loading=true;
        if(Loading)KeepAreaLoaded();
        if(voyage){Status=voyage.Status;return;}
        Status=Loading ? "Gull finding the ship" : "Gull flying to the ship";
        if(!ReadyArea || !ship || Vector3.Distance(gull!.transform.position,Center)>8)return;
        if(ship.HasPlayerOnboard() || ship.m_shipControlls.HaveValidUser()){Cancel("Summon stopped: ship is occupied.");return;}
        if(!PrivateArea.CheckAccess(ship.transform.position,0,false,true)){Cancel("Summon stopped: this ship is inside a protected ward.");return;}
        var view=ship.GetComponent<ZNetView>();if(ZNet.instance.IsServer())view.ClaimOwnership();
        if(!view.IsOwner())return;
        if(Voyage.BeginSummoned(ship,home,gull,out var reason)){voyage=Plugin.Instance.Voyage;Status=shoreline ? "Bringing the ship to your shoreline" : "Bringing the ship to the dock";}
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
        var request=Plugin.Instance.Summon;if(!request || !request.Loading || !Plugin.LocalSession)return;
        remote.Clear();
        ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(request.SimulationCenter),new SimulationDistance(2,0,true),remote);
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
        if(request && request.Loading && Plugin.LocalSession && Mathf.Abs(point.x-request.SimulationCenter.x)<128 && Mathf.Abs(point.z-request.SimulationCenter.z)<128)__result=true;
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
