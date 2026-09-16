using UnityEngine;
using UnityEngine.UI;

namespace Helmsman;

// Use the native hotbar's canvas and slot bounds, so UI scaling and repositioning
// move the voyage status with the game HUD instead of over the item icons.
public sealed class VoyageHudAnchor : MonoBehaviour
{
    internal HotkeyBar Bar=null!;
    private readonly Vector3[] corners=new Vector3[4];

    private void LateUpdate()
    {
        if(!Bar)return;
        RectTransform? first=null;
        float left=float.PositiveInfinity, bottom=float.PositiveInfinity;
        foreach(Transform child in Bar.transform)
        {
            if(!child.gameObject.activeSelf || !(child is RectTransform slot) || !child.GetComponent<Button>())continue;
            slot.GetWorldCorners(corners);
            var lower=Bar.transform.InverseTransformPoint(corners[0]);
            bottom=Mathf.Min(bottom,lower.y);
            if(lower.x<left){left=lower.x;first=slot;}
        }
        if(first)
            transform.localPosition=new Vector3(left,bottom-12,0);
    }
}
