"""Snekkja: standalone game study informed by Skuldelev 5. No game assets are imported or registered.
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
ship=bpy.data.collections.new('SNEKKJA - standalone review model')
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
paint=mat('Burnt ochre hull band',(.28,.09,.043),grain=True)
yellow=mat('Natural ochre hull band',(.42,.29,.08),grain=True)
oak=mat('Hewn oak framing',(.25,.135,.058),grain=True)
endgrain=mat('Fresh worn timber edges',(.35,.21,.10),grain=True)
deckm=[mat('Worn deck pine %d'%i,(.35+i*.015,.236+i*.010,.125+i*.006),grain=True) for i in range(4)]
spar=mat('Pine spar',(.31,.181,.081),grain=True,vertical=True)
rope=mat('Hemp and bast rope',(.38,.29,.16),.95)
rope_dark=mat('Tarred standing rigging',(.11,.095,.066),.98)
iron=mat('Dark iron clinch heads',(.075,.077,.070),.61)
cloth=[mat('Unbleached wool sail panel %02d'%i,(.53+(i%3)*.014,.46+(i%3)*.011,.34+(i%3)*.008),.98) for i in range(12)]
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


# Evidence-led proportions: Skuldelev 5 museum archaeological reconstruction.
# Original interpreted lines, not a traced archaeological lines plan.
L=8.65;beam=2.47;depth=1.16
profile_w=[0,.25,.51,.68,.81,.91,.96,1.0,1.0]
profile_z=[0,.06,.17,.32,.48,.65,.79,.91,1.0]
def interp(values,t):
 q=t*(len(values)-1);i=min(int(q),len(values)-2);u=q-i
 return values[i]*(1-u)+values[i+1]*u
def hull(x,s,side=1,inside=0):
 t=min(abs(x/L),1);w=beam*.5*max(.00001,1-t**2.6)**.70
 keel=.025+.48*t**5;rail=depth+.65*t**3
 return (x,side*max(.016,w*interp(profile_w,s)-inside),keel+(rail-keel)*interp(profile_z,s))
def inside_width_at(x,z):
 lo,hi=0.,1.
 for _ in range(30):
  mid=(lo+hi)/2
  if hull(x,mid)[2]<z:lo=mid
  else:hi=mid
 return abs(hull(x,(lo+hi)/2,inside=.07)[1])
xs=[-L+2*L*i/100 for i in range(101)]
row_x=[-5.4+.9*i for i in range(13)]
port_objects={}
for side in [-1,1]:
 for j in range(8):
  low=max(0,j/8-.009);high=(j+1)/8;verts=[];faces=[]
  for x in xs:
   for s,d in [(low,0),(high,0),(low,.036),(high,.036)]:
    p=list(hull(x,s,side,d));p[1]+=side*.010*(1-s);verts.append(p)
  for k in range(100):
   a=k*4;b=a+4;faces.extend([(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a+1,b+1,b+3,a+3),(a,a+2,b+2,b)])
  faces.extend([(0,1,3,2),(400,402,403,401)])
  ob=mesh(('Port' if side==1 else 'Starboard')+' clinker strake %02d'%j,verts,faces,yellow if j==6 else paint if j==7 else pine[j%7])
  if j==7:port_objects[side]=ob
 tube(('Port' if side==1 else 'Starboard')+' gunwale',[hull(x,1,side,-.008) for x in xs],.053,oak,8)
 # Surviving evidence supports a rail retaining shields along the gunwale.
 tube('Shield retaining rail '+str(side),[hull(x,.91,side,-.044) for x in xs[13:-13]],.025,oak,6)
 for x in row_x:
  tube('Curved oak frame',[hull(x,j/20,side,.052) for j in range(1,21)],.046,oak,6)
# Circular working oarports in the upper strake, cut at each rowing station.
for side,ob in port_objects.items():
 for x in row_x:
  hx=x+.20;hp=hull(hx,.93,side)
  bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.064,depth=.50,location=hp,rotation=(pi/2,0,0))
  cut=bpy.context.object;cut.name='temporary oarport cutter'
  bpy.context.view_layer.objects.active=ob
  mod=ob.modifiers.new('Round oarport','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cut
  bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cut,do_unlink=True)
# Modest, swept stems. Decorative dragon heads are not assumed from the wreck.
tube('Oak keel',[(x,0,-.035+.48*(abs(x)/L)**5) for x in xs],.082,oak,8)
for side in [-1,1]:
 tube(('Bow' if side==1 else 'Stern')+' stepped stem',[(side*x,0,z) for x,z in [(7.87,.47),(8.24,.78),(8.48,1.20),(8.64,1.73),(8.63,2.12),(8.51,2.39)]],.082,oak,8)
# Low removable floorboards; no raised fighting castle or freight hold.
for x in [-6.75+i*.25 for i in range(55)]:
 z=.31+.18*(abs(x)/6.8)**4;w=2*(inside_width_at(x,z)-.035)
 if w>.05:box('Removable sole board',(x,0,z),(.241,w,.054),deckm[int((x+7)*4)%4],.004)
# Thirteen rowing thwarts; central mast sits between the middle stations.
attachments=[]
for i,x in enumerate(row_x):
 z=.66+.18*(abs(x)/6.8)**4;w=2*(inside_width_at(x,z)-.028)
 for dx in [-.073,.073]:box('Rowing thwart %02d'%i,(x+dx,0,z),(.142,w,.072),deckm[i%4],.006)
 for y in [-w*.35,w*.35]:
  box('Thwart support',(x,y,z-.14),(.07,.07,.24),oak,.004)
  attachments.append({'id':'seat_%02d_%s'%(i,'port' if y>0 else 'starboard'),'kind':'passenger seat guide','position':[x,y,z+.05],'facing':[-1,0,0]})
# Rowing oars omitted for the Valheim model; retain the historical benches and ports.
# Shields: display arrangement, with space between each shield and the adjacent oarport.
shield_mats=[yellow,paint,mat('Soot-black shield boards',(.10,.105,.09),grain=True)]
for side in [-1,1]:
 for i,x in enumerate(row_x):
  cx=x-.24;cy=side*(abs(hull(cx,.95)[1])+.10);cz=hull(cx,.95)[2]-.05;r=.34;N=20
  verts=[]
  for dy in [-.014,.014]:
   for k in range(N):verts.append((cx+r*cos(k*2*pi/N),cy+dy,cz+r*sin(k*2*pi/N)))
  faces=[tuple(reversed(range(N))),tuple(N+k for k in range(N))]+[(k,(k+1)%N,(k+1)%N+N,k+N) for k in range(N)]
  mesh('Shield board %02d %s'%(i,side),verts,faces,shield_mats[i%3])
  tube('Shield rawhide rim',[(cx+r*cos(k*pi/16),cy,cz+r*sin(k*pi/16)) for k in range(33)],.016,rope_dark,6)
  # Boss rises outboard; low-poly hemisphere rather than a flat painted disc.
  verts=[];faces=[]
  for j in range(5):
   rr=.089*cos(j*pi/8);yy=cy+side*(.025+.07*sin(j*pi/8))
   for k in range(12):verts.append((cx+rr*cos(k*pi/6),yy,cz+rr*sin(k*pi/6)))
  for j in range(4):
   for k in range(12):a=j*12+k;b=j*12+(k+1)%12;faces.append((a,b,b+12,a+12))
  mesh('Iron shield boss',verts,faces,iron)
# Plain square wool sail, nominal projected cloth area about 46 m2.
mastx=.45
box('Mast keelson',(mastx,0,.40),(3.3,.24,.23),oak,.02)
box('Mast partner',(mastx,0,.81),(.71,.51,.19),oak,.015)
tube('Mast',[(mastx,0,.42),(mastx,0,9.44)],.12,spar,12,r2=.062)
yaw=math.radians(12)
def sailpoint(u,v):
 width=6.85+.32*v;y=(u-.5)*width;x=.16+.66*sin(pi*u)*sin(pi*v)
 return (mastx+x*cos(yaw)-y*sin(yaw),x*sin(yaw)+y*cos(yaw),9.02-6.55*v-.06*sin(pi*u))
sa=sailpoint(0,0);sb=sailpoint(1,0)
tube('Yard',[(sa[0],sa[1],9.12),(mastx,0,9.18),(sb[0],sb[1],9.12)],.071,spar,10)
sail_objs=[]
for panel in range(12):
 verts=[];faces=[]
 for j in range(17):
  for k in range(4):verts.append(sailpoint((panel+k/3)/12,j/16))
 for j in range(16):
  for k in range(3):a=j*4+k;faces.append((a,a+1,a+5,a+4))
 ob=mesh('Sail sewn panel %02d'%panel,verts,faces,cloth[panel]);sail_objs.append(ob)
 sol=ob.modifiers.new('Cloth thickness','SOLIDIFY');sol.thickness=.007
for i in range(13):sail_objs.append(tube('Sail panel seam',[sailpoint(i/12,j/24) for j in range(25)],.008,cloth_seam,5))
for v in [0,1]:sail_objs.append(tube('Sail bolt rope',[sailpoint(i/40,v) for i in range(41)],.020,rope,6))
for u in [0,1]:sail_objs.append(tube('Sail leech rope',[sailpoint(u,i/30) for i in range(31)],.020,rope,6))
for v in [.62,.81]:
 for i in range(1,12):
  p=Vector(sailpoint(i/12,v));sail_objs.append(line('Sail reef tie',p,p+Vector((.04,0,-.16)),.011,rope_dark,.02))
line('Forestay',(mastx,0,9.27),(7.42,0,1.58),.027,rope_dark,.05)
line('Backstay',(mastx,0,9.19),(-7.32,0,1.60),.023,rope_dark,.05)
for side in [-1,1]:
 for x in [-1.6,-.7]:line('Shroud',(mastx,side*.06,8.83),(x,side*1.17,1.10),.022,rope_dark,.04)
 line('Yard brace',sa if side==-1 else sb,(-4.5,side*.91,1.04),.019,rope,.08)
 sail_objs.append(line('Sail sheet',sailpoint(0 if side==-1 else 1,1),(-3.60,side*.97,1.06),.022,rope,.08))
line('Halyard',(mastx,0,9.16),(mastx+.10,.17,.77),.023,rope,.02)
# Continuous carved steering oar, shallow and outside the starboard planking.
sections=[((-6.52,-.10,1.67),.045,.036),((-6.70,-.47,1.72),.057,.045),((-6.95,-1.00,1.66),.071,.055),((-7.05,-1.15,1.15),.067,.055),((-7.17,-1.27,.70),.074,.049),((-7.28,-1.40,.28),.14,.042),((-7.39,-1.53,-.12),.23,.036),((-7.47,-1.63,-.47),.24,.027),((-7.49,-1.65,-.56),.16,.022)]
verts=[];faces=[];N=10
for i,(p,w,t) in enumerate(sections):
 center=Vector(p);direction=(Vector(sections[min(i+1,len(sections)-1)][0])-Vector(sections[max(0,i-1)][0])).normalized()
 across=Vector((1,0,0));across=(across-direction*across.dot(direction)).normalized();dep=direction.cross(across).normalized()
 for k in range(N):verts.append(tuple(center+across*w*cos(k*2*pi/N)+dep*t*sin(k*2*pi/N)))
for j in range(len(sections)-1):
 for k in range(N):a=j*N+k;b=j*N+(k+1)%N;faces.append((a,b,b+N,a+N))
faces.extend([tuple(reversed(range(N))),tuple((len(sections)-1)*N+k for k in range(N))])
mesh('Steering oar single carved oak',verts,faces,oak)
tube('Steering bearing',[(-7.05,-.53,1.14),(-7.05,-1.12,1.14)],.055,oak,8)
for j in range(4):tube('Steering rope collar',[(-7.05+.085*cos(k*pi/8),-1.15+.08*sin(k*pi/8),1.07+j*.033) for k in range(17)],.014,rope,6)
# Small end platforms and restrained stores; these placements are design choices.
for side in [-1,1]:
 for i in range(8):
  x=side*(6.0+i*.22);z=.62+.18*(abs(x)-6)
  w=2*(inside_width_at(x,z)-.026)
  if w>.06:box('Low end platform',(x,0,z),(.214,w,.055),deckm[i%4],.004)
 for j in range(5):hoop('Mooring rope coil',(side*7.55,0,.91+j*.016),.14+j*.014,.12+j*.014,rope,.016)
 for y in [-.30,.30]:
  x=side*6.3;box('Lidded sea chest',(x,y,.88),(.46,.34,.34),pine[2],.012)
  for dx in [-.16,.16]:box('Sea chest iron band',(x+dx,y,1.052),(.025,.35,.015),iron,.002)
attachments.append({'id':'helm','kind':'helm guide','position':[-6.60,-.23,.90]})
# No decorative cargo fills the rowing floor; keep the actual warship layout readable.
(ROOT/'attachment-points.json').write_text(json.dumps({'status':'Visual alignment guides only','axes':'X bow, Y port, Z up; metres','points':attachments},indent=2))
report={'name':'WarShip snekkja study','revision':2,'standalone':True,'reference':'Skuldelev 5 and Helge Ask, Viking Ship Museum','archaeological_target_length_m':17.3,'archaeological_target_beam_m':2.47,'archaeological_target_depth_midships_m':1.16,'historical_rowing_pairs':13,'rowing_oars':0,'steering_oars':1,'shields_displayed':26,'projected_sail_area_m2':45.9155,'textures':'Original 128px maps; embedded','interpretation':'Original hull lines, rig, equipment placement and colour arrangement; not an exact archaeological reconstruction','limitations':['No game integration or player movement tests','Rowing oars intentionally omitted for Valheim; benches retained as passenger-seat guides','Exact paint scheme is not established for the excavated ship']}
(ROOT/'model-info.json').write_text(json.dumps(report,indent=2))
bpy.ops.object.select_all(action='DESELECT')
for ob in ship.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=next(iter(ship.objects))
bpy.ops.export_scene.gltf(filepath=str(ROOT/'snekkja.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True)
# Review studio.
ground=box('Studio floor',(0,0,-.73),(200,200,.07),mat('Studio slate',(.028,.048,.058),.95),0);move(ground,studio)
def light(name,loc,power,size,color):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
 ob=bpy.data.objects.new(name,data);studio.objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector((0,0,3))-ob.location).to_track_quat('-Z','Y').to_euler()
light('Warm key',(3,-12,20),3400,12,(1,.91,.79));light('Cool fill',(-5,9,12),2500,10,(.70,.82,1));light('Bow rim',(12,4,10),1800,8,(1,.9,.72));light('Front fill',(3,-16,6),1400,10,(1,.96,.89))
data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);studio.objects.link(camera);scene.camera=camera;data.type='ORTHO'
def camera_at(loc,target,scale):camera.location=loc;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=scale
camera_at((23,-30,20),(0,0,4.0),21.7)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'snekkja.blend'))
scene.render.filepath=str(ROOT/'snekkja-hero.png');bpy.ops.render.render(write_still=True)
for ob in sail_objs:ob.hide_render=True
camera_at((12,-20,22),(0,0,.75),20.8);scene.render.resolution_x=1700;scene.render.resolution_y=1050
scene.render.filepath=str(ROOT/'snekkja-deck.png');bpy.ops.render.render(write_still=True)
camera_at((0,-27,10),(0,0,1.0),19.5);scene.render.resolution_x=1800;scene.render.resolution_y=700
scene.render.filepath=str(ROOT/'snekkja-profile.png');bpy.ops.render.render(write_still=True)
print('Historical-reference snekkja study exported and rendered',flush=True)
