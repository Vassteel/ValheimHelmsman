"""Four original maritime marker studies, with real recessed carving."""
from pathlib import Path
import sys
R=Path(__file__).resolve().parents[2]
source=R/'tools/WorkshopModels/build.py';ns={'__file__':str(source)}
exec(compile(source.read_text().split('MODELS=')[0],str(source),'exec'),ns)
globals().update({k:v for k,v in ns.items() if k!='__file__'})
OUT=R/'design/maritime-markers-v2';OUT.mkdir(exist_ok=True)
bone=mat('Weathered bone',(.50,.46,.35));oldwood=mat('Salt worn carved timber',(.26,.215,.145),grain=True)

def bake(ob):
 bpy.context.view_layer.objects.active=ob
 for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)

def subtract(ob,cutter):
 bake(ob)
 for obj in [ob,cutter]:
  bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
 bpy.context.view_layer.update();bpy.context.view_layer.objects.active=ob
 mod=ob.modifiers.new('Cut into solid material','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
 bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)

def groove(ob,points,r=.013):subtract(ob,tube('Temporary carving chisel',points,r,wood,6))

def profile(name,outline,depth,material,y=0):
 n=len(outline);v=[(x,y+side*depth/2,z) for side in [-1,1] for x,z in outline]
 f=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,v,f,material,bevel=.008)

def post(h=1.5,w=.40,d=.34):
 box('Broad hewn foot',(0,0,.10),(w+.20,d+.17,.20),oldwood,.025)
 ob=box('Carved timber shaft',(0,0,(h+.2)/2),(w,d,h-.2),oldwood,.020)
 # Chips and checks are incisions, not raised black ropes on the face.
 for x,z,l in [(-w*.31,.32,.24),(w*.22,.72,.19),(-w*.08,h-.18,.13)]:
  groove(ob,[(x,-d/2-.003,z),(x+.008,-d/2+.004,z+l*.45),(x-.004,-d/2-.002,z+l)],.006)
 for j in range(4):
  z=.28+j*.028
  tube('Base rope wrap',[(-w/2-.009,-d/2-.01,z),(w/2+.009,-d/2-.01,z+.008),(w/2+.009,d/2+.01,z),(-w/2-.009,d/2+.01,z-.004),(-w/2-.009,-d/2-.01,z)],.011,rope,6)
 tube('Binding tucked tail',[(w/2+.012,-d/2-.013,.34),(w/2+.016,-d/2-.02,.26),(w/2-.05,-d/2-.019,.22)],.012,rope,6)
 # Cut irregular adze strokes at the corners; keep the emblem faces readable.
 for side in [-1,1]:
  for j in range(3):
   z=.43+j*(h-.57)/3
   groove(ob,[(side*(w/2-.011),-d/2+.012,z),(side*(w/2+.006),-d/2+.021,z+.085)],.014)
 # Binding knot and uneven hanging end are attached to the lowest wrap.
 tube('Base binding knot',[(w/2+.016,-d/2-.022,.31),(w/2-.033,-d/2-.041,.34),(w/2-.045,-d/2-.020,.29),(w/2+.016,-d/2-.022,.31),(w/2-.023,-d/2-.035,.26)],.010,rope,6)
 return ob

def wavecut(ob,z,y=-.171,width=.14):
 groove(ob,[(width*math.sin(t*math.pi/12),y,z+t*.018) for t in range(25)],.012)

