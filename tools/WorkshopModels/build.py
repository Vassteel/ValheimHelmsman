"""Original workshop, slipway and supply review models. No imported meshes or textures.
Run with the project's bpy Python. Review assets only: no live prefab changes.
"""
import os
if hasattr(os,"sched_getaffinity"):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,bmesh,runpy,math,json,random,sys
import numpy as np
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];OUT=R/'design/workshop-supplies-v1';OUT.mkdir(exist_ok=True)
env=runpy.run_path(str(R/'design/small-boat-expansion-v1/primitives.py'))
box,tube,mesh,mat,move=[env[n] for n in ['box','tube','mesh','mat','move']];col=env['ship'];scene=env['scene'];studio=env['studio']
wood=env['oak'];edge=env['endgrain'];boards=env['deckm'];iron=env['iron'];rope=env['rope'];tar=env['rope_dark']
linen=mat('Undyed sail canvas',(.59,.52,.37));blue=mat('Woad pigment',(.095,.21,.29));red=mat('Red ochre pigment',(.39,.12,.058));yellow=mat('Yellow ochre pigment',(.58,.37,.085));leather=mat('Oiled leather',(.20,.09,.042));brass=mat('Worn bronze buckle',(.38,.25,.09),.48);clay=mat('Fired grey clay',(.28,.32,.28));ceramic=mat('Blue green stoneware',(.09,.26,.25));oil=mat('Amber fish oil',(.47,.24,.047));resin=mat('Amber resin',(.45,.23,.04));fishskin=mat('Dried fish skin',(.27,.29,.22));fishbelly=mat('Dried fish belly',(.49,.42,.28));darkeye=mat('Fish eyes',(.045,.043,.035))
for m in bpy.data.materials:
 if m.use_nodes:
  for n in m.node_tree.nodes:
   if n.type=='BUMP':n.inputs['Strength'].default_value=.08;n.inputs['Distance'].default_value=.004

def rotate_new(before,center,angle):
 for o in set(col.objects)-before:
  o.location=Vector(center);o.rotation_euler.z=angle

def ring(name,c,rx,ry,r,m,segments=48):
 return tube(name,[(c[0]+rx*math.cos(i*math.tau/segments),c[1]+ry*math.sin(i*math.tau/segments),c[2]) for i in range(segments+1)],r,m,6)
def beam(name,a,b,w,d,m):
 mid=(Vector(a)+Vector(b))*.5;ob=box(name,(0,0,0),(w,d,(Vector(b)-Vector(a)).length),m,.008);ob.location=mid;ob.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler();return ob
def ellipsoid(name,c,s,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=c);o=move(bpy.context.object);o.name=name;o.scale=s;o.data.materials.append(m);return o

def hank(c=(0,0,0),scale=1,hanging=False):
 # Continuous irregular oval turns with tails; bindings cross the actual bundle.
 x,y,z=c
 for layer in range(2):
  for j in range(6):
   points=[]
   for i in range(57):
    t=i*math.tau/56;r=.19+j*.017;px=r*math.cos(t)*(1+.025*math.sin(3*t+j));py=(.085+j*.013)*math.sin(t)
    points.append((x+px*scale,y+py*scale,z+(.02+layer*.028+.003*math.sin(2*t+j))*scale))
   tube('Laid rope / coil turn',points,.011*scale,rope,6)
 for side in [-1,1]:
  for j in range(3):
   a=side*.13+j*.017
   tube('Coil binding',[(x+a*scale,y-.18*scale,z+.06*scale),(x+a*scale,y+.18*scale,z+.06*scale)],.006*scale,tar,5)
 tube('Working rope tail',[(x+.26*scale,y,z+.023*scale),(x+.32*scale,y-.07*scale,z+.013*scale),(x+.27*scale,y-.25*scale,z+.012*scale),(x+.16*scale,y-.28*scale,z+.012*scale)],.011*scale,rope,6)

def timber(sealed=False):
 for layer in range(2):
  for j in range(3):
   z=.055+layer*.105;x=-.18+j*.18
   ob=box('Adzed sealed plank' if sealed else 'Resin treated timber',(x,0,z),(.171,.88-j*.045,.098),boards[(j+layer)%4],.008)
   if sealed:
    box('Black caulking groove',(x,0,z+.0495),(.016,.81-j*.045,.004),tar,.001)
    for yy in [-.30,.28]:box('Square treenail',(x+.04,yy,z+.051),(.027,.027,.009),edge,.002)
   else:
    for k in range(3):
     box('Resin coating streak',(x-.05+k*.046,.04*(k-1),z+.050),(.025,.32+.065*k,.003),resin,.001)
 for yy in [-.25,.24]:
  for j in range(3):
   y=yy+j*.014
   tube('Timber bundle lashing',[(-.282,y,.021),(-.282,y,.219),(.282,y,.219),(.282,y,.021),(-.282,y,.021)],.006,rope,6)
 # Clearly supported knot and tucked tail on the top.
 ring('Timber lashing knot',(.23,.268,.229),.035,.018,.006,rope,20)

