# Ottar - full inboard load, revision 6

Standalone visual revision; not installed in Helmsman. Earlier open, grated and packed versions remain available.

- Rolled rail cargo and spare oars are lowered inside the hull, close to deck level, with inboard securing ropes.
- Passenger seats are now simple backless benches. The helmsman's bench and mast holdfast remain.
- The hold is packed more densely: fourteen barrels, fourteen layered hide bales, forty tied sacks, ten rolled bundles and two timber packs, plus the separate end-deck supplies.
- The cargo grating remains absent.

The Blender scene and GLB contain the actual model shown in the renders. The deck inspection hides sailcloth for visibility. The eight attachment guides identify one seated helm, four passenger seats, the standing mast holdfast and two boarding ladders, but no working Valheim interactions are included.

Cargo is appearance-only. The old smooth walking-support guide is retained as provisional reference; the fuller pile rises above it. Foot contact, access to seating and the helm, collision and moving-ship behaviour must be resolved during future integration. This is a visual review asset, not a completed movement implementation.

Hull proportions follow the prior Skuldelev 1 / Ottar study. Cargo placement, seating and decoration are original game-oriented design choices, not a claim of exact historical reconstruction.

## Boarding ladders

Two static rope ladders follow the hull at midships, one port and one starboard. Their nine wooden rungs curve inward along the bilge, with just enough offset to clear the planking. The bottom rungs sit beneath the intended waterline without extending below the keel. These are rigid meshes, with no flexible-rope simulation. Small inboard landing planks are provided. attachment-points.json records interaction positions, landing targets, facing directions and landing-collider guides. The visible ladders are modelled, but working boarding behaviour is still pending game integration.

## End-deck dressing

Low spare-sail and supply rolls, a hide bundle, a small provision barrel, rope coils and a bow mooring tail dress the fore and aft working decks. They sit outside the seat footprints and remain appearance-only guides for future collision integration.

## Steering oar and textured finish

The steering oar is a single connected carved-oak mesh, including the crooked inboard grip and a gradual stock-to-blade transition. Its stock sits outside the hull on a small bearing with rope lashings.

Original 128px surface textures provide coarse grain, dark pores, worn colour variation, rough woven cloth and a muted iron-oxide sheer stripe. Textures are packed into the Blender file and embedded in the GLB; the Blender renderer adds subtle bump. These are original game-style textures, not copied Valheim textures.

Extra sacks and rolls fill the mast-foot, perimeter and barrel-top gaps. Bow and stern have additional low provision bundles, bags and barrels. Cargo remains visual only; no movement or boarding implementation is included.
