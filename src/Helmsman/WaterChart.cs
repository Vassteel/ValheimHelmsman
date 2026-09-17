using System.Collections.Generic;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

internal sealed class WaterChart
{
    private readonly Dictionary<(int,int), bool> chart = new Dictionary<(int,int), bool>();
    private readonly Ship? ignoredShip;
    internal readonly ShipProfile Profile;
    internal Collider? BlockingCollider {get;private set;}
    internal bool PlanRockClearing;
    private static int Mask => LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
    internal WaterChart(Ship? ignoredShip = null, ShipProfile? profile = null) { this.ignoredShip = ignoredShip; Profile=profile ?? ShipProfile.For(ignoredShip); }
    internal static float Sea => ZoneSystem.instance ? ZoneSystem.instance.m_waterLevel : 30;
    internal static Vector3 AtSea(Vector3 p) => new Vector3(p.x, Sea, p.z);
    internal static Point Point(Vector3 p) => new Point(p.x,p.z);
    internal static Vector3 Vector(Point p) => new Vector3((float)p.X,Sea,(float)p.Y);

    internal bool RegionalSegment(Point a, Point b)
    {
        int steps = Mathf.Max(1,Mathf.CeilToInt((float)a.Distance(b)/4));
        for (int i=0;i<=steps;i++)
        {
            var p = Vector3.Lerp(Vector(a),Vector(b),(float)i/steps);
            if (!RegionalPoint(p)) return false;
        }
        // Loaded objects refine the otherwise approximate regional chart.
        var from = Vector(a); var to = Vector(b);
        if (ZoneSystem.instance.IsZoneLoaded(from) && ZoneSystem.instance.IsZoneLoaded(to))
            return HullSegment(from,to,Quaternion.LookRotation((to-from).sqrMagnitude > .01f ? to-from : Vector3.forward),false,out _,allowClearableRocks:PlanRockClearing);
        return true;
    }

    private bool RegionalPoint(Vector3 p)
    {
        var key = (Mathf.RoundToInt(p.x/2),Mathf.RoundToInt(p.z/2));
        if (chart.TryGetValue(key,out var okay)) return okay;
        p.x=key.Item1*2; p.z=key.Item2*2;
        okay = Depth(p,false,out _);
        for (int i=0;okay && i<8;i++)
        {
            float a = i*Mathf.PI/4;
            okay = Depth(p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Mathf.Max(3,Profile.Width*.6f),false,out _);
        }
        chart[key]=okay; return okay;
    }

    private bool Depth(Vector3 p, bool requireLoaded, out string reason)
    {
        if (new Vector2(p.x,p.z).magnitude > 9800)
        {reason="Too close to the world edge for navigation.";return false;}
        if (WorldGenerator.instance == null || !ZoneSystem.instance)
        {reason="World terrain is not available to check yet.";return false;}
        bool loaded = ZoneSystem.instance.IsZoneLoaded(p);
        if (requireLoaded && !loaded)
        {reason="Part of the approach or departure area is not loaded yet.";return false;}
        float height;
        if (!loaded || !Heightmap.GetHeight(p,out height))
        {
            if (requireLoaded)
            {reason="Terrain height is not available to check yet.";return false;}
            height = WorldGenerator.instance.GetHeight(p);
        }
        float depth=Sea-height;
        if(depth<Profile.Draft)
        {reason="Water depth "+depth.ToString("0.0")+" m; navigation minimum "+Profile.Draft.ToString("0.0")+" m.";return false;}
        reason="Water depth clear.";return true;
    }

