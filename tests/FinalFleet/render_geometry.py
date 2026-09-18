"""Check the actual evaluated models with backface-culling geometry, not just metadata.
Run with the project's bpy Python after exporting tools/FinalFleet/build.py.
"""
from pathlib import Path
import bpy,bmesh,json
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
for kind in ['ottar','freighter','snekkja','falkusa','ceol','currach']:
 folder=ROOT/'design/fleet-final'/kind
 bpy.ops.wm.open_mainfile(filepath=str(folder/(kind+'.blend')))
 objects=bpy.data.collections['Helmsman '+kind+' final'].objects
 checked=0
 for ob in objects:
  if not any(k in ob.name.lower() for k in ['barrel stave','stern skin','bow skin closure','tied sack','freight sack','knapsack']):continue
  if 'strap' in ob.name.lower():continue
  ev=ob.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();bm=bmesh.new();bm.from_mesh(me)
  assert all(e.is_manifold for e in bm.edges),(kind,ob.name,'open surface disappears from one side')
  assert bm.calc_volume(signed=True)>1e-8,(kind,ob.name,'inward-facing solid')
  bm.free();ev.to_mesh_clear();checked+=1
 if kind=='currach':assert 'Currach bow cap' in objects
 s=json.loads((folder/'runtime.json').read_text());mask=s['waterMask']
 assert len(mask)>=40 and len(mask)%2==0
 for left,right in zip(mask[::2],mask[1::2]):
  assert left[0]<right[0] and left[1]==right[1] and left[2]==right[2]
  assert left[1]>s['waterline']+.12,(kind,'mask is submerged')
 for p in s['points']:
  if p['kind']!='ladder':continue
  x,y,z=p['position'];sx,sy,sz=p['size'];ex,ey,ez=p['exit']
  assert y-sy/2<s['waterline']<y+sy/2,(kind,'boarding target misses waterline')
  assert p['facing'][0]*x<0 and abs(ex)<abs(x),(kind,'exit faces away from deck')
  blockers=[c for c in s['colliders'] if abs(c['position'][2]-z)<c['size'][2]/2+sz/2 and c['position'][0]*x>0]
  assert all(abs(x)+sx/2>abs(c['position'][0])+c['size'][0]/2 for c in blockers),(kind,'hull hides boarding target')
  supports=[c for c in s['colliders'] if abs(c['position'][0]-ex)<=c['size'][0]/2 and abs(c['position'][2]-ez)<=c['size'][2]/2 and c['position'][1]+c['size'][1]/2<=ey]
  assert supports,(kind,'boarding exit has no walkable support')
  assert ey-max(c['position'][1]+c['size'][1]/2 for c in supports)<.45,(kind,'boarding exit too far above deck')
 print('PASS:',kind,checked,'closed outward cargo/end surfaces; above-water fitted mask; reachable boarding strips and supported exits.',flush=True)
