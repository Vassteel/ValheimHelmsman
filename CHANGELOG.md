# Changelog

## 0.2.41

- Skip unsupported blueprint objects and allow construction orders anywhere within workshop coverage.
- Honor no-cost building without requesting or withdrawing construction supplies.
- Add a required-materials scrollbar, faster mouse-wheel scrolling and controller paging.
- Fit dock approaches to ship size and clear water, with finer route searches through narrow river bends.

## 0.2.40

- Build larger ships on a slipway with staged construction, arriving puffin crews, launching and reset animations; small boats remain instant builds.
- Show required materials at the Carpenter’s Table and collect supplies through Quartermaster, with bench storage as a fallback.
- Fix missing slipway construction ghosts, puffin arrivals and cancellation cleanup.
- Add original workshop upgrades and animated harbor equipment, plus blueprint and vbuild construction through the puffin.
- Improve kayak speed, turning and player paddling, including tandem assistance with rear-seat steering.
- Reduce ship and workstation mesh complexity, repair the Currach keel and improve small-boat materials and workshop wear.
- Remove remaining borrowed asset dependencies; use vanilla construction materials.

## 0.2.35

- Add Dugout, Finewood Kayak and Tandem Finewood Kayak, with seated player paddling and reachable boarding grips.
- Give the dugout a cheap wood recipe and slower paddling than the kayaks.
- Build all nine authored ship models on native Valheim components; remove the eight remaining source-model ships and both old canoe IDs without compatibility aliases.
- Remove imported ship prefabs, optional ship styles, derived ship meshes and unused resource-stream data from release assets.

## 0.2.34

- Reduce excessive heeling across imported ships with hull-sized sail leverage and stronger roll damping.
- Lower Big Cargo’s ride height by 20 cm.
- Add working halyard and jib rigging, fish-filled crates, a rolled net, bait bucket and lashed supplies to Falkuša.

## 0.2.33

- Make rope ladders reachable from the water and place boarding exits on supported deck areas.
- Restore the heavy freighter's helm interaction above the deck.
- Close barrel staves, repair inward cargo surfaces, and seal the Currach stern and bow joint.
- Fit native water masks to each redesigned hull opening to hide water inside the boats.
- Increase wind motion on redesigned sails, retain fully deployed source meshes, and tether legacy cloth to prevent excessive stretching.
- Correct legacy sails saved in a reefed pose so they can deploy fully.

## 0.2.32

- Match redesigned ships to native wood and cloth materials; separate overlapping plank surfaces and restore vanilla water-impact splashes.
- Repair sail furling, add wind movement, lower the small boats in the water and correct their helm poses.
- Widen both merchant ships’ side walkways, improve rope coils and working pulley details, and close Currach’s bow gap.
- Animate the heavy freighter’s steering oar in rowing gear and remove obsolete ship effect triggers.

## 0.2.31

- Build ships and harbor pieces from Hammer → Helmsman; the puffin now handles paint and decoration.
- Craft maritime supplies at an ordinary Workbench. Existing paid ship orders still finish.

- Fix ship model loading that prevented the Carpenter’s Table and harbor stations from appearing.
- Register workstations before ships so a ship loading error cannot hide them.

## 0.2.30

- Add the redesigned Ottar, heavy freighter, Snekkja, Falkuša, Ceol and six-slot Currach to puffin shipbuilding.
- Improve rigging, steering clearance, paint options and boarding; restore natural cargo stacks with smooth walking collision.

## 0.2.29

- Add optional close-range rock clearing with automatic stone collection into ship cargo. Excess stone stays in the world when cargo is full.
- Enable fish pass-through during gull steering by default, with a separate menu toggle.

## 0.2.28

- Ignore fish during route obstruction checks and try finer local detours around blocked passages.
- Restore replan attempts after substantial progress instead of limiting an entire voyage to three obstructions.
- Make the gull’s interaction collider non-solid and disable it before takeoff detaches it from a ship.

## 0.2.27

- Ask the dock gull to scout its island, then collect terrain discoveries and points of interest at a nearby Cartographer’s Table.
- Save uncollected surveys across reconnects and server restarts.

## 0.2.26

- Refresh the README with current features, setup and controls.

## 0.2.25

- Hugin explains Dock Wards, the Carpenter’s Table and Gullcall Whistle on first construction, discovery or use. Read lessons are remembered per character.

## 0.2.24

- Restore masked glow to merchant lantern glass and warm deck illumination.
- Stabilize the merchant ship lantern: steady illumination without moving point-light shadows on the hull.


## 0.2.23

- Fix imported ship sail animation errors interrupting player movement and leaving characters floating.

## 0.2.22

