using Duccsoft.ImGui;

namespace ImGuiTests;

public static partial class ImGuiSelfTest
{
	private static string _txt = "";
	private static string _multiTxt = "";
	private static bool _enterPressed;
	private static float _dragVal;
	private static float _sliderVal;
	private static float _ctrlSliderVal;
	private static int _stepInt;
	private static int _sortCol = -1;
	private static ImGuiSortDirection _sortDir;
	private static float _colWidth;
	private static int _activeTab = -1;
	private static bool _tabBOpen = true;
	private static Vector4 _editCol;
	private static Vector4 _pickCol;
	private static int _dropped = -1;
	private static float _vsliderVal;
	private static bool _mainMenuItem;
	private static bool _tableCheck;
	private static float _plotHover;

	static partial void RegisterExtendedTestsImpl()
	{
		Add( "InputText typing/backspace/enter", () =>
		{
			if ( TestWindow( "T InputText", new Vector2( 360, 150 ) ) )
			{
				if ( ImGui.InputText( "Name", ref _txt, ImGuiInputTextFlags.EnterReturnsTrue ) ) _enterPressed = true;
				Rec( "it" );
			}
			ImGui.End();
		}, Test_InputText );

		Add( "InputText select-all + replace + escape", () =>
		{
			if ( TestWindow( "T InputText2", new Vector2( 360, 150 ) ) )
			{
				ImGui.InputText( "Field", ref _txt );
				Rec( "it" );
			}
			ImGui.End();
		}, Test_InputTextSelectAll );

		Add( "InputTextMultiline newline", () =>
		{
			if ( TestWindow( "T Multiline", new Vector2( 360, 200 ) ) )
			{
				ImGui.InputTextMultiline( "##ml", ref _multiTxt, new Vector2( 300, 100 ) );
				Rec( "ml" );
			}
			ImGui.End();
		}, Test_Multiline );

		Add( "DragFloat drag", () =>
		{
			if ( TestWindow( "T Drag", new Vector2( 360, 150 ) ) )
			{
				ImGui.DragFloat( "Drag", ref _dragVal, 0.1f );
				Rec( "d" );
			}
			ImGui.End();
		}, Test_Drag );

		Add( "SliderFloat click + drag", () =>
		{
			if ( TestWindow( "T Slider", new Vector2( 360, 150 ) ) )
			{
				ImGui.SetNextItemWidth( 200 );
				ImGui.SliderFloat( "Slider", ref _sliderVal, 0, 1 );
				Rec( "s" );
			}
			ImGui.End();
		}, Test_Slider );

		Add( "Slider Ctrl+click text input", () =>
		{
			if ( TestWindow( "T SliderInput", new Vector2( 360, 150 ) ) )
			{
				ImGui.SliderFloat( "SliderTI", ref _ctrlSliderVal, 0, 100 );
				Rec( "s" );
			}
			ImGui.End();
		}, Test_SliderTextInput );

		Add( "InputInt step buttons", () =>
		{
			if ( TestWindow( "T InputInt", new Vector2( 360, 150 ) ) )
			{
				ImGui.SetNextItemWidth( 250 );
				ImGui.InputInt( "Steps", ref _stepInt );
				Rec( "ii" );
			}
			ImGui.End();
		}, Test_InputInt );

		Add( "VSliderFloat drag", () =>
		{
			if ( TestWindow( "T VSlider", new Vector2( 200, 250 ) ) )
			{
				ImGui.VSliderFloat( "##vs", new Vector2( 30, 150 ), ref _vsliderVal, 0, 1 );
				Rec( "vs" );
			}
			ImGui.End();
		}, Test_VSlider );

		Add( "Table header sort + cell widgets", () =>
		{
			if ( TestWindow( "T Table", new Vector2( 420, 260 ) ) )
			{
				if ( ImGui.BeginTable( "t", 3, ImGuiTableFlags.Sortable | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable ) )
				{
					ImGui.TableSetupColumn( "A" );
					ImGui.TableSetupColumn( "B" );
					ImGui.TableSetupColumn( "C" );
					ImGui.TableNextRow( ImGuiTableRowFlags.Headers );
					for ( int c = 0; c < 3; c++ )
					{
						ImGui.TableSetColumnIndex( c );
						ImGui.TableHeader( new[] { "A", "B", "C" }[c] );
						Rec( "h" + c );
					}
					var specs = ImGui.TableGetSortSpecs();
					if ( specs is not null && specs.SpecsCount > 0 )
					{
						_sortCol = specs.Specs[0].ColumnIndex;
						_sortDir = specs.Specs[0].SortDirection;
						specs.SpecsDirty = false;
					}
					for ( int r = 0; r < 3; r++ )
					{
						ImGui.TableNextRow();
						ImGui.TableNextColumn(); ImGui.Text( "r{0}", r );
						ImGui.TableNextColumn();
						if ( r == 1 ) { ImGui.Checkbox( "##cb", ref _tableCheck ); Rec( "cb" ); }
						ImGui.TableNextColumn(); ImGui.Text( "x" );
					}
					ImGui.TableSetColumnIndex( 0 );
					_colWidth = ImGui.GetContentRegionAvail().x;
					ImGui.EndTable();
				}
			}
			ImGui.End();
		}, Test_Table );

		Add( "TabBar switch + close", () =>
		{
			if ( TestWindow( "T Tabs", new Vector2( 420, 200 ) ) )
			{
				if ( ImGui.BeginTabBar( "tb" ) )
				{
					if ( ImGui.BeginTabItem( "Alpha" ) ) { _activeTab = 0; ImGui.EndTabItem(); }
					Rec( "t0" );
					if ( _tabBOpen )
					{
						if ( ImGui.BeginTabItem( "Bravo", ref _tabBOpen ) ) { _activeTab = 1; ImGui.EndTabItem(); }
						Rec( "t1" );
					}
					if ( ImGui.BeginTabItem( "Charlie" ) ) { _activeTab = 2; ImGui.EndTabItem(); }
					Rec( "t2" );
					ImGui.EndTabBar();
				}
			}
			ImGui.End();
		}, Test_Tabs );

		Add( "ColorEdit4 drag component", () =>
		{
			if ( TestWindow( "T ColorEdit", new Vector2( 420, 150 ) ) )
			{
				ImGui.SetNextItemWidth( 300 );
				ImGui.ColorEdit4( "Col", ref _editCol );
				Rec( "ce" );
			}
			ImGui.End();
		}, Test_ColorEdit );

		Add( "ColorPicker4 SV click", () =>
		{
			if ( TestWindow( "T ColorPicker", new Vector2( 400, 360 ) ) )
			{
				ImGui.SetNextItemWidth( 250 );
				ImGui.ColorPicker4( "##pk", ref _pickCol, ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoInputs );
				Rec( "pk" );
			}
			ImGui.End();
		}, Test_ColorPicker );

		Add( "Drag and drop payload", () =>
		{
			if ( TestWindow( "T DragDrop", new Vector2( 360, 200 ) ) )
			{
				ImGui.Button( "Source", new Vector2( 100, 40 ) );
				Rec( "src" );
				if ( ImGui.BeginDragDropSource() )
				{
					ImGui.SetDragDropPayload( "TEST_INT", 42 );
					ImGui.Text( "Dragging 42" );
					ImGui.EndDragDropSource();
				}
				ImGui.SameLine( 0, 60 );
				ImGui.Button( "Target", new Vector2( 100, 40 ) );
				Rec( "dst" );
				if ( ImGui.BeginDragDropTarget() )
				{
					var payload = ImGui.AcceptDragDropPayload( "TEST_INT" );
					if ( payload is not null ) _dropped = payload.GetData<int>();
					ImGui.EndDragDropTarget();
				}
			}
			ImGui.End();
		}, Test_DragDrop );

		Add( "Main menu bar", () =>
		{
			if ( ImGui.BeginMainMenuBar() )
			{
				if ( ImGui.BeginMenu( "Game" ) )
				{
					if ( ImGui.MenuItem( "Restart" ) ) _mainMenuItem = true;
					Rec( "restart" );
					ImGui.EndMenu();
				}
				Rec( "game" );
				ImGui.EndMainMenuBar();
			}
		}, Test_MainMenu );

		Add( "PlotLines hover + legacy Columns", () =>
		{
			if ( TestWindow( "T Plot", new Vector2( 420, 260 ) ) )
			{
				ImGui.PlotLines( "Plot", new float[] { 0, 1, 0.5f, 0.2f, 0.9f }, 0, null, 0, 1, new Vector2( 250, 60 ) );
				Rec( "plot" );
				_plotHover = ImGui.IsItemHovered() ? 1 : 0;
				ImGui.Columns( 2, "cols" );
				ImGui.Text( "Left" );
				_colWidth = ImGui.GetColumnWidth();
				ImGui.NextColumn();
				ImGui.Text( "Right" );
				ImGui.Columns( 1 );
			}
			ImGui.End();
		}, Test_PlotColumns );
	}

