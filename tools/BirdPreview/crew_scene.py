"""Production puffin geometry/poses, with scripted review routes (no Unity physics)."""
import bpy,json,math,os
from mathutils import Matrix,Vector
C=Matrix(((1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
class CrewScene:
 def __init__(self,root,scene):
  self.data=json.loads((root/os.environ.get('PUFFIN_CREW_DATA','design/puffin-circus-v1/crew.json')).read_text());self.actors=[];self.props=[];self.lines=[]
  teximage=bpy.data.images.load(str(root/'assets/birds/detail.png'));mats={};meshes=[]
  for p in self.data['parts']:
   name=p['material']
   if name not in mats:
    m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;shader=n['Principled BSDF'];shader.inputs['Roughness'].default_value=.85;shader.inputs['Base Color'].default_value=(*p['color'],1)
    if 'tile -1' not in name:
     t=n.new('ShaderNodeTexImage');t.image=teximage;mix=n.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.8;mix.inputs[1].default_value=(*p['color'],1);m.node_tree.links.new(t.outputs['Color'],mix.inputs[2]);m.node_tree.links.new(mix.outputs[0],shader.inputs['Base Color'])
    mats[name]=m
   mesh=bpy.data.meshes.new(p['name']);tri=p['triangles'];mesh.from_pydata([(v[0],-v[2],v[1]) for v in p['vertices']],[],[tri[j:j+3] for j in range(0,len(tri),3)]);mesh.update();uv=mesh.uv_layers.new()
   for loop in mesh.loops:uv.data[loop.index].uv=p['uv'][loop.vertex_index]
   for face in mesh.polygons:face.use_smooth=True
   mesh.normals_split_custom_set_from_vertices([(v[0],-v[2],v[1]) for v in p['normals']]);mesh.materials.append(mats[name]);meshes.append(mesh)
  wood=bpy.data.materials.new('Crew worked timber');wood.diffuse_color=(.44,.30,.16,1);wood.use_nodes=True;wood.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=wood.diffuse_color
  for i in range(6):
   objects=[]
   for mesh in meshes:
    o=bpy.data.objects.new(mesh.name,mesh);scene.collection.objects.link(o);objects.append(o)
   self.actors.append(objects)
   side=-1 if i%2==0 else 1;z=[5.2,1.8,-1.8][i//2];y=1.212+(10+z)*.075;anchor=Matrix.Translation((side*3.05,-z,y))@Matrix.Rotation(math.radians(90 if side<0 else -90),4,'Z')
   shapes=[]
   def box(label,p,scale):
    bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name=label;o.matrix_world=anchor@Matrix.Translation((p[0],-p[2],p[1]));o.scale=(scale[0],scale[2],scale[1]);o.data.materials.append(wood);shapes.append(o)
   if i in (0,1,3):
    height=.32 if i in (0,3) else .28
    box('Timber being worked',(0,height,.43),(.72,.09,.32))
    for x in (-.22,.22):box('Trestle',(x,(height-.045)*.5,.43),(.09,height-.045,.30))
   if i==2:
    box('Rope hauling post',(0,.16,.68),(.08,.32,.08));box('Post base',(0,.035,.68),(.30,.07,.26))
   if i>=4:
    for j in range(3):box('Supply timber',(.3,.05+j*.08,.58),(.70-j*.08,.07,.13))
   self.props.append(shapes)
   self.lines.append(None)
  curve=bpy.data.curves.new('Working rope','CURVE');curve.dimensions='3D';curve.bevel_depth=.009;curve.bevel_resolution=1;sp=curve.splines.new('POLY');sp.points.add(2);o=bpy.data.objects.new('Working rope',curve);scene.collection.objects.link(o);curve.materials.append(wood);self.rope=o
 def apply(self,agents):
  self.rope.hide_render=True
  for a in agents:
   i=a['index'];p=a['position'];anchor=Matrix.Translation((p[0],-p[2],p[1]))@Matrix.Rotation(math.radians(a['yaw']),4,'Z')
   for o,p in zip(self.actors[i],a['parts']):
    transform=Matrix([p['matrix'][k:k+4] for k in range(0,16,4)]).transposed();o.matrix_world=anchor@C@transform@C.inverted();o.hide_render=not(a['visible'] and p['visible'])
   for o in self.props[i]:o.hide_render=not a['propsVisible']
   if i==2 and a['propsVisible']:
    self.rope.hide_render=False;beak=a['beak'];start=anchor@Vector((beak[0],-beak[2],beak[1]));end=Vector((-2.37,-1.8,1.212+11.8*.075+.30));mid=(start+end)*.5-Vector((0,0,.025))
    for point,v in zip(self.rope.data.splines[0].points,(start,mid,end)):point.co=(*v,1)
