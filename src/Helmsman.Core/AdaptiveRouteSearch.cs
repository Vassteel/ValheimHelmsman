using System;
using System.Collections.Generic;

namespace Helmsman.Core;

/// <summary>Retry a missed river channel at finer spacing with the same edge clearance.</summary>
public sealed class AdaptiveRouteSearch
{
    private RouteSearch search;
    private readonly Point start,goal;
    private readonly Func<Point,Point,bool> clear;
    private int regionalExpanded;
    public bool Fine {get;private set;}
    public SearchState State=>search.State;
    public List<Point> Route=>search.Route;
    public int Expanded=>regionalExpanded+search.Expanded;
    public AdaptiveRouteSearch(Point start,Point goal,Func<Point,Point,bool> clear)
    {this.start=start;this.goal=goal;this.clear=clear;search=new RouteSearch(start,goal,clear);}
    public void Step(int budget=8)
    {
        search.Step(budget);
        if(!Fine&&search.State==SearchState.NoRoute)
        {
            regionalExpanded=search.Expanded;Fine=true;
            search=new RouteSearch(start,goal,clear,spacing:6);
        }
    }
}