def gullwatch():
 shaft=post(1.36,.40,.34)
 for z in [.52,.72,.92]:groove(shaft,[(-.14,-.169,z),(-.06,-.163,z+.08),(.02,-.163,z+.09),(.10,-.169,z+.02),(.15,-.169,z+.05)],.012)
 box('Gull carved plinth',(0,0,1.385),(.57,.43,.09),edge,.016)
 # All parts are timber: a folk carving, not another living worker bird.
 body=ellipsoid('Gull carved body',(0,.025,1.69),(.25,.165,.30),oldwood)
 for j in range(3):
  z=1.56+j*.09
  groove(body,[(-.105,-.113,z),(-.008,-.136,z-.021),(.093,-.12,z+.006)],.006)
 head=ellipsoid('Gull carved head',(0,-.085,1.965),(.17,.145,.17),wood)
 for side in [-1,1]:
  subtract(head,ellipsoid('Eye chisel',(side*.137,-.164,1.99),(.035,.04,.039),wood))
  wing=profile('Carved folded wing',[(side*.13,1.86),(side*.285,1.74),(side*.23,1.47),(side*.10,1.43)],.055,wood,y=-.086)
  for k in range(3):groove(wing,[(side*(.17+k*.026),-.116,1.74-k*.01),(side*(.14+k*.026),-.116,1.50+k*.015)],.006)
 mesh('Gull wooden bill',[(-.06,-.19,1.98),(.06,-.19,1.98),(0,-.405,1.93),(-.058,-.19,1.925),(.058,-.19,1.925)],[(0,1,2),(3,2,4),(0,2,3),(1,4,2),(0,3,4,1)],edge,bevel=.006)
 for x in [-.085,.085]:
  beam('Carved bird foot',(x,-.07,1.43),(x,-.02,1.52),.045,.055,wood)
  box('Flat carved toes',(x,-.09,1.445),(.09,.14,.038),wood,.007)
 profile('Wooden tail feathers',[(-.12,1.59),(0,1.32),(.13,1.58)],.075,wood,y=.17)


def serpent():
 # A continuous python-like body climbs a rounded, hewn timber marker.
 box('Broad hewn foot',(0,0,.10),(.61,.57,.20),oldwood,.025)
 shaft=tube('Octagonal tide marker',[(0,0,.20),(0,0,1.91)],.207,oldwood,8)
 for x,z,l in [(-.075,.35,.21),(.056,.91,.24),(-.018,1.53,.22)]:
  groove(shaft,[(x,-.195,z),(x+.012,-.194,z+l*.5),(x+.004,-.198,z+l)],.006)
 # Foot binding is separate from the sculpted animal and stays below its tail.
 for j in range(3):ring('Marker foot binding',(0,0,.25+j*.027),.214,.214,.011,rope,32)
 tube('Marker binding tail',[(.15,-.16,.29),(.19,-.15,.25),(.20,-.19,.205)],.010,rope,6)
 points=[];radii=[];angles=[]
 turns=2.40;N=169
 for i in range(N):
  t=i/(N-1);angle=-math.pi/2-turns*math.tau+turns*math.tau*t
  # Radius grows with the animal; its belly remains against the post.
  radius=.018+.073*min(1,t/.26)-.023*max(0,(t-.80)/.20)
  center_radius=.196+radius
  points.append(Vector((center_radius*math.cos(angle),center_radius*math.sin(angle),.41+1.37*t)))
  radii.append(radius);angles.append(angle)
 # An S-shaped upper neck remains attached to the crown, with head facing forward.
 for xyz,r in [((.018,-.256,1.83),.070),((.040,-.222,1.90),.075),((.058,-.218,1.98),.075),((.070,-.250,2.045),.071)]:
  points.append(Vector(xyz));radii.append(r)
 verts=[];sides=12
 for i,c in enumerate(points):
  tangent=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
  radial=Vector((c.x,c.y,0)).normalized();u=(radial-tangent*radial.dot(tangent)).normalized();v=tangent.cross(u).normalized()
  for j in range(sides):
   theta=j*math.tau/sides
   verts.append(tuple(c+radii[i]*(u*math.cos(theta)+v*math.sin(theta))))
 faces=[tuple(range(sides-1,-1,-1))]
 faces += [(i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j) for i in range(len(points)-1) for j in range(sides)]
 faces.append(tuple((len(points)-1)*sides+j for j in range(sides)))
 body=mesh('Continuous climbing serpent',verts,faces,wood)
 # Small overlapping V-shaped incisions follow the outside of the coiled body.
 for i in range(23,N-5,7):
  c=points[i];angle=angles[i];outward=Vector((math.cos(angle),math.sin(angle),0));along=Vector((-math.sin(angle),math.cos(angle),.15)).normalized()
  q=c+outward*(radii[i]-.002)
  groove(body,[q-along*.027+Vector((0,0,.014)),q-Vector((0,0,.011)),q+along*.027+Vector((0,0,.014))],.0035)
 # Broad snake head with a tapered snout, sculpted as one closed mesh.
 verts=[];sections=[(-.20,2.02,.067,.070),(-.32,2.085,.147,.092),(-.47,2.06,.125,.068),(-.575,2.04,.060,.043)]
 for y,z,w,h in sections:
  for j in range(12):
   t=j*math.tau/12;verts.append((.070+w*math.cos(t),y,z+h*math.sin(t)))
 faces=[tuple(range(11,-1,-1))]+[(i*12+j,i*12+(j+1)%12,(i+1)*12+(j+1)%12,(i+1)*12+j) for i in range(3) for j in range(12)]+[tuple(36+j for j in range(12))]
 head=mesh('Carved python head',verts,faces,wood)
 for side in [-1,1]:
  subtract(head,ellipsoid('Snake eye socket cutter',(.070+side*.137,-.354,2.116),(.021,.027,.024),wood))
  ellipsoid('Inset carved snake eye',(.070+side*.132,-.354,2.116),(.012,.017,.013),oldwood)
  groove(head,[(.070+side*.131,-.34,2.047),(.070+side*.123,-.465,2.035),(.070+side*.060,-.572,2.030)],.004)
  subtract(head,ellipsoid('Snake nostril cutter',(.070+side*.046,-.561,2.071),(.009,.013,.010),wood))
 groove(head,[(.07,-.29,2.174),(.07,-.37,2.16),(.07,-.46,2.126)],.004)


