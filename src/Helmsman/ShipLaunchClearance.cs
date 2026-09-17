using System.Collections.Generic;
using UnityEngine;

namespace Helmsman;

// Stationary launches need the actual ship solids, not the autopilot's padded
// rectangular navigation envelope. No live ship is instantiated for this check.
internal static class ShipLaunchClearance
{
    private static int Mask=>LayerMask.GetMask("Default","static_solid","Default_small","piece","terrain","vehicle");
    internal static bool Clear(Ship ship,Vector3 position,Quaternion rotation,out string reason)
    {
        position=WaterChart.AtSea(position);
        if(!ZoneSystem.instance || !ZoneSystem.instance.IsZoneLoaded(position) || !Heightmap.GetHeight(position,out var ground))
        {reason="Launch water is not loaded yet.";return false;}
        if(WaterChart.Sea-ground<.25f)
        {reason="Move the launch point into the water.";return false;}
        var root=ship.transform;
        var inverse=Quaternion.Inverse(root.rotation);
        var parts=new List<Collider>();
        var bounds=new Bounds();bool first=true;
        foreach(var part in ship.GetComponentsInChildren<Collider>(true))
        {
            if(!part.enabled || part.isTrigger || !ActiveBelow(part.transform,root))continue;
            if(!LocalBounds(part,out var local))
            {
                // Unknown collider kinds retain the conservative navigation check.
                return new WaterChart(null,ShipProfile.For(ship)).HullSegment(position,position,rotation,true,out reason);
            }
            parts.Add(part);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var corner=local.center+Vector3.Scale(local.extents,new Vector3(x,y,z));
                // Preserve nested scales while removing the prefab holder's pose.
                var point=inverse*(part.transform.TransformPoint(corner)-root.position);
                if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
            }
        }
        if(parts.Count==0){reason="The ship's launch collision model is not ready.";return false;}
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            if(!ZoneSystem.instance.IsZoneLoaded(position+rotation*(bounds.center+new Vector3(x*bounds.extents.x,0,z*bounds.extents.z))))
            {reason="Part of the launch area is not loaded yet.";return false;}
        var nearby=Physics.OverlapBox(position+rotation*bounds.center,bounds.extents,rotation,Mask,QueryTriggerInteraction.Ignore);
        foreach(var obstacle in nearby)
        {
            if(!obstacle || obstacle.transform.IsChildOf(root) || obstacle.GetComponentInParent<Character>())continue;
            foreach(var part in parts)
            {
                var partPosition=position+rotation*(inverse*(part.transform.position-root.position));
                var partRotation=rotation*inverse*part.transform.rotation;
                if(Physics.ComputePenetration(part,partPosition,partRotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out var depth) && depth>.02f)
                {reason=obstacle.GetComponentInParent<Heightmap>()?"The ship hull touches the shore here. Move the launch point slightly farther into the water.":"Launch blocked by "+obstacle.name+". Move the obstruction or launch point.";return false;}
            }
        }
        reason="Launch space clear.";return true;
    }
    private static bool ActiveBelow(Transform part,Transform root)
    {
        for(var node=part;node && node!=root;node=node.parent)
            if(!node.gameObject.activeSelf)return false;
        return true;
    }
    private static bool LocalBounds(Collider part,out Bounds bounds)
    {
        switch(part)
        {
            case BoxCollider box:bounds=new Bounds(box.center,box.size);return true;
            case SphereCollider sphere:bounds=new Bounds(sphere.center,Vector3.one*sphere.radius*2);return true;
            case CapsuleCollider capsule:
                var size=Vector3.one*capsule.radius*2;size[capsule.direction]=Mathf.Max(capsule.height,capsule.radius*2);
                bounds=new Bounds(capsule.center,size);return true;
            case MeshCollider mesh when mesh.convex && mesh.sharedMesh:
                bounds=mesh.sharedMesh.bounds;return true;
            default:bounds=default;return false;
        }
    }
}
