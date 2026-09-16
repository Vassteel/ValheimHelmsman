using System;
using System.Collections.Generic;
using System.Linq;

namespace Helmsman.Core;

public sealed class ShipUpgrade
{
    public readonly string Id, Name, Recipe, Station, Requires;
    public readonly int Level;
    public ShipUpgrade(string id,string name,string recipe,string station,int level,string requires="")
    {Id=id;Name=name;Recipe=recipe;Station=station;Level=level;Requires=requires;}
}

public static class ShipwrightRules
{
    public static readonly ShipUpgrade Hull = new("longship","Commission longship","IronNails:100,DeerHide:10,FineWood:40,ElderBark:40","$piece_workbench",1);
    public static readonly ShipUpgrade[] Upgrades = {
        new("lantern","Deck lantern","SurtlingCore:3,BronzeNails:10,FineWood:6,Chain:1","$piece_forge",4),
        new("canopy","Sheltering canopy","JuteRed:2,LoxPelt:2,LinenThread:6,RoundLog:2","$piece_workbench",4),
        new("cargo1","Cargo hold I — 7 × 3","ElderBark:20,Silver:5,Obsidian:10","$piece_workbench",4),
        new("cargo2","Cargo hold II — 7 × 4","BlackMetal:20,YggdrasilWood:20,FineWood:20","$piece_artisanstation",1,"cargo1"),
        new("treatment","Fire / Ashlands treatment","CeramicPlate:20,Tar:20,YggdrasilWood:20,IronNails:50","$piece_blackforge",3),
        new("trophy","Decorative trophy mount","FineWood:4,BronzeNails:10","$piece_workbench",1)
    };
    public static Dictionary<string,int> Costs(string recipe)
    {
        var result=new Dictionary<string,int>(StringComparer.Ordinal);
        foreach(var entry in recipe.Split(','))
        {
            var pair=entry.Trim().Split(':');
            if(pair.Length!=2 || string.IsNullOrWhiteSpace(pair[0]) || !int.TryParse(pair[1],out var n) || n<=0 || n>100000)
                throw new ArgumentException("Invalid material cost: "+entry);
            result[pair[0]]=checked((result.TryGetValue(pair[0],out var previous)?previous:0)+n);
        }
        if(result.Count==0)throw new ArgumentException("A paid build needs materials.");
        return result;
    }
    public static string Check(ShipUpgrade upgrade,Func<string,bool> built)
    {
        if(built(upgrade.Id))return "Already fitted.";
        if(upgrade.Requires.Length>0 && !built(upgrade.Requires))return "Fit Cargo hold I first.";
        return "";
    }
    public static (int Width,int Height) Cargo(bool first,bool second,int width,int height)
        => (Math.Max(width,first||second?7:6),Math.Max(height,second?4:3));
    public static bool CanPay(IReadOnlyDictionary<string,int> costs,Func<string,int> available)
        => costs.All(pair=>available(pair.Key)>=pair.Value);
}
