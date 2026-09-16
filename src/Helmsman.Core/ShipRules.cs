using System;

namespace Helmsman.Core;

public static class ShipRules
{
    public static bool CanSail(string prefabName, bool hasSail, float sailForce, bool hasClothSail=false)
    {
        // Some rowing boats retain dummy mast/sail objects from a vanilla template.
        var name=prefabName.Replace("_", "").Replace("-", "").Replace(" ", "").ToLowerInvariant();
        return (hasSail || hasClothSail) && sailForce>0.0001f && !name.Contains("rowboat") &&
            !name.Contains("rowing") && !name.Contains("canoe");
    }
    public static bool IsMast(string animation)=>string.Equals(animation,"attach_mast",StringComparison.OrdinalIgnoreCase);
    public static bool IsSeat(string animation)=>!string.IsNullOrEmpty(animation) &&
        (animation.IndexOf("sit",StringComparison.OrdinalIgnoreCase)>=0 ||
         animation.IndexOf("chair",StringComparison.OrdinalIgnoreCase)>=0 ||
         animation.Equals("attach_lox",StringComparison.OrdinalIgnoreCase));
    public static bool Eligible(bool canSail,int seats)=>canSail || seats>1;
}
