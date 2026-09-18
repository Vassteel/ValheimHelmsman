"""Original dugout and strip-built kayaks; no imported meshes, materials or rigs.
Run with the project's bpy Python. +X bow, +Z up. Export the same HMF as FinalFleet.
"""
import ast,bpy,bmesh,gzip,io,json,math,struct,sys,runpy
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
CONFIG={'dugout':('HelmsmanDugout',4.6,1.10,[-.65]),'kayak':('HelmsmanFinewoodKayak',5.2,1.06,[0]),'tandem':('HelmsmanTandemKayak',6.8,1.18,[-1.25,1.15])}
def unity(p):return [-p[1],p[2],p[0]]
# Shared triangulation and coordinate conversion, without executing the fleet builder.
source=ast.parse((ROOT/'tools/FinalFleet/build.py').read_text())
exec(compile(ast.Module(body=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name=='meshdata'],type_ignores=[]),'meshdata','exec'))

def build(kind):
 env=runpy.run_path(str(ROOT/'design/small-boat-expansion-v1/primitives.py'))
 mesh,box,tube,mat=[env[k] for k in ['mesh','box','tube','mat']];ship=env['ship'];scene=env['scene']
 wood=env['oak'];rope=env['rope'];dark=env['rope_dark'];edge=env['endgrain'];deck=env['deckm']
 inlay=mat('Finewood dark inlay',(.16,.075,.029),grain=True)
 prefab,length,beam,seats=CONFIG[kind];L=length/2;kayak=kind!='dugout';floor=.15;rail=.58
 dest=ROOT/'design/paddle-craft-v1'/kind;dest.mkdir(parents=True,exist_ok=True)
 def width(x):return max(.022,beam/2*max(0,1-(abs(x)/L)**(1.7 if kayak else 3.2))**(.62 if kayak else .40))
 def sheer(x):return rail+.10*(abs(x)/L)**3
 def bottom(x):return .015+.38*(abs(x)/L)**5
 # Continuous thick hull, with closed ends and a hollow interior. No overlapping bands.
 xs=np.linspace(-L,L,81);M=20;vs=[]
 for inner in [False,True]:
  for x in xs:
   w=max(.008,width(x)-(.055 if inner else 0));top=sheer(x);base=bottom(x)+(.09 if inner else 0)
   if inner:base=max(base,top*max(0,(abs(x)/L-.88)/.12))
   for j in range(M+1):
    a=math.pi*j/M
    y=w*math.cos(a);z=top-(top-base)*math.sin(a)
    if not kayak:
     if inner:z=max(floor,z)
     else:z+=.013*math.sin(x*9+j*2)*math.sin(a)
    vs.append((x,y,z))
 faces=[];layer=len(xs)*(M+1)
 for layer_i in range(2):
  off=layer_i*layer
  for i in range(len(xs)-1):
   for j in range(M):
    a=off+i*(M+1)+j;f=(a,a+1,a+M+2,a+M+1);faces.append(f if layer_i==0 else f[::-1])
 for i in range(len(xs)-1):
  for j in [0,M]:
   a=i*(M+1)+j;b=a+M+1;faces.append((a,b,b+layer,a+layer))
 for i in [0,len(xs)-1]:
  for j in range(M):
   a=i*(M+1)+j;faces.append((a,a+layer,a+layer+1,a+1))
 hull=mesh('hull band - continuous carved shell',vs,faces,wood)
 # Wood grain follows length; outer and inner shell surfaces have predictable density.
 uv=hull.data.uv_layers.active
 for face in hull.data.polygons:
  for li in face.loop_indices:
   v=hull.data.vertices[hull.data.loops[li].vertex_index].co;uv.data[li].uv=(v.x/1.8,(v.y+v.z)/.75)
 for side in [-1,1]:tube('Gunwale timber',[(x,side*width(x),sheer(x)+.006) for x in xs],.024 if kayak else .034,edge,8)
 # A real closed end-grain cap seals each stem rather than covering a hole with a prop.
 for side in [-1,1]:tube('Stem endgrain',[(side*(L-.07),0,bottom(L-.07)),(side*L,0,sheer(L))],.035,edge,8)
 points=[];colliders=[]
 def solid(c,d):colliders.append({'position':unity(c),'size':[d[1],d[2],d[0]]})
 for i in range(36):
  x=-L*.88+(i+.5)*length*.88/36;w=max(.06,width(x)-.10)
  z=max(floor,bottom(x)+.1)
  solid((x,0,z-.05),(length*.88/36+.01,w*2,.10))
 def opening(x):
  return max([.365*math.sqrt(max(0,1-((x-c)/.68)**2)) for c in seats]+[0]) if kayak else 0
 if kayak:
  # Each deck strip follows the cockpit boundary. Hole edges are part of the mesh,
  # not transparent paint or a dark disc on top of an unbroken deck.
  stations=sorted(set(round(float(x),6) for x in xs)|{round(c+.68*math.cos(math.pi*i/32),6) for c in seats for i in range(33)})
  for side in [-1,1]:
   for stripe in range(7):
    vertices=[]
    for x in stations:
     a=opening(x);b=width(x)
     for t in [stripe/7,(stripe+1)/7]:
      y=a+(b-a)*t;z=sheer(x)+.05*(1-(y/max(.01,b))**2)
      vertices.append((x,side*y,z))
    faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(stations)-1)]
    ob=mesh('Finewood deck strip %d %d'%(side,stripe),vertices,faces,inlay if stripe in [1,5] else deck[stripe%4])
    mod=ob.modifiers.new('Deck thickness','SOLIDIFY');mod.thickness=.025
  for c in seats:
   loop=[]
   for i in range(65):
    a=i*math.pi/32;x=c+.68*math.cos(a);y=.365*math.sin(a);z=sheer(x)+.05*(1-(y/width(x))**2)
    loop.append((x,y,z+.06))
   tube('Raised cockpit coaming',loop,.035,edge,10)
   verts=[]
   for x,y,z in loop[:-1]:verts.extend([(x,y,z-.08),(x,y,z)])
   ob=mesh('Cockpit inner lip',verts,[(2*i,2*((i+1)%64),2*((i+1)%64)+1,2*i+1) for i in range(64)],wood)
   ob.modifiers.new('Lip thickness','SOLIDIFY').thickness=.024
  # Lashings have visible timber eyes at every attachment and sit above the deck.
  for side in [-1,1]:
   start=side*(max(abs(c) for c in seats)+.91);end=side*(L-.43)
   if abs(end-start)>.35:
    zs=np.linspace(start,end,4)
    for i,x in enumerate(zs):
     for s in [-1,1]:
      y=s*width(x)*.72;z=sheer(x)+.055
      tube('Deck lashing eye',[(x-.04,y,z),(x,y,z+.035),(x+.04,y,z)],.013,wood,6)
     if i:
      prev=zs[i-1]
      for s in [-1,1]:tube('Crossed deck lashing',[(prev,s*width(prev)*.72,sheer(prev)+.055),((prev+x)/2,0,sheer((prev+x)/2)+.072),(x,-s*width(x)*.72,sheer(x)+.055)],.009,dark,6)
 else:
  # A dugout reads as one hewn log; small irregular tool cuts are geometry in the rim.
  for i in range(18):
   x=-L*.8+i*length*.8/17
   for side in [-1,1]:tube('Adze rim edge',[(x-.035,side*(width(x)-.02),sheer(x)+.008),(x+.03,side*(width(x)-.038),sheer(x)+.005)],.006,edge,5)
 for i,c in enumerate(seats):
  z=.225
  box('Seat cushion timber',(c-.10,0,z),(.48,.59,.065),wood,.025)
  if kayak:
   box('Low cockpit backrest',(c-.40,0,.37),(.07,.51,.27),wood,.022)
   box('Foot brace',(c+.50,0,.20),(.06,.53,.07),edge,.008)
  else:
   for side in [-1,1]:box('Seat support',(c-.1,side*.22,.17),(.30,.065,.14),wood,.008)
  points.append({'kind':'helm' if i==0 else 'seat','position':unity((c-.1,0,z+.034)),'exit':unity((c-.1,0,.26)),'facing':[0,0,1],'size':[]})
  for side in [-1,1]:
   x=c;w=width(x);tube('Boarding grab loop',[(x-.11,side*(w+.02),sheer(x)),(x,side*(w+.065),sheer(x)+.025),(x+.11,side*(w+.02),sheer(x))],.013,rope,6)
   points.append({'kind':'ladder','position':unity((c,side*(w+.10),.40)),'exit':unity((c,0,.29)),'facing':[-side,0,0],'size':[.26,.78,.5]})
 # Cargo has a visible, reachable lid rather than an invisible point under the deck.
 holdx=0 if kind=='tandem' else length*.29
 if kayak:
  z=sheer(holdx)+.064;loop=[(holdx+.26*math.cos(i*math.pi/12),.23*math.sin(i*math.pi/12),z) for i in range(24)]
  verts=[(x,y,h) for h in [z-.018,z+.012] for x,y,_ in loop]
  mesh('Small sealed cargo hatch',verts,[tuple(reversed(range(24))),tuple(range(24,48))]+[(i,(i+1)%24,(i+1)%24+24,i+24) for i in range(24)],wood)
 else:
  box('Simple cargo box',(holdx,0,.27),(.36,.30,.23),wood,.012)
  box('Cargo box lid',(holdx,0,.395),(.38,.32,.03),edge,.008)
 tube('Cargo lid loop',[(holdx-.045,0,.415 if not kayak else z+.025),(holdx,0,.455 if not kayak else z+.065),(holdx+.045,0,.415 if not kayak else z+.025)],.010,rope,6)
 # The playable paddle is its own animation group, centred at the shaft grip midpoint.
 # +Y Blender = transverse shaft; positive end is the single blade on the dugout.
 tube('Paddle shaft',[(0,-1.12 if kayak else -.52,0),(0,1.12 if kayak else .98,0)],.020,wood,10)
 for side in ([-1,1] if kayak else [1]):
  outline=[(-.06,.70),(-.13,.83),(-.145,1.10),(-.10,1.25),(0,1.28),(.10,1.25),(.145,1.10),(.13,.83),(.06,.70)] if kayak else [(-.045,.54),(-.13,.67),(-.15,1.12),(-.1,1.23),(.1,1.23),(.15,1.12),(.13,.67),(.045,.54)]
  verts=[(x,side*y,z) for z in [-.012,.012] for x,y in outline];n=len(outline)
  mesh('Paddle blade',verts,[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],edge)
 if not kayak:tube('Paddle palm grip',[(-.075,-.52,0),(.075,-.52,0)],.025,edge,8)
 # Close solidified surfaces and orient normals before exporting to native culling shaders.
 for ob in ship.objects:
  if ob.type!='MESH':continue
  bpy.context.view_layer.objects.active=ob
  for mod in list(ob.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
  bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
  if all(e.is_manifold for e in bm.edges) and bm.calc_volume(signed=True)<0:bmesh.ops.reverse_faces(bm,faces=list(bm.faces))
  bm.to_mesh(ob.data);bm.free()
 # 18 closed fitted collision prisms; soles and rails leave the player cavity open.
 hullsolids=[]
 for i in range(18):
  vertices=[]
  for x in [-L*.94+i*length*.94/18,-L*.94+(i+1)*length*.94/18]:
   top=max(floor-.03,bottom(x)+.05);w=max(.02,width(x)*.5)
   vertices.extend([unity((x,-.02,bottom(x))),unity((x,.02,bottom(x))),unity((x,w,top)),unity((x,-w,top))])
  faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)];triangles=[]
  for a,b,c,d in faces:triangles.extend([a,c,b,a,d,c])
  hullsolids.append({'vertices':sum(vertices,[]),'triangles':triangles})
 for i in range(32):
  x=-L+(i+.5)*length/32
  for side in [-1,1]:solid((x,side*(width(x)-.03),.39),(length/32+.01,.06,.38))
 watermask=[unity((float(x),s*max(.005,width(x)-.018),sheer(x)-.022)) for x in xs for s in [1,-1]]
 meta={'prefab':prefab,'name':kind,'length':length,'beam':beam,'walkHeight':floor,'waterline':.30,'airHeight':.82,'cargoHeight':.3,'points':points,'colliders':colliders,'hullSolids':hullsolids,'cargoSolids':[],'sheets':[],'waterMask':watermask,'sailPivot':[0,.8,0],'rudderPivot':[0,0,0]}
 materials=[];groups={}
 for ob in ship.objects:
  if ob.type!='MESH':continue
  g='paddle' if ob.name.startswith('Paddle') else 'hull' if ob==hull else 'fixed';m=ob.data.materials[0]
  if m not in materials:materials.append(m)
  key=(g,materials.index(m));v,t=meshdata(ob);vs,ts=groups.setdefault(key,([],[]));off=len(vs);vs.extend(v);ts.extend(x+off for x in t)
 stream=io.BytesIO()
 def I(n):stream.write(struct.pack('<i',n))
 def raw(b):I(len(b));stream.write(b)
 def text(t):raw(t.encode())
 stream.write(b'HMF1');text(json.dumps(meta,separators=(',',':')));I(len(materials))
 for j,m in enumerate(materials):
  text(m.name);im=next(n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE');im.filepath_raw=str(dest/f'texture-{j:02d}.png');im.file_format='PNG';im.save();raw(Path(im.filepath_raw).read_bytes());stream.write(struct.pack('<4f',*m.diffuse_color))
 I(len(groups))
 for (g,m),(vs,ts) in groups.items():text(g);I(m);I(len(vs));stream.write(np.asarray(vs,dtype='<f4').tobytes());I(len(ts));stream.write(np.asarray(ts,dtype='<i4').tobytes())
 (ROOT/'assets/ships/final'/f'{prefab}.bin.gz').write_bytes(gzip.compress(stream.getvalue(),mtime=0))
 (dest/'runtime.json').write_text(json.dumps(meta,indent=2))
 (dest/'validation.json').write_text(json.dumps({'vertices':sum(len(v) for v,t in groups.values()),'triangles':sum(len(t)//3 for v,t in groups.values()),'seats':len(seats),'original_geometry':True},indent=2))
 # Display the paddle resting along the boat in the review, not through its hull.
 for ob in ship.objects:
  if ob.name.startswith('Paddle'):
   ob.rotation_euler.z=math.pi/2;ob.location=(L*.32,.18,.83)
 bpy.ops.object.select_all(action='DESELECT')
 for ob in ship.objects:ob.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(dest/f'{kind}.glb'),export_format='GLB',use_selection=True,export_apply=True)
 bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(dest/f'{kind}.blend'))
 print('EXPORTED',kind,flush=True)
for kind in sys.argv[1:] or CONFIG:build(kind)
