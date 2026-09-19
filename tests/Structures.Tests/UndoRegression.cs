using System;
using System.Collections.Generic;
using Helmsman.Structures;

// Run the production undo method with a small scene/network adapter. An unloaded
// object retains its network record; a removed object has neither record nor scene object.
static class UndoRegression
{
    internal static void Run(Action<bool,string> check)
    {
        PlacementTool Setup()
        {
            StructureImports.Allowed=true;ZNet.instance=new ZNet();ZNetScene.instance=new ZNetScene();ZDOMan.instance=new ZDOMan();
            var tool=new PlacementTool();tool.lastPieces.AddRange(new[]{1,2,3});
            foreach(var id in tool.lastPieces){ZDOMan.instance.records.Add(id);ZNetScene.instance.objects[id]=new UnityEngine.GameObject();}
            return tool;
        }
        void Removed(int id){ZNetScene.instance.objects.Remove(id);ZDOMan.instance.records.Remove(id);}
        void Reject(PlacementTool tool,string reason)
        {
            bool rejected=false;try{tool.UndoForTest();}catch(InvalidOperationException){rejected=true;}
            check(rejected,reason+" rejects undo");
            check(ZNetScene.instance.destroyed==0&&!tool.lastTerrain.undone&&!tool.lastVegetation.undone,reason+" has no partial mutation");
            check(tool.lastPieces.Count==3,reason+" retains retry history");
        }
        var t=Setup();Removed(2);var terrain=t.lastTerrain;var vegetation=t.lastVegetation;
        t.UndoForTest();check(ZNetScene.instance.destroyed==2,"Missing piece does not block removing survivors");
        check(terrain.undone&&vegetation.undone,"Partial structure still restores terrain and vegetation");
        check(t.lastPieces.Count==0&&t.lastTerrain==null&&t.lastVegetation==null,"Successful undo clears history");
        t.UndoForTest();check(ZNetScene.instance.destroyed==2,"Repeated undo does not delete anything else");
        t=Setup();foreach(var id in new[]{1,2,3})Removed(id);terrain=t.lastTerrain;
        t.UndoForTest();check(terrain.undone&&ZNetScene.instance.destroyed==0,"All pieces already removed still permits terrain restore");
        t=Setup();Removed(1);ZNetScene.instance.objects.Remove(3);Reject(t,"Unloaded surviving piece");
        ZNetScene.instance.objects[3]=new UnityEngine.GameObject();t.UndoForTest();check(ZNetScene.instance.destroyed==2,"Reloaded piece can be retried after a missing piece");
        t=Setup();Removed(1);ZNetScene.instance.objects[3].containers=new[]{new Container{items=1}};Reject(t,"Occupied container");
        t=Setup();Removed(1);ZNetScene.instance.objects[3].view.owner=false;Reject(t,"Unowned surviving piece");
        t=Setup();ZNetScene.instance.objects[3].view.valid=false;Reject(t,"Invalid network view");
        t=Setup();t.lastTerrain.safe=false;Reject(t,"Subsequent terrain edits");
        t=Setup();t.lastVegetation.safe=false;Reject(t,"Unavailable vegetation area");
        t=Setup();ZNet.instance.uid=2;Reject(t,"Different world");
        t=Setup();StructureImports.Allowed=false;Reject(t,"Missing admin permission");
    }
}

namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value)=>value!=null; }
    public class GameObject:Object
    {
        internal ZNetView view=new ZNetView();internal Container[] containers=Array.Empty<Container>();
        public T GetComponent<T>() where T:class=>view as T;
        public T[] GetComponentsInChildren<T>()=>containers as T[];
    }
}
class ZNet:UnityEngine.Object { public static ZNet instance;public long uid=1;public long GetWorldUID()=>uid; }
class ZDOMan { public static ZDOMan instance;public HashSet<int> records=new HashSet<int>();public object GetZDO(int id)=>records.Contains(id)?(object)id:null; }
class ZNetScene:UnityEngine.Object
{
    public static ZNetScene instance;public Dictionary<int,UnityEngine.GameObject> objects=new Dictionary<int,UnityEngine.GameObject>();public int destroyed;
    public UnityEngine.GameObject FindInstance(int id)=>objects.TryGetValue(id,out var value)?value:null;
    public void Destroy(UnityEngine.GameObject go){destroyed++;}
}
class ZNetView:UnityEngine.Object { public bool valid=true,owner=true;public bool IsValid()=>valid;public bool IsOwner()=>owner; }
class Container { public int items;public Container GetInventory()=>this;public int NrOfItems()=>items; }
namespace Helmsman.Structures
{
    static class StructureImports { internal static bool Allowed=true; }
    class UndoArea { internal bool safe=true,undone;internal void AssertUndoSafe(){if(!safe)throw new InvalidOperationException();}internal void Undo(){undone=true;} }
    public sealed partial class PlacementTool
    {
        internal List<int> lastPieces=new List<int>();internal UndoArea lastTerrain=new UndoArea(),lastVegetation=new UndoArea();private long worldUid=1;
        internal void UndoForTest()=>UndoNow();
    }
}
