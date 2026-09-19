using System;
using System.Collections.Generic;
using System.Linq;
using Helmsman.Core;
using UnityEngine;
using VikingBirds;
namespace Helmsman;
// Local cosmetic NPC, like the Quartermaster owl: no health, drops, network
// inventory or physical body. Construction authority stays on the slipway.
internal sealed class PuffinWorker:IDisposable
{
    private readonly Renderer[] renderers;private bool distant;
    private readonly Shipyard home;private readonly PerchedBird bird;private readonly ShipyardAudio audio;
    private readonly Vector3 perch;private readonly Quaternion homeRotation;
    private Vector3 goal,aim,velocity;private readonly List<Vector3> route=new();private int waypoint;
    private PuffinGroundPath? ground;private PuffinFlightPath? flight;
    private bool flying,arrived=true,nearby,sleeping;private float flightBlend,chooseAt,retryAt,voiceAt,footAt,phase,workTime,offset;
    private int tool=1;private string job="Resting at the workbench";private Slipway? slip;
    private float scanAt;private bool working;
    internal string Status=>sleeping?"Sleeping until morning":!arrived&&route.Count==0&&ground==null&&flight==null?"Waiting for a clear route · "+job:!arrived?(flying?"Flying · ":"Walking · ")+job:working?job:job=="Resting at the workbench"?"Keeping an eye on the workshop":job;
    internal PuffinWorker(Shipyard owner,PerchedBird model)
    {
        home=owner;bird=model;renderers=bird.Root.GetComponentsInChildren<Renderer>(true);audio=new ShipyardAudio(bird.Root.transform);perch=home.transform.InverseTransformPoint(bird.Root.transform.position);
        goal=bird.Root.transform.position;aim=goal+home.transform.forward;homeRotation=bird.Root.transform.rotation;
        offset=UnityEngine.Random.Range(0,100f);voiceAt=Time.time+30+offset;chooseAt=Time.time+6;
    }
    private bool Clear(Vector3 a,Vector3 b)=>flying?PuffinNavigation.Clear(a,b):PuffinNavigation.WalkClear(PuffinNavigation.Point(a),PuffinNavigation.Point(b));
    private void BeginRoute()
    {
        route.Clear();waypoint=0;flight=null;ground=null;retryAt=Time.time+3;arrived=false;
        if(!PuffinNavigation.SearchTurn())return;
        var a=PuffinNavigation.Point(bird.Root.transform.position);var b=PuffinNavigation.Point(goal);
        flying=false;ground=new PuffinGroundPath(a,b,PuffinNavigation.Floor,PuffinNavigation.WalkClear);arrived=false;
    }
    private void FindRoute()
    {
        if(ground==null&&flight==null)return;
        if(!PuffinNavigation.SearchTurn())return;
        if(ground!=null)
        {
            ground.Step(24);
            if(ground.State==PuffinFlightPath.Result.Searching)return;
            if(ground.State==PuffinFlightPath.Result.Found){route.AddRange(ground.Points.Select(PuffinNavigation.Vector));ground=null;waypoint=1;return;}
            ground=null;flying=true;
            if(PuffinNavigation.Hop(bird.Root.transform.position,goal,out var lift))
            {
                route.Add(bird.Root.transform.position);route.Add(lift);route.Add(goal+(lift-bird.Root.transform.position));route.Add(goal);waypoint=1;return;
            }
            flight=new PuffinFlightPath(PuffinNavigation.Point(bird.Root.transform.position),PuffinNavigation.Point(goal),PuffinNavigation.Clear,.6f);
        }
        if(flight!=null)
        {
            flight.Step(24);
            if(flight.State==PuffinFlightPath.Result.Searching)return;
            if(flight.State==PuffinFlightPath.Result.Found){route.AddRange(flight.Points.Select(PuffinNavigation.Vector));waypoint=1;}
            flight=null;
        }
    }
    private bool Surface(Transform station,Vector3 local,out Vector3 target)
    {
        target=station.TransformPoint(local);var ray=new Ray(target+Vector3.up*3,Vector3.down);float closest=float.MaxValue;bool found=false;
        foreach(var c in station.GetComponentsInChildren<Collider>())
            if(!c.isTrigger&&c.Raycast(ray,out var hit,6)&&hit.normal.y>.8f&&hit.distance<closest)
            {closest=hit.distance;target=hit.point+Vector3.up*.012f;found=true;}
        return found&&PuffinNavigation.Clear(target,target);
    }
    private void Choose()
    {
        chooseAt=Time.time+18+UnityEngine.Random.Range(0,8);
        var order=slip?slip.Order:null;
        working=order!=null&&!order.awaitingMaterials&&!order.AwaitingCrew&&order.launchStarted==0;
        Transform station=home.transform;Vector3 spot=perch;tool=1;job="Resting at the workbench";
        if(working)
        {
            double elapsed=order!.Elapsed(Slipway.Now);
            float progress=(float)(elapsed/order.order.duration);
            string kind=progress<.35?"tools":progress<.68?"caulking":"rigging";
            tool=kind=="tools"?1:kind=="caulking"?2:3;
            job=kind=="tools"?"Preparing hull timbers":kind=="caulking"?"Caulking the hull":"Preparing ropes and sailcloth";
            var upgrade=UnityEngine.Object.FindObjectsByType<WorkshopUpgrade>(FindObjectsSortMode.None)
                .Where(u=>u&&u.Kind==kind&&WorkshopRange.Connected(Player.m_localPlayer,home.transform.position,u.transform.position))
                .OrderBy(u=>(u.transform.position-home.transform.position).sqrMagnitude).FirstOrDefault();
            // Alternate station work with inspection from the slipway side platform.
            if(((int)(elapsed/22))%2==1&&slip)
            {station=slip.transform;spot=new Vector3(3.05f,1.8f,2);working=false;job="Inspecting the ship's "+(progress<.68?"hull":"rigging");}
            else if(upgrade){station=upgrade.transform;spot=kind=="rigging"?new Vector3(.32f,.47f,.13f):new Vector3(.22f,.82f,.20f);}
        }
        else if(order?.awaitingMaterials==true){job="Waiting for construction supplies";}
        else if(home.GetComponent<Structures.StructureConstruction>()?.Busy==true){working=true;job="Preparing structure timbers";tool=1;}
        else if(order?.AwaitingCrew==true){job="Waiting for the construction crew";}
        else if(order!=null){station=slip!.transform;spot=new Vector3(3.05f,1.8f,2);job=order.launchPaused>0?"Waiting for clear launch water":"Watching the launch";}
        else if(EnvMan.instance&&EnvMan.IsDaylight()&&UnityEngine.Random.value<.5f)
        {
            var paint=UnityEngine.Object.FindObjectsByType<WorkshopUpgrade>(FindObjectsSortMode.None).FirstOrDefault(u=>u&&u.Kind=="paint"&&WorkshopRange.Connected(Player.m_localPlayer,home.transform.position,u.transform.position));
            if(paint){station=paint.transform;spot=new Vector3(.06f,.80f,.22f);tool=4;working=true;job="Tending paints and brushes";}
            else {spot=perch+new Vector3(-.30f,0,.07f);job="Checking the bench";}
        }
        Vector3 target;
        if(station==home.transform)target=home.transform.TransformPoint(spot);
        else if(!Surface(station,spot,out target))
        {
            if(!PuffinNavigation.GroundNear(station.position,bird.Root.transform.position,station,out target,true))
            {target=home.transform.TransformPoint(perch);job="Preparing supplies at the bench";}
        }
        aim=station.position+Vector3.up*.85f;
        if(Vector3.Distance(target,goal)>.15f){goal=target;BeginRoute();}
    }
    internal void Tick(float delta)
    {
        bool far=!Player.m_localPlayer||(Player.m_localPlayer.transform.position-bird.Root.transform.position).sqrMagnitude>6400;
        if(far!=distant){distant=far;foreach(var r in renderers)if(r)r.forceRenderingOff=far;}
        if(far)return;
        float dt=Mathf.Min(delta,.05f);if(dt<=0)return;
        if(Time.time>scanAt)
        {
            scanAt=Time.time+2;
            var next=UnityEngine.Object.FindObjectsByType<Slipway>(FindObjectsSortMode.None).Where(s=>s&&s.Ready&&s.Order?.bench==home.Identity).OrderBy(s=>s.Order!.order.started).FirstOrDefault();
            if(next!=slip){slip=next;chooseAt=0;}
        }
        if(slip&&slip.Order?.launchStarted>0&&working)chooseAt=0;
        if(Time.time>chooseAt)Choose();
        var root=bird.Root.transform;var before=root.position;
        bool near=Player.m_localPlayer&&(Player.m_localPlayer.transform.position-before).sqrMagnitude<16;
        if(near&&!nearby)audio.Play(YardSound.PuffinGreeting,.65f);nearby=near;
        sleeping=!working&&!slip&&!near&&arrived&&EnvMan.instance&&!EnvMan.IsDaylight();
        if(!sleeping&&Time.time>voiceAt){audio.Play(YardSound.PuffinMurmur,.45f);voiceAt=Time.time+90+UnityEngine.Random.Range(0,70);}
        if(!arrived)
        {
            FindRoute();
            if(waypoint<route.Count)
            {
                // Look ahead only through verified clear space; velocity rounds
                // turns, and every resulting curved step gets another sweep.
                for(int n=Math.Min(route.Count-1,waypoint+5);n>waypoint;n--)if((!flying||n<route.Count-1||Vector3.Distance(before,goal)<1)&&Clear(before,route[n])){waypoint=n;break;}
                var to=route[waypoint]-before;float remaining=Vector3.Distance(before,goal);
                float max=flying?2.1f:.46f;
                float speed=Mathf.Min(max,Mathf.Sqrt(2*(flying?2.4f:1.1f)*remaining));
                var desired=to.normalized*speed;
                velocity=Vector3.MoveTowards(velocity,desired,dt*(flying?3.5f:1.5f));
                var step=velocity*dt;
                if(step.magnitude>to.magnitude)step=to;
                if(Clear(before,before+step))root.position+=step;
                else {velocity=Vector3.zero;route.Clear();retryAt=Time.time+1;}
                if(waypoint<route.Count-1&&Vector3.Distance(root.position,route[waypoint])<.07f)waypoint++;
                if(Vector3.Distance(root.position,goal)<.035f&&Clear(root.position,goal)){root.position=goal;velocity=Vector3.zero;arrived=true;workTime=0;}
            }
            else if(ground==null&&flight==null&&Time.time>retryAt)BeginRoute();
        }
        float moved=Vector3.Distance(before,root.position);phase+=moved/.24f*Mathf.PI*2;
        var floor=PuffinNavigation.Floor(PuffinNavigation.Point(root.position));
        bool unsupported=!floor.HasValue||Mathf.Abs(floor.Value.Y-root.position.y)>.12f;
        flightBlend=Mathf.MoveTowards(flightBlend,!arrived&&flying&&(route.Count>0||unsupported)?1:0,dt*3);
        if(velocity.sqrMagnitude>.0001f)
        {
            var flat=new Vector3(velocity.x,0,velocity.z);
            if(flat.sqrMagnitude>.001f)root.rotation=Quaternion.RotateTowards(root.rotation,Quaternion.LookRotation(flat),dt*(flying?160:110));
        }
        else if(arrived)
        {
            var look=aim-root.position;look.y=0;
            var rotation=look.sqrMagnitude>.02f?Quaternion.LookRotation(look):homeRotation;
            root.rotation=Quaternion.RotateTowards(root.rotation,rotation,dt*65);
        }
        workTime+=dt;
        var pose=working&&arrived?PuffinPerformance.Station(tool,workTime):default;
        bird.SetTool(pose.Tool);bird.Pose(pose.Pitch,pose.HeadYaw,pose.Roll,pose.BodyPitch,pose.Crouch,pose.Wing);
        if(!working||!arrived)bird.Idle(Time.time+offset);
        if(arrived&&near&&!working)bird.LookAt(Player.m_localPlayer!.GetEyePoint(),dt);
        if(flightBlend>.01f)bird.Fly(Time.time,flightBlend,velocity.y,-Vector3.SignedAngle(root.forward,velocity.normalized,Vector3.up)*.35f);
        else if(moved>.00001f)bird.Waddle(phase,Mathf.Clamp01(velocity.magnitude/.46f));
        bird.Rest(sleeping,Time.time+offset,dt);
        if(moved>.0001f&&!flying&&Time.time>footAt){audio.Play(YardSound.Footstep,.18f);footAt=Time.time+.32f;}
        if(working&&arrived&&PuffinPerformance.Strike(tool,workTime-dt,workTime))audio.Play(tool==1?YardSound.Hammer:tool==3?YardSound.Rope:YardSound.Scrape,.65f);
    }
    public void Dispose()=>audio.Dispose();
}
