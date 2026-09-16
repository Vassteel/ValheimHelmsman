"""Render actual exported triangle geometry, not a concept image or a game screenshot."""
import json,sys,math
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
src=Path(sys.argv[1]);scenes=json.loads(src.read_text());W,H=540,640
image=Image.new('RGB',(W*len(scenes),H),(39,43,43));draw=ImageDraw.Draw(image)
yaw=.38;el=.10
camera=np.array([math.sin(yaw)*math.cos(el),math.sin(el),math.cos(yaw)*math.cos(el)])
right=np.array([math.cos(yaw),0,-math.sin(yaw)]);up=np.cross(camera,right)
light=np.array([-.5,1,1]);light/=np.linalg.norm(light)
for j,scene in enumerate(scenes):
 tris=[]
 for part in scene['parts']:
  vertices=np.asarray(part['vertices']);idx=np.asarray(part['triangles']).reshape(-1,3)
  for t in vertices[idx]:
   normal=np.cross(t[1]-t[0],t[2]-t[0]);length=np.linalg.norm(normal)
   if length<1e-10:continue
   normal/=length
   if normal@camera<=0:continue
   shade=.48+.52*max(0,normal@light)
   color=tuple(int(max(0,min(1,c*shade))*255) for c in part['color'])
   points=[(j*W+W/2+(p@right)*415,H-65-(p@up)*415) for p in t]
   tris.append((t.mean(axis=0)@camera,points,color))
 for depth,points,color in sorted(tris,key=lambda t:t[0]):draw.polygon(points,fill=color)
 draw.text((j*W+22,20),scene['name'].replace('—','-'),fill=(225,217,198))
image.save(src.with_suffix('.png'));print(src.with_suffix('.png'))
