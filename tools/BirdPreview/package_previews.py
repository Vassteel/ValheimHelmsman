"""Encode review clips and first-pass audio with portable codecs."""
import os
if hasattr(os,'sched_getaffinity'):os.sched_setaffinity(0,set(sorted(os.sched_getaffinity(0))[:2]))
import json,wave,subprocess,struct,math,os
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[2];P=R/os.environ.get('PUFFIN_PREVIEW_DIR','design/puffin-animation-v1');S=R/'design/slipway-animation-v1';rate=22050
font=ImageFont.truetype('/usr/share/fonts/TTF/DejaVuSans.ttf',18)
def read(name):
 with wave.open(str(P/(name+'.wav')),'rb') as f:return list(struct.unpack('<'+'h'*f.getnframes(),f.readframes(f.getnframes())))
def write(path,seconds,cues,fade_at=None):
 samples=[0]*int(seconds*rate)
 for t,name,gain in cues:
  for j,v in enumerate(read(name)):
   i=int(t*rate)+j
   if i<len(samples):samples[i]+=int(v*gain)
 if fade_at is not None:
  for i in range(int(fade_at*rate),len(samples)):samples[i]=int(samples[i]*max(0,1-(i/rate-fade_at)/.35))
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(struct.pack('<'+'h'*len(samples),*(max(-32767,min(32767,v)) for v in samples)))
def encode(folder,labels,seconds,audio,filename,source_indices=None):
 out=folder/'captioned';out.mkdir(exist_ok=True)
 for i,label in enumerate(labels):
  src=folder/'frames'/f'{source_indices[i] if source_indices is not None else i:04}.png';im=Image.open(src).convert('RGB');d=ImageDraw.Draw(im);d.rectangle((0,0,im.width,56),fill=(18,31,37));d.text((16,8),label,font=font,fill=(232,237,233));d.text((16,31),'Production model preview • Blender lighting',font=font.font_variant(size=13),fill=(163,184,190));im.save(out/f'{i:04}.png')
 silent=os.environ.get('PREVIEW_SILENT')=='1'
 common=['ffmpeg','-y','-loglevel','error','-framerate','12','-i',str(out/'%04d.png')]+(['-an'] if silent else ['-i',str(audio)])+['-t',str(seconds)]
 subprocess.run(common+['-threads','2','-c:v','libx264','-preset','fast','-crf','22','-pix_fmt','yuv420p','-c:a','aac','-b:a','128k','-movflags','+faststart',str(folder/(filename+'.mp4'))],check=True)
 subprocess.run(common+['-threads','2','-c:v','libvpx-vp9','-deadline','realtime','-cpu-used','6','-crf','33','-b:v','0','-pix_fmt','yuv420p','-c:a','libopus',str(folder/(filename+'.webm'))],check=True)
 # Visual QA sheet: all chapters / phases in a single artifact.
 indices=[26,72,120,168,216,258] if filename=="puffin-performance" else [0,90,168,216,264,359] if filename in ('slipway-sequence-30s','slipway-circus-30s') else [0,30,72,108,156,239];sheet=Image.new('RGB',(960,720),(18,31,37))
 for k,i in enumerate(indices):
  im=Image.open(out/f'{i:04}.png');im.thumbnail((480,240));sheet.paste(im,(k%2*480+(480-im.width)//2,k//2*240))
 sheet.save(folder/'review-sheet.jpg',quality=93)
mode=__import__('sys').argv[1] if len(__import__('sys').argv)>1 else 'puffin'
if mode=='puffin':
 cues=[(.15,'PuffinGreeting',.6)]
 for chapter,(sound,period) in enumerate([('Hammer',1.45),('Scrape',2.1),('Rope',1.8),('Scrape',2.6)]):
  t=.74
  while t<4:cues.append((chapter*4+t,sound,.55));t+=period
 for i in range(12):cues.append((16+i*.32,'Footstep',.18))
 write(P/'puffin-performance.wav',24,cues);data=json.loads((P/'animation.json').read_text());encode(P,[f['label'] for f in data['frames']],24,P/'puffin-performance.wav','puffin-performance')
 demo=[(0,'PuffinGreeting',.7),(1.4,'PuffinMurmur',.7),(3.1,'Hammer',.7),(4.1,'Scrape',.7),(5.2,'Rope',.7),(6.5,'Creak',.7),(8.2,'WoodSlide',.7)]
 write(P/'shipyard-audio-samples.wav',10.2,demo)
elif mode=='circus':
 folder=R/os.environ.get('CIRCUS_REVIEW_DIR','design/slipway-circus-v1')
 cues=[(t,'Hammer',.28) for t in [5.5,6.2,7,7.7,8.5,9.2,10,10.7,11.5]]+[(6,'Scrape',.4),(8.2,'Scrape',.4),(10.4,'Scrape',.4),(7.5,'Rope',.4),(10.2,'Rope',.4)]
 cues += [(t,'WoodSlide',.7) for t in [13.4,17.45,22,26.05]]
 cues += [(t,'Creak'+(str(i%4) if i%4 else ''),.45+(i%3)*.08) for i,t in enumerate([13.45,14.4,15.6,16.4,17.5,18.4,19.5,20.7,22.1,23.3,24.1,25.4,26.2])]
 cues += [(t,'Rope',.25) for t in [22.5,24.2,25.8]]
 if os.environ.get('PREVIEW_SILENT')!='1':write(folder/'slipway-review.wav',30,cues,fade_at=27.65)
 labels=['Crew arriving • construction waiting' if i<102 else 'Construction • circus at work' if i<144 else 'Launching • crew departing' if i<240 else 'Water entry • no added splash' if i<264 else 'Reset • hauling the cradle home' if i<336 else 'Reset complete' for i in range(360)]
 encode(folder,labels,30,folder/'slipway-review.wav','slipway-circus-30s')
elif mode in ('slipway30','slipway-stages'):
 # Twelve seconds of staged assembly, native 10-second launch and 6-second
 # reset, then two seconds showing the fully returned cradle. Reuse identical
 # production scene frames; only construction is retimed.
 folder=R/('design/slipway-stages-30s' if mode=='slipway-stages' else 'design/slipway-animation-30s');folder.mkdir(exist_ok=True)
 frames=folder/'frames'
 if not frames.exists():frames.symlink_to((R/'design/slipway-stages-source' if mode=='slipway-stages' else S)/'frames',target_is_directory=True)
 source_indices=[i//3 for i in range(144)]+list(range(48,240))+[240]*24
 labels=['Construction • accelerated for preview' if i<144 else 'Brake release' if i<161 else 'Launching • cradle and capstan' if i<240 else 'Water entry' if i<264 else 'Reset • hauling the cradle home' if i<336 else 'Reset complete • ready for the next ship' for i in range(360)]
 if mode=='slipway-stages':
  starts=json.loads((P/'construction-stages.json').read_text())
  stages=[('Hull and frames',0),('Main deck',starts['deck']),('Mast and spars',starts['mast']),('Standing rigging',starts['rigging']),('Sails',starts['sail']),('Cargo and finishing',starts['cargo'])]
  for i in range(144):labels[i]=next(name for name,start in reversed(stages) if i/144>=start)+' • accelerated construction'
 cues=[(t,'Hammer',.5) for t in [.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5]]+[(8.5,'Scrape',.5),(10,'Rope',.6),(13.4,'Creak',.7),(22,'Creak',.6)]+[(t,'Rope',.5) for t in [14,15.3,16.6,17.9,19.2,22.5,23.6,24.7,25.8,26.9]]
 cues += [(t,'WoodSlide',.65) for t in [13.4,17.45,22,26.05]]
 write(folder/'slipway-review.wav',30,cues)
 encode(folder,labels,30,folder/'slipway-review.wav','slipway-sequence-30s',source_indices)
else:
 cues=[(.4,'Hammer',.5),(1.2,'Hammer',.5),(2,'Hammer',.5),(3,'Rope',.6),(5.4,'Creak',.7),(14,'Creak',.6)]+[(t,'Rope',.5) for t in [6,7.3,8.6,9.9,11.2,14.5,15.6,16.7,17.8,18.9]]
 cues += [(t,'WoodSlide',.65) for t in [5.4,9.45,14,18.05]]
 write(S/'slipway-review.wav',20,cues);timeline=json.loads((P/'slipway-timeline.json').read_text());encode(S,['Construction • accelerated for preview' if p['time']<4 else 'Brake release' if p['time']<5.4 else 'Launching • cradle and capstan' if p['time']<12 else 'Water entry' if p['time']<14 else 'Reset • hauling the cradle home' for p in timeline],20,S/'slipway-review.wav','slipway-sequence')
print('Encoded and captioned',mode,'review clips')
