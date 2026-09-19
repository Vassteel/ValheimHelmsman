import bpy,json,math,sys,numpy as np
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
R=Path(__file__).resolve().parents[2];out=R/'output/kayak-paddling-blade-fix-preview';out.mkdir(exist_ok=True,parents=True)
bpy.ops.wm.open_mainfile(filepath=str(R/'design/paddle-craft-v1/kayak/kayak.blend'))
scene=bpy.context.scene
D=json.loads((R/'.build/preview-player.json').read_text());nodes=D['nodes'];names={n['name']:i for i,n in enumerate(nodes)};ids={n['id']:i for i,n in enumerate(nodes)}
parents=[n['parent'] for n in nodes];lp=[];lr=[];sc=[];wm=[]
def reset():
 global lp,lr,sc,wm
 lp=[Vector(n['pos']) for n in nodes];lp[0]=Vector((0,0,0));lr=[Quaternion(n['rot']) for n in nodes];sc=[Vector(n['scale']) for n in nodes];update()
def update():
 global wm
 wm=[]
 for i in range(len(nodes)):
  local=Matrix.LocRotScale(lp[i],lr[i],sc[i]);wm.append(wm[parents[i]]@local if parents[i]>=0 else local)
def pos(i):return wm[i].translation.copy()
def rot(i):return wm[i].to_quaternion()
def rotate(i,q):
 lr[i]=(rot(parents[i]).inverted()@q) if parents[i]>=0 else q;update()
def move(i,p):
 lp[i]=(wm[parents[i]].inverted()@p) if parents[i]>=0 else p;update()
def fromto(a,b):return a.normalized().rotation_difference(b.normalized())
def solve(a,b,e,target,pole):
 a,b,e=[names[k] for k in (a,b,e)];origin=pos(a);l1=(pos(b)-origin).length;l2=(pos(e)-pos(b)).length
 delta=target-origin;d=max(abs(l1-l2)+.001,min(l1+l2-.001,delta.length));axis=delta.normalized();v=pole-origin;bend=(v-axis*v.dot(axis)).normalized();along=(l1*l1+d*d-l2*l2)/(2*d);height=math.sqrt(max(0,l1*l1-along*along));elbow=origin+axis*along+bend*height
 rotate(a,fromto(pos(b)-origin,elbow-origin)@rot(a));rotate(b,fromto(pos(e)-pos(b),origin+axis*d-pos(b))@rot(b))
def basis(f,u):
 f=f.normalized();r=u.cross(f).normalized();u=f.cross(r);return Matrix((r,u,f)).transposed().to_quaternion()
def grip(hand,shaft,left):
 h=names[hand];mid=names[hand+'Middle1'];ix=names[hand+'Index1'];pk=names[hand+'Pinky1'];along=lp[mid].normalized();across=(lp[ix]-lp[pk]).normalized();f=Vector((0,0,1));f=(f-shaft*f.dot(shaft)).normalized();side=shaft if left else -shaft
 q=basis(f,side.cross(f))@basis(along,across.cross(along)).inverted();length=(wm[h].to_3x3()@lp[mid]).length*.72
 return q,f*length-Vector((0,.02,0))
def curl(hand,shaft,point):
 f=Vector((0,0,1));f=(f-shaft*f.dot(shaft)).normalized()
 for kind in ['Index','Middle','Ring','Pinky','Thumb']:
  for seg,angle in enumerate([65,120,165],1):
   key=hand+kind+str(seg)
   if key not in names:continue
   j=names[key];children=[i for i,p in enumerate(parents) if p==j]
   if not children:continue
   e=children[0];d=point-pos(j) if kind=='Thumb' else Quaternion(shaft,math.radians(angle))@f
   if d.length>.0001:rotate(j,fromto(pos(e)-pos(j),d)@rot(j))
