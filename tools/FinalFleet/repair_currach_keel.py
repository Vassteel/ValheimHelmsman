"""Append the authored seam without rebuilding or undoing existing optimization."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,bmesh,gzip,io,struct,json
from pathlib import Path
from keel_skin import geometry
from build import meshdata
R=Path(__file__).resolve().parents[2]
p=R/'design/fleet-final/currach/currach.blend';bpy.ops.wm.open_mainfile(filepath=str(p))
ship=bpy.data.collections['Helmsman currach final'];name='Currach keel skin closure'
if name in ship.objects:raise SystemExit('Keel skin already present; refusing duplicate append.')
material=next(o.data.materials[0] for o in ship.objects if 'hull band' in o.name)
vs,fs=geometry();me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
if bm.calc_volume(signed=True)<0:bmesh.ops.reverse_faces(bm,faces=list(bm.faces))
assert all(e.is_manifold for e in bm.edges)
bm.to_mesh(me);bm.free();ob=bpy.data.objects.new(name,me);ship.objects.link(ob);me.materials.append(material)
uv=me.uv_layers.new()
for loop in me.loops:
 v=me.vertices[loop.vertex_index].co;uv.data[loop.index].uv=(v.x/5.5+.5,v.y/.036+.5)
bpy.context.view_layer.update();newv,newt=meshdata(ob)
def patch(path):
 data=gzip.decompress(path.read_bytes());f=io.BytesIO(data)
 def I():return struct.unpack('<i',f.read(4))[0]
 def raw():return f.read(I())
 assert f.read(4)==b'HMF1';raw();materials=[]
 for _ in range(I()):materials.append(raw().decode());raw();f.read(16)
 header=data[:f.tell()];parts=[];patched=False
 for _ in range(I()):
  tag=raw();mi=I();nv=I();v=f.read(nv*32);nt=I();t=f.read(nt*4)
  if tag==b'hull' and materials[mi]==material.name:
   v+=b''.join(struct.pack('<8f',*row) for row in newv);t+=struct.pack('<'+'i'*len(newt),*(index+nv for index in newt));nv+=len(newv);nt+=len(newt);patched=True
  parts.append((tag,mi,nv,v,nt,t))
 assert patched and not f.read();out=io.BytesIO();out.write(header)
 def put(n):out.write(struct.pack('<i',n))
 put(len(parts))
 for tag,mi,nv,v,nt,t in parts:
  put(len(tag));out.write(tag);put(mi);put(nv);out.write(v);put(nt);out.write(t)
 path.write_bytes(gzip.compress(out.getvalue(),mtime=0));print('PATCHED',path.name,'+',len(newt)//3,'triangles; metadata and other parts unchanged',flush=True)
for path in [R/'assets/ships/final/HelmsmanCurrach.bin.gz',R/'design/asset-optimization-v1/HelmsmanCurrach.bin.gz',R/'.build/asset-budget-baseline/HelmsmanCurrach.bin.gz']:patch(path)
report=R/'design/fleet-final/currach/validation.json';data=json.loads(report.read_text());data['triangles']+=len(newt)//3;data['vertices']+=len(newv);data['keel_skin_closed']=True;report.write_text(json.dumps(data,indent=2)+'\n')
bpy.ops.wm.save_as_mainfile(filepath=str(p));bpy.ops.object.select_all(action='DESELECT')
for o in ship.objects:o.select_set(True)
bpy.ops.export_scene.gltf(filepath=str(p.with_suffix('.glb')),export_format='GLB',use_selection=True,export_apply=True)
