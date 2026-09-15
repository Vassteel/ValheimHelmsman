# Helmsman UI reference

The user requested the menu theme from the local Quartermaster project. The reference is `Quartermaster/src/ChestUi.cs` in the sibling `Valheim mods` workspace. Its UI is recorded there as new Quartermaster implementation, separate from its upstream Hearthkeeper snapshot. Helmsman adapts the theme and UI construction pattern without adding a runtime dependency on Quartermaster.

The implementation is in `src/Helmsman/MenuTheme.cs` and `HelmsmanUI.cs`:

- Panel: RGBA (0.055, 0.070, 0.075, 0.995), with a 2-pixel gold outline.
- Gold headings/borders: RGB (0.92, 0.73, 0.38).
- Muted text: RGB (0.73, 0.77, 0.79).
- Buttons: RGB (0.16, 0.18, 0.19); inputs: RGB (0.11, 0.14, 0.16).
- Selected tabs: RGB (0.28, 0.23, 0.13); enabled toggles: RGB (0.12, 0.23, 0.22).
- Valheim inventory action font, 28-point headings, 18–20-point controls and body text.
- 720×620 reference panel with proportional fit to the current canvas and 24-pixel outer clearance.
- Native Unity UI/TextMeshPro controls. Controller activation uses ZInput once; pointer input uses the native button behavior.

Berth/departure editing offers an **Adjust view** mode instead of forcibly changing camera position. It balances this menu's Jötunn input-block request, preserves the draft and preview, then restores the menu when the user returns.

Only the editing ghost and berth guides use an always-visible depth test. The implementation uses the built-in colored shader's depth/blend settings, following [Unity's Material.SetPass example](https://docs.unity3d.com/cn/2022.1/ScriptReference/Material.SetPass.html). The runtime checks for shader support and falls back to ordinary transparency with a log message if unavailable. Voyage route lines and real world objects keep their normal rendering behavior.

Compilation is verified. In-game appearance, controller behavior and overlay rendering require a restart and visual playtest; no screenshot has yet been captured from this version.
