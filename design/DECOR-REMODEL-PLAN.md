# Maritime décor replacement plan

All playable ship geometry is authored by Helmsman. The four remaining fishing/decor pieces are now retired at the user’s request, and the final OdinShip bundle and loader are removed. No imported content roots remain. Original fishing replacement studies are retained for a later remake.

## 1. Workshop and supplies

Temporary recipe policy: ships consume vanilla materials directly. Resin wood becomes core wood plus resin; caulked wood becomes fine wood, resin and coal; each sail canvas becomes five deer hide; each marine rope becomes five leather scraps. Raw ingredient costs are preserved. Crafting these four intermediate supplies is deferred; their item prefabs remain for existing inventory and exact refunds on already-paid construction orders. Workshop upgrades and slipway construction remain active.

Replace the Carpenter’s Table and eight supplies: ResinWood, CaulkedWood, ClothShip, ShipRope, WindBelt, FishExtract, FishExtract2 and DriedFishBasket. Use an original timber bench, tool rack, adzed lumber, folded cloth, tied rope hanks, belt, oil bottles and woven fish basket. Use native Valheim materials and native item/bench components. The first group retains the existing recipe IDs and intended stack sizes while replacing the asset content. Buildables and ships stay in the hammer menu. Ships require the nearby construction bench. The base bench provides construction; separate tool, caulking, rigging and paint stations add capabilities. Tools shorten slipway construction time; caulking and rigging bonuses apply to newly built ships and persist when they leave the workshop. Paint and décor are unlocked through the paint upgrade. Exact costs, times and bonus values remain to be balanced.

Larger ships use an original timber slipway with timed construction and launch into water. The dugout, both finewood kayaks, Ceol and Currach remain instant builds. Slipway side platforms and landward end carry regularly spaced snap points for adjacent floors, docks and stairs, with underside attachment points for supports. Keep the seaward launch corridor unobstructed. The model and game prefab include 53 snap points. Runtime integration now includes saved orders, refunds, automatic hammer preview placement, launch clearance and an eight-second slide preview. Live game validation is still required. The shore end is raised 1.5 m. Quartermaster connects a bench, slipway and builder throughout a single Deposit Chest zone; without it the bench range is 30 m.

Local 0.2.39 includes this first group. Version 0.2.40 adds the rack/paint refinements and compact pelican workstation; it has been installed locally for iterative testing. See `workshop-supplies-v1/LOCAL-TEST.md` for installation and live acceptance checks.

## 2. Harbor structures and lifting gear

Replace ShipConstruction, ShipConstruction1, ShipConstruction2, PierCrane1, PierCrane2, PulleyCobia, PulleyElephantSeal and PulleyMarlin with original timber frames, trestles, lifting booms and individual pulley blocks. Lines must run around visible sheaves and terminate at pins, cleats or seized eyes. Match the current working clearances and attachment points. Fit walkable collision independently of thin rigging and hanging cargo.

Eight updated first-pass review models are available in `harbor-refinement-v2/`: cradle, rollers, gantry, two cranes and three block sets. They add joined supports, tackle return guides, windlasses, ratchets, lifting slings and working accessories. Those versions are retained design studies. Earlier studies remain in `harbor-refinement-v1/`. A second visual pass is now in `harbor-refinement-v3/`, adding timber checks, pegged joints, stitched pads, roller straps, improved coils and windlass winding. Only revised models were rendered for that pass.

The eight pieces are now registered from native game templates with packed original meshes in `assets/workshop/harbor-*.bin.gz`. `design/harbor-runtime-v1/` contains the optimized models, renders and sampled animation poses. The cradle is static; rollers, gantry, both cranes and all pulley sets run a synchronized 12-second operating cycle on interaction. Hoists raise, hold, lower and reset; rollers turn and return. These are cosmetic machinery cycles and do not attach or move player ships/cargo. Static collision remains separate from moving ropes and hooks. The imported eight roots and omitted dock extension have been removed. Live placement, interaction and multiplayer acceptance are still required.

