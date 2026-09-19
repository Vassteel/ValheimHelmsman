using System;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Helmsman;

// Expanding intermediate supplies can require six materials plus the workshop.
// Native HUD code indexes the station slot immediately after the materials.
[HarmonyPatch(typeof(Hud),"SetupPieceInfo")]
internal static class ShipRequirementHud
{
    private static void Prefix(Piece piece,ref GameObject[] ___m_requirementItems)
    {
        if(!piece || !ShipConstruction.Blueprints.Any(b=>b.Prefab==Utils.GetPrefabName(piece.gameObject)))return;
        var slots=___m_requirementItems;
        int needed=piece.m_resources.Length+(piece.m_craftingStation?1:0);
        if(slots.Length>=needed || slots.Length==0)return;
        var first=slots[0].transform as RectTransform;
        if(!first)return;
        var parent=first!.parent;
        bool automatic=parent.GetComponent<LayoutGroup>();
        int previous=slots.Length;
        int original=Array.FindIndex(slots,s=>s.name.StartsWith("helmsman_requirement_",StringComparison.Ordinal));
        if(original<0)original=previous;
        Array.Resize(ref slots,needed);
        for(int i=previous;i<needed;i++)
        {
            var slot=UnityEngine.Object.Instantiate(slots[0],parent,false);
            slot.name="helmsman_requirement_"+i;
            // Layout groups position their own children. For the native fixed row,
            // place overflow on a second row using the existing column spacing.
            if(!automatic)
            {
                var rect=(RectTransform)slot.transform;
                var column=(RectTransform)slots[(i-original)%original].transform;
                rect.anchoredPosition=column.anchoredPosition-new Vector2(0,(Mathf.Max(first.rect.height,64)+8)*(1+(i-original)/original));
            }
            slot.SetActive(false);
            slots[i]=slot;
        }
        ___m_requirementItems=slots;
    }
}
