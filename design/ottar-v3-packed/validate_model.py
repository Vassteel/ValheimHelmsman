from pathlib import Path
import bpy,json,struct,math
p=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(p/'ottar.blend'))
ship=bpy.data.collections['OTTAR - standalone review model']
assert not any('grating' in o.name.lower() for o in ship.objects)
points=json.loads((p/'attachment-points.json').read_text())['points']
assert len(points)==6
assert sum(a['kind']=='seated helm' for a in points)==1
assert sum(a['kind']=='passenger seat' for a in points)==4
assert sum(a['kind']=='standing holdfast' for a in points)==1
for prefix in ['Helmsman bench','Passenger seat 1','Passenger seat 2','Passenger seat 3','Passenger seat 4','Mast holdfast / grab bar']:
 assert any(o.name.startswith(prefix) for o in ship.objects),prefix
assert sum(o.name.startswith('Cargo / barrel lid') for o in ship.objects)==12
assert sum(o.name.startswith('Cargo / tied sack') for o in ship.objects)==10
assert sum(o.name.startswith('Cargo / rail-lashed hide roll') for o in ship.objects)==2
for o in ship.objects:
 assert all(math.isfinite(c) for v in o.data.vertices for c in v.co)
 if o.name.startswith('Cargo /'):assert o.get('appearance_only') and o.get('future_collision')=='none'
raw=(p/'ottar.glb').read_bytes();assert struct.unpack_from('<4sII',raw)==(b'glTF',2,len(raw))
n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n])
assert sum('mesh' in a for a in doc['nodes'])==len(ship.objects)
assert not doc.get('cameras')
assert not any('GUIDE' in a.get('name','') or 'Studio' in a.get('name','') for a in doc['nodes'])
print('PASS: no grating; all requested cargo types; two rail rolls; five modelled seats and one mast grab bar; six attachment guides; valid ship-only GLB.')
print('Movement and interaction remain unimplemented in Valheim; this validates the review asset only.')
