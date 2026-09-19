using System;
using System.Collections.Generic;
using System.Linq;

namespace Helmsman.Core;

public sealed class ShipBlueprint
{
    public readonly string Id, Name, Source, Recipe;
    public readonly int BuildSeconds;
    public readonly bool HasSail;
    public bool UsesSlipway => Id is "hercule" or "merchant" or "longship" or "big_cargo" or "warship";
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
        new ShipBlueprint("hercule", "Falkuša fishing boat", "HerculeShip", "DeerHide:10,RoundLog:30,Resin:30,BronzeNails:100,LeatherScraps:10", 360),
        new ShipBlueprint("merchant", "Ottar", "MercantShip", "DeerHide:20,RoundLog:40,Resin:40,IronNails:100,LeatherScraps:10", 480),
        new ShipBlueprint("longship", "Longship", "VikingShip", ShipwrightRules.Hull.Recipe, 720),
        new ShipBlueprint("big_cargo", "Big cargo ship", "BigCargoShip", "DeerHide:20,FineWood:50,Resin:50,Coal:50,IronNails:150,LeatherScraps:20", 900),
        new ShipBlueprint("warship", "Snekkja", "WarShip", "DeerHide:20,FineWood:50,Resin:50,Coal:50,IronNails:120,LeatherScraps:10", 1080),
    });
    public static ShipBlueprint? Find(string id) => Blueprints.FirstOrDefault(b=>b.Id==id);
    // Accept only known historical costs so paid orders can finish or refund exactly
    // what was charged before the temporary switch to vanilla materials.
    public static bool ValidRecipe(string id, string recipe)
    {
        var plan=Find(id);
        if(plan==null)return false;
        if(recipe==plan.Recipe)return true;
        return id switch {
            "hercule" => recipe=="ClothShip:2,ResinWood:30,BronzeNails:100,ShipRope:2",
            "merchant" => recipe=="ClothShip:4,ResinWood:40,IronNails:100,ShipRope:2",
            "big_cargo" => recipe=="ClothShip:4,CaulkedWood:50,IronNails:150,ShipRope:4",
            "warship" => recipe=="ClothShip:4,CaulkedWood:50,IronNails:120,ShipRope:2",
            _ => false
        };
    }
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
