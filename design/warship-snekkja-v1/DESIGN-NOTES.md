# WarShip — snekkja study, revision 1

Standalone visual review model. No existing ship is replaced or registered in Helmsman. The other ship studies and full-fleet fork remain separate.

## Historical basis

The primary archaeological reference is Skuldelev 5, a small Danish warship associated with the snekkja category. The museum records reconstructed dimensions of 17.3 m length, 2.47 m beam and 1.16 m depth amidships, thirteen pairs of oars, mast evidence, and a surviving shield-retaining rail. The ship also incorporated reused timbers and repaired oarholes.

Source: https://www.vikingeskibsmuseet.dk/en/professions/education/the-longships/findings-of-longships-from-the-viking-age/skuldelev-5

Helge Ask provides a secondary reference for a sailing reconstruction. Its published beam is 2.70 m, distinct from the archaeological estimate used for this study. The museum lists 26 oars and 46 square metres of sail. Its yellow and brown-red paint scheme is interpretive: no original paint was found on the Skuldelev ships. Its festive dragon head is not treated here as evidence that the original small warship carried one.

Source: https://www.vikingeskibsmuseet.dk/en/professions/the-boat-collection/helge-ask/

The museum's newer reconstruction project explains that Helge Ask regularised some of the original planking and omitted reused boards. This study therefore does not claim that reproducing Helge Ask's appearance would reproduce every feature of the excavated ship.

Source: https://www.vikingeskibsmuseet.dk/nyheder/gensyn-med-skuldelev-5

## Model choices

- Target archaeological proportions, low clinker hull, thirteen thwarts and 26 oars.
- Actual round oarports through the top strakes; a static deployed rowing pose.
- 26 round shields in an illustrative display arrangement along retaining rails.
- Plain square sail with a nominal projected area of approximately 46 square metres.
- A continuous carved starboard steering oar and low end platforms.
- Restrained sea chests and mooring coils, with no cargo-filled rowing floor or raised fighting castle.
- Original 128px wood, cloth and pigment textures, embedded in the GLB and Blender file.

Hull lines, framing details, rigging, steering-oar geometry, shield dimensions, colour arrangement and equipment positions are original interpretations. No archaeological lines plan was traced. This is a historically informed game concept, not an exact museum reconstruction. Reused timbers, old patched oarholes and the original carved decoration have not yet been reproduced.

## Review and integration

The Blender scene and GLB include the full sail and deployed oars. Deck and profile renders hide the sailcloth for inspection. Attachment positions are preliminary guides only. No Unity collisions, seated animations, rowing animations, steering, flotation or player movement have been implemented or tested.

Run build_model.py with Python containing bpy 5.1 and numpy. Output remains beside the script.
