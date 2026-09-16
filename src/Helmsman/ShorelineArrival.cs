using System.Collections.Generic;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

internal static class ShorelineArrival
{
    internal static bool CallerReady(Player player,out string reason)
    {
        if(!player || player.IsDead() || player.IsSwimming() || player.GetStandingOnShip() ||
            Mathf.Abs(player.transform.position.y-WaterChart.Sea)>8)
        {reason="Stand near the shoreline or on a low pier to call your ship.";return false;}
        reason="";return true;
    }
    internal static IEnumerable<Berth> Candidates(Vector3 origin,ShipProfile profile,string shipType)
    {
        foreach(var candidate in ShorelineSearch.Candidates(WaterChart.Point(origin),profile.Length))
        {
            var rotation=Quaternion.Euler(0,(float)candidate.Heading,0);
            yield return new Berth {name="Whistle landing",configured=true,shipType=shipType,
                position=WaterChart.Vector(candidate.Center)-rotation*profile.Center,heading=(float)candidate.Heading};
        }
    }
    internal static bool Validate(Vector3 origin,Berth berth,WaterChart chart,out string reason)
    {
        var rotation=Quaternion.Euler(0,berth.heading,0);
        var bow=berth.position+rotation*(chart.Profile.Center+Vector3.forward*(chart.Profile.Length*.5f));
        double? Height(Point point)
        {
            var p=WaterChart.Vector(point);
            return ZoneSystem.instance && ZoneSystem.instance.IsZoneLoaded(p) && Heightmap.GetHeight(p,out var height) ? height : (double?)null;
        }
        if(!ShorelineSearch.ShoreAccess(WaterChart.Point(origin),WaterChart.Point(bow),WaterChart.Sea,Height))
        {reason="No accessible shoreline close enough to the bow.";return false;}
        // Shore arrivals always check buildings, rocks, other boats, hull depth and mast clearance.
        if(!chart.ValidateArrival(berth,out reason,false))return false;
        var leadIn=berth.Approach-berth.Forward*20;
        if(!chart.HullSegment(leadIn,berth.Approach,rotation,true,out reason))return false;
        return chart.TurningArea(leadIn,out reason);
    }
}
