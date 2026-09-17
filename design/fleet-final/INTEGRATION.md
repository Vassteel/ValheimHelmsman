# Final fleet integration

- MercantShip → Ottar; BigCargoShip → heavy freighter; WarShip → Snekkja.
- HerculeShip → Falkuša; LittleBoat → Ceol; HelmsmanCurrach is a new prefab cloned from LittleBoat's native components.
- Existing prefab names, order IDs, recipe requirements and container slot/save identities remain. Currach adds one 3 × 2 hold. No rowing oars were added.
- Native ships receive authored render meshes, fitted convex keel sections, deck/cargo supports, buoyancy dimensions, onboard triggers, seats, helm, mast holdfast and two boarding ladders.
- Existing imported colliders/meshes are retired on these six prefabs. Cargo props have no individual blocking colliders. Merchants retain full barrels, layered hides, tied sacks and rolled bundles; the freighter also retains its logs and ore bins. Loose props settle onto the hull, deck or lower cargo. Small visible gaps are intentional. A continuous invisible triangulated support follows the load with gentle grades and shared edges, rather than colliders on every prop.
- Cloth furls on synchronized speed settings. Standing rigging stays fixed; sheets follow cloth height. Rudder movement is limited to the checked ±8-degree sweep.
- Original textures are embedded in the mesh payloads and applied to native game materials. Puffin hull/sail colors preserve those textures; merchants also have a carved trim option.
- The Falkuša keeps active net fishing; fish pass-through does not suppress trigger-based catches.

## Rebuild

Run `tools/FinalFleet/build.py` with the bpy Python environment. It reads the preserved review models, writes six final Blend/GLB assets and exports `assets/ships/final/*.bin.gz`. Set `FLEET_SKIP_RENDER=1` for mesh-only regeneration. `tools/FinalFleet/render.py` produces fitted hero views from final Blend files.

Run `tools/FinalFleet/check_geometry.py` with bpy for actual mesh intersections and animated rudder clearance. `tests/FinalFleet/run.py` verifies the shipped binary geometry, UV textures, convex-section topology and cargo coverage; the normal build calls it before compilation. The game API checker verifies the six embedded payloads byte-for-byte.

## Acceptance

Compilation, resource checks and geometry checks passed. Native physics, player walking/boarding, styling, saves and multiplayer still require the game acceptance pass in TESTING.md. Harbour Workbench remains a separate review asset; this change integrates the six ships.
