# Workshop and slipway — local test 0.2.40

0.2.40 is built and ready for local installation. The install guard detected Valheim running and changed no installed files; 0.2.39 remains installed. No public upload, Git push or server update was performed. READMEs were not edited.

## Included

- Original shipwright bench, raised slipway and four workshop upgrades.
- Eight original supply models: treated timber, sealed timber, canvas, rope, wind belt, fish oil, wind extract and dried-fish basket.
- Native game components and shaders. The wind belt retains a native Valheim wearable attachment; its inventory/drop model is authored here.
- Removed the old bench and eight supply prefab roots from the imported asset bundle. Seventeen harbor/decor roots remain; full dependency removal is not complete.
- Slipway: raised shore end, 53 snap points, automatic hammer preview, saved timed orders, exclusive access, refund on destruction, launch path checks and an eight-second slide preview. The completed ship appears in the water after the preview; it does not physically roll down the slipway as a live rigidbody.

## 0.2.40 refinements

- Rigging rack: reeved two-block purchase, hook, laced and repaired canvas, belaying pins and varied rope hanks.
- Paint stand: six hollow pots, separated sample hems, supported brushes and small placement variations.
- Compact pelican workstation: freestanding bench, side perch, native catch chest, repaired hanging net, fish, rag, bucket, basket and rope. No bundled pier/deck; source FishingDock root removed.
- Targeted geometry checks cover pot/sample clearance, pot support, fish scale and unobstructed chest approach. Model renders inspected. These checks do not establish live game acceptance.
- Pelican flight fishing is recorded for the next bird behavior pass; the current runtime still uses its existing fishing animation.

Place the new pelican station on an existing floor and test chest access, catches, save/reload, ward access and dropped contents on destruction. Inspect perch alignment and native game shading.

## Build rules

Ships stay in the hammer menu. Dugout, Finewood Kayak, Tandem Finewood Kayak, Ceol and Currach build instantly. Falkuša, Ottar, Longship, Big cargo ship and Snekkja use a free slipway.

With Quartermaster enabled, builder, supporting bench and slipway must fit inside one accessible Deposit Chest zone using Quartermaster's configured radius. They can be farther than 30 m apart. Upgrades must share a zone with that worksite, bench and builder. Overlapping zones do not form a chain of unlimited coverage.

Without Quartermaster, the bench supports the builder, worksite and upgrades within 30 m of the bench. A paint stand unlocks the puffin's paint/decor menu.

Default new-order bonuses:

- Tool rack: 25% less construction time, with a 30-second minimum.
- Caulking station: 15% more hull durability.
- Rigging rack: 10% more sail force; paddle-only boats do not gain propulsion from rigging.

Bonuses are recorded for new ships/orders. Existing ships are not upgraded retroactively. Base construction times use the existing Construction settings; changes do not alter an already paid order.

## In-game checks still needed

1. Confirm the bench, slipway and four upgrades appear under Hammer → Helmsman. Check their icons and materials beside vanilla pieces, including the backs of the canvas samples.
2. Place the slipway facing clear, sufficiently deep water. Snap floors/stairs to the side and shore platform. Walk the platforms; check the ghost rests on the cradles and clears the shore during launch.
3. Place a bench over 30 m away in the same Quartermaster base. Select a large ship: its preview should snap to the free slipway. Order it once and verify a single material deduction. Try a bench in a different base; it must not count.
4. Without Quartermaster, check the 30 m bench boundary. Build an instant boat to verify it bypasses the slipway timer.
5. Start a paid order, save/quit and reload. Confirm remaining time, completed launch and stored upgrades. Block the launch path: the paid order should wait. Clear it: one ship should launch.
6. Destroy an unfinished paid slipway and check ship materials are refunded once. A free-build order should refund no ship materials. Test two players ordering the same slipway.
7. Compare an upgraded newly built ship with an unupgraded one; verify full initial health, repair maximum, sailing behavior and persistence after reload. Existing ships should retain their previous values.
8. Check paint access, the puffin's interaction, supply recipes, item pickup/drop and belt equip/unequip visuals.

## Automated validation

The complete build/check suite passed with no compiler warnings or errors. It checked all 15 embedded authored station/supply models, winding/normal agreement, 53 snap points, fleet exceptions, coverage boundaries, costs, placement categories, authority/payment, launch clearance, existing fleet regressions and 961 installed game/framework members across 44 Harmony patches.

Physics/network checks use test doubles. These checks do not establish live game acceptance of the new slipway, station hooks or artwork. Build log: `.build/workshop-pass2-checks.log`. The existing install receipt `output/category-install.json` still describes 0.2.39.
