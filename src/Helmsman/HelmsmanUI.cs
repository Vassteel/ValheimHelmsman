using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Helmsman;

public sealed partial class HelmsmanUI : MonoBehaviour
{
    internal bool IsOpen {get;private set;}
    private bool adjustingView, inputBlocked;
    private bool editing, onboard, rebuild, naming;
    private Ship? namingShip;
    private ItemDrop.ItemData? whistle;
    private SummonRequest? whistleRequestSnapshot;
    private GullCall? calledGull;
    private GullGuide? cargoSpeaker;
    private bool hadCargoTab;
    private Ship? CargoShip=>calledGull ? calledGull.Ship : onboard && Plugin.Instance.Voyage ? Plugin.Instance.Voyage.Ship : null;
    private bool CargoAvailable=>onboard && cargoSpeaker && CargoShip && cargoSpeaker.ReadyOn(CargoShip) &&
        QuartermasterBridge.Available && QuartermasterBridge.InRange(CargoShip);
    private string shipName="";
    private int tab, page;
    private DockMarker? dock;
    private Berth? draft;
    private Ship? selectedShip;
    private readonly List<Ship> nearby=new List<Ship>();
    private readonly List<Selectable> controls=new List<Selectable>();
    private readonly List<Action> refreshLabels=new List<Action>();
    private string notice="";
    private GameObject? preview, ghost, shield, modal, hud;
    private MenuTheme? theme;
    private TMP_Text? status, hudStatus;
    private RectTransform? body;
    private readonly List<LineRenderer> lines=new List<LineRenderer>();
    private float nextValidation, nextLabels;
    private bool valid;
    private string validation="Not fully checked";

