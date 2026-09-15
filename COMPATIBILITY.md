# Ship compatibility — 0.2.1

Ships are discovered through the live Valheim prefab registry and original scene prefab list. There is no Karve-only or OdinShip-only name whitelist. Navigation currently requires standard `Ship` physics, a float collider, rigidbody, ship controls and reverse force. Custom movement implementations need an adapter.

Sailing ships are eligible regardless of seating. Rowboats need at least two distinct seats; a seated helm counts. Beds, standing helms and mast/bow hold-fast points do not count. Nearby voyage selection, naming, the berth preview list and named-ship summoning use the same eligibility rule. Only named ships appear in the summon list.

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

Other OdinShip versions and compatible ship mods are discovered through the same rules without a hard dependency. The table documents local metadata checks, not verified sailing performance for every vessel. Hull dimensions, draft and braking remain estimates and require individual playtests. Restart Valheim after updating and look for `Helmsman 0.2.7 loaded` followed by `Detected ships:` in the BepInEx log.
