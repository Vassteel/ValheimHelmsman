"""Keep construction stages separate without changing sail/rudder animation groups."""
def legacy_group(ob):
    n=ob.name.lower()
    if n.startswith('carved merchant'):return 'decoration'
    if n in ['steering oar / single carved oak','steering oar single carved oak','carved stern rudder','raised bent tiller']:return 'rudder'
    if 'sail' in n and not any(k in n for k in ['yard','spare','roll','mast lacing']):return 'sail'
    if n=='jib' or n.startswith(('lateen mainsail','jib bolt rope','jib stitched seam','red sailing cloth')):return 'sail'
    if 'clinker strake' in n or 'hull band' in n or n=='currach keel skin closure':return 'hull'
    return 'fixed'

def group(ob):
    original=legacy_group(ob)
    if original!='fixed':return original
    n=ob.name.lower()
    if any(k in n for k in ['cargo','deck gear','catch /','fish crate','shield board','shield rawhide','shield boss','stowed harbour']):return 'cargo'
    if any(k in n for k in ['rope','shroud','stay','halyard','lashing','brace','hoist','roband','parrel','block','belaying','cleat','knot','rigging']):return 'rigging'
    if any(k in n for k in ['mast','yard','bowsprit']) and not any(k in n for k in ['keelson','step','partner']):return 'mast'
    if any(k in n for k in ['deck','working plank','floorboard','sole board','thwart','bench','seat','walkway']):return 'deck'
    return 'fixed'
