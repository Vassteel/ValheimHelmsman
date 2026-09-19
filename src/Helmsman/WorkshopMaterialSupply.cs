using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;
using Jotunn.Managers;
using UnityEngine;
namespace Helmsman;

internal static class WorkshopMaterialSupply
{
    internal static void AddStorage(GameObject bench)
    {
        var native=PrefabManager.Instance.GetPrefab("piece_chest_wood").GetComponent<Container>();
        var storage=bench.AddComponent<Container>();
        storage.m_name="Shipwright supplies";storage.m_width=8;storage.m_height=4;
        storage.m_bkg=native.m_bkg;storage.m_privacy=native.m_privacy;
        storage.m_checkGuardStone=true;storage.m_autoDestroyEmpty=false;
        storage.m_openEffects=native.m_openEffects;storage.m_closeEffects=native.m_closeEffects;
        storage.m_destroyedLootPrefab=native.m_destroyedLootPrefab;
    }
    internal static string ItemName(string id)
    {
        var prefab=ObjectDB.instance?ObjectDB.instance.GetItemPrefab(id):null;
        return prefab?Localization.instance.Localize(prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name):id;
    }
    internal static string Missing(string recipe,string supplied)=>string.Join(", ",ShipwrightRules.Costs(recipe).Select(c=>(c.Key,n:ConstructionFunding.Remaining(recipe,supplied,c.Key))).Where(c=>c.n>0).Select(c=>ItemName(c.Key)+" × "+c.n));
    internal static void Return(System.Collections.Generic.IReadOnlyDictionary<string,int> materials,Vector3 position)
    {
        foreach(var cost in materials)
        {
            var prefab=ObjectDB.instance.GetItemPrefab(cost.Key);if(!prefab)continue;
            int left=cost.Value;while(left>0){var item=prefab.GetComponent<ItemDrop>().m_itemData.Clone();item.m_dropPrefab=prefab;item.m_stack=Math.Min(left,Math.Max(1,item.m_shared.m_maxStackSize));ItemDrop.DropItem(item,item.m_stack,position,Quaternion.identity);left-=item.m_stack;}
        }
    }
    private static bool Usable(Container container,Player player)
    {
        var view=container?container.GetComponent<ZNetView>():null;
        return container&&view&&view.IsValid()&&view.IsOwner()&&!container.IsInUse()&&view.GetZDO().GetInt(ZDOVars.s_inUse)==0&&
            (bool)AccessTools.Method(typeof(Container),"CheckAccess").Invoke(container,new object[]{player.GetPlayerID()})&&PrivateArea.CheckAccess(container.transform.position,0,false,true);
    }
    internal static void Collect(Player player,Shipyard bench,SlipwayOrder order,Action save)
        =>Collect(player,bench,order.order.recipe,order.supplied,order.order.creator,receipt=>{order.supplied=receipt;save();});
    private static List<(Inventory inventory,Func<bool> valid,string kind)> Sources(Player player,Shipyard bench,long creator)
    {
        var sources=new List<(Inventory inventory,Func<bool> valid,string kind)>();
        var seen=new HashSet<Inventory>();
        void Add(Inventory inventory,Func<bool> valid,string kind){if(seen.Add(inventory))sources.Add((inventory,valid,kind));}
        var storage=bench.GetComponent<Container>();
        foreach(var chest in QuartermasterWorkshop.Supplies(player,bench.transform.position))
        {var source=chest;if(source==storage)continue;Add(source.GetInventory(),()=>QuartermasterWorkshop.UsableSupply(source),"Quartermaster");}
        if(storage&&Usable(storage,player))Add(storage.GetInventory(),()=>Usable(storage,player),"Bench");
        if(player.GetPlayerID()==creator)Add(player.GetInventory(),()=>player==Player.m_localPlayer,"Carried");
        return sources;
    }
    internal static Dictionary<string,(int quartermaster,int bench,int carried)> Available(Player player,Shipyard bench,long creator,IEnumerable<string> items)
    {
        var sources=Sources(player,bench,creator);
        var result=new Dictionary<string,(int,int,int)>();
        foreach(string id in items)
        {
            var prefab=ObjectDB.instance.GetItemPrefab(id);if(!prefab)continue;
            string name=prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            int qm=0,stored=0,carried=0;
            foreach(var source in sources)
            {
                if(!source.valid())continue;
                int count=source.inventory.GetAllItems().Where(i=>i.m_shared.m_name==name&&i.m_worldLevel>=Game.m_worldLevel).Sum(i=>i.m_stack);
                if(source.kind=="Quartermaster")qm+=count;else if(source.kind=="Bench")stored+=count;else carried+=count;
            }
            result[id]=(qm,stored,carried);
        }
        return result;
    }
    internal static void Collect(Player player,Shipyard bench,string recipe,string supplied,long creator,Action<string> save)
    {
        // Display and collection share the exact ownership, ward, supply-setting
        // and world-level filters. Ghost icons never create inventory items.
        var sources=Sources(player,bench,creator);
        foreach(var cost in ShipwrightRules.Costs(recipe))
        {
            var prefab=ObjectDB.instance.GetItemPrefab(cost.Key);if(!prefab)continue;
            string name=prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            foreach(var source in sources)
            {
                if(!source.valid())continue;
                foreach(var item in source.inventory.GetAllItems().ToArray())
                {
                    int missing=ConstructionFunding.Remaining(recipe,supplied,cost.Key);if(missing==0)break;
                    if(item.m_shared.m_name!=name||item.m_worldLevel<Game.m_worldLevel)continue;
                    int count=Math.Min(missing,item.m_stack);if(count<=0||!source.valid())continue;
                    if(source.inventory.RemoveItem(item,count))
                    {supplied=ConstructionFunding.Credit(recipe,supplied,cost.Key,count);save(supplied);}
                }
            }
        }
    }
}

[HarmonyPatch(typeof(CraftingStation),nameof(CraftingStation.GetHoverText))]
internal static class ShipwrightSupplyHint
{
    private static void Postfix(CraftingStation __instance,ref string __result)
    {
        var yard=__instance.GetComponent<Shipyard>();if(yard)__result=yard.GetHoverText();
    }
}
