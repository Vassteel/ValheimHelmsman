from pathlib import Path
BASE=Path(__file__).resolve().parent
exec(compile((BASE/'primitives.py').read_text(),'primitives.py','exec'))
ROOT=BASE/'harbour-workbench';ROOT.mkdir(exist_ok=True);ship.name='HARBOUR WORKBENCH - standalone concept'
# Three-metre width by two-metre depth, including the standing apron.
for i in range(12):box('Footprint floor plank',(-1.375+i*.25,0,.07),(.241,2,.10),deckm[i%4],.008)
for x in [-1.30,1.30]:
 for y in [.13,.81]:box('Bench leg',(x,y,.56),(.14,.14,1.0),oak,.015)
for y in [.15,.79]:box('Bench stretcher',(0,y,.42),(2.70,.10,.14),oak,.012)
for i in range(4):box('Workbench top',(0,.125+i*.215,1.08),(2.94,.207,.13),deckm[i%4],.012)
box('Back tool rail',(0,.92,1.34),(2.86,.08,.18),oak,.01)
# Puffin station: planking samples, mallet, drawknife and a little hull mould.
for i in range(3):box('Shipwright timber sample',(-.99,.54,1.18+i*.045),(.58,.16,.04),deckm[i],.008)
tube('Mallet handle',[(-1.13,.13,1.16),(-.82,.13,1.16)],.022,oak,8)
box('Mallet head',(-1.16,.13,1.18),(.12,.19,.10),oak,.02)
tube('Drawknife blade',[(-.73,.45,1.17),(-.42,.45,1.17)],.014,iron,6)
for x in [-.74,-.42]:tube('Drawknife grip',[(x,.45,1.17),(x,.59,1.17)],.018,oak,8)
# Gull station: parchment with an illustrative coast, route and dock markers.
parchment=mat('Map parchment',(.60,.51,.32),.98)
mapink=mat('Map faded blue ink',(.08,.19,.22),.98)
box('Navigation chart',(.02,.43,1.157),(.71,.51,.008),parchment,.005)
tube('Chart coast',[(-.26,.30,1.165),(-.16,.27,1.165),(-.10,.42,1.165),(.04,.45,1.165),(.12,.62,1.165),(.30,.56,1.165)],.006,mapink,5)
for x,y in [(-.19,.51),(.16,.32)]:
 hoop('Chart dock circle',(x,y,1.17),.025,.025,mapink,.004)
for i in range(7):box('Chart dotted route',(-.17+i*.05,.51-i*.025,1.168),(.013,.010,.005),mapink,.001)
for x in [-.31,.35]:tube('Map weighting rod',[(x,.18,1.171),(x,.68,1.171)],.018,oak,8)
# Pelican station: covered catch chest, net repair gear and bait pot.
for x in [.50,1.25]:box('Catch chest side',(x,.49,.48),(.065,.66,.50),pine[3],.01)
box('Catch chest front',(.875,.15,.48),(.81,.065,.50),pine[3],.01)
box('Catch chest back',(.875,.83,.48),(.81,.065,.50),pine[3],.01)
box('Catch chest lid',(.875,.49,.765),(.85,.73,.085),deckm[1],.012)
for x in [.61,1.13]:box('Catch chest iron strap',(x,.49,.818),(.040,.71,.018),iron,.002)
box('Catch chest latch',(.875,.105,.66),(.08,.028,.16),iron,.005)
for i in range(13):
 x=.48+i*.042;tube('Net mesh length',[(x,.33+.035*sin(j),1.18+j*.012) for j in range(16)],.004,rope_dark,5)
for i in range(12):
 y=.32+i*.019;tube('Net mesh cross',[(.47+j*.04,y,1.19+.022*sin(j)) for j in range(14)],.004,rope_dark,5)
for i in range(6):box('Net cork',(.50+i*.09,.68,1.18),(.06,.04,.04),endgrain,.005)
for j in range(5):hoop('Fishing line coil',(1.12,.36,1.18+j*.011),.14-j*.012,.11-j*.009,rope,.010)
# Bird posts and guide points, all within the proposed footprint.
tube('Gull perch post',[(.02,.84,.15),(.02,.84,2.13)],.055,oak,8)
tube('Gull perch crossbar',[(-.23,.84,2.13),(.27,.84,2.13)],.043,oak,8)
tube('Pelican perch post',[(1.22,.69,.15),(1.22,.69,1.50)],.065,oak,8)
box('Pelican perch board',(1.12,.65,1.52),(.48,.42,.07),deckm[2],.014)
white=mat('Bird warm white',(.66,.64,.55),.95);birdblack=mat('Bird charcoal',(.029,.035,.037),.90)
gray=mat('Gull grey wings',(.27,.31,.31),.96);orange=mat('Bird orange beak and feet',(.66,.23,.033),.93)
yellowbeak=mat('Pelican ochre beak',(.65,.38,.075),.95);eye=mat('Bird dark eyes',(.008,.009,.008),.50)
def ellipsoid(name,c,scale,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1,location=c)
 ob=move(bpy.context.object);ob.name=name;ob.scale=scale;ob.data.materials.append(m)
 return ob
