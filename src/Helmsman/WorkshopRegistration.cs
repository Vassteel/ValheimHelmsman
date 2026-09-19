using System;
using System.Linq;
using Helmsman.Core;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Helmsman;
internal static class WorkshopRegistration
{
    internal const string SlipwayPrefab="HelmsmanSlipway";
    internal static void Register()
    {
        Add(SlipwayPrefab,"slipway","Shipwright slipway","Select a large ship with the hammer to construct it here. Keep the seaward end and launch water clear.","Wood:100,RoundLog:40,BronzeNails:40",null);
        Add("HelmsmanToolRack","tool-rack","Shipwright tool rack","Shortens construction time for new slipway orders by 25%.","Wood:15,Bronze:5", "tools");
        Add("HelmsmanCaulkingStation","caulking-station","Caulking station","New workshop ships receive 15% more hull durability.","Wood:15,Resin:20,Bronze:2","caulking");
        Add("HelmsmanRiggingRack","rigging-rack","Rigging rack","New workshop ships receive 10% more sail force.","Wood:15,LeatherScraps:10,Bronze:2","rigging");
        Add("HelmsmanPaintStand","paint-stand","Shipwright paint stand","Unlocks the puffin's paint and decoration services.","Wood:15,Resin:10,Coal:5","paint");
    }
    private static void Add(string id,string model,string name,string description,string recipe,string? kind)
    {
        var go=PrefabManager.Instance.CreateClonedPrefab(id,"wood_floor");
        WorkshopModels.Apply(go,model);
        var piece=go.GetComponent<Piece>();piece.m_waterPiece=false;piece.m_noClipping=false;piece.m_groundOnly=false;piece.m_usage=Piece.UsageTagFlags.Crafting;
        var wear=go.GetComponent<WearNTear>();if(wear){wear.m_health=1000;wear.m_noRoofWear=false;wear.m_noSupportWear=kind!=null;}
        if(kind==null){go.AddComponent<Slipway>();go.AddComponent<WorkstationLease>();}
        else {go.AddComponent<WorkshopUpgrade>().Kind=kind;go.AddComponent<WorkshopUpgradeHover>();}
        BoatyardModels.RefreshIcon(go);
        PieceManager.Instance.AddPiece(new CustomPiece(go,false,new PieceConfig{Name=name,Description=description,PieceTable="Hammer",Category="Helmsman",CraftingStation="piece_workbench",Requirements=ShipwrightRules.Costs(recipe).Select(c=>new RequirementConfig(c.Key,c.Value,0,true)).ToArray()}));
    }
}
public sealed class WorkshopUpgrade:MonoBehaviour
{
    public string Kind="";
}
internal static class WorkshopRange
{
    internal const float Range=30;
    internal static bool Connected(Player player,params Vector3[] points)
    {
        if(QuartermasterWorkshop.Available)return QuartermasterWorkshop.SharesZone(player,points);
        for(int i=1;i<points.Length;i++){var d=points[0]-points[i];if(!WorkshopCoverage.Contains(Range,d.x,d.y,d.z))return false;}
        return true;
    }
    internal static Shipyard? Bench(Player player,Vector3 point)=>UnityEngine.Object.FindObjectsByType<Shipyard>(FindObjectsSortMode.None)
        .Where(b=>b&&b.Ready&&PrivateArea.CheckAccess(b.transform.position,0,false,true)&&Connected(player,b.transform.position,point,player.transform.position))
        .OrderBy(b=>(b.transform.position-point).sqrMagnitude).FirstOrDefault();
    internal static bool Upgrade(Shipyard bench,Player player,string kind,Vector3? worksite=null)=>UnityEngine.Object.FindObjectsByType<WorkshopUpgrade>(FindObjectsSortMode.None)
        .Any(u=>u&&u.Kind==kind&&u.GetComponent<ZNetView>()&&u.GetComponent<ZNetView>().IsValid()&&PrivateArea.CheckAccess(u.transform.position,0,false,true)&&Connected(player,bench.transform.position,u.transform.position,player.transform.position,worksite??player.transform.position));
}
