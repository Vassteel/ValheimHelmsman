# Helmsman guide — 0.2.15

Helmsman works in solo/local play. **Multiplayer and dedicated-server operation are disabled by the mod.** This guide describes current controls; [Testing](https://github.com/Vassteel/ValheimHelmsman/blob/master/TESTING.md) separates observed play from remaining checks.

## Install

Install BepInEx, Jötunn and Helmsman through your mod manager. For manual installation, copy both `ValheimHelmsman.dll` and `Helmsman.Core.dll` to `BepInEx/plugins/ValheimHelmsman/`. Close Valheim before replacing DLLs, then restart. Avoid duplicate copies in your active profile.

Look for `Helmsman 0.2.15 loaded`, `Registered Dock Ward` and `Registered hand-crafted Gullcall Whistle` in `BepInEx/LogOutput.log`.

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
