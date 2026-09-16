# Gullcall Whistle assets

Original faceted geometry authored by tools/gullcall_model.py. The model represents carved bone with wooden wings/mouthpiece, small leather wraps and two gull feather ties (no large loop). No game meshes/textures are bundled.

- model.json: runtime triangle/color data, embedded in the DLL.
- icon.png: 128×128 transparent inventory sprite rendered from that geometry, embedded in the DLL.
- preview.png: 512×512 inspection render of the same asset.
- gullcall.obj / gullcall.mtl: editable interchange export.

Regenerate with Python plus NumPy and Pillow: `python tools/gullcall_model.py`. The script validates nondegenerate triangle geometry while rendering. Meshes use flat normals recalculated by Unity and native world-lit materials with emission disabled. No asset bundle or external Unity project is required.
