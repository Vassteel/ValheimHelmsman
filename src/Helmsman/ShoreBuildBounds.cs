using HarmonyLib;
using UnityEngine;

namespace Helmsman;

// Only rebuilt shore structures carry this marker. Bounds are in prefab space.
public sealed class ShoreBuildBounds : MonoBehaviour
{
    public Bounds Bounds;
    public bool RelaxedDockPlacement;

    internal static void ConfigureDock(GameObject prefab, string prefabName)
    {
        if(prefabName!="FishingDock" && prefabName!="FishingDock_Extension")return;
        var piece=prefab.GetComponent<Piece>();
        // Piers need to intersect shoreline terrain and adjacent building pieces.
        // Keep the ordinary station, access, biome and no-build-zone checks.
        piece.m_noClipping=false;
        // Fixed piers use surface placement, not the ship-only three-metre lift.
        piece.m_waterPiece=false;
        var bounds=prefab.GetComponent<ShoreBuildBounds>();
        if(bounds)bounds.RelaxedDockPlacement=true;
    }
}

[HarmonyPatch(typeof(Player), "SetupPlacementGhost")]
internal static class ShorePlacementPreview
{
    private static void Postfix(GameObject ___m_placementGhost)
    {
        var ghost=___m_placementGhost;
        if(!ghost)return;
        var bounds=ghost.GetComponent<ShoreBuildBounds>();
        if(!bounds)return;
        // ClosestPoint in Player.UpdatePlacementGhost only considers primitives
        // and convex meshes. With only a concave mesh it uses Vector3.zero and
        // moves the preview far away. Keep this proxy exclusively on the ghost.
        // Native player-overlap checks skip root colliders. The dock's enclosing
        // box is only an alignment aid, not solid space around its posts; retain
        // checks on actual child colliders such as the chest instead.
        var proxy=ghost;
        if(!bounds.RelaxedDockPlacement)
        {
            proxy=new GameObject("Helmsman placement bounds");
            proxy.layer=ghost.layer;
            proxy.transform.SetParent(ghost.transform,false);
        }
        var collider=proxy.AddComponent<BoxCollider>();
        collider.center=bounds.Bounds.center;
        collider.size=bounds.Bounds.size;
    }
}
