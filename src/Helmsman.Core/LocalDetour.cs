using System;
using System.Collections.Generic;

namespace Helmsman.Core;

// Repair the next part of a route at ship-sized resolution. Keep the distant
// course so a newly loaded rock does not restart an entire ocean crossing.
public sealed class LocalDetour
{
    public readonly int RejoinIndex;
    public readonly RouteSearch Search;
    private LocalDetour(int index,RouteSearch search){RejoinIndex=index;Search=search;}
    public static LocalDetour? Create(Point start,IReadOnlyList<Point> route,int waypoint,Func<Point,Point,bool> clear)
    {
        if(waypoint<0||waypoint>=route.Count)return null;
        int join=waypoint;
        while(join<route.Count-1&&start.Distance(route[join])<48)join++;
        if(start.Distance(route[join])>160)return null;
        return new LocalDetour(join,new RouteSearch(start,route[join],
            (a,b)=>start.Distance(a)<=192&&start.Distance(b)<=192&&clear(a,b),6,4096));
    }
    public List<Point> Splice(IReadOnlyList<Point> previous)
    {
        if(Search.State!=SearchState.Found)throw new InvalidOperationException("No clear local detour.");
        var result=new List<Point>(Search.Route);
        for(int i=RejoinIndex+1;i<previous.Count;i++)result.Add(previous[i]);
        return result;
    }
}
