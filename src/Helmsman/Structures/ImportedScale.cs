#nullable disable
using HarmonyLib;
using UnityEngine;
namespace Helmsman.Structures
{
    // Only our marked imports are affected; preserve nonuniform blueprint scale after reload and on peers.
    [HarmonyPatch(typeof(ZNetView),"Awake")]
    internal static class ImportedScale
    {
        internal const string Key="poitotem_import_scale";
        private static void Postfix(ZNetView __instance)
        {
            if(!__instance.IsValid())return;
            var scale=__instance.GetZDO().GetVec3(Key,Vector3.zero);
            if(scale.x>=.01f&&scale.y>=.01f&&scale.z>=.01f&&scale.x<=20&&scale.y<=20&&scale.z<=20)__instance.transform.localScale=scale;
        }
    }
}
