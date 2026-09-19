using System;
using System.Collections.Generic;
using Helmsman.Structures;
using UnityEngine;

static class InputRegression
{
    internal static void Run(Action<bool,string> check)
    {
        void Frame(params KeyCode[] keys){Time.frameCount++;ZInput.down.Clear();ZInput.held.Clear();ZInput.buttons.Clear();ZInput.back=false;ZInput.scroll=0;ZInput.stick=0;ZInput.gamepad=false;foreach(var key in keys)ZInput.down.Add(key);}
        var t=new PlacementTool{preview=true,hit=true};
        Frame(KeyCode.RightBracket);t.PositionKeysForTest();check(t.yaw==22.5f,"Right bracket rotates preview using Valheim input");
        Frame(KeyCode.LeftBracket);t.PositionKeysForTest();check(t.yaw==0,"Left bracket rotates preview back");
        Frame(KeyCode.LeftBracket);t.PositionKeysForTest();check(t.yaw==337.5f,"Preview rotation wraps correctly");
        Frame(KeyCode.PageUp);t.PositionKeysForTest();check(t.height==.5f,"Normal height step");
        Frame(KeyCode.PageDown);ZInput.held.Add(KeyCode.RightShift);t.PositionKeysForTest();check(Math.Abs(t.height-.4f)<.001f,"Either shift enables fine height step");
        Frame(KeyCode.KeypadEnter);t.PositionKeysForTest();check(t.confirm&&t.locked&&t.inputBlocked,"Keypad Enter opens review and locks anchor");
        foreach(bool review in new[]{false,true})
        {
            t=new PlacementTool{preview=true,confirm=review};Frame(KeyCode.Escape);
            check(t.SuppressGameMenu,"POI blocks pause before its own update");
            check(t.BackForTest()&&t.browser&&!t.preview&&!t.confirm&&t.inputBlocked,"Escape returns placement or review to selection");
            check(t.SuppressGameMenu,"Escape cannot open pause after selection transition");
            check(!t.BackForTest()&&t.browser,"Same frame cannot also close selection");
            Frame(KeyCode.Escape);check(t.BackForTest()&&!t.browser&&!t.inputBlocked,"Separate Escape closes selection");
            check(t.SuppressGameMenu,"Closing selection consumes the pause key for the whole frame");
            Frame();check(!t.SuppressGameMenu,"Normal pause behavior returns after POI closes");
        }
        t=new PlacementTool{preview=true};Frame();ZInput.back=true;
        check(t.BackForTest()&&t.browser&&!ZInput.back,"Controller Back also returns to selection and is consumed");
        t=new PlacementTool{busy=true,preview=true};Frame(KeyCode.Escape);
        check(t.BackForTest()&&t.busy&&t.preview&&!t.browser,"Escape does not interrupt placement mutation");
        t=new PlacementTool();Frame(KeyCode.Escape);check(!t.BackForTest()&&!t.SuppressGameMenu,"Inactive tool leaves normal Escape alone");
        t=new PlacementTool{preview=true,hit=false};Frame(KeyCode.Return);t.PositionKeysForTest();check(!t.confirm,"Review still requires a valid terrain hit");
        t=new PlacementTool{preview=true};Frame();ZInput.scroll=.06f;t.PositionKeysForTest();check(t.yaw==0,"Small wheel deltas accumulate");
        Frame();ZInput.scroll=.06f;t.PositionKeysForTest();check(t.yaw==22.5f,"Wheel rotates after native scroll threshold");
        Frame();ZInput.scroll=-1;t.PositionKeysForTest();check(t.yaw==0,"Opposite wheel direction reverses rotation");
        Frame();ZInput.gamepad=true;ZInput.InputLayout=InputLayout.Default;ZInput.stick=.8f;t.PositionKeysForTest();check(t.yaw==0,"Unmodified right stick keeps camera control");
        ZInput.buttons.Add("JoyRotate");Time.unscaledTime=1;t.PositionKeysForTest();check(t.yaw==337.5f,"Classic controller LT and right stick rotates");
        Time.unscaledTime=1.1f;t.PositionKeysForTest();check(t.yaw==337.5f,"Held rotation waits before repeating");
        Time.unscaledTime=1.26f;t.PositionKeysForTest();check(t.yaw==315,"Held rotation repeats at a controlled rate");
        ZInput.stick=-.8f;t.PositionKeysForTest();check(t.yaw==337.5f,"Controller direction reversal responds immediately");
        foreach(var layout in new[]{InputLayout.Alternative1,InputLayout.Alternative2})
        {
            t=new PlacementTool{preview=true};Frame();ZInput.gamepad=true;ZInput.InputLayout=layout;ZInput.buttons.Add("JoyRotate");t.PositionKeysForTest();
            check(t.yaw==337.5f,"Alternate layout left rotation trigger");
            ZInput.buttons.Clear();ZInput.buttons.Add("JoyRotateRight");t.PositionKeysForTest();check(t.yaw==0,"Alternate layout right rotation trigger");
        }
        check(!t.ReadingPlacementInput,"Own input sampling always releases capture");
        t.confirm=true;check(!t.Positioning&&!t.ControllerRotating,"Review does not capture ghost rotation input");
    }
}
namespace UnityEngine
{
    public enum KeyCode {Escape,LeftBracket,RightBracket,LeftShift,RightShift,PageUp,PageDown,Return,KeypadEnter}
    public static class Time {public static int frameCount;public static float unscaledTime;}
}
enum InputLayout {Default,Alternative1,Alternative2}
static class ZInput
{
    internal static HashSet<KeyCode> down=new HashSet<KeyCode>(),held=new HashSet<KeyCode>();internal static bool back;
    internal static HashSet<string> buttons=new HashSet<string>();internal static bool gamepad;internal static float scroll,stick;
    internal static InputLayout InputLayout;
    public static bool IsGamepadActive()=>gamepad;
    public static bool GetButton(string name)=>buttons.Contains(name);
    public static float GetMouseScrollWheel()=>scroll;
    public static float GetJoyRightStickX()=>stick;
    public static bool GetKeyDown(KeyCode key)=>down.Contains(key);
    public static bool GetKey(KeyCode key)=>held.Contains(key);
    public static bool GetButtonDown(string name)=>back;
    public static void ResetButtonStatus(string name){back=false;}
}
namespace Helmsman.Structures
{
    public sealed partial class PlacementTool
    {
        internal bool browser,confirm,preview,busy,locked,hit,inputBlocked;
        internal float yaw,height;private string error="",menuState="";
        private void ResetPreview(){preview=false;locked=false;ResetPositioningInput();}
        private void SetInput(bool value){inputBlocked=value;}
        internal bool BackForTest()=>HandleBackInput();
        internal void PositionKeysForTest()=>UpdatePositioningKeys();
    }
}
