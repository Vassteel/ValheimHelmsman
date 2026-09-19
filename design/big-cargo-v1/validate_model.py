from pathlib import Path
import bpy,json,math,struct
from collections import Counter
from mathutils.bvhtree import BVHTree
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'big-cargo.blend'))
ship=bpy.data.collections['BIG CARGO - standalone review model']
assert sum(o.name.startswith('Freight / round timber') for o in ship.objects)==15
assert sum(o.name.startswith('Freight / ore bin bottom') for o in ship.objects)==6
assert sum(o.name.startswith('Freight / ore lump') for o in ship.objects)==66
for side in ['port','starboard']:
 assert sum(o.name.startswith('Boarding '+side+' rung') for o in ship.objects)==10
for o in ship.objects:
 assert o.type=='MESH'
 assert all(math.isfinite(c) for v in o.data.vertices for c in v.co)
 if o.name.startswith(('Cargo /','Freight /')):assert o.get('appearance_only') and o.get('future_collision')=='none'
oar=ship.objects['Steering oar / single carved oak']
edges=Counter(tuple(sorted((f.vertices[i],f.vertices[(i+1)%len(f.vertices)]))) for f in oar.data.polygons for i in range(len(f.vertices)))
assert all(n==2 for n in edges.values())
def bvh(o):return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[list(f.vertices) for f in o.data.polygons])
tree=bvh(oar)
for o in ship.objects:
 if o.name.startswith('Starboard clinker strake') or o.name=='Starboard gunwale':assert not tree.overlap(bvh(o)),'Oar intersects '+o.name
points=json.loads((p/'attachment-points.json').read_text())['points'];assert len(points)==8
raw=(p/'big-cargo.glb').read_bytes();assert struct.unpack_from('<4sII',raw)==(b'glTF',2,len(raw))
n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n])
assert sum('mesh' in a for a in doc['nodes'])==len(ship.objects)
assert len(doc.get('images',[]))>20 and all('bufferView' in im for im in doc['images'])
assert not doc.get('cameras')
assert not any('Studio' in a.get('name','') for a in doc['nodes'])
print('PASS: 15 timber logs, 6 ore bins, 66 ore lumps, 2 hull-following ladders, 8 attachment guides; closed steering oar clear of hull; finite geometry; textured ship-only GLB.')
print('Visual review asset only. No in-game movement, steering or flotation validation.')
