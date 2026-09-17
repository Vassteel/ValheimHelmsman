using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Helmsman;

internal static class RockCargo
{
    private static readonly Func<Container,long,bool> Access=AccessTools.MethodDelegate<Func<Container,long,bool>>(AccessTools.Method(typeof(Container),"CheckAccess"));
    private const string CollectedKey="helmsman_rock_collected";
    private static IEnumerable<Container> Holds(Ship ship,Player player)
    {
        if(!ship||!ship.IsOwner()||!player||player!=Player.m_localPlayer||!ship.IsPlayerInBoat(player))yield break;
        foreach(var chest in ship.GetComponentsInChildren<Container>(true))
        {
            if(chest.GetComponentInParent<Ship>()!=ship||chest.IsInUse()||!chest.IsOwner()||!Access(chest,player.GetPlayerID()))continue;
            if(chest.m_checkGuardStone&&!PrivateArea.CheckAccess(chest.transform.position,0,false,true))continue;
            var scope=chest.GetComponent<ShipHoldScope>();if(scope&&scope.MigrationBlocked)continue;
            var view=chest.m_rootObjectOverride?chest.m_rootObjectOverride:chest.GetComponent<ZNetView>();
            if(!view||!view.IsValid()||!view.IsOwner()||view.GetZDO().GetInt(scope?scope.Key(ZDOVars.s_inUse):ZDOVars.s_inUse)!=0)continue;
            yield return chest;
        }
    }
    private static long Space(Inventory inventory,ItemDrop.ItemData item)
    {
        int maximum=item.m_shared.m_maxStackSize;
        return (long)inventory.GetEmptySlots()*maximum+inventory.GetAllItems()
            .Where(i=>i.m_shared.m_name==item.m_shared.m_name&&i.m_quality==item.m_quality&&i.m_worldLevel==item.m_worldLevel)
            .Sum(i=>(long)Math.Max(0,maximum-i.m_stack));
    }
    internal static bool Store(Ship ship,Player player,ItemDrop drop)
    {
        if(!drop||!drop.m_itemData.m_dropPrefab||drop.m_itemData.m_dropPrefab.name!="Stone")return false;
        var source=drop.GetComponent<ZNetView>();
        if(!source||!source.IsValid()||!source.IsOwner()||source.GetZDO().GetBool(CollectedKey)||!drop.CanPickup(false))return false;
        var item=drop.m_itemData;
        if(item.m_stack<1||item.m_stack>item.m_shared.m_maxStackSize)return false;
        // Native rock drops are single-item stacks. Keep each complete stack
        // intact if no accessible hold can accept it; never sweep nearby items.
        var chest=Holds(ship,player).FirstOrDefault(c=>Space(c.GetInventory(),item)>=item.m_stack&&c.GetInventory().CanAddItem(item,item.m_stack));
        if(!chest)return false;
        var inventory=chest.GetInventory();var backup=new ZPackage();inventory.Save(backup);int amount=item.m_stack;
        try
        {
            if(!inventory.AddItem(item.Clone()))
            {inventory.Load(new ZPackage(backup.GetArray()),false);return false;}
            item.m_stack=0;ItemDrop.SaveToZDO(item,source.GetZDO());
            source.GetZDO().Set(CollectedKey,true);
        }
        catch(Exception error)
        {
            inventory.Load(new ZPackage(backup.GetArray()),false);
            item.m_stack=amount;ItemDrop.SaveToZDO(item,source.GetZDO());
            source.GetZDO().Set(CollectedKey,false);Plugin.Instance.Error(error);return false;
        }
        // A zero stack plus the saved marker prevents another pickup even if
        // network destruction is delayed. The resource is already in cargo.
        try {source.Destroy();}catch(Exception error){Plugin.Instance.Error(error);}
        return true;
    }
}
