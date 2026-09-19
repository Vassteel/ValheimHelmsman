#nullable disable
using HarmonyLib;

namespace Helmsman.Structures
{
    [HarmonyPatch(typeof(Menu),nameof(Menu.Show))]
    internal static class PlacementMenuInput
    {
        private static bool Prefix()=>StructureImports.Instance==null||!StructureImports.Instance.Tool||!StructureImports.Instance.Tool.SuppressGameMenu;
    }
    // The wheel belongs to the POI ghost while positioning, not camera zoom or
    // the equipped hammer. Our own sampler explicitly opts in to reading it.
    [HarmonyPatch(typeof(ZInput),nameof(ZInput.GetMouseScrollWheel))]
    internal static class PlacementWheelInput
    {
        private static void Postfix(ref float __result)
        {var tool=StructureImports.Instance!=null?StructureImports.Instance.Tool:null;if(tool&&tool.Positioning&&!tool.ReadingPlacementInput)__result=0;}
    }
    [HarmonyPatch(typeof(ZInput),nameof(ZInput.GetJoyRightStickX))]
    internal static class PlacementStickXInput
    {
        internal static void Postfix(ref float __result)
        {var tool=StructureImports.Instance!=null?StructureImports.Instance.Tool:null;if(tool&&tool.ControllerRotating&&!tool.ReadingPlacementInput)__result=0;}
    }
    [HarmonyPatch(typeof(ZInput),nameof(ZInput.GetJoyRightStickY))]
    internal static class PlacementStickYInput
    {
        private static void Postfix(ref float __result)=>PlacementStickXInput.Postfix(ref __result);
    }
    [HarmonyPatch(typeof(ZInput),nameof(ZInput.GetButton))]
    internal static class PlacementTriggerInput
    {
        private static void Postfix(string name,ref bool __result)
        {
            if(name!="JoyAttack"&&name!="JoyBlock"&&name!="JoySecondaryAttack")return;
            var tool=StructureImports.Instance!=null?StructureImports.Instance.Tool:null;
            if(tool&&tool.ControllerRotating)__result=false;
        }
    }
    [HarmonyPatch(typeof(Player),"UpdatePlacement")]
    internal static class PlacementHammerInput
    {
        private static void Prefix(Player __instance,ref bool takeInput)
        {var tool=StructureImports.Instance!=null?StructureImports.Instance.Tool:null;if(__instance==Player.m_localPlayer&&tool&&tool.Positioning)takeInput=false;}
    }
}
