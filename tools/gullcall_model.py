"""Author the original faceted Gullcall mesh and render a transparent inventory icon.
No game assets are redistributed. Runtime and preview use the same triangle data.
"""
import json, math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
parts=[]
def part(name,color):
 p=dict(name=name,color=dict(zip('rgba',(*color,1))),vertices=[],triangles=[]);parts.append(p);return p
def tri(p,a,b,c):
 n=len(p['vertices']);p['vertices'] += [dict(zip('xyz',v)) for v in (a,b,c)];p['triangles'] += [n,n+1,n+2]
def ellipsoid(p,center,size,n=10,rings=6):
 def v(i,j):
  a=2*math.pi*i/n;e=math.pi*j/rings
  return (center[0]+size[0]*math.sin(e)*math.cos(a),center[1]+size[1]*math.cos(e),center[2]+size[2]*math.sin(e)*math.sin(a))
 for j in range(rings):
  for i in range(n):
   a,b,c,d=v(i,j),v(i+1,j),v(i,j+1),v(i+1,j+1)
   if j:tri(p,a,b,c)
   if j<rings-1:tri(p,b,d,c)
def box(p,c,s):
 vs=[(c[0]+x*s[0]/2,c[1]+y*s[1]/2,c[2]+z*s[2]/2) for x,y,z in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
 for a,b,c,d in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:tri(p,vs[a],vs[b],vs[c]);tri(p,vs[a],vs[c],vs[d])
def tube(p,points,r=.003,n=6):
 import numpy as np
 vs=[]
 for i,point in enumerate(points):
  tangent=np.array(points[min(i+1,len(points)-1)])-np.array(points[max(0,i-1)])
  tangent/=np.linalg.norm(tangent)
  ref=np.array([0.,0.,1.]);u=np.cross(tangent,ref)
  if np.linalg.norm(u)<.01:u=np.cross(tangent,[0.,1.,0.])
  u/=np.linalg.norm(u);v=np.cross(tangent,u)
  vs.append([tuple(np.array(point)+r*(math.cos(2*math.pi*j/n)*u+math.sin(2*math.pi*j/n)*v)) for j in range(n)])
 for i in range(len(vs)-1):
  for j in range(n):
   a,b,c,d=vs[i][j],vs[i][(j+1)%n],vs[i+1][j],vs[i+1][(j+1)%n]
   tri(p,a,b,c);tri(p,b,d,c)
 # close ends
 for j in range(n):tri(p,points[0],vs[0][(j+1)%n],vs[0][j]);tri(p,points[-1],vs[-1][j],vs[-1][(j+1)%n])
bone=part('Carved bone gull',(.79,.73,.57));ellipsoid(bone,(-.012,.014,0),(.075,.031,.025));ellipsoid(bone,(-.061,.043,0),(.024,.025,.022))
wood=part('Wooden wing inlays and mouthpiece',(.27,.13,.055))
for side in [-1,1]:ellipsoid(wood,(.012,.018,side*.020),(.049,.017,.008),8,4)
box(wood,(.081,.012,0),(.038,.018,.022))
beak=part('Carved beak',(.62,.40,.14))
base=[(-.077,.051,-.010),(-.077,.051,.010),(-.077,.037,.010),(-.077,.037,-.010)];tip=(-.118,.040,0)
for i in range(4):tri(beak,base[i],tip,base[(i+1)%4])
tri(beak,base[0],base[2],base[3]);tri(beak,base[0],base[1],base[2])
dark=part('Inset eyes and whistle openings',(.035,.025,.018))
for side in [-1,1]:ellipsoid(dark,(-.064,.050,side*.0205),(.0035,.0035,.002),8,4)
box(dark,(.1002,.012,0),(.001,.005,.012))
box(dark,(.048,.037,0),(.015,.001,.009))
leather=part('Leather wraps and cord',(.19,.085,.035))
for x in [.055,.061]:tube(leather,[(x,.012+.013*math.cos(2*math.pi*i/16),.015*math.sin(2*math.pi*i/16)) for i in range(17)],.002)
feather=part('Two gull feather ties',(.70,.70,.65))
for dx in [0,.017]:
 c=(.056+dx,-.025,.008)
 v=[(c[0],c[1]+.013,c[2]),(c[0]-.010,c[1]-.012,c[2]),(c[0]-.004,c[1]-.036,c[2]),(c[0]+.007,c[1]-.017,c[2]),(c[0],c[1]-.012,c[2]+.003)]
 for a,b in [(0,1),(1,2),(2,3),(3,0)]:tri(feather,v[a],v[b],v[4]);tri(feather,v[b],v[a],(c[0],c[1]-.012,c[2]-.002))
 tube(leather,[(.059+dx,-.001,.008),(c[0],c[1]+.005,.008)],.0015)
for p in parts:
 for v in p['vertices']:
  for k in v:v[k]=round(v[k],7)
asset=root/'assets/gullcall/model.json';asset.write_text(json.dumps(dict(parts=parts),separators=(',',':'))+'\n')
# Export an inspectable standard OBJ with material colors.
obj=['mtllib gullcall.mtl'];mtl=[];offset=1
for i,p in enumerate(parts):
 obj+=['o '+p['name'].replace(' ','_'),'usemtl m'+str(i)]
 obj += ['v '+' '.join(str(v[k]) for k in 'xyz') for v in p['vertices']]
 obj += ['f '+' '.join(str(offset+j) for j in p['triangles'][t:t+3]) for t in range(0,len(p['triangles']),3)]
 offset+=len(p['vertices']);mtl+=['newmtl m'+str(i),'Kd '+' '.join(str(p['color'][k]) for k in 'rgb')]
(root/'assets/gullcall/gullcall.obj').write_text('\n'.join(obj)+'\n');(root/'assets/gullcall/gullcall.mtl').write_text('\n'.join(mtl)+'\n')
# Deterministic orthographic mesh render, opaque triangles over transparent pixels.
import numpy as np
from PIL import Image
size=1024;rgba=np.zeros((size,size,4),dtype=np.uint8);depth=np.full((size,size),-np.inf)
view=np.array([-.28,.25,1.]);view/=np.linalg.norm(view);right=np.cross([0,1,0],view);right/=np.linalg.norm(right);up=np.cross(view,right)
allv=np.array([[v[k] for k in 'xyz'] for p in parts for v in p['vertices']]);xy=np.stack((allv@right,allv@up),axis=1);center=(xy.max(0)+xy.min(0))/2;scale=size*.86/max(np.ptp(xy,axis=0))
light=np.array([-.5,.9,1.]);light/=np.linalg.norm(light)
for p in parts:
 verts=np.array([[v[k] for k in 'xyz'] for v in p['vertices']]);projected=np.stack(((verts@right-center[0])*scale+size/2,size/2-(verts@up-center[1])*scale,verts@view),axis=1)
 for ids in np.array(p['triangles']).reshape(-1,3):
  a,b,c=projected[ids];n=np.cross(verts[ids[1]]-verts[ids[0]],verts[ids[2]]-verts[ids[0]]);length=np.linalg.norm(n)
  if length<1e-12:raise ValueError('Degenerate triangle')
  n/=length
  # Render both sides for asset inspection; runtime normals/winding are validated below.
  illumination=.40+.60*max(0,float(n@light));color=np.array([p['color'][k] for k in 'rgb']);color=np.clip(color*illumination*1.3,0,1)
  lo=np.maximum(np.floor(np.min([a[:2],b[:2],c[:2]],axis=0)).astype(int),0);hi=np.minimum(np.ceil(np.max([a[:2],b[:2],c[:2]],axis=0)).astype(int),size-1)
  if np.any(hi<lo):continue
  y,x=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
  if abs(den)<1e-10:continue
  wa=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den;wb=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;wc=1-wa-wb
  z=wa*a[2]+wb*b[2]+wc*c[2];sl=np.s_[lo[1]:hi[1]+1,lo[0]:hi[0]+1];mask=(wa>=0)&(wb>=0)&(wc>=0)&(z>depth[sl]);depth[sl][mask]=z[mask];rgba[sl][mask]=[*np.round(color*255).astype(int),255]
im=Image.fromarray(rgba)
im.resize((128,128),Image.Resampling.LANCZOS).save(root/'assets/gullcall/icon.png')
im.resize((512,512),Image.Resampling.LANCZOS).save(root/'assets/gullcall/preview.png')
print(f'{len(parts)} mesh parts; {sum(len(p["triangles"])//3 for p in parts)} triangles; bounds {allv.min(0)} to {allv.max(0)}; icon 128x128 RGBA')
