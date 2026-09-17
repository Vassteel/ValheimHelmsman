import json,math,gzip
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
root=Path(__file__).resolve().parents[2]
env=UnityPy.load(str(root/'assets/ships/helmsman-ships'))
names=json.loads((root/'assets/ships/content-roots.json').read_text());results=[];cache={}
I=[[int(i==j) for j in range(4)] for i in range(4)]
def mul(a,b):return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
def matrix(t):
 q=t.m_LocalRotation;x,y,z,w=q.x,q.y,q.z,q.w;s=t.m_LocalScale;p=t.m_LocalPosition
 r=[[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w),p.x],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w),p.y],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y),p.z],[0,0,0,1]]
 for i in range(3):
  for j,v in enumerate([s.x,s.y,s.z]):r[i][j]*=v
 return r
def xyz(v):return list(v)[:3] if isinstance(v,(list,tuple)) else [v.x,v.y,v.z]
for path,ptr in env.container.items():
 if not path.endswith('.prefab'):continue
 name=ptr.read().m_Name
 if name not in names:continue
 parts=[]
 def walk(go,parent,first=False,route=()):
  if not first and not go.m_IsActive:return
  if any(s in go.m_Name.lower() for s in ['lod1','lod2','lod3','shadow','watermask','turret']):return
  cs=[c.component.read() for c in go.m_Component];t=next((c for c in cs if type(c).__name__=='Transform'),None)
  if t is None:return
  mat=parent if first else mul(parent,matrix(t))
  renderer=next((c for c in cs if type(c).__name__ in ['MeshRenderer','SkinnedMeshRenderer']),None)
  filt=next((c for c in cs if type(c).__name__=='MeshFilter'),None)
  if renderer and renderer.m_Enabled and (filt or hasattr(renderer,'m_Mesh')):
   ref=filt.m_Mesh if filt else renderer.m_Mesh
   try:
    key=ref.path_id
    if key not in cache:
     mh=MeshHandler(ref.read());mh.process();cache[key]=([xyz(v) for v in mh.m_Vertices],mh.get_triangles(),[[float(u[0]),float(u[1])] for u in (mh.m_UV0 or [[0,0]]*len(mh.m_Vertices))])
    vs,groups,uv=cache[key];v=[[sum(mat[i][k]*p[k] for k in range(3))+mat[i][3] for i in range(3)] for p in vs]
    for si,faces in enumerate(groups):
     mn=renderer.m_Materials[min(si,len(renderer.m_Materials)-1)].read().m_Name if renderer.m_Materials else ''
     if any(k in mn.lower() for k in ['water','shadow','particle']):continue
     parts.append(dict(name=go.m_Name,material=mn,route=list(route),matrix=mat,skinned=type(renderer).__name__=="SkinnedMeshRenderer",submesh=si,uv=uv,vertices=v,triangles=[n for f in faces for n in f]))
   except Exception as e:print('SKIP',name,go.m_Name,type(e).__name__,str(e)[:100])
  for ci,c in enumerate(t.m_Children):walk(c.read().m_GameObject.read(),mat,route=route+(ci,))
 walk(ptr.read(),I,True)
 results.append(dict(name=name,parts=parts));print(name,len(parts),sum(len(p['triangles'])//3 for p in parts),flush=True)
with gzip.open(root/'output/model-assessment/source-meshes.json.gz','wt') as f:json.dump(results,f)
