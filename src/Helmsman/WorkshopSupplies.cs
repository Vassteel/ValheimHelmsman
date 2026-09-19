using System;
using System.Linq;
using Helmsman.Core;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
namespace Helmsman;
internal static class WorkshopSupplies
{
    internal static GameObject Create(HarborEntry entry)
    {
        string model=entry.Prefab switch{"ResinWood"=>"resin-wood","CaulkedWood"=>"caulked-wood","ClothShip"=>"sail-canvas","ShipRope"=>"marine-rope","WindBelt"=>"wind-belt","FishExtract"=>"fish-oil","FishExtract2"=>"wind-extract","DriedFishBasket"=>"dried-fish-basket",_=>throw new ArgumentException(entry.Prefab)};
        string native=entry.Prefab switch{"WindBelt"=>"BeltStrength","FishExtract" or "FishExtract2"=>"MeadStaminaMinor",_=>"Wood"};
        var go=PrefabManager.Instance.CreateClonedPrefab(entry.Prefab,native);
        // Keep the native wearable binding; the inventory/drop model is authored here.
        var attachments=entry.Prefab=="WindBelt"?go.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="attach"||t.name.StartsWith("attach_",StringComparison.Ordinal)).ToArray():Array.Empty<Transform>();
        var renderers=attachments.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).Distinct().Select(r=>(Renderer:r,Enabled:r.enabled,Off:r.forceRenderingOff)).ToArray();
        WorkshopModels.Apply(go,model);
        foreach(var r in renderers){r.Renderer.enabled=r.Enabled;r.Renderer.forceRenderingOff=r.Off;}
        // VisEquipment activates a copy of these attachment roots when equipped.
        foreach(var attach in attachments)attach.gameObject.SetActive(false);
        var data=go.GetComponent<ItemDrop>().m_itemData;var shared=data.m_shared;
        shared.m_equipStatusEffect=null!;shared.m_consumeStatusEffect=null!;shared.m_useDurability=false;
        shared.m_food=shared.m_foodStamina=shared.m_foodEitr=shared.m_foodBurnTime=shared.m_foodRegen=0;
        shared.m_maxStackSize=entry.Prefab switch{"WindBelt" or "DriedFishBasket"=>1,"FishExtract" or "FishExtract2"=>10,"ClothShip" or "ShipRope"=>20,_=>50};
        shared.m_weight=entry.Prefab switch{"ClothShip"=>5,"FishExtract" or "FishExtract2" or "DriedFishBasket"=>1,_=>2};
        data.m_dropPrefab=go;
        if(entry.Prefab is "WindBelt" or "FishExtract" or "FishExtract2")
        {
            var effect=ScriptableObject.CreateInstance<SE_Stats>();effect.name="Helmsman"+entry.Prefab+"Effect";effect.m_name=entry.Name;
            effect.m_ttl=entry.Prefab=="WindBelt"?0:entry.Prefab=="FishExtract"?300:600;
            if(entry.Prefab=="FishExtract")effect.m_addMaxCarryWeight=250;
            else effect.m_attributes=StatusEffect.StatusAttribute.SailingPower;
            if(entry.Prefab=="WindBelt")shared.m_equipStatusEffect=effect;else shared.m_consumeStatusEffect=effect;
        }
        return go;
    }
}
