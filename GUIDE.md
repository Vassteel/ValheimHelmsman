## Gull announcements — 0.2.12

The gull uses Valheim's seagull sound and writes local chat lines for route obstructions, shallow water, planning lasting over five seconds, and arrival. **What's happening?** in the voyage menu repeats the current explanation on request. Automatic lines accompany events, not every navigation tick. Requested cargo unloading also announces its result, including Quartermaster's reason for leftovers.

On arrival, the same gull stays on the stern for further orders. After an unattended summon it waits up to two minutes for a nearby caller to board; walking away or leaving after boarding dismisses it. Chat lines are local NPC feedback, not messages broadcast as the player.

## Blue route wisps — 0.2.11

The navigation guide is now a flowing blue mist trail, raised 4 metres above the route. Choose **Hide route wisps** or **Show route wisps** at the top of the gull, dock or whistle menu. The setting takes effect immediately and is saved for future sessions. Existing `Diagnostics.ShowRoute` preferences are retained.

## Gullcall Whistle — 0.2.10

Craft by hand in the inventory crafting list: **4 Bone Fragments, 2 Wood, 2 Leather Scraps, 2 Feathers**. No crafting station, metal or later-biome ingredient is required. Discover the ingredients to unlock the recipe. The whistle weighs 0.2, does not stack, has no durability and is not consumed.

Stand near the shoreline or on a low pier, use the whistle from inventory or a hotbar slot, and choose a named ship. **No Dock Ward is required.** The gull checks nearby landing positions and sails the existing ship to a clear offshore stop near where you called. The whistle never teleports or creates a ship. Name ships with Shift + Use at their mast or helm.

The landing search accounts for the selected hull's length, width, draft and mast height. It checks loaded terrain, depth, the full approach, turning space, buildings, rocks and other boats. The bow must be accessible from the calling shore with at most roughly 12 m of water between shore and bow. Narrow/shallow or obstructed shores may be rejected, particularly for larger ships. A rejected search leaves the ship in place. Searches are spread over frames and bounded to 48 m around the caller. Arrival clearance is checked again using the loaded ship before departure and before the final approach.

**Stay within 64 m of the calling spot.** Walking/teleporting farther away, dying or changing worlds cancels the request. The landing point stays fixed rather than following you. Use the whistle again to see progress or cancel. Removing it closes its menu but does not cancel an accepted request. Existing occupied-ship, supported-hull and single-active-request restrictions remain. Summoning retains the existing solo-only runtime guard; multiplayer untested.

The whistle has an original faceted bone-and-wood gull model, small leather wraps and two feather ties. The large loop has been removed from the model and its matching inventory icon. Both assets are embedded in the DLL. Existing Dock Ward summon controls still work separately.

## Optional ship unloading

Requires **Helmsman 0.2.8 or newer** and **Quartermaster 0.1.11 or newer**, both enabled. Stop the boat within a Deposit Chest's configured **BaseRange**, close the cargo hold and release the helm. Call the Helmsman gull, wait for it to land, then interact with the gull itself and choose **Unload cargo → Unload cargo to base**. Calling it or docking alone never transfers items.

The gull unloads one occupied cargo slot per step into accessible storage in the nearest Deposit Chest's base group and radius. Quartermaster's learned types, preferred/overflow destinations, accepting-storage settings, access checks and ordinary stack limits apply. Each successful slot queues three temporary thrown props; the next slot waits for those throws. Props bounce and fade; they are decorative and cannot be collected. Unmatched cargo or excess that cannot fit remains aboard.

You can close the dialogue while it works. **Stop unloading**, taking the helm, starting a voyage, opening the hold, leaving the boat/base, losing access or disabling either mod ends the request. A stopped/completed request never restarts automatically; talk to the gull to request another. Boats need one supported cargo hold. Existing multiplayer restrictions remain; multiplayer is untested.


## Call the gull from your ship

At the mast, **tap Use to hold fast** or **hold Use for 0.6 seconds to call the gull**. A tap attaches when you release the button; a long press calls once without attaching. Both keyboard and controller Use bindings work. Looking away, moving out of reach or opening another interface cancels a pending press. **Shift + Use** still opens ship naming directly. Passenger seats keep their ordinary interaction.

Stay aboard while the gull flies to the stern. Once he lands, interact with him and choose a configured dock. F8 can also call him, then reopen his destination menu after landing. Repeated calls reuse the same gull. A nearby ward lends its existing guide; its replacement waits until that guide flies away. **Dismiss the gull** sends him away without starting a voyage.

Release the helm and slow below 0.5 m/s before selecting a destination. Near a configured source berth, the existing boarding and forward/reverse departure checks apply. In open water, the route starts at the ship's current position. Choosing a destination transfers the perched gull directly into the voyage. Leaving the ship, dying, losing ownership or leaving solo play ends an idle visit.

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
