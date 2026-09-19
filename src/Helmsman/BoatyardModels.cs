using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Jotunn.Managers;
using Object=UnityEngine.Object;

namespace Helmsman;

// Icons for original models; the retired imported-hierarchy loader is removed.
internal static class BoatyardModels
{
    private static readonly List<Sprite> icons=new();
    internal static void RefreshIcon(GameObject prefab)
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
        try
        {
            var icon=RenderManager.Instance.Render(new RenderManager.RenderRequest(prefab){Rotation=RenderManager.IsometricRotation,Width=128,Height=128,UseCache=true,TargetPlugin=Plugin.Instance.Info.Metadata,ParticleSimulationTime=-1});
            if(!icon)return;
            icons.Add(icon);
            var item=prefab.GetComponent<ItemDrop>();if(item)item.m_itemData.m_shared.m_icons=new[]{icon};
            var piece=prefab.GetComponent<Piece>();if(piece)piece.m_icon=icon;
        }
        catch(Exception ex){Plugin.Instance.Record("Boatyard icon unavailable for "+prefab.name+": "+ex.Message);}
    }
    internal static void Release()
    {
        foreach(var icon in icons)if(icon){if(icon.texture)Object.Destroy(icon.texture);Object.Destroy(icon);}icons.Clear();
    }
}
