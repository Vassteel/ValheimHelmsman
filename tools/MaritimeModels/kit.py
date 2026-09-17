"""Original low-poly boatyard geometry. Units are metres; Y is up."""
import math
import numpy as np
WOOD=0;END=1;IRON=2;ROPE=3;CLOTH=4;BLUE=5;OIL=6
COLORS=[(.43,.29,.16),(.58,.41,.23),(.16,.19,.19),(.52,.44,.29),(.74,.69,.52),(.24,.35,.38),(.45,.52,.31)]
class Kit:
 def __init__(self):self.v=[];self.uv=[];self.t=[[] for _ in COLORS]
 def face(self,points,mat=WOOD):
  n=len(self.v);self.v.extend([list(p) for p in points]);self.uv.extend([[p[0]+p[2],p[1]+p[2]*.2] for p in points])
  for i in range(1,len(points)-1):self.t[mat].extend([n,n+i,n+i+1])
 def box(self,c,size,mat=WOOD):
  c=np.array(c);h=np.array(size)/2
  v=[c+h*np.array(p) for p in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
  for f in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:self.face([v[i] for i in f],mat)
 def beam(self,a,b,width=.12,depth=None,mat=WOOD):
  a=np.array(a,float);b=np.array(b,float);z=b-a;l=np.linalg.norm(z)
  if l<1e-8:return
  z/=l;up=np.array([0.,1,0]) if abs(z[1])<.95 else np.array([1.,0,0]);x=np.cross(up,z);x/=np.linalg.norm(x);y=np.cross(z,x)
  w=width/2;h=(depth or width)/2
  v=[p+x*s*w+y*t*h for p in [a,b] for s,t in [(-1,-1),(1,-1),(1,1),(-1,1)]]
  for f in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:self.face([v[i] for i in f],mat)
 def tube(self,points,radius,mat=ROPE,sides=8):
  p=np.array(points,float);rings=[]
  for i,pt in enumerate(p):
   tangent=p[min(i+1,len(p)-1)]-p[max(0,i-1)];tangent/=max(1e-9,np.linalg.norm(tangent));up=np.array([0.,1,0]) if abs(tangent[1])<.9 else np.array([1.,0,0]);x=np.cross(tangent,up);x/=max(1e-9,np.linalg.norm(x));y=np.cross(tangent,x)
   rings.append([pt+radius*(math.cos(j*2*math.pi/sides)*x+math.sin(j*2*math.pi/sides)*y) for j in range(sides)])
  for a,b in zip(rings,rings[1:]):
   for j in range(sides):k=(j+1)%sides;self.face([a[j],a[k],b[k],b[j]],mat)
  self.face(list(reversed(rings[0])),mat);self.face(rings[-1],mat)
 def ring(self,c,rx,rz,radius=.025,mat=ROPE,n=24):
  self.tube([(c[0]+rx*math.cos(i*2*math.pi/n),c[1],c[2]+rz*math.sin(i*2*math.pi/n)) for i in range(n+1)],radius,mat,6)
 def barrel(self,c,r=.35,h=.7,mat=WOOD,n=12,open_top=True):
  c=np.array(c);ys=[0,.12*h,.5*h,.88*h,h];rs=[r*.85,r*.96,r,r*.96,r*.85]
  for j in range(n):
   a=j*2*math.pi/n+.012;b=(j+1)*2*math.pi/n-.012
   for k in range(4):self.face([c+[rs[k]*math.cos(a),ys[k],rs[k]*math.sin(a)],c+[rs[k+1]*math.cos(a),ys[k+1],rs[k+1]*math.sin(a)],c+[rs[k+1]*math.cos(b),ys[k+1],rs[k+1]*math.sin(b)],c+[rs[k]*math.cos(b),ys[k],rs[k]*math.sin(b)]],mat if j%3 else END)
  for y in [.13*h,.83*h]:self.ring(c+[0,y,0],r*.98,r*.98,.025,IRON,n)
  self.face([c+[r*.85*math.cos(i*2*math.pi/n),0,r*.85*math.sin(i*2*math.pi/n)] for i in reversed(range(n))],WOOD)
  if not open_top:self.face([c+[r*.85*math.cos(i*2*math.pi/n),h,r*.85*math.sin(i*2*math.pi/n)] for i in range(n)],END)
 def cloth(self,c,w,l,drape=.15,mat=CLOTH):
  for i in range(8):
   a=-w/2+i*w/8;b=a+w/8
   ya=c[1]-drape*(2*a/w)**2;yb=c[1]-drape*(2*b/w)**2
   q=[[c[0]+a,ya,c[2]-l/2],[c[0]+a,ya,c[2]+l/2],[c[0]+b,yb,c[2]+l/2],[c[0]+b,yb,c[2]-l/2]]
   self.face(q,mat);self.face(q[::-1],mat)
 def bird_carving(self,c,s=1,variant=0):
  x,y,z=c
  self.beam((x,y,z),(x,y+.55*s,z),.22*s,.18*s,END)
  self.beam((x,y+.48*s,z),(x,y+.7*s,z+.16*s),.19*s,.18*s,END)
  self.beam((x,y+.68*s,z+.12*s),(x,y+.66*s,z+.38*s),.08*s,.10*s,END)
  for side in [-1,1]:
   self.beam((x,y+.4*s,z),(x+side*(.23+.08*variant)*s,y+.18*s,z-.12*s),.1*s,.20*s,WOOD)
  self.box((x,y+.27*s,z+.108*s),(.1*s,.16*s,.018*s),BLUE)
 def fish(self,c,l=.45,mat=END):
  x,y,z=c;r=l*.14;points=[(x,y-l*.35,z),(x-r,y,z),(x,y+l*.35,z),(x+r,y,z),(x,y,z+r*.7),(x,y,z-r*.7)]
  for f in [(0,1,4),(1,2,4),(2,3,4),(3,0,4),(1,0,5),(2,1,5),(3,2,5),(0,3,5)]:self.face([points[i] for i in f],mat)
  self.face([(x,y-l*.35,z),(x-l*.16,y-l*.55,z),(x+l*.16,y-l*.55,z)],mat)
 def peg(self,c,r=.07,h=.16):self.tube([c,(c[0],c[1]+h,c[2])],r,END,8)
 def transform(self,scale=(1,1,1),offset=(0,0,0)):
  self.v=(np.asarray(self.v)*np.array(scale)+np.array(offset)).tolist();return self

def bench():
 k=Kit()
 for x in [-.96,.88]:
  for z in [-.42,.42]:k.beam((x,0,z),(x*.94,.90,z*.82),.17)
  k.beam((x-.05,.3,-.46),(x-.05,.3,.46),.14)
 for z in [-.41,.41]:k.beam((-.99,.34,z),(.92,.34,z),.12)
 for i in range(5):k.box((0,.95,-.44+i*.22),(2.5,.16,.211),WOOD if i%2 else END)
 # Vise, inset tool tray, rib jig and pencil/tool storage. Clear centre for puffin work.
 k.box((-.97,.92,-.61),(.34,.32,.16),WOOD);k.beam((-1.12,.89,-.52),(-1.12,.89,-.81),.05,mat=IRON)
 k.box((.87,1.055,.15),(.55,.035,.52),BLUE)
 for x in [.60,1.14]:k.box((x,1.1,.15),(.035,.10,.55),END)
 for z in [-.12,.42]:k.box((.87,1.1,z),(.55,.10,.035),END)
 k.tube([(-.77,1.03,.40),(-.55,1.12,.40),(-.35,1.2,.40),(-.12,1.23,.40)],.03,END)
 for x in [-.76,-.13]:k.peg((x,1.01,.4),.035,.19)
 k.box((.05,.42,.07),(1.3,.08,.66),WOOD)
 for i in range(3):k.beam((-.5+i*.2,.48,-.17),(.25+i*.15,.48,.3),.08,.12,END)
 k.beam((.75,1.1,.1),(.77,1.25,.16),.026,mat=END)
 k.beam((-.44,1.05,-.17),(-.1,1.05,-.17),.035,mat=END);k.box((-.46,1.06,-.17),(.09,.12,.12),WOOD)
 return k

def dock(extension=False):
 k=Kit();length=10
 for z in [0,length*.5,length-.2]:
  for x in [-2.1,2.1]:k.beam((x,-7.3,z),(x,.72,z),.24)
 for x in [-1.8,1.8]:k.beam((x,-.22,0),(x,-.22,length),.20,.28)
 for i in range(int(length/.32)):k.box((0,.08,.16+i*.32),(4.22,.18,.305),WOOD if i%3 else END)
 for x in [-2.02,2.02]:
  for z in [1,length-1]:k.beam((x,-2,z),(x,-.15,z+(.8 if z<length/2 else -.8)),.16)
 if not extension:
  # Keep the chest at x~1.3,z~4.7 and the worker's original end position clear.
  k.beam((-1.8,.2,7.0),(-1.8,1.0,7.0),.13);k.beam((-1.8,.2,8.15),(-1.8,1.0,8.15),.13)
  k.box((-1.68,1.03,7.55),(.65,.10,1.5),END)
  k.barrel((-1.65,.18,6.1),.24,.45)
  k.beam((1.67,.2,8.6),(1.67,1.08,8.6),.12)
  k.beam((1.67,.2,9.35),(1.67,1.08,9.35),.12)
  k.tube([(1.67,1.08,8.55),(1.67,1.08,9.40)],.11,WOOD,12)
  for z in np.linspace(8.6,9.35,6):k.ring((1.67,1.1,z),.14,.06,.016,ROPE,12)
  k.bird_carving((-2.1,.72,9.8),.5,1)
 return k

def press():
 k=Kit()
 for x in [-.94,.94]:
  k.box((x,.12,0),(.23,.24,2.05),WOOD);k.beam((x,.12,0),(x,2.52,0),.22)
  for z in [-.9,.9]:k.beam((x,.2,z),(x,1.15,0),.13)
 k.box((0,2.48,0),(2.2,.27,.34),END)
 for z in [-.52,-.26,0,.26,.52]:k.box((0,.99,z),(1.8,.13,.24),WOOD)
 k.barrel((0,1.07,0),.52,.60)
 k.tube([(0,1.48,0),(0,2.94,0)],.072,IRON,10)
 for y in np.linspace(1.8,2.87,12):k.ring((0,y,0),.09,.09,.018,IRON,12)
 k.beam((-.65,2.82,0),(.65,2.82,0),.085,mat=END)
 k.box((0,1.58,0),(.85,.12,.85),END)
 k.beam((.34,.97,.48),(.34,.63,1.03),.09,.16,END)
 k.barrel((.34,.04,1.05),.26,.52)
 return k

def crane(large):
 k=Kit();height=5.6 if large else 4.;reach=3.8 if large else 2.6
 for z in [-1,1]:k.box((0,.16,z),(3.4,.28,.26),WOOD)
 for x in [-1.1,1.1]:
  k.beam((x,.18,-.85),(0,height,0),.25)
  k.beam((x,.18,.85),(0,height,0),.25)
 k.beam((0,height-.18,-.6),(0,height-.6,reach),.24,.30)
 k.beam((0,height-1.8,0),(0,height-.6,reach*.8),.17)
 k.tube([(0,height-.45,reach),(0,1.35,reach)],.035,ROPE)
 k.tube([(-.25,1.42,reach),(0,1.14,reach),(.25,1.42,reach)],.035,IRON)
 k.tube([(-.67,1.5,0),(.67,1.5,0)],.32 if large else .22,WOOD,12)
 for x in [-.7,.7]:k.beam((x,.2,0),(x,1.8,0),.14)
 k.beam((.8,1.5,0),(.8,1.0,.4),.07,mat=IRON)
 if large:
  for x in [-.75,.75]:k.beam((x,.5,-.7),(x,.5,.7),.14)
  k.cloth((0,2.5,-.4),1.8,1.2,.18)
 return k

def rack(net=False,eels=False):
 k=Kit()
 for x in [-.8,.8]:
  for z in [-.45,.45]:k.beam((x,0,z),(x,1.7,0),.075)
 k.beam((-.95,1.68,0),(.95,1.68,0),.1)
 k.beam((-.8,.26,-.35),(.8,.26,-.35),.065)
 if net:
  for i in range(9):
   x=-.7+i*.175;k.tube([(x,1.55,0),(x+.1,.3,.07)],.01,ROPE,4)
  for y in np.linspace(.3,1.55,8):k.tube([(-.72,y,0),(.8,y,.07)],.01,ROPE,4)
 else:
  for i in range(5):
   x=-.63+i*.30;k.tube([(x,1.68,0),(x,1.38,0)],.012,ROPE,4)
   if eels:k.tube([(x,1.4,0),(x+.1,1.1,0),(x-.08,.7,.05),(x,.4,0)],.055,END,8)
   else:k.fish((x,1.1,0),.62)
 return k

def totem(i):
 k=Kit();h=[1.45,1.9,1.65,1.35][i]
 k.beam((0,0,0),(0,h,0),.25,.23)
 for y in [.25,h-.25]:
  for a in [0,.07]:k.ring((0,y+a,0),.145,.145,.02,ROPE,12)
 if i in [0,1]:k.bird_carving((0,h,0),.65,i)
 elif i==2:
  for side in [-1,1]:k.beam((0,h,0),(side*.35,h+.45,0),.14,.16,END)
  k.ring((0,h+.02,0),.30,.14,.035,BLUE)
 else:
  for side in [-1,1]:k.beam((0,.8,0),(side*.38,0,.18),.1)
  k.box((0,h+.15,0),(.60,.42,.13),END)
  for side in [-1,1]:k.beam((0,h+.25,.08),(side*.22,h+.1,.08),.035,mat=BLUE)
 return k

def item(name):
 k=Kit()
 if name in ['ResinWood','CaulkedWood']:
  for i in range(3):
   x=(i-1)*.16;k.box((x,.11,0),(.14,.16,.75 if i!=1 else .83),WOOD if name=='ResinWood' else END)
   if name=='CaulkedWood':k.box((x,.2,0),(.10,.015,.66),IRON)
  for z in [-.23,.23]:
   k.beam((-.25,.22,z),(.25,.22,z),.035,mat=ROPE)
   for x in [-.25,.25]:k.beam((x,.025,z),(x,.22,z),.035,mat=ROPE)
 elif name=='ClothShip':
  for x in [-.15,0,.15]:k.tube([(x,.1,-.32),(x,.1,.32)],.11,CLOTH,12)
  for z in [-.19,.19]:k.beam((-.28,.22,z),(.28,.22,z),.03,mat=ROPE)
  k.cloth((.14,.025,.40),.35,.20,.015,BLUE)
 elif name=='ShipRope':
  for j in range(4):
   for n in range(3):k.ring((0,.035+j*.05,0),.15+n*.045,.19+n*.05,.023,ROPE,24)
  k.tube([(.24,.05,0),(.32,.03,.1),(.4,.025,.05),(.46,.025,.18)],.024,ROPE,6)
 elif name=='WindBelt':
  k.ring((0,.07,0),.27,.19,.055,BLUE,24);k.box((0,.1,.20),(.17,.13,.07),IRON)
  for x in [-.05,.05]:k.box((x,.13,.242),(.016,.06,.018),END)
  k.beam((0,.03,.18),(0,-.12,.20),.045,.025,ROPE)
 elif name in ['FishExtract','FishExtract2']:
  mat=BLUE if name=='FishExtract' else OIL
  k.barrel((0,0,0),.13,.26,mat,10,False);k.tube([(0,.24,0),(0,.34,0)],.065,END,8)
  k.ring((0,.27,0),.078,.078,.014,ROPE,12)
  k.box((0,.14,.132),(.12,.1,.014),CLOTH)
  k.beam((-.04,.13,.145),(.04,.17,.145),.016,mat=IRON)
 else:
  k.barrel((0,0,0),.28,.36,WOOD,14)
  k.tube([(-.28,.30,0),(-.24,.57,0),(0,.65,0),(.24,.57,0),(.28,.30,0)],.035,ROPE,8)
  for x,z in [(-.13,0),(.1,.07),(0,-.11)]:k.fish((x,.33,z),.40)
 return k

def construction(stage):
 k=Kit();length=10.;width=2.
 for z in np.linspace(-4,4,5):
  k.box((0,.15,z),(3.,.22,.25),WOOD)
  for side in [-1,1]:k.beam((side*1.3,.22,z),(side*.8,1.2,z),.12)
 k.beam((0,.7,-5),(0,.7,5),.16,.24,END)
 for z in np.linspace(-4.7,4.7,15):
  w=width*.5*math.sqrt(max(.05,1-(z/5)**2));k.tube([(-w,1.7,z),(-w*.8,1.05,z),(0,.77,z),(w*.8,1.05,z),(w,1.7,z)],.06,END,6)
 if stage<2:
  for side in [-1,1]:
   for j in range(3 if stage==1 else 6):
    y=.85+j*.15;pts=[]
    for z in np.linspace(-4.8,4.8,24):pts.append((side*width*.5*math.sqrt(max(.04,1-(z/5)**2))*(.4+j*.1),y,z))
    k.tube(pts,.09,WOOD,4)
 if stage==0:k.beam((0,1.1,0),(0,5.5,0),.18)
 return k
