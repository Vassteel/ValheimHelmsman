using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;

namespace Helmsman;

// Bundled Windows shaders are not portable to the Linux server/client. Bind actual
// shaders from the running game's prefabs, and keep each atlas on its original UVs.
internal static class ImportedShipMaterials
{
    private static readonly Dictionary<string,Material> owned=new();
    private static readonly Dictionary<string,Shader> native=new();
    private static Material? wood,cloth;
    internal static void Prepare()
    {
        var ship=PrefabManager.Instance.GetPrefab("VikingShip");
        if(!ship)throw new InvalidOperationException("Native longship materials are not ready.");
        var materials=ship.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader).ToArray();
        wood=materials.FirstOrDefault(m=>m.shader.name=="Custom/Piece");
        cloth=ship.GetComponent<Ship>().m_sailObject.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.shader);
        if(!wood||!cloth)throw new InvalidOperationException("Native wood or sail material is missing.");
        foreach(var material in materials)native[material.shader.name]=material.shader;
    }
    internal static void Apply(GameObject prefab,string source)
    {
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if(renderer.GetComponent<TMPro.TMP_Text>())continue;
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var original=materials[i];if(!original)continue;
                string key=source+"/"+original.GetInstanceID();
                if(!owned.TryGetValue(key,out var material))
                {
                    string shaderName=original.shader?original.shader.name:"";
                    bool fabric=IsFabric(original.name);
                    // Masks, shadows and particles need their native rendering behavior.
                    bool special=shaderName.Contains("WaterMask")||shaderName.Contains("ShadowBlob")||renderer is ParticleSystemRenderer;
                    if(special)
                    {
                        if(!native.TryGetValue(shaderName,out var shader))shader=Shader.Find(shaderName);
                        if(!shader || !shader.isSupported){renderer.enabled=false;continue;}
                        material=new Material(original){shader=shader};
                    }
                    else
                    {
                        material=new Material(fabric?cloth!:wood!);
                        var texture=Replacement(source,original.name,fabric)??original.mainTexture;
                        if(texture)material.mainTexture=texture;
                        material.mainTextureScale=original.mainTextureScale;material.mainTextureOffset=original.mainTextureOffset;
                        material.color=Color.white;
                        // Newly painted hulls are rough timber, not glossy PBR surfaces.
                        foreach(var property in new[]{"_Metallic","_Glossiness","_MetalGloss","_NoiseGlowEnabled","_AddSnow"})
                            if(material.HasProperty(property))material.SetFloat(property,0);
                        foreach(var property in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"})
                            if(material.HasProperty(property))material.SetColor(property,Color.black);
                        material.DisableKeyword("_EMISSION");material.DisableKeyword("NOISEGLOW");
                        material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                        if(material.HasProperty("_MoveableObject"))material.SetFloat("_MoveableObject",source=="CarpentersTable"?0:1);
                    }
                    material.name=original.name+" [Helmsman vanilla finish]";owned.Add(key,material);
                }
                materials[i]=material;
            }
            renderer.sharedMaterials=materials;
        }
    }
    private static bool IsFabric(string name)=>name.StartsWith("Sail",StringComparison.OrdinalIgnoreCase)||name.StartsWith("Vela",StringComparison.OrdinalIgnoreCase)||name=="Cloth"||name=="HerculeSail"||name=="Roman Sail";
    private static Texture2D? Replacement(string source,string name,bool fabric)
    {
        if(fabric)return ShipwrightAssets.Texture("Sea flax","sails");
        if((source=="RowingCanoe"||source=="DoubleRowingCanoe"||source=="LittleBoat")&&name.StartsWith("Viking_Ship",StringComparison.Ordinal))
            return ShipwrightAssets.Texture("hulls/canoe");
        if(source=="MercantShip"&&name=="ship")return ShipwrightAssets.Texture("hulls/merchant");
        if(source=="CargoCaravel"&&name=="Hull")return ShipwrightAssets.Texture("hulls/caravel");
        if(source=="CargoShip"&&(name=="main1"||name=="main2"||name=="main3") || source=="WarShip"&&name.StartsWith("WarShipTex",StringComparison.Ordinal))
            return ShipwrightAssets.Texture("hulls/planks-vertical");
        if(source=="BigCargoShip"&&name=="Wood01" || source=="HugeCargoShip"&&(name=="Hull"||name=="Mast Coat"||name=="Deck"))
            return ShipwrightAssets.Texture("hulls/planks-horizontal");
        if((source=="CargoAnimalShip"||source=="HerculeShip")&&(name=="Hull"||name=="Mast Coat"||name=="Deck"))return ShipwrightAssets.Texture("hulls/planks-horizontal");
        if(source=="FastShipSkuldelev"&&(name=="Hull"||name=="HullRed"))return ShipwrightAssets.Texture("hulls/fast-skuldelev");
        if(source=="FastShipSkuldelev"&&name=="Frames")return ShipwrightAssets.Texture("hulls/planks-vertical");
        if(source=="GoblinShip")
        {
            if(name=="Hull_4K")return ShipwrightAssets.Texture("hulls/goblin");
            if(name.StartsWith("Hullsupport",StringComparison.Ordinal)||name.StartsWith("Keel",StringComparison.Ordinal)||name.StartsWith("Mast_4K",StringComparison.Ordinal))return ShipwrightAssets.Texture("hulls/planks-vertical");
        }
        if(source=="TaurusWarShip")
        {
            if(name=="MarlthonFrontMat_Base_color")return ShipwrightAssets.Texture("hulls/taurus-front");
            if(name=="MarlthonTailSideMat_Base_color")return ShipwrightAssets.Texture("hulls/taurus-tail");
            if(name=="MarlthonSailSideMat_Base_color")return ShipwrightAssets.Texture("hulls/taurus-sail-support");
            if(name=="Mast Coat")return ShipwrightAssets.Texture("hulls/planks-horizontal");
        }
        if(source=="Skuldelev")
        {
            if(name=="Viking Ship - 4-Material Blend")return ShipwrightAssets.Texture("hulls/skuldelev-painted");
            if(name.Contains("Rawoak")||name.Contains("Tar")||name.Contains("linseed")||name=="Viking Ship - 4-Material Blend.001")return ShipwrightAssets.Texture("hulls/planks-horizontal");
        }
        return null;
    }
    internal static Material Style(Material original,bool fabric)
    {
        string key="variant/"+original.GetInstanceID();if(owned.TryGetValue(key,out var cached))return cached;
        var material=new Material(fabric?cloth!:wood!){name=original.name+" [Helmsman style]"};
        material.mainTexture=original.mainTexture;material.mainTextureScale=original.mainTextureScale;material.mainTextureOffset=original.mainTextureOffset;
        material.color=original.HasProperty("_Color")?original.color:Color.white;
        foreach(var property in new[]{"_Metallic","_Glossiness","_NoiseGlowEnabled","_AddSnow"})if(material.HasProperty(property))material.SetFloat(property,0);
        foreach(var property in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"})if(material.HasProperty(property))material.SetColor(property,Color.black);
        material.DisableKeyword("_EMISSION");material.DisableKeyword("NOISEGLOW");material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        owned[key]=material;return material;
    }
    internal static void Release()
    {foreach(var material in owned.Values)if(material)UnityEngine.Object.Destroy(material);owned.Clear();native.Clear();wood=null;cloth=null;}
}
