using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

internal enum VoyagePhase { Boarding, Planning, Departing, Reversing, Turning, Cruising, Approaching, Docking, Paused, Finished }

public sealed class Voyage : MonoBehaviour
{
    private static readonly AccessTools.FieldRef<Ship,Ship.Speed> SpeedField=AccessTools.FieldRefAccess<Ship,Ship.Speed>("m_speed");
    private static readonly AccessTools.FieldRef<Ship,float> RudderField=AccessTools.FieldRefAccess<Ship,float>("m_rudderValue");
    internal Ship Ship=null!;
    internal bool Unattended {get;private set;}
    internal string Status {get;private set;}="Preparing voyage";
    internal string DestinationName=>destination.Berth.name;
    internal bool CanControl=>Ship && Ship.IsOwner() && Plugin.Solo && passenger && !passenger.IsDead() &&
        (Unattended || Ship.IsPlayerInBoat(passenger)) && phase!=VoyagePhase.Finished;
    private Player passenger=null!;
    private DockRecord destination=null!;
    private Berth? source;
    private Berth? departure;
    private static bool Relaxed => Plugin.Instance.RelaxedDockChecks.Value;
    private bool DockManeuver => phase==VoyagePhase.Departing || phase==VoyagePhase.Reversing ||
        (!destination.Temporary && (phase==VoyagePhase.Approaching || phase==VoyagePhase.Docking));
    private ZDOID sourceId;
    private BoardingGate? gate;
    private VoyagePhase phase;
    private WaterChart chart=null!;
    private readonly List<Vector3> route=new List<Vector3>();
    private int waypoint;
    private bool rerouting, slowingForTurn;
    private float nextDecision, modeSince, nextDestinationCheck, progressTime;
    private Vector3 progressPosition;
    private Ship.Speed command=Ship.Speed.Stop;
    private float rudder;
    private GullGuide? gull;
    private RouteWisps? routeEffect;
    private Coroutine? planning;
    private int automaticReplans;
    private float waitingForWorldSince=-1;
    private bool worldWaitAnnounced;
    private const float Deceleration=.25f; // Conservative starting estimate, awaiting measured calibration.

    internal static bool Begin(Ship ship,DockRecord from,DockRecord to,out string reason,GullGuide? aboardGuide=null)
    {
        if(!Plugin.Solo) {reason="Helmsman voyages are disabled in multiplayer.";return false;}
        if(Plugin.Instance.Voyage || Plugin.Instance.Summon || (Plugin.Instance.CalledGull && !aboardGuide)) {reason="Cancel the current voyage, summon or gull visit first.";return false;}
        if(GullGuide.Traveller(from.Id) && GullGuide.Traveller(from.Id)!=aboardGuide) {reason="The dock gull is finishing its previous trip; try again in a moment.";return false;}
        if(!ShipProfile.Supports(ship) || !ship.IsOwner()) {reason="Select a locally owned supported ship.";return false;}
        if(ship.m_shipControlls.HaveValidUser()) {reason="Release the helm before choosing a voyage.";return false;}
        var initialVelocity=ship.GetComponent<Rigidbody>().linearVelocity;
        if(new Vector2(initialVelocity.x,initialVelocity.z).magnitude>.5f) {reason="Slow the ship before starting a voyage.";return false;}
        if(Vector3.Distance(WaterChart.AtSea(ship.transform.position),WaterChart.AtSea(from.Berth.position))> (Relaxed ? 20 : 4) ||
            (!Relaxed && Mathf.Abs(Mathf.DeltaAngle(ship.transform.eulerAngles.y,from.Berth.heading))>15))
        {reason=Relaxed ? "Bring the ship within 20 m of the source berth." : "Park the ship in the source berth, facing its arrival arrow.";return false;}
        var chart=new WaterChart(ship);
        if(!from.Berth.ValidData || !to.Berth.ValidData) {reason="Configure both docks first.";return false;}
        if(!Relaxed && !chart.ValidateBerth(from.Berth,out reason)) return false;
        var voyage=ship.gameObject.AddComponent<Voyage>();
        voyage.Ship=ship; voyage.passenger=Player.m_localPlayer;voyage.destination=to;
        voyage.source=from.Berth.Copy();voyage.sourceId=from.Id;voyage.chart=chart;voyage.phase=VoyagePhase.Boarding;
        voyage.gate=new BoardingGate(Time.time,Plugin.Instance.BoardingSeconds.Value);
        Plugin.Instance.Voyage=voyage;
        DockMarker? sourceMarker=null;
        foreach(var marker in FindObjectsByType<DockMarker>(FindObjectsSortMode.None))
            if(marker.Ready && marker.Id==from.Id){sourceMarker=marker;break;}
        if(aboardGuide){voyage.gull=aboardGuide;aboardGuide.Board(voyage);}
        else
        {
            voyage.gull=sourceMarker ? sourceMarker.BoardGuide(voyage) : GullGuide.Create(from.MarkerPosition+Vector3.up*2,null,voyage);
            voyage.gull.ReserveDock(from.Id);
        }
        voyage.routeEffect=RouteWisps.Create(voyage.transform);
        Plugin.Instance.Record("Voyage selected: "+from.Berth.name+" -> "+to.Berth.name+"; reverse departure="+from.Berth.reverseDeparture);
        reason="Boarding countdown started.";return true;
    }

