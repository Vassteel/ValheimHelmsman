using Jotunn.Managers;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Helmsman;

internal static class NativeFishingDock
{
    internal static GameObject Create()
    {
        var dock=PrefabManager.Instance.CreateClonedPrefab("FishingDock","wood_floor");
        WorkshopModels.Apply(dock,"fishing-dock");
        var wear=dock.GetComponent<WearNTear>();
        wear.m_health=1000;wear.m_noSupportWear=true;wear.m_noRoofWear=false;
        var storage=Object.Instantiate(PrefabManager.Instance.GetPrefab("piece_chest_wood"),dock.transform);
        storage.name="Catch storage";storage.transform.localPosition=new Vector3(-.35f,0,0);storage.transform.localRotation=Quaternion.identity;
        // One workstation ZDO owns the vanilla container. Authored geometry supplies its appearance.
        foreach(var behaviour in storage.GetComponentsInChildren<MonoBehaviour>(true))
            if(!(behaviour is Container))Object.DestroyImmediate(behaviour);
        foreach(var lod in storage.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(lod);
        foreach(var collider in storage.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
        foreach(var renderer in storage.GetComponentsInChildren<Renderer>(true)){renderer.enabled=false;renderer.forceRenderingOff=true;}
        foreach(var t in storage.GetComponentsInChildren<Transform>(true))if(t.CompareTag("snappoint"))Object.DestroyImmediate(t.gameObject);
        var chest=storage.GetComponent<Container>();chest.m_rootObjectOverride=dock.GetComponent<ZNetView>();chest.m_checkGuardStone=true;
        // Slightly encloses the authored chest, so its own interactable wins the raycast.
        var target=storage.AddComponent<BoxCollider>();target.center=new Vector3(0,.325f,0);target.size=new Vector3(1.04f,.61f,.73f);
        var anchor=new GameObject("Pelican perch");anchor.transform.SetParent(dock.transform,false);anchor.transform.localPosition=new Vector3(.99f,.97f,-.03f);
        var fishing=dock.AddComponent<FishingDock>();fishing.Chest=chest;fishing.WorkerAnchor=anchor.transform;
        return dock;
    }
}
