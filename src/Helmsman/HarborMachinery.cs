using System;
using System.Collections.Generic;
using System.Linq;
using Helmsman.Core;
using Jotunn.Managers;
using UnityEngine;

namespace Helmsman;

[Serializable] internal sealed class HarborRigPart
{
    public string tag="",kind="";
    public Vector3 pivot=Vector3.zero,axis=Vector3.up;
    public float lift=0,ratio=1;
}
[Serializable] internal sealed class HarborRigSpec
{
    public string key="",prefab="",name="";
    public float lift=0;
    public HarborRigPart[] rigs=Array.Empty<HarborRigPart>();
}

internal static class NativeHarbor
{
    internal static string? Model(string id)=>id switch {
        "ShipConstruction"=>"keel-cradle","ShipConstruction1"=>"roller-bed","ShipConstruction2"=>"framing-rack",
        "PierCrane1"=>"pier-crane","PierCrane2"=>"heavy-crane","PulleyCobia"=>"single-pulley",
        "PulleyElephantSeal"=>"double-pulley","PulleyMarlin"=>"triple-pulley",_=>null
    };
    internal static GameObject Create(string id,string model)
    {
        var go=PrefabManager.Instance.CreateClonedPrefab(id,"wood_floor");
        WorkshopModels.Apply(go,"harbor-"+model);
        var piece=go.GetComponent<Piece>();piece.m_waterPiece=false;piece.m_noClipping=false;piece.m_groundOnly=false;
        var wear=go.GetComponent<WearNTear>();wear.m_health=1000;wear.m_noSupportWear=true;wear.m_noRoofWear=false;
        if(model!="keel-cradle")go.AddComponent<WorkstationLease>();
        return go;
    }
}

// Network time drives a hand-operated demonstration cycle. The machinery is
// visual: it never moves player hulls, transfers inventory or awards bonuses.
public sealed class HarborMachinery:MonoBehaviour,Hoverable,Interactable
{
    [SerializeField] internal HarborRigSpec Spec=new();
    private const string StartedKey="helmsman_harbor_cycle_started";
    private ZNetView view=null!;
    private readonly List<(Transform pivot,HarborRigPart rig)> pivots=new();
    private readonly List<(Mesh mesh,Vector3[] rest,Vector3[] current,Vector2[] weights)> ropes=new();
    private bool prepared;
    private float lastTravel=float.NaN;
    private long Now=>ZNet.instance?ZNet.instance.GetTime().Ticks:0;
    private bool Ready=>view&&view.IsValid();
    private long Started=>Ready?view.GetZDO().GetLong(StartedKey):0;
    private void Awake()=>view=GetComponent<ZNetView>();
    public string GetHoverName()=>Spec.name;
    public float GetHoverOffset()=>0;
    public string GetHoverText()=>Spec.key=="keel-cradle"?Spec.name:Localization.instance.Localize(Spec.name+"\n[<color=yellow><b>$KEY_Use</b></color>] "+(HarborMotion.Busy(Started,Now)?"Working…":"Operate"));
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
    public bool Interact(Humanoid user,bool hold,bool alt)
    {
        if(hold||Spec.key=="keel-cradle"||user is not Player player||!Ready)return false;
        Plugin.Message(GetComponent<WorkstationLease>().Run(()=>{
            if(!Ready||!view.IsOwner()||!PrivateArea.CheckAccess(transform.position,0,false,true))return "This equipment is not accessible.";
            if(HarborMotion.Busy(Started,Now))return "The equipment is already working.";
            view.GetZDO().Set(StartedKey,Now);return "Operating "+Spec.name.ToLowerInvariant()+".";
        }));
        return true;
    }
    private void Prepare()
    {
        var parts=GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.StartsWith("Authored harbor-",StringComparison.Ordinal)).ToArray();
        foreach(var rig in Spec.rigs)
        {
            var members=parts.Where(f=>f.name.Contains(" "+rig.tag+" ")).ToArray();if(members.Length==0)continue;
            var pivot=new GameObject("Harbor motion "+rig.tag).transform;pivot.SetParent(transform,false);pivot.localPosition=rig.pivot;
            foreach(var f in members)f.transform.SetParent(pivot,true);
            pivots.Add((pivot,rig));
        }
        foreach(var f in parts.Where(f=>f.name.Contains(" running-rope ")))
        {
            var mesh=UnityEngine.Object.Instantiate(f.sharedMesh);mesh.name="Working harbor rope";mesh.MarkDynamic();f.sharedMesh=mesh;
            var rest=mesh.vertices;ropes.Add((mesh,rest,new Vector3[rest.Length],mesh.uv2));
        }
        prepared=true;
    }
    private void LateUpdate()
    {
        if(!Ready||!Player.m_localPlayer)return;
        // Do not allocate cosmetic rigs for distant structures or server-only copies.
        if((Player.m_localPlayer.transform.position-transform.position).sqrMagnitude>6400)return;
        if(!prepared)Prepare();
        float travel=HarborMotion.Travel(HarborMotion.Elapsed(Started,Now));
        if(travel==lastTravel)return;lastTravel=travel;
        float distance=travel*Spec.lift;
        foreach(var (pivot,rig) in pivots)
        {
            pivot.localPosition=rig.pivot+Vector3.up*(distance*rig.lift);
            float radians=rig.kind=="roller"?travel*1.6f*rig.ratio:rig.kind=="wheel"?distance*rig.ratio:0;
            pivot.localRotation=Quaternion.AngleAxis(radians*Mathf.Rad2Deg,rig.axis);
        }
        foreach(var (mesh,rest,current,weights) in ropes)
        {
            for(int i=0;i<rest.Length;i++)current[i]=rest[i]+Vector3.up*(distance*weights[i].x);
            mesh.vertices=current;mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
    }
    private void OnDestroy(){foreach(var r in ropes)if(r.mesh)UnityEngine.Object.Destroy(r.mesh);}
}