U=lambda v:(v[2],-v[0],v[1])
# Runtime model coordinate mapping, Unity right/up/forward to boat coordinates.
C=np.array([[0,0,1],[-1,0,0],[0,1,0]],float)
verts=np.array(D['vertices']);vh=np.column_stack((verts,np.ones(len(verts))));weights=np.array(D['weights']);indices=np.array(D['indices']);bind=np.array(D['bind']);bone_nodes=[ids[i] for i in D['bones']]
faces=[tuple(t) for sub in D['triangles'] for t in sub];mesh=bpy.data.meshes.new('Native player preview');mesh.from_pydata([(0,0,0)]*len(verts),[],faces);mesh.update();player=bpy.data.objects.new('Seated native player',mesh);scene.collection.objects.link(player)
def material(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.8;return m
skin=material('Player skin',(.58,.34,.21));tunic=material('Preview wool tunic',(.075,.16,.21));trousers=material('Preview leather trousers',(.14,.085,.043));hair=material('Hair',(.14,.066,.027))
for m in [skin,tunic,trousers,hair]:mesh.materials.append(m)
# Use the actual player mesh and weights; preview clothing colors are illustrative.
for poly in mesh.polygons:
 influences=[nodes[bone_nodes[int(indices[v,np.argmax(weights[v])])]]['name'] for v in poly.vertices]
 poly.material_index=2 if any(any(k in n for k in ['Leg','Foot','Toe']) for n in influences) else 0 if any(any(k in n for k in ['Hand','Head','Neck']) for n in influences) else 1
paddles=[o for o in bpy.data.objects if o.name.startswith('Paddle')]
# Recover the authored paddle vertices before the static review placement.
for ob in paddles:ob.location=(0,0,0);ob.rotation_euler=(0,0,0)
# Save original local mesh coordinates, then animate the rigid paddle from runtime axis.
paddleverts={o.name:[v.co.copy() for v in o.data.vertices] for o in paddles if o.type=='MESH'}
def pose(t):
 reset();seat=Vector((0,.259,-.1));a=t/1.8*math.tau;s=math.sin(a);c=math.cos(a);axis=Vector((1,-.76*s,.46*c)).normalized();center=seat+Vector((.06*s,.805+.055*math.cos(2*a),.30+.055*math.cos(2*a)))
 move(names['Hips'],seat+Vector((0,.12,0)));rotate(names['Hips'],Quaternion())
 torso=Quaternion((0,1,0),math.radians(12*c))@Quaternion((1,0,0),math.radians(3+5*math.cos(2*a)))
 rotate(names['Spine'],torso);rotate(names['Spine1'],torso)
 lq,lo=grip('LeftHand',axis,True);rq,ro=grip('RightHand',axis,False)
 for _ in range(8):
  for side,g,off in [('Left',-.31,lo),('Right',.31,ro)]:
   sh=names[side+'Arm'];el=names[side+'ForeArm'];ha=names[side+'Hand'];reach=(pos(sh)-pos(el)).length+(pos(el)-pos(ha)).length-.003;delta=center+axis*g-off-pos(sh)
   if delta.length>reach:center-=delta.normalized()*(delta.length-reach)
 for side,g,off in [('Left',-.31,lo),('Right',.31,ro)]:
  sg=-1 if side=='Left' else 1
  solve(side+'Arm',side+'ForeArm',side+'Hand',center+axis*g-off,seat+Vector((sg*.8,.45,0)))
  solve(side+'UpLeg',side+'Leg',side+'Foot',seat+Vector((sg*.18,-.055,.77)),seat+Vector((sg*.25,.26,.43)))
  foot=names[side+'Foot'];toe=names[side+'ToeBase'];rotate(foot,fromto(pos(toe)-pos(foot),Vector((0,0,1)))@rot(foot))
 rotate(names['LeftHand'],lq);rotate(names['RightHand'],rq);curl('LeftHand',axis,center-axis*.31);curl('RightHand',axis,center+axis*.31)
 matrices=np.array([np.array(wm[i]) for i in bone_nodes])@bind
 skinned=np.zeros((len(vh),4))
 for k in range(4):skinned+=np.einsum('nij,nj->ni',matrices[indices[:,k]],vh)*weights[:,k,None]
 xyz=skinned[:,:3]@C.T;mesh.vertices.foreach_set('co',xyz.astype(np.float32).ravel());mesh.update()
 # Authored Blender paddle axis is negative Y (Unity X).
 q=fromto(Vector((0,-1,0)),Vector(U(axis)))@Quaternion(Vector((0,-1,0)),-math.pi/2)
 for ob in paddles:ob.rotation_mode='QUATERNION';ob.rotation_quaternion=q;ob.location=U(center)
 # Actual solved palm distance is checked every frame.
 errors=[(pos(names['LeftHand'])+lo-(center-axis*.31)).length,(pos(names['RightHand'])+ro-(center+axis*.31)).length]
 return max(errors)
# Shore study setting, clear water and sand, without pretending to run game physics.
for ob in list(bpy.data.objects):
 if ob.type in ['LIGHT','CAMERA']:bpy.data.objects.remove(ob,do_unlink=True)
water=material('Coastal water',(.038,.16,.17));bs=water.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.23;bs.inputs['Metallic'].default_value=.15
noise=water.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=8;noise.inputs['Detail'].default_value=2;bump=water.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.23;bump.inputs['Distance'].default_value=.07;water.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);water.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.035));bpy.context.object.name='Calm coastal water';bpy.context.object.data.materials.append(water)
# Remove preexisting studio floor to expose water.
for ob in list(bpy.data.objects):
 if ob.type=='MESH' and ob.name.lower() in ['ground','floor','studio floor']:bpy.data.objects.remove(ob,do_unlink=True)
world=scene.world;world.use_nodes=True;world.node_tree.nodes.get('Background').inputs[0].default_value=(.34,.43,.50,1);world.node_tree.nodes.get('Background').inputs[1].default_value=.65
for name,loc,energy,size in [('Sunlight',(-2,-4,7),1100,5),('Soft fill',(3,4,5),650,5)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.4))-o.location).to_track_quat('-Z','Y').to_euler()
camd=bpy.data.cameras.new('Paddling study');cam=bpy.data.objects.new('Paddling study',camd);scene.collection.objects.link(cam);scene.camera=cam;cam.location=(4.5,-6.4,4.1);cam.rotation_euler=(Vector((.05,0,.6))-cam.location).to_track_quat('-Z','Y').to_euler();camd.type='ORTHO';camd.ortho_scale=6.6
scene.render.resolution_x=960;scene.render.resolution_y=540;scene.render.resolution_percentage=100;scene.cycles.samples=12;scene.render.threads=6;scene.render.fps=25;scene.render.use_persistent_data=True
frames=range(45) if '--all' in sys.argv else [0]
maxerror=0
for frame in frames:
 maxerror=max(maxerror,pose(frame/25));scene.render.filepath=str(out/f'frame-{frame:03}.png');bpy.ops.render.render(write_still=True);print('FRAME',frame,'max palm error',round(maxerror,6),flush=True)
(out/'validation.json').write_text(json.dumps({'duration':15,'fps':25,'cycle_frames':45,'max_palm_target_error_m':maxerror,'native_player_mesh':True,'game_cloth_not_simulated':True},indent=2))
