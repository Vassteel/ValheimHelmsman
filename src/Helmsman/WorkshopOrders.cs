using System;
using System.Collections.Generic;
using System.Linq;
using Helmsman.Core;
using UnityEngine;

namespace Helmsman;

internal sealed class WorkshopOrder
{
    internal string Id="",Name="",Status="";
    internal long Creator;
    internal Dictionary<string,int> Needed=new();
    internal Func<string> Cancel=null!;
}
internal static class WorkshopOrders
{
    internal static List<WorkshopOrder> At(Shipyard bench,Player player)
    {
        var result=new List<WorkshopOrder>();
        foreach(var slip in UnityEngine.Object.FindObjectsByType<Slipway>(FindObjectsSortMode.None))
        {
            var state=slip.Order;if(state==null||state.bench!=bench.Identity)continue;
            result.Add(new WorkshopOrder{
                Id="ship/"+slip.GetComponent<ZNetView>().GetZDO().m_uid+"/"+state.order.started,
                Name=state.order.name,Status=slip.GetHoverText(),Creator=state.order.creator,
                Needed=ShipwrightRules.Costs(state.order.recipe).ToDictionary(c=>c.Key,c=>state.order.freeBuild||!state.awaitingMaterials?0:ConstructionFunding.Remaining(state.order.recipe,state.supplied,c.Key)),
                Cancel=()=>slip.Cancel(player,bench,state.order.started)
            });
        }
        var structure=bench.GetComponent<Structures.StructureConstruction>();var order=structure?structure.Order:null;
        if(order!=null)
            result.Add(new WorkshopOrder{Id="structure/"+bench.Identity,Name=order.name,Status=structure!.Status,Creator=order.creator,Needed=structure.RemainingCosts(),
                Cancel=()=>bench.GetComponent<WorkstationLease>().Run(()=>ReferenceEquals(structure.Order,order)?structure.Cancel(player):"That structure order has changed; select it again.")});
        return result.OrderBy(o=>o.Id,StringComparer.Ordinal).ToList();
    }
}