def canvas():
 for layer in range(5):
  z=.014+layer*.021
  box('Folded canvas layer',(0,.012*(layer%2),z),(.51,.40,.019),linen,.008)
  tube('Canvas fold piping',[(-.252,-.177,z+.008),(.25,-.177,z+.008),(.252,.19,z+.008)],.004,linen,5)
 # Stitched reinforced edge and a short partially opened flap.
 for i in range(15):box('Sail seam stitch',(-.227+i*.032,-.157,.112),(.012,.014,.002),tar,.0003)
 box('Canvas reinforcement strip',(0,-.127,.115),(.5,.034,.006),env['bale_mat'],.002)
 for x in [-.20,.20]:ring('Bronze sail eyelet',(x,-.127,.120),.017,.017,.004,brass,16)
 for y in [-.04,.105]:tube('Folded canvas tie',[(-.27,y,.015),(-.27,y,.13),(.27,y,.13),(.27,y,.015)],.007,rope,6)

def bottle(wind=False,c=(0,0,0),scale=1):
 x,y,z=c;m=ceramic if wind else clay
 profile=[(0,.075),(.025,.100),(.07,.118),(.235,.118),(.285,.078),(.30,.043),(.345,.043),(.35,.053)]
 vs=[];N=16
 for h,r in profile:
  for j in range(N):a=j*math.tau/N;vs.append((x+r*math.cos(a)*scale,y+r*math.sin(a)*scale,z+h*scale))
 faces=[tuple(range(N-1,-1,-1))]+[(i*N+j,i*N+(j+1)%N,(i+1)*N+(j+1)%N,(i+1)*N+j) for i in range(len(profile)-1) for j in range(N)]+[tuple((len(profile)-1)*N+j for j in range(N))]
 mesh('Wind extract / stoneware' if wind else 'Fish oil / stoppered crock',vs,faces,m)
 tube('Cork stopper',[(x,y,z+.34*scale),(x,y,z+.386*scale)],.039*scale,edge,10)
 for h in [.305,.320]:ring('Bottle neck cord',(x,y,z+h*scale),.05*scale,.05*scale,.006*scale,rope,24)
 # Different bands and embossed symbols distinguish equal-named consumables.
 ring('Extract identity band',(x,y,z+.13*scale),.120*scale,.120*scale,.011*scale,blue if wind else oil,32)
 if wind:
  for h in [.18,.215,.25]:tube('Raised wind curl',[(x-.061*scale,y-.112*scale,z+h*scale),(x+.035*scale,y-.12*scale,z+h*scale),(x+.055*scale,y-.11*scale,z+(h+.018)*scale)],.005*scale,linen,5)
 else:
  ellipsoid('Fish mark body',(x,y-.120*scale,z+.22*scale),(.039*scale,.008*scale,.019*scale),linen)
  mesh('Fish mark tail',[(x+.025*scale,y-.123*scale,z+.22*scale),(x+.06*scale,y-.113*scale,z+.241*scale),(x+.06*scale,y-.113*scale,z+.199*scale)],[(0,1,2)],linen)

def belt():
 N=64;vs=[]
 for i in range(N):
  a=i*math.tau/N;r=.22+.012*math.sin(3*a)
  for radius,h in [(r-.009,.014),(r+.009,.014),(r+.009,.085),(r-.009,.085)]:vs.append((radius*math.cos(a),radius*math.sin(a),h))
 mesh('Supple leather wind belt',vs,[(4*i+j,4*((i+1)%N)+j,4*((i+1)%N)+(j+1)%4,4*i+(j+1)%4) for i in range(N) for j in range(4)],leather)
 for h in [.025,.074]:
  for i in range(40):a=i*math.tau/40;tube('Belt edge stitching',[(.233*math.cos(a),.233*math.sin(a),h),(.233*math.cos(a+.045),.233*math.sin(a+.045),h)],.0025,linen,5)
 tube('Bronze buckle frame',[(-.067,-.252,.02),(-.067,-.252,.092),(.067,-.252,.092),(.067,-.252,.02),(-.067,-.252,.02)],.009,brass,8)
 tube('Buckle tongue',[(0,-.263,.024),(0,-.263,.083)],.005,brass,6)
 ellipsoid('Ruby clasp',(.0,-.274,.056),(.017,.012,.018),red)
 for x in [-.09,.09]:ellipsoid('Amber pearl setting',(x,-.211,.067),(.020,.020,.020),yellow)
 box('Belt free end',(.17,-.25,.023),(.26,.068,.025),leather,.009)
 for i in range(4):box('Belt punched hole',(.16+i*.042,-.251,.037),(.010,.012,.002),tar,.002)

