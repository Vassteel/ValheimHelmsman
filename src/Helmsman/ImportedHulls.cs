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
        // Stations must remain buildable even if a ship model fails to load.
        RegisterStations();
        var visualBindings=ShipCosmetics.ReadBindings();
        foreach(var blueprint in ShipConstruction.Blueprints.Where(b=>b.Id!="longship"))
        {
            var model=blueprint.Source;
            var prefab=NativeFleetFactory.Create(blueprint);
            FinalFleetModels.Apply(prefab,model);
            var ship=prefab.GetComponent<Ship>();
            if(!ship || !ship.m_floatCollider || !ship.m_shipControlls || !prefab.GetComponent<ZNetView>())
                throw new InvalidOperationException(blueprint.Name+" is missing native ship components.");
            prefab.AddComponent<ShipOarAnimation>();
            var sailAnimation=prefab.AddComponent<ImportedSailAnimation>();
            sailAnimation.FixedMast=sailAnimation.MeshFurl=true;
            if(ship.m_sailObject)
            {
                sailAnimation.RestScale=ship.m_sailObject.transform.localScale;
                // These legacy Cloth rigs are stored reefed in the source bundle.
                if(!sailAnimation.MeshFurl&&ship.m_sailObject.GetComponentInChildren<Cloth>(true)&&Mathf.Abs(sailAnimation.RestScale.y-.1f)<.001f)
                    sailAnimation.RestScale.y=1;
                sailAnimation.ConfiguredScale=true;
            }
            FleetStability.Apply(ship);
            ImportedShipMaterials.NativeWaterImpact(ship);
            if(!sailAnimation.MeshFurl&&blueprint.HasSail)prefab.AddComponent<ImportedSailFlutter>();
            ship.m_hasSail=blueprint.HasSail;
            if(!blueprint.HasSail)ship.m_sailForceFactor=0;
            var piece=prefab.GetComponent<Piece>();piece.m_name=blueprint.Name;piece.m_usage=Piece.UsageTagFlags.Transport;
            piece.m_description="Build with the hammer. Visit the puffin for paint and decoration. Name her to call her with a Gullcall Whistle.";
            piece.m_waterPiece=true;
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
            FinalFleetModels.ConfigureStyles(prefab,cosmetics);
            BoatyardModels.RefreshIcon(prefab);
            PieceManager.Instance.AddPiece(new CustomPiece(prefab,false,new PieceConfig {
                Name=blueprint.Name,Description=piece.m_description,PieceTable="Hammer",Category="Helmsman",
                CraftingStation="piece_workbench",Requirements=Costs(blueprint.Recipe)
            }));
        }
        Plugin.Instance.Record("Registered the player fleet and the Carpenter's Table.");
    }
    private static void RegisterStations()
    {
        var table=Clone("CarpentersTable",TablePrefab);
        ImportedShipMaterials.Apply(table,"CarpentersTable");
        BoatyardModels.Apply(table,"CarpentersTable");
        var station=table.GetComponent<CraftingStation>();
        if(station)Object.DestroyImmediate(station); // Supplies are crafted at the ordinary workbench.
        foreach(var guide in table.GetComponentsInChildren<GuidePoint>(true))Object.DestroyImmediate(guide);
        foreach(var particles in table.GetComponentsInChildren<ParticleSystem>(true))Object.DestroyImmediate(particles.gameObject);
        table.GetComponent<Piece>().m_usage=Piece.UsageTagFlags.Crafting;
        table.AddComponent<Shipyard>();
        table.AddComponent<WorkstationLease>();
        BoatyardModels.RefreshIcon(table);
        PieceManager.Instance.AddPiece(new CustomPiece(table,true,new PieceConfig {
            Name="Carpenter's Table",Description="Visit the puffin to paint and decorate ships within 35 m. Ships and harbor pieces are built with the hammer.",
            PieceTable="Hammer",Category="Helmsman",CraftingStation="piece_workbench",
            Requirements=Costs(HarborCatalog.TableRecipe)
        }));
        RegisterHarbor();
        Plugin.Instance.Record("Registered Carpenter's Table and harbor workstations.");
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
