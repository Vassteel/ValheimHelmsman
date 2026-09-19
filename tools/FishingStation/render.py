"""Studio views of the authored workstation with the production pelican as a scale reference."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];OUT=R/'design/pelican-station-v1/fishing-dock'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'fishing-dock.blend'));scene=bpy.context.scene
bird=next(p for p in json.loads((R/'output/fishing-station-pelican.json').read_text()) if p['name']=='Pelican — idle')
preview=bpy.data.collections.new('PREVIEW ONLY - production pelican');scene.collection.children.link(preview)
for part in bird['parts']:
 m=bpy.data.materials.new('Pelican '+part['material']);m.diffuse_color=(*part['color'],1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*part['color'],1);bs.inputs['Roughness'].default_value=.92
 vs=[(.99+.8*v[0],.03-.8*v[2],.97+.8*v[1]) for v in part['vertices']]
 data=bpy.data.meshes.new(part['name']);t=part['triangles'];data.from_pydata(vs,[],[t[i:i+3] for i in range(0,len(t),3)]);data.materials.append(m)
 o=bpy.data.objects.new(part['name'],data);preview.objects.link(o)
floor=bpy.data.materials.new('Review slate');floor.diffuse_color=(.037,.055,.065,1);floor.use_nodes=True;floor.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.037,.055,.065,1);floor.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.9
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008));bpy.context.object.data.materials.append(floor)
center=Vector((0,0,.85))
for name,loc,power,size in [('Warm key',(1,-4,6),650,4),('Cool fill',(-4,-2,3),420,5),('Rim',(2,3,5),850,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
 d.color=(1,.91,.77) if name=='Warm key' else (.78,.88,1) if name=='Cool fill' else (1,1,1)
camd=bpy.data.cameras.new('Dock review');cam=bpy.data.objects.new('Dock review',camd);scene.collection.objects.link(cam);scene.camera=cam;camd.type='ORTHO'
scene.render.resolution_x=1600;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.cycles.samples=40;scene.render.threads=6
for name,eye,target,size in [('review',(4,-7,4),(0,0,.88),4.3),('work-area',(-4,-6,4),(-.25,0,.90),3.8)]:
 cam.location=eye;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();camd.ortho_scale=size
 scene.render.filepath=str(OUT/('fishing-dock-'+name+'.png'));bpy.ops.render.render(write_still=True)
 print('RENDERED',name,flush=True)
