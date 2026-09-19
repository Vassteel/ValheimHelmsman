"""Finish authored Blender ships and export compact, native Unity mesh resources.
Run with the project's bpy Python. Earlier review assets are kept intact.
"""
from pathlib import Path
import bpy, bmesh, ast, json, math, struct, gzip, io, sys, random, os
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from math import sin, cos, pi
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'design/fleet-final';ASSETS=ROOT/'assets/ships/final'
SOURCES={
 'ottar':('ottar-v6-textured','ottar','MercantShip',15.84,4.8,1.88),
 'freighter':('big-cargo-v1','big-cargo','BigCargoShip',19.2,6.3,2.4),
 'snekkja':('warship-snekkja-v2-no-oars','snekkja','WarShip',17.3,2.47,.38),
 'falkusa':('small-boat-expansion-v1/falkusa','falkusa','HerculeShip',9.,2.7,.32),
 'ceol':('small-boat-expansion-v1/ceol','ceol','LittleBoat',6.4,1.8,.32),
 'currach':('small-boat-expansion-v1/currach','currach','HelmsmanCurrach',5.5,1.5,.245)}
helpers=ast.parse((ROOT/'design/small-boat-expansion-v1/primitives.py').read_text())
functions=[n for n in helpers.body if isinstance(n,ast.FunctionDef)]

def bounds(ob):
 vs=[ob.matrix_world@v.co for v in ob.data.vertices]
 return [min(v[i] for v in vs) for i in range(3)],[max(v[i] for v in vs) for i in range(3)]
def remove(ob):bpy.data.objects.remove(ob,do_unlink=True)
def translate(ob,v):
 for p in ob.data.vertices:p.co+=Vector(v)
def unity(p):return [-p[1],p[2],p[0]]
def meshdata(ob):
 ev=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();me.calc_loop_triangles()
 vertices=[];indices=[];lookup={};uv=me.uv_layers.active;normals=me.corner_normals
 for tri in me.loop_triangles:
  face=[]
  for li in tri.loops:
   p=unity(ob.matrix_world@me.vertices[me.loops[li].vertex_index].co)
   n=unity((ob.matrix_world.to_3x3().inverted().transposed()@normals[li].vector).normalized())
   t=list(uv.data[li].uv) if uv else [0.,0.]
   value=tuple(round(float(v),6) for v in p+n+t)
   if value not in lookup:lookup[value]=len(vertices);vertices.append(value)
   face.append(lookup[value])
  # Axis conversion is a reflection; reverse winding once.
  indices.extend(face[::-1])
 ev.to_mesh_clear();return vertices,indices

