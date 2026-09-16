#!/usr/bin/env python3
"""Export only serialized visual bindings from the authorized source models."""
import json,sys
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parents[1];env=UnityPy.load(sys.argv[1]);objects={o.path_id:o for o in env.objects}
scripts={o.path_id:o.read().m_ClassName for o in objects.values() if o.type.name=='MonoScript'}
selected=set(json.loads((root/'assets/ships/content-roots.json').read_text()));entries=[];extras={}
for path,ref in env.container.items():
 if not path.endswith('.prefab') or ref.read().m_Name not in selected:continue
 paths={};components=[]
 def visit(gid,relative):
  d=objects[gid].read_typetree();paths[gid]=relative
  for c in d['m_Component']:
   oid=c['component']['m_PathID'];o=objects[oid];data=o.read_typetree();paths[oid]=relative
   if o.type.name=='Transform':
    for child in data['m_Children']:
     t=objects[child['m_PathID']].read_typetree();go=t['m_GameObject']['m_PathID'];name=objects[go].read().m_Name
     visit(go,relative+'/'+name if relative else name)
   elif o.type.name=='MonoBehaviour' and scripts.get(data['m_Script']['m_PathID'])=='ShipCustomization':components.append(data)
 visit(ref.path_id,'')
 for data in components:
  entry={'prefab':ref.read().m_Name}
  for key in ['FigureheadObjects','ShieldRenderers','HullRenderers','NameplateRenderers']:
   entry[key]=[paths[v['m_PathID']] for v in data.get(key,[]) if v['m_PathID'] in paths]
  entry['SailRenderer']=paths.get(data.get('SailRenderer',{}).get('m_PathID',0),'')
  for key in ['SailMaterials','ShieldMaterials','HullMaterials']:
   entry[key]=[]
   for v in data.get(key,[]):
    oid=v['m_PathID']
    if oid:
     name='helmsman/styles/'+str(oid)+'.mat';extras[name]=oid;entry[key].append(name)
  entries.append(entry)
(root/'assets/ships/customization.json').write_text(json.dumps({'entries':entries},indent=2)+'\n')
(root/'assets/ships/style-resources.json').write_text(json.dumps(extras,indent=2)+'\n')
print(f'{len(entries)} visual bindings; {len(extras)} original material variants')
