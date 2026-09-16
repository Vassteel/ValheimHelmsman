#!/usr/bin/env python3
"""Export model attachment paths and numeric settings, without original plugin code."""
import json,sys
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parents[1]
env=UnityPy.load(sys.argv[1]);objects={o.path_id:o for o in env.objects}
scripts={o.path_id:o.read().m_ClassName for o in objects.values() if o.type.name=='MonoScript'}
selected=set(json.loads((root/'assets/ships/content-roots.json').read_text()));records=[]
for path,ref in env.container.items():
 if not path.endswith('.prefab'):continue
 prefab=ref.read().m_Name
 if prefab not in selected:continue
 paths={};components=[]
 def visit(gid,relative):
  g=objects[gid].read_typetree();paths[gid]=relative
  for c in g['m_Component']:
   oid=c['component']['m_PathID'];o=objects[oid];d=o.read_typetree();paths[oid]=relative
   if o.type.name=='Transform':
    for child in d['m_Children']:
     td=objects[child['m_PathID']].read_typetree();go=td['m_GameObject']['m_PathID'];name=objects[go].read().m_Name
     visit(go,relative+'/'+name if relative else name)
   elif o.type.name=='MonoBehaviour':components.append((scripts.get(d['m_Script']['m_PathID']),relative,d))
 visit(ref.path_id,'')
 def resolve(d,k):return paths.get(d.get(k,{}).get('m_PathID',0),'')
 def item(d,k):
  oid=d.get(k,{}).get('m_PathID',0)
  if not oid:return ''
  return objects[objects[oid].read_typetree()['m_GameObject']['m_PathID']].read().m_Name
 for kind,relative,d in components:
  if kind=='OdinFishingDock':
   records.append(dict(prefab=prefab,kind='dock',path=relative,container=resolve(d,'m_targetContainer'),visual=resolve(d,'m_productionEffect'),item=item(d,'m_fishItem'),seconds=d['m_secPerFish'],count=d['m_maxFishInContainer']))
  elif kind=='FishPress':
   records.append(dict(prefab=prefab,kind='press',path=relative,output=resolve(d,'m_outputPoint'),visual=resolve(d,'m_processingObject'),item=item(d,'m_outputItem'),seconds=d['m_conversionDurationSeconds'],count=d['m_fishRequired'],switches=[paths.get(v['m_PathID'],'') for v in d['m_addFishSwitches']]))
  elif kind=='FishingNet':
   records.append(dict(prefab=prefab,kind='net',path=relative,container=resolve(d,'fishContainer'),visual=resolve(d,'visualNetObject')))
(root/'assets/ships/component-bindings.json').write_text(json.dumps({'entries':records},indent=2)+'\n')
print(json.dumps(records,indent=2))
