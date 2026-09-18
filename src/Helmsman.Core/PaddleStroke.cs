using System;
namespace Helmsman.Core;

// Unit-length shaft axis and grip midpoint, in seated player's right/up/forward
// coordinates. Smooth periodic strokes, shared by clients through world time.
public readonly struct PaddleStroke
{
    public readonly double X,Y,Z,AxisX,AxisY,AxisZ,Twist,Lean;
    private PaddleStroke(double x,double y,double z,double ax,double ay,double az,double twist,double lean)
    {X=x;Y=y;Z=z;double n=Math.Sqrt(ax*ax+ay*ay+az*az);AxisX=ax/n;AxisY=ay/n;AxisZ=az/n;Twist=twist;Lean=lean;}
    public static PaddleStroke Sample(double seconds,bool doubleBlade,bool reverse)
    {
        if(double.IsNaN(seconds)||double.IsInfinity(seconds))seconds=0;
        double t=(seconds%(doubleBlade?1.8:1.65))/(doubleBlade?1.8:1.65)*Math.PI*2;
        if(reverse)t=-t;
        double s=Math.Sin(t),c=Math.Cos(t);
        if(doubleBlade)
            return new PaddleStroke(.06*s,.805+.055*Math.Cos(2*t),.30+.055*Math.Cos(2*t),1,-.76*s,.46*c,12*c,3+5*Math.Cos(2*t));
        // Single blade is authored on the negative end of the Unity shaft axis.
        // Pull beside the left gunwale, then lift the blade for its recovery.
        double recovery=(1-c)*.5;
        return new PaddleStroke(-.22,.67+.35*recovery,.20+.15*s,.64,.95-.35*recovery,.08*s,9*s,6*c);
    }
}
