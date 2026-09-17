import UnityPy,json
from pathlib import Path
r=Path(__file__).resolve().parents[2];e=UnityPy.load(str(r/'assets/ships/helmsman-ships'));names=set(json.loads((r/'assets/ships/content-roots.json').read_text()));result={}
for path,ptr in e.container.items():
 if not path.endswith('.prefab'):continue
 go=ptr.read()
 if go.m_Name not in names:continue
 targets=[]
 for c in go.m_Component:
  if c.component.type.name=='MonoBehaviour':
   data=c.component.read_typetree()
   if 'm_mastObject' in data:
    targets=[data[k]['m_PathID'] for k in ['m_mastObject','m_sailObject'] if data.get(k,{}).get('m_PathID')]
 routes=[]
 def walk(ref,route=()):
  obj=ref.read();cs=[c.component.read() for c in obj.m_Component];t=next((c for c in cs if type(c).__name__=='Transform'),None)
  if ref.path_id in targets or t and t.object_reader.path_id in targets:routes.append(list(route))
  if t:
   for i,c in enumerate(t.m_Children):walk(c.read().m_GameObject,route+(i,))
 walk(ptr);result[go.m_Name]=routes
(r/'assets/ships/redesign/rig-routes.json').write_text(json.dumps(result,indent=2)+'\n');print('Recorded protected rig roots for',sum(bool(v) for v in result.values()),'ships')
