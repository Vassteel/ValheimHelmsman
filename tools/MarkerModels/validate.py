"""Validate authored review geometry and retained placement markers."""
import bpy,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'design/maritime-markers-v2';rows=[]
for m in json.loads((root/'models.json').read_text()):
 bpy.ops.wm.open_mainfile(filepath=str(root/m['id']/(m['id']+'.blend')))
 obs=[o for o in bpy.data.collections['MODEL - standalone review asset'].objects if o.type=='MESH']
 assert all(len(o.data.polygons)>0 for o in obs),'Boolean erased an object'
 coords=[o.matrix_world@v.co for o in obs for v in o.data.vertices]
 assert all(math.isfinite(c) for v in coords for c in v)
 lo=[min(v[i] for v in coords) for i in range(3)];hi=[max(v[i] for v in coords) for i in range(3)]
 assert abs(lo[2])<1e-6,'Grounded base'
 assert max(abs(hi[i]-lo[i]-m['dimensions_m'][i]) for i in range(3))<1e-5
 tris=sum(len(p.vertices)-2 for o in obs for p in o.data.polygons);assert tris==m['triangles']
 assert (root/m['id']/(m['id']+'.glb')).stat().st_size>1000
 if m['id']=='wayfinder-marker':
  ob=next(o for o in obs if o.name=='Carved direction disc');p=[ob.matrix_world@v.co for v in ob.data.vertices]
  assert max(v.x for v in p)-min(v.x for v in p)>.93,'Disc must remain beneath relief'
 rows.append({'model':m['id'],'triangles':tris,'finite_geometry':True,'bounds_match':True,'no_empty_meshes':True,'base_grounded':True});print('PASS',m['id'],tris)
(root/'geometry-checks.json').write_text(json.dumps(rows,indent=2)+'\n')
