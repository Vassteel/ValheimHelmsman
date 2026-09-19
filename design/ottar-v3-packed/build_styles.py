"""Ottar appearance studies. Original mesh decoration; no game registration.
Run after build_model.py with Python containing bpy 5.1.
"""
from pathlib import Path
import bpy,math,json,random
from math import sin,cos,pi
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
OUT=ROOT/'styles';OUT.mkdir(exist_ok=True)
STYLES=[
 dict(id='carved-timber',name='Carved timber',paint=(.29,.15,.064),trim=(.48,.30,.13),sail=(.69,.58,.37),stripe=None,lower=(.17,.087,.039),description='Tarred timber, contrasting carved braid and seabird medallions; ochre sail.'),
 dict(id='red-ochre',name='Red ochre',paint=(.31,.052,.026),trim=(.74,.62,.38),sail=(.77,.70,.52),stripe=(.36,.074,.033),lower=(.085,.046,.027),description='Red ochre upper strakes, pale braided trim and a cream sail with red panels.'),
 dict(id='deep-blue',name='Deep blue',paint=(.025,.105,.15),trim=(.73,.62,.37),sail=(.73,.72,.59),stripe=(.04,.135,.19),lower=(.06,.06,.044),description='Deep blue upper strakes, pale golden trim and an ivory sail with blue edges; an artistic game palette.')]

def material(name,c):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Roughness'].default_value=.83
 return m

def interp(values,t):
 q=t*(len(values)-1);i=min(int(q),len(values)-2);u=q-i
 return values[i]*(1-u)+values[i+1]*u
W=[0,.18,.40,.61,.78,.87,.94,.98,1,1,.995,.98]
Z=[0,.055,.13,.24,.36,.47,.58,.69,.79,.87,.94,1]
def hp(x,s,side):
 t=abs(x/7.92);w=2.4*max(.001,1-t**2.55)**.70
 keel=.02+.77*t**5;rail=2.26+.99*t**3
 return (x,side*(max(.018,w*interp(W,s))+.026),keel+(rail-keel)*interp(Z,s))
def mesh(name,verts,faces,mat,col):
 data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
 ob=bpy.data.objects.new(name,data);col.objects.link(ob);data.materials.append(mat);return ob

def tube(name,points,r,mat,col,sides=6):
 pts=[Vector(p) for p in points];vs=[];fs=[]
 for i,p in enumerate(pts):
  direction=pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)];direction.normalize()
  axis=direction.cross(Vector((0,0,1)) if abs(direction.z)<.9 else Vector((0,1,0)));axis.normalize();up=direction.cross(axis)
  vs.extend(tuple(p+r*(axis*cos(j*2*pi/sides)+up*sin(j*2*pi/sides))) for j in range(sides))
 for i in range(len(pts)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs.extend([tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+j for j in range(sides))])
 return mesh(name,vs,fs,mat,col)

def sailpoint(u,v,offset=.014):
 width=10.2+.8*v;y=(u-.5)*width;x=.32+1.05*sin(pi*u)*sin(pi*v)+offset
 z=11.56-7.55*v-.13*sin(pi*u);a=math.radians(14)
 return (x*cos(a)-y*sin(a),x*sin(a)+y*cos(a),z)

manifest={'vessel':'Ottar','status':'Standalone appearance studies; not registered or installed in Helmsman.',
 'base_model':'../ottar.blend','styles':[],'future_controls':['Hull finish','Decorative trim','Sailcloth'],
 'integration_note':'The current puffin material switcher needs an Ottar-specific binding that preserves each strake and sail-panel slot. Decorative parts belong in a separate toggle group. These files do not install that binding.',
 'historical_note':'Hull type follows the Skuldelev 1 / Ottar study. Paint schemes and seabird ornaments are original artistic choices, not claimed documented Ottar liveries.'}
