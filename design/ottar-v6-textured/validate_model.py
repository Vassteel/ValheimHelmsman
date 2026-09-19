from pathlib import Path
import bpy,json,struct,math
from mathutils.bvhtree import BVHTree
from collections import Counter
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'ottar.blend'))
ship=bpy.data.collections['OTTAR - standalone review model']
assert not any('grating' in o.name.lower() for o in ship.objects)
points=json.loads((p/'attachment-points.json').read_text())['points']
assert len(points)==8
assert sum(a['kind']=='boarding ladder' for a in points)==2
for side in ['port','starboard']:
 assert sum(o.name.startswith('Boarding ladder / '+side+' rung ') and 'knot' not in o.name for o in ship.objects)==9
# The lower rungs must tuck inward with the bilge, rather than hang in space.
for side in ['port','starboard']:
 rungs=[ship.objects['Boarding ladder / '+side+' rung %02d'%i] for i in range(1,10)]
 widths=[abs(sum(v.co.y for v in o.data.vertices)/len(o.data.vertices)) for o in rungs]
 assert widths[-1]<widths[0]-1.0,(side,widths)
 assert all(min(v.co.z for v in o.data.vertices)>.05 for o in rungs)
assert any(o.name.startswith('Deck gear / Bow mooring coil') for o in ship.objects)
assert any(o.name.startswith('Deck gear / Stern working coil') for o in ship.objects)
assert sum(a['kind']=='seated helm' for a in points)==1
assert sum(a['kind']=='passenger seat' for a in points)==4
assert sum(a['kind']=='standing holdfast' for a in points)==1
for prefix in ['Helmsman bench','Passenger seat 1','Passenger seat 2','Passenger seat 3','Passenger seat 4','Mast holdfast / grab bar']:
 assert any(o.name.startswith(prefix) for o in ship.objects),prefix
assert sum(o.name.startswith('Cargo / barrel lid') for o in ship.objects)==14
assert sum(o.name.startswith('Cargo / tied sack') for o in ship.objects)==18
assert sum(o.name.startswith('Cargo / deck-stowed hide roll') for o in ship.objects)==2
assert sum(o.name.startswith('Cargo / hide bale folded flap') for o in ship.objects)==14
assert len([o for o in ship.objects if o.name=='Steering oar / single carved oak'])==1
oar=ship.objects['Steering oar / single carved oak']
edges=Counter(tuple(sorted((f.vertices[i],f.vertices[(i+1)%len(f.vertices)]))) for f in oar.data.polygons for i in range(len(f.vertices)))
assert all(n==2 for n in edges.values()),'Steering oar is not a closed continuous surface'
adjacency={i:set() for i in range(len(oar.data.vertices))}
for a,b in edges:adjacency[a].add(b);adjacency[b].add(a)
seen=set();pending=[0]
while pending:
 v=pending.pop()
 if v not in seen:seen.add(v);pending.extend(adjacency[v]-seen)
assert len(seen)==len(oar.data.vertices),'Disconnected steering oar geometry'
def bvh(o):return BVHTree.FromPolygons([o.matrix_world@v.co for v in o.data.vertices],[list(f.vertices) for f in o.data.polygons])
oar_tree=bvh(oar)
for o in ship.objects:
 if o.name.startswith('Starboard clinker strake') or o.name=='Starboard gunwale':
  assert not oar_tree.overlap(bvh(o)),'Steering oar intersects '+o.name
assert not any(o.name in ['Steering oar / stock','Steering oar / blade','Steering tiller'] for o in ship.objects)
assert not any('/ back post' in o.name or '/ back rail' in o.name for o in ship.objects)
for o in ship.objects:
 if o.name.startswith('Cargo /'):
  assert all(abs(v.co.y)<2.0 for v in o.data.vertices),o.name+' extends to the rail exterior'
for o in ship.objects:
 assert all(math.isfinite(c) for v in o.data.vertices for c in v.co)
 if o.name.startswith('Cargo /'):assert o.get('appearance_only') and o.get('future_collision')=='none'
raw=(p/'ottar.glb').read_bytes();assert struct.unpack_from('<4sII',raw)==(b'glTF',2,len(raw))
n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n])
assert sum('mesh' in a for a in doc['nodes'])==len(ship.objects)
assert len(doc.get('images',[]))>20
assert all('bufferView' in im for im in doc['images'])
assert not doc.get('cameras')
assert not any('GUIDE' in a.get('name','') or 'Studio' in a.get('name','') for a in doc['nodes'])
print('PASS: no grating; increased cargo counts; all cargo inside the rails; five backless seats and one mast grab bar; eight attachment guides and two hull-following nine-rung boarding ladders and end-deck gear; closed one-piece steering oar clear of hull and gunwale; textured ship-only GLB.')
print('Movement and interaction remain unimplemented in Valheim; this validates the review asset only.')
