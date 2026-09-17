using UnityEngine;

namespace Helmsman;

// Cosmetic, local-space stroll along the clear middle of the carpenter's bench.
internal sealed class PuffinBenchWalk
{
    private readonly Transform bird;
    private readonly Vector3 home,away;
    private float wait,phase,gait;
    private bool returning;
    internal PuffinBenchWalk(Transform bird)
    {
        this.bird=bird;home=bird.localPosition;away=home+Vector3.left*.6f;
        wait=Random.Range(8f,17f);
    }
    internal bool AtHome=>Vector3.Distance(bird.localPosition,home)<.001f && Quaternion.Angle(bird.localRotation,Quaternion.identity)<1;
    internal void Tick(VikingBirds.PerchedBird model,bool mayStroll,float delta)
    {
        delta=Mathf.Min(delta,.05f);
        if(!mayStroll){returning=true;wait=0;}
        bool moving=false;
        var target=returning?home:away;
        if(wait>0)wait-=delta;
        else
        {
            var direction=target-bird.localPosition;
            if(direction.sqrMagnitude>.000001f)
            {
                var facing=Quaternion.LookRotation(direction,Vector3.up);
                bird.localRotation=Quaternion.RotateTowards(bird.localRotation,facing,180*delta);
                if(Quaternion.Angle(bird.localRotation,facing)<8)
                {
                    bird.localPosition=Vector3.MoveTowards(bird.localPosition,target,.19f*delta);
                    moving=true;
                }
            }
            else if(returning)
            {
                bird.localRotation=Quaternion.RotateTowards(bird.localRotation,Quaternion.identity,180*delta);
                if(AtHome && mayStroll){returning=false;wait=Random.Range(14f,25f);}
            }
            else {returning=true;wait=Random.Range(1.5f,3f);}
        }
        gait=Mathf.MoveTowards(gait,moving?1:0,delta*7);
        if(moving)phase+=delta*13;
        model.Waddle(phase,gait);
    }
}
