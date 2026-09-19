using Helmsman;
using Helmsman.Core;
using UnityEngine;
using Newtonsoft.Json.Linq;
int count=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;}
var damaged=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"../../../missing-order.json"));
Check(SlipwayOrderCodec.EmptyBrokenBlueprint(damaged,out var bench)&&bench.Length>0,"Recognize the actual empty broken world-save record");
try{SlipwayOrderCodec.Read(damaged);throw new Exception("Accepted missing order");}catch(System.IO.InvalidDataException){}
var paid=JObject.Parse(damaged);paid["supplied"]="Wood:5";
Check(!SlipwayOrderCodec.EmptyBrokenBlueprint(paid.ToString(),out _),"Never clear contributed materials");
paid=JObject.Parse(damaged);paid["awaitingMaterials"]=false;
Check(!SlipwayOrderCodec.EmptyBrokenBlueprint(paid.ToString(),out _),"Never clear a paid or free active build");
paid=JObject.Parse(damaged);paid["launchStarted"]=1;
Check(!SlipwayOrderCodec.EmptyBrokenBlueprint(paid.ToString(),out _),"Never clear a launched order");
Check(!SlipwayOrderCodec.EmptyBrokenBlueprint("{",out _),"Never discard unrecognized corrupt records");
foreach(bool drop in new[]{false,true})
foreach(var plan in ShipConstruction.Blueprints.Where(b=>b.UsesSlipway))
foreach(bool free in new[]{false,true})
{
 JsonUtility.DropNested=drop;
 var state=new SlipwayOrder{bench="1:94827",stage=new(1,2,3),awaitingMaterials=!free,crewCalledAt=free?500:0,
 order=new ConstructionOrder{blueprint=plan.Id,recipe=plan.Recipe,name=plan.Name,started=500,creator=123,duration=plan.BuildSeconds,freeBuild=free,position=new(4,5,6),heading=180}};
 var json=SlipwayOrderCodec.Write(state);var read=SlipwayOrderCodec.Read(json);
 Check(read.order.blueprint==plan.Id&&read.order.recipe==plan.Recipe&&read.order.creator==123&&read.order.position.z==6&&read.order.freeBuild==free&&read.bench==state.bench,"Ship identity, payment mode, launch and bench survive the serializer");
 Check(read.awaitingMaterials==!free&&read.AwaitingCrew==free&&read.Elapsed(700)==0,"Reloaded job drives waiting ghost or crew arrival before building");
 Check(!SlipwayOrderCodec.EmptyBrokenBlueprint(json,out _),"Valid job cannot be cleared by recovery");
}
Console.WriteLine($"PASS: {count} production slipway save checks, including the actual missing-payload fixture and a serializer that drops nested ship data.");
