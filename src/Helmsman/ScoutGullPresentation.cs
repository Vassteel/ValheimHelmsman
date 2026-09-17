using UnityEngine;

namespace Helmsman;

// Client-only actor. Survey authority and persistence live in IslandScouting.
public sealed class ScoutGullPresentation : MonoBehaviour
{
    private GullGuide? reportGull;
    private string token="",announced="";
    private ZDOID tableId;
    internal static bool BusyDock(ZDOID dock)=>IslandScouting.Instance&&IslandScouting.Instance.Current?.Dock==dock;
    internal static void Depart(ScoutJob job)
    {
        var dock=ZNetScene.instance?ZNetScene.instance.FindInstance(job.Dock)?.GetComponent<DockMarker>():null;
        if(dock)dock.SendScout();
    }
    private void Update()
    {
        var job=IslandScouting.Instance.Current;
        if(job==null||!job.ready||!Player.m_localPlayer||!ZNetScene.instance)
        {Release();return;}
        var table=ZNetScene.instance.FindInstance(job.Table)?.GetComponent<MapTable>();
        if(!table){Release();return;}
        if(token!=job.token||tableId!=job.Table){Release();token=job.token;tableId=job.Table;}
        if(!reportGull)
        {
            reportGull=GullGuide.Create(table.transform.position+Vector3.up*12-table.transform.forward*18,null,null);
            reportGull.DeliverScoutReport(table);
        }
        if(reportGull.ReportLanded&&announced!=job.token&&(Player.m_localPlayer.transform.position-reportGull.transform.position).sqrMagnitude<225)
        {
            announced=job.token;reportGull.Speak("Squawk! Your island chart is ready. Come take a look!",true,true);
        }
    }
    private void Release(){if(reportGull)reportGull.FlyAway();reportGull=null;token="";}
    private void OnDestroy(){if(reportGull)Destroy(reportGull.gameObject);}
}
