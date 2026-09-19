"""Original timber construction and lifting gear; review models for the next asset group."""
from pathlib import Path
import sys
R=Path(__file__).resolve().parents[2]
source=R/'tools/WorkshopModels/build.py'
ns={'__file__':str(source)}
exec(compile(source.read_text().split('MODELS=')[0],str(source),'exec'),ns)
globals().update({k:v for k,v in ns.items() if k!='__file__'})
OUT=R/'design/harbor-refinement-v3';OUT.mkdir(exist_ok=True)

# Bounded handmade variation stays in the finish and loose gear, not the load path.
weather=[mat('Weathered harbour timber '+str(i),c,grain=True) for i,c in enumerate([(.27,.215,.145),(.31,.25,.17),(.25,.225,.175)])]
from mathutils import Matrix

def hank(c=(0,0,0),scale=1,hanging=False):
 x,y,z=c
 for layer in range(2):
  for j in range(5):
   pts=[]
   for i in range(81):
    t=i*math.tau/80;rx=.19+j*.019;ry=.083+j*.015
    pts.append((x+scale*(rx*math.cos(t)+.012*math.sin(3*t+j*.4)),y+scale*(ry*math.sin(t)+.012*math.cos(2*t+j*.7)),z+scale*(.014+layer*.024+.003*math.sin(2*t+j))))
   tube('Loose irregular rope coil',pts,.011*scale,rope,8)
 for xx in [-.155,.17]:
  for k in range(2):
   tube('Coil twine binding',[(x+(xx+k*.011)*scale,y-.155*scale,z+.065*scale),(x+(xx+k*.011)*scale,y+.155*scale,z+.065*scale),(x+(xx+k*.011)*scale,y+.162*scale,z+.007*scale),(x+(xx+k*.011)*scale,y-.161*scale,z+.007*scale),(x+(xx+k*.011)*scale,y-.155*scale,z+.065*scale)],.005*scale,rope,6)
 tube('Loose working end',[(x+.26*scale,y-.015*scale,z+.012*scale),(x+.35*scale,y-.10*scale,z+.012*scale),(x+.28*scale,y-.25*scale,z+.012*scale),(x+.14*scale,y-.29*scale,z+.012*scale),(x+.09*scale,y-.23*scale,z+.012*scale)],.011*scale,rope,8)
 for i in range(3):ring('Coil tying knot',(x-.155*scale,y-.16*scale,z+.04*scale+i*.006*scale),.015*scale,.012*scale,.005*scale,rope,16)

