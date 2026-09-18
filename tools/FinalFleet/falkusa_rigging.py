"""Working lateen halyard, mast parrel, jib sheet and supported rig attachments."""
import math
from mathutils import Vector

def finish(ship,env):
    tube,line,box,oak,rope,iron=(env[k] for k in ['tube','line','box','oak','rope','iron'])
    def block(name,p):
        p=Vector(p)
        for side in [-1,1]:
            box(name+' cheek',p+Vector((0,side*.043,0)),(.12,.025,.19),oak,.006)
        tube(name+' sheave',[p+Vector((0,-.032,0)),p+Vector((0,.032,0))],.052,oak,10)
        line(name+' axle',p+Vector((0,-.065,0)),p+Vector((0,.065,0)),.01,iron)
        tube(name+' strop',[p+Vector((.076*math.sin(i*math.pi/12),0,.116*math.cos(i*math.pi/12))) for i in range(25)],.011,rope,6)
    # Halyard block hangs against the masthead; fall runs down its aft face.
    head=Vector((-.80,-.10,5.86));block('Lateen halyard block',head)
    tube('Halyard head lashing',[(-.8,-.10,5.97),(-.75,.045,5.97),(-.85,.045,5.97),(-.8,-.10,5.97)],.012,rope,6)
    tube('Lateen halyard fall',[head+Vector((-.06,0,0)),(-.02,-.12,1.13),(.075,-.15,1.0)],.015,rope,6)
    # Parrel encircles mast and yard at their real crossing, holding the yard to the mast.
    tube('Lateen yard parrel',[(-.67,-.45,6.03),(-.86,-.07,6.02),(-.87,.065,5.97),(-.73,.065,5.95),(-.67,-.45,6.03)],.018,rope,7)
    for i in range(5):
        y=-.40+i*.06
        t=(y+.45)/.38
        box('Parrel wooden bead',(-.67-.19*t,y,6.03-.01*t),(.06,.045,.07),oak,.014)
    # Jib stay follows the head and tack edge; lash its lower end to the bowsprit.
    line('Jib supporting stay',(-.8,.20,6),(5.45,.20,1.48),.018,env['rope_dark'])
    tube('Bowsprit stay lashing',[(5.39,.20,1.48),(5.45,.035,1.40),(5.49,-.04,1.40),(5.42,-.04,1.46),(5.39,.20,1.48)],.012,rope,6)
    # Separate sheet to the starboard working rail, with a real turning block.
    foot=Vector((.90,1.25,1.29));block('Jib turning block',foot)
    tube('Jib block rail lashing',[(.90,1.25,1.18),(.90,1.30,1.26),(.90,1.25,1.37),(.90,1.20,1.26),(.90,1.25,1.18)],.014,rope,6)
    line('Jib working sheet',(1.65,.26,1.50),foot,.018,rope)
    # Tail and a compact belay remain against the rail instead of crossing the deck.
    tube('Jib control tail',[foot,(.55,1.27,1.26),(.38,1.27,1.29)],.015,rope,6)
    box('Jib belaying cleat',(.38,1.27,1.29),(.23,.07,.06),oak,.008)
    tube('Jib cleat knot',[(.28,1.25,1.32),(.47,1.29,1.32),(.47,1.25,1.33),(.28,1.29,1.33),(.28,1.25,1.32)],.011,rope,6)
