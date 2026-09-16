using System.Collections.Generic;
using Helmsman.Core;
using Jotunn.Managers;
using UnityEngine;

namespace Helmsman;

public sealed class GullGuide : MonoBehaviour, Interactable, Hoverable
{
    private string lastSpeech="";
    private float nextSpeech;
    internal void Speak(string text,bool important=false,bool requested=false)
    {
        if(!important && (Time.time<nextSpeech || (!requested && text==lastSpeech)))return;
        if(!Player.m_localPlayer)return;
        lastSpeech=text;nextSpeech=Time.time+4;
        CallDown();GullSpeech.Say(transform,text);
    }
    internal void StayAfterArrival(Ship ship)
    {
        voyage=null;visitingShip=ship;dock=null;fetching=false;leaving=false;
        ResolvePerch();
    }
    private static readonly Dictionary<ZDOID,GullGuide> travellers=new Dictionary<ZDOID,GullGuide>();
    private ZDOID? homeDock;
    private DockMarker? dock;
    private Voyage? voyage;
    private Ship? visitingShip;
    private Ship? PerchShip=>voyage && voyage.Ship ? voyage.Ship : visitingShip;
    internal bool ReadyOn(Ship ship)=>perched && !leaving && !fetching && PerchShip==ship;
    private GameObject? landed, flying;
    private Transform? pose;
    private readonly List<BirdAnimator> animators=new List<BirdAnimator>();
    private readonly GullMoodState mood=new GullMoodState();
    private bool leaving, perched, fetching;
    private Vector3 fetchPosition;
    private float disappearAt, nextSense, greetUntil, idleOffset;
    private Vector3 flightTarget, perchLocal;
    private Transform? perchParent;
    private GullMeshRig? sittingRig;
    private ItemDrop.ItemData? cargoItem;
    private int cargoThrows;
    private float cargoStarted, nextCargoThrow;
    internal bool SortingCargo=>cargoThrows>0;
    internal void SortCargo(ItemDrop.ItemData item)
    { cargoItem=item.Clone();cargoThrows=3;cargoStarted=Time.time;nextCargoThrow=Time.time+.34f; }
    internal void EndCargoSort(bool clearProps=true)
    {
        cargoThrows=0;cargoItem=null;
        if(clearProps && QuartermasterBridge.Available)QuartermasterBridge.ClearThrows(transform);
    }
    private GullMood animatedMood;
    private GullPose blendedPose;
    private float moodStarted, flightStarted;
    private bool displayFlight;
    private Vector3 flightOrigin, flightBase, threatPosition;
    private Quaternion flightRestRotation;
    internal bool IsTravelling=>voyage || visitingShip || leaving || fetching;
    internal static GullGuide? Traveller(ZDOID id)=>travellers.TryGetValue(id,out var guide) && guide ? guide : null;
    internal void ReserveDock(ZDOID id){homeDock=id;travellers[id]=this;}

    internal static GullGuide Create(Vector3 start,DockMarker? dock,Voyage? voyage)
    {
        var root=new GameObject("Helmsman guide gull");root.transform.position=start;
        root.layer=LayerMask.NameToLayer("piece_nonsolid");
        var guide=root.AddComponent<GullGuide>();guide.dock=dock;guide.voyage=voyage;
        guide.idleOffset=Random.Range(0f,30f);
        var prefab=PrefabManager.Instance.GetPrefab("Seagal");
        var bird=prefab ? prefab.GetComponent<RandomFlyingBird>() : null;
        guide.pose=new GameObject("Perched pose").transform;guide.pose.SetParent(root.transform,false);
        if(bird)
        {
            guide.landed=Visuals.Clone(bird.m_landedModel ? bird.m_landedModel : bird.m_flyingModel,guide.pose,false);
            guide.flying=Visuals.Clone(bird.m_flyingModel,root.transform,false);
            guide.BindAnimations(guide.landed,false);
            guide.BindAnimations(guide.flying,true);
            if(guide.landed)
            {
                guide.landed.transform.localPosition=Vector3.zero;
                // Place the lowest part of the resting model at the perch, not its prefab origin.
                float foot=float.PositiveInfinity;
                foreach(var renderer in guide.landed.GetComponentsInChildren<Renderer>())
                    foot=Mathf.Min(foot,renderer.bounds.min.y-root.transform.position.y);
                if(!float.IsInfinity(foot))guide.landed.transform.localPosition=Vector3.down*foot;
            }
            if(guide.landed)guide.sittingRig=GullMeshRig.Create(guide.landed);
            if(guide.flying)
            {
                guide.flying.transform.localPosition=Vector3.zero;
                guide.flightRestRotation=guide.flying.transform.localRotation;
                var standing=guide.landed ? guide.landed.GetComponentInChildren<Renderer>() : null;
                var airborne=guide.flying.GetComponentInChildren<Renderer>();
                if(standing && airborne)guide.flightBase=Vector3.up*(standing.bounds.center.y-airborne.bounds.center.y);
                VikingGull.GullHelmet.FitFlying(guide.flying,root.transform);
            }
        }
        guide.moodStarted=Time.time-guide.idleOffset;
        guide.flightStarted=Time.time;guide.flightOrigin=start;
        guide.ResolvePerch();
        if(dock)
        {
            guide.perched=true;root.transform.SetParent(guide.perchParent,false);
            root.transform.localPosition=guide.perchLocal;root.transform.localRotation=Quaternion.identity;
        }
        var collider=root.AddComponent<SphereCollider>();collider.radius=.55f;collider.center=Vector3.up*.3f;
        guide.SetModels(!guide.perched);
        return guide;
    }

