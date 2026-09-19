"""Original fishing equipment studies. No imported assets or runtime changes."""
from pathlib import Path
import sys
R=Path(__file__).resolve().parents[2]
source=R/'tools/FishingStation/build.py';ns={'__file__':str(source)}
exec(compile(source.read_text().split('# Compact fish-prep workstation')[0],str(source),'exec'),ns)
globals().update({k:v for k,v in ns.items() if k!='__file__'})
OUT=R/'design/fishing-equipment-v2';OUT.mkdir(exist_ok=True)
eelskin=mat('Smoked eel skin',(.14,.17,.135));eelbelly=mat('Smoked eel belly',(.32,.30,.22));oilmat=mat('Collected amber fish oil',(.38,.20,.045))

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


def finish_fishing_timber():
 for i,ob in enumerate(list(col.objects)):
  if ob.type!='MESH':continue
  if ob.name.startswith(('Press upright','Press headstock','Rack upright','Hanging rail')):
   ob.data.materials[0]=aged_boards[i%5];end_checks(ob)
def ties(c,r=.055):
 for j in range(3):ring('Seized rope knot',(c[0],c[1],c[2]+j*.013),r,r,.007,rope,16)
def upright_frame(width=1.65,height=1.8):
 for x in [-width/2,width/2]:
  box('Rack foot',(x,0,.055),(.19,.84,.11),wood,.01)
  beam('Rack upright',(x,0,.11),(x,0,height),.10,.11,wood)
  for side in [-1,1]:beam('Rack knee',(x,side*.35,.11),(x,0,.57),.055,.06,wood)
 beam('Hanging rail',(-width/2-.13,0,height),(width/2+.13,0,height),.085,.095,wood)
 beam('Lower stretcher',(-width/2,0,.25),(width/2,0,.25),.065,.07,wood)
 for x in [-width/2,width/2]:
  tube('Rail locking pin',[(x,-.072,height),(x,.072,height)],.015,edge,6)
  ties((x,0,height-.02),.075)
  box('Proud rail tenon',(x,-.07,height),(.085,.055,.072),edge,.004)
  tube('Rack peg locking wedge',[(x-.013,-.095,height-.025),(x+.006,-.095,height+.035)],.007,wood,4)
  tube('Lashing tucked end',[(x+.065,-.03,height-.025),(x+.10,-.065,height-.07),(x+.07,-.063,height-.15)],.008,rope,6)

