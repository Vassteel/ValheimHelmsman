"""Ray-check the rendered Currach skin from inside and underneath its centreline."""
import gzip,io,struct,json,sys,math
from pathlib import Path
def check(path):
 f=io.BytesIO(gzip.decompress(Path(path).read_bytes()))
 def I():return struct.unpack('<i',f.read(4))[0]
 def raw():return f.read(I())
 assert f.read(4)==b'HMF1';spec=json.loads(raw())
 for _ in range(I()):raw();raw();f.read(16)
 faces=[]
 for _ in range(I()):
  tag=raw().decode();I();vs=[struct.unpack('<8f',f.read(32)) for _ in range(I())];nt=I();ts=struct.unpack('<'+'i'*nt,f.read(nt*4))
  if tag=='hull':faces.extend(tuple(vs[t] for t in ts[i:i+3]) for i in range(0,nt,3))
 half=spec['length']/2
 for i in range(1,64):
  z=-half+spec['length']*(i-.37)/64
  expected=.015+.12*abs(z/half)**4
  for x in [-.009,0,.009]:
   top=bottom=False
   for a,b,c in faces:
    if not min(a[0],b[0],c[0])-1e-8<=x<=max(a[0],b[0],c[0])+1e-8:continue
    if not min(a[2],b[2],c[2])-1e-8<=z<=max(a[2],b[2],c[2])+1e-8:continue
    dx,dz=b[0]-a[0],b[2]-a[2];ex,ez=c[0]-a[0],c[2]-a[2];den=dx*ez-dz*ex
    if abs(den)<1e-12:continue
    u=((x-a[0])*ez-(z-a[2])*ex)/den;v=(dx*(z-a[2])-dz*(x-a[0]))/den
    if u< -1e-6 or v< -1e-6 or u+v>1+1e-6:continue
    y=a[1]+u*(b[1]-a[1])+v*(c[1]-a[1])
    if abs(y-(expected+.002))<.001 and -den>0:top=True
    if abs(y-(expected-.010))<.001 and -den<0:bottom=True
   assert top and bottom,(Path(path).name,'Open keel skin',x,z,'inside',top,'underside',bottom)
 print('PASS: Currach keel skin closes 189 sampled rays from both sides, with outward winding.')
if __name__=='__main__':check(sys.argv[1] if len(sys.argv)>1 else Path(__file__).resolve().parents[2]/'assets/ships/final/HelmsmanCurrach.bin.gz')
