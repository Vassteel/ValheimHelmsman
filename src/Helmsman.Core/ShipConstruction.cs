using System;
using System.Collections.Generic;
using System.Linq;

namespace Helmsman.Core;

public sealed class ShipBlueprint
{
    public readonly string Id, Name, Source, Recipe;
    public readonly int BuildSeconds;
    public readonly bool HasSail;
    public string Prefab => Source;
    public ShipBlueprint(string id, string name, string source, string recipe, int seconds, bool sails = true)
    { Id=id; Name=name; Source=source; Recipe=recipe; BuildSeconds=seconds; HasSail=sails; }
}

public enum ShipwrightTask { Idle, Hammer, Chisel, Stitch, AwaitingLaunch }

public static class ShipConstruction
{
    // Preserve original prefab identities so existing world ships remain loadable.
    // Full player fleet; autonomous enemy ships and weapon systems are deferred.
    public static readonly IReadOnlyList<ShipBlueprint> Blueprints = Array.AsReadOnly(new[] {
        new ShipBlueprint("canoe", "Rowing canoe", "RowingCanoe", "Resin:8,Wood:20", 120, false),
        new ShipBlueprint("double_canoe", "Double rowing canoe", "DoubleRowingCanoe", "Resin:10,Wood:25", 180, false),
        new ShipBlueprint("little_boat", "Ceol", "LittleBoat", "LeatherScraps:8,Wood:40", 240),
        new ShipBlueprint("currach", "Currach", "HelmsmanCurrach", "DeerHide:10,Wood:25,Resin:10", 180),
        new ShipBlueprint("hercule", "Falkuša fishing boat", "HerculeShip", "ClothShip:2,ResinWood:30,BronzeNails:100,ShipRope:2", 360),
        new ShipBlueprint("merchant", "Ottar", "MercantShip", "ClothShip:4,ResinWood:40,IronNails:100,ShipRope:2", 480),
        new ShipBlueprint("cargo", "Cargo ship", "CargoShip", "ClothShip:4,ResinWood:40,IronNails:100,ShipRope:2", 600),
        new ShipBlueprint("longship", "Longship", "VikingShip", ShipwrightRules.Hull.Recipe, 720),
        new ShipBlueprint("fast_skuldelev", "Fast ship Skuldelev", "FastShipSkuldelev", "ClothShip:4,CaulkedWood:40,IronNails:100,ShipRope:2", 780),
        new ShipBlueprint("big_cargo", "Big cargo ship", "BigCargoShip", "ClothShip:4,CaulkedWood:50,IronNails:150,ShipRope:4", 900),
        new ShipBlueprint("warship", "Snekkja", "WarShip", "ClothShip:4,CaulkedWood:50,IronNails:120,ShipRope:2", 1080),
        new ShipBlueprint("skuldelev", "War ship Skuldelev", "Skuldelev", "ClothShip:4,CaulkedWood:60,IronNails:160,ShipRope:4", 1140),
        new ShipBlueprint("caravel", "Cargo caravel", "CargoCaravel", "ClothShip:5,CaulkedWood:60,IronNails:120,ShipRope:4", 1200),
        new ShipBlueprint("goblin", "Goblin boat", "GoblinShip", "ClothShip:5,CaulkedWood:50,IronNails:120,ShipRope:5", 1260),
        new ShipBlueprint("taurus", "Taurus war ship", "TaurusWarShip", "ClothShip:4,CaulkedWood:60,IronNails:180,ShipRope:4", 1380),
        new ShipBlueprint("animals", "Animal transport ship", "CargoAnimalShip", "ClothShip:4,CaulkedWood:50,IronNails:150,ShipRope:6", 1560),
        new ShipBlueprint("huge_cargo", "Huge cargo ship", "HugeCargoShip", "ClothShip:4,CaulkedWood:50,IronNails:150,ShipRope:6", 1800)
    });
    public static ShipBlueprint? Find(string id) => Blueprints.FirstOrDefault(b=>b.Id==id);
    public static bool ValidDuration(double seconds) => !double.IsNaN(seconds) && !double.IsInfinity(seconds) && seconds>=30 && seconds<=86400;
    public static double Elapsed(long started, long now, double duration)
    {
        if(started<0 || now<=started || !ValidDuration(duration)) return 0;
        // Convert before subtraction, so corrupted timestamps cannot wrap signed arithmetic.
        return Math.Min(duration, ((double)now-started)/TimeSpan.TicksPerSecond);
    }
    public static ShipwrightTask Task(bool hasSail, double elapsed, double duration)
    {
        if(!ValidDuration(duration) || double.IsNaN(elapsed) || elapsed<0) return ShipwrightTask.Idle;
        if(elapsed>=duration) return ShipwrightTask.AwaitingLaunch;
        // Short repeating work sessions keep even long builds visibly alive.
        int phase=(int)((elapsed%36)/12);
        return phase==0 ? ShipwrightTask.Hammer : phase==1 || !hasSail ? ShipwrightTask.Chisel : ShipwrightTask.Stitch;
    }
}
