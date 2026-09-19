using Helmsman;using UnityEngine;using System.Reflection;
int checks=0;void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
var input=typeof(TandemPaddleInput).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic)!;
var thrust=typeof(TandemPaddleThrust).GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic)!;
var rig=new PaddleCraftRig();var ship=new Ship();ship.Components[typeof(PaddleCraftRig)]=rig;var shipView=new ZNetView();var body=new Rigidbody();var player=new Player();var view=new ZNetView();player.Components[typeof(ZNetView)]=view;
var seat=new Vector3(0,.259f,1.05f);player.transform.position=seat;player.Anchor=new Transform{position=seat,Parent=rig};view.Data.Set("helmsman_paddle_ship",shipView.Data.m_uid);view.Data.Set("helmsman_paddle_seat",seat);
Vector3 Controls(Vector3 dir,bool jump=false){object[] args={player,dir,jump};input.Invoke(null,args);return (Vector3)args[1];}
void Tick(params Player[] crew)=>thrust.Invoke(null,new object[]{ship,.02f,crew.ToList(),shipView,body,new WaterVolume()});
Check(Controls(new(0,0,1)).sqrMagnitude==0,"W paddles without leaving the front chair");Check(TandemPaddling.Input(view.Data)==1,"Forward effort is replicated");
Tick(player);Check(body.Calls==1&&Math.Abs(body.Impulse.z-.78f)<.0001&&body.Impulse.x==0,"Front paddler adds 60% impulse without steering");
body.Calls=0;Tick(player,player);Check(body.Calls==1,"Duplicate occupant never doubles the boost");
Controls(Vector3.zero);body.Calls=0;Tick(player);Check(body.Calls==0,"Released W stops boost");
Controls(new(1,0,0));Check(TandemPaddling.Input(view.Data)==0,"Front A/D gives no propulsion or steering");
Controls(new(0,0,-1));Check(TandemPaddling.Input(view.Data)==-1,"Front S supports reverse paddling");
Controls(new(0,0,1));shipView.Owner=false;body.Calls=0;Tick(player);Check(body.Calls==0,"Only network owner adds force");shipView.Owner=true;
ZNet.instance.Now=ZNet.instance.Now.AddSeconds(3);Tick(player);Check(body.Calls==0,"Stale remote input expires");
Controls(new(0,0,1));Floating.Level=-10;Tick(player);Check(body.Calls==0,"Paddling on dry land adds no force");Floating.Level=1;
Controls(new(0,0,1),true);Check(TandemPaddling.Input(view.Data)==0,"Jump clears effort before normal dismount");
player.Anchor!.position=new(0,.259f,-1.35f);var rear=Controls(new(1,0,1));Check(rear.x==1&&rear.z==1,"Rear seat retains native steering and speed controls");
player.Anchor=null;Controls(new(0,0,1));Check(TandemPaddling.Input(view.Data)==0,"Detached players cannot contribute");
Console.WriteLine($"PASS: {checks} production tandem input and owner-physics checks.");
