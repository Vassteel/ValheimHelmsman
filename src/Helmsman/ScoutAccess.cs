using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Helmsman;

// Check the requesting character, not the server's local player.
internal static class ScoutAccess
{
    private static readonly Func<PrivateArea,bool> Enabled=AccessTools.MethodDelegate<Func<PrivateArea,bool>>(AccessTools.Method(typeof(PrivateArea),"IsEnabled"));
    private static readonly Func<PrivateArea,Vector3,float,bool> Inside=AccessTools.MethodDelegate<Func<PrivateArea,Vector3,float,bool>>(AccessTools.Method(typeof(PrivateArea),"IsInside"));
    private static readonly Func<PrivateArea,long,bool> Permitted=AccessTools.MethodDelegate<Func<PrivateArea,long,bool>>(AccessTools.Method(typeof(PrivateArea),"IsPermitted"));
    private static List<PrivateArea> Areas=>AccessTools.StaticFieldRefAccess<List<PrivateArea>>(typeof(PrivateArea),"m_allAreas");
    internal static bool Allowed(long owner,Vector3 point)
    {
        foreach(var area in Areas)
            if(area&&Enabled(area)&&Inside(area,point,0)&&area.GetComponent<Piece>().GetCreator()!=owner&&!Permitted(area,owner))return false;
        return true;
    }
}
