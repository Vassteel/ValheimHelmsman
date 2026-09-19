"""Original compact pelican workstation. Authored props and joinery; no imported dock assets."""
from pathlib import Path
import sys
R=Path(__file__).resolve().parents[2]
source=R/'tools/WorkshopModels/build.py';ns={'__file__':str(source)}
exec(compile(source.read_text().split('MODELS=')[0],str(source),'exec'),ns)
globals().update({k:v for k,v in ns.items() if k!='__file__'})
from mathutils import Matrix
OUT=R/'design/pelican-station-v1';OUT.mkdir(exist_ok=True)
for ob in list(col.objects):bpy.data.objects.remove(ob,do_unlink=True)
aged_boards=[mat('Weathered dock timber '+str(i),c,grain=True) for i,c in enumerate([(.26,.22,.16),(.30,.255,.19),(.23,.20,.155),(.34,.265,.17),(.28,.245,.195)])]
wet=mat('Tarred standing wet wood',(.10,.12,.105));skin=mat('Fresh fish scales',(.28,.36,.37));belly=mat('Fresh fish belly',(.62,.63,.51));cork=mat('Cork net floats',(.42,.26,.11))

def place(make,c,angle=0,scale=1):
 before=set(col.objects);make();bpy.context.view_layer.update();matrix=Matrix.Translation(c)@Matrix.Rotation(angle,4,'Z')@Matrix.Scale(scale,4)
 for ob in set(col.objects)-before:ob.matrix_world=matrix@ob.matrix_world

# Preserve each fish part's authored local offset when rotating the assembled prop.
def rotate_fish_parts(before,center,angle):
 bpy.context.view_layer.update();transform=Matrix.Translation(center)@Matrix.Rotation(angle,4,'Z')
 for ob in set(col.objects)-before:ob.matrix_world=transform@ob.matrix_world
ns['rotate_new']=rotate_fish_parts

def fresh(c,angle=0,size=.5):
 before=set(col.objects);fish(c,angle,size)
 for ob in set(col.objects)-before:
  ob.name=ob.name.replace('Dried','Fresh')
  for i,m in enumerate(ob.data.materials):
   if m==fishskin:ob.data.materials[i]=skin
   if m==fishbelly:ob.data.materials[i]=belly

def marker(name,c,role):
 o=bpy.data.objects.new(name,None);col.objects.link(o);o.location=c;o['purpose']=role
 if name.startswith('snap_'):o['runtime_tag']='snappoint'

def bucket(c,r=.25,h=.39,fill=False):
 x,y,z=c;n=16
 profile=[(r*.82,0),(r,h),(r-.025,h),(r*.82-.025,.035)]
 verts=[(x+rad*math.cos(i*math.tau/n),y+rad*math.sin(i*math.tau/n),z+zz) for rad,zz in profile for i in range(n)]
 faces=[tuple(range(n-1,-1,-1))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(3) for i in range(n)]+[tuple(3*n+i for i in range(n))]
 mesh('Open stave bucket',verts,faces,wood)
 for zz in [.06,h-.045]:ring('Bucket iron hoop',(x,y,z+zz),r*(.82+.18*zz/h)+.009,r*(.82+.18*zz/h)+.009,.013,iron,32)
 for i in range(n):
  a=(i+.04)*math.tau/n;tube('Bucket stave seam',[(x+r*.84*math.cos(a),y+r*.84*math.sin(a),z+.045),(x+r*1.001*math.cos(a),y+r*1.001*math.sin(a),z+h-.018)],.003,tar,5)
 tube('Bucket rope handle',[(x-r,y,z+h-.04),(x-r,y,z+h+.12),(x,y,z+h+.24),(x+r,y,z+h+.12),(x+r,y,z+h-.04)],.011,rope,6)
 if fill:
  tube('Dark bucket water',[(x,y,z+h-.09),(x,y,z+h-.085)],r-.03,wet,20)
  fresh((x,y,z+h-.063),.4,r*1.7)

