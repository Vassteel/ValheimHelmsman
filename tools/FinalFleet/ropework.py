"""Supported mooring coils and belayed running-line hanks, with visible laid strands."""
import math
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def finish(kind,ship,env,length,beam,walkz):
    tube,rope=env['tube'],env['rope']
    for ob in list(ship.objects):
        if any(s in ob.name.lower() for s in ['rope coil','coiled running','mooring coil','stern working coil']):
            bpy.data.objects.remove(ob,do_unlink=True)
    floors=[o for o in ship.objects if any(k in o.name.lower() for k in ['deck plank','floorboard','footboard','side working','sole board'])]
    vs,fs=[],[]
    for o in floors:
        offset=len(vs);vs.extend(o.matrix_world@v.co for v in o.data.vertices)
        fs.extend([offset+i for i in p.vertices] for p in o.data.polygons)
    tree=BVHTree.FromPolygons(vs,fs)
    def deck(px,py):
        # A rope may bridge a narrow plank seam, but never a cargo well.
        for dx,dy in [(0,0),(.016,0),(-.016,0),(0,.016),(0,-.016)]:
            hit,_,_,_=tree.ray_cast(Vector((px+dx,py+dy,walkz+2)),Vector((0,0,-1)),5)
            if hit is not None:return hit
        return None
    merchant=kind in ['ottar','freighter']
    report=[]
    def laid(name,pts,r=.018):
        pts=[Vector(p) for p in pts]
        distance=0
        paths=[[] for _ in range(3)]
        for i,p in enumerate(pts):
            if i:distance+=(p-pts[i-1]).length
            axis=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized()
            u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u)
            for strand in range(3):
                phase=distance/r*1.7+strand*2*math.pi/3
                paths[strand].append(p+r*.48*(u*math.cos(phase)+v*math.sin(phase)))
        for n,path in enumerate(paths):tube(name+' laid strand '+str(n),path,r*.57,rope,5)
    for side in [-1,1]:
        x=side*length*(.35 if merchant else .265);y=beam*.12 if merchant else 0
        radius=.27 if merchant else .12;r=.020 if merchant else .012
        pts=[];maxgap=0
        for i in range(241):
            t=i/240;angle=t*math.pi*10;rad=radius*(1-.58*t)
            px=x+rad*math.cos(angle);py=y+rad*.73*math.sin(angle)
            hit=deck(px,py)
            if hit is None:raise AssertionError((kind,'unsupported coil',px,py))
            pts.append((px,py,hit.z+r*1.05))
        laid('Deck mooring coil',pts,r)
        # A short loose end lies against the same deck rather than ending in midair.
        a=Vector(pts[-1]);tail=[]
        for i in range(25):
            t=i/24;px=a.x+t*.22;py=a.y+.04*math.sin(math.pi*t)
            hit=deck(px,py)
            if hit is not None:tail.append((px,py,hit.z+r*1.05))
        laid('Mooring coil tail',tail,r)
        report.append({'kind':'deck','center':[x,y],'supported_samples':len(pts)})
    # Loop over the existing mast pins. The top bight surrounds the wooden peg;
    # the lower bights hang freely inside the hull and above the walking surface.
    pegs=[o for o in ship.objects if o.name.startswith('Mast belaying peg')]
    for peg in pegs:
        verts=[peg.matrix_world@v.co for v in peg.data.vertices]
        center=(min(verts,key=lambda v:v.y)+max(verts,key=lambda v:v.y))/2
        y=max(v.y for v in verts) if center.y>0 else min(v.y for v in verts)
        z=sum(v.z for v in verts)/len(verts)
        for loop in range(4):
            pts=[]
            for i in range(81):
                t=2*math.pi*i/80
                pts.append((center.x+(.09+loop*.009)*math.sin(t),y-(.02+loop*.018)*(1 if y>0 else -1),z-(.22+loop*.008)+(.244+loop*.008)*math.cos(t)))
            laid('Belaying pin running-line hank',pts,.011)
        report.append({'kind':'hanging','pin':peg.name})
    return report
