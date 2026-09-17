using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Helmsman;

internal sealed class ShipBuildGhost : IDisposable
{
    private GameObject? visual;
    private Material? material;
    private string blueprint="";
    internal void Update(ConstructionOrder? order,float progress)
    {
        if(order==null){Dispose();return;}
        if(!visual || blueprint!=order.blueprint)
        {
            Dispose();
            var plan=Helmsman.Core.ShipConstruction.Find(order.blueprint);
            var prefab=plan!=null?ShipDirectory.FindPrefab(plan.Prefab):null;
            if(!prefab)return;
            try
            {
                visual=ShipMenuPreviews.CreateVisual(prefab,"Ship under construction");blueprint=order.blueprint;
                material=Visuals.PreviewMaterial(new Color(.46f,.78f,.85f,.16f));
                // World preview: respect walls/terrain and never cast solid shadows.
                if(material.HasProperty("_ZTest"))material.SetInt("_ZTest",(int)CompareFunction.LessEqual);
                if(material.HasProperty("_Cull"))material.SetInt("_Cull",(int)CullMode.Back);
                material.renderQueue=3000;
                foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=material;
                    renderer.sharedMaterials=slots;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                }
                visual.SetActive(true);
            }
            catch{Dispose();throw;}
        }
        visual.transform.position=WaterChart.AtSea(order.position);
        visual.transform.rotation=Quaternion.Euler(0,order.heading,0);
        // Gently strengthen the blueprint as work progresses. It stays a ghost
        // while waiting for launch clearance, then disappears with the saved order.
        material!.color=new Color(.46f,.78f,.85f,.12f+.10f*Mathf.Clamp01(progress));
    }
    public void Dispose()
    {if(visual)Object.Destroy(visual);if(material)Object.Destroy(material);visual=null;material=null;blueprint="";}
}
