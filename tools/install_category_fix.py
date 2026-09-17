from pathlib import Path
import datetime,hashlib,json,os,tempfile
root=Path(__file__).resolve().parents[1]
def closed():
 for proc in Path('/proc').glob('[0-9]*/cmdline'):
  try:name=proc.read_bytes().split(b'\0')[0].decode(errors='replace').replace('\\','/').rsplit('/',1)[-1].lower()
  except OSError:continue
  if name in ('valheim.exe','valheim.x86_64'):raise SystemExit('Valheim is running; no files changed.')
def atomic(path,data):
 fd,temp=tempfile.mkstemp(dir=path.parent,prefix='.helmsman-update-')
 try:
  with os.fdopen(fd,'wb') as f:f.write(data);f.flush();os.fsync(f.fileno())
  os.chmod(temp,0o644);os.replace(temp,path)
 finally:
  if os.path.exists(temp):os.unlink(temp)
closed()
manifest=root/'packaging/manifest.json';version=json.loads(manifest.read_text())['version_number'];assert version=='0.2.30'
sources=[root/'dist/ValheimHelmsman/ValheimHelmsman.dll',root/'dist/ValheimHelmsman/Helmsman.Core.dll',manifest]
places=[Path('/home/deck/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins/ValheimHelmsman'),Path('/home/deck/.var/app/io.github.ebkr.r2modman/config/r2modmanPlus-local/Valheim/profiles/Mods/BepInEx/plugins/local-ValheimHelmsman')]
for folder in places:
 assert folder.is_dir(),folder
 for source in sources[:2]:assert all(p==folder/source.name for p in folder.parent.rglob(source.name)),'Duplicate DLL location'
backup=root/'.build'/('category-install-'+datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ'));backup.mkdir()
changes=[];files=[]
try:
 for i,folder in enumerate(places):
  for source in sources:
   closed();dest=folder/source.name;old=dest.read_bytes() if dest.exists() else None
   if old is not None:(backup/(str(i)+'-'+source.name)).write_bytes(old)
   data=source.read_bytes();changes.append((dest,old));atomic(dest,data);assert dest.read_bytes()==data
   files.append({'mod':'Helmsman','version':version,'path':str(dest),'sha256':hashlib.sha256(data).hexdigest(),'changed':old!=data})
except BaseException:
 for path,old in reversed(changes):
  if old is None:path.unlink(missing_ok=True)
  else:atomic(path,old)
 raise
report={'version':version,'files':files,'backup':str(backup),'server_updated':False,'readmes_changed':False}
(root/'output/category-install.json').write_text(json.dumps(report,indent=2)+'\n')
shared=Path('/home/deck/Documents/ChatGPT/Valheim mods/output/latest-local-deployment.json')
if shared.exists():
 old=shared.read_bytes();(backup/'previous-local-deployment.json').write_bytes(old);record=json.loads(old)
 record['files']=[r for r in record['files'] if r['mod']!='Helmsman']+files
 shared.write_text(json.dumps(record,indent=2)+'\n')
print('Installed Helmsman '+version+' in Steam and Mods; DLLs hash-verified. Server and READMEs unchanged.')
