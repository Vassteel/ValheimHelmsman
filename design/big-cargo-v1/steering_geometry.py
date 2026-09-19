# Original continuous steering oar; fitted to the new hull below.
# One connected carved steering oar, blending from the crooked grip into the blade.
# Entire stock stays outside the starboard hull; a small bearing supports its lash.
sections=[((-5.15,-.20,3.04),.055,.05),((-5.43,-.87,2.99),.065,.055),
 ((-5.75,-1.67,2.90),.078,.065),((-5.93,-2.02,2.73),.087,.075),
 ((-6.06,-2.17,2.13),.082,.070),((-6.20,-2.32,1.48),.090,.065),
 ((-6.32,-2.45,.94),.15,.060),((-6.43,-2.55,.56),.26,.055),
 ((-6.56,-2.67,.04),.34,.050),((-6.65,-2.75,-.32),.30,.040),
 ((-6.67,-2.76,-.42),.20,.030)]
verts=[];faces=[];N=10
for i,(pos,width,thickness) in enumerate(sections):
 center=Vector(pos);direction=Vector(sections[min(i+1,len(sections)-1)][0])-Vector(sections[max(0,i-1)][0]);direction.normalize()
 across=Vector((1,0,0));across=(across-direction*across.dot(direction)).normalized();depth=direction.cross(across).normalized()
 for j in range(N):verts.append(tuple(center+across*width*cos(j*2*pi/N)+depth*thickness*sin(j*2*pi/N)))
for i in range(len(sections)-1):
 for j in range(N):a=i*N+j;b=i*N+(j+1)%N;faces.append((a,b,b+N,a+N))
faces.extend([tuple(reversed(range(N))),tuple((len(sections)-1)*N+j for j in range(N))])
mesh('Steering oar / single carved oak',verts,faces,oak)
tube('Steering oar / outboard bearing',[(-5.96,-1.42,2.12),(-6.06,-2.13,2.13)],.080,oak,8)
for i in range(5):
 z=2.05+i*.038
 tube('Steering oar / bearing lashing',[(-6.15+.13*cos(k*pi/8),-2.17+.115*sin(k*pi/8),z) for k in range(17)],.019,rope,6)
