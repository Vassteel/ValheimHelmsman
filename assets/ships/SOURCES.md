# Maritime content sources

## Authored ships

Helmsman supplies original geometry for Dugout, Finewood Kayak, Tandem Finewood Kayak, Currach, Ceol, Falkuša, Ottar, the large freighter and Snekkja. The models and paddles are built from the checked-in Blender scripts. Runtime materials, networking, buoyancy and effect components come from the installed Valheim game; game assets are not copied into the release. Player paddling is procedural skeletal animation against the game’s player rig.

The eight retired ship types are removed without save aliases, as requested for the testing world. No OdinShip ship prefab, mesh, sail, animation controller or optional ship-style material is retained in the distributed asset bundle. Stream compaction removes unreferenced binary data as well as object entries.

## Authored workshop and supplies

The shipwright bench, slipway, four workshop upgrades and eight supplies use original Blender geometry from `tools/WorkshopModels`. They register from native game component templates. Supply effects are recreated with native status-effect types; no imported item or bench hierarchy is instantiated. The nine superseded roots have been removed from the reduced source bundle, with unreferenced stream data compacted.

## Authored harbor equipment

The keel cradle, launching rollers, framing gantry, two pier cranes and three pulley sets now use original geometry from `tools/HarborModels/runtime_build.py`, based on the refined harbor studies. Native game piece components provide placement and damage handling. Seven pieces have synchronized cosmetic operating cycles; the cradle is static. Their eight imported roots and the omitted dock extension are removed from the reduced bundle.

## Retired imported content

Enguias, Peixes, RedePesca and OilPress are removed from the build catalog at the user's request. The final OdinShip bundle, hidden source meshes/materials and boatyard overlay payload are no longer embedded or loaded. No imported content roots remain. Local rollback copies are outside release assets in `.build/retired-fishing-dependencies/`.

Original replacement studies for the oil press, net rack, fish dryer and eel rack are retained under `design/fishing-equipment-v3/` for a future remake. They are not registered. The original pelican workstation remains available.

## Birds

Birds use original procedural low-poly geometry and generated surface textures. No external bird model was imported. Concept drawings are design references, not game screenshots.

Build and asset checks do not replace in-game or server acceptance testing.

The pelican workstation replaces the imported FishingDock root with original bench, net, fish, basket, bucket and perch geometry. Its storage uses a native Valheim Container; the source dock hierarchy is no longer packaged.

Totem1–Totem4 are excluded from the release bundle and build catalog. Original marker studies remain shelved under `design/maritime-markers-v1/` and `design/maritime-markers-v2/`.

## Geometry budget pass

Packed authored fleet and workstation meshes have a conservative reduction pass documented in `design/asset-optimization-v1/ASSET-REVIEW.md`. Original Blender design masters remain available. Hull surfaces, sail/rudder/paddle topology, gameplay metadata and snap points are preserved. The 5,000–20,000 triangle range is a guideline; larger ships and detailed characters retain documented exceptions. Native game meshes are referenced, not redistributed or modified by this pass.
