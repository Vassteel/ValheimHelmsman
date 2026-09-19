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
        new HarborEntry("ShipConstruction","Keel cradle","FineWood:20,IronNails:10,ElderBark:10"),
        new HarborEntry("ShipConstruction1","Launching rollers","FineWood:20,IronNails:10,ElderBark:10"),
        new HarborEntry("ShipConstruction2","Ship framing gantry","FineWood:20,IronNails:10,ElderBark:10"),
        new HarborEntry("PierCrane1","Timber pier crane","Chain:4,Wood:40,RoundLog:20,IronNails:20"),
        new HarborEntry("PierCrane2","Braced pier crane","Iron:4,Wood:40,RoundLog:20,IronNails:20"),
        new HarborEntry("PulleyCobia","Single block","Wood:10,LeatherScraps:4"),
        new HarborEntry("PulleyElephantSeal","Double purchase","Wood:10,LeatherScraps:4"),
        new HarborEntry("PulleyMarlin","Heavy purchase","Wood:10,LeatherScraps:4"),
        new HarborEntry("FishingDock","Pelican fishing station","BronzeNails:20,Wood:20,RoundLog:4,Coins:250")
    });
    public static readonly IReadOnlyList<HarborEntry> Items=Array.AsReadOnly(new[]{
        // Retain existing inventory and paid-order refunds; crafting is deferred.
        new HarborEntry("ResinWood","Resin wood",amount:10),
        new HarborEntry("CaulkedWood","Caulked wood",amount:10),
        new HarborEntry("ClothShip","Sail canvas"),
        new HarborEntry("ShipRope","Marine rope"),
        new HarborEntry("WindBelt","Wind belt","Ruby:1,AmberPearl:4,DeerHide:5,Blueberries:2"),
        new HarborEntry("FishExtract","Fish extract"),
        new HarborEntry("FishExtract2","Fish extract"),
        new HarborEntry("DriedFishBasket","Dried fish basket")
    });
    public const string TableRecipe="Bronze:1,Wood:40,BronzeNails:20";
}