# Compact fish-prep workstation; no deck, pier or surrounding floor.
for x in [-1.14,.44]:
 for y in [-.34,.34]:beam('Bench leg',(x,y,0),(x,y,.89),.11,.12,wood)
 beam('End foot stretcher',(x,-.39,.18),(x,.39,.18),.085,.10,wood)
beam('Back stretcher',(-1.14,.34,.23),(.44,.34,.23),.08,.10,wood)
for y in [-.34,.34]:beam('Table apron',(-1.24,y,.79),(.54,y,.79),.085,.14,wood)
for i in range(5):
 box('Scrubbed sorting plank',(-.35,-.40+i*.20,.915),(1.97,.19,.09),aged_boards[i],.009)
 for x in [-1.14,.44]:tube('Top treenail',[(x,-.40+i*.20,.958),(x,-.40+i*.20,.963)],.014,edge,6)
box('Low splash back',(-.35,.48,1.075),(2.0,.05,.23),wood,.009)
# Rear drying rail is part of the bench. The drape has one repaired tear.
for x in [-1.14,.44]:beam('Drying rail upright',(x,.37,.1),(x,.37,1.75),.075,.085,wood)
beam('Net drying rail',(-1.30,.37,1.74),(.60,.37,1.74),.07,.07,wood)
def netpoint(u,v):
 return (-1.12+1.32*u,.40+.05*math.sin(u*8+v*4),1.70-.09*math.sin(u*math.pi)-v*(.53+.10*math.sin(u*7)))
for i in range(19):tube('Drying net warp',[netpoint(i/18,j/14) for j in range(15)],.0045,tar,5)
for j in range(10):
 for a,b in ([(0,9),(12,24)] if j in [5,6] else [(0,24)]):tube('Drying net weft',[netpoint(i/24,j/9) for i in range(a,b+1)],.0045,tar,5)
for i in range(7):
 q=netpoint(i/6,0);tube('Net head tie',[q,(q[0],.32,1.79),(q[0],.42,1.79),q],.007,rope,6)
 ellipsoid('Cork float',(q[0],q[1]-.012,q[2]-.035),(.043,.023,.026),cork)
 q=netpoint(i/6,1);ellipsoid('Stone net sinker',q,(.023,.024,.035),clay)
tube('Net repair stitching',[netpoint(.405+.015*math.sin(t*math.pi),.48+t*.013) for t in range(18)],.005,rope,5)
# Offset cutting board, fish, knife and an uneven draped rag.
place(lambda:box('Cutting board',(0,0,.022),(.68,.43,.044),edge,.016),(-.66,-.04,.962),-.12)
fresh((-.67,-.04,1.01),-.17,.55)
fresh((-.14,.14,.967),.47,.48)
tube('Knife handle',[(-.96,-.24,1.024),(-.77,-.22,1.024)],.020,wood,8)
mesh('Knife blade',[(-.77,-.23,1.02),(-.55,-.21,1.02),(-.52,-.17,1.02),(-.77,-.185,1.02)],[(0,1,2,3)],iron)
mesh('Draped working rag',[(.10,-.22,.968),(.37,-.24,.968),(.38,-.49,.966),(.13,-.49,.969),(.36,-.508,.74),(.12,-.514,.79)],[(0,1,2,3),(3,2,4,5)],linen)
for i in range(19):
 a=i*2.4;ellipsoid('Scattered fish scale',(-.6+.48*math.cos(a),.04+.25*math.sin(a),.965),(.008,.012,.002),belly)
# Catch chest under the bench, front-facing latch remains reachable.
for j in range(4):
 for x in [-.81,.11]:box('Catch chest side',(x,0,.11+j*.12),(.06,.66,.114),boards[j%4],.005)
 for y in [-.31,.31]:box('Catch chest front back',(-.35,y,.11+j*.12),(.90,.06,.114),boards[(j+1)%4],.005)
