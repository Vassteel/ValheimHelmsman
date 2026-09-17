using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

public sealed partial class Shipwright : MonoBehaviour
{
    internal Ship Ship=null!;
    internal ZNetView View=null!;
    internal ZDO Data=>View.GetZDO();
    internal bool Ready=>View && View.IsValid() && visualsReady;
    internal static bool OtherMod=>Chainloader.PluginInfos.ContainsKey("shudnal.LongshipUpgrades");
    internal static readonly int AmmoKey=Key("ammo"),AmmoTypeKey=Key("ammo_type");
    internal static int Key(string name)=>("helmsman_shipwright_"+name+"_v1").GetStableHashCode();
    private float refreshAt;
    private uint revision=uint.MaxValue;
    private bool visualsReady;
    private WearNTear wear=null!;
    private readonly List<GameObject> ownedObjects=new();
    private static readonly Dictionary<string,string> legacy=new(){["lantern"]="LanternUpgraded",["canopy"]="TentUpgraded",["cargo1"]="ContainerUpgradedLvl1",["cargo2"]="ContainerUpgradedLvl2",["treatment"]="AshlandsUpgraded",["ballistas"]="TurretsUpgraded"};
    internal bool Built(string id)=>Data.GetBool(Key(id)) || (legacy.TryGetValue(id,out var old) && Data.GetBool(old.GetStableHashCode()));
    internal static bool Supports(Ship ship)=>ship && ShipProfile.PrefabName(ship)=="VikingShip";
    internal static Shipwright? For(Ship? ship)=>ship && Supports(ship) ? ship.GetComponent<Shipwright>() : null;
    private IEnumerator Start()
    {
        Ship=GetComponent<Ship>();View=GetComponent<ZNetView>();wear=GetComponent<WearNTear>();
        while(View && !View.IsValid())yield return null;
        if(!View || OtherMod)yield break;
        while(!ObjectDB.instance || !ZNetScene.instance)yield return null;
        try {CreateParts();visualsReady=true;if(wear)wear.m_onDestroyed+=Refund;Refresh();}
        catch(Exception e){Plugin.Instance.Error(e);Cleanup();}
    }
    private void Update()
    {
        if(!Ready || Time.time<refreshAt)return;
        refreshAt=Time.time+.25f;
        if(revision!=Data.DataRevision){Refresh();revision=Data.DataRevision;}
    }
    internal string CheckAccess(Player player)
    {
        if(!player || player.IsDead() || !Ready)return "The shipwright's workshop is not ready.";
        if(Vector3.Distance(player.transform.position,transform.position)>30)return "Stay beside the ship.";
        if(!PrivateArea.CheckAccess(transform.position,0,false,true))return "This ship is inside a protected ward.";
        if(Plugin.Instance.Voyage && Plugin.Instance.Voyage.Ship==Ship)return "End the voyage before refitting.";
        if(Ship.GetComponent<Rigidbody>().linearVelocity.magnitude>.8f)return "Bring the ship to a stop first.";
        if(!View.IsOwner())return "Take the helm to claim this ship, stop, then ask the puffin again.";
        var hold=Ship.GetComponentInChildren<Container>();
        if(hold && hold.IsInUse())return "Close the cargo hold before refitting.";
        return "";
    }
    internal static string Requirements(ShipUpgrade upgrade)
    {
        var costs=ShipwrightRules.Costs(upgrade.Recipe);
        return string.Join(", ",costs.Select(p=>p.Value+" "+ItemName(p.Key)))+" · "+Localization.instance.Localize(upgrade.Station)+" "+upgrade.Level;
    }
    internal static string ItemName(string prefab)
    {
        var item=ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>() : null;
        return item ? Localization.instance.Localize(item.m_itemData.m_shared.m_name) : prefab;
    }
    internal static string StationCheck(ShipUpgrade upgrade,Vector3 position)
    {
        var stations=new List<CraftingStation>();CraftingStation.FindStationsInRange(upgrade.Station,position,100,stations);
        return stations.Any(s=>s && s.GetLevel()>=upgrade.Level && PrivateArea.CheckAccess(s.transform.position,0,false,true)) ? "" :
            "Needs "+Localization.instance.Localize(upgrade.Station)+" level "+upgrade.Level+" within 100 m.";
    }
    internal static string Pay(Player player,ShipUpgrade upgrade,Vector3 position,Action commit,bool noCost=false)
    {
        return CommissionCosts.Commit(noCost,()=>PayMaterials(player,upgrade,position,commit),commit,
            error=>Plugin.Instance.Error(error));
    }
    private static string PayMaterials(Player player,ShipUpgrade upgrade,Vector3 position,Action commit)
    {
        string station=StationCheck(upgrade,position);if(station.Length>0)return station;
        var inventory=player.GetInventory();var costs=ShipwrightRules.Costs(upgrade.Recipe);
        if(!ShipwrightRules.CanPay(costs,name=>(int)Math.Min(int.MaxValue,inventory.GetAllItems().Where(i=>i.m_dropPrefab && i.m_dropPrefab.name==name).Sum(i=>(long)i.m_stack))))
            return "Bring "+Requirements(upgrade)+" to the shipwright.";
        // An immediate local transaction: full native serialization preserves metadata on failure.
        var backup=new ZPackage();inventory.Save(backup);
        try
        {
            foreach(var cost in costs)
            {
                int left=cost.Value;
                foreach(var item in inventory.GetAllItems().Where(i=>i.m_dropPrefab && i.m_dropPrefab.name==cost.Key).ToArray())
                {int take=Math.Min(left,item.m_stack);if(!inventory.RemoveItem(item,take))throw new InvalidOperationException("Materials changed during payment.");left-=take;if(left==0)break;}
                if(left!=0)throw new InvalidOperationException("Incomplete payment.");
            }
            commit();return "";
        }
        catch(Exception e){inventory.Load(new ZPackage(backup.GetArray()),false);Plugin.Instance.Error(e);return "Work stopped; your materials were returned.";}
    }
    internal string Buy(ShipUpgrade upgrade)
    {
        var player=Player.m_localPlayer;string reason=CheckAccess(player);if(reason.Length>0)return reason;
        reason=ShipwrightRules.Check(upgrade,Built);if(reason.Length>0)return reason;
        // All model dependencies have already been built successfully before charging.
        reason=Pay(player,upgrade,transform.position,()=>
        {
            try {Data.Set(Key("spent_"+upgrade.Id),upgrade.Recipe);Data.Set(Key(upgrade.Id),true);Refresh();}
            catch {Data.Set(Key(upgrade.Id),false);Data.Set(Key("spent_"+upgrade.Id),"");throw;}
        });
        return reason.Length>0?reason:upgrade.Name+" fitted. All done.";
    }
    internal string Toggle(string key)
    {
        var error=CheckAccess(Player.m_localPlayer);if(error.Length>0)return error;
        Data.Set(Key(key),!Data.GetBool(Key(key)));Refresh();return "Ship order carried out.";
    }
    internal string Style(string kind,string style)
    {
        var error=CheckAccess(Player.m_localPlayer);if(error.Length>0)return error;
        if(!ShipwrightAssets.Styles(kind).Contains(style))return "That cloth style is no longer available.";
        Data.Set(Key(kind),style);Refresh();return "Cloth style changed to "+style+".";
    }
    internal string Mount(ItemDrop.ItemData item)
    {
        var error=CheckAccess(Player.m_localPlayer);if(error.Length>0)return error;
        if(!Built("trophy") || item.m_shared.m_itemType!=ItemDrop.ItemData.ItemType.Trophy)return "Choose a trophy from your inventory.";
        if(Data.GetString(Key("trophy_item")).Length>0)return "Take down the current trophy first.";
        var inv=Player.m_localPlayer.GetInventory();if(!inv.ContainsItem(item))return "That trophy is no longer in your inventory.";
        var one=item.Clone();one.m_stack=1;one.m_gridPos=new Vector2i(0,0);
        var saved=new Inventory("Trophy",null,1,1);saved.AddItem(one);var package=new ZPackage();saved.Save(package);
        var backup=new ZPackage();inv.Save(backup);
        try
        {
            if(!inv.RemoveItem(item,1))return "Trophy changed; try again.";
            Data.Set(Key("trophy_saved"),package.GetArray());Data.Set(Key("trophy_item"),one.m_dropPrefab.name);
        }
        catch(Exception exception)
        {
            Data.Set(Key("trophy_item"),"");Data.Set(Key("trophy_saved"),Array.Empty<byte>());
            inv.Load(new ZPackage(backup.GetArray()),false);Plugin.Instance.Error(exception);return "Mounting stopped; trophy returned.";
        }
        Refresh();return "Trophy mounted. No Forsaken power attached.";
    }
    private ItemDrop.ItemData? SavedTrophy()
    {
        var bytes=Data.GetByteArray(Key("trophy_saved"));if(bytes==null || bytes.Length==0)return null;
        var inv=new Inventory("Trophy",null,1,1);inv.Load(new ZPackage(bytes),false);return inv.GetAllItems().FirstOrDefault();
    }
    internal string TakeTrophy()
    {
        var error=CheckAccess(Player.m_localPlayer);if(error.Length>0)return error;
        var item=SavedTrophy();if(item==null)return "No trophy is mounted.";
        var inv=Player.m_localPlayer.GetInventory();if(!inv.HaveEmptySlot())return "Make one free inventory slot first.";
        var backup=new ZPackage();inv.Save(backup);
        var saved=Data.GetByteArray(Key("trophy_saved"));var name=Data.GetString(Key("trophy_item"));
        try
        {
            if(!inv.AddItem(item))return "No room for the trophy.";
            Data.Set(Key("trophy_item"),"");Data.Set(Key("trophy_saved"),Array.Empty<byte>());
        }
        catch(Exception exception)
        {
            inv.Load(new ZPackage(backup.GetArray()),false);Data.Set(Key("trophy_saved"),saved);Data.Set(Key("trophy_item"),name);
            Plugin.Instance.Error(exception);return "Trophy transfer stopped; it remains mounted.";
        }
        Refresh();return "Trophy returned.";
    }
    private void Refund()
    {
        if(!View || !View.IsValid() || !View.IsOwner())return;
        var hold=Ship.GetComponentInChildren<Container>();var cratePrefab=hold ? hold.m_destroyedLootPrefab : null;
        Inventory? crate=null;
        void Store(ItemDrop.ItemData item)
        {
            if(cratePrefab)
            {
                if(crate==null || !crate.HaveEmptySlot())crate=Instantiate(cratePrefab,transform.position+Vector3.up,UnityEngine.Random.rotation).GetComponent<Container>().GetInventory();
                if(crate.AddItem(item))return;
            }
            ItemDrop.DropItem(item,item.m_stack,transform.position+Vector3.up,Quaternion.identity);
        }
        foreach(var upgrade in ShipwrightRules.Upgrades)
        {
            var recipe=Data.GetString(Key("spent_"+upgrade.Id));if(recipe.Length==0)continue;
            foreach(var cost in ShipwrightRules.Costs(recipe))
            {
                var prefab=ObjectDB.instance.GetItemPrefab(cost.Key);if(!prefab)continue;
                int left=cost.Value;while(left>0){var item=prefab.GetComponent<ItemDrop>().m_itemData.Clone();item.m_dropPrefab=prefab;item.m_stack=Math.Min(left,Math.Max(1,item.m_shared.m_maxStackSize));left-=item.m_stack;Store(item);}
            }
            Data.Set(Key("spent_"+upgrade.Id),"");
        }
        var trophy=SavedTrophy();if(trophy!=null)Store(trophy);
        var ammo=ObjectDB.instance.GetItemPrefab(Data.GetString(AmmoTypeKey));int remaining=Data.GetInt(AmmoKey);
        if(ammo)while(remaining>0){var item=ammo.GetComponent<ItemDrop>().m_itemData.Clone();item.m_dropPrefab=ammo;item.m_stack=Math.Min(remaining,Math.Max(1,item.m_shared.m_maxStackSize));remaining-=item.m_stack;Store(item);}
        Data.Set(AmmoKey,0);Data.Set(Key("trophy_item"),"");Data.Set(Key("trophy_saved"),Array.Empty<byte>());
    }
    private void OnDestroy(){if(wear)wear.m_onDestroyed-=Refund;Cleanup();}
}

[HarmonyPatch(typeof(Ship),"Awake")]
internal static class ShipwrightAttach
{
    private static void Postfix(Ship __instance)
    {if(Shipwright.Supports(__instance) && !Shipwright.OtherMod && !__instance.GetComponent<Shipwright>())__instance.gameObject.AddComponent<Shipwright>();}
}
