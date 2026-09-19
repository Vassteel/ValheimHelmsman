using System;
using System.Collections.Generic;
using System.Linq;
using Helmsman.Core;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Helmsman;

internal static partial class ImportedHulls
{
    private static RequirementConfig[] Costs(string recipe)=>ShipwrightRules.Costs(recipe).Select(c=>new RequirementConfig(c.Key,c.Value,0,true)).ToArray();
    private static void RegisterItems()
    {
        var effects=new HashSet<string>(StringComparer.Ordinal);
        foreach(var entry in HarborCatalog.Items)
        {
            var prefab=WorkshopSupplies.Create(entry);
            string description=entry.Prefab switch {
                "ResinWood"=>"Resin-treated timber for shipbuilding.",
                "CaulkedWood"=>"Sealed timber for larger vessels.",
                "ClothShip"=>"Canvas for a shipwright's sails.",
                "ShipRope"=>"Rope for rigging and dock work.",
                "WindBelt"=>"A sailor's belt that grants a following wind.",
                "FishExtract"=>"Pressed fish oil. Temporarily increases carrying capacity.",
                "FishExtract2"=>"Fish extract that grants a following wind.",
                _=>"A basket of dried fish."
            };
            var config=new ItemConfig {Name=entry.Name,Description=description,Amount=entry.Amount};
            if(entry.Recipe.Length>0)
            {config.CraftingStation="piece_workbench";config.MinStationLevel=1;config.Requirements=Costs(entry.Recipe);}
            BoatyardModels.RefreshIcon(prefab);
            var item=new CustomItem(prefab,false,config);
            foreach(var effect in new[]{item.ItemDrop.m_itemData.m_shared.m_equipStatusEffect,item.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect})
                if(effect && effects.Add(effect.name))
                {effect.m_name=entry.Name;effect.m_tooltip=description;effect.m_icon=prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons[0];ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect,true));}
            if(!ItemManager.Instance.AddItem(item))throw new InvalidOperationException("Cannot register maritime item "+entry.Prefab);
        }
    }
    private static void RegisterHarbor()
    {
        foreach(var entry in HarborCatalog.Pieces)
        {
            string? model=NativeHarbor.Model(entry.Prefab);
            var prefab=model!=null?NativeHarbor.Create(entry.Prefab,model):entry.Prefab=="FishingDock"?NativeFishingDock.Create():throw new InvalidOperationException("No native harbor model for "+entry.Prefab);
            ShoreBuildBounds.ConfigureDock(prefab,entry.Prefab);
            BoatyardModels.RefreshIcon(prefab);
            prefab.GetComponent<Piece>().m_usage=Piece.UsageTagFlags.Decor|
                (entry.Prefab=="FishingDock"?Piece.UsageTagFlags.Crafting:0);
            PieceManager.Instance.AddPiece(new CustomPiece(prefab,false,new PieceConfig {
                Name=entry.Name,Description=entry.Prefab=="FishingDock"?"Daylight fishing with the pelican. Catches enter the station chest.":model!=null?(model=="keel-cradle"?"A padded timber cradle for supporting a hull.":"Operate the harbor machinery to run its working cycle."):"Harbor decoration.",PieceTable="Hammer",Category="Helmsman",CraftingStation="piece_workbench",Requirements=Costs(entry.Recipe)
            }));
        }
    }
}