    internal bool HullSegment(Vector3 from, Vector3 to, Quaternion orientation, bool requireLoaded, out string reason, bool allowDockPieces = false,bool allowClearableRocks = false)
    {
        BlockingCollider=null;
        from = AtSea(from)+orientation*Profile.Center; to = AtSea(to)+orientation*Profile.Center;
        int steps = Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/2));
        for (int i=0;i<=steps;i++)
        {
            var p = Vector3.Lerp(from,to,(float)i/steps);
            for (int side=-1;side<=1;side++) for (int end=-1;end<=1;end++)
            {
                var sample=p+orientation*new Vector3(side*(Profile.Width/2+Profile.Margin),0,end*Profile.Length/2);
                if (!Depth(sample,requireLoaded,out reason)) return false;
            }
        }
        // The hull is broad only near water level. A full-width box up to the mast
        // falsely hits dock decking/roof edges beside otherwise open water.
        var hullHalf=new Vector3(Profile.Width/2+Profile.Margin,
            (1.5f+Profile.Draft)/2,Profile.Length/2+Profile.Margin);
        if(!Sweep(from,to,Vector3.up*((1.5f-Profile.Draft)/2),hullHalf,orientation,allowDockPieces,allowClearableRocks,out reason))return false;
        var mastHalf=new Vector3(.35f,Profile.AirHeight/2,.35f);
        if(!Sweep(from,to,orientation*(Profile.MastCenter-Profile.Center)+Vector3.up*(Profile.AirHeight/2),mastHalf,orientation,allowDockPieces,allowClearableRocks,out reason))return false;
        reason="Clear for the current ship hull and mast estimate."; return true;
    }

    private bool Sweep(Vector3 from,Vector3 to,Vector3 offset,Vector3 half,Quaternion orientation,
        bool allowDockPieces,bool allowClearableRocks,out string reason)
    {
        foreach(var collider in Physics.OverlapBox(from+offset,half,orientation,Mask,QueryTriggerInteraction.Ignore))
            if(Obstacle(collider,allowDockPieces,allowClearableRocks))
            {BlockingCollider=collider;reason="Clearance blocked by "+Describe(collider)+".";return false;}
        var delta=to-from;
        if(delta.sqrMagnitude>.01f)
            foreach(var hit in Physics.BoxCastAll(from+offset,half,delta.normalized,orientation,delta.magnitude,Mask,QueryTriggerInteraction.Ignore))
                if(Obstacle(hit.collider,allowDockPieces,allowClearableRocks))
                {BlockingCollider=hit.collider;reason="Passage blocked by "+Describe(hit.collider)+".";return false;}
        reason="Clear.";return true;
    }

    private bool Obstacle(Collider collider,bool allowDockPieces,bool allowClearableRocks)
    {
        if(!collider || (ignoredShip && collider.transform.IsChildOf(ignoredShip.transform)) ||
            collider.GetComponentInParent<Character>() || collider.GetComponentInParent<Fish>() ||
            collider.GetComponentInParent<GullGuide>())return false;
        // Ships can also be build pieces; never discard another boat as a dock decoration.
        if(collider.GetComponentInParent<Ship>())return true;
        // Loose resources left by clearing are not fixed route obstructions.
        if(collider.GetComponentInParent<ItemDrop>()&&!collider.GetComponentInParent<Piece>())return false;
        if(allowClearableRocks&&RockClearing.CanPlanThrough(collider))return false;
        return !(allowDockPieces && collider.GetComponentInParent<Piece>());
    }

    private static string Describe(Collider collider)
    {
        var piece=collider.GetComponentInParent<Piece>();
        return (piece ? piece.name : collider.transform.root.name)+" / "+collider.name;
    }

    internal bool ValidateArrival(Berth berth,out string reason,bool allowDockPieces)
    {
        if(!berth.ValidData){reason="Invalid berth settings.";return false;}
        return HullSegment(berth.Approach,berth.position,Quaternion.Euler(0,berth.heading,0),true,out reason,allowDockPieces);
    }

    internal bool TurningArea(Vector3 center, out string reason)
    {
        // A full disk deliberately rejects tight turns until maneuvering has been calibrated.
        for (int angle=0;angle<360;angle+=30)
        {
            var rot=Quaternion.Euler(0,angle,0);
            var p=center+rot*Vector3.forward*(Profile.TurnRadius-Profile.Length/2);
            if (!HullSegment(center,p,rot,true,out reason)) return false;
        }
        reason="Turning area clear."; return true;
    }

    internal bool ValidateBerth(Berth berth,out string reason)
    {
        if (!berth.ValidData) {reason="Invalid berth settings.";return false;}
        var rotation=Quaternion.Euler(0,berth.heading,0);
        if (!HullSegment(berth.Approach,berth.position,rotation,true,out reason)) return false;
        if (!HullSegment(berth.position,berth.Exit,rotation,true,out reason)) return false;
        if (!TurningArea(berth.Exit,out reason)) {reason="Departure turning area: "+reason;return false;}
        if (!TurningArea(berth.Approach,out reason)) {reason="Arrival alignment area: "+reason;return false;}
        reason="Arrival, berth and departure clear for this ship.";return true;
    }
}
