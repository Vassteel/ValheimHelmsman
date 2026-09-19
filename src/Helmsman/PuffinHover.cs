using System.Collections.Generic;
using UnityEngine;

namespace Helmsman;

// Crosshair-only targeting keeps the cosmetic puffin out of building, combat and
// pathfinding physics. Native interaction targets and chest controls are retained.
internal sealed class PuffinHover:MonoBehaviour
{
    internal Shipyard Owner=null!;
    private static Shipyard? target;
    private static int targetFrame;
    private static readonly List<PuffinHover> active=new List<PuffinHover>();
    private static readonly int mask=LayerMask.GetMask("item","piece","piece_nonsolid","Default","static_solid","Default_small","character","character_net","terrain","vehicle","character_ghost");
    private static readonly RaycastHit[] hits=new RaycastHit[32];
    private static readonly Bounds body=new Bounds(new Vector3(0,.47f,.04f),new Vector3(.62f,.94f,.65f));
    private void OnEnable(){if(!active.Contains(this))active.Add(this);}
    private void OnDisable()=>active.Remove(this);
    internal static void Draw(Hud hud,Player player)
    {
        target=null;
        if(!player||player!=Player.m_localPlayer||!GameCamera.instance||
            InventoryGui.IsVisible()||Menu.IsVisible()||Console.IsVisible()||
            (TextViewer.instance&&TextViewer.instance.IsVisible())||!hud.m_crosshair.gameObject.activeInHierarchy)return;
        var camera=GameCamera.instance.transform;
        var ray=new Ray(camera.position,camera.forward);
        PuffinHover? nearest=null;float distance=float.PositiveInfinity;
        foreach(var owl in active)
        {
            if(!owl||!owl.Owner||!owl.gameObject.activeInHierarchy||owl.transform.lossyScale.x<.1f)continue;
            var localRay=new Ray(owl.transform.InverseTransformPoint(ray.origin),owl.transform.InverseTransformVector(ray.direction).normalized);
            if(!body.IntersectRay(localRay,out float localDistance))continue;
            var point=owl.transform.TransformPoint(localRay.GetPoint(localDistance));
            float candidate=Vector3.Distance(ray.origin,point);
            if(candidate>=distance||Vector3.Distance(player.GetEyePoint(),point)>player.m_maxInteractDistance||
                ParticleMist.IsMistBlocked(player.GetEyePoint(),point))continue;
            distance=candidate;nearest=owl;
        }
        if(!nearest)return;
        int count=Physics.RaycastNonAlloc(ray,hits,distance,mask,QueryTriggerInteraction.Ignore);
        // Fail closed when the bounded buffer fills. Never show through a wall.
        if(count==hits.Length)return;
        for(int i=0;i<count;i++)
        {
            var collider=hits[i].collider;
            if(collider&&collider.transform!=player.transform&&!collider.transform.IsChildOf(player.transform)&&hits[i].distance<distance-.03f)return;
        }
        target=nearest.Owner;targetFrame=Time.frameCount;
        hud.m_hoverName.text=Localization.instance.Localize("Puffin shipwright\n[<color=yellow>$KEY_Use</color>] Workshop\n")+nearest.Owner.NpcStatus;
    }
    internal static void Interact(Player player)
    {
        if(!target||Time.frameCount-targetFrame>1||player!=Player.m_localPlayer||!target.Near(player)||
            Plugin.Instance.UI.IsOpen||InventoryGui.IsVisible()||Menu.IsVisible()||Console.IsVisible()||TextInput.IsVisible()||
            (Chat.instance&&Chat.instance.HasFocus())||Hud.InRadial())return;
        if(!ZInput.GetButtonDown("Use")&&!ZInput.GetButtonDown("JoyUse"))return;
        target.Interact(player,false,ZInput.GetButton("AltPlace")||ZInput.GetButton("JoyAltPlace"));
        ZInput.ResetButtonStatus("Use");ZInput.ResetButtonStatus("JoyUse");
    }

}

[HarmonyLib.HarmonyPatch(typeof(Hud),"UpdateCrosshair")]
internal static class PuffinHoverPatch
{
    private static void Postfix(Hud __instance,Player player)=>PuffinHover.Draw(__instance,player);
}

[HarmonyLib.HarmonyPatch(typeof(Player),"Update")]
internal static class PuffinInteractionInput
{
    private static readonly System.Func<Player,bool> Input=HarmonyLib.AccessTools.MethodDelegate<System.Func<Player,bool>>(HarmonyLib.AccessTools.Method(typeof(Player),"TakeInput"));
    private static void Prefix(Player __instance){if(__instance==Player.m_localPlayer&&Input(__instance)&&!__instance.InAttack()&&!__instance.InDodge())PuffinHover.Interact(__instance);}
}
