#!/usr/bin/env python3
"""Verify model data and runtime bindings before any live world is exposed to them."""
import json,sys
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parents[1];env=UnityPy.load(str(root/'assets/ships/helmsman-ships'))
objects={o.path_id:o for o in env.objects};scripts={o.path_id:o.read() for o in objects.values() if o.type.name=='MonoScript'}
roots={ref.read().m_Name:ref for path,ref in env.container.items() if path.endswith('.prefab')}
expected=set(json.loads((root/'assets/ships/content-roots.json').read_text()));assert set(roots)==expected
assert not any(s.m_AssemblyName.startswith('OdinShip') for s in scripts.values())
bindings=json.loads((root/'assets/ships/customization.json').read_text())['entries'];styles=set(json.loads((root/'assets/ships/style-resources.json').read_text()))
assert styles.issubset(env.container)
checks=0;ships=0;holds=0;paths_by_root={}
for name,ref in roots.items():
 paths={};native=[]
 def walk(goid,path):
  global checks
  obj=objects[goid].read_typetree();paths[path]=goid
  for c in obj['m_Component']:
   o=objects[c['component']['m_PathID']];d=o.read_typetree()
   if o.type.name=='Transform':
    for child in d['m_Children']:
     go=objects[child['m_PathID']].read_typetree()['m_GameObject']['m_PathID'];n=objects[go].read().m_Name
     walk(go,path+'/'+n if path else n)
   if o.type.name=='MonoBehaviour':
    script=scripts[d['m_Script']['m_PathID']];native.append((script.m_ClassName,d,path))
 walk(ref.path_id,'');paths_by_root[name]=paths
 for kind,d,path in native:
  if kind=='Ship':
   ships+=1
   for field in ['m_floatCollider','m_shipControlls','m_sailObject','m_mastObject']:
    assert d[field]['m_PathID'] in objects,(name,field)
   assert d['m_backwardForce']>0,(name,'rowing thrust')
   assert d['m_sailForceFactor']>0 or 'Canoe' in name,(name,'sail force')
   checks+=6
  if kind=='Container':holds+=1
 for b in [v for v in bindings if v['prefab']==name]:
  for key in ['FigureheadObjects','ShieldRenderers','HullRenderers','NameplateRenderers']:
   for path in b[key]:assert path in paths,(name,key,path);checks+=1
  if b['SailRenderer']:assert b['SailRenderer'] in paths;checks+=1
for binding in json.loads((root/'assets/ships/component-bindings.json').read_text())['entries']:
 paths=paths_by_root[binding['prefab']]
 for key in ['path','container','visual','output']:
  if key in binding:assert binding[key] in paths,(binding['prefab'],key,binding[key]);checks+=1
for material in scripts.values():assert 'ShipManualTurret' not in material.m_ClassName
assert ships==15,ships
print(f'PASS: {len(roots)} public content roots, {ships} native player ships, {holds} native containers, {len(styles)} material variants, {checks} component/visual bindings; no original plugin scripts or enemy roots.')
