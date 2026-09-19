using System;
using System.Collections.Generic;
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
    internal static void Register()
    {
        if(BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("marlthon.OdinShip") || BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("marlthon.OdinShipPlus"))
            throw new InvalidOperationException("Disable the original OdinShip/OdinShipPlus plugins before using Helmsman's replacement prefab registry.");
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
            piece.m_description=(blueprint.UsesSlipway?"Select with the hammer to start construction on an available slipway.":"Build instantly with the hammer at a shipwright workshop.")+" Visit the puffin for paint and decoration. Name her to call her with a Gullcall Whistle.";
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
                CraftingStation=TablePrefab,Requirements=Costs(blueprint.Recipe)
            }));
        }
        var nativeLongship=PrefabManager.Instance.GetPrefab("VikingShip");
        if(nativeLongship)nativeLongship.GetComponent<Piece>().m_craftingStation=PrefabManager.Instance.GetPrefab(TablePrefab).GetComponent<CraftingStation>();
        Plugin.Instance.Record("Registered the player fleet and the Carpenter's Table.");
    }
    private static void RegisterStations()
    {
        var table=PrefabManager.Instance.CreateClonedPrefab(TablePrefab,"piece_workbench");
        WorkshopModels.Apply(table,"workbench");
        var station=table.GetComponent<CraftingStation>();
        station.m_name="Puffin shipwright";station.m_rangeBuild=WorkshopRange.Range;
        station.m_craftRequireRoof=false;station.m_craftRequireFire=false;station.m_showBasicRecipies=false;station.m_upgrader=false;
        foreach(var guide in table.GetComponentsInChildren<GuidePoint>(true))Object.DestroyImmediate(guide);
        foreach(var particles in table.GetComponentsInChildren<ParticleSystem>(true))Object.DestroyImmediate(particles.gameObject);
        table.GetComponent<Piece>().m_usage=Piece.UsageTagFlags.Crafting;
        WorkshopMaterialSupply.AddStorage(table);
        table.AddComponent<Shipyard>();
        table.AddComponent<Structures.StructureConstruction>();
        table.AddComponent<WorkstationLease>();
        BoatyardModels.RefreshIcon(table);
        PieceManager.Instance.AddPiece(new CustomPiece(table,true,new PieceConfig {
            Name="Shipwright bench",Description="Supports ship construction from the hammer menu. Add workshop upgrades for faster construction, tougher hulls, improved rigging and paint services.",
            PieceTable="Hammer",Category="Helmsman",CraftingStation="piece_workbench",
            Requirements=Costs(HarborCatalog.TableRecipe)
        }));
        WorkshopRegistration.Register();
        RegisterHarbor();
        Plugin.Instance.Record("Registered Carpenter's Table and harbor workstations.");
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
