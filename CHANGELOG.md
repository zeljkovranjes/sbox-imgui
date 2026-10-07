# Changelog

## 2026-10-06
### Breaking
- `Duccsoft.ImGui.ImGuiSystem` is now `Duccsoft.ImGui.Systems.ImGuiSystem`. Add `using Duccsoft.ImGui.Systems;` where you use `ImGuiSystem` (e.g. `ImGuiSystem.Current.SimulateInput`).
- `Duccsoft.ImGui.Rendering.ImDrawList` is now `Duccsoft.ImGui.ImDrawList`. Remove `using Duccsoft.ImGui.Rendering;`; `using Duccsoft.ImGui;` covers it.
- The sample components moved from `Duccsoft.ImGui.Samples` to `Duccsoft.ImGui.Components` (`ExampleComponent`, `ImGuiDemoWindowComponent`, `ImGuiInspectorComponent`). Scenes and prefabs that use them keep loading; only code that names the old namespace needs updating.
### Changed
- Brought under the workspace package standard: source reorganised into the `ImGui.<Part>.cs` facade partials, `Api/` (public types), `Engine/`, `Components/`, `Systems/` and `UI/`. The `ImGui` API, every enum and every other public type keep their names, and behaviour is unchanged.
- The usage guide moved from `docs.md` to `docs/USAGE.md`; the demo scene moved to `Assets/scenes/imgui_demo.scene`.
- Package summary, description and tags rewritten; README follows the standard format.
