using System;

namespace Helmsman.Core;

public struct PuffinWorkPose
{
    public int Tool;
    public float X,Z,Hop,Yaw,Pitch,HeadYaw,Roll,BodyPitch,Crouch,Wing;
}

public static class PuffinPerformance
{
    // Each work session: anticipate, hop to a spot, settle, work, hop back,
    // then inspect. Tool changes happen with feet planted between sessions.
    public static PuffinWorkPose Sample(bool hasSail,double elapsed)
    {
        double t=Math.Max(0,elapsed)%36;
        int job=(int)(t/12);t%=12;
        int tool=job==0?1:job==1||!hasSail?2:3;
        double travel=Smooth((t-.45)/.65)*(1-Smooth((t-9.85)/.65));
        float hop=(float)(Arc(t,.45,.65)+Arc(t,9.85,.65))*.22f;
        var p=new PuffinWorkPose {
            X=(float)travel*(tool==1?-.22f:tool==2?-.48f:-.12f),Z=(float)travel*(tool==2?.09f:-.03f),Hop=hop,
            Yaw=(float)travel*(tool==1?-24:tool==2?18:-12),
            Crouch=(float)(Pulse(t,.05,.4)*.075+Pulse(t,1.1,.4)*.05+Pulse(t,9.45,.4)*.075+Pulse(t,10.5,.4)*.05),
            Wing=hop*125,Pitch=-hop*35,Roll=(float)(Math.Sin(t*Math.PI/6)*3)
        };
        if(t>=1.5 && t<9.4)
        {
            p.Tool=tool;
            double fade=Smooth((t-1.5)/.35)*Smooth((9.4-t)/.35);
            if(tool==1)
            {
                double beat=(t-1.5)%1.45;
                double windup=Pulse(beat,.05,.52),strike=Pulse(beat,.62,.24),recoil=Pulse(beat,.86,.35);
                p.Pitch+=(float)((-24*windup+43*strike-9*recoil)*fade);
                p.BodyPitch=(float)((8-9*windup+44*strike)*fade);
                p.Crouch+=(float)(.065*strike*fade);p.Wing+=(float)(12*fade);
            }
            else if(tool==2)
            {
                double beat=(t-1.5)%2.1;
                double tap=Pulse(beat,.25,.26)+Pulse(beat,.72,.24)+Pulse(beat,1.1,.22);
                p.Pitch+=(float)((24+16*tap)*fade);p.BodyPitch=(float)((38+8*tap)*fade);
                p.HeadYaw=(float)(8*Math.Sin(t*1.8)*fade);p.Crouch+=(float)(.025*tap*fade);p.Wing+=(float)(10*fade);
            }
            else
            {
                double stitch=(t-1.5)%1.8/1.8;
                p.Pitch+=(float)((23+12*Math.Sin(stitch*Math.PI*2))*fade);
                p.HeadYaw=(float)(22*Math.Sin(stitch*Math.PI*2)*fade);
                p.BodyPitch=(float)(10*fade);p.Wing+=(float)((20+6*Math.Cos(stitch*Math.PI*2))*fade);
            }
        }
        else p.HeadYaw=(float)(25*Pulse(t,11,.8));
        return p;
    }
    private static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    private static double Pulse(double t,double start,double duration)
    {if(t<=start||t>=start+duration)return 0;double s=Math.Sin((t-start)/duration*Math.PI);return s*s;}
    private static double Arc(double t,double start,double duration)
    {if(t<=start||t>=start+duration)return 0;double u=(t-start)/duration;return 4*u*(1-u);}
}
