using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;
namespace Helmsman.Structures;

[Serializable]
internal sealed class StructureOrder
{
    public string name="",supplied="",recipe="",status="Waiting for materials";
    public BuildPiece[] pieces=Array.Empty<BuildPiece>();
    public int next;
    public long creator;
    public Vector3 origin;
    public Quaternion rotation;
    public long placedUser;
    public uint placedId;
    // ZDOID uses private backing fields and is not a JsonUtility save format.
    internal ZDOID placed {get=>new ZDOID(placedUser,placedId);set{placedUser=value.UserID;placedId=value.ID;}}
}
// The bench owns the job and its escrow. Disconnects pause work; owners resume
// from the saved piece receipt without spawning or charging that piece twice.
public sealed class StructureConstruction:MonoBehaviour
{
    private const string Key="helmsman_structure_order_v1";
    private ZNetView view=null!;private Shipyard yard=null!;
    private float tick;
    private byte[]? serialized;
    private StructureOrder? cached;
    internal bool Busy=>view&&view.IsValid()&&view.GetZDO().GetByteArray(Key)?.Length>0;
    internal StructureOrder? Order
    {
        get
        {
            if(!view||!view.IsValid())return null;
            var bytes=view.GetZDO().GetByteArray(Key);if(bytes==null||bytes.Length==0)return null;
            if(ReferenceEquals(bytes,serialized))return cached;
            try
            {
                using var input=new GZipStream(new MemoryStream(bytes),CompressionMode.Decompress);
                using var output=new MemoryStream();var chunk=new byte[4096];int read;
                while((read=input.Read(chunk,0,chunk.Length))>0){output.Write(chunk,0,read);if(output.Length>2_000_000)throw new InvalidDataException("Construction plan is too large");}
                var state=JsonUtility.FromJson<StructureOrder>(Encoding.UTF8.GetString(output.ToArray()));
                if(state==null||state.pieces==null||state.pieces.Length==0||state.next<0||state.next>state.pieces.Length)return null;
                serialized=bytes;cached=state;return state;
            }
            catch(Exception e){Helmsman.Plugin.Instance.Record("Retaining unreadable structure order: "+e.Message);return null;}
        }
    }
    private void Awake(){view=GetComponent<ZNetView>();yard=GetComponent<Shipyard>();}
    private void Start(){var wear=GetComponent<WearNTear>();if(wear)wear.m_onDestroyed+=ReturnEscrow;}
    private void OnDestroy(){var wear=GetComponent<WearNTear>();if(wear)wear.m_onDestroyed-=ReturnEscrow;}
    private void Save(StructureOrder state)
    {
        var text=Encoding.UTF8.GetBytes(JsonUtility.ToJson(state));if(text.Length>2_000_000)throw new InvalidOperationException("Structure exceeds the saved construction plan limit.");
        using var output=new MemoryStream();using(var zip=new GZipStream(output,System.IO.Compression.CompressionLevel.Fastest,true))zip.Write(text,0,text.Length);
        var bytes=output.ToArray();view.GetZDO().Set(Key,bytes);serialized=bytes;cached=state;
    }
    private void Clear(){view.GetZDO().Set(Key,Array.Empty<byte>());serialized=null;cached=null;}
    internal static bool SupportedPrefab(GameObject prefab)
    {
        if(!prefab||!prefab.GetComponent<Piece>()||prefab.GetComponent<Ship>()||prefab.GetComponent<StructureConstruction>())return false;
        var components=prefab.GetComponentsInChildren<MonoBehaviour>(true);
        bool spawner=components.Any(c=>c&&(c.GetType().Name=="SpawnArea"||c.GetType().Name=="CreatureSpawner"||c.GetType().Name=="SpawnSystem"));
        return PrefabPolicy.Rejection(prefab.GetComponent<ZNetView>()!=null,prefab.GetComponentInChildren<Character>(true)!=null,
            spawner,prefab.GetComponentInChildren<TerrainOp>(true)!=null||prefab.GetComponentInChildren<TerrainModifier>(true)!=null,prefab.GetComponent<ItemDrop>()!=null)==null
            &&Recipe(prefab.GetComponent<Piece>()).Length>0;
    }
    internal string Validate(Blueprint plan,Vector3 origin,Quaternion rotation,Player player)
    {
        if(!view||!view.IsValid()||!view.IsOwner())return "Wait for ownership of the puffin bench, then try again.";
        if(!StructureImports.Allowed||!yard||!yard.Ready||!player||player.IsDead()||!PrivateArea.CheckAccess(yard.transform.position,0,false,true))return "An accessible puffin bench and administrator access are required.";
        if(Order!=null||Busy&&view.GetZDO().GetByteArray(Key).Length>0)return "This bench already has a construction order.";
        if(plan.Pieces.Count>5000)return "Use structure sections of at most 5,000 pieces for staged construction.";
        foreach(var p in plan.Pieces)
        {
            if(!SupportedPrefab(ZNetScene.instance.GetPrefab(p.Prefab)))return "Available building pieces changed. Select the file again to skip unsupported objects.";
            var at=origin+rotation*new Vector3(p.X,p.Y,p.Z);
            if(!WorkshopRange.Connected(player,yard.transform.position,at))return "The entire structure must be inside this workshop's Quartermaster zone or bench range.";
            if(Location.IsInsideNoBuildLocation(at)||!PrivateArea.CheckAccess(at,0,false,true))return "Structure overlaps restricted building space.";
        }
        return "";
    }
    internal string Queue(Blueprint plan,string name,Vector3 origin,Quaternion rotation,Player player)
    {
        string problem=Validate(plan,origin,rotation,player);if(problem.Length>0)return problem;
        Save(new StructureOrder{name=Path.GetFileNameWithoutExtension(name),pieces=plan.Pieces.OrderBy(p=>p.Y).ToArray(),creator=player.GetPlayerID(),origin=origin,rotation=rotation});
        return "";
    }
    internal static string Recipe(Piece piece)=>!piece||piece.m_resources==null?"":string.Join(",",piece.m_resources.Where(r=>r!=null&&r.m_resItem&&r.m_amount>0).GroupBy(r=>Utils.GetPrefabName(r.m_resItem.gameObject)).OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>g.Key+":"+g.Sum(r=>r.m_amount)));
    private static bool FreeBuild(StructureOrder order,Piece piece)
    {
        var player=Player.m_localPlayer;
        // Global build/craft modifiers apply per piece. A local cheat may only
        // waive this player's own job, never somebody else's networked order.
        return piece&&ZoneSystem.instance&&ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey())
            ||player&&player.GetPlayerID()==order.creator&&player.NoCostCheat();
    }
    internal string Status
    {
        get
        {
            var s=Order;if(s==null)return Busy&&view.GetZDO().GetByteArray(Key).Length>0?"Saved structure needs inspection.":"No structure queued.";
            var prefab=s.next<s.pieces.Length?ZNetScene.instance.GetPrefab(s.pieces[s.next].Prefab):null;
            bool free=FreeBuild(s,prefab?prefab.GetComponent<Piece>():null!);
            string need=free?"":s.recipe.Length>0?WorkshopMaterialSupply.Missing(s.recipe,s.supplied):"Checking the next foundation piece";
            string status=free&&s.status=="Waiting for Quartermaster or bench supplies"?"No-cost construction":s.status;
            return s.name+" · "+s.next+" / "+s.pieces.Length+" pieces\n"+status+(free?"\nNo materials required for the next piece.":need.Length>0?"\nNeeded next: "+need:"");
        }
    }
    internal string RemainingMaterials()=>string.Join(", ",RemainingCosts().Where(c=>c.Value>0).Select(c=>WorkshopMaterialSupply.ItemName(c.Key)+" × "+c.Value));
    internal Dictionary<string,int> RemainingCosts()
    {
        var s=Order;if(s==null)return new Dictionary<string,int>();
        var costs=new Dictionary<string,int>(StringComparer.Ordinal);
        for(int i=s.next;i<s.pieces.Length;i++)
        {
            var prefab=ZNetScene.instance.GetPrefab(s.pieces[i].Prefab);if(!prefab||FreeBuild(s,prefab.GetComponent<Piece>()))continue;
            foreach(var c in ShipwrightRules.Costs(Recipe(prefab.GetComponent<Piece>())))costs[c.Key]=(costs.TryGetValue(c.Key,out var n)?n:0)+c.Value;
        }
        foreach(var c in ConstructionFunding.Supplied(s.supplied))if(costs.ContainsKey(c.Key))costs[c.Key]=Math.Max(0,costs[c.Key]-c.Value);
        return costs;
    }
    private void Update()
    {
        if(Time.time<tick)return;tick=Time.time+2;
        if(!view||!view.IsValid()||!view.IsOwner()||!Player.m_localPlayer||!StructureImports.Allowed)return;
        var s=Order;if(s==null)return;
        try{Advance(s);}catch(Exception e){s.status="Construction paused: "+e.GetBaseException().Message;Save(s);Helmsman.Plugin.Instance.Error(e);tick=Time.time+10;}
    }
    private void Advance(StructureOrder s)
    {
        if(!yard)yard=GetComponent<Shipyard>();
        var player=Player.m_localPlayer;
        if(!WorkshopRange.Connected(player,yard.transform.position,player.transform.position)||!PrivateArea.CheckAccess(yard.transform.position,0,false,true))return;
        if(s.placed!=ZDOID.None)
        {
            // A durable spawn receipt is consumed once, including after a reload.
            s.next++;s.placed=ZDOID.None;s.supplied="";s.recipe="";Save(s);
        }
        if(s.next>=s.pieces.Length){Clear();StructureImports.Report(s.name+" construction complete.");return;}
        var part=s.pieces[s.next];var prefab=ZNetScene.instance.GetPrefab(part.Prefab);if(!prefab){s.status="Waiting for "+part.Prefab;Save(s);return;}
        var at=s.origin+s.rotation*new Vector3(part.X,part.Y,part.Z);
        if(!Heightmap.FindHeightmap(at)||!WorkshopRange.Connected(player,yard.transform.position,at)){s.status="Waiting for loaded terrain inside the workshop zone";Save(s);return;}
        if(Location.IsInsideNoBuildLocation(at)||!PrivateArea.CheckAccess(at,0,false,true)){s.status="Building access blocked";Save(s);return;}
        string recipe=Recipe(prefab.GetComponent<Piece>());
        bool free=FreeBuild(s,prefab.GetComponent<Piece>());
        if(s.recipe.Length==0){s.recipe=recipe;Save(s);}
        if(recipe!=s.recipe||!ConstructionFunding.Valid(recipe,s.supplied)){s.status="Recipe changed; cancel to recover supplied materials";Save(s);return;}
        if(!NativeBuildingSupport.Stable(yard.gameObject)){s.status="Support the puffin workbench before construction continues";Save(s);return;}
        WorkshopMaterialSupply.Collect(player,yard,recipe,s.supplied,s.creator,r=>{s.supplied=r;Save(s);},free);
        if(!free&&!ConstructionFunding.Complete(recipe,s.supplied)){s.status="Waiting for Quartermaster or bench supplies";Save(s);return;}
        // Probe the actual material and collision shape through vanilla support,
        // then immediately remove an unsupported candidate without resource drops.
        // Never suppress wear or give the candidate artificial support.
        GameObject? go=null;bool committed=false;
        try
        {
            go=Instantiate(prefab,at,s.rotation*new Quaternion(part.Qx,part.Qy,part.Qz,part.Qw));
            var network=go.GetComponent<ZNetView>();if(!network||!network.IsValid())throw new InvalidOperationException("Piece network state unavailable");
            if(part.HasScale){go.transform.localScale=new Vector3(part.ScaleX,part.ScaleY,part.ScaleZ);network.GetZDO().Set(ImportedScale.Key,go.transform.localScale);}
            if(!NativeBuildingSupport.Stable(go))
            {s.status="Needs structural support: "+part.Prefab+". Add foundations or beams; supplied materials are held.";Save(s);return;}
            go.GetComponent<Piece>().SetCreator(s.creator,Splatform.PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            if(part.Data.Length>0)go.GetComponent<TextReceiver>()?.SetText(part.Data);
            s.placed=network.GetZDO().m_uid;s.status="Building "+part.Prefab;Save(s);committed=true;
            tick=Time.time+(WorkshopRange.Upgrade(yard,player,"tools")?1.5f:2);
        }
        finally{if(go&&!committed){go.SetActive(false);ZNetScene.instance.Destroy(go);}}
    }
    internal string Cancel(Player player)
    {
        if(!yard.Near(player)||!view.IsOwner()||!StructureImports.Allowed)return "Return to the puffin bench as an administrator.";
        ReturnEscrow();return "Construction cancelled. Unused supplied materials returned; completed pieces remain.";
    }
    private void ReturnEscrow()
    {
        if(!view||!view.IsValid()||!view.IsOwner())return;
        var s=Order;if(s==null)return;
        var refund=s.placed==ZDOID.None?ConstructionFunding.Supplied(s.supplied):new Dictionary<string,int>();
        Clear();WorkshopMaterialSupply.Return(refund,transform.position+Vector3.up);
    }
}

internal static class NativeBuildingSupport
{
    private static readonly System.Reflection.MethodInfo Update=AccessTools.Method(typeof(WearNTear),"UpdateSupport");
    private static readonly System.Reflection.MethodInfo Have=AccessTools.Method(typeof(WearNTear),"HaveSupport");
    internal static bool Stable(GameObject go)
    {
        var wear=go.GetComponent<WearNTear>();if(!wear||wear.m_noSupportWear)return true;
        var view=wear.GetComponent<ZNetView>();if(!view||!view.IsValid())return false;
        if(!view.IsOwner())return (float)AccessTools.Method(typeof(WearNTear),"GetSupport").Invoke(wear,null)>=(float)AccessTools.Method(typeof(WearNTear),"GetMinSupport").Invoke(wear,null);
        Physics.SyncTransforms();Update.Invoke(wear,null);return (bool)Have.Invoke(wear,null);
    }
}
