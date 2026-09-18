using System;
using System.Linq;
using HarmonyLib;
using Helmsman.Core;
using UnityEngine;
namespace Helmsman;

public sealed class PaddleCraftRig : MonoBehaviour
{
    public GameObject PaddleTemplate=null!;
    public bool DoubleBlade;
}

// Native AttachStart still handles movement, equipment hiding and seat ownership.
// These two keys replicate only the presentation anchor, including a tandem passenger.
[HarmonyPatch(typeof(Player),nameof(Player.AttachStart))]
internal static class PaddleSeatAttach
{
    private static void Prefix(Transform attachPoint,ref bool hideWeapons)
    {if(attachPoint&&attachPoint.GetComponentInParent<PaddleCraftRig>())hideWeapons=true;}
    private static void Postfix(Player __instance)
    {
        var anchor=__instance.GetAttachPoint();var craft=anchor?anchor.GetComponentInParent<PaddleCraftRig>():null;
        var view=__instance.GetComponent<ZNetView>();
        if(!view||!view.IsValid()||!view.IsOwner())return;
        var shipView=craft?craft.GetComponent<ZNetView>():null;
        view.GetZDO().Set("helmsman_paddle_ship",shipView&&shipView.IsValid()?shipView.GetZDO().m_uid:ZDOID.None);
        if(craft)view.GetZDO().Set("helmsman_paddle_seat",craft.transform.InverseTransformPoint(anchor!.position));
    }
}
[HarmonyPatch(typeof(Player),"LateUpdate")]
internal static class PaddlePlayerInstall
{
    private static void Postfix(Player __instance)
    {
        if(ZNet.instance&&ZNet.instance.IsDedicated())return;
        if(!__instance.GetComponent<PaddlePlayerPose>())__instance.gameObject.AddComponent<PaddlePlayerPose>();
    }
}

