using Helmsman;
using UnityEngine;
int tests=0;void Check(bool value,string label){tests++;if(!value)throw new Exception(label);}
var player=new Player();Player.m_localPlayer=player;
var prefab=new GameObject{name="Wood"};var wood=new ItemDrop();wood.m_itemData.m_shared.m_name="$wood";prefab.component=wood;ObjectDB.instance.Items["Wood"]=prefab;
Container Chest(int n,int world=1){var c=new Container();c.components[typeof(ZNetView)]=new ZNetView();c.Items.Items.Add(new(){m_stack=n,m_worldLevel=world,m_shared=wood.m_itemData.m_shared});return c;}
var bench=new Shipyard();var storage=Chest(4);bench.components[typeof(Container)]=storage;
var qm=Chest(8);var wrongWorld=Chest(20,0);var locked=Chest(30);locked.Access=false;
QuartermasterWorkshop.Chests=new(){qm,qm,wrongWorld,locked,storage};player.Items=Chest(3).Items;
var counts=WorkshopMaterialSupply.Available(player,bench,1,new[]{"Wood"})["Wood"];
Check(counts==(8,4,3),"Ghost counts exclude locked/old-world stock, deduplicate Quartermaster and show the bench separately");
Check(qm.Items.Items[0].m_stack==8&&storage.Items.Items[0].m_stack==4,"Reading ghost requirements never moves or creates items");
Check(WorkshopMaterialSupply.Available(player,bench,999,new[]{"Wood"})["Wood"].carried==0,"Other players' carried materials are not offered");
string paid="Wood:2";int freeSaves=0;
WorkshopMaterialSupply.Collect(player,bench,"Wood:12",paid,1,s=>{paid=s;freeSaves++;},true);
Check(freeSaves==0&&paid=="Wood:2"&&qm.Items.Items[0].m_stack==8&&storage.Items.Items[0].m_stack==4&&player.Items.Items[0].m_stack==3,"No-cost construction leaves all supply sources and real escrow unchanged");
Check(Helmsman.Core.ConstructionFunding.Supplied(paid)["Wood"]==2,"No-cost cancellation can refund only the actual prior deposit");
paid="";WorkshopMaterialSupply.Collect(player,bench,"Wood:12","",1,s=>paid=s);
Check(paid=="Wood:12"&&qm.Items.Items.Count==0&&storage.Items.Items.Count==0&&player.Items.Items[0].m_stack==3,"Auto collection uses exactly the displayed eligible Quartermaster then bench sources");
Check(locked.Items.Items[0].m_stack==30&&wrongWorld.Items.Items[0].m_stack==20,"Inaccessible stock stays untouched");
void Reset(out Slipway slip,out WorkstationLease lease){slip=new();lease=new();slip.components[typeof(WorkstationLease)]=lease;ItemDrop.Dropped=0;ItemDrop.BeforeDrop=null;bench.Nearby=true;}
Reset(out var s,out var l);ItemDrop.BeforeDrop=()=>Check(s.Order==null,"Saved job cleared before any refund");
Check(s.Cancel(player,bench,42).Contains("cancelled"),"Selected ship cancellation reaches slipway job");
Check(s.Order==null&&s.ghost.Cleared==1&&s.crew.Leaving==1&&ItemDrop.Dropped==7,"Cancellation clears ghost, dismisses crew and returns only supplied materials");
s.Cancel(player,bench,42);Check(ItemDrop.Dropped==7&&s.ghost.Cleared==1,"Repeated cancel cannot duplicate refunds");
Reset(out s,out l);s.State.awaitingMaterials=false;s.State.supplied="Wood:20";s.Cancel(player,bench,42);Check(ItemDrop.Dropped==20,"Funded ship refunds its paid recipe");
Reset(out s,out l);s.State.order.freeBuild=true;s.Cancel(player,bench,42);Check(s.Order==null&&ItemDrop.Dropped==0,"Free ship cancels without generating resources");
Reset(out s,out l);s.State.launchStarted=10;s.Cancel(player,bench,42);Check(s.Order!=null&&ItemDrop.Dropped==0,"Launch in progress cannot be refunded");
Reset(out s,out l);s.view.Data.Receipt=new();s.Cancel(player,bench,42);Check(s.Order!=null,"Spawned ship receipt prevents cancellation");
Reset(out s,out l);s.State.bench="other";s.Cancel(player,bench,42);Check(s.Order!=null,"Different workshop's job is untouched");
Reset(out s,out l);l.Delay=true;s.Cancel(player,bench,42);s.State.order.started=43;l.Pending();Check(s.Order!=null,"Delayed ownership response cannot cancel replacement job");
Reset(out s,out l);l.Delay=true;s.Cancel(player,bench,42);bench.Nearby=false;l.Pending();Check(s.Order!=null,"Walking away invalidates a pending cancellation");
Console.WriteLine($"PASS: {tests} production workshop resource and ship cancellation checks.");
