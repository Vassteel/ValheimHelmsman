from pathlib import Path
import sys
BASE=Path(__file__).resolve().parent
kind=sys.argv[1] if len(sys.argv)>1 else 'currach'
exec(compile((BASE/'primitives.py').read_text(),'primitives.py','exec'))
ROOT=BASE/kind;ROOT.mkdir(exist_ok=True)
config={'falkusa':(9.0,2.7,1.23),'ceol':(6.4,1.8,.81),'currach':(5.5,1.5,.69)}
length,beam,height=config[kind];L=length/2
ship.name=kind.upper()+' - standalone review model'
black=mat('Tarred skin or dark hull',(.046,.042,.029),.95,grain=kind!='currach')
cream=mat('Chalk sheer stripe',(.55,.49,.34),grain=True)
redcloth=mat('Red ochre currach sail',(.43,.065,.026),.98)
hide_mats=[mat('Cargo hide chestnut',(.28,.135,.063),.97),mat('Cargo hide pale',(.48,.31,.17),.96),mat('Cargo hide dark',(.16,.14,.11),.98)]
sackmat=mat('Coarse linen knapsack',(.38,.32,.19),.99);bindmat=mat('Dark cargo hemp',(.19,.14,.08),.98)
exec(compile((BASE/'cargo_primitives.py').read_text(),'cargo_primitives.py','exec'))
# Profiled original hulls, interpreted from the user's visual references.
def hull(x,s,side=1,inside=0):
 t=x/L
 if kind=='currach':
  w=beam/2*(max(.001,1-t*t)**.45 if t>=0 else (.50+.50*max(0,1-t*t)**.65))
  z0=.015+.12*abs(t)**4;rail=height+.16*max(0,t)**3+.06*max(0,-t)**2
  ww=sin(s*pi/2)**.64;zz=(1-cos(s*pi/2))**.82
 else:
  w=beam/2*max(.001,1-abs(t)**(2.8 if kind=='falkusa' else 2.2))**.65
  z0=.025+.29*abs(t)**5;rail=height+(.32 if kind=='falkusa' else .43)*abs(t)**3
  ww=sin(s*pi/2)**.83;zz=(1-cos(s*pi/2))**.9
 return (x,side*max(.012,w*ww-inside),z0+(rail-z0)*zz)
def width_at(x,z):
 lo,hi=0.,1.
 for _ in range(24):
  mid=(lo+hi)/2
  if hull(x,mid)[2]<z:lo=mid
  else:hi=mid
 return abs(hull(x,(lo+hi)/2,inside=.055)[1])
xs=[-L+length*i/64 for i in range(65)]
for side in [-1,1]:
 for j in range(9):
  lo=max(0,j/9-(0 if kind=='currach' else .008));hi=(j+1)/9;v=[];f=[]
  for x in xs:
   for s,d in [(lo,0),(hi,0),(lo,.015 if kind=='currach' else .037),(hi,.015 if kind=='currach' else .037)]:v.append(hull(x,s,side,d))
  for i in range(64):
   a=i*4;b=a+4;f.extend([(a,b,b+1,a+1),(a+2,a+3,b+3,b+2),(a,a+2,b+2,b),(a+1,b+1,b+3,a+3)])
  f.extend([(0,1,3,2),(256,258,259,257)])
  material=black if kind in ['currach','falkusa'] else pine[j%7]
  if kind=='falkusa' and j==7:material=cream
  mesh(('Port' if side==1 else 'Starboard')+' hull band %02d'%j,v,f,material)
 tube('Gunwale '+str(side),[hull(x,1,side,-.005) for x in xs],.044 if kind=='currach' else .055,oak,8)
 # Currach's light ribs and longitudinal laths remain visible inside the skin.
 for x in [-L+.20+i*(length-.4)/(28 if kind=='currach' else 15) for i in range(29 if kind=='currach' else 16)]:
  tube('Curved frame',[(xx,yy,zz+(.019 if kind=='currach' else .041)) for xx,yy,zz in [hull(x,j/20,side,.044 if kind=='currach' else .075) for j in range(21)]],.014 if kind=='currach' else .039,oak,6)
 if kind=='currach':
  for s in [.2,.32,.44,.56,.68,.80,.91]:tube('Longitudinal inner lath',[hull(x,s,side,.034) for x in xs],.011,deckm[2],5)
