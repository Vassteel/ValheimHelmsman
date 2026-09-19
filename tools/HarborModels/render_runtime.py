"""Review the exact packed runtime meshes, with the C# harbor animation samples."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,gzip,struct,io,json,math,sys
from pathlib import Path
from mathutils import Vector,Matrix
R=Path(__file__).resolve().parents[2];D=R/'design/asset-optimization-v1';H=R/'design/harbor-runtime-v1'
def vector(v):return Vector((v[0],-v[2],v[1]))
def load(path):
 f=io.BytesIO(gzip.decompress(path.read_bytes()))
 def I():return struct.unpack('<i',f.read(4))[0]
 def raw():return f.read(I())
 magic=f.read(4);spec=json.loads(raw()) if magic in (b'HMF1',b'HMW3') else None;mats=[]
 for index in range(I()):
  name=raw().decode();png=raw() if magic==b'HMF1' else None;color=struct.unpack('<4f',f.read(16))
  m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color;bs.inputs['Roughness'].default_value=.85
  if png:
   texture=D/'review-textures'/path.stem;texture.mkdir(exist_ok=True,parents=True);p=texture/(str(index)+'.png');p.write_bytes(png)
   im=bpy.data.images.load(str(p),check_existing=True);node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;node.interpolation='Closest';m.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color'])
  else:
   nodes=m.node_tree.nodes;tex=nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=22;tex.inputs['Detail'].default_value=1
   bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.08;bump.inputs['Distance'].default_value=.006;nodes.links if False else None
   m.node_tree.links.new(tex.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
  mats.append(m)
 objects=[]
 for j in range(I()):
  tag=raw().decode() if magic!=b'HMW1' else 'static';mi=I();n=I();stride=9 if magic==b'HMW3' else 8;vs=[struct.unpack('<'+'f'*stride,f.read(stride*4)) for _ in range(n)];nt=I();ts=struct.unpack('<'+'i'*nt,f.read(nt*4))
  mesh=bpy.data.meshes.new(tag);mesh.from_pydata([vector(v[:3]) for v in vs],[],[ts[i:i+3] for i in range(0,nt,3)]);mesh.update();uv=mesh.uv_layers.new()
  for loop in mesh.loops:uv.data[loop.index].uv=vs[loop.vertex_index][6:8]
  # Geometry review uses Blender normals; shipped explicit normals are separately validated.
  mesh.validate(clean_customdata=False);mesh.update()
  o=bpy.data.objects.new(tag+' '+str(j),mesh);bpy.context.scene.collection.objects.link(o);mesh.materials.append(mats[mi]);objects.append((o,tag,vs))
 return objects,spec

def setup(path,deck=False):
 bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.cycles.use_denoising=True;scene.render.threads_mode='FIXED';scene.render.threads=2;scene.render.use_persistent_data=True;scene.view_settings.view_transform='AgX'
 objects,spec=load(path)
 points=[o.matrix_world@Vector(v) for o,_,_ in objects for v in o.bound_box];lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));center=(hi+lo)/2;extent=hi-lo;factor=max(extent)/2
 scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.23,.3,.36,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.6
 floor=bpy.data.materials.new('Slate');floor.diffuse_color=(.04,.065,.08,1);bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,lo.z-.006));bpy.context.object.data.materials.append(floor)
 for name,loc,power,color in [('Key',(2,-4,6),600,(1,.91,.78)),('Fill',(-4,-1,3),350,(.72,.85,1)),('Rim',(1,4,5),500,(1,1,1))]:
  light=bpy.data.lights.new(name,'AREA');light.energy=power*factor**2;light.size=4*factor;light.color=color;o=bpy.data.objects.new(name,light);scene.collection.objects.link(o);o.location=center+Vector(loc)*factor;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
 camdata=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',camdata);scene.collection.objects.link(cam);scene.camera=cam
 cam.location=center+Vector((4,-6,7 if deck else 4.2))*factor;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=max(extent)*1.55
 scene.render.resolution_x=800;scene.render.resolution_y=640;scene.render.resolution_percentage=100
 if deck:
  for o,tag,_ in objects:
   if tag=='sail':o.hide_render=True
 return scene,objects,spec

def pose(objects,spec,travel):
 rigs={r['tag']:r for r in spec['rigs']};distance=spec['lift']*travel
 for o,tag,vs in objects:
  if tag in rigs:
   r=rigs[tag];pivot=vector([r['pivot'][k] for k in 'xyz']);axis=vector([r['axis'][k] for k in 'xyz']);angle=travel*1.6*r['ratio'] if r['kind']=='roller' else distance*r['ratio'] if r['kind']=='wheel' else 0
   o.matrix_world=Matrix.Translation(pivot+Vector((0,0,distance*r['lift'])))@Matrix.Rotation(angle,4,axis)@Matrix.Translation(-pivot)
  if tag=='running-rope':
   for vertex,v in zip(o.data.vertices,vs):vertex.co=vector(v[:3])+Vector((0,0,distance*v[8]))
   o.data.update()

if '--harbor' in sys.argv:
 for item in json.loads((H/'models.json').read_text()):
  key=item['id'];scene,obs,spec=setup(R/'assets/workshop'/('harbor-'+key+'.bin.gz'));scene.render.filepath=str(H/key/'runtime-review.png');bpy.ops.render.render(write_still=True);print('RENDERED',key,flush=True)
 for key in ['heavy-crane','framing-rack','double-pulley']:
  scene,obs,spec=setup(R/'assets/workshop'/('harbor-'+key+'.bin.gz'))
  for t in [0,4,8,12]:
   sample=next(s for s in json.loads((H/'motion.json').read_text()) if s['seconds']==t);pose(obs,spec,sample['travel']);scene.render.filepath=str(H/key/f'pose-{t:02}.png');bpy.ops.render.render(write_still=True)
if '--existing' in sys.argv:
 for row in json.loads((D/'optimization.json').read_text()):
  name=row['asset']
  only=next((a.split('=',1)[1] for a in sys.argv if a.startswith('--asset=')),None)
  if only and name!=only:continue
  dest=D/name.removesuffix('.bin.gz');dest.mkdir(exist_ok=True)
  for phase,path in [('before',R/'.build/asset-budget-baseline'/name),('after',D/name)]:
   scene,obs,spec=setup(path);scene.render.filepath=str(dest/(phase+'.png'));bpy.ops.render.render(write_still=True)
   if name in ['BigCargoShip.bin.gz','MercantShip.bin.gz','HelmsmanFinewoodKayak.bin.gz']:
    scene,obs,spec=setup(path,True);scene.render.filepath=str(dest/(phase+'-deck.png'));bpy.ops.render.render(write_still=True)
  print('REVIEWED',name,flush=True)
