# Authored cargo shape definitions, loaded into the build script namespace.
def barrel(cx,cy,cz,r=.36,h=.83):
 for j in range(12):
  a=2*pi*j/12+.006;b=2*pi*(j+1)/12-.006;v=[]
  for zz,rr in [(0,.86*r),(.12*h,.95*r),(.5*h,r),(.88*h,.95*r),(h,.86*r)]:
   v.extend([(cx+rr*cos(a),cy+rr*sin(a),cz+zz),(cx+rr*cos(b),cy+rr*sin(b),cz+zz)])
  mesh('Cargo / barrel stave',v,[(i,i+1,i+3,i+2) for i in range(0,8,2)],pine[j%7])
 for z in [.12*h,.85*h]:hoop('Cargo / barrel hoop',(cx,cy,cz+z),r*.95,r*.95,iron,.023)
 v=[(cx+r*.86*cos(i*pi/8),cy+r*.86*sin(i*pi/8),cz+h) for i in range(16)]
 mesh('Cargo / barrel lid',v,[tuple(range(16))],endgrain)
 for dx in [-.14,0,.14]:line('Cargo / lid joint',(cx+dx,cy-r*.68,cz+h+.002),(cx+dx,cy+r*.68,cz+h+.002),.003,oak)
def hide_bale(x,y,z,w=.82,d=.70,h=.75):
 # Ragged, layered hide edges distinguish these from generic wrapped boxes.
 outline=[(-.50,-.43),(-.22,-.50),(.02,-.43),(.32,-.50),(.50,-.31),(.43,-.06),(.51,.18),(.40,.46),(.10,.43),(-.11,.51),(-.40,.42),(-.48,.17),(-.43,-.08)]
 for layer in range(9):
  zz=z+layer*h/9;verts=[]
  for top in [0,.071]:
   for i,(a,b) in enumerate(outline):verts.append((x+a*w,y+b*d,zz+top+.009*sin(i*1.7+layer)))
  n=len(outline);faces=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
  mesh('Cargo / bale of hides / layered skins',verts,faces,hide_mats[layer%3],bevel=.008)
 for dx in [-.24*w,.24*w]:
  tube('Cargo / hide bale binding',[(x+dx,y-d*.52,z+.035),(x+dx,y-d*.52,z+h+.01),(x+dx,y+d*.52,z+h+.01),(x+dx,y+d*.52,z+.035),(x+dx,y-d*.52,z+.035)],.025,bindmat,6)
 # A folded flap at the top with a slightly curled edge.
 mesh('Cargo / hide bale folded flap',[(x-w*.36,y-d*.30,z+h+.006),(x+w*.24,y-d*.30,z+h+.006),(x+w*.34,y+d*.25,z+h+.035),(x-w*.26,y+d*.35,z+h+.016)],[(0,1,2,3)],hide_mats[1])
def sack(x,y,z,r=.29,h=.69,label='Cargo / tied sack'):
 levels=[(0,.63),(.07,.94),(.30,1.07),(.56,.94),(.75,.69),(.85,.34),(.91,.26),(1,.40)]
 verts=[];faces=[];N=14
 for k,(zz,rr) in enumerate(levels):
  for i in range(N):
   a=2*pi*i/N;wrinkle=1+.045*sin(i*2.2+k)
   verts.append((x+r*rr*cos(a)*wrinkle,y+r*.84*rr*sin(a)*wrinkle,z+h*zz+(.017*sin(i*2.5) if k==len(levels)-1 else 0)))
 for k in range(len(levels)-1):
  for i in range(N):a=k*N+i;b=k*N+(i+1)%N;faces.append((a,b,b+N,a+N))
 faces.extend([tuple(reversed(range(N))),tuple((len(levels)-1)*N+i for i in range(N))])
 ob=mesh(label,verts,faces,sackmat)
 for poly in ob.data.polygons:poly.use_smooth=True
 hoop('Cargo / sack neck tie',(x,y,z+h*.865),r*.32,r*.28,bindmat,.018)
 line('Cargo / sack knot tails',(x+r*.31,y,z+h*.865),(x+r*.46,y+.06,z+h*.64),.015,bindmat,.03)
 tube('Cargo / sack stitched seam',[(x+r*rr,y,z+h*zz) for zz,rr in levels[:-1]],.008,bindmat,5)
def bundle(x,y,z,length=1.48,r=.25,label='Cargo / rolled trade bundle'):
 tube(label,[(x-length/2,y,z),(x+length/2,y,z)],r,bale_mat,12)
 # Concentric end-grain-like rings read as a rolled sheet rather than a timber log.
 for end in [-1,1]:
  xx=x+end*(length/2+.008)
  for rad in [r*.36,r*.68,r*.90]:tube('Cargo / rolled hide edge',[(xx,y+rad*cos(i*pi/12),z+rad*sin(i*pi/12)) for i in range(25)],.010,hide_mats[0],6)
 for dx in [-length*.31,length*.31]:
  tube('Cargo / bundle binding',[(x+dx,y+(r+.015)*cos(i*pi/12),z+(r+.015)*sin(i*pi/12)) for i in range(25)],.025,bindmat,6)
