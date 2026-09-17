using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

internal static class ScoutMap
{
    private static readonly Func<Minimap,int,int,bool> Explore=AccessTools.MethodDelegate<Func<Minimap,int,int,bool>>(
        AccessTools.Method(typeof(Minimap),"Explore",new[]{typeof(int),typeof(int)}));
    private static readonly AccessTools.FieldRef<Minimap,Texture2D> Fog=AccessTools.FieldRefAccess<Minimap,Texture2D>("m_fogTexture");
    private static readonly AccessTools.FieldRef<Minimap,List<Minimap.PinData>> Pins=AccessTools.FieldRefAccess<Minimap,List<Minimap.PinData>>("m_pins");
    internal static IEnumerator Apply(ScoutReport report,Func<bool> current)
    {
        var map=Minimap.instance;if(!map||!Game.instance)throw new InvalidOperationException("Player map is not ready.");
        int size=map.m_textureSize;float pixel=map.m_pixelSize,half=size/2;
        if(size<1||pixel<=0)throw new InvalidOperationException("Player map dimensions are invalid.");
        int count=0;
        foreach(var cell in report.Cells)
        {
            if(!current()||Minimap.instance!=map)throw new InvalidOperationException("Map owner changed during report collection.");
            float x=cell.X*IslandSurvey.CellSize,z=cell.Z*IslandSurvey.CellSize,r=IslandSurvey.CellSize*.5f;
            int minX=Mathf.Max(0,Mathf.CeilToInt((x-r)/pixel+half)),maxX=Mathf.Min(size-1,Mathf.CeilToInt((x+r)/pixel+half)-1);
            int minZ=Mathf.Max(0,Mathf.CeilToInt((z-r)/pixel+half)),maxZ=Mathf.Min(size-1,Mathf.CeilToInt((z+r)/pixel+half)-1);
            for(int py=minZ;py<=maxZ;py++)for(int px=minX;px<=maxX;px++)Explore(map,px,py);
            if(++count%512==0){Fog(map).Apply();yield return null;}
        }
        Fog(map).Apply();
        foreach(var point in report.Points)
        {
            var position=new Vector3(point.X,0,point.Z);
            bool exists=false;
            foreach(var pin in Pins(map))
                if(pin.m_save&&pin.m_name==point.Name&&new Vector2(pin.m_pos.x-position.x,pin.m_pos.z-position.z).sqrMagnitude<144){exists=true;break;}
            if(!exists)map.AddPin(position,Minimap.PinType.Icon3,point.Name,true,false);
        }
        if(!current())throw new InvalidOperationException("Map owner changed before saving.");
        map.SaveMapData();
    }
}