def fish(c,angle=0,length=.36):
 before=set(col.objects)
 ellipsoid('Dried catch body',(0,0,.035),(length*.38,.051,.033),fishskin)
 ellipsoid('Dried catch belly',(0,-.020,.043),(length*.28,.034,.018),fishbelly)
 mesh('Split fish tail',[(length*.28,0,.028),(length*.5,-.065,.021),(length*.45,0,.030),(length*.5,.065,.021)],[(0,1,2),(0,2,3)],fishskin)
 for side in [-1,1]:ellipsoid('Fish eye',(-length*.28,side*.027,.052),(.008,.006,.007),darkeye)
 for i in range(6):tube('Fish skin scoring',[(-.06+i*.027,-.033,.055),(-.045+i*.027,.020,.061)],.002,fishbelly,4)
 rotate_new(before,c,angle)

def basket():
 N=32
 box('Basket bottom',(0,0,.016),(.37,.29,.024),rope,.035)
 # Alternating wicker over/under weave follows a tapered oval, not a solid bucket.
 for j in range(N):
  a=j*math.tau/N;pts=[]
  for k in range(10):h=.025+k*.023;rx=.19+h*.19;ry=.15+h*.19;ripple=.002*(-1)**k;pts.append(((rx+ripple)*math.cos(a),(ry+ripple)*math.sin(a),h))
  tube('Basket upright stake',pts,.006,rope,5)
 for k in range(10):
  h=.028+k*.023;pts=[]
  for j in range(N*2+1):a=j*math.tau/(N*2);ripple=.003*math.cos(j*math.pi/2+k*math.pi);pts.append(((.19+h*.19+ripple)*math.cos(a),(.15+h*.19+ripple)*math.sin(a),h))
  tube('Woven willow band',pts,.006,edge,5)
 for h in [.246,.258]:ring('Basket bound rim',(0,0,h),.241,.20,.012,rope,48)
 for side in [-1,1]:tube('Basket handle',[(side*.236,-.08,.25),(side*.28,-.065,.35),(side*.28,.065,.35),(side*.236,.08,.25)],.015,rope,8)
 for i,(x,y,a) in enumerate([(-.015,-.09,.1),(.015,0,-.18),(-.025,.085,.23),(.025,-.035,.5)]):fish((x,y,.20+i*.017),a,.36)

def bench():
 # Original joinery with pegged mortises, solid top, clear central bird area.
 for x in [-1.00,1.00]:
  for y in [-.36,.36]:
   beam('Splayed trestle leg',(x*1.05,y*1.08,.015),(x,y,.965),.14,.16,wood)
  beam('Trestle crossbar',(x,-.45,.30),(x,.45,.30),.13,.13,wood)
 beam('Long lower stretcher',(-1.03,0,.28),(1.03,0,.28),.14,.15,wood)
 for x in [-1.06,1.06]:
  box('Through tenon',(x,0,.28),(.19,.11,.12),edge,.006)
  tube('Tenon locking peg',[(x,-.075,.21),(x,-.075,.36)],.019,wood,6)
 for y in [-.355,.355]:beam('Top apron',(-1.1,y,.865),(1.1,y,.865),.13,.18,wood)
 for i in range(5):box('Planed working top',(0,-.438+i*.219,.984),(2.50,.212,.142),boards[i%4],.009)
 # Top remains exactly 1.055m to match the puffin's existing placement probe.
 for x in [-1.02,1.02]:
  for y in [-.37,.37]:tube('Bench top peg',[(x,y,1.045),(x,y,1.060)],.020,edge,8)
 for i in range(4):box('Lower storage shelf',(-.08,-.29+i*.19,.38),(1.75,.18,.07),boards[i],.006)
 # Working vise at one end; screw and sliding tommy bar have visible supports.
 box('Wooden face vise',(-.92,-.635,.915),(.36,.15,.27),wood,.015)
 tube('Vise screw',[(-.92,-.56,.91),(-.92,-.79,.91)],.033,wood,10)
 tube('Vise tommy bar',[(-1.025,-.81,.82),(-.815,-.81,1.0)],.016,edge,8)
 for x in [-1.025,-.815]:ellipsoid('Vise handle stop',(x,-.81,.82 if x<-.9 else 1.0),(.025,.022,.025),wood)
 # Left tray keeps tools away from the puffin's central/right walking area.
 box('Tool tray base',(-.87,-.07,1.07),(.52,.46,.025),boards[2],.004)
 for x in [-1.13,-.61]:box('Tray side',(x,-.07,1.105),(.023,.47,.082),wood,.004)
 for y in [-.30,.16]:box('Tray end',(-.87,y,1.105),(.52,.023,.082),wood,.004)
 tube('Caulking iron',[(-1.04,-.15,1.09),(-.75,-.15,1.09)],.012,iron,6)
 tube('Caulking handle',[(-.92,-.15,1.09),(-.75,-.15,1.09)],.024,wood,8)
 # Supported stored rope, canvas and lumber under the bench.
 before=set(col.objects);hank(scale=.9);rotate_new(before,(-.53,-.02,.417),.18)
 before=set(col.objects);canvas();rotate_new(before,(.37,-.06,.42),-.08)
 for i in range(3):box('Shelf offcut',(.53,.1,.58+i*.036),(.40,.14,.03),boards[i],.004)

