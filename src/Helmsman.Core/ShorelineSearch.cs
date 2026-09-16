using System;
using System.Collections.Generic;

namespace Helmsman.Core;

public readonly struct ShoreCandidate
{
    public readonly Point Center;
    public readonly double Heading;
    public ShoreCandidate(Point center,double heading){Center=center;Heading=heading;}
}

public static class ShorelineSearch
{
    public const double MaximumRadius=48,MaximumSwimGap=12;
    // Closest rings first. An individual ship's actual clearance is checked by the game adapter.
    public static IEnumerable<ShoreCandidate> Candidates(Point caller,double length)
    {
        if(!Finite(caller.X)||!Finite(caller.Y)||!Finite(length)||length<=0)yield break;
        for(double radius=Math.Max(8,length*.5+3);radius<=MaximumRadius;radius+=4)
        for(int angle=0;angle<24;angle++)
        {
            double a=angle*Math.PI/12;
            yield return new ShoreCandidate(new Point(caller.X+Math.Sin(a)*radius,caller.Y+Math.Cos(a)*radius),angle*15+180);
        }
    }
    // Reject water separated from the caller by another strip of land, long swims,
    // unloaded terrain and cliffs. A caller standing on a pier may start above water.
    public static bool ShoreAccess(Point caller,Point bow,double sea,Func<Point,double?> height)
    {
        if(!Finite(caller.X)||!Finite(caller.Y)||!Finite(bow.X)||!Finite(bow.Y)||!Finite(sea))return false;
        double distance=caller.Distance(bow);
        if(distance>MaximumRadius || distance<1)return false;
        int steps=(int)Math.Ceiling(distance);double lastLand=0;bool enteredWater=false;
        for(int i=0;i<=steps;i++)
        {
            double t=(double)i/steps;
            var h=height(new Point(caller.X+(bow.X-caller.X)*t,caller.Y+(bow.Y-caller.Y)*t));
            if(!h.HasValue||!Finite(h.Value)||h.Value>sea+8)return false;
            if(h.Value<sea-.25)enteredWater=true;
            else {if(enteredWater)return false;lastLand=distance*t;}
        }
        return enteredWater && distance-lastLand<=MaximumSwimGap;
    }
    private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
}
