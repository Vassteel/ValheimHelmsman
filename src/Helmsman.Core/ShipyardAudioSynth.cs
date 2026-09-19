using System;
namespace Helmsman.Core;
public enum YardSound { PuffinMurmur, PuffinGreeting, Hammer, Scrape, Rope, Creak, Splash, Footstep, WoodSlide }
// Original procedural first-pass sounds; no recordings or external runtime dependencies.
public static class ShipyardAudioSynth
{
    public const int Rate=22050;
    public static float[] Create(YardSound sound,int variant=0)
    {
        double duration=sound==YardSound.WoodSlide?4.2:sound==YardSound.Splash?1.8:sound==YardSound.Creak?2.4:sound==YardSound.PuffinMurmur?1.1:sound==YardSound.PuffinGreeting?.52:sound==YardSound.Rope?.8:sound==YardSound.Scrape?.65:.19;
        var data=new float[(int)(Rate*duration)];var random=new Random(2311+(int)sound*911+variant*1777);double phase=0,low=0,grain=0,resonance=0;
        for(int i=0;i<data.Length;i++)
        {
            double t=(double)i/Rate,u=t/duration,noise=random.NextDouble()*2-1;low=.86*low+.14*noise;
            double envelope=Math.Pow(Math.Sin(Math.PI*u),.8),sample=0;
            switch(sound)
            {
                case YardSound.PuffinMurmur:case YardSound.PuffinGreeting:
                    phase+=Math.PI*2*(sound==YardSound.PuffinGreeting?260-75*u:150+22*Math.Sin(t*11))/Rate;
                    sample=(Math.Sin(phase)*.20+Math.Sin(phase*2)*.11+low*.3)*(.6+.4*Math.Sin(t*37));break;
                case YardSound.Hammer:sample=(Math.Sin(t*2*Math.PI*190)*.50+noise*.17)*Math.Exp(-t*25);envelope=Math.Min(1,t/.003);break;
                case YardSound.Footstep:sample=low*.45*Math.Exp(-t*23);envelope=Math.Min(1,t/.004);break;
                case YardSound.Scrape:sample=(noise-low)*.18*(.65+.35*Math.Sin(t*83));break;
                case YardSound.Rope:sample=low*.38+noise*.06*(.5+.5*Math.Sin(t*49));break;
                case YardSound.Creak:
                    // Irregular stick/slip tension, dry crackles and damped timber
                    // resonances instead of a sustained, pitched electronic squeak.
                    grain=.992*grain+.008*noise;
                    double pressure=.35+.65*Math.Pow(Math.Sin(t*5.2+variant),2);
                    double crack=random.NextDouble()<.003*pressure?(random.NextDouble()*2-1)*.5:0;
                    resonance=.91*resonance+crack;
                    phase+=Math.PI*2*(67+grain*420+27*Math.Sin(t*3.8+Math.Sin(t*13)))/Rate;
                    sample=(low*.42+resonance*.65+Math.Sin(phase)*.075+Math.Sin(phase*2.73)*.04)*pressure;break;
                case YardSound.WoodSlide:
                    grain=.998*grain+.002*noise;
                    double rasp=.50+.24*Math.Sin(t*19+Math.Sin(t*7))+.16*Math.Sin(t*43);
                    double bump=Math.Pow(Math.Max(0,Math.Sin(t*11.7+.6*Math.Sin(t*3))),9);
                    sample=low*(.62+.3*bump)+(noise-low)*.08*rasp+grain*.28;
                    envelope=Math.Min(1,t/.035)*Math.Min(1,(duration-t)/.035);break;
                case YardSound.Splash:sample=low*.65+noise*.16;envelope=Math.Pow(Math.Sin(Math.PI*u),.55)*Math.Exp(-u*1.8);break;
            }
            // Raised-cosine tail eliminates residual clicks for impact clips too.
            data[i]=(float)(sample*envelope*Math.Min(1,(1-u)*30));
        }
        return data;
    }
}
