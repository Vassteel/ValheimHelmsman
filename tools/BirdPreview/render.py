"""Orthographic depth-buffer render of production triangle geometry (not Unity)."""
import json,sys,math
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
src=Path(sys.argv[1]);scenes=json.loads(src.read_text());W,H=540,640
image=Image.new('RGB',(W*len(scenes),H),(39,43,43))
yaw=.38;el=.10
camera=np.array([math.sin(yaw)*math.cos(el),math.sin(el),math.cos(yaw)*math.cos(el)])
right=np.array([math.cos(yaw),0,-math.sin(yaw)]);up=np.cross(camera,right)
light=np.array([-.5,1,1]);light/=np.linalg.norm(light)
for j,scene in enumerate(scenes):
 pixels=np.full((H,W,3),(39,43,43),dtype=np.uint8);depth=np.full((H,W),-np.inf)
 for part in scene['parts']:
  vertices=np.asarray(part['vertices']);idx=np.asarray(part['triangles']).reshape(-1,3)
  for t in vertices[idx]:
   normal=np.cross(t[1]-t[0],t[2]-t[0]);length=np.linalg.norm(normal)
   if length<1e-10:continue
   normal/=length
   if normal@camera<=0:continue
   shade=.48+.52*max(0,normal@light);color=np.array([int(max(0,min(1,c*shade))*255) for c in part['color']],dtype=np.uint8)
   p=np.column_stack((W/2+(t@right)*415,H-65-(t@up)*415));z=t@camera
   x0=max(0,int(np.floor(p[:,0].min())));x1=min(W-1,int(np.ceil(p[:,0].max())))
   y0=max(0,int(np.floor(p[:,1].min())));y1=min(H-1,int(np.ceil(p[:,1].max())))
   if x1<x0 or y1<y0:continue
   x,y=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
   a,b,c=p;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
   if abs(den)<1e-10:continue
   u=((b[1]-c[1])*(x-c[0])+(c[0]-b[0])*(y-c[1]))/den
   v=((c[1]-a[1])*(x-c[0])+(a[0]-c[0])*(y-c[1]))/den;w=1-u-v;d=u*z[0]+v*z[1]+w*z[2]
   region=depth[y0:y1+1,x0:x1+1];mask=(u>=0)&(v>=0)&(w>=0)&(d>region)
   region[mask]=d[mask];pixels[y0:y1+1,x0:x1+1][mask]=color
 image.paste(Image.fromarray(pixels),(j*W,0))
 ImageDraw.Draw(image).text((j*W+22,20),scene['name'].replace('—','-'),fill=(225,217,198))
image.save(src.with_suffix('.png'));print(src.with_suffix('.png'))
