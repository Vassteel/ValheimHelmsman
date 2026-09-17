using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Helmsman;

internal enum RockClearResult {NotApplicable,Waiting,Cleared,Blocked}

internal static class RockClearing
{
    private static readonly Func<MineRock,Collider,int> OldIndex=AccessTools.MethodDelegate<Func<MineRock,Collider,int>>(AccessTools.Method(typeof(MineRock),"GetAreaIndex"));
    private static readonly Func<MineRock5,Collider,int> NewIndex=AccessTools.MethodDelegate<Func<MineRock5,Collider,int>>(AccessTools.Method(typeof(MineRock5),"GetAreaIndex"));
    private static readonly Action<MineRock,long,HitData,int> OldHit=AccessTools.MethodDelegate<Action<MineRock,long,HitData,int>>(AccessTools.Method(typeof(MineRock),"RPC_Hit"));
    private static readonly Action<MineRock5,long,HitData,int> NewHit=AccessTools.MethodDelegate<Action<MineRock5,long,HitData,int>>(AccessTools.Method(typeof(MineRock5),"RPC_Damage"));
    private static readonly Action<Destructible,long,HitData> WholeHit=AccessTools.MethodDelegate<Action<Destructible,long,HitData>>(AccessTools.Method(typeof(Destructible),"RPC_Damage"));
    private static HashSet<ItemDrop>? clearingDrops;
    internal static void Capture(ItemDrop drop)
    {if(drop&&clearingDrops!=null)clearingDrops.Add(drop);}

    // Classification fails closed: only naturally spawned stone-only rocks.
    // Ore, build pieces, creatures, terrain and location props cannot be cleared.
    internal static bool StoneOnly(DropTable? drops)
    {
        if(drops==null||drops.m_drops.Count==0)return false;
        return drops.m_drops.All(d=>d.m_item&&d.m_item.name=="Stone"&&d.m_item.GetComponent<ItemDrop>());
    }
    private static bool NaturalName(string name)=>name.StartsWith("rock",StringComparison.OrdinalIgnoreCase)||name.StartsWith("MineRock_Stone",StringComparison.OrdinalIgnoreCase);
    internal static bool Eligible(Collider obstacle,out Component? rock,out DropTable? drops)
    {
        rock=null;drops=null;
        if(!obstacle||obstacle.GetComponentInParent<Piece>()||obstacle.GetComponentInParent<Ship>()||obstacle.GetComponentInParent<Character>())return false;
        var old=obstacle.GetComponentInParent<MineRock>();
        var current=obstacle.GetComponentInParent<MineRock5>();
        if(old){rock=old;drops=old.m_dropItems;}
        else if(current){rock=current;drops=current.m_dropItems;}
        else
        {
            var whole=obstacle.GetComponentInParent<Destructible>();if(!whole)return false;
            // Native intact boulders may replace themselves with a MineRock5.
            var fractured=whole.m_spawnWhenDestroyed?whole.m_spawnWhenDestroyed.GetComponent<MineRock5>():null;
            if(whole.m_spawnWhenDamaged)return false;
            if(whole.m_spawnWhenDestroyed&&!fractured)return false;
            var extra=whole.GetComponent<DropOnDestroyed>();
            if(extra&&!StoneOnly(extra.m_dropWhenDestroyed))return false;
            drops=fractured?fractured.m_dropItems:extra?extra.m_dropWhenDestroyed:null;
            rock=whole;
        }
        if(!NaturalName(rock!.name)||!StoneOnly(drops)){rock=null;return false;}
        return true;
    }
    internal static bool CanPlanThrough(Collider obstacle)
    {
        if(!Eligible(obstacle,out var rock,out _))return false;
        var view=rock!.GetComponent<ZNetView>();
        return view&&view.IsValid()&&view.IsOwner()&&PrivateArea.CheckAccess(rock.transform.position,0,false,true)&&PrivateArea.CheckAccess(obstacle.bounds.center,0,false,true);
    }
    internal static RockClearResult TryClear(Ship ship,Player player,Collider obstacle,float speed,bool ready,out string message)
    {
        message="";
        if(!ship||!player||player!=Player.m_localPlayer||!ship.IsOwner()||!ship.IsPlayerInBoat(player)||player.IsDead()||ship.m_shipControlls.HaveValidUser()||
            !Eligible(obstacle,out var rock,out _))return RockClearResult.NotApplicable;
        var point=obstacle.ClosestPoint(ship.transform.position);
        var profile=ShipProfile.For(ship);float reach=profile.Length*.5f+profile.Margin+4;
        if((WaterChart.AtSea(point)-WaterChart.AtSea(ship.transform.position)).sqrMagnitude>reach*reach)
        {message="Slowing for a rock beyond clearing range";return RockClearResult.Waiting;}
        message="Slowing to clear the rock ahead";
        if(speed>.75f||!ready)return RockClearResult.Waiting;
        if(!PrivateArea.CheckAccess(point,0,false,true)||!PrivateArea.CheckAccess(rock!.transform.position,0,false,true))
        {message="Rock clearing blocked by a ward.";return RockClearResult.Blocked;}
        var view=rock!.GetComponent<ZNetView>();
        if(!view||!view.IsValid()||!view.IsOwner())
        {message="Rock clearing needs local rock authority. Take the helm or replot around it.";return RockClearResult.Blocked;}
        if(clearingDrops!=null)return RockClearResult.Waiting;
        var spawned=new HashSet<ItemDrop>();clearingDrops=spawned;
        bool success=false;
        try
        {
            var hit=new HitData {m_hitCollider=obstacle,m_point=point,m_dir=ship.transform.forward,m_toolTier=short.MaxValue};
            hit.m_damage.m_pickaxe=1000000;hit.SetAttacker(player);
            // Invoke the owner's native handler synchronously so only drops from
            // this one authorized hit (and native support collapse) are captured.
            if(rock is MineRock old){int index=OldIndex(old,obstacle);if(index<0)return RockClearResult.NotApplicable;OldHit(old,ZNet.GetUID(),hit,index);}
            else if(rock is MineRock5 current){int index=NewIndex(current,obstacle);if(index<0)return RockClearResult.NotApplicable;NewHit(current,ZNet.GetUID(),hit,index);}
            else WholeHit((Destructible)rock,ZNet.GetUID(),hit);
            success=true;
        }
        catch(Exception error){Plugin.Instance.Error(error);message="Rock clearing stopped after an error. Dropped resources remain available.";}
        finally {clearingDrops=null;}
        int collected=0;
        foreach(var drop in spawned)if(drop&&RockCargo.Store(ship,player,drop))collected++;
        if(!success)return RockClearResult.Blocked;
        message=collected>0?"Clearing rock · stone collected into ship cargo":"Clearing rock ahead";
        return RockClearResult.Cleared;
    }
}

[HarmonyPatch(typeof(ItemDrop),nameof(ItemDrop.OnCreateNew),new[]{typeof(ItemDrop),typeof(bool)})]
internal static class RockClearingDrops
{
    private static void Postfix(ItemDrop item)=>RockClearing.Capture(item);
}