    private void ResolvePerch()
    {
        var ship=PerchShip;
        if(ship)
        {
            perchParent=ship.transform;
            var requested=Plugin.Instance.GullSternPerch.Value;
            if(ShipProfile.PrefabName(ship)!="Karve")
            {var profile=ShipProfile.For(ship);requested.z=profile.Center.z-profile.Length*.43f;}
            perchLocal=SurfacePerch(perchParent,requested,true)+Vector3.up*Plugin.Instance.GullPerchLift.Value;
        }
        else if(dock)
        {
            perchParent=dock.transform;
            perchLocal=SurfacePerch(perchParent,new Vector3(0,1.6f,0));
        }
    }

    private static Vector3 SurfacePerch(Transform owner,Vector3 fallback,bool searchStern=false)
    {
        float highest=float.NegativeInfinity;var result=fallback;
        var colliders=owner.GetComponentsInChildren<Collider>();
        // Query only this ward/ship, so nearby roof pieces or players cannot become the perch.
        // Sample the aft centreline tip to find its top instead of the lower steering surface.
        for(int sample=searchStern ? -2 : 0;sample<=(searchStern ? 2 : 0);sample++)
        {
            var ray=new Ray(owner.TransformPoint(new Vector3(fallback.x,15,fallback.z+sample*.3f)),-owner.up);
            foreach(var collider in colliders)
            {
                if(!collider.enabled || collider.isTrigger || collider.GetComponentInParent<GullGuide>() ||
                    collider.GetComponentInParent<Character>())continue;
                if(!collider.Raycast(ray,out var hit,20))continue;
                var local=owner.InverseTransformPoint(hit.point);
                if(local.y>highest){highest=local.y;result=local;}
            }
        }
        return result+Vector3.up*.015f;
    }

    internal void FetchTarget(Vector3 target)=>fetchPosition=target;
    internal void Fetch(Vector3 target)
    {
        dock=null;fetching=true;perched=false;fetchPosition=target;
        transform.SetParent(null,true);flightOrigin=transform.position;flightStarted=Time.time;SetModels(true);
    }
    internal void Visit(Ship ship)
    {
        dock=null;voyage=null;visitingShip=ship;perched=false;leaving=false;fetching=false;
        transform.SetParent(null,true);flightOrigin=transform.position;flightStarted=Time.time;
        ResolvePerch();SetModels(true);
    }
    internal void Board(Voyage trip)
    {
        // Transfer this actor: leave its source ward reference alive until this voyage gull despawns.
        bool stayPerched=ReadyOn(trip.Ship);
        dock=null;voyage=trip;visitingShip=null;perched=stayPerched;leaving=false;fetching=false;
        if(!stayPerched){transform.SetParent(null,true);flightOrigin=transform.position;flightStarted=Time.time;}
        ResolvePerch();SetModels(!stayPerched);
    }

    internal void CallDown() {greetUntil=Time.time+6;}
    internal void FlyAway()
    {
        leaving=true;perched=false;fetching=false;transform.SetParent(null,true);
        disappearAt=Time.time+4;flightTarget=transform.position+Vector3.up*18+transform.forward*25;
        GetComponent<Collider>().enabled=false;
        SetModels(true);
    }

