using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

internal sealed class ShipProfile
{
    internal float Width=4,Length=10,AirHeight=12,Margin=.2f;
    internal float Draft=1;
    internal float TurnRadius=>Mathf.Max(16,Length*1.6f);
    internal Vector3 Center,MastCenter;
    internal static string PrefabName(Ship ship)=>ship.name.Replace("(Clone)","").Trim();
    internal static bool CanSail(Ship ship)=>ship && ShipRules.CanSail(PrefabName(ship),ship.m_sailObject,ship.m_sailForceFactor,ship.m_hasSail);
    internal static int SeatCount(Ship ship)
    {
        var seats=new List<Vector3>();
        void Add(Transform point,string animation)
        {
            if(!point || !ShipRules.IsSeat(animation))return;
            var position=ship.transform.InverseTransformPoint(point.position);
            if(!seats.Any(p=>(p-position).sqrMagnitude<.0625f))seats.Add(position);
        }
        foreach(var chair in ship.GetComponentsInChildren<Chair>(true))
            if(chair.m_inShip && chair.GetComponentInParent<Ship>()==ship)Add(chair.m_attachPoint,chair.m_attachAnimation);
        var helm=ship.m_shipControlls;
        if(helm)Add(helm.m_attachPoint,helm.m_attachAnimation);
        return seats.Count;
    }
    internal static bool Supports(Ship? ship)=>ship && ship.m_floatCollider && ship.m_shipControlls &&
        ship.GetComponent<Rigidbody>() && ship.m_backwardForce>0 &&
        ship.GetType().GetMethod(nameof(Ship.CustomFixedUpdate),new[]{typeof(float)})?.DeclaringType==typeof(Ship) &&
        ShipRules.Eligible(CanSail(ship),CanSail(ship) ? 0 : SeatCount(ship));
    internal static ShipProfile For(Ship? ship)
    {
        var p=new ShipProfile {Draft=Plugin.Instance.MinimumWaterDepth.Value};
        if(!ship || PrefabName(ship)=="Karve")return p;
        bool sailing=CanSail(ship);
        if(!sailing)p.AirHeight=3;
        var box=ship.m_floatCollider;
        if(box)
        {
            var bounds=new Bounds(ship.transform.InverseTransformPoint(box.transform.TransformPoint(box.center)),Vector3.zero);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                bounds.Encapsulate(ship.transform.InverseTransformPoint(box.transform.TransformPoint(box.center+Vector3.Scale(box.size,new Vector3(x,y,z))*.5f)));
            p.Width=Mathf.Max(2,bounds.size.x);p.Length=Mathf.Max(4,bounds.size.z);p.Center=new Vector3(bounds.center.x,0,bounds.center.z);
            p.Draft=Mathf.Max(p.Draft,Mathf.Min(3,bounds.size.y*.5f));
        }
        if(sailing && ship.m_mastObject)
        {
            p.MastCenter=ship.transform.InverseTransformPoint(ship.m_mastObject.transform.position);p.MastCenter.y=0;
            foreach(var renderer in ship.m_mastObject.GetComponentsInChildren<Renderer>())
                p.AirHeight=Mathf.Max(p.AirHeight,ship.transform.InverseTransformPoint(renderer.bounds.max).y);
        }
        if(sailing)p.AirHeight=Mathf.Max(p.AirHeight,ship.m_mastObject ? Mathf.Max(8,ship.transform.InverseTransformPoint(ship.m_mastObject.transform.position).y+8) : 4);
        var final=ship.GetComponent<FinalShipPresentation>();if(final)p.AirHeight=final.AirHeight;
        if(PrefabName(ship)=="VikingShip") {p.Width=Mathf.Max(p.Width,6);p.Length=Mathf.Max(p.Length,22);p.AirHeight=Mathf.Max(p.AirHeight,20);p.Draft=Mathf.Max(p.Draft,1.5f);}
        return p;
    }
}

internal sealed class ShipRecord
{
    internal ZDOID Id;
    internal string Name="",Prefab="";
    internal Vector3 Position;
    internal Ship? Loaded {get {var go=ZNetScene.instance ? ZNetScene.instance.FindInstance(Id) : null;return go ? go.GetComponent<Ship>() : null;}}
}

