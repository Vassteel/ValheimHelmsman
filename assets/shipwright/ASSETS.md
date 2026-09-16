# Shipwright assets — work in progress

The implementation reuses models already shipped with Valheim at runtime: the longship's dormant canopy/supports, handheld lantern, item stand and ballista visuals. No extracted vanilla model or original texture files are bundled here.

Placement and selected upgrade behavior were adapted from shudnal's LongshipUpgrades (Unlicense): https://github.com/shudnal/LongshipUpgrades . The original license is retained beside this file. Helmsman's payment flow, inventory-only cargo expansion, shared ammunition and selected feature scope are separate implementation work.

## Generated replacement textures

Created with Codex's built-in image generation tool; original outputs remain in the session's generated_images directory. Native texture atlases were used as UV-layout references; replacement images preserve their layouts and use coarse, subdued materials. Runtime loading reduces built-in textures to native-scale texel density; custom PNG styles are loaded separately.

- `sail-sea-flax.png`: dirty flax and faded iron-blue sail panels, restrained ochre seams.
- `canopy-sea-flax.png`: same cloth palette with leather/fur atlas regions retained.
- `ballista-weathered.png`: weathered oak, tar-dark iron and aged brass; dark engravings replace cyan painted glow patches.
- `lantern-weathered.png`: dark iron, aged brass and warm ochre glass.

## Puffin shipwright concept

User chose a new puffin at the Carpenter's Table, handling construction/refits while the onboard gull retains navigation/cargo. Approved details: knitted cap with fuzzy pom-pom, worn leather apron and a pocket with a carpenter's pencil. The user approved `concepts/puffin-shipwright.png` as the visual direction. This is concept artwork, not a finished rigged game model. Construction must take time; work animations use a mallet, chisel and needlework for sailcloth.

Prompt set: `PROMPTS.md`.

## OdinShip

No OdinShip or OdinShipPlus models have been imported or bundled. User has explicitly confirmed Marlthon’s permission for model reuse, retexturing and redistribution inside Helmsman. Keep author attribution with imported assets. Existing installed assets must not be mistaken for Helmsman-owned models or proof of redistribution permission.
