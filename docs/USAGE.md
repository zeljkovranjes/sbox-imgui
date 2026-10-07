# Dear ImGui for s&box — Usage Guide

This library is a C# port of the [Dear ImGui](https://github.com/ocornut/imgui) 1.91 API, rendered with s&box's `Painter`.
Everything lives in the static class `Duccsoft.ImGui.ImGui`; names and behavior follow Dear ImGui, so most C++ examples
translate almost line-for-line (`bool*` becomes `ref bool`, `ImVec2` becomes `Vector2`, `ImU32` becomes `Color32`).

- [Getting started](#getting-started)
- [Concepts](#concepts): frames, IDs, labels, formats, input capture, scaling
- [Windows](#windows) · [Child windows](#child-windows) · [Layout](#layout) · [Style](#style)
- [Text](#text) · [Buttons & toggles](#buttons--toggles) · [Images](#images)
- [Text input](#text-input) · [Drags](#drags) · [Sliders](#sliders) · [Numeric input](#numeric-input)
- [Combo boxes](#combo-boxes) · [List boxes](#list-boxes) · [Selectables](#selectables) · [Trees & collapsing headers](#trees--collapsing-headers)
- [Menus](#menus) · [Popups & modals](#popups--modals) · [Tooltips](#tooltips)
- [Tables](#tables) · [Legacy columns](#legacy-columns) · [Tab bars](#tab-bars)
- [Color editors & pickers](#color-editors--pickers) · [Plots & progress bars](#plots--progress-bars)
- [Drag and drop](#drag-and-drop) · [Custom drawing (ImDrawList)](#custom-drawing-imdrawlist)
- [Item & window queries](#item--window-queries) · [Input queries](#input-queries)
- [Built-in tools](#built-in-tools) · [Component inspector](#component-inspector) · [Sample components](#sample-components)
- [Verification](#verification): what was tested in-engine, with screenshots
- [Differences from Dear ImGui](#differences-from-dear-imgui)

---

## Getting started

1. Add the library to your project (`Libraries/duccsoft.imgui`, or install the package).
2. Call ImGui from any component's `OnUpdate`. No setup is needed: `ImGuiSystem` (a `GameObjectSystem` in `Duccsoft.ImGui.Systems`) starts a frame before
   components update and draws everything on the main camera after they update.

```csharp
using Duccsoft.ImGui;

public sealed class MyDebugPanel : Component
{
	private float _speed = 1.0f;
	private bool _godMode;

	protected override void OnUpdate()
	{
		Mouse.Visible = true; // ImGui needs a visible cursor to be clicked

		if ( ImGui.Begin( "Debug" ) )
		{
			ImGui.Text( "FPS: {0:F0}", 1.0f / Time.Delta );
			ImGui.SliderFloat( "Speed", ref _speed, 0.0f, 10.0f );
			ImGui.Checkbox( "God mode", ref _godMode );
			if ( ImGui.Button( "Respawn" ) )
				Respawn();
		}
		ImGui.End(); // always call End(), even when Begin() returned false
	}
}
```

To see everything the library can do, add the **ImGui Demo Window** component (`ImGuiDemoWindowComponent`) to any GameObject,
or call `ImGui.ShowDemoWindow()`.

## Concepts

**Immediate mode.** You call widget functions every frame; they return what happened this frame (`Button` returns `true` on
the frame it is clicked, `SliderFloat` returns `true` when the value changed). State lives in your variables, passed by `ref`.

**Frames.** ImGui calls must happen during the update loop (`OnUpdate`, not `OnFixedUpdate`). `Begin`/`End`,
`BeginChild`/`EndChild`, `TreeNode`/`TreePop`, `BeginTable`/`EndTable`... must be balanced. Unbalanced `Push*/Pop*` calls inside
a window are recovered (with a warning) when the window ends.

**IDs and labels.** Widgets are identified by a hash of their label and the ID stack. Labels must be unique within a window/scope.
- `"Play##1"` and `"Play##2"`: same visible text, different IDs (everything after `##` is hidden).
- `"Score: 10###score"`: visible text can change every frame while the ID stays `###score`.
- `ImGui.PushID( i )` / `ImGui.PopID()` around loops to make labels unique.

**Format strings.**
- `Text`, `TextColored`, `BulletText`, `LabelText`, `SetTooltip` use .NET composite formatting: `ImGui.Text( "{0} / {1:F2}", a, b )`.
- Numeric widgets (`DragFloat`, `SliderInt`, `InputFloat`...) use Dear ImGui's printf-style formats: `"%.3f"`, `"%d%%"`, `"%.0f deg"`,
  `"%e"`, `"%x"`. A format without `%` is treated as a .NET format (`"F2"`, `"0.00"`).

**Input capture.** While the mouse is over an ImGui window (or a widget is being dragged), ImGui receives the clicks and your
game should ignore them. Check `ImGui.GetIO().WantCaptureMouse` / `WantCaptureKeyboard` / `WantTextInput`.
Mouse capture can be turned off with the `imgui_mouse_capture 0` convar; `imgui_enabled 0` disables ImGui entirely.

**Scaling.** By default the whole UI scales with the screen height (reference: 1080p) via `io.AutoScale`.
Extra scaling: `ImGui.GetIO().FontGlobalScale`. Font family/size: `io.FontName` (default `"Roboto Mono"`), `io.FontSize`.

## Windows

```csharp
ImGui.SetNextWindowPos( new Vector2( 100, 100 ), ImGuiCond.FirstUseEver );
ImGui.SetNextWindowSize( new Vector2( 400, 300 ), ImGuiCond.FirstUseEver );
if ( ImGui.Begin( "Inventory", ref _inventoryOpen, ImGuiWindowFlags.MenuBar ) ) // ref bool adds a close (X) button
{
	ImGui.Text( "Items: {0}", items.Count );
}
ImGui.End();
```

- `Begin( name, flags )` / `Begin( name, ref open, flags )`: returns `false` when collapsed or clipped. With `ref open`, clicking X sets `open = false`; don't call `Begin` again while it's false.
- Useful flags: `NoTitleBar`, `NoResize`, `NoMove`, `NoScrollbar`, `NoCollapse`, `AlwaysAutoResize`, `NoBackground`, `MenuBar`, `HorizontalScrollbar`, `NoFocusOnAppearing`, `NoBringToFrontOnFocus`, `NoInputs`, `NoDecoration`, `UnsavedDocument`.
- Users can move windows (drag title or empty space), resize them (bottom corner grips and edges), collapse them (arrow or double-click title), and scroll them (mouse wheel, Shift+wheel for horizontal).
- Before `Begin`: `SetNextWindowPos( pos, cond, pivot )`, `SetNextWindowSize`, `SetNextWindowSizeConstraints( min, max, callback )`, `SetNextWindowContentSize`, `SetNextWindowCollapsed`, `SetNextWindowFocus`, `SetNextWindowScroll`, `SetNextWindowBgAlpha`.
- Inside a window: `GetWindowPos/Size/Width/Height`, `SetWindowPos/Size/Collapsed/Focus` (also by name), `IsWindowAppearing`, `IsWindowCollapsed`, `IsWindowFocused( flags )`, `IsWindowHovered( flags )`, `GetWindowDrawList`, `SetWindowFontScale`.
- `ImGuiCond`: `Always`, `Once`, `FirstUseEver` (only if the window has no saved position this session), `Appearing`.
- Scrolling: `GetScrollX/Y`, `GetScrollMaxX/Y`, `SetScrollX/Y`, `SetScrollHereX/Y( ratio )`, `SetScrollFromPosX/Y`.
- Window positions/sizes are remembered for the session (disable with `NoSavedSettings`).

## Child windows

```csharp
ImGui.BeginChild( "log", new Vector2( 0, 200 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY );
foreach ( var line in lines )
	ImGui.TextUnformatted( line );
ImGui.EndChild(); // always call
```

Size: `0` = fill available space, negative = leave that much space at the edge. `ImGuiChildFlags`: `Borders`,
`AlwaysUseWindowPadding`, `ResizeX`, `ResizeY` (user-resizable), `AutoResizeX/Y`, `AlwaysAutoResize`, `FrameStyle` (looks like an input frame).

## Layout

| Function | Purpose |
|---|---|
| `SameLine( offsetFromStartX = 0, spacing = -1 )` | Put the next item on the same line |
| `NewLine()`, `Spacing()`, `Dummy( size )` | Vertical spacing / empty item |
| `Separator()`, `SeparatorText( "Title" )` | Horizontal lines |
| `Indent( w )` / `Unindent( w )` | Shift content right/left |
| `BeginGroup()` / `EndGroup()` | Lock horizontal position; treat several items as one |
| `AlignTextToFramePadding()` | Vertically align text with framed widgets on the same line |
| `PushItemWidth( w )` / `PopItemWidth()` / `SetNextItemWidth( w )` | Widget width (`>0` px, `<0` = distance from right edge, `-float.MinValue`-like `-1.17549435E-38f` = fill) |
| `PushTextWrapPos( x )` / `PopTextWrapPos()` | Word-wrap text at a local x |
| `GetCursorPos/ScreenPos`, `SetCursorPos/ScreenPos/PosX/PosY`, `GetCursorStartPos` | Manual positioning |
| `GetContentRegionAvail()` | Remaining space from the cursor |
| `GetFrameHeight()`, `GetTextLineHeight()`, `GetFontSize()`, `CalcTextSize( text )` | Metrics |
| `BeginDisabled( bool )` / `EndDisabled()` | Grey out and disable a block of widgets |
| `PushItemFlag( ImGuiItemFlags.ButtonRepeat, true )` / `PopItemFlag()` | Per-item behaviour flags |
| `PushClipRect( min, max, intersect )` / `PopClipRect()` | Clip rendering and hit-testing |

## Style

```csharp
ImGui.StyleColorsLight();                       // or StyleColorsDark() / StyleColorsClassic()
var style = ImGui.GetStyle();
style.FrameRounding = 4;
style.Colors[(int)ImGuiCol.WindowBg] = new Vector4( 0.1f, 0.1f, 0.12f, 0.95f );

ImGui.PushStyleColor( ImGuiCol.Button, new Color( 0.8f, 0.2f, 0.2f ) );
ImGui.PushStyleVar( ImGuiStyleVar.FramePadding, new Vector2( 10, 6 ) );
ImGui.Button( "Danger" );
ImGui.PopStyleVar();
ImGui.PopStyleColor();

ImGui.PushFont( "Poppins", 20 ); // family and/or size (reference pixels), until PopFont()
ImGui.Text( "Big title" );
ImGui.PopFont();
```

`GetColorU32( ImGuiCol )` returns a `Color32` for drawing. Style values are in pixels and already scaled for the screen.
`ShowStyleEditor()` edits everything live.

## Text

```csharp
ImGui.Text( "Health: {0}", hp );
ImGui.TextColored( new Vector4( 1, 0.4f, 0.4f, 1 ), "Low health!" );
ImGui.TextDisabled( "(optional)" );
ImGui.TextWrapped( "A long paragraph that wraps at the window edge..." );
ImGui.TextUnformatted( rawStringWithBraces );   // no formatting, fastest
ImGui.LabelText( "Name", "{0}", player.Name );  // value + label aligned like widgets
ImGui.BulletText( "Point {0}", 1 );
ImGui.SeparatorText( "Section" );
ImGui.Value( "Count", 3 );
```

## Buttons & toggles

```csharp
if ( ImGui.Button( "Save" ) ) Save();
if ( ImGui.Button( "Wide", new Vector2( -1.17549435E-38f, 0 ) ) ) { }   // fill width
if ( ImGui.SmallButton( "x" ) ) { }
if ( ImGui.ArrowButton( "##left", ImGuiDir.Left ) ) index--;
if ( ImGui.InvisibleButton( "canvas", new Vector2( 200, 100 ) ) ) { }  // behaviour without visuals
ImGui.Checkbox( "Enabled", ref enabled );
ImGui.CheckboxFlags( "Flag A", ref flags, (int)MyFlags.A );           // also generic enum overload
ImGui.RadioButton( "Easy", ref difficulty, 0 ); ImGui.SameLine();
ImGui.RadioButton( "Hard", ref difficulty, 1 );
ImGui.Bullet(); ImGui.Text( "bulleted" );
if ( ImGui.TextLink( "Read more" ) ) { }
ImGui.TextLinkOpenURL( "Docs", "https://..." ); // copies the URL to the clipboard (s&box can't open browsers)
```

Hold-to-repeat: wrap a button in `PushItemFlag( ImGuiItemFlags.ButtonRepeat, true )` / `PopItemFlag()`.

## Images

```csharp
ImGui.Image( texture, new Vector2( 128, 128 ) );
ImGui.Image( texture, size, uv0, uv1, new Vector4( 1, 1, 1, 1 ) /* tint */, new Vector4( 1, 1, 1, 0.5f ) /* border */ );
if ( ImGui.ImageButton( "icon", texture, new Vector2( 32, 32 ) ) ) { }
```

Any s&box `Texture` works (including render targets). `uv0`/`uv1` select a sub-rectangle.

## Text input

```csharp
ImGui.InputText( "Name", ref name );
ImGui.InputText( "Code", ref code, 8, ImGuiInputTextFlags.CharsUppercase );            // max length 8
ImGui.InputTextWithHint( "Search", "type to filter...", ref filter );
ImGui.InputText( "Password", ref pw, ImGuiInputTextFlags.Password );
if ( ImGui.InputText( "Command", ref cmd, ImGuiInputTextFlags.EnterReturnsTrue ) )   // true when Enter is pressed
	Run( cmd );
ImGui.InputTextMultiline( "##notes", ref notes, new Vector2( -1.17549435E-38f, 200 ), ImGuiInputTextFlags.AllowTabInput );
```

Editing: click/drag/double-click to select, Shift+arrows, Ctrl+arrows (word jump), Home/End, Ctrl+A, Ctrl+C/X/V, Ctrl+Z/Y,
Escape to revert. Flags: `CharsDecimal`, `CharsHexadecimal`, `CharsScientific`, `CharsUppercase`, `CharsNoBlank`, `ReadOnly`,
`Password`, `AutoSelectAll`, `EnterReturnsTrue`, `EscapeClearsAll`, `CtrlEnterForNewLine`, `AllowTabInput`, `NoUndoRedo`,
callbacks (`CallbackCompletion`, `CallbackHistory`, `CallbackAlways`, `CallbackCharFilter`, `CallbackEdit`) through an
`ImGuiInputTextCallback` delegate receiving `ImGuiInputTextCallbackData` (`Buf`, `CursorPos`, `InsertChars`, `DeleteChars`, `SelectAll`...).
See the demo's Console example for completion and history.

Focus a field from code with `ImGui.SetKeyboardFocusHere()` before it.

## Drags

Click and drag horizontally to change the value; Shift = faster, Alt = slower; double-click or Ctrl+click to type a value.

```csharp
ImGui.DragFloat( "Speed", ref speed, 0.1f );                    // speed per pixel
ImGui.DragFloat( "Ratio", ref ratio, 0.005f, 0f, 1f, "%.2f" );  // clamped range + format
ImGui.DragInt( "Percent", ref pct, 1, 0, 100, "%d%%", ImGuiSliderFlags.AlwaysClamp );
ImGui.DragFloat3( "Position", ref position );                   // Vector2/3/4 or float[] overloads
ImGui.DragInt2( "Grid", ref gridSize );                         // Vector2Int/Vector3Int or int[]
ImGui.DragFloatRange2( "Range", ref min, ref max, 0.25f, 0f, 100f, "Min: %.1f", "Max: %.1f" );
ImGui.DragScalar( "Long", ref myLong, 1f );                     // any INumber<T>
```

## Sliders

```csharp
ImGui.SliderFloat( "Volume", ref volume, 0f, 1f );
ImGui.SliderFloat( "Exposure", ref exposure, 0.01f, 100f, "%.3f", ImGuiSliderFlags.Logarithmic );
ImGui.SliderInt( "Level", ref level, 1, 10 );
ImGui.SliderAngle( "Yaw", ref yawRadians );                      // edits radians, displays degrees
ImGui.SliderFloat4( "Weights", ref weights, 0f, 1f );
ImGui.VSliderFloat( "##eq", new Vector2( 20, 120 ), ref band, 0f, 1f );
```

Flags (shared with drags): `AlwaysClamp`, `Logarithmic`, `NoRoundToFormat`, `NoInput` (disable Ctrl+click typing), `WrapAround`.

## Numeric input

```csharp
ImGui.InputInt( "Count", ref count );                   // with -/+ step buttons (hold to repeat)
ImGui.InputInt( "No buttons", ref count, 0 );
ImGui.InputFloat( "Mass", ref mass, 0.1f, 1.0f, "%.2f" );
ImGui.InputDouble( "Precise", ref value, 0, 0, "%.8f" );
ImGui.InputFloat3( "Scale", ref scale );
ImGui.InputScalar( "Ticks", ref ticks );                // any INumber<T>
```

Simple expressions are accepted while typing: `*2`, `/2`, hex with `CharsHexadecimal`.

## Combo boxes

```csharp
ImGui.Combo( "Quality", ref quality, new[] { "Low", "Medium", "High" } );
ImGui.Combo( "Mode", ref mode, "Off\0On\0Auto\0" );     // zero-separated
ImGui.Combo( "Team", ref team );                        // any enum: Combo<T>( label, ref T value )

if ( ImGui.BeginCombo( "Weapon", weapons[current].Name ) ) // custom contents
{
	for ( int i = 0; i < weapons.Count; i++ )
		if ( ImGui.Selectable( weapons[i].Name, i == current ) )
			current = i;
	ImGui.EndCombo(); // only if BeginCombo returned true
}
```

`ImGuiComboFlags`: `PopupAlignLeft`, `HeightSmall/Regular/Large/Largest`, `NoArrowButton`, `NoPreview`, `WidthFitPreview`.

## List boxes

```csharp
ImGui.ListBox( "Players", ref selectedPlayer, playerNames, heightInItems: 6 );

if ( ImGui.BeginListBox( "##files", new Vector2( -1.17549435E-38f, 200 ) ) )
{
	foreach ( var file in files )
		if ( ImGui.Selectable( file.Name, file == selected ) ) selected = file;
	ImGui.EndListBox();
}
```

## Selectables

```csharp
if ( ImGui.Selectable( "Row 1", selected == 1 ) ) selected = 1;
ImGui.Selectable( "Toggle me", ref isOn );                                     // toggles the bool
ImGui.Selectable( "Double click", false, ImGuiSelectableFlags.AllowDoubleClick );
ImGui.Selectable( "Cell", false, ImGuiSelectableFlags.None, new Vector2( 50, 50 ) );
```

Inside popups and menus, clicking a selectable closes the popup unless `NoAutoClosePopups` is set.

## Trees & collapsing headers

```csharp
if ( ImGui.TreeNode( "Settings" ) )
{
	ImGui.Text( "Inside" );
	ImGui.TreePop(); // only when TreeNode returned true
}

var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
if ( isSelected ) flags |= ImGuiTreeNodeFlags.Selected;
bool open = ImGui.TreeNodeEx( "node_id", flags, "GameObject {0}", go.Name );
if ( ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen() ) Select( go );
if ( open ) { /* children */ ImGui.TreePop(); }

if ( ImGui.CollapsingHeader( "Physics", ImGuiTreeNodeFlags.DefaultOpen ) ) { }   // no TreePop needed
if ( ImGui.CollapsingHeader( "Closable", ref headerVisible ) ) { }              // with a close button
ImGui.SetNextItemOpen( true, ImGuiCond.Once );                                  // open state from code
```

Flags: `DefaultOpen`, `OpenOnArrow`, `OpenOnDoubleClick`, `Leaf`, `Bullet`, `Selected`, `Framed`, `FramePadding`,
`SpanAvailWidth`, `SpanFullWidth`, `SpanAllColumns` (in tables), `NoTreePushOnOpen`.

## Menus

```csharp
// Full-width bar at the top of the screen
if ( ImGui.BeginMainMenuBar() )
{
	if ( ImGui.BeginMenu( "File" ) )
	{
		if ( ImGui.MenuItem( "Save", "Ctrl+S" ) ) Save();       // shortcut text is display-only
		ImGui.MenuItem( "Autosave", null, ref autosave );         // checkmark toggle
		if ( ImGui.BeginMenu( "Recent" ) )                        // nested menus open on hover
		{
			ImGui.MenuItem( "level1.scene" );
			ImGui.EndMenu();
		}
		ImGui.Separator();
		ImGui.MenuItem( "Quit", null, false, enabled: false );
		ImGui.EndMenu();
	}
	ImGui.EndMainMenuBar();
}

// A window's own menu bar: pass ImGuiWindowFlags.MenuBar to Begin
if ( ImGui.BeginMenuBar() ) { /* BeginMenu... */ ImGui.EndMenuBar(); }
```

## Popups & modals

```csharp
if ( ImGui.Button( "Options..." ) )
	ImGui.OpenPopup( "options" );            // call once, not every frame
if ( ImGui.BeginPopup( "options" ) )         // closes when clicking outside
{
	if ( ImGui.Selectable( "Reset" ) ) Reset(); // auto-closes
	if ( ImGui.Button( "Close" ) ) ImGui.CloseCurrentPopup();
	ImGui.EndPopup();                        // only if BeginPopup returned true
}

if ( ImGui.Button( "Delete" ) ) ImGui.OpenPopup( "Confirm delete" );
if ( ImGui.BeginPopupModal( "Confirm delete", ImGuiWindowFlags.AlwaysAutoResize ) ) // blocks everything behind it
{
	ImGui.Text( "Really delete?" );
	if ( ImGui.Button( "Yes" ) ) { Delete(); ImGui.CloseCurrentPopup(); }
	ImGui.SameLine();
	if ( ImGui.Button( "No" ) ) ImGui.CloseCurrentPopup();
	ImGui.EndPopup();
}

// Right-click context menus
ImGui.Selectable( item.Name );
if ( ImGui.BeginPopupContextItem() )        // on the last item (ID-based)
{
	if ( ImGui.MenuItem( "Rename" ) ) { }
	ImGui.EndPopup();
}
if ( ImGui.BeginPopupContextWindow() ) { /* right-click on window background */ ImGui.EndPopup(); }
if ( ImGui.BeginPopupContextVoid() ) { /* right-click outside any window */ ImGui.EndPopup(); }
ImGui.OpenPopupOnItemClick( "my popup" );   // open an existing popup from an item click
bool isOpen = ImGui.IsPopupOpen( "options" );
```

## Tooltips

```csharp
ImGui.Button( "?" );
ImGui.SetItemTooltip( "Shown after hovering for a moment" );

if ( ImGui.BeginItemTooltip() )             // rich tooltip
{
	ImGui.Text( "Stats" );
	ImGui.ProgressBar( 0.5f );
	ImGui.EndTooltip();
}

if ( ImGui.IsItemHovered() ) ImGui.SetTooltip( "Immediate tooltip {0}", value );
```

## Tables

```csharp
var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable
          | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollY;
if ( ImGui.BeginTable( "players", 3, flags, new Vector2( 0, 300 ) ) )
{
	ImGui.TableSetupScrollFreeze( 0, 1 );                       // keep the header visible
	ImGui.TableSetupColumn( "Name", ImGuiTableColumnFlags.DefaultSort );
	ImGui.TableSetupColumn( "Score", ImGuiTableColumnFlags.WidthFixed, 80 );
	ImGui.TableSetupColumn( "Ping", ImGuiTableColumnFlags.WidthFixed, 60 );
	ImGui.TableHeadersRow();

	var sort = ImGui.TableGetSortSpecs();
	if ( sort is { SpecsDirty: true } )
	{
		SortPlayers( sort.Specs[0].ColumnIndex, sort.Specs[0].SortDirection );
		sort.SpecsDirty = false;
	}

	foreach ( var p in players )
	{
		ImGui.TableNextRow();
		ImGui.TableNextColumn(); ImGui.Text( p.Name );
		ImGui.TableNextColumn(); ImGui.Text( "{0}", p.Score );
		ImGui.TableNextColumn(); ImGui.Text( "{0} ms", p.Ping );
	}
	ImGui.EndTable(); // only if BeginTable returned true
}
```

- Cells: `TableNextRow( flags, minHeight )`, `TableNextColumn()` (returns visibility), `TableSetColumnIndex( n )`.
- Columns: `TableSetupColumn( label, flags, initWidthOrWeight, userId )`, `TableHeadersRow()`, `TableHeader( label )`.
- Queries: `TableGetColumnCount/Index`, `TableGetRowIndex`, `TableGetColumnName`, `TableGetColumnFlags`, `TableGetHoveredColumn`, `TableSetColumnEnabled`.
- Colors: `TableSetBgColor( ImGuiTableBgTarget.RowBg0 / RowBg1 / CellBg, color, column )`.
- Table flags: borders (`Borders`, `BordersH/V`, `BordersInner/Outer`), `RowBg`, `Resizable` (drag column borders, double-click to fit), `Reorderable` (drag headers), `Hideable` (right-click header menu), `Sortable` (+ `SortMulti` with Shift, `SortTristate`), sizing policies (`SizingFixedFit`, `SizingFixedSame`, `SizingStretchProp`, `SizingStretchSame`), `ScrollX`/`ScrollY` + `TableSetupScrollFreeze`, `NoHostExtendX`, `PadOuterX`, `NoClip`, `HighlightHoveredColumn`, `ContextMenuInBody`.
- Column flags: `WidthFixed`, `WidthStretch`, `DefaultSort`, `NoSort`, `PreferSortAscending/Descending`, `NoResize`, `NoReorder`, `NoHide`, `DefaultHide`, `NoHeaderLabel`, `IndentEnable`.

## Legacy columns

Older, simpler API (prefer tables):

```csharp
ImGui.Columns( 3, "mycolumns", borders: true );
for ( int i = 0; i < 9; i++ )
{
	ImGui.Text( "Item {0}", i );
	ImGui.NextColumn();
}
ImGui.Columns( 1 );
```

Also `GetColumnIndex`, `GetColumnsCount`, `GetColumnWidth/SetColumnWidth`, `GetColumnOffset/SetColumnOffset`. Dividers are draggable.

## Tab bars

```csharp
if ( ImGui.BeginTabBar( "tabs", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.TabListPopupButton ) )
{
	if ( ImGui.BeginTabItem( "General" ) )
	{
		ImGui.Text( "General settings" );
		ImGui.EndTabItem(); // only if BeginTabItem returned true
	}
	if ( ImGui.BeginTabItem( "Scene.scene", ref sceneTabOpen, dirty ? ImGuiTabItemFlags.UnsavedDocument : 0 ) )
	{
		ImGui.EndTabItem();
	}
	if ( ImGui.TabItemButton( "+", ImGuiTabItemFlags.Trailing ) )
		AddTab();
	ImGui.EndTabBar();
}
```

Tab bar flags: `Reorderable`, `AutoSelectNewTabs`, `TabListPopupButton`, `NoCloseWithMiddleMouseButton`, `NoTabListScrollingButtons`,
`NoTooltip`, `FittingPolicyResizeDown` (shrink tabs), `FittingPolicyScroll` (scroll arrows). Tab item flags: `UnsavedDocument`, `SetSelected`,
`NoCloseWithMiddleMouseButton`, `Leading`, `Trailing`, `NoTooltip`, `NoReorder`. `SetTabItemClosed( label )` closes a tab from code.

## Color editors & pickers

```csharp
ImGui.ColorEdit3( "Tint", ref tint );                                   // Vector3, Vector4 (keeps alpha) or s&box Color
ImGui.ColorEdit4( "Fog", ref fogColor, ImGuiColorEditFlags.AlphaBar );
ImGui.ColorEdit4( "Hex", ref c, ImGuiColorEditFlags.DisplayHex );
ImGui.ColorPicker4( "##picker", ref c, ImGuiColorEditFlags.PickerHueWheel, refCol: originalColor );
if ( ImGui.ColorButton( "swatch", c, ImGuiColorEditFlags.None, new Vector2( 40, 40 ) ) ) { }
ImGui.SetColorEditOptions( ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayHSV ); // default options
```

Clicking the small swatch opens a picker popup; right-click opens options (display mode, copy as...). Color swatches are
drag & drop sources (`"_COL3F"` / `"_COL4F"` payloads) and color editors accept drops. Flags: `NoAlpha`, `NoPicker`, `NoOptions`,
`NoSmallPreview`, `NoInputs`, `NoTooltip`, `NoLabel`, `NoSidePreview`, `NoDragDrop`, `NoBorder`, `AlphaBar`, `AlphaPreviewHalf`,
`AlphaOpaque`, `HDR`, `DisplayRGB/HSV/Hex`, `Uint8/Float`, `PickerHueBar/PickerHueWheel`, `InputRGB/InputHSV`.

## Plots & progress bars

```csharp
ImGui.PlotLines( "Frame times", frameTimes, offset, $"avg {avg:F2} ms", 0, 33, new Vector2( 0, 80 ) );
ImGui.PlotHistogram( "Histogram", values, 0, null, 0, 1, new Vector2( 0, 80 ) );
ImGui.PlotLines( "Sin", i => MathF.Sin( i * 0.1f ), 100 );    // getter overload
ImGui.ProgressBar( 0.42f );                                   // fills width, "42%"
ImGui.ProgressBar( loaded, new Vector2( 200, 0 ), $"{done}/{total}" );
ImGui.ProgressBar( -1.0f * (float)ImGui.GetTime() );          // negative = indeterminate animation
```

`scaleMin/scaleMax = float.MaxValue` auto-scale. Hovering a plot shows the value under the cursor.

## Drag and drop

```csharp
ImGui.Button( item.Name );
if ( ImGui.BeginDragDropSource() )
{
	ImGui.SetDragDropPayload( "INVENTORY_ITEM", item );   // any object
	ImGui.Text( "Moving {0}", item.Name );                // preview tooltip
	ImGui.EndDragDropSource();
}

ImGui.Button( "Trash" );
if ( ImGui.BeginDragDropTarget() )
{
	var payload = ImGui.AcceptDragDropPayload( "INVENTORY_ITEM" );
	if ( payload is not null )
		Destroy( payload.GetData<Item>() );
	ImGui.EndDragDropTarget();
}
```

Flags: `SourceNoPreviewTooltip`, `SourceNoDisableHover`, `SourceNoHoldToOpenOthers`, `SourceAllowNullID` (sources on items without IDs such as Text),
`AcceptBeforeDelivery`, `AcceptNoDrawDefaultRect`, `AcceptNoPreviewTooltip`, `AcceptPeekOnly`. `GetDragDropPayload()` returns the in-flight payload.
Hovering a tree node or tab with a payload for a moment opens it.

## Custom drawing (ImDrawList)

```csharp
var dl = ImGui.GetWindowDrawList();             // also GetBackgroundDrawList() / GetForegroundDrawList()
var p = ImGui.GetCursorScreenPos();
dl.AddRectFilled( p, p + new Vector2( 100, 50 ), new Color32( 255, 80, 80, 255 ), rounding: 6 );
dl.AddRect( p, p + new Vector2( 100, 50 ), ImGui.GetColorU32( ImGuiCol.Border ), 6, ImDrawFlags.None, 2 );
dl.AddLine( a, b, color, thickness: 2 );
dl.AddCircle( center, 20, color );  dl.AddCircleFilled( center, 10, color );
dl.AddNgon( center, 20, color, 6 ); dl.AddEllipseFilled( center, new Vector2( 30, 15 ), color );
dl.AddTriangleFilled( a, b, c, color ); dl.AddQuad( a, b, c, d, color );
dl.AddBezierCubic( p1, p2, p3, p4, color, 2 ); dl.AddPolyline( points, color, ImDrawFlags.Closed, 2 );
dl.AddConvexPolyFilled( points, color );
dl.AddRectFilledMultiColor( min, max, topLeft, topRight, bottomRight, bottomLeft ); // gradients
dl.AddText( pos, color, "Hello" );  dl.AddText( 24f, pos, color, "Big text" );
dl.AddImage( texture, min, max, uv0, uv1, tint ); dl.AddImageRounded( texture, min, max, uv0, uv1, tint, 8 );
dl.PathClear(); dl.PathLineTo( a ); dl.PathArcTo( c, r, 0, MathF.PI ); dl.PathStroke( color, ImDrawFlags.None, 2 );
dl.PushClipRect( min, max, true ); /* ... */ dl.PopClipRect();
dl.ChannelsSplit( 2 ); dl.ChannelsSetCurrent( 1 ); /* ... */ dl.ChannelsMerge();
ImGui.Dummy( new Vector2( 100, 50 ) ); // reserve the space you drew into
```

Rounded corners are selected with `ImDrawFlags.RoundCornersTopLeft` etc.

## Item & window queries

After submitting an item: `IsItemHovered( flags )`, `IsItemActive()` (held), `IsItemClicked( button )`, `IsItemFocused()`,
`IsItemVisible()`, `IsItemEdited()`, `IsItemActivated()`, `IsItemDeactivated()`, `IsItemDeactivatedAfterEdit()` (great for undo),
`IsItemToggledOpen()`, `GetItemRectMin/Max/Size()`, `GetItemID()`. Global: `IsAnyItemHovered/Active/Focused()`.
Overlapping items: `SetNextItemAllowOverlap()` before the item that sits underneath. `SetItemDefaultFocus()` for popups.
`IsItemHovered( ImGuiHoveredFlags.ForTooltip | DelayNormal | Stationary )` for delayed hover.

## Input queries

`GetIO()`, `GetMousePos()`, `IsMouseDown/Clicked/Released/DoubleClicked( ImGuiMouseButton )`, `GetMouseClickedCount`,
`IsMouseDragging`, `GetMouseDragDelta`, `ResetMouseDragDelta`, `IsMouseHoveringRect`, `IsAnyMouseDown`,
`IsKeyDown/Pressed/Released( ImGuiKey )`, `IsKeyChordPressed( ImGuiKey.ImGuiMod_Ctrl | ImGuiKey.S )`, `Shortcut(...)`,
`GetKeyName`, `SetMouseCursor( ImGuiMouseCursor )`, `SetNextFrameWantCaptureMouse/Keyboard( bool )`, `GetTime()`, `GetFrameCount()`.

To drive ImGui yourself (tests, replays), set `ImGuiSystem.Current.SimulateInput = true` (`using Duccsoft.ImGui.Systems;`) and feed `ImGui.GetIO()`:
`AddMousePosEvent`, `AddMouseButtonEvent`, `AddMouseWheelEvent`, `AddInputCharacter`, `AddKeyTyped`, `AddKeyEvent`, `AddPasteEvent`.

## Built-in tools

- `ImGui.ShowDemoWindow( ref open )`: port of `imgui_demo.cpp`: every widget, layout, popups, tables, inputs, plus example apps
  (main menu bar, console, log, property editor, documents, custom rendering, overlay, constrained/auto resize, fullscreen, window titles).
  The example apps are also public: `ShowExampleAppConsole`, `ShowExampleAppLog`, `ShowExampleAppPropertyEditor`, `ShowExampleAppDocuments`,
  `ShowExampleAppCustomRendering`, `ShowExampleAppLayout`, `ShowExampleAppMainMenuBar`.
- `ImGui.ShowMetricsWindow( ref open )`: windows, popups, hovered/active IDs, draw list sizes.
- `ImGui.ShowStyleEditor()`: live editing of sizes, colors, fonts; `ShowStyleSelector( label )`, `ShowFontSelector( label )`.
- `ImGui.ShowAboutWindow( ref open )`, `ImGui.ShowUserGuide()`.

## Component inspector

```csharp
myComponent.ImGuiInspector();   // window named after the component, or inline inside the current window
myComponent.ImGuiProperty( propertyDescription ); // a single property
```

Supported `[Property]` types: `float`, `double`, `int`, `bool`, `string`, `Vector2/3/4`, `Angles`, `Rotation`, `Color`, enums.
`[Range]` turns numbers/vectors into sliders; other types are shown read-only.

## Sample components

In `Code/Duccsoft.ImGui/Components` (namespace `Duccsoft.ImGui.Components`):
- **ImGui Demo Window**: toggles for the demo, metrics, style editor and about windows.
- **ImGui Inspector**: draws an inspector for a target component.
- **ImGui Example Properties**: one property of each supported type.

`Assets/scenes/imgui_demo.scene` wires all three together.

---

## Verification

Every component was compiled by the real s&box editor (including the API whitelist) and exercised in play mode through the
scripted test harness in `dev/editor-rig` (`host-code/ImGuiSelfTest*.cs`), which drives ImGui with simulated mouse and keyboard
input and asserts on the results. Latest run: **32 / 32 tests passed**.

| Area | In-engine interaction test | Screenshot |
|---|---|---|
| Windows: move, resize grip, collapse, close (X) | Window move/resize/collapse, Window close button, Demo window X closes it | `gallery_0` |
| Button, Checkbox, CheckboxFlags, RadioButton | Button click (incl. release-outside), Checkbox toggle, RadioButton select | `gallery_1` |
| Text, TextColored, TextDisabled, BulletText, SeparatorText, LabelText, TextLink, Image, ProgressBar | (rendering) | `gallery_0`, `gallery_1` |
| InputText, hint, password, multiline | InputText typing/backspace/enter, select-all + replace, Multiline newline | `gallery_1` |
| DragFloat/Int/N, DragFloatRange2 | DragFloat drag | `gallery_1` |
| SliderFloat/Int/N, SliderAngle, VSlider | SliderFloat click + drag, Ctrl+click text input, VSliderFloat drag | `gallery_1` |
| InputInt/Float/Double/N | InputInt step buttons | `gallery_1` |
| Combo, ListBox, Selectable | Combo, ListBox, Selectable | `gallery_1`, `gallery_4` |
| TreeNode, CollapsingHeader | TreeNode + CollapsingHeader | `gallery_4` |
| Child windows, scrolling | Child window mouse wheel scrolling | `gallery_4` |
| Menu bars, nested menus, main menu bar | Menu bar + menus (hover-open submenu), Main menu bar | `gallery_5` |
| Popups, modals, context menus, tooltips | Popup + click outside closes, Modal popup, Context menu, Tooltip on hover | `gallery_5` |
| Disabled blocks | Disabled items ignore clicks | `gallery_1` |
| Tables (sort, resize, cell widgets, bg colors, frozen scrolling) | Table header sort + cell widgets (incl. column resize) | `gallery_3` |
| Legacy columns | PlotLines hover + legacy Columns | `gallery_3` |
| Tab bars (switch, close, unsaved, scrolling, leading/trailing) | TabBar switch + close | `gallery_4` |
| ColorEdit3/4, ColorButton, ColorPicker (hue bar & wheel) | ColorEdit4 drag component, ColorPicker4 SV click | `gallery_2` |
| PlotLines, PlotHistogram | PlotLines hover | `gallery_2` |
| Drag and drop | Drag and drop payload | (interaction test) |
| ImDrawList primitives | (rendering) | `gallery_6` |
| Demo window, example apps, style editor, metrics | Demo window X closes it | `demo_window`, `gallery_7`, `gallery_8` |
| Component inspector | (rendering + live edit) | `gallery_1` |

Screenshots are in `dev/screenshots/`. To reproduce:

```powershell
powershell -ExecutionPolicy Bypass -File dev\editor-rig\start-editor.ps1   # scratch editor, sbox-mcp on port 8433
sh dev/editor-rig/run-selftest.sh                                         # prints the PASS/FAIL report
```

## Differences from Dear ImGui

- Rendering goes through s&box `Painter` (vector shapes, GPU text) instead of vertex buffers. Per-vertex colors are approximated:
  4-color gradients use strips, the hue wheel/SV triangle use flat-shaded segments.
- Fonts: any font family available to s&box (`io.FontName`), sized in pixels; no font atlas API.
- Text formatting uses .NET composite format for text functions; numeric widgets accept printf-style formats.
- No keyboard/gamepad navigation between items (Tab-cycling and arrow-key navigation); mouse and text editing are complete.
- No docking or multi-viewports. Settings (window positions, table column widths) persist for the session, not to an .ini file.
- Clipboard: copy uses `Clipboard.SetText`; paste comes from the OS paste event (Ctrl+V) since s&box has no clipboard read API.
- `TextLinkOpenURL` copies the URL to the clipboard (games cannot open a browser).
- Table angled headers are drawn upright.
