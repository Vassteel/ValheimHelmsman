# Ship compatibility — 0.2.15

**Solo/local operation works. Multiplayer and dedicated-server operation are disabled and untested.**

Ships are discovered through the live Valheim prefab registry and original scene prefab list. There is no Karve-only or OdinShip-only name whitelist. Navigation currently requires standard `Ship` physics, a float collider, rigidbody, ship controls and reverse force. Custom movement implementations need an adapter.

Sailing ships are eligible regardless of seating. Both the current cloth-sail flag (Karve/Raft) and legacy sail-object references are recognized. Rowboats need at least two distinct seats; a seated helm counts. Beds, standing helms and mast/bow hold-fast points do not count. Nearby voyage selection, naming, the berth preview list and named-ship summoning use the same eligibility rule. Only named ships appear in the summon list.

Inspected installed **OdinShip 0.7.9** assembly and prefab metadata:

| Boat | Prefab | Seats including seated helm | Eligible |
|---|---|---:|---|
| Merchant’s Boat | MercantShip | 6 | Yes, sailing |
| Cargo Ship | CargoShip | 5 | Yes, sailing |
| Big Cargo Ship | BigCargoShip | 5 | Yes, sailing |
| Little Boat | LittleBoat | 0 | Yes, sailing with standing helm |
| War Ship | WarShip | 13 | Yes, sailing |
| Double Rowing Canoe | DoubleRowingCanoe | 2 | Yes, rowing |
| Rowing Canoe | RowingCanoe | 1 | No |

The canoes retain dummy sail objects but have zero sail force. Helmsman keeps eligible rowboats in rowing mode. OdinShip’s ordinary canoe speed boost depends on a player being aboard; unattended summoning uses standard slow rowing and does not fake player occupancy.

Other OdinShip versions and compatible ship mods are discovered through the same rules without a hard dependency. The table documents local metadata checks, not verified sailing performance for every vessel. Hull dimensions, draft and braking remain estimates and require individual playtests. Restart Valheim after updating and look for `Helmsman 0.2.15 loaded` followed by `Detected ships:` in the BepInEx log.


## Other mods and installation

Helmsman must be installed on the solo player's active client, with BepInEx and Jötunn. Quartermaster is optional and enables requested unloading only when both mods are present. A server-only installation does not provide supported operation.

The highest conflict risk is with mods changing `Ship.CustomFixedUpdate`: Helmsman applies a control prefix and an unattended-crew transpiler there. It also patches controller detection, helm input and remote scene loading. Custom ship physics need an adapter; different Harmony patches can conflict even when both DLLs load. Summoning is disabled if its expected crew-count pattern is missing.

The OdinShip table records inspected 0.7.9 metadata. It does not guarantee that every prefab passes the live registry's current checks or sails correctly in a combined mod pack. Karve/Raft sail detection was corrected in 0.2.15; confirm the fresh detection log and a short voyage.

## Identity

The public mod is Valheim Helmsman. Plugin GUID `local.valheim.helmsman` and its saved-data keys are intentionally retained for existing configs, worlds and Quartermaster integration. The Thunderstore upload manifest uses the publishing account's namespace; `Local` appears only in legacy local-import packaging.