if kind=='currach':
 # The broad squared stern is a skin-covered transom, unlike the pointed bow.
 vs=[hull(-L,s/12,side,.001) for side in [-1,1] for s in (range(12,-1,-1) if side==-1 else range(13))]
 mesh('Tarred stern skin',vs,[tuple(range(len(vs)))],black)
 tube('Stern transom frame',[hull(-L,1,-1),(-L,0,height+.06),hull(-L,1,1)],.040,oak,8)
else:
 tube('Keel',[(x,0,-.035+.29*(abs(x)/L)**5) for x in xs],.065,oak,8)
 for side in [-1,1]:tube('Swept stem',[(side*(L-.3),0,.3),(side*(L-.08),0,.75*height),(side*L,0,height+.28),(side*(L-.11),0,height+.59)],.060,oak,8)
if kind=='falkusa':
 for side in [-1,1]:
  for zoff in [.08,.26]:
   vs=[];fs=[]
   for x in xs[7:-7]:
    _,y,z=hull(x,1,side)
    for dy,dz in [(-.022,-.085),(.022,-.085),(.022,.085),(-.022,.085)]:vs.append((x,y+dy,z+zoff+dz))
   for i in range(len(xs[7:-7])-1):
    a=i*4;b=a+4
    for j in range(4):fs.append((a+j,b+j,b+(j+1)%4,a+(j+1)%4))
   fs.extend([(0,3,2,1),tuple(len(vs)-4+j for j in range(4))])
   mesh('Removable falka sideboard',vs,fs,pine[2])
  for x in [-3,-1.5,0,1.5,3]:
   y=hull(x,1,side)[1];box('Falka retaining upright',(x,y,height+.15),(.06,.065,.47),oak,.007)
# Simple low sole boards; exposed lattice retained in the currach.
if kind!='currach':
 for i in range(29):
  x=-L*.78+i*length*.78/28;z=.28+.13*(abs(x)/L)**4;w=2*max(.04,width_at(x,z)-.015)
  box('Floorboard',(x,0,z),(.18 if kind=='ceol' else .24,w,.05),deckm[i%4],.004)
else:
 for y in [-.18,0,.18]:box('Narrow footboard',(-.10,y,.23),(3.50,.14,.03),deckm[1],.003)
seat_x={'falkusa':[-2.9,-1.3,1.2,2.9],'ceol':[-1.9,-.7,1.2],'currach':[-1.8,-.30,1.20]}[kind]
attachments=[]
for i,x in enumerate(seat_x):
 z=height*.67;w=2*max(.1,width_at(x,z)-.015)
 box('Backless thwart %02d'%i,(x,0,z),(.29,w,.065),deckm[i%4],.007)
 attachments.append({'id':'seat_'+str(i),'position':[x,0,z+.06],'kind':'seat guide'})
# Cloth meshes and sewn seams. Only sails and steering rudders; no rowing oars.
sail_objs=[]
def triangle_sail(name,A,B,C,material,belly=.20):
 A,B,C=map(Vector,(A,B,C));N=24;v=[];f=[];idx={}
 def point(u,w):
  t=A*(1-u-w)+B*u+C*w;t.y-=belly*27*max(0,(1-u-w)*u*w);return t
 for i in range(N+1):
  for j in range(N+1-i):idx[i,j]=len(v);v.append(tuple(point(i/N,j/N)))
 for i in range(N):
  for j in range(N-i):
   f.append((idx[i,j],idx[i+1,j],idx[i,j+1]))
   if j<N-i-1:f.append((idx[i+1,j],idx[i+1,j+1],idx[i,j+1]))
 ob=mesh(name,v,f,material);sail_objs.append(ob);sol=ob.modifiers.new('Cloth thickness','SOLIDIFY');sol.thickness=.005
 for P,Q in [(A,B),(B,C),(C,A)]:sail_objs.append(line(name+' bolt rope',P,Q,.014,rope))
 for k in range(1,9):
  u=k/9;pts=[point(u*(1-j/24),j/24) for j in range(25)]
  sail_objs.append(tube(name+' stitched seam',pts,.006,cloth_seam,5))
