using System;
using System.Reflection;
using Helmsman;
public class ItemDrop {public class ItemData {}}
namespace Helmsman
{
    internal static class QuartermasterBridge
    {
        internal static bool Available=true,Done;
        internal static int Begins,Steps,Cancels;
        internal static string Invalid="";
        internal static string Check(Ship ship)=>Invalid;
        internal static object BeginUnload(Ship ship){Begins++;return new object();}
        internal static ItemDrop.ItemData? UnloadNext(object token){Steps++;return new();}
        internal static bool Finished(object token)=>Done;
        internal static string Status(object token)=>"Unloaded";
        internal static void Cancel(object token)=>Cancels++;
    }
}
internal static class CargoTests
{
    internal static void Run(Action<bool,string> check)
    {
        void Tick(CargoOrder order)=>typeof(CargoOrder).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(order,null);
        Ship ship=null!;GullGuide gull=null!;
        void Reset()
        {
            Plugin.Instance=new();Plugin.LocalSession=true;Player.m_localPlayer=new();ship=new();gull=new(){Ship=ship,Landed=true};
            QuartermasterBridge.Available=true;QuartermasterBridge.Done=false;QuartermasterBridge.Invalid="";
            QuartermasterBridge.Begins=QuartermasterBridge.Steps=QuartermasterBridge.Cancels=0;
        }
        Reset();check(Plugin.Instance.Cargo==null&&QuartermasterBridge.Begins==0,"arriving in range creates no cargo order");
        QuartermasterBridge.Available=false;
        check(!CargoOrder.Start(ship,gull,out _)&&QuartermasterBridge.Begins==0,"missing Quartermaster rejects cargo request");
        Reset();gull.Landed=false;check(!CargoOrder.Start(ship,gull,out _),"gull must land before cargo request");
        Reset();QuartermasterBridge.Invalid="No base";check(!CargoOrder.Start(ship,gull,out _),"out-of-range request rejected before creating job");
        Reset();check(CargoOrder.Start(ship,gull,out _)&&QuartermasterBridge.Begins==1,"explicit request creates one job");
        var order=Plugin.Instance.Cargo;
        check(!CargoOrder.Start(ship,gull,out _)&&QuartermasterBridge.Begins==1,"duplicate requests cannot create competing jobs");
        Tick(order);check(QuartermasterBridge.Steps==1&&gull.Sorts==1,"successful slot starts gull sorting");
        Tick(order);Tick(order);check(QuartermasterBridge.Steps==1,"next slot waits while gull completes throws");
        gull.SortingCargo=false;Tick(order);check(QuartermasterBridge.Steps==2&&gull.Sorts==2,"completed throws permit next slot");
        order.Stop("Cancelled");check(!Plugin.Instance.Cargo&&gull.Clears==1&&QuartermasterBridge.Cancels==1,"cancel clears animation, props and job");
        Tick(order);order.Stop("Again");check(QuartermasterBridge.Steps==2&&QuartermasterBridge.Cancels==1,"stopped job never resumes or cancels twice");
        foreach(var cause in new[]{"mod","gull","aboard","player","dead","range","helm"})
        {
            Reset();CargoOrder.Start(ship,gull,out _);order=Plugin.Instance.Cargo;Tick(order);
            switch(cause)
            {
                case "mod":QuartermasterBridge.Available=false;break;
                case "gull":gull.Landed=false;break;
                case "aboard":ship.Aboard=false;break;
                case "player":Player.m_localPlayer=new();break;
                case "dead":Player.m_localPlayer.Dead=true;break;
                default:QuartermasterBridge.Invalid=cause;break;
            }
            Tick(order);check(!Plugin.Instance.Cargo&&!gull.SortingCargo&&QuartermasterBridge.Steps==1,"stop even during animation when "+cause+" changes");
        }
        Reset();CargoOrder.Start(ship,gull,out _);order=Plugin.Instance.Cargo;Tick(order);QuartermasterBridge.Done=true;
        Tick(order);check(Plugin.Instance.Cargo==order,"final slot finishes its visual throws before completing");
        gull.SortingCargo=false;Tick(order);check(!Plugin.Instance.Cargo&&gull.Clears==0,"normal completion lets thrown props finish fading");
    }
}
