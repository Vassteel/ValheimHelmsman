"""Verify the second-pass asset optimization protects functional mesh data."""
import ast,gzip,struct,io,json,math
from pathlib import Path
R=Path(__file__).resolve().parents[2]
source=ast.parse((R/'tools/HarborModels/optimize_existing.py').read_text())
exec(compile(ast.Module(body=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name=='unpack'],type_ignores=[]),'<packed mesh reader>','exec'))
rows=json.loads((R/'design/fleet-detail-optimization/optimization.json').read_text())
rows.append(dict(asset='MercantShip.bin.gz',**json.loads((R/'design/ottar-placement-v1/optimization.json').read_text())))
for row in rows:
 name=row['asset'];folder='ottar-placement-before' if name=='MercantShip.bin.gz' else 'fleet-detail-before'
 before=unpack(R/'.build'/folder/name);after=unpack(R/'assets/ships/final'/name)
 assert before[:3]==after[:3] and before[4]==after[4],name+' functional metadata/materials changed'
 assert len(before[3])==len(after[3]),name+' lost a mesh batch'
 for a,b in zip(before[3],after[3]):
  assert a[:2]==b[:2]
  if a[0] in ('hull','sail','rudder','mast','paddle'):assert a==b,name+' protected geometry changed'
  assert len(b[3])>0 and len(b[3])<=len(a[3])
  assert all(math.isfinite(x) for v in b[2] for x in v)
 assert sum(len(p[3])//3 for p in before[3])==row['before']
 assert sum(len(p[3])//3 for p in after[3])==row['after']
 print('PASS',name,row['before'],'->',row['after'],'triangles; protected meshes, materials and functional data unchanged')
