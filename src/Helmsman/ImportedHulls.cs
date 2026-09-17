using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helmsman.Core;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Helmsman;

internal static partial class ImportedHulls
{
    internal const string TablePrefab="CarpentersTable";
    private static AssetBundle? bundle;
    internal static void Register()
    {
        if(BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("marlthon.OdinShip") || BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("marlthon.OdinShipPlus"))
            throw new InvalidOperationException("Disable the original OdinShip/OdinShipPlus plugins before using Helmsman's replacement prefab registry.");
        using var resource=typeof(Plugin).Assembly.GetManifestResourceStream("Helmsman.Ships.bundle")
            ?? throw new InvalidOperationException("Missing ship asset bundle.");
        using var data=new MemoryStream();resource.CopyTo(data);
        bundle=AssetBundle.LoadFromMemory(data.ToArray());
        if(!bundle)throw new InvalidOperationException("Cannot read ship models.");
        ImportedShipMaterials.Prepare();
        RegisterItems();
        var visualBindings=ShipCosmetics.ReadBindings();
        foreach(var blueprint in ShipConstruction.Blueprints.Where(b=>b.Id!="longship"))
        {
            var prefab=Clone(FinalFleetModels.Template(blueprint.Source),blueprint.Prefab);
            ImportedShipMaterials.Apply(prefab,FinalFleetModels.Template(blueprint.Source));
            if(FinalFleetModels.Has(blueprint.Source))FinalFleetModels.Apply(prefab,blueprint.Source);
            else BoatyardModels.Apply(prefab,blueprint.Source);
            // The merchant lamp's animated point light sweeps full-strength shadows
            // across nearby hull surfaces. Keep its warm light steady on a moving ship.
            if(blueprint.Source=="MercantShip")
            {
                var lamp=prefab.transform.Find("ship/visual/Customize/TraderLamp");
                if(lamp)
                {
                    foreach(var flicker in lamp.GetComponentsInChildren<LightFlicker>(true))
                    {flicker.m_flickerIntensity=0;flicker.m_flickerSpeed=0;flicker.m_movement=0;}
                    foreach(var light in lamp.GetComponentsInChildren<Light>(true))
                    {light.shadows=LightShadows.None;light.color=new Color(1f,.78f,.52f);light.intensity=1.5f;light.range=7f;}
                }
            }
            var ship=prefab.GetComponent<Ship>();
            if(!ship || !ship.m_floatCollider || !ship.m_shipControlls || !prefab.GetComponent<ZNetView>())
                throw new InvalidOperationException(blueprint.Name+" is missing native ship components.");
            prefab.AddComponent<ShipOarAnimation>();
            prefab.AddComponent<ImportedSailAnimation>().FixedMast=FinalFleetModels.Has(blueprint.Source);
            ship.m_hasSail=blueprint.HasSail;
            if(!blueprint.HasSail)ship.m_sailForceFactor=0;
            var piece=prefab.GetComponent<Piece>();piece.m_name=blueprint.Name;piece.m_usage=Piece.UsageTagFlags.Transport;
            piece.m_description="Built by the puffin shipwright. Name her to call her with a Gullcall Whistle.";
            piece.m_resources=Costs(blueprint.Recipe).Select(c=>c.GetRequirement()).ToArray();
            var holds=prefab.GetComponentsInChildren<Container>(true);
            for(int i=0;i<holds.Length;i++)
            {
                holds[i].m_rootObjectOverride=prefab.GetComponent<ZNetView>();
                string legacyName=holds[i].m_name;
                holds[i].m_name=blueprint.Name+" cargo"+(holds.Length>1?" "+(i+1):"");
                holds[i].m_checkGuardStone=true;
                var scope=holds[i].gameObject.AddComponent<ShipHoldScope>();scope.Slot=i;scope.LegacyName=legacyName;
            }
            // Player hulls are included; all naval weapons are deferred.
            foreach(var node in prefab.GetComponentsInChildren<Transform>(true))
                if(node.name.IndexOf("Turret",StringComparison.OrdinalIgnoreCase)>=0)node.gameObject.SetActive(false);
            foreach(var text in prefab.GetComponentsInChildren<TMPro.TMP_Text>(true))text.enabled=false;
            var binding=visualBindings.FirstOrDefault(b=>b.prefab==blueprint.Prefab);
            var cosmetics=prefab.AddComponent<ShipCosmetics>();
            cosmetics.Binding=binding??new ShipCosmeticBinding {prefab=blueprint.Prefab};
            Material[] Styles(string[] paths,bool cloth)=>paths.Select(path=>ImportedShipMaterials.Style(bundle!.LoadAsset<Material>(path),cloth)).ToArray();
            if(binding!=null)
            {
                cosmetics.SailStyles=Styles(binding.SailMaterials,true);cosmetics.ShieldStyles=Styles(binding.ShieldMaterials,false);cosmetics.HullStyles=Styles(binding.HullMaterials,false);
                // Index zero uses the freshly painted default hull/sail, retaining the
                // original material variants as optional saved style choices.
                void Default(Material[] choices,string path){var renderer=path.Length>0?prefab.transform.Find(path)?.GetComponent<Renderer>():null;if(choices.Length>0&&renderer)choices[0]=renderer.sharedMaterial;}
                Default(cosmetics.SailStyles,binding.SailRenderer);Default(cosmetics.HullStyles,binding.HullRenderers.FirstOrDefault()??"");
            }
            FinalFleetModels.ConfigureStyles(prefab,cosmetics);
            if(blueprint.Source=="HerculeShip"&&!FinalFleetModels.Has(blueprint.Source))
            {
                var net=prefab.transform.Find("FishingNetSystem/CatchTrigger").gameObject.AddComponent<ShipFishingNet>();
                net.Chest=prefab.transform.Find("piece_chest").GetComponent<Container>();
                net.Visual=prefab.transform.Find("FishingNetSystem/RedePesca").gameObject;
                var handle=prefab.transform.Find("FishingNetSystem/RedePesca").gameObject.AddComponent<NetInteraction>();
                handle.Net=net;
                // A separate handle stays visible when the net is stowed.
                var handleObject=new GameObject("Fishing net handle");handleObject.transform.SetParent(net.transform.parent,false);
                handleObject.transform.localPosition=new Vector3(0,.8f,0);
                var trigger=handleObject.AddComponent<SphereCollider>();trigger.radius=.45f;
                handleObject.AddComponent<NetInteraction>().Net=net;
            }
            BoatyardModels.RefreshIcon(prefab);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab,true));
        }
        var table=Clone("CarpentersTable",TablePrefab);
        ImportedShipMaterials.Apply(table,"CarpentersTable");
        BoatyardModels.Apply(table,"CarpentersTable");
        var station=table.GetComponent<CraftingStation>();
        if(station)CarpenterRecipes.Configure(station);
        foreach(var guide in table.GetComponentsInChildren<GuidePoint>(true))Object.DestroyImmediate(guide);
        foreach(var particles in table.GetComponentsInChildren<ParticleSystem>(true))Object.DestroyImmediate(particles.gameObject);
        table.GetComponent<Piece>().m_usage=Piece.UsageTagFlags.Crafting;
        table.AddComponent<Shipyard>();
        table.AddComponent<WorkstationLease>();
        BoatyardModels.RefreshIcon(table);
        PieceManager.Instance.AddPiece(new CustomPiece(table,true,new PieceConfig {
            Name="Carpenter's Table",Description="Pay the puffin to build ships and refit nearby longships. Place beside a configured Dock Ward.",
            PieceTable="Hammer",Category="Helmsman",CraftingStation="piece_workbench",
            Requirements=Costs(HarborCatalog.TableRecipe)
        }));
        RegisterHarbor();
        Plugin.Instance.Record("Registered the player fleet and the Carpenter's Table.");
    }
    private static GameObject Clone(string source,string name)
    {
        var path=bundle!.GetAllAssetNames().SingleOrDefault(p=>p.EndsWith("/"+source.ToLowerInvariant()+".prefab",StringComparison.Ordinal));
        if(path==null)throw new InvalidOperationException("Missing selected model "+source);
        var original=bundle.LoadAsset<GameObject>(path);
        return PrefabManager.Instance.CreateClonedPrefab(name,original);
    }
}

// Stable slot identity: native containers keep their own serialization, UI, access checks
// and ownership handoff, but do not share save keys or RPC names with another hold.
public sealed class ShipHoldScope : MonoBehaviour
{
    public int Slot;
    public string LegacyName="";
    internal bool MigrationWarning;
    public bool MigrationBlocked
    {
        get
        {
            var container=GetComponent<Container>();
            var view=container&&container.m_rootObjectOverride?container.m_rootObjectOverride:GetComponent<ZNetView>();
            if(!view||!view.IsValid()||LegacyName.Length==0)return false;
            var data=view.GetZDO();
            return data.GetByteArray(Key(ZDOVars.s_items))==null&&data.GetString("items "+LegacyName).Length>0;
        }
    }
    internal int Key(int original)=>("helmsman_hold_"+Slot+"_"+original).GetStableHashCode();
    internal string Rpc(string original)=>"HelmsmanHold"+Slot+"_"+original;
}
