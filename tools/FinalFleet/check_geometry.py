from pathlib import Path
import bpy,json,math
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[2]
for kind in ['currach','ceol','falkusa','ottar','freighter','snekkja']:
 p=root/'design/fleet-final'/kind;bpy.ops.wm.open_mainfile(filepath=str(p/(kind+'.blend')))
 objects=bpy.data.collections['Helmsman '+kind+' final'].objects
 def tree(o,matrix=None):return BVHTree.FromPolygons([(matrix or Matrix.Identity(4))@o.matrix_world@v.co for v in o.data.vertices],[list(p.vertices) for p in o.data.polygons])
 hull=[(o.name,tree(o)) for o in objects if 'hull band' in o.name or 'clinker strake' in o.name or 'gunwale' in o.name.lower() or o.name.startswith(('Swept stem','Stern transom'))]
 data=json.loads((p/'runtime.json').read_text());v=data['rudderPivot'];pivot=Vector((v[2],-v[0],v[1]))
 for o in objects:
  if o.name in ['Steering oar / single carved oak','Steering oar single carved oak','Carved stern rudder','Raised bent tiller']:
   for angle in [-8,0,8]:
    m=Matrix.Translation(pivot)@Matrix.Rotation(math.radians(angle),4,'Z')@Matrix.Translation(-pivot)
    t=tree(o,m)
    for name,h in hull:assert not t.overlap(h),(kind,o.name,angle,'clips',name)
 sails=[(o.name,tree(o)) for o in objects if any(x in o.name.lower() for x in ['sewn wool panel','square sail panel','lateen mainsail','red sailing cloth']) and 'seam' not in o.name and 'rope' not in o.name]
 for o in objects:
  if o.name in ['Mast','Raked mast','Light mast'] or o.name.startswith(('Mast shroud','Forestay','Backstay')):
   t=tree(o)
   for name,s in sails:assert not t.overlap(s),(kind,o.name,'clips',name)
 print('PASS:',kind,'rudder clearance at -8/0/+8 degrees; mast and standing rigging clear of sail.',flush=True)
