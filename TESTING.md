# Helmsman validation — 0.2.15

Helmsman works in local solo play. The checklist below tracks specific remaining regressions; it does not label all existing gameplay as untested.

## Solo verified

- User play confirmed departure and ongoing land avoidance in a local world.
- Later sessions show the gull interaction and voyage status running. The current screenshot shows a shallow-water pause and the whistle in the hotbar.
- User feedback has guided mast controls, cargo throwing effects and visual fixes. This does not establish that every later change or ship has been exercised.

## Automated checks

Release build: zero warnings or errors.

- 120 Core checks: routing/boarding, both sail representations, shoreline clearance, binary model validation and coroutine failure cleanup.
- 64 mast/gull interaction and optional-cargo checks.
- 28 production whistle registration/use checks.
- 16 production summon-lifetime checks, including moving far away during shoreline search and sailing while preserving the original destination.
- 475 game/Unity/Jötunn member references, 12 Harmony targets, reflected fields and both native empty-crew branch sites match the installed game. Plugin, Core and manifest versions agree.

Tests with host doubles do not simulate Unity physics, rendering or real multiplayer.

## Still to verify

| Area | In-game check |
|---|---|
| Launch | Log says `Helmsman 0.2.15 loaded`, followed by Dock Ward and Gullcall Whistle registration. No duplicate plugin DLLs or patch errors. |
| Ship discovery | Karve and Raft with the current cloth sail flag appear; legacy modded sails and eligible two-seat rowboats still work. Inspect `Detected ships:`. |
| Whistle | Craft by hand after discovering ingredients; use from inventory/hotbar without consuming it. Confirm model, icon and normal save/reload. |
| Fixed destination | Call from shore, walk or teleport well away during search/fetch/travel, and verify the ship reaches the original spot. It must not follow the player. Repeat from a Dock Ward. |
| Arrival safety | Small and large ships reject shallow, rocky or occupied approaches. New obstructions pause arrival. Check wind/waves, braking and final heading; no teleportation or automatic anchoring. |
| Cancellation | Cancel while searching, fetching and sailing; also test boarding, helm takeover, death, ship/dock removal and world exit. No orphan guide, control or remote loading remains. |
| HUD | Status is below the hotbar and aligned with slot one at different resolutions/UI scales. It follows the native HUD when hidden. |
| Mast/menu | Quick Use holds fast; hold Use calls once. Rebound `Controls / CallGullKey`, controller navigation and ordinary inventory controls remain usable. |
| Gull | Day/night feathers, helmet, perch height, weather gestures, squawks/chat and arrival-perch transition. Recent lighting changes still need visual confirmation. |
| Route | Wisps stay four metres above the real course; toggling persists; reroutes remove the old trail. |
| Cargo | Both mods installed: request explicitly, observe one slot and three throws, conserve contents/metadata, retain blocked cargo. Changing the base group stops the order. Repeat with either mod absent. |
| Conflicts | Repeat short routes with each installed sailing/physics mod. Metadata eligibility alone is not a sailing test. |

## Multiplayer N/A — disabled

Multiplayer and dedicated-server operation are blocked in this release. They have not been validated; this is stronger than an “untested but available” claim. Do not advertise server support or silently remove those guards as part of a documentation change.

## Report a problem

Include version, ship prefab, visible status, source/destination settings and relevant `BepInEx/LogOutput.log` lines. State whether the issue occurred after a fresh launch and which sailing mods were active. Previous release checks remain in Git history and the changelog.
