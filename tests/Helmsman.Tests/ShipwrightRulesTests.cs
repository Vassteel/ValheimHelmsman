using System;
using System.Linq;
using Helmsman.Core;

internal static class ShipwrightRulesTests
{
    internal static void Run(Action<bool,string> check)
    {
        var first=ShipwrightRules.Upgrades.Single(u=>u.Id=="cargo1");
        var second=ShipwrightRules.Upgrades.Single(u=>u.Id=="cargo2");
        check(ShipwrightRules.Check(second,_=>false).Length>0,"Second cargo purchase requires first stage");
        check(ShipwrightRules.Check(second,id=>id==first.Id)=="","First stage unlocks the second cargo purchase");
        check(ShipwrightRules.Check(first,id=>id==first.Id).Length>0,"Duplicate purchase is rejected before charging");
        check(ShipwrightRules.Cargo(false,false,6,3)==(6,3),"Unmodified longship keeps its native hold");
        check(ShipwrightRules.Cargo(true,false,6,3)==(7,3),"First stage adds only a cargo column");
        check(ShipwrightRules.Cargo(false,true,6,3)==(7,4),"Saved second-stage flag restores complete capacity");
        check(ShipwrightRules.Cargo(true,true,10,8)==(10,8),"Refits never shrink an already larger inventory");
        foreach(var upgrade in ShipwrightRules.Upgrades.Append(ShipwrightRules.Hull))
        {
            var cost=ShipwrightRules.Costs(upgrade.Recipe);
            check(!ShipwrightRules.CanPay(cost,_=>0),upgrade.Name+" cannot be purchased with no materials");
            check(ShipwrightRules.CanPay(cost,name=>cost[name]),upgrade.Name+" accepts exact material payment");
            var missing=cost.Keys.First();
            check(!ShipwrightRules.CanPay(cost,name=>cost[name]-(name==missing?1:0)),upgrade.Name+" rejects one missing material");
        }
        check(ShipwrightRules.Costs("Wood:2,Wood:3")["Wood"]==5,"Repeated recipe entries charge the full combined cost");
        foreach(var invalid in new[]{"","Wood:0","Wood:-1","Wood:100001","Wood","Wood:1:2",":2","Wood:NaN"})
        {
            bool rejected=false;try{ShipwrightRules.Costs(invalid);}catch(ArgumentException){rejected=true;}
            check(rejected,"Reject malformed material recipe: "+invalid);
        }
    }
}