if kind=='falkusa':
 mastfoot=(.2,0,.30);masthead=(-.8,0,6.0)
 tube('Raked mast',[mastfoot,masthead],.085,spar,10,r2=.047)
 A=(3.4,.10,1.95);B=(-3.25,.10,8.65);C=(-3.45,.10,1.75)
 tube('Long lateen yard',[A,B],.064,spar,10,r2=.039)
 triangle_sail('Lateen mainsail',A,B,C,cloth[0],.36)
 tube('Bowsprit',[(2.7,0,1.03),(5.55,0,1.43)],.052,spar,10,r2=.027)
 triangle_sail('Jib',(5.45,-.03,1.48),masthead,(1.65,-.04,1.5),cloth[1],.22)
 line('Yard hoist',masthead,(-.55,.10,5.95),.022,rope)
 for side in [-1,1]:line('Mast shroud',masthead,(-1.4,side*1.18,1.2),.019,rope_dark)
 line('Mainsheet',C,(-3.80,-.55,.85),.024,rope)
elif kind=='ceol':
 masthead=(.30,0,5.9);tube('Mast',[(.30,0,.27),masthead],.065,spar,10,r2=.035)
 def squarepoint(u,v):return (.42+.35*sin(pi*u)*sin(pi*v),(u-.5)*(3.7+.3*v),5.53-3.92*v)
 tube('Square sail yard',[squarepoint(0,0),squarepoint(1,0)],.047,spar,8)
 for k in range(8):
  v=[];f=[]
  for j in range(17):
   for i in range(4):v.append(squarepoint((k+i/3)/8,j/16))
  for j in range(16):
   for i in range(3):a=j*4+i;f.append((a,a+1,a+5,a+4))
  ob=mesh('Square sail panel %d'%k,v,f,cloth[k]);sail_objs.append(ob)
 for i in range(9):sail_objs.append(tube('Sail seam',[squarepoint(i/8,j/24) for j in range(25)],.007,cloth_seam,5))
 for side in [-1,1]:
  line('Mast shroud',masthead,(-.9,side*.85,.78),.018,rope_dark)
  sail_objs.append(line('Sail sheet',squarepoint(0 if side==-1 else 1,1),(-1.65,side*.65,.73),.020,rope,.05))
 line('Forestay',masthead,(2.8,0,1.1),.020,rope_dark)
 line('Backstay',masthead,(-2.65,0,1.02),.019,rope_dark)
else:
 masthead=(1.12,0,4.45);tube('Light mast',[(1.12,0,.23),masthead],.041,spar,10,r2=.024)
 triangle_sail('Red sailing cloth',(1.12,-.02,4.4),(-1.55,-.05,.97),(1.12,-.03,.95),redcloth,.18)
 tube('Light boom',[(1.12,0,.94),(-1.62,0,.96)],.028,spar,8)
 line('Forestay',masthead,(2.3,0,.77),.015,rope_dark)
 for side in [-1,1]:line('Shroud',masthead,(.18,side*.65,.64),.013,rope_dark)
 line('Mainsheet',(-1.6,0,.96),(-2.1,-.36,.56),.016,rope)
