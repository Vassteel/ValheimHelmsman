"""BigCargoShip: standalone original heavy freight study. No game assets are imported or registered.
Run with Python containing bpy 5.1. Writes only beside this script.
Axes: +X bow, -Y starboard, +Z up. Units: metres.
"""
import bpy, math, random, json, sys
import numpy as np
from pathlib import Path
from mathutils import Vector
from math import sin, cos, pi
ROOT=Path(__file__).resolve().parent
random.seed(1030)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=24
scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED'
scene.render.threads=4
scene.render.resolution_x=1500
scene.render.resolution_y=1200
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Soft studio world')
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.14,.20,.24,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.45
scene.view_settings.view_transform='AgX'
ship=bpy.data.collections.new('BIG CARGO - standalone review model')
scene.collection.children.link(ship)
studio=bpy.data.collections.new('STUDIO - excluded from export')
scene.collection.children.link(studio)

def move(obj,col=ship):
 for c in list(obj.users_collection):c.objects.unlink(obj)
 col.objects.link(obj)
 return obj

def mat(name,c,rough=.8,grain=False,vertical=False):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
 n=m.node_tree.nodes;links=m.node_tree.links;bs=n.get('Principled BSDF')
 bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Roughness'].default_value=rough
 # Original pixel-textured surfaces, packed into the blend and exported in the GLB.
 if not name.startswith('Studio'):
  N=128;rng=np.random.default_rng(sum(map(ord,name)));yy,xx=np.mgrid[0:N,0:N]
  grit=rng.random((N,N));broad=rng.random((16,16)).repeat(8,0).repeat(8,1)
  if grain:
   warp=yy+2.1*np.sin(xx*.045)+1.8*np.sin(xx*.097+yy*.027)
   streak=np.sin(warp*1.45)+.5*np.sin(warp*3.11)
   variation=.81+.10*streak+.16*broad+.055*grit
   # Worn streaks, knots and tar-dark pores stay readable at game distance.
   knot=np.sqrt(((xx-78)/2.5)**2+(yy-48)**2)
   variation-=.18*(np.sin(knot*1.65)>.5)*np.exp(-knot/15)
   variation-=.22*((grit>.965)&(np.sin(warp*2)>.25))
   base=np.array(c)*1.30+np.array([.055,.063,.073])
  else:
   variation=.80+.22*broad+.10*grit
   variation+=.035*((xx%3==0)+(yy%3==0))
   base=np.array(c)*1.28+.055
  rgba=np.ones((N,N,4),dtype=np.float32);rgba[:,:,:3]=np.clip(base[None,None,:]*variation[:,:,None],0,1)
  im=bpy.data.images.new(name+' - original 128px texture',width=N,height=N)
  im.pixels.foreach_set(rgba.ravel());im.pack()
  tex=n.new('ShaderNodeTexImage');tex.image=im;tex.interpolation='Closest';tex.extension='REPEAT'
  links.new(tex.outputs['Color'],bs.inputs['Base Color'])
  bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.20 if grain else .12;bump.inputs['Distance'].default_value=.022
  links.new(tex.outputs['Color'],bump.inputs['Height']);links.new(bump.outputs[0],bs.inputs['Normal'])
 m['vertical_grain']=vertical
 return m

