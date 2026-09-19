"""Inventory shipped geometry and prepare the review of this optimization pass."""
import csv,gzip,io,json,struct,html
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
R=Path(__file__).resolve().parents[2];D=R/'design/asset-optimization-v1';H=R/'design/harbor-runtime-v1'
def packed(path):
 f=io.BytesIO(gzip.decompress(path.read_bytes()))
 def I():return struct.unpack('<i',f.read(4))[0]
 def raw():return f.read(I())
 magic=f.read(4);spec=json.loads(raw()) if magic in (b'HMF1',b'HMW3') else {}
 materials=I()
 for _ in range(materials):
  raw()
  if magic==b'HMF1':raw()
  f.read(16)
 batches=I();triangles=vertices=0
 for _ in range(batches):
  if magic!=b'HMW1':raw()
  I();n=I();vertices+=n;f.read(n*(36 if magic==b'HMW3' else 32));n=I();triangles+=n//3;f.read(n*4)
 return triangles,vertices,batches,spec
old={r['asset']:r for r in json.loads((D/'optimization.json').read_text())};harbor=json.loads((H/'models.json').read_text());hb={r['id']:r for r in harbor}
names={'BigCargoShip':'Big cargo ship','MercantShip':'Ottar','HerculeShip':'Falkuša','LittleBoat':'Ceol','WarShip':'Snekkja','HelmsmanCurrach':'Currach','HelmsmanDugout':'Dugout','HelmsmanFinewoodKayak':'Finewood kayak','HelmsmanTandemKayak':'Tandem finewood kayak','fishing-dock':'Pelican workstation'}
for p in [R/'design/workshop-supplies-v1/models.json']:
 for r in json.loads(p.read_text()):names.setdefault(r['id'],r['name'])
rows=[]
for directory,group in [('assets/ships/final','Fleet'),('assets/workshop','Workshop and supplies')]:
 for p in sorted((R/directory).glob('*.bin.gz')):
  key=p.name.removesuffix('.bin.gz');t,v,b,spec=packed(p);baseline=old.get(p.name,{}).get('before',t);state='Integrated; local test pending'
  if key.startswith('harbor-'):
   h=hb[key[7:]];name=h['name'];baseline=h['before'];kind='Harbor equipment'
  else:name=names.get(key,key.replace('-',' ').capitalize());kind=group
  rows.append(dict(name=name,category=kind,before=baseline,triangles=t,vertices=v,batches=b,status=state,source=str(p.relative_to(R))))
