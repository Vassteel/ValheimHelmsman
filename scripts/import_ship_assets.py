#!/usr/bin/env python3
"""Import the user-approved Marlthon models, without the original plugin or its behaviors.
Requires UnityPy. Supply an extracted UnityFS asset bundle; writes a reduced bundle and audit.
"""
import argparse, collections, hashlib, json
from pathlib import Path
import UnityPy

APPROVED = tuple(json.loads((Path(__file__).resolve().parents[1]/'assets/ships/content-roots.json').read_text()))

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source',type=Path)
    parser.add_argument('destination',type=Path)
    args=parser.parse_args()
    env=UnityPy.load(str(args.source))
    objects={o.path_id:o for o in env.objects}
    scripts={o.path_id:o.read() for o in objects.values() if o.type.name=='MonoScript'}
    roots={p.read().m_Name:(path,p.path_id) for path,p in env.container.items()
           if path.endswith('.prefab') and p.read().m_Name in APPROVED}
    if set(roots)!=set(APPROVED):raise ValueError('Missing selected models: '+str(set(APPROVED)-set(roots)))
    native_container=next(pid for pid,s in scripts.items() if s.m_ClassName=='Container' and s.m_AssemblyName.startswith('assembly_valheim'))
    native_container_object=next(o for o in objects.values() if o.type.name=='MonoBehaviour' and o.read_typetree()['m_Script']['m_PathID']==native_container)
    stripped=[];cargo={};blocked=set();converted=[]
    for o in objects.values():
        if o.type.name!='MonoBehaviour':continue
        d=o.read_typetree();script=scripts.get(d['m_Script']['m_PathID'])
        if script and script.m_AssemblyName.startswith('OdinShip'):
            if script.m_ClassName=='ShipContainer':
                # Only inherited Container fields are serialized. Rebind to the game's class;
                # Helmsman scopes its save keys/RPC names for multiple holds on one ship.
                cargo[str(d['m_GameObject']['m_PathID'])]=dict(d)
                d['m_Script']={'m_FileID':0,'m_PathID':native_container}
                o.type_id=native_container_object.type_id
                o.serialized_type=native_container_object.serialized_type
                o.save_typetree(d);converted.append(o.path_id)
            else:
                blocked.add(o.path_id)
                stripped.append({'id':o.path_id,'type':script.m_ClassName})
    # Remove custom components before following references: no missing-script components
    # are left on the imported objects, and no paid DLL is needed at runtime.
    for o in objects.values():
        if o.type.name=='GameObject':
            d=o.read_typetree();parts=d['m_Component']
            d['m_Component']=[c for c in parts if c['component']['m_PathID'] not in blocked]
            if len(parts)!=len(d['m_Component']):o.save_typetree(d)
    def refs(value):
        if isinstance(value,dict):
            if set(value)=={'m_FileID','m_PathID'}:
                if value['m_FileID']==0 and value['m_PathID']:yield value['m_PathID']
            else:
                for child in value.values():yield from refs(child)
        elif isinstance(value,(list,tuple)):
            for child in value:yield from refs(child)
    extras=json.loads((Path(__file__).resolve().parents[1]/'assets/ships/style-resources.json').read_text())
    keep=set();queue=[p for _,p in roots.values()]+list(extras.values())
    while queue:
        pid=queue.pop()
        if pid in keep or pid in blocked:continue
        if pid not in objects:raise ValueError('Dangling reference '+str(pid))
        keep.add(pid)
        queue.extend(refs(objects[pid].read_typetree()))
    keep.difference_update(pid for pid,s in scripts.items() if s.m_AssemblyName.startswith('OdinShip'))
    bundle=next(o for o in objects.values() if o.type.name=='AssetBundle')
    d=bundle.read_typetree()
    d['m_Name']='helmsman-maritime-content'
    d['m_AssetBundleName']='helmsman-maritime-content'
    d['m_Container']=[(path,{'preloadIndex':0,'preloadSize':len(keep),'asset':{'m_FileID':0,'m_PathID':pid}})
                      for path,pid in list(roots.values())+list(extras.items())]
    d['m_PreloadTable']=[{'m_FileID':0,'m_PathID':pid} for pid in sorted(keep)]
    bundle.save_typetree(d);keep.add(bundle.path_id)
    for af in env.assets:
        af.objects={pid:o for pid,o in af.objects.items() if pid in keep}
        for script in af.script_types:
            if script.local_serialized_file_index==0 and script.local_identifier_in_file not in keep:
                script.local_identifier_in_file=0
    # Copy only referenced stream slices. Keeping the old .resS/.resource files
    # wholesale would still distribute removed ship meshes, textures and audio.
    from UnityPy.streams import EndianBinaryReader
    streams={};slices={};rewritten={}
    def compact(value):
        if isinstance(value,dict):
            keys=('path','offset','size') if all(k in value for k in ('path','offset','size')) else ('m_Source','m_Offset','m_Size')
            if all(k in value for k in keys) and value[keys[2]]:
                path,offset,size=(value[k] for k in keys);name=path.rsplit('/',1)[-1]
                if name not in env.file.files:raise ValueError('External stream '+path)
                old=env.file.files[name].bytes
                if offset<0 or offset+size>len(old):raise ValueError('Invalid stream range')
                key=(name,offset,size)
                target=streams.setdefault(name,bytearray())
                if key not in slices:
                    target.extend(b'\0'*((-len(target))%16));slices[key]=len(target)
                    target.extend(old[offset:offset+size])
                value[keys[1]]=slices[key]
            else:
                for child in value.values():compact(child)
        elif isinstance(value,(list,tuple)):
            for child in value:compact(child)
    for pid in sorted(keep):
        if objects[pid].type.name not in ("Mesh","Texture2D","AudioClip"):continue
        value=objects[pid].read_typetree();compact(value);objects[pid].save_typetree(value)
    for name,old in list(env.file.files.items()):
        if name.endswith(('.resS','.resource')):
            replacement=EndianBinaryReader(bytes(streams.get(name,b'')))
            replacement.flags=old.flags;env.file.files[name]=replacement
    args.destination.parent.mkdir(parents=True,exist_ok=True)
    args.destination.write_bytes(env.file.save(packer='lz4'))
    audit={'source_sha256':hashlib.sha256(args.source.read_bytes()).hexdigest(),
           'bundle_sha256':hashlib.sha256(args.destination.read_bytes()).hexdigest(),
           'approved':list(APPROVED),'objects':len(keep),'removed_plugin_components':len(stripped),'native_container_conversions':len(set(converted)&keep),
           'remaining_scripts':sorted({scripts[o.read_typetree()['m_Script']['m_PathID']].m_ClassName
               for o in objects.values() if o.path_id in keep and o.type.name=='MonoBehaviour'}),
           'cargo':{key:value for key,value in cargo.items() if int(key) in keep}}
    args.destination.with_suffix('.audit.json').write_text(json.dumps(audit,indent=2)+'\n')
    # Reload serialized output, and fail on retained executable components from either mod.
    check=UnityPy.load(str(args.destination))
    imported={o.path_id:o for o in check.objects}
    for o in imported.values():
        missing=set(refs(o.read_typetree()))-imported.keys()
        if missing:raise ValueError('Missing serialized dependencies: '+str(sorted(missing)[:10]))
        if o.type.name=='MonoScript' and o.read().m_AssemblyName.startswith('OdinShip'):
            raise ValueError('Unexpected original-mod script dependency')
    print(f'Imported {len(roots)} approved models, {len(keep)} objects; {args.destination.stat().st_size:,} bytes')
if __name__=='__main__':main()