def press():
 for x in [-.65,.65]:
  box('Press foot',(x,0,.09),(.22,1.22,.18),wood,.015)
  beam('Press upright',(x,0,.18),(x,0,1.94),.16,.19,wood)
  for side in [-1,1]:beam('Press knee',(x,side*.50,.18),(x,0,.70),.08,.085,wood)
 beam('Press headstock',(-.84,0,1.84),(.84,0,1.84),.24,.24,wood)
 for x in [-.65,.65]:
  box('Headstock iron strap',(x,-.131,1.84),(.11,.022,.33),iron,.003)
  for z in [1.75,1.93]:tube('Headstock bolt',[(x,-.15,z),(x,.15,z)],.017,iron,8)
 beam('Press bed beam',(-.65,0,.61),(.65,0,.61),.34,.16,wood)
 # Solid catch pan with raised edge, draining out of its front opening.
 tube('Catch pan base',[(0,0,.675),(0,0,.73)],.56,wood,32)
 tube('Catch pan rim',[(.55*math.cos(-math.pi/2+.16+t*(math.tau-.32)/64),.55*math.sin(-math.pi/2+.16+t*(math.tau-.32)/64),.76) for t in range(65)],.025,edge,8)
 for i in range(24):
  a=i*math.tau/24
  ob=box('Press basket stave',(0,0,0),(.082,.045,.48),boards[i%4],.005);ob.location=(.43*math.cos(a),.43*math.sin(a),.97);ob.rotation_euler.z=a+math.pi/2
 for z in [.80,1.14]:
  ring('Press basket hoop',(0,0,z),.461,.461,.018,iron,48)
  box('Riveted hoop splice',(.04,-.465,z),(.12,.018,.047),iron,.003)
  for x in [0,.08]:tube('Hoop splice rivet',[(x,-.466,z),(x,-.482,z)],.008,iron,8)
 # A supported feed paddle and its retaining tie belong to the press, not the walkway.
 tube('Feed paddle shaft',[(.66,.25,.20),(.70,.28,1.05)],.018,wood,8)
 ob=box('Feed paddle blade',(0,0,0),(.115,.031,.23),edge,.006);ob.location=(.698,.278,.94);ob.rotation_euler.y=.047
 tube('Feed paddle retaining tie',[(.60,.085,.65),(.75,.085,.65),(.75,.31,.65),(.66,.31,.65),(.60,.085,.65)],.007,rope,6)
 tube('Pressing plate',[(0,0,1.08),(0,0,1.16)],.396,wood,32)
 tube('Press screw core',[(0,0,1.16),(0,0,2.12)],.07,iron,16)
 tube('Helical screw thread',[(.09*math.cos(i*math.tau/20),.09*math.sin(i*math.tau/20),1.17+i*.0045) for i in range(210)],.015,iron,8)
 box('Threaded headstock nut',(0,0,1.84),(.23,.28,.28),iron,.014)
 tube('Turning cross handle',[(-.57,0,2.10),(.57,0,2.10)],.032,wood,10)
 for x in [-.57,.57]:ellipsoid('Handle stop',(x,0,2.1),(.048,.045,.045),edge)
 # A supported spout and removable collection pot below it.
 box('Front spout floor',(0,-.59,.728),(.13,.34,.024),wood,.004)
 for x in [-.069,.069]:box('Front spout lip',(x,-.59,.75),(.025,.34,.056),wood,.004)
 beam('Catch shelf support',(-.60,-.02,.25),(-.60,-.74,.25),.07,.08,wood)
 beam('Catch shelf support',(.60,-.02,.25),(.60,-.74,.25),.07,.08,wood)
 for j in range(3):box('Catch shelf',(0,-.30-j*.19,.30),(1.28,.18,.065),aged_boards[j],.006)
 place(lambda:bucket((0,0,0),.19,.29),(.03,-.61,.333),.17)
 tube('Oil inside collection pot',[(.03,-.61,.55),(.03,-.61,.554)],.154,oilmat,24)
 place(basket,(-1.04,-.18,0),-.33,1.25)
 place(lambda:hank(scale=.65),(1.02,.36,.001),.3)
 marker('input',(0,-.50,1.04),'future fish insertion interaction')
 marker('output',(0,-.83,.57),'future native output item spawn; preserve processing semantics')
 marker('screw_pivot',(0,0,1.84),'future screw and handle rotation axis')
 marker('platen',(0,0,1.16),'future limited pressing travel')


def net():
 upright_frame(2.0,1.8)
 def p(u,v):return (-.89+1.78*u,-.045-.12*math.sin(v*math.pi)+.025*math.sin(u*11+v*3),1.74-.12*math.sin(u*math.pi)-v*(1.02+.15*math.sin(u*7)))
 for i in range(25):tube('Net warp',[p(i/24,j/24) for j in range(25)],.0045,tar,5)
 for j in range(17):
  for a,b in ([(0,11),(15,32)] if j in [7,8] else [(0,32)]):tube('Net weft',[p(i/32,j/16) for i in range(a,b+1)],.0045,tar,5)
 tube('Net head rope',[p(i/48,0) for i in range(49)],.015,rope,6)
 tube('Weighted net foot',[p(i/48,1) for i in range(49)],.012,rope,6)
 for i in range(10):
  q=p(i/9,0);tube('Net rail tie',[q,(q[0],-.07,1.86),(q[0],.08,1.86),(q[0],.07,1.76),q],.008,rope,6)
  ellipsoid('Cork float',(q[0],q[1]-.015,q[2]-.038),(.052+.012*(i%3),.032,.035),cork)
  tube('Float binding',[(q[0],q[1]-.052,q[2]-.03),(q[0],q[1]-.035,q[2]-.072),(q[0],q[1]+.022,q[2]-.045),(q[0],q[1]-.015,q[2]+.005),(q[0],q[1]-.052,q[2]-.03)],.004,rope,5)
  q=p(i/9,1);ellipsoid('Stone sinker',q,(.038,.027,.047),clay)
 tube('Mended net edge',[p(.346+.012*math.sin(i*math.pi/2),.405+i*.007) for i in range(24)],.006,rope,5)
 # Removable folded reserve net rests in a low slatted tray.
 for y in [-.27,.19]:box('Net tray side',(.44,y,.13),(.65,.035,.20),wood,.005)
 for x in [.13,.75]:box('Net tray end',(x,-.04,.13),(.035,.43,.20),wood,.005)
 box('Net tray base',(.44,-.04,.035),(.65,.47,.065),wood,.006)
 for i in range(12):
  tube('Folded net length',[(.17+i*.049+.008*math.sin(j),-.23+j*.032,.10+.10*math.sin(j*math.pi/12)**2) for j in range(13)],.007,tar,5)
 for j in range(13):tube('Folded net cross',[(.17+i*.049+.008*math.sin(j),-.23+j*.032,.10+.10*math.sin(j*math.pi/12)**2) for i in range(12)],.006,tar,5)
 place(lambda:hank(scale=.8),(-.50,-.12,0),-.27)
 for ob in col.objects:
  if ob.name.startswith(('Net tray','Folded net')):ob.location.y-=.38
 # A repair shuttle hangs against the left upright on its own peg.
 tube('Shuttle peg',[(-1,-.01,1.13),(-1,-.12,1.13)],.012,wood,8)
 tube('Shuttle hanging loop',[(-1,-.13,1.13),(-1.06,-.14,.98),(-.98,-.14,.92),(-1,-.13,1.13)],.007,rope,6)
 box('Net mending shuttle',(-1.02,-.145,.90),(.065,.026,.24),edge,.01)
 for j in range(5):
  z=.86+j*.016;tube('Shuttle twine wrap',[(-1.054,-.165,z),(-.986,-.165,z+.006),(-.986,-.124,z),(-1.054,-.124,z),(-1.054,-.165,z)],.005,rope,5)
 marker('net_hanging_origin',(0,0,1.74),'future net sway/deploy rig; preserve head ties')
 marker('net_stowed_origin',(.44,-.42,.12),'future stowed net visual')


