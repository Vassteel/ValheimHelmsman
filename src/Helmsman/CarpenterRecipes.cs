using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;

namespace Helmsman;

internal static class CarpenterRecipes
{
    private static readonly HashSet<string> Items=new(
        HarborCatalog.Items.Where(item=>item.Recipe.Length>0).Select(item=>item.Prefab),StringComparer.Ordinal);
    internal static bool IsTable(CraftingStation station)=>station && Utils.GetPrefabName(station.gameObject)==ImportedHulls.TablePrefab;
    internal static bool Allows(Recipe recipe)=>recipe && recipe.m_item && IsTable(recipe.m_craftingStation) &&
        Items.Contains(Utils.GetPrefabName(recipe.m_item.gameObject));
    internal static void Configure(CraftingStation station)
    {
        station.m_name="Carpenter's Table";
        station.m_showBasicRecipies=false;
        station.m_upgrader=false;
    }
}

// Native no-cost mode bypasses station restrictions and lists every known recipe.
// Filter only this station's display list, including the inventory upgrade tab.
[HarmonyPatch(typeof(Player),nameof(Player.GetAvailableRecipes))]
internal static class CarpenterRecipeList
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Player __instance,ref List<Recipe> available)
    {
        if(CarpenterRecipes.IsTable(__instance.GetCurrentCraftingStation()))
            available.RemoveAll(recipe=>!CarpenterRecipes.Allows(recipe));
    }
}