- Center ship previews and display the selected ship name beneath them.
- Add livelier pelican and puffin idle poses, plus occasional puffin bench strolls.
- Add building hops with anticipation, landings and distinct tool-work sequences; show a translucent ship at its berth during construction.
- Check ship launches against actual collision shapes instead of the padded navigation box, allowing tighter berths.

## 0.2.21

- Relax fishing dock and dock extension placement against existing piers and shoreline terrain.
- Browse ship commissions with an image carousel using the actual ship models.
- Honor debug zero-cost building for ship commissions; free orders do not refund unpaid materials.
- Restrict the Carpenter’s Table to Helmsman recipes, including in no-cost mode.
- Include the current build-menu Helmsman category correction.

## 0.2.20

- Add the Helmsman category to the current usage-based build menu, alongside other mods’ categories. Keep legacy tabs and existing category entries intact.

## 0.2.19

- Group all Helmsman build pieces in the Helmsman tab.
- Fix rebuilt harbor previews jumping away from the cursor; keep preview collision separate from finished structures.
- Use normal surface placement for the fishing dock.

## 0.2.17

- Apply a boatyard visual pass to all 42 imported assets: rebuilt shore structures and supplies, individual ship-stem profiles, working shelters and nautical fittings. Refresh menu icons from the revised models.
- Keep original prefab identities and functional ship rigs; rebuilt shore structures use matching collision meshes. This visual pass still requires in-game validation.

- Fix maritime registration failing when the current longship lacks a legacy sail renderer or Custom/Piece hull material. Use native building materials and discover cloth renderers directly.
- Set current build-menu usage tags; put the Carpenter’s Table in Crafting and harbor decorations in Decor. Ships remain timed puffin commissions.
- Replace raised clothing blobs with fitted panels, remove clothing surface noise, and rebuild the pelican hat as a continuous mesh.
- Add regression checks for the material-resolution failure reported in the live log.

## 0.2.16

- Integrate the civilian OdinShip/OdinShipPlus fleet, harbor decorations, original prefab names and recipes; exclude autonomous enemies and naval combat. Original plugin DLLs are not included.
- Add timed, saved ship commissions through a puffin at the Carpenter's Table, with hammer, chisel and sail-stitching work. Original costs; separate configurable durations by hull.
- Add native-longship lantern, sheltering canopy, inventory-only cargo expansions, protective treatment and decorative trophy refits. Add saved ship styles and custom sailcloth.
- Repaint default hull textures and fittings with coarse, muted finishes on native Valheim shaders.
- Add daylight pelican fishing, a shipboard net, fish-oil pressing, maritime materials and sleeping bird poses.
- Isolate multi-hold cargo saves/RPCs; preserve old Odin cargo keys during validated migration. Unreadable cargo stays locked instead of being overwritten.
- Enable multiplayer navigation with server reservations, remote scene manifests, disconnect expiry and manual-takeover protection. Require matching Helmsman patch versions. Multiplayer still needs live acceptance.
- Support one-seat canoe piloting and recall. Requests keep the original calling destination when the player moves away.
- Add transactional workshop access, trophy transfer rollback and persistent launch/output receipts.

## 0.2.15

- Recognize the current game's cloth sails on Karves and rafts, which no longer require the legacy sail-object reference.
- Whistle summons sail to the original calling spot. Moving or teleporting away no longer cancels them; shoreline checks stay loaded during the search and run again before arrival.
- Place voyage status below the native hotbar, aligned with its first slot and UI scale.
- Pause and explain coroutine planning failures; remove partial Harmony patches when startup hooks fail.
- Version the plugin and Core DLL together. Keep the established plugin GUID and saved-world keys.
- Refresh player documentation, separate developer notes, and distinguish local play from remaining checks and disabled multiplayer.

## 0.2.14

- Replace Unity JSON model loading with a validated binary mesh decoder after the live whistle registration failed with an empty-array access.
- Validate all model parts before modifying the item prefab; add real-asset and damaged-asset regression checks.

## 0.2.13

- Fix the missing Gullcall Whistle by cloning the native BoneFragments material instead of looking up bundled shaders by name.
- Isolate whistle registration errors so Dock Ward setup still runs.
- Use native gull material references for the ship gull's helmet; remove the unlit fallback.

## 0.2.12

- Native gull squawks and local chat explanations for blocked/shallow routes, longer planning delays and arrival; ask “What's happening?” during a voyage.
- Reuse the voyage gull as a perched visitor after arrival. A summoned ship's gull waits up to two minutes for boarding.
- Speak the result of requested Quartermaster unloading, including where unmatched cargo belongs.
- Rewrite GitHub and Thunderstore READMEs as compact feature and control guides.

## 0.2.11

