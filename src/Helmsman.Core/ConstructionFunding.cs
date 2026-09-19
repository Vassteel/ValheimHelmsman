using System;
using System.Collections.Generic;
using System.Linq;
namespace Helmsman.Core;

// A persisted receipt for actual supplied materials, never the unfulfilled recipe.
public static class ConstructionFunding
{
    public static Dictionary<string,int> Supplied(string supplied)=>string.IsNullOrEmpty(supplied)?new Dictionary<string,int>(StringComparer.Ordinal):ShipwrightRules.Costs(supplied);
    public static bool Valid(string recipe,string supplied)
    {
        try{var costs=ShipwrightRules.Costs(recipe);return Supplied(supplied).All(p=>costs.TryGetValue(p.Key,out var max)&&p.Value<=max);}
        catch(ArgumentException){return false;}
        catch(OverflowException){return false;}
    }
    public static int Remaining(string recipe,string supplied,string item)
    {var costs=ShipwrightRules.Costs(recipe);var paid=Supplied(supplied);return costs.TryGetValue(item,out int total)?Math.Max(0,total-(paid.TryGetValue(item,out int n)?n:0)):0;}
    public static string Credit(string recipe,string supplied,string item,int actual)
    {
        if(!Valid(recipe,supplied)||actual<0||actual>Remaining(recipe,supplied,item))throw new ArgumentException("Invalid construction material receipt");
        var paid=Supplied(supplied);if(actual>0)paid[item]=(paid.TryGetValue(item,out int n)?n:0)+actual;
        return string.Join(",",paid.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+":"+p.Value));
    }
    public static bool Complete(string recipe,string supplied)=>Valid(recipe,supplied)&&ShipwrightRules.Costs(recipe).All(p=>Remaining(recipe,supplied,p.Key)==0);
    public static IReadOnlyDictionary<string,int> Refund(string recipe,string supplied,bool waiting,bool free)
        =>free?new Dictionary<string,int>():waiting?Supplied(supplied):ShipwrightRules.Costs(recipe);
}