def small_table(width=.9,depth=.6,height=.75):
 for x in [-width*.4,width*.4]:
  for y in [-depth*.37,depth*.37]:beam('Upgrade table leg',(x,y,.015),(x,y,height-.045),.075,.075,wood)
 for j in range(3):box('Upgrade table top',(0,-depth/3+j*depth/3,height),(width,depth/3-.006,.07),boards[j],.007)
 for y in [-depth*.37,depth*.37]:beam('Upgrade lower brace',(-width*.4,y,.20),(width*.4,y,.20),.06,.07,wood)

def tools_upgrade():
 small_table(.93,.60,.75)
 for x in [-.38,.38]:beam('Tool rack upright',(x,.23,.72),(x,.23,1.46),.06,.06,wood)
 for z in [1.08,1.37]:box('Tool hanging rail',(0,.24,z),(.94,.05,.16),boards[1],.006)
 for x in [-.32,-.1,.12,.34]:tube('Tool support pin',[(x,.21,1.39),(x,.13,1.39)],.013,edge,6)
 for x,h in [(-.31,1.18),(-.09,1.14),(.32,1.17)]:
  tube('Hanging tool grip',[(x,.12,h),(x,.12,1.40)],.020,wood,8)
  box('Hanging tool iron head',(x,.12,h),(.08,.042,.10),iron,.008)
 tube('Saw wooden bow',[(-.30,.11,1.05),(-.30,.11,.9),(.26,.11,.9),(.26,.11,1.05)],.022,wood,8)
 box('Saw blade',(0,.1,.99),(.53,.014,.028),iron,.001)
 for i in range(22):mesh('Saw tooth',[( -.25+i*.024,.1,.986),(-.238+i*.024,.1,.967),(-.23+i*.024,.1,.986)],[(0,1,2)],iron)
 box('Sharpening stone',(.25,-.08,.80),(.21,.13,.045),clay,.012)
 box('Plane sole',(-.19,-.08,.82),(.35,.12,.09),wood,.015)
 beam('Plane wedge',(-.18,-.07,.85),(-.12,-.07,.94),.043,.050,edge)
 tube('Bench mallet handle',[(-.3,-.23,.808),(.05,-.23,.808)],.019,wood,8)
 box('Bench mallet head',(-.30,-.23,.82),(.09,.17,.08),edge,.012)

def caulking_upgrade():
 small_table(1.05,.66,.78)
 # Demonstration of two planks with packed oakum and a tar seam.
 for j in range(2):box('Caulking plank sample',(-.19,-.05+j*.13,.852),(.57,.121,.072),boards[j],.006)
 tube('Packed oakum seam',[(-.46,.015,.891),(.09,.015,.891)],.008,rope,6)
 tube('Fresh pitch seam',[(-.46,.015,.899),(-.16,.015,.899)],.006,tar,6)
 # Pitch pot has a rim, inset contents and a resting lid.
 tube('Pitch pot body',[(.32,.10,.819),(.32,.10,1.03)],.115,iron,16)
 tube('Pitch surface',[(.32,.10,1.025),(.32,.10,1.03)],.096,tar,16)
 ring('Pitch pot lip',(.32,.10,1.042),.113,.113,.008,iron,32)
 tube('Pitch pot handle',[(.22,.10,.99),(.20,.10,1.14),(.43,.10,1.14),(.43,.10,.99)],.010,iron,8)
 tube('Caulking iron',[(-.32,-.23,.835),(-.03,-.23,.835)],.010,iron,6)
 tube('Caulking iron grip',[(-.14,-.23,.835),(-.03,-.23,.835)],.023,wood,8)
 before=set(col.objects);hank(scale=.65);rotate_new(before,(-.06,.0,.07),.08)

