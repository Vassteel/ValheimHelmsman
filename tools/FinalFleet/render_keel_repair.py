import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import runpy,bpy
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];env=runpy.run_path(str(R/'tools/HarborModels/render_runtime.py'));D=R/'design/currach-keel-fix';D.mkdir(exist_ok=True)
for phase,path in [('before',R/'.build/currach-keel-before/HelmsmanCurrach.bin.gz'),('after',R/'assets/ships/final/HelmsmanCurrach.bin.gz')]:
 for view in ['interior','underside']:
  scene,obs,spec=env['setup'](path)
  for o in list(scene.objects):
   if o.type=='MESH' and o not in [ob for ob,_,_ in obs]:bpy.data.objects.remove(o,do_unlink=True)
  for o,tag,vs in obs:
   if tag in ('sail','mast','rigging') or (view=='underside' and tag!='hull'):o.hide_render=True
  cam=scene.camera;cam.location=Vector((3,-4,6) if view=='interior' else (3,-4,-4));target=Vector((0,0,.12));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=6.1
  if view=='underside':
   light=bpy.data.lights.new('Keel inspection','AREA');light.energy=500;light.size=5;ob=bpy.data.objects.new('Keel inspection',light);scene.collection.objects.link(ob);ob.location=(0,-1,-4);ob.rotation_euler=(target-ob.location).to_track_quat('-Z','Y').to_euler()
  scene.render.resolution_x=960;scene.render.resolution_y=720;scene.render.filepath=str(D/f'{phase}-{view}.png');bpy.ops.render.render(write_still=True)
