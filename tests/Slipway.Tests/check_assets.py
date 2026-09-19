import gzip,struct,math,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
for path in sorted((root/'assets/workshop').glob('*.bin.gz')):
 data=gzip.decompress(path.read_bytes());pos=0
 def read(fmt):
  global pos
  result=struct.unpack_from('<'+fmt,data,pos);pos+=struct.calcsize('<'+fmt);return result
 def i():return read('i')[0]
 def text():
  global pos
  n=i();value=data[pos:pos+n].decode();pos+=n;return value
 assert data[:4] in (b'HMW1',b'HMW2',b'HMW3');version=data[:4];pos=4
 spec=json.loads(text()) if version==b'HMW3' else None
 nm=i();assert 0<nm<64
 for _ in range(nm):text();assert all(math.isfinite(n) for n in read('4f'))
 triangles=0
 tags=set()
 for _ in range(i()):
  if version!=b'HMW1':tags.add(text())
  assert 0<=i()<nm;n=i();vs=[read('9f' if spec else '8f') for _ in range(n)];assert all(math.isfinite(v) for row in vs for v in row)
  nt=i();assert nt%3==0;ts=read('i'*nt);assert all(0<=t<n for t in ts);triangles+=nt//3
  # Exported triangle winding and shading normals must agree after coordinate conversion.
  for a,b,c in zip(ts[::3],ts[1::3],ts[2::3]):
   p,q,r=vs[a],vs[b],vs[c];u=[q[j]-p[j] for j in range(3)];v=[r[j]-p[j] for j in range(3)]
   cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
   assert sum(cross[j]*p[j+3] for j in range(3))>=-1e-5,(path.name,'inverted face')
 snaps={}
 for _ in range(i()):
  name=text();assert name not in snaps;snaps[name]=read('3f')
 assert pos==len(data)
 if path.name=='slipway.bin.gz':
  assert tags=={'static','cradle','capstan','haul'}
  assert len(snaps)==53
  corners=['snap_head_-4','snap_head_4','snap_side_-4_10','snap_side_4_10']
  assert all(k in snaps for k in corners), 'Missing selectable platform corner'
  assert all(abs(snaps[k][0])==4 for k in corners)
  assert all(abs(snaps[k][2])>=10 for k in corners)
  for name,(x,y,z) in snaps.items():
   if name.startswith('snap_side_'):assert abs(y-(1.2+(10+z)*.075))<1e-5
 if spec:
  assert triangles<=20000
  assert tags<=({'static','detail','running-rope'}|{r['tag'] for r in spec['rigs']})
  assert 'static' in tags
  assert len(snaps)==(6 if spec['key'] in ('pier-crane','heavy-crane') else 0)
 print('PASS',path.name,triangles,'triangles',len(snaps),'snap points')
assert len(list((root/'assets/workshop').glob('*.bin.gz')))==23
