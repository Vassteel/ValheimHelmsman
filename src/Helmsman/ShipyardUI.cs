using System;
using System.Collections.Generic;
using System.Linq;
using Helmsman.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Helmsman;

public sealed partial class HelmsmanUI
{
    private Shipyard? yard;
    private bool yardDecoration;
    private string workshopOrderId="";
    private float yardNoticeUntil;
    private void YardNotice(string text){notice=text;yardNoticeUntil=Time.unscaledTime+5;}
    internal void OpenShipyard(Shipyard table)
    {
        Close();yard=table;yardDecoration=false;tab=0;page=0;RefreshRefitShips();Open();
    }
    private void RefreshRefitShips()
    {
        nearby.Clear();if(!yard)return;
        nearby.AddRange(FindObjectsByType<Ship>(FindObjectsSortMode.None).Where(s=>(Shipwright.Supports(s)||s.GetComponent<ShipCosmetics>())&&Vector3.Distance(s.transform.position,yard.transform.position)<35));
        if(!selectedShip||!nearby.Contains(selectedShip))selectedShip=nearby.FirstOrDefault();
    }
    private void BuildShipyardMenu(RectTransform rect)
    {
        if(!yardDecoration){BuildWorkshopMenu(rect);return;}
        theme!.Text(rect,"Puffin shipwright",24,-65,672,32,22,MenuTheme.Gold);
        theme.Text(rect,"Paint and decoration · build ships with Hammer → Helmsman",24,-108,672,38,19,MenuTheme.Muted);
        body=MenuTheme.Rect("Ship decoration",rect,24,-164,672,360);
        BuildRefits();
        status=theme.Text(rect,"",24,-535,672,54,17,MenuTheme.Gold);
        refreshLabels.Add(()=>{if(status&&yard)status.text=notice.Length>0&&Time.unscaledTime<yardNoticeUntil?notice:yard.Status;});
        theme.Text(rect,"D-pad ↑↓: select   A: activate   B / Esc: close",24,-596,672,18,15,MenuTheme.Muted);
        nextLabels=0;FitModal();
        if(ZInput.IsGamepadActive()&&controls.Count>0)controls[0].Select();
    }
    private void BuildWorkshopMenu(RectTransform rect)
    {
        var bench=yard!;var jobs=WorkshopOrders.At(bench,Player.m_localPlayer);
        var job=jobs.FirstOrDefault(o=>o.Id==workshopOrderId)??jobs.FirstOrDefault();
        workshopOrderId=job?.Id??"";
        string signature=string.Join("|",jobs.Select(o=>o.Id));
        theme!.Text(rect,"Puffin workshop",24,-65,672,32,22,MenuTheme.Gold);
        theme.Text(rect,"Construction plans and supplies",24,-108,672,32,19,MenuTheme.Muted);
        Button(rect,job!=null?"Order: "+job.Name+(jobs.Count>1?"  ›":""):"No active construction",24,-150,672,()=>
        {
            if(jobs.Count>1){workshopOrderId=jobs[(jobs.FindIndex(o=>o.Id==workshopOrderId)+1)%jobs.Count].Id;rebuild=true;}
        });
        Button(rect,"Import structure",24,-196,330,()=>
        {
            if(!bench||!bench.Near(Player.m_localPlayer))return;
            if(!Structures.StructureImports.Allowed){YardNotice("Structure imports require local-world or server-admin access.");return;}
            Close();var message=bench.GetComponent<WorkstationLease>().Run(()=>{Structures.StructureImports.Instance.Tool.Open(bench.GetComponent<ZNetView>().GetZDO().m_uid);return "Choose a structure to build.";});Plugin.Message(message);
        });
        Button(rect,"Open construction supplies",366,-196,330,()=>{Close();if(bench&&bench.Near(Player.m_localPlayer))bench.GetComponent<Container>().Interact(Player.m_localPlayer,false,false);});
        Button(rect,"Paint and decoration",24,-242,330,()=>{if(bench&&WorkshopRange.Upgrade(bench,Player.m_localPlayer,"paint")){yardDecoration=true;rebuild=true;}else YardNotice("Add a paint stand to unlock painting and decoration.");});
        Button(rect,"Cancel selected order",366,-242,330,()=>
        {
            if(!bench||!bench.Near(Player.m_localPlayer))return;
            YardNotice(job!=null?job.Cancel():"No active order to cancel.");rebuild=true;
        });
        var statusScroll=MenuTheme.Rect("Order status",rect,24,-290,672,84);
        MenuTheme.Panel(statusScroll,Color.clear);statusScroll.gameObject.AddComponent<RectMask2D>();
        var statusContent=MenuTheme.Rect("Content",statusScroll,0,0,650,84);
        var text=theme.Text(statusContent,"",0,0,650,84,16,MenuTheme.Muted);
        var statusScroller=statusScroll.gameObject.AddComponent<ScrollRect>();statusScroller.viewport=statusScroll;statusScroller.content=statusContent;statusScroller.horizontal=false;statusScroller.scrollSensitivity=20;
        theme.Text(rect,"Required materials · Quartermaster supplies are pulled automatically",24,-376,672,20,15,MenuTheme.Gold);
        var scroll=MenuTheme.Rect("Required materials",rect,24,-400,672,184);
        MenuTheme.Panel(scroll,Color.clear);
        var viewport=MenuTheme.Rect("Viewport",scroll,0,0,650,184);viewport.gameObject.AddComponent<RectMask2D>();
        var content=MenuTheme.Rect("Resource slots",viewport,0,0,650,184);
        var scroller=scroll.gameObject.AddComponent<ScrollRect>();scroller.viewport=viewport;scroller.content=content;scroller.horizontal=false;
        scroller.scrollSensitivity=124;scroller.movementType=ScrollRect.MovementType.Clamped;scroller.inertia=false;
        var scrollbar=theme.VerticalScrollbar(scroll,656,0,16,184);
        scroller.verticalScrollbar=scrollbar;controls.Add(scrollbar);
        var slots=new Dictionary<string,(Image icon,TMPro.TMP_Text need,TMPro.TMP_Text supply)>();
        void CreateSlots(WorkshopOrder? selected)
        {
            foreach(Transform child in content)Destroy(child.gameObject);slots.Clear();
            int i=0;
            foreach(var id in selected?.Needed.Keys.OrderBy(k=>k,StringComparer.Ordinal)??Enumerable.Empty<string>())
            {
                var card=MenuTheme.Rect("Required "+id,content,(i%2)*330,-(i/2)*62,320,58);i++;
                MenuTheme.Panel(card,new Color(.10f,.08f,.055f,.58f));
                var icon=MenuTheme.Rect("Ghost item",card,6,-8,42,42).gameObject.AddComponent<Image>();
                var prefab=ObjectDB.instance.GetItemPrefab(id);var item=prefab?prefab.GetComponent<ItemDrop>():null;
                if(item)icon.sprite=item.m_itemData.GetIcon();icon.preserveAspect=true;icon.raycastTarget=false;
                theme.Text(card,WorkshopMaterialSupply.ItemName(id),56,-2,258,18,16,MenuTheme.Muted);
                var need=theme.Text(card,"",56,-21,258,18,15,MenuTheme.Muted);
                var supply=theme.Text(card,"",56,-39,258,16,13,MenuTheme.Muted);
                slots[id]=(icon,need,supply);
            }
            if(i==0)theme.Text(content,selected==null?"Select a ship with the hammer or import a structure.":"No further materials required.",4,0,638,48,17,MenuTheme.Muted);
            content.sizeDelta=new Vector2(650,Mathf.Max(184,((i+1)/2)*62));
            content.anchoredPosition=new Vector2(0,Mathf.Clamp(content.anchoredPosition.y,0,content.sizeDelta.y-184));
            scrollbar.interactable=content.sizeDelta.y>184;
        }
        CreateSlots(job);float nextResources=0;
        refreshLabels.Add(()=>
        {
            if(!text||!bench||Time.unscaledTime<nextResources)return;nextResources=Time.unscaledTime+1;
            var active=WorkshopOrders.At(bench,Player.m_localPlayer);
            if(string.Join("|",active.Select(o=>o.Id))!=signature){rebuild=true;return;}
            job=active.FirstOrDefault(o=>o.Id==workshopOrderId);
            text.text=notice.Length>0&&Time.unscaledTime<yardNoticeUntil?notice:job?.Status??bench.ConstructionStatus;
            float h=Mathf.Max(84,text.GetPreferredValues(text.text,650,0).y+8);statusContent.sizeDelta=new Vector2(650,h);((RectTransform)text.transform).sizeDelta=new Vector2(650,h);
            if(job==null)return;
            if(!slots.Keys.OrderBy(k=>k).SequenceEqual(job.Needed.Keys.OrderBy(k=>k)))CreateSlots(job);
            var available=WorkshopMaterialSupply.Available(Player.m_localPlayer,bench,job.Creator,job.Needed.Keys);
            foreach(var row in slots)
            {
                int need=job.Needed[row.Key];available.TryGetValue(row.Key,out var source);
                row.Value.icon.color=new Color(1,1,1,need>0?.32f:1);
                row.Value.need.text=need==0?"Supplied":"Need "+need+" · Quartermaster "+source.quartermaster;
                row.Value.need.color=need>source.quartermaster+source.bench+source.carried?new Color(1,.65f,.5f):MenuTheme.Muted;
                row.Value.supply.text=need==0?"Ready for construction":"Bench "+source.bench+" · Carried "+source.carried;
            }
        });
        theme.Text(rect,"D-pad ↑↓: select   ←→: scroll materials   A: activate   B / Esc: close",24,-596,672,18,15,MenuTheme.Muted);
        nextLabels=0;FitModal();if(ZInput.IsGamepadActive()&&controls.Count>0)controls[0].Select();
    }
    private void BuildRefits()
    {
        Button(body!,selectedShip?"Ship: "+ShipDirectory.Display(selectedShip)+"  ›":"No ship within 35 m — refresh",0,0,672,()=>
        {
            int old=selectedShip?nearby.IndexOf(selectedShip):-1;RefreshRefitShips();
            if(nearby.Count>0)selectedShip=nearby[(old+1)%nearby.Count];page=0;rebuild=true;
        });
        var fittings=Shipwright.For(selectedShip);
        var actions=new List<(string Label,Action Action)>();
        void Add(string label,Func<string> callback)=>actions.Add((label,()=>YardNotice(callback())));
        var cosmetics=selectedShip?selectedShip.GetComponent<ShipCosmetics>():null;
        if(cosmetics&&cosmetics.Ready)
        {
            if(cosmetics.Binding.FigureheadObjects.Length>0)Add("Change figurehead / deck decorations",()=>cosmetics.Change("figurehead"));
            if(cosmetics.SailStyles.Length>0)Add("Cycle sail pattern",()=>cosmetics.Change("sail"));
            if(cosmetics.ShieldStyles.Length>0)Add("Change decorative shields",()=>cosmetics.Change("shield"));
            if(cosmetics.HullStyles.Length>0)Add("Change hull finish",()=>cosmetics.Change("hull"));
            if(ShipProfile.CanSail(selectedShip!))foreach(var style in ShipwrightAssets.Styles("sails")){var choice=style;Add("Sailcloth: "+choice,()=>cosmetics.Cloth(choice));}
        }
        if(fittings&&fittings.Ready)
        {
        foreach(var upgrade in ShipwrightRules.Upgrades.Where(u=>u.Id=="lantern"||u.Id=="trophy"))
        {
            var u=upgrade;
            if(!fittings.Built(u.Id))actions.Add((u.Name+" · "+Shipwright.Requirements(u),()=>YardNotice(fittings.Buy(u))));
        }
        if(fittings.Built("lantern"))Add("Toggle lantern",()=>fittings.Toggle("lantern_off"));
        if(fittings.Built("canopy"))Add("Raise / stow canopy",()=>fittings.Toggle("canopy_hidden"));
        foreach(var style in ShipwrightAssets.Styles("sails")){string selected=style;Add("Sail cloth: "+selected,()=>fittings.Style("sails",selected));}
        if(fittings.Built("canopy"))foreach(var style in ShipwrightAssets.Styles("canopies")){string selected=style;Add("Canopy cloth: "+selected,()=>fittings.Style("canopies",selected));}
        if(fittings.Built("trophy"))
        {
            Add("Take down trophy",fittings.TakeTrophy);
            foreach(var item in Player.m_localPlayer.GetInventory().GetAllItems().Where(i=>i.m_shared.m_itemType==ItemDrop.ItemData.ItemType.Trophy))
            {var trophy=item;Add("Mount "+Localization.instance.Localize(trophy.m_shared.m_name),()=>fittings.Mount(trophy));}
        }
        }
        const int perPage=5;int pages=Math.Max(1,(actions.Count+perPage-1)/perPage);page=Mathf.Clamp(page,0,pages-1);
        foreach(var entry in actions.Skip(page*perPage).Take(perPage).Select((a,i)=>(a,i)))
        {var a=entry.a;Button(body!,a.Label,0,-53-entry.i*46,672,()=>{if(yard&&yard.Near(Player.m_localPlayer)){a.Action();rebuild=true;}});}
        Button(body!,"‹",0,-295,80,()=>{page=(page+pages-1)%pages;rebuild=true;});
        theme!.Text(body!,"Decorations "+(page+1)+" / "+pages,104,-295,450,38,19,MenuTheme.Muted);
        Button(body!,"›",592,-295,80,()=>{page=(page+1)%pages;rebuild=true;});
    }
}
