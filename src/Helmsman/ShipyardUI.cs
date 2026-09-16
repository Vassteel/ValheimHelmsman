using System;
using System.Collections.Generic;
using System.Linq;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

public sealed partial class HelmsmanUI
{
    private Shipyard? yard;
    private int yardDockIndex,blueprintIndex;
    private float yardNoticeUntil;
    private void YardNotice(string text){notice=text;yardNoticeUntil=Time.unscaledTime+5;}
    internal void OpenShipyard(Shipyard table)
    {
        Close();yard=table;tab=0;page=0;RefreshRefitShips();Open();
    }
    private void RefreshRefitShips()
    {
        nearby.Clear();if(!yard)return;
        nearby.AddRange(FindObjectsByType<Ship>(FindObjectsSortMode.None).Where(s=>(Shipwright.Supports(s)||s.GetComponent<ShipCosmetics>())&&Vector3.Distance(s.transform.position,yard.transform.position)<35));
        if(!selectedShip||!nearby.Contains(selectedShip))selectedShip=nearby.FirstOrDefault();
    }
    private void BuildShipyardMenu(RectTransform rect)
    {
        theme!.Text(rect,"Puffin shipwright",24,-65,672,32,22,MenuTheme.Gold);
        Button(rect,"Commission a ship",24,-108,328,()=>{tab=0;page=0;rebuild=true;});
        Button(rect,"Refits and styles",368,-108,328,()=>{tab=1;page=0;RefreshRefitShips();rebuild=true;});
        body=MenuTheme.Rect("Shipyard orders",rect,24,-164,672,360);
        if(tab==0)BuildCommission();else BuildRefits();
        status=theme.Text(rect,"",24,-535,672,54,17,MenuTheme.Gold);
        refreshLabels.Add(()=>{if(status&&yard)status.text=notice.Length>0&&Time.unscaledTime<yardNoticeUntil?notice:yard.Status;});
        theme.Text(rect,"D-pad ↑↓: select   A: activate   B / Esc: close",24,-596,672,18,15,MenuTheme.Muted);
        nextLabels=0;FitModal();
        if(ZInput.IsGamepadActive()&&controls.Count>0)controls[0].Select();
    }
    private void BuildCommission()
    {
        if(!yard)return;
        var choices=ShipConstruction.Blueprints;
        blueprintIndex=Mathf.Clamp(blueprintIndex,0,choices.Count-1);
        var blueprint=choices[blueprintIndex];
        Button(body!,"‹",0,0,48,()=>{blueprintIndex=(blueprintIndex+choices.Count-1)%choices.Count;rebuild=true;});
        theme!.Text(body!,blueprint.Name,66,0,536,40,25,MenuTheme.Gold);
        Button(body!,"›",624,0,48,()=>{blueprintIndex=(blueprintIndex+1)%choices.Count;rebuild=true;});
        theme.Text(body!,Shipwright.Requirements(new ShipUpgrade(blueprint.Id,blueprint.Name,blueprint.Recipe,"$piece_workbench",1)),0,-62,672,80,20,MenuTheme.Muted);
        var docks=yard.Docks;
        yardDockIndex=docks.Count>0?yardDockIndex%docks.Count:0;
        var dock=docks.Count>0?docks[yardDockIndex]:null;
        Button(body!,dock==null?"No configured Dock Ward nearby":"Launch at: "+dock.Berth.name+"  ›",0,-158,672,()=>{yardDockIndex++;rebuild=true;});
        theme.Text(body!,"Build time: "+Shipyard.FormatDuration(Shipyard.Duration(blueprint))+". Pay materials to start; she launches when the berth is clear.",0,-212,672,66,19,MenuTheme.Muted);
        var button=Button(body!,yard.Busy?"Construction in progress":"Pay materials and begin building",0,-295,672,()=>
        {if(yard&&dock!=null){YardNotice(yard.Commission(blueprint,dock));rebuild=true;}});
        button.interactable=!yard.Busy&&dock!=null;
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
        foreach(var upgrade in ShipwrightRules.Upgrades)
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
        theme!.Text(body!,"Refits "+(page+1)+" / "+pages,104,-295,450,38,19,MenuTheme.Muted);
        Button(body!,"›",592,-295,80,()=>{page=(page+1)%pages;rebuild=true;});
    }
}
