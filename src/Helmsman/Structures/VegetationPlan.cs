#nullable disable
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Helmsman.Structures
{
    internal sealed class VegetationPlan
    {
        private sealed class Entry
        {
            internal ZNetView View; internal Vector3 Position; internal int Prefab;
            internal byte[] Data; internal bool Removed;
        }
        private readonly List<Entry> entries=new List<Entry>();
        internal VegetationPlan(Blueprint blueprint,Vector3 origin,Quaternion rotation)
        {
            var inverse=Quaternion.Inverse(rotation);
            foreach(var view in UnityEngine.Object.FindObjectsByType<ZNetView>(FindObjectsSortMode.None))
            {
                if(!view.IsValid()||view.GetComponent<Piece>()||view.GetComponent<Plant>()||view.GetComponent<Character>())continue;
                string name=Utils.GetPrefabName(view.gameObject);
                if(!view.GetComponent<TreeBase>()&&name.IndexOf("bush",StringComparison.OrdinalIgnoreCase)<0&&name.IndexOf("shrub",StringComparison.OrdinalIgnoreCase)<0)continue;
                var local=inverse*(view.transform.position-origin);
                if(Geometry.Distance(new Point(local.x,local.z),blueprint.Hull)>0)continue;
                if(!PrivateArea.CheckAccess(view.transform.position,0,false,true))throw new InvalidOperationException("Vegetation overlaps a protected ward.");
                entries.Add(new Entry{View=view});
            }
        }
        internal void RequestOwnership(){foreach(var e in entries)e.View.ClaimOwnership();}
        internal bool Owned {get{foreach(var e in entries)if(!e.View||!e.View.IsValid()||!e.View.IsOwner())return false;return true;}}
        internal void Apply()
        {
            if(!Owned)throw new InvalidOperationException("Waiting for vegetation ownership.");
            // Snapshot everything before removing anything. Do not damage plants or spawn loot.
            foreach(var e in entries)
            {var data=e.View.GetZDO();e.Position=data.GetPosition();e.Prefab=data.GetPrefab();var package=new ZPackage();data.Serialize(package);e.Data=package.GetArray();}
            foreach(var e in entries){ZNetScene.instance.Destroy(e.View.gameObject);e.Removed=true;}
        }
        internal void AssertUndoSafe()
        {
            foreach(var e in entries)if(e.Removed&&(!Heightmap.FindHeightmap(e.Position)||!PrivateArea.CheckAccess(e.Position,0,false,true)))
                throw new InvalidOperationException("Return to the cleared vegetation before undoing.");
        }
        internal void Undo()
        {
            AssertUndoSafe();
            foreach(var e in entries)
            {
                if(!e.Removed)continue;
                var restored=ZDOMan.instance.CreateNewZDO(e.Position,e.Prefab);
                restored.Deserialize(new ZPackage(e.Data));
                e.Removed=false;
            }
        }
    }
}
