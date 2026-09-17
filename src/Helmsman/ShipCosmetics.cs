using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Helmsman;

[Serializable]
public sealed class ShipCosmeticBinding
{
    public string prefab="",SailRenderer="";
    public string[] FigureheadObjects=Array.Empty<string>(),ShieldRenderers=Array.Empty<string>(),HullRenderers=Array.Empty<string>(),NameplateRenderers=Array.Empty<string>();
    public string[] SailMaterials=Array.Empty<string>(),ShieldMaterials=Array.Empty<string>(),HullMaterials=Array.Empty<string>();
}
[Serializable]
internal sealed class ShipCosmeticCatalog {public ShipCosmeticBinding[] entries=Array.Empty<ShipCosmeticBinding>();}

public sealed class ShipCosmetics : MonoBehaviour
{
    public ShipCosmeticBinding Binding=new();
    public Material[] SailStyles=Array.Empty<Material>(),ShieldStyles=Array.Empty<Material>(),HullStyles=Array.Empty<Material>();
    private ZNetView view=null!;
    private uint revision=uint.MaxValue;
    private float next;
    private ShipwrightSkin? customCloth;
    private TMP_Text[] plates=Array.Empty<TMP_Text>();
    private string currentCloth="", appliedState="";
    internal bool Ready=>view&&view.IsValid();
    private IEnumerator Start()
    {
        view=GetComponent<ZNetView>();while(view&&!view.IsValid())yield return null;
        if(!view)yield break;
        plates=Binding.NameplateRenderers.Select(p=>transform.Find(p)?.GetComponent<TMP_Text>()).Where(t=>t).ToArray()!;
        if(!ZNet.instance.IsDedicated())
        {
            var font=MenuTheme.FindFont();while(!font){yield return null;font=MenuTheme.FindFont();}
            foreach(var text in plates){text.font=font;text.fontSharedMaterial=font!.material;text.richText=false;text.enabled=true;}
        }
        Refresh();
    }
    private void Update()
    {if(!Ready||Time.time<next)return;next=Time.time+.25f;if(revision!=view.GetZDO().DataRevision)Refresh();}
    internal string Change(string kind)
    {
        if(!Ready||!view.IsOwner())return "Take the helm first, then ask the puffin to change this ship.";
        var player=Player.m_localPlayer;
        if(!player||player.IsDead()||Vector3.Distance(player.transform.position,transform.position)>35||!PrivateArea.CheckAccess(transform.position,0,false,true))return "Stand beside an accessible ship.";
        int count=kind=="figurehead"?Binding.FigureheadObjects.Length:kind=="sail"?SailStyles.Length:kind=="shield"?ShieldStyles.Length:kind=="hull"?HullStyles.Length:0;
        if(count==0)return "No alternate styles for this part.";
        string key="odinship_"+kind+"_index";
        view.GetZDO().Set(key,(Math.Max(0,view.GetZDO().GetInt(key))+1)%count);
        if(kind=="sail")view.GetZDO().Set("helmsman_imported_cloth","");
        Refresh();return "Ship style changed.";
    }
    internal string Cloth(string name)
    {
        if(!Ready||!view.IsOwner())return "Take the helm first to choose sailcloth.";
        var player=Player.m_localPlayer;
        if(!player||player.IsDead()||Vector3.Distance(player.transform.position,transform.position)>35||!PrivateArea.CheckAccess(transform.position,0,false,true))return "Stand beside an accessible ship.";
        if(!ShipwrightAssets.Styles("sails").Contains(name))return "That sailcloth is unavailable.";
        view.GetZDO().Set("helmsman_imported_cloth",name=="Vanilla"?"":name);Refresh();return "Sailcloth changed.";
    }
    private void Refresh()
    {
        if(!Ready)return;var data=view.GetZDO();
        revision=data.DataRevision;
        string state=data.GetInt("odinship_figurehead_index")+"|"+data.GetInt("odinship_sail_index")+"|"+
            data.GetInt("odinship_shield_index")+"|"+data.GetInt("odinship_hull_index")+"|"+
            data.GetString("helmsman_imported_cloth")+"|"+ShipDirectory.SavedName(data);
        if(state==appliedState)return;
        if(Binding.FigureheadObjects.Length>0)
        {
            int index=Mathf.Clamp(data.GetInt("odinship_figurehead_index"),0,Binding.FigureheadObjects.Length-1);
            for(int i=0;i<Binding.FigureheadObjects.Length;i++){var node=transform.Find(Binding.FigureheadObjects[i]);if(node)node.gameObject.SetActive(i==index);}
        }
        void Paint(string[] paths,Material[] choices,string key)
        {
            if(choices.Length==0)return;var material=choices[Mathf.Clamp(data.GetInt(key),0,choices.Length-1)];
            foreach(var path in paths){var renderer=transform.Find(path)?.GetComponent<Renderer>();if(renderer)renderer.sharedMaterial=material;}
        }
        customCloth?.Dispose();customCloth=null;
        var final=GetComponent<FinalShipPresentation>();
        if(final)final.Paint(data.GetInt("odinship_hull_index"),data.GetInt("odinship_sail_index"),HullStyles,SailStyles);
        Paint(new[]{Binding.SailRenderer},SailStyles,"odinship_sail_index");
        Paint(Binding.ShieldRenderers,ShieldStyles,"odinship_shield_index");
        Paint(Binding.HullRenderers,HullStyles,"odinship_hull_index");
        currentCloth=data.GetString("helmsman_imported_cloth");
        if(currentCloth.Length>0&&!ZNet.instance.IsDedicated())
        {
            var target=GetComponent<Ship>().m_sailObject;var texture=ShipwrightAssets.Texture(currentCloth,"sails");
            if(target&&texture){customCloth=new ShipwrightSkin();customCloth.Paint(target,texture);}
        }
        foreach(var text in plates)if(text)text.text=ShipDirectory.SavedName(data);
        appliedState=state;
    }
    private void OnDestroy()=>customCloth?.Dispose();
    internal static ShipCosmeticBinding[] ReadBindings()
    {
        using var stream=typeof(Plugin).Assembly.GetManifestResourceStream("Helmsman.Ships.customization.json")??throw new InvalidOperationException("Ship visual bindings missing.");
        using var reader=new StreamReader(stream);return JsonUtility.FromJson<ShipCosmeticCatalog>(reader.ReadToEnd()).entries;
    }
}
