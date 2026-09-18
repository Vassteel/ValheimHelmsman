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
    // Authored player fleet and the native longship. Superseded source ships are removed.
    public static readonly IReadOnlyList<ShipBlueprint> Blueprints = Array.AsReadOnly(new[] {
        new ShipBlueprint("dugout", "Dugout", "HelmsmanDugout", "Wood:12", 120, false),
        new ShipBlueprint("kayak", "Finewood Kayak", "HelmsmanFinewoodKayak", "FineWood:10,Wood:12,Resin:8", 150, false),
        new ShipBlueprint("tandem_kayak", "Tandem Finewood Kayak", "HelmsmanTandemKayak", "FineWood:16,Wood:16,Resin:10", 180, false),
        new ShipBlueprint("little_boat", "Ceol", "LittleBoat", "LeatherScraps:8,Wood:40", 240),
        new ShipBlueprint("currach", "Currach", "HelmsmanCurrach", "DeerHide:10,Wood:25,Resin:10", 180),
        new ShipBlueprint("hercule", "Falkuša fishing boat", "HerculeShip", "ClothShip:2,ResinWood:30,BronzeNails:100,ShipRope:2", 360),
        new ShipBlueprint("merchant", "Ottar", "MercantShip", "ClothShip:4,ResinWood:40,IronNails:100,ShipRope:2", 480),
        new ShipBlueprint("longship", "Longship", "VikingShip", ShipwrightRules.Hull.Recipe, 720),
        new ShipBlueprint("big_cargo", "Big cargo ship", "BigCargoShip", "ClothShip:4,CaulkedWood:50,IronNails:150,ShipRope:4", 900),
        new ShipBlueprint("warship", "Snekkja", "WarShip", "ClothShip:4,CaulkedWood:50,IronNails:120,ShipRope:2", 1080),
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