def wayfinder():
 shaft=post(1.10,.30,.28)
 for z in [.50,.66,.82]:groove(shaft,[(-.075,-.14,z),(.075,-.14,z+.07)],.01)
 disc=tube('Carved direction disc',[(0,.075,1.45),(0,-.115,1.45)],.47,wood,24)
 # Eight-point compass relief and an engraved rim; no text is implied.
 groove(disc,[(.405*math.cos(i*math.tau/64),-.117,1.45+.405*math.sin(i*math.tau/64)) for i in range(64)],.012)
 outline=[]
 for i in range(16):
  a=math.pi/2+i*math.tau/16;r=(.34 if i%4==0 else .25) if i%2==0 else .074
  outline.append((r*math.cos(a),1.45+r*math.sin(a)))
 profile('Eight point compass relief',outline,.04,edge,y=-.132)
 ellipsoid('Compass centre boss',(0,-.175,1.45),(.048,.024,.048),wood)
 for i in range(8):
  a=i*math.tau/8+.1;groove(disc,[(.36*math.cos(a),-.117,1.45+.36*math.sin(a)),(.39*math.cos(a),-.117,1.45+.39*math.sin(a))],.008)
 groove(disc,[(.20,-.117,1.86),(.18,-.111,1.77),(.21,-.117,1.69)],.005)
 for x,z in [(-.30,1.71),(.29,1.19)]:
  tube('Compass oak fastening peg',[(x,-.112,z),(x,-.127,z)],.013,oldwood,6)
 # A single short bound strip adds asymmetry without hiding the emblem.
 tube('Disc rim lashing',[(-.32,-.14,1.12),(-.37,-.14,1.17),(-.37,.10,1.17),(-.32,.10,1.12),(-.32,-.14,1.12)],.012,rope,6)


