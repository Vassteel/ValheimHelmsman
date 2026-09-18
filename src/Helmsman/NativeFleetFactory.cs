using System;
using System.IO;
using Helmsman.Core;
using Jotunn.Managers;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Helmsman;

// Only vanilla game prefabs provide physics, networking and effects. Imported ship
// hierarchies, meshes, textures and animation controllers are no longer distributed.
internal static class NativeFleetFactory
{
    internal static GameObject Create(ShipBlueprint blueprint)
    {
        string model=blueprint.Source;
        bool large=model is "MercantShip" or "BigCargoShip" or "WarShip";
        var native=PrefabManager.Instance.GetPrefab(large?"VikingShip":"Karve");
        if(!native)throw new InvalidDataException("Native ship template unavailable");
        var prefab=PrefabManager.Instance.CreateClonedPrefab(blueprint.Prefab,native);
        // Deactivate native visuals, including their current cloth solvers. Their
        // native materials/effects are obtained independently from the running game.
        var visual=prefab.transform.Find("ship/visual");if(visual)visual.gameObject.SetActive(false);
        var holds=prefab.GetComponentsInChildren<Container>(true);
        if(holds.Length!=1)throw new InvalidDataException("Unexpected native hold topology");
        var source=holds[0];
        int count=model=="BigCargoShip"?2:1;
        int width=model switch {"MercantShip"=>7,"BigCargoShip"=>8,"WarShip" or "HerculeShip"=>6,"HelmsmanCurrach"=>3,_=>2};
        int height=model switch {"BigCargoShip" or "WarShip"=>4,"MercantShip"=>3,"HelmsmanDugout"=>1,_=>2};
        for(int i=0;i<count;i++)
        {
            var hold=i==0?source:Object.Instantiate(source.gameObject,prefab.transform).GetComponent<Container>();
            hold.name="Cargo slot "+i;hold.m_name="Storage";hold.m_width=width;hold.m_height=height;
            foreach(var childView in hold.GetComponentsInChildren<ZNetView>(true))Object.DestroyImmediate(childView);
            hold.m_rootObjectOverride=prefab.GetComponent<ZNetView>();
        }
        var ship=prefab.GetComponent<Ship>();
        if(FinalFleetModels.PaddleCraft(model))
        {
            bool log=model=="HelmsmanDugout";
            ship.m_backwardForce=log?.075f:.16f;
            ship.m_dampingForward=log?.018f:.009f;
            ship.m_dampingSideway=.24f;ship.m_angularDamping=.28f;
            ship.m_stearForce=log?.12f:.17f;ship.m_stearVelForceFactor=.12f;
            ship.m_hasSail=false;ship.m_sailForceFactor=0;
        }
        return prefab;
    }
}
