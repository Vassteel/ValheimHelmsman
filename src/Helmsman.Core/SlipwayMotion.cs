using System;
namespace Helmsman.Core;
// Absolute-time presentation: reconnects and ownership changes sample the same pose.
public static class SlipwayMotion
{
    public const float LaunchSeconds=10,ResetSeconds=6;
    public static float Clamp(double t)=>(float)Math.Max(0,Math.Min(1,double.IsNaN(t)?0:t));
    public static float Ease(double t){float x=Clamp(t);return x*x*(3-2*x);}
    // Brake release, accelerating slide, then a gentle water entry.
    public static float Travel(double seconds)
    {
        float t=Clamp((seconds-1.4)/(LaunchSeconds-1.4));
        return t*t*(2-t);
    }
    public static float Level(double seconds)=>Ease((Travel(seconds)-.48)/.52);
    public static float Cradle(double seconds,float launchDistance=23)=>Math.Min(11,Travel(seconds)*Math.Max(0,launchDistance));
    public static float Reset(double seconds)=>1-Ease(seconds/ResetSeconds);
    public static double LaunchElapsed(long now,long started,long paused,long pauseTicks)
        =>started<=0?0:Math.Max(0,((double)(paused>0?paused:now)-started-Math.Max(0,pauseTicks))/TimeSpan.TicksPerSecond);
    public static float FloatOffset(float waterOffset,float massCenter,float forceDistance,float force,float gravity)
        =>waterOffset-massCenter-Math.Abs(gravity)*forceDistance/(50*Math.Max(.01f,force));
    public static float PartStart(string name,float height)
    {
        name=(name??"").ToLowerInvariant();
        // Explicit authored groups precede material-name hints: deck timber can
        // share a material with the mast without sharing its construction step.
        if(name.StartsWith("deck ")||name=="deck")return .50f;
        if(name.StartsWith("mast ")||name=="mast")return .66f;
        if(name.StartsWith("rigging ")||name=="rigging")return .78f;
        if(name.StartsWith("sail ")||name=="sail")return .90f;
        if(name.StartsWith("cargo ")||name=="cargo"||name.StartsWith("decoration "))return .96f;
        if(name.StartsWith("fixed ")||name=="fixed")return .40f;
        if(name.Contains("sail"))return .90f;
        if(name.Contains("rope")||name.Contains("rigging")||name.Contains("shroud"))return .78f;
        if(name.Contains("cargo")||name.Contains("barrel")||name.Contains("sack")||name.Contains("shield")||name.Contains("decoration"))return .96f;
        if(name.Contains("mast")||name.Contains("yard"))return .66f;
        if(name.Contains("deck")||name.Contains("bench")||name.Contains("seat"))return .50f;
        return .05f+.48f*Clamp(height);
    }
}