- Replace the solid blue navigation line with layered, animated blue wisps and soft mist, raised 4 metres above the route.
- Add a saved show/hide control to gull, dock and whistle menus.
- Clear the old visual during route recalculation and clean up its materials on voyage completion.

## 0.2.10

- Whistle summons now bring a named ship to checked water beside the caller's shoreline; no Dock Ward or dock picker required.
- Search nearby loaded water for a hull-sized landing with depth, hull/mast, approach and turning clearance. Reject unsafe shores and cancel when the caller leaves the area.
- Keep the arrival point request-local; no permanent ward or saved berth is created. Existing dock summon controls remain available.
- Remove the large leather loop from the whistle model and regenerate its matching inventory icon; keep small wraps and feather ties.

## 0.2.9

- Add the reusable Gullcall Whistle, crafted without a station from bone fragments, wood, leather scraps and feathers.
- Inventory/hotbar use opens arrival-dock and named-ship selection, with progress and cancellation. Existing ships sail to configured docks through the existing summon system.
- Include an original faceted gull-shaped model and matching transparent inventory icon, embedded in the DLL.

## 0.2.8

- With Quartermaster 0.1.11+, talk to the landed ship gull near a Deposit Chest for **Unload cargo**.
- Explicit requests sort boat cargo into matching base storage one slot at a time, with three scoop-and-throw effects per slot.
- Stop from the dialogue or by taking the helm/leaving; unmatched cargo stays aboard. Cargo controls are absent without Quartermaster or outside base range.

## 0.2.7

- Added a faceted iron helmet with bronze trim and curved horns to the gull, matching the package artwork. The accessory follows perched head/body gestures and the flying body rig.


## 0.2.6

- Replace the mast menu with tap Use to hold fast and hold Use for 0.6 seconds to call the gull. Keep Shift + Use naming.
- Resolve each press once, preserve vanilla hold-fast checks, and cancel pending input when the target, player or input context changes.
- Update README controls and verify tap/hold handling with the production interaction harness.

## 0.2.5

- Updated README testing-status wording.


## 0.2.4

- Shortened the README and gave the gull a pitch specific to this mod.


## 0.2.3

- Rewrote the README in the voice of a Viking gull selling a well-used longship. Installation, controls and testing status remain documented.


## 0.2.2 — 2026-09-14

- Add a mast menu with Call the gull, Hold fast and Name ship. Preserve Shift + Use naming and ordinary passenger seats.
- Let the gull fly aboard before accepting a destination; reuse the same actor for repeat calls and the ensuing voyage. Nearby Dock Wards lend their guide without spawning a duplicate.
- Allow attended voyages to a configured dock from open water, retaining source-berth departure checks when near a dock.
- F8 now calls the gull before a voyage and opens orders after landing. Add visit dismissal and cleanup on disembarking, death and ownership/world changes.
- Build and automated checks pass; new mast menus, flight/landing and open-water trips await in-game verification.

## 0.2.1 — 2026-09-14

- Broaden ship discovery to the live registered prefab names plus the original scene list; resolve previews from the same live registry and log detected/excluded ships.
- Check compatibility against installed OdinShip 0.7.9 ship metadata. Include its sailing ships and Double Rowing Canoe; exclude the single Rowing Canoe.
- Require more than one seat for rowboats, counting a seated helm and ignoring beds, hold-fast points and duplicate positions. Keep eligible rowing boats in rowing propulsion despite favorable wind or dummy sail objects.
- Keep rowboat clearance from inheriting a tall dummy mast envelope.
- Build passes without warnings; 85 core checks pass. OdinShip voyages, summoning and live detection still require in-game verification.

## 0.2.0 — 2026-09-14

- Add persistent ship naming through Shift + Use at mast hold-fast and helm interactions.
- Add the dock gull’s Summon ship menu for named empty ships, including ships outside the player’s loaded area. The existing gull flies out, boards the stern and guides the ship back.
- Show summoned ship name, distance, compass bearing and status above the destination totem. Add cancellation, occupied-ship rejection and cleanup of the extra simulation area.
- Add Longship and automatic detection of modded ships using standard Ship physics/controls. Scale clearance estimates and selectable berth ghosts to ship profiles; custom movement systems require adapters.
- Include the approved Viking gull icon artwork.
- Build and 66 core checks pass; seven local assembly checks validate hook signatures and expected physics branches. New summoning, naming and larger-ship behavior await in-game testing.

## 0.1.7 — 2026-09-14

- Simplify gull presentation while retaining all five animation states and standard interaction.

## 0.1.6 — 2026-09-14

