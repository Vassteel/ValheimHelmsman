using System;
using System.Collections.Generic;
using System.Linq;
using Helmsman.Core;
using UnityEngine;
using VikingBirds;
using Object=UnityEngine.Object;
namespace Helmsman;
// Local crew reports arrival readiness. Only Slipway ownership starts the saved build clock.
internal sealed class PuffinConstructionCrew:IDisposable
{
    private static int activeBirds;
    private readonly Slipway home;
    private readonly List<Member> members=new();
    private Material? wood,rope;
    private readonly List<Bounds> obstacles=new();private float beam=4.8f;
    private long orderId=-1;private bool finishing;private float spawnAt;
    private int nextMember;
    internal bool InRange=>Player.m_localPlayer&&(Player.m_localPlayer.transform.position-home.transform.position).sqrMagnitude<=4900;
    internal int SettledCount=>members.Count(m=>m.Settled);
    internal bool Assembled=>SettledCount==PuffinCrewPlan.Count;
    internal PuffinConstructionCrew(Slipway slipway){home=slipway;}
    internal void Tick(SlipwayOrder? order,float delta)
    {
        if(!InRange){Clear();return;}
        bool building=order!=null&&order.launchStarted==0&&order.Elapsed(Slipway.Now)<order.order.duration;
        if(building&&order!.CrewIdentity!=orderId)
        {
            Clear();orderId=order.CrewIdentity;nextMember=0;finishing=false;spawnAt=Time.time;CacheHull(order);
        }
        float dt=Mathf.Min(delta,.05f);
        if(building&&!finishing&&nextMember<PuffinCrewPlan.Count&&activeBirds<18&&Time.time>=spawnAt+PuffinCrewPlan.ArrivalDelay(orderId,nextMember))
        {
            // One allocation per frame; crowded slipways share a bounded crew budget.
            if(TrySpawn(nextMember,order!))nextMember++;
            else spawnAt=Time.time+2;
        }
        if(!building&&!finishing){finishing=true;foreach(var m in members)m.Leave(orderId);}
        for(int i=members.Count-1;i>=0;i--)
        {
            members[i].Tick(dt,order!=null&&!order.AwaitingCrew&&Assembled);
            if(members[i].Done){members[i].Dispose();members.RemoveAt(i);activeBirds--;}
        }
    }
    private void CacheHull(SlipwayOrder order)
    {
        obstacles.Clear();var plan=ShipConstruction.Find(order.order.blueprint);var prefab=plan!=null?ShipDirectory.FindPrefab(plan.Prefab):null;if(!prefab)return;
        var ship=prefab.GetComponent<Ship>();beam=ship.m_floatCollider.size.x;
        foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            var r=f.GetComponent<Renderer>();if(!r||!r.enabled||r.forceRenderingOff||!f.sharedMesh)continue;
            bool hull=f.name.StartsWith("hull ")||f.name.StartsWith("mast ");
            if(!hull&&plan!.Source!="VikingShip")continue;
            if(!hull&&(r.bounds.size.y>4||Mathf.Max(r.bounds.size.x,r.bounds.size.z)<4))continue;
            var b=f.sharedMesh.bounds;Bounds area=default;bool first=true;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var p=prefab.transform.InverseTransformPoint(f.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
                p=home.transform.InverseTransformPoint(order.stage+home.StageRotation*p);
                if(first){area=new Bounds(p,Vector3.zero);first=false;}else area.Encapsulate(p);
            }
            // Mast and yard share a material batch: its full box would wrongly
            // block both side platforms. Reserve the vertical spar separately.
            if(f.name.StartsWith("mast "))
            {
                var center=home.transform.InverseTransformPoint(order.stage);center.y=area.center.y;
                area=new Bounds(center,new Vector3(.5f,area.size.y,.5f));
            }
            obstacles.Add(area);
        }
    }
    private bool FlightClear(Vector3 a,Vector3 b)
    {
        if(!PuffinNavigation.Clear(a,b))return false;
        // Include the bird's torso height when passing above unfinished geometry.
        var low=PuffinNavigation.Point(home.transform.InverseTransformPoint(a+Vector3.up*.3f));var high=PuffinNavigation.Point(home.transform.InverseTransformPoint(b+Vector3.up*.3f));
        return obstacles.All(box=>PuffinCrewSpace.Clear(low,high,PuffinNavigation.Point(box.min),PuffinNavigation.Point(box.max),.32f));
    }
    private bool TrySpawn(int index,SlipwayOrder order)
    {
        int side=index%2==0?-1:1;float z=new[]{5.2f,1.8f,-1.8f}[index/2];
        var hint=home.transform.TransformPoint(new Vector3(side*Mathf.Max(3.05f,beam*.5f+.48f),2.5f,z));
        if(!Physics.Raycast(hint+Vector3.up*2,Vector3.down,out var hit,5,PuffinNavigation.Mask,QueryTriggerInteraction.Ignore)||hit.normal.y<.75f)return false;
        var feet=hit.point+Vector3.up*.012f;if(!FlightClear(feet,feet))return false;
        var direction=PuffinNavigation.Vector(PuffinCrewPlan.Departure(orderId,index));
        direction.x=side*Mathf.Max(.4f,Mathf.Abs(direction.x));
        Vector3 arrival=feet;
        if(order.AwaitingCrew)
        {
            Vector3? fallback=null;bool direct=false;
            foreach(float height in new[]{4f,9f,14f,23f})
            {
                var candidate=feet+home.transform.TransformDirection(direction)*17+Vector3.up*height;
                if(!FlightClear(candidate,candidate))continue;
                fallback??=candidate;
                if(FlightClear(candidate,feet)){arrival=candidate;direct=true;break;}
            }
            if(!direct){if(!fallback.HasValue)return false;arrival=fallback.Value;}
        }
        var source=home.GetComponentsInChildren<MeshRenderer>().Select(r=>r.sharedMaterial).FirstOrDefault(m=>m&&m.shader&&m.shader.name=="Custom/Piece");
        if(!source)return false;
        wood??=ImportedShipMaterials.BoatyardMaterial(new Color(.44f,.30f,.16f),false);
        rope??=ImportedShipMaterials.BoatyardMaterial(new Color(.40f,.30f,.17f),false);
        var agent=new Member(home,index,orderId,source,wood,rope,feet,arrival,side,FlightClear,!order.AwaitingCrew);
        members.Add(agent);activeBirds++;return true;
    }
    private void Clear(){foreach(var m in members){m.Dispose();activeBirds--;}members.Clear();orderId=-1;nextMember=0;finishing=false;}
    public void Dispose(){Clear();if(wood)Object.Destroy(wood);if(rope)Object.Destroy(rope);}
    private sealed class Member:IDisposable
    {
        private readonly Func<Vector3,Vector3,bool> flightClear;
        private readonly Slipway home;private readonly PerchedBird bird;private readonly ShipyardAudio audio;
        private readonly PuffinCrewJob job;private readonly int index;private readonly float offset;
        private readonly Vector3 work,pickup,look;private readonly GameObject props;private readonly LineRenderer? line;
        private readonly List<Vector3> route=new();private PuffinFlightPath? search;
        private Vector3 goal,velocity;private int waypoint;private bool arrived,grounded,departing,carrying=true;
        private float retryAt,workTime,waitUntil,departAt,nextSound,phase,flightBlend=1;
        internal bool Done{get;private set;}
        private bool landed;private float landedAt;
        internal bool Settled=>landed&&Time.time-landedAt>=.6f;
        internal Member(Slipway owner,int number,long seed,Material source,Material timber,Material rope,Vector3 station,Vector3 start,int side,Func<Vector3,Vector3,bool> clear,bool alreadyBuilding)
        {
            home=owner;flightClear=clear;index=number;job=PuffinCrewPlan.Job(index);offset=PuffinCrewPlan.Variation(seed,index)*17;work=station;
            var pickupHint=work-home.transform.forward*(index>=4?2.3f:0);
            pickup=Physics.Raycast(pickupHint+Vector3.up,Vector3.down,out var support,2,PuffinNavigation.Mask,QueryTriggerInteraction.Ignore)&&support.normal.y>.75f?support.point+Vector3.up*.012f:work;
            look=work-home.transform.right*side*.55f;
            bird=new PerchedBird(home.transform,source,false);bird.Root.name="Construction puffin · "+job;bird.Root.transform.position=start;bird.Root.transform.localScale=Vector3.one*(.90f+PuffinCrewPlan.Variation(seed,index+9)*.12f);
            audio=new ShipyardAudio(bird.Root.transform);props=new GameObject("Puffin work supplies");props.transform.SetParent(home.transform,false);props.transform.position=work;props.transform.rotation=Quaternion.LookRotation(-home.transform.right*side);
            if(job==PuffinCrewJob.Hammer||job==PuffinCrewJob.Shave)
            {
                float height=job==PuffinCrewJob.Hammer?.32f:.28f;
                Box("Timber being worked",new Vector3(0,height,.43f),new Vector3(.72f,.09f,.32f),timber);
                Box("Trestle",new Vector3(-.22f,(height-.045f)*.5f,.43f),new Vector3(.09f,height-.045f,.30f),timber);
                Box("Trestle",new Vector3(.22f,(height-.045f)*.5f,.43f),new Vector3(.09f,height-.045f,.30f),timber);
                if(job==PuffinCrewJob.Shave)for(int i=0;i<5;i++)Box("Wood shaving",new Vector3((i-2)*.08f,.015f,.35f+i%2*.08f),new Vector3(.07f,.008f,.018f),timber);
            }
            if(job==PuffinCrewJob.Haul)
            {
                var n=new GameObject("Tensioned working rope");n.transform.SetParent(props.transform,false);line=n.AddComponent<LineRenderer>();line.positionCount=3;line.startWidth=line.endWidth=.018f;line.sharedMaterial=rope;line.useWorldSpace=true;line.enabled=false;
                Box("Rope hauling post",new Vector3(0,.16f,.68f),new Vector3(.08f,.32f,.08f),timber);
                Box("Hauling post base",new Vector3(0,.035f,.68f),new Vector3(.30f,.07f,.26f),timber);
            }
            if(job==PuffinCrewJob.Timber||job==PuffinCrewJob.Supplies)
            {
                for(int i=0;i<3;i++)
                {
                    Box("Delivered timber",new Vector3(.30f,.05f+i*.08f,.58f),new Vector3(.70f-i*.08f,.07f,.13f),timber);
                    Box("Stock timber",props.transform.InverseTransformPoint(pickup)+new Vector3(.30f,.05f+i*.08f,.58f),new Vector3(.70f-i*.08f,.07f,.13f),timber);
                }
            }
            props.SetActive(false);SetGoal(work);
            // Reloading or entering range during an existing build restores workers
            // on site; it must not replay arrivals over a half-built ship.
            if(alreadyBuilding){bird.Root.transform.position=work;arrived=landed=true;landedAt=Time.time-1;grounded=true;flightBlend=0;waitUntil=Time.time+2.4f;}
        }
        private void Box(string name,Vector3 p,Vector3 size,Material mat)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;Object.DestroyImmediate(o.GetComponent<Collider>());o.transform.SetParent(props.transform,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;
        }
        private void SetGoal(Vector3 p){goal=p;arrived=false;route.Clear();waypoint=0;search=null;retryAt=0;velocity=Vector3.zero;}
        internal void Leave(long seed)
        {
            if(departing)return;departing=true;departAt=Time.time;props.SetActive(false);if(line)line.enabled=false;
            var p=PuffinNavigation.Vector(PuffinCrewPlan.Departure(seed,index+6));
            // Spread the exits around the horizon and rise clear of the mast.
            SetGoal(bird.Root.transform.position+p*22+Vector3.up*7);waitUntil=Time.time+index*.22f;
        }
        private bool Clear(Vector3 a,Vector3 b)=>flightClear(a,b)&&(!grounded||PuffinNavigation.WalkClear(PuffinNavigation.Point(a),PuffinNavigation.Point(b)));
        private void Navigate()
        {
            if(search!=null)
            {
                if(!PuffinNavigation.SearchTurn())return;search.Step(24);
                if(search.State==PuffinFlightPath.Result.Searching)return;
                if(search.State==PuffinFlightPath.Result.Found){route.AddRange(search.Points.Select(PuffinNavigation.Vector));waypoint=1;}
                search=null;retryAt=Time.time+2;return;
            }
            if(route.Count>0||Time.time<retryAt||!PuffinNavigation.SearchTurn())return;
            var start=bird.Root.transform.position;retryAt=Time.time+2;
            grounded=!departing&&Vector3.Distance(start,goal)<4&&PuffinNavigation.WalkClear(PuffinNavigation.Point(start),PuffinNavigation.Point(goal));
            if(flightClear(start,goal)){route.Add(start);route.Add(goal);waypoint=1;return;}
            grounded=false;
            foreach(float height in new[]{1f,2f,4f})
            {
                var raised=start+Vector3.up*height;var approach=goal+Vector3.up*height;
                if(flightClear(start,raised)&&flightClear(raised,approach)&&flightClear(approach,goal))
                {route.Add(start);route.Add(raised);route.Add(approach);route.Add(goal);waypoint=1;return;}
            }
            search=new PuffinFlightPath(PuffinNavigation.Point(start),PuffinNavigation.Point(goal),(a,b)=>flightClear(PuffinNavigation.Vector(a),PuffinNavigation.Vector(b)),.9f);

        }
        internal void Tick(float dt,bool workAllowed)
        {
            var root=bird.Root.transform;var before=root.position;workTime+=dt;
            if(departing&&(Time.time-departAt>20||arrived)){Done=true;return;}
            if(!arrived&&Time.time>=waitUntil)
            {
                Navigate();
                if(waypoint<route.Count)
                {
                    for(int n=Math.Min(route.Count-1,waypoint+3);n>waypoint;n--)if(Clear(before,route[n])){waypoint=n;break;}
                    var delta=route[waypoint]-before;float distance=Vector3.Distance(before,goal);
                    float speed=Mathf.Min(grounded?.53f:departing?4:3,Mathf.Sqrt(distance*(grounded?2:4)));
                    velocity=Vector3.MoveTowards(velocity,delta.normalized*speed,dt*(grounded?1.7f:3));var step=velocity*dt;if(step.magnitude>delta.magnitude)step=delta;
                    if(Clear(before,before+step))root.position+=step;else {route.Clear();velocity=Vector3.zero;retryAt=Time.time+1;}
                    if(waypoint<route.Count-1&&Vector3.Distance(root.position,route[waypoint])<.09f)waypoint++;
                    if(Vector3.Distance(root.position,goal)<.04f&&Clear(root.position,goal)){root.position=goal;arrived=true;if(!landed){landed=true;landedAt=Time.time;}velocity=Vector3.zero;workTime=0;waitUntil=Time.time+2.4f+offset*.08f;}
                }
            }
            if(arrived&&!departing)
            {
                props.SetActive(true);
                if((job==PuffinCrewJob.Timber||job==PuffinCrewJob.Supplies)&&workAllowed&&Time.time>=waitUntil)
                {carrying=!carrying;SetGoal(carrying?work:pickup);}
            }
            var desired=velocity.sqrMagnitude>.005f?velocity:look-root.position;desired.y=0;
            if(desired.sqrMagnitude>.005f)root.rotation=Quaternion.RotateTowards(root.rotation,Quaternion.LookRotation(desired),dt*(grounded?120:170));
            bool working=workAllowed&&arrived&&!departing;bool carrier=job==PuffinCrewJob.Timber||job==PuffinCrewJob.Supplies;
            var pose=(!carrier&&working)||(carrier&&carrying&&!departing&&(!arrived||workAllowed))?PuffinCrewPlan.Work(job,workTime+offset):default;
            bird.SetTool(pose.Tool);bird.Pose(pose.Pitch,pose.HeadYaw,pose.Roll,pose.BodyPitch,pose.Crouch,pose.Wing);
            float moved=Vector3.Distance(before,root.position);phase+=moved/.24f*Mathf.PI*2;
            var floor=PuffinNavigation.Floor(PuffinNavigation.Point(root.position));bool airborne=!arrived&&!grounded&&(!floor.HasValue||Mathf.Abs(floor.Value.Y-root.position.y)>.12f);
            flightBlend=Mathf.MoveTowards(flightBlend,airborne?1:0,dt*4);
            if(flightBlend>.01f)bird.Fly(Time.time+offset,flightBlend,velocity.y,-Vector3.SignedAngle(root.forward,velocity.normalized,Vector3.up)*.25f);
            else if(moved>.0001f)bird.Waddle(phase,1);
            if(line)
            {
                line.enabled=working;
                if(working){var end=props.transform.TransformPoint(new Vector3(0,.30f,.68f));line.SetPosition(0,bird.BeakWorld);line.SetPosition(1,Vector3.Lerp(bird.BeakWorld,end,.5f)+Vector3.down*.025f);line.SetPosition(2,end);}
            }
            if(working&&Time.time>nextSound)
            {
                if(job==PuffinCrewJob.Hammer&&PuffinPerformance.Strike(1,workTime+offset-dt,workTime+offset)){audio.Play(YardSound.Hammer,.5f);nextSound=Time.time+.5f;}
                else if(job==PuffinCrewJob.Shave){audio.Play(YardSound.Scrape,.4f);nextSound=Time.time+2.2f;}
                else if(job==PuffinCrewJob.Haul){audio.Play(YardSound.Rope,.35f);nextSound=Time.time+2.7f;}
            }
        }
        public void Dispose(){audio.Dispose();bird.Dispose();if(props)Object.Destroy(props);}
    }
}
