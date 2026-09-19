"""Structural asset checks only; these do not simulate Valheim movement."""
from pathlib import Path
import bpy,json,hashlib,struct
ROOT=Path(__file__).resolve().parent

def signature(obj):
 h=hashlib.sha256()
 for v in obj.data.vertices:h.update(str(tuple(v.co)).encode())
 for p in obj.data.polygons:h.update(str(tuple(p.vertices)).encode())
 return h.hexdigest()
def glb(path,expected):
 data=path.read_bytes();assert struct.unpack_from('<4sII',data)==(b'glTF',2,len(data))
 n=struct.unpack_from('<I',data,12)[0];doc=json.loads(data[20:20+n])
 assert not doc.get('cameras')
 assert sum('mesh' in x for x in doc['nodes'])==expected
 assert not any('GUIDE' in x.get('name','') or 'Studio' in x.get('name','') for x in doc['nodes'])

bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ottar.blend'))
ship=bpy.data.collections['OTTAR - standalone review model']
base={o.name:signature(o) for o in ship.objects if o.type=='MESH'}
walk=json.loads((ROOT/'walking-surface.json').read_text())['hold_walk_surface']
for o in ship.objects:
 if o.name.startswith('Cargo grating /'):
  assert max(v.co.z for v in o.data.vertices)<=walk['top']+.00001
  assert all(abs(v.co.x)<=walk['size'][0]/2+.00001 and abs(v.co.y)<=walk['size'][1]/2+.00001 for v in o.data.vertices),o.name
 if o.name.startswith('Cargo /'):
  assert o.get('future_collision')=='none',o.name
  assert max(v.co.z for v in o.data.vertices)<1.70,o.name+' penetrates grating'
assert sum(o.name.startswith('Cargo / barrel lid') for o in ship.objects)==12
assert sum(o.name.startswith('Cargo / wrapped trade bale') for o in ship.objects)==4
assert len(bpy.data.collections['COLLISION GUIDES - not included in GLB'].objects)==1
assert all(o.hide_render for o in bpy.data.collections['COLLISION GUIDES - not included in GLB'].objects)
glb(ROOT/'ottar.glb',len(base))
print('PASS: cargo clears grating; walking guide covers every grate; twelve barrels and four bales; guide is excluded from GLB.')
for style in json.loads((ROOT/'styles/styles.json').read_text())['styles']:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'styles'/style['blend']))
 ship=bpy.data.collections['OTTAR - '+style['name']]
 assert {o.name:signature(o) for o in ship.objects if o.type=='MESH'}==base
 decor=bpy.data.collections['Ottar decorative trim - optional']
 assert all(o.get('appearance_only') and o.get('collision')=='none' for o in decor.objects)
 glb(ROOT/'styles'/style['glb'],len(base)+len(decor.objects))
 print('PASS:',style['name'],'retains identical deck, cargo and grating geometry; valid GLB without studio or collision guides.')
print('Asset checks complete. Unity collision integration and movement playtest remain pending.')
