from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[2]/'design/maritime-markers-v2'
items=json.loads((root/'models.json').read_text());font=ImageFont.truetype('/usr/share/fonts/noto/NotoSans-Regular.ttf',24)
sheet=Image.new('RGB',(1800,720),'#243640');d=ImageDraw.Draw(sheet)
for i,m in enumerate(items):
 pic=Image.open(root/m['id']/(m['id']+'-review.png')).convert('RGB')
 # These slender posts have generous studio margins. Crop equally before making the comparison sheet.
 pic=pic.crop((210,50,990,960));pic.thumbnail((438,605))
 x=i*450;sheet.paste(pic,(x+(450-pic.width)//2,49));d.text((x+14,10),m['name'],font=font,fill='#f5e2bf')
d.text((16,677),'Helmsman • original carved marker studies • second visual pass',font=font,fill='#d7e4e9')
sheet.save(root/'maritime-markers-review.png')