box('Catch chest base',(-.35,0,.063),(.95,.66,.066),wood,.005)
for i in range(4):box('Catch chest lid',(-.35,-.255+i*.17,.57),(1.0,.163,.07),boards[i],.006)
for x in [-.64,-.06]:box('Chest iron lid strap',(x,0,.61),(.035,.70,.012),iron,.002)
box('Catch chest latch',(-.35,-.356,.49),(.09,.022,.14),iron,.003)
marker('storage_access',(-.35,-.38,.35),'native Container interaction')
# Pelican's side perch, attached to the bench and braced down to its own feet.
for y in [-.21,.27]:beam('Perch leg',(1.08,y,0),(1.08,y,.92),.10,.10,wood)
beam('Perch bench tie',(.44,.15,.35),(1.08,.15,.35),.08,.10,wood)
beam('Perch diagonal brace',(.44,.15,.35),(1.08,.15,.83),.07,.07,wood)
for i in range(3):box('Pelican perch plank',(.99,-.22+i*.25,.92),(.83,.24,.10),boards[(i+1)%4],.009)
marker('pelican_perch',(.99,.03,.97),'home perch and fish drop-off for future flight behavior')
# Working clutter has individual ground contact and leaves chest access open.
bucket((-1.58,.13,0),.23,.38,True)
place(basket,(.82,-.36,0),-.29,1.22)
place(lambda:hank(scale=.75),(-1.58,-.39,.001),.46)
# Loose cord hangs from a real peg instead of floating beside the furniture.
tube('Rope peg',[(.43,.36,1.34),(.43,.18,1.34)],.020,wood,8)
for k in range(7):
 tube('Hanging working rope',[(.43+(.09+k*.008)*math.sin(t*math.tau/48),.18-k*.008,1.11+(.23+k*.004)*math.cos(t*math.tau/48)) for t in range(49)],.009,rope,6)
tube('Hank tied neck',[(.39,.10,1.31),(.48,.10,1.31),(.48,.21,1.31),(.39,.21,1.31),(.39,.10,1.31)],.009,rope,6)
# Short hand-net leaning against the left back corner, lashed to the upright.
tube('Landing net handle',[(-1.38,.40,.03),(-1.22,.40,1.12)],.021,wood,8)
for t in [0]:
 tube('Landing net hoop',[(-1.18+.22*math.cos(i*math.tau/36),.40,1.34+.25*math.sin(i*math.tau/36)) for i in range(37)],.014,wood,8)
for i in range(7):
 x=-.18+i*.06;h=.25*math.sqrt(1-(x/.22)**2)
 tube('Hand net vertical',[(-1.18+x,.40+.07*math.sin(j*math.pi/10),1.34-h+2*h*j/10) for j in range(11)],.004,tar,5)
for i in range(7):
 z=-.19+i*.063;w=.22*math.sqrt(1-(z/.25)**2)
 tube('Hand net cross',[(-1.18-w+2*w*j/10,.40+.07*math.sin(j*math.pi/10),1.34+z) for j in range(11)],.004,tar,5)
tube('Hand net lashing',[(-1.14,.31,1.02),(-1.26,.36,1.04),(-1.14,.45,1.06),(-1.08,.35,1.08)],.008,rope,6)

for ob in col.objects:
 if ob.type!='MESH':continue
 bpy.context.view_layer.objects.active=ob
 for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
 bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
bpy.context.view_layer.update()
points=np.array([ob.matrix_world@v.co for ob in col.objects if ob.type=='MESH' for v in ob.data.vertices]);lo=points.min(0);hi=points.max(0)
key='fishing-dock';dest=OUT/key;dest.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
for ob in col.objects:ob.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(dest/(key+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(dest/(key+'.blend')))
tri=sum(len(p.vertices)-2 for ob in col.objects if ob.type=='MESH' for p in ob.data.polygons)
(OUT/'models.json').write_text(json.dumps([dict(id=key,name="Pelican's messy fishing station",triangles=tri,dimensions_m=(hi-lo).tolist(),bounds_min=lo.tolist(),bounds_max=hi.tolist(),original_geometry=True)],indent=2)+'\n')
print('BUILT',key,tri,flush=True)
