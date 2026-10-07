using Duccsoft.ImGui;

namespace ImGuiTests;

/// <summary>
/// Screenshot gallery: each page lays out one family of widgets (popups/pickers pre-opened) for visual verification.
/// Select with the imgui_test_page convar while imgui_test_mode is "gallery".
/// </summary>
public static class ImGuiGallery
{
	public const int PageCount = 8;

	private static string _text = "Hello, s&box!";
	private static string _multi = "Multi-line text input\nLine 2\nLine 3";
	private static string _hint = "";
	private static string _password = "hunter2";
	private static int _i = 42;
	private static float _f = 3.14159f;
	private static Vector3 _v3 = new( 1, 2, 3 );
	private static float _drag = 0.5f;
	private static int _dragI = 50;
	private static Vector2 _drag2 = new( 0.25f, 0.75f );
	private static float _slider = 0.35f;
	private static int _sliderI = 7;
	private static Vector4 _slider4 = new( 0.1f, 0.4f, 0.6f, 0.9f );
	private static float _angle = 1.0f;
	private static float _rangeMin = 20, _rangeMax = 80;
	private static readonly float[] _vs = { 0.2f, 0.5f, 0.8f, 0.4f };
	private static Vector4 _col = new( 0.45f, 0.55f, 0.6f, 0.8f );
	private static Vector3 _col3 = new( 1.0f, 0.4f, 0.2f );
	private static Color _sboxColor = Color.Orange;
	private static readonly float[] _plot = Enumerable.Range( 0, 60 ).Select( i => MathF.Sin( i * 0.2f ) ).ToArray();
	private static readonly bool[] _tabsOpen = { true, true, true, true };
	private static int _selected = 1;
	private static int _list = 2;
	private static bool _check = true;
	private static int _combo = 1;
	private static bool _popupRequested;
	private static int _framesOnPage;
	private static int _lastPage = -1;

	public static void Draw( int page )
	{
		if ( page != _lastPage )
		{
			_lastPage = page;
			_framesOnPage = 0;
			_popupRequested = false;
		}
		_framesOnPage++;

		switch ( page )
		{
			case 0: PageInputs(); break;
			case 1: PageColorsPlots(); break;
			case 2: PageTables(); break;
			case 3: PageTabsTrees(); break;
			case 4: PagePopups(); break;
			case 5: PageDrawing(); break;
			case 6: PageExamples(); break;
			case 7: PageStyle(); break;
		}
	}

	private static void Win( string name, Vector2 pos, Vector2 size )
	{
		ImGui.SetNextWindowPos( pos, ImGuiCond.Always );
		ImGui.SetNextWindowSize( size, ImGuiCond.Always );
	}

