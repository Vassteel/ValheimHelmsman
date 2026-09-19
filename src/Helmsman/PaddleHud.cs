using HarmonyLib;
using UnityEngine;
namespace Helmsman;

[HarmonyPatch(typeof(Hud),"UpdateShipHud")]
internal static class PaddleHud
{
    private static void Postfix(Player player,GameObject ___m_shipControlsRoot)
    {
        var ship=player?player.GetControlledShip():null;
        if(!ship||!ship.GetComponent<PaddleCraftRig>()||!___m_shipControlsRoot)return;
        var rect=___m_shipControlsRoot.transform as RectTransform;
        var parent=rect?rect.parent as RectTransform:null;
        if(!parent)return;
        // Position in the canvas's own units so UI scaling and resolution are respected.
        // Vanilla rewrites this position next frame for any other boat.
        var bounds=parent.rect;
        rect!.position=parent.TransformPoint(new Vector3(bounds.xMin+bounds.width*.82f,bounds.yMin+bounds.height*.27f,0));
    }
}
