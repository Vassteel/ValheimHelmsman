using System;
using System.Linq;
using Helmsman.Core;

internal static class CommissionCostTests
{
    internal static void Run(Action<bool,string> check)
    {
        int inventory=30,orders=0,paidCalls=0,errors=0;
        void Commit()=>orders++;
        string Paid(){paidCalls++;if(inventory<20)return "Missing materials";inventory-=20;Commit();return "";}
        var result=CommissionCosts.Commit(true,Paid,Commit,_=>errors++);
        check(result==""&&inventory==30&&orders==1&&paidCalls==0,"Free ship creates one order without invoking material/station payment");
        result=CommissionCosts.Commit(false,Paid,Commit,_=>errors++);
        check(result==""&&inventory==10&&orders==2&&paidCalls==1,"Turning zero-cost off restores ordinary payment");
        result=CommissionCosts.Commit(false,Paid,Commit,_=>errors++);
        check(result=="Missing materials"&&inventory==10&&orders==2,"Normal insufficient-material failure creates no order");
        result=CommissionCosts.Commit(true,Paid,()=>throw new InvalidOperationException(),_=>errors++);
        check(result.Length>0&&inventory==10&&orders==2&&errors==1,"Free-order commit failure reports failure without inventory changes");
        foreach(var ship in ShipConstruction.Blueprints)
        {
            var paidRefund=CommissionCosts.Refund(ship.Recipe,false);var expected=ShipwrightRules.Costs(ship.Recipe);
            check(paidRefund.Count==expected.Count&&expected.All(k=>paidRefund[k.Key]==k.Value),ship.Name+" paid/legacy order refunds original recipe");
            check(CommissionCosts.Refund(ship.Recipe,true).Count==0,ship.Name+" free order never refunds unpaid resources");
        }
    }
}
