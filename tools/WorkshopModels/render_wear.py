"""Render only the revised workshop set, including an equal-scale lineup."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import bpy,runpy,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];OUT=R/'design/workshop-wear-v1';OUT.mkdir(exist_ok=True)
e=runpy.run_path(str(R/'tools/HarborModels/render_runtime.py'))
def surfaces(obs):
 # Local installed-game reference textures, preview only; never packaged with the mod.
 for m in {m for o,_,_ in obs for m in o.data.materials}:
  n=m.name.lower();nodes=m.node_tree.nodes;links=m.node_tree.links;bs=nodes.get('Principled BSDF')
  plain=any(w in n for w in ['iron','bronze','clay','stoneware','pigment','fish','resin','leather'])
  if plain:continue
  fabric=any(w in n for w in ['sail','cloth','wrapping'])
  color=tuple(bs.inputs['Base Color'].default_value)
  tint=(1,.96,.84,1) if fabric else (1,.98,.94,1)
  if n.startswith('workshop '):tint=tuple(.35+.65*max(0,c)**(1/2.2) for c in color[:3])+(1,)
  if 'tarred standing' in n:tint=(.42,.40,.36,1)
  texture=nodes.new('ShaderNodeTexImage');texture.image=bpy.data.images.load(str(R/'.build/native'/('sail_white.png' if fabric else 'ship_wood.png')),check_existing=True);texture.interpolation='Closest'
  uv=nodes.new('ShaderNodeTexCoord');mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(.3,.3,1) if fabric else (.35,.5,1)
  links.new(uv.outputs['UV'],mapping.inputs[0]);links.new(mapping.outputs['Vector'],texture.inputs['Vector'])
  mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=tint;links.new(texture.outputs['Color'],mix.inputs[1]);links.new(mix.outputs[0],bs.inputs['Base Color'])
keys=['workbench','tool-rack','caulking-station','rigging-rack','paint-stand']
for key in keys:
 scene,obs,_=e['setup'](R/'assets/workshop'/f'{key}.bin.gz')
 surfaces(obs)
 scene.render.resolution_x=1100;scene.render.resolution_y=850;scene.cycles.samples=12
 scene.render.filepath=str(OUT/(key+'.png'));bpy.ops.render.render(write_still=True)
# Begin with main bench then put upgrades beside it with their feet at the same Z.
scene,objects,_=e['setup'](R/'assets/workshop/workbench.bin.gz')
surfaces(objects)
right=1.25
for key in keys[1:]:
 obs,_=e['load'](R/'assets/workshop'/f'{key}.bin.gz')
 surfaces(obs)
 xs=[v.co.x for o,_,_ in obs for v in o.data.vertices];dx=right+.25-min(xs)
 for o,_,_ in obs:o.location.x+=dx
 right=max(xs)+dx
center=Vector(((right-1.25)/2,0,1.05))
cam=scene.camera;cam.location=center+Vector((2.8,-15,7));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=(right+1.25)*1.12
for o in scene.objects:
 if o.type=='LIGHT':o.location.x+=center.x;o.data.energy*=3;o.data.size*=2
scene.render.resolution_x=1800;scene.render.resolution_y=720;scene.cycles.samples=16;scene.render.filepath=str(OUT/'lineup.png');bpy.ops.render.render(write_still=True)