def wreckward():
 shaft=post(1.40,.48,.28)
 back=profile('Split wreck timber backboard',[(-.31,1.12),(-.32,1.82),(-.23,2.01),(-.08,1.96),(.015,2.075),(.18,1.97),(.31,1.91),(.29,1.10)],.18,oldwood,y=.045)
 groove(back,[(-.07,-.049,2.0),(-.04,-.044,1.82),(-.09,-.049,1.65)],.009)
 # A bone warning emblem mounted against timber, with hollow sockets and nose.
 skull=ellipsoid('Weathered skull carving',(0,-.115,1.65),(.25,.19,.275),bone)
 for x,z in [(-.095,1.71),(.098,1.695)]:subtract(skull,ellipsoid('Eye socket chisel',(x,-.275,z),(.075,.105,.086),bone))
 subtract(skull,profile('Nose chisel',[(-.044,1.53),(.049,1.53),(.008,1.635)],.23,bone,y=-.255))
 groove(skull,[(.035,-.252,1.86),(.012,-.291,1.81),(.039,-.304,1.775)],.004)
 for side in [-1,1]:
  ellipsoid('Skull cheek',(side*.17,-.20,1.49),(.07,.08,.095),bone)
  beam('Jaw hinge',(side*.19,-.13,1.46),(side*.15,-.19,1.34),.044,.049,bone)
 tube('Lower jaw', [(-.15,-.19,1.35),(-.10,-.27,1.325),(.08,-.27,1.32),(.15,-.19,1.35)],.035,bone,8)
 for i in range(6):
  x=-.105+i*.041
  ob=box('Uneven carved tooth',(0,0,0),(.032,.044,.054+(i%2)*.009),bone,.005);ob.location=(x,-.274,1.416-(i%3)*.004);ob.rotation_euler.y=(i-2)*.024
 tube('Skull mounting peg',[(0,.08,1.68),(0,-.12,1.68)],.035,wood,8)
 for z in [.73,.91]:
  groove(shaft,[(-.16,-.139,z),(.16,-.139,z+.12)],.015)
  groove(shaft,[(.16,-.139,z),(-.16,-.139,z+.12)],.015)
 for j in range(3):tube('Backboard securing cord',[(-.27,-.065,1.16+j*.022),(.27,-.065,1.16+j*.022),(.27,.15,1.16+j*.022),(-.27,.15,1.16+j*.022),(-.27,-.065,1.16+j*.022)],.010,rope,6)

MODELS=[('gullwatch-post','Gullwatch post','Totem1',gullwatch),('tide-serpent','Tide serpent','Totem2',serpent),('wayfinder-marker','Wayfinder marker','Totem3',wayfinder),('wreckward-marker','Wreckward marker','Totem4',wreckward)]
manifest=json.loads((OUT/'models.json').read_text()) if len(sys.argv)>1 and (OUT/'models.json').exists() else []
for key,label,replaces,make in MODELS:
 if len(sys.argv)>1 and key not in sys.argv[1:]:continue
 for ob in list(col.objects):bpy.data.objects.remove(ob,do_unlink=True)
 make()
 for ob in col.objects:
  if ob.type!='MESH':continue
  bake(ob);bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
 bpy.context.view_layer.update();points=np.array([ob.matrix_world@v.co for ob in col.objects if ob.type=='MESH' for v in ob.data.vertices]);lo=points.min(0);hi=points.max(0)
 dest=OUT/key;dest.mkdir(exist_ok=True);bpy.ops.object.select_all(action='DESELECT')
 for ob in col.objects:ob.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(dest/(key+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
 bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(dest/(key+'.blend')))
 tri=sum(len(p.vertices)-2 for ob in col.objects if ob.type=='MESH' for p in ob.data.polygons)
 manifest=[m for m in manifest if m['id']!=key];manifest.append(dict(id=key,name=label,replaces=replaces,triangles=tri,dimensions_m=(hi-lo).tolist(),bounds_min=lo.tolist(),bounds_max=hi.tolist(),original_geometry=True))
 print('BUILT',key,tri,flush=True)
manifest.sort(key=lambda m:[k for k,*_ in MODELS].index(m['id']));(OUT/'models.json').write_text(json.dumps(manifest,indent=2)+'\n')
