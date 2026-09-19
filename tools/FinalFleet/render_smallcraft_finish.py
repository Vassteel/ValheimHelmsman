"""Compare packed small craft with native textures and the runtime finish mapping.
Local game textures are review inputs only, never copied into distributable assets.
Lighting is a Blender approximation; final shader acceptance requires Valheim.
"""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,runpy
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];D=R/'design/smallcraft-native-finish';D.mkdir(exist_ok=True)
e=runpy.run_path(str(R/'tools/HarborModels/render_runtime.py'))
for key in ['HelmsmanFinewoodKayak','LittleBoat','HelmsmanCurrach']:
 for phase in ['before','after']:
  after=phase=='after';scene,obs,spec=e['setup'](R/'assets/ships/final'/(key+'.bin.gz'),True)
  updated=set()
  for o,tag,vs in obs:
   if tag in ('sail','mast','rigging','paddle'):o.hide_render=True
   m=o.data.materials[0];n=m.name.lower()
   shell=tag=='hull' and any(w in n for w in ['pine','oak','tarred skin'])
   deck=tag=='fixed' and (n.startswith('worn deck pine') or 'finewood dark inlay' in n)
   if after and (shell or deck):
    for loop in o.data.loops:
     p=vs[loop.vertex_index];o.data.uv_layers.active.data[loop.index].uv=(p[2]/1.8,(p[0] if deck else p[1]+abs(p[0])*.18)/.75)
   if m.name in updated:continue
   updated.add(m.name)
   fabric=any(w in n for w in ['sail','wool','sack','wrapping','hide','fish scales','net cloth']) or (after and 'tarred skin' in n)
   tint=(1,1,1,1)
   if 'finewood dark inlay' in n:tint=(.82,.76,.65,1) if after else (.47,.32,.20,1)
   elif 'tarred skin' in n:tint=(.30,.32,.32,1)
   elif 'iron' in n and 'oxide' not in n:tint=(.30,.31,.30,1)
   elif any(w in n for w in ['sack','knapsack','wrapping']):tint=(.92,.84,.65,1)
   elif 'tarred standing' in n:tint=(.42,.40,.36,1)
   elif any(w in n for w in ['rope','hemp','bast']):tint=(1,.94,.77,1)
   elif any(w in n for w in ['cloth','sail','wool']):tint=(1,.96,.84,1)
   elif any(w in n for w in ['pine','oak','timber']):tint=(1,.98,.94,1)
   nodes=m.node_tree.nodes;links=m.node_tree.links;nodes.clear()
   out=nodes.new('ShaderNodeOutputMaterial');bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.85;links.new(bs.outputs[0],out.inputs[0])
   tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(R/'.build/native'/('sail_white.png' if fabric else 'ship_wood.png')),check_existing=True);tex.interpolation='Closest'
   uv=nodes.new('ShaderNodeTexCoord');scale=nodes.new('ShaderNodeVectorMath');scale.operation='MULTIPLY';scale.inputs[1].default_value=(.3,.3,1) if fabric else (.35,.28 if after else .5,1);links.new(uv.outputs['UV'],scale.inputs[0]);links.new(scale.outputs[0],tex.inputs[0])
   mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=tint;links.new(tex.outputs['Color'],mix.inputs[1]);links.new(mix.outputs[0],bs.inputs['Base Color'])
  points=[Vector(v) for o,_,_ in obs if not o.hide_render for v in o.bound_box];lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)));target=(lo+hi)/2
  cam=scene.camera;cam.location=target+Vector((6,-7,10));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=max(hi-lo)*1.15
  scene.render.resolution_x=900;scene.render.resolution_y=800;scene.cycles.samples=12;scene.render.filepath=str(D/(key+'-'+phase+'.png'));bpy.ops.render.render(write_still=True)
  print('REVIEW',key,phase,flush=True)
