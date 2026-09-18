import bpy,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[2]
for kind,L in [('dugout',4.6),('kayak',5.2),('tandem',6.8)]:
 dest=root/'design/paddle-craft-v1'/kind;bpy.ops.wm.open_mainfile(filepath=str(dest/f'{kind}.blend'))
 scene=bpy.context.scene
 def area(name,loc,power,size):
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size
  o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.3))-o.location).to_track_quat('-Z','Y').to_euler()
 area('Key',(2,-5,7),1200,5);area('Fill',(-4,3,5),850,4);area('Rim',(3,4,3),550,3)
 camdata=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',camdata);scene.collection.objects.link(cam);scene.camera=cam
 cam.location=(L*.8,-L,L*1.6);cam.rotation_euler=(Vector((0,0,.3))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=L*1.15
 scene.render.resolution_x=1300;scene.render.resolution_y=850;scene.cycles.samples=24
 scene.render.filepath=str(dest/f'{kind}-review.png');bpy.ops.render.render(write_still=True)