pine=[mat('Tarred pine / strake %02d'%i,(.23+i*.009,.115+i*.0055,.049+i*.003),grain=True) for i in range(7)]
paint=mat('Weathered deep green sheer stripe',(.065,.145,.13),grain=True)
oak=mat('Hewn oak framing',(.25,.135,.058),grain=True)
endgrain=mat('Fresh worn timber edges',(.35,.21,.10),grain=True)
deckm=[mat('Worn deck pine %d'%i,(.35+i*.015,.236+i*.010,.125+i*.006),grain=True) for i in range(4)]
spar=mat('Pine spar',(.31,.181,.081),grain=True,vertical=True)
rope=mat('Hemp and bast rope',(.38,.29,.16),.95)
rope_dark=mat('Tarred standing rigging',(.11,.095,.066),.98)
iron=mat('Dark iron clinch heads',(.075,.077,.070),.61)
cloth=[mat('Heavy striped wool sail %02d'%i,(.49,.43,.31) if (i//2)%2==0 else (.32,.12,.065),.98) for i in range(12)]
cloth_seam=mat('Reinforced sail seams',(.41,.31,.16),.97)
bale_mat=mat('Undyed cargo wrapping',(.48,.43,.30),.99)

def mesh(name,verts,faces,material,col=ship,bevel=0):
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
 obj=bpy.data.objects.new(name,data);col.objects.link(obj)
 if material:data.materials.append(material)
 uv=data.uv_layers.new(name='SurfaceUV')
 for face in data.polygons:
  normal=face.normal;axis=max(range(3),key=lambda i:abs(normal[i]))
  axes=(0,1) if axis==2 else ((0,2) if axis==1 else (1,2))
  if material and material.get('vertical_grain'):axes=axes[::-1]
  for li in face.loop_indices:
   v=data.vertices[data.loops[li].vertex_index].co
   uv.data[li].uv=(v[axes[0]]/1.8,v[axes[1]]/.75)
 if bevel:
  mod=obj.modifiers.new('Soft hewn edges','BEVEL');mod.width=bevel;mod.segments=2
  mod=obj.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 return obj

def box(name,c,d,m,bevel=.012):
 x,y,z=c;a,b,h=[v*.5 for v in d]
 v=[(x+sx*a,y+sy*b,z+sz*h) for sx,sy,sz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
 return mesh(name,v,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],m,bevel=bevel)

def tube(name,pts,r,m,sides=8,r2=None):
 verts=[];faces=[]
 pts=[Vector(p) for p in pts]
 for i,p in enumerate(pts):
  direction=pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)];direction.normalize()
  cross=direction.cross(Vector((0,0,1)) if abs(direction.z)<.92 else Vector((0,1,0)));cross.normalize();up=direction.cross(cross)
  rad=r if r2 is None else r+(r2-r)*i/(len(pts)-1)
  for j in range(sides):verts.append(tuple(p+rad*(cross*cos(2*pi*j/sides)+up*sin(2*pi*j/sides))))
 for i in range(len(pts)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
 faces.append(tuple(reversed(range(sides))));faces.append(tuple((len(pts)-1)*sides+j for j in range(sides)))
 return mesh(name,verts,faces,m)

def line(name,a,b,r=.025,m=rope,sag=0):
 pts=[]
 for i in range(13):
  t=i/12;p=Vector(a).lerp(Vector(b),t);p.z-=sin(pi*t)*sag;pts.append(p)
 return tube(name,pts,r,m,6)

def hoop(name,c,rx,ry,m=rope,r=.025):
 return tube(name,[(c[0]+rx*cos(i*pi/20),c[1]+ry*sin(i*pi/20),c[2]) for i in range(41)],r,m,6)


# Original fuller freight hull: 19.2 m long, 6.3 m beam, deeper bilges.
L=9.6
profile_w=[0,.27,.53,.72,.85,.94,.985,1,1,.995,.99,.98]
profile_z=[0,.045,.11,.21,.33,.45,.57,.68,.78,.86,.94,1]
def interp(values,t):
 q=t*(len(values)-1);i=min(int(q),len(values)-2);u=q-i
 return values[i]*(1-u)+values[i+1]*u
def hull(x,s,side=1,inside=0):
 t=abs(x/L);w=3.15*max(.001,1-t**3.0)**.70
 keel=.04+.76*t**5;rail=2.86+.80*t**3
 return (x,side*max(.02,w*interp(profile_w,s)-inside),keel+(rail-keel)*interp(profile_z,s))
def inside_width_at(x,z):
 lo,hi=0.,1.
 for _ in range(30):
  mid=(lo+hi)/2
  if hull(x,mid)[2]<z:lo=mid
  else:hi=mid
 return abs(hull(x,(lo+hi)/2,inside=.13)[1])
xs=[-L+2*L*i/80 for i in range(81)]
for side in [-1,1]:
 for j in range(13):
  low=max(0,j/13-.008);high=(j+1)/13;verts=[];faces=[]
  for x in xs:
   for s,d in [(low,0),(high,0),(low,.065),(high,.065)]:
    p=list(hull(x,s,side,d));p[1]+=side*.014*(1-s);verts.append(p)
  for k in range(80):
   a=k*4;b=a+4;faces.extend([(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a+1,b+1,b+3,a+3),(a,a+2,b+2,b)])
  faces.extend([(0,1,3,2),(320,322,323,321)])
  mesh(('Port' if side==1 else 'Starboard')+' clinker strake %02d'%j,verts,faces,paint if j>=11 else pine[(j+(side==1))%7])
 tube(('Port' if side==1 else 'Starboard')+' gunwale',[hull(x,1,side,-.015) for x in xs],.105,oak,8)
 for height in [.35,.72]:
  tube('Heavy external rubbing strake',[hull(x,height,side,-.04) for x in xs[2:-2]],.062,oak,6)
 for x in [-7.7,-6.4,-5.1,-3.8,-2.5,-1.2,0,1.2,2.5,3.8,5.1,6.4,7.7]:
  tube('Visible reinforced rib',[hull(x,j/20,side,.12) for j in range(1,21)],.080,oak,6)
tube('Deep oak keel',[(x,0,-.08+.76*(abs(x)/L)**5) for x in xs],.145,oak,8)
for side in [-1,1]:
 tube(('Bow' if side==1 else 'Stern')+' stem',[(side*x,0,z) for x,z in [(8.8,.52),(9.23,1.0),(9.58,1.8),(9.64,2.8),(9.63,3.62),(9.5,3.91)]],.13,oak,8)
# Working decks at both ends, broad side walks, open packed freight wells.
def deck_height(x):return 2.34+.09*(abs(x)-4.85)+.047
for side in [-1,1]:
 for i in range(16):
  x=side*(4.85+i*.255);z=deck_height(x)-.047
  width=2*(inside_width_at(x,z)-.08)
  box(('Fore' if side==1 else 'Aft')+' deck plank %02d'%i,(x,0,z),(.246,width,.094),deckm[i%4],.007)
 for y in [side*2.35,side*2.62]:
  box('Side working walkway',(0,y,2.33),(9.5,.26,.10),deckm[2],.008)
for y in [-1.5,-1.2,-.9,-.6,-.3,0,.3,.6,.9,1.2,1.5]:
 box('Deep hold floor',(0,y,.80),(9.45,.289,.08),deckm[1],.005)
for x in [-4.72,0,4.72]:
 tube('Heavy cargo bulkhead beam',[(x,-2.78,1.64),(x,2.78,1.64)],.15,oak,8)
box('Freight mast keelson',(0,0,1.00),(5.0,.64,.43),oak,.04)
# Separate heavy rig, adapted from our original Ottar rigging geometry.
rig_before=set(ship.objects)
exec(compile((ROOT/'rigging_geometry.py').read_text(),'rigging_geometry.py','exec'))
for ob in set(ship.objects)-rig_before:
 for v in ob.data.vertices:v.co.x*=1.24;v.co.y*=1.31;v.co.z*=1.17
# Carved one-piece side rudder fitted outside the broader hull.
oar_before=set(ship.objects)
exec(compile((ROOT/'steering_geometry.py').read_text(),'steering_geometry.py','exec'))
for ob in set(ship.objects)-oar_before:
 for v in ob.data.vertices:v.co.x*=1.23;v.co.y*=1.38;v.co.z*=1.28
# Cargo prop materials and authored generators.
hide_mats=[mat('Chestnut freight hides',(.28,.135,.063),.97),mat('Tan freight hides',(.48,.31,.17),.96),mat('Dark freight hides',(.16,.14,.11),.98)]
sackmat=mat('Coarse ore and grain sacks',(.45,.40,.29),.99)
bindmat=mat('Dark cargo hemp',(.22,.17,.105),.98)
exec(compile((ROOT/'cargo_primitives.py').read_text(),'cargo_primitives.py','exec'))
# Aft well: substantial stacked logs with real cut ends and broad rope lashings.
for row,(z,count) in enumerate([(1.13,6),(1.68,5),(2.22,4)]):
 for j in range(count):
  y=(j-(count-1)/2)*.55
  x1=-4.46+.11*((j+row)%3);x2=-.53-.12*((j*2+row)%3)
  tube('Freight / round timber',[(x1,y,z),(x2,y,z)],.27,pine[(j+row)%7],10)
  for x in [x1-.005,x2+.005]:
   vs=[(x,y+.267*cos(i*pi/8),z+.267*sin(i*pi/8)) for i in range(16)]
   mesh('Freight / timber cut end',vs,[tuple(range(16))],endgrain)
   for rad in [.09,.17,.23]:tube('Freight / growth ring',[(x,y+rad*cos(i*pi/12),z+rad*sin(i*pi/12)) for i in range(25)],.008,oak,5)
for x in [-3.72,-1.27]:
 tube('Freight / timber stack lashing',[(x,-1.75,.88),(x,-1.72,1.37),(x,-1.19,2.33),(x,-.80,2.51),(x,.80,2.51),(x,1.19,2.33),(x,1.72,1.37),(x,1.75,.88),(x,-1.75,.88)],.047,rope,8)
 for y in [-1.72,1.72]:tube('Freight / timber retaining stake',[(x,y,.88),(x,y,2.1)],.065,oak,6)
# Forward well: ore bins, iron-bound boxes and closely packed stores.
oremat=mat('Dark mineral ore',(.095,.115,.12),.91)
cratewood=mat('Rough freight crate timber',(.25,.205,.125),grain=True)
def ore_crate(x,y,index):
 z=1.00;w=1.12;d=.98;h=1.02
 box('Freight / ore bin bottom',(x,y,z),(w,d,.10),cratewood)
 for level in range(4):
  zz=z+.14+level*.235
  for side in [-1,1]:
   box('Freight / ore bin board',(x,y+side*d/2,zz),(w,.07,.21),cratewood,.008)
   box('Freight / ore bin end',(x+side*w/2,y,zz),(.07,d,.21),deckm[level%4],.008)
 for dx in [-.44,.44]:
  for dy in [-.40,.40]:box('Freight / ore bin post',(x+dx,y+dy,z+.52),(.085,.085,1.11),oak,.009)
 for k in range(11):
  angle=k*2.399;rr=.10+.28*(k%3)/2
  xx=x+rr*cos(angle);yy=y+rr*sin(angle);zz=z+.87+.1*(k%2)
  bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.19,location=(xx,yy,zz))
  ob=move(bpy.context.object);ob.name='Freight / ore lump';ob.scale=(1.18,.85,.68);ob.data.materials.append(oremat)
 for side in [-1,1]:box('Freight / iron bin strap',(x+side*.39,y-d/2-.041,z+.50),(.035,.023,.98),iron,.004)
for i,x in enumerate([1.1,2.39,3.68]):
 for y in [-.63,.63]:ore_crate(x,y,i)
for x in [1.1,2.39,3.68]:
 for y in [-1.64,1.64]:barrel(x,y,1.12,r=.35,h=1.20)
# Soft cargo closes the well perimeter, including around the mast.
for x in [-4.28,-3.40,-2.52,-1.64,-.76,.62,1.5,2.4,3.3,4.20]:
 for y in [-2.00,2.00]:sack(x,y,1.56,r=.28,h=.77,label='Cargo / perimeter freight sack')
for x in [-.45,.45]:
 for y in [-.65,.65]:hide_bale(x,y,1.38,w=.70,d=.75,h=.94)
for x in [1.12,2.40,3.68]:
 bundle(x,-1.64,2.46,length=.78,r=.16)
for x in [-3.50,-2.1]:bundle(x,0,2.67,length=1.20,r=.17)
# Compact cargo handling boom, rope tackle and a manual hauling windlass.
tube('Cargo derrick / boom',[(.10,.14,2.61),(3.45,.23,5.05)],.093,spar,10,r2=.067)
line('Cargo derrick / topping lift',(0,0,8.5),(3.45,.23,5.05),.030,rope,.035)
for y in [.18,.34]:line('Cargo derrick / tackle',(3.38,y,5.01),(3.38,y,3.33),.025,rope)
for z in [4.93,3.40]:
 box('Cargo derrick / pulley block',(3.38,.26,z),(.22,.17,.29),oak,.04)
 tube('Cargo derrick / pulley iron',[(3.38,.15,z-.19),(3.38,.10,z),(3.38,.15,z+.19)],.016,iron,6)
tube('Cargo derrick / cargo hook',[(3.38,.26,3.21),(3.38,.26,3.08),(3.48,.26,3.01),(3.55,.26,3.12)],.026,iron,8)
# Deck gear and backless seating.
attachments=[]
def seat(name,x,y,width,helm=False):
 z=deck_height(x)
 for dx in [-.18,.18]:
  for dy in [-width*.35,width*.35]:box(name+' leg',(x+dx,y+dy,z+.23),(.08,.08,.46),oak)
 for dx in [-.13,.13]:box(name+' seat',(x+dx,y,z+.47),(.25,width,.10),deckm[2])
 attachments.append({'id':name,'kind':'helm' if helm else 'seat','position':[x,y,z+.54]})
seat('Helmsman bench',-7.05,-.40,1.28,True)
for x in [-5.35,5.28]:
 for y in [-1.3,1.3]:seat('Passenger bench '+str(x)+' '+str(y),x,y,.94)
for x in [-6.4,6.6]:
 for y in [-1.62,1.62]:barrel(x,y,deck_height(x),r=.31,h=.75)
for x,y in [(-7.8,.45),(-6.1,.45),(6.5,-.50),(7.5,.55)]:
 hide_bale(x,y,deck_height(x),w=.80,d=.65,h=.43)
 bundle(x,y,deck_height(x)+.61,length=.8,r=.15)
for x,y in [(-7.75,-.65),(-6.40,1.00),(6.3,.60),(7.5,-.60),(8.2,.20)]:sack(x,y,deck_height(x),r=.25,h=.58)
for x in [-8.5,8.5]:
 for j in range(6):hoop('Deck gear / mooring coil',(x,0,deck_height(x)+.025+j*.014),.25+j*.013,.22+j*.013,rope,.020)
# Foredeck windlass, visibly kept ahead of passenger seats and their access.
for y in [-.69,.69]:box('Windlass / bearing post',(6.1,y,deck_height(6.1)+.36),(.19,.18,.72),oak,.02)
tube('Windlass / oak drum',[(6.1,-.95,3.00),(6.1,.95,3.00)],.14,oak,10)
for j in range(14):tube('Windlass / wound rope',[(6.1+.16*cos(i*pi/12),-.30+j*.045,3+.16*sin(i*pi/12)) for i in range(25)],.024,rope,6)
tube('Windlass / handspike',[(5.64,.88,2.66),(6.56,.88,3.34)],.041,oak,8)
# Mast holdfast and static ladders, conforming closely to the wider hull.
tube('Mast handhold',[(.28,-.48,3.42),(.28,.48,3.42)],.05,oak,8)
attachments.append({'id':'mast_holdfast','kind':'holdfast','position':[.75,0,2.38]})
def ladder_width(z):return inside_width_at(0,z)+.13+.067
for side in [-1,1]:
 label='port' if side==1 else 'starboard'
 for x in [-.34,.34]:
  pts=[(x,side*2.84,2.62),(x,side*2.97,2.94),(x,side*3.11,2.97)]+[(x,side*ladder_width(2.84-j*.028),2.84-j*.028) for j in range(94)]
  tube('Boarding '+label+' rope',pts,.035,rope,8)
 for i in range(10):
  z=2.80-i*.28;yy=side*ladder_width(z)
  tube('Boarding '+label+' rung %02d'%i,[(-.43,yy,z),(.43,yy,z)],.045,oak,8)
 for x in [-.26,0,.26]:box('Boarding landing',(x,side*2.05,2.35),(.25,1.10,.10),deckm[1])
 attachments.append({'id':'boarding_'+label,'kind':'ladder','position':[0,side*(ladder_width(.82)+.10),.82],'exit':[0,side*2.02,2.40]})
for ob in ship.objects:
 if ob.name.startswith(('Cargo /','Freight /')):ob['appearance_only']=True;ob['future_collision']='none'
# Ship-only exports and integration notes; no game registration.
(ROOT/'attachment-points.json').write_text(json.dumps({'status':'Visual guides only; interactions unimplemented','axes':'X forward Y port Z up, metres','points':attachments},indent=2))
report={'name':'BigCargoShip heavy freighter study','revision':1,'standalone':True,'length_m':19.2,'beam_m':6.3,'hull_rail_height_midships_m':2.86,'timber_logs':15,'ore_bins':6,'boarding_ladders':2,'passenger_seats':4,'textures':'Original embedded 128px colour maps','limitations':['Original game-oriented concept, not an exact historical reconstruction','No game integration, collisions, flotation or movement validation','Cargo tackle is static; working animation not implemented']}
(ROOT/'model-info.json').write_text(json.dumps(report,indent=2))
bpy.ops.object.select_all(action='DESELECT')
for ob in ship.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=next(iter(ship.objects))
bpy.ops.export_scene.gltf(filepath=str(ROOT/'big-cargo.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True)
# Studio rendering: hull deck view first, then full sailing profile and freight close-up.
ground=box('Studio floor',(0,0,-.72),(200,200,.08),mat('Studio slate',(.028,.048,.058),.95),0);move(ground,studio)
def light(name,loc,power,size,color):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
 ob=bpy.data.objects.new(name,data);studio.objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector((0,0,4))-ob.location).to_track_quat('-Z','Y').to_euler()
light('Warm key',(5,-13,23),4300,14,(1,.90,.77))
light('Cool fill',(-7,12,15),3400,12,(.68,.82,1))
light('Bow rim',(15,5,13),2400,10,(1,.87,.68))
light('Front fill',(4,-19,8),1900,11,(1,.96,.9))
data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);studio.objects.link(camera);scene.camera=camera;data.type='ORTHO'
def camera_at(loc,target,scale):camera.location=loc;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=scale
camera_at((27,-36,24),(0,0,6),26)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'big-cargo.blend'))
scene.render.filepath=str(ROOT/'big-cargo-hero.png');bpy.ops.render.render(write_still=True)
for ob in sail_objs:ob.hide_render=True
camera_at((15,-24,24),(0,0,1.90),23.4);scene.render.resolution_x=1700;scene.render.resolution_y=1100
scene.render.filepath=str(ROOT/'big-cargo-deck.png');bpy.ops.render.render(write_still=True)
camera_at((11,-18,18),(0,0,2.2),16.7);scene.render.resolution_x=1400;scene.render.resolution_y=1100
scene.render.filepath=str(ROOT/'big-cargo-freight.png');bpy.ops.render.render(write_still=True)
print('Standalone heavy freighter exported and rendered',flush=True)
