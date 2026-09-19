using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Helmsman;

/// <summary>Vanilla inventory artwork and control treatment shared with Quartermaster.</summary>
internal sealed class MenuTheme
{
    internal static readonly Color Gold=new Color(.92f,.73f,.38f);
    internal static readonly Color Muted=new Color(.85f,.80f,.70f);
    internal static readonly Color Background=new Color(.19f,.13f,.08f,.99f);
    internal static readonly Color ButtonColor=new Color(.30f,.23f,.16f);
    internal static readonly Color InputColor=new Color(.12f,.09f,.06f);
    internal static readonly Color SelectedTab=new Color(.28f,.23f,.13f);
    internal static readonly Color EnabledToggle=new Color(.30f,.27f,.16f);
    private readonly TMP_FontAsset font;
    internal MenuTheme(TMP_FontAsset font){this.font=font;}

    internal static TMP_FontAsset? FindFont()
    {
        // Same source as Quartermaster: use the game's inventory action font.
        var gui=InventoryGui.instance;
        NativeMenuTheme.Capture(gui);
        if(gui && gui.m_stackAllButton)
        {
            var label=gui.m_stackAllButton.GetComponentInChildren<TMP_Text>(true);
            if(label && label.font)return label.font;
        }
        // Do not create unassigned TMP labels if another mod replaces that button.
        if(gui)
            foreach(var label in gui.GetComponentsInChildren<TMP_Text>(true))
                if(label.font)return label.font;
        return TMP_Settings.defaultFontAsset;
    }

