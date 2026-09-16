using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Helmsman;

[HarmonyPatch]
internal static class ShipHoldNetworking
{
    private static readonly HashSet<string> fields=new(){"s_items","s_inUse","s_addedDefaultItems"};
    private static readonly MethodInfo key=AccessTools.Method(typeof(ShipHoldNetworking),nameof(Key));
    private static readonly MethodInfo rpc=AccessTools.Method(typeof(ShipHoldNetworking),nameof(Rpc));
    // Restrict the transformation to the actual native methods containing these accesses.
    private static IEnumerable<MethodBase> TargetMethods()=>typeof(Container).GetMethods(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly)
        .Where(m=>m.GetMethodBody()!=null && (m.Name=="Awake" || m.Name=="UpdateUseVisual" || m.Name=="Interact" || m.Name=="Save" || m.Name=="Load" || m.Name.StartsWith("RPC_",StringComparison.Ordinal) || m.Name=="StackAll" || m.Name=="TakeAll"));
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach(var instruction in instructions)
        {
            yield return instruction;
            bool saveKey=instruction.opcode==OpCodes.Ldsfld && instruction.operand is FieldInfo field && field.DeclaringType==typeof(ZDOVars) && fields.Contains(field.Name);
            bool rpcName=instruction.opcode==OpCodes.Ldstr && instruction.operand is string text && text.StartsWith("RPC_",StringComparison.Ordinal);
            if(saveKey || rpcName){yield return new CodeInstruction(OpCodes.Ldarg_0);yield return new CodeInstruction(OpCodes.Call,saveKey?key:rpc);}
        }
    }
    private static int Key(int original,Container container)
    {var scope=container.GetComponent<ShipHoldScope>();return scope?scope.Key(original):original;}
    private static string Rpc(string original,Container container)
    {var scope=container.GetComponent<ShipHoldScope>();return scope?scope.Rpc(original):original;}
}

// Odin's inventory saves are base64 strings indexed by the original hold label.
// Convert only when the new slot has no data; retain the original record for recovery.
[HarmonyPatch]
internal static class ShipHoldMigration
{
    private static IEnumerable<MethodBase> TargetMethods()
    {yield return AccessTools.Method(typeof(Container),"Awake");yield return AccessTools.Method(typeof(Container),"Load");}
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Container __instance)
    {
        var scope=__instance.GetComponent<ShipHoldScope>();if(!scope||scope.LegacyName.Length==0)return;
        var view=__instance.m_rootObjectOverride?__instance.m_rootObjectOverride:__instance.GetComponent<ZNetView>();
        if(!view||!view.IsValid()||!view.IsOwner())return;
        var data=view.GetZDO();int key=scope.Key(ZDOVars.s_items);
        if(data.GetByteArray(key)!=null)return;
        string old=data.GetString("items "+scope.LegacyName);
        if(old.Length==0)return;
        try
        {
            var bytes=Convert.FromBase64String(old);
            var header=new ZPackage(bytes);var version=(global::Version.Item)header.ReadInt();
            int expected=version>=global::Version.Item.Smaller?header.ReadUShort():header.ReadInt();
            if(expected<0||expected>__instance.m_width*__instance.m_height)throw new InvalidOperationException("Cargo item count exceeds this hold.");
            // Native Load skips unavailable item prefabs. Comparing the slot count
            // prevents a missing dependency silently becoming a successful import.
            var inventory=new Inventory("Cargo migration",null,__instance.m_width,__instance.m_height);
            inventory.Load(new ZPackage(bytes),false);
            if(inventory.GetAllItems().Count!=expected)throw new InvalidOperationException("One or more saved cargo items are unavailable.");
            data.Set(key,bytes);
            data.Set(scope.Key(ZDOVars.s_addedDefaultItems),true);
            scope.MigrationWarning=false;
            Plugin.Instance.Record("Migrated existing ship cargo: "+scope.LegacyName);
        }
        catch(Exception error)
        {
            if(!scope.MigrationWarning)Plugin.Instance.Record("Cargo retained, hold locked ("+scope.LegacyName+"): "+error.Message);
            scope.MigrationWarning=true;
        }
    }
}

[HarmonyPatch(typeof(Container),"Save")]
internal static class ShipHoldMigrationSaveGuard
{
    private static bool Prefix(Container __instance)=>!(__instance.GetComponent<ShipHoldScope>()?.MigrationBlocked??false);
}
[HarmonyPatch(typeof(Container),"Load")]
internal static class ShipHoldMigrationLoadGuard
{
    [HarmonyPriority(Priority.Last)]
    private static bool Prefix(Container __instance,ref bool __result)
    {if(!(__instance.GetComponent<ShipHoldScope>()?.MigrationBlocked??false))return true;__result=false;return false;}
}
[HarmonyPatch(typeof(Container),nameof(Container.Interact))]
internal static class ShipHoldMigrationOpenGuard
{
    private static bool Prefix(Container __instance,ref bool __result)
    {
        if(!(__instance.GetComponent<ShipHoldScope>()?.MigrationBlocked??false))return true;
        Plugin.Message("This cargo hold is waiting for its saved items. Check the Helmsman log before using it.");__result=false;return false;
    }
}
