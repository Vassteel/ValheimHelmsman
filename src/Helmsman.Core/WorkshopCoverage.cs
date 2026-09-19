using System;
namespace Helmsman.Core;
public static class WorkshopCoverage
{
    public static bool Contains(float radius,float dx,float dy,float dz)
        =>radius>0&&!float.IsNaN(radius)&&!float.IsInfinity(radius)&&
            (double)dx*dx+(double)dy*dy+(double)dz*dz<=(double)radius*radius;
}