    internal static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;
        var r=(RectTransform)go.transform;r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);return r;
    }
    internal static Image Panel(RectTransform rect,Color color,bool border=false)
    {
        var image=rect.gameObject.AddComponent<Image>();image.color=color;
        if(border)NativeMenuTheme.Panel(image);
        return image;
    }
    internal TMP_Text Text(Transform parent,string text,float x,float y,float width,float height,float size,Color color)
    {
        var label=Rect("Label",parent,x,y,width,height).gameObject.AddComponent<TextMeshProUGUI>();
        label.font=font;label.text=text;label.fontSize=size;label.color=color;label.raycastTarget=false;
        label.alignment=TextAlignmentOptions.MidlineLeft;label.textWrappingMode=TextWrappingModes.Normal;
        label.richText=false;NativeMenuTheme.Text(label,size>=22);return label;
    }
    internal Button Button(Transform parent,string label,float x,float y,float width,Action action)
    {
        var rect=Rect(label,parent,x,y,width,38);var image=Panel(rect,ButtonColor);
        var button=rect.gameObject.AddComponent<HelmsmanMenuButton>();button.targetGraphic=image;
        button.navigation=new Navigation{mode=Navigation.Mode.None};
        var colors=button.colors;colors.highlightedColor=new Color(1.4f,1.4f,1.4f);
        colors.selectedColor=new Color(1.8f,1.6f,1.1f);button.colors=colors;
        var text=Text(rect,label,9,0,width-18,38,18,Color.white);
        text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;text.fontSizeMin=14;text.fontSizeMax=18;
        NativeMenuTheme.Button(button);button.onClick.AddListener(()=>action());return button;
    }
    internal TMP_InputField Input(Transform parent,string value,float x,float y,float width,Action<string> changed)
    {
        var rect=Rect("Dock name",parent,x,y,width,38);var image=Panel(rect,InputColor);
        var field=rect.gameObject.AddComponent<TMP_InputField>();field.targetGraphic=image;
        var viewport=Rect("Viewport",rect,10,-2,width-20,34);viewport.gameObject.AddComponent<RectMask2D>();
        field.textViewport=viewport;field.textComponent=Text(viewport,"",0,0,width-20,34,20,Color.white);
        field.fontAsset=font;field.placeholder=Text(viewport,"Name this dock",0,0,width-20,34,18,Muted);
        field.characterLimit=48;field.contentType=TMP_InputField.ContentType.Standard;
        field.lineType=TMP_InputField.LineType.SingleLine;field.text=value;
        field.navigation=new Navigation{mode=Navigation.Mode.None};
        NativeMenuTheme.Input(field);field.onValueChanged.AddListener(v=>changed(v));return field;
    }
    internal Slider Slider(Transform parent,string title,float y,float min,float max,float value,string unit,Action<float> changed)
    {
        var label=Text(parent,title+": "+value.ToString("0.0")+unit,0,y,672,28,19,Muted);
        var rect=Rect(title,parent,0,y-32,672,24);
        var slider=rect.gameObject.AddComponent<HelmsmanMenuSlider>();
        Panel(Rect("Track",rect,0,-8,672,8),InputColor).raycastTarget=false;
        var fillArea=Rect("Fill area",rect,8,-8,656,8);
        var fill=Rect("Fill",fillArea,0,0,0,0);fill.pivot=new Vector2(.5f,.5f);Panel(fill,Gold).raycastTarget=false;
        var handleArea=Rect("Handle area",rect,8,0,656,24);
        var handle=Rect("Handle",handleArea,0,0,16,0);handle.pivot=new Vector2(.5f,.5f);
        var handleImage=Panel(handle,Gold);
        slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handleImage;
        slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;slider.minValue=min;slider.maxValue=max;
        slider.SetValueWithoutNotify(value);slider.navigation=new Navigation{mode=Navigation.Mode.None};
        slider.onValueChanged.AddListener(v=>{label.text=title+": "+v.ToString("0.0")+unit;changed(v);});
        // A clear target across the full track makes pointer dragging easier.
        var hit=rect.gameObject.AddComponent<Image>();hit.color=Color.clear;
        NativeSlider(slider);return slider;
    }
    internal Scrollbar VerticalScrollbar(Transform parent,float x,float y,float width,float height)
    {
        var rect=Rect("Materials scrollbar",parent,x,y,width,height);
        var track=Panel(rect,InputColor);
        var area=Rect("Sliding area",rect,2,-2,width-4,height-4);
        var handle=Rect("Handle",area,0,0,0,0);
        var image=Panel(handle,Gold);
        var bar=rect.gameObject.AddComponent<HelmsmanMenuScrollbar>();
        bar.handleRect=handle;bar.targetGraphic=image;bar.direction=Scrollbar.Direction.BottomToTop;
        bar.navigation=new Navigation{mode=Navigation.Mode.None};bar.value=1;
        var colors=bar.colors;colors.highlightedColor=new Color(1.4f,1.4f,1.4f);
        colors.selectedColor=new Color(1.8f,1.6f,1.1f);bar.colors=colors;
        var gui=InventoryGui.instance;
        if(gui)foreach(var source in gui.GetComponentsInChildren<Scrollbar>(true))
        {
            if(source.direction!=Scrollbar.Direction.BottomToTop&&source.direction!=Scrollbar.Direction.TopToBottom)continue;
            NativeMenuTheme.Control(bar,source);
            NativeMenuTheme.Image(track,source.GetComponent<Image>());
            break;
        }
        return bar;
    }
    private static void NativeSlider(Slider slider)
    {
        var gui=InventoryGui.instance;var source=gui&&gui.m_splitDialog?gui.m_splitDialog.GetComponentInChildren<Slider>(true):null;
        if(!source)return;
        NativeMenuTheme.Control(slider,source);
        if(source.fillRect&&slider.fillRect)NativeMenuTheme.Image(slider.fillRect.GetComponent<Image>(),source.fillRect.GetComponent<Image>());
        var track=slider.transform.Find("Track")?.GetComponent<Image>();
        var nativeTrack=source.transform.Find("Background")?.GetComponent<Image>();if(track&&nativeTrack)NativeMenuTheme.Image(track,nativeTrack);
    }

}

// ZInput handles controller activation once; pointer activation remains native.
public sealed class HelmsmanMenuButton : Button
{
    public override void OnSubmit(BaseEventData eventData){}
}
public sealed class HelmsmanMenuSlider : Slider
{
    public override void OnMove(AxisEventData eventData){}
}
public sealed class HelmsmanMenuScrollbar : Scrollbar
{
    public override void OnMove(AxisEventData eventData){}
}
