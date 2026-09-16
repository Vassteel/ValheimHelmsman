using System;
using BepInEx.Bootstrap;
using UnityEngine;

namespace Helmsman;

// Only game/BCL types cross this optional boundary; there is no Quartermaster assembly reference.
internal static class QuartermasterBridge
{
    private static Type? bound;
    internal static Func<Ship,string> Check=null!;
    internal static Func<Ship,bool> InRange=null!;
    internal static Func<Ship,object> BeginUnload=null!;
    internal static Func<object,ItemDrop.ItemData?> UnloadNext=null!;
    internal static Func<object,bool> Finished=null!;
    internal static Func<object,string> Status=null!;
    internal static Action<object> Cancel=null!;
    internal static Action<Transform,ItemDrop.ItemData,Vector3> Throw=null!;
    internal static Action<Transform> ClearThrows=null!;
    internal static bool Available
    {
        get
        {
            if(!Chainloader.PluginInfos.TryGetValue("local.valheim.quartermaster",out var info) || !info.Instance || !info.Instance.isActiveAndEnabled) return false;
            var type=info.Instance.GetType().Assembly.GetType("Quartermaster.HelmsmanCargoAccess");
            if(type==null) return false;
            try
            {
                if(bound!=type)
                {
                    T Bind<T>(string name) where T:Delegate => (T)Delegate.CreateDelegate(typeof(T),type.GetMethod(name)!);
                    Check=Bind<Func<Ship,string>>("Check");InRange=Bind<Func<Ship,bool>>("InRange");
                    BeginUnload=Bind<Func<Ship,object>>("BeginUnload");
                    UnloadNext=Bind<Func<object,ItemDrop.ItemData?>>("UnloadNext");Finished=Bind<Func<object,bool>>("Finished");
                    Status=Bind<Func<object,string>>("Status");Cancel=Bind<Action<object>>("Cancel");
                    Throw=Bind<Action<Transform,ItemDrop.ItemData,Vector3>>("Throw");ClearThrows=Bind<Action<Transform>>("ClearThrows");
                    bound=type;
                }
                return (bool)type.GetProperty("Available")!.GetValue(null);
            }
            catch(Exception) { bound=null;return false; }
        }
    }
}
