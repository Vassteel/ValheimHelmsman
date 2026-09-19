# BigCargoShip — heavy freighter, first study

Standalone original model for review. It is not registered, installed or substituted into Helmsman. Ottar and the full-fleet fork remain unchanged.

- 19.2 m long, 6.3 m beam; fuller hull sections and deeper holds than Ottar.
- Fifteen lashed timber logs fill the aft well; six open ore bins and flanking barrels fill the forward well.
- Sack cargo closes the hold perimeter. Low supplies, rope coils and a manual windlass dress the end decks.
- A heavier square rig carries muted red and unbleached cloth stripes. A static cargo boom and tackle provide a working freight silhouette.
- Continuous carved side rudder, one helm bench, four backless passenger benches, mast holdfast and two hull-following rope ladders.
- Original 128px surface textures are embedded in the GLB and packed in the Blender file.

This is a game-oriented design study, not an exact historical reconstruction. Boarding, collisions, walking over cargo, steering, flotation and cargo-handling animations remain future integration work. Attachment coordinates are design guides only. Freight and sack meshes are marked appearance-only for eventual collision handling.

The deck and freight inspection renders hide the sailcloth for visibility. The saved Blender scene and GLB include the full rig.

Run build_model.py with Python containing bpy 5.1 and numpy. The supporting Python files contain authored shape definitions reused from the Ottar study. No original BigCargoShip mesh or copied game texture is used.
