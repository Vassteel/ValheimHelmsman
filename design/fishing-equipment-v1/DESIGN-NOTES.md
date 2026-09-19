# Fishing equipment — first model pass

Four original models following the harbor structures batch. The pelican's compact workstation is a separate completed model; its planned flight-and-return fishing behavior remains on the animation checkpoint.

| Model | Intended replacement | Design |
| --- | --- | --- |
| Fish oil screw press | OilPress | Braced timber frame, threaded shaft and turning handle, slotted basket, pressing plate, catch pan, spout and supported collection pot |
| Fishing net rack | RedePesca | Sagging open mesh, floats, stone weights, repaired tear and stowed reserve net tray |
| Fish drying rack | Peixes | Individually tied fish at varied heights, lower shelf, woven catch basket and spare line |
| Eel drying rack | Enguias | Tapered curved eel bodies, tail ties, belly details and drip tray |

Each model folder includes editable Blender source, GLB and studio render. All geometry is generated from authored primitives. No imported OdinShip asset files are loaded by this generator. The models remain review-only and do not remove their existing runtime roots yet.

The modular dock extension was removed at the user's request. For the retained equipment, variation is concentrated in catch heights, cloth/net sag, knots and supply placement rather than misaligned load-bearing joints.

## Integration and animation still pending

- Bind the press input/output to the existing ten-fish, ten-minute processing behavior and verify access, persistence and refunds. The pot is currently a visual prop, not an inventory.
- Rig limited press screw/platen travel, net movement and subtle catch sway. Markers identify intended anchors but are not working animations or skeletons.
- Use native game shaders, collision and placement bounds.
- Keep ropes/catches out of solid walking collision, test placement on existing constructions, and verify model scale in game.
- Replace the four retained equipment roots after validation and retire FishingDock_Extension without a replacement. Do not publish before the animation checkpoint and user testing.

## First-pass validation

All four Blender files and GLB exports were regenerated and rendered. Checks passed for finite geometry, bounds, triangle counts, named press interaction/animation anchors. Render review prompted corrections to fish orientation, dryer shelf supports, press drainage clearance, reserve-net tray placement and the eel tray crossbar. See `geometry-checks.json`. Runtime, collision and animation acceptance remain pending.
