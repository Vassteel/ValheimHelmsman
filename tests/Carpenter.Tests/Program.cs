using System.Reflection;
using Helmsman;
using Helmsman.Core;
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
var table=new CraftingStation{gameObject="CarpentersTable(Clone)"};var bench=new CraftingStation{gameObject="piece_workbench"};
CarpenterRecipes.Configure(table);
Check(!table.m_upgrader&&!table.m_showBasicRecipies,"Table cannot act as a universal upgrader or basic crafting station");
Recipe Make(string name,CraftingStation station)=>new(){m_item=new(){gameObject=name},m_craftingStation=station};
var relevant=HarborCatalog.Items.Where(i=>i.Recipe.Length>0).Select(i=>Make(i.Prefab,table)).ToList();
foreach(var recipe in relevant)Check(CarpenterRecipes.Allows(recipe),"Registered Helmsman supply remains craftable");
var all=relevant.Concat(new[]{Make("Hammer",null),Make("AxeIron",bench),Make("OtherModItem",table),Make("ResinWood",bench),Make("DriedFishBasket",table),new Recipe(),null}).ToList();
var hook=typeof(CarpenterRecipeList).GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic);
var filtered=all.ToList();hook.Invoke(null,new object[]{new Player{Station=table},filtered});
Check(filtered.SequenceEqual(relevant),"No-cost all-recipe list reduces to Helmsman table items only");
foreach(var station in new[]{bench,(CraftingStation)null})
{
 var normal=all.ToList();hook.Invoke(null,new object[]{new Player{Station=station},normal});
 Check(normal.SequenceEqual(all),"Workbench and hand-crafting lists remain unchanged");
}
var discovered=relevant.Take(2).ToList();hook.Invoke(null,new object[]{new Player{Station=table},discovered});
Check(discovered.Count==2,"Filtering does not grant undiscovered recipes");
Console.WriteLine($"PASS: {checks} carpenter recipe checks: station flags, all-recipes mode, unrelated recipes, other stations and discovery.");
