using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Helmsman;

internal static class SlipwaySnapping
{
    // Piece.GetSnapPoints follows root-child order; these are the four platform corners.
    internal static void OrderCorners(List<Transform> snaps)
    {
        var order=new[]{"snap_head_-4","snap_head_4","snap_side_-4_10","snap_side_4_10"};
        var names=new[]{"Shore corner left","Shore corner right","Water corner left","Water corner right"};
        for(int i=0;i<order.Length;i++)
        {
            var point=snaps.Find(t=>t.name==order[i]);
            if(!point)throw new InvalidOperationException("Missing slipway corner "+order[i]);
            point.name=names[i];point.SetSiblingIndex(i);
        }
        // Deck edges follow corners; buried support points are last when cycling.
        int next=4;
        foreach(var point in snaps)if(point.name.StartsWith("snap_side_")||point.name.StartsWith("snap_head_")&&!point.name.StartsWith("snap_head_support_"))point.SetSiblingIndex(next++);
    }
    internal static float SearchRadius(Transform ghost)
    {
        if(!ghost.GetComponent<Slipway>())return 10;
        var bounds=ghost.GetComponent<ShoreBuildBounds>();
        if(!bounds)return 10;
        // Vanilla searches only 10m around the origin; this 22m structure has
        // corners outside that sphere. Cover all corners plus the normal search margin.
        return 10+(Vector3.Scale(bounds.Bounds.extents,ghost.lossyScale).magnitude+
            Vector3.Scale(bounds.Bounds.center,ghost.lossyScale).magnitude);
    }
}

[HarmonyPatch(typeof(Player),"FindClosestSnapPoints")]
internal static class SlipwaySnapSearch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        int changed=0;
        foreach(var instruction in instructions)
        {
            if(instruction.opcode==OpCodes.Ldc_R4&&instruction.operand is float radius&&radius==10)
            {
                // Keep branch labels on the replacement load.
                var load=new CodeInstruction(instruction){opcode=OpCodes.Ldarg_1,operand=null};
                yield return load;
                yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(SlipwaySnapping),nameof(SlipwaySnapping.SearchRadius)));
                changed++;
            }
            else yield return instruction;
        }
        if(changed!=1)throw new InvalidOperationException("Slipway snapping expected one native search radius, found "+changed);
    }
}
