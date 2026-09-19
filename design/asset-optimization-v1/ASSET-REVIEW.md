> Subsequent retirement: Enguias, Peixes, RedePesca and OilPress have been removed, including the imported bundle and loader. Their counts below describe the preceding optimization snapshot; these four pieces are no longer shipped. Replacement studies remain shelved.

# Helmsman asset review — 18 September 2026

The 5k–20k triangle range is a flexible guide. Simple items stay below it; larger ships and the puffin retain exceptions. Counts are triangles per complete asset, not per submesh. This is a geometry audit, not an FPS benchmark.

Across the 23 changed designs, triangles fell from **1,422,814 to 563,752 (60.4% less)**. This total mixes shipped models and four review studies; it is not an estimate of a scene's rendering load. Unchanged eel study is excluded from that saving.

## Integration

Eight harbor pieces use original models and native Valheim piece components. The keel cradle is static. The rollers, gantry, two cranes and three pulley sets have interactive, network-time-driven 12-second operating cycles. Hoists raise, hold, lower and reset; rollers rotate and return. These are cosmetic operating cycles: they do not attach or transport ships or cargo. Static collision stays separate from moving tackle. Both cranes have six snap points.

The eight superseded imported roots and omitted dock extension are removed. The compacted source bundle retains only Enguias, Peixes, RedePesca and OilPress. Their replacement studies remain separate from runtime. Markers remain shelved. Sound work and pelican fishing flight remain deferred.

## Quality decisions

- Preserved all hull surfaces after an initial reduction opened a bow seam; the corrected renders were inspected again.
- Preserved sail, rudder and paddle geometry, hull/gameplay metadata, collision definitions and snap points byte for byte. Slipway haul rope topology is also unchanged.
- Kept timber joints, connected rope coils, baskets and individual cargo components. Connectivity and bounds checks found no lost disconnected pieces.
- Reduced overly dense fittings and fine puffin geometry while keeping major body and face resolution.
- Boats remain about 10.7k–80.8k triangles. The two cargo ships retain roughly 70k and 81k; forcing 20k would require a more extensive redesign or a separate distant LOD.
- Materials and draw calls are counted separately. Reduced triangles do not guarantee proportionate FPS gains; there has been no live GPU profile.

## Validation and limits

Full build and existing automated suite passed with zero compiler warnings/errors. Packed asset validation, production bird geometry checks, harbor motion checks, dependency/stream decoding checks and original-versus-reduced structural checks passed. Installed 0.2.40 locally in Steam and the Mods profile, with DLL hashes verified. Nothing published.

The renders use exact packed geometry with Blender studio lighting; they are not Unity/Valheim shader screenshots. Placement, collision cooking, operation from both clients, visual motion clearances and game lighting still need local acceptance. The scout gull figure is the perched model with helmet; its native flight mesh varies by game asset and is not included in this perched count. Native game-owned customization props and transient Unity primitive work tools are not independently remodeled or counted as new standalone assets. Each circus worker uses the puffin model below; simultaneous workers multiply that count.

## Reproduction

`tools/HarborModels/runtime_build.py` rebuilds the native harbor assets from the refined masters. `optimize_existing.py` stages conservative packed reductions in this folder using original binaries retained in `.build/asset-budget-baseline/`; it does not overwrite the editable fleet/workshop design masters. Preserve that baseline for identical before/after reproduction. `check_optimization.py` checks the staged result; promote reviewed files to their recorded `source` paths before rebuilding. `optimize_studies.py` produces the separate fishing v3 studies. Production puffin changes live in `src/Helmsman/Art/PerchedBird.cs`.

[Interactive comparison gallery](review.html) · [CSV audit](asset-audit.csv) · [JSON audit](asset-audit.json)

## Per-item counts

