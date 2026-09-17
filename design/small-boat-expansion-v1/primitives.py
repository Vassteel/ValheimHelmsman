"""Harbour expansion: original standalone review assets. No game assets are imported or registered.
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
ship=bpy.data.collections.new('MODEL - standalone review asset')
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
paint=mat('Weathered iron-oxide sheer stripe',(.25,.075,.035),grain=True)
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