    internal static bool BeginAboard(Ship ship,DockRecord to,GullGuide guide,out string reason)
    {
        if(!Plugin.Solo || Plugin.Instance.Voyage || Plugin.Instance.Summon || !ShipProfile.Supports(ship) || !ship.IsOwner())
        {reason="Board a locally owned supported ship with no other voyage active.";return false;}
        if(!ship.IsPlayerInBoat(Player.m_localPlayer) || Player.m_localPlayer.IsDead() || !guide.ReadyOn(ship))
        {reason="Stay aboard and wait for the gull to land.";return false;}
        if(ship.m_shipControlls.HaveValidUser()){reason="Release the helm before choosing a dock.";return false;}
        var velocity=ship.GetComponent<Rigidbody>().linearVelocity;
        if(new Vector2(velocity.x,velocity.z).magnitude>.5f){reason="Slow the ship before starting a voyage.";return false;}
        var fresh=DockDirectory.Resolve(to.Id);
        if(fresh==null){reason="That destination is no longer available.";return false;}
        if(Vector3.Distance(WaterChart.AtSea(ship.transform.position),WaterChart.AtSea(fresh.Berth.position))<6)
        {reason="You are already at that dock.";return false;}
        var from=Plugin.Instance.Directory.Records
            .Select(d=>DockDirectory.Resolve(d.Id)).Where(d=>d!=null &&
                Vector3.Distance(WaterChart.AtSea(ship.transform.position),WaterChart.AtSea(d.Berth.position))<20)
            .OrderBy(d=>Vector3.Distance(ship.transform.position,d!.Berth.position)).FirstOrDefault();
        if(from!=null)return Begin(ship,from,fresh,out reason,guide);
        // Already at sea: plan from the actual ship pose; no artificial source ward or dock exit.
        var trip=ship.gameObject.AddComponent<Voyage>();trip.Ship=ship;trip.passenger=Player.m_localPlayer;
        trip.destination=fresh;trip.chart=new WaterChart(ship);trip.phase=VoyagePhase.Planning;trip.rerouting=true;
        trip.gull=guide;guide.Board(trip);trip.routeEffect=RouteWisps.Create(trip.transform);
        Plugin.Instance.Voyage=trip;trip.planning=trip.StartCoroutine(trip.Plan(WaterChart.AtSea(ship.transform.position)));
        Plugin.Instance.Record("Onboard voyage selected: "+ShipDirectory.Display(ship)+" -> "+fresh.Berth.name);
        reason="The gull is plotting the course.";return true;
    }

