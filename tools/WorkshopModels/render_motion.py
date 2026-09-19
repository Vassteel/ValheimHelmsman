"""Review animation from the packed runtime meshes and exported C# timeline."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,struct,gzip,json,io,math,sys,os
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];D=R/os.environ.get('SLIPWAY_REVIEW_DIR','design/slipway-animation-v1');D.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=4;scene.render.use_persistent_data=True;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=2
scene.render.resolution_x=640;scene.render.resolution_y=426;scene.render.resolution_percentage=100;scene.world=bpy.data.worlds.new('Harbour light');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.3,.4,.5,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.7
scene.view_settings.view_transform='AgX'
def material(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=color;p.inputs['Roughness'].default_value=.85;return m
def load(path,ship=False):
 f=io.BytesIO(gzip.decompress(path.read_bytes()));magic=f.read(4)
 def I():return struct.unpack('<i',f.read(4))[0]
 def text():return f.read(I()).decode()
 spec=json.loads(text()) if ship else None;mats=[]
 for _ in range(I()):
  name=text()
  if ship:f.read(I())
  color=struct.unpack('<4f',f.read(16));mats.append(material(name,color))
 out=[]
 for i in range(I()):
  group=text() if ship or magic==b'HMW2' else 'static';mi=I();n=I();vs=[struct.unpack('<8f',f.read(32)) for _ in range(n)];nt=I();ts=struct.unpack('<'+'i'*nt,f.read(nt*4));out.append((group,mats[mi],vs,ts))
 return out,spec
def obj(group,mat,vs,ts,parent=None):
 mesh=bpy.data.meshes.new(group);mesh.from_pydata([(v[0],-v[2],v[1]) for v in vs],[],[ts[i:i+3] for i in range(0,len(ts),3)]);mesh.update();o=bpy.data.objects.new(group,mesh);scene.collection.objects.link(o);o.data.materials.append(mat);o.parent=parent
 # Slight procedural grain is a preview approximation, not the native game shader.
 return o
def empty(name):
 o=bpy.data.objects.new(name,None);scene.collection.objects.link(o);return o
cradle=empty('Cradle');capstan=empty('Capstan');capstan.location=(0,-11,3.175)
parts,_=load(R/'assets/workshop/slipway.bin.gz')
for group,mat,vs,ts in parts:
 if group=='haul':continue
 o=obj(group,mat,vs,ts)
 if group=='cradle':o.parent=cradle
 if group=='capstan':o.parent=capstan;o.location=-capstan.location
ropeMat=material('Haul line',(.38,.28,.16,1));curve=bpy.data.curves.new('Haul line','CURVE');curve.dimensions='3D';curve.bevel_depth=.018;curve.bevel_resolution=2;spline=curve.splines.new('POLY');spline.points.add(3);line=bpy.data.objects.new('Haul line',curve);scene.collection.objects.link(line);curve.materials.append(ropeMat)
stage_starts=json.loads((R/'design/puffin-animation-v1/construction-stages.json').read_text())
parts,spec=load(R/'assets/ships/final/MercantShip.bin.gz',True);ship=empty('Ship assembly');built=[]
ghost=bpy.data.materials.new('Construction outline');ghost.use_nodes=True;nodes=ghost.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');mix=nodes.new('ShaderNodeMixShader');mix.inputs[0].default_value=.045;transparent=nodes.new('ShaderNodeBsdfTransparent');em=nodes.new('ShaderNodeEmission');em.inputs[0].default_value=(.32,.72,.85,1);ghost.node_tree.links.new(transparent.outputs[0],mix.inputs[1]);ghost.node_tree.links.new(em.outputs[0],mix.inputs[2]);ghost.node_tree.links.new(mix.outputs[0],out.inputs[0])
bottom=min(v[1] for group,mat,vs,ts in parts if group=='hull' for v in vs);stage=Vector((0,-2,2.13-bottom));end=Vector((0,12+spec['length']*.5,-spec['waterline']))
for group,mat,vs,ts in parts:
 if group=='paddle':continue
 if group=='hull':
  lo=min(v[1] for v in vs);hi=max(v[1] for v in vs);bins=[[] for _ in range(6)]
  for j in range(0,len(ts),3):
   height=sum(vs[k][1] for k in ts[j:j+3])/3;band=min(5,int((height-lo)/max(.0001,hi-lo)*6));bins[band].extend(ts[j:j+3])
  for band,faces in enumerate(bins):
   if faces:built.append((obj(group,mat,vs,faces,ship),mat,.06+band*.082))
 else:
  start=stage_starts.get(group,.58)
  built.append((obj(group,mat,vs,ts,ship),mat,start))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,0));water=bpy.context.object;water.name='Review sea';water.data.materials.append(material('Sea',(.035,.16,.18,1)))
# Shoreward portion of the launch platform remains above a low bank.
bpy.ops.mesh.primitive_cube_add(size=1,location=(0,-19,-1.4));bank=bpy.context.object;bank.scale=(35,24,3);bank.data.materials.append(material('Shore',(.12,.15,.12,1)))
for name,loc,power,size in [('Daylight',(-10,-8,25),9500,20),('Fill',(16,4,18),7000,18)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,4,2))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Slipway review');cam=bpy.data.objects.new('Slipway review',d);scene.collection.objects.link(cam);scene.camera=cam;cam.location=(28,34,25);cam.rotation_euler=(Vector((0,3,2))-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=43
crew=None
if '--circus' in sys.argv:
 sys.path.insert(0,str(R/'tools/BirdPreview'))
 from crew_scene import CrewScene
 crew=CrewScene(R,scene)
frames=D/'frames';frames.mkdir(exist_ok=True);timeline=json.loads((R/'design/puffin-animation-v1/slipway-timeline.json').read_text())
if crew:timeline=crew.data['frames']
for i,p in enumerate(timeline):
 if '--still' in sys.argv and i!=38:continue
 path=D/'slipway-construction.png' if '--still' in sys.argv else frames/f'{i:04}.png'
 if path.exists() and '--still' not in sys.argv:continue
 if crew:crew.apply(p['agents'])
 ship.location=stage.lerp(end,p['travel']);ship.rotation_euler=(math.radians(5.71)*(1-p['level']),0,math.pi)
 cradle.location=(0,p['cradle'],-p['cradle']*.1);capstan.rotation_euler.z=math.radians(p['cradle']*160)
 endrope=Vector((0,-8.2,2.665))+cradle.location;start=Vector((.28,-10.4,2.78));points=[Vector((.215,-11,3.175)),start,start.lerp(endrope,.5)-Vector((0,0,.08)),endrope]
 for dest,v in zip(spline.points,points):dest.co=(*v,1)
 for o,mat,at in built:o.data.materials[0]=mat if p['progress']>=at else ghost
 scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
print('Rendered slipway timeline',flush=True)
