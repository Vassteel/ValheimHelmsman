"""Bake the boatyard visual pass. Never changes physics, scripts or prefab identities."""
from pathlib import Path
import gzip,json,struct,hashlib
import numpy as np
from kit import *
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'assets/ships/redesign';OUT.mkdir(exist_ok=True)
source=json.load(gzip.open(ROOT/'output/model-assessment/source-meshes.json.gz','rt'))
byname={s['name']:s for s in source}
rig_roots=json.loads((OUT/'rig-routes.json').read_text())
ships=['RowingCanoe','DoubleRowingCanoe','LittleBoat','HerculeShip','MercantShip','CargoShip','FastShipSkuldelev','BigCargoShip','WarShip','Skuldelev','CargoCaravel','GoblinShip','TaurusWarShip','CargoAnimalShip','HugeCargoShip']
items=['ResinWood','CaulkedWood','ClothShip','ShipRope','WindBelt','FishExtract','FishExtract2','DriedFishBasket']
# Deck heights remain fixed. Only exposed stem/upper-end surfaces are reshaped.
decks=[-.2,-.2,-.2,.95,.63,.45,.9,.75,1.15,1.25,5.1,1.16,3.35,1.1,1.1]
profiles=[(.55,.75),(.70,.52),(.52,.85),(.80,.60),(.62,.45),(.45,.65),(.85,.60),(.55,.42),(.76,.52),(.62,.83),(.75,.55),(.42,.70),(.40,.42),(.62,.52),(.52,.40)]
plans=[];preview=[];audit=[]
def bounds(parts):
 v=np.concatenate([np.array(p['vertices']) for p in parts]);return v.min(axis=0),v.max(axis=0)
def mesh_from_kit(k,route=[],name='',matrix=None):
 v=np.array(k.v)
 if matrix is not None:
  inv=np.linalg.inv(matrix);v=(np.column_stack([v,np.ones(len(v))])@inv.T)[:,:3]
 return dict(route=route,name=name,expected=0,custom=True,vertices=v.tolist(),uv=k.uv,groups=k.t)
def fit(k,lo,hi):
 v=np.array(k.v);a=v.min(axis=0);b=v.max(axis=0);scale=(hi-lo)/np.maximum(b-a,.001)
 return k.transform(scale,lo-a*scale)
def visual(k,name):return [dict(name=name,material=['wood','wood','iron','rope','cloth','blue cloth','oilcloth'][i],vertices=k.v,triangles=t,color=COLORS[i]) for i,t in enumerate(k.t) if t]
def unique(parts):
 d={}
 for p in parts:d.setdefault(tuple(p['route']),[]).append(p)
 return d