    internal void OpenDock(DockMarker marker)
    {
        Close();dock=marker;editing=true;draft=marker.Settings.Copy();tab=0;
        RefreshShips();if(Open())CreatePreview();
    }
    internal void OpenDestinations(DockMarker marker)
    {
        Close();dock=marker;draft=marker.Settings.Copy();tab=2;RefreshShips();Open();
        notice=marker.Settings.configured ? "Choose a ship and destination." : "Configure this ward's berth first.";
    }
    internal void OpenVoyage(GullGuide? speaker=null)
    {
        if(!Plugin.Instance.Voyage || !Plugin.Instance.Voyage.GullReady)return;
        Close();onboard=true;cargoSpeaker=speaker;tab=2;Plugin.Instance.Voyage.CallGull();Open();
    }
    internal void OpenCalledGull(GullCall call,GullGuide? speaker=null)
    {
        if(!call.Ready || !Player.m_localPlayer || !call.Ship.IsPlayerInBoat(Player.m_localPlayer))return;
        Close();calledGull=call;onboard=true;cargoSpeaker=speaker;tab=2;Open();
    }
    internal void OpenShipName(Ship ship)
    {
        Close();naming=true;namingShip=ship;shipName=ShipDirectory.Display(ship);Open();
    }
    internal void OpenWhistle(ItemDrop.ItemData item)
    {
        if(!GullcallWhistle.Carried(Player.m_localPlayer,item))return;
        Close();whistle=item;Open();
    }
    private bool Open()
    {
        var font=MenuTheme.FindFont();
        if(!font || !GUIManager.CustomGUIFront)
        {Plugin.Message("The game menu is not ready yet; try again in a moment.");Close();return false;}
        theme=new MenuTheme(font);IsOpen=true;BlockInput(true);rebuild=true;return true;
    }
    private void BlockInput(bool value)
    {
        if(inputBlocked==value)return;
        GUIManager.BlockInput(value);inputBlocked=value;
    }
    private void AdjustView()
    {
        if(!editing || tab>=2)return;
        adjustingView=true;
        if(modal)modal.SetActive(false);
        if(shield)shield.SetActive(false);
        if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        BlockInput(false);
    }
    private void ReturnFromView()
    {
        adjustingView=false;BlockInput(true);
        if(modal)modal.SetActive(true);
        if(shield)shield.SetActive(true);
        if(ZInput.IsGamepadActive() && controls.Count>0)controls[0].Select();
    }
    internal void Close()
    {
        BlockInput(false);adjustingView=false;
        IsOpen=false;yard=null;calledGull=null;naming=false;namingShip=null;editing=false;onboard=false;rebuild=false;dock=null;draft=null;selectedShip=null;notice="";page=0;
        cargoSpeaker=null;hadCargoTab=false;
        whistle=null;whistleRequestSnapshot=null;
        controls.Clear();refreshLabels.Clear();
        if(EventSystem.current && modal && EventSystem.current.currentSelectedGameObject &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(modal.transform))
            EventSystem.current.SetSelectedGameObject(null);
        Remove(ref modal);Remove(ref shield);body=null;status=null;
        if(preview){Visuals.DestroyMaterials(preview);Destroy(preview);}
        preview=null;ghost=null;lines.Clear();
    }
    private static void Remove(ref GameObject? obj)
    {
        if(obj){obj.SetActive(false);Destroy(obj);}obj=null;
    }
    private void RefreshShips()
    {
        var previous=selectedShip;nearby.Clear();
        if(!dock)return;
        nearby.AddRange(FindObjectsByType<Ship>(FindObjectsSortMode.None).Where(s=>ShipProfile.Supports(s) &&
            Vector3.Distance(s.transform.position,dock.transform.position)<35).OrderBy(s=>Vector3.Distance(s.transform.position,dock.transform.position)));
        selectedShip=previous && nearby.Contains(previous) ? previous : nearby.Count==1 ? nearby[0] : null;
    }
    private void Update()
    {
        UpdateHud();
        if(!IsOpen)return;
        if(!Player.m_localPlayer || Player.m_localPlayer.IsDead() || !GUIManager.CustomGUIFront ||
            (!onboard && !dock && !naming && whistle==null && !yard) || (yard && !yard.Near(Player.m_localPlayer)) || (naming && !namingShip) ||
            (whistle!=null && (!Plugin.LocalSession || !GullcallWhistle.Carried(Player.m_localPlayer,whistle))) ||
            (onboard && !Plugin.Instance.Voyage && !calledGull) ||
            (calledGull && (!calledGull.Ready || !calledGull.Ship.IsPlayerInBoat(Player.m_localPlayer)))){Close();return;}
        if(adjustingView)
        {
            UpdatePreview();
            if(Input.GetKeyDown(Plugin.Instance.CallKey.Value) || Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
            {ZInput.ResetButtonStatus("JoyButtonB");ReturnFromView();}
            return;
        }
        if(Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
        {ZInput.ResetButtonStatus("JoyButtonB");Close();return;}
        // Rebuild after the input callback has completed, preserving live input controls while typing.
        if(whistle!=null && !ReferenceEquals(whistleRequestSnapshot,Plugin.Instance.Summon))rebuild=true;
        if(CargoAvailable!=hadCargoTab) { if(!CargoAvailable && tab==4)tab=2;rebuild=true; }
        if(dock&&!onboard&&tab==4&&scoutSnapshot!=ScoutState)rebuild=true;
        if(rebuild){BuildMenu();rebuild=false;}
        FitModal();
        if(editing && draft!=null)
        {
            UpdatePreview();
            if(Time.unscaledTime>nextValidation)
            {
                nextValidation=Time.unscaledTime+.5f;
                valid=new WaterChart(selectedShip,PreviewProfile()).ValidateBerth(draft,out validation);
            }
        }
        if(Time.unscaledTime>nextLabels)
        {
            nextLabels=Time.unscaledTime+.2f;
            foreach(var update in refreshLabels)update();
            if(status)status.text=notice.Length>0 ? notice : yard ? yard.Status : whistle!=null ? (Plugin.Instance.Summon ? Plugin.Instance.Summon.Status : "Stand near shore. The gull finds safe water nearby; no Dock Ward needed.") : tab==4&&!onboard ? IslandScouting.Instance.Status : tab==4 ? "Cargo moves only after your request." : calledGull ? "Choose a dock and the gull will guide you there." : onboard && Plugin.Instance.Voyage ? Plugin.Instance.Voyage.Status :
                naming ? "Ship names are saved with the world." : tab==3 ? "Choose an empty named ship to summon." :
                editing && tab<2 ? (valid ? "Clearance: " : "Advisory: ")+validation : "Select a named dock to set sail.";
        }
        Controller();
    }
    private void BuildMenu()
    {
        if(theme==null || !GUIManager.CustomGUIFront)return;
        controls.Clear();refreshLabels.Clear();Remove(ref modal);Remove(ref shield);
        var parent=GUIManager.CustomGUIFront.transform;
        var shieldRect=MenuTheme.Rect("Helmsman_InputShield",parent,0,0,0,0);
        shieldRect.anchorMin=Vector2.zero;shieldRect.anchorMax=Vector2.one;
        shieldRect.offsetMin=shieldRect.offsetMax=Vector2.zero;
        shield=shieldRect.gameObject;MenuTheme.Panel(shieldRect,new Color(0,0,0,.65f));
        var rect=MenuTheme.Rect("Helmsman_Menu",parent,0,0,720,620);modal=rect.gameObject;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
        MenuTheme.Panel(rect,MenuTheme.Background,true);
        theme.Text(rect,yard ? "CARPENTER’S TABLE" : whistle!=null ? "GULLCALL WHISTLE" : naming ? "SHIP NAME" : onboard ? "SHIP ORDERS" : "DOCK CONFIG",24,-16,360,38,28,MenuTheme.Gold);
        Button(rect,"Close",602,-18,94,Close);
        if(!naming && !yard)
        {
            var routeToggle=Button(rect,"",400,-18,186,()=>
            {
                Plugin.Instance.DebugRoute.Value=!Plugin.Instance.DebugRoute.Value;
                Plugin.Instance.Config.Save();
            });
            var label=routeToggle.GetComponentInChildren<TMP_Text>();
            Action updateRouteLabel=()=>label.text=Plugin.Instance.DebugRoute.Value ? "Hide route wisps" : "Show route wisps";
            updateRouteLabel();refreshLabels.Add(updateRouteLabel);
        }
        if(yard){BuildShipyardMenu(rect);return;}
        if(whistle!=null)
        {
            theme.Text(rect,"Give me a ship's name, Viking. I'll bring her in.",24,-68,672,45,21,MenuTheme.Muted);
            body=MenuTheme.Rect("Whistle orders",rect,24,-130,672,390);
            BuildWhistle();
            status=theme.Text(rect,"",24,-535,672,50,18,MenuTheme.Gold);
            theme.Text(rect,"D-pad ↑↓: select   A: activate   B / Esc: close",24,-596,672,18,15,MenuTheme.Muted);
            nextLabels=0;FitModal();
            if(ZInput.IsGamepadActive() && controls.Count>0)controls[0].Select();
            return;
        }
        if(naming)
        {
            theme.Text(rect,"Give this ship a name so the dock gull can find it.",24,-76,672,50,21,MenuTheme.Muted);
            controls.Add(theme.Input(rect,shipName,24,-150,672,v=>shipName=v));
            Button(rect,"Save ship name",24,-220,672,()=>
            {
                if(namingShip && ShipDirectory.Rename(namingShip,shipName,out var reason)){Plugin.Message(reason);Close();}
                else notice="Could not save. Stay near the ship and enter a name.";
            });
            status=theme.Text(rect,"",24,-280,672,70,19,MenuTheme.Gold);FitModal();return;
        }
        theme.Text(rect,tab==4&&!onboard ? "Ask the gull to chart this island." : tab==4 ? "Let me carry that cargo ashore, Viking." : calledGull ? "Where shall we sail? Choose a named dock below." : onboard ? "Speak to the gull to change course or end the voyage." : tab>=2 ? "Sail to a dock or summon a named ship" : "Ghost setup · No ship required",24,-60,onboard ? 672 : 482,28,18,MenuTheme.Muted);
        if(!onboard && tab<2)Button(rect,"Adjust view",522,-58,174,AdjustView);
        hadCargoTab=CargoAvailable;
        var tabs=onboard ? hadCargoTab ? new[]{"Destination","Unload cargo"} : new[]{"Destination"} : new[]{"Berth","Departure","Destinations","Summon ship","Scout island"};
        for(int i=0;i<tabs.Length;i++)
        {
            int index=i;
            float spacing=onboard ? (hadCargoTab ? 344 : 680) : 136;
            var button=Button(rect,tabs[i],24+i*spacing,-101,onboard ? (hadCargoTab ? 328 : 672) : 128,()=>SwitchTab(index));
            if(!onboard){var label=button.GetComponentInChildren<TMP_Text>();label.enableAutoSizing=true;label.fontSizeMin=13;label.fontSizeMax=18;}
            if((onboard ? (tab==4 ? 1 : 0) : tab)==i)button.GetComponent<Image>().color=MenuTheme.SelectedTab;
        }
        body=MenuTheme.Rect("Contents",rect,24,-155,672,382);
        if(tab==4 && onboard)BuildCargo();else if(tab==4)BuildScouting();else if(tab==3 && !onboard)BuildSummon();else if(onboard || tab==2)BuildDestinations();else if(tab==0)BuildBerth();else BuildDeparture();
        status=theme.Text(rect,"",24,-546,672,46,17,MenuTheme.Gold);
        status.enableAutoSizing=true;status.fontSizeMin=14;status.fontSizeMax=17;
        theme.Text(rect,"D-pad ↑↓: select   ←→: adjust   A: activate   B / Esc: close",24,-596,672,18,15,MenuTheme.Muted);
        nextLabels=0;FitModal();
        if(ZInput.IsGamepadActive() && controls.Count>0)controls[0].Select();
    }
    private void FitModal()
    {
        if(!modal || !GUIManager.CustomGUIFront)return;
        var bounds=(RectTransform)GUIManager.CustomGUIFront.transform;
        float scale=Mathf.Min(1,Mathf.Min((bounds.rect.width-24)/720,(bounds.rect.height-24)/620));
        modal.transform.localScale=Vector3.one*Mathf.Max(.1f,scale);
    }
    private void SwitchTab(int index)
    {
        if(onboard) { tab=index==1 && CargoAvailable ? 4 : 2;page=0;notice="";rebuild=true;return; }
        tab=index;page=0;notice="";
        if(tab<2 && !editing){editing=true;if(draft==null && dock)draft=dock.Settings.Copy();CreatePreview();}
        if(preview)preview.SetActive(tab<2);
        rebuild=true;
    }
    private Button Button(Transform parent,string label,float x,float y,float width,Action action)
    {
        var b=theme!.Button(parent,label,x,y,width,action);controls.Add(b);return b;
    }
    private void Slider(string title,float y,float min,float max,float value,string unit,Action<float> changed)
    {
        controls.Add(theme!.Slider(body!,title,y,min,max,value,unit,v=>{changed(v);notice="";nextValidation=0;}));
    }
    private void BuildBerth()
    {
        if(draft==null || !dock)return;
        var settings=draft;var origin=dock.transform.position;
        theme!.Text(body!,"Dock name",0,0,140,38,21,MenuTheme.Gold);
        controls.Add(theme.Input(body!,settings.name,156,0,516,v=>{settings.name=v;notice="";}));
        Slider("East / west",-51,-45,45,settings.position.x-origin.x," m",v=>settings.position=new Vector3(origin.x+v,WaterChart.Sea,settings.position.z));
        Slider("North / south",-120,-45,45,settings.position.z-origin.z," m",v=>settings.position=new Vector3(settings.position.x,WaterChart.Sea,origin.z+v));
        Slider("Bow direction",-189,0,360,settings.heading,"°",v=>settings.heading=v);
        ShipButton(0,-260,328,optional:true);
        var fromShip=Button(body!,"Copy ship position (optional)",346,-260,326,()=>
        {
            if(!selectedShip)return;
            settings.position=WaterChart.AtSea(selectedShip.transform.position);settings.heading=selectedShip.transform.eulerAngles.y;
            settings.shipType=ShipProfile.PrefabName(selectedShip);ResetPreview();
            notice="Berth positioned from selected ship. Save to apply.";nextValidation=0;rebuild=true;
        });
        refreshLabels.Add(()=>fromShip.interactable=selectedShip);
        Button(body!,"Refresh ships",0,-313,212,()=>{RefreshShips();nextValidation=0;});
        Button(body!,"Save berth",230,-313,442,SaveBerth);
        Button(body!,"Preview ship: "+settings.shipType,0,-353,672,()=>
        {
            var prefabs=Plugin.Instance.Ships.Prefabs;
            if(prefabs.Count==0)return;
            int current=prefabs.FindIndex(p=>p.name==settings.shipType);
            settings.shipType=prefabs[(current+1)%prefabs.Count].name;
            ResetPreview();rebuild=true;
        });
    }
    private void BuildDeparture()
    {
        if(draft==null)return;
        var settings=draft;
        var toggle=Button(body!,"",0,0,672,()=>{settings.reverseDeparture=!settings.reverseDeparture;notice="";nextValidation=0;rebuild=true;});
        var label=toggle.GetComponentInChildren<TMP_Text>();
        label.text=(settings.reverseDeparture ? "✓  " : "○  ")+"Reverse out, then turn"+(settings.reverseDeparture ? "  · ON" : "  · OFF");
        if(settings.reverseDeparture)toggle.GetComponent<Image>().color=MenuTheme.EnabledToggle;
        if(settings.reverseDeparture)Slider("Back out distance",-57,16,80,settings.reverseDistance," m",v=>settings.reverseDistance=v);
        else theme!.Text(body!,"The ship will depart forward along its bow direction.",0,-57,672,56,20,MenuTheme.Muted);
        theme!.Text(body!,"Cyan: arrival path. Orange: departure path.\nCircles mark the open water needed to align and turn.",0,-134,672,60,19,MenuTheme.Muted);
        theme.Text(body!,"Configure departure using the ghost. No ship needs to be present.",0,-214,672,42,18,MenuTheme.Muted);
        Button(body!,"Check clearance",0,-274,324,()=>{valid=new WaterChart(selectedShip,PreviewProfile()).ValidateBerth(settings,out validation);notice=validation;});
        Button(body!,"Save berth",346,-274,326,SaveBerth);
        theme.Text(body!,"The ship backs clear of the dock before beginning its turn.",0,-328,672,48,18,MenuTheme.Muted);
    }
    private void ShipButton(float x,float y,float width,bool optional=false)
    {
        var button=Button(body!,"Select a nearby ship",x,y,width,()=>
        {
            nearby.RemoveAll(s=>!s);
            if(nearby.Count==0)
            {
                notice=optional ? "No ship nearby. Position the ghost and save; a real ship is optional." : "No supported ship within 35 m. A ship is needed to start a voyage.";
                return;
            }
            int index=selectedShip ? nearby.IndexOf(selectedShip) : -1;
            selectedShip=nearby[(index+1)%nearby.Count];nextValidation=0;notice="";
        });
        var label=button.GetComponentInChildren<TMP_Text>();
        refreshLabels.Add(()=>
        {
            label.text=selectedShip && Player.m_localPlayer ? ShipDirectory.Display(selectedShip)+" · "+
                Vector3.Distance(selectedShip.transform.position,Player.m_localPlayer.transform.position).ToString("0")+" m  ›" : optional ? "Optional: choose ship to copy  ›" : "Select a nearby ship  ›";
        });
    }
    private void SaveBerth()
    {
        if(draft==null || !dock)return;
        draft.name=draft.name.Trim();
        if(!draft.ValidData){notice="Enter a dock name and valid berth values before saving.";return;}
        // Saving configuration is independent of current water, loading and obstacle conditions.
        // Actual maneuver checks use the active relaxed/strict navigation setting.
        var saved=draft.Copy();saved.configured=true;
        if(!dock.Save(saved)){notice="Could not save; stay near the ward.";return;}
        draft.configured=true;
        notice=valid ? "Berth save requested. The ward will confirm when saved." :
            "Berth save requested. Clearance advisory: "+validation;
    }
    private void BuildDestinations()
    {
        if(calledGull)
        {
            var call=calledGull;
            Button(body!,"Dismiss the gull",0,0,672,()=>{call.Dismiss("The gull is flying away.");Close();});
        }
        else if(onboard)
        {
            Button(body!,"Stop voyage",0,0,328,()=>{var voyage=Plugin.Instance.Voyage;if(voyage)voyage.Cancel("Voyage cancelled.");Close();});
            Button(body!,"What's happening?",344,0,328,()=>{if(Plugin.Instance.Voyage)Plugin.Instance.Voyage.ExplainStatus();});
        }
        else
        {
            ShipButton(0,0,442);Button(body!,"Refresh ships",460,0,212,()=>RefreshShips());
        }
        theme!.Text(body!,calledGull ? "Choose a dock to start your voyage." : onboard ? "Choose a dock to change destination or replot the course." : "Choose a named dock. Departure allows time to board.",0,-52,672,35,18,MenuTheme.Muted);
        var destinations=Plugin.Instance.Directory.Records.Where(d=>!dock || d.Id!=dock.Id).OrderBy(d=>d.Berth.name).ToList();
        int pages=Math.Max(1,(destinations.Count+2)/3);page=Mathf.Clamp(page,0,pages-1);
        for(int i=0;i<3 && page*3+i<destinations.Count;i++)
        {
            var destination=destinations[page*3+i];var berth=destination.Berth;
            Button(body!,berth.name+" — "+berth.position.x.ToString("0")+", "+berth.position.z.ToString("0"),0,-99-i*49,672,()=>SelectDestination(destination));
        }
        if(destinations.Count==0)theme.Text(body!,"No other configured docks found.\nSave another berth, then refresh the list.",0,-110,672,96,21,MenuTheme.Muted);
        var previous=Button(body!,"Previous",0,-258,160,()=>{page--;rebuild=true;});previous.interactable=page>0;
        theme.Text(body!,(page+1)+" / "+pages,178,-258,90,38,18,MenuTheme.Muted);
        var next=Button(body!,"Next",282,-258,160,()=>{page++;rebuild=true;});next.interactable=page<pages-1;
        Button(body!,"Refresh docks",460,-258,212,()=>{notice="";rebuild=true;});
        Button(body!,Plugin.Instance.ClearNavigationRocks.Value?"Rock clearing: ON":"Rock clearing: OFF",0,-313,328,()=>
        {
            Plugin.Instance.ClearNavigationRocks.Value=!Plugin.Instance.ClearNavigationRocks.Value;
            notice=Plugin.Instance.ClearNavigationRocks.Value?"Clears natural stone rocks while you sail with the gull. Changes to rocks are permanent.":"Rock clearing disabled.";
            rebuild=true;
        });
        Button(body!,Plugin.Instance.FishPassThrough.Value?"Fish pass-through: ON":"Fish pass-through: OFF",344,-313,328,()=>
        {
            Plugin.Instance.FishPassThrough.Value=!Plugin.Instance.FishPassThrough.Value;
            notice=Plugin.Instance.FishPassThrough.Value?"Fish pass through this boat while the gull steers.":"Normal fish collisions restored.";
            rebuild=true;
        });
    }
    private void BuildCargo()
    {
        var ship=CargoShip;
        if(!CargoAvailable || !ship || !cargoSpeaker) return;
        if(Plugin.Instance.Cargo)
        {
            var job=Plugin.Instance.Cargo;
            var text=theme!.Text(body!,job.Status,0,0,672,90,21,MenuTheme.Gold);
            refreshLabels.Add(()=> { if(job)text.text=job.Status;else {notice="Cargo request finished.";rebuild=true;} });
            Button(body!,"Stop unloading",0,-130,672,()=>{if(job)job.Stop("Cargo unloading stopped.");rebuild=true;});return;
        }
        string reason=QuartermasterBridge.Check(ship);
        if(reason.Length>0)
        {
            theme!.Text(body!,reason,0,0,672,90,21,MenuTheme.Gold);
            Button(body!,"Check again",0,-130,672,()=>rebuild=true);return;
        }
        theme!.Text(body!,"Unload this boat into matching Quartermaster storage nearby.\n\nI'll sort one cargo slot at a time. Anything without matching space stays aboard.",0,0,672,150,21,MenuTheme.Muted);
        Button(body!,"Unload cargo to base",0,-180,672,()=>
        {
            CargoOrder.Start(ship,cargoSpeaker,out var message);
            notice=message;rebuild=true;
        });
    }

    private void BuildSummon()
    {
        if(!dock)return;
        var request=Plugin.Instance.Summon;
        if(request)
        {
            theme!.Text(body!,request.Status,0,0,672,100,22,MenuTheme.Gold);
            Button(body!,"Cancel summon",0,-130,672,()=>{request.Cancel("Summon cancelled.");rebuild=true;});return;
        }
        theme!.Text(body!,"Choose an empty named ship to bring here. Name ships at the mast or helm.",0,0,672,66,19,MenuTheme.Muted);
        var ships=Plugin.Instance.Ships.Records.OrderBy(s=>s.Name).ToList();
        int pages=Math.Max(1,(ships.Count+3)/4);page=Mathf.Clamp(page,0,pages-1);
        for(int i=0;i<4 && page*4+i<ships.Count;i++)
        {
            var ship=ships[page*4+i];var delta=ship.Position-dock.transform.position;
            Button(body!,ship.Name+" ("+ship.Prefab+") · "+new Vector2(delta.x,delta.z).magnitude.ToString("0")+" m "+Helmsman.Core.ShipText.Bearing(delta.x,delta.z),0,-80-i*49,672,()=>
            {
                if(ScoutGullPresentation.BusyDock(dock.Id)){notice="This gull is scouting. Collect or cancel his survey first.";return;}
                if(SummonRequest.Begin(dock,ship,out var reason)){Close();Plugin.Message(reason);}else notice=reason;
            });
        }
        if(ships.Count==0)theme.Text(body!,"No named ships found yet. Name one, then refresh.",0,-90,672,90,21,MenuTheme.Muted);
        var previous=Button(body!,"Previous",0,-313,160,()=>{page--;rebuild=true;});previous.interactable=page>0;
        var next=Button(body!,"Next",180,-313,160,()=>{page++;rebuild=true;});next.interactable=page<pages-1;
        Button(body!,"Refresh list",360,-313,312,()=>rebuild=true);
    }
    private void BuildWhistle()
    {
        var request=Plugin.Instance.Summon;
        whistleRequestSnapshot=request;
        if(request)
        {
            theme!.Text(body!,"The gull sails your ship to the original calling spot. Arrival takes time; you can move on. Use the whistle again to check progress.",0,0,672,100,22,MenuTheme.Muted);
            Button(body!,"Cancel summon",0,-145,672,()=>{request.Cancel("Summon cancelled.");rebuild=true;});return;
        }
        if(Plugin.Instance.Voyage || Plugin.Instance.CalledGull)
        {theme!.Text(body!,"Finish or cancel your current voyage or gull visit before requesting another ship.",0,0,672,120,22,MenuTheme.Muted);return;}
        theme!.Text(body!,"Choose a ship to bring to your shoreline",0,0,672,46,23,MenuTheme.Gold);
        var ships=Plugin.Instance.Ships.Records.OrderBy(s=>s.Name).ToList();int count=ships.Count;
        page=Mathf.Clamp(page,0,Math.Max(0,(count-1)/4));
        for(int i=0;i<4 && page*4+i<count;i++)
        {
            var ship=ships[page*4+i];var distance=Vector3.Distance(ship.Position,Player.m_localPlayer.transform.position);
            Button(body!,ship.Name+" ("+ship.Prefab+") · "+distance.ToString("0")+" m",0,-60-i*52,672,()=>
            {
                if(!GullcallWhistle.Carried(Player.m_localPlayer,whistle)){Close();return;}
                if(SummonRequest.BeginShoreline(ship,out var reason)){notice="";rebuild=true;}else notice=reason;
            });
        }
        if(count==0)theme.Text(body!,"No named ships found. Name a ship at its mast or helm, then refresh.",0,-65,672,140,21,MenuTheme.Muted);
        theme.Text(body!,"I'll sail her to safe water beside this calling spot. You can move on.",0,-340,672,55,18,MenuTheme.Muted);
        var previous=Button(body!,"Previous",0,-282,160,()=>{page--;rebuild=true;});previous.interactable=page>0;
        var next=Button(body!,"Next",180,-282,160,()=>{page++;rebuild=true;});next.interactable=page+1<(count+3)/4;
        Button(body!,"Refresh list",360,-282,312,()=>rebuild=true);
    }
    private void SelectDestination(DockRecord destination)
    {
        var fresh=DockDirectory.Resolve(destination.Id);
        if(fresh==null){notice="That destination is no longer available.";return;}
        if(calledGull)
        {
            if(calledGull.Sail(fresh,out var message)){Close();Plugin.Message(message);}else notice=message;
            return;
        }
        if(onboard)
        {
            var voyage=Plugin.Instance.Voyage;
            if(voyage)voyage.ChangeDestination(fresh);Close();return;
        }
        if(dock&&ScoutGullPresentation.BusyDock(dock.Id)){notice="This gull is scouting. Collect his report before asking him to sail.";return;}
        if(!dock || !selectedShip){notice="Select the ship to use.";return;}
        var from=DockDirectory.Resolve(dock.Id);
        if(from==null){notice="Configure and save this ward's berth first.";return;}
        if(Voyage.Begin(selectedShip,from,fresh,out var reason)){Close();Plugin.Message(reason);return;}
        notice=reason;
    }
    private void Controller()
    {
        if(!EventSystem.current)return;
        var current=EventSystem.current.currentSelectedGameObject;
        if(current && current.GetComponent<TMP_InputField>()?.isFocused==true)return;
        var available=controls.Where(c=>c && c.IsActive() && c.IsInteractable()).ToList();
        if(available.Count==0)return;
        int index=available.FindIndex(c=>current && c.gameObject==current);
        bool down=ZInput.GetButtonDown("JoyDPadDown"),up=ZInput.GetButtonDown("JoyDPadUp");
        if(down || up || Input.GetKeyDown(KeyCode.Tab))
        {
            int delta=up || Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
            index=index<0 ? (delta>0 ? 0 : available.Count-1) : (index+delta+available.Count)%available.Count;
            available[index].Select();
            ZInput.ResetButtonStatus("JoyDPadDown");ZInput.ResetButtonStatus("JoyDPadUp");
        }
        if(index<0)return;
        var selected=available[index];
        int horizontal=ZInput.GetButtonDown("JoyDPadRight") ? 1 : ZInput.GetButtonDown("JoyDPadLeft") ? -1 : 0;
        if(selected is Slider slider && horizontal!=0)
        {
            slider.value+=horizontal*(slider.maxValue-slider.minValue)/100;
            ZInput.ResetButtonStatus("JoyDPadRight");ZInput.ResetButtonStatus("JoyDPadLeft");
        }
        if(ZInput.GetButtonDown("JoyButtonA") || Input.GetKeyDown(KeyCode.Return))
        {
            ZInput.ResetButtonStatus("JoyButtonA");
            if(selected is Button button)button.onClick.Invoke();
            else if(selected is TMP_InputField input)input.ActivateInputField();
        }
    }
    private void UpdateHud()
    {
        var voyage=Plugin.Instance.Voyage;
        var call=Plugin.Instance.CalledGull;
        if((!voyage && !call && !adjustingView) || !Player.m_localPlayer){Remove(ref hud);hudStatus=null;return;}
        if(!hud)
        {
            var font=MenuTheme.FindFont();
            var hotbar=Hud.instance ? Hud.instance.GetComponentInChildren<HotkeyBar>(true) : null;
            if(!font || !hotbar)return;
            var t=new MenuTheme(font);
            var rect=MenuTheme.Rect("Helmsman_VoyageStatus",hotbar.transform,0,-100,460,84);
            hud=rect.gameObject;MenuTheme.Panel(rect,MenuTheme.Background,true);
            hud.AddComponent<VoyageHudAnchor>().Bar=hotbar;
            hud.AddComponent<CanvasGroup>().blocksRaycasts=false;
            hudStatus=t.Text(rect,"",16,-8,428,68,18,MenuTheme.Gold);
            hudStatus.enableAutoSizing=true;hudStatus.fontSizeMin=14;hudStatus.fontSizeMax=18;
        }
        hud.SetActive(!IsOpen || adjustingView);
        if(hudStatus)hudStatus.text=adjustingView ? "Adjust the camera · Berth settings kept\n["+Plugin.Instance.CallKey.Value+"] / Esc / B: return to menu" :
            call ? call.Status+"\n["+Plugin.Instance.CallKey.Value+"] Speak after landing" :
            voyage ? voyage.Status+"\n["+Plugin.Instance.CallKey.Value+"] Call gull" : "";
    }

    private ShipProfile PreviewProfile()
    {
        var prefab=ShipDirectory.FindPrefab(draft?.shipType ?? "Karve") ?? ShipDirectory.FindPrefab("Karve");
        return ShipProfile.For(prefab ? prefab.GetComponent<Ship>() : null);
    }
    private void ResetPreview()
    {
        if(preview){Visuals.DestroyMaterials(preview);Destroy(preview);}preview=null;ghost=null;lines.Clear();
        CreatePreview();nextValidation=0;notice="";
    }
    private void CreatePreview()
    {
        preview=new GameObject("Helmsman berth preview");
        var prefab=ShipDirectory.FindPrefab(draft?.shipType ?? "Karve") ?? ShipDirectory.FindPrefab("Karve");
        if(prefab)ghost=Visuals.Clone(prefab,preview.transform,true);
        for(int i=0;i<5;i++)lines.Add(Visuals.Line(preview.transform,Color.cyan,overlay:true));
    }
    private void UpdatePreview()
    {
        if(!preview || draft==null)return;
        var p=WaterChart.AtSea(draft.position);var rot=Quaternion.Euler(0,draft.heading,0);
        if(ghost){ghost.transform.position=p;ghost.transform.rotation=rot;}
        var color=valid ? Color.green : new Color(1,.45f,.15f);
        var profile=PreviewProfile();
        var half=new Vector3(profile.Width/2+profile.Margin,0,profile.Length/2+profile.Margin);
        var hullCenter=p+rot*profile.Center;
        SetLine(0,new[]{hullCenter+rot*new Vector3(-half.x,0,-half.z),hullCenter+rot*new Vector3(-half.x,0,half.z),
            hullCenter+rot*new Vector3(half.x,0,half.z),hullCenter+rot*new Vector3(half.x,0,-half.z),hullCenter+rot*new Vector3(-half.x,0,-half.z)},color);
        SetLine(1,new[]{draft.Approach,p,p-rot*new Vector3(2,0,4),p,p-rot*new Vector3(-2,0,4)},Color.cyan);
        SetLine(2,new[]{p,draft.Exit},new Color(1,.6f,.15f));
        for(int line=3;line<=4;line++)
        {
            var center=line==3 ? draft.Exit : draft.Approach;var circle=new Vector3[37];
            for(int i=0;i<circle.Length;i++)circle[i]=center+Quaternion.Euler(0,i*10,0)*Vector3.forward*profile.TurnRadius;
            SetLine(line,circle,line==3 ? new Color(1,.6f,.15f) : Color.cyan);
        }
    }
    private void SetLine(int index,Vector3[] points,Color color)
    {
        for(int i=0;i<points.Length;i++)points[i].y=WaterChart.Sea+.5f;
        lines[index].positionCount=points.Length;lines[index].SetPositions(points);
        lines[index].startColor=lines[index].endColor=color;
    }
    private void OnDestroy(){Close();Remove(ref hud);}
}
