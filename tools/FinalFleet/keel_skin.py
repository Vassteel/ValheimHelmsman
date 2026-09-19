"""Fitted, closed skin bridge over the Currach's 24 mm centreline slot.
Blender coordinates: X along the keel, Y across, Z up. Follow the authored
65-station bottom profile; a small overlap avoids cracks at the skin join.
"""
def geometry(length=5.5):
 half=length/2;vertices=[];faces=[]
 for i in range(65):
  x=-half+length*i/64;z=.015+.12*abs(x/half)**4
  vertices.extend([(x,-.018,z+.002),(x,.018,z+.002),(x,.018,z-.010),(x,-.018,z-.010)])
 for i in range(64):
  a=i*4;b=a+4
  for j in range(4):faces.append((a+j,b+j,b+(j+1)%4,a+(j+1)%4))
 faces.extend([(3,2,1,0),(256,257,258,259)])
 return vertices,faces
