using System;
using Helmsman.Core;
internal static class ConstructionFundingTests
{
    internal static void Run(Action<bool,string> check)
    {
        const string recipe="Wood:20,IronNails:5";
        check(ConstructionFunding.Valid(recipe,"")&&!ConstructionFunding.Complete(recipe,""),"Unfunded blueprint is valid and waits for materials");
        string receipt=ConstructionFunding.Credit(recipe,"","Wood",7);
        check(ConstructionFunding.Remaining(recipe,receipt,"Wood")==13,"Reloaded partial receipt only requests the outstanding amount");
        var refund=ConstructionFunding.Refund(recipe,receipt,true,false);
        check(refund.Count==1&&refund["Wood"]==7,"Cancelled pending build refunds only collected materials");
        receipt=ConstructionFunding.Credit(recipe,receipt,"Wood",13);
        receipt=ConstructionFunding.Credit(recipe,receipt,"IronNails",5);
        check(ConstructionFunding.Complete(recipe,receipt),"Mixed supply sources complete one recipe");
        bool rejected=false;try{ConstructionFunding.Credit(recipe,receipt,"Wood",1);}catch(ArgumentException){rejected=true;}
        check(rejected,"Already funded items cannot be charged again");
        check(!ConstructionFunding.Valid(recipe,"Wood:21")&&!ConstructionFunding.Valid(recipe,"Stone:1")&&!ConstructionFunding.Valid(recipe,"Wood:-1"),"Corrupt and unrelated receipts are rejected");
        check(ConstructionFunding.Refund(recipe,"",false,false)["Wood"]==20,"Legacy paid orders retain their full refund");
        check(ConstructionFunding.Refund(recipe,receipt,false,true).Count==0,"Free construction cannot refund uncharged resources");
    }
}
