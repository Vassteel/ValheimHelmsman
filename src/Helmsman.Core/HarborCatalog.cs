using System;
using System.Collections.Generic;

namespace Helmsman.Core;

public sealed class HarborEntry
{
    public readonly string Prefab,Name,Recipe;
    public readonly int Amount;
    public HarborEntry(string prefab,string name,string recipe="",int amount=1)
    {Prefab=prefab;Name=name;Recipe=recipe;Amount=amount;}
}

// Original public content identifiers and recipe data. Enemy fleets and ammunition
// are intentionally not registered in this release. See assets/ships/SOURCES.md.
public static class HarborCatalog
{
    public static readonly IReadOnlyList<HarborEntry> Pieces=Array.AsReadOnly(new[]{
        new HarborEntry("ShipConstruction","Ship construction","FineWood:20,IronNails:10,ElderBark:10"),
        new HarborEntry("ShipConstruction1","Ship construction I","FineWood:20,IronNails:10,ElderBark:10"),
        new HarborEntry("ShipConstruction2","Ship construction II","FineWood:20,IronNails:10,ElderBark:10"),
        new HarborEntry("PierCrane1","Pier crane I","Chain:4,Wood:40,RoundLog:20,IronNails:20"),
        new HarborEntry("PierCrane2","Pier crane II","Iron:4,Wood:40,RoundLog:20,IronNails:20"),
        new HarborEntry("PulleyCobia","Cobia pulley","Wood:10,LeatherScraps:4"),
        new HarborEntry("PulleyElephantSeal","Elephant seal pulley","Wood:10,LeatherScraps:4"),
        new HarborEntry("PulleyMarlin","Blue marlin pulley","Wood:10,LeatherScraps:4"),
        new HarborEntry("Enguias","Eels","Wood:4,FishRaw:2"),
        new HarborEntry("Peixes","Fish dryer","Wood:4,FishRaw:2"),
        new HarborEntry("RedePesca","Fishing net","Wood:3,LeatherScraps:10"),
        new HarborEntry("Totem1","Totem I","RoundLog:2,Wood:2"),
        new HarborEntry("Totem2","Totem II","RoundLog:2"),
        new HarborEntry("Totem3","Totem III","RoundLog:2"),
        new HarborEntry("Totem4","Totem IV","Wood:8,TrophySkeleton:1"),
        new HarborEntry("OilPress","Oil extractor","Wood:20,Iron:2,IronNails:10"),
        new HarborEntry("FishingDock","Fishing dock","BronzeNails:20,Wood:20,RoundLog:4,Coins:250"),
        new HarborEntry("FishingDock_Extension","Dock extension","BronzeNails:20,Wood:20,RoundLog:4")
    });
    public static readonly IReadOnlyList<HarborEntry> Items=Array.AsReadOnly(new[]{
        new HarborEntry("ResinWood","Resin wood","RoundLog:10,Resin:10",10),
        new HarborEntry("CaulkedWood","Caulked wood","FineWood:10,Resin:10,Coal:10",10),
        new HarborEntry("ClothShip","Sail canvas","DeerHide:5"),
        new HarborEntry("ShipRope","Marine rope","LeatherScraps:5"),
        new HarborEntry("WindBelt","Wind belt","Ruby:1,AmberPearl:4,DeerHide:5,Blueberries:2"),
        new HarborEntry("FishExtract","Fish extract"),
        new HarborEntry("FishExtract2","Fish extract"),
        new HarborEntry("DriedFishBasket","Dried fish basket")
    });
    public const string TableRecipe="Bronze:1,Wood:40,BronzeNails:20";
}
