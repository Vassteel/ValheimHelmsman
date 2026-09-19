using System;
using Helmsman.Core;
using UnityEngine;
namespace Helmsman;

[Serializable]
public sealed class SlipwayOrder
{
    public ConstructionOrder order=new();
    public long launchStarted,launchPaused,launchPauseTicks;
    public long crewCalledAt,constructionStarted;
    public long supportPaused,supportPauseTicks;
    public bool awaitingMaterials;
    public string supplied="";
    internal long CrewIdentity=>crewCalledAt>0?crewCalledAt:order.started;
    internal bool AwaitingCrew=>!awaitingMaterials&&PuffinCrewPlan.WaitingForCrew(crewCalledAt,constructionStarted);
    internal double Elapsed(long now)=>awaitingMaterials?0:PuffinCrewPlan.ConstructionElapsed(crewCalledAt,constructionStarted,order.started,now-supportPauseTicks-(supportPaused>0?Math.Max(0,now-supportPaused):0),order.duration);
    public string bench="";
    public Vector3 stage;
    public float hullBonus=1,sailBonus=1;
    internal bool Valid=>order!=null&&order.Valid&&ConstructionFunding.Valid(order.recipe,supplied)&&crewCalledAt>=0&&constructionStarted>=0&&(constructionStarted==0||constructionStarted>=crewCalledAt)&&launchStarted>=0&&launchPaused>=0&&launchPauseTicks>=0&&supportPaused>=0&&supportPauseTicks>=0&&Finite(stage.x)&&Finite(stage.y)&&Finite(stage.z)&&hullBonus>=1&&hullBonus<=1.15f&&sailBonus>=1&&sailBonus<=1.1f;
    private static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
}