def hanging_fish(c,length,angle):
 before=set(col.objects);fish((0,0,0),0,length)
 for side in [-1,1]:
  tube('Dried gill crease',[(-length*.22,side*.033,.040),(-length*.19,side*.044,.036),(-length*.16,side*.039,.028)],.003,tar,5)
  mesh('Dried pectoral fin',[(-length*.12,side*.041,.033),(length*.01,side*.092,.023),(length*.07,side*.037,.027)],[(0,1,2)],fishbelly)
 bpy.context.view_layer.update()
 matrix=Matrix.Translation(c)@Matrix.Rotation(angle,4,'Y')@Matrix.Rotation(math.pi/2,4,'X')@Matrix.Rotation(math.pi/2,4,'Z')
 for ob in set(col.objects)-before:ob.matrix_world=matrix@ob.matrix_world
 # Tail is local +X. Attach the string to the transformed tail, never the air above it.
 q=matrix@Vector((length*.43,0,.03))
 tube('Fish tail tie',[(q.x,q.y,q.z),(c[0],-.06,1.73),(c[0],.06,1.73),(c[0],.04,1.62),(q.x,q.y,q.z)],.008,rope,6)
 ties((q.x,q.y,q.z),.023)

def dryer():
 upright_frame(1.9,1.67)
 for i,x in enumerate([-.72,-.40,-.02,.33,.68]):
  hanging_fish((x,-.04,1.13+(i%3)*.07),.57+(i%2)*.07,[-.12,.08,-.035,.13,-.08][i])
 for x in [-.85,.85]:beam('Shelf bearer',(x,-.36,.305),(x,.33,.305),.10,.07,wood)
 # Scrubbed lower shelf and an untidy reserve basket leave the fish suspended clear.
 for j in range(3):box('Drying rack shelf',(0,-.22+j*.19,.37),(1.76,.18,.06),aged_boards[j],.007)
 place(basket,(.48,-.015,.402),.25,1.0)
 place(lambda:hank(scale=.65),(-.49,-.04,.402),-.32)
 marker('fish_sway_axis',(0,0,1.67),'future individually weighted hanging fish sway')


