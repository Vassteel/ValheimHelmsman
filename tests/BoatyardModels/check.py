"""Validate generated visual bindings against the source bundle and mesh stream.
Run with UnityPy Python. No Unity runtime or screenshots are claimed by this test.
"""
import gzip,io,struct,json,math,hashlib
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
root=Path(__file__).resolve().parents[2]
env=UnityPy.load(str(root/'assets/ships/helmsman-ships'))
prefabs={ptr.read().m_Name:ptr for path,ptr in env.container.items() if path.endswith('.prefab')}
expected=set(json.loads((root/'assets/ships/content-roots.json').read_text()))
rigs=json.loads((root/'assets/ships/redesign/rig-routes.json').read_text())
f=io.BytesIO(gzip.decompress((root/'assets/ships/redesign/models.bin.gz').read_bytes()))
assert f.read(4)==b'HMD1'
def integer():return struct.unpack('<i',f.read(4))[0]
def text():return f.read(integer()).decode()
def target(go,protect_rig=False):
 asset_name=go.m_Name
 route=[integer() for _ in range(integer())];name=text()
 if protect_rig and name:assert not any(route[:len(r)]==r for r in rigs.get(asset_name,[])),(asset_name,name,'protected rig')
 for idx in route:
  comps=[c.component.read() for c in go.m_Component];t=next(c for c in comps if type(c).__name__=='Transform');go=t.m_Children[idx].read().m_GameObject.read()
 if name:assert go.m_Name==name,(go.m_Name,name,route)
 return go,name
checks=0;triangles=0;vertices=0;seen=set()
def shape(go):
 global checks,triangles,vertices
 node,name=target(go,True);expected_vertices=integer();custom=integer();n=integer()
 assert 0<n<2000000 and custom in [0,1]
 coords=struct.unpack('<'+'f'*(n*3),f.read(n*12));uv=struct.unpack('<'+'f'*(n*2),f.read(n*8));assert all(math.isfinite(v) and abs(v)<100000 for v in coords+uv)
 groups=integer();assert 0<groups<=32
 for _ in range(groups):
  size=integer();assert size%3==0;ix=struct.unpack('<'+'i'*size,f.read(size*4));assert not ix or min(ix)>=0 and max(ix)<n;triangles+=size//3
 if name:
  comps=[c.component.read() for c in node.m_Component];mf=next(c for c in comps if type(c).__name__=='MeshFilter');assert any(type(c).__name__=='MeshRenderer' for c in comps),(go.m_Name,name,[type(c).__name__ for c in comps])
  if expected_vertices:
   mh=MeshHandler(mf.m_Mesh.read());mh.process();assert len(mh.m_Vertices)==expected_vertices,(name,expected_vertices)
 if custom:assert groups==7
 checks+=1;vertices+=n
count=integer();assert count==len(expected)
for _ in range(count):
 name=text();assert name in expected and name not in seen;seen.add(name);go=prefabs[name].read();mods=integer()
 for i in range(mods):shape(go)
 hides=integer()
 for i in range(hides):
  node,label=target(go);assert any(c.component.type.name in ['MeshRenderer','SkinnedMeshRenderer'] for c in node.m_Component);checks+=1
 extra=integer();assert extra in [0,1]
 if extra:shape(go)
 assert mods or extra,name
assert not f.read();assert seen==expected
result={'assets':len(seen),'validated_bindings_and_meshes':checks,'vertices':vertices,'triangle_indices_valid':triangles,'binary_sha256':hashlib.sha256((root/'assets/ships/redesign/models.bin.gz').read_bytes()).hexdigest(),'runtime_status':'Not playtested; collider cooking, ship clearance and moving-part appearance require Unity validation'}
(root/'output/model-assessment/validation.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
