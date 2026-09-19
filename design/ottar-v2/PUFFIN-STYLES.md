# Ottar - puffin appearance studies

These are standalone model variants for review, not installed shipwright options. The full fleet fork and existing game models remain unchanged.

## Choices

- Natural: the original Ottar model, with plain timber and ochre wool sail.
- Carved timber: darker lower hull, contrasting upper timber, a shallow braided band and seabird medallions.
- Red ochre: red upper strakes, pale braided trim, cream sail with red panels and a small seabird emblem.
- Deep blue: blue upper strakes, pale golden trim, ivory sail with blue edges and a small seabird emblem.

The new decoration and colour schemes are artistic choices for Helmsman, not claims about the documented paintwork of the historical Ottar reconstruction.

## Intended puffin controls

Hull finish, decorative trim and sailcloth should be separate choices when Ottar is eventually integrated. The trim is in an optional collection and has no collider. Original hull, deck, cargo and steering geometry are retained across all three variants.

The existing shipwright material switcher needs an Ottar-specific binding before these can be used in game: it currently replaces whole renderer materials, whereas Ottar uses separate strake and sail-panel materials. No menu entries, purchases, save keys or game integration are added by this study.

## Files

Each variant in styles/ includes an editable Blender scene, a ship-only GLB and an actual model render. styles/styles.json records the colours and file mappings. build_styles.py reproduces the variants from ottar.blend using bpy 5.1. The original plain model remains available at the top level. All variants share revision 2 cargo, wooden grating and the separate walking-surface guide.

The Blender scenes include procedural wood grain on the original timber; GLBs use portable base colours. Collision, animation and in-game movement work are still pending for every version.