	private static void PageInputs()
	{
		Win( "Text input", new Vector2( 10, 10 ), new Vector2( 430, 330 ) );
		if ( ImGui.Begin( "Text input" ) )
		{
			ImGui.InputText( "InputText", ref _text );
			ImGui.InputTextWithHint( "WithHint", "type here...", ref _hint );
			ImGui.InputText( "Password", ref _password, ImGuiInputTextFlags.Password );
			ImGui.InputTextMultiline( "Multiline", ref _multi, new Vector2( 0, ImGui.GetTextLineHeight() * 4 ) );
			ImGui.InputInt( "InputInt", ref _i );
			ImGui.InputFloat( "InputFloat", ref _f, 0.1f, 1.0f, "%.4f" );
			ImGui.InputFloat3( "InputFloat3", ref _v3 );
		}
		ImGui.End();

		Win( "Drags & sliders", new Vector2( 450, 10 ), new Vector2( 470, 330 ) );
		if ( ImGui.Begin( "Drags & sliders" ) )
		{
			ImGui.DragFloat( "DragFloat", ref _drag, 0.01f, 0, 1 );
			ImGui.DragInt( "DragInt %", ref _dragI, 1, 0, 100, "%d%%" );
			ImGui.DragFloat2( "DragFloat2", ref _drag2, 0.01f );
			ImGui.DragFloatRange2( "Range", ref _rangeMin, ref _rangeMax, 0.5f, 0, 100, "Min: %.0f", "Max: %.0f" );
			ImGui.SliderFloat( "SliderFloat", ref _slider, 0, 1, "ratio = %.2f" );
			ImGui.SliderInt( "SliderInt", ref _sliderI, 0, 10 );
			ImGui.SliderFloat4( "SliderFloat4", ref _slider4, 0, 1 );
			ImGui.SliderAngle( "SliderAngle", ref _angle );
			for ( int i = 0; i < _vs.Length; i++ )
			{
				if ( i > 0 ) ImGui.SameLine();
				ImGui.PushID( i );
				ImGui.VSliderFloat( "##v", new Vector2( 22, 90 ), ref _vs[i], 0, 1, "%.1f" );
				ImGui.PopID();
			}
		}
		ImGui.End();

		Win( "Buttons & toggles", new Vector2( 930, 10 ), new Vector2( 400, 330 ) );
		if ( ImGui.Begin( "Buttons & toggles" ) )
		{
			ImGui.Button( "Button" ); ImGui.SameLine();
			ImGui.SmallButton( "SmallButton" ); ImGui.SameLine();
			ImGui.ArrowButton( "##l", ImGuiDir.Left ); ImGui.SameLine();
			ImGui.ArrowButton( "##r", ImGuiDir.Right );
			ImGui.Checkbox( "Checkbox", ref _check );
			ImGui.RadioButton( "Radio A", ref _combo, 0 ); ImGui.SameLine();
			ImGui.RadioButton( "Radio B", ref _combo, 1 );
			ImGui.Combo( "Combo", ref _combo, new[] { "First", "Second", "Third" } );
			ImGui.ProgressBar( 0.66f );
			ImGui.BulletText( "BulletText" );
			ImGui.TextLink( "TextLink" );
			ImGui.LabelText( "LabelText", "value" );
			ImGui.SeparatorText( "SeparatorText" );
			ImGui.BeginDisabled();
			ImGui.Button( "Disabled button" );
			ImGui.EndDisabled();
			ImGui.Image( Texture.White, new Vector2( 32, 32 ), Vector2.Zero, Vector2.One, new Vector4( 0.3f, 0.7f, 1, 1 ), new Vector4( 1, 1, 1, 1 ) );
		}
		ImGui.End();

		Win( "Inspector", new Vector2( 10, 350 ), new Vector2( 600, 380 ) );
		if ( ImGui.Begin( "Inspector" ) )
		{
			var example = Game.ActiveScene?.GetAllComponents<Duccsoft.ImGui.Components.ExampleComponent>().FirstOrDefault();
			if ( example is null && Game.ActiveScene is not null )
				example = Game.ActiveScene.CreateObject().Components.Create<Duccsoft.ImGui.Components.ExampleComponent>();
			if ( example is null )
				ImGui.TextDisabled( "(no ExampleComponent in scene)" );
			else
				example.ImGuiInspector();
		}
		ImGui.End();
	}

	private static void PageColorsPlots()
	{
		Win( "Color editors", new Vector2( 10, 10 ), new Vector2( 470, 300 ) );
		if ( ImGui.Begin( "Color editors" ) )
		{
			ImGui.ColorEdit3( "ColorEdit3", ref _col3 );
			ImGui.ColorEdit4( "ColorEdit4", ref _col );
			ImGui.ColorEdit4( "HSV", ref _col, ImGuiColorEditFlags.DisplayHSV );
			ImGui.ColorEdit4( "Hex", ref _col, ImGuiColorEditFlags.DisplayHex );
			ImGui.ColorEdit4( "Float", ref _col, ImGuiColorEditFlags.Float );
			ImGui.ColorEdit4( "s&box Color", ref _sboxColor );
			ImGui.ColorButton( "btn", _col, ImGuiColorEditFlags.None, new Vector2( 60, 40 ) ); ImGui.SameLine();
			ImGui.ColorButton( "btn2", new Vector4( 1, 0, 0, 0.5f ), ImGuiColorEditFlags.AlphaPreviewHalf, new Vector2( 60, 40 ) );
		}
		ImGui.End();

		Win( "Picker (hue bar)", new Vector2( 490, 10 ), new Vector2( 400, 330 ) );
		if ( ImGui.Begin( "Picker (hue bar)" ) )
			ImGui.ColorPicker4( "##picker", ref _col, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.PickerHueBar );
		ImGui.End();

		Win( "Picker (hue wheel)", new Vector2( 900, 10 ), new Vector2( 430, 330 ) );
		if ( ImGui.Begin( "Picker (hue wheel)" ) )
			ImGui.ColorPicker4( "##wheel", ref _col, ImGuiColorEditFlags.PickerHueWheel | ImGuiColorEditFlags.NoSidePreview );
		ImGui.End();

		Win( "Plots", new Vector2( 10, 350 ), new Vector2( 660, 380 ) );
		if ( ImGui.Begin( "Plots" ) )
		{
			ImGui.PlotLines( "PlotLines", _plot, 0, "sin(x)", -1, 1, new Vector2( 0, 100 ) );
			ImGui.PlotHistogram( "PlotHistogram", _plot.Select( MathF.Abs ).ToArray(), 0, null, 0, 1, new Vector2( 0, 100 ) );
			ImGui.ProgressBar( 0.3f, new Vector2( -1.17549435E-38f, 0 ), "30% progress" );
			ImGui.ProgressBar( -1.0f * (float)ImGui.GetTime(), new Vector2( -1.17549435E-38f, 0 ), "Indeterminate" );
		}
		ImGui.End();
	}

