"""Ottar: standalone original knarr study. No game assets are imported or registered.
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
ship=bpy.data.collections.new('OTTAR - standalone review model')
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
for x,y in [(-6.2,-.48),(-6.2,.48),(6.2,-.48),(6.2,.48)]:
 tube('Belaying pin',[(x,y,1.92),(x,y,2.27)],.042,oak,8)
 for j in range(4):hoop('Coiled running line',(x+.28,y,1.925+j*.013),.17+j*.017,.22+j*.017,rope,.016)

# Starboard steering oar and inboard tiller, separate objects for future rigging.
shaft=[(-5.92,-1.77,2.59),(-6.28,-2.01,1.15),(-6.53,-2.20,-.38)]
tube('Steering oar / stock',shaft,.072,oak,10)
blade=[(-6.46,-2.19,.78),(-6.94,-2.24,.65),(-7.11,-2.30,-.37),(-6.44,-2.30,-.50),(-6.28,-2.20,.36)]
verts=blade+[(x,y+.08,z) for x,y,z in blade];faces=[tuple(range(4,-1,-1)),tuple(range(5,10))]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)]
mesh('Steering oar / blade',verts,faces,oak,bevel=.025)
tube('Steering tiller',[shaft[0],(-5.60,-.62,2.93),(-5.18,-.25,3.05)],.052,oak,8)
line('Steering oar upper lash',(-5.85,-1.5,2.15),(-6.11,-1.88,2.11),.046,rope)
line('Steering oar lower lash',(-6.08,-1.44,1.48),(-6.28,-2.04,1.33),.042,rope)
# Four manoeuvring oars stowed, with no fighting-ship rows of shields.
for side in [-1,1]:
 for i in range(2):
  y=side*(1.65+i*.08);z=1.68+i*.06
  tube('Stowed harbour oar / shaft',[(-3.20,y,z),(1.0,y,z)],.032,spar,8,r2=.024)
  mesh('Stowed harbour oar / blade',[(-3.35,y-.12,z),(-2.38,y-.14,z),(-2.20,y,z),(-2.38,y+.14,z),(-3.35,y+.12,z)],[(0,1,2,3,4)],endgrain)

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
# Packed cargo is appearance-only. Large pieces stop around deck height;
# rail bundles are kept outboard of the central passage and seating stations.
hide_mats=[mat('Cargo hide / chestnut',(.28,.135,.063),.97),mat('Cargo hide / pale tan',(.48,.31,.17),.96),mat('Cargo hide / charcoal',(.16,.14,.11),.98)]
sackmat=mat('Cargo sacks / coarse linen',(.53,.43,.28),.99)
bindmat=mat('Cargo lashings / dark hemp',(.22,.17,.105),.98)
for i,x in enumerate([-2.85,-1.95,-1.05,1.05,1.95,2.85]):
 for j,y in enumerate([-.85,0]):barrel(x,y,.72,r=.39,h=1.03+((i+j)%3)*.045)

def hide_bale(x,y,z,w=.82,d=.70,h=.75):
 # Ragged, layered hide edges distinguish these from generic wrapped boxes.
 outline=[(-.50,-.43),(-.22,-.50),(.02,-.43),(.32,-.50),(.50,-.31),(.43,-.06),(.51,.18),(.40,.46),(.10,.43),(-.11,.51),(-.40,.42),(-.48,.17),(-.43,-.08)]
 for layer in range(9):
  zz=z+layer*h/9;verts=[]
  for top in [0,.071]:
   for i,(a,b) in enumerate(outline):verts.append((x+a*w,y+b*d,zz+top+.009*sin(i*1.7+layer)))
  n=len(outline);faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
  mesh('Cargo / bale of hides / layered skins',verts,faces,hide_mats[layer%3],bevel=.008)
 for dx in [-.24*w,.24*w]:
  tube('Cargo / hide bale binding',[(x+dx,y-d*.52,z+.035),(x+dx,y-d*.52,z+h+.01),(x+dx,y+d*.52,z+h+.01),(x+dx,y+d*.52,z+.035),(x+dx,y-d*.52,z+.035)],.025,bindmat,6)
 # A folded flap at the top with a slightly curled edge.
 mesh('Cargo / hide bale folded flap',[(x-w*.36,y-d*.30,z+h+.006),(x+w*.24,y-d*.30,z+h+.006),(x+w*.34,y+d*.25,z+h+.035),(x-w*.26,y+d*.35,z+h+.016)],[(0,1,2,3)],hide_mats[1])
for i,x in enumerate([-3.0,-2.03,-1.06,1.06,2.03,3.0]):hide_bale(x,.91,.76,h=.83+(i%2)*.08)

def sack(x,y,z,r=.29,h=.69,label='Cargo / tied sack'):
 levels=[(0,.63),(.07,.94),(.30,1.07),(.56,.94),(.75,.69),(.85,.34),(.91,.26),(1,.40)]
 verts=[];faces=[];N=14
 for k,(zz,rr) in enumerate(levels):
  for i in range(N):
   a=2*pi*i/N;wrinkle=1+.045*sin(i*2.2+k)
   verts.append((x+r*rr*cos(a)*wrinkle,y+r*.84*rr*sin(a)*wrinkle,z+h*zz+(.017*sin(i*2.5) if k==len(levels)-1 else 0)))
 for k in range(len(levels)-1):
  for i in range(N):a=k*N+i;b=k*N+(i+1)%N;faces.append((a,b,b+N,a+N))
 faces.extend([tuple(reversed(range(N))),tuple((len(levels)-1)*N+i for i in range(N))])
 ob=mesh(label,verts,faces,sackmat)
 for poly in ob.data.polygons:poly.use_smooth=True
 hoop('Cargo / sack neck tie',(x,y,z+h*.865),r*.32,r*.28,bindmat,.018)
 line('Cargo / sack knot tails',(x+r*.31,y,z+h*.865),(x+r*.46,y+.06,z+h*.64),.015,bindmat,.03)
 tube('Cargo / sack stitched seam',[(x+r*rr,y,z+h*zz) for zz,rr in levels[:-1]],.008,bindmat,5)
for i,(x,y) in enumerate([(-3.40,-1.03),(-3.35,.20),(-.65,-1.15),(.65,-1.15),(3.40,-1.03),(3.35,.20),(-.57,.93),(.57,.93)]):
 sack(x,y,1.08,r=.26,h=.62+(i%3)*.035)
# A few soft bags nestle atop barrels, keeping their highest point near the load top.
for x in [-2.40,-1.45,1.45,2.40]:
 hide_bale(x,-1.08,1.10,w=.74,d=.50,h=.65)
 for y in [-.45,-1.08]:sack(x,y,1.78,r=.24,h=.50)
for x in [-3.15,3.15]:sack(x,.92,1.68,r=.22,h=.50)
for x in [-3.50,3.50]:barrel(x,-.36,.76,r=.28,h=1.15)
for x in [-1.50,1.50]:hide_bale(x,.40,1.80,w=.72,d=.55,h=.35)

# Rolled hide/cloth bundles are secured inside the hull near deck height.
def bundle(x,y,z,length=1.48,r=.25,label='Cargo / rolled trade bundle'):
 tube(label,[(x-length/2,y,z),(x+length/2,y,z)],r,bale_mat,12)
 # Concentric end-grain-like rings read as a rolled sheet rather than a timber log.
 for end in [-1,1]:
  xx=x+end*(length/2+.008)
  for rad in [r*.36,r*.68,r*.90]:tube('Cargo / rolled hide edge',[(xx,y+rad*cos(i*pi/12),z+rad*sin(i*pi/12)) for i in range(25)],.010,hide_mats[0],6)
 for dx in [-length*.31,length*.31]:
  tube('Cargo / bundle binding',[(x+dx,y+(r+.015)*cos(i*pi/12),z+(r+.015)*sin(i*pi/12)) for i in range(25)],.025,bindmat,6)
for side in [-1,1]:
 x=2.0;y=side*1.64;z=2.06
 bundle(x,y,z,length=1.62,r=.26,label='Cargo / deck-stowed hide roll')
 for dx in [-.5,.5]:
  tube('Cargo / deck securing lashing',[(x+dx,side*1.35,1.81),(x+dx,side*1.93,1.81),(x+dx,side*1.94,2.16),(x+dx,side*1.64,2.35),(x+dx,side*1.34,2.16),(x+dx,side*1.35,1.81)],.029,rope,6)
 # Spare oars are also lowered and secured inside the hull.
 for xx in [-2.6,-.8]:
  tube('Cargo / inboard oar lashing',[(xx,side*1.52,1.80),(xx,side*1.89,1.80),(xx,side*1.90,2.10),(xx,side*1.52,2.10),(xx,side*1.52,1.80)],.025,rope,6)
bundle(-2.10,.93,1.86,length=1.35,r=.16,label='Cargo / upper hide roll')
bundle(2.07,.94,1.88,length=1.30,r=.16,label='Cargo / upper cloth roll')
for x in [-1.50,1.50]:bundle(x,.40,2.31,length=.74,r=.14,label='Cargo / stacked hide roll')
# Small tied timber packs across the load, not across the helm or passenger stations.
for x in [-3.05,3.05]:
 for i,y in enumerate([-.84,-.63,-.42]):box('Cargo / trade timber',(x,y,1.89),(.78,.18,.14),deckm[i%4],.015)
 for dx in [-.25,.25]:
  tube('Cargo / timber pack rope',[(x+dx,-.96,1.82),(x+dx,-.96,1.98),(x+dx,-.31,1.98),(x+dx,-.31,1.82),(x+dx,-.96,1.82)],.024,rope,6)

# Low working gear makes the end decks lived-in without occupying the seats.
def deck_height(x):return 1.80+.11*(abs(x)-3.98)+.042
gear_before=set(ship.objects)
bundle(5.65,.85,deck_height(5.65)+.19,length=.95,r=.18,label='Cargo / spare sail roll')
hide_bale(5.67,-.81,deck_height(5.67),w=.83,d=.61,h=.38)
barrel(-5.12,1.16,deck_height(-5.12),r=.23,h=.48)
bundle(-6.55,.10,deck_height(-6.55)+.15,length=.58,r=.14,label='Cargo / stern supply roll')
for ob in set(ship.objects)-gear_before:
 ob.name=ob.name.replace('Cargo /','Deck gear /')
 ob['appearance_only']=True;ob['future_collision']='none'
for name,x,y,rx,ry in [('Bow mooring coil',6.62,0,.30,.27),('Stern working coil',-5.48,1.10,.27,.22)]:
 z=deck_height(x)+.025
 for turn in range(5):hoop('Deck gear / '+name,(x,y,z+turn*.023),rx-turn*.022,ry-turn*.020,rope,.022)
 tube('Deck gear / '+name+' loose end',[(x+rx,y,z),(x+rx+.13,y-.12,z),(x+.15,y-.36,z),(x-.15,y-.33,z)],.023,rope,8)
# A mooring tail runs forward to the stem, kept low on the deck.
tube('Deck gear / bow mooring tail',[(6.75,.12,deck_height(6.75)+.03),(7.02,.14,deck_height(7.02)+.03),(7.22,.03,2.18),(7.40,0,2.35)],.025,rope,8)

# Five actual seats: helmsman's cross-bench plus four individual passenger benches.
# Coordinates are guide points only; no Valheim Sit/ShipControlls components exist yet.
attachments=[]
def seat(name,x,y,width,helm=False):
 deckz=1.80+.11*(abs(x)-3.98)+.042;top=deckz+.46
 for dx in [-.16,.16]:
  for dy in [-width*.36,width*.36]:
   box(name+' / leg',(x+dx,y+dy,deckz+.21),(.075,.075,.42),oak,.01)
 for dx in [-.12,.12]:box(name+' / seat board',(x+dx,y,top-.045),(.225,width,.09),deckm[1],.014)
 box(name+' / stretcher',(x,y,deckz+.20),(.07,width*.79,.075),oak,.008)
 attachments.append({'id':'helm_seat' if helm else name.lower().replace(' ','_'),'kind':'seated helm' if helm else 'passenger seat','position':[x,y,top+.025],'facing_blender':[1,0,0]})
seat('Helmsman bench',-5.72,-.24,1.22,True)
for i,(x,y) in enumerate([(-4.46,-.94),(-4.46,.94),(4.62,-.88),(4.62,.88)],1):seat('Passenger seat '+str(i),x,y,.82)
# Mast holdfast: a visible grab bar with wrapped grips, placed above the packed load.
for y in [-.24,.24]:tube('Mast holdfast / stand-off',[(.03,y,2.97),(.35,y,2.97)],.05,oak,8)
tube('Mast holdfast / grab bar',[(.35,-.40,2.97),(.35,.40,2.97)],.045,oak,10)
for y in [-.27,.27]:
 for j in range(7):
  yy=y-.075+j*.025
  tube('Mast holdfast / rope grip',[(.35+.052*cos(i*pi/8),yy,2.97+.052*sin(i*pi/8)) for i in range(17)],.009,rope,6)
attachments.append({'id':'mast_holdfast','kind':'standing holdfast','position':[.76,0,1.88],'hand_target':[.35,0,2.97],'facing_blender':[-1,0,0]})
# Port and starboard midships rope ladders, with a clear inboard landing.
# Native boarding behaviour is specified by guides; not yet a game component.
def ladder_width(z):
 # Follow the outer planking, leaving only clearance for the rope and rung thickness.
 return inside_width_at(0,z)+.11+.065
for side in [-1,1]:
 label='starboard' if side==-1 else 'port'
 for x in [-.30,.30]:
  tube('Boarding ladder / '+label+' side rope',[(x,side*2.20,2.12),(x,side*2.24,2.30),(x,side*2.36,2.37),(x,side*2.44,2.30)]+[(x,side*ladder_width(2.24-i*.026),2.24-i*.026) for i in range(83)],.030,rope,8)
  tube('Boarding ladder / '+label+' gunwale tie',[(x,side*2.22,2.39),(x+.06,side*2.37,2.23),(x,side*2.19,2.13),(x-.06,side*2.09,2.29),(x,side*2.22,2.39)],.025,bindmat,6)
 for i in range(9):
  z=2.24-i*.26;yy=side*ladder_width(z)
  tube('Boarding ladder / '+label+' rung %02d'%(i+1),[(-.39,yy,z),(.39,yy,z)],.040,oak,8)
  for x in [-.30,.30]:
   tube('Boarding ladder / '+label+' rung knot',[(x+.045*cos(j*pi/6),yy-.015,z+.045*sin(j*pi/6)) for j in range(13)],.014,bindmat,6)
 for x in [-.23,0,.23]:box('Boarding ladder / '+label+' landing plank',(x,side*1.73,1.835),(.222,.90,.09),deckm[2],.009)
 attachments.append({'id':'boarding_'+label,'kind':'boarding ladder','position':[0,side*(ladder_width(.66)+.10),.66],
  'exit_position':[0,side*1.58,1.88],'facing_blender':[0,-side,0],
  'landing_collision_guide':{'center':[0,side*1.73,1.83],'size':[.72,.90,.10]},
  'status':'Interaction and landing guides only; add native boarding behaviour during integration'})
(ROOT/'attachment-points.json').write_text(json.dumps({'status':'Visual guides only; game interaction and animation alignment pending','coordinates':'Blender local metres: X bow, Y port, Z up','points':attachments},indent=2)+'\n')
for ob in ship.objects:
 if ob.name.startswith('Cargo /'):ob['appearance_only']=True;ob['future_collision']='none'
# Smooth support over the loaded hold, independent of all individual cargo props.
# The slightly uneven visible load requires a later foot-contact/playtest pass.
walking={'status':'Guide only; walking on visual cargo requires game integration and foot-contact testing',
 'cargo_collision':'none','coordinates':'Blender local metres',
 'hold_walk_surface':{'type':'BoxCollider','center':[0,0,1.83],'size':[7.90,3.40,.10],'top':1.88},
 'review_note':'Dense visual cargo now rises above the old support plane; this plane is provisional, not approved movement geometry. Resolve foot contact and accessibility during future integration. Cargo remains appearance-only.'}
(ROOT/'walking-surface.json').write_text(json.dumps(walking,indent=2)+'\n')
guides=bpy.data.collections.new('INTERACTION GUIDES - not included in GLB');scene.collection.children.link(guides)
ob=box('GUIDE / cargo walking support',(0,0,1.83),(7.90,3.40,.10),oak,0);move(ob,guides);ob.display_type='WIRE';ob.hide_render=True;ob.hide_set(True)
for anchor in attachments:
 ob=bpy.data.objects.new('GUIDE / '+anchor['id'],None);guides.objects.link(ob);ob.location=anchor['position'];ob.empty_display_type='ARROWS';ob.empty_display_size=.25;ob.hide_render=True

# Set useful origins for later animation, without claiming a gameplay rig.
for ob in ship.objects:
 ob['standalone_review_only']=True
 ob['source']='Original generated geometry; historical reference: Viking Ship Museum, Skuldelev 1 / Ottar'
# Simple mesh validation and exact export inventory.
mesh_objects=[o for o in ship.objects if o.type=='MESH']
for ob in mesh_objects:
 assert all(math.isfinite(c) for v in ob.data.vertices for c in v.co),ob.name
 for poly in ob.data.polygons:assert len(poly.vertices)>=3,ob.name
report={'name':'Ottar','status':'Standalone visual study. Not imported, registered, packaged or installed in Helmsman.',
 'basis':'Skuldelev 1 / Ottar; interpreted hull lines, framing, rigging pose and cargo',
 'units':'metres; Blender X forward, Y port, Z up','nominal_hull_length':15.84,'nominal_beam':4.8,'revision':5,'cargo_barrels':14,'cargo_hide_bales':12,'cargo_sacks':18,'cargo_rolled_bundles':6,'cargo_timber_bundles':2,'grating_panels':0,'helm_benches':1,'passenger_seats':4,'mast_holdfasts':1,'boarding_ladders':2,
 'mesh_objects':len(mesh_objects),'vertices':sum(len(o.data.vertices) for o in mesh_objects),
 'triangles_before_bevel':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in mesh_objects),
 'limitations':['Not an exact archaeological reconstruction','Walking and attachment guides prepared; no Unity collision, seated animations or functional interactions','GLB uses base material colours; Blender has procedural grain bump']}
(ROOT/'model-info.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report),flush=True)
# Export the ship alone, before adding the studio.
bpy.ops.object.select_all(action='DESELECT')
for o in ship.objects:o.select_set(True)
bpy.context.view_layer.objects.active=mesh_objects[0]
bpy.ops.export_scene.gltf(filepath=str(ROOT/'ottar.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True)
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
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ottar.blend'))
scene.render.filepath=str(ROOT/'ottar-hero.png');bpy.ops.render.render(write_still=True)
print('Hero render saved',flush=True)
for ob in sail_objs:ob.hide_render=True
camera_at((13,-20,20),(0,0,1.50),18.8)
scene.render.resolution_x=1600;scene.render.resolution_y=1000
scene.render.filepath=str(ROOT/'ottar-deck.png');bpy.ops.render.render(write_still=True)
print('Deck inspection saved; full sail remains in saved model',flush=True)

# Close view of the starboard ladder and its inboard landing.
camera_at((5,-18,9),(0,-1.45,1.35),8.4)
scene.render.resolution_x=1200;scene.render.resolution_y=1000
scene.render.filepath=str(ROOT/'ottar-boarding.png');bpy.ops.render.render(write_still=True)
print('Boarding ladder inspection saved',flush=True)
