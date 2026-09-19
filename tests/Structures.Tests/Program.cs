using System;
using System.IO;
using System.Linq;
using Helmsman.Structures;
int checks=0;
void Check(bool valid,string name){checks++;if(!valid)throw new Exception(name);}
void Reject(string content,string name){bool rejected=false;try{Blueprint.Parse(content);}catch(FormatException){rejected=true;}Check(rejected,name);}
var b=Blueprint.Parse("wood_floor  .382683  .923879 1 2 3\r\nwood_floor  ,382683  ,923879 5 4 9\r\n");
Check(b.Pieces.Count==2,"Decimal variants and CRLF");
Check(b.Pieces[0].Qx==0&&b.Pieces[0].Qz==0,"Empty fields must remain quaternion zeros");
Check(b.Pieces[0].X==-2&&b.Pieces[0].Y==0&&b.Pieces[0].Z==-3,"Placement rebasing");
Check(b.Pieces[1].X==2&&b.Pieces[1].Y==2&&b.Pieces[1].Z==3,"Relative geometry preserved");
Reject("wood_floor  .5  .5 1 2 3","Invalid quaternion rejected");
Reject("wood_floor    1 NaN 2 3","NaN rejected");
Reject("wood_floor    1 Infinity 2 3","Infinity rejected");
Reject("wood_floor    1 1,000.5 2 3","Ambiguous locale rejected");
Reject("wood_floor 0 1 2 3","Missing fields rejected");
Reject("","Empty file rejected");
bool oversizedRejected=false;try{Blueprint.Parse("wood_floor    1 0 0 0\nwood_floor    1 170 0 0").ValidatePlacementSize();}catch(FormatException){oversizedRejected=true;}Check(oversizedRejected,"Oversized footprint rejected at placement validation");
// Missing scenery below and far outside the house must not affect its footprint.
var filtered=Blueprint.Parse("wood_floor    1 10 4 20\nBush01    1 500 -100 500\nwood_wall    1 12 6 24");
Check(filtered.RetainPieces(p=>p.Prefab!="Bush01")==1,"Unsupported scenery skipped");
filtered.ValidatePlacementSize();
Check(filtered.Pieces.Count==2&&filtered.Width==2&&filtered.Depth==4&&filtered.Height==2,"Only supported geometry sets placement limits");
Check(filtered.Pieces[0].Y==0&&filtered.Pieces[1].Y==2&&filtered.Pieces[1].X-filtered.Pieces[0].X==2,"Filtering preserves relative geometry and rebases foundations");
Check(filtered.Radius<6&&Geometry.Distance(new Point(0,0),filtered.Hull)==0,"Skipped scenery cannot enlarge terrain work");
Check(filtered.RetainPieces(p=>true)==0&&filtered.Pieces[0].Y==0,"Repeated filtering is stable");
var nothing=Blueprint.Parse("Bush01    1 0 0 0");
bool noSupported=false;try{nothing.RetainPieces(p=>false);}catch(FormatException){noSupported=true;}
Check(noSupported,"All unsupported file rejected before preview or terrain work");
var hull=Geometry.Hull(new[]{new Point(-5,-3),new Point(5,-3),new Point(5,3),new Point(-5,3),new Point(0,0),new Point(-5,-3)});
Check(hull.Length==4,"Hull ignores interior/duplicate points");
Check(Geometry.Distance(new Point(0,0),hull)==0,"Footprint interior flat");
Check(Math.Abs(Geometry.Distance(new Point(8,7),hull)-5)<.0001,"Rounded corner distance");
Check(Geometry.BlendWeight(0,10)==1&&Geometry.BlendWeight(10,10)==0,"Exact blend endpoints");
Check(Math.Abs(Geometry.BlendWeight(5,10)-.5)<.0001,"Mid-shoulder blending");
Check(Math.Abs(Geometry.FoundationHeight(4,10,0,10)-10.2f)<.001f,"Fill supports foundation with shallow overlap");
Check(Math.Abs(Geometry.FoundationHeight(14,10,0,10)-10.35f)<.001f,"Excavation leaves soil against foundation");
Check(Math.Abs(Geometry.FoundationHeight(10.28f,10,0,10)-10.28f)<.001f,"Small natural terrain variation preserved");
Check(Geometry.FoundationHeight(14,10,10,10)==14,"Contact adjustment leaves outer terrain untouched");
Check(Geometry.FoundationHeight(14,10,5,10)>Geometry.Height(14,10,5,10),"Shoulder excavation is reduced too");
Check(Math.Abs(Geometry.FoundationHeight(-8,-5,0,10)+4.8f)<.001f,"Ground contact handles negative world heights");
Check(Geometry.Rotate(0,-1)==337.5f,"Vanilla rotation wraps backwards");
Check(Geometry.Rotate(337.5f,1)==0,"Vanilla rotation wraps forwards");
float heading=0;for(int turn=0;turn<16;turn++){heading=Geometry.Rotate(heading,1);Check(heading==(turn+1)%16*22.5f,"Each vanilla rotation step matches");}
Check(Math.Abs(Geometry.FoundationHeight(0,6,0,12)-6.2f)<.001f,"Raised terrain plane supports long foundation posts");
Check(Geometry.FoundationHeight(0,6,12,12)==0,"Raised terrain still blends to original outer height");
Check(Geometry.Height(4,10,0,10)==10,"Foundation reaches selected height");
Check(Geometry.Height(4,10,10,10)==4,"Outer terrain unchanged");
Check(Geometry.Height(10,4,5,10)==7,"Excavation uses same blend");
float previous=1;
for(int i=0;i<=100;i++){float w=Geometry.BlendWeight(i*.1f,10);Check(w<=previous+.00001f&&w>=-.00001f&&w<=1.00001f,"Monotonic bounded shoulder");previous=w;}
Check(Math.Abs(Geometry.BlendWeight(.01f,10)-1)<.000001f,"Smooth foundation edge");
Check(Math.Abs(Geometry.BlendWeight(9.99f,10))<.000001f,"Smooth outer edge");
var bp=Blueprint.Parse("#Name:Test\n#SnapPoints\n9;8;7\n#Pieces\nwood_floor;Building;1;2;3;0;0;0;1;\"hello; world\";2;1;.5\n");
Check(bp.Pieces.Count==1&&bp.Pieces[0].Data=="hello; world","Blueprint headers and quoted semicolon");
Check(bp.Pieces[0].ScaleX==2&&bp.Pieces[0].ScaleZ==.5f,"Nonuniform blueprint scales");
var old=Blueprint.Parse("#Pieces\nwood_floor;Building;1;2;3;0;0;0;1;text\n");
Check(!old.Pieces[0].HasScale&&old.Pieces[0].Qw==1,"Legacy ten-field blueprint");
Check(ImportPaths.Normalize("/home/deck/Downloads/Valheim Buildings",true)==@"Z:\home\deck\Downloads\Valheim Buildings","Proton path normalization");
Check(ImportPaths.Normalize("/home/deck/Downloads/Valheim Buildings",false)=="/home/deck/Downloads/Valheim Buildings","Native Unix path preserved");
Check(ImportPaths.Normalize(@"S:\builds",true)==@"S:\builds","Windows path preserved");
Reject("#Pieces\nwood_floor;Building;0;0;0;0;0;0;1;;0;1;1","Zero blueprint scale rejected");
Check(PrefabPolicy.Rejection(true,false,false,false,false)==null,"Networked world props accepted without a Piece component");
Check(PrefabPolicy.Rejection(false,false,false,false,false)!=null,"Unnetworked props cannot silently disappear on reload");
Check(PrefabPolicy.Rejection(true,true,false,false,false)!=null,"Creatures excluded");
Check(PrefabPolicy.Rejection(true,false,true,false,false)!=null,"Creature spawners excluded");
Check(PrefabPolicy.Rejection(true,false,false,true,false)!=null,"Terrain-mutating objects excluded");
Check(PrefabPolicy.Rejection(true,false,false,false,true)!=null,"Loose inventory items excluded");
string folder=args.Length>0?args[0]:Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","Valheim Buildings");
int total=0;
UndoRegression.Run(Check);
InputRegression.Run(Check);
foreach(var file in (Directory.Exists(folder)?ImportPaths.Find(folder):Array.Empty<string>()))
{
 var build=Blueprint.Parse(File.ReadAllText(file));total+=build.Pieces.Count;
 Check(build.Hull.Length>=3&&build.Pieces.All(p=>p.Y>=0),Path.GetFileName(file));
 Console.WriteLine($"{Path.GetFileName(file)}: {build.Pieces.Count} pieces, {build.Hull.Length} footprint corners");
}
Console.WriteLine($"PASS: {checks} checks; {total} real building piece records parsed.");