for phase in ['before','after']:
 scenes=json.loads((D/f'birds-{phase}.json').read_text())
 for s in scenes[::2]:
  name=s['name'].split(' —')[0];t=sum(len(p['triangles'])//3 for p in s['parts']);v=sum(len(p['vertices']) for p in s['parts'])
  if phase=='before':rows.append(dict(name=name,category='Birds',before=t,triangles=t,vertices=v,batches=len(s['parts']),status='Integrated; all generated parts counted',source='src/Helmsman/Art/PerchedBird.cs'))
  else:next(r for r in rows if r['name']==name).update(triangles=t,vertices=v)
s=json.loads((D/'gull-audit.json').read_text())[0];t=sum(len(p['triangles'])//3 for p in s['parts'])
rows.append(dict(name='Scout gull with helmet (perched)',category='Birds',before=t,triangles=t,vertices=sum(len(p['vertices']) for p in s['parts']),batches=len(s['parts']),status='Native base refined at runtime; perched instance counted',source='tools/GullPreview/Program.cs'))
rows.append(dict(name='Gullcall whistle',category='Supplies',before=906,triangles=906,vertices='',batches='',status='Unchanged; already inexpensive',source='assets/gullcall/model.json'))
for s in json.loads(gzip.open(R/'output/model-assessment/redesigned.json.gz').read()):
 t=sum(len(p['triangles'])//3 for p in s['parts']);rows.append(dict(name=s['name'],category='Remaining imported hierarchy',before=t,triangles=t,vertices='',batches=len(s['parts']),status='Current visible replacement; original hierarchy still retained',source='assets/ships/redesign/models.bin.gz'))
for category,path in [('Fishing studies','fishing-equipment-v3'),('Shelved markers','maritime-markers-v2')]:
 for s in json.loads((R/'design'/path/'models.json').read_text()):
  rows.append(dict(name=s['name'],category=category,before=s.get('before',s['triangles']),triangles=s['triangles'],vertices='',batches='',status='Review only; not shipped' if category=='Fishing studies' else 'Shelved; not shipped',source=f'design/{path}/{s["id"]}/{s["id"]}.blend'))
(D/'asset-audit.json').write_text(json.dumps(rows,indent=2,ensure_ascii=False)+'\n')
with (D/'asset-audit.csv').open('w') as f:
 w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
fontpath='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
if not Path(fontpath).exists():fontpath='/usr/share/fonts/noto/NotoSans-Regular.ttf'
font=ImageFont.truetype(fontpath,19);small=ImageFont.truetype(fontpath,16)
def sheet(cards,path,cols=4):
 width=400;height=365;canvas=Image.new('RGB',(width*cols,height*((len(cards)+cols-1)//cols)), '#223641');d=ImageDraw.Draw(canvas)
 for i,(label,subtitle,img) in enumerate(cards):
  x=(i%cols)*width;y=(i//cols)*height;d.text((x+12,y+9),label,font=font,fill='#f5e3c2');d.text((x+12,y+36),subtitle,font=small,fill='#c7d5dc');im=Image.open(img).convert('RGB');im.thumbnail((width-8,height-64));canvas.paste(im,(x+(width-im.width)//2,y+62))
 canvas.save(path)
sheet([(r['name'],f"{r['before']:,} to {r['triangles']:,} triangles",H/r['id']/'runtime-review.png') for r in harbor],H/'harbor-review.png')
sheet([(names.get(r['asset'].removesuffix('.bin.gz'),r['asset']),f"{r['before']:,} to {r['triangles']:,} triangles",D/r['asset'].removesuffix('.bin.gz')/'after.png') for r in old.values() if r['source'].startswith('assets/ships/')],D/'fleet-review.png',3)
sheet([(label,f'{t} seconds',H/'heavy-crane'/f'pose-{t:02}.png') for label,t in [('Rest',0),('Raised',4),('Lowering',8),('Reset',12)]],H/'crane-cycle.png')
changed=[r for r in rows if r['before']!=r['triangles']];before=sum(r['before'] for r in changed);after=sum(r['triangles'] for r in changed)
text=f'''# Helmsman asset review — 18 September 2026

The 5k–20k triangle range is a flexible guide. Simple items stay below it; larger ships and the puffin retain exceptions. Counts are triangles per complete asset, not per submesh. This is a geometry audit, not an FPS benchmark.

Across the {len(changed)} changed designs, triangles fell from **{before:,} to {after:,} ({100*(1-after/before):.1f}% less)**. This total mixes shipped models and four review studies; it is not an estimate of a scene's rendering load. Unchanged eel study is excluded from that saving.

## Integration

Eight harbor pieces use original models and native Valheim piece components. The keel cradle is static. The rollers, gantry, two cranes and three pulley sets have interactive, network-time-driven 12-second operating cycles. Hoists raise, hold, lower and reset; rollers rotate and return. These are cosmetic operating cycles: they do not attach or transport ships or cargo. Static collision stays separate from moving tackle. Both cranes have six snap points.

The eight superseded imported roots and omitted dock extension are removed. The compacted source bundle retains only Enguias, Peixes, RedePesca and OilPress. Their replacement studies remain separate from runtime. Markers remain shelved. Sound work and pelican fishing flight remain deferred.

## Quality decisions

- Preserved all hull surfaces after an initial reduction opened a bow seam; the corrected renders were inspected again.
- Preserved sail, rudder and paddle geometry, hull/gameplay metadata, collision definitions and snap points byte for byte. Slipway haul rope topology is also unchanged.
- Kept timber joints, connected rope coils, baskets and individual cargo components. Connectivity and bounds checks found no lost disconnected pieces.
- Reduced overly dense fittings and fine puffin geometry while keeping major body and face resolution.
- Boats remain about 10.7k–80.8k triangles. The two cargo ships retain roughly 70k and 81k; forcing 20k would require a more extensive redesign or a separate distant LOD.
- Materials and draw calls are counted separately. Reduced triangles do not guarantee proportionate FPS gains; there has been no live GPU profile.

## Validation and limits

Full build and existing automated suite passed with zero compiler warnings/errors. Packed asset validation, production bird geometry checks, harbor motion checks, dependency/stream decoding checks and original-versus-reduced structural checks passed. Installed 0.2.40 locally in Steam and the Mods profile, with DLL hashes verified. Nothing published.

The renders use exact packed geometry with Blender studio lighting; they are not Unity/Valheim shader screenshots. Placement, collision cooking, operation from both clients, visual motion clearances and game lighting still need local acceptance. The scout gull figure is the perched model with helmet; its native flight mesh varies by game asset and is not included in this perched count. Native game-owned customization props and transient Unity primitive work tools are not independently remodeled or counted as new standalone assets. Each circus worker uses the puffin model below; simultaneous workers multiply that count.

## Reproduction

`tools/HarborModels/runtime_build.py` rebuilds the native harbor assets from the refined masters. `optimize_existing.py` stages conservative packed reductions in this folder using original binaries retained in `.build/asset-budget-baseline/`; it does not overwrite the editable fleet/workshop design masters. Preserve that baseline for identical before/after reproduction. `check_optimization.py` checks the staged result; promote reviewed files to their recorded `source` paths before rebuilding. `optimize_studies.py` produces the separate fishing v3 studies. Production puffin changes live in `src/Helmsman/Art/PerchedBird.cs`.

[Interactive comparison gallery](review.html) · [CSV audit](asset-audit.csv) · [JSON audit](asset-audit.json)

## Per-item counts

| Item | Group | Before | Now | Render batches | State |
|---|---|---:|---:|---:|---|
'''
for r in rows:text+=f"| {r['name']} | {r['category']} | {r['before']:,} | {r['triangles']:,} | {r['batches']} | {r['status']} |\n"
(D/'ASSET-REVIEW.md').write_text(text)
css='body{margin:0;background:#17252d;color:#e7e4dc;font:17px system-ui}main{max-width:1250px;margin:auto;padding:28px}h1,h2{color:#f3d7a5}a{color:#8edbd0}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(330px,1fr));gap:20px}.card{background:#263a44;padding:15px;border-radius:9px}img{width:100%;height:auto}button{padding:9px 14px;background:#d5ac6f;border:0;border-radius:5px;cursor:pointer}table{width:100%;border-collapse:collapse;font-size:14px}td,th{text-align:left;padding:9px;border-bottom:1px solid #43545b}p{line-height:1.6}.muted{color:#b3c6cf}'
page=f'<!doctype html><html><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Helmsman asset review</title><style>{css}</style><main><h1>Helmsman · asset review</h1><p>Flexible budgets, preserved hulls and working rigging. {100*(1-after/before):.1f}% fewer triangles across changed designs. <a href="ASSET-REVIEW.md">Full audit and limitations</a> · <a href="asset-audit.csv">Download counts</a></p><p class="muted">Studio previews of packed geometry; local game acceptance is still required. Nothing published.</p><h2>Integrated harbor equipment</h2><img src="../harbor-runtime-v1/harbor-review.png"><h2>Crane operating cycle</h2><p>Interactive cosmetic hoist, hold, lower and reset. No cargo or ship transport.</p><img src="../harbor-runtime-v1/crane-cycle.png"><h2>Before / after</h2><div class="grid">'
for i,r in enumerate(old.values()):
 key=r['asset'].removesuffix('.bin.gz');name=names.get(key,key.replace('-',' ').capitalize());suffix='-deck' if key in ('BigCargoShip','MercantShip','HelmsmanFinewoodKayak') else ''
 page+=f'<article class="card"><h3>{html.escape(name)}</h3><p>{r["before"]:,} to {r["triangles"]:,} triangles</p><img id="asset{i}" src="{key}/after{suffix}.png" alt="{html.escape(name)}"><button data-img="asset{i}" data-key="{key}" data-suffix="{suffix}" onclick="toggle(this)">Show before</button></article>'
page+='</div><h2>Puffin detail</h2><img src="puffin-comparison.png"><h2>Optimized fishing studies</h2><p>Review models only; these four are not integrated yet.</p><div class="grid">'
for r in json.loads((R/'design/fishing-equipment-v3/models.json').read_text()):page+=f'<article class="card"><h3>{r["name"]}</h3><p>{r["before"]:,} to {r["triangles"]:,} triangles</p><img src="../fishing-equipment-v3/{r["id"]}/{r["id"]}-review.png"></article>'
page+='</div><h2>Complete inventory</h2><table><tr><th>Item</th><th>Group</th><th>Before</th><th>Now</th></tr>'
for r in rows:page+=f'<tr><td>{html.escape(r["name"])}</td><td>{r["category"]}</td><td>{r["before"]:,}</td><td>{r["triangles"]:,}</td></tr>'
page+='</table></main><script>function toggle(b){const before=b.textContent==="Show before";document.getElementById(b.dataset.img).src=b.dataset.key+"/"+(before?"before":"after")+b.dataset.suffix+".png";b.textContent=before?"Show after":"Show before"}</script></html>'
(D/'review.html').write_text(page)
print(f'{len(rows)} asset rows; {len(changed)} changed designs; {before:,} -> {after:,} triangles; {100*(1-after/before):.1f}% reduction')
