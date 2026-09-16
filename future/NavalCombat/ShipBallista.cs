using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Helmsman;

// One network owner fires both fittings; observers only animate the saved aim and shot stamp.
public sealed class ShipBallista : MonoBehaviour
{
    private Shipwright ship=null!;
    private Turret source=null!;
    private Transform body=null!,neck=null!,eye=null!;
    private int shotKey,aimKey;
    private double seenShot;
    private float nextTarget,warmup;
    private Character? target;
    private readonly List<Character> filter=new();
    private bool observed,wasOwner;
    internal void Initialize(Shipwright owner,int index,Turret native)
    {
        ship=owner;source=native;shotKey=Shipwright.Key("shot_"+index);aimKey=Shipwright.Key("aim_"+index);
        body=transform.Find("BodyRotation");neck=transform.Find("NeckRotation");eye=body?body.Find("Eye"):null!;
        if(!body || !neck || !eye)throw new InvalidOperationException("Native ballista joints are missing.");
    }
    private ItemDrop.ItemData? Ammo()
    {
        string name=ship.Data.GetString(Shipwright.AmmoTypeKey);
        var found=source.m_allowedAmmo.FirstOrDefault(a=>a.m_ammo && a.m_ammo.name==name);
        return found.m_ammo?found.m_ammo.m_itemData:null;
    }
    private bool ValidTarget(Character? candidate)
    {
        if(!candidate || candidate.IsDead() || candidate.IsPlayer() || candidate.IsTamed())return false;
        var direction=candidate.GetCenterPoint()-eye.position;
        return direction.sqrMagnitude<=30*30 && Vector3.Angle(transform.forward,Vector3.ProjectOnPlane(direction,ship.transform.up))<60;
    }
    private bool ClearShot(Character candidate,Vector3 point)
    {
        var delta=point-eye.position;
        var hits=Physics.RaycastAll(eye.position,delta.normalized,delta.magnitude,LayerMask.GetMask("Default","static_solid","Default_small","piece","terrain","vehicle","character","character_net","character_ghost"),QueryTriggerInteraction.Ignore);
        foreach(var hit in hits.OrderBy(h=>h.distance))
        {
            if(hit.collider.GetComponentInParent<Ship>()==ship.Ship)return false; // Never shoot through our own mast, canopy or hull.
            var character=hit.collider.GetComponentInParent<Character>();
            if(character==candidate)return true;
            return false;
        }
        return true;
    }
    private void OnEnable(){observed=false;wasOwner=false;target=null;warmup=Time.time+1;nextTarget=0;}
    private void Update()
    {
        if(!ship || !ship.Ready || !ZNet.instance)return;
        bool owner=ship.View.IsOwner();
        if(owner!=wasOwner){wasOwner=owner;target=null;warmup=Time.time+1;nextTarget=0;}
        int count=ship.Data.GetInt(Shipwright.AmmoKey);var ammo=Ammo();
        if(owner && Time.time>=nextTarget)
        {
            nextTarget=Time.time+.5f;
            var found=count>0 && ammo!=null ? BaseAI.FindClosestCreature(transform,eye.position,0,30,60,false,false,true,false,false,true,filter) : null;
            if(found!=target){target=found;warmup=Time.time+1;}
            if(!ValidTarget(target))target=null;
            ship.Data.Set(aimKey,target?target!.GetCenterPoint():Vector3.zero);
        }
        var aim=owner && ValidTarget(target)?target!.GetCenterPoint():ship.Data.GetVec3(aimKey,Vector3.zero);
        Vector3 direction=aim==Vector3.zero?transform.forward:aim-eye.position;
        if(direction.sqrMagnitude>.001f)body.rotation=Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(direction,ship.transform.up),60*Time.deltaTime);
        neck.localRotation=Quaternion.Euler(0,body.localEulerAngles.y,0);
        double stamp=ship.Data.GetLong(shotKey,0)/1000.0;
        if(!observed){seenShot=stamp;observed=true;}
        if(stamp!=seenShot){seenShot=stamp;source.m_shootEffect.Create(eye.position,eye.rotation);}
        bool cooling=ZNet.instance.GetTimeSeconds()<stamp+Math.Max(2,source.m_attackCooldown);
        body.Find("Body")?.gameObject.SetActive(!cooling && count>0);
        body.Find("Body_Unarmed")?.gameObject.SetActive(cooling || count<=0);
        foreach(var kind in source.m_allowedAmmo)if(kind.m_visual)body.Find(kind.m_visual.name)?.gameObject.SetActive(!cooling && count>0 && kind.m_ammo.m_itemData==ammo);
        if(!owner || count<=0 || ammo==null || !ValidTarget(target) || cooling || Time.time<warmup || Vector3.Angle(eye.forward,direction)>3 || !ClearShot(target!,aim))return;
        Fire(ammo,count);
    }
    private void Fire(ItemDrop.ItemData ammo,int count)
    {
        var attack=ammo.m_shared.m_attack;if(!attack.m_attackProjectile || attack.m_attackProjectile.GetComponent<IProjectile>()==null)return;
        // Deduct once before creating the network projectile. No client-side replica fires.
        ship.Data.Set(Shipwright.AmmoKey,count-1);ship.Data.Set(shotKey,(long)(ZNet.instance.GetTimeSeconds()*1000));
        GameObject? projectile=null;
        try
        {
            projectile=Instantiate(attack.m_attackProjectile,eye.position,eye.rotation);
            var hit=new HitData{m_toolTier=(short)ammo.m_shared.m_toolTier,m_pushForce=ammo.m_shared.m_attackForce,
                m_backstabBonus=ammo.m_shared.m_backstabBonus,m_staggerMultiplier=attack.m_staggerMultiplier,
                m_statusEffectHash=ammo.m_shared.m_attackStatusEffect?ammo.m_shared.m_attackStatusEffect.NameHash():0,
                m_blockable=ammo.m_shared.m_blockable,m_dodgeable=ammo.m_shared.m_dodgeable,m_skill=ammo.m_shared.m_skillType,
                m_itemWorldLevel=(byte)Game.m_worldLevel,m_hitType=HitData.HitType.Turret};
            hit.m_damage.Add(ammo.GetDamage());projectile.GetComponent<IProjectile>()!.Setup(null,eye.forward*attack.m_projectileVel,source.m_hitNoise,hit,null,ammo);
        }
        catch(Exception error)
        {
            if(projectile)ZNetScene.instance.Destroy(projectile);
            ship.Data.Set(Shipwright.AmmoKey,count);Plugin.Instance.Error(error);
        }
    }
}
