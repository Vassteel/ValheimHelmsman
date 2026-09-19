"""Build optimized authored harbor assets with explicit mechanical animation groups."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import sys,json,math,gzip,struct,io
from pathlib import Path
from collections import defaultdict
import bpy,bmesh
from mathutils import Vector
R=Path(__file__).resolve().parents[2];OUT=R/'design/harbor-runtime-v1';OUT.mkdir(exist_ok=True)
ns={'__file__':str(R/'tools/HarborModels/build.py')}
exec(compile((R/'tools/HarborModels/build.py').read_text().split('\nMODELS=')[0],ns['__file__'],'exec'),ns)
col=ns['col'];base_tube=ns['tube'];base_sheave=ns['sheave'];base_purchase=ns['purchase']
state={};rigs=[]
def rig(objects,tag,kind,pivot=(0,0,0),axis=(0,0,1),lift=0,ratio=1):
 if not objects:return
 rigs.append(dict(tag=tag,kind=kind,pivot=list(pivot),axis=list(axis),lift=lift,ratio=ratio))
 for o in objects:
  if o.type=='MESH':o['rig']=tag

def tube(name,pts,r,m,sides=8,r2=None):
 pts=list(pts)
 if name.startswith(('Loose irregular rope coil','Wound hauling rope','Groove retaining flange','Grooved wooden sheave','Rope following sheave')):
  pts=pts[::2]+([] if (len(pts)-1)%2==0 else [pts[-1]])
 if name=='Loose irregular rope coil':
  pts=[pts[round(i*(len(pts)-1)/32)] for i in range(33)]
 if r<=.025:sides=min(sides,6)
 o=base_tube(name,pts,r,m,sides,r2)
 if name=='Continuous reeved purchase':
  upper,lower,number,tail=state['purchase'];weights=[]
  for idx,p in enumerate(pts):
   w=1 if p[2]<upper-.01 else 0
   tail_count=(len(tail) if isinstance(tail,list) else 1) if tail is not None else 0
   if tail_count and idx>=len(pts)-tail_count:w=0
   if tail is None and idx>=len(pts)-2:w=-2*number
   weights.extend([w]*sides)
  # One weight per vertex; straight spans interpolate between the anchored ends.
  o['weights']=weights;o['rig']='running-rope'
 return o
ns['tube']=tube

def sheave(c,r=.15,number=1,rope_arc=True):
 before=set(col.objects);base_sheave(c,r,number,rope_arc);obs=set(col.objects)-before
 lower=state.get('lower');moving=lower is not None and abs(c[2]-lower)<.01
 wheels=[o for o in obs if o.name.startswith(('Solid sheave','Groove retaining','Grooved wooden'))]
 index=state.get('wheel',0);state['wheel']=index+1
 rig(wheels,'wheel'+str(index),'wheel',c,(0,1,0),1 if moving else 0,1/r)
 for o in obs-set(wheels):o['rig']='load' if moving else 'detail'
ns['sheave']=sheave

def purchase(c,lower,r=.16,number=1,tail=None):
 state['lower']=lower;state['purchase']=(c[2],lower,number,tail)
 before=set(col.objects);base_purchase(c,lower,r,number,tail)
 for o in set(col.objects)-before:
  if o.name.startswith(('Lower block becket','Dead end seizure')):o['rig']='load'
  elif o.name.startswith(('Tail whipping',)):o['rig']='free-tail'
  elif o.name.startswith('Forged attachment eye') and 'rig' not in o:o['rig']='load'
 state.pop('lower',None)
ns['purchase']=purchase
base_hook=ns['hook']
def hook(c,size=1):
 before=set(col.objects);base_hook(c,size)
 for o in set(col.objects)-before:o['rig']='load'
ns['hook']=hook

MODELS=[('ShipConstruction','keel-cradle','Keel cradle','cradle'),('ShipConstruction1','roller-bed','Launching rollers','rollers'),('ShipConstruction2','framing-rack','Ship framing gantry','framing_rack'),('PierCrane1','pier-crane','Timber pier crane','crane'),('PierCrane2','heavy-crane','Braced pier crane','crane'),('PulleyCobia','single-pulley','Single block','pulley'),('PulleyElephantSeal','double-pulley','Double purchase','pulley'),('PulleyMarlin','triple-pulley','Heavy purchase','pulley')]
old={m['id']:m for m in json.loads((R/'design/harbor-refinement-v3/models.json').read_text())};manifest=[]
def unity(v):return [v[0],v[2],-v[1]]
for prefab,key,label,fn in MODELS:
 for o in list(col.objects):bpy.data.objects.remove(o,do_unlink=True)
 rigs=[];state={}
 if fn=='crane':ns[fn](key=='heavy-crane')
 elif fn=='pulley':ns[fn](['single-pulley','double-pulley','triple-pulley'].index(key))
 else:ns[fn]()
 bpy.context.view_layer.update();ns['finish_timber']()
 lift=.45 if key=='framing-rack' else .55 if fn=='crane' else .18 if fn=='pulley' else 0
 if any(o.get('rig')=='load' for o in col.objects):rig([], 'load','lift')
 rigs.extend([dict(tag='load',kind='lift',pivot=[0,0,0],axis=[0,0,1],lift=1,ratio=1),dict(tag='free-tail',kind='lift',pivot=[0,0,0],axis=[0,0,1],lift=-2*(['single-pulley','double-pulley','triple-pulley'].index(key)+1) if fn=='pulley' else 0,ratio=1)])
 for o in col.objects:
  n=o.name
  if o.type!='MESH':continue
  if n.startswith(('Forged hook','Lifting spreader','Spreader bridle','Cargo sling')):o['rig']='load'
  if fn=='crane' and n.startswith('Forged attachment eye') and 'rig' not in o:o['rig']='load'
  if fn=='crane' and n.startswith(('Windlass spindle','Windlass drum','Wound hauling','Crank','Ratchet wheel','Ratchet tooth')):o['rig']='winch'
  if key=='single-pulley' and n.startswith('Hanging tackle fall'):
   # Opposing falls translate equally and oppositely, keeping each upper end attached.
   side=1 if sum(v.co.x for v in o.data.vertices)<0 else -1
   o['weights']=[side*max(0,min(1,(.44-v.co.z)/.67)) for v in o.data.vertices];o['rig']='running-rope'
 if fn=='crane':rig([o for o in col.objects if o.get('rig')=='winch'],'winch','wheel',(.62,-.47,1.02),(1,0,0),0,-(4 if key=='heavy-crane' else 2)/.156)
 if key=='roller-bed':
  for i,y in enumerate([-2,-1,0,1,2]):
   objects=[o for o in col.objects if o.name.startswith(('Timber launching roller','Roller end iron band','Roller spindle')) and abs(sum((o.matrix_world@v.co).y for v in o.data.vertices)/len(o.data.vertices)-y)<.03]
   rig(objects,'roller'+str(i),'roller',(0,y,.43),(1,0,0),0,1/.20)
 # Coarse collision excludes rope, fittings and moving blocks. Roller collision
 # remains stationary: rotating a cylinder around its axle preserves its surface.
 solids=('Cradle sleeper','Keel support','Splayed hull shore','Padded hull bearing','Cradle longitudinal tie','Roller bed runner','Roller bearing','Roller track stop','Framing rack foot','Framing upright','Framing diagonal','Framing head','Gantry corner','Dock bearer','Driven timber pile','Pile knee','Deck board','Crane kingpost','Kingpost brace','Crane A-frame','Crane backstay','A-frame cross','Jib pivot cross','Strut heel','Crane jib','Jib compression','Pulley wall rail')
 for o in col.objects:
  if o.type!='MESH':continue
  if 'rig' not in o:o['rig']='static' if o.name.startswith(solids) else 'detail'
  bpy.context.view_layer.objects.active=o
  for mod in list(o.modifiers):
   if mod.type=='BEVEL':mod.segments=1
   bpy.ops.object.modifier_apply(modifier=mod.name)
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
  if 'weights' in o:assert len(o['weights'])==len(o.data.vertices)
 bpy.context.view_layer.update()
 tris=sum(len(p.vertices)-2 for o in col.objects if o.type=='MESH' for p in o.data.polygons)
 assert tris<=20000,(key,tris)
 dest=OUT/key;dest.mkdir(exist_ok=True)
 spec=dict(key=key,prefab=prefab,name=label,lift=lift,rigs=rigs)
 (dest/'rig.json').write_text(json.dumps(spec,indent=2)+'\n')
 bpy.ops.wm.save_as_mainfile(filepath=str(dest/(key+'.blend')))
 bpy.ops.object.select_all(action='DESELECT')
 for o in col.objects:o.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(dest/(key+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
 # Pack in native Unity coordinates and weld equal position/normal/UV/weight tuples.
 groups=defaultdict(lambda:([],[],{}));mats=[];snaps=[]
 for o in col.objects:
  if o.type=='EMPTY' and o.get('runtime_tag')=='snappoint':snaps.append((o.name,unity(o.location)));continue
  if o.type!='MESH':continue
  me=o.data;me.calc_loop_triangles();nm=o.matrix_world.to_3x3().inverted().transposed();uv=me.uv_layers.active;weights=o.get('weights')
  for tri in me.loop_triangles:
   mat=me.materials[tri.material_index]
   if mat not in mats:mats.append(mat)
   vs,ts,lookup=groups[(mats.index(mat),o['rig'])]
   a,b,c=[me.vertices[i].co for i in tri.vertices];face=(b-a).cross(c-a).normalized()
   for loop in tri.loops:
    vi=me.loops[loop].vertex_index;v=me.vertices[vi];normal=me.corner_normals[loop].vector
    if normal.dot(face)<=0:normal=face
    row=(*unity(o.matrix_world@v.co),*unity((nm@normal).normalized()),*(uv.data[loop].uv if uv else (0,0)),weights[vi] if weights else 0)
    # Float32 canonical key keeps the packed vertex count honest.
    packed=struct.pack('<9f',*row)
    if packed not in lookup:lookup[packed]=len(vs);vs.append(packed)
    ts.append(lookup[packed])
 data=io.BytesIO()
 def I(n):data.write(struct.pack('<i',n))
 def text(s):b=s.encode();I(len(b));data.write(b)
 data.write(b'HMW3');uspec=json.loads(json.dumps(spec))
 for r in uspec['rigs']:r['pivot']=dict(zip('xyz',unity(r['pivot'])));r['axis']=dict(zip('xyz',unity(r['axis'])))
 text(json.dumps(uspec,separators=(',',':')));I(len(mats))
 for m in mats:text(m.name);data.write(struct.pack('<4f',*m.diffuse_color))
 I(len(groups))
 for (mi,tag),(vs,ts,lookup) in groups.items():
  text(tag);I(mi);I(len(vs));data.write(b''.join(vs));I(len(ts));data.write(struct.pack('<'+'i'*len(ts),*ts))
 I(len(snaps))
 for n,v in snaps:text(n);data.write(struct.pack('<3f',*v))
 (R/'assets/workshop'/('harbor-'+key+'.bin.gz')).write_bytes(gzip.compress(data.getvalue(),mtime=0))
 coords=[o.matrix_world@v.co for o in col.objects if o.type=='MESH' for v in o.data.vertices]
 manifest.append(dict(id=key,prefab=prefab,name=label,before=old[key]['triangles'],triangles=tris,vertices=sum(len(vs) for vs,ts,l in groups.values()),batches=len(groups),snaps=len(snaps),dimensions_m=[max(v[i] for v in coords)-min(v[i] for v in coords) for i in range(3)]))
 print('PACKED',manifest[-1],flush=True)
(OUT/'models.json').write_text(json.dumps(manifest,indent=2)+'\n')
