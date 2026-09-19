// Runs the production ghost hook; Unity collision cooking needs an in-game check.
using System.Reflection;
using Helmsman;
using UnityEngine;
var hook=typeof(ShorePlacementPreview).GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic)!;
void Apply(GameObject ghost)=>hook.Invoke(null,new object[]{ghost});
void Check(bool ok,string message){if(!ok)throw new Exception(message);}
Apply(null);
var vanilla=new GameObject("vanilla chest");Apply(vanilla);
Check(vanilla.transform.Children.Count==0,"Vanilla previews must stay untouched");
var prefab=new GameObject("rebuilt shore prefab");
var bounds=new Bounds{center=new Vector3(1,2,-3),size=new Vector3(4,6,8)};
prefab.AddComponent<ShoreBuildBounds>().Bounds=bounds;
Check(prefab.transform.Children.Count==0,"World prefab must not get a filled collision box");
var ghost=new GameObject("shore preview"){layer=31};ghost.AddComponent<ShoreBuildBounds>().Bounds=bounds;
Apply(ghost);
Check(ghost.transform.Children.Count==1,"Preview needs a native placement collider");
var proxy=ghost.transform.Children.Single();var box=proxy.gameObject.GetComponent<BoxCollider>();
Check(box!=null,"Concave-only geometry must gain a primitive ClosestPoint target");
Check(box.center==bounds.center && box.size==bounds.size,"Collider must preserve asymmetric mesh bounds");
Check(proxy.gameObject.layer==ghost.layer && !proxy.WorldPositionStays,"Proxy must use ghost layer and prefab-local coordinates");
Check(prefab.transform.Children.Count==0,"Creating a preview must not mutate the world prefab");
var station=new GameObject("FishingDock");
var stationPiece=station.AddComponent<Piece>();stationPiece.m_noClipping=true;stationPiece.m_waterPiece=false;
var stationBounds=station.AddComponent<ShoreBuildBounds>();
ShoreBuildBounds.ConfigureDock(station,"FishingDock");
Check(stationPiece.m_noClipping&&!stationPiece.m_waterPiece&&!stationBounds.RelaxedDockPlacement,"Freestanding pelican station must retain ordinary furniture placement checks");
foreach(var name in new[]{"FishingDock_Extension"})
{
    var dock=new GameObject(name);
    var piece=dock.AddComponent<Piece>();piece.m_noClipping=true;piece.m_waterPiece=true;
    var marker=dock.AddComponent<ShoreBuildBounds>();marker.Bounds=bounds;
    ShoreBuildBounds.ConfigureDock(dock,name);
    Check(!piece.m_noClipping && !piece.m_waterPiece,"Docks must allow building overlap without the ship placement lift");
    Check(marker.RelaxedDockPlacement,"Dock previews must opt out of enclosing-box player collisions");
    Check(dock.GetComponent<BoxCollider>()==null,"Registered docks must not gain solid bounding boxes");
    var dockGhost=new GameObject("dock preview"){layer=31};
    var ghostMarker=dockGhost.AddComponent<ShoreBuildBounds>();ghostMarker.Bounds=marker.Bounds;ghostMarker.RelaxedDockPlacement=marker.RelaxedDockPlacement;
    var chest=new GameObject("chest collider");chest.transform.SetParent(dockGhost.transform,false);var chestBox=chest.AddComponent<BoxCollider>();
    Apply(dockGhost);
    var rootBox=dockGhost.GetComponent<BoxCollider>();
    Check(rootBox!=null && rootBox.center==bounds.center && rootBox.size==bounds.size,"Relaxed dock still needs a root ClosestPoint target");
    Check(dockGhost.transform.Children.Count==1 && chest.GetComponent<BoxCollider>()==chestBox,"Actual child colliders must remain available for native player collision checks");
    Check(dock.GetComponent<BoxCollider>()==null,"Dock preview must not alter finished dock collision");
}
var otherPiece=prefab.AddComponent<Piece>();otherPiece.m_noClipping=true;otherPiece.m_waterPiece=true;
ShoreBuildBounds.ConfigureDock(prefab,"OilPress");
Check(otherPiece.m_noClipping && otherPiece.m_waterPiece && !prefab.GetComponent<ShoreBuildBounds>().RelaxedDockPlacement,"Other harbor pieces must retain their restrictions");
ShoreBuildBounds.ConfigureDock(vanilla,"piece_chest");
Check(vanilla.GetComponent<ShoreBuildBounds>()==null,"Vanilla pieces must remain unchanged");
Console.WriteLine("PASS: ghost-only placement bounds; docks allow overlap without enclosing-box player collisions; actual child colliders, other pieces and world prefabs remain unchanged.");

var slip=new GameObject("slipway ghost");slip.AddComponent<Slipway>();
slip.AddComponent<ShoreBuildBounds>().Bounds=new Bounds{center=new Vector3(0,1.8f,1),size=new Vector3(8,3.6f,22)};
var points=new List<Transform>();
foreach(var name in new[]{"snap_support_-3_-10","snap_side_4_0","snap_head_support_-3","snap_side_4_10","snap_head_4","snap_side_-4_10","snap_head_-4"}) {
 var obj=new GameObject(name);obj.transform.SetParent(slip.transform,false);points.Add(obj.transform);
}
SlipwaySnapping.OrderCorners(points);
Check(slip.transform.Children.Take(4).Select(t=>t.name).SequenceEqual(new[]{"Shore corner left","Shore corner right","Water corner left","Water corner right"}),"Corner cycle order must precede both edges and supports");
Check(slip.transform.Children[4].name=="snap_side_4_0","Deck edge must precede buried support points");
Check(slip.transform.Children.Distinct().Count()==7,"Reordering must preserve every snap transform");
Check(SlipwaySnapping.SearchRadius(vanilla.transform)==10,"Vanilla snap radius must remain exactly unchanged");
Check(SlipwaySnapping.SearchRadius(slip.transform)>22 && SlipwaySnapping.SearchRadius(slip.transform)<30,"Search must reach the far corner plus the native neighborhood");
var transpiler=typeof(SlipwaySnapSearch).GetMethod("Transpiler",BindingFlags.Static|BindingFlags.NonPublic)!;
var instructions=new[]{new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Ldc_R4,10f),new HarmonyLib.CodeInstruction(System.Reflection.Emit.OpCodes.Ldc_R4,.5f)};
var patched=((IEnumerable<HarmonyLib.CodeInstruction>)transpiler.Invoke(null,new object[]{instructions})!).ToArray();
Check(patched.Length==3 && patched[0].opcode==System.Reflection.Emit.OpCodes.Ldarg_1 && patched[1].opcode==System.Reflection.Emit.OpCodes.Call && (float)patched[2].operand==.5f,"Production hook must replace only search radius, preserving snap tolerance");
Console.WriteLine("PASS: production slipway corner ordering, complete point retention, scoped search radius and unchanged snap tolerance.");
