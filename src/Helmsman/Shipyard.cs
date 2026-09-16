using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

[Serializable]
internal sealed class ConstructionOrder
{
    public string blueprint="", recipe="", name="";
    public long started,creator;
    public float duration;
    public Vector3 position;
    public float heading;
    internal bool Valid=>ShipConstruction.Find(blueprint)!=null && ShipConstruction.ValidDuration(duration) && started>=0 &&
        Finite(position.x) && Finite(position.y) && Finite(position.z) && Finite(heading) && name!=null && name.Length<=48 && recipe==ShipConstruction.Find(blueprint)!.Recipe;
    private static bool Finite(float n)=>!float.IsInfinity(n)&&!float.IsNaN(n);
}

public sealed class Shipyard : MonoBehaviour,Interactable,Hoverable
{
    private static readonly int OrderKey="helmsman_build_order_v1".GetStableHashCode();
    private const string LaunchKey="helmsman_build_launch_v1";
    private static readonly Dictionary<string,ConfigEntry<float>> durations=new();
    private ZNetView view=null!;
    private WearNTear wear=null!;
    private VikingBirds.PerchedBird? bird;
    private float nextTick;
    private string status="";
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
            var o=Order;if(o==null)return Busy?"Saved order needs inspection; its materials are retained.":"Ready for a commission.";
            double left=Math.Max(0,o.duration-Elapsed(o));
            return left>0?o.name+" — "+FormatDuration(left)+" remaining":status.Length>0?status:"Construction finished; checking launch area.";
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
        bird.Probes(transform);
        var trigger=bird.Root.AddComponent<SphereCollider>();trigger.center=new Vector3(0,.6f,0);trigger.radius=.35f;
        var interaction=bird.Root.AddComponent<BirdInteraction>();interaction.Label=GetHoverName;interaction.Hint=GetHoverText;
        interaction.Use=user=>Interact(user,false,false);
    }
    private void Update()
    {
        if(!Ready)return;
        var order=Order;
        if(bird!=null)
        {
            var task=order==null?ShipwrightTask.Idle:ShipConstruction.Task(ShipConstruction.Find(order.blueprint)!.HasSail,Elapsed(order),order.duration);
            int tool=task==ShipwrightTask.Hammer?1:task==ShipwrightTask.Chisel?2:task==ShipwrightTask.Stitch?3:0;
            bird.SetTool(tool);
            float cycle=tool==1?Mathf.Sin(Time.time*8):tool==2?Mathf.Sin(Time.time*4):Mathf.Sin(Time.time*5);
            float stroke=Mathf.Max(0,cycle);
            bird.Pose(tool==1?20+10*stroke:tool==2?35+8*cycle:tool==3?30+8*cycle:0,
                tool==3?cycle*16:0,0,tool==1?10+55*stroke:tool==2?35+12*stroke:tool==3?15:0,
                tool==1?.09f*stroke:tool>0?.025f*(cycle+1):0,tool>0?15:0);
            bool nearby=Player.m_localPlayer&&(Player.m_localPlayer.transform.position-bird.Root.transform.position).sqrMagnitude<9;
            bird.Rest(order==null&&EnvMan.instance&&!EnvMan.IsDaylight()&&!nearby,Time.time,Time.deltaTime);
        }
        if(order==null || !view.IsOwner() || Time.time<nextTick || Elapsed(order)<order.duration)return;
        nextTick=Time.time+2;
        try{Launch(order);}catch(Exception error){status="Launch paused; the saved order is retained.";Plugin.Instance.Error(error);}
    }
    internal bool Near(Player player)=>Ready&&player&&!player.IsDead()&&Vector3.Distance(player.transform.position,transform.position)<6&&PrivateArea.CheckAccess(transform.position,0,false,true);
    internal List<DockRecord> Docks=>Plugin.Instance.Directory.Records.Where(d=>Vector3.Distance(d.MarkerPosition,transform.position)<45).ToList();
    internal string Commission(ShipBlueprint blueprint,DockRecord dock)
        =>GetComponent<WorkstationLease>().Run(()=>CommissionOwned(blueprint,dock));
    private string CommissionOwned(ShipBlueprint blueprint,DockRecord dock)
    {
        var player=Player.m_localPlayer;
        if(!Near(player))return "Stand beside an accessible Carpenter's Table.";
        if(Busy)return "Let me finish this ship first.";
        if(!GetComponent<WorkstationLease>().Held)return "Workshop access changed. Please try again.";
        var current=DockDirectory.Resolve(dock.Id);
        if(current==null || Vector3.Distance(current.MarkerPosition,transform.position)>=45)return "Choose a configured Dock Ward within 45 m.";
        var prefab=ZNetScene.instance.GetPrefab(blueprint.Prefab);
        if(!prefab || !ShipProfile.Supports(prefab.GetComponent<Ship>()))return "That ship model is not ready.";
        var berth=current.Berth;
        if(!new WaterChart(null,ShipProfile.For(prefab.GetComponent<Ship>())).HullSegment(berth.position,berth.position,Quaternion.Euler(0,berth.heading,0),true,out var reason))return reason;
        var order=new ConstructionOrder {blueprint=blueprint.Id,recipe=blueprint.Recipe,name=blueprint.Name,creator=player.GetPlayerID(),
            started=ZNet.instance.GetTime().Ticks,duration=durations[blueprint.Id].Value,position=berth.position,heading=berth.heading};
        return Shipwright.Pay(player,new ShipUpgrade(blueprint.Id,blueprint.Name,blueprint.Recipe,"$piece_workbench",1),transform.position,()=>
        {
            if(!view.IsOwner() || Busy)throw new InvalidOperationException("The workshop changed before payment completed.");
            view.GetZDO().Set(LaunchKey,ZDOID.None);
            view.GetZDO().Set(OrderKey,JsonUtility.ToJson(order));
        }) is string error && error.Length>0 ? error : "Materials paid. I'll build "+blueprint.Name+" here; come back when she's ready.";
    }
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
        var chart=new WaterChart(null,ShipProfile.For(prefab.GetComponent<Ship>()));
        if(!chart.HullSegment(order.position,order.position,Quaternion.Euler(0,order.heading,0),true,out status))return;
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
        foreach(var cost in ShipwrightRules.Costs(order.recipe))
        {
            var prefab=ObjectDB.instance.GetItemPrefab(cost.Key);if(!prefab)continue;
            int left=cost.Value;
            while(left>0){var item=prefab.GetComponent<ItemDrop>().m_itemData.Clone();item.m_dropPrefab=prefab;item.m_stack=Math.Min(left,Math.Max(1,item.m_shared.m_maxStackSize));ItemDrop.DropItem(item,item.m_stack,transform.position+Vector3.up,Quaternion.identity);left-=item.m_stack;}
        }
        view.GetZDO().Set(OrderKey,"");
    }
    public string GetHoverName()=>"Puffin shipwright";
    public float GetHoverOffset()=>0;
    public string GetHoverText()=>Localization.instance.Localize("Puffin shipwright\n[<color=yellow><b>$KEY_Use</b></color>] Ships and refits\n")+Status;
    public bool Interact(Humanoid user,bool hold,bool alt)
    {if(hold||!(user is Player p)||p!=Player.m_localPlayer||!Near(p))return false;Plugin.Instance.UI.OpenShipyard(this);return true;}
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
    private void OnDestroy(){if(wear)wear.m_onDestroyed-=ReturnMaterials;bird?.Dispose();}
}
