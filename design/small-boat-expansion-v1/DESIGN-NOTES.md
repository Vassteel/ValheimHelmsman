# Small boats and Harbour Workbench — first pass

Four standalone review assets. No game source, installed mod or saved world was changed. All three boats include sails and have no rowing oars. Each model has an editable Blender file and a GLB with embedded original textures. Deck-inspection renders hide sailcloth; saved models retain it.

## Roles

- Falkuša: 9 m fishing boat based on user image 1, with lateen mainsail, jib, raised sideboards, fish crates, folded netting and two casks.
- Ceol: 6.4 m Little Boat replacement concept based on user images 2–3, with a square sail, open seating, sea chest and rope coils.
- Currach: 5.5 m additional small boat based on user images 4–5, with a red triangular sail, dark skin hull and exposed timber frame. Cargo comprises two hide bales, one cask and two knapsacks.
- Harbour Workbench: 3 m wide by 2 m deep including the standing apron, with shared worktop, tool area, chart, fishing equipment, covered catch chest and three bird perches.

Currach inventory target: **3 columns × 2 rows = 6 slots**, recorded in metadata only. No functional inventory is implemented. Cargo is marked appearance-only for future collision handling.

## Shared station

Puffin: ship orders, refits and styles. Gull: dock designation, routes and scouting. Pelican: fishing and catch storage/reporting.

The birds belong to the station and are intended to perch and wander nearby. Rendered birds are static concept meshes, not rigged or animated. A later game implementation will use bounded idle movement and separate configured water targets for launching and fishing.

Planned interaction pages: Ships, Docks/Scouting and Fishing, with individual birds opening their corresponding page. Retain timed ship orders and the existing zero-cost option. When consolidating existing stations, preserve dock IDs, active orders and stored catches. No migration has been performed.

## Reference basis

The five user-supplied images are the primary visual references. Dimensions, individual hull lines, rudders, equipment and rigging are original game-oriented interpretations, not exact historical reconstructions. The requested Ceol name does not assert that both reference photos document a single archaeological type.

Supporting research from the planning turn:

- Falkuša raised sideboards and fishing role: https://www.geopark-vis.com/eng/gajeta-falkusa
- Small sailing craft context: https://regia.org/fleet/fleet.php
- Currach rib, lath and tarred canvas construction: https://source.museum.ie/en-IE/Press-and-Media-Information/Latest-Media-Releases/21-April-2021-New-online-exhibition-reveals-unique

No photographed texture is copied into the models. Materials use original generated 128px colour maps. No claim is made that these regional designs share one historical period.

## Rebuilding

Use Python with bpy 5.1 and numpy. Run build_boat.py with falkusa, ceol or currach as the argument, and build_workbench.py for the station. Run validate_models.py to verify exports, sails, cargo counts and station footprint.

Game integration, collisions, seat interactions, sail and rudder motion, buoyancy, bird animations, fishing and menus remain unimplemented. Visual and structural checks do not establish in-game acceptance.