public sealed class ShipDirectory : MonoBehaviour
{
    internal static readonly int NameKey="helmsman_ship_name_v1".GetStableHashCode();
    internal List<ShipRecord> Records=new List<ShipRecord>();
    internal readonly List<GameObject> Prefabs=new List<GameObject>();
    private string catalogSummary="";
    internal static GameObject? FindPrefab(string name)=>ZNetScene.instance ? ZNetScene.instance.GetPrefab(name) :
        Jotunn.Managers.PrefabManager.Instance.GetPrefab(name);
    private void RefreshPrefabs()
    {
        var scene=ZNetScene.instance;
        // The name registry also contains mods registered after the scene's original prefab list.
        var candidates=scene.GetPrefabNames().Select(scene.GetPrefab).Concat(scene.m_prefabs)
            .Where(p=>p && p.GetComponent<Ship>()).GroupBy(p=>p.name).Select(g=>g.First()).OrderBy(p=>p.name).ToArray();
        Prefabs.Clear();Prefabs.AddRange(candidates.Where(p=>ShipProfile.Supports(p.GetComponent<Ship>())));
        var summary="Detected ships: "+string.Join(", ",Prefabs.Select(p=>p.name))+". Excluded: "+
            string.Join(", ",candidates.Except(Prefabs).Select(p=>p.name));
        if(summary!=catalogSummary){catalogSummary=summary;Plugin.Instance.Record(summary);}
    }
    internal static string SavedName(ZDO data)=>ShipText.CleanName(data.GetString(NameKey,data.GetString("shipName","")));
    internal static string Display(Ship ship)
    {
        var view=ship.GetComponent<ZNetView>();var name=view && view.IsValid() ? SavedName(view.GetZDO()) : "";
        return name.Length>0 ? name : ShipProfile.PrefabName(ship)=="VikingShip" ? "Longship" : ShipProfile.PrefabName(ship);
    }
    internal static bool Rename(Ship ship,string name,out string reason)
    {
        name=Helmsman.Core.ShipText.CleanName(name);
        if(name.Length==0){reason="Enter a ship name (up to 48 characters).";return false;}
        if(!Plugin.LocalSession || !ship || !Player.m_localPlayer || Vector3.Distance(ship.transform.position,Player.m_localPlayer.transform.position)>30)
        {reason="Stay near the ship.";return false;}
        var view=ship.GetComponent<ZNetView>();if(!view || !view.IsValid()){reason="Ship data is not ready.";return false;}
        if(!PrivateArea.CheckAccess(ship.transform.position,0,false,true)){reason="This ship is protected by a ward.";return false;}
        if(!view.IsOwner()){reason="Take the helm first to name this ship.";return false;}
        view.GetZDO().Set(NameKey,name);reason="Ship named "+name+".";return true;
    }
    private IEnumerator Start()
    {
        while(true)
        {
            if(!Plugin.LocalSession || !ZNetScene.instance || ZDOMan.instance==null)
            {Records.Clear();Prefabs.Clear();catalogSummary="";yield return new WaitForSeconds(1);continue;}
            long world=ZNet.instance.GetWorldUID();
            RefreshPrefabs();
            var next=new List<ShipRecord>();
            foreach(var prefab in Prefabs.ToArray())
            {
                var found=new List<ZDO>();int index=0;
                while(ZDOMan.instance!=null && !ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab.name,found,ref index))yield return null;
                if(!Plugin.LocalSession || ZNet.instance.GetWorldUID()!=world)break;
                foreach(var zdo in found)
                {
                    var name=SavedName(zdo);
                    if(name.Length>0)next.Add(new ShipRecord {Id=zdo.m_uid,Name=name,Prefab=prefab.name,Position=zdo.GetPosition()});
                }
                yield return null;
            }
            if(Plugin.LocalSession && ZNet.instance.GetWorldUID()==world)Records=next;
            yield return new WaitForSeconds(3);
        }
    }
}

[HarmonyPatch(typeof(Chair),nameof(Chair.GetHoverText))]
internal static class NameAtMastHover
{
    internal static void Postfix(Chair __instance,ref string __result)
    {
        var ship=__instance.GetComponentInParent<Ship>();
        if(MastInteraction.IsMast(__instance))__result=Localization.instance.Localize("Mast\n[<color=yellow><b>$KEY_Use</b></color>] Hold fast\n[<color=yellow>Hold $KEY_Use</color>] Call the gull");
        if(ShipProfile.Supports(ship))__result+=ShipNameInteraction.Hint(ship);
    }
}
[HarmonyPatch(typeof(Chair),nameof(Chair.Interact))]
internal static class NameAtMastUse
{
    internal static bool Prefix(Chair __instance,Humanoid __0,bool __1,bool __2,ref bool __result)
    {
        if(!MastInteraction.Intercept(__instance,__0,__1,__2,ref __result))return false;
        return ShipNameInteraction.Intercept(__instance.GetComponentInParent<Ship>(),__0,__1,__2,ref __result);
    }
}
[HarmonyPatch(typeof(ShipControlls),nameof(ShipControlls.GetHoverText))]
internal static class NameAtHelmHover
{
    internal static void Postfix(ShipControlls __instance,ref string __result)
    {if(ShipProfile.Supports(__instance.m_ship))__result+=ShipNameInteraction.Hint(__instance.m_ship);}
}
[HarmonyPatch(typeof(ShipControlls),nameof(ShipControlls.Interact))]
internal static class NameAtHelmUse
{
    internal static bool Prefix(ShipControlls __instance,Humanoid character,bool repeat,bool alt,ref bool __result)=>
        ShipNameInteraction.Intercept(__instance.m_ship,character,repeat,alt,ref __result);
}
internal static class ShipNameInteraction
{
    internal static string Hint(Ship ship)=>Localization.instance.Localize("\n"+ShipDirectory.Display(ship)+"\n[<color=yellow>Shift + $KEY_Use</color>] Name ship");
    internal static bool Intercept(Ship? ship,Humanoid user,bool hold,bool alt,ref bool result)
    {
        if(!alt || hold || user!=Player.m_localPlayer || !ShipProfile.Supports(ship!))return true;
        if(Vector3.Distance(user.transform.position,ship!.transform.position)>30)return true;
        Plugin.Instance.UI.OpenShipName(ship);result=true;return false;
    }
}
