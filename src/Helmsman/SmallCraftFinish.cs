using UnityEngine;

namespace Helmsman;

internal static class SmallCraftFinish
{
    internal static bool Applies(string name)=>name is "HelmsmanDugout" or "HelmsmanFinewoodKayak" or "HelmsmanTandemKayak" or "HelmsmanCurrach" or "LittleBoat" or "HerculeShip";

    internal static void Map(string group,string label,Vector3[] vertices,Vector2[] uv)
    {
        string n=label.ToLowerInvariant();
        bool shell=group=="hull"&&(n.Contains("pine")||n.Contains("oak")||n.Contains("tarred skin"));
        bool deck=group=="fixed"&&(n.StartsWith("worn deck pine")||n.Contains("finewood dark inlay"));
        if(!shell&&!deck)return;
        // One continuous boat-local projection: per-face dominant-axis mapping
        // changes direction around curved strakes and makes narrow bright bands.
        // Preserve authored mapping on cross benches, fittings, rope and sails.
        for(int i=0;i<vertices.Length;i++)
        {
            var p=vertices[i];
            uv[i]=new Vector2(p.z/1.8f,(deck?p.x:p.y+Mathf.Abs(p.x)*.18f)/.75f);
        }
    }
}
