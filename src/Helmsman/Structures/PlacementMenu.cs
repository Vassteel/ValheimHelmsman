#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Helmsman.Structures
{
    public sealed partial class PlacementTool
    {
        private GameObject menuRoot;
        private RectTransform panel;
        private MenuTheme theme;
        private TMP_Text live;
        private readonly List<Selectable> controls=new List<Selectable>();
        private string menuState="";
        private int filePage;
        private void DestroyMenu()
        {
            if(menuRoot){menuRoot.SetActive(false);Destroy(menuRoot);}
            menuRoot=null;panel=null;live=null;controls.Clear();
        }
        private void CloseMenu(){browser=false;confirm=false;ResetPreview();SetInput(false);}
        private Button Action(Transform parent,string title,float x,float y,float width,System.Action action)
        {var b=theme.Button(parent,title,x,y,width,action);controls.Add(b);return b;}
        private void LateUpdate()
        {
            bool visible=browser||confirm||busy||preview;
            if(!visible){if(menuRoot)DestroyMenu();menuState="";return;}
            if(!GUIManager.CustomGUIFront)return;
            var state=$"{browser}/{confirm}/{busy}/{preview}/{filePage}/{error}/{shape}";
            if(!menuRoot||state!=menuState)
            {
                var font=MenuTheme.FindFont();if(!font){CloseMenu();return;}
                DestroyMenu();theme=new MenuTheme(font);menuState=state;BuildMenu();
            }
            bool modal=browser||confirm||busy;
            var bounds=(RectTransform)GUIManager.CustomGUIFront.transform;
            panel.localScale=Vector3.one*Mathf.Max(.1f,Mathf.Min(1,Mathf.Min((bounds.rect.width-24)/720,(bounds.rect.height-24)/(modal?650:155))));
            if(live)
            {
                if(busy)live.text=$"Placing structure… {lastPieces.Count} pieces. Keep the game open.";
                else if(confirm)live.text=$"Rotation {yaw%360:0.#}°   Height offset {height:0.0} m";
                else if(!browser)live.text=$"{label}\n"+(ZInput.IsGamepadActive()?(ZInput.InputLayout==InputLayout.Default?"Rotate: hold LT + right stick left/right":"Rotate: LT / RT")+" · B: choose structure":"Rotate: mouse wheel or [ / ] · F: lock · PgUp/PgDn: height")+$"\nShift: fine height · Enter: review · Esc: choose structure\nHeight {height:0.0} m · Rotation {yaw%360:0.#}° · "+(!hit?"Aim at loaded ground within 100 m":locked?"Position locked":"Position follows aim");
            }
            if(modal&&!busy)Controller();
        }
        private void BuildMenu()
        {
            bool modal=browser||confirm||busy;
            var root=MenuTheme.Rect("POI menu",GUIManager.CustomGUIFront.transform,0,0,0,0);
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.sizeDelta=Vector2.zero;
            menuRoot=root.gameObject;
            if(modal)MenuTheme.Panel(root,new Color(0,0,0,.45f));
            else root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts=false;
            panel=MenuTheme.Rect("Puffin construction",root,0,0,720,modal?650:155);
            panel.anchorMin=panel.anchorMax=modal?new Vector2(.5f,.5f):new Vector2(0,0);
            panel.pivot=modal?new Vector2(.5f,.5f):new Vector2(0,0);
            panel.anchoredPosition=modal?Vector2.zero:new Vector2(24,24);
            MenuTheme.Panel(panel,MenuTheme.Background,true);
            if(!modal){live=theme.Text(panel,"",20,-12,680,132,18,MenuTheme.Muted);return;}
            theme.Text(panel,"Puffin construction",24,-18,590,38,28,MenuTheme.Gold);
            theme.Text(panel,busy?"Building structure":confirm?"Review placement":"Choose a structure",24,-60,672,28,20,MenuTheme.Muted);
            if(error.Length>0){var warning=theme.Text(panel,error,24,-94,672,86,17,MenuTheme.Gold);warning.enableAutoSizing=true;warning.fontSizeMin=12;}
            if(busy)live=theme.Text(panel,"",24,-190,672,100,22,MenuTheme.Gold);
            else if(browser)BuildFiles();
            else if(confirm)BuildConfirmation();
            if(!busy)Action(panel,"Close",548,-592,148,CloseMenu);
            theme.Text(panel,"D-pad ↑↓: select   A: activate   "+(confirm?"B / Esc: choose structure":"B / Esc: close"),24,-602,510,24,15,MenuTheme.Muted);
            if(ZInput.IsGamepadActive()&&controls.Count>0)controls[0].Select();
        }
        private void BuildFiles()
        {
            filePage=Mathf.Clamp(filePage,0,Mathf.Max(0,(files.Length-1)/6));
            for(int i=filePage*6;i<Math.Min(files.Length,filePage*6+6);i++)
            {string path=files[i];Action(panel,Path.GetFileName(path),24,-190-(i%6)*44,672,()=>Select(path));}
            if(files.Length==0)theme.Text(panel,"Add .blueprint or .vbuild files to your imports folder, then speak to the puffin again.",24,-190,672,100,20,MenuTheme.Muted);
            if(files.Length>6)
            {
                Action(panel,"‹ Previous",24,-458,160,()=>filePage=Mathf.Max(0,filePage-1));
                theme.Text(panel,$"Page {filePage+1} / {(files.Length+5)/6}",220,-458,220,38,18,MenuTheme.Muted);
                Action(panel,"Next ›",536,-458,160,()=>filePage=Mathf.Min((files.Length-1)/6,filePage+1));
            }
            theme.Text(panel,StructureImports.Instance.ResolvedImportFolder,24,-504,672,34,14,MenuTheme.Muted);
            if(lastTerrain!=null)Action(panel,"Restore interrupted terrain work",24,-548,320,()=>StartCoroutine(UndoLast()));

        }
        private void BuildConfirmation()
        {
            theme.Text(panel,$"{label} · {blueprint.Pieces.Count} pieces",24,-182,672,35,21,Color.white);
            live=theme.Text(panel,"",24,-220,672,30,19,MenuTheme.Muted);
            Action(panel,"Rotate −22.5°",24,-260,160,()=>RotatePreview(-1));
            Action(panel,"Rotate +22.5°",194,-260,160,()=>RotatePreview(1));
            Action(panel,"Lower 0.5 m",364,-260,160,()=>height-=.5f);
            Action(panel,"Raise 0.5 m",534,-260,162,()=>height+=.5f);
            Action(panel,"Blend terrain: "+(shape?"On":"Off"),24,-310,672,()=>shape=!shape);
            var body=MenuTheme.Rect("Terrain controls",panel,24,-350,672,140);
            var slider=theme.Slider(body,"Curved terrain shoulder",0,4,30,blend," m",v=>blend=v);slider.interactable=shape;controls.Add(slider);
            var level=theme.Slider(body,"Terrain height above foundation",66,0,6,terrainHeight," m",v=>terrainHeight=v);level.interactable=shape;controls.Add(level);
            theme.Text(panel,dataWarning.Length>0?dataWarning:"Cyan grid: terrain height. Trees, bushes and grass inside the footprint are cleared. Materials come from Quartermaster or the puffin bench.",24,-490,672,45,17,MenuTheme.Muted);
            Action(panel,"Back to positioning",24,-540,328,()=>{confirm=false;SetInput(false);});
            Action(panel,"Queue construction",368,-540,328,()=>StartCoroutine(Place()));
        }
        private void Controller()
        {
            if(!ZInput.IsGamepadActive()||controls.Count==0||!EventSystem.current)return;
            var selected=EventSystem.current.currentSelectedGameObject;
            int index=controls.FindIndex(c=>c&&c.gameObject==selected);
            int delta=ZInput.GetButtonDown("JoyDPadDown")?1:ZInput.GetButtonDown("JoyDPadUp")?-1:0;
            if(delta!=0){ZInput.ResetButtonStatus(delta>0?"JoyDPadDown":"JoyDPadUp");index=(index+delta+controls.Count)%controls.Count;controls[index].Select();}
            if(index<0)return;
            if(controls[index] is Button button&&button.interactable&&ZInput.GetButtonDown("JoyButtonA"))
            {ZInput.ResetButtonStatus("JoyButtonA");button.onClick.Invoke();}
            if(controls[index] is Slider slider&&slider.interactable)
            {if(ZInput.GetButtonDown("JoyDPadLeft")){ZInput.ResetButtonStatus("JoyDPadLeft");slider.value-=1;}if(ZInput.GetButtonDown("JoyDPadRight")){ZInput.ResetButtonStatus("JoyDPadRight");slider.value+=1;}}
        }
    }
}
