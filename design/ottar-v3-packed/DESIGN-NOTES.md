# Ottar - packed cargo study, revision 3

Separate no-grid variant. The earlier open-hold and grated-hold versions remain unchanged. No model, interaction or menu entry from this study is installed in Helmsman.

## Visible changes

- No wooden cargo grating.
- Twelve barrels, six layered hide bales, ten tied sacks, four rolled bundles and two small timber packs fill the hold.
- Two of the rolled bundles are lashed to the deck rails. Spare oars have their own rail lashings.
- One helmsman's bench beside the raised tiller.
- Four individual passenger seats: two on the aft deck and two on the fore deck.
- A wooden mast grab bar with rope grips and a standing holdfast guide.

## Interaction handoff

attachment-points.json contains six local-space guides: the seated helm, four passenger seats and a standing mast holdfast. The Blender scene includes these in an interaction-guide collection; they are not part of the visible GLB. These are modelling references, not working Valheim Sit or ShipControlls components.

Cargo is tagged as appearance-only, with no intended individual collision. walking-surface.json defines a single smooth support over the hold for later development. Because the load is visibly uneven, foot contact must be reviewed in game: this file does not claim collision-free or clipping-free player movement. Seating, the helm, holdfast, moving-ship behaviour and waterline handling still need integration and playtesting.

## Files

ottar.blend is the editable scene. ottar.glb contains only visible ship geometry. ottar-hero.png and ottar-deck.png are actual model renders; the deck inspection hides the sail cloth to show the cargo and seating. build_model.py reproduces the study. build_styles.py can derive the previously chosen paint schemes later; painted exports are not part of this particular review package.

## Reference and interpretation

Hull proportions derive from the Viking Ship Museum's [Skuldelev 1](https://www.vikingeskibsmuseet.dk/en/visit-the-museum/exhibitions/the-five-viking-ships/skuldelev-1/) and [Ottar](https://www.vikingeskibsmuseet.dk/en/professions/the-boat-collection/ottar) descriptions. This cargo arrangement, seating and mast handle are original game-oriented interpretations, not an exact reconstruction of the historical ship.
