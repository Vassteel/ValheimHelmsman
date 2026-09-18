"""Validate the authored paddle hulls and actual cockpit openings with Blender."""
from pathlib import Path
import bpy,bmesh,json
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[2]
for kind,count in [('dugout',1),('kayak',1),('tandem',2)]:
 d=root/'design/paddle-craft-v1'/kind;bpy.ops.wm.open_mainfile(filepath=str(d/f'{kind}.blend'))
 objects=bpy.data.collections['MODEL - standalone review asset'].objects
 hull=objects['hull band - continuous carved shell'];bm=bmesh.new();bm.from_mesh(hull.data)
 assert all(e.is_manifold for e in bm.edges),(kind,'open hull shell')
 assert bm.calc_volume(signed=True)>0,(kind,'inward hull normals');bm.free()
 spec=json.loads((d/'runtime.json').read_text());seats=[p for p in spec['points'] if p['kind'] in ['seat','helm']]
 assert len(seats)==count and sum(p['kind']=='helm' for p in seats)==1
 decks=[o for o in objects if o.name.startswith('Finewood deck strip')]
 if kind!='dugout':
  assert len(decks)==14
  for seat in seats:
   x=seat['position'][2]+.10
   for offset in [-.2,0,.2]:
    start=Vector((x+offset,0,1.5))
    for ob in decks:
     tree=BVHTree.FromPolygons([v.co for v in ob.data.vertices],[tuple(p.vertices) for p in ob.data.polygons])
     assert tree.ray_cast(start,Vector((0,0,-1)),1.1)[0] is None,(kind,'deck seals cockpit')
 assert len([o for o in objects if o.name.startswith('Paddle blade')])==(1 if kind=='dugout' else 2)
 for point in spec['points']:
  if point['kind']!='ladder':continue
  x,y,z=point['position'];ex,ey,ez=point['exit'];sx,sy,sz=point['size']
  assert y-sy/2<spec['waterline']<y+sy/2 and abs(ex)<abs(x)
  assert any(abs(c['position'][0]-ex)<c['size'][0]/2 and abs(c['position'][2]-ez)<c['size'][2]/2 for c in spec['colliders'])
 print('PASS:',kind,'closed outward hull; open cockpits; correct seat/blade counts; supported water boarding.',flush=True)
