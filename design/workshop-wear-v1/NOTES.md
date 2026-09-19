# Puffin workshop play-test revisions

The main bench remains at its existing 1.055 m working height. Tool, caulking and paint tables now share that height; complete assemblies are scaled together so tools, pots and shelves retain their contact points. The rigging rack's belaying rail is brought to that working height without enlarging it as much as the tables.

The five stations have varied weathered timbers, irregular lip nicks, worn edges and surface scoring. The tool table has shavings and less uniform tool angles; the caulking table has pitch spills and an oakum offcut; the paint table has dried pigment stains. Bird walking and bench placement remain at the existing height.

Hovering a connected upgrade uses the installed game's workbench-extension particle prefab. Connections use the existing Helmsman radius or Quartermaster base-zone policy and ward access. They expire after looking away, and are cleaned up on disable/destruction. This is cosmetic and does not add vanilla station levels.

Slipway snap selection starts with the four platform corners, followed by deck edges and then supports. Its native snap search now reaches past the large footprint; vanilla pieces and the 0.5 m snap tolerance are unchanged.

The renders use the exact packed meshes and locally extracted vanilla reference textures under Blender studio lighting. They are model previews, not game screenshots. No extracted vanilla textures are added to the shipped resources.

Validation results are recorded in CHECKS.txt. In-game particle appearance, placement feel and final shading require another play test. No website publication is part of this pass.
