using System;
namespace Helmsman.Core;
public enum PuffinCrewJob { Hammer, Shave, Haul, Timber, Supplies }
// Stable per-order variation keeps clients from all choosing the same departure.
public static class PuffinCrewPlan
{
    public const int Count=6;
    // Unobserved/dedicated-server orders simulate the same gathering phase.
    // Nearby owners wait for real landings, never just an elapsed timer.
    public const int UnobservedArrivalSeconds=20;
    public static bool WaitingForCrew(long called,long begun)=>called>0&&begun==0;
    public static bool CanBeginConstruction(long called,long begun,long now,bool observed,bool assembled)
        =>WaitingForCrew(called,begun)&&now>=called&&(observed?assembled:(double)now-called>=UnobservedArrivalSeconds*(double)TimeSpan.TicksPerSecond);
    public static double ConstructionElapsed(long called,long begun,long commissioned,long now,double duration)
        =>WaitingForCrew(called,begun)?0:ShipConstruction.Elapsed(called>0?begun:commissioned,now,duration);
    public static PuffinCrewJob Job(int index)=>index==0||index==3?PuffinCrewJob.Hammer:index==1?PuffinCrewJob.Shave:index==2?PuffinCrewJob.Haul:index==4?PuffinCrewJob.Timber:PuffinCrewJob.Supplies;
    public static float Variation(long seed,int index)
    {unchecked{uint v=(uint)seed^(uint)(seed>>32)^(uint)(index+1)*2654435761u;v^=v>>16;v*=2246822519u;v^=v>>13;return (v&0xFFFFFF)/16777216f;}}
    public static float ArrivalDelay(long seed,int index)=>index*.65f+Variation(seed,index)*.8f;
    public static PuffinPoint Departure(long seed,int index)
    {
        double angle=(index+(Variation(seed,index)-.5)*.55)*Math.PI*2/Count+Variation(seed,17)*Math.PI*2;
        return new PuffinPoint((float)Math.Cos(angle),.32f+Variation(seed,index+31)*.20f,(float)Math.Sin(angle));
    }
    public static PuffinWorkPose Work(PuffinCrewJob job,double seconds)
    {
        if(job==PuffinCrewJob.Hammer){var p=PuffinPerformance.Station(1,seconds);p.Pitch*=.65f;p.BodyPitch*=.25f;p.Crouch*=.2f;return p;}
        float beat=(float)(Math.Sin(seconds*Math.PI*2/(job==PuffinCrewJob.Shave?2.2:2.7))*.5+.5);
        if(job==PuffinCrewJob.Shave)return new PuffinWorkPose{Tool=5,Pitch=42-beat*18,BodyPitch=5+beat*10,Wing=18};
        if(job==PuffinCrewJob.Haul)return new PuffinWorkPose{Pitch=10-beat*17,BodyPitch=-beat*17,Wing=25,Crouch=beat*.045f};
        return new PuffinWorkPose{Tool=job==PuffinCrewJob.Timber?6:7,Pitch=-8,BodyPitch=5,Wing=22};
    }
}
