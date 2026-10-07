<!-- sbox-standard: v1 | type: library | root: Duccsoft.ImGui -->
# Dear ImGui

**Standard: sbox-standard v1** (library, root namespace `Duccsoft.ImGui`). This package follows the package layout and
code standard in E:\.sbox\CLAUDE.md (loaded automatically), enforced by `sbox-check`. Run it before
calling work done.

s&box library `duccsoft.imgui`.

## Package facts

- Imported package (Org `duccsoft`, Ident `imgui`, fork of chrisspieler/sbox-imgui). Never change Org/Ident.
- The public API is the Dear ImGui API. User code depends on `Duccsoft.ImGui.ImGui` and every public type
  (enums, `ImGuiStyle`, `ImGuiIO`, `ImDrawList`, `ImRect`, `ImGuiPayload`, sort specs, callbacks, `ComponentExtensions`)
  being in the root namespace. Namespace = folder path, so all of them, and every partial of the static `ImGui` class,
  live in the facade `Code/Duccsoft.ImGui/ImGui.cs` (one `#region` per former file). Add new widgets there, in the
  matching region. Never move a public type into a layer namespace: it breaks every user.
- Internal state types (`ImGuiContext` and its partials, `ImGuiWindow`, `ImGuiTable`, `ImGuiTabBar`, ...) are in
  `Engine/`. They reference the facade's public types (enums, style); that is accepted.
- No Core layer: every type uses s&box `Vector2`/`Color`, and `Assembly.cs` has `global using Sandbox;`. Do not
  add `dev\Duccsoft.ImGui.Core.csproj` (re-running `new-sbox-library` recreates it; delete it again) until real
  engine-free logic exists.
- Tests: none in plain .NET. The in-engine self-test (32 interaction tests) is `dev/editor-rig`: `start-editor.ps1`
  then `run-selftest.sh` (needs the sbox-mcp library next to this repo or `IMGUI_MCP_ROOT`).
- The generated `Code\imgui.csproj` from P: pointed at `../../../../../../Program Files (x86)/Steam/...`; on E: it
  needs `C:/Program Files (x86)/Steam/...` until the editor regenerates it.
