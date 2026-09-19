using System;
namespace Helmsman.Core;
// Segment versus inflated box, including endpoints: preview ships have no physics
// colliders, but construction workers still need to fly around the visible hull.
public static class PuffinCrewSpace
{
    public static bool Clear(PuffinPoint a,PuffinPoint b,PuffinPoint min,PuffinPoint max,float padding=.20f)
    {
        float enter=0,exit=1;
        bool Slab(float p,float q,float lo,float hi)
        {
            lo-=padding;hi+=padding;float d=q-p;
            if(Math.Abs(d)<.000001f)return p>=lo&&p<=hi;
            float t0=(lo-p)/d,t1=(hi-p)/d;if(t0>t1){float t=t0;t0=t1;t1=t;}
            enter=Math.Max(enter,t0);exit=Math.Min(exit,t1);return enter<=exit;
        }
        return !(Slab(a.X,b.X,min.X,max.X)&&Slab(a.Y,b.Y,min.Y,max.Y)&&Slab(a.Z,b.Z,min.Z,max.Z));
    }
}
