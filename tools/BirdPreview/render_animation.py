"""Render the exported production rig; these are Blender previews, not game captures."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Matrix,Vector
R=Path(__file__).resolve().parents[2];D=R/'design/puffin-animation-v1';data=json.loads((D/'animation.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True;scene.render.threads=6
scene.render.resolution_x=960;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.16,.2,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.55
scene.view_settings.view_transform='AgX'
C=Matrix(((1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
texture=bpy.data.images.load(str(R/'assets/birds/detail.png'));mats={};objects=[]
for part in data['parts']:
 mat=mats.get(part['material'])
 if mat is None:
  mat=bpy.data.materials.new(part['material']);mat.use_nodes=True;p=mat.node_tree.nodes['Principled BSDF'];p.inputs['Roughness'].default_value=.88;p.inputs['Base Color'].default_value=(*part['color'],1)
  if 'tile -1' not in part['material']:
   tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=texture;mix=mat.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.8;mix.inputs[1].default_value=(*part['color'],1);mat.node_tree.links.new(tex.outputs['Color'],mix.inputs[2]);mat.node_tree.links.new(mix.outputs[0],p.inputs['Base Color'])
  mats[part['material']]=mat
 mesh=bpy.data.meshes.new(part['name']);tri=part['triangles'];mesh.from_pydata([(v[0],-v[2],v[1]) for v in part['vertices']],[],[tri[i:i+3] for i in range(0,len(tri),3)]);mesh.update()
 uv=mesh.uv_layers.new()
 for loop in mesh.loops:uv.data[loop.index].uv=part['uv'][loop.vertex_index]
 for p in mesh.polygons:p.use_smooth=True
 mesh.normals_split_custom_set_from_vertices([(v[0],-v[2],v[1]) for v in part['normals']])
 obj=bpy.data.objects.new(part['name'],mesh);scene.collection.objects.link(obj);obj.data.materials.append(mat);objects.append(obj)
floor=bpy.data.materials.new('Slate');floor.diffuse_color=(.045,.065,.074,1);floor.use_nodes=True;floor.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.045,.065,.074,1)
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(floor)
for name,loc,power,size in [('Key',(2,-3,4),350,3),('Fill',(-3,-1,2),220,3),('Rim',(1,3,3),400,2)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Production rig camera');cam=bpy.data.objects.new('Production rig camera',d);scene.collection.objects.link(cam);scene.camera=cam;cam.location=(1.6,-2.5,1.3);target=Vector((0,0,.58));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=1.7
frames=D/'frames';frames.mkdir(exist_ok=True)
def apply(frame):
 for obj,p in zip(objects,frame['parts']):
  mat=Matrix([p['matrix'][i:i+4] for i in range(0,16,4)]).transposed();obj.matrix_world=C@mat@C.inverted();obj.hide_render=not p['visible']
if '--station' in sys.argv:
 path=R/'design/workshop-supplies-v1/workbench/workbench.blend'
 with bpy.data.libraries.load(str(path),link=False) as (available,loaded):loaded.collections=[n for n in available.collections if n.startswith('MODEL')]
 for collection in loaded.collections:scene.collection.children.link(collection)
 apply(data['frames'][9])
 anchor=Matrix.Translation((.45,0,1.055))@Matrix.Rotation(-math.pi/2,4,'Z')@Matrix.Scale(.65,4)
 for ob in objects:ob.matrix_world=anchor@ob.matrix_world
 cam.location=(2.8,-3.5,2.6);target=Vector((0,0,1.05));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();d.ortho_scale=3.8
 scene.cycles.samples=24;scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.filepath=str(D/'puffin-at-workbench.png');bpy.ops.render.render(write_still=True)
elif '--still' in sys.argv:
 apply(data['frames'][0]);scene.cycles.samples=32;scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.filepath=str(D/'puffin-refined.png');bpy.ops.render.render(write_still=True)
else:
 scene.render.resolution_x=768;scene.render.resolution_y=576;scene.cycles.samples=8
 for i,frame in enumerate(data['frames']):
  path=frames/f'{i:04}.png'
  if path.exists():continue
  apply(frame);scene.render.filepath=str(path);bpy.ops.render.render(write_still=True)
 print('Rendered',len(data['frames']),'production frames',flush=True)
