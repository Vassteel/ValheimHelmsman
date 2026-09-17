using System.Linq;
using UnityEngine;

namespace Helmsman;

public sealed partial class HelmsmanUI
{
    private int scoutTableIndex;
    private string scoutSnapshot="";
    private string ScoutState=>IslandScouting.Instance.Current is ScoutJob job?job.token+"/"+job.ready+"/"+job.error:"none";
    private void BuildScouting()
    {
        if(!dock)return;
        var scout=IslandScouting.Instance;scoutSnapshot=ScoutState;
        var tables=FindObjectsByType<MapTable>(FindObjectsSortMode.None)
            .Where(t=>(t.transform.position-dock.transform.position).sqrMagnitude<100*100&&PrivateArea.CheckAccess(t.transform.position,0,false,true))
            .OrderBy(t=>(t.transform.position-dock.transform.position).sqrMagnitude).ToArray();
        scoutTableIndex=tables.Length>0?scoutTableIndex%tables.Length:0;
        var table=tables.Length>0?tables[scoutTableIndex]:null;
        bool active=scout.Current!=null;
        var text=theme!.Text(body!,active?scout.Status:"Send the gull to scout this island. He returns to a Cartographer's Table with terrain discoveries and points of interest.",0,0,672,100,21,MenuTheme.Gold);
        if(active)refreshLabels.Add(()=>text.text=scout.Status);
        Button(body!,table?"Cartographer's Table · "+Vector3.Distance(table.transform.position,dock.transform.position).ToString("0")+" m  ›":"No Cartographer's Table within 100 m",0,-113,672,()=>{scoutTableIndex++;rebuild=true;});
        theme.Text(body!,active?"Speak to the returned gull to copy his chart onto your map. Uncollected reports are saved. If his table is gone, choose another here.":"Connected land only. Nearby separate islands stay hidden. Your map updates when you collect his report.",0,-165,672,82,19,MenuTheme.Muted);
        var action=Button(body!,active?"Use this return table":"Scout this island",0,-265,672,()=>
        {
            if(!dock||!table)return;
            if(active)scout.Retarget(dock,table);else scout.Begin(dock,table);
        });
        action.interactable=table&&!scout.Applying;
        Button(body!,"Refresh tables",0,-318,324,()=>rebuild=true);
        if(active)Button(body!,"Cancel and discard survey",344,-318,328,()=>{scout.Cancel();}).interactable=!scout.Applying;
    }
}
