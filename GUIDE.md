# Helmsman guide

A gull-guided ship autopilot between named Dock Wards, with ship naming and empty-ship summoning.

**Status:** builds against the locally installed Valheim 1.0.12 / Unity 6 assemblies, BepInEx 5.4.2350, and Jötunn 2.30.0. Automated core checks pass. The user has reported successful Karve departure and land avoidance. Arrival and the new naming, summoning, and larger-ship features still require in-game validation. This is a prototype for a disposable test world, not a proven unattended navigator.

## Included

- A separate Dock Ward in its own **Helmsman** hammer category, using the ward appearance and recipe. It does not provide ordinary ward protection.
- Quartermaster's charcoal/gold menu theme, native Valheim text, tabbed panels and controller-friendly controls.
- Named berths saved in the ward's world data; a directory that includes unloaded wards.
- **No real ship is required to configure or save a dock.** Position the ghost directly. Copying an existing ship's position is an optional shortcut; ship selection is required only to start a voyage.
- Berth saving is independent of clearance checks. Shallow water, unloaded areas and obstacles produce advisories; terrain and other ships still block actual movement. Built dock pieces are advisory during slow departure/arrival with relaxed checks enabled (the default).
- Navigation water-depth minimum defaults to **1 m** and is configurable. Biomes are no longer excluded by name.
- A selectable ship preview (including Karve, Longship and compatible registered modded ships), clearance outline, bow/arrival arrow, departure line, and turning-area previews.
- Position and heading sliders, optional “Set berth from selected ship,” and “Reverse out, then turn.”
- **Adjust view** hides the menu and releases camera input without discarding the berth draft. F8, Escape or controller B returns to editing. The editing ghost and guides draw through structures when the overlay shader is available.
- The ward's existing guide gull takes off and follows an arc to the stern when a voyage is selected; the source ward does not spawn a duplicate while it travels.
- Five distinct gull performances with head/neck and tail articulation: calm pecking/preening, rough-water balancing and flutter-hops, storm tucking/shaking, fog scanning, and threat-facing combat alarm hops/wingbeats.
- Animation poses for calm water, rough seas, storms, fog and nearby combat. Combat takes priority; weather changes settle before switching.
- A 10-second boarding grace period. Late boarding starts a fresh 3-second countdown; stepping off resets it.
- Incremental A* water routing, checked route simplification, loaded-world hull/mast sweeps and depth checks.
- Prefer wind-supported half sail through ordinary open-water turns. Sharp turns above 80 degrees coast before paddling, returning to normal propulsion below 55 degrees.
- Wind-dependent half-sail/paddling, reverse departure, low-speed approach, counter-thrust braking, and immediate manual helm takeover.
- Up to three automatic replans per voyage for cruise obstructions or lack of progress, after slowing. Constrained dock failures require player intervention.
- F8 to get the stern gull's attention and open destination/stop controls; the key is configurable.
- Arrival releases autopilot and sends the gull away to despawn. Normal ship behavior continues.

- **Shift + Use** at the mast hold-fast or helm opens ship naming. Names persist with the world.
- The dock gull menu has a **Summon ship** tab listing named ships, including unloaded ships. The same gull flies to the selected empty ship, boards it, and guides it back.
- While summoning, the totem shows the ship name, distance, compass direction and trip status. Cancel from the summoning menu.
- Discover ships from the live network prefab registry as well as the original prefab list, including Karve, Longship and OdinShip. Ships with custom movement implementations need an adapter.
- Rowboats require **at least two actual seats**, including a seated helm. Beds, mast hold-fast points and duplicate seat interactions do not increase capacity. Eligible rowboats use rowing propulsion even in favorable wind.
- Installed OdinShip 0.7.9 metadata checked: Merchant’s Boat, Cargo Ship, Big Cargo Ship, Little Boat, War Ship and Double Rowing Canoe are eligible; single Rowing Canoe is excluded. See [COMPATIBILITY.md](COMPATIBILITY.md).

## First playtest

1. Restart Valheim after installing the two DLLs. Open a **solo test world**. Open-server and multiplayer voyages are deliberately disabled in this version.
2. Build two Dock Wards beside deep, open water. The prototype keeps the vanilla ward costs, so use creative building in the test world if convenient.
3. Interact with each ward. In **Berth**, enter a name and position/rotate the ship ghost. Cyan shows arrival, orange shows departure. Use **Adjust view** to move the camera; press F8, Escape or controller B to return with your edits preserved. The editor uses the ghost's final position and orientation; ward orientation is independent.
4. In **Departure**, enable **Reverse out, then turn** for a bow-in berth and adjust the backing distance. Save the berth even if the clearance preview shows an advisory. The full turning circles are advisory by default; terrain and other ships still block actual movement.
5. Park a supported ship within 20 m of the source berth. Departure uses the ship's actual position and heading; aim its bow toward open water, or enable reverse departure to back out. You can also select it in the editor and use **Set berth from selected ship**. Nearby ship selection excludes that ship from clearance checks, but continues to detect other boats.
6. Release the helm, speak to the ward gull, select the ship and destination, and board during the countdown. After boarding, the status changes to **Plotting course** before departure. Use **Refresh docks** if a newly saved destination has not appeared; the world directory updates every few seconds. The ship selector cycles through nearby ships.
7. Use **F8** for the onboard gull menu. Using the actual rudder cancels autopilot immediately. Changing destination is allowed after clearing the source dock; during departure, cancel and select the trip again.
8. At arrival, confirm that propulsion stops, control releases, and the travelling gull flies away. Dock guide gulls remain available for subsequent trips.

