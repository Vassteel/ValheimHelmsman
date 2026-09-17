using System;
using System.Collections.Generic;
using System.Linq;

namespace Helmsman.Core;

public readonly struct IslandCell : IEquatable<IslandCell>
{
    public readonly int X,Z;
    public IslandCell(int x,int z){X=x;Z=z;}
    public bool Equals(IslandCell other)=>X==other.X&&Z==other.Z;
    public override bool Equals(object? obj)=>obj is IslandCell c&&Equals(c);
    public override int GetHashCode()=>unchecked(X*397^Z);
}
public enum SurveyState {SearchingShore,Surveying,Complete,Failed}

// Pure incremental terrain survey. Edge samples keep narrow sea channels from
// joining islands; diagonal corner contact never counts as connected land.
public sealed class IslandSurvey
{
    public const int CellSize=8,WorldRadius=9800,MaximumCells=1000000;
    private readonly Func<float,float,float> height;
    private readonly float sea;
    private readonly int maximum;
    private readonly Queue<IslandCell> seeds,frontier=new();
    private readonly HashSet<IslandCell> examined=new();
    private readonly HashSet<IslandCell> land=new();
    private IslandCell? expanding;
    private int side;
    private static readonly int[] Dx={-1,1,0,0},Dz={0,0,-1,1};
    public SurveyState State {get;private set;}=SurveyState.SearchingShore;
    public string Error {get;private set;}="";
    public IReadOnlyCollection<IslandCell> Cells=>land;
    public IslandSurvey(float originX,float originZ,float sea,Func<float,float,float> height,int maximum=MaximumCells)
    {
        if(!Finite(originX)||!Finite(originZ)||!Finite(sea)||maximum<1)throw new ArgumentException("Invalid survey origin or budget.");
        this.height=height;this.sea=sea;this.maximum=maximum;
        int x=(int)Math.Round(originX/CellSize),z=(int)Math.Round(originZ/CellSize);
        var candidates=new List<IslandCell>();
        for(int a=-16;a<=16;a++)for(int b=-16;b<=16;b++)
            if(a*a+b*b<=256)candidates.Add(new IslandCell(x+a,z+b));
        seeds=new Queue<IslandCell>(candidates.OrderBy(c=>Distance(c,originX,originZ)));
    }
    private static double Distance(IslandCell c,float x,float z)=>Math.Pow(c.X*CellSize-x,2)+Math.Pow(c.Z*CellSize-z,2);
    private bool IsLand(float x,float z)
    {float h=height(x,z);if(!Finite(h))throw new InvalidOperationException("Terrain height is unavailable.");return h>sea+.05f;}
    private static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
    private bool Inside(IslandCell c)=>(double)c.X*c.X+(double)c.Z*c.Z<(double)WorldRadius*WorldRadius/(CellSize*CellSize);
    private void Fail(string reason){Error=reason;State=SurveyState.Failed;frontier.Clear();}
    public void Step(int budget)
    {
        if(budget<1)return;
        try
        {
            while(budget-->0 && State!=SurveyState.Complete && State!=SurveyState.Failed)
            {
                if(State==SurveyState.SearchingShore)
                {
                    if(seeds.Count==0){Fail("No island shoreline within 128 m of this dock.");return;}
                    var seed=seeds.Dequeue();if(!Inside(seed))continue;
                    if(!IsLand(seed.X*CellSize,seed.Z*CellSize))continue;
                    land.Add(seed);examined.Add(seed);frontier.Enqueue(seed);State=SurveyState.Surveying;
                    continue;
                }
                if(!expanding.HasValue)
                {
                    if(frontier.Count==0){State=SurveyState.Complete;return;}
                    expanding=frontier.Dequeue();side=0;
                }
                var cell=expanding.Value;var next=new IslandCell(cell.X+Dx[side],cell.Z+Dz[side]);
                if(++side==4)expanding=null;
                if(land.Contains(next))continue;
                if(!Inside(next)){Fail("This landmass reaches the edge of the surveyable world.");return;}
                // An edge can fail from one neighbour but succeed from another.
                // Only cache water cells, not land behind an impassable edge.
                if(examined.Contains(next))continue;
                if(!IsLand(next.X*CellSize,next.Z*CellSize)){examined.Add(next);continue;}
                bool connected=true;
                for(int sample=1;sample<=3;sample++)
                    if(!IsLand((cell.X+(next.X-cell.X)*sample*.25f)*CellSize,(cell.Z+(next.Z-cell.Z)*sample*.25f)*CellSize)){connected=false;break;}
                if(!connected)continue;
                if(land.Count>=maximum){Fail("This landmass is too large to survey in one report.");return;}
                land.Add(next);frontier.Enqueue(next);
            }
        }
        catch(Exception error){Fail("Survey paused: "+error.Message);}
    }
    public bool Contains(float x,float z)=>land.Contains(new IslandCell((int)Math.Round(x/CellSize),(int)Math.Round(z/CellSize)));
}

public static class ScoutingPoi
{
    public static string Label(string prefab)
    {
        string n=(prefab??"").ToLowerInvariant();
        if(n.Contains("hildir")||n.Contains("boss")||n.Contains("altar")||n.Contains("starttemple"))return "";
        if(n.Contains("sunkencrypt"))return "Sunken crypt";
        if(n.Contains("crypt"))return "Burial chamber";
        if(n.Contains("trollcave"))return "Troll cave";
        if(n.Contains("mountaincave"))return "Frost cave";
        if(n.Contains("dvergrtownentrance")||n.Contains("infestedmine"))return "Infested mine";
        if(n.Contains("goblincamp"))return "Fuling village";
        if(n.Contains("draugrvillage"))return "Draugr village";
        if(n.Contains("charredfortress")||n.Contains("fortress"))return "Fortress";
        if(n.Contains("dvergr") && (n.Contains("tower")||n.Contains("harbour")||n.Contains("town")))return "Dvergr outpost";
        if(n.Contains("ruin")||n.Contains("stonetower")||n.Contains("woodhouse"))return "Ruins";
        return "";
    }
}
