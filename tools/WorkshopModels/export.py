"""Export authored workshop meshes and snap markers; no source-bundle dependency."""
import bpy, json, gzip, io, struct, sys
from pathlib import Path
from collections import defaultdict
R=Path(__file__).resolve().parents[2];ROOT=R/'design/workshop-supplies-v1';DEST=R/'assets/workshop';DEST.mkdir(exist_ok=True)
def unity(v):return (v.x,v.z,-v.y)
for key in [m['id'] for m in json.loads((ROOT/'models.json').read_text())]:
 if len(sys.argv)>1 and key not in sys.argv[1:]:continue
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/key/(key+'.blend')))
 groups=defaultdict(lambda:([],[]));materials=[];snaps=[]
 for o in bpy.data.collections['MODEL - standalone review asset'].objects:
  if o.type=='EMPTY' and o.name.startswith('snap_'):snaps.append((o.name,unity(o.location)));continue
  if o.type!='MESH':continue
  me=o.data;me.calc_loop_triangles();nm=o.matrix_world.to_3x3().inverted().transposed();uv=me.uv_layers.active
  for tri in me.loop_triangles:
   m=me.materials[tri.material_index]
   if m not in materials:materials.append(m)
   tag='static'
   if key=='slipway':
    if o.name.startswith(('Movable cradle','Keel bearing','Cradle angled','Cradle hull','Cradle securing')):tag='cradle'
    elif o.name.startswith(('Capstan drum','Capstan iron','Capstan rope','Capstan turning')):tag='capstan'
    elif o.name.startswith('Cradle haul line'):tag='haul'
   vs,ts=groups[(materials.index(m),tag)]
   a,b,c=[me.vertices[i].co for i in tri.vertices];face=(b-a).cross(c-a).normalized()
   for loop in tri.loops:
    v=me.vertices[me.loops[loop].vertex_index];p=unity(o.matrix_world@v.co)
    normal=me.corner_normals[loop].vector
    # Tight rope bends can have smoothing normals across opposing faces.
    if normal.dot(face)<=0:normal=face
    n=unity((nm@normal).normalized());t=uv.data[loop].uv if uv else (0,0)
    ts.append(len(vs));vs.append((*p,*n,*t))
 stream=io.BytesIO()
 def I(n):stream.write(struct.pack('<i',n))
 def text(s):b=s.encode();I(len(b));stream.write(b)
 stream.write(b'HMW2' if key=='slipway' else b'HMW1');I(len(materials))
 for m in materials:text(m.name);stream.write(struct.pack('<4f',*m.diffuse_color))
 I(len(groups))
 for (mi,tag),(vs,ts) in groups.items():
  if key=='slipway':text(tag)
  I(mi);I(len(vs))
  for v in vs:stream.write(struct.pack('<8f',*v))
  I(len(ts));stream.write(struct.pack('<'+'i'*len(ts),*ts))
 I(len(snaps))
 for name,point in snaps:text(name);stream.write(struct.pack('<3f',*point))
 (DEST/(key+'.bin.gz')).write_bytes(gzip.compress(stream.getvalue(),mtime=0))
 print(key,len(groups),'batches',sum(len(t)//3 for v,t in groups.values()),'triangles',len(snaps),'snap points')
