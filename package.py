"""Build separate local-import and public Thunderstore archives."""
from pathlib import Path
import hashlib,json,struct,zipfile
root=Path(__file__).resolve().parent
manifest=json.loads((root/'packaging/manifest.json').read_text())
version=manifest['version_number']
files={
 'README.md':root/'packaging/README.md',
 'icon.png':root/'dist/ValheimHelmsman/icon.png',
 'LICENSE.txt':root/'LICENSE.txt',
 'BepInEx/plugins/ValheimHelmsman/ValheimHelmsman.dll':root/'dist/ValheimHelmsman/ValheimHelmsman.dll',
 'BepInEx/plugins/ValheimHelmsman/Helmsman.Core.dll':root/'dist/ValheimHelmsman/Helmsman.Core.dll',
}
public={key:manifest[key] for key in ('name','version_number','website_url','description','dependencies')}
assert len(public['description'])<=250
for filename,metadata in [(f'Local-ValheimHelmsman-{version}.zip',manifest),(f'ValheimHelmsman-{version}-thunderstore.zip',public)]:
 out=root/'dist'/filename
 with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED) as z:
  z.writestr('manifest.json',json.dumps(metadata,indent=2)+'\n')
  for name,path in files.items():z.write(path,name)
 with zipfile.ZipFile(out) as z:
  assert z.testzip() is None
  assert json.loads(z.read('manifest.json'))==metadata
  assert struct.unpack('>II',z.read('icon.png')[16:24])==(256,256)
  for name,path in files.items():assert z.read(name)==path.read_bytes()
  assert len([name for name in z.namelist() if name.endswith('.dll')])==2
 out.with_suffix('.zip.sha256').write_text(hashlib.sha256(out.read_bytes()).hexdigest()+'  '+out.name+'\n')
 print(out)
