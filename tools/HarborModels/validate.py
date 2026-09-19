"""Validate authored review geometry and retained placement markers."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]/'design/harbor-refinement-v3'
results=[]
for m in json.loads((root/'models.json').read_text()):
 bpy.ops.wm.open_mainfile(filepath=str(root/m['id']/(m['id']+'.blend')))
 obs=list(bpy.data.collections['MODEL - standalone review asset'].objects)
 assert all(len(o.data.vertices)>0 and len(o.data.polygons)>0 for o in obs if o.type=='MESH'), 'Empty mesh after carving'
 coords=[o.matrix_world@v.co for o in obs if o.type=='MESH' for v in o.data.vertices]
 assert all(math.isfinite(c) for v in coords for c in v)
 lo=[min(v[i] for v in coords) for i in range(3)];hi=[max(v[i] for v in coords) for i in range(3)]
 assert max(abs(hi[i]-lo[i]-m['dimensions_m'][i]) for i in range(3))<1e-5
 tris=sum(len(p.vertices)-2 for o in obs if o.type=='MESH' for p in o.data.polygons)
 assert tris==m['triangles']
 assert (root/m['id']/(m['id']+'.glb')).stat().st_size>1000
 snaps=sum(o.get('runtime_tag')=='snappoint' for o in obs)
 assert snaps==(6 if m['id'] in ['pier-crane','heavy-crane'] else 0)
 if m['id']=='keel-cradle':
  pads=[o for o in obs if o.name.startswith('Padded hull bearing')]
  assert len(pads)==8
  for o in pads:assert abs(o.location.z-1.32)<1e-5 and abs(abs(o.location.x)-.78)<1e-5
 if m['id']=='heavy-crane':
  drum=next(o for o in obs if o.name.startswith('Windlass drum') and not o.name.startswith('Windlass drum flange'))
  p=[drum.matrix_world@v.co for v in drum.data.vertices]
  assert max(v.y for v in p)<-.30,'Windlass must stay ahead of A-frame legs'
 results.append({'model':m['id'],'triangles':tris,'snap_markers':snaps,'finite_geometry':True,'bounds_match':True})
 print('PASS',m['id'],tris,flush=True)
(root/'geometry-checks.json').write_text(json.dumps(results,indent=2)+'\n')
