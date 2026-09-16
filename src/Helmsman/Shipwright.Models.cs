using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

public sealed partial class Shipwright
{
    private GameObject? fittings, canopy, lantern, trophyRoot;
    private readonly List<GameObject> supports=new();
    private readonly List<BoxCollider> roof=new();
    private readonly List<ShipwrightSkin> fixedSkins=new();
    private ShipwrightSkin? sailSkin, canopySkin;
    private string sailStyle="", canopyStyle="", trophyName="";
    private Transform sail=null!;
    private GameObject? trophyVisual;
    private bool ashReady, ashResist, burnable;
    private HitData.DamageModifiers originalDamage;

    private GameObject Own(string name,Transform parent)
    {var obj=new GameObject(name);obj.transform.SetParent(parent,false);ownedObjects.Add(obj);return obj;}
    private GameObject Part(Transform source,Transform parent)
    {
        if(!source)throw new InvalidOperationException("Missing vanilla ship fitting.");
        var part=Instantiate(source.gameObject,parent,false);part.name=source.name+" [Helmsman]";part.SetActive(true);return part;
    }
    private void Skin(GameObject obj,string texture)
    {if(ZNet.instance && ZNet.instance.IsDedicated())return;var skin=new ShipwrightSkin();fixedSkins.Add(skin);skin.Paint(obj,ShipwrightAssets.Texture(texture)!);}
    private void CreateParts()
    {
        var customize=transform.Find("ship/visual/Customize");
        if(!customize)throw new InvalidOperationException("Vanilla longship Customize models are missing.");
        fittings=Own("Helmsman fittings",customize.parent);fittings.SetActive(false);
        var parent=fittings.transform;parent.localPosition=customize.localPosition;parent.localRotation=customize.localRotation;parent.localScale=customize.localScale;
        canopy=Part(customize.Find("ShipTen2 (1)"),parent);canopy.transform.localPosition+=Vector3.up*.08f;
        supports.Add(Part(customize.Find("ShipTentHolders"),parent));
        supports.Add(Part(customize.Find("ShipTentHolders (1)"),parent));
        supports[0].transform.localPosition+=Vector3.up*.01f;
        supports[1].transform.localPosition+=new Vector3(.1f,-.18f,.11f);
        supports[1].transform.localEulerAngles+=new Vector3(0,5,6.6f);
        var beam=Part(customize.Find("ShipTen2_beam"),parent);beam.transform.localPosition+=new Vector3(.1f,.1f,0);supports.Add(beam);
        void Roof(string name,Vector3 position,Vector3 size,float angle)
        {
            var obj=Own(name,canopy.transform);obj.layer=LayerMask.NameToLayer("vehicle");obj.transform.localPosition=position;obj.transform.localRotation=Quaternion.Euler(0,0,angle);
            var box=obj.AddComponent<BoxCollider>();box.size=size;roof.Add(box);
        }
        Roof("Starboard roof",new Vector3(1.58f,1.18f,-.65f),new Vector3(1.9f,.04f,2.6f),-6);
        Roof("Port roof",new Vector3(-1.05f,.95f,-.65f),new Vector3(1,.04f,2.5f),23);
        Roof("Port outer roof",new Vector3(-2.1f,.7f,-.55f),new Vector3(1.15f,.04f,3),6);
        var lamp=ObjectDB.instance.GetItemPrefab("Lantern")?.transform.Find("attach/equiped");
        if(!lamp)throw new InvalidOperationException("Native lantern model is missing.");
        lantern=Part(lamp!,parent);lantern.transform.localScale=Vector3.one*.45f;lantern.transform.localPosition=new Vector3(.1035f,.855f,0);
        foreach(var joint in lantern.GetComponentsInChildren<Joint>(true))DestroyImmediate(joint);
        foreach(var collider in lantern.GetComponentsInChildren<Collider>(true))DestroyImmediate(collider);
        foreach(var rb in lantern.GetComponentsInChildren<Rigidbody>(true))DestroyImmediate(rb);
        foreach(var light in lantern.GetComponentsInChildren<Light>(true)){light.color=new Color(1,.72f,.4f);light.range=6;light.intensity=1.1f;}
        Skin(lantern,"lantern-weathered");
        sail=transform.Find("ship/visual/Mast/Karve_Sail/Karve_Sail");
        if(!sail)throw new InvalidOperationException("Native sail mesh is missing.");
        trophyRoot=Own("Bow trophy mount",transform);trophyRoot.transform.localPosition=new Vector3(0,2.4f,6.3f);
        var stand=ZNetScene.instance.GetPrefab("itemstand");
        if(!stand)throw new InvalidOperationException("Native trophy mount is missing.");
        // The mount itself uses the vanilla item's visible mesh; never clone its network view.
        foreach(var renderer in stand.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mesh=renderer.GetComponent<MeshFilter>();if(!mesh || !mesh.sharedMesh)continue;
            var plaque=Own("Trophy plaque",trophyRoot.transform);plaque.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;plaque.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            plaque.transform.localScale=Vector3.one*.6f;
        }
        ashReady=Ship.m_ashlandsReady;if(wear){ashResist=wear.m_ashDamageResist;burnable=wear.m_burnable;originalDamage=wear.m_damages;}
        fittings.SetActive(true);
    }
    private void Refresh()
    {
        if(!fittings)return;
        bool cover=Built("canopy") && !Data.GetBool(Key("canopy_hidden"));
        canopy!.SetActive(cover);foreach(var support in supports)support.SetActive(cover || Built("lantern"));
        lantern!.SetActive(Built("lantern") && !Data.GetBool(Key("lantern_off")));
        trophyRoot!.SetActive(Built("trophy"));
        Ship.m_ashlandsReady=ashReady || Built("treatment");
        if(wear)
        {
            wear.m_ashDamageResist=ashResist || Built("treatment");wear.m_burnable=burnable && !Built("treatment");
            wear.m_damages=originalDamage;
            if(Built("treatment"))wear.m_damages.Apply(new List<HitData.DamageModPair>{new(){m_type=HitData.DamageType.Fire,m_modifier=HitData.DamageModifier.VeryResistant}});
        }
        ShipwrightCargo.Resize(Ship.GetComponentInChildren<Container>());
        if(!ZNet.instance.IsDedicated())
        {
            var next=Data.GetString(Key("sails"),"Vanilla");
            if(next!=sailStyle){sailSkin?.Dispose();sailSkin=new ShipwrightSkin();var texture=ShipwrightAssets.Texture(next,"sails");if(texture)sailSkin.Paint(sail.gameObject,texture);sailStyle=next;}
            next=Data.GetString(Key("canopies"),"Sea flax");
            if(next!=canopyStyle){canopySkin?.Dispose();canopySkin=new ShipwrightSkin();var texture=ShipwrightAssets.Texture(next,"canopies");if(texture)canopySkin.Paint(canopy,texture);canopyStyle=next;}
        }
        string mounted=Data.GetString(Key("trophy_item"));
        if(mounted!=trophyName)
        {
            if(trophyVisual)Destroy(trophyVisual);trophyVisual=null;trophyName=mounted;
            var item=mounted.Length>0?ObjectDB.instance.GetItemPrefab(mounted):null;var attach=item?item.transform.Find("attach"):null;
            if(attach){trophyVisual=Part(attach,trophyRoot.transform);trophyVisual.transform.localPosition=Vector3.zero;trophyVisual.transform.localRotation=Quaternion.identity;trophyVisual.transform.localScale=Vector3.one*.65f;}
        }
    }
    internal bool Covers(Player player)
    {
        if(!Ready || !canopy || !canopy.activeInHierarchy || player.GetStandingOnShip()!=Ship)return false;
        var ray=new Ray(player.transform.position+Vector3.up*.25f,transform.up);
        return roof.Any(box=>box && box.Raycast(ray,out _,5));
    }
    internal bool Warm=>lantern && lantern.activeInHierarchy;
    private void Cleanup()
    {
        sailSkin?.Dispose();canopySkin?.Dispose();foreach(var skin in fixedSkins)skin.Dispose();fixedSkins.Clear();
        foreach(var obj in ownedObjects)if(obj)Destroy(obj);ownedObjects.Clear();
        if(Ship && visualsReady){Ship.m_ashlandsReady=ashReady;if(wear){wear.m_ashDamageResist=ashResist;wear.m_burnable=burnable;wear.m_damages=originalDamage;}}
        visualsReady=false;
    }
}