| Item | Group | Before | Now | Render batches | State |
|---|---|---:|---:|---:|---|
| Big cargo ship | Fleet | 256,920 | 69,898 | 71 | Integrated; local test pending |
| Currach | Fleet | 97,898 | 28,592 | 38 | Integrated; local test pending |
| Dugout | Fleet | 10,744 | 10,744 | 6 | Integrated; local test pending |
| Finewood kayak | Fleet | 25,356 | 21,440 | 11 | Integrated; local test pending |
| Tandem finewood kayak | Fleet | 31,360 | 22,472 | 11 | Integrated; local test pending |
| Falkuša | Fleet | 101,390 | 32,262 | 40 | Integrated; local test pending |
| Ceol | Fleet | 77,514 | 27,326 | 31 | Integrated; local test pending |
| Ottar | Fleet | 281,432 | 80,814 | 67 | Integrated; local test pending |
| Snekkja | Fleet | 126,598 | 43,312 | 48 | Integrated; local test pending |
| Caulked wood | Workshop and supplies | 3,176 | 3,176 | 7 | Integrated; local test pending |
| Caulking station / hull durability | Workshop and supplies | 10,148 | 10,148 | 7 | Integrated; local test pending |
| Dried fish basket | Workshop and supplies | 13,144 | 13,144 | 5 | Integrated; local test pending |
| Fish extract / carrying | Workshop and supplies | 1,393 | 1,393 | 5 | Integrated; local test pending |
| Pelican workstation | Workshop and supplies | 45,982 | 19,978 | 23 | Integrated; local test pending |
| Double purchase | Harbor equipment | 14,404 | 9,840 | 14 | Integrated; local test pending |
| Ship framing gantry | Harbor equipment | 27,916 | 14,420 | 17 | Integrated; local test pending |
| Braced pier crane | Harbor equipment | 37,944 | 19,656 | 23 | Integrated; local test pending |
| Keel cradle | Harbor equipment | 21,826 | 10,382 | 10 | Integrated; local test pending |
| Timber pier crane | Harbor equipment | 33,304 | 16,952 | 23 | Integrated; local test pending |
| Launching rollers | Harbor equipment | 4,580 | 2,924 | 17 | Integrated; local test pending |
| Single block | Harbor equipment | 3,876 | 2,844 | 8 | Integrated; local test pending |
| Heavy purchase | Harbor equipment | 17,972 | 11,792 | 14 | Integrated; local test pending |
| Marine rope | Workshop and supplies | 8,300 | 8,300 | 2 | Integrated; local test pending |
| Paint stand / paint and decoration | Workshop and supplies | 7,812 | 7,812 | 10 | Integrated; local test pending |
| Resin wood | Workshop and supplies | 3,176 | 3,176 | 6 | Integrated; local test pending |
| Rigging rack / ship speed | Workshop and supplies | 22,408 | 19,992 | 11 | Integrated; local test pending |
| Sail canvas | Workshop and supplies | 2,886 | 2,886 | 5 | Integrated; local test pending |
| Shipwright slipway | Workshop and supplies | 40,160 | 19,988 | 14 | Integrated; local test pending |
| Shipwright tools / construction speed | Workshop and supplies | 2,542 | 2,542 | 7 | Integrated; local test pending |
| Wind belt | Workshop and supplies | 2,788 | 2,788 | 6 | Integrated; local test pending |
| Fish extract / wind | Workshop and supplies | 1,350 | 1,350 | 5 | Integrated; local test pending |
| Puffin's Construction Bench | Workshop and supplies | 14,822 | 14,822 | 12 | Integrated; local test pending |
| Puffin | Birds | 40,276 | 26,630 | 34 | Integrated; all generated parts counted |
| Burrowing owl | Birds | 15,872 | 15,872 | 27 | Integrated; all generated parts counted |
| Pelican | Birds | 18,806 | 18,806 | 30 | Integrated; all generated parts counted |
| Scout gull with helmet (perched) | Birds | 13,712 | 13,712 | 17 | Native base refined at runtime; perched instance counted |
| Gullcall whistle | Supplies | 906 | 906 |  | Unchanged; already inexpensive |
| Enguias | Remaining imported hierarchy | 432 | 432 | 3 | Current visible replacement; original hierarchy still retained |
| Peixes | Remaining imported hierarchy | 177 | 177 | 3 | Current visible replacement; original hierarchy still retained |
| RedePesca | Remaining imported hierarchy | 276 | 276 | 2 | Current visible replacement; original hierarchy still retained |
| OilPress | Remaining imported hierarchy | 2,884 | 2,884 | 3 | Current visible replacement; original hierarchy still retained |
| Fish oil screw press | Fishing studies | 40,116 | 21,296 |  | Review only; not shipped |
| Fishing net rack | Fishing studies | 36,998 | 19,706 |  | Review only; not shipped |
| Fish drying rack | Fishing studies | 36,584 | 21,236 |  | Review only; not shipped |
| Eel drying rack | Fishing studies | 14,112 | 14,112 |  | Review only; not shipped |
| Gullwatch post | Shelved markers | 2,578 | 2,578 |  | Shelved; not shipped |
| Tide serpent | Shelved markers | 7,616 | 7,616 |  | Shelved; not shipped |
| Wayfinder marker | Shelved markers | 2,562 | 2,562 |  | Shelved; not shipped |
| Wreckward marker | Shelved markers | 2,988 | 2,988 |  | Shelved; not shipped |
