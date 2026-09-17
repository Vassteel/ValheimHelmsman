# Maritime content sources

Models: Marlthon, OdinShip 0.7.9 and OdinShipPlus 0.8.3. The user confirmed permission to reuse, retexture and redistribute these assets. Original plugin DLLs and licensing code are not included. Helmsman implements its own runtime behavior.

Original prefab identifiers and recipe data were checked against the author's published descriptions:
- https://thunderstore.io/c/valheim/p/Marlthon/OdinShip/
- https://valheim.hexium.gg/mods/Marlthon/OdinShipPlus (author-published package description)

`content-roots.json` lists the imported player-facing models. Autonomous enemy ships, enemy crews, their weapons and naval combat ammunition are excluded for a later release. Development-only duplicate models are not added as extra recipes.

Import audit: `helmsman-ships.audit.json`. Resource streams and mesh dependencies are preserved; original-mod MonoBehaviours are removed and cargo containers are rebound to Valheim's native Container. Old cargo records remain available during conversion.

Shipbuilding uses the original ingredient costs, through timed commissions at the puffin. New materials retain their original identifiers. The native longship keeps its vanilla recipe.

Retextures: original generated albedo edits under `../shipwright/hulls`, bound to native Valheim shaders. All new behavioral code remains under validation; this asset import is not evidence of in-game or server acceptance.

Visual variants: 38 original serialized sail, shield and hull materials are retained as choices. Default hulls use repainted atlases; optional variants preserve their pattern textures on native, matte shaders. Non-sailing single-seat and double canoes use native rowing gear.

Birds: original procedural low-poly geometry and generated surface texture; no external bird model was imported. The approved concept drawings are design references, not screenshots of game models.

## Boatyard geometry redesign

`redesign/` contains Helmsman-authored shore structures, supplies and ship fittings plus derived ship-stem mesh changes. Imported ship bases, animated rigs, alternate styles and catch animals retain their source attribution. See `redesign/DESIGN.md` for exact coverage and validation limits.
