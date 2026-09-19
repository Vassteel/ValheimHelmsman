# Ottar - full inboard load, revision 5

Standalone visual revision; not installed in Helmsman. Earlier open, grated and packed versions remain available.

- Rolled rail cargo and spare oars are lowered inside the hull, close to deck level, with inboard securing ropes.
- Passenger seats are now simple backless benches. The helmsman's bench and mast holdfast remain.
- The hold is packed more densely: fourteen barrels, twelve layered hide bales, eighteen tied sacks, six rolled bundles and two timber packs.
- The cargo grating remains absent.

The Blender scene and GLB contain the actual model shown in the renders. The deck inspection hides sailcloth for visibility. The eight attachment guides identify one seated helm, four passenger seats, the standing mast holdfast and two boarding ladders, but no working Valheim interactions are included.

Cargo is appearance-only. The old smooth walking-support guide is retained as provisional reference; the fuller pile rises above it. Foot contact, access to seating and the helm, collision and moving-ship behaviour must be resolved during future integration. This is a visual review asset, not a completed movement implementation.

Hull proportions follow the prior Skuldelev 1 / Ottar study. Cargo placement, seating and decoration are original game-oriented design choices, not a claim of exact historical reconstruction.

## Boarding ladders

Two static rope ladders follow the hull at midships, one port and one starboard. Their nine wooden rungs curve inward along the bilge, with just enough offset to clear the planking. The bottom rungs sit beneath the intended waterline without extending below the keel. These are rigid meshes, with no flexible-rope simulation. Small inboard landing planks are provided. attachment-points.json records interaction positions, landing targets, facing directions and landing-collider guides. The visible ladders are modelled, but working boarding behaviour is still pending game integration.

## End-deck dressing

Low spare-sail and supply rolls, a hide bundle, a small provision barrel, rope coils and a bow mooring tail dress the fore and aft working decks. They sit outside the seat footprints and remain appearance-only guides for future collision integration.
