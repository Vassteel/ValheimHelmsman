# Helmsman guide — 0.2.35

Existing sailing has been used in solo/local play. **This build enables multiplayer; the new fleet and server behavior still need in-game verification.** This guide describes current controls; [Testing](https://github.com/Vassteel/ValheimHelmsman/blob/master/TESTING.md) separates observed play from remaining checks.

## Install

Install BepInEx, Jötunn and Helmsman through your mod manager. For manual installation, copy both `ValheimHelmsman.dll` and `Helmsman.Core.dll` to `BepInEx/plugins/ValheimHelmsman/`. Install the same Helmsman patch version on every client and the server. Close Valheim before replacing client DLLs; restart the server after its files are updated. Remove original OdinShip/OdinShipPlus and LongshipUpgrades DLLs to avoid overlapping prefabs and refits. Keep a world backup from before replacement. Avoid duplicate copies in your active profile.

Look for `Helmsman 0.2.35 loaded`, `Registered Dock Ward` and `Registered hand-crafted Gullcall Whistle` in `BepInEx/LogOutput.log`.

## Configure docks

1. Build a **Dock Ward** beside open water. It uses the vanilla ward recipe and is a berth marker, not a protection ward.
2. Use the ward to name the dock, position its ghost ship and set the bow direction. **Preview ship** selects a hull; a real ship is optional. You can copy a nearby ship's pose.
3. In **Departure**, choose forward departure or reverse out before turning. Save the berth.
4. **Adjust view** temporarily releases the camera. F8 (or your CallGullKey), Esc or controller B returns to editing.

Cyan shows arrival; orange shows departure. Clearance warnings are advisory while saving. The ship checks its actual hull when sailing. Give large boats deeper water and a wider approach.

## Sail

At the mast, **tap Use to hold fast; hold Use to call the gull**. Speak after he lands and choose a named dock. F8 calls or speaks to him too. Release the helm and slow below 0.5 m/s before giving orders.

You can also select a nearby ship and destination at a dock gull. The boarding countdown waits for you; returning after the grace period gives a final countdown. A voyage started at sea plots from the ship's current position.

The gull rows or uses half sail as conditions allow. Speak to change destination once clear of the departure maneuver, ask **What's happening?**, or stop the voyage. Taking the helm cancels autopilot. Leaving the ship or dying also ends an ordinary passenger voyage.

Squawks and chat explain delays and announce arrival. The gull settles on his perch after arrival; speaking again allows another order. Weather and combat gestures are cosmetic.

The status panel sits beneath the hotbar. Blue route wisps float four metres above the navigation course. Toggle them in any gull, dock or whistle menu; the choice is saved.

## Name and fetch a ship

**Shift + Use** at the mast or helm names a ship. Names persist in the world. Allow a few seconds for the list to refresh.

At a configured Dock Ward's gull, choose **Summon ship** and an empty named ship. The gull flies out, then sails it back. **This takes travel time; the ship is not teleported.**

### Gullcall Whistle

Handcraft in the inventory menu with **4 bone fragments, 2 wood, 2 leather scraps and 2 feathers**. Discover the ingredients to unlock the recipe. No station is required. The custom bone-gull whistle has an inventory icon, weighs 0.2, does not stack and is reusable.

Use it from the inventory or hotbar while standing near shore or on a low pier. Select an empty named ship. The gull checks safe water for that hull and sails it to the **original calling spot**. You can walk or teleport elsewhere; the destination stays fixed. No Dock Ward is needed.

Use the whistle again to check progress or cancel. Putting it away does not cancel an accepted order. One voyage or summon can run at a time. Boarding the fetched ship, taking the helm, player death, ending the world session, or losing the ship ends the summon. Removing a destination ward ends a dock summon.

The mod keeps terrain and objects loaded around the travelling ship, which adds simulation work. Blocked or shallow approaches pause for intervention. Arrival releases the helm; it does not anchor the boat. Active orders do not resume after restarting.

## Unload with Quartermaster

Both mods must be enabled. Stop aboard a boat with one supported cargo hold, release the helm and close the hold. Within a Quartermaster Deposit Chest's base range, speak directly to the landed Helmsman gull and request **Unload cargo to base**.

One occupied cargo slot transfers per step. The gull throws three temporary trinkets, which bounce and fade. Unmatched items and items without storage space remain aboard; the gull explains why. The request stays tied to the chosen base. Movement, access/ownership changes, a changed base group or an opened hold stop it. Approaching a base never unloads cargo automatically.

## Settings

Generated file: `BepInEx/config/local.valheim.helmsman.cfg`.

| Section / key | Default | Purpose |
|---|---|---|
| Controls / CallGullKey | F8 | Call or speak to the gull |
| Voyages / BoardingGraceSeconds | 10 | Initial boarding grace, 3–60 seconds |
| Diagnostics / ShowRoute | true | Show raised blue route wisps |
| Navigation / MinimumWaterDepth | 1 | Minimum depth, 0.25–5 metres; larger hulls can require more |
| Navigation / RelaxedDockChecks | true | Allow nearby departure from the actual pose; built dock pieces become advisories during slow dock maneuvers |
| Gull / SternPerch | (0, 1.7, -4.3) | Ship-local perch probe; negative Z is aft |
| Gull / PerchHeightAdjustment | 0 | Final perch height adjustment, -2–2 metres |

Relaxed dock checks still respect terrain and other boats. Dock pieces still physically collide with the ship. Whistle landings always use strict clearance checks.

## Saves and support

Dock settings and ship names persist. Keep a world backup and remove custom Dock Wards before uninstalling from a world you intend to keep. No trip resumes automatically on load.

For a problem, include the ship type, action, visible status, berth settings and Helmsman log lines. See [Compatibility](https://github.com/Vassteel/ValheimHelmsman/blob/master/COMPATIBILITY.md) for supported physics and mod conflicts.


## Shipwright and fleet

Build ships and harbor pieces with **Hammer → Helmsman**, using their displayed material costs and an ordinary Workbench. Zero-cost building uses the normal game setting. Maritime supplies (sail canvas, marine rope and prepared timber) are crafted at the ordinary Workbench.

The puffin handles paint and decoration at the Carpenter's Table. New timed commissions are disabled for now. Previously paid orders still finish at their saved berth, and dismantling an unfinished table returns paid materials.

The fleet has nine authored models: Dugout, Finewood Kayak, Tandem Finewood Kayak, Currach, Ceol, Falkuša, Ottar, the large freighter and Snekkja. The native longship is also available. The dugout costs 12 wood and paddles slowly; the kayaks cost fine wood, ordinary wood and resin. Use the seated helm to paddle forward or backward. The tandem's front passenger follows the paddling animation while seated. Side grab loops provide boarding interactions from the water.

The eight remaining source-model ships and the two old canoe IDs have been removed. This testing-world build does not provide compatibility aliases for them. The retained harbor and décor assets have a separate replacement plan.

### Refits and styles

Visit the puffin for **Paint and decoration** on a ship within 35 m. Take its helm first if another peer owns it, then stop and return to the table. Change hull finishes, sails, shields and figureheads where supported. Vanilla longships can add a decorative lantern or trophy mount; existing canopy styles remain available. Cargo and fire-treatment refits are not offered in this menu.

Imported ships expose their available figurehead/deck, shield, hull and sail variants. Decorative choices do not grant boss powers or weapon attacks. Custom PNG sailcloth belongs in `BepInEx/config/Helmsman/ShipStyles/sails/`; vanilla-longship canopy textures use `canopies/`. Each client needs the same custom files; only the style name is synchronized. PNG files must be no larger than 8 MB or 4096×4096.

## Fishing and harbor work

Harbor decorations, dock extensions and processing pieces appear in the Hammer's **Helmsman** category. Resin wood, caulked wood, sail canvas, rope and the wind belt are crafted at the Carpenter's Table using their original recipes.

- **Fishing Dock:** the pelican catches fish during daylight into the dock chest. Default interval: 20 seconds; default cap: 50 fish. Both are configurable. Full/open storage pauses production; unloaded time does not accumulate catches.
- **Falkuša fishing boat:** board and use its net interaction to lower/stow the fishing net. Nearby fish enter its cargo hold only while the net is deployed and storage has room.
- **Oil press:** add ten fish. One bottle of fish oil ejects after ten minutes. Destroying a press mid-batch returns fish instead of skipping the timer.
- Eel racks, fish dryers and loose harbor nets are decorations.

The gull, puffin, owl and pelican have sleeping poses. Construction continues overnight; fishing waits for daylight.

## Server operation

The connected caller controls their voyage; the server reserves remote ships and sends the loading data needed for a summon. Another caller cannot take the same reservation. Disconnecting ends the request, and taking the helm overrides autopilot. This is not an offline ferry service. Do not mix different Helmsman patch versions.

Install Quartermaster on the server and clients for requested cargo unloading. Open cargo holds, unavailable migrated cargo, ward restrictions or ownership changes stop unloading. All holds are checked; one occupied slot moves per step across the whole boat.

## Island scouting

At a configured Dock Ward, choose **Scout island** and a Cartographer’s Table within 100 m. The gull surveys the connected landmass for at least five minutes, then returns to the table and squawks when you approach. Speak to him to reveal the island’s terrain and add its points of interest to your personal map. Nearby separate islands remain hidden.

Uncollected reports survive reconnects. Use the dock’s scouting menu to choose a replacement return table or cancel a survey.

## Navigation options

Open the gull’s **Destination / Destinations** menu to toggle **Rock clearing** and **Fish pass-through**. Rock clearing starts off; fish pass-through starts on.

While you are aboard under gull control, rock clearing targets natural stone rocks obstructing the hull, capped at 4 m beyond half the ship’s length (about 9 m from a Karve’s centre to the nearest rock surface, including its navigation margin). It slows before each hit and collects that rock’s stone into accessible cargo holds. Full cargo does not stop clearing: excess stone remains in the world. Clearing permanently changes rocks; terrain, ore deposits and build pieces remain obstacles. Remote-owned or ward-protected rocks cannot be cleared.

Fish pass-through only changes fish collisions with the gull-controlled boat. Fish remain alive and normal collisions return when autopilot ends or the option is disabled.
