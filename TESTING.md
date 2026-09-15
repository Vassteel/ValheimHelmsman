# Prototype validation

## Completed outside the game

- Compiled both libraries against the installed Valheim 1.0.12 assemblies and local BepInEx/Jötunn.
- Build completed with zero warnings and zero errors.
- 66 automated core assertions passed: early/late boarding, absence after countdown, countdown reset on disembarking, island routing, exact endpoints, per-edge clearance, blocked starts/goals, search budget, speed-dependent stopping margin, reverse steering sign, seven turn-mode/hysteresis checks, and eleven mood-priority/settling checks, twelve gesture/transition/bounds checks, and fourteen ship-name/compass checks.
- Inspected the installed ship implementation to confirm the owner simulation path, speed/rudder fields, wind factor, manual controls, and the occupied-control requirement for rowing/reversing.

These checks do **not** run Unity physics or prove that Harmony patches, the ghost, the gull, or ship control work during play. In-game results below are pending until observed.

## In-game acceptance checklist — pending

| Test | Expected result |
|---|---|
| Launch | Log reports `Helmsman 0.2.0 loaded` and `Registered Dock Ward`; no Helmsman exception. |
| Build ward | Dock Ward appears in its own **Helmsman** hammer category; placing one provides a gull and an interaction menu. |
| Theme | Dock/gull menus and voyage status use Quartermaster's charcoal/gold theme and Valheim font. Verify no missing-font warnings and readable sizing on Steam Deck. |
| Menu input | Mouse and controller can select ships/destinations, edit the name, adjust sliders, switch tabs, save and close. A activates once; B closes; no gameplay input leaks while editing. |
| Adjust view | Button hides the menu and releases camera input; preview remains. F8/Escape/B returns to the same tab and unsaved berth values, with menu input blocking restored. Repeat, then close; camera remains usable. |
| Overlay | Ghost and berth guides stay visible behind dock structures; leaving configuration removes them. Confirm shaders and ghost materials do not change the actual ship or dock. |
| Editor | Ghost has the correct scale/orientation; cyan arrival and orange departure are legible. |
| Clearance advisories | Shallows, dock posts, other ships, and low overhead structures produce editor advisories without preventing save. Selected source ship is excluded. An actual voyage still checks clearance. |
| Save despite warnings | Set a named berth with shallow water or unloaded approach area, save, close and reopen/reload: name, pose and reverse setting persist despite the advisory. |
| Configure without ship | With no ship nearby and no selected ship, position the ghost, name the dock, set reverse departure and save. Settings persist after reopening. Copying a real ship's pose is optional; starting a voyage still requires a ship. |
| Relaxed depth/regions | Navigation defaults to 1 m minimum depth, including its submerged collision envelope. A biome name alone never rejects a sample. World-edge and physical checks still apply. |
| Save/reload | Dock name, berth pose and reverse setting persist; no duplicate guides or directory entries. |
| Initial voyage | Park within 20 m, offset from the ghost and facing a different heading; release helm. Countdown starts, then plotting is displayed, and departure uses the real ship pose. Beyond 20 m still rejects. |
| Boarding | Remain ashore beyond 10 seconds: ship does not depart. Board: final 3 seconds count down. Step off: return to waiting. |
| Relaxed dock checks | Nearby built posts/decking do not automatically pause slow departure/arrival. They still physically collide. Rocks and another Karve still block, and pause text identifies the object. Disable relaxed checks and verify strict validation returns. |
| Seated boarding | A passenger sitting on the ship counts as aboard; remaining ashore never authorizes departure. |
| Forward departure | Lowered sails and slow propulsion clear the berth before route following. |
| Reverse departure | Back out with lowered sails, clear the entire bow, slow backward motion, then turn in open water. |
| Occupied exit | Another ship blocks departure; autopilot reports the obstruction and does not push through. |
| Wind | Headwind selects paddling; useful wind selects half sail after the dwell period. |
| Turning speed | In useful wind, 30–75 degree cruise turns keep half sail. Above 80 degrees, coast with steering above 2.5 m/s, then paddle. Below 55 degrees, wind-based propulsion resumes without repeated sail toggling. Obstruction and dock braking still work. |
| Island route | Generated waypoints pass around the island and the actual hull follows without grounding. |
| New obstruction | Slow and attempt a bounded replan when a cruise obstacle appears; terminate retries after three failures. |
| Manual takeover | Using the helm cancels autopilot before ordinary control is granted. |
| Onboard interaction | F8 gets the stern gull's attention and exposes stop/change-destination controls. |
| Gull perch | Dock gull feet rest on the ward top. Travelling gull settles on the aft centreline tip, clear of the mast and rudder interaction. It stays perched through waves without hovering or flapping each frame. Fine-tune Gull config if the visual stern tip differs from its collider. |
| Gull idle | Calm: separate head looks, two pecks, preening and tail flick. Rough: counter-roll, tail spread, flutter-hop. Storm: tuck, feather shake, brief peek. Fog: alternating held horizon scans, head tilts, lookout-hop. Combat: face threat, large wingbeat hop, forward jabs, second hop. Feet remain planted outside intentional hops. No torn neck/tail, mesh drift or model-height pop. Flight uses native wing animation. |
| Single travelling actor | Start beside the ward: its existing gull flies up and across to the stern, with no bird left at the source. Unload/reload that ward mid-voyage: no duplicate appears. Arrival/cancel flies away and cleans up; only after despawn can the source guide reappear. Removing the source ward while underway must not destroy its travelling gull. |
| Gull moods | Mood changes are communicated through animation only. Hold weather conditions for 3 seconds. Nearby combat overrides weather immediately, lingers, then settles. No reactions to distant fights or friendly idle creatures. |
| No gull labels | No floating text, kaomoji or mood suffix in any state, including when an older config has ShowExpressions=true. Basic hover interaction and F8 still work. |
| Changed destination | Mid-cruise selection replans toward the new berth. |
| Arrival | Position within 2.5 m, heading within 15 degrees and horizontal speed below 0.35 m/s; stop propulsion, center rudder, release autopilot and despawn travelling gull after fly-away. |
| World changes | Removed or reconfigured destination produces a stop message. An altered source during boarding prevents departure. |
| Passenger/authority loss | Leaving the ship, death, opening multiplayer, or ownership loss ends autopilot. |
| Ship naming | Shift + Use at mast hold-fast and helm opens the themed name editor; ordinary Use still works. Name persists after reload and appears in the summon list. Naming at the helm does not cancel an active voyage. |
| Ghost profiles | Cycle Karve, Longship and compatible modded previews without a real ship. Copying a nearby ship also selects its profile. Clearance outline follows hull size/offset and save remains independent of advisories. |
| Nearby summon | Name an empty ship, summon from another configured dock: the existing gull flies to the ship, boards the stern, and returns under autopilot. No boarding countdown or duplicate guide. |
| Unloaded summon | Walk far enough to unload the named ship, then summon it. The gull reaches it; terrain and objects load before propulsion; the ship travels continuously rather than teleporting. Check for errors and frame-time spikes. |
| Totem status | Ship name, distance, compass direction and status update above the destination totem. Status disappears on completion/cancel and returns after leaving/reloading the ward area during a summon. |
| Summon cancellation | Cancel from the menu, board the ship, remove the ship/destination or die: propulsion stops, guide flies away and extra simulation area is released. An occupied ship cannot be selected for summoning. |
| Ship compatibility | Repeat departure, island avoidance, overhead clearance, reverse exit and arrival with Longship and each modded ship. Ships overriding standard movement are excluded with an explanatory message. |
| Repeat trip | Guide available at each dock; another voyage can be selected without rebuilding wards. |

