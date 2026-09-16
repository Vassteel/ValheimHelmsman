using System;
using System.Linq;
using Helmsman.Core;
internal static class ShorelineTests
{
    internal static void Run(Action<bool,string> check)
    {
        var origin=new Point(100,-50);
        var candidates=ShorelineSearch.Candidates(origin,10).ToArray();
        check(candidates.Length==264,"Shore search has a finite 264-candidate budget for a small ship");
        check(candidates.All(c=>c.Center.Distance(origin)<=48.00001),"Shore candidates remain near the caller");
        check(candidates.Zip(candidates.Skip(1),(a,b)=>a.Center.Distance(origin)<=b.Center.Distance(origin)+.00001).All(x=>x),"Nearest landing rings are considered first");
        check(candidates.Take(24).Select(c=>c.Heading%360).Distinct().Count()==24,"Search covers every shoreline direction");
        check(candidates.All(c=> {
            var a=c.Heading*Math.PI/180;var toward=new Point(c.Center.X+Math.Sin(a),c.Center.Y+Math.Cos(a));
            return toward.Distance(origin)<c.Center.Distance(origin);
        }),"Every candidate points the bow toward the caller");
        check(ShorelineSearch.Candidates(origin,22).First().Center.Distance(origin)>candidates[0].Center.Distance(origin),"Large hulls start farther offshore");
        check(!ShorelineSearch.Candidates(origin,100).Any(),"Oversized ships cannot extend the shore search without bound");
        check(!ShorelineSearch.Candidates(origin,double.NaN).Any()&&!ShorelineSearch.Candidates(origin,-1).Any(),"Invalid hull size yields no landing");
        var caller=new Point(0,0);var bow=new Point(10,0);
        check(ShorelineSearch.ShoreAccess(caller,bow,30,p=>p.X<3 ? 31 : 27),"Open shoreline with a short swim permits approach validation");
        check(!ShorelineSearch.ShoreAccess(caller,bow,30,p=>31),"Dry land is never accepted as a landing");
        check(!ShorelineSearch.ShoreAccess(caller,new Point(24,0),30,p=>p.X<3 ? 31 : 27),"Long swim between caller and bow is rejected");
        check(!ShorelineSearch.ShoreAccess(caller,bow,30,p=>p.X<2 || (p.X>5&&p.X<7) ? 31 : 27),"Intervening island or land strip is rejected");
        check(!ShorelineSearch.ShoreAccess(caller,bow,30,p=>p.X>5 ? null : 31),"Unloaded or unavailable terrain is rejected");
        check(!ShorelineSearch.ShoreAccess(caller,bow,30,p=>double.NaN),"Invalid terrain height is rejected");
        check(!ShorelineSearch.ShoreAccess(caller,bow,30,p=>p.X<2 ? 45 : 27),"Cliff-top calls cannot pass shoreline access");
        check(ShorelineSearch.ShoreAccess(caller,bow,30,p=>27),"A separately validated caller on a low pier can start above water");
        check(!ShorelineSearch.ShoreAccess(caller,new Point(100,0),30,p=>27),"Distant shoreline samples are rejected");
    }
}
