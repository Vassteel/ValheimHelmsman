"""Standalone original knarr study. No game assets are imported or registered.
Run with Python containing bpy 5.1. Writes only beside this script.
Axes: +X bow, -Y starboard, +Z up. Units: metres.
"""
import bpy, math, random, json, sys
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
ship=bpy.data.collections.new('MERCHANT KNARR - standalone review model')
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
 if grain:
  tex=n.new('ShaderNodeTexCoord');mapping=n.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY'
  mapping.inputs[1].default_value=(60,60,1.6) if vertical else (1.6,65,65)
  links.new(tex.outputs['Generated'],mapping.inputs[0]);noise=n.new('ShaderNodeTexNoise')
  noise.inputs['Scale'].default_value=3;noise.inputs['Detail'].default_value=2
  links.new(mapping.outputs[0],noise.inputs['Vector']);bump=n.new('ShaderNodeBump')
  bump.inputs['Strength'].default_value=.15;bump.inputs['Distance'].default_value=.027
  links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs[0],bs.inputs['Normal'])
 return m
pine=[mat('Tarred pine / strake %02d'%i,(.23+i*.009,.115+i*.0055,.049+i*.003),grain=True) for i in range(7)]
oak=mat('Hewn oak framing',(.25,.135,.058),grain=True)
endgrain=mat('Fresh worn timber edges',(.35,.21,.10),grain=True)
deckm=[mat('Worn deck pine %d'%i,(.35+i*.015,.236+i*.010,.125+i*.006),grain=True) for i in range(4)]
spar=mat('Pine spar',(.31,.181,.081),grain=True,vertical=True)
rope=mat('Hemp and bast rope',(.38,.29,.16),.95)
rope_dark=mat('Tarred standing rigging',(.11,.095,.066),.98)
iron=mat('Dark iron clinch heads',(.075,.077,.070),.61)
cloth=[mat('Ochre wool sail panel %02d'%i,(.67+(i%3)*.017,.53+(i%3)*.012,.30+(i%3)*.009),.98) for i in range(12)]
cloth_seam=mat('Reinforced sail seams',(.41,.31,.16),.97)
bale_mat=mat('Undyed cargo wrapping',(.48,.43,.30),.99)

def mesh(name,verts,faces,material,col=ship,bevel=0):
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
 obj=bpy.data.objects.new(name,data);col.objects.link(obj)
 if material:data.materials.append(material)
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

# Rounded, full-bellied cargo hull: 15.84 m overall, 4.80 m beam.
# This is an interpretation of the museum's published proportions, not traced lines.
L=7.92
profile_w=[0,.18,.40,.61,.78,.87,.94,.98,1,1.0,.995, .98]
profile_z=[0,.055,.13,.24,.36,.47,.58,.69,.79,.87,.94,1]
def interp(values,t):
 q=t*(len(values)-1);i=min(int(q),len(values)-2);u=q-i
 return values[i]*(1-u)+values[i+1]*u

def hull(x,s,side=1,inside=0):
 t=abs(x/L);w=2.4*max(.001,1-t**2.55)**.70
 keel=.02+.77*t**5;rail=2.26+.99*t**3
 return (x,side*max(.018,w*interp(profile_w,s)-inside),keel+(rail-keel)*interp(profile_z,s))

xs=[-L+2*L*i/64 for i in range(65)]
for side in [-1,1]:
 for j in range(11):
  # Lower lip overlaps the preceding strake; each plank has a real inner face.
  low=max(0,j/11-.010);high=(j+1)/11
  verts=[];faces=[]
  for x in xs:
   for s,d in [(low,0),(high,0),(low,.052),(high,.052)]:
    p=list(hull(x,s,side,d));p[1]+=side*.013*(1-s);verts.append(p)
  for k in range(64):
   a=k*4;b=a+4
   faces.extend([(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a+1,b+1,b+3,a+3),(a,a+2,b+2,b)])
  faces.extend([(0,1,3,2),(256,258,259,257)])
  mesh(('Port' if side==1 else 'Starboard')+' clinker strake %02d'%(j+1),verts,faces,pine[(j+(side==1))%7])
 # Inwale and outer sheer strake capping follow the full curved sheer.
 tube(('Port' if side==1 else 'Starboard')+' gunwale',[hull(x,1,side,-.015) for x in xs],.086,oak,8)
 tube(('Port' if side==1 else 'Starboard')+' inner stringer',[hull(x,.84,side,.082) for x in xs[3:-3]],.065,oak,6)

# Stepped keel and restrained, swept stems; no fantasy figureheads.
tube('Oak keel',[(x,0,-.11+.78*(abs(x)/L)**5) for x in xs],.12,oak,8)
for side in [-1,1]:
 pts=[(side*x,0,z) for x,z in [(7.28,.48),(7.58,.84),(7.81,1.35),(7.93,1.95),(7.94,2.65),(7.92,3.27),(7.77,3.51)]]
 tube(('Bow' if side==1 else 'Stern')+' hewn stem',pts,.105,oak,8,r2=.075)

