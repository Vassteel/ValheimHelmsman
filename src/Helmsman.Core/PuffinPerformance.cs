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
    // Station poses keep feet planted. Travel is handled by swept navigation.
    public static PuffinWorkPose Station(int tool,double seconds)
    {
        double fade=Smooth(seconds/.35),beat=Math.Max(0,seconds)%(tool==1?1.45:tool==2?2.1:tool==3?1.8:2.6);
        var p=new PuffinWorkPose{Tool=tool};
        if(tool==1)
        {
            double lift=Pulse(beat,.05,.52),strike=Pulse(beat,.62,.24),recoil=Pulse(beat,.86,.35);
            p.Pitch=(float)(-24*lift+43*strike-9*recoil);p.BodyPitch=(float)(8-9*lift+44*strike);p.Crouch=(float)(.065*strike);p.Wing=12;
        }
        else if(tool==2){double tap=Pulse(beat,.62,.24);p.Pitch=(float)(24+16*tap);p.BodyPitch=(float)(38+8*tap);p.HeadYaw=(float)Math.Sin(seconds*1.8)*8;p.Wing=10;}
        else if(tool==3){p.Pitch=23+12*(float)Math.Sin(beat*Math.PI*2/1.8);p.HeadYaw=22*(float)Math.Sin(beat*Math.PI*2/1.8);p.BodyPitch=10;p.Wing=20+6*(float)Math.Cos(beat*Math.PI*2/1.8);}
        else {p.Pitch=26+(float)Math.Sin(seconds*2.4)*9;p.HeadYaw=(float)Math.Sin(seconds*2.4)*22;p.BodyPitch=16;p.Wing=12;}
        p.Pitch*=(float)fade;p.BodyPitch*=(float)fade;p.Crouch*=(float)fade;p.HeadYaw*=(float)fade;p.Wing*=(float)fade;
        return p;
    }
    public static bool Strike(int tool,double before,double after)
    {
        double period=tool==1?1.45:tool==2?2.1:tool==3?1.8:2.6;
        // One cue per contact, never per rendered frame.
        return Math.Floor((before-.74)/period)!=Math.Floor((after-.74)/period)&&after>=.74;
    }
    private static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    private static double Pulse(double t,double start,double duration)
    {if(t<=start||t>=start+duration)return 0;double s=Math.Sin((t-start)/duration*Math.PI);return s*s;}
    private static double Arc(double t,double start,double duration)
    {if(t<=start||t>=start+duration)return 0;double u=(t-start)/duration;return 4*u*(1-u);}
}
