# Helmsman development

## Build and verify

Requires .NET SDK 8 and an installed Valheim with BepInEx and Jötunn. Game assemblies stay local and are never distributed.

```sh
DOTNET=/path/to/dotnet bash build.sh -p:ValheimDir='/path/to/Valheim'
python3 package.py
```

`DOTNET` is optional when `dotnet` is on PATH. This workstation's temporary SDK fallback is `/tmp/wildglow-dotnet/dotnet`. Set `ValheimDir` in `Environment.props` for a persistent local override; that file is ignored by Git.

The plugin resolves against the installed game's Mono/Unity assemblies. Core targets .NET Standard 2.1; executable tests use .NET 8 with host doubles where Unity cannot run headlessly. API checks resolve shipped references and validate Harmony signatures. They do not prove Harmony behavior inside a running mod pack or GPU appearance.

Both release DLLs are copied to `dist/ValheimHelmsman/`. `Directory.Build.props` owns their common version. The BepInPlugin version and package manifest are checked against the compiled version before release. Increment the patch version for an approved README update too.

## Stable identity

Public mod name: **Valheim Helmsman**. Repository: **Vassteel/ValheimHelmsman**. Keep plugin GUID **`local.valheim.helmsman`**: existing config paths and Quartermaster's optional integration depend on it. Keep `helmsman_berth_v1`, `helmsman_ship_name_v1`, the Dock Ward and whistle prefab IDs. A future identity change needs an explicit migration.

`Local` is retained only as the legacy r2modman import author. The separate Thunderstore upload archive omits that nonstandard manifest field; the publishing account controls its namespace.

## Code map

- `src/Helmsman.Core`: route search, boarding, shoreline candidates, steering, gull poses and model decoding.
- `src/Helmsman`: game adapters, persistent dock/ship records, input/UI, local visuals, request lifetimes and owner-only ship controls.
- `tests`: core, interaction, whistle and API checks.
- `assets/gullcall`: original mesh source, validated binary model and matching icon. `tools/pack_gullcall_model.py` regenerates the binary.
- `FEASIBILITY.md`: historical design and future work, not the release contract.
- `INSTALLATION.md`: this workstation's installation history, excluded from player packages.

## Navigation and conflict surface

`Ship.CustomFixedUpdate` has a control prefix and an empty-crew transpiler. `HaveControllingPlayer` has a postfix. `Ship.ApplyControlls` and `ShipControlls.Interact` return control to a player. Remote summons extend `ZNetScene.CreateObjects` and `PointInsideActiveArea` around their simulation center. These are the main integration risks with other sailing, ownership and streaming mods.

The empty-crew transpiler requires exactly two native crew-count sites; otherwise summoning is unavailable. Startup patch errors remove Helmsman's partial patches. Planning errors pause the voyage and retain the diagnostic exception.

Route search uses a 24 m grid, 20,000-cell limit and incremental work. A Unity terrain query can exceed the per-frame target. Three automatic cruise replans are allowed; docking failures pause. Braking starts from a conservative 0.25 m/s² estimate; hulls, waves and other physics mods require live checks. No trip is resumed from saved data.

## Release discipline

Run the checks, inspect the packaged manifest/docs/DLL versions, and review the diff. Get README approval before applying a new draft. Close the game before installation, back up replaced DLLs and verify their hashes. GPU and multiplayer claims require actual game evidence.
