using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Helmsman;

public sealed class HuginTutorials : MonoBehaviour
{
    internal const string Dock="helmsman_dock_intro", Carpenter="helmsman_shipwright_intro", Whistle="helmsman_whistle_intro";
    private readonly Queue<string> pending=new();
    private readonly HashSet<string> queued=new();
    private Player? character;
    private float next;
    internal static void Queue(Player? player,string key)
    {
        if(!player||player!=Player.m_localPlayer||!Plugin.Instance)return;
        var tutorials=Plugin.Instance.GetComponent<HuginTutorials>();
        if(!tutorials)tutorials=Plugin.Instance.gameObject.AddComponent<HuginTutorials>();
        if(tutorials.character!=player)
        {tutorials.pending.Clear();tutorials.queued.Clear();tutorials.character=player;tutorials.next=0;}
        if(!player.HaveSeenTutorial(key)&&tutorials.queued.Add(key))tutorials.pending.Enqueue(key);
    }
    private void Update()
    {
        if(!Player.m_localPlayer||character!=Player.m_localPlayer)
        {pending.Clear();queued.Clear();character=null;return;}
        if(pending.Count==0||Time.unscaledTime<next||!Tutorial.instance||character.IsDead())return;
        if(Plugin.Instance.UI.IsOpen||InventoryGui.IsVisible()||Menu.IsVisible()||Console.IsVisible()||TextInput.IsVisible()||Chat.instance&&Chat.instance.HasFocus())return;
        var key=pending.Dequeue();
        if(character.HaveSeenTutorial(key))return;
        string title,text;
        if(key==Dock)
        {
            title="A berth for your ship";
            text="This Dock Ward marks a destination for the gull. Use it with [$KEY_Use] to name the dock and set a clear berth in deep enough water.\n\nAboard ship, briefly use the mast to hold fast; hold the use button to call the gull. Speak to him to choose a destination. Take the helm to stop his voyage.";
        }
        else if(key==Carpenter)
        {
            title="The puffin shipwright";
            text="Use the puffin's Carpenter's Table with [$KEY_Use] to commission ships or refit a nearby vessel. Place it beside a configured Dock Ward.\n\nChoose a ship with the menu arrows, choose its launch berth and pay the listed materials. Construction takes time. Keep the launch area clear; the puffin will launch the ship when it is ready.";
        }
        else
        {
            title="Call your ship home";
            text="Use the Gullcall Whistle from your inventory or hotbar near the shore. Choose your named, unoccupied ship and a safe landing spot.\n\nThe gull sails the ship to the place you called from; this is no instant summon. You may leave while he travels. The whistle can be used again.";
        }
        var tutorial=Tutorial.instance.m_texts.Find(t=>t.m_name==key);
        if(tutorial==null){tutorial=new Tutorial.TutorialText{m_name=key,m_isMunin=false};Tutorial.instance.m_texts.Add(tutorial);}
        tutorial.m_topic=title;tutorial.m_label="Helmsman";tutorial.m_text=text;
        character.ShowTutorial(key);
        next=Time.unscaledTime+20;
    }
}

[HarmonyPatch(typeof(Player),nameof(Player.TryPlacePiece))]
internal static class HuginAfterBuilding
{
    private static void Postfix(Player __instance,Piece piece,bool __result)
    {
        if(!__result||!piece)return;
        var name=Utils.GetPrefabName(piece.gameObject);
        if(name==Plugin.DockPrefab)HuginTutorials.Queue(__instance,HuginTutorials.Dock);
        else if(name==ImportedHulls.TablePrefab)HuginTutorials.Queue(__instance,HuginTutorials.Carpenter);
    }
}
[HarmonyPatch(typeof(Player),nameof(Player.AddKnownItem))]
internal static class HuginWhistleDiscovery
{
    private static void Postfix(Player __instance,ItemDrop.ItemData item)
    {if(GullcallWhistle.IsWhistle(item))HuginTutorials.Queue(__instance,HuginTutorials.Whistle);}
}
[HarmonyPatch(typeof(DockMarker),nameof(DockMarker.Interact))]
internal static class HuginDockUse
{
    private static void Postfix(Humanoid user,bool __result)
    {if(__result)HuginTutorials.Queue(user as Player,HuginTutorials.Dock);}
}
[HarmonyPatch(typeof(Shipyard),nameof(Shipyard.Interact))]
internal static class HuginCarpenterUse
{
    private static void Postfix(Humanoid user,bool __result)
    {if(__result)HuginTutorials.Queue(user as Player,HuginTutorials.Carpenter);}
}
[HarmonyPatch(typeof(Humanoid),nameof(Humanoid.UseItem))]
internal static class HuginWhistleUse
{
    private static void Postfix(Humanoid __instance,ItemDrop.ItemData item)
    {var player=__instance as Player;if(GullcallWhistle.Carried(player,item))HuginTutorials.Queue(player,HuginTutorials.Whistle);}
}
