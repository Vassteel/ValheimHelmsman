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
        Material[] From(string name)
        {
            var prefab=PrefabManager.Instance.GetPrefab(name);
            return prefab?prefab.GetComponentsInChildren<Renderer>(true)
                .Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer)
                .SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader).ToArray():Array.Empty<Material>();
        }
        var materials=From("VikingShip");
        var building=From("piece_workbench").Concat(From("piece_chest_wood")).ToArray();
        // Current ships can use cloth sails outside the legacy m_sailObject and
        // hull shaders other than Custom/Piece. Resolve actual renderer materials.
        wood=building.Concat(materials).FirstOrDefault(m=>m.shader.name=="Custom/Piece")
            ??materials.Concat(building).FirstOrDefault(m=>WorldSurface(m.shader.name));
        cloth=materials.FirstOrDefault(m=>WorldSurface(m.shader.name)&&m.name.IndexOf("sail",StringComparison.OrdinalIgnoreCase)>=0)??wood;
        if(!wood||!cloth)throw new InvalidOperationException("No native world-surface material is available for maritime content.");
        foreach(var material in materials.Concat(building))native[material.shader.name]=material.shader;
        Plugin.Instance.Record("Maritime materials: wood="+wood.name+" ("+wood.shader.name+"); cloth="+cloth.name+" ("+cloth.shader.name+").");
    }
    internal static bool WorldSurface(string shader)=>shader.StartsWith("Custom/",StringComparison.Ordinal)&&
        shader.IndexOf("Water",StringComparison.OrdinalIgnoreCase)<0&&shader.IndexOf("Shadow",StringComparison.OrdinalIgnoreCase)<0&&
        shader.IndexOf("Particle",StringComparison.OrdinalIgnoreCase)<0&&shader.IndexOf("Unlit",StringComparison.OrdinalIgnoreCase)<0;
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
                    // Lantern glass has a dedicated emission mask. Preserve that mask
                    // without making the metal frame, hull or crew emissive.
                    if(source=="MercantShip"&&original.name=="fi_village_lighting")
                    {
                        var emission=original.HasProperty("_EmissionMap")?original.GetTexture("_EmissionMap"):null;
                        if(emission)
                        {
                            if(!material.HasProperty("_EmissionMap"))
                            {
                                var standard=Shader.Find("Standard");
                                if(standard&&standard.isSupported)
                                {UnityEngine.Object.Destroy(material);material=new Material(original){shader=standard};}
                            }
                            if(material.HasProperty("_EmissionMap")&&material.HasProperty("_EmissionColor"))
                            {
                                material.SetTexture("_EmissionMap",emission);
                                material.SetColor("_EmissionColor",new Color(1.5f,1.15f,.65f));
                                material.EnableKeyword("_EMISSION");
                                material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
                                if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.15f);
                            }
                            else Plugin.Instance.Record("Merchant lantern: native shader lacks masked emission.");
                        }
                    }
                    material.name=original.name+" [Helmsman vanilla finish]";owned.Add(key,material);
                }
                materials[i]=material;
            }
            renderer.sharedMaterials=materials;
        }
    }
    internal static Material BoatyardMaterial(Color color,bool timber)
    {
        if(!wood)throw new InvalidOperationException("Boatyard materials requested before native materials.");
        var material=new Material(wood){name="Helmsman boatyard matte",color=color};
        material.mainTexture=timber?ShipwrightAssets.Texture("hulls/planks-horizontal"):Texture2D.whiteTexture;
        material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;
        foreach(var p in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"})if(material.HasProperty(p))material.SetColor(p,Color.black);
        foreach(var p in new[]{"_Glossiness","_Metallic","_MetalGloss","_NoiseGlowEnabled"})if(material.HasProperty(p))material.SetFloat(p,0);
        material.DisableKeyword("_EMISSION");material.DisableKeyword("NOISEGLOW");
        material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;return material;
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
