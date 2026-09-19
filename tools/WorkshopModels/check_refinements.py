"""Check the reported pot clipping, prop scale and workstation chest access."""
import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
def bounds(o):
 p=[o.matrix_world@v.co for v in o.data.vertices]
 return tuple(min(v[i] for v in p) for i in range(3)),tuple(max(v[i] for v in p) for i in range(3))
def intersects(a,b):return all(a[0][i]<b[1][i] and a[1][i]>b[0][i] for i in range(3))
paint=R/'design/workshop-supplies-v1/paint-stand/paint-stand.blend';bpy.ops.wm.open_mainfile(filepath=str(paint))
objects=list(bpy.data.collections['MODEL - standalone review asset'].objects)
pots=[o for o in objects if o.name.startswith('Hollow pigment pot')];samples=[o for o in objects if o.name.startswith('Ship cloth sample')]
assert len(pots)==6 and len(samples)==3
for p in pots:
 pb=bounds(p);assert min(abs(pb[0][2]-h) for h in [1.055,1.103*(1.055/.795)])<1e-5,'Unsupported pot'
 for c in samples:assert not intersects(pb,bounds(c)),(p.name,c.name,'clipping')
print('PASS: all six hollow pots rest on their shelves and clear every hanging sample')
bpy.ops.wm.open_mainfile(filepath=str(R/'design/pelican-station-v1/fishing-dock/fishing-dock.blend'))
objects=list(bpy.data.collections['MODEL - standalone review asset'].objects)
route=((-0.79,-.9,.04),(.09,-.39,.69))
for o in objects:
 if o.type!='MESH':continue
 b=bounds(o)
 assert not intersects(b,route),(o.name,'blocks chest approach')
 if 'catch body' in o.name.lower() or 'catch belly' in o.name.lower():assert max(b[1][i]-b[0][i] for i in range(3))<1,'Oversized fish/basket transform'
print('PASS: chest approach is unobstructed; fish/basket scales are bounded')
