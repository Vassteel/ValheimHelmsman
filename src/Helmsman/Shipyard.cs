using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

public sealed class Shipyard : MonoBehaviour,Interactable,Hoverable
{
    private static readonly int OrderKey="helmsman_build_order_v1".GetStableHashCode();
    private const string LaunchKey="helmsman_build_launch_v1";
    private static readonly Dictionary<string,ConfigEntry<float>> durations=new();
    private ZNetView view=null!;
    private WearNTear wear=null!;
    private VikingBirds.PerchedBird? bird;
    private PuffinWorker? worker;
    private readonly ShipBuildGhost buildGhost=new();
    private bool ghostFailed;
    private float nextTick;
    private string status="";
    internal string Identity=>Ready?view.GetZDO().m_uid.ToString():"";
    internal string NpcStatus=>worker?.Status??Status;
    internal bool Ready=>view&&view.IsValid();
    internal static void Configure(ConfigFile config)
    {
        foreach(var b in ShipConstruction.Blueprints)
            durations[b.Id]=config.Bind("Construction",b.Source+"BuildSeconds",(float)b.BuildSeconds,
                new ConfigDescription("Construction time in seconds for "+b.Name+". Existing orders keep their paid duration.",new AcceptableValueRange<float>(30,86400), new ConfigurationManagerAttributes { IsAdminOnly=true }));
    }
    internal ConstructionOrder? Order
    {
        get
        {
            if(!Ready)return null;
            string json=view.GetZDO().GetString(OrderKey);
            if(json.Length==0 || json.Length>4096)return null;
            try{var order=JsonUtility.FromJson<ConstructionOrder>(json);return order!=null&&order.Valid?order:null;}
            catch(ArgumentException){return null;}
        }
    }
    internal bool Busy=>Ready&&view.GetZDO().GetString(OrderKey).Length>0;
    internal double Elapsed(ConstructionOrder order)=>ZNet.instance?ShipConstruction.Elapsed(order.started,ZNet.instance.GetTime().Ticks,order.duration):0;
    internal string Status
    {
        get
        {
            var o=Order;if(o==null)return Busy?"Saved order needs inspection; its materials are retained.":"Build ships with the hammer; a paint stand unlocks decoration.";
            double left=Math.Max(0,o.duration-Elapsed(o));
            return left>0?o.name+" — "+FormatDuration(left)+" remaining":status.Length>0?status:"Construction finished; checking launch area.";
        }
    }
    private float nextConstructionStatus;
    private string constructionStatus="";
    internal string ConstructionStatus
    {
        get
        {
            if(Time.time<nextConstructionStatus)return constructionStatus;
            nextConstructionStatus=Time.time+1;
            var lines=new List<string>();
            var structure=GetComponent<Structures.StructureConstruction>();
            if(structure&&structure.Busy){lines.Add(structure.Status);var need=structure.RemainingMaterials();if(need.Length>0)lines.Add("Total outstanding: "+need);}
            foreach(var slip in UnityEngine.Object.FindObjectsByType<Slipway>(FindObjectsSortMode.None))
            {var report=slip.StatusFor(Identity);if(report.Length>0)lines.Add(report);}
            constructionStatus=lines.Count>0?string.Join("\n\n",lines):"No active construction. Select a ship with the hammer or import a structure.";
            return constructionStatus;
        }
    }
    internal static float Duration(ShipBlueprint blueprint)=>durations[blueprint.Id].Value;
    internal static string FormatDuration(double seconds)
    {var time=TimeSpan.FromSeconds(Math.Ceiling(seconds));return seconds>=3600?((int)time.TotalHours)+time.ToString(@"\:mm\:ss"):time.ToString(@"mm\:ss");}
    private void Awake(){view=GetComponent<ZNetView>();wear=GetComponent<WearNTear>();}
    private IEnumerator Start()
    {
        while(!Ready)yield return null;
        if(wear)wear.m_onDestroyed+=ReturnMaterials;
        while(!Player.m_localPlayer)yield return new WaitForSeconds(1);
        // Cosmetic only; dedicated servers never create a bird or material instances.
        var material=GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.shader&&m.shader.name=="Custom/Piece");
        if(!material)
        {
            var bench=ZNetScene.instance?ZNetScene.instance.GetPrefab("piece_workbench"):null;
            material=bench?bench.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.shader&&m.shader.name=="Custom/Piece"):null;
        }
        if(!material)yield break;
        bird=new VikingBirds.PerchedBird(transform,material,false);
        bird.Root.transform.localScale=Vector3.one*.65f;
        // Probe only this table's solid collider so the feet follow its actual top.
        var point=transform.TransformPoint(new Vector3(.45f,5,0));
        var ray=new Ray(point,-transform.up);float closest=float.PositiveInfinity;
        var perch=new Vector3(.45f,1.055f,0);
        foreach(var collider in GetComponentsInChildren<Collider>(true))
            if(!collider.isTrigger && collider.Raycast(ray,out var hit,10) && hit.distance<closest)
            {closest=hit.distance;perch=transform.InverseTransformPoint(hit.point);}
        bird.Root.transform.localPosition=perch;
        worker=new PuffinWorker(this,bird);
        bird.Probes(transform);
        bird.Root.AddComponent<PuffinHover>().Owner=this;
    }
    private void Update()
    {
        if(!Ready){buildGhost.Dispose();return;}
        var order=Order;
        if(order==null)ghostFailed=false;
        if(Player.m_localPlayer && !ghostFailed)
            try{buildGhost.Update(order,order==null?0:(float)(Elapsed(order)/order.duration));}
            catch(Exception error){ghostFailed=true;Plugin.Instance.Error(error);}

        worker?.Tick(Time.deltaTime);
        if(order==null || !view.IsOwner() || Time.time<nextTick || Elapsed(order)<order.duration)return;
        nextTick=Time.time+2;
        try{Launch(order);}catch(Exception error){status="Launch paused; the saved order is retained.";Plugin.Instance.Error(error);}
    }
    internal bool Near(Player player)=>Ready&&player&&!player.IsDead()&&Vector3.Distance(player.transform.position,transform.position)<6&&PrivateArea.CheckAccess(transform.position,0,false,true);
    internal List<DockRecord> Docks=>Plugin.Instance.Directory.Records.Where(d=>Vector3.Distance(d.MarkerPosition,transform.position)<45).ToList();
    // Existing paid orders still finish and refund normally; no new timed orders.
    private void Launch(ConstructionOrder order)
    {
        var zdo=view.GetZDO();var id=zdo.GetZDOID(LaunchKey);
        // A previously reserved native ship ZDO is the durable launch receipt. A reload
        // instantiates that same ship rather than creating another vessel.
        if(id!=ZDOID.None)
        {
            var existing=ZDOMan.instance.GetZDO(id);
            if(existing!=null){FinishLaunch(existing,order);zdo.Set(OrderKey,"");status="Ship launched.";}
            else status="Launch record is unavailable. Order retained for inspection.";
            return;
        }
        var blueprint=ShipConstruction.Find(order.blueprint)!;
        var prefab=ZNetScene.instance.GetPrefab(blueprint.Prefab);if(!prefab){status="Waiting for the ship model.";return;}
        if(!ShipLaunchClearance.Clear(prefab.GetComponent<Ship>(),order.position,Quaternion.Euler(0,order.heading,0),out status))return;
        // No Unity object is spawned until construction is finished and water is clear.
        // Native ZNetScene creates this persistent ZDO through its normal world-loading path.
        var ship=ZDOMan.instance.CreateNewZDO(WaterChart.AtSea(order.position),blueprint.Prefab.GetStableHashCode());
        zdo.Set(LaunchKey,ship.m_uid);
        FinishLaunch(ship,order);
        zdo.Set(OrderKey,"");status="Ship launched.";
    }
    private static void FinishLaunch(ZDO ship,ConstructionOrder order)
    {
        var blueprint=ShipConstruction.Find(order.blueprint)!;
        ship.Persistent=true;ship.SetPrefab(blueprint.Prefab.GetStableHashCode());ship.SetRotation(Quaternion.Euler(0,order.heading,0));
        ship.Set(ShipDirectory.NameKey,order.name);ship.Set("creator",order.creator);
    }
    private void ReturnMaterials()
    {
        if(!Ready||!view.IsOwner()||view.GetZDO().GetZDOID(LaunchKey)!=ZDOID.None)return;
        var order=Order;if(order==null)return;
        foreach(var cost in CommissionCosts.Refund(order.recipe,order.freeBuild))
        {
            var prefab=ObjectDB.instance.GetItemPrefab(cost.Key);if(!prefab)continue;
            int left=cost.Value;
            while(left>0){var item=prefab.GetComponent<ItemDrop>().m_itemData.Clone();item.m_dropPrefab=prefab;item.m_stack=Math.Min(left,Math.Max(1,item.m_shared.m_maxStackSize));ItemDrop.DropItem(item,item.m_stack,transform.position+Vector3.up,Quaternion.identity);left-=item.m_stack;}
        }
        view.GetZDO().Set(OrderKey,"");
    }
    public string GetHoverName()=>"Puffin shipwright";
    public float GetHoverOffset()=>0;
    public string GetHoverText()=>Localization.instance.Localize("Puffin shipwright\n[<color=yellow><b>$KEY_Use</b></color>] Workshop\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] Supplies\n")+ConstructionStatus;
    public bool Interact(Humanoid user,bool hold,bool alt)
    {if(hold||!(user is Player p)||p!=Player.m_localPlayer||!Near(p))return false;if(alt)return GetComponent<Container>().Interact(user,false,false);Plugin.Instance.UI.OpenShipyard(this);return true;}
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
    private void OnDestroy(){if(wear)wear.m_onDestroyed-=ReturnMaterials;worker?.Dispose();bird?.Dispose();buildGhost.Dispose();}
}
