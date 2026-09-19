"""Conservative per-part budget pass on packed authored assets; preserve gameplay metadata.
Original bytes are backed up once. Hull seams, sail, rudder and paddle topology are protected.
"""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,bmesh,struct,gzip,io,json,math,shutil,sys
from pathlib import Path
R=Path(__file__).resolve().parents[2];D=R/'design/asset-optimization-v1';D.mkdir(exist_ok=True)
B=R/'.build/asset-budget-baseline';B.mkdir(exist_ok=True)

def unpack(path):
 data=gzip.decompress(path.read_bytes());f=io.BytesIO(data)
 def I():return struct.unpack('<i',f.read(4))[0]
 def raw():return f.read(I())
 magic=f.read(4);spec=json.loads(raw()) if magic==b'HMF1' else None
 for _ in range(I()):
  raw()
  if spec:raw()
  f.read(16)
 header=data[:f.tell()];parts=[]
 for _ in range(I()):
  tag=raw().decode() if magic!=b'HMW1' else 'static';mi=I();nv=I();v=struct.unpack('<'+'f'*nv*8,f.read(nv*32));vs=[v[i:i+8] for i in range(0,len(v),8)];nt=I();ts=struct.unpack('<'+'i'*nt,f.read(nt*4));parts.append((tag,mi,vs,ts))
 return magic,spec,header,parts,f.read()

def simplify(vs,ts,ratio):
 me=bpy.data.meshes.new('budget');me.from_pydata([v[:3] for v in vs],[],[ts[i:i+3] for i in range(0,len(ts),3)]);me.update()
 uv=me.uv_layers.new()
 for loop in me.loops:uv.data[loop.index].uv=vs[loop.vertex_index][6:8]
 # Weld triangle soup while retaining separate per-corner UVs.
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bm.to_mesh(me);bm.free()
 # Recompute explicit flat normals for timber; preserve smooth normals where the
 # input mesh was smooth by transferring closest identical-position normals.
 normals={tuple(v[:3]):v[3:6] for v in vs}
 me.normals_split_custom_set([normals.get(tuple(me.vertices[l.vertex_index].co),(0,1,0)) for l in me.loops])
 obj=bpy.data.objects.new('budget',me);bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj
 mod=obj.modifiers.new('Conservative silhouette reduction','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True
 bpy.ops.object.modifier_apply(modifier=mod.name)
 me=obj.data;me.calc_loop_triangles();uv=me.uv_layers.active;out=[];indices=[];lookup={}
 for tri in me.loop_triangles:
  a,b,c=[me.vertices[i].co for i in tri.vertices];face=(b-a).cross(c-a)
  if face.length_squared<1e-16:continue
  face.normalize()
  for li in tri.loops:
   p=me.vertices[me.loops[li].vertex_index].co;n=me.corner_normals[li].vector
   if n.dot(face)<=0:n=face
   row=(*p,*n,*uv.data[li].uv);k=struct.pack('<8f',*row)
   if k not in lookup:lookup[k]=len(out);out.append(row)
   indices.append(lookup[k])
 bpy.data.objects.remove(obj,do_unlink=True);bpy.data.meshes.remove(me)
 return out,indices

paths=list((R/'assets/ships/final').glob('*.bin.gz'))+[R/'assets/workshop'/f'{k}.bin.gz' for k in ['slipway','fishing-dock','rigging-rack']]
report=[]
for path in sorted(paths):
 only=next((a.split("=",1)[1] for a in sys.argv if a.startswith("--asset=")),None)
 if only and path.name!=only:continue
 backup=B/path.name
 if not backup.exists():shutil.copy2(path,backup)
 magic,spec,header,parts,tail=unpack(backup);before=sum(len(ts)//3 for _,_,_,ts in parts)
 target=45000 if path.name in ['MercantShip.bin.gz','BigCargoShip.bin.gz'] else 30000 if path.name=='WarShip.bin.gz' else 20000
 protected={'sail','rudder','paddle','haul'}
 fixed=sum(len(ts)//3 for tag,mi,vs,ts in parts if tag in protected or len(ts)<600)
 ratio=min(1,max(.16,(target-fixed)/max(1,before-fixed)))
 # Independently simplifying adjoining strakes opens visible seams at the bow.
 # Keep authored hull surfaces without increasing reduction pressure elsewhere.
 protected.add('hull')
 new=[]
 for tag,mi,vs,ts in parts:
  if tag in protected or len(ts)<600 or before<=target:new.append((tag,mi,vs,ts));continue
  original_bounds=[(min(v[i] for v in vs),max(v[i] for v in vs)) for i in range(3)]
  nv,nt=simplify(vs,ts,ratio)
  # Reject catastrophic loss or dropped parts rather than forcing the budget.
  valid=bool(nv) and len(nt)>=3
  if valid:
   for i,(lo,hi) in enumerate(original_bounds):
    tol=max(.008,(hi-lo)*.015)
    if abs(min(v[i] for v in nv)-lo)>tol or abs(max(v[i] for v in nv)-hi)>tol:valid=False
  new.append((tag,mi,nv,nt) if valid else (tag,mi,vs,ts))
  if not valid:print('PRESERVED bounds',path.name,tag,mi,flush=True)
 stream=io.BytesIO();stream.write(header)
 def I(n):stream.write(struct.pack('<i',n))
 def text(s):b=s.encode();I(len(b));stream.write(b)
 I(len(new))
 for tag,mi,vs,ts in new:
  if magic!=b'HMW1':text(tag)
  I(mi);I(len(vs))
  for v in vs:stream.write(struct.pack('<8f',*v))
  I(len(ts));stream.write(struct.pack('<'+'i'*len(ts),*ts))
 stream.write(tail)
 output=D/path.name;output.write_bytes(gzip.compress(stream.getvalue(),mtime=0))
 after=sum(len(t)//3 for _,_,v,t in new)
 entry=dict(asset=path.name,source=str(path.relative_to(R)),before=before,triangles=after,vertices_before=sum(len(v) for _,_,v,t in parts),vertices=sum(len(v) for _,_,v,t in new),target=target,protected_groups=sorted(set(t for t,_,_,_ in parts)&protected),metadata_unchanged=True)
 report.append(entry);print('OPTIMIZED',entry,flush=True)
report_path=D/'optimization.json'
if any(a.startswith('--asset=') for a in sys.argv) and report_path.exists():
 changed={r['asset'] for r in report};report=[r for r in json.loads(report_path.read_text()) if r['asset'] not in changed]+report
report_path.write_text(json.dumps(report,indent=2)+'\n')
