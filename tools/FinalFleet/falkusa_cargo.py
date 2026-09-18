"""A deck-supported working catch and gear load for the fishing boat."""
import math,bpy
from mathutils import Vector

def finish(ship,e):
    mesh,tube,line,box=(e[k] for k in ['mesh','tube','line','box'])
    rope,oak,iron=e['rope'],e['oak'],e['iron']
    silver=bpy.data.materials['Ochre wool sail panel 00'].copy();silver.name='Fish scales silver';silver.diffuse_color=(.43,.55,.57,1)
    netmat=bpy.data.materials['Ochre wool sail panel 00'].copy();netmat.name='Green fishing net cloth';netmat.diffuse_color=(.10,.15,.11,1)
    for mat in [silver,netmat]:mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=mat.diffuse_color
    def fish(x,y,z,scale=1):
        verts=[];faces=[];rings=[(-.24,.012),(-.14,.058),(.05,.067),(.19,.035),(.24,.004)]
        for xx,r in rings:
            for j in range(8):
                a=j*math.pi/4;verts.append((x+xx*scale,y+r*math.cos(a)*scale,z+r*.65*math.sin(a)*scale))
        for k in range(4):
            for j in range(8):a=k*8+j;b=k*8+(j+1)%8;faces.append((a,b,b+8,a+8))
        faces.extend([tuple(reversed(range(8))),tuple(range(32,40))])
        mesh('Catch / silver fish',verts,faces,silver)
        mesh('Catch / tail fin',[(x+xx*scale,y+yy*scale,z+zz*scale) for zz in [-.006,.006] for xx,yy in [(-.23,0),(-.32,-.065),(-.29,0),(-.32,.065)]],[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],silver)
        line('Catch / gill',(x+.12*scale,y-.035*scale,z+.025*scale),(x+.12*scale,y+.035*scale,z+.025*scale),.003,iron)
    # Existing open crates: layered catch below the rim, each fish resting on the next layer.
    for x in [-.6,.55]:
        for layer in range(2):
            for row in range(3):fish(x+(row%2)*.07-.025,-.55+row*.19,.467+layer*.074,.85)
    # Replace the flat loose mesh with a tied rolled net, floats along its rope edge.
    for ob in list(ship.objects):
        if any(k in ob.name for k in ['Folded fishing net','Net cross strand','Net cork float']):bpy.data.objects.remove(ob,do_unlink=True)
    tube('Stowed net core',[(-.82,.66,.495),(.60,.66,.495)],.17,netmat,12)
    for i in range(18):
        x=-.82+i*1.42/17
        tube('Net mesh cross cord',[(x,.66+.174*math.cos(j*math.pi/12),.495+.174*math.sin(j*math.pi/12)) for j in range(25)],.006,e['rope_dark'],5)
    for i in range(12):
        a=i*math.pi/6
        line('Net mesh length cord',(-.82,.66+.174*math.cos(a),.495+.174*math.sin(a)),(.60,.66+.174*math.cos(a),.495+.174*math.sin(a)),.006,e['rope_dark'])
    for x in [-.53,.31]:
        tube('Net bundle tie',[(x,.66+.19*math.cos(j*math.pi/12),.495+.19*math.sin(j*math.pi/12)) for j in range(25)],.014,rope,6)
    for i in range(8):box('Net cork float',(-.73+i*.17,.77,.66),(.105,.07,.075),e['endgrain'],.015)
    # Bait bucket in the forward working bay, between the bench and frame.
    x,y,z=1.82,-.52,.315
    tube('Bait bucket bottom',[(x,y,z),(x,y,z+.025)],.16,oak,12)
    for i in range(12):
        a=i*math.pi/6;b=(i+1)*math.pi/6-.015
        vs=[(x+r*math.cos(t),y+r*math.sin(t),z+h) for r,h in [(.16,0),(.21,.30)] for t in [a,b]]
        mesh('Bait bucket stave',vs,[(0,1,3,2)],e['deckm'][i%4])
    for h,r in [(.04,.17),(.26,.203)]:e['hoop']('Bait bucket hoop',(x,y,z+h),r,r,iron,.012)
    tube('Bait bucket handle',[(x-.21,y,z+.25),(x-.13,y,z+.47),(x+.13,y,z+.47),(x+.21,y,z+.25)],.013,rope,6)
    # A folded supply bundle lashed low against the aft quarter.
    box('Wrapped fishing supplies',(-2.13,-.53,.462),(.65,.42,.30),netmat,.055)
    for xx in [-2.32,-1.94]:
        tube('Supply bundle lashing',[(xx,-.75,.312),(xx,-.75,.625),(xx,-.31,.625),(xx,-.31,.312),(xx,-.75,.312)],.014,rope,6)
