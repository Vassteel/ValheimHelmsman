using System;

namespace Helmsman.Core;

/// <summary>Keep a straight final berth corridor, but fit its length to the water and hull.</summary>
public static class DockApproach
{
    public static bool TrySelect(Point berth,Point forward,double shipLength,
        Func<Point,Point,bool> corridorClear,Func<Point,Point,bool> leadClear,
        out Point approach,out Point lead)
    {
        approach=lead=berth;
        double minimum=Math.Min(35,Math.Max(10,shipLength*1.25));
        double alignment=Math.Min(20,Math.Max(6,shipLength*.75));
        for(double distance=35;;distance=Math.Max(minimum,distance-6))
        {
            var entry=new Point(berth.X-forward.X*distance,berth.Y-forward.Y*distance);
            if(corridorClear(entry,berth))
                for(double run=20;;run=Math.Max(alignment,run-6))
                {
                    var before=new Point(entry.X-forward.X*run,entry.Y-forward.Y*run);
                    if(leadClear(before,entry)){approach=entry;lead=before;return true;}
                    if(run<=alignment)break;
                }
            if(distance<=minimum)return false;
        }
    }
}
