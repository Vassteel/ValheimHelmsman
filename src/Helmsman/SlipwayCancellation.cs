using Helmsman.Core;
using UnityEngine;
namespace Helmsman;

public sealed partial class Slipway
{
    internal string Cancel(Player player,Shipyard bench,long expectedStart)
    {
        if(!bench||!bench.Near(player)||!CanOrder(player))return "Return to the accessible puffin workshop.";
        return GetComponent<WorkstationLease>().Run(()=>
        {
            if(!bench||!bench.Near(player)||!CanOrder(player)||!view.IsOwner())return "Workshop access changed; order retained.";
            var state=Order;
            if(state==null||state.bench!=bench.Identity||state.order.started!=expectedStart)return "That ship order is no longer active.";
            if(state.launchStarted>0||view.GetZDO().GetZDOID(Receipt)!=ZDOID.None)return "Launch is already underway; this ship cannot be cancelled.";
            Refund();
            return state.order.name+" cancelled. Supplied materials returned beside the slipway.";
        });
    }
    private void Refund()
    {
        if(!Ready||!view.IsOwner()||view.GetZDO().GetZDOID(Receipt)!=ZDOID.None)return;
        var state=Order;if(state==null)return;
        var refund=ConstructionFunding.Refund(state.order.recipe,state.supplied,state.awaitingMaterials,state.order.freeBuild);
        // Clear before issuing any refund so a repeated cancellation cannot pay twice.
        view.GetZDO().Set(OrderKey,"");cachedRaw="";cachedOrder=null;
        ghost.Dispose();crew?.Tick(null,0);
        WorkshopMaterialSupply.Return(refund,transform.position+Vector3.up);

    }
}