	private static void PageTables()
	{
		Win( "Tables", new Vector2( 10, 10 ), new Vector2( 660, 380 ) );
		if ( ImGui.Begin( "Tables" ) )
		{
			var flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.Sortable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable;
			if ( ImGui.BeginTable( "tbl", 4, flags ) )
			{
				ImGui.TableSetupColumn( "ID", ImGuiTableColumnFlags.DefaultSort );
				ImGui.TableSetupColumn( "Name" );
				ImGui.TableSetupColumn( "Qty" );
				ImGui.TableSetupColumn( "Action", ImGuiTableColumnFlags.NoSort );
				ImGui.TableHeadersRow();
				string[] names = { "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig" };
				for ( int r = 0; r < names.Length; r++ )
				{
					ImGui.TableNextRow();
					ImGui.TableNextColumn(); ImGui.Text( "{0:D3}", r );
					ImGui.TableNextColumn(); ImGui.Text( names[r] );
					ImGui.TableNextColumn(); ImGui.Text( "{0}", r * 7 % 11 );
					ImGui.TableNextColumn(); ImGui.PushID( r ); ImGui.SmallButton( "Buy" ); ImGui.PopID();
				}
				ImGui.EndTable();
			}

			ImGui.SeparatorText( "Scrolling table with frozen header" );
			if ( ImGui.BeginTable( "scroll", 3, ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV, new Vector2( 0, ImGui.GetTextLineHeightWithSpacing() * 6 ) ) )
			{
				ImGui.TableSetupScrollFreeze( 0, 1 );
				ImGui.TableSetupColumn( "One" );
				ImGui.TableSetupColumn( "Two" );
				ImGui.TableSetupColumn( "Three" );
				ImGui.TableHeadersRow();
				for ( int r = 0; r < 40; r++ )
				{
					ImGui.TableNextRow();
					for ( int c = 0; c < 3; c++ )
					{
						ImGui.TableSetColumnIndex( c );
						ImGui.Text( "Cell {0},{1}", r, c );
					}
				}
				ImGui.EndTable();
			}
		}
		ImGui.End();

		Win( "Legacy columns", new Vector2( 680, 10 ), new Vector2( 650, 250 ) );
		if ( ImGui.Begin( "Legacy columns" ) )
		{
			ImGui.Columns( 3, "cols", true );
			ImGui.Separator();
			for ( int i = 0; i < 9; i++ )
			{
				ImGui.Text( "Item {0}", i );
				ImGui.Text( "Width {0:F0}", ImGui.GetColumnWidth() );
				ImGui.NextColumn();
			}
			ImGui.Columns( 1 );
			ImGui.Separator();
		}
		ImGui.End();

		Win( "Table bg colors", new Vector2( 680, 270 ), new Vector2( 650, 200 ) );
		if ( ImGui.Begin( "Table bg colors" ) )
		{
			if ( ImGui.BeginTable( "bg", 4, ImGuiTableFlags.Borders ) )
			{
				for ( int r = 0; r < 4; r++ )
				{
					ImGui.TableNextRow();
					if ( r == 1 ) ImGui.TableSetBgColor( ImGuiTableBgTarget.RowBg0, new Color32( 80, 40, 120, 255 ) );
					for ( int c = 0; c < 4; c++ )
					{
						ImGui.TableSetColumnIndex( c );
						if ( r == 2 && c == 2 ) ImGui.TableSetBgColor( ImGuiTableBgTarget.CellBg, new Color32( 180, 60, 60, 255 ) );
						ImGui.Text( "{0}{1}", (char)('A' + r), c );
					}
				}
				ImGui.EndTable();
			}
		}
		ImGui.End();
	}

