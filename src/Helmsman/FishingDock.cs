using System;
using System.Collections;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace Helmsman;

public sealed class FishingDock : MonoBehaviour
{
    public Container Chest=null!;
    public Transform WorkerAnchor=null!;
    public string Catch="Fish1";
    private static ConfigEntry<float> seconds=null!;
    private static ConfigEntry<int> limit=null!;
    private static readonly int Progress="helmsman_fishing_progress_v1".GetStableHashCode();
    private ZNetView view=null!;
    private VikingBirds.PerchedBird? bird;
    private double previous;
    private float next;
    private Vector3 perch;
    private float idleOffset;
    internal static void Configure(ConfigFile config)
    {
        seconds=config.Bind("Fishing Dock","SecondsPerCatch",20f,new ConfigDescription("Daylight work seconds per fish.",new AcceptableValueRange<float>(5,3600), new ConfigurationManagerAttributes { IsAdminOnly=true }));
        limit=config.Bind("Fishing Dock","CatchLimit",50,new ConfigDescription("Pause when the dock chest contains this many fish.",new AcceptableValueRange<int>(1,10000), new ConfigurationManagerAttributes { IsAdminOnly=true }));
    }
    private void Awake(){view=GetComponent<ZNetView>();}
    private IEnumerator Start()
    {
        while(!view||!view.IsValid()||!Player.m_localPlayer)yield return new WaitForSeconds(1);
        var material=GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m&&m.shader&&m.shader.name=="Custom/Piece");
        if(!material)yield break;
        bird=new VikingBirds.PerchedBird(null!,material,false,true);bird.Root.transform.localScale=Vector3.one*.8f;bird.Probes(transform);
        idleOffset=UnityEngine.Random.Range(0f,23f);
        perch=WorkerAnchor?transform.InverseTransformPoint(WorkerAnchor.position):Vector3.zero;
        var ray=new Ray(transform.TransformPoint(perch)+transform.up*4,-transform.up);float closest=float.PositiveInfinity;
        foreach(var collider in GetComponentsInChildren<Collider>(true))
            if(collider.enabled&&!collider.isTrigger&&collider.gameObject.activeInHierarchy&&collider.Raycast(ray,out var hit,6)&&hit.distance<closest)
            {closest=hit.distance;perch=transform.InverseTransformPoint(hit.point);}
        var trigger=bird.Root.AddComponent<SphereCollider>();trigger.center=new Vector3(0,.7f,0);trigger.radius=.35f;
        var interaction=bird.Root.AddComponent<BirdInteraction>();interaction.Label=()=>"Pelican fisherman";
        interaction.Hint=()=>Localization.instance.Localize("Pelican fisherman\n[<color=yellow><b>$KEY_Use</b></color>] Catch report");
        interaction.Use=user=>{Plugin.Message(!EnvMan.IsDaylight()?"Fishing resumes at dawn.":CanFish(out _)?"Fishing. Next catch in "+Shipyard.FormatDuration(Math.Max(0,seconds.Value-view.GetZDO().GetFloat(Progress)))+".":"Waiting for space in the station chest.");return true;};
    }
    private bool CanFish(out ItemDrop? item)
    {
        item=ObjectDB.instance?ObjectDB.instance.GetItemPrefab(Catch)?.GetComponent<ItemDrop>():null;
        if(!item||!Chest||Chest.GetInventory()==null||Chest.IsInUse()||view.GetZDO().GetInt(ZDOVars.s_inUse)!=0||!EnvMan.instance||!EnvMan.IsDaylight())return false;
        var inventory=Chest.GetInventory();
        return inventory.CountItems(item.m_itemData.m_shared.m_name)<limit.Value&&inventory.CanAddItem(item.m_itemData,1);
    }
    private void Update()
    {
        if(!view||!view.IsValid()||!ZNet.instance)return;
        bool working=CanFish(out var item);
        if(bird!=null)
        {
            bird.Root.transform.position=transform.TransformPoint(perch);
            bird.Fishing(working);
            bird.Root.transform.rotation=WorkerAnchor?WorkerAnchor.rotation:transform.rotation;
            float bob=Mathf.Sin(Time.time*.75f);
            bird.Pose(working?12+6*bob:0,working?10*bob:0,0,working?4:0,0,working?8+4*bob:0);
            bool nearby=Player.m_localPlayer&&(Player.m_localPlayer.transform.position-bird.Root.transform.position).sqrMagnitude<9;
            bool sleeping=!working&&EnvMan.instance&&!EnvMan.IsDaylight()&&!nearby;
            if(!sleeping)bird.Idle(Time.time+idleOffset,working);
            bird.Rest(sleeping,Time.time+idleOffset,Time.deltaTime);
        }
        if(Time.time<next)return;next=Time.time+1;
        double now=ZNet.instance.GetTimeSeconds();double elapsed=previous>0?Math.Max(0,Math.Min(2,now-previous)):0;previous=now;
        // Only the current owner writes production. Unloaded/night time cannot become
        // a backlog of free daytime catches on the next visit.
        if(!view.IsOwner()||!working)return;
        float progress=Mathf.Clamp(view.GetZDO().GetFloat(Progress),0,seconds.Value)+(float)elapsed;
        if(progress>=seconds.Value)
        {
            var catchItem=item!.m_itemData.Clone();catchItem.m_stack=1;catchItem.m_dropPrefab=ObjectDB.instance.GetItemPrefab(Catch);
            if(Chest.GetInventory().AddItem(catchItem))progress-=seconds.Value;
        }
        view.GetZDO().Set(Progress,Mathf.Min(progress,seconds.Value));
    }
    private void OnDestroy()=>bird?.Dispose();
}