# Visible inside frames, ceiling planks and two open holds.
for i,x in enumerate([-6.5,-5.5,-4.4,-3.3,-2.2,-1.1,0,1.1,2.2,3.3,4.4,5.5,6.5]):
 points=[hull(x,k/16,-1,.09) for k in range(16,-1,-1)]+[hull(x,k/16,1,.09) for k in range(1,17)]
 tube('Curved oak frame %02d'%i,points,.071,oak,6)
def inside_width_at(x,z):
 lo,hi=0.,1.
 for _ in range(30):
  mid=(lo+hi)/2
  if hull(x,mid)[2]<z:lo=mid
  else:hi=mid
 return abs(hull(x,(lo+hi)/2,inside=.11)[1])
# Cut plank ends to the actual bilge envelope, so flooring cannot pierce the hull.
for i,y in enumerate([-1.12,-.84,-.56,-.28,0,.28,.56,.84,1.12]):
 xmax=4.0
 while xmax>.2 and inside_width_at(xmax,.635)<abs(y)+.134:xmax-=.025
 box('Hold ceiling plank',(0,y,.67),(2*xmax,.267,.064),deckm[i%4],.005)
box('Mast keelson',(0,0,.79),(4.4,.48,.32),oak,.055)
box('Mast step',(0,0,1.00),(1.15,.83,.42),oak,.055)
# Cross beams with clear bays on each side of the mast.
for x in [-3.85,0,3.85]:
 w=inside_width_at(x,1.61)-.11
 tube('Cargo hold cross-beam',[(x,-w,1.61),(x,w,1.61)],.105,oak,8)
for side in [-1,1]:
 for i in range(15):
  x=side*(3.98+i*.235);z=1.80+.11*(abs(x)-3.98)
  width=abs(hull(x,.90)[1])*1.78
  box(('Fore' if side==1 else 'Aft')+' working deck plank %02d'%i,(x,0,z),(.224,width,.083),deckm[i%4],.006)
 # Low coaming at ends of the working deck, not a roof or enclosed cabin.
 box('Working deck edge',(side*3.82,0,1.83),(.105,3.82,.18),oak,.012)
# Narrow side walkways leave open central cargo wells.
for side in [-1,1]:
 for y in [side*1.75,side*1.98]:
  xmax=3.8
  while xmax>.2 and inside_width_at(xmax,1.73)<abs(y)+.108:xmax-=.025
  box('Side working plank',(0,y,1.78),(2*xmax,.214,.082),deckm[2],.005)

# Clinch heads: combine into one lightweight mesh, not hundreds of scene objects.
vs=[];fs=[]
for side in [-1,1]:
 for j in range(1,11):
  for i in range(1,32):
   x=-7.6+i*15.2/32;s=j/11-.018;p=Vector(hull(x,s,side,-.018))
   a=len(vs);rad=.021
   for k in range(6):vs.append((p.x+rad*cos(k*pi/3),p.y,p.z+rad*sin(k*pi/3)))
   fs.append(tuple(a+k for k in range(6)))
mesh('Iron clinch nail heads',vs,fs,iron)

# Heavy mast, tapered yard and a billowing square wool sail.
tube('Mast',[(0,0,.96),(0,0,12.12)],.17,spar,12,r2=.076)
# Yard yaw keeps the rig readable and is a review pose, not an animation.
yaw=math.radians(14)
def sailpoint(u,v):
 width=10.2+.8*v
 y=(u-.5)*width;x=.32+1.05*sin(pi*u)*sin(pi*v)
 z=11.56-7.55*v-.13*sin(pi*u)
 return (x*cos(yaw)-y*sin(yaw),x*sin(yaw)+y*cos(yaw),z)
yard_a=sailpoint(0,0);yard_b=sailpoint(1,0)
tube('Yard',[(yard_a[0],yard_a[1],11.72),(0,0,11.76),(yard_b[0],yard_b[1],11.72)],.095,spar,10)
sail_objs=[]
for panel in range(12):
 verts=[];faces=[]
 for j in range(17):
  for k in range(4):verts.append(sailpoint((panel+k/3)/12,j/16))
 for j in range(16):
  for k in range(3):a=j*4+k;faces.append((a,a+1,a+5,a+4))
 ob=mesh('Sail / sewn wool panel %02d'%(panel+1),verts,faces,cloth[panel]);sail_objs.append(ob)
 sol=ob.modifiers.new('Wool thickness','SOLIDIFY');sol.thickness=.009
 for poly in ob.data.polygons:poly.use_smooth=True