    internal static bool BeginSummoned(Ship ship,DockRecord to,GullGuide guide,out string reason)
    {
        if(!Plugin.Solo || Plugin.Instance.Voyage || !UnattendedShipPhysics.Installed || !ShipProfile.Supports(ship) || !ship.IsOwner())
        {reason="This ship cannot start an unattended voyage.";return false;}
        if(ship.HasPlayerOnboard() || ship.m_shipControlls.HaveValidUser()){reason="The ship is occupied.";return false;}
        // The shore was checked when called. It may now be unloaded because the
        // caller moved away; recheck it when the travelling ship reaches the approach.
        var trip=ship.gameObject.AddComponent<Voyage>();trip.Ship=ship;trip.passenger=Player.m_localPlayer;
        trip.Unattended=true;trip.destination=to;trip.sourceId=to.Id;trip.chart=new WaterChart(ship);
        trip.departure=new Berth {name="Summoned ship",position=WaterChart.AtSea(ship.transform.position),heading=ship.transform.eulerAngles.y,configured=true};
        bool docked=false;
        foreach(var dock in Plugin.Instance.Directory.Records)
            if(Vector3.Distance(WaterChart.AtSea(dock.Berth.position),trip.departure.position)<20)
            {docked=true;trip.departure.reverseDeparture=dock.Berth.reverseDeparture;trip.departure.reverseDistance=dock.Berth.reverseDistance;break;}
        trip.phase=VoyagePhase.Planning;trip.gull=guide;guide.Board(trip);
        trip.routeEffect=RouteWisps.Create(trip.transform);
        var start=docked ? trip.departure.Exit : trip.departure.position;
        if(!docked){trip.departure=null;trip.rerouting=true;}
        Plugin.Instance.Voyage=trip;trip.planning=trip.StartCoroutine(trip.Plan(start));
        reason="Ship on its way.";return true;
    }

    internal bool GullReady=>gull && gull.ReadyOn(Ship);
    internal void CallGull() {if(gull) gull.CallDown();}

    private IEnumerator Plan(Vector3 start) => GuardedSteps.Run(BuildPlan(start), error =>
    {
        planning=null;
        Plugin.Instance.Error(error);
        Pause("Course planning failed. Choose the destination again or take the helm.");
    });

    private IEnumerator BuildPlan(Vector3 start)
    {
        // Yield once so the coroutine handle is assigned before it can complete or fail.
        yield return null;
        route.Clear();waypoint=0;
        if(routeEffect)routeEffect.Clear();
        var approach=destination.Berth.Approach;
        var leadIn=approach-destination.Berth.Forward*20;
        if(!chart.RegionalSegment(WaterChart.Point(leadIn),WaterChart.Point(approach)))
        {Pause("No clear straight approach to that dock.");yield break;}
        float planStarted=Time.time;bool planningAnnounced=false;
        var search=new RouteSearch(WaterChart.Point(start),WaterChart.Point(leadIn),chart.RegionalSegment);
        while(search.State==SearchState.Searching)
        {
            if(!planningAnnounced && Time.time-planStarted>=5)
            { planningAnnounced=true;if(gull)gull.Speak("Still finding us a safe course, Viking. Give me a moment."); }
            var watch=System.Diagnostics.Stopwatch.StartNew();
            do {search.Step(1);} while(search.State==SearchState.Searching && watch.ElapsedMilliseconds<3);
            if(phase!=VoyagePhase.Boarding) Status="Plotting course — "+search.Expanded+" water cells checked";
            yield return null;
        }
        if(search.State!=SearchState.Found)
        {Pause(search.State==SearchState.BudgetExceeded ? "Route search limit reached; choose a nearer dock." : "No safe route found.");yield break;}
        // Remove grid zigzags only when the complete replacement edge is clear.
        for(int i=0;i<search.Route.Count;)
        {
            route.Add(WaterChart.Vector(search.Route[i]));
            if(i==search.Route.Count-1)break;
            int next=i+1;
            for(int candidate=Math.Min(i+6,search.Route.Count-1);candidate>next;candidate--)
                if(chart.RegionalSegment(search.Route[i],search.Route[candidate])){next=candidate;break;}
            i=next;yield return null;
        }
        planning=null;
        route.Add(approach);
        if(routeEffect)routeEffect.SetRoute(route);
        // The first point is the departure endpoint/current ship position, already handled separately.
        waypoint=Math.Min(1,route.Count-1);
        if(planningAnnounced && gull)gull.Speak("Course found, Viking. On we go!",true);
        Plugin.Instance.Record("Route ready: "+route.Count+" waypoints; "+search.Expanded+" cells examined.");
        if(rerouting) {phase=VoyagePhase.Cruising;rerouting=false;ResetProgress();}
        else if(phase==VoyagePhase.Planning && departure!=null)
        {
            phase=departure.reverseDeparture ? VoyagePhase.Reversing : VoyagePhase.Departing;
            Status=departure.reverseDeparture ? "Backing out of the dock" : "Leaving the dock";
            Plugin.Instance.Record("Departure started from actual ship pose: "+departure.position+"; heading="+departure.heading);
            ResetProgress();
        }
    }

