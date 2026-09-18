# Maritime décor replacement plan

All playable ship geometry is now authored by Helmsman. The remaining imported asset bundle is limited to 27 workshop, harbor, supply and decorative prefab roots. Some visible geometry has already been rebuilt, but their source hierarchies and shared materials still require replacement before declaring the whole mod independent of OdinShip assets.

## 1. Workshop and supplies

Replace the Carpenter’s Table and eight supplies: ResinWood, CaulkedWood, ClothShip, ShipRope, WindBelt, FishExtract, FishExtract2 and DriedFishBasket. Use an original timber bench, tool rack, adzed lumber, folded cloth, tied rope hanks, belt, oil bottles and woven fish basket. Use native Valheim materials and native item/bench components. Use clear Helmsman IDs for replacement items and preserve the intended stack sizes and recipes. The puffin continues to handle paint and décor; buildables and ships stay in the hammer menu.

## 2. Harbor structures and lifting gear

Replace ShipConstruction, ShipConstruction1, ShipConstruction2, PierCrane1, PierCrane2, PulleyCobia, PulleyElephantSeal and PulleyMarlin with original timber frames, trestles, lifting booms and individual pulley blocks. Lines must run around visible sheaves and terminate at pins, cleats or seized eyes. Match the current working clearances and attachment points. Fit walkable collision independently of thin rigging and hanging cargo.

## 3. Fishing equipment

Replace FishingDock, FishingDock_Extension, OilPress, RedePesca, Peixes and Enguias with original dock modules, a screw press, sewn net sections, fish and eels. Ground every basket, barrel and coil. Preserve fishing triggers, oil-press output bindings and inventories. Give moving nets their own rig and test stowed/deployed states.

## 4. Markers and carved décor

Replace Totem1–Totem4 with four distinct carved maritime markers. Match Valheim’s texture density and shading. Give them clear Helmsman IDs and preserve their placement purpose and interactions.

## 5. Remove the remaining dependency

Register these pieces and items from native game component templates plus authored meshes. Remove the imported bundle and all derived geometry, obsolete texture resources and hierarchy bindings once every replacement is verified. Keep the original archive outside release packages for rollback and provenance.

## Acceptance for each group

- Compare preview renders and in-game views with vanilla pieces in the same light.
- Check normals, texture seams, floating props, animation clearance and placement collisions.
- Verify placement and inventories in the testing world. Save migration is not required for retired source assets.
- Check clients and dedicated servers, then verify the release contains no superseded source meshes, textures, animation controllers or unused resource-stream bytes.

This is a plan for the remaining décor work; it does not claim those replacements are finished.
