"""Optimize the deferred fishing studies without registering them in the game."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];source=R/'design/fishing-equipment-v2';dest=R/'design/fishing-equipment-v3';dest.mkdir(exist_ok=True)
rows=[]
def tris(o):return sum(len(p.vertices)-2 for p in o.data.polygons)
for item in json.loads((source/'models.json').read_text()):
 key=item['id'];bpy.ops.wm.open_mainfile(filepath=str(source/key/(key+'.blend')));obs=list(bpy.data.collections['MODEL - standalone review asset'].objects)
 ratio=min(1,19000/item['triangles'])
 for o in obs:
  if o.type!='MESH' or ratio==1 or tris(o)<100:continue
  original=o.data.copy();bpy.context.view_layer.objects.active=o
  mod=o.modifiers.new('Conservative study budget','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
  if len(o.data.vertices)<4:o.data=original
  else:
   # Loose accessories and future motion markers keep their authored placement.
   a=[(min(v.co[i] for v in original.vertices),max(v.co[i] for v in original.vertices)) for i in range(3)]
   if any(abs(min(v.co[i] for v in o.data.vertices)-lo)>max(.006,(hi-lo)*.015) or abs(max(v.co[i] for v in o.data.vertices)-hi)>max(.006,(hi-lo)*.015) for i,(lo,hi) in enumerate(a)):o.data=original
 count=sum(tris(o) for o in obs if o.type=='MESH');out=dest/key;out.mkdir(exist_ok=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(out/(key+'.blend')));bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.ops.export_scene.gltf(filepath=str(out/(key+'.glb')),export_format='GLB',use_selection=True,export_apply=True)
 row=dict(item,before=item['triangles'],triangles=count);rows.append(row);print('OPTIMIZED STUDY',key,item['triangles'],count,flush=True)
(dest/'models.json').write_text(json.dumps(rows,indent=2)+'\n')
# Render revised models with the existing studio setup, bounded to two cores.
s=(R/'tools/FishingModels/render.py').read_text().replace('fishing-equipment-v2','fishing-equipment-v3').replace('scene.render.threads=6','scene.render.threads_mode="FIXED";scene.render.threads=2').replace('scene.cycles.samples=32','scene.cycles.samples=8').replace('resolution_x=1200','resolution_x=800').replace('resolution_y=1000','resolution_y=640')
exec(compile(s,str(R/'tools/FishingModels/render.py'),'exec'),{'__file__':str(R/'tools/FishingModels/render.py')})
