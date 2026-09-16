using Helmsman;
using Jotunn.Managers;
int checks=0;void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
GullcallWhistle.Register();var registered=ItemManager.Instance.Added;var config=registered.Config;
Check(config.CraftingStation=="" && config.MinStationLevel==0,"recipe requires no station");
var expected=new Dictionary<string,int>{{"BoneFragments",4},{"Wood",2},{"LeatherScraps",2},{"Feathers",2}};
Check(config.Requirements.Length==4 && config.Requirements.All(r=>expected.TryGetValue(r.Item,out var amount)&&amount==r.Amount),"only agreed Meadows/Black Forest ingredients");
Check(config.Amount==1 && config.StackSize==1 && config.Weight==.2f,"one reusable lightweight whistle");
Check(config.Icon && GullcallAssets.Attached==registered.ItemPrefab,"custom model and icon connected to registered item");
Check(!registered.ItemDrop.m_itemData.m_shared.m_useDurability && registered.ItemDrop.m_itemData.m_shared.m_itemType==ItemDrop.ItemData.ItemType.Misc,"whistle is not consumable or durability based");
var item=registered.ItemDrop.m_itemData;Player player=null;
void Reset(){Plugin.Instance=new();Plugin.LocalSession=true;player=Player.m_localPlayer=new();player.Inventory.Items.Add(item);InventoryGui.instance=new();}
Reset();Check(UseGullcallWhistle.Prefix(player,null,new ItemDrop.ItemData{m_dropPrefab=new(){name="Wood"}}),"unrelated item reaches vanilla use");
Check(UseGullcallWhistle.Prefix(player,null,null),"null item is not misidentified as whistle");
Check(!UseGullcallWhistle.Prefix(player,null,item)&&Plugin.Instance.UI.Opens==1&&InventoryGui.instance.Hides==1,"hotbar use opens orders and closes inventory");
Check(player.Inventory.ContainsItem(item)&&item.m_stack==1,"opening does not consume or remove whistle");
Check(!UseGullcallWhistle.Prefix(player,player.Inventory,item)&&Plugin.Instance.UI.Opens==2,"inventory use also opens orders");
foreach(var cause in new[]{"remote","nonplayer","dead","missing","container","no session","attack","dodge"})
{
 Reset();Humanoid actor=player;Inventory inventory=null;
 switch(cause){case "remote":actor=new Player();break;case "nonplayer":actor=new Humanoid();break;case "dead":player.Dead=true;break;case "missing":player.Inventory.Items.Clear();break;case "container":inventory=new Inventory();inventory.Items.Add(item);break;case "no session":Plugin.LocalSession=false;break;case "attack":player.Attacking=true;break;case "dodge":player.Dodging=true;break;}
 Check(!UseGullcallWhistle.Prefix(actor,inventory,item)&&Plugin.Instance.UI.Opens==0&&InventoryGui.instance.Hides==0,"blocked use has no menu/inventory side effects: "+cause);
 Check(item.m_stack==1,"blocked use does not consume whistle: "+cause);
}
Reset();Check(GullcallWhistle.Carried(player,item),"carried item authorizes menu");player.Inventory.Items.Clear();Check(!GullcallWhistle.Carried(player,item),"dropping/transferring whistle revokes menu and confirm action");
Console.WriteLine($"PASS: {checks} production whistle registration/use checks.");
