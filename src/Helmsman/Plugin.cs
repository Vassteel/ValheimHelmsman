using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace Helmsman;

[BepInPlugin(Guid, "Valheim Helmsman", "0.2.26")]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency("local.valheim.quartermaster", BepInDependency.DependencyFlags.SoftDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid = "local.valheim.helmsman";
    public const string DockPrefab = "helmsman_dock_ward";
    public static Plugin Instance = null!;
    internal DockDirectory Directory = null!;
    internal HelmsmanUI UI = null!;
    internal Voyage? Voyage;
    internal ShipDirectory Ships=null!;
    internal SummonRequest? Summon;
    internal GullCall? CalledGull;
    internal CargoOrder? Cargo;
    internal ConfigEntry<float> BoardingSeconds = null!;
    internal ConfigEntry<KeyCode> CallKey = null!;
    internal ConfigEntry<bool> DebugRoute = null!;
    internal ConfigEntry<float> MinimumWaterDepth = null!;
    internal ConfigEntry<bool> RelaxedDockChecks = null!;
    internal ConfigEntry<Vector3> GullSternPerch = null!;
    internal ConfigEntry<float> GullPerchLift = null!;
    private Harmony? harmony;
    private static readonly Func<Player,bool> PlayerAcceptsInput=AccessTools.MethodDelegate<Func<Player,bool>>(
        AccessTools.Method(typeof(Player),"TakeInput"));

    private void Awake()
    {
        Instance = this;
        BoardingSeconds = Config.Bind("Voyages", "BoardingGraceSeconds", 10f,
            new ConfigDescription("Minimum boarding grace period.", new AcceptableValueRange<float>(3,60), new ConfigurationManagerAttributes { IsAdminOnly=true }));
        CallKey = Config.Bind("Controls", "CallGullKey", KeyCode.F8, "Call the gull aboard, or speak to him after he lands.");
        DebugRoute = Config.Bind("Diagnostics", "ShowRoute", true, "Show blue route wisps four metres above the navigation path. Also toggled from the gull, dock or whistle menu.");
        MinimumWaterDepth = Config.Bind("Navigation", "MinimumWaterDepth", 1f,
            new ConfigDescription("Minimum water depth in metres for navigation checks. Berth settings can be saved even when clearance checks fail.",
                new AcceptableValueRange<float>(.25f,5f), new ConfigurationManagerAttributes { IsAdminOnly=true }));
        RelaxedDockChecks = Config.Bind("Navigation", "RelaxedDockChecks", true,
            new ConfigDescription("Allow nearby departure from the actual ship pose; treat built dock pieces and full turning-disk checks as advisories during slow dock maneuvers. Terrain and other ships still block. Disable for strict clearance checks.",null,new ConfigurationManagerAttributes { IsAdminOnly=true }));
        GullSternPerch = Config.Bind("Gull", "SternPerch", new Vector3(0,1.7f,-4.3f), "Ship-local stern perch. X/Z select a surface to probe; Y is the fallback height.");
        GullPerchLift = Config.Bind("Gull", "PerchHeightAdjustment", 0f, new ConfigDescription("Additional ship-perch height adjustment after surface probing.", new AcceptableValueRange<float>(-2,2)));
        Shipyard.Configure(Config);
        FishingDock.Configure(Config);
        harmony = new Harmony(Guid);
        try { harmony.PatchAll(typeof(Plugin).Assembly); }
        catch (Exception error)
        {
            harmony.UnpatchSelf();
            Logger.LogError("Helmsman disabled: game hooks did not match. " + error);
            enabled=false;
            return;
        }
        Directory = gameObject.AddComponent<DockDirectory>();
        UI = gameObject.AddComponent<HelmsmanUI>();
        Ships=gameObject.AddComponent<ShipDirectory>();
        gameObject.AddComponent<NetworkNavigation>();
        PrefabManager.OnVanillaPrefabsAvailable += RegisterDock;
        Logger.LogInfo("Helmsman 0.2.26 loaded. Peer-owned voyages and server-coordinated ship calls; no voyage resumes automatically on load.");
    }

    private void RegisterDock()
    {
        try { ImportedHulls.Register(); }
        catch(Exception error){Logger.LogError("Shipyard model registration failed: "+error);}
        try { GullcallWhistle.Register(); }
        catch(Exception error){Logger.LogError("Gullcall Whistle registration failed: "+error);}
        var prefab = PrefabManager.Instance.CreateClonedPrefab(DockPrefab, "guard_stone");
        var area = prefab.GetComponent<PrivateArea>();
        if (area)
        {
            if (area.m_enabledEffect) area.m_enabledEffect.SetActive(false);
            if (area.m_areaMarker) area.m_areaMarker.gameObject.SetActive(false);
            if (area.m_inRangeEffect) area.m_inRangeEffect.SetActive(false);
            DestroyImmediate(area);
        }
        foreach (var guide in prefab.GetComponentsInChildren<GuidePoint>(true)) DestroyImmediate(guide);
        prefab.AddComponent<DockMarker>();
        prefab.AddComponent<WorkstationLease>();
        var config = new PieceConfig
        {
            Name = "Dock Ward", Description = "Name and configure a ship berth. Speak to its gull to sail.",
            PieceTable = "Hammer", Category = "Helmsman"
        };
        // Retain the vanilla ward recipe.
        PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, config));
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterDock;
        Logger.LogInfo("Registered Dock Ward.");
    }

    internal static bool LocalSession => ZNet.instance && Player.m_localPlayer;
    internal static void Message(string text)
    {
        if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageHud.MessageType.Center, text);
    }
    internal void Error(Exception e) => Logger.LogError(e);
    internal void Record(string message) => Logger.LogInfo(message);

    private void Update()
    {
        if (Input.GetKeyDown(CallKey.Value) && !UI.IsOpen && Player.m_localPlayer &&
            (!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !TextInput.IsVisible() && !InventoryGui.IsVisible() && !Menu.IsVisible())
        {
            foreach(var ship in FindObjectsByType<Ship>(FindObjectsSortMode.None))
                if(ship.IsPlayerInBoat(Player.m_localPlayer)){GullCall.Call(ship);break;}
        }
        if (Voyage && !LocalSession) Voyage.Cancel("Voyage stopped: player session ended.");
    }

    private void LateUpdate()
    {
        if(!MastInteraction.Pending)return;
        var player=Player.m_localPlayer;
        var hover=player ? player.GetHoverObject() : null;
        bool alt=ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive() ? ZInput.GetButton("JoyAltKeys") :
            ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
        bool accepts=player && PlayerAcceptsInput(player) && !player.InAttack() && !player.InDodge() &&
            !UI.IsOpen && !Hud.InRadial() && !alt;
        MastInteraction.Tick(Time.unscaledTime,ZInput.GetButton("Use") || ZInput.GetButton("JoyUse"),
            player,hover ? hover.GetComponentInParent<Chair>() : null,accepts);
    }

    private void OnDestroy()
    {
        MastInteraction.Cancel();
        if(Cargo)Cargo.Stop("Helmsman unloaded.");
        if(CalledGull)CalledGull.Dismiss("Helmsman unloaded.");
        if(Summon)Summon.Cancel("Helmsman unloaded.");
        if (Voyage) Voyage.Cancel("Helmsman unloaded.");
        if (UI) UI.Close();
        PrefabManager.OnVanillaPrefabsAvailable -= RegisterDock;
        harmony?.UnpatchSelf();
        GullcallAssets.Release();
        ShipwrightAssets.Release();
        ShipMenuPreviews.Release();
        BoatyardModels.Release();
        ImportedShipMaterials.Release();
    }
}

