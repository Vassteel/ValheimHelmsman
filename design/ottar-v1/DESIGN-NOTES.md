# Ottar - standalone merchant model study

Original mesh built for review; no imported WarShip or MercantShip geometry is used. The existing merchant and warship are unchanged. This model is not registered, embedded, packaged or installed in Helmsman.

## Historical basis

- Viking Ship Museum, [Skuldelev 1](https://www.vikingeskibsmuseet.dk/en/visit-the-museum/exhibitions/the-five-viking-ships/skuldelev-1/): broad pine-planked cargo hull, fore and aft working decks, open hold amidships.
- Viking Ship Museum, [Ottar](https://www.vikingeskibsmuseet.dk/en/professions/the-boat-collection/ottar): approximately 15.8 m by 4.8 m; two central cargo spaces, square wool sail, four harbour oars, hemp standing rigging. The museum describes ochre treatment of the wool sail.

The hull follows those published proportions. Lines, frame spacing, strake count, steering-oar shape, sail pose, cargo and details are design interpretations, not a measured archaeological reconstruction. The sail is a subdued ochre wool interpretation; no aft awning or shield racks are fitted.

## Files

- ottar.blend: editable parts, procedural grain, studio lights and full-sail camera.
- ottar.glb: ship only, with base material colours; no studio.
- ottar-hero.png: render of the actual model with full sail.
- ottar-deck.png: inspection render with sail cloth hidden to show the holds and working decks.
- build_model.py: reproducible original geometry and scene generation; requires bpy 5.1.
- model-info.json: geometry counts and integration limits.

Blender units are metres, +X points toward the bow, -Y toward starboard and +Z up. GLB converts to Y-up. Rig parts are separate objects for later animation work, but the model has no gameplay rig, colliders or LODs yet.

## Preservation

All existing ships are preserved separately on codex/full-fleet-remodel in the sibling Valheim Helmsman - full fleet fork checkout. The fork retains the previous complete visual pass, including the aft awnings, for future individual remodels.