- Replace generic whole-body oscillation with five timed performances: calm look/peck/preen/tail-flick, rough-sea counterbalance and flutter-hop, storm tuck/shake/peek, fog horizon scans/head tilts/lookout-hop, and threat-directed combat alarm hops/wingbeats/forward jabs.
- Inspection confirmed vanilla Seagal has a static sitting mesh and zero native idle variants. Add a private runtime head/neck, body and tail deformation rig from the installed readable mesh, with feet held at the perch. Use the native animated flight model during short hops; blend pose transitions. No game mesh assets are packaged.
- Transfer the existing source-ward gull to the ship along a rising flight arc. Reserve its source ward until it despawns, including across source-zone unload/reload, to prevent a duplicate guide appearing during the voyage.
- Navigation behavior unchanged. Local mesh-deformation review and core checks completed; in-game animation timing, wing-model transitions and actor handoff await testing.

## 0.1.5 — 2026-09-14

- Move the travelling gull to a surface-probed stern perch. Anchor it to the ship so wave motion does not repeatedly trigger flight. Probe ward tops and align the resting model to the surface to reduce floating.
- Restore supported native bird idle/flight Animator parameters and add small perched breathing, pecking, scanning, bracing and alert motions.
- Add calm, rough-seas, storm, fog and combat animation states. Combat interrupts immediately and lingers; weather must remain stable for three seconds.
- F8/direct interaction gets the stern gull's attention; arrival retains its fly-away cleanup. Add Gull config settings for the stern perch and height adjustment.
- Navigation logic unchanged. Build and 40 core checks pass; new perch/animation/weather visuals need in-game verification.

## 0.1.4 — 2026-09-14

- Keep wind-supported half sail during ordinary open-water turns instead of forcing rowing above 30 degrees.
- Slow only above 80 degrees of heading error; resume normal wind-based propulsion at 55 degrees to prevent repeated sail changes.
- Sharp turns coast with the rudder still applied above 2.5 m/s, then paddle. Obstruction braking and slow dock maneuvers remain active.
- Add seven core checks for turn thresholds and hysteresis; actual handling still needs in-game testing.

## 0.1.3 — 2026-09-14

- Relaxed dock checks default on: start within 20 m of a berth without matching its arrow, then use the actual ship position and heading for forward/reverse departure. Saved arrival settings remain unchanged.
- Built dock pieces are advisory during slow departure/arrival; terrain, rocks and other ships still block. Full turning disks remain editor advisories. Disable `Navigation / RelaxedDockChecks` to restore strict dock validation.
- Reduce the estimated hull envelope to 4 by 10 m with 0.2 m margins, check the mast separately, and reduce the regional shoreline sampling radius from 9 to 3 m. Estimates still need in-game calibration.
- Fix the observed countdown-to-clearance-pause failure path: remove full-berth validation as a default departure gate, plan after boarding from the actual departure endpoint, and display explicit planning/paused status. Blocker reports now include the owning object name.
- Use ship occupancy consistently for boarding, including seated passengers. Preserve countdown and missing-passenger checks.
- Build and 22 existing core checks pass; new Unity navigation behavior awaits playtesting.

- Clarify that ghost-only dock setup needs no real ship. Label position-copy helpers as optional and remove ship-selection controls from departure configuration. Version 0.1.2 already allows saving without a selected ship.

## 0.1.2 — 2026-09-14

- Allow berth settings to be saved despite clearance advisories. Only valid settings, local access and save ownership are required; navigation still checks the actual maneuver.
- Reduce the default navigation depth minimum from 2.5 m to 1 m and expose `Navigation / MinimumWaterDepth` (0.25–5 m).
- Remove blanket Ashlands and Deep North exclusions. World-edge, loaded-terrain and physical clearance checks remain part of navigation.
- Report measured shallow-water depth separately from unloaded terrain and world-edge failures.

## 0.1.1 — 2026-09-14

- Move the Dock Ward into its own **Helmsman** hammer category.
- Adopt Quartermaster's charcoal/gold native UI, game font, highlighted buttons, tabbed panels and controller navigation for dock and gull menus, including the voyage status display.
- Add **Adjust view** to berth/departure setup. Temporarily release camera input with the preview and draft preserved; F8, Escape or controller B returns to editing.
- Draw the ship ghost and berth guides through structures while configuring. Fall back to normal transparency if the runtime overlay shader is unavailable.
- Build verified; native menu, controller and overlay appearance still await in-game verification.

## 0.1.0 — 2026-09-14

First solo Karve prototype: named Dock Ward berth editor, preview and clearance checks, forward/reverse departure, boarding grace period, guide gull menus, incremental water routing, wind-aware propulsion, bounded cruise replanning, manual takeover and arrival cleanup. Builds and core checks pass; in-game acceptance remains pending.