[HarmonyPatch(typeof(Ship), "HaveControllingPlayer")]
internal static class PilotCountsAsController
{
    private static void Postfix(Ship __instance, ref bool __result)
    {
        var voyage = Plugin.Instance.Voyage;
        if (voyage && voyage.Ship == __instance && voyage.CanControl) __result = true;
    }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
internal static class PilotBeforePhysics
{
    private static void Prefix(Ship __instance, float fixedDeltaTime)
    {
        var voyage = Plugin.Instance.Voyage;
        if (!voyage || voyage.Ship != __instance) return;
        try { voyage.Tick(fixedDeltaTime); }
        catch (Exception e) { Plugin.Instance.Error(e); voyage.Cancel("Autopilot error; take the helm."); }
    }
}

[HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.Interact))]
internal static class ManualTakeover
{
    private static void Prefix(ShipControlls __instance, Humanoid character, bool repeat, bool alt)
    {
        if(alt)return;
        var voyage = Plugin.Instance.Voyage;
        if (!repeat && voyage && voyage.Ship == __instance.m_ship && character is Player player &&
            player.GetStandingOnShip() == __instance.m_ship && !player.IsEncumbered() &&
            __instance.m_attachPoint && Vector3.Distance(player.transform.position, __instance.m_attachPoint.position) < __instance.m_maxUseRange)
            voyage.Cancel("You have the helm.");
    }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.ApplyControlls))]
internal static class ManualInputTakeover
{
    private static void Prefix(Ship __instance)
    {
        var voyage = Plugin.Instance.Voyage;
        if (voyage && voyage.Ship == __instance) voyage.Cancel("You have the helm.");
    }
}