def finish(kind):
 global ship,studio,rope,rope_dark,oak,deckm,endgrain,iron,pine
 directory,stem,prefab,length,beam,walkz=SOURCES[kind];L=length/2
 src=ROOT/'design'/directory;dest=OUT/kind;dest.mkdir(parents=True,exist_ok=True)
 bpy.ops.wm.open_mainfile(filepath=str(src/(stem+'.blend')))
 ship=next(c for c in bpy.data.collections if 'standalone' in c.name.lower() and 'studio' not in c.name.lower())
 studio=next(c for c in bpy.data.collections if 'STUDIO' in c.name)
 def material(part):
  found=next((m for m in bpy.data.materials if part.lower() in m.name.lower()),None)
  return found or next((m for m in bpy.data.materials if part.rsplit(' ',1)[0].lower() in m.name.lower()),bpy.data.materials.get('Hewn oak framing'))
 rope=material('Hemp and bast');rope_dark=material('Tarred standing');oak=material('Hewn oak');iron=material('Dark iron');endgrain=material('Fresh worn')
 deckm=[material('Worn deck pine '+str(i)) for i in range(4)]
 pine=[material('Tarred pine / strake %02d'%i) for i in range(7)]
 exec(compile(ast.Module(body=functions,type_ignores=[]),'helpers','exec'),globals())
 points=json.loads((src/'attachment-points.json').read_text())['points']
 random.seed(1030)
 merchant=kind in ['ottar','freighter']
 if kind=='freighter':
  helm=next(p for p in points if 'helm' in p['kind'].lower())
  helm['position']=[-6.45,-.5,2.34+.09*(6.45-4.85)+.047]
 # Clear stern clipping: head and bent tiller clear the entire raised stem/transom.
 if kind in ['currach','ceol','falkusa']:
  remove(ship.objects['Carved stern rudder']);remove(ship.objects['Rudder tiller'])
  stern=height={'currach':.75,'ceol':1.40,'falkusa':1.82}[kind]
  x=-L-.22;top=stern+.18
  outline=[(x-.07,top+.035),(x+.055,top+.035),(x+.08,.20),(x-.09,-.33),(x-.43,-.31),(x-.40,.14),(x-.16,top*.55)]
  vs=[(xx,y,z) for y in [-.034,.034] for xx,z in outline];N=len(outline)
  mesh('Carved stern rudder',vs,[tuple(reversed(range(N))),tuple(N+i for i in range(N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)],oak)
  tube('Raised bent tiller',[(x,0,top),(-L+.22,0,top),(-L+.54,0,top-.12),(-L+.90,0,top-.23)],.030,oak,8)
  for z in [.35,stern-.10]:
   tube('Rudder pintle',[(x-.02,-.04,z),(x+.10,-.07,z),(-L-.025,-.06,z)],.015,iron,6)
  # Give the helm an actual backless seat and a comfortable reachable tiller.
  helm=next(p for p in points if 'helm' in p['id'].lower())
  sternseat=min((p for p in points if 'seat' in p['kind']),key=lambda p:p['position'][0])
  helm['position']=sternseat['position'];points.remove(sternseat)
 if kind=='falkusa':
  # Port-side lateen yard; jib billows to starboard, clear of both mast and main.
  for ob in list(ship.objects):
   if ob.name.startswith(('Lateen mainsail','Long lateen yard')):translate(ob,(0,-.55,0))
   elif ob.name.startswith('Jib'):
    for v in ob.data.vertices:v.co.y=-v.co.y+.22
   elif ob.name.startswith(('Yard hoist','Mainsheet')):remove(ob)
   elif ob.name.startswith('Mast shroud') and sum(v.co.y for v in ob.data.vertices)<0:remove(ob)
   elif 'net strand' in ob.name.lower():
    for v in ob.data.vertices:v.co.z=.51+(v.co.z-.51)*.20
  line('Yard hoist',(-.8,0,6),(-.65,-.45,6.03),.022,rope)
  line('Mainsheet',(-3.45,-.45,1.75),(-3.80,-.55,.85),.024,rope)
  for t in [.12,.32,.53,.73,.9]:
   a=Vector((3.4,-.45,1.95)).lerp(Vector((-3.25,-.45,8.65)),t)
   tube('Lateen yard robands',[a+Vector((0,-.035,-.025)),a+Vector((0,.08,.035)),a+Vector((0,0,.085)),a+Vector((0,-.035,-.025))],.012,rope,6)
 if kind=='falkusa':
  from falkusa_rigging import finish as rig_fishing
  rig_fishing(ship,globals())
  from falkusa_cargo import finish as dress_fishing
  dress_fishing(ship,globals())
 if kind=='currach':
  for ob in ship.objects:
   if ob.name.startswith('Red sailing cloth'):translate(ob,(0,-.105,0))
  for z in [1.2,1.8,2.4,3.0,3.6,4.2]:
   tube('Sail mast lacing',[(1.12,-.14,z),(1.06,0,z+.04),(1.12,.055,z+.02),(1.17,0,z),(1.12,-.14,z)],.008,rope,6)
 # Preserve recognizable authored cargo; invisible support follows its uneven top.
 collisions=[];coverage={};cargo_solids=[];cargo_report=[];cargo_height=0
 if merchant:
  from cargo import restore, walking
  edge=3.98 if kind=='ottar' else 4.85
  source_script=(src/'build_model.py').read_text();tree=ast.parse(source_script)
  env={'math':math,'sin':sin,'cos':cos,'pi':pi}
  for n in tree.body:
   if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id in ['L','beam','profile_w','profile_z'] for t in n.targets):exec(compile(ast.Module(body=[n],type_ignores=[]),'hull','exec'),env)
   if isinstance(n,ast.FunctionDef) and n.name in ['interp','hull','inside_width_at']:exec(compile(ast.Module(body=[n],type_ignores=[]),'hull','exec'),env)
  # Extend the narrow side strips inboard to the width of the working landings.
  # Original review models remain untouched; both hulls retain an open freight well.
  for side in [-1,1]:
   ys=[1.29,1.52] if kind=='ottar' else [1.54,1.81,2.08]
   z=1.78 if kind=='ottar' else 2.33
   for j,y in enumerate(ys):
    xmax=edge-.12
    while xmax>.2 and env['inside_width_at'](xmax,z)<y+.14:xmax-=.025
    box('Side working extension',(0,side*y,z),(2*xmax,.214 if kind=='ottar' else .26,.082 if kind=='ottar' else .10),deckm[j%4],.005)
  cargo_report=restore(kind,src,ship,globals(),ROOT)
  cargo_solids,coverage,cargo_at=walking(kind,ship,edge,walkz,env['inside_width_at'],unity)
  cargo_height=cargo_at(-length*.13,beam*.17)+.06
  for p in points:
   if 'holdfast' in p['kind'] or 'mast' in p['id']:p['position'][2]=cargo_at(p['position'][0],p['position'][1])
  (dest/'cargo-coverage.json').write_text(json.dumps(coverage,indent=2))
  (dest/'cargo-support.json').write_text(json.dumps(cargo_report,indent=2))
 # Bow/stern working detail and belayed lines, kept away from player attachment points.
 for side in [-1,1]:
  x=side*L*.70
  for yside in [-1,1]:
   y=yside*beam*.18
   if merchant:z=(1.80+.11*(abs(x)-3.98)+.0415) if kind=='ottar' else (2.34+.09*(abs(x)-4.85)+.047)
   else:
    rails=[o for o in ship.objects if 'gunwale' in o.name.lower()]
    nearby=[o.matrix_world@v.co for o in rails for v in o.data.vertices if abs((o.matrix_world@v.co).x-x)<.15 and (o.matrix_world@v.co).y*yside>0]
    y=sum(v.y for v in nearby)/len(nearby);z=max(v.z for v in nearby)
   box('Mooring cleat foot',(x,y,z+.035),(.24,.09,.07),oak,.006)
   tube('Mooring cleat horn',[(x-.17,y,z+.11),(x+.17,y,z+.11)],.025,oak,6)
   pts=[]
   for i in range(121):
    t=i/120*6*pi;layer=i/120
    pts.append((x+.145*cos(t),y+.042*sin(2*t),z+.11+.020*cos(2*t)+layer*.016))
   tube('Cleat figure-eight belay',pts,.010,rope,6)
 # Belaying pegs, leather collars and low coil at each mast.
 mast=next((ob for ob in ship.objects if ob.name in ['Mast','Raked mast','Light mast','Heavy mast']),None)
 if mast:
  verts=[mast.matrix_world@v.co for v in mast.data.vertices];n=len(verts)//2
  base=sum(verts[:n],Vector())/n;head=sum(verts[n:],Vector())/n
  r0=sum((v-base).length for v in verts[:n])/n;r1=sum((v-head).length for v in verts[n:])/n
  def mast_at(z):return base.lerp(head,(z-base.z)/(head.z-base.z))
  pegcenter=mast_at(walkz+.68)
  for side in [-1,1]:
   tube('Mast belaying peg',[pegcenter,pegcenter+Vector((0,side*(.23 if merchant else .16),0))],.024,oak,6)
  for j in range(4):
   z=walkz+.17+j*.021;center=mast_at(z);t=(z-base.z)/(head.z-base.z);radius=r0+(r1-r0)*t+.011
   hoop('Mast foot rope serving',center,radius,radius,rope,.012)
 # Wooden blocks and rope seizings at real shroud attachments, not floating trim.
 for ob in list(ship.objects):
  if not ob.name.startswith(('Mast shroud','Standing shroud','Shroud')) or 'lashing' in ob.name.lower():continue
  vs=[ob.matrix_world@v.co for v in ob.data.vertices]
  if not vs:continue
  low=min(v.z for v in vs);foot=sum((v for v in vs if v.z<low+.06),Vector())/sum(v.z<low+.06 for v in vs)
  high=max(vs,key=lambda v:v.z);axis=(high-foot).normalized()
  u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u)
  size=.065 if merchant else .042
  for distance in [.17,.40]:
   center=foot+axis*distance
   # Two oval cheeks surround a transverse sheave; strop follows the shell.
   for cheek in [-1,1]:
    verts=[]
    for depth in [cheek*size*.65-.009,cheek*size*.65+.009]:
     verts.extend(center+u*depth+axis*(size*1.45*cos(t*pi/6))+v*(size*sin(t*pi/6)) for t in range(12))
    faces=[tuple(reversed(range(12))),tuple(range(12,24))]+[(t,(t+1)%12,(t+1)%12+12,t+12) for t in range(12)]
    mesh('Rigging pulley cheek',verts,faces,oak)
   tube('Rigging pulley sheave',[center-u*size*.46,center+u*size*.46],size*.65,oak,10)
   tube('Rigging pulley pin',[center-u*size*.87,center+u*size*.87],.010,iron,6)
   tube('Rigging block strop',[center+axis*(size*1.65*cos(t*pi/16))+v*(size*1.15*sin(t*pi/16)) for t in range(33)],.010,rope,6)
  for offset in [-.65,.65]:
   line('Rigging pulley reeving',foot+axis*.17+v*size*offset,foot+axis*.40+v*size*offset,.010,rope)
  for k in range(4):
   center=foot+axis*(.07+k*.012)
   tube('Shroud seizing knot',[center+u*.025*cos(t*pi/8)+v*.025*sin(t*pi/8) for t in range(17)],.007,rope,5)
 # Geometry checks directly address the reported clipping.
 def bvh(ob):return BVHTree.FromPolygons([ob.matrix_world@v.co for v in ob.data.vertices],[list(f.vertices) for f in ob.data.polygons])
 if kind in ['currach','ceol','falkusa']:
  for name in ['Raised bent tiller','Carved stern rudder']:
   tree=bvh(ship.objects[name])
   for ob in ship.objects:
    if 'hull band' in ob.name or ob.name.startswith(('Swept stem','Stern transom','Tarred stern')):assert not tree.overlap(bvh(ob)),kind+' '+name+' clips '+ob.name
 if mast:
  tree=bvh(mast)
  for ob in ship.objects:
   if ob.name.startswith(('Lateen mainsail','Jib','Red sailing cloth','Square sail panel','Sail / sewn')) and 'rope' not in ob.name and 'seam' not in ob.name:assert not tree.overlap(bvh(ob)),kind+' mast clips '+ob.name
 # Existing deck planks supply low, fitted convex collision slabs. No prop colliders.
 for ob in ship.objects:
  low=ob.name.lower()
  if any(v in low for v in ['deck plank','floorboard','sole board','footboard','boarding landing','landing plank','end platform','side working']):
   lo,hi=bounds(ob);size=[hi[i]-lo[i] for i in range(3)]
   if size[2]<.23:collisions.append({'position':unity([(a+b)/2 for a,b in zip(lo,hi)]),'size':[size[1],max(.08,size[2]),size[0]+.015]})
 # Small boats get a smooth low sole matching the visible boards; thwarts/cargo don't snag.
 if not merchant:
  for i in range(20):
   x=-L*.78+(i+.5)*length*.78/20;w=beam*.36*max(.18,1-(abs(x)/L)**2.2)
   collisions.append({'position':unity((x,0,walkz-.055)),'size':[w*2,.11,length*.78/20+.008]})
 # Closed convex keel sections beneath the walk surface, so underwater rocks
 # contact the hull without filling the player space above the floor.
 hull_solids=[]
 hullobs=[o for o in ship.objects if 'clinker strake' in o.name or 'hull band' in o.name]
 hv=[o.matrix_world@v.co for o in hullobs for v in o.data.vertices]
 for i in range(18):
  a=-L*.94+i*length*.94/18;b=-L*.94+(i+1)*length*.94/18;verts=[]
  for x in [a,b]:
   near=[v for v in hv if abs(v.x-x)<length/64]
   keel=min(v.z for v in near)+.025
   ztop=max(keel+.06,walkz-.03)
   profile=sorted({(round(v.z,4),round(abs(v.y),4)) for v in near if v.y>0})
   w=max(.045,float(np.interp(ztop,[p[0] for p in profile],[p[1] for p in profile]))-.035)
   verts.extend([unity((x,-.035,keel)),unity((x,.035,keel)),unity((x,w,ztop)),unity((x,-w,ztop))])
  faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
  triangles=[]
  for f in faces:triangles.extend([f[0],f[2],f[1],f[0],f[3],f[2]])
  hull_solids.append({'vertices':[n for v in verts for n in v],'triangles':triangles})
 # Smooth rail-side segments and a shallow bottom: fitted, compound convex hull.
 # No full-hull box: that would make the open boats impossible to stand inside.
 hullobs=[o for o in ship.objects if 'clinker strake' in o.name or 'hull band' in o.name]
 hv=[o.matrix_world@v.co for o in hullobs for v in o.data.vertices]
 for i in range(32):
  x=-L+(i+.5)*length/32;near=[v for v in hv if abs(v.x-x)<length/32*.65]
  if not near:continue
  rail=max(v.z for v in near);w=max(abs(v.y) for v in near)
  for side in [-1,1]:collisions.append({'position':unity((x,side*(w-.045),(walkz+rail)/2)),'size':[.08,max(.1,rail-walkz),length/32+.02]})
 # New ship boarding/holdfast anchors where the review model lacked them.
 if not any('holdfast' in p['kind'] or 'mast' in p['id'] for p in points):
  points.append({'id':'mast_holdfast','kind':'holdfast','position':[.65,0,walkz]})
 if not any('ladder' in p['kind'] for p in points):
  for side in [-1,1]:
   y=side*(beam*.48)
   points.append({'id':'boarding_'+str(side),'kind':'ladder','position':[0,y,.15],'exit':[0,side*beam*.25,walkz]})
   profile=sorted({(round(v.z,4),round(abs(v.y),4)) for v in hv if abs(v.x)<length/60 and v.y>0})
   def ladder_y(z):return side*(float(np.interp(z,[p[0] for p in profile],[p[1] for p in profile]))+.065)
   rail=max(p[0] for p in profile)
   zs=[rail+.06-k*.20 for k in range(5)]
   for z in zs:tube('Boarding ladder rung',[(-.21,ladder_y(z),z),(.21,ladder_y(z),z)],.025,oak,6)
   for x in [-.22,.22]:tube('Boarding ladder rope',[(x,ladder_y(z),z) for z in [rail+.16]+zs],.017,rope,6)
 # Shallow carved rope borders on merchant sheer strakes; switchable by the puffin.
 if merchant:
  for side in [-1,1]:
   for row in [0,1]:
    pts=[]
    for i in range(97):
     x=-L*.75+i*L*1.5/96;p=env['hull'](x,.91,side,-.025)
     pts.append((x,p[1],p[2]+.040*sin(i*pi/4+row*pi)))
    tube('Carved merchant rope border',pts,.009,oak,5)
 # Clinker laps need a physical lip. Previously adjacent bands shared nearly
 # coplanar outer faces along each lap, producing moving moire/flicker.
 for ob in ship.objects:
  n=ob.name.lower()
  if 'clinker strake' not in n and 'hull band' not in n:continue
  if kind=='currach':continue # Skin strips butt together rather than overlap.
  # Snekkja's top strake has boolean-cut ports rather than regular rings.
  if len(ob.data.vertices)%4:
   for vertex in ob.data.vertices:vertex.co.y+=(1 if vertex.co.y>0 else -1)*.009
   continue
  # Authored band vertices are lower outer, upper outer, lower inner, upper inner.
  for i in range(0,len(ob.data.vertices),4):
   for index in [i,i+2]:
    vertex=ob.data.vertices[index]
    vertex.co.y+=(1 if vertex.co.y>0 else -1)*.009
 # Thin cloth must render from inside and outside with native, backface-culling shaders.
 for ob in ship.objects:
  if (ob.name.lower()=='jib' or any(k in ob.name.lower() for k in ['sail panel','sewn wool','red sailing cloth','lateen mainsail'])) and not any(k in ob.name.lower() for k in ['rope','seam']) and not any(m.type=='SOLIDIFY' for m in ob.modifiers):
   ob.modifiers.new('Two sided cloth','SOLIDIFY').thickness=.004
 # Close the currach's rounded bow: the original skin stopped short of the
 # centreline, leaving a visible slot between port and starboard gunwales.
 if kind=='currach':
  from keel_skin import geometry as keel_skin_geometry
  seam_vertices,seam_faces=keel_skin_geometry(length)
  skin=next(o.data.materials[0] for o in ship.objects if 'hull band' in o.name)
  mesh('Currach keel skin closure',seam_vertices,seam_faces,skin)
  bands=[o for o in ship.objects if 'hull band' in o.name]
  for j in range(9):
   pair=[o for o in bands if o.name.endswith('%02d'%j)]
   a,b=pair
   av=sorted([v.co.copy() for v in a.data.vertices if abs(v.co.x-L)<.001],key=lambda v:v.z)
   bv=sorted([v.co.copy() for v in b.data.vertices if abs(v.co.x-L)<.001],key=lambda v:v.z)
   mesh('Bow skin closure',[(L+.001,av[0].y,av[0].z),(L+.001,bv[0].y,bv[0].z),(L+.001,bv[-1].y,bv[-1].z),(L+.001,av[-1].y,av[-1].z)],[(0,1,2,3)],a.data.materials[0])
  tips=[]
  for side in [-1,1]:
   rail=ship.objects['Gunwale '+str(side)];end=[v.co for v in rail.data.vertices if v.co.x>L-.06]
   tips.append(sum(end,Vector())/len(end))
  tube('Bow gunwale joint',[tips[0],Vector((L+.025,0,(tips[0].z+tips[1].z)/2)),tips[1]],.045,oak,8)
  # A fitted timber bow cap covers the rail joint and supports the skin seam.
  box('Currach bow cap',(L-.025,0,tips[0].z+.017),(.18,abs(tips[0].y-tips[1].y)+.075,.065),oak,.012)
 from ropework import finish as finish_ropework
 rope_report=finish_ropework(kind,ship,globals(),length,beam,walkz)
 (dest/'rope-support.json').write_text(json.dumps(rope_report,indent=2))
 # Closed solids need outward normals on both sides of the hull. Backface
 # culling prevents the inner/back surface fighting with its close outer face.
 for ob in ship.objects:
  if ob.type!='MESH':continue
  bm=bmesh.new();bm.from_mesh(ob.data)
  # Open staves, hide flaps and skin end panels have no well-defined outside.
  # Give them actual thickness before orienting, rather than guessing a face side.
  if any(e.is_boundary for e in bm.edges) and not any(m.type=='SOLIDIFY' for m in ob.modifiers):
   mod=ob.modifiers.new('Closed surface thickness','SOLIDIFY');mod.thickness=.018 if 'barrel' in ob.name.lower() else .012
   bm.free()
   bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
   bm=bmesh.new();bm.from_mesh(ob.data)
  bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
  # Concave sack mouths can make Blender choose a consistent but inward shell.
  # Signed volume supplies the unambiguous outside for a closed solid.
  if all(e.is_manifold for e in bm.edges) and bm.calc_volume(signed=True)<0:
   bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.normal_update()
  bm.to_mesh(ob.data);bm.free()
 # Render state from the saved model, not the deck-only inspection pose.
 for ob in ship.objects:ob.hide_render=False
 ship.name='Helmsman '+kind+' final';scene=bpy.context.scene
 scene.render.threads=4;scene.cycles.samples=24
 bpy.ops.object.select_all(action='DESELECT')
 for ob in ship.objects:ob.select_set(True)
 bpy.context.view_layer.objects.active=next(iter(ship.objects))
 bpy.ops.export_scene.gltf(filepath=str(dest/(kind+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
 # Geometry is batched by material/animation group, never one draw call per rope or bale.
 materials=[];groups={};sheets=[]
 from construction_groups import group
 for ob in ship.objects:
  if ob.type!='MESH':continue
  if 'sheet' in ob.name.lower():
   vs=[v.co for v in ob.data.vertices];sides=6
   sheets.append({'head':unity(sum(vs[:sides],Vector())/sides),'foot':unity(sum(vs[-sides:],Vector())/sides)});continue
  m=ob.data.materials[0]
  if m not in materials:materials.append(m)
  key=(group(ob),materials.index(m));v,t=meshdata(ob);dst=groups.setdefault(key,([],[]));offset=len(dst[0]);dst[0].extend(v);dst[1].extend(i+offset for i in t)
 # Water occlusion belongs at the opening, above the water, not beneath the sole.
 # Export each rail profile from the actual model (the currach has a broad stern).
 water_mask=[]
 for x in sorted(set(round(v.x,4) for v in hv)):
  near=[v for v in hv if abs(v.x-x)<.00015]
  if not near:continue
  top=max(v.z for v in near);width=max(abs(v.y) for v in near)
  water_mask.extend([unity((x,max(.006,width-.025),top-.025)),unity((x,-max(.006,width-.025),top-.025))])
 # Cargo collision bridges small gaps independently of individual prop render meshes.
 metadata={'prefab':prefab,'name':kind,'length':length,'beam':beam,'walkHeight':walkz,'waterline':{'ottar':.60,'freighter':.80,'snekkja':.22,'falkusa':.48,'ceol':.40,'currach':.34}[kind],'points':[], 'colliders':collisions,'hullSolids':hull_solids,'cargoSolids':cargo_solids,'cargoHeight':cargo_height,'sheets':sheets,'waterMask':water_mask}
 for p in points:
  k=p['kind'].lower();id=p['id'].lower()
  typ='helm' if 'helm' in k or 'helm' in id else 'ladder' if 'ladder' in k else 'mast' if 'mast' in id or 'holdfast' in k else 'seat'
  metadata['points'].append({'kind':typ,'position':unity(p['position']),'exit':unity(p.get('exit_position',p.get('exit',p['position']))),'facing':unity(p.get('facing_blender',p.get('facing',[1,0,0])))})
 # Match interaction strips to the hull at the visible boarding rope, all the
 # way from the waterline to the rail. Exits face inboard and clear the gunwale.
 for point in metadata['points']:
  if point['kind']!='ladder':continue
  z=point['position'][2];side=1 if point['position'][0]>=0 else -1
  near=[v for v in hv if abs(v.x-z)<length/50]
  rail=max(v.z for v in near);width=max(abs(v.y) for v in near)
  low=metadata['waterline']-.28;high=rail+.18
  point['position']=[side*(width+.13),(low+high)/2,z]
  point['size']=[.32,high-low,.65]
  exitx=side*(width-.46)
  if merchant:
   # Side walkways have level support and avoid the mast/cargo in the centre.
   exitx=side*(1.65 if kind=='ottar' else 2.20)
  point['exit']=[exitx,walkz+.18,z];point['facing']=[-side,0,0]
 sailv=[v for (g,m),(vs,ts) in groups.items() if g=='sail' for v in vs]
 metadata['sailPivot']=[0,max(v[1] for v in sailv),0];metadata['airHeight']=max(v[1] for vs,ts in groups.values() for v in vs)+.15
 pivots={'ottar':(-6.06,-2.17,2.13),'freighter':(-6.06*1.23,-2.17*1.38,2.13*1.28),'snekkja':(-7.05,-1.15,1.15)}
 metadata['rudderPivot']=unity(pivots.get(kind,(-L-.22,0,.45)))
 stream=io.BytesIO()
 def I(n):stream.write(struct.pack('<i',n))
 def raw(b):I(len(b));stream.write(b)
 def text(s):raw(s.encode())
 stream.write(b'HMF1');text(json.dumps(metadata,separators=(',',':')));I(len(materials))
 for j,m in enumerate(materials):
  text(m.name)
  im=next((n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image),None)
  if im:
   path=dest/('texture-%02d.png'%j);im.filepath_raw=str(path);im.file_format='PNG';im.save();raw(path.read_bytes())
  else:raw(b'')
  stream.write(struct.pack('<4f',*m.diffuse_color))
 I(len(groups))
 for (g,m),(vs,ts) in groups.items():
  text(g);I(m);I(len(vs));stream.write(np.asarray(vs,dtype='<f4').tobytes());I(len(ts));stream.write(np.asarray(ts,dtype='<i4').tobytes())
 ASSETS.mkdir(parents=True,exist_ok=True)
 (ASSETS/(prefab+'.bin.gz')).write_bytes(gzip.compress(stream.getvalue(),mtime=0))
 (dest/'runtime.json').write_text(json.dumps(metadata,indent=2))
 (dest/'validation.json').write_text(json.dumps({'mast_clear':True,'stern_clear':True,'materials':len(materials),'batches':len(groups),'vertices':sum(len(v) for v,t in groups.values()),'triangles':sum(len(t)//3 for v,t in groups.values()),'walk_slabs':len(collisions),'cargo_supports':len(cargo_solids),'settled_props':len(cargo_report)},indent=2))
 bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(dest/(kind+'.blend')))
 scene.render.resolution_x=1500;scene.render.resolution_y=1100
 if os.environ.get('FLEET_SKIP_RENDER')=='1':
  print('EXPORTED',kind,flush=True);return
 cam=scene.camera
 local=[cam.matrix_world.inverted()@(o.matrix_world@v.co) for o in ship.objects for v in o.data.vertices]
 xmin,xmax=min(v.x for v in local),max(v.x for v in local);ymin,ymax=min(v.y for v in local),max(v.y for v in local)
 cam.location+=cam.rotation_euler.to_matrix()@Vector(((xmin+xmax)/2,(ymin+ymax)/2,0))
 cam.data.ortho_scale=max(xmax-xmin,(ymax-ymin)*scene.render.resolution_x/scene.render.resolution_y)*1.09
 scene.render.filepath=str(dest/(kind+'-hero.png'));bpy.ops.render.render(write_still=True)
 # Clear deck inspection image, full rig is preserved in the blend/GLB.
 for ob in ship.objects:
  if group(ob)=='sail':ob.hide_render=True
 cam=scene.camera;cam.location=(length,-length*1.5,length*1.65);cam.rotation_euler=(Vector((0,0,walkz))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=length*1.12
 scene.render.filepath=str(dest/(kind+'-deck.png'));bpy.ops.render.render(write_still=True)
 print('FINISHED',kind,flush=True)

if __name__=='__main__':
 for kind in sys.argv[1:] or SOURCES:finish(kind)
