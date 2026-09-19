from pathlib import Path
import bpy,json,math,struct
from collections import Counter
from mathutils.bvhtree import BVHTree
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'snekkja.blend'))
ship=bpy.data.collections['SNEKKJA - standalone review model']
assert sum(o.name.startswith('Rowing oar ') for o in ship.objects)==26
assert sum(o.name.startswith('Shield board ') for o in ship.objects)==26
assert sum(o.name.startswith('Rowing thwart ') for o in ship.objects)==26 # two boards per thwart
assert not any('temporary' in o.name for o in bpy.data.objects)
for o in ship.objects:
 assert all(math.isfinite(c) for v in o.data.vertices for c in v.co)
def tree(o):return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[list(f.vertices) for f in o.data.polygons])
oar=ship.objects['Steering oar single carved oak']
edges=Counter(tuple(sorted((f.vertices[i],f.vertices[(i+1)%len(f.vertices)]))) for f in oar.data.polygons for i in range(len(f.vertices)))
assert all(n==2 for n in edges.values()),'Steering mesh is not closed'
t=tree(oar)
for o in ship.objects:
 if o.name.startswith('Starboard clinker strake') or o.name=='Starboard gunwale':assert not t.overlap(tree(o)),'Steering intersects '+o.name
# Check the cut ports against the deployed rowing shafts.
for side,title in [('port','Port'),('starboard','Starboard')]:
 upper=tree(ship.objects[title+' clinker strake 07'])
 for o in ship.objects:
  if o.name.startswith('Rowing oar ') and o.name.endswith(side):assert not upper.overlap(tree(o)),'Rowing oar intersects upper strake: '+o.name
points=json.loads((p/'attachment-points.json').read_text())['points'];assert len(points)==27
raw=(p/'snekkja.glb').read_bytes();assert struct.unpack_from('<4sII',raw)==(b'glTF',2,len(raw))
n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n])
assert sum('mesh' in a for a in doc['nodes'])==len(ship.objects)
assert len(doc.get('images',[]))>15 and all('bufferView' in im for im in doc['images'])
assert not doc.get('cameras')
assert not any('Studio' in a.get('name','') for a in doc['nodes'])
print('PASS: 13 thwarts, 26 oars, 26 shields; actual oarports clear of shafts; closed steering oar clear of hull; finite geometry; embedded textures and ship-only GLB.')
print('Historically informed visual study only; no in-game movement, rowing or flotation acceptance.')
