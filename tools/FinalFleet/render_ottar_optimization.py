"""Compare original and simplified packed meshes, not source geometry."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,runpy
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];D=R/'design/ottar-placement-v1'
e=runpy.run_path(str(R/'tools/HarborModels/render_runtime.py'))
for phase,path in [('before',R/'.build/ottar-placement-before/MercantShip.bin.gz'),('after',D/'MercantShip.bin.gz')]:
 scene,obs,_=e['setup'](path,True)
 for o,tag,_ in obs:
  if tag in ('mast','sail','rigging'):o.hide_render=True
 target=Vector((0,0,.7));cam=scene.camera;cam.location=Vector((7,-9,14));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=14
 scene.render.resolution_x=1100;scene.render.resolution_y=950;scene.cycles.samples=12
 scene.render.filepath=str(D/(phase+'-deck.png'));bpy.ops.render.render(write_still=True)
