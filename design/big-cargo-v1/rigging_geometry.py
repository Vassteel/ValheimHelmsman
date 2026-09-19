# Shared authored square-rig geometry; transformed for the heavy freighter.
# Heavy mast, tapered yard and a billowing square wool sail.
tube('Mast',[(0,0,.96),(0,0,12.12)],.17,spar,12,r2=.076)
# Yard yaw keeps the rig readable and is a review pose, not an animation.
yaw=math.radians(14)
def sailpoint(u,v):
 width=10.2+.8*v
 y=(u-.5)*width;x=.32+1.05*sin(pi*u)*sin(pi*v)
 z=11.56-7.55*v-.13*sin(pi*u)
 return (x*cos(yaw)-y*sin(yaw),x*sin(yaw)+y*cos(yaw),z)
yard_a=sailpoint(0,0);yard_b=sailpoint(1,0)
tube('Yard',[(yard_a[0],yard_a[1],11.72),(0,0,11.76),(yard_b[0],yard_b[1],11.72)],.095,spar,10)
sail_objs=[]
for panel in range(12):
 verts=[];faces=[]
 for j in range(17):
  for k in range(4):verts.append(sailpoint((panel+k/3)/12,j/16))
 for j in range(16):
  for k in range(3):a=j*4+k;faces.append((a,a+1,a+5,a+4))
 ob=mesh('Sail / sewn wool panel %02d'%(panel+1),verts,faces,cloth[panel]);sail_objs.append(ob)
 sol=ob.modifiers.new('Wool thickness','SOLIDIFY');sol.thickness=.009
 for poly in ob.data.polygons:poly.use_smooth=True
for i in range(13):
 ob=tube('Sail panel seam %02d'%i,[sailpoint(i/12,j/24) for j in range(25)],.011,cloth_seam,5);sail_objs.append(ob)
for v in [0,1]:
 ob=tube('Sail bolt rope',[sailpoint(i/40,v) for i in range(41)],.026,rope,6);sail_objs.append(ob)
for u in [0,1]:
 ob=tube('Sail leech rope',[sailpoint(u,i/30) for i in range(31)],.025,rope,6);sail_objs.append(ob)
# Short reef-point ties, stitched to the sail rather than decorative paint.
for v in [.58,.79]:
 for i in range(1,12):
  p=Vector(sailpoint(i/12,v));ob=line('Sail reef tie',p,p+Vector((.055,.035,-.23)),.014,rope_dark,.035);sail_objs.append(ob)
for u in [i/12 for i in range(13)]:
 p=sailpoint(u,0);tube('Sail yard lashing',[(p[0],p[1],p[2]),(p[0]+.11,p[1],11.85),(p[0]-.08,p[1],11.87),(p[0],p[1],p[2])],.018,rope,6)
line('Forestay',(0,0,11.96),(7.13,0,2.65),.037,rope_dark,.07)
line('Backstay',(0,0,11.89),(-6.78,0,2.41),.030,rope_dark,.07)
for side in [-1,1]:
 for i,x in enumerate([-2.9,-1.9,-.9]):
  low=(x,side*2.15,2.15);line('Shroud',(0,side*.075,11.15-i*.11),low,.024,rope_dark,.065)
  tube('Shroud lashing',[(x-.12,side*2.2,2.12),(x,side*2.27,1.86),(x+.12,side*2.2,2.12)],.025,rope,6)
 line('Yard brace',yard_a if side==-1 else yard_b,(-5.6,side*1.35,2.05),.021,rope,.11)
 corner=sailpoint(0 if side==-1 else 1,1)
 ob=line('Sail sheet',corner,(-5.20,side*1.35,2.05),.029,rope,.14);sail_objs.append(ob)
line('Halyard',(0,.14,11.76),(-.40,.24,1.73),.025,rope,.02)