    internal void ChangeDestination(DockRecord to)
    {
        if(!CanControl || phase==VoyagePhase.Boarding || phase==VoyagePhase.Departing || phase==VoyagePhase.Reversing ||
            (phase==VoyagePhase.Planning && departure!=null && !rerouting))
        {Plugin.Message("Change destination after clearing the dock, or cancel and choose again.");return;}
        if(planning!=null) StopCoroutine(planning);
        destination=to;chart=new WaterChart(Ship);source=null;gate=null;rerouting=true;
        phase=VoyagePhase.Planning;command=Ship.Speed.Stop;rudder=0;
        planning=StartCoroutine(Plan(Ship.transform.position));
    }

    private void Update()
    {
        if(!Ship || !passenger || passenger.IsDead() || !ZNet.instance)
        {Cancel("Voyage ended.");return;}
    }

    internal void Tick(float dt)
    {
        if(phase==VoyagePhase.Finished) return;
        if(!Ship || !Ship.IsOwner() || !Plugin.Solo) {Cancel("Autopilot lost ship authority.");return;}
        if(Ship.m_shipControlls.HaveValidUser()) {Cancel("You have the helm.");return;}
        if(!passenger || passenger.IsDead()) {Cancel("Voyage stopped.");return;}
        if(Unattended && Ship.HasPlayerOnboard()){Cancel("Summon stopped: player boarded the ship.");return;}
        if(Unattended && (!Plugin.Instance.Summon || !Plugin.Instance.Summon.ReadyArea))
        {
            command=Ship.Speed.Stop;SpeedField(Ship)=command;RudderField(Ship)=0;Status="Waiting for water and terrain to load";
            if(waitingForWorldSince<0)waitingForWorldSince=Time.time;
            if(!worldWaitAnnounced && Time.time-waitingForWorldSince>=5)
            {worldWaitAnnounced=true;if(gull)gull.Speak("Holding here until I can check the waters ahead, Viking.");}
            return;
        }
        waitingForWorldSince=-1;worldWaitAnnounced=false;
        bool aboard=Ship.IsPlayerInBoat(passenger);
        if(phase==VoyagePhase.Boarding)
        {
            command=Ship.Speed.Stop;rudder=0;
            bool ready=gate!.Update(Time.time,aboard);
            Status=gate.State==BoardingState.WaitingForPlayer ? "Waiting for you to board" :
                ready ? "Checking departure route" : "Departure in "+Mathf.CeilToInt((float)gate.Remaining)+"s";
            if(ready)
            {
                var currentSource=DockDirectory.Resolve(sourceId);
                if(currentSource==null || Vector3.Distance(currentSource.Berth.position,source!.position)>.2f ||
                    Mathf.Abs(Mathf.DeltaAngle(currentSource.Berth.heading,source!.heading))>1 ||
                    currentSource.Berth.reverseDeparture!=source!.reverseDeparture ||
                    Mathf.Abs(currentSource.Berth.reverseDistance-source!.reverseDistance)>.2f)
                    Pause("Source berth changed; cancel and choose the voyage again.");
                else if(Vector3.Distance(WaterChart.AtSea(Ship.transform.position),WaterChart.AtSea(source!.position))>(Relaxed ? 20 : 4) ||
                    (!Relaxed && Mathf.Abs(Mathf.DeltaAngle(Ship.transform.eulerAngles.y,source.heading))>15))
                    Pause("Ship moved outside its berth; reposition before departure.");
                else if(!Relaxed && !chart.ValidateBerth(source!,out var reason)) Pause(reason);
                else
                {
                    departure=source!.Copy();
                    departure.position=WaterChart.AtSea(Ship.transform.position);
                    departure.heading=Ship.transform.eulerAngles.y;
                    phase=VoyagePhase.Planning;
                    Status="Boarded — plotting departure route";
                    Plugin.Instance.Record("Boarding complete; planning from actual ship pose.");
                    planning=StartCoroutine(Plan(departure.Exit));
                }
            }
        }
        else if(!Unattended && !Ship.IsPlayerInBoat(passenger)) {Cancel("Player left the ship; autopilot stopped.");return;}
        if(Time.time>=nextDecision && phase!=VoyagePhase.Boarding)
        {
            nextDecision=Time.time+.2f;
            Decide();
        }
        SpeedField(Ship)=command;
        RudderField(Ship)=Mathf.MoveTowards(Ship.GetRudderValue(),rudder,Ship.m_rudderSpeed*dt);
    }