// Resize before native inventory deserialization so saved cargo in added columns is retained.
[HarmonyPatch]
internal static class ShipwrightCargo
{
    private static readonly AccessTools.FieldRef<Inventory,int> Width=AccessTools.FieldRefAccess<Inventory,int>("m_width");
    private static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
    {yield return AccessTools.Method(typeof(Container),"Awake");yield return AccessTools.Method(typeof(Container),"Load");}
    private static void Prefix(Container __instance)=>Resize(__instance);
    internal static void Resize(Container container)
    {
        if(!container || Shipwright.OtherMod)return;
        var ship=container.GetComponentInParent<Ship>();if(!Shipwright.Supports(ship))return;
        var view=ship.GetComponent<ZNetView>();if(!view || !view.IsValid())return;
        var data=view.GetZDO();
        bool first=data.GetBool(Shipwright.Key("cargo1")) || data.GetBool("ContainerUpgradedLvl1".GetStableHashCode());
        bool second=data.GetBool(Shipwright.Key("cargo2")) || data.GetBool("ContainerUpgradedLvl2".GetStableHashCode());
        if(!first && !second)return;
        var inv=container.GetInventory();var size=ShipwrightRules.Cargo(first,second,Math.Max(container.m_width,inv?.GetWidth()??0),Math.Max(container.m_height,inv?.GetHeight()??0));
        container.m_width=size.Width;container.m_height=size.Height;
        if(inv!=null){Width(inv)=size.Width;inv.SetHeight(size.Height);}
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.InShelter))]
internal static class ShipwrightShelter
{
    private static void Postfix(Player __instance,ref bool __result)
    {if(!__result && Shipwright.For(__instance.GetStandingOnShip()) is { } ship && ship.Covers(__instance))__result=true;}
}
[HarmonyPatch(typeof(Player),"UpdateEnvStatusEffects")]
internal static class ShipwrightWeather
{
    private static void Prefix(Player __instance,ref bool ___m_underRoof,out bool __state)
    {
        __state=___m_underRoof;
        if(Shipwright.For(__instance.GetStandingOnShip()) is { } ship && ship.Covers(__instance))
        {___m_underRoof=true;if(ship.Warm)__instance.OnNearFire(ship.transform.position);}
    }
    private static void Finalizer(ref bool ___m_underRoof,bool __state)=>___m_underRoof=__state;
}
