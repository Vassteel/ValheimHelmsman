"""Inspect each optimized runtime asset in a consistent deck view."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,runpy,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];D=R/'design/fleet-detail-optimization'
e=runpy.run_path(str(R/'tools/HarborModels/render_runtime.py'))
for row in json.loads((D/'optimization.json').read_text()):
 key=row['asset'].removesuffix('.bin.gz');path=D/key/row['asset'];scene,obs,_=e['setup'](path,True)
 for o,tag,_ in obs:
  if tag in ('sail','mast','rigging'):o.hide_render=True
 points=[o.matrix_world@Vector(v) for o,tag,_ in obs if tag not in ('sail','mast','rigging') for v in o.bound_box]
 lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));target=(lo+hi)/2
 cam=scene.camera;cam.location=target+Vector((5,-8,12));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=max(hi-lo)*1.04
 scene.render.resolution_x=800;scene.render.resolution_y=800;scene.cycles.samples=8;scene.render.filepath=str(D/key/'review.png');bpy.ops.render.render(write_still=True)
