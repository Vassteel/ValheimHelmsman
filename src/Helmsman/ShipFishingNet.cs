using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Helmsman;

public sealed class ShipFishingNet : MonoBehaviour
{
    public Container Chest=null!;
    public GameObject Visual=null!;
    private ZNetView view=null!;
    private float nextCatch;
    private const string ActiveKey="odinship_net_is_active";
    internal bool Active=>view&&view.IsValid()&&view.GetZDO().GetBool(ActiveKey);
    private IEnumerator Start()
    {
        view=GetComponentInParent<Ship>().GetComponent<ZNetView>();
        while(view&&!view.IsValid())yield return null;
        if(view)view.Register<long>("HelmsmanNetToggle",Toggle);
    }
    internal void Request()=>view.InvokeRPC("HelmsmanNetToggle",Player.m_localPlayer.GetPlayerID());
    private void Toggle(long sender,long playerId)
    {
        if(!view||!view.IsValid()||!view.IsOwner())return;
        var player=Player.GetAllPlayers().FirstOrDefault(p=>p&&p.GetPlayerID()==playerId);
        if(!player||player.GetComponent<ZNetView>().GetZDO().GetOwner()!=sender||player.GetStandingOnShip()!=GetComponentInParent<Ship>())return;
        view.GetZDO().Set(ActiveKey,!Active);
    }
    private void Update(){if(Visual)Visual.SetActive(Active);}
    private void OnTriggerStay(Collider other)
    {
        if(!Active||!view.IsOwner()||Time.time<nextCatch||!Chest||Chest.IsInUse())return;
        var fish=other.GetComponentInParent<Fish>();if(!fish)return;
        var drop=fish.GetComponent<ItemDrop>();var fishView=fish.GetComponent<ZNetView>();
        // Never delete a fish simulated by another peer. Normal sector ownership
        // moves nearby fish to the same peer as the boat; the next trigger retries.
        if(!drop||!fishView||!fishView.IsValid()||!fishView.IsOwner()||fishView.GetZDO().GetBool("helmsman_net_caught"))return;
        var inventory=Chest.GetInventory();var scope=Chest.GetComponent<ShipHoldScope>();
        if(scope&&scope.MigrationBlocked)return;
        if(view.GetZDO().GetInt(scope?scope.Key(ZDOVars.s_inUse):ZDOVars.s_inUse)!=0)return;
        var item=drop.m_itemData.Clone();item.m_stack=1;
        if(!inventory.CanAddItem(item,1))return;
        var backup=new ZPackage();inventory.Save(backup);
        try
        {
            if(!inventory.AddItem(item))return;
            fishView.GetZDO().Set("helmsman_net_caught",true);
        }
        catch(Exception error)
        {
            inventory.Load(new ZPackage(backup.GetArray()),false);
            fishView.GetZDO().Set("helmsman_net_caught",false);
            Plugin.Instance.Error(error);nextCatch=Time.time+5;return;
        }
        // The caught marker prevents another trigger adding this fish if destruction
        // must wait for the normal network cleanup.
        fishView.Destroy();nextCatch=Time.time+1;
    }
}

public sealed class NetInteraction : MonoBehaviour,Hoverable,Interactable
{
    public ShipFishingNet Net=null!;
    public string GetHoverName()=>"Fishing net";
    public float GetHoverOffset()=>0;
    public string GetHoverText()=>Localization.instance.Localize("Fishing net\n[<color=yellow><b>$KEY_Use</b></color>] "+(Net.Active?"Stow net":"Lower net"));
    public bool Interact(Humanoid user,bool hold,bool alt)
    {if(hold||user!=Player.m_localPlayer||Player.m_localPlayer.GetStandingOnShip()!=GetComponentInParent<Ship>())return false;Net.Request();return true;}
    public bool UseItem(Humanoid user,ItemDrop.ItemData item)=>false;
}
