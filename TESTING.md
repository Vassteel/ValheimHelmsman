# Helmsman validation — 0.2.30

## Observed local play

Earlier builds have been used for departure, ongoing land avoidance, gull interaction, status messages and the whistle. User feedback drove mast controls, cargo tosses and visual fixes. This does not verify newly imported ships, the new bird models or multiplayer operation.

## Automated verification

Release compilation: zero warnings/errors. Current checks:

- 357 Core checks: routing, boarding, shoreline clearance, ship eligibility, construction phases/durations and upgrade costs.
- 64 production interaction/cargo checks; 28 whistle checks; 16 summon-lifetime checks.
- 18 production server reservation/loading checks: competing callers, missing manifests, expiry/disconnect, occupied ships and manual ownership takeover.
- 13 production workshop checks: authority handoff, competing players, duplicate clicks, timeout, impersonation and one payment per accepted action.
- Native binary inspection resolves game/Unity/Jötunn members, Harmony targets, reflected fields and the two empty-crew branch sites. Container checks cover 6 save-key accesses and 19 RPC-name accesses.
- Asset verification: 42 public roots, 15 native player ships, 24 containers, 38 material variants and 156 component/visual bindings. No original Odin plugin scripts or enemy roots remain.
- Production bird geometry exported for CPU preview with finite vertices and valid triangle indices. This does not reproduce Unity shaders or lighting.

Host doubles do not simulate Unity physics, GPU output or real network latency.

## Server acceptance — still to verify

| Area | Check |
|---|---|
| Startup | Matching 0.2.30 clients/server; no original Odin/LongshipUpgrades DLLs. Fleet, table, harbor items and whistle register without missing-script/shader errors. |
| Existing world | Old hulls, names, styles and every cargo hold survive replacement/save/reload. Missing cargo items lock migration instead of losing contents. |
| Construction | Two players try one table; materials are charged once. Restart midway; timer resumes. Occupied/shallow berth waits. Dismantling refunds once. Each hull fits its berth. |
| Birds | Puffin tools, owl three-toss sorting, pelican peg leg/fishing and all sleeping poses. Feet stay on surfaces; no self-lit feathers indoors or at night. |
| Ship textures | Inspect all hulls and style variants in sunlight, rain and darkness; UV alignment, coarse texture scale, water masks, sail motion and transparent parts. |
| Refits | Costs/station checks, shelter/rest, inventory-only expansion, protective treatment, trophy metadata and saved cloth choices survive reload/ownership change. |
| Fishing | Daylight catches, cap/open-chest pause, net deployment/collection, full cargo and ten-fish oil batch. No duplicates across ownership changes or destruction. |
| Navigation | Each hull: reverse departure, turning, braking, shallow-water pause and clear arrival. One-seat canoe recall works. No teleportation. |
| Remote recall | Call across unloaded sectors; leave the calling area; ship reaches that fixed spot. Competing caller, helm takeover, disconnect and reconnect never steal control or duplicate the voyage. |
| Cargo | Explicit request only. Across all holds: one slot and three tosses per step, quantity/metadata conservation, full/blocked storage and ward/access changes. |
| UI | Password field stays focused; hotbar/status alignment, controller navigation and rebound F8 work. Menus do not consume typing outside their own focus. |

No new-fleet or multiplayer acceptance pass is claimed until these run in the actual server world.

## Island scouting — 0.2.27

Production host tests cover connected terrain, narrow channels, bounded survey work, report serialization, personal map application, duplicate pins, server identity/access checks, persistence, reconnects and interrupted client saves. Native API inspection checks the private map and ward delegates.

In-game acceptance pending: choose **Scout island** at a configured Dock Ward with a vanilla Cartographer’s Table within 100 m. Confirm departure, the minimum five-minute survey, return flight, tabletop perch and arrival squawk. Speak to the returned gull: only then should that island’s terrain and POIs appear on your personal map. Check a separate nearby island remains hidden, a second collection adds no duplicate pins, and a replaced table can be selected. Repeat with two clients and across a server restart.

Terrain boundaries use 8 m samples of generated land; very narrow land bridges or channels can fall below survey resolution. No terrain zones are spawned by scouting. Per-world report metadata is stored under BepInEx/config/ValheimHelmsman/scouting; preserve this directory when migrating a server.

## Navigation feedback — 0.2.28

20 production navigation/local-detour checks cover fish and gull exclusions, retained rock/terrain/other-ship blocking, loaded-zone requirements, a passage missed by the 24 m regional grid but found at 6 m, and preservation of the remaining voyage. These use host physics doubles, not Unity collision simulation.

The installed game enables trigger ray queries; native Player.FindHoverObject uses that setting, allowing the gull’s trigger to remain interactive. The gull’s trigger is disabled before detaching on takeoff. Native Ship.UpdateWaterForce has a default 10-damage water impact, so the report’s exact damage source still requires a controlled in-game reproduction; ordinary ship damage has not been disabled.

In-game acceptance pending: sail a Karve through fish, around loaded submerged rocks and along a shallow shoreline. Confirm fewer spurious replans while real hazards remain blocked. Leave/reboard a stationary repaired Karve in calm water repeatedly, observe gull hover/use/takeoff and record ship health; compare with a gull-free ship and waves.

## Rock clearing and fish pass-through — 0.2.29

Production host checks cover ordinary stone-only classification, range/speed/cooldown, ownership/wards/manual helm, native drop capture, cargo access and capacity, partial insertion rollback, resource metadata, zero-stack disposal, continued clearing with full cargo, and fish-only collision cleanup. Native binary checks validate damage-handler delegates and the exact OnCreateNew overload.

In-game acceptance remains pending: enable rock clearing on a Karve and approach ordinary shoreline rocks slowly. Verify hits stop beyond the roughly 9 m centre-to-rock cap, stone quantities match native drops, filled holds leave stone behind, and all imported ship holds retain contents after reload. Check intact boulder conversion and fractured-rock support collapse. Confirm rock clearing does not run on an unattended ship recall.

Fish pass-through defaults on. Check fish remain alive, fishing/net interactions remain usable, and manual takeover/ending the voyage restores normal hull contact. Repeat with two clients; clearing requires the local peer to own both boat and rock.

## Redesigned fleet acceptance (0.2.30)

Automated geometry and binary-resource checks cover all six redesigned ships, rudder sweep, sail clearance, closed convex keel sections and continuous merchant cargo supports. The compiled DLL embeds the checked resources. These do not constitute an in-game physics test.

Pending in-game: commission each ship; test stop/half/full sails and steering; board both ladders; sit and use the mast holdfast; open cargo (Currach: 3 × 2); walk the merchant load from end to end; change paint/cloth and reload. Check existing ships retain saved cargo and names, and test a second client observing seating, cargo and net fishing. Hull dimensions changed, so inspect existing moorings as well as newly launched ships.
