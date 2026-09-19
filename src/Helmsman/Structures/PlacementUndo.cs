#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Helmsman.Structures
{
    public sealed partial class PlacementTool
    {
        private void UndoNow()
        {
            if(!StructureImports.Allowed||!ZNet.instance||!ZNetScene.instance||ZDOMan.instance==null||ZNet.instance.GetWorldUID()!=worldUid)throw new InvalidOperationException("Return to the original world as an administrator.");
            lastTerrain?.AssertUndoSafe();lastVegetation?.AssertUndoSafe();
            var objects=new List<GameObject>();
            foreach(var id in lastPieces)
            {
                var go=ZNetScene.instance.FindInstance(id);
                if(!go)
                {
                    // Unloading removes the scene object but keeps its ZDO. A destroyed
                    // piece has no ZDO left, so it is already undone and can be skipped.
                    if(ZDOMan.instance.GetZDO(id)==null)continue;
                    throw new InvalidOperationException("Some placed pieces are unloaded; move back to the build.");
                }
                var view=go.GetComponent<ZNetView>();
                if(!view||!view.IsValid()||!view.IsOwner())throw new InvalidOperationException("Waiting for piece ownership; try Undo again.");
                foreach(var container in go.GetComponentsInChildren<Container>())
                    if(container.GetInventory()!=null&&container.GetInventory().NrOfItems()>0)throw new InvalidOperationException("Empty the imported structure’s containers before undoing it.");
                objects.Add(go);
            }
            lastTerrain?.Undo();lastVegetation?.Undo();
            foreach(var go in objects)ZNetScene.instance.Destroy(go);
            lastPieces.Clear();lastTerrain=null;lastVegetation=null;
        }
    }
}
