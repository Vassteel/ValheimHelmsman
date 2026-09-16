"""Pack the authored JSON mesh into the versioned runtime format used by GullcallModel."""
from pathlib import Path
import json,struct
root=Path(__file__).resolve().parents[1]/'assets/gullcall'
parts=json.loads((root/'model.json').read_text())['parts']
with (root/'model.bin').open('wb') as f:
 f.write(struct.pack('<Ii',0x314C5547,len(parts)))
 for part in parts:
  name=part['name'].encode();f.write(struct.pack('<H',len(name)));f.write(name)
  f.write(struct.pack('<4f',*(part['color'][k] for k in 'rgba')))
  f.write(struct.pack('<i',len(part['vertices'])))
  for v in part['vertices']:f.write(struct.pack('<3f',*(v[k] for k in 'xyz')))
  f.write(struct.pack('<i',len(part['triangles'])))
  f.write(struct.pack('<'+str(len(part['triangles']))+'i',*part['triangles']))
print('Packed',len(parts),'Gullcall parts into model.bin')
