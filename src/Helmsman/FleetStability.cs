using UnityEngine;

namespace Helmsman;

// Native righting impulses grow with beam cubed (arm * immersion squared).
// A donor ship's fixed sail lever can overwhelm a narrower replacement hull.
internal static class FleetStability
{
    internal static float SailLever(float beam)=>Mathf.Clamp(beam*beam*beam*.005f,.015f,.22f);
    internal static void Apply(Ship ship)
    {
        ship.m_sailForceOffset=Mathf.Min(ship.m_sailForceOffset,SailLever(ship.m_floatCollider.size.x));
        ship.m_angularDamping=Mathf.Max(ship.m_angularDamping,.12f);
    }
}
