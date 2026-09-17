from pathlib import Path
import bpy,sys
from mathutils import Vector
root=Path(__file__).resolve().parents[2]/'design/fleet-final'
for kind in sys.argv[1:]:
 p=root/kind;bpy.ops.wm.open_mainfile(filepath=str(p/(kind+'.blend')))
 scene=bpy.context.scene;cam=scene.camera;objects=bpy.data.collections['Helmsman '+kind+' final'].objects
 scene.render.resolution_x=1500;scene.render.resolution_y=1100;scene.render.threads=4;scene.cycles.samples=24
 bpy.context.view_layer.update()
 local=[cam.matrix_world.inverted()@(o.matrix_world@v.co) for o in objects for v in o.data.vertices]
 xmin,xmax=min(v.x for v in local),max(v.x for v in local);ymin,ymax=min(v.y for v in local),max(v.y for v in local)
 cam.location+=cam.rotation_euler.to_matrix()@Vector(((xmin+xmax)/2,(ymin+ymax)/2,0))
 cam.data.ortho_scale=max(xmax-xmin,(ymax-ymin)*scene.render.resolution_x/scene.render.resolution_y)*1.09
 scene.render.filepath=str(p/(kind+'-hero.png'));bpy.ops.render.render(write_still=True)
 print('RENDERED',kind,flush=True)