	private static IEnumerator<object> Test_InputText()
	{
		_txt = "";
		_enterPressed = false;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "it" ).Min + new Vector2( 20, R( "it" ).Height * 0.5f ) ) ) yield return f;
		foreach ( var f in Type( "abcd" ) ) yield return f;
		Check( _txt == "abcd", $"typed text should be 'abcd', got '{_txt}'" );
		foreach ( var f in Key( ImGuiKey.Backspace ) ) yield return f;
		Check( _txt == "abc", $"backspace should give 'abc', got '{_txt}'" );
		foreach ( var f in Key( ImGuiKey.LeftArrow ) ) yield return f;
		foreach ( var f in Type( "X" ) ) yield return f;
		Check( _txt == "abXc", $"left arrow + type should give 'abXc', got '{_txt}'" );
		foreach ( var f in Key( ImGuiKey.Enter ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _enterPressed, "EnterReturnsTrue should return true on Enter" );
	}

	private static IEnumerator<object> Test_InputTextSelectAll()
	{
		_txt = "hello world";
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "it" ).Min + new Vector2( 20, R( "it" ).Height * 0.5f ) ) ) yield return f;
		foreach ( var f in Key( ImGuiKey.A, ctrl: true ) ) yield return f;
		foreach ( var f in Type( "new" ) ) yield return f;
		Check( _txt == "new", $"Ctrl+A then typing should replace, got '{_txt}'" );
		foreach ( var f in Key( ImGuiKey.Home ) ) yield return f;
		foreach ( var f in Key( ImGuiKey.Delete ) ) yield return f;
		Check( _txt == "ew", $"Home + Delete should give 'ew', got '{_txt}'" );
		// Click elsewhere deactivates
		foreach ( var f in Click( new Vector2( 1000, 600 ) ) ) yield return f;
		Check( !ImGui.GetIO().WantTextInput, "clicking outside should deactivate the text field" );
	}

	private static IEnumerator<object> Test_Multiline()
	{
		_multiTxt = "";
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "ml" ).Min + new Vector2( 20, 10 ) ) ) yield return f;
		foreach ( var f in Type( "one" ) ) yield return f;
		foreach ( var f in Key( ImGuiKey.Enter ) ) yield return f;
		foreach ( var f in Type( "two" ) ) yield return f;
		Check( _multiTxt.Replace( "\r", "" ) == "one\ntwo", $"multiline should contain 'one\\ntwo', got '{_multiTxt}'" );
	}

	private static IEnumerator<object> Test_Drag()
	{
		_dragVal = 1.0f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		var c = R( "d" ).Center;
		foreach ( var f in Drag( c, c + new Vector2( 50, 0 ), 10 ) ) yield return f;
		Check( _dragVal > 3.0f, $"dragging right by 50px at speed 0.1 should increase value well above 1, got {_dragVal}" );
		var before = _dragVal;
		// Wait past the double-click window: a second press at the same spot would enter text input mode.
		foreach ( var f in Frames( 40 ) ) yield return f;
		foreach ( var f in Drag( c, c - new Vector2( 30, 0 ), 10 ) ) yield return f;
		Check( _dragVal < before, $"dragging left should decrease the value ({before} -> {_dragVal})" );
	}

	private static IEnumerator<object> Test_Slider()
	{
		_sliderVal = 0;
		foreach ( var f in Frames( 3 ) ) yield return f;
		var r = R( "s" );
		var target = new Vector2( r.Min.x + 200 * 0.75f, r.Center.y );
		foreach ( var f in Click( target ) ) yield return f;
		Check( MathF.Abs( _sliderVal - 0.75f ) < 0.08f, $"clicking at 75% should set ~0.75, got {_sliderVal}" );
		foreach ( var f in Drag( target, new Vector2( r.Min.x + 2, r.Center.y ) ) ) yield return f;
		Check( _sliderVal < 0.05f, $"dragging to the left edge should set ~0, got {_sliderVal}" );
	}

	private static IEnumerator<object> Test_SliderTextInput()
	{
		_ctrlSliderVal = 10;
		foreach ( var f in Frames( 3 ) ) yield return f;
		var c = R( "s" ).Center;
		IO.AddKeyEvent( ImGuiKey.LeftCtrl, true );
		yield return null;
		foreach ( var f in Click( c ) ) yield return f;
		IO.AddKeyEvent( ImGuiKey.LeftCtrl, false );
		yield return null;
		foreach ( var f in Key( ImGuiKey.A, ctrl: true ) ) yield return f;
		foreach ( var f in Type( "42.5" ) ) yield return f;
		foreach ( var f in Key( ImGuiKey.Enter ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( MathF.Abs( _ctrlSliderVal - 42.5f ) < 0.01f, $"Ctrl+click then typing 42.5 should set the slider, got {_ctrlSliderVal}" );
	}

	private static IEnumerator<object> Test_InputInt()
	{
		_stepInt = 5;
		foreach ( var f in Frames( 3 ) ) yield return f;
		var r = R( "ii" );
		float btn = ImGui.GetFrameHeight();
		// Layout: [field][-][+] label. Item rect covers field + buttons + label; buttons sit right after the 250px width minus 2 buttons.
		var plus = new Vector2( r.Min.x + 250 - btn * 0.5f, r.Center.y );
		var minus = new Vector2( r.Min.x + 250 - btn * 1.5f - ImGui.GetStyle().ItemInnerSpacing.x, r.Center.y );
		foreach ( var f in Click( plus ) ) yield return f;
		Check( _stepInt == 6, $"+ button should increment to 6, got {_stepInt}" );
		foreach ( var f in Click( minus ) ) yield return f;
		foreach ( var f in Click( minus ) ) yield return f;
		Check( _stepInt == 4, $"- button twice should give 4, got {_stepInt}" );
	}

	private static IEnumerator<object> Test_VSlider()
	{
		_vsliderVal = 0;
		foreach ( var f in Frames( 3 ) ) yield return f;
		var r = R( "vs" );
		foreach ( var f in Click( new Vector2( r.Center.x, r.Min.y + 5 ) ) ) yield return f;
		Check( _vsliderVal > 0.9f, $"clicking near the top of a vertical slider should set ~1, got {_vsliderVal}" );
	}

	private static IEnumerator<object> Test_Table()
	{
		_sortCol = -1;
		_tableCheck = false;
		foreach ( var f in Frames( 4 ) ) yield return f;
		foreach ( var f in Click( R( "h1" ).Center ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _sortCol == 1, $"clicking header B should sort by column 1, got {_sortCol}" );
		var dir1 = _sortDir;
		foreach ( var f in Click( R( "h1" ).Center ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _sortDir != dir1 && _sortDir != ImGuiSortDirection.None, $"clicking again should flip direction ({dir1} -> {_sortDir})" );
		if ( Has( "cb" ) )
		{
			foreach ( var f in Click( R( "cb" ).Center ) ) yield return f;
			Check( _tableCheck, "checkbox inside a table cell should toggle" );
		}
		// Resize column A by dragging its right border.
		var w0 = R( "h0" ).Width;
		var border = new Vector2( R( "h0" ).Max.x, R( "h0" ).Center.y );
		foreach ( var f in Drag( border, border + new Vector2( 40, 0 ) ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( R( "h0" ).Width > w0 + 20, $"dragging the column border should widen column A ({w0} -> {R( "h0" ).Width})" );
	}

	private static IEnumerator<object> Test_Tabs()
	{
		_tabBOpen = true;
		foreach ( var f in Frames( 4 ) ) yield return f;
		Check( _activeTab == 0, $"first tab should be selected initially, got {_activeTab}" );
		foreach ( var f in Click( R( "t2" ).Center ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _activeTab == 2, $"clicking Charlie should select it, got {_activeTab}" );
		foreach ( var f in Click( R( "t1" ).Center ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _activeTab == 1, $"clicking Bravo should select it, got {_activeTab}" );
		// Close button on the selected tab (right side).
		var t1 = R( "t1" );
		var close = new Vector2( t1.Max.x - ImGui.GetStyle().FramePadding.x - ImGui.GetFontSize() * 0.5f, t1.Center.y );
		foreach ( var f in MoveTo( close ) ) yield return f;
		foreach ( var f in Click( close ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( !_tabBOpen, "clicking the tab close button should set open=false" );
	}

	private static IEnumerator<object> Test_ColorEdit()
	{
		_editCol = new Vector4( 0.2f, 0.4f, 0.6f, 1.0f );
		foreach ( var f in Frames( 3 ) ) yield return f;
		var r = R( "ce" );
		var start = new Vector2( r.Min.x + 15, r.Center.y );
		foreach ( var f in Drag( start, start + new Vector2( 40, 0 ), 8 ) ) yield return f;
		Check( _editCol.x > 0.25f, $"dragging the R field should increase red, got {_editCol.x}" );
		Check( MathF.Abs( _editCol.y - 0.4f ) < 0.01f, "other components should be unchanged" );
	}

	private static IEnumerator<object> Test_ColorPicker()
	{
		_pickCol = new Vector4( 1, 0, 0, 1 );
		foreach ( var f in Frames( 3 ) ) yield return f;
		var r = R( "pk" );
		// Bottom-left of the SV square: value ~0 -> near black.
		foreach ( var f in Click( new Vector2( r.Min.x + 4, r.Min.y + r.Height * 0.85f ) ) ) yield return f;
		float lum = _pickCol.x + _pickCol.y + _pickCol.z;
		Check( lum < 1.0f, $"clicking low in the SV square should darken the color, got ({_pickCol.x:F2},{_pickCol.y:F2},{_pickCol.z:F2})" );
	}

	private static IEnumerator<object> Test_DragDrop()
	{
		_dropped = -1;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Drag( R( "src" ).Center, R( "dst" ).Center, 12 ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _dropped == 42, $"dropping onto the target should deliver 42, got {_dropped}" );
	}

	private static IEnumerator<object> Test_MainMenu()
	{
		_mainMenuItem = false;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "game" ).Center ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( Has( "restart" ), "main menu should open" );
		if ( !Has( "restart" ) ) yield break;
		foreach ( var f in Click( R( "restart" ).Center ) ) yield return f;
		Check( _mainMenuItem, "main menu item should fire" );
	}

	private static IEnumerator<object> Test_PlotColumns()
	{
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in MoveTo( R( "plot" ).Min + new Vector2( 60, 30 ) ) ) yield return f;
		Check( _plotHover > 0, "plot should report hovered" );
		Check( _colWidth > 50, $"legacy column width should be positive, got {_colWidth}" );
	}
}