// Apply after the vanilla seated Animator. Restore the captured local pose before
// the next animation evaluation, so offsets never accumulate or leak on dismount.
[DefaultExecutionOrder(10000)]
public sealed class PaddlePlayerPose : MonoBehaviour
{
    private Player player=null!;private ZNetView view=null!;private Animator animator=null!;
    private Transform[] bones=Array.Empty<Transform>();
    private Quaternion[] rotations=Array.Empty<Quaternion>();private Vector3[] positions=Array.Empty<Vector3>();
    private bool applied,failed;private GameObject? paddle;private PaddleCraftRig? craft;
    private float blend;private double lastTime;
    private static readonly HumanBodyBones[] ids={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,
        HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,
        HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,
        HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,
        HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
    private static readonly string[] names={"Hips","Spine","Spine1","LeftArm","LeftForeArm","LeftHand","RightArm","RightForeArm","RightHand","LeftUpLeg","LeftLeg","LeftFoot","RightUpLeg","RightLeg","RightFoot"};
    private void Awake(){player=GetComponent<Player>();view=GetComponent<ZNetView>();}
    private void Update(){Restore();}
    private bool BindBones()
    {
        if(bones.Length>0)return true;if(failed)return false;
        animator=GetComponentsInChildren<Animator>().FirstOrDefault(a=>a.isHuman)??GetComponentInChildren<Animator>();
        if(!animator)return false;
        var descendants=animator.GetComponentsInChildren<Transform>(true);
        bones=ids.Select((id,i)=>animator.isHuman?animator.GetBoneTransform(id):descendants.FirstOrDefault(t=>t.name==names[i]||t.name.EndsWith(":"+names[i],StringComparison.Ordinal))).ToArray()!;
        if(bones.Any(b=>!b)){bones=Array.Empty<Transform>();failed=true;Plugin.Instance.Record("Paddling rig could not bind player skeleton; retained native seated animation.");return false;}
        bones=bones.Concat(descendants.Where(t=>t.name.Contains("Hand")&&new[]{"Index","Middle","Ring","Pinky","Thumb"}.Any(f=>t.name.Contains(f))&&!t.name.EndsWith("_end",StringComparison.Ordinal))).Distinct().ToArray();
        rotations=new Quaternion[bones.Length];positions=new Vector3[bones.Length];return true;
    }
    private void LateUpdate()
    {
        if(!view||!view.IsValid()||!ZNetScene.instance)return;
        if(view.IsOwner()&&(!player.IsAttached()||player.IsDead()))
        {
            if(view.GetZDO().GetZDOID("helmsman_paddle_ship")!=ZDOID.None)view.GetZDO().Set("helmsman_paddle_ship",ZDOID.None);
            Release();return;
        }
        var id=view.GetZDO().GetZDOID("helmsman_paddle_ship");
        var instance=id!=ZDOID.None?ZNetScene.instance.FindInstance(id):null;
        var next=instance?instance.GetComponent<PaddleCraftRig>():null;
        if(!next){Release();return;}
        var seat=view.GetZDO().GetVec3("helmsman_paddle_seat",Vector3.zero);
        var anchor=next.transform.TransformPoint(seat);
        // Reject stale remote attachment data after teleports, death or a destroyed ship.
        if((transform.position-anchor).sqrMagnitude>4||player.IsDead()){Release();return;}
        if(craft!=next)
        {
            Release();craft=next;
            paddle=Instantiate(craft.PaddleTemplate,craft.transform);paddle.name="Player paddle";paddle.SetActive(true);
        }
        if(!BindBones()){if(paddle)paddle.SetActive(false);return;}
        if(!paddle)return;
        paddle.SetActive(true);
        var ship=craft.GetComponent<Ship>();var speed=ship.GetSpeedSetting();bool rowing=speed==Ship.Speed.Slow||speed==Ship.Speed.Back;
        blend=Mathf.MoveTowards(blend,rowing?1:0,Time.deltaTime*3);
        double time=ZNet.instance?ZNet.instance.GetTime().Ticks/(double)TimeSpan.TicksPerSecond:Time.time;
        if(rowing)lastTime=time;
        var stroke=PaddleStroke.Sample(lastTime,craft.DoubleBlade,speed==Ship.Speed.Back);
        var root=craft.transform;
        var moving=new Vector3((float)stroke.X,(float)stroke.Y,(float)stroke.Z);
        var axis=Vector3.Lerp(Vector3.right,new Vector3((float)stroke.AxisX,(float)stroke.AxisY,(float)stroke.AxisZ),blend).normalized;
        Vector3 center=anchor+root.TransformDirection(Vector3.Lerp(new Vector3(0,.40f,.32f),moving,blend));
        Vector3 shaft=root.TransformDirection(axis);
        // HMF paddle lies on local X; rotate it as one rigid object between both grips.
        paddle.transform.position=center;paddle.transform.rotation=root.rotation*Quaternion.FromToRotation(Vector3.right,axis);
        for(int i=0;i<bones.Length;i++){rotations[i]=bones[i].localRotation;positions[i]=bones[i].localPosition;}
        applied=true;
        bones[0].position=anchor+root.up*.12f;bones[0].rotation=root.rotation;
        var torso=root.rotation*Quaternion.Euler((float)stroke.Lean*blend,(float)stroke.Twist*blend,0);
        bones[1].rotation=torso;bones[2].rotation=torso;
        // Keep the rigid shaft inside the reach of the actual animated shoulders,
        // including different avatar proportions. Hands never slide off the paddle.
        for(int pass=0;pass<8;pass++)
        {
            center=FitGrip(center,shaft,-.31f,bones[3],bones[4],bones[5]);
            center=FitGrip(center,shaft,.31f,bones[6],bones[7],bones[8]);
        }
        paddle.transform.position=center;
        // Targets remain on the shaft, with elbow poles outside the torso.
        Solve(bones[3],bones[4],bones[5],center-shaft*.31f,anchor-root.right*.8f+root.up*.45f);
        Solve(bones[6],bones[7],bones[8],center+shaft*.31f,anchor+root.right*.8f+root.up*.45f);
        Solve(bones[9],bones[10],bones[11],anchor+root.TransformDirection(new Vector3(-.18f,-.055f,.77f)),anchor+root.TransformDirection(new Vector3(-.25f,.26f,.43f)));
        Solve(bones[12],bones[13],bones[14],anchor+root.TransformDirection(new Vector3(.18f,-.055f,.77f)),anchor+root.TransformDirection(new Vector3(.25f,.26f,.43f)));
        foreach(int index in new[]{11,14})
        {
            var toe=bones[index].Cast<Transform>().FirstOrDefault(t=>t.name.Contains("ToeBase"));
            if(toe)bones[index].rotation=Quaternion.FromToRotation(toe.position-bones[index].position,root.forward)*bones[index].rotation;
        }
        // Orient each palm around the shaft using its rig's original hand-to-finger axis.
        AlignHand(bones[5],shaft,root.forward);AlignHand(bones[8],shaft,root.forward);
        foreach(var finger in bones.Skip(ids.Length))
        {
            if(finger.name.Contains("Thumb"))continue;
            float curl=finger.name.EndsWith("1",StringComparison.Ordinal)?30:55;
            finger.rotation=Quaternion.AngleAxis(curl,shaft)*finger.rotation;
        }
    }
    private static Vector3 FitGrip(Vector3 center,Vector3 shaft,float grip,Transform shoulder,Transform elbow,Transform hand)
    {
        float reach=Vector3.Distance(shoulder.position,elbow.position)+Vector3.Distance(elbow.position,hand.position)-.003f;
        Vector3 delta=center+shaft*grip-shoulder.position;
        return delta.magnitude>reach?center-delta.normalized*(delta.magnitude-reach):center;
    }
    private static void AlignHand(Transform hand,Vector3 shaft,Vector3 forward)
    {
        var finger=hand.Cast<Transform>().FirstOrDefault(t=>t.name.IndexOf("Middle",StringComparison.OrdinalIgnoreCase)>=0);
        if(finger&&(finger.position-hand.position).sqrMagnitude>.00001f)
            hand.rotation=Quaternion.FromToRotation((finger.position-hand.position).normalized,Vector3.Cross(shaft,forward).normalized)*hand.rotation;
    }
    private static void Solve(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 pole)
    {
        Vector3 origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
        if(a<.001f||b<.001f)return;
        Vector3 delta=target-origin;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);Vector3 axis=delta.normalized;
        Vector3 bend=Vector3.ProjectOnPlane(pole-origin,axis).normalized;
        if(bend.sqrMagnitude<.001f)bend=Vector3.ProjectOnPlane(upper.forward,axis).normalized;
        float along=(a*a+d*d-b*b)/(2*d),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        Vector3 elbow=origin+axis*along+bend*height;
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,origin+axis*d-lower.position)*lower.rotation;
    }
    private void Restore()
    {
        if(!applied)return;for(int i=0;i<bones.Length;i++)if(bones[i]){bones[i].localRotation=rotations[i];bones[i].localPosition=positions[i];}applied=false;
    }
    private void Release(){Restore();if(paddle)Destroy(paddle);paddle=null;craft=null;blend=0;}
    private void OnDisable(){Release();}
    private void OnDestroy(){Release();}
}