	private static void PageTabsTrees()
	{
		Win( "Tab bars", new Vector2( 10, 10 ), new Vector2( 560, 260 ) );
		if ( ImGui.Begin( "Tab bars" ) )
		{
			string[] names = { "Artichoke", "Beetroot", "Celery", "Daikon" };
			if ( ImGui.BeginTabBar( "tabs", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.TabListPopupButton ) )
			{
				ImGui.TabItemButton( "+", ImGuiTabItemFlags.Trailing );
				for ( int n = 0; n < names.Length; n++ )
				{
					if ( !_tabsOpen[n] ) continue;
					if ( ImGui.BeginTabItem( names[n], ref _tabsOpen[n], n == 2 ? ImGuiTabItemFlags.UnsavedDocument : ImGuiTabItemFlags.None ) )
					{
						ImGui.Text( "Contents of the {0} tab", names[n] );
						ImGui.EndTabItem();
					}
				}
				ImGui.EndTabBar();
			}
			ImGui.SeparatorText( "Many tabs (scroll policy)" );
			if ( ImGui.BeginTabBar( "many", ImGuiTabBarFlags.FittingPolicyScroll ) )
			{
				for ( int n = 0; n < 14; n++ )
					if ( ImGui.BeginTabItem( $"Tab {n}" ) )
					{
						ImGui.Text( "Tab {0}", n );
						ImGui.EndTabItem();
					}
				ImGui.EndTabBar();
			}
		}
		ImGui.End();

		Win( "Trees & selectables", new Vector2( 580, 10 ), new Vector2( 380, 460 ) );
		if ( ImGui.Begin( "Trees & selectables" ) )
		{
			ImGui.SetNextItemOpen( true );
			if ( ImGui.TreeNode( "Root" ) )
			{
				ImGui.SetNextItemOpen( true );
				if ( ImGui.TreeNode( "Child A" ) )
				{
					ImGui.TreeNodeEx( "Leaf 1", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Bullet );
					ImGui.TreeNodeEx( "Leaf 2 (selected)", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Selected );
					ImGui.TreePop();
				}
				ImGui.TreeNode( "Child B (closed)" );
				ImGui.TreePop();
			}
			ImGui.SetNextItemOpen( true );
			ImGui.CollapsingHeader( "CollapsingHeader (open)" );
			bool visible = true;
			ImGui.CollapsingHeader( "Header with close", ref visible );
			for ( int i = 0; i < 4; i++ )
				if ( ImGui.Selectable( $"Selectable {i}", _selected == i ) ) _selected = i;
			ImGui.ListBox( "ListBox", ref _list, new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" }, 4 );
		}
		ImGui.End();

		Win( "Child windows", new Vector2( 970, 10 ), new Vector2( 360, 460 ) );
		if ( ImGui.Begin( "Child windows" ) )
		{
			ImGui.BeginChild( "c1", new Vector2( 0, 150 ), ImGuiChildFlags.Borders );
			for ( int i = 0; i < 30; i++ ) ImGui.Text( "Scrollable line {0}", i );
			ImGui.EndChild();
			ImGui.BeginChild( "c2", new Vector2( 0, 120 ), ImGuiChildFlags.FrameStyle );
			ImGui.Text( "FrameStyle child" );
			ImGui.Button( "Inside child" );
			ImGui.EndChild();
			ImGui.BeginGroup();
			ImGui.Button( "Group A" );
			ImGui.Button( "Group B" );
			ImGui.EndGroup();
			ImGui.SameLine();
			ImGui.Button( "Tall", new Vector2( 80, ImGui.GetItemRectSize().y ) );
		}
		ImGui.End();
	}

	private static void PagePopups()
	{
		if ( ImGui.BeginMainMenuBar() )
		{
			if ( ImGui.BeginMenu( "File" ) )
			{
				ImGui.MenuItem( "New", "Ctrl+N" );
				ImGui.MenuItem( "Open", "Ctrl+O" );
				ImGui.MenuItem( "Toggle", null, true );
				if ( ImGui.BeginMenu( "Recent" ) )
				{
					ImGui.MenuItem( "a.txt" );
					ImGui.EndMenu();
				}
				ImGui.Separator();
				ImGui.MenuItem( "Disabled", null, false, false );
				ImGui.EndMenu();
			}
			ImGui.BeginMenu( "Edit" );
			ImGui.BeginMenu( "View" );
			ImGui.EndMainMenuBar();
		}

		Win( "Popups", new Vector2( 10, 300 ), new Vector2( 400, 200 ) );
		if ( ImGui.Begin( "Popups" ) )
		{
			ImGui.Button( "Popup host" );
			if ( !_popupRequested && _framesOnPage > 2 )
			{
				ImGui.OpenPopup( "pop" );
				_popupRequested = true;
			}
			if ( ImGui.BeginPopup( "pop" ) )
			{
				ImGui.SeparatorText( "Popup" );
				ImGui.Selectable( "Bream" );
				ImGui.Selectable( "Haddock" );
				ImGui.Selectable( "Mackerel" );
				ImGui.EndPopup();
			}
			ImGui.Button( "Has tooltip" );
			ImGui.BeginTooltip();
			ImGui.Text( "Tooltip (always shown for this screenshot)" );
			ImGui.EndTooltip();
		}
		ImGui.End();

		Win( "Menu bar window", new Vector2( 420, 300 ), new Vector2( 380, 200 ) );
		if ( ImGui.Begin( "Menu bar window", ImGuiWindowFlags.MenuBar ) )
		{
			if ( ImGui.BeginMenuBar() )
			{
				ImGui.BeginMenu( "Options" );
				ImGui.BeginMenu( "Help" );
				ImGui.EndMenuBar();
			}
			ImGui.Text( "A window with its own menu bar." );
		}
		ImGui.End();
	}

	private static bool _customRendering = true;

	private static void PageDrawing()
	{
		ImGui.SetNextWindowPos( new Vector2( 10, 10 ), ImGuiCond.Always );
		ImGui.SetNextWindowSize( new Vector2( 900, 420 ), ImGuiCond.Always );
		ImGui.ShowExampleAppCustomRendering( ref _customRendering );
	}

	private static bool _console = true, _log = true, _props = true, _docs = true;

	private static void PageExamples()
	{
		ImGui.SetNextWindowPos( new Vector2( 10, 10 ), ImGuiCond.Always );
		ImGui.SetNextWindowSize( new Vector2( 440, 360 ), ImGuiCond.Always );
		ImGui.ShowExampleAppConsole( ref _console );
		ImGui.SetNextWindowPos( new Vector2( 460, 10 ), ImGuiCond.Always );
		ImGui.SetNextWindowSize( new Vector2( 430, 360 ), ImGuiCond.Always );
		ImGui.ShowExampleAppPropertyEditor( ref _props );
		ImGui.SetNextWindowPos( new Vector2( 900, 10 ), ImGuiCond.Always );
		ImGui.SetNextWindowSize( new Vector2( 430, 360 ), ImGuiCond.Always );
		ImGui.ShowExampleAppDocuments( ref _docs );
		ImGui.SetNextWindowPos( new Vector2( 10, 380 ), ImGuiCond.Always );
		ImGui.SetNextWindowSize( new Vector2( 700, 350 ), ImGuiCond.Always );
		ImGui.ShowExampleAppLog( ref _log );
	}

	private static bool _metrics = true;

	private static void PageStyle()
	{
		ImGui.SetNextWindowPos( new Vector2( 10, 10 ), ImGuiCond.Always );
		ImGui.SetNextWindowSize( new Vector2( 560, 720 ), ImGuiCond.Always );
		if ( ImGui.Begin( "Dear ImGui Style Editor" ) )
			ImGui.ShowStyleEditor();
		ImGui.End();
		ImGui.SetNextWindowPos( new Vector2( 580, 10 ), ImGuiCond.Always );
		ImGui.SetNextWindowSize( new Vector2( 750, 420 ), ImGuiCond.Always );
		ImGui.ShowMetricsWindow( ref _metrics );
	}
}
