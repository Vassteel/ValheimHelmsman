# Maritime markers — first review pass

**Shelved — excluded from the current release at the user's request. Retain these models for possible future use.**

Four original decorative markers replace the remaining Totem1–Totem4 designs when runtime integration is performed. These are model studies, not installed prefabs, navigation beacons or new gameplay effects.

| Model | Intended replacement | Distinctive features |
| --- | --- | --- |
| Gullwatch post | Totem1 | Timber gull carving, folded wings, hollow carved eyes, wave cuts |
| Tide serpent | Totem2 | Curled solid neck, incised eye/mouth/neck line, tide groove |
| Wayfinder marker | Totem3 | Eight-point compass relief on a solid disc, recessed rim and ticks |
| Wreckward marker | Totem4 | Weathered bone skull emblem, hollow sockets/nose, uneven teeth, split backboard |

All four use grounded hewn feet, rope bindings and shallow cracks cut into timber geometry. Detail remains restrained enough to read at the game's scale. Names are review labels; runtime names/identifiers are unchanged. The fourth model keeps the bone motif associated with its existing skeleton-trophy recipe.

Editable Blender files, GLB exports and studio renders are in the model folders. The generator uses authored primitives and real boolean carving; it does not load OdinShip meshes or textures. Studio materials are previews; native shader matching and in-game appearance remain to be validated.

Next: user feedback, collision and placement integration, native prefab registration, removal of the four imported root closures, then local testing. These are stationary carvings; no new animation is implied. Keep the separate animation checkpoint for the cranes, fishing equipment and pelican before public publishing.

## Review validation

All four Blender models and GLB exports were regenerated and rendered. Geometry checks passed for finite coordinates, manifest bounds/counts, ground contact and nonempty meshes after carving. A specific check verifies the compass disc remains beneath its relief after the boolean cuts. See `geometry-checks.json`. Game shading, collision and placement are not validated by these checks.