for i in range(13):
 ob=tube('Sail panel seam %02d'%i,[sailpoint(i/12,j/24) for j in range(25)],.011,cloth_seam,5);sail_objs.append(ob)
for v in [0,1]:
 ob=tube('Sail bolt rope',[sailpoint(i/40,v) for i in range(41)],.026,rope,6);sail_objs.append(ob)
for u in [0,1]:
 ob=tube('Sail leech rope',[sailpoint(u,i/30) for i in range(31)],.025,rope,6);sail_objs.append(ob)
# Short reef-point ties, stitched to the sail rather than decorative paint.
for v in [.58,.79]:
 for i in range(1,12):
  p=Vector(sailpoint(i/12,v));ob=line('Sail reef tie',p,p+Vector((.055,.035,-.23)),.014,rope_dark,.035);sail_objs.append(ob)
for u in [i/12 for i in range(13)]:
 p=sailpoint(u,0);tube('Sail yard lashing',[(p[0],p[1],p[2]),(p[0]+.11,p[1],11.85),(p[0]-.08,p[1],11.87),(p[0],p[1],p[2])],.018,rope,6)
line('Forestay',(0,0,11.96),(7.13,0,2.65),.037,rope_dark,.07)
line('Backstay',(0,0,11.89),(-6.78,0,2.41),.030,rope_dark,.07)
for side in [-1,1]:
 for i,x in enumerate([-2.9,-1.9,-.9]):
  low=(x,side*2.15,2.15);line('Shroud',(0,side*.075,11.15-i*.11),low,.024,rope_dark,.065)
  tube('Shroud lashing',[(x-.12,side*2.2,2.12),(x,side*2.27,1.86),(x+.12,side*2.2,2.12)],.025,rope,6)
 line('Yard brace',yard_a if side==-1 else yard_b,(-5.6,side*1.35,2.05),.021,rope,.11)
 corner=sailpoint(0 if side==-1 else 1,1)
 ob=line('Sail sheet',corner,(-5.20,side*1.35,2.05),.029,rope,.14);sail_objs.append(ob)
line('Halyard',(0,.14,11.76),(-.40,.24,1.73),.025,rope,.02)
# Hand-shaped belaying pins and rope coils on working decks.
for x,y in [(-4.6,-1),(-4.6,1),(4.6,-1),(4.6,1)]:
 tube('Belaying pin',[(x,y,1.92),(x,y,2.27)],.042,oak,8)
 for j in range(4):hoop('Coiled running line',(x+.28,y,1.925+j*.013),.17+j*.017,.22+j*.017,rope,.016)

# Starboard steering oar and inboard tiller, separate objects for future rigging.
shaft=[(-5.92,-1.77,2.59),(-6.28,-2.01,1.15),(-6.53,-2.20,-.38)]
tube('Steering oar / stock',shaft,.072,oak,10)
blade=[(-6.46,-2.19,.78),(-6.94,-2.24,.65),(-7.11,-2.30,-.37),(-6.44,-2.30,-.50),(-6.28,-2.20,.36)]
verts=blade+[(x,y+.08,z) for x,y,z in blade];faces=[tuple(range(4,-1,-1)),tuple(range(5,10))]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)]
mesh('Steering oar / blade',verts,faces,oak,bevel=.025)
tube('Steering tiller',[shaft[0],(-5.60,-.62,2.52),(-5.18,-.25,2.45)],.052,oak,8)
line('Steering oar upper lash',(-5.85,-1.5,2.15),(-6.11,-1.88,2.11),.046,rope)
line('Steering oar lower lash',(-6.08,-1.44,1.48),(-6.28,-2.04,1.33),.042,rope)
# Four manoeuvring oars stowed, with no fighting-ship rows of shields.
for side in [-1,1]:
 for i in range(2):
  y=side*(1.10+i*.18);z=2.17+i*.06
  tube('Stowed harbour oar / shaft',[(-6.55,y,z),(-2.38,y,z)],.032,spar,8,r2=.024)
  mesh('Stowed harbour oar / blade',[(-6.7,y-.12,z),(-5.73,y-.14,z),(-5.55,y,z),(-5.73,y+.14,z),(-6.7,y+.12,z)],[(0,1,2,3,4)],endgrain)