def bird(species,pos,size):
 x,y,z=pos;s=size;start=set(ship.objects)
 def e(name,loc,scale,m):return ellipsoid(species+' / '+name,(x+loc[0]*s,y+loc[1]*s,z+loc[2]*s),tuple(v*s for v in scale),m)
 dark=birdblack if species=='Puffin' else gray
 e('body',(0,0,.25),(.15,.19,.23),dark if species=='Puffin' else white)
 e('breast',(0,-.13,.25),(.115,.09,.18),white)
 for side in [-1,1]:e('folded wing',(side*.128,.025,.27),(.045,.15,.18),dark)
 neck=.17 if species=='Pelican' else .0
 if neck:e('neck',(0,-.08,.51),(.065,.07,.19),white)
 hz=.53+neck
 e('head',(0,-.075,hz),(.12,.115,.12),birdblack if species=='Puffin' else white)
 for side in [-1,1]:
  if species=='Puffin':e('white cheek',(side*.080,-.128,hz),(.05,.055,.08),white)
  e('eye',(side*.088,-.159,hz+.021),(.014,.009,.014),eye)
 beaklen=.39 if species=='Pelican' else .15 if species=='Puffin' else .17
 verts=[(x-.052*s,y-.16*s,z+(hz+.027)*s),(x+.052*s,y-.16*s,z+(hz+.027)*s),(x,y-(.16+beaklen)*s,z+(hz-.020)*s),(x,y-.17*s,z+(hz-(.10 if species=='Puffin' else .05))*s)]
 mesh(species+' / beak',verts,[(0,1,2),(0,2,3),(1,3,2),(0,3,1)],orange if species=='Puffin' else yellowbeak)
 if species=='Pelican':e('throat pouch',(0,-.24,hz-.09),(.067,.13,.07),yellowbeak)
 for side in [-1,1]:
  xx=x+side*.066*s
  tube(species+' / leg',[(xx,y,z+.09*s),(xx,y,z+.017*s)],.012*s,orange,6)
  mesh(species+' / webbed foot',[(xx-.042*s,y-.10*s,z+.009*s),(xx+.042*s,y-.10*s,z+.009*s),(xx,y+.025*s,z+.009*s)],[(0,1,2)],orange)
 for ob in set(ship.objects)-start:ob['bird_role']=species;ob['pose']='Static concept; animation not implemented'
bird('Puffin',(-1.16,.64,1.31),.70)
bird('Gull',(.02,.84,2.18),.78)
bird('Pelican',(1.12,.63,1.56),1.02)
# Concept metadata, not functional controllers.
roles=[{'bird':'Puffin','role':'Ship orders, upgrades and styles','perch':[-1.16,.64,1.31]}, {'bird':'Gull','role':'Dock designation, routes and scouting','perch':[.02,.84,2.18]}, {'bird':'Pelican','role':'Fishing and catch storage','perch':[1.12,.63,1.56]}]
(ROOT/'model-info.json').write_text(json.dumps({'name':'Harbour Workbench','standalone':True,'footprint_m':[3,2],'roles':roles,'bird_behaviour':'Station-bound; planned perching and bounded nearby idle movement','water_targets':'Separate configured launch berth and fishing location','limitations':['Static concept birds, not rigged or animated','No functional game menus, inventory, fishing or migration implemented']},indent=2))
bpy.ops.object.select_all(action='DESELECT')
for ob in ship.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=next(iter(ship.objects))
bpy.ops.export_scene.gltf(filepath=str(ROOT/'harbour-workbench.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True)
ground=box('Studio floor',(0,0,-.035),(100,100,.04),mat('Studio slate',(.028,.048,.058),.95),0);move(ground,studio)
def light(name,loc,power,size,color):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
 ob=bpy.data.objects.new(name,data);studio.objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector((0,0,1))-ob.location).to_track_quat('-Z','Y').to_euler()
light('Key',(2,-4,7),520,5,(1,.90,.76));light('Fill',(-4,2,5),400,4,(.73,.85,1));light('Front',(1,-5,3),220,3,(1,.95,.87))
data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);studio.objects.link(camera);scene.camera=camera;data.type='ORTHO';data.ortho_scale=4.75
camera.location=(5,-8,5.8);camera.rotation_euler=(Vector((0,.15,1.3))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'harbour-workbench.blend'))
scene.render.resolution_x=1450;scene.render.resolution_y=1200;scene.render.filepath=str(ROOT/'harbour-workbench-hero.png');bpy.ops.render.render(write_still=True)
print('Harbour Workbench concept exported and rendered',flush=True)
