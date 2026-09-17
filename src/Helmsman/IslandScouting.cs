using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

[Serializable]
internal sealed class ScoutJob
{
    public long owner,dockUser,tableUser;
    public uint dockId,tableId;
    public string token="",dockName="",error="";
    public Vector3 origin;
    public double due;
    public bool collected;
    // These are transmitted but regenerated from terrain after a server restart.
    [NonSerialized] internal IslandSurvey? survey;
    public bool ready;
    internal ZDOID Dock=>new(dockUser,dockId);
    internal ZDOID Table=>new(tableUser,tableId);
}
[Serializable]
internal sealed class ScoutSave { public int version=1;public List<ScoutJob> jobs=new(); }

// Only the server reads undiscovered location records. The saved job contains no
// map data; terrain and POIs are delivered personally after collecting the gull.
public sealed class IslandScouting : MonoBehaviour
{
    private const string RequestRpc="HelmsmanScoutRequest",StateRpc="HelmsmanScoutState",ReportRpc="HelmsmanScoutReport";
    private readonly Dictionary<long,ScoutJob> jobs=new();
    private readonly Dictionary<long,float> requests=new();
    private readonly HashSet<long> sending=new();
    private ZRoutedRpc? registered;
    private long world;
    private string savePath="";
    private bool saveBlocked;
    private float nextPoll;
    private long character;
    private ConfigEntry<float> duration=null!;
    internal ScoutJob? Current {get;private set;}
    internal string Message {get;private set;}="Ask the gull to chart this island.";
    internal bool Applying {get;private set;}
    internal static IslandScouting Instance=>Plugin.Instance.GetComponent<IslandScouting>();
    internal string ReceiptKey=>"helmsman_scout_report_"+world;
    private void Awake()
    {
        duration=Plugin.Instance.Config.Bind("Scouting","SurveySeconds",300f,
            new ConfigDescription("Minimum time in seconds for a gull island survey. Large landmasses may take longer to chart.",new AcceptableValueRange<float>(30,7200),new ConfigurationManagerAttributes {IsAdminOnly=true}));
    }
    private void Register()
    {
        if(registered==ZRoutedRpc.instance)return;
        registered=ZRoutedRpc.instance;if(registered==null)return;
        registered.Register<ZPackage>(RequestRpc,Request);
        registered.Register<ZPackage>(StateRpc,ReceiveState);
        registered.Register<ZPackage>(ReportRpc,ReceiveReport);
    }
    private void Update()
    {
        if(!ZNet.instance||!ZoneSystem.instance||ZDOMan.instance==null||WorldGenerator.instance==null||!ZNetScene.instance)
        {if(world!=0){StopAllCoroutines();world=0;jobs.Clear();requests.Clear();sending.Clear();Applying=false;}Current=null;character=0;return;}
        Register();long nextWorld=ZNet.instance.GetWorldUID();
        if(world!=nextWorld)
        {
            StopAllCoroutines();world=nextWorld;Current=null;Applying=false;nextPoll=0;character=0;
            jobs.Clear();requests.Clear();sending.Clear();saveBlocked=false;
            savePath=Path.Combine(Paths.ConfigPath,"ValheimHelmsman","scouting",world+".json");
            if(ZNet.instance.IsServer())LoadJobs();
        }
        if(ZNet.instance.IsServer())
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();
            foreach(var job in jobs.Values.Where(j=>!j.collected&&j.error.Length==0).ToArray())
            {
                EnsureSurvey(job);
                if(job.survey!.State==SurveyState.SearchingShore||job.survey.State==SurveyState.Surveying)job.survey.Step(32);
                if(job.survey.State==SurveyState.Failed)job.error=job.survey.Error;
                job.ready=job.survey.State==SurveyState.Complete&&ZNet.instance.GetTimeSeconds()>=job.due;
                if(watch.ElapsedMilliseconds>=2)break;
            }
        }
        if(!Player.m_localPlayer)return;
        long id=Player.m_localPlayer.GetPlayerID();
        if(character!=id){character=id;Current=null;nextPoll=0;}
        if(id!=0 && Time.unscaledTime>=nextPoll)
        {nextPoll=Time.unscaledTime+5;Send("status",ZDOID.None,ZDOID.None,"");}
    }
    private void EnsureSurvey(ScoutJob job)
    {
        job.survey??=new IslandSurvey(job.origin.x,job.origin.z,WaterChart.Sea,(x,z)=>WorldGenerator.instance.GetHeight(x,z));
    }
    private void LoadJobs()
    {
        try
        {
            if(!File.Exists(savePath))return;
            var info=new FileInfo(savePath);if(info.Length>4*1024*1024)throw new InvalidDataException("Scouting save is too large.");
            var save=JsonUtility.FromJson<ScoutSave>(File.ReadAllText(savePath));
            if(save==null||save.version!=1||save.jobs==null||save.jobs.Count>4096)throw new InvalidDataException("Invalid scouting save.");
            foreach(var job in save.jobs)
            {
                if(job.owner==0||job.token.Length!=32||double.IsNaN(job.due)||double.IsInfinity(job.due)||job.due<0||
                    !Finite(job.origin.x)||!Finite(job.origin.z))throw new InvalidDataException("Invalid saved scouting job.");
                job.ready=false;job.error="";jobs.Add(job.owner,job);
            }
        }
        catch(Exception error){saveBlocked=true;jobs.Clear();Plugin.Instance.Error(error);}
    }
    private void SaveJobs()
    {
        if(saveBlocked)throw new IOException("The existing scouting save needs inspection; it has not been overwritten.");
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        string temporary=savePath+".tmp";
        File.WriteAllText(temporary,JsonUtility.ToJson(new ScoutSave {jobs=jobs.Values.ToList()}));
        if(File.Exists(savePath))File.Replace(temporary,savePath,savePath+".bak");else File.Move(temporary,savePath);
    }
    private static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n)&&Mathf.Abs(n)<10000;
    private static long Server=>ZNet.instance.GetServerPeer()?.m_uid??ZNet.GetUID();
    private bool Identity(long sender,out long owner,out Vector3 position)
    {
        owner=0;position=Vector3.zero;
        if(sender==ZNet.GetUID()&&Player.m_localPlayer)
        {owner=Player.m_localPlayer.GetPlayerID();position=Player.m_localPlayer.transform.position;return owner!=0&&!Player.m_localPlayer.IsDead();}
        var peer=ZNet.instance.GetPeer(sender);if(peer==null||!peer.IsReady())return false;
        var data=ZDOMan.instance.GetZDO(peer.m_characterID);if(data==null)return false;
        owner=data.GetLong(ZDOVars.s_playerID);position=data.GetPosition();return owner!=0;
    }
    private static bool TableData(ZDOID id,out ZDO? data)
    {
        data=ZDOMan.instance.GetZDO(id);
        var prefab=data!=null?ZNetScene.instance.GetPrefab(data.GetPrefab()):null;
        return prefab&&prefab.GetComponent<MapTable>();
    }
    private void Request(long sender,ZPackage pkg)
    {
        if(!ZNet.instance||!ZNet.instance.IsServer()||!Identity(sender,out var owner,out var position))return;
        try
        {
            string action=pkg.ReadString();var dockId=pkg.ReadZDOID();var tableId=pkg.ReadZDOID();string token=pkg.ReadString();
            if(action.Length>16||token.Length>32)return;
            if(action!="ack"&&action!="status")
            {if(requests.TryGetValue(sender,out var at)&&Time.unscaledTime-at<.3f)return;requests[sender]=Time.unscaledTime;}
            jobs.TryGetValue(owner,out var job);
            if(action=="start"||action=="table")
            {
                if(saveBlocked){Reply(sender,job,"Scouting save unavailable; your existing reports have been preserved.");return;}
                var dock=DockDirectory.Resolve(dockId);
                if(dock==null||(position-dock.MarkerPosition).sqrMagnitude>100)
                {Reply(sender,job,"Stand beside a configured Dock Ward.");return;}
                if(!TableData(tableId,out var table)||(table!.GetPosition()-dock.MarkerPosition).sqrMagnitude>100*100)
                {Reply(sender,job,"Choose a Cartographer's Table within 100 m of the dock.");return;}
                if(!ScoutAccess.Allowed(owner,dock.MarkerPosition)||!ScoutAccess.Allowed(owner,table!.GetPosition()))
                {Reply(sender,job,"You do not have access to this dock or table.");return;}
                if(action=="table")
                {
                    if(job==null||job.token!=token)return;
                    long oldUser=job.tableUser;uint oldId=job.tableId;
                    job.tableUser=tableId.UserID;job.tableId=tableId.ID;
                    try{SaveJobs();}catch{job.tableUser=oldUser;job.tableId=oldId;throw;}
                    Reply(sender,job,"The gull will bring your report to that table.");return;
                }
                if(job!=null&&!job.collected)
                {Reply(sender,job,"Collect or cancel your current survey first.");return;}
                if(job==null&&jobs.Count>=4096){Reply(sender,null,"The scouting ledger is full.");return;}
                var fresh=new ScoutJob {owner=owner,token=Guid.NewGuid().ToString("N"),dockUser=dockId.UserID,dockId=dockId.ID,
                    tableUser=tableId.UserID,tableId=tableId.ID,origin=dock.MarkerPosition,dockName=dock.Berth.name,
                    due=ZNet.instance.GetTimeSeconds()+duration.Value};
                jobs[owner]=fresh;
                try{SaveJobs();}catch{if(job==null)jobs.Remove(owner);else jobs[owner]=job;throw;}
                Reply(sender,fresh,"The gull is scouting the island. Collect his report at the Cartographer's Table.");return;
            }
            if(action=="cancel")
            {
                if(job==null||job.token!=token)return;
                jobs.Remove(owner);try{SaveJobs();}catch{jobs[owner]=job;throw;}
                Reply(sender,null,"Scouting cancelled.");return;
            }
            if(action=="ack")
            {
                if(job==null||job.token!=token)return;
                bool wasCollected=job.collected;job.collected=true;try{SaveJobs();}catch{job.collected=wasCollected;throw;}job.survey=null;Reply(sender,job,"Island chart received.");return;
            }
            if(action=="collect")
            {
                if(job==null||job.token!=token)return;
                if(!TableData(job.Table,out var table)||(position-table!.GetPosition()).sqrMagnitude>64)
                {Reply(sender,job,"Speak to the gull at his Cartographer's Table.");return;}
                if(!ScoutAccess.Allowed(owner,table!.GetPosition())){Reply(sender,job,"You do not have access to this table.");return;}
                EnsureSurvey(job);
                if(job.survey!.State!=SurveyState.Complete||ZNet.instance.GetTimeSeconds()<job.due)
                {job.collected=false;Reply(sender,job,"The gull is still preparing your report.");return;}
                if(sending.Add(owner))StartCoroutine(SendReport(sender,job));
                return;
            }
            if(action=="status")Reply(sender,job,"");
        }
        catch(Exception error){Plugin.Instance.Error(error);Reply(sender,jobs.TryGetValue(owner,out var job)?job:null,"The scouting request could not be completed. Your saved report is retained.");}
    }
    private IEnumerator SendReport(long sender,ScoutJob job)
    {
        var scan=job.survey!;
        var points=ZoneSystem.instance.m_locationInstances.Values
            .Where(l=>l.m_location!=null&&scan.Contains(l.m_position.x,l.m_position.z))
            .Select(l=>new ScoutPoint(l.m_position.x,l.m_position.z,ScoutingPoi.Label(l.m_location.m_name)))
            .Where(p=>p.Name.Length>0).ToArray();
        if(points.Length>10000){sending.Remove(job.owner);Reply(sender,job,"Too many points of interest for one report.");yield break;}
        var report=new ScoutReport(scan.Cells,points);long reportWorld=world;
        var task=System.Threading.Tasks.Task.Run(()=>report.Encode());
        while(!task.IsCompleted)yield return null;
        sending.Remove(job.owner);
        if(!ZNet.instance||world!=reportWorld||!jobs.TryGetValue(job.owner,out var current)||current.token!=job.token)yield break;
        if(task.IsFaulted){Plugin.Instance.Error(task.Exception!);Reply(sender,job,"The report could not be prepared. It is still saved.");yield break;}
        var reply=new ZPackage();reply.Write(world);reply.Write(job.token);reply.Write(task.Result);
        if(sender==ZNet.GetUID()){reply.SetPos(0);ReceiveReport(sender,reply);}else registered!.InvokeRoutedRPC(sender,ReportRpc,reply);
    }
    private void Reply(long sender,ScoutJob? job,string message)
    {
        if(job!=null&&job.collected)job.ready=true;
        var reply=new ZPackage();reply.Write(world);reply.Write(job==null?"":JsonUtility.ToJson(job));reply.Write(message);
        if(sender==ZNet.GetUID()){reply.SetPos(0);ReceiveState(sender,reply);}else registered!.InvokeRoutedRPC(sender,StateRpc,reply);
    }
    private void ReceiveState(long sender,ZPackage pkg)
    {
        if(!ZNet.instance||sender!=Server||!Player.m_localPlayer)return;
        try
        {
            if(pkg.ReadLong()!=world)return;
            string json=pkg.ReadString(),message=pkg.ReadString();if(json.Length>4096)return;
            var job=json.Length>0?JsonUtility.FromJson<ScoutJob>(json):null;
            if(job!=null&&job.owner!=Player.m_localPlayer.GetPlayerID())return;
            if(job!=null&&Player.m_localPlayer.m_customData.TryGetValue(ReceiptKey,out var received)&&received==job.token)
            {if(!job.collected)Send("ack",ZDOID.None,ZDOID.None,job.token);job=null;}
            bool newJob=job!=null&&Current?.token!=job.token;
            Current=job;if(message.Length>0){Message=message;Plugin.Message(message);}
            if(newJob)ScoutGullPresentation.Depart(job!);
        }
        catch(Exception error){Plugin.Instance.Error(error);}
    }
    private void ReceiveReport(long sender,ZPackage pkg)
    {
        if(!ZNet.instance||sender!=Server||!Player.m_localPlayer||Applying)return;
        try
        {
            if(pkg.ReadLong()!=world)return;
            string token=pkg.ReadString();if(Current==null||Current.token!=token)return;
            var report=ScoutReport.Decode(pkg.ReadByteArray());Applying=true;
            StartCoroutine(ApplyReport(report,token,world,Player.m_localPlayer));
        }
        catch(Exception error){Applying=false;Plugin.Instance.Error(error);Plugin.Message("The report could not be read. It is still saved with the gull.");}
    }
    private IEnumerator ApplyReport(ScoutReport report,string token,long reportWorld,Player player)
    {
        var apply=ScoutMap.Apply(report,()=>world==reportWorld&&player&&player==Player.m_localPlayer);
        bool failed=false;
        while(true)
        {
            bool next;
            try{next=apply.MoveNext();}
            catch(Exception error){Plugin.Instance.Error(error);failed=true;break;}
            if(!next)break;yield return apply.Current;
        }
        Applying=false;
        if(failed||world!=reportWorld||!player||player!=Player.m_localPlayer)
        {Plugin.Message("Map update paused. Collect the saved report again.");yield break;}
        player.m_customData[ReceiptKey]=token;
        Send("ack",ZDOID.None,ZDOID.None,token);Current=null;
        Plugin.Message("Island mapped. "+report.Points.Count+" points of interest charted.");
    }
    private void Send(string action,ZDOID dock,ZDOID table,string token)
    {
        if(!ZNet.instance||registered==null)return;
        var pkg=new ZPackage();pkg.Write(action);pkg.Write(dock);pkg.Write(table);pkg.Write(token);
        if(ZNet.instance.IsServer()){pkg.SetPos(0);Request(ZNet.GetUID(),pkg);}else registered.InvokeRoutedRPC(Server,RequestRpc,pkg);
    }
    internal void Begin(DockMarker dock,MapTable table)
    {
        if(!dock.Ready||!Player.m_localPlayer||!table||!PrivateArea.CheckAccess(dock.transform.position,0,false,true)||!PrivateArea.CheckAccess(table.transform.position,0,false,true))return;
        if(Plugin.Instance.Voyage||Plugin.Instance.Summon||Plugin.Instance.CalledGull)
        {Plugin.Message("Let the gull finish his current voyage or ship call first.");return;}
        var view=table.GetComponent<ZNetView>();if(!view||!view.IsValid())return;
        Send("start",dock.Id,view.GetZDO().m_uid,"");
    }
    internal void Retarget(DockMarker dock,MapTable table)
    {
        if(Current==null||!table||!PrivateArea.CheckAccess(table.transform.position,0,false,true)||!PrivateArea.CheckAccess(dock.transform.position,0,false,true))return;
        var view=table.GetComponent<ZNetView>();if(view&&view.IsValid())Send("table",dock.Id,view.GetZDO().m_uid,Current.token);
    }
    internal void Cancel(){if(Current!=null&&!Applying)Send("cancel",ZDOID.None,ZDOID.None,Current.token);}
    internal void Collect()
    {
        if(Current==null||Applying||!Current.ready)return;
        var table=ZNetScene.instance.FindInstance(Current.Table);
        if(!table||!Player.m_localPlayer||(table.transform.position-Player.m_localPlayer.transform.position).sqrMagnitude>64||!PrivateArea.CheckAccess(table.transform.position,0,false,true))return;
        Send("collect",ZDOID.None,ZDOID.None,Current.token);
    }
    internal string Status=>Applying?"Copying the gull's chart onto your map…":Current==null?Message:
        Current.error.Length>0?Current.error:Current.ready?"The gull has your island chart. Speak to him at the Cartographer's Table.":
        "Scouting "+Current.dockName+" · "+(ZNet.instance.GetTimeSeconds()<Current.due?Shipyard.FormatDuration(Current.due-ZNet.instance.GetTimeSeconds())+" remaining":"Finishing the island chart…");
}
