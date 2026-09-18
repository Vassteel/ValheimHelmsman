using System;
using System.Linq;
using Helmsman.Core;

internal static class ShipConstructionTests
{
    internal static void Run(Action<bool,string> check)
    {
        var roster=ShipConstruction.Blueprints;
        check(roster.Count==10,"Nine authored models and native longship");
        check(roster.Select(b=>b.Prefab).Distinct().Count()==roster.Count,"No ship registry collisions");
        check(roster.All(b=>!b.Source.Contains("Auto")),"Autonomous enemy ships remain excluded");
        check(roster.Count(b=>!b.HasSail)==3,"Dugout, solo and tandem paddle craft");
        foreach(var b in roster)
        {
            check(ShipConstruction.ValidDuration(b.BuildSeconds),b.Name+" has a bounded non-instant duration");
            check(ShipwrightRules.Costs(b.Recipe).Values.All(n=>n>0),b.Name+" requires materials");
            check(ShipConstruction.Task(b.HasSail,0,b.BuildSeconds)==ShipwrightTask.Hammer,b.Name+" starts work before launch");
            check(ShipConstruction.Task(b.HasSail,b.BuildSeconds,b.BuildSeconds)==ShipwrightTask.AwaitingLaunch,b.Name+" waits for clearance when completed");
        }
        long start=TimeSpan.FromDays(12).Ticks;
        check(ShipConstruction.Elapsed(start,start+TimeSpan.FromSeconds(90).Ticks,120)==90,"Saved timestamp resumes without resetting work");
        check(ShipConstruction.Elapsed(start,start+TimeSpan.FromDays(2).Ticks,120)==120,"Unloaded build caps at completion");
        check(ShipConstruction.Elapsed(start,start-1,120)==0,"Clock rollback never creates negative progress");
        check(ShipConstruction.Elapsed(0,long.MaxValue,120)==120,"Extreme timestamps cannot overflow elapsed math");
        foreach(var invalid in new[]{double.NaN,double.PositiveInfinity,-1,0,29,86401})
            check(!ShipConstruction.ValidDuration(invalid),"Reject invalid duration "+invalid);
        for(int t=0;t<120;t++)
            check(ShipConstruction.Task(false,t,120)!=ShipwrightTask.Stitch,"Rowing boat skips sail stitching at "+t);
        check(ShipConstruction.Task(true,24,120)==ShipwrightTask.Stitch,"Sailing hull includes needlework");
        check(ShipConstruction.Task(true,36,120)==ShipwrightTask.Hammer,"Long builds cycle through work tools");
        check(roster.First().BuildSeconds<roster.Last().BuildSeconds,"Largest cargo ship takes longer than a canoe");
    }
}