    private void Decide()
    {
        var position=WaterChart.AtSea(Ship.transform.position);
        float speed=Ship.GetSpeed();
        var body=Ship.GetComponent<Rigidbody>();
        float groundSpeed=new Vector2(body.linearVelocity.x,body.linearVelocity.z).magnitude;
        if(phase==VoyagePhase.Paused || phase==VoyagePhase.Planning)
        {Brake(speed);return;}
        if(!destination.Temporary && Time.time>nextDestinationCheck)
        {
            nextDestinationCheck=Time.time+2;
            var current=DockDirectory.Resolve(destination.Id);
            if(current==null) {Pause("Destination ward removed or unconfigured.");return;}
            if(Vector3.Distance(current.Berth.position,destination.Berth.position)>.2f ||
                Mathf.Abs(Mathf.DeltaAngle(current.Berth.heading,destination.Berth.heading))>1)
            {Pause("Destination berth changed; choose it again to replan.");return;}
            destination=current;
        }
        float stopping=(float)HelmMath.StoppingDistance(groundSpeed,Deceleration,1.5,4);
        var motion=groundSpeed>.3f ? new Vector3(body.linearVelocity.x,0,body.linearVelocity.z).normalized :
            (phase==VoyagePhase.Reversing ? -Ship.transform.forward : Ship.transform.forward);
        if(!chart.HullSegment(position,position+motion*stopping,Quaternion.Euler(0,Ship.transform.eulerAngles.y,0),true,out var obstruction,Relaxed && DockManeuver))
        {ReplanOrPause(obstruction);Brake(speed);return;}

        if(phase==VoyagePhase.Reversing || phase==VoyagePhase.Departing)
        {
            bool reverse=phase==VoyagePhase.Reversing;
            var delta=departure!.Exit-position;
            var along=departure.Forward*(reverse ? -1 : 1);
            float remaining=Vector3.Dot(delta,along);
            float cross=Mathf.Abs(Vector3.Dot(position-departure.position,Quaternion.Euler(0,90,0)*departure.Forward));
            if(cross>(Relaxed ? 8 : 2.5f)) {Pause("Ship moved outside the departure corridor.");return;}
            if(remaining<2)
            {
                Brake(speed);
                if(groundSpeed<.35f)
                {
                    if(!Relaxed && !chart.TurningArea(position,out var reason)) Pause(reason);
                    else {phase=VoyagePhase.Turning;ResetProgress();}
                }
                return;
            }
            float error=Mathf.DeltaAngle(Ship.transform.eulerAngles.y,departure.heading);
            // Correct both orientation and cross-track drift while preserving the bow-in pose.
            float track=Vector3.Dot(position-departure.position,Quaternion.Euler(0,90,0)*departure.Forward);
            error += (reverse ? 1 : -1)*track*8;
            rudder=(float)HelmMath.Rudder(error,reverse);
            float target=Mathf.Clamp(remaining/8,.35f,1.2f);
            if((reverse ? -speed : speed)>target) Brake(speed);
            else SetMode(reverse ? Ship.Speed.Back : Ship.Speed.Slow,true);
            Status=reverse ? "Backing out of the dock" : "Leaving the dock";
        }
        else if(phase==VoyagePhase.Turning || phase==VoyagePhase.Cruising)
        {
            if(waypoint>=route.Count) {phase=VoyagePhase.Approaching;return;}
            var target=route[waypoint];float distance=Vector3.Distance(position,target);
            if(distance<6 && waypoint<route.Count-1) {waypoint++;target=route[waypoint];distance=Vector3.Distance(position,target);}
            if(waypoint==route.Count-1 && distance<5)
            {
                string reason;
                bool clear=destination.Temporary
                    ? ShorelineArrival.Validate(destination.MarkerPosition,destination.Berth,chart,out reason)
                    : chart.ValidateArrival(destination.Berth,out reason,Relaxed);
                if(!clear) {Pause("Destination unavailable: "+reason);return;}
                phase=VoyagePhase.Approaching;ResetProgress();return;
            }
            var delta=target-position;
            float error=Vector3.SignedAngle(Ship.transform.forward,delta,Vector3.up);
            rudder=(float)HelmMath.Rudder(error,false);
            // Check the predicted turning sweep before applying rudder, not just a straight cast.
            var predicted=position+motion*Mathf.Min(stopping,8);
            if(!chart.HullSegment(position,predicted,Quaternion.Euler(0,Ship.transform.eulerAngles.y+Mathf.Clamp(error,-20,20),0),true,out var turnReason))
            {ReplanOrPause("Turning clearance blocked: "+turnReason);return;}
            slowingForTurn=HelmMath.NeedsSlowTurn(error,slowingForTurn);
            if(slowingForTurn)
            {
                // Coast through a sharp turn before rowing. Keep rudder authority instead
                // of counter-thrust braking, which centers the rudder and kills momentum.
                SetMode(groundSpeed>2.5f ? Ship.Speed.Stop : Ship.Speed.Slow,true);
                Status=groundSpeed>2.5f ? "Coasting through a sharp turn" : "Paddling through a sharp turn";
            }
            else
            {
                phase=VoyagePhase.Cruising;
                float wind=Ship.GetWindAngleFactor();
                bool sail=ShipProfile.CanSail(Ship) && wind>(command==Ship.Speed.Half || command==Ship.Speed.Full ? .15f : .35f);
                SetMode(sail ? Ship.Speed.Half : Ship.Speed.Slow,!sail);
                Status=sail ? "Sailing to "+DestinationName : "Paddling to "+DestinationName;
            }
        }
        else if(phase==VoyagePhase.Approaching || phase==VoyagePhase.Docking)
        {
            var berth=destination.Berth;
            var delta=WaterChart.AtSea(berth.position)-position;
            float distance=delta.magnitude;
            float headingError=Mathf.DeltaAngle(Ship.transform.eulerAngles.y,berth.heading);
            if(distance<2.5f)
            {
                Brake(speed);
                if(groundSpeed<.35f)
                {
                    if(Mathf.Abs(headingError)<15) Arrive();
                    else Pause("Reached berth area but alignment is wrong; take the helm.");
                }
                return;
            }
            float along=Vector3.Dot(delta,berth.Forward);
            float cross=Mathf.Abs(Vector3.Dot(delta,Quaternion.Euler(0,90,0)*berth.Forward));
            if(along<0 || cross>4) {Pause("Outside the dock approach corridor; take the helm.");return;}
            float error=Vector3.SignedAngle(Ship.transform.forward,delta,Vector3.up);
            rudder=(float)HelmMath.Rudder(error,false);
            float targetSpeed=Mathf.Clamp(distance/12,.25f,1.2f);
            if(groundSpeed>targetSpeed || distance<stopping-2) Brake(speed);
            else SetMode(Ship.Speed.Slow,true);
            phase=VoyagePhase.Docking;Status="Approaching berth at "+DestinationName;
        }
        if(phase!=VoyagePhase.Paused && phase!=VoyagePhase.Finished)
        {
            if(Vector3.Distance(progressPosition,position)>2) ResetProgress();
            else if(Time.time-progressTime>35) ReplanOrPause("No progress along the route.");
        }
    }

