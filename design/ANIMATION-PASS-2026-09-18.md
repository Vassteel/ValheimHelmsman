# Slipway and puffin local review pass

Local development build: **0.2.40**. Website publishing remains on hold for the user's in-game test. No README or public package changed in this pass.

## Slipway

- Hull strakes and framing assemble first, then the main deck, mast/spars, rigging, sails and cargo/finishing appear in separate steps. Deck, mast and rigging now have their own exported mesh groups rather than being combined as fixed timber. Unfinished geometry remains a faint depth-tested blueprint.
- Ten-second launch: brake hold, accelerating slide, later hull leveling into the water at the native buoyancy equilibrium height, avoiding an extra drop after spawning. Cradle follows the hull's longitudinal distance until the release end; capstan rotates and the haul line follows the cradle.
- Six-second reset hauls the cradle home; new orders wait for reset completion.
- Construction, launch, pause and reset use saved network timestamps. Obstruction pauses preserve travel instead of jumping the vessel back to the start. Native ship launch receipts continue to prevent duplicate spawns.
- Spatial timber creaks, continuous sliding friction and rope sounds, with a separate shipyard volume control. Scripted launch splash removed; native ship/water interaction owns water-entry sound.

## Puffin

- More detailed head/body geometry, layered wing feathers, smaller inset eyes, dark/orange bill bands, blue ribbed knitted cap, stitched leather apron, pencil and square pocket. Approximately 40,500 visible triangles in working poses, batched into 36 renderers.
- Four planted work actions: mallet, caulking iron, sail needle/cloth, and paintbrush. Tool contact sounds are event-timed rather than played every frame.
- New slipway orders record the supporting bench so its puffin follows construction activity. He alternates preparation at workshop upgrades with slipway inspection and returns to the bench when idle.
- Walking and short flights use the Quartermaster owl's bounded ground/flight search and swept clearance approach. Velocity eases into turns and brakes near the destination; visible shortcuts and actual movement steps are checked again for obstacles. Feet follow travel distance, with a planted stance and lifted recovery.
- Owl-style cosmetic NPC treatment: occlusion-tested hover status, greetings, sparse quiet calls, glances/preening, nighttime rest and wake-up near the player, no physical collision/combat/drop body. Rendering and local behavior are suspended at long distance. Dedicated servers do not create cosmetic birds.
- `Audio/ShipyardVolume` and `Audio/PuffinVolume` independently control the first-pass audio.

## Validation and previews

- Full repository build/check script passed; final changes compiled with zero warnings/errors and passed the game API audit (1007 members and 45 Harmony targets).
- Timeline, blocked-launch resume, cradle coupling, detour clearance, station poses and synthesized-sound signal checks passed.
- Workshop binary geometry validates with all 53 slipway snap points retained.
- Production bird geometry/UV/triangle budgets validated and actual C# pose samples rendered. Both MP4 and WebM previews are supplied, with sound.
- `puffin-animation-v1/puffin-performance.mp4`: 24-second work/walk/flight pose review.
- `puffin-animation-v1/puffin-refined.png`: refined production model.
- `puffin-animation-v1/shipyard-audio-samples.wav`: initial call and workshop sound samples.
- `slipway-stages-30s/slipway-sequence-30s.mp4`: revised 30-second construction/launch/reset review with separate deck, mast and rigging steps. Construction is compressed to 12 seconds; launch takes 10, reset takes 6, followed by a 2-second completed-reset hold.
- Regrouped all six authored sailing boats; triangle fingerprints verify unchanged geometry, material assignments and existing sail/rudder groups. Native ship logic, hull/cargo collision and metadata are preserved.

Blender previews use studio lighting and review materials; they do not validate native Unity shader appearance or live Unity physics. In-game acceptance is still required for furnished-base routes, actual workshop tool contact, multiplayer pause/resume, wave-height launch clearance and perceived sound balance. NPC movement and voices are local cosmetic presentation; paid construction and spawning remain network authoritative.

The modular dock remains retired from the new model batch, and carved markers remain shelved from release.

## Construction circus and revised launch audio

- Six local puffins fly in with staggered arrivals: two hammer timber, one planes wood, one hauls rope, and two carry timber/supplies. Work tools attach to the beak; the plane sole stays level with its trestle.
- Supply carriers make return trips along supported platforms. Movement uses swept collision checks and the unfinished hull/mast as additional obstacles, because the construction preview has no physics colliders.
- Completion or cancellation sends workers away on varied per-order bearings, with staggered departures and cleanup after flight. Nearby crews share an 18-bird cap; birds are cosmetic and do not alter construction timing or resources.
- Launch and reset now have sustained sliding friction underneath irregular, varied wood creaks. Pausing stops machinery audio. No additional splash is played.
- The circus review uses production geometry/poses with scripted open-slipway flight routes, rather than a recording of Unity navigation. Close-ups show the five roles separately. Construction is accelerated for review.
- After the interrupted render, previews use two fixed CPU threads and lower resolution. Existing ship-only frames after the crew departs are reused from the unchanged machinery review.

Live acceptance still needs crew navigation around player buildings, tool contact at different worker sizes, cancellation/cleanup, multiplayer construction and in-game audio balance.

The circus build passed the complete build/check script and production bird geometry checks, and was installed locally in Steam and the Mods profile with byte verification and backups. No public upload was made.

Review artifacts: `puffin-circus-v1/crew-roles.jpg` and `slipway-circus-v1/slipway-circus-30s.mp4` / `.webm`. The 30-second clip includes the revised generated creak variants and sliding layer; no splash is mixed into the review.

## Crew arrival before construction

- New slipway orders save a gathering phase separately from their construction start timestamp. While observed by their owner, all six puffins must land and settle for 0.6 seconds before the owner starts construction. The full paid build duration begins then, with no elapsed gathering time deducted.
- Workers wait before operating tools or making supply trips. The hull remains a blueprint during gathering; the hover status reports arriving crew. Finished ships retain their built appearance while waiting for launch clearance.
- Rejoining an already active build restores workers at their stations. A dedicated or unobserved owner simulates a 20-second gathering interval; there is no dependency on cosmetic NPC rendering on a headless server. The saved build start survives ownership changes.
- Higher arrival approaches are tried when terrain or buildings block the preferred spawn point. If work positions cannot be reached, the nearby order remains waiting and asks players to keep the side platforms clear.
- Audio refinement is deferred at the user's request. The revised review clip is silent. Its eight-and-a-half-second arrival segment finishes before accelerated assembly begins; launch and reset retain the prior timing.

Arrival-order validation passed the complete repository build and checks plus the final game API audit (1007 members, 45 Harmony targets). The updated 0.2.40 DLLs are installed locally with verified bytes. Revised silent review: `slipway-arrival-v2/slipway-circus-30s.mp4` / `.webm`. Runtime acceptance around furnished slipways is still pending.
