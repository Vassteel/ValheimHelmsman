"""Regroup saved final meshes for construction; preserve metadata and appearance."""
import bpy,gzip,io,struct,hashlib
from collections import defaultdict,Counter
from build import ROOT,SOURCES,meshdata
from construction_groups import group,legacy_group
import numpy as np

def signatures(parts):
 result=defaultdict(Counter)
 for _,mi,vs,ts in parts:
  packed=[struct.pack('<8f',*(x+0.0 for x in v)) for v in vs]
  for i in range(0,len(ts),3):
   result[mi][hashlib.sha256(b''.join(packed[j] for j in ts[i:i+3])).digest()]+=1
 return result

for kind,(_,_,prefab,*_) in SOURCES.items():
 path=ROOT/'assets/ships/final'/(prefab+'.bin.gz');data=gzip.decompress(path.read_bytes());f=io.BytesIO(data)
 def I():return struct.unpack('<i',f.read(4))[0]
 def raw():return f.read(I())
 assert f.read(4)==b'HMF1';raw();materials=[]
 for _ in range(I()):materials.append(raw().decode());raw();f.read(16)
 header=data[:f.tell()];old=[]
 for _ in range(I()):
  tag=raw().decode();mi=I();vs=[struct.unpack('<8f',f.read(32)) for _ in range(I())];nt=I();ts=struct.unpack('<'+'i'*nt,f.read(nt*4));old.append((tag,mi,vs,ts))
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'design/fleet-final'/kind/(kind+'.blend')))
 groups={};source={(tag,mi):(vs,ts) for tag,mi,vs,ts in old};cursors=defaultdict(lambda:[0,0])
 for ob in bpy.data.collections['Helmsman '+kind+' final'].objects:
  if ob.type!='MESH' or 'sheet' in ob.name.lower():continue
  mi=materials.index(ob.data.materials[0].name);v,t=meshdata(ob)
  key=(group(ob) if any(p[0]=='deck' for p in old) else legacy_group(ob),mi);ov,ot=source[key];vi,ti=cursors[key]
  saved=ov[vi:vi+len(v)];saved_indices=[j-vi for j in ot[ti:ti+len(t)]]
  assert len(saved)==len(v) and np.allclose(saved,v,rtol=0,atol=.000002),(kind,ob.name,'saved vertex mismatch')
  assert saved_indices==t,(kind,ob.name,'saved topology mismatch')
  cursors[key]=[vi+len(v),ti+len(t)]
  dst=groups.setdefault((group(ob),mi),([],[]));offset=len(dst[0]);dst[0].extend(saved);dst[1].extend(i+offset for i in saved_indices)
 new=[(tag,mi,vs,ts) for (tag,mi),(vs,ts) in groups.items()]
 assert signatures(old)==signatures(new),(kind,'Geometry or material assignment changed; refusing repack')
 # Existing animated mesh groups must remain identical; new groups are static.
 for tag in ['hull','sail','rudder','decoration']:
  assert signatures([p for p in old if p[0]==tag])==signatures([p for p in new if p[0]==tag]),(kind,tag)
 assert {'deck','mast','rigging'}<=set(tag for tag,_,_,_ in new),kind
 out=io.BytesIO();out.write(header)
 def put(n):out.write(struct.pack('<i',n))
 put(len(new))
 for tag,mi,vs,ts in new:
  b=tag.encode();put(len(b));out.write(b);put(mi);put(len(vs))
  for v in vs:out.write(struct.pack('<8f',*v))
  put(len(ts));out.write(struct.pack('<'+'i'*len(ts),*ts))
 path.write_bytes(gzip.compress(out.getvalue(),mtime=0))
 print('PASS',kind,len(old),'->',len(new),'batches; exact geometry, materials, metadata and animated groups preserved',flush=True)