    private void ReplanOrPause(string reason)
    {
        if((phase!=VoyagePhase.Cruising && phase!=VoyagePhase.Turning) || automaticReplans>=3)
        {Pause(reason+" Take the helm or choose a destination to retry.");return;}
        automaticReplans++;
        phase=VoyagePhase.Planning;command=Ship.Speed.Stop;rudder=0;
        Status=reason+" Slowing to replot course ("+automaticReplans+"/3).";
        if(gull)gull.Speak(VoyageWords.Obstruction(reason)+" Slowing down to find another way.");
        if(planning!=null)StopCoroutine(planning);
        planning=StartCoroutine(ReplanAfterSlowing());
    }
    private IEnumerator ReplanAfterSlowing()
    {
        float deadline=Time.time+15;
        while(Ship && Time.time<deadline)
        {
            var v=Ship.GetComponent<Rigidbody>().linearVelocity;
            if(new Vector2(v.x,v.z).magnitude<.4f)break;
            yield return null;
        }
        if(!Ship)yield break;
        var velocity=Ship.GetComponent<Rigidbody>().linearVelocity;
        if(new Vector2(velocity.x,velocity.z).magnitude>=.4f)
        {Pause("Unable to slow enough to replan; take the helm.");yield break;}
        chart=new WaterChart(Ship);source=null;rerouting=true;
        yield return Plan(Ship.transform.position);
    }