for name in json.loads((ROOT/'assets/ships/content-roots.json').read_text()):
 s=byname[name];parts=s['parts'];plan=dict(name=name,replacements=[],hide=[],extra=None);shown=[];note=''
 if name in ships:
  ni=ships.index(name);deck=decks[ni];bow,stern=profiles[ni]
  # Target the structure, never animated oars, sails, ropes, rudders, seats or cargo lids.
  selected=[p for p in parts if any(x in (p['name']+' '+p['material']).lower() for x in ['hull','keel','frontmaterial','tail_sidematerial','sail_sidematerial']) and not any(x in p['name'].lower() for x in ['mast','yard'])]
  if not selected:
   selected=[max(parts,key=lambda p:np.ptp(np.array(p['vertices'])[:,2]))]
  lo,hi=bounds(selected);mid=(lo[2]+hi[2])/2;half=(hi[2]-lo[2])/2
  routes=unique([p for p in parts if not p.get('skinned',False) and not any(p['route'][:len(r)]==r for r in rig_roots.get(name,[]))]);changed={}
  for route,ps in routes.items():
   allparts=[p for p in parts if tuple(p['route'])==route];p=ps[0];v=np.array(p['vertices']);new=v.copy();t=(v[:,2]-mid)/max(half,.01)
   if any(word in (p['name']+' '+p['material']).lower() for word in ['oar','rudder','sail','mast','rigging','rope','yard','shroud','tackle','pulley','halyard','parrel','deck','floor','hatch','wheel']):continue
   end=np.clip((np.abs(t)-.67)/.33,0,1);above=np.maximum(v[:,1]-(deck+.55),0)
   # Swept ornamental ends become lower, straighter working stems. Mid-deck is untouched.
   factor=np.where(t>0,bow,stern);new[:,1]-=above*end*(1-factor)
   new[:,2]-=np.sign(t)*end*np.minimum(above,.8)*(.16+(ni%3)*.035)
   # A narrow bevel on the upper rail gives each class its own stem section.
   new[:,0]*=1-end*.04*(above>0)
   if np.max(np.abs(new-v))<.001: # low canoe ends: modest upper-stem rake
    upper=np.clip((v[:,1]-deck)/max(.1,hi[1]-deck),0,1)
    new[:,2]-=np.sign(t)*end*upper*.14
   if np.max(np.abs(new-v))<.0001:continue
   inv=np.linalg.inv(p['matrix']);local=(np.column_stack([new,np.ones(len(new))])@inv.T)[:,:3]
   groups=[[] for _ in range(max(q['submesh'] for q in allparts)+1)]
   for q in allparts:groups[q['submesh']]=q['triangles']
   plan['replacements'].append(dict(route=list(route),name=p['name'],expected=len(v),custom=False,vertices=local.tolist(),uv=p['uv'],groups=groups))
   for q in allparts:changed[id(q)]=dict(q,vertices=new.tolist())
  # Rebuild separate ornamental figureheads as restrained carved seabirds.
  ornaments=[p for p in parts if p['name'].lower() in ['skull_head','taurus']]
  for route,ps in unique(ornaments).items():
   a,b=bounds([changed.get(id(q),q) for q in ps]);k=Kit();k.bird_carving((0,0,0),1,ni%2);fit(k,a,b)
   plan['replacements']=[m for m in plan['replacements'] if m['route']!=list(route)]
   p=ps[0];plan['replacements'].append(mesh_from_kit(k,list(route),p['name'],p['matrix']));
   for q in ps:changed[id(q)]=None
   shown+=visual(k,'Carved seabird prow')
  # Purpose-specific additions use open, high-clearance framing. The deck stays open.
  k=Kit();width=min((hi[0]-lo[0])*.30,1.7);z=mid-half*(.70 if name=='CargoCaravel' else .40)
  if ni<3:
   # Low stern rack. Its position is outside the central rowing station.
   z=mid-half*.65;y=deck+.24
   for side in [-1,1]:k.beam((side*width*.4,y,z-.2),(side*width*.4,y,z+.2),.055)
   for zz in [z-.18,z,z+.18]:k.beam((-width*.48,y+.04,zz),(width*.48,y+.04,zz),.05,.09,END)
  elif name in ['HerculeShip','CargoAnimalShip']:
   # Net/lashing gantry at the aft working area, rather than a cabin.
   for side in [-1,1]:k.beam((side*width,deck+.1,z),(side*width,deck+2.15,z),.11)
   k.beam((-width-.15,deck+2.15,z),(width+.15,deck+2.15,z),.14)
   for x in [-width*.5,width*.5]:k.tube([(x,deck+2.15,z),(x,deck+1.9,z+.15)],.03,ROPE,6)
  else:
   # Merchant/cargo shelters have different lengths and roof forms. Fast hulls get
   # only a short navigation awning, keeping their low runner silhouette.
   length=half*(.36 if name in ['MercantShip','BigCargoShip','HugeCargoShip','CargoCaravel'] else .19)
   height=deck+2.15;roofwidth=width*1.7
   for side in [-1,1]:
    for zz in [z-length/2,z+length/2]:k.beam((side*roofwidth/2,deck+.1,zz),(side*roofwidth/2,height-.22,zz),.10)
    k.beam((side*roofwidth/2,height-.22,z-length/2),(side*roofwidth/2,height-.22,z+length/2),.08)
   k.cloth((0,height,z),roofwidth,length,.25,CLOTH if ni%2 else BLUE)
   for zz in [z-length/2,z+length/2]:
    k.beam((-roofwidth/2,height-.22,zz),(0,height+.015,zz),.055)
    k.beam((0,height+.015,zz),(roofwidth/2,height-.22,zz),.055)
  plan['extra']=mesh_from_kit(k);shown+=visual(k,'Boatyard end fittings')
  shown += [changed.get(id(p),p) for p in parts if changed.get(id(p),p) is not None]
  note='Individual stem rake/height; deck preserved; original rig and cargo retained; class-specific rack, gantry or shelter'+('; replacement bird figurehead' if ornaments else '')
 else:
  keep=[]
  if name=='CarpentersTable':k=bench();note='Asymmetric pegged bench, vise, tool tray, rib jig and shelf'
  elif name in ['FishingDock','FishingDock_Extension']:
   k=dock(name.endswith('Extension'));note='Braced plank dock; dedicated fish sorting shelf and net roller'
   if name=='FishingDock':keep=[p for p in parts if 'woodchest' in p['name'].lower()];
  elif name=='OilPress':k=press();note='New braced frame, screw press, staves and collection spout'
  elif name in ['PierCrane1','PierCrane2']:k=crane(name.endswith('1'));note='New winch-jib geometry, replacing treadwheel silhouette'
  elif name.startswith('Pulley'):
   keep=[parts[0]];a,b=bounds(parts);k=Kit();h=b[1]-a[1];z=(a[2]+b[2])/2
   for side in [-1,1]:k.beam((side*.55,a[1],z),(side*.55,b[1],z),.13)
   k.beam((-.70,b[1],z),(.70,b[1],z),.16)
   for side in [-1,1]:k.beam((side*.55,b[1]-.65,z),(0,b[1],z),.08)
   k.tube([(0,b[1],z),(0,b[1]-.5,z)],.035,ROPE)
   for side in [-1,1]:k.box((side*.55,a[1]+.06,z),(.26,.12,.9),WOOD)
   note='Twin-post catch hoist, diagonal braces; original catch species retained'
  elif name.startswith('ShipConstruction'):k=construction({'ShipConstruction':0,'ShipConstruction1':1,'ShipConstruction2':2}[name]);note='New keel/rib/planking geometry on braced slipway'
  elif name.startswith('Totem'):k=totem(int(name[-1])-1);note='New carved nautical post with rope binding'
  elif name in ['Enguias','Peixes','RedePesca']:k=rack(name=='RedePesca',name=='Enguias');note='New pegged frame and individually modelled catch/net'
  elif name in items:k=item(name);note='Original modelled boatyard supply item with readable role-specific silhouette'
  else:raise RuntimeError('Uncovered asset: '+name)
  # Dock and workbench are authored to the existing interaction surface height.
  # Other static models use the old footprint; no recipe or collider changes.
  replaced=[p for p in parts if p not in keep and not (name=='FishingDock' and p['name'] in ['civilian 3','default'])]
  if name not in ['CarpentersTable','FishingDock','FishingDock_Extension','OilPress'] and not name.startswith('Pulley'):
   lo,hi=bounds(parts)
   if name in items:
    v=np.array(k.v);a=v.min(axis=0);b=v.max(axis=0);scale=max(hi-lo)/max(b-a);centre=(lo+hi)/2
    k.transform([scale]*3,centre-(a+b)*.5*scale)
   else:fit(k,lo,hi)
  if name in items:
   first=parts[0];plan['replacements'].append(mesh_from_kit(k,first['route'],first['name'],first['matrix']))
   plan['hide']=[dict(route=list(r),name=ps[0]['name']) for r,ps in unique(parts).items() if list(r)!=first['route']]
  else:
   plan['hide']=[dict(route=list(r),name=ps[0]['name']) for r,ps in unique(replaced).items()]
   plan['extra']=mesh_from_kit(k)
  shown=keep+visual(k,name+' boatyard')
 plans.append(plan);preview.append(dict(name=name,parts=shown));audit.append(dict(name=name,design=note,replaced_meshes=len(plan['replacements']),hidden_renderers=len(plan['hide']),new_triangles=sum(len(t)//3 for t in (plan['extra']['groups'] if plan['extra'] else []))))
# Versioned, deterministic binary consumed without a JSON dependency in Unity.
buf=bytearray(b'HMD1')
def integer(n):buf.extend(struct.pack('<i',n))
def string(s):b=s.encode('utf-8');integer(len(b));buf.extend(b)
def target(m):integer(len(m['route']));[integer(n) for n in m['route']];string(m['name'])
def mesh(m):
 target(m);integer(m['expected']);integer(int(m['custom']));v=np.array(m['vertices'],dtype='<f4');u=np.array(m['uv'],dtype='<f4')
 assert len(v)==len(u) and np.isfinite(v).all() and np.isfinite(u).all()
 integer(len(v));buf.extend(v.tobytes());buf.extend(u.tobytes());integer(len(m['groups']))
 for indices in m['groups']:
  assert len(indices)%3==0 and (not indices or min(indices)>=0 and max(indices)<len(v))
  integer(len(indices));buf.extend(np.array(indices,dtype='<i4').tobytes())
integer(len(plans))
for p in plans:
 string(p['name']);integer(len(p['replacements']))
 for m in p['replacements']:mesh(m)
 integer(len(p['hide']))
 for h in p['hide']:target(h)
 integer(1 if p['extra'] else 0)
 if p['extra']:mesh(p['extra'])
(OUT/'models.bin.gz').write_bytes(gzip.compress(buf,mtime=0))
(OUT/'designs.json').write_text(json.dumps(audit,indent=2)+'\n')
with gzip.open(ROOT/'output/model-assessment/redesigned.json.gz','wt') as f:json.dump(preview,f)
print(f'{len(plans)} covered assets; {len(buf):,} bytes raw; {len(gzip.compress(buf,mtime=0)):,} compressed')
