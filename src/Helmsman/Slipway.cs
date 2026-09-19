using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;
public sealed partial class Slipway:MonoBehaviour,Hoverable,Interactable
{
    private static readonly int OrderKey="helmsman_slip_order_v1".GetStableHashCode();
    private const string ResetKey="helmsman_slip_reset_started";
    private const string Receipt="helmsman_slip_launch_v1";
    private const string BrokenBackup="helmsman_slip_broken_backup_v1",NoticeBench="helmsman_slip_notice_bench",NoticeText="helmsman_slip_notice";
    private string badOrder="",cachedRaw="";
    private SlipwayOrder? cachedOrder;
    private ZNetView view=null!;private WearNTear wear=null!;
    private SlipwayPresentation? presentation;
    private PuffinConstructionCrew? crew;
    private readonly ShipBuildGhost ghost=new();private float nextTick;private string status="";private float nextSupport;private bool supported=true;
    internal bool Ready=>view&&view.IsValid();
    internal bool Resetting=>Ready&&view.GetZDO().GetLong(ResetKey)>0&&Now-view.GetZDO().GetLong(ResetKey)<SlipwayMotion.ResetSeconds*TimeSpan.TicksPerSecond;
    internal static long Now=>ZNet.instance?ZNet.instance.GetTime().Ticks:0;
    internal bool Busy=>Ready&&(view.GetZDO().GetString(OrderKey).Length>0||Resetting);
    internal SlipwayOrder? Order
    {
        get
        {
            if(!Ready)return null;string raw=view.GetZDO().GetString(OrderKey);if(raw.Length==0)return null;
            if(raw==cachedRaw)return cachedOrder;
            cachedRaw=raw;cachedOrder=null;
            try{return cachedOrder=SlipwayOrderCodec.Read(raw);}
            catch(Exception e)
            {
                if(badOrder!=raw){badOrder=raw;Plugin.Instance.Record("Slipway "+view.GetZDO().m_uid+" cannot read construction: "+e.Message);}
                return null;
            }
        }
    }
    internal string StatusFor(string bench)
    {
        var order=Order;if(order!=null)return order.bench==bench?GetHoverText():"";
        if(!Ready)return "";
        var data=view.GetZDO();
        if(data.GetString(NoticeBench)==bench)return data.GetString(NoticeText);
        var raw=data.GetString(OrderKey);
        try{if(raw.Length>0&&SlipwayOrderCodec.Bench(raw)==bench)return "Slipway: saved construction needs inspection; materials retained.";}catch{}
        return "";
    }
    private void RecoverEmptyOrder()
    {
        var data=view.GetZDO();var raw=data.GetString(OrderKey);
        if(raw.Length==0||data.GetZDOID(Receipt)!=ZDOID.None)return;
        if(!SlipwayOrderCodec.EmptyBrokenBlueprint(raw,out var bench))return;
        // Retain the exact old payload. Only unfunded, unstarted orders with the
        // missing nested ship payload are recoverable without knowing a recipe.
        data.Set(BrokenBackup,raw);data.Set(NoticeBench,bench);
        data.Set(NoticeText,"An empty slipway blueprint was recovered. Place the ship again; no materials were taken.");
        data.Set(OrderKey,"");
        Plugin.Instance.Record("Recovered empty slipway blueprint on "+data.m_uid+"; original payload retained in ZDO.");
    }
    private void Awake(){view=GetComponent<ZNetView>();wear=GetComponent<WearNTear>();}
    private IEnumerator Start()
    {
        while(!Ready)yield return null;
        GetComponent<WorkstationLease>().AccessRange=CanOrder;
        if(wear)wear.m_onDestroyed+=Refund;
    }
    internal bool CanOrder(Player player)=>Ready&&player&&!player.IsDead()&&WorkshopRange.Bench(player,transform.position)&&PrivateArea.CheckAccess(transform.position,0,false,true);
    internal static Slipway? Find(Player player)=>UnityEngine.Object.FindObjectsByType<Slipway>(FindObjectsSortMode.None)
        .Where(s=>s&&s.Ready&&!s.Busy&&s.CanOrder(player)).OrderBy(s=>(s.transform.position-player.transform.position).sqrMagnitude).ThenBy(s=>s.view.GetZDO().m_uid.ToString(),StringComparer.Ordinal).FirstOrDefault();
    internal Quaternion Heading=>transform.rotation*Quaternion.Euler(0,180,0);
    internal Quaternion StageRotation=>Heading*Quaternion.Euler(5.71f,0,0);
    internal Vector3 Stage(Ship ship)
    {
        float bottom=0;
        foreach(var f in ship.GetComponentsInChildren<MeshFilter>(true))
        {
            var r=f.GetComponent<Renderer>();if(!r||!r.enabled||r.forceRenderingOff||!f.sharedMesh||r.sharedMaterials.Any(m=>m&&m.shader&&m.shader.name.Contains("WaterMask")))continue;
            if(ship.GetComponent<FinalShipPresentation>()&&!f.name.StartsWith("hull ",StringComparison.Ordinal))continue;
            var b=f.sharedMesh.bounds;
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                bottom=Mathf.Min(bottom,ship.transform.InverseTransformPoint(f.transform.TransformPoint(b.center+new Vector3(x*b.extents.x,-b.extents.y,z*b.extents.z))).y);
        }
        return transform.TransformPoint(new Vector3(0,2.13f-bottom,2));
    }
    private Vector3 LaunchPoint(Ship ship)
    {
        var point=WaterChart.AtSea(transform.TransformPoint(new Vector3(0,0,-12-ship.m_floatCollider.size.z*.5f)));
        var body=ship.GetComponent<Rigidbody>();
        point.y+=SlipwayMotion.FloatOffset(ship.m_waterLevelOffset,body?body.centerOfMass.y:0,ship.m_forceDistance,ship.m_force,Physics.gravity.y);
        return point;
    }
    internal string Request(Player player,Piece piece,ShipBlueprint plan,bool free)
        =>GetComponent<WorkstationLease>().Run(()=>Begin(player,piece,plan,free));
    private string Begin(Player player,Piece piece,ShipBlueprint plan,bool free)
    {
        if(!Ready||!view.IsOwner()||!CanOrder(player))return "The shipwright bench and slipway must share your workshop area.";
        if(Busy)return "This slipway already has a ship under construction.";
        if(!Structures.NativeBuildingSupport.Stable(gameObject))return "Support the slipway before ordering construction.";
        var bench=WorkshopRange.Bench(player,transform.position);if(!bench)return "No shipwright bench supports this slipway.";
        var prefab=ZNetScene.instance.GetPrefab(plan.Prefab);var ship=prefab?prefab.GetComponent<Ship>():null;if(!ship)return "Ship model is not ready.";
        if(Location.IsInsideNoBuildLocation(Stage(ship))||Location.IsInsideNoBuildLocation(LaunchPoint(ship)))return "Construction or launch overlaps a no-build area.";
        if(!PrivateArea.CheckAccess(LaunchPoint(ship),0,false,true))return "The launch water is in a protected area.";
        if(!ShipLaunchClearance.Clear(ship,LaunchPoint(ship),Heading,out var reason,preserveHeight:true))return reason;
        var order=new SlipwayOrder{awaitingMaterials=!free,crewCalledAt=free?Now:0,bench=bench.Identity,order=new ConstructionOrder{blueprint=plan.Id,recipe=plan.Recipe,name=plan.Name,creator=player.GetPlayerID(),started=ZNet.instance.GetTime().Ticks,duration=Mathf.Max(30,Shipyard.Duration(plan)*(WorkshopRange.Upgrade(bench,player,"tools",transform.position)?.75f:1)),freeBuild=free,position=LaunchPoint(ship),heading=Heading.eulerAngles.y},stage=Stage(ship),hullBonus=WorkshopRange.Upgrade(bench,player,"caulking",transform.position)?1.15f:1,sailBonus=WorkshopRange.Upgrade(bench,player,"rigging",transform.position)?1.10f:1};
        string serialized=SlipwayOrderCodec.Write(order);
        // Persist the empty blueprint before accepting any actual material contribution.
        view.GetZDO().Set(Receipt,ZDOID.None);view.GetZDO().Set(OrderKey,serialized);
        view.GetZDO().Set(NoticeText,"");view.GetZDO().Set(NoticeBench,"");
        Plugin.Instance.Record("Slipway order saved: "+plan.Id+" on "+view.GetZDO().m_uid+" for bench "+bench.Identity+"; waiting for materials="+order.awaitingMaterials);
        if(!free)return "Blueprint placed for "+plan.Name+". Supply materials through Quartermaster, the shipwright bench, or your inventory.";
        return "Calling the puffin crew for "+plan.Name+" · "+Shipyard.FormatDuration(order.order.duration)+" construction after arrival";
    }
    private void Update()
    {
        var state=Order;
        if(Player.m_localPlayer&&Ready)
        {
            presentation??=new SlipwayPresentation(transform);
            presentation.Tick(state,view.GetZDO().GetLong(ResetKey),Now);
            crew??=new PuffinConstructionCrew(this);crew.Tick(state?.awaitingMaterials==true?null:state,Time.deltaTime);
        }
        if(state==null)
        {
            ghost.Dispose();
            if(Ready&&view.IsOwner())RecoverEmptyOrder();
            return;
        }
        if(Ready&&view.IsOwner()&&Time.time>=nextSupport)
        {
            nextSupport=Time.time+1;
            var bench=UnityEngine.Object.FindObjectsByType<Shipyard>(FindObjectsSortMode.None).FirstOrDefault(b=>b&&b.Identity==state.bench);
            supported=Structures.NativeBuildingSupport.Stable(gameObject)&&bench&&Structures.NativeBuildingSupport.Stable(bench.gameObject);
            if(!supported&&state.launchStarted>0&&state.launchPaused==0){state.launchPaused=Now;view.GetZDO().Set(OrderKey,SlipwayOrderCodec.Write(state));}
            if(!supported&&state.constructionStarted>0&&state.supportPaused==0){state.supportPaused=Now;view.GetZDO().Set(OrderKey,SlipwayOrderCodec.Write(state));}
            if(supported&&state.supportPaused>0){state.supportPauseTicks+=Math.Max(0,Now-state.supportPaused);state.supportPaused=0;view.GetZDO().Set(OrderKey,SlipwayOrderCodec.Write(state));}
        }
        if(supported&&state.AwaitingCrew&&Ready&&view.IsOwner())
        {
            bool observed=Player.m_localPlayer&&(Player.m_localPlayer.transform.position-transform.position).sqrMagnitude<=4900;
            if(PuffinCrewPlan.CanBeginConstruction(state.crewCalledAt,state.constructionStarted,Now,observed,crew?.Assembled??false))
            {state.constructionStarted=Now;view.GetZDO().Set(OrderKey,SlipwayOrderCodec.Write(state));}
        }
        float elapsed=(float)state.Elapsed(Now);
        double seconds=SlipwayMotion.LaunchElapsed(Now,state.launchStarted,state.launchPaused,state.launchPauseTicks);
        float launch=SlipwayMotion.Travel(seconds);
        if(Player.m_localPlayer)ghost.Update(state.order,crew!=null&&crew.InRange&&!crew.Assembled&&elapsed<state.order.duration&&state.launchStarted==0?0:elapsed/state.order.duration,Vector3.Lerp(state.stage,state.order.position,launch),Quaternion.Slerp(StageRotation,Quaternion.Euler(0,state.order.heading,0),SlipwayMotion.Level(seconds)));
        if(!supported)return;
        if(state.awaitingMaterials)
        {
            if(Ready&&view.IsOwner()&&Time.time>=nextTick)
            {
                nextTick=Time.time+1;
                try{Supply(state);}catch(Exception e){status="Supply paused; saved materials retained.";Plugin.Instance.Error(e);nextTick=Time.time+10;}
            }
            return;
        }
        if(!Ready||!view.IsOwner()||Time.time<nextTick||state.AwaitingCrew||elapsed<state.order.duration)return;
        nextTick=Time.time+.25f;
        try{Advance(state,launch);}catch(Exception e){status="Launch paused; saved construction retained.";Plugin.Instance.Error(e);}
    }
    private void Supply(SlipwayOrder state)
    {
        var player=Player.m_localPlayer;if(!player||!CanOrder(player))return;
        var bench=UnityEngine.Object.FindObjectsByType<Shipyard>(FindObjectsSortMode.None).FirstOrDefault(b=>b&&b.Identity==state.bench);
        if(!bench||!WorkshopRange.Connected(player,bench.transform.position,transform.position,player.transform.position))return;
        WorkshopMaterialSupply.Collect(player,bench,state,()=>view.GetZDO().Set(OrderKey,SlipwayOrderCodec.Write(state)));
        if(!ConstructionFunding.Complete(state.order.recipe,state.supplied))return;
        state.awaitingMaterials=false;state.crewCalledAt=Now;state.order.started=Now;state.constructionStarted=0;
        view.GetZDO().Set(OrderKey,SlipwayOrderCodec.Write(state));status="Materials supplied; crew arriving.";
    }
    private void CompleteReset(ZDO data){data.Set(ResetKey,Now);data.Set(OrderKey,"");}
    private void Advance(SlipwayOrder state,float progress)
    {
        var data=view.GetZDO();var receipt=data.GetZDOID(Receipt);
        if(receipt!=ZDOID.None)
        {
            var existing=ZDOMan.instance.GetZDO(receipt);if(existing==null){status="Launch receipt unavailable; order retained.";return;}
            Finish(existing,state);CompleteReset(data);return;
        }
        var plan=ShipConstruction.Find(state.order.blueprint)!;var prefab=ZNetScene.instance.GetPrefab(plan.Prefab);if(!prefab){status="Waiting for ship model.";return;}
        var ship=prefab.GetComponent<Ship>();
        if(!PathClear(ship,state,progress,out status))
        {
            if(state.launchStarted>0&&state.launchPaused==0){state.launchPaused=Now;data.Set(OrderKey,SlipwayOrderCodec.Write(state));}
            return;
        }
        if(state.launchPaused>0){state.launchPauseTicks+=Math.Max(0,Now-state.launchPaused);state.launchPaused=0;data.Set(OrderKey,SlipwayOrderCodec.Write(state));status="Launching…";return;}
        if(state.launchStarted==0){state.launchStarted=ZNet.instance.GetTime().Ticks;data.Set(OrderKey,SlipwayOrderCodec.Write(state));status="Launching…";return;}
        if(progress<1)return;
        var launched=ZDOMan.instance.CreateNewZDO(state.order.position,plan.Prefab.GetStableHashCode());data.Set(Receipt,launched.m_uid);Finish(launched,state);CompleteReset(data);status="Ship launched.";
    }
    private bool PathClear(Ship ship,SlipwayOrder state,float progress,out string reason)
    {
        if(!ShipLaunchClearance.Clear(ship,state.order.position,Quaternion.Euler(0,state.order.heading,0),out reason,preserveHeight:true))return false;
        int steps=Mathf.CeilToInt(Vector3.Distance(state.stage,state.order.position));
        for(int i=0;i<steps;i++)
        {
            float t=progress+(1-progress)*(float)i/steps;
            if(!ShipLaunchClearance.Clear(ship,Vector3.Lerp(state.stage,state.order.position,t),Quaternion.Slerp(StageRotation,Quaternion.Euler(0,state.order.heading,0),SlipwayMotion.Ease((t-.48)/.52)),out reason,transform,false))return false;
        }
        reason="Launch space clear.";return true;
    }
    private static void Finish(ZDO ship,SlipwayOrder state)
    {
        var plan=ShipConstruction.Find(state.order.blueprint)!;ship.Persistent=true;ship.SetPrefab(plan.Prefab.GetStableHashCode());ship.SetRotation(Quaternion.Euler(0,state.order.heading,0));ship.Set(ShipDirectory.NameKey,state.order.name);ship.Set("creator",state.order.creator);
        var prefab=ZNetScene.instance.GetPrefab(plan.Prefab);
        WorkshopShipBonus.Stamp(ship,state.hullBonus,state.sailBonus,prefab.GetComponent<WearNTear>().m_health);
    }
    public string GetHoverName()=>"Shipwright slipway";
    public string GetHoverText()
    {
        var o=Order;if(o==null)return GetHoverName()+"\n"+(Resetting?"Hauling the cradle back into position…":Busy?"Saved order needs inspection.":"Choose a large ship in Hammer → Helmsman.");
        if(!supported||o.supportPaused>0)return GetHoverName()+"\nConstruction paused: reinforce the slipway and workbench supports.";
        if(o.awaitingMaterials)
        {
            var remaining=ShipwrightRules.Costs(o.order.recipe).Select(c=>(c.Key,count:ConstructionFunding.Remaining(o.order.recipe,o.supplied,c.Key))).Where(c=>c.count>0).Select(c=>c.Key+" × "+c.count);
            return GetHoverName()+"\n"+o.order.name+" · Waiting for materials\n"+string.Join(", ",remaining)+"\nUse Quartermaster supplies or the shipwright bench inventory.";
        }
        if(o.AwaitingCrew)return GetHoverName()+"\n"+o.order.name+" · Crew arriving"+(crew!=null?" ("+crew.SettledCount+"/"+PuffinCrewPlan.Count+")":"")+"\nConstruction starts once the crew is in position. Keep the side platforms clear.";
        double elapsed=o.Elapsed(Now);
        return GetHoverName()+"\n"+o.order.name+" · "+(elapsed<o.order.duration?Shipyard.FormatDuration(o.order.duration-elapsed)+" remaining":status.Length>0?status:"Waiting for launch");
    }
    public float GetHoverOffset()=>0;
    public bool Interact(Humanoid user,bool hold,bool alt){if(hold)return false;Plugin.Message(GetHoverText());return true;}
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
    private void OnDestroy(){if(wear)wear.m_onDestroyed-=Refund;ghost.Dispose();presentation?.Dispose();crew?.Dispose();}
}