    private void SetMode(Ship.Speed mode,bool immediate)
    {
        if(command==mode)return;
        if(immediate || Time.time-modeSince>=3) {command=mode;modeSince=Time.time;}
    }
    private void Brake(float speed)
    {
        rudder=0;command=Ship.Speed.Stop;
        if(Mathf.Abs(speed)<.2f)return;
        var direction=Ship.transform.forward*(speed>0 ? -1 : 1);
        if(chart.HullSegment(Ship.transform.position,Ship.transform.position+direction*3,
            Quaternion.Euler(0,Ship.transform.eulerAngles.y,0),true,out _,Relaxed && DockManeuver))
            command=speed>0 ? Ship.Speed.Back : Ship.Speed.Slow;
    }
    private void ResetProgress(){progressPosition=WaterChart.AtSea(Ship.transform.position);progressTime=Time.time;}
    private void Pause(string reason)
    {
        phase=VoyagePhase.Paused;command=Ship.Speed.Stop;rudder=0;Status="Paused: "+reason;
        if(gull)gull.Speak(VoyageWords.Obstruction(reason)+" Take the helm or choose the destination again when it's clear.",true);
        Plugin.Message(Status);Plugin.Instance.Record("Paused: "+reason);
    }
    internal void ExplainStatus()
    {
        if(!gull)return;
        string text=phase==VoyagePhase.Planning ? "I'm finding a safe course, Viking. "+Status :
            phase==VoyagePhase.Paused ? VoyageWords.Obstruction(Status)+" Take the helm or choose the destination again when it's clear." :
            "Here's the situation, Viking: "+Status+".";
        gull.Speak(text,requested:true);
    }
    private void Arrive()
    {
        Status="Arrived at "+DestinationName;
        if(gull)gull.Speak(destination.Temporary ? "Your ship's here, Viking. Mind the step aboard!" :
            "We've reached "+DestinationName+", Viking. I'll keep this perch warm.",true);
        Finish(Status,true);
    }
    internal void Cancel(string reason)=>Finish(reason);
    private void Finish(string reason,bool arrived=false)
    {
        if(phase==VoyagePhase.Finished)return;
        phase=VoyagePhase.Finished;
        Plugin.Instance.Record("Voyage ended: "+reason);
        if(Ship && Ship.IsOwner()) {SpeedField(Ship)=Ship.Speed.Stop;RudderField(Ship)=0;}
        command=Ship.Speed.Stop;rudder=0;
        if(gull) {if(arrived && Ship)GullCall.AfterArrival(Ship!,gull);else gull.FlyAway();gull=null;}
        if(routeEffect) {routeEffect.gameObject.SetActive(false);Destroy(routeEffect.gameObject);}
        if(Plugin.Instance.Voyage==this)Plugin.Instance.Voyage=null;
        if(Unattended && Plugin.Instance.Summon)Plugin.Instance.Summon.Complete(reason);
        Plugin.Message(reason);Destroy(this);
    }
    private void OnDestroy()
    {
        if(phase!=VoyagePhase.Finished) Finish("Voyage ended.");
    }
}
