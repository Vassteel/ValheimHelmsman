"""Second-stage reduction of Óttarr details; preserve hull, sail and physical data."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,bmesh,ast,struct,gzip,io,json
from pathlib import Path
R=Path(__file__).resolve().parents[2]
s=ast.parse((R/'tools/HarborModels/optimize_existing.py').read_text());exec(compile(ast.Module(body=[n for n in s.body if isinstance(n,ast.FunctionDef)],type_ignores=[]),'<mesh helpers>','exec'))
path=R/'.build/ottar-placement-before/MercantShip.bin.gz';magic,spec,header,parts,tail=unpack(path)
def split(vs,ts):
 lookup={};ids=[]
 for v in vs:
  k=tuple(round(x,6) for x in v[:3])
  if k not in lookup:lookup[k]=len(lookup)
  ids.append(lookup[k])
 parent=list(range(len(lookup)))
 def find(i):
  while i!=parent[i]:parent[i]=parent[parent[i]];i=parent[i]
  return i
 for i in range(0,len(ts),3):
  a,b,c=[find(ids[j]) for j in ts[i:i+3]];parent[b]=a;parent[c]=a
 groups={}
 for i in range(0,len(ts),3):groups.setdefault(find(ids[ts[i]]),[]).extend(ts[i:i+3])
 for indices in groups.values():
  used=list(dict.fromkeys(indices));remap={j:i for i,j in enumerate(used)}
  yield [vs[j] for j in used],[remap[j] for j in indices]
new=[];report=[]
for tag,mi,vs,ts in parts:
 if tag in ['hull','sail','rudder','mast']:
  new.append((tag,mi,vs,ts));continue
 out=[];indices=[];kept=0;reduced=0
 for cv,ct in split(vs,ts):
  nv,nt=(cv,ct) if len(ct)<96 else simplify(cv,ct,.46 if tag in ['cargo','deck','rigging'] else .6)
  if len(nt)<12 or any(abs(fn(v[j] for v in cv)-fn(v[j] for v in nv))>max(.012 if tag=='cargo' else .004,(max(v[j] for v in cv)-min(v[j] for v in cv))*(.06 if tag=='cargo' else .025)) for j in range(3) for fn in (min,max)):
   nv,nt=cv,ct;kept+=1
  else:reduced+=int(len(nt)<len(ct))
  base=len(out);out+=nv;indices.extend(i+base for i in nt)
 new.append((tag,mi,out,indices));report.append(dict(group=tag,material=mi,before=len(ts)//3,after=len(indices)//3,reduced_components=reduced,preserved_components=kept))
stream=io.BytesIO();stream.write(header)
def I(n):stream.write(struct.pack('<i',n))
def text(s):b=s.encode();I(len(b));stream.write(b)
I(len(new))
for tag,mi,vs,ts in new:
 text(tag);I(mi);I(len(vs))
 for v in vs:stream.write(struct.pack('<8f',*v))
 I(len(ts));stream.write(struct.pack('<'+'i'*len(ts),*ts))
stream.write(tail)
out=R/'design/ottar-placement-v1';out.mkdir(exist_ok=True)
(out/'MercantShip.bin.gz').write_bytes(gzip.compress(stream.getvalue(),mtime=0))
report=dict(before=sum(len(t)//3 for _,_,_,t in parts),after=sum(len(t)//3 for _,_,_,t in new),parts=report,protected=['hull','sail','rudder','mast'])
(out/'optimization.json').write_text(json.dumps(report,indent=2)+'\n');print(report['before'],report['after'],flush=True)
