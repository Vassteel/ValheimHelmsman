"""Validate exactly the mesh resources loaded by FinalFleetModels, without Unity."""
from pathlib import Path
import gzip,io,struct,json,math
ROOT=Path(__file__).resolve().parents[2]
expected={'MercantShip','BigCargoShip','WarShip','HerculeShip','LittleBoat','HelmsmanCurrach'}
actual={p.name[:-7] for p in (ROOT/'assets/ships/final').glob('*.bin.gz')}
assert actual==expected,(actual,expected)
checks=0
for name in sorted(expected):
 r=io.BytesIO(gzip.decompress((ROOT/'assets/ships/final'/(name+'.bin.gz')).read_bytes()))
 def num():return struct.unpack('<i',r.read(4))[0]
 def raw():
  n=num();assert 0<=n<=1000000
  b=r.read(n);assert len(b)==n;return b
 def text():return raw().decode()
 assert r.read(4)==b'HMF1';s=json.loads(text());assert s['prefab']==name
 assert sum(p['kind']=='helm' for p in s['points'])==1
 assert sum(p['kind']=='ladder' for p in s['points'])==2
 assert sum(p['kind']=='mast' for p in s['points'])==1
 assert len(s['hullSolids'])==18
 for h in s['hullSolids']:
  assert len(h['vertices'])==24 and all(math.isfinite(v) for v in h['vertices'])
  assert len(h['triangles'])==36 and all(0<=i<8 for i in h['triangles'])
  # Each convex section must be watertight: every undirected edge used twice.
  edges={}
  for i in range(0,36,3):
   t=h['triangles'][i:i+3]
   for a,b in zip(t,t[1:]+t[:1]):
    k=tuple(sorted((a,b)));edges[k]=edges.get(k,0)+1
  assert set(edges.values())=={2}
 for c in s['colliders']:
  assert len(c['size'])==3 and all(math.isfinite(v) and v>0 for v in c['size'])
  assert c['size'][1]<max(5,s['beam']), 'Invisible solid across the open hull'
 materials=num();assert 0<materials<128
 for _ in range(materials):
  text();png=raw();assert png.startswith(b'\x89PNG\r\n\x1a\n');assert struct.unpack('>II',png[16:24])==(128,128);r.read(16)
 groups=set();parts=num();verts=0;triangles=0
 for _ in range(parts):
  group=text();groups.add(group);assert 0<=num()<materials
  n=num();verts+=n;assert 0<n<1000000
  vs=struct.unpack('<'+'f'*n*8,r.read(n*32));assert all(math.isfinite(v) for v in vs)
  assert all(sum(v*v for v in vs[i+3:i+6])>.7 for i in range(0,len(vs),8)), 'Missing normals'
  nt=num();triangles+=nt//3;assert nt%3==0
  ts=struct.unpack('<'+'i'*nt,r.read(nt*4));assert min(ts)>=0 and max(ts)<n
 assert {'sail','hull','fixed','rudder'}<=groups
 assert not r.read(1),'Trailing bytes'
 if s['name'] in ['ottar','freighter']:
  directory=ROOT/'design/fleet-final'/s['name']
  support=json.loads((directory/'cargo-support.json').read_text())
  assert len(support)>30 and {'barrel','hide_bale','sack','bundle'}<={p['shape'] for p in support}
  assert all(p['support_gap']<=.004 for p in support),'Floating cargo prop'
  grid=json.loads((directory/'cargo-coverage.json').read_text())
  xs,ys,hs=grid['x'],grid['y'],grid['heights']
  assert len(s['cargoSolids'])==2*(len(xs)-1)*(len(ys)-1)
  corners={}
  for c in s['cargoSolids']:
   assert len(c['vertices'])==18 and len(c['triangles'])==24
   vs=[c['vertices'][i:i+3] for i in range(0,18,3)]
   for x,y,z in vs[3:]:
    key=(round(x,5),round(z,5))
    if key in corners:assert abs(corners[key]-y)<1e-5,'Uneven collision seam'
    corners[key]=y
   edges={}
   for i in range(0,24,3):
    t=c['triangles'][i:i+3]
    assert all(0<=k<6 for k in t)
    for a,b in zip(t,t[1:]+t[:1]):
     key=tuple(sorted((a,b)));edges[key]=edges.get(key,0)+1
   assert set(edges.values())=={2},'Open cargo collision prism'
  for i,x in enumerate(xs):
   for j,y in enumerate(ys):
    assert abs(corners[(round(-y,5),round(x,5))]-hs[i][j])<1e-5
    if i:assert abs(hs[i][j]-hs[i-1][j])/(x-xs[i-1])<=.361
    if j:assert abs(hs[i][j]-hs[i][j-1])/(y-ys[j-1])<=.361
  assert max(map(max,hs))-min(map(min,hs))>.1,'Cargo collision flattened into a floor'
 assert 0<s['sailPivot'][1]<=s['airHeight']<25
 checks+=1;print(f'PASS: {name}: {triangles:,} triangles, {parts} material batches; sail, helm, boarding and collision resources valid.')
print(f'PASS: all {checks} final fleet assets, closed keel sections and merchant hold coverage.')
