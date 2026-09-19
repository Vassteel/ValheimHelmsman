"""Check packed optimization preserves animated groups, metadata, parts and bounds."""
import ast,gzip,io,struct,json,math
from pathlib import Path
R=Path(__file__).resolve().parents[2];D=R/'design/asset-optimization-v1';B=R/'.build/asset-budget-baseline'
source=ast.parse((R/'tools/HarborModels/optimize_existing.py').read_text());exec(compile(ast.Module(body=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name=='unpack'],type_ignores=[]),'<reader>','exec'))
def components(vs,ts):
 lookup={};ids=[]
 for v in vs:
  k=tuple(round(x,6) for x in v[:3])
  if k not in lookup:lookup[k]=len(lookup)
  ids.append(lookup[k])
 parent=list(range(len(lookup)));used=set()
 def find(n):
  while parent[n]!=n:parent[n]=parent[parent[n]];n=parent[n]
  return n
 for i in range(0,len(ts),3):
  a,b,c=(ids[t] for t in ts[i:i+3])
  if len({a,b,c})!=3:continue
  used.update((a,b,c));parent[find(b)]=find(a);parent[find(c)]=find(a)
 return len({find(i) for i in used})
results=[]
for row in json.loads((D/'optimization.json').read_text()):
 name=row['asset'];am,aspec,ah,ap,at=unpack(B/name);bm,bspec,bh,bp,bt=unpack(D/name)
 assert (am,aspec,ah,at)==(bm,bspec,bh,bt),(name,'gameplay metadata/materials/snap points changed')
 assert [(g,m) for g,m,v,t in ap]==[(g,m) for g,m,v,t in bp]
 lost=[]
 for index,((g,m,av,ai),(_,_,bv,bi)) in enumerate(zip(ap,bp)):
  assert all(math.isfinite(x) for v in bv for x in v)
  assert all(0<=i<len(bv) for i in bi)
  if g in row['protected_groups']:assert (av,ai)==(bv,bi),(name,g,'animated geometry changed')
  ca,cb=components(av,ai),components(bv,bi)
  if cb<ca:lost.append(dict(part=index,tag=g,material=m,before=ca,after=cb))
 result=dict(asset=name,metadata_unchanged=True,animated_topology_unchanged=True,lost_components=lost);results.append(result)
 print(name,'lost components',lost,flush=True)
(D/'optimization-checks.json').write_text(json.dumps(results,indent=2)+'\n')
assert not any(r['lost_components'] for r in results),'Review reductions that lost disconnected model details'
