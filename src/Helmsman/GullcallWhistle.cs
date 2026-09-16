using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Helmsman;

internal static class GullcallWhistle
{
    internal const string PrefabName="helmsman_gullcall_whistle";
    internal static void Register()
    {
        var item=new CustomItem(PrefabName,"BoneFragments",new ItemConfig {
            Name="Gullcall Whistle",
            Description="A bone gull with wooden wings and feather ties. Use from your inventory or hotbar near shore to ask the gull to bring a named ship to safe water nearby. No Dock Ward required. Reusable.",
            Amount=1,StackSize=1,Weight=.2f,Icon=GullcallAssets.Icon(),
            CraftingStation="",MinStationLevel=0,
            Requirements=new[] {
                new RequirementConfig("BoneFragments",4),new RequirementConfig("Wood",2),
                new RequirementConfig("LeatherScraps",2),new RequirementConfig("Feathers",2)
            }
        });
        GullcallAssets.Attach(item.ItemPrefab);
        var shared=item.ItemDrop.m_itemData.m_shared;
        shared.m_itemType=ItemDrop.ItemData.ItemType.Misc;
        shared.m_maxQuality=1;shared.m_useDurability=false;shared.m_questItem=false;shared.m_teleportable=true;
        shared.m_value=0;
        if(!ItemManager.Instance.AddItem(item))throw new System.InvalidOperationException("Could not register Gullcall Whistle.");
        Plugin.Instance.Record("Registered hand-crafted Gullcall Whistle with custom mesh and inventory icon.");
    }
    internal static bool IsWhistle(ItemDrop.ItemData? item)=>item!=null && item.m_dropPrefab && item.m_dropPrefab.name==PrefabName;
    internal static bool Carried(Player? player,ItemDrop.ItemData? item)=>player && !player.IsDead() &&
        IsWhistle(item) && player.GetInventory().ContainsItem(item);
}

[HarmonyPatch(typeof(Humanoid),nameof(Humanoid.UseItem))]
internal static class UseGullcallWhistle
{
    internal static bool Prefix(Humanoid __instance,Inventory inventory,ItemDrop.ItemData item)
    {
        if(!GullcallWhistle.IsWhistle(item))return true;
        // Intercept only this item; never consume it, equip it, or use it from another inventory.
        var player=__instance as Player;
        if(!player || player!=Player.m_localPlayer || !GullcallWhistle.Carried(player,item) ||
            (inventory!=null && inventory!=player.GetInventory()))return false;
        if(!Plugin.Solo){Plugin.Message("Ship summoning is not available in multiplayer yet.");return false;}
        if(player.InAttack() || player.InDodge()){Plugin.Message("Finish your current action before calling the gull.");return false;}
        if(InventoryGui.instance)InventoryGui.instance.Hide();
        Plugin.Instance.UI.OpenWhistle(item);
        return false;
    }
}
