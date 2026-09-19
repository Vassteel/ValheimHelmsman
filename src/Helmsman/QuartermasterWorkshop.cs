using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace Helmsman;

// Optional reflection boundary: use Quartermaster's live hub registry, Deposit flag
// and configured three-dimensional base radius, rather than inventing a second zone.
internal static class QuartermasterWorkshop
{
    private static object? Value(Type t,string field)=>AccessTools.Field(t,field)?.GetValue(null);
    private static object? Config(Type t,string field){var c=Value(t,field);return c?.GetType().GetProperty("Value")?.GetValue(c);}
    internal static bool Available=>Chainloader.PluginInfos.TryGetValue("local.valheim.quartermaster",out var p)&&p.Instance&&p.Instance.isActiveAndEnabled&&Config(p.Instance.GetType(),"Enabled") is true;
    internal static System.Collections.Generic.List<Container> Supplies(Player player,Vector3 point)
    {
        var result=new System.Collections.Generic.List<Container>();
        if(!player||player!=Player.m_localPlayer||!Available)return result;
        try
        {
            var plugin=Chainloader.PluginInfos["local.valheim.quartermaster"].Instance.GetType();
            if(!(Config(plugin,"CraftFromContainers") is true)||!(Config(plugin,"CraftRange") is float radius))return result;
            var registry=plugin.Assembly.GetType("Quartermaster.ContainerRegistry");if(registry==null)return result;
            var nearby=AccessTools.Method(registry,"Nearby")?.Invoke(null,new object?[]{point,radius,false,false,null}) as IEnumerable;
            var settings=AccessTools.Method(registry,"GetSettings");
            if(nearby!=null)foreach(var entry in nearby)
                if(entry is Container chest&&chest)
                {
                    var config=settings?.Invoke(null,new object[]{chest});
                    if(config!=null&&AccessTools.Field(config.GetType(),"CraftingSupply")?.GetValue(config) is true && SharesZone(player,point,chest.transform.position))result.Add(chest);
                }
        }
        catch(Exception error){Plugin.Instance.Record("Quartermaster ship supplies unavailable: "+error.GetBaseException().Message);}
        return result;
    }
    internal static bool UsableSupply(Container chest)
    {
        if(!Available||!chest)return false;
        try
        {
            var registry=Chainloader.PluginInfos["local.valheim.quartermaster"].Instance.GetType().Assembly.GetType("Quartermaster.ContainerRegistry");
            return registry!=null&&AccessTools.Method(registry,"IsUsable")?.Invoke(null,new object[]{chest,false}) is true;
        }
        catch{return false;}
    }
    internal static bool SharesZone(Player player,params Vector3[] points)
    {
        if(!player||!Available)return false;
        try
        {
            var plugin=Chainloader.PluginInfos["local.valheim.quartermaster"].Instance.GetType();
            if(!(Config(plugin,"Range") is float radius))return false;
            var registry=plugin.Assembly.GetType("Quartermaster.ContainerRegistry");if(registry==null)return false;
            if(!(Value(registry,"All") is IEnumerable hubs))return false;
            var settings=AccessTools.Method(registry,"GetSettings");var access=AccessTools.Method(typeof(Container),"CheckAccess");
            foreach(var entry in hubs)
            {
                if(!(entry is Container hub)||!hub)continue;
                var view=hub.m_rootObjectOverride?hub.m_rootObjectOverride:hub.GetComponent<ZNetView>();if(!view||!view.IsValid())continue;
                var config=settings?.Invoke(null,new object[]{hub});if(config==null||!(AccessTools.Field(config.GetType(),"Deposit")?.GetValue(config) is true))continue;
                if(!(access?.Invoke(hub,new object[]{player.GetPlayerID()}) is true))continue;
                bool inside=true;foreach(var point in points){var d=hub.transform.position-point;if(!Helmsman.Core.WorkshopCoverage.Contains(radius,d.x,d.y,d.z)){inside=false;break;}}
                if(inside)return true;
            }
        }
        catch(Exception error){Plugin.Instance.Record("Quartermaster workshop coverage unavailable: "+error.GetBaseException().Message);}
        return false;
    }
}