for style in STYLES:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ottar.blend'))
 ship=bpy.data.collections['OTTAR - standalone review model']
 ship.name='OTTAR - '+style['name']
 decor=bpy.data.collections.new('Ottar decorative trim - optional');ship.children.link(decor)
 trim=material('Ottar trim / '+style['id'],style['trim'])
 finish=material('Ottar upper hull / '+style['id'],style['paint'])
 lower=material('Ottar lower tar / '+style['id'],style['lower'])
 cloth=material('Ottar sail / '+style['id'],style['sail'])
 stripe=material('Ottar sail accent / '+style['id'],style['stripe'] or style['sail'])
 for ob in list(ship.objects):
  name=ob.name
  if 'clinker strake' in name:
   number=int(name.rsplit(' ',1)[-1])
   if number>=7:ob.data.materials.clear();ob.data.materials.append(finish)
   elif number<=3:ob.data.materials.clear();ob.data.materials.append(lower)
  if 'gunwale' in name:
   ob.data.materials.clear();ob.data.materials.append(trim)
  if 'Sail / sewn wool panel' in name:
   number=int(name.rsplit(' ',1)[-1]);accent=(number in [3,4,9,10] if style['id']=='red-ochre' else number in [1,2,11,12]) and style['stripe'] is not None
   ob.data.materials.clear();ob.data.materials.append(stripe if accent else cloth)
 # Braided, shallow relief follows the actual upper hull without touching decks.
 for side in [-1,1]:
  for strand in [-1,1]:
   pts=[]
   for i in range(337):
    x=-6.45+i*12.9/336;s=.885+strand*.025*sin((x+6.45)*pi*1.25)
    p=list(hp(x,s,side));p[1]+=side*.007*cos((x+6.45)*pi*1.25)
    pts.append(p)
   tube('Ottar braid / '+('port' if side==1 else 'starboard')+' / '+str(strand),pts,.012,trim,decor)
  for edge in [.846,.925]:tube('Ottar braid border',[hp(-6.55+i*13.1/150,edge,side) for i in range(151)],.009,trim,decor)
  # Small seabird medallions near the bow and stern, well outside player paths.
  for x in [-6.7,6.7]:
   s=.75;r=.19
   tube('Ottar seabird medallion rim',[hp(x+r*cos(i*pi/20),s+.068*sin(i*pi/20),side) for i in range(41)],.011,trim,decor)
   wings=[(-.145,0),(-.073,.027),(0,.003),(.073,.027),(.145,0)]
   tube('Ottar carved gull wings',[hp(x+dx,s+ds,side) for dx,ds in wings],.015,trim,decor)
   tube('Ottar carved gull tail',[hp(x,s+.006,side),hp(x-.028,s-.029,side),hp(x+.028,s-.024,side)],.011,trim,decor)
 # A modest painted gull emblem on the square sail. Two surfaces cover both sides.
 if style['stripe']:
  ink=material('Ottar sail bird / '+style['id'],style['paint'])
  shape=[(.37,.38),(.435,.338),(.48,.353),(.505,.376),(.53,.353),(.575,.338),(.64,.38),(.577,.365),(.54,.387),(.505,.419),(.47,.387),(.433,.365)]
  for offset in [.016,-.025]:
   mesh('Ottar sail painted seabird', [sailpoint(u,v,offset) for u,v in shape], [tuple(range(len(shape)))],ink,decor)
 for ob in decor.objects:
  ob['appearance_only']=True;ob['collision']='none';ob['future_toggle']='Decorative trim'
 bpy.context.scene['vessel_name']='Ottar';bpy.context.scene['appearance']=style['name']
 # Save a display-ready editable scene; export only the ship and ornament meshes.
 bpy.ops.object.select_all(action='DESELECT')
 for ob in ship.all_objects:ob.select_set(True)
 bpy.context.view_layer.objects.active=next(iter(ship.objects))
 bpy.ops.export_scene.gltf(filepath=str(OUT/('ottar-'+style['id']+'.glb')),export_format='GLB',use_selection=True,export_apply=True,export_yup=True)
 scene=bpy.context.scene;scene.render.resolution_x=1100;scene.render.resolution_y=1000
 scene.render.resolution_percentage=100;scene.render.threads_mode='FIXED';scene.render.threads=4;scene.cycles.samples=24
 scene.render.filepath=str(OUT/('ottar-'+style['id']+'.png'))
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/('ottar-'+style['id']+'.blend')))
 bpy.ops.render.render(write_still=True)
 entry=dict(style);entry['blend']='ottar-'+style['id']+'.blend';entry['glb']='ottar-'+style['id']+'.glb';entry['preview']='ottar-'+style['id']+'.png';entry['decoration_objects']=len(decor.objects)
 manifest['styles'].append(entry)
 print('COMPLETE '+style['name'],flush=True)
(OUT/'styles.json').write_text(json.dumps(manifest,indent=2)+'\n')
