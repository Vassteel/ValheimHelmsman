using System;
using Helmsman.Core;
internal static class PuffinPerformanceTests
{
    internal static void Run(Action<bool,string> check)
    {
        foreach(bool sails in new[]{false,true})
        {
            bool bounded=true,groundedTools=true,continuous=true;int hops=0;bool airborne=false;bool hammer=false,chisel=false,needle=false;
            var previous=PuffinPerformance.Sample(sails,0);
            for(int i=0;i<=7200;i++)
            {
                var p=PuffinPerformance.Sample(sails,i*.01);
                bounded &= float.IsFinite(p.X+p.Hop+p.Pitch+p.HeadYaw+p.Crouch) && p.X>=-.481f && p.X<=.001f && Math.Abs(p.Z)<.1f && p.Hop>=0 && p.Hop<=.221f && p.Crouch>=0 && p.Crouch<.131f;
                groundedTools &= p.Hop<.001f || p.Tool==0;
                continuous &= Math.Abs(p.X-previous.X)<.02f && Math.Abs(p.Hop-previous.Hop)<.02f && Math.Abs(p.Yaw-previous.Yaw)<2;
                if(p.Hop>.001f&&!airborne)hops++;
                airborne=p.Hop>.001f;
                hammer |= p.Tool==1;chisel |= p.Tool==2;needle |= p.Tool==3;
                previous=p;
            }
            check(bounded,"Puffin gestures stay finite and inside the clear bench area (sails="+sails+")");
            check(groundedTools,"Puffin stows tools during hops (sails="+sails+")");
            check(continuous,"Puffin hop position and heading remain continuous across job/cycle boundaries (sails="+sails+")");
            check(hops==12,"Each of six work sessions has outbound and return hops (sails="+sails+")");
            check(hammer&&chisel&&needle==sails,"Puffin only stitches sail-equipped ships (sails="+sails+")");
        }
        var crouch=PuffinPerformance.Sample(true,.25);var flight=PuffinPerformance.Sample(true,.775);var land=PuffinPerformance.Sample(true,1.3);
        check(crouch.Crouch>.05f&&crouch.Hop==0&&flight.Hop>.2f&&land.Hop==0&&land.Crouch>.04f,"Hops include anticipation, airborne travel and a landing settle");
    }
}
