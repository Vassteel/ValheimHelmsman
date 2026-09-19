"""Grounded wear for the five puffin workstations, authored in mesh space."""
import math, random
from mathutils import Matrix,Vector

def apply(key,e):
 if key not in ('workbench','tool-rack','caulking-station','rigging-rack','paint-stand'):return
 col=e['col'];mesh=e['mesh'];mat=e['mat'];tube=e['tube'];box=e['box']
 rng=random.Random(831+sum(map(ord,key)))
 heights={'workbench':1.055,'tool-rack':.785,'caulking-station':.815,'paint-stand':.795,'rigging-rack':.985}
 scale=1.055/heights[key]
 # Uniform scale keeps pot bases, tools, rope bindings and their supports together.
 import bpy
 bpy.context.view_layer.update()
 for o in col.objects:o.matrix_world=Matrix.Scale(scale,4)@o.matrix_world
 bpy.context.view_layer.update()
 shade=[mat('Workshop weathered timber '+str(i),c,grain=True) for i,c in enumerate([(.30,.21,.12),(.38,.27,.16),(.43,.31,.19)])]
 scuff=mat('Workshop worn timber',(.52,.39,.25),grain=True)
 cut=mat('Workshop resin ingrained cuts',(.105,.076,.045))
 dirt=mat('Workshop resin old grime',(.18,.135,.08))
 def patch(name,x,y,z,rx,ry,m):
  pts=[(x+rx*math.cos(i*math.tau/9)*rng.uniform(.75,1.1),y+ry*math.sin(i*math.tau/9)*rng.uniform(.75,1.1),z) for i in range(9)]
  return mesh(name,pts,[tuple(range(9))],m)
 # Vary separate timbers and nick selected top edges without shifting contact heights.
 tops=[]
 for ob in list(col.objects):
  if ob.type!='MESH':continue
  name=ob.name.lower()
  structural=any(s in name for s in ['table top','working top','shelf','apron','leg','brace','stretcher','rack upright','rack crossbeam','hanging rail','rack post','trestle'])
  if not structural:continue
  ob.data.materials.clear();ob.data.materials.append(rng.choice(shade))
  points=[ob.matrix_world@v.co for v in ob.data.vertices]
  lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
  if 'table top' in name or 'working top' in name or 'shelf' in name:
   tops.append((lo,hi))
   inv=ob.matrix_world.inverted()
   # Small adze dents along the front lip leave a continuous, closed timber mesh.
   for v in ob.data.vertices:
    p=ob.matrix_world@v.co
    if p.y<lo.y+.012 and p.z>hi.z-.025:
     p.y+=.008*(.5+.5*math.sin(p.x*37+len(name)));v.co=inv@p
   for i in range(4):
    x=rng.uniform(lo.x+.04,hi.x-.04);y=rng.uniform(lo.y+.018,hi.y-.018)
    patch('Worn surface scoring',x,y,hi.z+.0015,rng.uniform(.025,.095),rng.uniform(.0015,.0035),cut if i%3 else scuff)
   for i in range(3):
    patch('Abraded front edge',rng.uniform(lo.x+.05,hi.x-.05),lo.y+.009,hi.z+.002,rng.uniform(.025,.075),.004,scuff)
 # Distinct work traces, kept on the clear front working edge rather than through props.
 z=1.055
 if key=='tool-rack':
  for i in range(7):
   x=rng.uniform(-.30,.20);y=rng.uniform(-.35,-.27)
   tube('Curled plane shaving',[(x+.020*math.cos(t*.3),y+.016*math.sin(t*.3),z+.009+t*.0007) for t in range(19)],.0028,scuff,4)
 elif key=='caulking-station':
  for i in range(8):patch('Old pitch splash',rng.uniform(.13,.48),rng.uniform(-.25,-.10),z+.002,rng.uniform(.007,.027),rng.uniform(.005,.014),cut)
  tube('Discarded oakum strand',[(-.41,-.31,z+.005),(-.32,-.30,z+.013),(-.26,-.34,z+.007),(-.18,-.32,z+.007)],.007,e['rope'],5)
 elif key=='paint-stand':
  for i in range(14):patch('Old pigment spill',rng.uniform(-.52,.5),rng.uniform(-.36,-.26),z+.002,rng.uniform(.008,.033),rng.uniform(.005,.018),[e['blue'],e['red'],e['yellow']][i%3])
 elif key=='workbench':
  for i in range(8):patch('Adze working scar',rng.uniform(-.40,.86),rng.uniform(-.36,.28),z+.002,rng.uniform(.02,.08),.002,cut)
 # Hand tools sit at slightly different working angles, as complete assemblies.
 groups=[]
 if key=='tool-rack':
  groups=[(('Plane sole','Plane wedge'),(-.19,-.08,.785),.13),
          (('Bench mallet',),(-.20,-.23,.785),-.19),
          (('Sharpening stone',),(.25,-.08,.785),.09)]
 if key=='caulking-station':
  groups=[(('Caulking iron',),(-.16,-.23,.815),-.11)]
 for prefixes,pivot,angle in groups:
  c=Vector(pivot)*scale;turn=Matrix.Translation(c)@Matrix.Rotation(angle,4,'Z')@Matrix.Translation(-c)
  for ob in col.objects:
   if ob.name.startswith(prefixes):ob.matrix_world=turn@ob.matrix_world
