"""Extract installed native player data for a local animation review only. Run from repo root; nothing is embedded or shipped."""
import UnityPy,json
from pathlib import Path
from UnityPy.helpers.MeshHelper import MeshHandler
E=UnityPy.load('/home/deck/.local/share/Steam/steamapps/common/Valheim/valheim_Data/StreamingAssets/SoftRef/Bundles/c4210710')
p=next(p for path,p in E.container.items() if path.lower().endswith('/player.prefab'))
rows=[];render=None
V=lambda v:[v.x,v.y,v.z]
def walk(p,parent=-1):
 global render
 go=p.read();tr=next(c.component.read() for c in go.m_Component if c.component.type.name=='Transform');n=len(rows)
 rows.append(dict(name=go.m_Name,id=tr.object_reader.path_id,parent=parent,pos=V(tr.m_LocalPosition),rot=[tr.m_LocalRotation.w,tr.m_LocalRotation.x,tr.m_LocalRotation.y,tr.m_LocalRotation.z],scale=V(tr.m_LocalScale)))
 for pair in go.m_Component:
  if pair.component.type.name=='SkinnedMeshRenderer':render=pair.component.read()
 for ch in tr.m_Children:walk(ch.read().m_GameObject,n)
walk(p)
m=render.m_Mesh.read();h=MeshHandler(m);h.process()
print('weights',h.m_BoneWeights[0],h.m_BoneIndices[0],'vertices',h.m_Vertices[0])
for ptr in render.m_Materials:
 mat=ptr.read();print(mat.m_Name,[(n,t.m_Texture.path_id) for n,t in mat.m_SavedProperties.m_TexEnvs])
texture=next(t.m_Texture for n,t in render.m_Materials[0].read().m_SavedProperties.m_TexEnvs if n=='_MainTex');texture.read().image.save('.build/preview-player-texture.png')
rowsout=dict(nodes=rows,bones=[b.path_id for b in render.m_Bones],bind=[[[getattr(b,f'e{i}{j}') for j in range(4)] for i in range(4)] for b in m.m_BindPose],vertices=h.m_Vertices,weights=h.m_BoneWeights,indices=h.m_BoneIndices,triangles=h.get_triangles(),uv=h.m_UV0)
Path('.build/preview-player.json').write_text(json.dumps(rowsout))
