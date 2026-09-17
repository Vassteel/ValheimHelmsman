using System;
using System.Collections.Generic;

namespace Helmsman.Core;

public static class CommissionCosts
{
    public static string Commit(bool freeBuild,Func<string> payMaterials,Action commit,Action<Exception> reportError)
    {
        if(!freeBuild)return payMaterials();
        // Native zero-cost building skips material and crafting-station costs.
        // Shipyard access, reservation and berth validation happen before this.
        try{commit();return "";}
        catch(Exception error){reportError(error);return "The free build order could not be started.";}
    }
    public static IReadOnlyDictionary<string,int> Refund(string recipe,bool freeBuild)
        =>freeBuild?new Dictionary<string,int>():ShipwrightRules.Costs(recipe);
}