def rigging_upgrade():
 # A working rigger's station: sewn canvas, rigged purchase, belayed hanks and tools.
 for x in [-.63,.63]:
  box('Rigging rack foot',(x,.08,.07),(.18,.80,.14),wood,.008)
  beam('Rigging rack upright',(x,.08,.10),(x,.08,1.72),.10,.10,wood)
  beam('Rigging rack brace',(x,-.24,.14),(x,.08,.70),.065,.065,wood)
 beam('Rigging rack crossbeam',(-.74,.08,1.72),(.74,.08,1.72),.14,.14,wood)
 beam('Lower belaying rail',(-.63,.08,.94),(.63,.08,.94),.085,.09,wood)
 for x in [-.63,.63]:
  for z in [.94,1.70]:tube('Rack joint treenail',[(x,-.001,z),(x,.17,z)],.021,edge,8)
 for j in range(3):box('Rigger work shelf',(0,-.11+j*.13,.43),(1.30,.123,.07),boards[j],.005)
 for x in [-.54,.54]:beam('Shelf knee',(x,.08,.21),(x,-.20,.41),.07,.07,wood)
 # Asymmetric sailmaking panel with its own edge rope, grommets and patch stitches.
 def cloth_point(u,v):return (-.51+u*.53,.065+.030*math.sin(u*math.pi)*math.sin(v*math.pi),1.045+v*.54)
 vs=[cloth_point(i/10,j/10) for j in range(11) for i in range(11)]
 panel=mesh('Sewn sail sample',vs,[(j*11+i,j*11+i+1,(j+1)*11+i+1,(j+1)*11+i) for j in range(10) for i in range(10)],linen)
 solid=panel.modifiers.new('Canvas thickness','SOLIDIFY');solid.thickness=.004
 for u in [0,1]:tube('Sail boltrope',[cloth_point(u,j/16) for j in range(17)],.009,rope,6)
 for v in [0,1]:tube('Sail boltrope',[cloth_point(j/16,v) for j in range(17)],.009,rope,6)
 for i in range(4):
  x=-.47+i*.15
  tube('Laced brass sail eye',[(x+.014*math.cos(t*math.tau/16),.051,1.56+.014*math.sin(t*math.tau/16)) for t in range(17)],.003,brass,6)
  tube('Sail head lacing',[(x,.041,1.56),(x,-.005,1.79),(x,.17,1.79),(x,.106,1.57)],.006,rope,6)
 box('Sail repair patch',(-.33,.052,1.21),(.18,.006,.15),env['bale_mat'],.005)
 for i in range(8):
  x=-.406+i*.022
  for z in [1.145,1.275]:tube('Patch edge stitch',[(x,.046,z-.005),(x+.009,.046,z+.005)],.0018,tar,5)
 for i in range(6):
  z=1.157+i*.022
  for x in [-.410,-.250]:tube('Patch side stitch',[(x-.004,.046,z),(x+.004,.046,z+.008)],.0018,tar,5)
 tube('Panel sewn seam',[(-.16,.045,1.055),(-.16,.045,1.577)],.003,rope,5)
 # Two real two-sheave blocks, with a continuous rope reeved through both.
 def block(z):
  for y in [-.035,.13]:ellipsoid('Hardwood pulley cheek',(.35,y,z),(.10,.023,.125),wood)
  tube('Pulley axle',[(.35,-.074,z),(.35,.16,z)],.018,iron,8)
  for y in [.019,.074]:
   tube('Pulley sheave',[(.35+.057*math.cos(t*math.tau/24),y,z+.057*math.sin(t*math.tau/24)) for t in range(25)],.012,edge,8)
  ring('Pulley attachment eye',(.35,.049,z+.145),.027,.030,.008,iron,20)
 purchase_before=set(col.objects)
 block(1.39);block(.985)
 points=[(.282,.019,1.08)]
 for y in [.019,.074]:
  points.append((.282,y,1.39))
  points += [(.35+.068*math.cos(math.pi-t*math.pi/20),y,1.39+.068*math.sin(math.pi-t*math.pi/20)) for t in range(1,21)]
  points.append((.418,y,.985))
  points += [(.35+.068*math.cos(-t*math.pi/20),y,.985+.068*math.sin(-t*math.pi/20)) for t in range(1,21)]
 points += [(.265,.16,1.39),(.52,-.10,.945)]
 tube('Reeved hemp purchase',points,.006,rope,6)
 tube('Lower block seized becket',[(.282,.019,1.08),(.32,.019,1.08)],.007,iron,6)
 for j in range(3):ring('Purchase dead-end whipping',(.282,.019,1.09+j*.012),.012,.012,.003,tar,12)
 tube('Practice lifting hook',[(.35,.049,.875),(.35,.049,.79),(.395,.049,.76),(.425,.049,.79),(.425,.049,.82)],.010,iron,8)
 for ob in set(col.objects)-purchase_before:ob.location.y-=.20
 tube('Upper block strop',[(.35,-.152,1.54),(.35,-.015,1.79),(.35,.17,1.79),(.35,-.14,1.54)],.009,rope,6)
 # Rope hangs on visible belaying pins instead of hovering around the frame.
 for x,material in [(-.43,rope),(-.10,tar),(.53,rope)]:
  tube('Belaying pin',[(x,.14,.935),(x,-.25,.935)],.016,edge,8)
  ellipsoid('Belaying pin head',(x,-.25,.935),(.024,.023,.025),wood)
  for j in range(5):
   rx=.045+j*.008;rz=.14+j*.004
   tube('Hanging rope hank',[(x+rx*math.cos(t*math.tau/40)+.007*math.sin(t*math.tau/20+j),-.218-j*.005+.007*math.sin(t*math.tau/40+j),.758+rz*math.sin(t*math.tau/40)+.005*math.sin(t*math.tau/10+j)) for t in range(41)],.006,material,6)
  tube('Hank suspension loop',[(x-.025,-.24,.893),(x-.025,-.20,.952),(x+.025,-.20,.952),(x+.025,-.24,.893)],.006,material,6)
  for j in range(3):
   tube('Hank throat seizing',[(x-.06,-.25,.874+j*.008),(x+.06,-.25,.874+j*.008)],.003,rope,5)
  tube('Hank tucked tail',[(x+.04,-.24,.873),(x+.09,-.26,.70),(x+.065,-.26,.62)],.006,material,6)
 # Sail thread reels, fid and marlinespike are supported by the work shelf.
 for x in [.10,.34]:
  for z in [.477,.618]:tube('Thread reel flange',[(x,.06,z),(x,.06,z+.017)],.065,wood,12)
  tube('Thread reel core',[(x,.06,.49),(x,.06,.62)],.034,edge,12)
  for j in range(9):ring('Wound sail thread',(x,.06,.50+j*.012),.047,.047,.006,linen,24)
 tube('Wooden splicing fid',[(-.31,-.17,.480),(-.025,-.17,.480)],.014,edge,8,r2=.002)
 tube('Marlinespike grip',[(-.32,-.055,.482),(-.23,-.055,.482)],.016,wood,8)
 tube('Steel marlinespike',[(-.23,-.055,.482),(-.065,-.055,.482)],.009,iron,8,r2=.001)


