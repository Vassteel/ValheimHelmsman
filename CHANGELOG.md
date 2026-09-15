# Changelog

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
