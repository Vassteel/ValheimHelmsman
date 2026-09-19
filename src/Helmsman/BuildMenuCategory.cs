using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;

namespace Helmsman;

// The current build menu groups by ByUsagePieceList, separately from Jotunn's
// legacy PieceCategory tabs. Use a named virtual tag: allocating enum bits would
// collide with other mods and exhaust the game's finite usage-flag space.
internal static class BuildMenuCategory
{
    internal const string Token = "$helmsman_build_category";
    private static readonly HashSet<string> Prefabs = new(
        HarborCatalog.Pieces.Select(p => p.Prefab).Concat(ShipConstruction.Blueprints.Select(b => b.Prefab)).Concat(new[] { Plugin.DockPrefab, ImportedHulls.TablePrefab, "HelmsmanSlipway", "HelmsmanToolRack", "HelmsmanCaulkingStation", "HelmsmanRiggingRack", "HelmsmanPaintStand" }),
        StringComparer.Ordinal);
    internal static bool Contains(Piece piece) => piece && Prefabs.Contains(Utils.GetPrefabName(piece.gameObject));
}

[HarmonyPatch(typeof(ByUsagePieceList), MethodType.Constructor, new[] { typeof(string) })]
internal static class AddHelmsmanUsageCategory
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ref Piece.UsageTagFlags[] ___m_usageTags, ref string[] ___m_usageTagDisplayNames)
    {
        if (___m_usageTagDisplayNames.Contains(BuildMenuCategory.Token)) return;
        // Preserve all native and other-mod entries and their existing indices.
        ___m_usageTags = ___m_usageTags.Concat(new[] { (Piece.UsageTagFlags)0 }).ToArray();
        ___m_usageTagDisplayNames = ___m_usageTagDisplayNames.Concat(new[] { BuildMenuCategory.Token }).ToArray();
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetTagDisplayName))]
internal static class HelmsmanUsageLabel
{
    private static void Postfix(ref string __result)
    {
        if (__result == BuildMenuCategory.Token) __result = "Helmsman";
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.UpdateAvailableTags))]
internal static class HideUnrelatedHelmsmanCategory
{
    private static void Postfix(PieceTable pieceTable, string[] ___m_usageTagDisplayNames, List<int> ___m_availableTags)
    {
        int id = Array.IndexOf(___m_usageTagDisplayNames, BuildMenuCategory.Token);
        if (id >= 0 && !pieceTable.m_availablePieces.Any(BuildMenuCategory.Contains)) ___m_availableTags.Remove(id);
    }
}

[HarmonyPatch(typeof(ByUsagePieceList), nameof(ByUsagePieceList.GetAvailablePiecesWithTag))]
internal static class FilterHelmsmanUsageCategory
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(int tagId, PieceTable pieceTable, IList<Piece> resultOut, string[] ___m_usageTagDisplayNames)
    {
        if (tagId < 0 || tagId >= ___m_usageTagDisplayNames.Length || ___m_usageTagDisplayNames[tagId] != BuildMenuCategory.Token) return true;
        foreach (var piece in pieceTable.m_availablePieces)
            if (BuildMenuCategory.Contains(piece) || piece.m_repairPiece || piece.m_removePiece) resultOut.Add(piece);
        return false;
    }
}