def eel(c,length,phase):
 # Bent, tapering body with a continuous silhouette and a small dorsal fin.
 pts=[];n=34;sides=10
 for i in range(n):
  t=i/(n-1);pts.append(Vector((c[0]+.045*math.sin(t*5+phase)*(1-t),c[1]+.025*math.sin(t*7+phase),c[2]+length*t)))
 verts=[]
 for i,p in enumerate(pts):
  t=i/(n-1);r=.041*(1-.83*t)+.013*math.sin(math.pi*t)
  for j in range(sides):
   a=j*math.tau/sides;verts.append(tuple(p+Vector((r*math.cos(a),r*.72*math.sin(a),0))))
 faces=[tuple(range(sides-1,-1,-1))]+[(i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j) for i in range(n-1) for j in range(sides)]+[tuple((n-1)*sides+j for j in range(sides))]
 mesh('Hanging smoked eel body',verts,faces,eelskin)
 fin=[]
 for i,p in enumerate(pts[5:-2]):
  t=(i+5)/(n-1);r=.041*(1-.83*t)+.013*math.sin(math.pi*t)
  fin.extend([(p.x,p.y+r*.72,p.z),(p.x,p.y+r*.72+.015*math.sin(t*math.pi),p.z)])
 mesh('Eel dorsal ribbon',fin,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(fin)//2-1)],eelbelly)
 q=pts[0];ellipsoid('Eel head',q,(.041,.032,.06),eelskin)
 tube('Eel mouth crease',[(q.x-.022,q.y-.025,q.z-.030),(q.x,q.y-.033,q.z-.037),(q.x+.021,q.y-.025,q.z-.030)],.0025,tar,5)
 for side in [-1,1]:ellipsoid('Eel eye',(q.x+side*.029,q.y-.018,q.z-.015),(.007,.005,.007),darkeye)
 tube('Eel belly seam',[(p.x,p.y-.026*(1-.8*i/(n-1)),p.z) for i,p in enumerate(pts[:-3])],.008,eelbelly,5)
 q=pts[-1];tube('Eel tail hanging loop',[q,(c[0],-.06,1.73),(c[0],.06,1.73),(c[0],.06,1.62),q],.008,rope,6)
 ties(q,.019)

def eels():
 upright_frame(1.34,1.67)
 for ob in col.objects:
  if ob.name.startswith('Lower stretcher'):ob.location.z-=.15
 for i,x in enumerate([-.47,-.21,.02,.29,.48]):eel((x,-.03,.60+(i%3)*.07),.83-(i%2)*.09,i*.8)
 # Two low bearers support a drip tray without adding a platform around the piece.
 for x in [-.51,.51]:beam('Drip tray bearer',(x,-.26,.14),(x,.26,.14),.07,.07,wood)
 for j in range(3):box('Eel drip tray plank',(0,-.20+j*.20,.19),(1.20,.19,.04),aged_boards[j],.005)
 for y in [-.29,.29]:box('Tray edge',(0,y,.24),(1.24,.035,.11),wood,.005)
 for x in [-.6,.6]:box('Tray end',(x,0,.24),(.035,.56,.11),wood,.005)
 marker('eel_sway_axis',(0,0,1.67),'future hanging eel motion; ties stay fixed')

MODELS=[('fish-oil-press','Fish oil screw press','OilPress',press),('net-rack','Fishing net rack','RedePesca',net),('fish-dryer','Fish drying rack','Peixes',dryer),('eel-rack','Eel drying rack','Enguias',eels)]
manifest=json.loads((OUT/'models.json').read_text()) if len(sys.argv)>1 and (OUT/'models.json').exists() else []
for key,label,replaces,make in MODELS:
 if len(sys.argv)>1 and key not in sys.argv[1:]:continue
 for ob in list(col.objects):bpy.data.objects.remove(ob,do_unlink=True)
 make();bpy.context.view_layer.update();finish_fishing_timber()
 for ob in col.objects:
  if ob.type!='MESH':continue
  bpy.context.view_layer.objects.active=ob
  for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
  bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
 bpy.context.view_layer.update();points=np.array([ob.matrix_world@v.co for ob in col.objects if ob.type=='MESH' for v in ob.data.vertices]);lo=points.min(0);hi=points.max(0)
 dest=OUT/key;dest.mkdir(exist_ok=True);bpy.ops.object.select_all(action='DESELECT')
 for ob in col.objects:ob.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(dest/(key+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
 bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(dest/(key+'.blend')))
 tri=sum(len(p.vertices)-2 for ob in col.objects if ob.type=='MESH' for p in ob.data.polygons)
 manifest=[m for m in manifest if m['id']!=key];manifest.append(dict(id=key,name=label,replaces=replaces,triangles=tri,dimensions_m=(hi-lo).tolist(),bounds_min=lo.tolist(),bounds_max=hi.tolist(),original_geometry=True))
 print('BUILT',key,tri,flush=True)
manifest.sort(key=lambda m:[k for k,*_ in MODELS].index(m['id']));(OUT/'models.json').write_text(json.dumps(manifest,indent=2)+'\n')
