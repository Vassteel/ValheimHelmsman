"""Render review images of the original workshop assets; game registration is separate."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];OUT=R/'design/fishing-equipment-v2'
models=json.loads((OUT/'models.json').read_text())
for item in models:
 key=item['id']
 if len(sys.argv)>1 and key not in sys.argv[1:]:continue
 bpy.ops.wm.open_mainfile(filepath=str(OUT/key/f'{key}.blend'));scene=bpy.context.scene
 model=bpy.data.collections['MODEL - standalone review asset']
 # Grounding and camera use the actual evaluated model bounds.
 vertices=[o.matrix_world@Vector(c) for o in model.objects if o.type=='MESH' for c in o.bound_box]
 lo=Vector(tuple(min(v[i] for v in vertices) for i in range(3)));hi=Vector(tuple(max(v[i] for v in vertices) for i in range(3)));center=(hi+lo)*.5;extent=hi-lo
 floor=bpy.data.materials.new('Review slate');floor.diffuse_color=(.037,.055,.065,1);floor.use_nodes=True;floor.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.037,.055,.065,1);floor.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.9
 bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,lo.z-.001));bpy.context.object.data.materials.append(floor)
 size=max(extent);factor=size/2
 for name,loc,power in [('Warm key',(2,-4,6),600),('Cool fill',(-4,-1,3),350),('Rim',(1,4,5),500)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power*factor*factor;d.size=4*factor;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=center+Vector(loc)*factor;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
  if name=='Warm key':d.color=(1,.91,.77)
  elif name=='Cool fill':d.color=(.73,.85,1)
 camd=bpy.data.cameras.new('Asset review');cam=bpy.data.objects.new('Asset review',camd);scene.collection.objects.link(cam);scene.camera=cam
 cam.location=center+Vector((4,-6,4.2))*factor
 cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=max(extent.x,extent.y,extent.z)*1.55
 scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.cycles.samples=32;scene.render.threads=6
 scene.render.filepath=str(OUT/key/f'{key}-review.png');bpy.ops.render.render(write_still=True)
 print('RENDERED',key,flush=True)
