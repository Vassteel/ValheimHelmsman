using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Helmsman;

// Retain Odin's save keys so a press already working in a world finishes its batch.
public sealed class FishOilPress : MonoBehaviour, Hoverable, Interactable
{
    public Transform Output=null!;
    public GameObject Working=null!;
    private const int Batch=10;
    private const double Seconds=600;
    private ZNetView view=null!;
    private WearNTear wear=null!;
    private float next;
    private Animator[] animators=Array.Empty<Animator>();
    private ZDO Data=>view.GetZDO();
    private bool Ready=>view&&view.IsValid();
    private int Queued=>Mathf.Clamp(Data.GetInt("FP_queued"),0,Batch);
    private bool Processing=>Data.GetBool("FP_IsProcessing");
    private double Remaining=>Math.Max(0,Seconds-(ZNet.instance.GetTime().Ticks-Data.GetLong("FP_ProcessingStartTime"))/(double)TimeSpan.TicksPerSecond);
    private IEnumerator Start()
    {
        view=GetComponent<ZNetView>();wear=GetComponent<WearNTear>();
        while(view&&!view.IsValid())yield return null;
        if(!view)yield break;
        if(wear)wear.m_onDestroyed+=ReturnContents;
        animators=GetComponentsInChildren<Animator>(true).Where(a=>a.parameters.Any(p=>p.name=="active"&&p.type==AnimatorControllerParameterType.Bool)).ToArray();
        foreach(var input in GetComponentsInChildren<Switch>(true))
        {input.m_onUse=(_,user,item)=>item==null?Interact(user,false,false):UseItem(user,item);input.m_onHover=()=>GetHoverText();}
    }
    private static bool Allowed(ItemDrop.ItemData item)=>item.m_dropPrefab&&
        (item.m_dropPrefab.GetComponent<Fish>()||item.m_dropPrefab.name=="FishRaw");
    private string Add(ItemDrop.ItemData? chosen)
    {
        if(!Ready||Processing||Queued>=Batch)return "The press is full; wait for this batch.";
        var player=Player.m_localPlayer;
        if(!player||!GetComponent<WorkstationLease>().Held)return "Press access changed. Try again.";
        var inv=player.GetInventory();var item=chosen??inv.GetAllItems().FirstOrDefault(Allowed);
        if(item==null||!Allowed(item)||!inv.ContainsItem(item))return "Bring fish to the press.";
        int count=Queued;string old=Data.GetString("fp_item"+count);
        var backup=new ZPackage();inv.Save(backup);
        try
        {
            if(!inv.RemoveItem(item,1))return "The fish is no longer available.";
            Data.Set("fp_item"+count,item.m_dropPrefab.name);Data.Set("FP_queued",count+1);
            return "Fish added: "+(count+1)+" / "+Batch+".";
        }
        catch(Exception error)
        {
            Data.Set("FP_queued",count);Data.Set("fp_item"+count,old);
            inv.Load(new ZPackage(backup.GetArray()),false);Plugin.Instance.Error(error);
            return "Press stopped; your fish was returned.";
        }
    }
    private void Update()
    {
        if(!Ready||!ZNet.instance||Time.time<next)return;next=Time.time+1;
        if(Working)Working.SetActive(Processing);
        foreach(var animator in animators)if(animator)animator.SetBool("active",Processing);
        if(!view.IsOwner())return;
        if(!Processing&&Queued>=Batch)
        {Data.Set("FP_ProcessingStartTime",ZNet.instance.GetTime().Ticks);Data.Set("FP_IsProcessing",true);}
        if(!Processing||Remaining>0)return;
        // A native item ZDO is the persistent receipt; retrying a failed visual spawn
        // cannot produce a second bottle. The next batch clears the old receipt.
        if(Data.GetZDOID("helmsman_press_output")==ZDOID.None)
        {
            var prefab=ObjectDB.instance.GetItemPrefab("FishExtract");if(!prefab)return;
            var product=ZDOMan.instance.CreateNewZDO(Output.position,"FishExtract".GetStableHashCode());
            Data.Set("helmsman_press_output",product.m_uid);
            product.Persistent=true;product.SetPrefab("FishExtract".GetStableHashCode());product.SetRotation(Output.rotation);
        }
        Data.Set("FP_queued",0);Data.Set("FP_IsProcessing",false);Data.Set("FP_ProcessingStartTime",0L);
    }
    public string GetHoverName()=>"Fish oil press";
    public float GetHoverOffset()=>0;
    public string GetHoverText()=>!Ready?GetHoverName():Localization.instance.Localize(GetHoverName()+"\n"+
        (Processing?"Pressing — "+Shipyard.FormatDuration(Remaining):Queued+" / "+Batch+" fish\n[<color=yellow><b>$KEY_Use</b></color>] Add fish"));
    public bool Interact(Humanoid user,bool hold,bool alt)
    {
        if(hold||user!=Player.m_localPlayer)return false;
        Plugin.Message(GetComponent<WorkstationLease>().Run(()=>
        {if(!Processing&&Queued==0)Data.Set("helmsman_press_output",ZDOID.None);return Add(null);}));return true;
    }
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)
    {
        if(user!=Player.m_localPlayer||!Allowed(item))return false;
        Plugin.Message(GetComponent<WorkstationLease>().Run(()=>
        {if(!Processing&&Queued==0)Data.Set("helmsman_press_output",ZDOID.None);return Add(item);}));return true;
    }
    private void ReturnContents()
    {
        if(!Ready||!view.IsOwner())return;
        // Legacy batches consumed their queue when starting. Refund raw fish if
        // interrupted; destroying a press must never skip the processing timer.
        if(Processing&&Queued==0)
        {
            if(Remaining<=0)Drop("FishExtract");
            else for(int i=0;i<Batch;i++)Drop("FishRaw");
            Data.Set("FP_IsProcessing",false);return;
        }
        for(int i=Queued-1;i>=0;i--){Drop(Data.GetString("fp_item"+i));Data.Set("FP_queued",i);}
    }
    private void Drop(string name)
    {
        var prefab=ObjectDB.instance.GetItemPrefab(name);if(!prefab)return;
        var item=prefab.GetComponent<ItemDrop>().m_itemData.Clone();item.m_dropPrefab=prefab;item.m_stack=1;
        ItemDrop.DropItem(item,1,Output.position,Output.rotation);
    }
    private void OnDestroy(){if(wear)wear.m_onDestroyed-=ReturnContents;}
}
