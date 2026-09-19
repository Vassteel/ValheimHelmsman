using System;
using System.Linq;
using Helmsman.Core;
using UnityEngine;
namespace Helmsman;
// Cosmetic machinery sampled from network time. No collider or ship ownership changes.
internal sealed class SlipwayPresentation:IDisposable
{
    private readonly Transform cradle,capstan;
    private readonly LineRenderer line;
    private readonly Material rope;
    private readonly ShipyardAudio audio;
    private readonly Transform root;
    private float nextSound;
    internal SlipwayPresentation(Transform parent)
    {
        root=parent;audio=new ShipyardAudio(parent);
        cradle=Pivot("Animated launch cradle",Vector3.zero);
        capstan=Pivot("Animated capstan",new Vector3(0,3.175f,11));
        foreach(var f in parent.GetComponentsInChildren<MeshFilter>(true).ToArray())
        {
            Transform? pivot=f.name.Contains("slipway cradle ")?cradle:f.name.Contains("slipway capstan ")?capstan:null;
            if(pivot){f.transform.SetParent(pivot,true);}
            if(f.name.Contains("slipway haul "))f.GetComponent<Renderer>().enabled=false;
        }
        var go=new GameObject("Working haul rope");go.transform.SetParent(parent,false);
        line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=4;line.startWidth=line.endWidth=.035f;line.numCornerVertices=2;
        var surfaces=parent.GetComponentsInChildren<MeshRenderer>().Select(r=>r.sharedMaterial).Where(m=>m).ToArray();
        var source=surfaces.FirstOrDefault(m=>m.name.IndexOf("rope",StringComparison.OrdinalIgnoreCase)>=0)??surfaces[0];
        rope=new Material(source){name="Slipway running rope"};rope.color=new Color(.38f,.28f,.16f);line.sharedMaterial=rope;
    }
    private Transform Pivot(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root,false);t.localPosition=p;return t;}
    internal void Tick(SlipwayOrder? state,long reset,long now)
    {
        double launch=state==null?0:SlipwayMotion.LaunchElapsed(now,state.launchStarted,state.launchPaused,state.launchPauseTicks);
        float travel=state!=null&&state.launchStarted>0?SlipwayMotion.Cradle(launch,Mathf.Abs(root.InverseTransformPoint(state.order.position).z-root.InverseTransformPoint(state.stage).z)):reset>0?11*SlipwayMotion.Reset((now-reset)/(double)TimeSpan.TicksPerSecond):0;
        cradle.localPosition=new Vector3(0,-travel*.1f,-travel);
        capstan.localRotation=Quaternion.Euler(0,-travel*160,0);
        line.SetPosition(0,new Vector3(.215f,3.175f,11));line.SetPosition(1,new Vector3(.28f,2.78f,10.4f));
        var end=new Vector3(0,2.665f,8.2f)+cradle.localPosition;
        line.SetPosition(2,Vector3.Lerp(new Vector3(0,2.78f,10.4f),end,.5f)+Vector3.down*.08f);line.SetPosition(3,end);
        bool launching=state!=null&&state.launchStarted>0&&state.launchPaused==0&&launch>1.4&&launch<9.5;
        double resetSeconds=reset>0?(now-reset)/(double)TimeSpan.TicksPerSecond:99;
        bool resetting=state==null&&resetSeconds>=0&&resetSeconds<SlipwayMotion.ResetSeconds;
        float speed=launching?(SlipwayMotion.Travel(launch)-SlipwayMotion.Travel(launch-.05))*20:resetting?(SlipwayMotion.Reset(resetSeconds-.05)-SlipwayMotion.Reset(resetSeconds))*20:0;
        float strength=(launching||resetting)?Mathf.Clamp01(.3f+speed*3):0;
        audio.Sliding(strength*.8f);
        if(strength>0&&Time.time>nextSound)
        {
            audio.Play(YardSound.Creak,UnityEngine.Random.Range(.65f,1f));
            if(resetting)audio.Play(YardSound.Rope,.35f);
            nextSound=Time.time+UnityEngine.Random.Range(.65f,1.4f);
        }
        // Water-entry sound belongs to the native ship/water interaction.

    }
    public void Dispose(){audio.Dispose();if(line)UnityEngine.Object.Destroy(line.gameObject);if(rope)UnityEngine.Object.Destroy(rope);}
}
