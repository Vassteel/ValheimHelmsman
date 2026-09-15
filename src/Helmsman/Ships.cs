using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Helmsman;

internal sealed class ShipProfile
{
    internal float Width=4,Length=10,AirHeight=12,Margin=.2f;
    internal float Draft=1;
    internal float TurnRadius=>Mathf.Max(16,Length*1.6f);
    internal Vector3 Center,MastCenter;
    internal static string PrefabName(Ship ship)=>ship.name.Replace("(Clone)","").Trim();
    internal static bool Supports(Ship ship)=>ship && ship.m_floatCollider && ship.m_shipControlls &&
        ship.GetComponent<Rigidbody>() && ship.m_backwardForce>0 &&
        ship.GetType().GetMethod(nameof(Ship.CustomFixedUpdate),new[]{typeof(float)})?.DeclaringType==typeof(Ship);
    internal static ShipProfile For(Ship? ship)
    {
        var p=new ShipProfile {Draft=Plugin.Instance.MinimumWaterDepth.Value};
        if(!ship || PrefabName(ship)=="Karve")return p;
        var box=ship.m_floatCollider;
        if(box)
        {
            var bounds=new Bounds(ship.transform.InverseTransformPoint(box.transform.TransformPoint(box.center)),Vector3.zero);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                bounds.Encapsulate(ship.transform.InverseTransformPoint(box.transform.TransformPoint(box.center+Vector3.Scale(box.size,new Vector3(x,y,z))*.5f)));
            p.Width=Mathf.Max(2,bounds.size.x);p.Length=Mathf.Max(4,bounds.size.z);p.Center=new Vector3(bounds.center.x,0,bounds.center.z);
            p.Draft=Mathf.Max(p.Draft,Mathf.Min(3,bounds.size.y*.5f));
        }
        if(ship.m_mastObject)
        {
            p.MastCenter=ship.transform.InverseTransformPoint(ship.m_mastObject.transform.position);p.MastCenter.y=0;
            foreach(var renderer in ship.m_mastObject.GetComponentsInChildren<Renderer>())
                p.AirHeight=Mathf.Max(p.AirHeight,ship.transform.InverseTransformPoint(renderer.bounds.max).y);
        }
        p.AirHeight=Mathf.Max(p.AirHeight,ship.m_mastObject ? Mathf.Max(8,ship.transform.InverseTransformPoint(ship.m_mastObject.transform.position).y+8) : 4);
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
    internal static string Display(Ship ship)
    {
        var view=ship.GetComponent<ZNetView>();var name=view && view.IsValid() ? view.GetZDO().GetString(NameKey,"") : "";
        return name.Length>0 ? name : ShipProfile.PrefabName(ship)=="VikingShip" ? "Longship" : ShipProfile.PrefabName(ship);
    }
    internal static bool Rename(Ship ship,string name,out string reason)
    {
        name=Helmsman.Core.ShipText.CleanName(name);
        if(name.Length==0){reason="Enter a ship name (up to 48 characters).";return false;}
        if(!Plugin.Solo || !ship || !Player.m_localPlayer || Vector3.Distance(ship.transform.position,Player.m_localPlayer.transform.position)>30)
        {reason="Stay near the ship in a solo world.";return false;}
        var view=ship.GetComponent<ZNetView>();if(!view || !view.IsValid()){reason="Ship data is not ready.";return false;}
        view.ClaimOwnership();if(!view.IsOwner()){reason="Could not claim ship ownership.";return false;}
        view.GetZDO().Set(NameKey,name);reason="Ship named "+name+".";return true;
    }
    private IEnumerator Start()
    {
        while(true)
        {
            if(!Plugin.Solo || !ZNetScene.instance || ZDOMan.instance==null)
            {Records.Clear();Prefabs.Clear();yield return new WaitForSeconds(1);continue;}
            long world=ZNet.instance.GetWorldUID();
            Prefabs.Clear();
            Prefabs.AddRange(ZNetScene.instance.m_prefabs.Where(p=>p && ShipProfile.Supports(p.GetComponent<Ship>())).OrderBy(p=>p.name));
            var next=new List<ShipRecord>();
            foreach(var prefab in Prefabs.ToArray())
            {
                var found=new List<ZDO>();int index=0;
                while(ZDOMan.instance!=null && !ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab.name,found,ref index))yield return null;
                if(!Plugin.Solo || ZNet.instance.GetWorldUID()!=world)break;
                foreach(var zdo in found)
                {
                    var name=zdo.GetString(NameKey,"");
                    if(name.Length>0)next.Add(new ShipRecord {Id=zdo.m_uid,Name=name,Prefab=prefab.name,Position=zdo.GetPosition()});
                }
                yield return null;
            }
            if(Plugin.Solo && ZNet.instance.GetWorldUID()==world)Records=next;
            yield return new WaitForSeconds(3);
        }
    }
}

[HarmonyPatch(typeof(Chair),nameof(Chair.GetHoverText))]
internal static class NameAtMastHover
{
    internal static void Postfix(Chair __instance,ref string __result)
    {var ship=__instance.GetComponentInParent<Ship>();if(ShipProfile.Supports(ship))__result+=ShipNameInteraction.Hint(ship);}
}
[HarmonyPatch(typeof(Chair),nameof(Chair.Interact))]
internal static class NameAtMastUse
{
    internal static bool Prefix(Chair __instance,Humanoid __0,bool __1,bool __2,ref bool __result)=>
        ShipNameInteraction.Intercept(__instance.GetComponentInParent<Ship>(),__0,__1,__2,ref __result);
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
