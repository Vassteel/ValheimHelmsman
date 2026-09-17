# Boatyard visual pass

All 42 imported public prefab entries have a design in `designs.json`. Native Valheim base models and the separately generated bird models are outside this imported-content pass.

- Fifteen ships: individual upper-stem profiles; corresponding static end fittings follow the new profile; new stern racks, net gantries or cloth working shelters; separate merchant/Taurus ornaments replaced with seabird carvings. Separate deck/floor/hatch meshes are protected; combined hull surfaces below the configured deck-clearance threshold remain unchanged. Mast/sail hierarchies, separate rigging parts, native animated sails/oars/rudders, cargo paths and ship physics remain in place.
- Carpenter's Table: pegged frame, vise, rib jig, tool tray, shelf and hand tools.
- Fishing dock and extension: full-length compatible plank footprints, bracing, sorting shelf and net roller. The dock chest keeps its original components and visuals.
- Oil press: new frame, screw, wooden tubs and collection channel.
- Cranes: new hand-winch jib forms replacing the treadwheel silhouettes.
- Hanging catches: new twin-post hoists; existing fish/seal meshes retained.
- Construction displays: new keel, rib and planking forms on slipway supports.
- Carved posts, drying racks and net: new nautical geometry.
- Eight supplies: new timber bundles, canvas rolls, rope coil, belt, oil containers and fish basket. Preserve proportions when fitting supply models; do not stretch them to their old boxes.

This is a derivative visual redesign, not a claim that every part of every ship was built from scratch. Source attribution remains in `../SOURCES.md`. Existing alternate material styles remain available.

## Reproduction

1. Run `tools/MaritimeModels/extract.py` using Python with UnityPy. It exports the shipped bundle's visual mesh hierarchy to `output/model-assessment/source-meshes.json.gz`.
2. Run `tools/MaritimeModels/rig_roots.py` with UnityPy to record protected mast/sail hierarchies. Run `tools/MaritimeModels/build.py` using Python with numpy. It builds `models.bin.gz`, the design inventory and the review geometry. `kit.py` contains original generated replacement geometry.
3. Run `tests/BoatyardModels/check.py` using UnityPy. It validates complete catalogue coverage, exact hierarchy routes and renderer types, source vertex counts, finite coordinates and triangle indices against the bundle.
4. Build Helmsman. `BoatyardModels` loads the versioned mesh stream and validates all targets for a prefab before applying any changes.

The supplied source bundle is not modified by this build. Replacements attach to original MeshFilter transforms. Visual-only extras are children of the existing prefab. Static rebuilt pieces receive a matching mesh collider; old solid colliders are disabled, while triggers and the fishing chest are preserved. Ships keep their existing physics/colliders. Inventory/build icons are rendered from the revised prefabs through Jotunn on graphical clients; headless servers skip icon rendering.

## Validation limits

CPU review renders are diagnostic geometry views, not Unity screenshots. They do not reproduce final materials, alpha clipping, skinning or sail animation. The source review only extracted active visual states. Native animated meshes and inactive customization variants are deliberately not replaced.

In-game checks are still required: shoreline placement, static mesh-collider cooking, ship deck/head clearance, cargo/helm access, sail states, canopy overlap, item pickup/equip appearance, build icons, multiplayer loading and save/reload. Ship shelters are visual fittings; they do not automatically grant the purchased native-longship canopy upgrade's gameplay effects.
