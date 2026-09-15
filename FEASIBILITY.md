# Valheim stýrimaður — feasibility and prototype plan

Planning assessment: 2026-09-14. A first solo Karve prototype has now been implemented; see README.md and TESTING.md for what is included and verified. In-game testing remains pending. This document describes the intended design, including work beyond the prototype.

The concept is feasible as a substantial Valheim mod. Dock naming, destination selection, and the gull presentation are relatively straightforward. Reliable navigation around islands, shallow water, structures, and crowded docks is the principal engineering risk. A prototype should demonstrate those behaviors before investing heavily in presentation.

## Evidence and its limits

- Jötunn documents cloning existing building pieces and adding them to the hammer menu. This supports a dock marker that reuses the ward appearance: [Pieces and PieceTables](https://valheim-modding.github.io/Jotunn/tutorials/pieces.html).
- Its generated prefab catalogue identifies the ward as `guard_stone`, with `PrivateArea`, and the seagull as `Seagal`, with `RandomFlyingBird`, landed/flying presentation components, and network components: [Prefab list](https://valheim-modding.github.io/Jotunn/data/prefabs/prefab-list.html). These are suitable starting assets; neither supplies the proposed navigation behavior.
- Valheim's world is divided into zones, with objects instantiated as players approach. Distant terrain estimates cannot alone establish that a route is clear of actual objects: [Zones](https://valheim-modding.github.io/Jotunn/tutorials/zones.html).
- The proposed C# stack is BepInEx, Harmony, and Jötunn, compiled against the installed game: [Developer quickstart](https://valheim-modding.github.io/Jotunn/guides/quickstart.html). Multiplayer installations can enforce compatible mod versions: [Network compatibility](https://valheim-modding.github.io/Jotunn/tutorials/networkcompatibility.html).
- A local Valheim installation and `assembly_valheim.dll` were found. A string-level inspection confirmed names such as `GetSailForce`, `GetWindAngleFactor`, `GetWindDir`, `GetWaterLevel`, and `RandomFlyingBird`. This does not verify method signatures, patch locations, or runtime behavior. Those require an implementation spike against the installed build.

The architecture below is a proposed design, not a claim that these features already exist in Valheim.

## Intended player experience

1. Build a dock and place a ward-based dock marker. A guide gull appears at it.
2. Name the marker and configure its berth using a movable, rotatable ship ghost with a clearance outline and arrival arrow. Choose forward or reverse departure. Saving settings must remain possible despite clearance advisories; the named dock becomes selectable after saving, and actual voyages validate their maneuvers separately.
3. Interact with the gull to choose another registered, accessible dock by name. If several eligible ships are nearby, select the intended ship explicitly.
4. The gull flies to that ship. Selecting the destination starts a short boarding grace period, proposed default 10 seconds, with a visible departure countdown and a cancel action. Autopilot must not engage propulsion during this period. When the countdown ends, departure proceeds only if the initiating player is aboard the selected ship and the route passes initial checks. Otherwise show “Waiting for you to board” and hold departure; expiration alone must never send the ship away without the player. Once the player boards after the grace period, show a final 3-second countdown before departure. Recheck boarding and route clearance when engaging propulsion; if the player steps off during the final countdown, return to waiting.
5. Autopilot steers, chooses sail or paddling, and slows or reverses when appropriate. A player can take the helm immediately to cancel autopilot.
6. During travel, call the gull down to an accessible perch to change destination, stop, or cancel. A perch high on the yard is visually suitable but beyond ordinary interaction reach.
7. On reaching the berth at sufficiently low speed and acceptable alignment, lower sails, set propulsion to stop, center the rudder, and release autopilot. The travelling gull flies away and despawns; immediate despawn is also acceptable for the prototype. Leave the ship to normal game behavior after arrival. No mooring or continued position-holding feature is required. The cosmetic fly-away must not delay release of ship control.

Recommended lifecycle rule: each valid dock can provide an idle guide when no departure is using it. Restore a source dock's guide after departure, so subsequent voyages do not require rebuilding the ward. Keep dock guides and travelling guides distinct in saved state to avoid duplicates after loading.

## Dock design

Recommended: register a separate nameable “Dock Ward” using the existing ward model. This retains the requested placeholder appearance and gives it dedicated interaction behavior. Adding dock behavior directly to selected ordinary wards is also feasible, but introduces more interaction and compatibility work with ward protection mods. Ordinary ward protection behavior must be an explicit design choice for the dock piece.

The marker position itself cannot be the navigation target: it sits on the dock. Store a stable dock ID, display name, owner/access policy, marker position, berth position, final ship heading, approach corridor, supported ship profile, departure mode, and optional reverse-clearance point. Renaming must not break routes; duplicate names need a distinguishing location or owner label.

Dock configuration and saving must work without any real ship present. The ship ghost is sufficient to place the berth and choose departure settings. Copying a real ship's pose is only an optional shortcut. Require ship selection when starting a voyage, not when editing the dock.

The berth editor should use both a translucent ship ghost and a surrounding rectangular clearance outline. The ghost makes the bow, stern, scale, and final orientation readable; the outline shows the space reserved for clearance beyond the hull. An arrival arrow and corridor show how the ship reaches that final pose. Moving and rotating the ghost sets the berth independently of the ward's placement. Use the supported ship's dimensions, including depth and mast clearance; the rectangle is a visual aid, not a substitute for checking the actual swept hull and overhead geometry.

Show green with “Valid,” red with a specific obstruction or depth reason, and amber with “Not fully checked.” These are advisories during configuration, not save restrictions. Keep the preview visible during configuration or when explicitly requested. Offer “Set berth from this ship” as a convenient alternative starting position, followed by the same advisory checks. For the initial version, use a straight, bow-first final approach aligned with the ghost; turning into that approach happens in open water. A voyage requiring a more complicated maneuver can be rejected with an explanation even though its berth settings were saved.

Validate surrounding water depth, hull clearance, approach corridor, and the space occupied throughout departure. This is a one-time dock setup step; route plotting between docks remains automatic. Preview validation describes current conditions and must be repeated when a ship uses the dock.

### Reverse departure

Include a ward setting labelled “Departure mode” with “Forward” and “Reverse out, then turn.” This controls departure from this dock independently of the chosen destination. Reverse departure is a planned maneuver, separate from emergency stuck recovery.

When reverse departure is selected, extend a visible corridor behind the ship ghost to a proposed point in open water. Let the player move that clearance point along the initial reverse direction if the automatic suggestion does not fit the dock. Preview the proposed turning area and validate the full hull sweep, including bow swing, rather than testing the endpoint alone. Start with a straight reverse corridor; curved backing maneuvers can be deferred.

Departure sequence: keep sails lowered → reverse slowly along the checked corridor → bring backward motion under control once the whole hull clears the dock → use low-speed propulsion and rudder to turn through a checked arc → join the route toward the destination → raise sails when appropriate. The ship must not attempt to pivot in place or begin its turn while the bow is still between dock structures. The outgoing route determines the required heading; it need not be an exact 180-degree turn.

Recheck stern clearance, depth, the turning area, and current ship position before and during departure. A changed or occupied exit requires waiting or player intervention. A ship parked outside the configured pose needs a newly validated maneuver; do not blindly replay the saved reverse path. If there is room to back out but insufficient room to turn, report that separately and request a farther clearance point or a different berth.

Maintain a persistent world dock directory so unloaded destinations remain selectable. Update it on creation, rename, access changes, and destruction. Revalidate the destination on approach because its geometry or occupancy may have changed.

## Navigation architecture

Separate the route planner from the ship controller and gull animation.

**Regional planner:** Build a coarse water graph from terrain samples and search it with A*. Penalize coastal proximity and unsuitable depths, exclude the world boundary, and refine near shore. Do not exclude whole biomes by name. Use a configurable depth minimum, initially 1 m. Inflate obstacles for hull width and safety margin. Check the entire connection between samples, including diagonal passages, and validate any route smoothing. A route a point can traverse may still be impossible for a turning ship.

**Local avoidance:** Inspect loaded terrain and colliders ahead of the ship using hull-sized sweeps and multiple depth samples. Account for rocks, docks, other ships, terrain edits, and mast clearance. Ignore the controlled ship's own geometry. Add newly observed obstructions to the local route and request a wider replan when needed.

**Controller:** Follow a look-ahead target with rudder control and select propulsion according to wind, measured speed, curvature, and available stopping distance. Use existing ship control behavior where practical. Measure each ship's turning and slowing response instead of assigning unverified universal constants.

Use sails when useful and clearance permits; use paddling into unfavorable wind or during close maneuvering. Lowering the sail is not an instantaneous brake. Apply measured reverse thrust when needed, checking stern clearance and reverse steering behavior. Add separate enter/exit thresholds and minimum dwell times so changing wind does not cause rapid sail/paddle oscillation. Tacking can be a later optimization; rowing into the wind is sufficient for the first version.

Stopping distance must remain within confirmed clear water, with margin for sensing delays, loading delays, waves, and drift. Unknown space is not automatically safe. If planning fails, slow while clearance permits and report that a safe route is unavailable. Do not promise a collision-proof autopilot under every condition.

Proposed voyage states: awaiting boarding → departing → cruising → approaching → docking → arrived, with explicit replanning, recovery, paused, and cancelled states. Departure includes reversing out, slowing, and turning substates when the source dock requests reverse departure. Detect lack of progress; attempt a bounded reverse-and-turn recovery only when the escape area is clear. Escalate to player control after repeated failure.

## Gull and multiplayer behavior

Reuse the native gull visuals with a custom guide controller. Replace the guide instance's ambient wandering/fleeing behavior, and give it dedicated interaction and lifecycle handling. Keep the autopilot independent of its animation.

Represent its onboard perch using a ship ID and local attachment transform. Synchronize those facts and reconstruct the visual attachment on clients, rather than assuming a transform parent change alone will replicate correctly.

Run ship decisions only on the peer currently authorized to simulate that ship. Validate destination/control requests and transfer voyage state safely when ownership changes. Prevent two passengers from issuing competing commands; give the initiating player control, with manual helm takeover as an override. Require the mod on participating clients and server for the multiplayer release.

Recommended initial scope requires a player aboard. Empty ferries and offline voyages need additional simulation/streaming work. On passenger loss, disconnect, ship destruction, or reload, reconcile the state and attempt a controlled stop where simulation is available; never treat an unloaded ship as actively navigating. Resume saved trips only after validation and player action in the first release.

## Prototype stages and decision gates

1. **Ship control spike:** One vanilla ship, preferably the Karve. Demonstrate waypoint following, wind-dependent sail/paddle choice, reverse, slowing, and immediate manual takeover. Gate: repeatable steering and measured stopping behavior without teleporting or replacing normal ship physics.
2. **Navigation spike:** Automatically route around an island and react to a submerged rock and blocked passage. Test changing wind and newly loaded obstacles. Gate: arrive or report failure without repeatedly grounding or circling indefinitely. This is the main feasibility gate.
3. **Dock-to-dock loop:** Two named wards, saved directory, ship ghost berth editor, validated arrival and departure corridors, ship selection, and controlled arrival. Include a bow-in berth that requires reversing out before turning. Gate: reload preserves dock identities and departure settings; blocked or destroyed destinations are handled; arrival requires position and low speed rather than proximity alone; reverse departure clears the entire hull before turning and stops safely for a blocked exit.
4. **Gull experience:** Spawn, destination menu, boarding flight and grace-period countdown, accessible onboard interaction, mid-voyage rerouting, and arrival cleanup. Gate: the whole loop can be repeated without duplicate or missing guides; departure waits through the grace period, cannot start without the initiating player aboard, and returns to waiting if that player steps off during the final countdown.
5. **Multiplayer and expansion:** Verify ownership transfer, two-player command conflicts, disconnect/reconnect, and dedicated-server persistence. Then calibrate other vanilla ship types and broader environments.

Use a controlled test world and a debug overlay showing route, sensed obstacles, target heading, propulsion mode, and stopping margin. Include sheltered and exposed berths, headwind, crosswind, following wind, narrow rejected channels, occupied berths, no-route destinations, and low frame-rate conditions. Declare supported test conditions explicitly; a few successful trips do not establish universal safety.

## Recommended initial boundaries

- One vanilla ship type, one active voyage per ship, player aboard.
- Automatically generated routes through ordinary open sea and wide coastal passages.
- One-time berth setup at each named dock; no requirement to record or manually sail routes.
- Normal game wind and ship physics; paddling handles headwind initially.
- Defer unmanned shipping, perfect docking in arbitrary structures, advanced tacking, combat avoidance, custom/modular ships, and hazardous-region navigation until the core passes its gates.

Proceed to a navigation proof of concept if implementation is requested. This is a multi-stage development project; reliable timing estimates should follow the ship-control and navigation spikes.
