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
foreach(var name in new[]{"FishingDock","FishingDock_Extension"})
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
