# Maritime content sources

## Authored ships

Helmsman supplies original geometry for Dugout, Finewood Kayak, Tandem Finewood Kayak, Currach, Ceol, Falkuša, Ottar, the large freighter and Snekkja. The models and paddles are built from the checked-in Blender scripts. Runtime materials, networking, buoyancy and effect components come from the installed Valheim game; game assets are not copied into the release. Player paddling is procedural skeletal animation against the game’s player rig.

The eight retired ship types are removed without save aliases, as requested for the testing world. No OdinShip ship prefab, mesh, sail, animation controller or optional ship-style material is retained in the distributed asset bundle. Stream compaction removes unreferenced binary data as well as object entries.

## Remaining harbor and decorative content

The 27 roots in `content-roots.json` retain assets from Marlthon’s OdinShip 0.7.9 and OdinShipPlus 0.8.3. The user confirmed permission to reuse, retexture and redistribute these assets. Original plugin DLLs and licensing code are not included. Helmsman implements its own runtime behavior. Some visible geometry has already been replaced by Helmsman’s boatyard designs; retained hierarchy, shared textures, catch animals and other source data keep this attribution.

The exact bundle contents and dependency checks are recorded in `helmsman-ships.audit.json`. The remaining replacement plan is `design/DECOR-REMODEL-PLAN.md` in the repository.

Author pages:
- https://thunderstore.io/c/valheim/p/Marlthon/OdinShip/
- https://valheim.hexium.gg/mods/Marlthon/OdinShipPlus

## Birds

Birds use original procedural low-poly geometry and generated surface textures. No external bird model was imported. Concept drawings are design references, not game screenshots.

Build and asset checks do not replace in-game or server acceptance testing.
