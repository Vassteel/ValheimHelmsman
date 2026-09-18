using UnityEngine;
namespace Helmsman;

// Pure authored-rig geometry, also exercised without a Unity player.
internal static class FleetSailShape
{
    internal static Vector3 Anchor(string kind,Vector3 p,float top)
    {
        if(kind=="falkusa")
        {
            // Jib and main are on opposite sides of the mast, including their boltropes.
            return p.x>0?Project(p,new Vector3(.45f,1.95f,3.4f),new Vector3(.45f,8.65f,-3.25f)):
                Project(p,new Vector3(-.25f,1.48f,5.45f),new Vector3(-.22f,6,-.8f));
        }
        if(kind=="currach")return Project(p,new Vector3(.125f,4.4f,1.12f),new Vector3(.135f,.95f,1.12f));
        return new Vector3(p.x,top,p.z);
    }
    private static Vector3 Project(Vector3 p,Vector3 a,Vector3 b)
    {var line=b-a;return a+line*Mathf.Clamp01(Vector3.Dot(p-a,line)/line.sqrMagnitude);}
    internal static float Weight(string kind,Vector3 p,float top,float bottom,float halfWidth)
    {
        if(kind=="currach")return Triangle(p,new Vector2(1.12f,4.4f),new Vector2(-1.55f,.97f),new Vector2(1.12f,.95f));
        if(kind=="falkusa")return p.x>0?
            Triangle(p,new Vector2(3.4f,1.95f),new Vector2(-3.25f,8.65f),new Vector2(-3.45f,1.75f)):
            Triangle(p,new Vector2(5.45f,1.48f),new Vector2(-.8f,6),new Vector2(1.65f,1.5f));
        float u=Mathf.Clamp01(.5f+p.x/(2*halfWidth));float v=Mathf.Clamp01((p.y-bottom)/Mathf.Max(.1f,top-bottom));
        return Mathf.Sin(Mathf.PI*u)*Mathf.Sin(Mathf.PI*v);
    }
    private static float Triangle(Vector3 p,Vector2 a,Vector2 b,Vector2 c)
    {
        float d=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);
        float u=((b.y-c.y)*(p.z-c.x)+(c.x-b.x)*(p.y-c.y))/d;
        float v=((c.y-a.y)*(p.z-c.x)+(a.x-c.x)*(p.y-c.y))/d;
        return Mathf.Clamp01(27*Mathf.Max(0,u)*Mathf.Max(0,v)*Mathf.Max(0,1-u-v));
    }
}
