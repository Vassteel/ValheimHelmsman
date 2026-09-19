"""Arrange the rendered model views into a review sheet."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont
root=Path(__file__).resolve().parents[2]/'design/harbor-refinement-v3'
models=json.loads((root/'models.json').read_text())
sheet=Image.new('RGB',(1600,850),'#243640');draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('/usr/share/fonts/noto/NotoSans-Regular.ttf',21) if Path('/usr/share/fonts/noto/NotoSans-Regular.ttf').exists() else ImageFont.load_default()
for i,item in enumerate(models):
 im=Image.open(root/item['id']/(item['id']+'-review.png')).convert('RGB');im.thumbnail((390,350))
 x=(i%4)*400;y=(i//4)*400
 sheet.paste(im,(x+(400-im.width)//2,y+38))
 draw.text((x+16,y+12),item['name'],font=font,fill='#f5e2bf')
draw.text((16,810),'Original harbor models • review only • Helmsman',font=font,fill='#d7e4e9')
sheet.save(root/'harbor-review.png')