# Stern-mounted carved rudder for the sailing craft; adaptation for future game controls.
x=-L-.11;z=height*.69
outline=[(x-.02,z+.30),(x+.075,z+.16),(x+.09,.10),(x-.10,-.33),(x-.39,-.31),(x-.40,.08),(x-.17,z*.45)]
v=[(xx,y,zz) for y in [-.035,.035] for xx,zz in outline];N=len(outline)
mesh('Carved stern rudder',v,[tuple(reversed(range(N))),tuple(N+i for i in range(N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)],oak)
tube('Rudder tiller',[(x,0,z+.21),(-L+.78,0,z+.26)],.031,oak,8)
attachments.append({'id':'helm','position':[-L+.65,0,height*.6],'kind':'helm guide'})
# Role-specific loads.
if kind=='currach':
 hide_bale(.38,-.22,.27,w=.56,d=.46,h=.36);hide_bale(.45,.29,.27,w=.52,d=.43,h=.32)
 barrel(-.95,.20,.22,r=.20,h=.48)
 for x,y in [(-.80,-.32),(.73,-.30)]:
  sack(x,y,.28,r=.18,h=.40,label='Cargo / knapsack')
  tube('Knapsack shoulder strap',[(x-.13,y,.44),(x-.16,y+.06,.67),(x+.12,y+.05,.67),(x+.13,y,.44)],.017,bindmat,6)
 inventory={'columns':3,'rows':2,'slots':6,'status':'Integration target only; not implemented in-game'}
elif kind=='falkusa':
 for x in [-2.3,2.25]:barrel(x,.72,.31,r=.28,h=.62)
 for x in [-.6,.55]:
  box('Fish crate base',(x,-.35,.38),(.78,.65,.10),deckm[2])
  for h in [.5,.66]:
   for y in [-.66,-.04]:box('Fish crate side',(x,y,h),(.78,.047,.13),pine[3])
   for xx in [x-.38,x+.38]:box('Fish crate end',(xx,-.35,h),(.045,.65,.13),pine[3])
 # Folded fine-mesh fishing net with rope cork floats.
 for j in range(16):
  xx=-.9+j*.09;tube('Folded fishing net strand',[(xx,.37+.16*sin(i*.7),.52+i*.025) for i in range(18)],.007,rope_dark,5)
 for j in range(14):
  yy=.40+j*.021;tube('Net cross strand',[(-.90+i*.08,yy,.55+.045*sin(i*.8+j)) for i in range(18)],.007,rope_dark,5)
 for i in range(9):box('Net cork float',(-.83+i*.16,.62,.63),(.10,.055,.055),endgrain,.01)
 inventory={'status':'Fishing inventory capacity to be chosen during integration'}
else:
 box('Small sea chest',(-1.28,.27,.47),(.55,.45,.37),pine[2],.014)
 for dx in [-.17,.17]:box('Chest iron band',(-1.28+dx,.27,.66),(.025,.46,.016),iron,.002)
 inventory={'status':'Little Boat replacement capacity retained during integration'}
for x,y in [(-L*.65,-beam*.20),(L*.62,0)]:
 for j in range(5):hoop('Mooring rope coil',(x,y,.37+j*.014),.13+j*.014,.11+j*.014,rope,.015)
for ob in ship.objects:
 if ob.name.startswith('Cargo /'):ob['appearance_only']=True;ob['future_collision']='none'
(ROOT/'model-info.json').write_text(json.dumps({'name':kind,'standalone':True,'length_m':length,'beam_m':beam,'rowing_oars':0,'inventory':inventory,'reference':'User-supplied images; original interpreted geometry','limitations':['No Unity integration, collision, sailing physics or seat interactions','Dimensions and rig are first-pass game-design choices']},indent=2))
(ROOT/'attachment-points.json').write_text(json.dumps({'status':'Alignment guides only','points':attachments},indent=2))
# Export and render.
bpy.ops.object.select_all(action='DESELECT')
for ob in ship.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=next(iter(ship.objects))
bpy.ops.export_scene.gltf(filepath=str(ROOT/(kind+'.glb')),export_format='GLB',use_selection=True,export_apply=True,export_yup=True)
def light(name,loc,power,size,color):
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
 ob=bpy.data.objects.new(name,data);studio.objects.link(ob);ob.location=loc;ob.rotation_euler=(Vector((0,0,2))-ob.location).to_track_quat('-Z','Y').to_euler()
ground=box('Studio floor',(0,0,-.51),(200,200,.06),mat('Studio slate',(.028,.048,.058),.95),0);move(ground,studio)
light('Warm key',(4,-10,14),1800,8,(1,.90,.77));light('Cool fill',(-4,8,10),1400,8,(.68,.82,1));light('Front',(2,-12,5),700,7,(1,.97,.9))
data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);studio.objects.link(camera);scene.camera=camera;data.type='ORTHO'
def camera_at(loc,target,scale):camera.location=loc;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=scale
scale={'falkusa':14.6,'ceol':8.9,'currach':7.1}[kind]
camera_at((length*1.4,-length*2,length*1.25),(0,0,{'falkusa':3.7,'ceol':2.5,'currach':1.9}[kind]),scale)
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/(kind+'.blend')))
scene.render.resolution_x=1400;scene.render.resolution_y=1150
scene.render.filepath=str(ROOT/(kind+'-hero.png'));bpy.ops.render.render(write_still=True)
for ob in sail_objs:ob.hide_render=True
camera_at((length,-length*1.6,length*1.6),(0,0,height*.55),length*1.18);scene.render.resolution_x=1500;scene.render.resolution_y=1000
scene.render.filepath=str(ROOT/(kind+'-deck.png'));bpy.ops.render.render(write_still=True)
print(kind+' exported and rendered',flush=True)
