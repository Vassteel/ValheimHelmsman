using System.Collections;
using System.Reflection;
using System.IO.Compression;
using Helmsman;
using Helmsman.Core;
using UnityEngine;

int checks=0;
void Check(bool yes,string name){if(!yes)throw new Exception("FAIL: "+name);checks++;Console.WriteLine("PASS: "+name);}
IslandSurvey Finish(IslandSurvey s){for(int i=0;i<200000&&s.State is SurveyState.SearchingShore or SurveyState.Surveying;i++)s.Step(17);return s;}
var island=Finish(new IslandSurvey(0,0,30,(x,z)=>Math.Abs(z)<=24&&(Math.Abs(x)<=24||x>=64&&x<=96)?35:20));
Check(island.State==SurveyState.Complete&&island.Cells.Count==49,"Connected island includes all terrain cells");
Check(!island.Contains(80,0),"Neighbouring island is not surveyed");
var channel=Finish(new IslandSurvey(0,0,30,(x,z)=>Math.Abs(z)<1&&x>=0&&x<=16&&!(x>2&&x<6)?35:20));
Check(channel.Cells.Count==1&&!channel.Contains(8,0),"Narrow channel between grid centres separates islands");
var diagonal=Finish(new IslandSurvey(0,0,30,(x,z)=>(Math.Abs(x)<=1&&Math.Abs(z)<=1)||(Math.Abs(x-8)<=1&&Math.Abs(z-8)<=1)?35:20));
Check(diagonal.Cells.Count==1,"Diagonal contact cannot join islands");
var alternate=Finish(new IslandSurvey(0,0,30,(x,z)=>x>=0&&x<=8&&z>=0&&z<=8&&!(z==0&&x>2&&x<6)?35:20));
Check(alternate.Cells.Count==4,"Land rejected across one edge can join from another connected edge");
Check(Finish(new IslandSurvey(0,0,30,(x,z)=>20)).State==SurveyState.Failed,"Open water has no false island report");
Check(Finish(new IslandSurvey(0,0,30,(x,z)=>35,5)).State==SurveyState.Failed,"Survey budget failure never returns a partial success");
Check(Finish(new IslandSurvey(9792,0,30,(x,z)=>35)).State==SurveyState.Failed,"World edge stops survey explicitly");
Check(Finish(new IslandSurvey(0,0,30,(x,z)=>float.NaN)).State==SurveyState.Failed,"Unavailable heights fail explicitly");
int samples=0;var incremental=new IslandSurvey(0,0,30,(x,z)=>{samples++;return 35;});incremental.Step(1);Check(samples==1,"Shore search is incremental");samples=0;incremental.Step(10);Check(samples<=40,"Per-update terrain query count is bounded");
var nearest=Finish(new IslandSurvey(28,0,30,(x,z)=>Math.Abs(z)<=8&&(x>=0&&x<=16||x>=48&&x<=64)?35:20));
Check(nearest.Contains(16,0)&&!nearest.Contains(48,0),"Dock over water selects its nearest shoreline");
var report=new ScoutReport(island.Cells,new[]{new ScoutPoint(8,8,"Ruins")});var roundtrip=ScoutReport.Decode(report.Encode());
Check(roundtrip.Cells.SetEquals(report.Cells)&&roundtrip.Points.Single().Name=="Ruins","Compressed report preserves terrain and POIs");
byte[] Bad(Action<BinaryWriter> write){using var m=new MemoryStream();using(var g=new GZipStream(m,CompressionLevel.Fastest,true))using(var w=new BinaryWriter(g))write(w);return m.ToArray();}
void Reject(byte[] bytes,string name){bool rejected=false;try{ScoutReport.Decode(bytes);}catch(InvalidDataException){rejected=true;}catch(EndOfStreamException){rejected=true;}Check(rejected,name);}
Reject(Bad(w=>{w.Write(2);}),"Unknown report versions rejected");
Reject(Bad(w=>{w.Write(1);w.Write(1000001);}),"Oversized decompressed counts rejected");
Reject(Bad(w=>{w.Write(1);w.Write(2);w.Write(0);w.Write(0);w.Write(0);w.Write(0);}),"Duplicate report cells rejected");
Reject(Bad(w=>{w.Write(1);w.Write(1);w.Write(int.MinValue);w.Write(0);}),"Invalid coordinates do not overflow bounds checks");
Reject(Bad(w=>{w.Write(1);w.Write(1);}),"Truncated cell data rejected");
Reject(Bad(w=>{w.Write(1);w.Write(1);w.Write(0);w.Write(0);w.Write(1);w.Write(float.NaN);w.Write(0f);w.Write(1);}),"Invalid POI coordinates rejected");
Reject(Bad(w=>{w.Write(1);w.Write(1);w.Write(0);w.Write(0);w.Write(0);w.Write(1);}),"Trailing report data rejected");
Check(ScoutingPoi.Label("SunkenCrypt4")=="Sunken crypt"&&ScoutingPoi.Label("DvergrTownEntrance1")=="Infested mine","Dungeon POI labels resolve");
Check(ScoutingPoi.Label("BossStone")==""&&ScoutingPoi.Label("Hildir_cave")==""&&ScoutingPoi.Label("StartTemple")=="","Excluded special locations are not disclosed");
void Drain(IEnumerator e){while(e.MoveNext()){} }
Drain(ScoutMap.Apply(report,()=>true));var map=Minimap.instance;
Check(map.Explored.Count==196&&map.m_pins.Count==1&&map.Saves==1,"Native map copy reveals only report cells and saves map data");
Drain(ScoutMap.Apply(report,()=>true));Check(map.m_pins.Count==1&&map.Explored.Count==196,"Repeated delivery preserves existing terrain and deduplicates POIs");
map.AddPin(new Vector3(100,0,100),Minimap.PinType.Icon3,"My home",true,false);
Drain(ScoutMap.Apply(report,()=>true));Check(map.m_pins.Any(p=>p.m_name=="My home"),"Existing custom markers are preserved");
var many=new ScoutReport(Enumerable.Range(0,600).Select(i=>new IslandCell(i%30,i/30)),Array.Empty<ScoutPoint>());
var interrupted=ScoutMap.Apply(many,()=>false);bool abort=false;try{interrupted.MoveNext();}catch(InvalidOperationException){abort=true;}Check(abort,"Map application stops if character changes");
map=new Minimap();Minimap.instance=map;
BepInEx.Paths.ConfigPath=Path.Combine(Path.GetTempPath(),"helmsman-scout-test-"+Guid.NewGuid().ToString("N"));
void Invoke(IslandScouting s,string method,params object[] args){try{typeof(IslandScouting).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(s,args);}catch(TargetInvocationException e){throw e.InnerException!;}}
IslandScouting Create(){var s=new IslandScouting();Plugin.Instance.Scout=s;Invoke(s,"Awake");Invoke(s,"Update");return s;}
void Advance(IslandScouting s,int frames=100){for(int i=0;i<frames;i++){Time.unscaledTime+=.05f;Invoke(s,"Update");s.Tick();}}
void Request(IslandScouting s,long sender,string action,ZDOID dock=default,ZDOID table=default,string token=""){
 var q=new ZPackage();q.Write(action);q.Write(dock);q.Write(table);q.Write(token);q.SetPos(0);Time.unscaledTime+=1;Invoke(s,"Request",sender,q);
}
void CompleteCoroutines(IslandScouting s){for(int i=0;i<2000&&s.Routines.Count>0;i++){s.Tick();Thread.Sleep(1);}Check(s.Routines.Count==0,"Report delivery finishes");}
var dock=new DockMarker{Id=new(42,1)};DockDirectory.Docks[dock.Id]=new();
var tableData=new ZDO{m_uid=new(42,2),Position=new Vector3(8,0,0)};
ZDOMan.instance.Data[tableData.m_uid]=tableData;
var table=new MapTable{View=new(){Data=tableData}};table.transform.position=tableData.Position;
ZNetScene.instance.Prefabs[1]=new(){Component=table};
var tableObject=new GameObject{Component=table};tableObject.transform.position=tableData.Position;ZNetScene.instance.Live[tableData.m_uid]=tableObject;
ZoneSystem.instance.m_locationInstances[1]=new(){m_location=new(){m_name="WoodHouse1"},m_position=new Vector3(8,0,8)};
ZoneSystem.instance.m_locationInstances[2]=new(){m_location=new(){m_name="WoodHouse1"},m_position=new Vector3(80,0,8)};
try {
 var s=Create();s.Begin(dock,table);
 Check(s.Current!=null&&ScoutGullPresentation.Departures==1,"Host request rewinds package and starts gull departure");
 string token=s.Current!.token;string save=Path.Combine(BepInEx.Paths.ConfigPath,"ValheimHelmsman/scouting/7.json");
 Check(File.Exists(save)&&File.ReadAllText(save).Contains(token),"Survey persists before confirming departure");
 Time.unscaledTime+=1;s.Begin(dock,table);Check(s.Current!.token==token,"Duplicate start preserves existing survey");
 Advance(s);Check(map.Explored.Count==0&&map.m_pins.Count==0&&!s.Current!.ready,"No terrain or POIs appear while gull is away");
 Request(s,42,"collect",token:token);Check(map.Explored.Count==0,"Premature collection reveals nothing");
 ZNet.instance.Clock=301;Advance(s);Check(s.Current!.ready,"Completed survey waits for collection");
 // Recreate entire service: no completed terrain payload is required in the save.
 s.StopAllCoroutines();s=Create();Advance(s);Check(s.Current!.token==token&&s.Current.ready,"Uncollected report survives service restart and terrain reconstruction");
 Player.m_localPlayer.transform.position=new Vector3(100,0,0);Request(s,42,"collect",token:token);Check(s.Routines.Count==0&&map.Explored.Count==0,"Collection away from table is rejected");
 Player.m_localPlayer.transform.position=tableData.Position;ScoutAccess.DeniedOwner=10;Request(s,42,"collect",token:token);Check(map.Explored.Count==0,"Server enforces requesting player's table permissions");ScoutAccess.DeniedOwner=0;
 Minimap.instance=null;Time.unscaledTime+=1;s.Collect();CompleteCoroutines(s);
 Check(s.Current?.token==token&&!s.Applying&&!Player.m_localPlayer.m_customData.ContainsKey(s.ReceiptKey),"Unavailable map never acknowledges or discards the report");
 Minimap.instance=map;Time.unscaledTime+=1;s.Collect();CompleteCoroutines(s);
 Check(map.Explored.Count==196&&map.m_pins.Count==1&&map.m_pins[0].m_pos.x==8,"Collect reveals island terrain and only its POIs");
 Check(s.Current==null&&Player.m_localPlayer.m_customData[s.ReceiptKey]==token,"Successful map copy records personal receipt");
 Check(File.ReadAllText(save).Contains("\"collected\":true"),"Server retains acknowledged metadata for crash recovery");
 s=Create();Advance(s);Check(s.Current==null,"Received report stays completed after reconnect");
 // Simulate crash before native character/profile save: no receipt or map survived.
 Player.m_localPlayer.m_customData.Clear();Minimap.instance=new();s=Create();Advance(s);
 Check(s.Current?.token==token,"Lost client save can recover already delivered report");
 s.Collect();Advance(s,200);Time.unscaledTime+=1;s.Collect();CompleteCoroutines(s);
 Check(Minimap.instance.m_pins.Count==1&&Minimap.instance.Explored.Count==196,"Recovery reconstructs map without losing report");
 // Remote requests derive identity from server-owned peer character records.
 ZDOMan.instance.Data[new(99,1)]=new(){m_uid=new(99,1),PlayerID=20};ZNet.instance.Peers[99]=new(){m_uid=99,m_characterID=new(99,1)};
 Request(s,123,"start",dock.Id,tableData.m_uid);Check(!File.ReadAllText(save).Contains("\"owner\":20"),"Unknown network sender cannot create jobs");
 ScoutAccess.DeniedOwner=20;Request(s,99,"start",dock.Id,tableData.m_uid);Check(!File.ReadAllText(save).Contains("\"owner\":20"),"Remote ward denial is enforced by character identity");ScoutAccess.DeniedOwner=0;
 Request(s,99,"start",dock.Id,tableData.m_uid);Check(File.ReadAllText(save).Contains("\"owner\":20"),"Authorized remote request persists its own job");
 var remoteReply=ZRoutedRpc.instance.Sent.Last().Data;remoteReply.SetPos(0);remoteReply.ReadLong();var remoteJob=JsonUtility.FromJson<ScoutJob>(remoteReply.ReadString());
 Request(s,42,"cancel",token:remoteJob.token);Check(File.ReadAllText(save).Contains(remoteJob.token),"One player cannot cancel another player's survey");
 var spoof=new ZPackage();spoof.Write(7L);spoof.Write(remoteJob.token);spoof.Write(report.Encode());spoof.SetPos(0);Invoke(s,"ReceiveReport",99L,spoof);Check(s.Current==null,"Report from non-server peer is ignored");
 var replacement=new ZDO{m_uid=new(42,3),Position=new Vector3(16,0,0)};ZDOMan.instance.Data[replacement.m_uid]=replacement;
 Request(s,99,"table",dock.Id,replacement.m_uid,remoteJob.token);
 Check(File.ReadAllText(save).Contains("\"tableId\":3"),"Return table can be reassigned without discarding survey");
 Request(s,99,"cancel",token:remoteJob.token);Check(!File.ReadAllText(save).Contains(remoteJob.token),"Explicit cancellation removes only the caller's report");
 // Same-world exit clears coroutine and client state; next session reloads disk.
 ZNet.instance=null;Invoke(s,"Update");Check(!s.Applying&&s.Current==null,"Session exit clears report application state");ZNet.instance=new(){Clock=1000};Invoke(s,"Update");Advance(s);Check(s.Current==null,"Same-world reconnect honors saved personal receipt");
 // Preserve malformed save instead of overwriting a user's existing reports.
 File.WriteAllText(save,"broken save");s=Create();Time.unscaledTime+=1;s.Begin(dock,table);Check(File.ReadAllText(save)=="broken save"&&s.Current==null,"Damaged ledger is preserved and new requests fail safely");
} finally {System.IO.Directory.Delete(BepInEx.Paths.ConfigPath,true);}
Console.WriteLine($"PASS: {checks} scouting and map regression checks.");