# Illustrative removable cargo: coopered barrels and rope-bound cloth bales.
def barrel(cx,cy,cz,r=.36,h=.83):
 for j in range(12):
  a=2*pi*j/12+.006;b=2*pi*(j+1)/12-.006;v=[]
  for zz,rr in [(0,.86*r),(.12*h,.95*r),(.5*h,r),(.88*h,.95*r),(h,.86*r)]:
   v.extend([(cx+rr*cos(a),cy+rr*sin(a),cz+zz),(cx+rr*cos(b),cy+rr*sin(b),cz+zz)])
  mesh('Cargo / barrel stave',v,[(i,i+1,i+3,i+2) for i in range(0,8,2)],pine[j%7])
 for z in [.12*h,.85*h]:hoop('Cargo / barrel hoop',(cx,cy,cz+z),r*.95,r*.95,iron,.023)
 v=[(cx+r*.86*cos(i*pi/8),cy+r*.86*sin(i*pi/8),cz+h) for i in range(16)]
 mesh('Cargo / barrel lid',v,[tuple(range(16))],endgrain)
 for dx in [-.14,0,.14]:line('Cargo / lid joint',(cx+dx,cy-r*.68,cz+h+.002),(cx+dx,cy+r*.68,cz+h+.002),.003,oak)
for c in [(1.4,-.65,.72),(2.22,-.62,.72),(1.74,.20,.72),(2.60,.22,.72),(-1.45,.57,.72),(-2.22,.57,.72)]:barrel(*c)
for x,y,z in [(-1.65,-.78,.72),(-2.55,-.78,.72),(2.75,1.05,.72)]:
 ob=box('Cargo / wrapped trade bale',(x,y,z+.30),(.78,.64,.6),bale_mat,.11)
 for dx in [-.23,.23]:
  tube('Cargo / bale binding',[(x+dx,y-.33,z+.08),(x+dx,y-.33,z+.56),(x+dx,y+.33,z+.56),(x+dx,y+.33,z+.08),(x+dx,y-.33,z+.08)],.018,rope_dark,6)

# Set useful origins for later animation, without claiming a gameplay rig.
for ob in ship.objects:
 ob['standalone_review_only']=True
 ob['source']='Original generated geometry; historical reference: Viking Ship Museum, Skuldelev 1 / Ottar'
# Simple mesh validation and exact export inventory.
mesh_objects=[o for o in ship.objects if o.type=='MESH']
for ob in mesh_objects:
 assert all(math.isfinite(c) for v in ob.data.vertices for c in v.co),ob.name
 for poly in ob.data.polygons:assert len(poly.vertices)>=3,ob.name
report={'status':'Standalone visual study. Not imported, registered, packaged or installed in Helmsman.',
 'basis':'Skuldelev 1 / Ottar; interpreted hull lines, framing, rigging pose and cargo',
 'units':'metres; Blender X forward, Y port, Z up','nominal_hull_length':15.84,'nominal_beam':4.8,
 'mesh_objects':len(mesh_objects),'vertices':sum(len(o.data.vertices) for o in mesh_objects),
 'triangles_before_bevel':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in mesh_objects),
 'limitations':['Not an exact archaeological reconstruction','No game colliders, LODs, sail animation or network components','GLB uses base material colours; Blender has procedural grain bump']}
(ROOT/'model-info.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report),flush=True)
# Export the ship alone, before adding the studio.
bpy.ops.object.select_all(action='DESELECT')
for o in ship.objects:o.select_set(True)
bpy.context.view_layer.objects.active=mesh_objects[0]
bpy.ops.export_scene.gltf(filepath=str(ROOT/'merchant-knarr.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True)
print('GLB saved',flush=True)

# Studio and product cameras. Only actual model geometry is rendered.
ground=box('Studio floor',(0,0,-.55),(200,200,.08),mat('Studio slate',(.028,.048,.058),.95),0);move(ground,studio)
def light(name,loc,power,size,color):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
 obj=bpy.data.objects.new(name,data);studio.objects.link(obj);obj.location=loc;obj.rotation_euler=(Vector((0,0,4))-obj.location).to_track_quat('-Z','Y').to_euler()
light('Warm broad key',(5,-11,20),3200,12,(1,.86,.69))
light('Cool port fill',(-5,10,12),2400,10,(.68,.82,1))
light('Bow rim',(12,4,10),1800,8,(1,.88,.68))
light('Front softbox',(4,-16,7),1300,10,(1,.95,.88))
data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);studio.objects.link(camera);scene.camera=camera;data.type='ORTHO'
def camera_at(loc,target,scale):
 camera.location=loc;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=scale
camera_at((23,-31,21),(0,0,5.25),22)
bpy.ops.object.select_all(action='DESELECT')
# Pack any resources, save the full editable scene before rendering.
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'merchant-knarr.blend'))
scene.render.filepath=str(ROOT/'merchant-knarr-hero.png');bpy.ops.render.render(write_still=True)
print('Hero render saved',flush=True)
for ob in sail_objs:ob.hide_render=True
camera_at((13,-20,20),(0,0,1.50),18.8)
scene.render.resolution_x=1600;scene.render.resolution_y=1000
scene.render.filepath=str(ROOT/'merchant-knarr-deck.png');bpy.ops.render.render(write_still=True)
print('Deck inspection saved; full sail remains in saved model',flush=True)
