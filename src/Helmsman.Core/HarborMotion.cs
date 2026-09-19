using System;
namespace Helmsman.Core;

public static class HarborMotion
{
    public const double Duration=12;
    public static double Elapsed(long started,long now)=>started<=0||now<=started?0:Math.Min(Duration,((double)now-started)/TimeSpan.TicksPerSecond);
    public static float Travel(double seconds)
    {
        if(double.IsNaN(seconds)||seconds<=0||seconds>=10)return 0;
        double t=seconds<4?seconds/4:seconds<=6?1:(10-seconds)/4;
        return (float)(t*t*(3-2*t));
    }
    public static bool Busy(long started,long now)=>started>0&&now>=started&&((double)now-started)/TimeSpan.TicksPerSecond<Duration;
}