Measure actual stopping distances, reverse speed, turning arcs, and berth tolerances during these tests. Repeat in calm water first, then crosswind and rough water. Monitor frame times during route planning.

## Deliberate prototype limits

- Solo worlds only. Ordinary voyages require a player aboard; explicitly summoned empty ships use a separate simulation area. Karve, Longship and compatible standard-physics modded ships are detected. Custom movement systems need adapters.
- The Longship estimate is at least 6 m wide, 22 m long, 20 m mast height and 1.5 m draft. Other hull profiles derive from float colliders and mast geometry. These estimates and their effects on routing need in-game calibration.
- Only one active voyage/summon. Loading a second area may increase frame time and activates nearby world objects. Cancelling or completing the summon releases that extra area.
- Names and docks persist; active summons do not. Summon discovery includes named ships throughout the current solo world.
- The Karve relaxed estimated envelope is 4 m wide, 10 m long, with 0.2 m margins and configurable depth (default 1 m). The broad hull check reaches 1.5 m above the water, with a separate narrow 12 m mast check. The 16 m turning disk is advisory by default. These are **engineering estimates, not measured ship dimensions**. Ignored dock pieces can still physically block the ship.
- Final approaches and reverse exits are straight. Curved reversing and arbitrary harbor maneuvers are not supported.
- Berth position uses sliders rather than free placement with the hammer. The stern perch probes the ship collider, with a configurable local fallback; placement, feet alignment and animation still need visual QA.
- Half sail is the initial sailing ceiling; full sail, tacking, and optimized voyage times are deferred.
- Global terrain samples estimate uninstantiated areas. Loaded terrain and physics checks are required before the ship moves into them. Biomes are not excluded by name; normal environmental hazards still apply. The world-edge navigation exclusion remains.
- Route search uses 24 m grid spacing, checks segments at smaller intervals, and has a 20,000-cell cap. Planning is incremental with a target per-frame budget; an individual Unity/world-generation query can still exceed that target.
- Automatic cruise replanning is bounded to three attempts per voyage. Docking failures pause for manual action. Autonomous reverse recovery after grounding, berth reservations, and offshore waiting loops are deferred.
- The initial braking estimate is 0.25 m/s² with added reaction distance/margin. Wave/current drift, lateral motion and modded ship forces can invalidate that estimate. This requires real ship testing.
- Active voyages are not restored on load. Dock settings persist on ZDOs; the trip must be selected again.
- Pause uses ordinary propulsion controls to reduce motion; it is not mooring. There is no position hold after arrival.
- Compatibility with ship-physics mods and other UI/input mods is untested.

## Useful bug report

Provide the action that failed, source/destination settings, whether the ship was moving, the visible status message, and the Helmsman-related lines from `BepInEx/LogOutput.log`. The default diagnostic route overlay can be disabled in the Helmsman config.

## User-observed progress

The user reports a successful departure and ongoing land avoidance with the 0.1.3 test session. This does not yet validate arrival, the subsequent turning-speed changes, or 0.1.5 gull visuals.

## Local animation review for 0.1.6

Inspected the actual vanilla gull hierarchy: the sitting model is a readable 344-vertex static mesh, with no Animator and zero native idle variants. The flying model has an Animator and left/right wing bones. Reviewed deformed sitting-mesh snapshots and sampled 700 local deformations for finite/bounded vertices. These renders omit game textures and do not validate the in-game flying-model transition.

## Local API verification for 0.2.0

Seven checks against the installed assembly passed: the two empty-boat crew-count branches used by the physics patch and the six method signatures used by naming and remote-area hooks. The empty-ship patch declines to activate if the expected two branches are not found. These are structural checks, not a runtime Harmony or remote-world simulation test.
