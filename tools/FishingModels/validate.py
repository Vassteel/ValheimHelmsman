"""Validate authored review geometry and retained placement markers."""
import bpy,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[2]/'design/fishing-equipment-v2'
rows=[]
for m in json.loads((root/'models.json').read_text()):
 bpy.ops.wm.open_mainfile(filepath=str(root/m['id']/(m['id']+'.blend')))
 obs=list(bpy.data.collections['MODEL - standalone review asset'].objects)
 assert all(len(o.data.polygons)>0 for o in obs if o.type=='MESH'), 'Empty carved mesh'
 coords=[o.matrix_world@v.co for o in obs if o.type=='MESH' for v in o.data.vertices]
 assert all(math.isfinite(c) for v in coords for c in v)
 lo=[min(v[i] for v in coords) for i in range(3)];hi=[max(v[i] for v in coords) for i in range(3)]
 assert max(abs(hi[i]-lo[i]-m['dimensions_m'][i]) for i in range(3))<1e-5
 tris=sum(len(p.vertices)-2 for o in obs if o.type=='MESH' for p in o.data.polygons)
 assert tris==m['triangles']
 assert (root/m['id']/(m['id']+'.glb')).stat().st_size>1000
 snaps=sum(o.get('runtime_tag')=='snappoint' for o in obs)
 assert snaps==0
 if m['id']=='fish-oil-press':assert all(any(o.name==name for o in obs) for name in ['input','output','screw_pivot','platen'])
 rows.append({'model':m['id'],'triangles':tris,'snap_markers':snaps,'finite_geometry':True,'bounds_match':True})
 print('PASS',m['id'],tris,flush=True)
(root/'geometry-checks.json').write_text(json.dumps(rows,indent=2)+'\n')
