from pathlib import Path
import bpy,json,struct,math
p=Path(__file__).resolve().parent
results=[]
for name in ['falkusa','ceol','currach','harbour-workbench']:
 d=p/name
 bpy.ops.wm.open_mainfile(filepath=str(d/(name+'.blend')))
 objects=list(next(c for c in bpy.data.collections if 'standalone' in c.name).objects)
 assert not any('rowing oar' in o.name.lower() for o in objects)
 for o in objects:
  assert o.type=='MESH'
  assert all(math.isfinite(c) for v in o.data.vertices for c in o.matrix_world@v.co)
 raw=(d/(name+'.glb')).read_bytes()
 assert struct.unpack_from('<4sII',raw)==(b'glTF',2,len(raw))
 n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n])
 assert sum('mesh' in a for a in doc['nodes'])==len(objects)
 assert not doc.get('cameras')
 assert len(doc.get('images',[]))>8 and all('bufferView' in im for im in doc['images'])
 assert not any('Studio' in a.get('name','') for a in doc['nodes'])
 info=json.loads((d/'model-info.json').read_text());assert info['standalone']
 if name!='harbour-workbench':assert any('sail' in o.name.lower() or o.name=='Red sailing cloth' for o in objects)
 if name=='currach':
  assert info['inventory']['slots']==6 and info['inventory']['columns']*info['inventory']['rows']==6
  assert sum(o.name.startswith('Cargo / barrel lid') for o in objects)==1
  assert sum(o.name.startswith('Cargo / hide bale folded flap') for o in objects)==2
  assert sum(o.name.startswith('Cargo / knapsack') for o in objects)==2
 if name=='harbour-workbench':
  verts=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
  assert max(abs(v.x) for v in verts)<=1.501
  assert max(abs(v.y) for v in verts)<=1.001
  assert {o.get('bird_role') for o in objects if o.get('bird_role')}=={'Puffin','Gull','Pelican'}
 results.append({'asset':name,'mesh_objects':len(objects),'embedded_images':len(doc['images']),'passed':True})
(p/'validation-results.json').write_text(json.dumps(results,indent=2))
print('PASS: four finite textured model exports; no rowing oars; all boats have sails; currach cargo and six-slot target correct; workbench fits 3m x 2m and includes all three birds.')
print('Standalone visual assets. Inventory, movement, menus and gameplay are not implemented.')