def end_checks(ob):
 # Two shallow checks cut into the end grain of a long beam, in its own local space.
 coords=[v.co for v in ob.data.vertices];lo=Vector(tuple(min(v[i] for v in coords) for i in range(3)));hi=Vector(tuple(max(v[i] for v in coords) for i in range(3)))
 ext=hi-lo;axis=max(range(3),key=lambda i:ext[i]);others=[i for i in range(3) if i!=axis]
 if ext[axis]<.7 or min(ext[i] for i in others)<.09:return
 for mod in list(ob.modifiers):
  bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
 for obj in [ob]:
  bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
 for side in [0,1]:
  center=(lo+hi)/2;center[axis]=(lo if side==0 else hi)[axis]
  for j in range(2):
   a=center.copy();b=center.copy();a[others[0]]+=ext[others[0]]*(.08+j*.20);b[others[0]]-=ext[others[0]]*.25
   a[others[1]]+=ext[others[1]]*(.08-j*.21);b[others[1]]+=ext[others[1]]*(.03-j*.16)
   cutter=tube('Temporary wood check',[a,b],.0035,wood,5);cutter.matrix_world=ob.matrix_world.copy()
   bm=bmesh.new();bm.from_mesh(cutter.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(cutter.data);bm.free();bpy.context.view_layer.update()
   bpy.context.view_layer.objects.active=ob;mod=ob.modifiers.new('End grain check','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cutter
   bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)

def pegged_joint(x,y,z,width=.18):
 box('Exposed wedged tenon',(x,y,z),(width,.075,.17),edge,.006)
 tube('Oak drawbore peg',[(x,y-.055,z+.025),(x,y+.055,z+.025)],.018,wood,6)
 box('Tenon locking wedge',(x+.025,y-.047,z-.005),(.020,.025,.19),wood,.002)

def finish_timber():
 for i,ob in enumerate(list(col.objects)):
  if ob.type!='MESH' or not ob.data.materials:continue
  n=ob.name
  if n.startswith(('Crane kingpost','Crane A-frame','Framing upright','Framing head','Cradle sleeper','Roller bed runner','Pulley wall rail','Crane jib','Lifting spreader')):
   # Local corner variations are millimetres, preserving support contact and silhouette.
   for v in ob.data.vertices:
    v.co.x+=.0016*math.sin(v.co.z*4.3+v.co.y*9+i*.7)
   ob.data.materials[0]=weather[i%3];end_checks(ob)

def peg(c,length=.30):
 x,y,z=c;tube('Through pin',[(x,y-length/2,z),(x,y+length/2,z)],.027,iron,8)
 for side in [-1,1]:ellipsoid('Forged pin head',(x,y+side*length*.52,z),(.047,.019,.047),iron)

def eye(c,r=.06):
 x,y,z=c
 tube('Forged attachment eye',[(x+r*math.cos(t*math.tau/24),y,z+r*math.sin(t*math.tau/24)) for t in range(25)],.014,iron,8)

def sheave(c,r=.15,number=1,rope_arc=True):
 x,y,z=c;span=.105*number
 for yy in [y-span/2-.022,y+span/2+.022]:
  ellipsoid('Shaped hardwood pulley cheek',(x,yy,z),(r*1.20,.024,r*1.55),wood)
  for zz in [-r*.90,r*.90]:tube('Cheek rivet',[(x,yy-.027,z+zz),(x,yy+.027,z+zz)],.018,iron,8)
 peg(c,span+.13)
 tube('Axle split retaining pin',[(x-.015,y-span/2-.085,z-.039),(x+.015,y-span/2-.085,z+.044)],.006,iron,6)
 for yy in [y-span/2-.052,y+span/2+.052]:
  for zz in [-r*.90,r*.90]:
   tube('Rivet washer',[(x,yy-.004,z+zz),(x,yy+.004,z+zz)],.028,iron,10)
 for j in range(number):
  yy=y+(j-(number-1)/2)*.105
  tube('Solid sheave web',[(x,yy-.018,z),(x,yy+.018,z)],r*.79,edge,20)
  for offset in [-.028,.028]:
   tube('Groove retaining flange',[(x+r*.94*math.cos(t*math.tau/32),yy+offset,z+r*.94*math.sin(t*math.tau/32)) for t in range(33)],.014,wood,6)
  tube('Grooved wooden sheave',[(x+(r-.022)*math.cos(t*math.tau/32),yy,z+(r-.022)*math.sin(t*math.tau/32)) for t in range(33)],.015,edge,8)
  if rope_arc:tube('Rope following sheave',[(x+r*math.cos(t*math.pi/32),yy,z+r*math.sin(t*math.pi/32)) for t in range(33)],.014,rope,6)
 # Iron side straps hold the suspension eye rather than leaving it above the cheeks.
 for side in [-1,1]:
  yy=y+side*(span/2+.052)
  tube('Block cheek strap',[(x,yy,z-r*1.35),(x,yy,z+r*1.2),(x,y,z+r*1.52)],.017,iron,6)
 eye((x,y,z+r*1.75),r*.32)

def purchase(c,lower,r=.16,number=1,tail=None):
 x,y,z=c;small=r*.86
 # Extra upper wheel leads the final fall back to the operator's cleat/winch.
 sheave((x,y+.0525,z),r,number+1,False);sheave((x,y,lower),small,number,False)
 depths=[y+(j-(number-1)/2)*.105 for j in range(number+1)]
 points=[(x-r,depths[0],lower+.22)]
 for j,yy in enumerate(depths):
  points.append((x-r,yy,z))
  points += [(x+r*math.cos(math.pi-i*math.pi/24),yy,z+r*math.sin(math.pi-i*math.pi/24)) for i in range(1,25)]
  if j<number:
   points.append((x+small,yy,lower))
   points += [(x+small*math.cos(-i*math.pi/24),yy,lower+small*math.sin(-i*math.pi/24)) for i in range(1,25)]
 if tail is not None:
  points.extend(tail if isinstance(tail,list) else [tail])
 else:points.extend([(x+r,depths[-1],lower-.22),(x+r+.02,depths[-1]-.016,lower-.32)])
 tube('Continuous reeved purchase',points,.014,rope,8)
 if tail is None:
  q=points[-1]
  for k in range(4):ring('Tail whipping',(q[0],q[1],q[2]+.008*k),.015,.015,.004,tar,16)
 tube('Lower block becket',[(x-r,depths[0],lower+.22),(x-.035,depths[0],lower+.22)],.018,iron,6)
 for i in range(3):ring('Dead end seizure',(x-r,depths[0],lower+.23+i*.02),.024,.024,.007,tar,12)
 eye((x,y,lower-small*1.65),.05)

def hook(c,size=1):
 x,y,z=c
 tube('Forged hook shank',[(x,y,z+.11*size),(x,y,z-.10*size),(x+.04*size,y,z-.19*size),(x+.15*size,y,z-.23*size),(x+.23*size,y,z-.17*size),(x+.22*size,y,z-.055*size)],.023*size,iron,10)
 eye((x,y,z+.12*size),.046*size)


def cleat(c):
 x,y,z=c
 box('Cleat foot',(x,y,z+.03),(.14,.25,.06),wood,.008)
 beam('Cleat stem',(x,y,z+.03),(x,y,z+.13),.08,.08,wood)
 beam('Cleat horn',(x,y-.24,z+.14),(x,y+.24,z+.14),.08,.08,edge)
 for j in range(3):tube('Belayed line',[(x-.06,y-.18,z+.15+j*.017),(x+.055,y+.18,z+.16+j*.017),(x-.055,y+.18,z+.16+j*.017),(x+.06,y-.18,z+.15+j*.017)],.011,rope,6)


def mallet(c,angle=0):
 before=set(col.objects)
 box('Worn mallet head',(0,0,.067),(.28,.115,.13),edge,.015)
 tube('Mallet handle',[(0,0,.055),(0,-.40,.038)],.021,wood,8)
 rotate_new(before,c,angle)

def wedge(c,angle=0):
 before=set(col.objects)
 mesh('Loose adjustment wedge',[(-.13,-.09,0),(.13,-.09,0),(.13,.09,0),(-.13,.09,0),(-.13,-.09,.08),(-.13,.09,.08)],[(0,3,2,1),(0,1,4),(3,5,2),(0,4,5,3),(4,1,2,5)],edge,bevel=.004)
 rotate_new(before,c,angle)

def nail(c):
 x,y,z=c;tube('Square deck fastening',[(x,y,z-.014),(x,y,z)],.012,iron,4)

def deck(w=3,l=3):
 for x in [-w/2+.15,w/2-.15]:
  beam('Dock bearer',(x,-l/2,.19),(x,l/2,.19),.25,.28,wood)
  for y in [-l/2+.2,l/2-.2]:
   beam('Driven timber pile',(x,y,-.65),(x,y,.35),.22,.22,wood)
   beam('Pile knee brace',(x,y,-.37),(x,y+(.55 if y<0 else -.55),.19),.11,.13,wood)
 for i in range(int(l/.2)):
  y=-l/2+.1+i*.2;box('Deck board',(0,y,.39),(w-.012*(i%3),.192,.14),boards[(i*3+i//4)%4],.008)
  for x in [-w/2+.15,w/2-.15]:nail((x,y,.464))
 for x in [-w/2,w/2]:
  for y in [-l/2,0,l/2]:
   o=bpy.data.objects.new('snap_dock',None);col.objects.link(o);o.location=(x,y,.46);o['runtime_tag']='snappoint'


def cradle():
 for y in [-2,-.67,.67,2]:
  box('Cradle sleeper',(0,y,.12),(2.8,.28,.24),wood,.014)
  box('Keel support',(0,y,.40),(.44,.30,.36),edge,.012)
  for side in [-1,1]:
   beam('Splayed hull shore',(side*1.18,y,.24),(side*.78,y,1.32),.19,.19,wood)
   ob=box('Padded hull bearing',(0,0,0),(.48,.35,.16),leather,.02);ob.location=(side*.78,y,1.32);ob.rotation_euler.y=side*.32
   transform=ob.matrix_world.copy();bpy.context.view_layer.update();transform=ob.matrix_world.copy()
   for k in range(5):
    x=-.17+k*.085
    seam=tube('Leather bearing stitching',[(x,-.173,.058),(x+.012,-.179,.04),(x+.027,-.173,.058)],.003,rope,5);seam.matrix_world=transform
   peg((side*1.02,y,.54))
 for side in [-1,1]:
  beam('Cradle longitudinal tie',(side*1.1,-2.25,.27),(side*1.1,2.25,.27),.14,.17,wood)
  for y in [-2,-.67,.67,2]:
   box('Iron tie strap',(side*1.1,y,.365),(.24,.07,.015),iron,.002)
   wedge((side*.10,y,.59),side*.13)
 mallet((1.17,-1.32,.36),-.38)
 wedge((1.04,.08,.36),.27)
 before=set(col.objects);hank(scale=.72);rotate_new(before,(-1.12,1.38,.356),.2)


def rollers():
 for x in [-1.2,1.2]:beam('Roller bed runner',(x,-2.4,.15),(x,2.4,.15),.24,.25,wood)
 for y in [-2,-1,0,1,2]:
  tube('Timber launching roller',[(-1.15,y,.43),(1.15,y,.43)],.20,boards[int(y+2)%4],16)
  for x in [-.94,.94]:
   tube('Roller end iron band',[(x,y,.43),(x+.045,y,.43)],.204,iron,16)
  for x in [-1.3,1.3]:
   box('Roller bearing',(x,y,.31),(.22,.37,.38),edge,.01)
   tube('Roller spindle',[(x-.14,y,.43),(x+.14,y,.43)],.06,iron,8)
   box('Bearing cap strap',(x,y,.509),(.22,.12,.021),iron,.004)
   for yy in [-.13,.13]:nail((x,y+yy,.506))
 for x in [-1.2,1.2]:
  for y in [-2.3,2.3]:box('Roller track stop',(x,y,.34),(.29,.18,.24),wood,.012)

 mallet((1.24,.55,.275),.05)
 wedge((-1.24,1.56,.275),.26)

def framing_rack():
 for x in [-1.5,1.5]:
  box('Framing rack foot',(x,0,.10),(.28,2.4,.20),wood,.012)
  beam('Framing upright',(x,0,.2),(x,0,3.0),.23,.23,wood)
  for side in [-1,1]:beam('Framing diagonal brace',(x,side*1.05,.2),(x,0,1.3),.13,.13,wood)
 beam('Framing head beam',(-1.8,0,3.0),(1.8,0,3.0),.25,.29,wood)
 for x in [-1.5,1.5]:peg((x,0,2.95),.34)
 purchase((0,-.20,2.56),1.57,.14,1,[(1.37,-.34,2.45),(1.48,-.40,.38)])
 eye((1.37,-.34,2.45),.045)
 tube('Haul guide mounting pin',[(1.37,-.34,2.45),(1.5,0,2.45)],.017,iron,8)
 for x in [-1.5,1.5]:pegged_joint(x,-.16,2.92,.15)
 tube('Gantry block top strop',[(0,-.1475,2.88),(0,-.18,3.19),(0,.18,3.19),(0,.18,2.87),(0,-.1475,2.88)],.022,rope,8)
 hook((0,-.20,1.28),.9)
 beam('Lifting spreader',(-1.05,-.20,.91),(1.05,-.20,.91),.16,.17,wood)
 for side in [-1,1]:
  tube('Spreader bridle',[(0,-.20,1.08),(side*.82,-.20,.92),(side*.82,-.31,.79),(side*.82,-.08,.79),(side*.82,-.20,.92)],.02,rope,8)
  tube('Cargo sling',[(side*.68+.016*math.sin(t*.7),-.20-.19*math.cos(t*math.pi/24),.95-.52*math.sin(t*math.pi/24)**.65) for t in range(25)],.025,rope,8)
 for x in [-1.5,1.5]:
  beam('Gantry corner knee',(x,0,2.3),(x+(-.52 if x>0 else .52),0,2.98),.12,.14,wood)
  box('Gantry joint strap',(x,-.132,2.98),(.12,.025,.43),iron,.003)
 cleat((1.48,-.4,.20))
 before=set(col.objects);hank(scale=1.0);rotate_new(before,(1.91,-.71,0),-.18)
 mallet((-1.49,.60,.20),.12)



def crane(twin=False):
 deck(3.6 if twin else 3,3)
 if twin:
  for x in [-1.0,1.0]:
   beam('Crane A-frame leg',(x,.35,.46),(x*.25,.35,4.35),.24,.28,wood)
   beam('Crane backstay',(x,1.1,.46),(x*.25,.35,3.2),.14,.14,wood)
  beam('A-frame crosshead',(-.40,.35,4.35),(.40,.35,4.35),.28,.30,wood)
  beam('A-frame cross tie',(-.83,.35,1.25),(.83,.35,1.25),.18,.20,wood)
  beam('Jib pivot cross member',(-.50,.35,3.30),(.50,.35,3.30),.22,.26,wood)
  beam('Strut heel cross member',(-.82,.35,1.55),(.82,.35,1.55),.22,.24,wood)
 else:
  beam('Crane kingpost',(0,.35,.46),(0,.35,4.1),.30,.30,wood)
  for x,y in [(-1,.4),(1,.4),(0,1.25)]:beam('Kingpost brace',(x,y,.47),(0,.35,1.6),.16,.16,wood)
 beam('Crane jib',(0,.35,3.3),(0,-2.8,3.65),.25,.27,wood)
 beam('Jib compression strut',(0,.35,1.55),(0,-2.45,3.59),.19,.19,wood)
 peg((0,.35,3.30),.42)
 for side in [-1,1]:
  box('Jib pivot cheek iron',(side*.139,.35,3.30),(.025,.35,.34),iron,.005)
  tube('Jib pivot through axle',[(side*.115,.35,3.30),(side*.19,.35,3.30)],.033,iron,10)
 for x in ([-.50,.50] if twin else [0]):pegged_joint(x,.17,3.30,.12)
 # Visible paired purchase with reeved working ends and supported haul line.
 purchase((0,-2.8,3.24),1.94,.18,2 if twin else 1,(.62,-.47,1.19))
 tube('Top block securing strop',[(0,-2.7475,3.57),(0,-2.67,3.82),(0,-2.98,3.82),(0,-2.98,3.57),(0,-2.7475,3.57)],.027,rope,8)
 hook((0,-2.8,1.55),1.2)
 # Hand windlass rests in two trestles, with an accessible crank and pawl.
 for x in [.34,1.17]:
  beam('Windlass upright',(x,.38,.46),(x,.38,1.02),.10,.12,wood)
  beam('Windlass foot',(x,.1,.51),(x,.67,.51),.11,.10,wood)
 tube('Windlass spindle',[(.22,.38,1.02),(1.40,.38,1.02)],.045,iron,10)
 tube('Windlass drum',[(.37,.38,1.02),(1.12,.38,1.02)],.14,wood,16)
 for x in [.38,1.10]:tube('Windlass drum flange',[(x-.035,.38,1.02),(x+.035,.38,1.02)],.20,edge,16)
 tube('Wound hauling rope',[(.50+t*.0015,.38+.156*math.sin(t*math.tau/24),1.02+.156*math.cos(t*math.tau/24)) for t in range(289)],.014,rope,6)
 tube('Drum leading rope',[(.62,.38,1.19),(.50,.38,1.176)],.014,rope,6)
 tube('Crank arm',[(1.37,.38,1.02),(1.37,.38,.77),(1.58,.38,.77)],.025,iron,8)
 tube('Crank grip',[(1.41,.38,.77),(1.58,.38,.77)],.036,wood,10)
 cleat((.75,-.05,.47))
 before=set(col.objects);hank(scale=1.3);rotate_new(before,(.9,-1.04,.47),.3)
 for h in [1.4,3.2]:
  centres=[0] if not twin else [side*(1-.75*(h-.46)/(4.35-.46)) for side in [-1,1]]
  for cx in centres:
   for j in range(3):tube('Timber joint rope seizure',[(cx-.17,.17,h+j*.035),(cx+.17,.17,h+j*.035),(cx+.17,.53,h+j*.035),(cx-.17,.53,h+j*.035),(cx-.17,.17,h+j*.035)],.015,rope,6)
 # Toothed stop wheel and pawl sit beside the operator crank.
 tube('Ratchet wheel',[(1.23,.38,1.02),(1.27,.38,1.02)],.145,iron,16)
 for i in range(12):
  a=i*math.tau/12
  tube('Ratchet tooth',[(1.25,.38+.14*math.cos(a),1.02+.14*math.sin(a)),(1.25,.38+.169*math.cos(a+.09),1.02+.169*math.sin(a+.09))],.017,iron,5)
 tube('Ratchet pawl',[(1.25,.55,1.17),(1.25,.42,1.17)],.018,iron,6)
 beam('Pawl mounting arm',(1.17,.38,.87),(1.25,.55,1.17),.04,.05,iron)
 for ob in col.objects:
  if ob.name.startswith(('Windlass','Wound hauling','Drum leading','Crank','Ratchet','Pawl')):ob.location.y-=.85
 mallet((-.88,-.48,.461),.48)



def pulley(kind):
 n=kind+1
 beam('Pulley wall rail',(-.40,0,.8),(.40,0,.8),.14,.18,wood)
 for x in [-.31,.31]:peg((x,0,.8),.25)
 if kind==0:sheave((0,-.22,.44),.16,n)
 else:purchase((0,-.22,.44),-.40,.16,n)
 tube('Block hanging strop',[(0,-.22,.72),(0,-.20,.93),(0,.1,.93),(0,.1,.74),(0,-.22,.72)],.018,rope,6)
 if kind==0:
  for x in [-.16,.16]:tube('Hanging tackle fall',[(x,-.22,.44),(x,-.22,-.23),(x+.026,-.22,-.39),(x+.014,-.20,-.45)],.014,rope,6)
 if kind>0:
  hook((0,-.22,-.74),.85)

MODELS=[('keel-cradle','Keel cradle',cradle),('roller-bed','Launching rollers',rollers),('framing-rack','Ship framing gantry',framing_rack),('pier-crane','Timber pier crane',lambda:crane(False)),('heavy-crane','Braced pier crane',lambda:crane(True)),('single-pulley','Single block',lambda:pulley(0)),('double-pulley','Double purchase',lambda:pulley(1)),('triple-pulley','Heavy purchase',lambda:pulley(2))]
manifest=json.loads((OUT/'models.json').read_text()) if len(sys.argv)>1 and (OUT/'models.json').exists() else []
for key,label,make in MODELS:
 if len(sys.argv)>1 and key not in sys.argv[1:]:continue
 for ob in list(col.objects):bpy.data.objects.remove(ob,do_unlink=True)
 make();bpy.context.view_layer.update();finish_timber()
 for ob in col.objects:
  if ob.type!='MESH':continue
  bpy.context.view_layer.objects.active=ob
  for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
  bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
 bpy.context.view_layer.update()
 points=np.array([ob.matrix_world@v.co for ob in col.objects if ob.type=='MESH' for v in ob.data.vertices]);lo=points.min(0);hi=points.max(0)
 dest=OUT/key;dest.mkdir(exist_ok=True)
 bpy.ops.object.select_all(action='DESELECT')
 for ob in col.objects:ob.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(dest/f'{key}.glb'),export_format='GLB',use_selection=True,export_apply=True)
 bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(dest/f'{key}.blend'))
 tri=sum(len(p.vertices)-2 for ob in col.objects if ob.type=='MESH' for p in ob.data.polygons)
 manifest=[m for m in manifest if m["id"]!=key]
 manifest.append(dict(triangles=tri,id=key,name=label,dimensions_m=(hi-lo).tolist(),bounds_min=lo.tolist(),bounds_max=hi.tolist(),original_geometry=True))
 print('BUILT',key,flush=True)
manifest.sort(key=lambda m:[k for k,_,_ in MODELS].index(m['id']))
(OUT/'models.json').write_text(json.dumps(manifest,indent=2)+'\n')
