"""Build the r2modman import ZIP from the current manifest, README and release DLLs."""
from pathlib import Path
import hashlib,json,struct,zipfile
root=Path(__file__).resolve().parent
manifest=json.loads((root/'packaging/manifest.json').read_text())
version=manifest['version_number']
out=root/'dist'/f'Local-ValheimHelmsman-{version}.zip'
files={
 'manifest.json':root/'packaging/manifest.json',
 'README.md':root/'packaging/README.md',
 'icon.png':root/'dist/ValheimHelmsman/icon.png',
 'BepInEx/plugins/ValheimHelmsman/ValheimHelmsman.dll':root/'dist/ValheimHelmsman/ValheimHelmsman.dll',
 'BepInEx/plugins/ValheimHelmsman/Helmsman.Core.dll':root/'dist/ValheimHelmsman/Helmsman.Core.dll',
}
files.update({name:root/name for name in ['CHANGELOG.md','COMPATIBILITY.md','TESTING.md','GUIDE.md']})
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED) as z:
 for name,path in files.items():z.write(path,name)
with zipfile.ZipFile(out) as z:
 assert z.testzip() is None
 assert struct.unpack('>II',z.read('icon.png')[16:24])==(256,256)
 for name,path in files.items():assert z.read(name)==path.read_bytes()
 assert len([name for name in z.namelist() if name.endswith('.dll')])==2
out.with_suffix('.zip.sha256').write_text(hashlib.sha256(out.read_bytes()).hexdigest()+'  '+out.name+'\n')
print(out)