Keep the first pair of docks within a short trip on the same open coastline. After that works, test an island between them and a bow-in berth. Read [TESTING.md](TESTING.md) for the acceptance checklist and current limitations.

If the native gull visuals fail to appear, the ward editor's **Destinations** tab still opens the trip menu. Report the missing visual as a failed playtest rather than treating this as expected presentation.

## Naming and summoning

Look at the ship’s mast hold-fast or helm and press **Shift + Use** (normally Shift + E), enter a name, then save. Give the directory a few seconds to refresh. At a configured dock, interact with its gull and open **Summon ship**. Select an empty named ship. No player needs to board a summoned ship; ordinary selected voyages still require boarding. The gull physically flies out before the ship starts its return trip.

Only one voyage or summon runs at a time, in a solo world. Summoning loads an additional area around the ship for terrain and obstacle simulation, which can increase frame time. Boarding the summoned ship, taking the helm, player death or removal of the dock/ship ends the summon. Arrival releases control and the gull flies away. Active jobs do not resume after restarting the game.

For a larger boat, use **Preview ship** in the berth editor before positioning the ghost. Copying a nearby ship also selects its preview. Navigation uses the actual ship’s clearance estimate. Modded ships with altered physics still need individual playtesting; automatic detection does not certify compatibility.

## Build

Requires .NET SDK 8, installed Valheim, BepInEx and Jötunn. Run:

```sh
bash build.sh
```

Use `DOTNET=/path/to/dotnet` if the SDK is not on PATH. This workspace also recognizes the existing SDK under `/tmp/wildglow-dotnet`; that temporary installation is not included in the mod.

Set a different game path with:

```sh
bash build.sh -p:ValheimDir='/path/to/Valheim'
```

The plugin compiles against the game's own Mono framework references. The pure navigation library targets .NET Standard 2.1; its executable tests run on .NET 8 without loading Unity. No NuGet packages or copied game assemblies are needed in the release.

Output:

```text
dist/ValheimHelmsman/ValheimHelmsman.dll
dist/ValheimHelmsman/Helmsman.Core.dll
dist/ValheimHelmsman/icon.png
```

Install that directory under the active game's `BepInEx/plugins` directory. Both DLLs are required. A running game loads the change on its next launch. Remove the custom Dock Wards before uninstalling from a world you intend to keep.

Configuration is generated at `BepInEx/config/local.valheim.helmsman.cfg` on first load. BepInEx's `LogOutput.log` records registration, selected voyages, route results, pauses, and completion.

`Navigation / MinimumWaterDepth` defaults to 1 metre and accepts 0.25–5 metres. This setting supplies the minimum for depth sampling and the submerged clearance envelope. Larger ship profiles may require more depth (Longship: at least 1.5 m). Saving the berth does not depend on this threshold. Removing biome exclusions does not add protection from those biomes' normal environmental damage.

## Code map

- `src/Helmsman.Core`: boarding state machine, incremental route search, steering and stopping calculations.
- `src/Helmsman`: plugin registration, persistent dock records, berth UI, visual-only gull/ghost creation, water/obstacle queries, and owner-only ship control.
- `tests/Helmsman.Tests`: executable core checks with no external test framework.
- `FEASIBILITY.md`: the intended design, including later multiplayer and expanded navigation work.

The build does not certify the full feasibility plan. The release deliberately exposes the first testable solo loop so its actual handling can be measured before broader support is claimed.

`Navigation / RelaxedDockChecks` defaults to `true`. It permits nearby departure without matching the ghost and skips built-piece collision blockers during slow departure/arrival. Those pieces still physically collide with the ship. Cruise checks remain active. Set it to `false` for strict dock validation. The broad hull estimate and narrow mast checks are still awaiting in-game calibration.

Gull settings: `Gull / SternPerch` uses ship-local coordinates (negative Z is aft); X/Z select the surface probe (with a short fore/aft search for the stern tip) and Y supplies a fallback height. `PerchHeightAdjustment` fine-tunes the final ship perch. Weather moods use local environment, fog, wind and ship motion; combat uses player attacks or nearby alerted hostiles targeting the player, crew, ship or ward. These reactions do not change navigation.

## Artwork

Approved icon artwork: [Viking gull on rough seas](assets/helmsman-icon-v1.png), generated using the supplied in-game gull screenshot as a visual reference. The full-resolution artwork is copied into the build output as `icon.png`.
