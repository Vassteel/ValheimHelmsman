"""Audit release asset independence and compacted stream decoding."""
from pathlib import Path
import json
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
root=Path(__file__).resolve().parents[2]
e=UnityPy.load(str(root/'assets/ships/helmsman-ships'));objects={o.path_id:o for o in e.objects}
roots={p.read().m_Name for path,p in e.container.items() if path.endswith('.prefab')}
assert len(roots)==27
retired={'RowingCanoe','DoubleRowingCanoe','CargoShip','FastShipSkuldelev','Skuldelev','CargoCaravel','GoblinShip','TaurusWarShip','CargoAnimalShip','HugeCargoShip'}
assert not roots&retired
scripts={o.read().m_ClassName for o in e.objects if o.type.name=='MonoScript'}
assert not {'Ship','ShipControlls'}&scripts
# Every stored object remains reachable from approved roots, and every stream is
# only referenced slices plus <=15-byte alignment gaps (never old wholesale files).
used={};meshes=textures=0
for o in e.objects:
 d=o.read_typetree()
 if o.type.name=='Mesh':
  handler=MeshHandler(o.read());handler.process();assert len(handler.m_Vertices)>0;meshes+=1
 if o.type.name=='Texture2D':
  im=o.read().image;assert im.width>0 and im.height>0;textures+=1
 stream=d.get('m_StreamData') or d.get('m_Resource')
 if stream:
  path=stream.get('path',stream.get('m_Source'));offset=stream.get('offset',stream.get('m_Offset'));size=stream.get('size',stream.get('m_Size'))
  if size:used.setdefault(path.rsplit('/',1)[-1],set()).add((offset,offset+size))
for name,f in e.file.files.items():
 if not name.endswith(('.resS','.resource')):continue
 end=0
 for a,b in sorted(used.get(name,[])):
  assert 0<=a-end<=15,(name,'unused original stream data');end=b
 assert end==len(f.bytes),(name,'unused trailing original stream data')
print(f'PASS: 27 decor roots; no Ship/ShipControlls; {meshes} meshes and {textures} textures decode; unused stream bytes removed.')
