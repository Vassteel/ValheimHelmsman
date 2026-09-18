using System;
using Helmsman.Core;
internal static class PaddleStrokeTests
{
    internal static void Run(Action<bool,string> check)
    {
        foreach(bool kayak in new[]{false,true})
        {
            double period=kayak?1.8:1.65;bool leftWet=false,rightWet=false,recovery=false;double worstReach=0;
            for(int i=0;i<=360;i++)
            {
                double t=period*i/360;var p=PaddleStroke.Sample(t,kayak,false);var repeat=PaddleStroke.Sample(t+period,kayak,false);var reverse=PaddleStroke.Sample(-t,kayak,true);
                check(Math.Abs(p.AxisX*p.AxisX+p.AxisY*p.AxisY+p.AxisZ*p.AxisZ-1)<1e-9,"Rigid paddle shaft length");
                check(Math.Abs(p.X-repeat.X)+Math.Abs(p.Y-repeat.Y)+Math.Abs(p.Z-repeat.Z)<1e-8,"Stroke loop has no jump");
                check(Math.Abs(p.Z-reverse.Z)+Math.Abs(p.AxisY-reverse.AxisY)<1e-8,"Reverse retraces the stroke");
                double bladeY=p.Y+.259-1.23*p.AxisY;
                leftWet|=bladeY<.3;rightWet|=p.Y+.259+1.23*p.AxisY<.3;recovery|=bladeY>.31;
                // Measured native player arm chain is approximately 0.61 m.
                foreach(int side in new[]{-1,1})
                {
                    double x=p.X+side*.31*p.AxisX-side*.25,y=p.Y+side*.31*p.AxisY-.77,z=p.Z+side*.31*p.AxisZ;
                    worstReach=Math.Max(worstReach,Math.Sqrt(x*x+y*y+z*z));
                }
            }
            check(leftWet&&recovery,"Single blade enters water and clears on recovery");
            if(kayak)check(rightWet,"Kayak alternates both blades in water");
            check(worstReach<.61,"Both grips remain within native player arm reach");
        }
        foreach(double invalid in new[]{double.NaN,double.NegativeInfinity,double.PositiveInfinity})
        {
            var p=PaddleStroke.Sample(invalid,true,false);check(!double.IsNaN(p.AxisX),"Corrupt presentation time cannot distort the skeleton");
        }
    }
}
