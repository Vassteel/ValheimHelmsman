using System;
using Helmsman.Core;
using UnityEngine;
namespace Helmsman;

[Serializable]
public sealed class ConstructionOrder
{
    public string blueprint="", recipe="", name="";
    public long started,creator;
    public float duration;
    // Missing on older saved orders, which were always paid.
    public bool freeBuild;
    public Vector3 position;
    public float heading;
    internal bool Valid=>ShipConstruction.Find(blueprint)!=null && ShipConstruction.ValidDuration(duration) && started>=0 &&
        Finite(position.x) && Finite(position.y) && Finite(position.z) && Finite(heading) && name!=null && name.Length<=48 && ShipConstruction.ValidRecipe(blueprint,recipe);
    private static bool Finite(float n)=>!float.IsInfinity(n)&&!float.IsNaN(n);
}