def paint_pot(x,y,base,pigment):
 # Closed bottom and hollow wall: paint sits below the open lip, never through a cap.
 n=20;profile=[(.0,.062),(.013,.070),(.12,.075),(.135,.075),(.135,.065),(.018,.061)]
 vs=[(x+r*math.cos(i*math.tau/n),y+r*math.sin(i*math.tau/n),base+z) for z,r in profile for i in range(n)]
 faces=[tuple(range(n-1,-1,-1))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(profile)-1) for i in range(n)]+[tuple((len(profile)-1)*n+i for i in range(n))]
 mesh('Hollow pigment pot',vs,faces,clay)
 tube('Inset pigment',[(x,y,base+.118),(x,y,base+.121)],.064,pigment,20)
 ring('Rounded pot lip',(x,y,base+.135),.070,.070,.005,clay,32)
 # Old paint has run down the front from the lip.
 for angle,length in [(-1.4,.054),(-1.9,.028)]:
  px=x+.074*math.cos(angle);py=y+.074*math.sin(angle)
  tube('Dried paint drip',[(px,py,base+.132),(px,py,base+.132-length)],.0035,pigment,6)
 for side in [-1,1]:tube('Pot side lug',[(x+side*.070,y,base+.105),(x+side*.086,y,base+.093),(x+side*.084,y,base+.063),(x+side*.070,y,base+.062)],.006,clay,6)


