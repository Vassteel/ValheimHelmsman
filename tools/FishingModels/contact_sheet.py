"""Compose the retained equipment renders; no Blender re-render is needed."""
from pathlib import Path
import json
import sys
from PIL import Image, ImageDraw, ImageFont
version=sys.argv[1] if len(sys.argv)>1 else 'v2'
if version not in ('v1','v2'):raise ValueError('Expected v1 or v2')
root=Path(__file__).resolve().parents[2]/('design/fishing-equipment-'+version)
items=json.loads((root/'models.json').read_text())
font=ImageFont.truetype('/usr/share/fonts/noto/NotoSans-Regular.ttf',25)
im=Image.new('RGB',(1200,1120),'#243640');d=ImageDraw.Draw(im)
for i,m in enumerate(items):
 x=(i%2)*600;y=(i//2)*520
 pic=Image.open(root/m['id']/(m['id']+'-review.png')).convert('RGB');pic.thumbnail((588,464))
 im.paste(pic,(x+(600-pic.width)//2,y+46));d.text((x+15,y+8),m['name'],font=font,fill='#f5e2bf')
d.text((20,1060),'Helmsman • fishing equipment • static models for review',font=font,fill='#d7e4e9')
im.save(root/'fishing-equipment-review.png')
