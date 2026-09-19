# Harbor structures and lifting gear — first review pass

Eight original models for feedback. This batch is model-only: no runtime registration, local installation or public publishing.

| Model | Intended replacement | First-pass features |
| --- | --- | --- |
| Keel cradle | ShipConstruction | Padded shores, tie straps, adjustment wedges, mallet and rope |
| Launching rollers | ShipConstruction1 | Banded timber rollers, spindles, bearings, stops and working tools |
| Ship framing gantry | ShipConstruction2 | Braced frame, paired tackle, lifting spreader, slings and belaying cleat |
| Timber pier crane | PierCrane1 | Kingpost, compression strut, paired tackle, windlass, crank and ratchet |
| Braced pier crane | PierCrane2 | A-frame with pivot cross members, larger tackle and windlass |
| Single block | PulleyCobia | Grooved sheave, cheek straps, wall rail and two falls |
| Double purchase | PulleyElephantSeal | Two lower sheaves, upper return guide, seized end and hook |
| Heavy purchase | PulleyMarlin | Three lower sheaves, upper return guide, seized end and hook |

Structural joints remain aligned. Variation is concentrated in rope coils, tool angles, board colors and working accessories. Crane platforms retain six snap markers each; in-game snap integration is still pending.

Files: each model folder contains an editable .blend, .glb and studio render. The generator uses authored primitives without importing the OdinShip bundle. The earlier v1 studies remain alongside this batch for comparison.

These are static concept models. Cranks, hooks and tackle do not yet animate or lift cargo in game. Studio materials approximate the desired finish; vanilla shader matching, collision, placement and networking require the later integration pass. Existing timed ship construction remains on the slipway.

Next: collect feedback on proportions and dock footprints, resolve remaining rigging detail, create collision separately from thin ropes, register native prefabs and remove the eight imported roots, then test locally before publication.

## Review validation

All eight Blender models and GLB exports were regenerated and rendered. Geometry checks passed for finite coordinates, recorded bounds and triangle counts; both crane decks retain six snap markers. The review caught and corrected the cradle pad pivots, gantry haul-line route and A-frame/windlass overlap. `geometry-checks.json` records the model checks. These checks do not validate game collision or animated lifting.