    private void LateUpdate()
    {
        if(!leaving && !fetching && (!perchParent || (!dock && !PerchShip))) {Destroy(gameObject);return;}
        if(leaving && Time.time>=disappearAt) {Destroy(gameObject);return;}
        var target=leaving ? flightTarget : fetching ? fetchPosition : perchParent!.TransformPoint(perchLocal);
        if(!perched)
        {
            // A rising arc clears the totem on takeoff and eases into the moving stern perch.
            var flightAim=target;
            if(!leaving)
            {
                float duration=Mathf.Clamp(Vector3.Distance(flightOrigin,target)/6,1.2f,5);
                float u=Mathf.Clamp01((Time.time-flightStarted)/duration);
                float eased=u*u*(3-2*u);
                flightAim=Vector3.Lerp(flightOrigin,target,eased)+Vector3.up*(Mathf.Sin(u*Mathf.PI)*2);
            }
            var delta=flightAim-transform.position;
            if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(delta),Time.deltaTime*5);
            transform.position=Vector3.MoveTowards(transform.position,flightAim,Time.deltaTime*12);
            if(!leaving && !fetching && Vector3.Distance(transform.position,target)<.15f)
            {
                perched=true;transform.SetParent(perchParent,true);transform.localPosition=perchLocal;
                transform.localRotation=Quaternion.identity;SetModels(false);
            }
        }
        else
        {
            // Follow the ship's full pitch/roll with fixed feet instead of chasing its position in flight.
            transform.localPosition=perchLocal;transform.localRotation=Quaternion.identity;
            if(Time.time>=nextSense) {nextSense=Time.time+.5f;SenseMood();}
            AnimatePerched();
        }
    }

    private void SenseMood()
    {
        var player=Player.m_localPlayer;
        bool combat=false;
        if(player && Vector3.Distance(player.transform.position,transform.position)<40)
        {
            combat=player.InAttack();
            if(combat)threatPosition=player.transform.position+player.transform.forward*8;
            foreach(var ai in BaseAI.GetAllInstances())
            {
                if(!ai || !ai.IsAlerted() || !ai.IsEnemy(player) ||
                    Vector3.Distance(ai.transform.position,transform.position)>40)continue;
                var target=ai.GetTargetCreature();
                if(target==player || (voyage && target is Player other && voyage.Ship.IsPlayerInBoat(other)))
                {combat=true;threatPosition=ai.transform.position;break;}
                var structure=ai is MonsterAI monster ? monster.GetStaticTarget() : null;
                if(structure && ((voyage && structure.GetComponentInParent<Ship>()==voyage.Ship) ||
                    (dock && structure.GetComponentInParent<DockMarker>()==dock)))
                {combat=true;threatPosition=ai.transform.position;break;}
            }
        }
        var env=EnvMan.instance ? EnvMan.instance.GetCurrentEnvironment() : null;
        float wind=EnvMan.instance ? EnvMan.instance.GetWindIntensity() : 0;
        string weather=env?.m_name.ToLowerInvariant() ?? "";
        bool storm=weather.Contains("thunder") || weather.Contains("storm") || (env!=null && env.m_isWet && wind>.8f);
        bool fog=weather.Contains("fog") || RenderSettings.fogDensity>.025f || ParticleMist.IsInMist(transform.position);
        bool rough=wind>.65f;
        var ship=PerchShip;
        if(ship)
        {
            var body=ship.GetComponent<Rigidbody>();
            rough |= Vector3.Angle(ship.transform.up,Vector3.up)>8 || (body && Mathf.Abs(body.linearVelocity.y)>.65f);
        }
        mood.Update(Time.time,combat,storm,fog,rough);
    }

    private void AnimatePerched()
    {
        if(!pose)return;
        if(animatedMood!=mood.Current){animatedMood=mood.Current;moodStarted=Time.time;}
        float threatYaw=LocalYaw(threatPosition-transform.position);
        float windYaw=EnvMan.instance ? LocalYaw(-EnvMan.instance.GetWindDir()) : 0;
        float roll=PerchShip ? Mathf.DeltaAngle(0,PerchShip!.transform.eulerAngles.z) : 0;
        float pitch=PerchShip ? Mathf.DeltaAngle(0,PerchShip!.transform.eulerAngles.x) : 0;
        var gesture=GullPerformance.Sample(mood.Current,Time.time-moodStarted,threatYaw,windYaw,roll,pitch);
        bool toss=false, sorting=SortingCargo;
        if(sorting)
        {
            float t=Time.time-cargoStarted, cycle=t%.65f;
            float scoop=Mathf.Sin(Mathf.Clamp01(cycle/.34f)*Mathf.PI);
            gesture=new GullPose { HeadPitch=60*scoop-22*Mathf.Sin(Mathf.Clamp01((cycle-.34f)/.31f)*Mathf.PI),
                BodyPitch=25*scoop,Crouch=.07*scoop,HeadYaw=22*Mathf.Sin(t*5),TailYaw=15*Mathf.Sin(t*12) };
            if(Time.time>=nextCargoThrow) { toss=true;cargoThrows--;nextCargoThrow=Time.time+.65f; }
        }
        if(!sorting && Time.time<greetUntil && Player.m_localPlayer && mood.Current!=GullMood.Combat)
            gesture.HeadYaw=Mathf.Clamp(LocalYaw(Player.m_localPlayer.transform.position-transform.position),-65,65);
        blendedPose=GullPerformance.Blend(blendedPose,gesture,1-Mathf.Exp(-Time.deltaTime*12));
        gesture=blendedPose;
        // Ease the body into a different orientation; head/feather motion is separately articulated.
        var targetRotation=Quaternion.Euler(sittingRig==null ? (float)gesture.BodyPitch : 0,(float)gesture.BodyYaw,
            sittingRig==null ? (float)gesture.BodyRoll : 0);
        pose.localRotation=Quaternion.Slerp(pose.localRotation,targetRotation,1-Mathf.Exp(-Time.deltaTime*8));
        pose.localScale=Vector3.one;pose.localPosition=Vector3.up*(float)gesture.Hop;
        sittingRig?.Apply(gesture);
        if(toss && cargoItem!=null && QuartermasterBridge.Available)
            QuartermasterBridge.Throw(transform,cargoItem,sittingRig!=null ? sittingRig.BeakWorld : transform.position+transform.up*.7f);
        bool hop=gesture.Hop>.035;
        if(displayFlight!=hop)SetModels(hop);
        if(hop && flying)
        {
            flying.transform.localPosition=flightBase+Vector3.up*(float)gesture.Hop;
            flying.transform.localRotation=Quaternion.Euler(-8,(float)gesture.BodyYaw,0)*flightRestRotation;
        }
    }
    private float LocalYaw(Vector3 direction)
    {
        var local=transform.InverseTransformDirection(direction);
        return Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
    }

    private void BindAnimations(GameObject? model,bool flight)
    {
        if(!model)return;
        foreach(var animator in model.GetComponentsInChildren<Animator>(true))
        {
            if(!animator.runtimeAnimatorController)continue;
            var binding=new BirdAnimator(animator,flight);animators.Add(binding);
            binding.SetFlight(flight);animator.Update(0);
        }
    }
    private void SetModels(bool flight)
    {
        displayFlight=flight;
        if(flying){flying.transform.localPosition=flightBase;flying.transform.localRotation=flightRestRotation;}
        if(landed)landed.SetActive(!flight);
        if(flying)flying.SetActive(flight);
        foreach(var animator in animators)animator.SetFlight(animator.Flying);
    }
    private sealed class BirdAnimator
    {
        private readonly Animator animator;
        private readonly bool hasFlying,hasFlapping;
        internal readonly bool Flying;
        internal BirdAnimator(Animator animator,bool flying)
        {
            this.animator=animator;Flying=flying;animator.applyRootMotion=false;
            foreach(var parameter in animator.parameters)
            {
                if(parameter.name=="flying" && parameter.type==AnimatorControllerParameterType.Bool)hasFlying=true;
                if(parameter.name=="flapping" && parameter.type==AnimatorControllerParameterType.Bool)hasFlapping=true;
            }
        }
        internal void SetFlight(bool value)
        {
            if(hasFlying)animator.SetBool("flying",value);
            if(hasFlapping)animator.SetBool("flapping",value);
        }
    }
    private void OnDestroy()
    {
        EndCargoSort();
        if(homeDock.HasValue && travellers.TryGetValue(homeDock.Value,out var guide) && guide==this)
            travellers.Remove(homeDock.Value);
        sittingRig?.Destroy();
    }
    public string GetHoverName()=>"Helmsman gull";
    public float GetHoverOffset()=>.25f;
    public string GetHoverText()=>Localization.instance.Localize(GetHoverName()+(leaving ? "\nFlying away" : !perched ? "\nLanding…" : "\n[<color=yellow><b>$KEY_Use</b></color>] Choose destination"));
    public bool Interact(Humanoid user,bool hold,bool alt)
    {
        if(hold || leaving || user!=Player.m_localPlayer)return false;
        if(!perched){Plugin.Message("Wait for the gull to land.");return false;}
        CallDown();
        if(voyage)Plugin.Instance.UI.OpenVoyage(this);
        else if(visitingShip && Plugin.Instance.CalledGull && Plugin.Instance.CalledGull.Ship==visitingShip)
            Plugin.Instance.UI.OpenCalledGull(Plugin.Instance.CalledGull,this);
        else if(dock)Plugin.Instance.UI.OpenDestinations(dock);
        return true;
    }
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
}
