# Dear ImGui

Journal for this library. Keep it current: decisions, engine gotchas, what failed and why, the next step.

## Package

| | |
|---|---|
| Ident | `duccsoft.imgui` |
| Type | library |
| Root namespace | `Duccsoft.ImGui` |
| Depends on | none |
| Published | |

## Log

- 2026-10-06: brought under the workspace standard. The static `ImGui` class (about 29 partial files) and every public API
  type must stay in `Duccsoft.ImGui`, and namespace = folder path, so they were concatenated unchanged into the facade
  `Code/Duccsoft.ImGui/ImGui.cs` (one `#region` per former file). Internal state types went to `Engine/` (they use s&box
  `Vector2`/`Color`, so no Core layer and no Core harness). `ImGuiSystem` -> `Systems/`, `ImGuiInputPanel` -> `UI/`, sample
  components -> `Components/` with `[Alias]`. Line-multiset check: only two lines changed (`Rendering.ImDrawList` ->
  `ImDrawList`). Compile warnings identical before and after (18). The editor rig host code compiles against the new
  build. Not re-run: the in-engine self-test (`dev/editor-rig/run-selftest.sh`) and the s&box whitelist compile.
  Next: open the package in the s&box editor and run the self-test to confirm 32/32 still pass.
- 2026-10-06: the standard now allows facade partials `ImGui.<Part>.cs` and an `Api/` layer in the root namespace, so
  the single 19k-line `ImGui.cs` was split back into the 29 original partials (Demo ones renamed `ImGui.DemoX.cs`: one
  part segment only) and the 43 public types went one per file into `Api/`. Same checks as before: identical warnings,
  host code builds, sbox-check passes.