## 3. Fishing equipment

Replace FishingDock, OilPress, RedePesca, Peixes and Enguias with the compact workstation, a screw press, sewn net sections, fish and eels. The modular dock model has been removed; retire the existing FishingDock_Extension root during runtime integration without replacing it. Ground every basket, barrel and coil. Preserve fishing triggers, oil-press output bindings and inventories. Give moving nets their own rig and test stowed/deployed states.

The pelican station now uses an original freestanding fish-prep bench with a side perch and native catch container. It can sit on an existing dock; no attached pier or deck. The source FishingDock root has been removed. Runtime and visual acceptance remain pending local testing.

Four original fishing-equipment studies are available in `fishing-equipment-v1/`: screw oil press, net rack, fish dryer and eel rack. These are static review models with future interaction/animation markers; they are not installed; the former imported runtime pieces are now removed. The second visual pass in `fishing-equipment-v2/` refines joinery, rope bindings, press hardware, fish detail and eel fins. The conservative optimized review models are in `fishing-equipment-v3/`; they remain unregistered studies.

### Pelican behavior improvement — deferred

Replace pole fishing with a flight sequence: leave the workstation perch, fly to water, catch a fish, return visibly carrying it in the bill, and deposit the catch into the station chest. The workstation is his home perch and drop-off point. Animate takeoff, flight, catch, return and landing; synchronize fish visibility and inventory delivery so one trip produces one catch. Check blocked landing space, full storage and multiplayer ownership. Deferred at the user’s request. This behavior is planned, not implemented in the current model pass.

## 4. Markers and carved décor — shelved

Omit all four markers from the current release. Remove Totem1–Totem4 from the build catalog and release bundle; keep the original model studies and generators shelved for possible future use. Do not integrate or publish them unless the user brings this category back into scope.

Four original first-pass marker studies are available in `maritime-markers-v1/`: Gullwatch post, Tide serpent, Wayfinder marker and Wreckward marker. They use authored carving and remain review-only. A second visual pass in `maritime-markers-v2/` adds adze cuts, rope knots, feather and scale carving, compass pegs and skull wear. Following user feedback, the Tide serpent now coils up and around its post like a python. The markers currently have a decorative role; no gameplay bonuses have been implemented. All remaining décor categories now have model studies; all imported fishing/decor roots have now been retired.

## 5. Final dependency removal — complete

Enguias, Peixes, RedePesca and OilPress are removed from registration. Their bundle and overlay payload are absent from runtime resources, and the imported-hierarchy loader is removed. Native game templates plus authored models supply all remaining pieces. Original fishing studies are shelved for later; local rollback data remains outside release assets.

## Acceptance for each group

- Compare preview renders and in-game views with vanilla pieces in the same light.
- Check normals, texture seams, floating props, animation clearance and placement collisions.
- Verify placement and inventories in the testing world. Save migration is not required for retired source assets.
- Check clients and dedicated servers, then verify the release contains no superseded source meshes, textures, animation controllers or unused resource-stream bytes.

This is a plan for the remaining décor work; it does not claim those replacements are finished.

## Before public publishing — animation checkpoint

User-requested reminder: revisit the animation pass before publishing this asset redesign. The harbor batch now has synchronized windlass, sheave, hook, sling and roller cycles. Complete multiplayer and in-game clearance checks before publishing. The pelican flight-and-fish-return behavior remains explicitly deferred. Explicitly bring any unfinished animation work to the user before a public release; do not treat model renders as animation acceptance. Public publishing also remains subject to the user's local testing requirement.

## Broad asset audit and optimization

See `asset-optimization-v1/ASSET-REVIEW.md` for per-item triangle counts, retained exceptions and comparison renders. The audit covers existing authored fleet, birds, workstations, supplies, harbor pieces, remaining imported décor, and deferred/shelved studies. The user clarified that 5k–20k triangles is a flexible guide, not a minimum or hard cap.
