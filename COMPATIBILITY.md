# Ship compatibility — 0.2.16

Existing sailing has been used locally. Multiplayer is now enabled for testing; new fleet, rendering and real server behavior are not yet verified.

## Ships

Navigation requires native `Ship` physics, a float collider, rigidbody, controls and reverse force. Current cloth sails and legacy sail objects are supported. Rowboats need one seat, including a seated helm. One-seat canoes can be piloted and recalled; they use the native forward rowing gear. Only named ships appear in recall menus.

All 15 imported player hulls use the original prefab identifiers. Cargo holds use separate native save keys and RPC names. Legacy base64 cargo is migrated only after native deserialization preserves its occupied-slot count. The original record remains. Missing items or corrupt records lock the affected hold and produce a log warning.

Hull dimensions, draft, wind handling and braking need individual server playtests. Prefab eligibility and asset inspection do not prove safe sailing for every vessel.

## Installation and conflicts

- Install matching Helmsman patch versions on every client and the server, with BepInEx and Jötunn.
- Remove original **OdinShip/OdinShipPlus** plugins before this replacement. Keep world and plugin backups. The original DLLs are not bundled or required.
- Remove **LongshipUpgrades** before using Helmsman's native-longship fittings.
- **Quartermaster** is optional; both mods are needed for requested base unloading. Quartermaster checks every cargo hold and its ownership/access state.
- Autonomous enemy ships, their crews, naval weapons and ammunition are deferred. Their saved objects are not migrated by this civilian import.
- Other custom-physics or sailing mods require compatibility testing.

The highest conflict risk remains `Ship.CustomFixedUpdate`: Helmsman applies navigation controls, an unattended-crew transpiler and a canoe gear correction. It also patches helm input, remote loading and ownership, native container serialization/RPC names and canopy shelter. Recall stays unavailable if the expected empty-crew branch pattern cannot be patched.

Custom texture filenames are saved, not the PNG contents. Distribute matching custom textures to every client.

## Identity and diagnostics

Plugin GUID `local.valheim.helmsman`, original imported prefab names and established saved-world keys remain stable. Plugin and Core DLL versions both report 0.2.16. Look for `Helmsman 0.2.16 loaded`, the fleet registration message and `Detected ships:`. Include the ship prefab, status text and relevant log lines in reports.
