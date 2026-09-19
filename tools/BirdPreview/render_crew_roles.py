"""Close-up review of production circus meshes and work poses."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
from pathlib import Path
import bpy,math
from mathutils import Vector
from crew_scene import CrewScene
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[2];D=R/'design/puffin-circus-v1'
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.samples=12;s.cycles.use_denoising=True
s.render.threads_mode='FIXED';s.render.threads=2
s.render.resolution_x=600;s.render.resolution_y=600;s.render.resolution_percentage=100
s.world=bpy.data.worlds.new('Soft daylight');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs[0].default_value=(.35,.43,.48,1);s.world.node_tree.nodes['Background'].inputs[1].default_value=.65
s.view_settings.view_transform='AgX'
crew=CrewScene(R,s)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,0));floor=bpy.context.object
mat=bpy.data.materials.new('Ground');mat.diffuse_color=(.13,.18,.19,1);floor.data.materials.append(mat)
for name,loc,power,size in [('Key',(2,1,7),650,5),('Fill',(-4,-2,4),400,4)]:
 light=bpy.data.lights.new(name,'AREA');light.energy=power;light.size=size;o=bpy.data.objects.new(name,light);s.collection.objects.link(o);o.location=loc
cam_data=bpy.data.cameras.new('Role close-up');cam=bpy.data.objects.new('Role close-up',cam_data);s.collection.objects.link(cam);s.camera=cam;cam_data.type='ORTHO';cam_data.ortho_scale=1.8
cards=[]
for i in [0,1,2,4,5]:
 agents=[]
 for j,role in enumerate(crew.data['roles']):
  side=-1 if j%2==0 else 1;z=[5.2,1.8,-1.8][j//2];y=1.212+(10+z)*.075
  agents.append(dict(index=j,position=[side*3.05,y,z],yaw=90 if side<0 else -90,visible=i==j,propsVisible=i==j,parts=role['parts'],beak=role['beak']))
 crew.apply(agents)
 a=agents[i];target=Vector((a['position'][0],-a['position'][2],a['position'][1]+.45));floor.location.z=a['position'][1]-.01
 # Front three-quarter view relative to the inward-facing work position.
 side=-1 if i%2==0 else 1;cam.location=target+Vector((-side*2.3,2.5,1.35));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 for name in ['Key','Fill']:
  light=s.objects[name];light.location=target+Vector(((-side*2 if name=='Key' else side*3),2,5));light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
 p=D/('crew-'+crew.data['roles'][i]['job'].lower()+'.png');s.render.filepath=str(p);bpy.ops.render.render(write_still=True);cards.append((p,crew.data['roles'][i]['job']))
font=ImageFont.truetype('/usr/share/fonts/TTF/DejaVuSans.ttf',24)
sheet=Image.new('RGB',(1800,1280),(25,38,44));d=ImageDraw.Draw(sheet)
for k,(p,label) in enumerate(cards):
 x=k%3*600;y=k//3*640;sheet.paste(Image.open(p).convert('RGB'),(x,y));d.text((x+20,y+606),label,font=font,fill='white')
d.text((1230,735),'Construction crew',font=font,fill='white');d.text((1230,780),'Production meshes and poses',font=font.font_variant(size=18),fill=(180,199,205));d.text((1230,815),'Blender review lighting',font=font.font_variant(size=18),fill=(180,199,205))
sheet.save(D/'crew-roles.jpg',quality=94)
