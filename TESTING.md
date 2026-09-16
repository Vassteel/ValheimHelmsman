# Helmsman validation — 0.2.16

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
| Startup | Matching 0.2.16 clients/server; no original Odin/LongshipUpgrades DLLs. Fleet, table, harbor items and whistle register without missing-script/shader errors. |
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
