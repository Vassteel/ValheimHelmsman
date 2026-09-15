using System;
using System.Collections.Generic;

namespace Helmsman.Core;

public readonly struct Point
{
    public readonly double X, Y;
    public Point(double x, double y) { X = x; Y = y; }
    public double Distance(Point b) => Math.Sqrt((X-b.X)*(X-b.X)+(Y-b.Y)*(Y-b.Y));
}

public enum SearchState { Searching, Found, NoRoute, BudgetExceeded }

/// <summary>Incremental A*; every edge, including goal connection, must pass clearance checks.</summary>
public sealed class RouteSearch
{
    private sealed class Node
    {
        public int X, Y;
        public double Cost;
        public Node? Parent;
        public bool Closed;
    }
    private readonly struct Entry
    {
        public readonly Node Node;
        public readonly double Priority, Cost;
        public Entry(Node node, double priority) { Node = node; Priority = priority; Cost = node.Cost; }
    }
    private readonly List<Entry> heap = new List<Entry>();
    private readonly Dictionary<(int,int), Node> nodes = new Dictionary<(int,int), Node>();
    private readonly Point start, goal;
    private readonly double spacing;
    private readonly int maxExpanded;
    private readonly Func<Point,Point,bool> clear;
    public SearchState State { get; private set; } = SearchState.Searching;
    public List<Point> Route { get; } = new List<Point>();
    public int Expanded { get; private set; }

    public RouteSearch(Point start, Point goal, Func<Point,Point,bool> clear, double spacing = 24, int maxExpanded = 20000)
    {
        if (spacing <= 0 || maxExpanded <= 0) throw new ArgumentOutOfRangeException();
        this.start = start; this.goal = goal; this.clear = clear;
        this.spacing = spacing; this.maxExpanded = maxExpanded;
        var first = new Node(); nodes[(0,0)] = first; Push(first, start.Distance(goal));
    }

    public void Step(int budget = 8)
    {
        while (State == SearchState.Searching && budget-- > 0)
        {
            if (heap.Count == 0) { State = SearchState.NoRoute; break; }
            var entry = Pop(); var n = entry.Node;
            if (n.Closed || entry.Cost != n.Cost) continue;
            if (++Expanded > maxExpanded) { State = SearchState.BudgetExceeded; break; }
            n.Closed = true;
            var p = Position(n);
            if (p.Distance(goal) <= spacing * 1.5 && clear(p, goal))
            {
                Route.Add(goal);
                for (Node? at = n; at != null; at = at.Parent) Route.Add(Position(at));
                Route.Reverse(); State = SearchState.Found; break;
            }
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                var key = (n.X+x, n.Y+y);
                if (nodes.TryGetValue(key, out var next) && next.Closed) continue;
                var q = new Point(start.X+key.Item1*spacing, start.Y+key.Item2*spacing);
                var cost = n.Cost + p.Distance(q);
                if (next != null && cost >= next.Cost) continue;
                if (!clear(p,q)) continue;
                if (next == null) { next = new Node { X=key.Item1,Y=key.Item2 }; nodes[key] = next; }
                next.Cost = cost; next.Parent = n;
                Push(next, cost + q.Distance(goal));
            }
        }
    }
    private Point Position(Node n) => new Point(start.X+n.X*spacing,start.Y+n.Y*spacing);
    private void Push(Node node, double priority)
    {
        var entry = new Entry(node,priority); int i = heap.Count; heap.Add(entry);
        while (i > 0)
        {
            int p = (i-1)/2; if (heap[p].Priority <= priority) break;
            heap[i] = heap[p]; i = p;
        }
        heap[i] = entry;
    }
    private Entry Pop()
    {
        var first = heap[0]; var last = heap[heap.Count-1]; heap.RemoveAt(heap.Count-1);
        if (heap.Count == 0) return first;
        int i = 0;
        while (i*2+1 < heap.Count)
        {
            int child = i*2+1;
            if (child+1 < heap.Count && heap[child+1].Priority < heap[child].Priority) child++;
            if (last.Priority <= heap[child].Priority) break;
            heap[i] = heap[child]; i = child;
        }
        heap[i] = last; return first;
    }
}
