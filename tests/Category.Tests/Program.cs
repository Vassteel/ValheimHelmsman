using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helmsman;
using Helmsman.Core;
int checks=0;
void Check(bool ok,string what){checks++;if(!ok)throw new Exception(what);}
object Call(Type t,string method,params object[] args)=>t.GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
object[] arrays={new[]{Piece.UsageTagFlags.Misc,Piece.UsageTagFlags.Decor,(Piece.UsageTagFlags)0},new[]{"Misc","Decor","Skylands"}};
Call(typeof(AddHelmsmanUsageCategory),"Postfix",arrays);
var names=(string[])arrays[1];var flags=(Piece.UsageTagFlags[])arrays[0];
Check(names.Length==4&&flags.Length==4,"Aligned category arrays");
Check(names.Take(3).SequenceEqual(new[]{"Misc","Decor","Skylands"}),"Preserve other mods and native tag indices");
Check(names[3]==BuildMenuCategory.Token&&flags[3]==0,"Virtual tag registered without consuming bits");
Call(typeof(AddHelmsmanUsageCategory),"Postfix",arrays);Check(((string[])arrays[1]).Length==4,"Registration is idempotent");
var table=new PieceTable();var dock=new Piece{gameObject=Plugin.DockPrefab};var foreign=new Piece{gameObject="Skylands_other"};var repair=new Piece{gameObject="repair",m_repairPiece=true};
table.m_availablePieces.UnionWith(new[]{dock,foreign,repair});
foreach(var entry in HarborCatalog.Pieces)Check(BuildMenuCategory.Contains(new Piece{gameObject=entry.Prefab}),"Catalog piece included: "+entry.Prefab);
foreach(var id in new[]{"Totem1","Totem2","Totem3","Totem4","Enguias","Peixes","RedePesca","OilPress","FishingDock_Extension"})
{
 Check(!HarborCatalog.Pieces.Any(entry=>entry.Prefab==id),"Retired piece is not registered: "+id);
 Check(!BuildMenuCategory.Contains(new Piece{gameObject=id}),"Retired piece is not in the build category: "+id);
}
foreach(var ship in ShipConstruction.Blueprints)Check(BuildMenuCategory.Contains(new Piece{gameObject=ship.Prefab}),"Hammer ship category includes: "+ship.Prefab);
foreach(var id in new[]{"HelmsmanSlipway","HelmsmanToolRack","HelmsmanCaulkingStation","HelmsmanRiggingRack","HelmsmanPaintStand"})Check(BuildMenuCategory.Contains(new Piece{gameObject=id}),"Workshop piece appears in Helmsman: "+id);
var result=new List<Piece>();Check(!(bool)Call(typeof(FilterHelmsmanUsageCategory),"Prefix",3,table,result,names),"Own category handles filtering");
Check(result.Contains(dock)&&result.Contains(repair)&&!result.Contains(foreign),"Only Helmsman plus native repair entries");
result.Clear();Check((bool)Call(typeof(FilterHelmsmanUsageCategory),"Prefix",2,table,result,names)&&result.Count==0,"Other-mod filters untouched");
Check((bool)Call(typeof(FilterHelmsmanUsageCategory),"Prefix",-1,table,result,names),"All-pieces view untouched");
var tags=new List<int>{0,1,2,3};Call(typeof(HideUnrelatedHelmsmanCategory),"Postfix",table,names,tags);Check(tags.Contains(3),"Helmsman visible for Hammer pieces");
table.m_availablePieces.Remove(dock);Call(typeof(HideUnrelatedHelmsmanCategory),"Postfix",table,names,tags);Check(tags.SequenceEqual(new[]{0,1,2}),"Unrelated tool hides only Helmsman");
Console.WriteLine($"PASS: {checks} production category checks: mixed-mod arrays, all harbor entries, filtering, repeated registration and other tools.");
