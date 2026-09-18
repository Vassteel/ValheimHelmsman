using HarmonyLib;
using UnityEngine;

namespace Helmsman;

// The imported fleet predates the game's MagicaCloth sail rig. Preserve its
// original scale-based furling without entering Ship.UpdateSailSize, whose new
// cloth/anchor references are absent from these assets. Native ship physics and
// synchronized speed settings still run normally.
public sealed class ImportedSailAnimation : MonoBehaviour
{
    public bool FixedMast,MeshFurl,ConfiguredScale;
    public Vector3 RestScale;
    public float FurlAmount=1;
    private Transform? sail;
    private Vector3 fullScale;
    internal void Tick(Ship ship,float delta)
    {
        if(!ship.m_hasSail)return;
        delta=Mathf.Max(0,delta);
        var speed=ship.GetSpeedSetting();
        if(ship.m_sailObject)
        {
            if(sail!=ship.m_sailObject.transform)
            {sail=ship.m_sailObject.transform;fullScale=ConfiguredScale?RestScale:sail.localScale;}
            float amount=speed==Ship.Speed.Full?1:speed==Ship.Speed.Half?.5f:.1f;
            FurlAmount=Mathf.MoveTowards(FurlAmount,amount,delta);
            if(!MeshFurl)
            {
            var scale=fullScale;
            scale.y=Mathf.MoveTowards(sail.localScale.y,fullScale.y*amount,Mathf.Abs(fullScale.y)*delta);
            sail.localScale=scale;
            }
        }
        if(FixedMast || !ship.m_mastObject || !EnvMan.instance)return;
        var root=ship.transform;
        var wind=EnvMan.instance.GetWindDir();wind=Vector3.Cross(Vector3.Cross(wind,root.up),root.up);
        if(wind.sqrMagnitude<.0001f)return;
        var mast=ship.m_mastObject.transform;
        if(speed==Ship.Speed.Full || speed==Ship.Speed.Half)
        {
            float t=.5f+Vector3.Dot(root.forward,wind)*.5f;
            var facing=-Vector3.Lerp(wind,Vector3.Normalize(wind-root.forward),t);
            if(facing.sqrMagnitude>.0001f)
                mast.rotation=Quaternion.RotateTowards(mast.rotation,Quaternion.LookRotation(facing,root.up),30*delta);
        }
        else if(speed==Ship.Speed.Back)
        {
            var centered=Quaternion.LookRotation(-root.forward,root.up);
            var target=Quaternion.RotateTowards(centered,Quaternion.LookRotation(-wind,root.up),80);
            mast.rotation=Quaternion.RotateTowards(mast.rotation,target,30*delta);
        }
    }
}

[HarmonyPatch(typeof(Ship),"UpdateSail")]
internal static class ImportedSailCompatibility
{
    internal static bool Prefix(Ship __instance,float dt)
    {
        var legacy=__instance.GetComponent<ImportedSailAnimation>();
        if(!legacy)return true;
        legacy.Tick(__instance,dt);
        return false;
    }
}
