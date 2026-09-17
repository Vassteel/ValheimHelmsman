using System.Collections.Generic;
using UnityEngine;

namespace Helmsman;

// Only ignore actual fish/boat collider pairs. Do not change physics layers,
// fish AI, pickup rules, or collisions with another boat.
public sealed class FishCollisionPass : MonoBehaviour
{
    private Ship ship=null!;
    private float nextScan;
    private readonly HashSet<(Collider Boat,Collider Fish)> changed=new();
    internal static void UpdateFor(Ship ship,bool active)
    {
        if(!ship)return;
        var pass=ship.GetComponent<FishCollisionPass>();
        if(!active){if(pass){pass.Restore();Destroy(pass);}return;}
        if(!pass){pass=ship.gameObject.AddComponent<FishCollisionPass>();pass.ship=ship;}
    }
    private void Update()
    {
        var voyage=Plugin.Instance.Voyage;
        if(!ship||!Plugin.Instance.FishPassThrough.Value||!voyage||voyage.Ship!=ship||!voyage.CanControl||ship.m_shipControlls.HaveValidUser())
        {Restore();Destroy(this);return;}
        if(Time.time<nextScan)return;nextScan=Time.time+.1f;
        Refresh();
    }
    internal void Refresh()
    {
        if(!ship.m_floatCollider)return;
        var bounds=ship.m_floatCollider.bounds;bounds.Expand(6);
        var fish=new HashSet<Fish>();
        foreach(var collider in Physics.OverlapBox(bounds.center,bounds.extents,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
        {var found=collider.GetComponentInParent<Fish>();if(found)fish.Add(found);}
        var wanted=new HashSet<(Collider Boat,Collider Fish)>();
        foreach(var boat in ship.GetComponentsInChildren<Collider>())
        {
            if(!boat.enabled||boat.isTrigger||boat.GetComponentInParent<Ship>()!=ship)continue;
            foreach(var animal in fish)foreach(var other in animal.GetComponentsInChildren<Collider>())
            {
                if(!other.enabled||other.isTrigger)continue;
                var pair=(boat,other);wanted.Add(pair);
                if(!changed.Contains(pair)&&!Physics.GetIgnoreCollision(boat,other))
                {Physics.IgnoreCollision(boat,other,true);changed.Add(pair);}
            }
        }
        foreach(var pair in new List<(Collider Boat,Collider Fish)>(changed))
            if(!wanted.Contains(pair))
            {if(pair.Boat&&pair.Fish)Physics.IgnoreCollision(pair.Boat,pair.Fish,false);changed.Remove(pair);}
    }
    internal void Restore()
    {
        foreach(var pair in changed)if(pair.Boat&&pair.Fish)Physics.IgnoreCollision(pair.Boat,pair.Fish,false);
        changed.Clear();
    }
    private void OnDisable()=>Restore();
    private void OnDestroy()=>Restore();
}
