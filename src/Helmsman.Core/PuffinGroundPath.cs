#nullable disable
using System;
using System.Collections.Generic;

namespace Helmsman.Core;

// A bounded, incremental ground search. Each edge requires both foot support
// and swept body clearance; height comes from the actual floor, not a flat grid.
public sealed class PuffinGroundPath
{
    public PuffinFlightPath.Result State { get; private set; }
    public readonly List<PuffinPoint> Points=new List<PuffinPoint>();
    public int Expanded { get; private set; }
    private readonly PuffinPoint start,goal;
    private readonly Func<PuffinPoint,PuffinPoint?> floor;
    private readonly Func<PuffinPoint,PuffinPoint,bool> clear;
    private readonly Dictionary<(int,int,int),Node> nodes=new Dictionary<(int,int,int),Node>();
    private readonly List<Node> open=new List<Node>();
    private const float Cell=.6f;
    private sealed class Node { public PuffinPoint Point;public float Cost,Score;public Node Parent;public bool Closed; }
    public PuffinGroundPath(PuffinPoint start,PuffinPoint goal,Func<PuffinPoint,PuffinPoint?> floor,Func<PuffinPoint,PuffinPoint,bool> clear)
    {
        this.start=start;this.goal=goal;this.floor=floor;this.clear=clear;
        if(float.IsNaN(start.X+start.Y+start.Z+goal.X+goal.Y+goal.Z)||
            float.IsInfinity(start.X+start.Y+start.Z+goal.X+goal.Y+goal.Z)||PuffinPoint.Distance(start,goal)>210||
            !clear(start,start)||!clear(goal,goal)){State=PuffinFlightPath.Result.Unreachable;return;}
        if(clear(start,goal)){Points.Add(start);Points.Add(goal);State=PuffinFlightPath.Result.Found;return;}
        var first=new Node{Point=start,Score=PuffinPoint.Distance(start,goal)};nodes[Key(start)]=first;open.Add(first);
    }
    private (int,int,int) Key(PuffinPoint p)=>((int)Math.Round((p.X-start.X)/Cell),(int)Math.Round(p.Y/.25f),(int)Math.Round((p.Z-start.Z)/Cell));
    public void Step(int budget)
    {
        if(State!=PuffinFlightPath.Result.Searching)return;
        for(int i=0;i<budget&&open.Count>0;i++)
        {
            int best=0;for(int j=1;j<open.Count;j++)if(open[j].Score<open[best].Score)best=j;
            var current=open[best];open.RemoveAt(best);current.Closed=true;Expanded++;
            if(PuffinPoint.Distance(current.Point,goal)<Cell*2&&clear(current.Point,goal))
            {
                Points.Add(goal);for(var n=current;n!=null;n=n.Parent)Points.Add(n.Point);Points.Reverse();State=PuffinFlightPath.Result.Found;return;
            }
            if(Expanded>=1024){State=PuffinFlightPath.Result.Unreachable;return;}
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                if(x==0&&z==0)continue;
                var guess=new PuffinPoint(current.Point.X+x*Cell,current.Point.Y,current.Point.Z+z*Cell);
                if(guess.X<Math.Min(start.X,goal.X)-9||guess.X>Math.Max(start.X,goal.X)+9||
                    guess.Z<Math.Min(start.Z,goal.Z)-9||guess.Z>Math.Max(start.Z,goal.Z)+9)continue;
                var surface=floor(guess);if(!surface.HasValue)continue;var p=surface.Value;
                if(!clear(current.Point,p))continue;
                var key=Key(p);nodes.TryGetValue(key,out var next);
                float cost=current.Cost+PuffinPoint.Distance(current.Point,p);
                if(next!=null&&(next.Closed||cost>=next.Cost))continue;
                if(next==null){next=new Node{Point=p};nodes.Add(key,next);open.Add(next);}
                next.Cost=cost;next.Score=cost+PuffinPoint.Distance(p,goal);next.Parent=current;
            }
        }
        if(open.Count==0)State=PuffinFlightPath.Result.Unreachable;
    }
}