def paint_upgrade():
 small_table(.98,.64,.76)
 for x in [-.43,.43]:beam('Paint rack post',(x,.27,.72),(x,.27,1.76),.065,.065,wood)
 for z in [1.08,1.76]:box('Paint shelf',(0,.24,z),(1.00,.23,.046),boards[1],.005)
 box("Paint sample hanging rail",(0,.24,1.694),(1.00,.06,.10),wood,.005)
 for i,m in enumerate([red,yellow,blue]):
  x=-.30+i*.30
  # Bases rest exactly on the tabletop and middle shelf. Pots sit fully on each.
  paint_pot(x+[.012,-.019,.006][i],.22+[.012,-.025,.005][i],.795,m)
  paint_pot(x+[-.010,.021,-.004][i],.24+[.005,.022,-.015][i],1.103,m)
 for x,m in [(-.30,linen),(0,blue),(.30,red)]:
  # Sample hems are above the tallest pot lip, with depth clearance as well.
  cloth_before=set(col.objects)
  box('Ship cloth sample',(x,.115,1.594),(.21,.010,.24),m,.008)
  tube('Sample hanging loop',[(x-.013,.109,1.708),(x-.013,.103,1.726),(x+.013,.103,1.726),(x+.013,.109,1.708)],.003,rope,5)
  from mathutils import Matrix
  turn=Matrix.Translation((x,.115,1.708))@Matrix.Rotation({-.30:-.07,0:.035,.30:-.045}[x],4,'Y')@Matrix.Translation((-x,-.115,-1.708))
  bpy.context.view_layer.update()
  for ob in set(col.objects)-cloth_before:ob.matrix_world=turn@ob.matrix_world
  tube('Sample peg',[(x,.24,1.708),(x,.07,1.708)],.011,wood,6)
 for i in range(3):
  brush_before=set(col.objects)
  tube('Paint brush',[( -.28+i*.13,-.13,.809),(-.28+i*.13,-.02,.809)],.010,wood,6)
  box('Brush bristles',(-.28+i*.13,-.17,.807),(.032,.073,.024),linen,.004)
  turn=Matrix.Translation((-.28+i*.13,-.10,.795))@Matrix.Rotation([-.17,.12,-.07][i],4,'Z')@Matrix.Translation((.28-i*.13,.10,-.795))
  bpy.context.view_layer.update()
  for ob in set(col.objects)-brush_before:ob.matrix_world=turn@ob.matrix_world
 box('Painted timber sample',(.27,-.12,.812),(.28,.24,.034),boards[1],.005)
 for j,m in enumerate([blue,red,yellow]):box('Paint test stripe',(.27,-.19+j*.07,.830),(.25,.040,.004),m,.001)

def slipway():
 # Landward end Y=-10; launch end Y=10. Whole assembly is raised at shore on export.
 # Empty markers are authoring data; runtime must create Piece snappoint tags.
 def snap(name,pos,role):
  o=bpy.data.objects.new('snap_'+name,None);col.objects.link(o);o.location=pos
  o.empty_display_type='PLAIN_AXES';o.empty_display_size=.18
  o['purpose']=role;o['runtime_tag']='snappoint'
 def rail_z(y):return .90-(y+10)*.025
 for y in range(-10,11,2):
  box('Slipway cross sleeper',(0,y,.12),(8.0,.30,.24),wood,.02)
  for x in [-3,3]:
   beam('Platform support',(x,y,.24),(x,y,1.08),.22,.22,wood)
   snap(f'support_{x}_{y}',(x,y,0),'support')
  for x in [-4,4]:snap(f'side_{x}_{y}',(x,y,1.2),'floor_dock_stair')
 for x in [-3,3]:
  for i in range(80):
   y=-9.875+i*.25
   box('Side working platform',(x,y,1.12),(2,.242,.16),boards[i%4],.008)
  for y in range(-9,10,2):
   beam('Platform longitudinal brace',(x,y-.85,.30),(x,y+.85,1.04),.12,.12,wood)
 for x in [-1.25,1.25]:
  beam('Greased launch runner',(x,-10,rail_z(-10)),(x,10,rail_z(10)),.25,.25,wood)
  beam('Runner wearing strip',(x,-10,rail_z(-10)+.13),(x,10,rail_z(10)+.13),.19,.018,tar)
 for y in [-8,-4,0,4]:
  z=rail_z(y)+.28
  box('Movable cradle crossbeam',(0,y,z),(3.5,.32,.26),wood,.015)
  box('Keel bearing block',(0,y,z+.25),(.48,.36,.26),edge,.014)
  for x in [-1.5,1.5]:
   beam('Cradle angled hull prop',(x,y,z+.13),(x*.8,y,z+.85),.16,.18,wood)
   box('Cradle hull cushion',(x*.8,y,z+.88),(.42,.36,.14),leather,.02)
   tube('Cradle securing peg',[(x,y-.21,z),(x,y+.21,z)],.032,edge,8)
 # Landward head walkway joins both side platforms without crossing launch exit.
 for x in range(-4,5,2):snap(f'head_{x}',(x,-12,1.2),'floor_dock_stair')
 for i in range(8):box('Head platform boards',(0,-11.875+i*.25,1.12),(8,.242,.16),boards[i%4],.008)
 for x in [-3,-1,1,3]:
  box('Head bearer',(x,-11,.65),(.22,2,.90),wood,.01)
  snap(f'head_support_{x}',(x,-11,.2),'support')
 # Capstan and brake on the landward platform; rope travels through a fairlead.
 tube('Capstan base',[(0,-11,1.21),(0,-11,1.38)],.40,wood,16)
 tube('Capstan drum',[(0,-11,1.38),(0,-11,2.03)],.19,wood,12)
 for z in [1.4,1.9]:ring('Capstan iron band',(0,-11,z),.20,.20,.02,iron,32)
 for i in range(5):ring('Capstan rope winding',(0,-11,1.49+i*.038),.215,.215,.017,rope,32)
 beam('Capstan turning bar',(-1.2,-11,2.0),(1.2,-11,2.0),.075,.075,wood)
 tube('Cradle haul line',[(.215,-11,1.6),(.28,-10.4,1.25),(0,-9,1.16),(0,-8.2,rail_z(-8)+.45)],.020,rope,8)
 for x in [-3,3]:
  for y in [-9,1,7]:
   box('Mooring cleat foot',(x,y,1.24),(.15,.28,.08),wood,.008)
   beam('Mooring cleat horn',(x,y-.24,1.40),(x,y+.24,1.40),.075,.075,wood)
 before=set(col.objects);hank(scale=1.4);rotate_new(before,(-2.8,-10.8,1.21),.2)

MODELS=[('slipway','Shipwright slipway',slipway),('workbench',"Puffin's Construction Bench",bench),('tool-rack','Shipwright tools / construction speed',tools_upgrade),('caulking-station','Caulking station / hull durability',caulking_upgrade),('rigging-rack','Rigging rack / ship speed',rigging_upgrade),('paint-stand','Paint stand / paint and decoration',paint_upgrade),('resin-wood','Resin wood',lambda:timber(False)),('caulked-wood','Caulked wood',lambda:timber(True)),('sail-canvas','Sail canvas',canvas),('marine-rope','Marine rope',hank),('wind-belt','Wind belt',belt),('fish-oil','Fish extract / carrying',lambda:bottle(False)),('wind-extract','Fish extract / wind',lambda:bottle(True)),('dried-fish-basket','Dried fish basket',basket)]
manifest=json.loads((OUT/"models.json").read_text()) if (OUT/"models.json").exists() else []
for key,label,make in MODELS:
 if len(sys.argv)>1 and key not in sys.argv[1:]:continue
 manifest=[m for m in manifest if m["id"]!=key]
 for ob in list(col.objects):bpy.data.objects.remove(ob,do_unlink=True)
 make()
 from wear import apply as apply_wear
 apply_wear(key,globals())
 for ob in col.objects:
  if key=='slipway' and ob.type=='EMPTY':
   ob.location.z+=(10-ob.location.y)*.075
  if ob.type!='MESH':continue
  bpy.context.view_layer.objects.active=ob
  for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
  if key=='slipway':
   # Raise the shore end 1.5m, keeping the launch end and horizontal snap grid fixed.
   inverse=ob.matrix_world.inverted()
   for v in ob.data.vertices:
    world=ob.matrix_world@v.co;world.z+=(10-world.y)*.075;v.co=inverse@world
  bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
 points=np.array([ob.matrix_world@v.co for ob in col.objects if ob.type=='MESH' for v in ob.data.vertices]);lo=points.min(0);hi=points.max(0)
 # Stage geometry in separate files; none modifies the installed bundle.
 dest=OUT/key;dest.mkdir(exist_ok=True)
 bpy.ops.object.select_all(action='DESELECT')
 for ob in col.objects:ob.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(dest/f'{key}.glb'),export_format='GLB',use_selection=True,export_apply=True)
 bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(dest/f'{key}.blend'))
 tri=sum(sum(len(p.vertices)-2 for p in ob.data.polygons) for ob in col.objects if ob.type=='MESH')
 manifest.append(dict(id=key,name=label,dimensions_m=(hi-lo).tolist(),bounds_min=lo.tolist(),bounds_max=hi.tolist(),triangles=tri,original_geometry=True,snap_points=[dict(name=o.name,blender_position=list(o.location),unity_position=[o.location.x,o.location.z,-o.location.y],purpose=o.get('purpose'),runtime_tag=o.get('runtime_tag')) for o in col.objects if o.type=='EMPTY' and o.name.startswith('snap_')]))
 print('BUILT',key,tri,flush=True)
manifest.sort(key=lambda m:next(i for i,p in enumerate(MODELS) if p[0]==m['id']))
(OUT/'models.json').write_text(json.dumps(manifest,indent=2)+'\n')
