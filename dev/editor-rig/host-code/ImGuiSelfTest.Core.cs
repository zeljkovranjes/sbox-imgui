using Duccsoft.ImGui;

namespace ImGuiTests;

public static partial class ImGuiSelfTest
{
	// Shared state used by the tests (reset by each test's coroutine).
	private static int _clicks;
	private static bool _check;
	private static int _radio;
	private static bool _nodeOpen;
	private static bool _headerOpen;
	private static int _selected = -1;
	private static Vector2 _winPos, _winSize;
	private static bool _winCollapsed;
	private static bool _winOpen = true;
	private static int _combo;
	private static string _menuClicked;
	private static bool _popupOpen;
	private static bool _modalOpen;
	private static float _scrollY;
	private static bool _tooltipShown;
	private static bool _ctxOpen;
	private static int _listIdx;
	private static int _disabledClicks;
	private static bool _hoveredFlag;
	private static int _flags;

	private static void RegisterCoreTests()
	{
		Add( "Button click", () =>
		{
			if ( TestWindow( "T Button", new Vector2( 300, 150 ) ) )
			{
				if ( ImGui.Button( "Press" ) ) _clicks++;
				Rec( "btn" );
			}
			ImGui.End();
		}, Test_Button );

		Add( "Checkbox toggle", () =>
		{
			if ( TestWindow( "T Checkbox", new Vector2( 300, 150 ) ) )
			{
				ImGui.Checkbox( "Check", ref _check );
				Rec( "cb" );
				ImGui.CheckboxFlags( "Flag 2", ref _flags, 2 );
				Rec( "cbf" );
			}
			ImGui.End();
		}, Test_Checkbox );

		Add( "RadioButton select", () =>
		{
			if ( TestWindow( "T Radio", new Vector2( 300, 150 ) ) )
			{
				ImGui.RadioButton( "A", ref _radio, 0 ); Rec( "a" );
				ImGui.SameLine();
				ImGui.RadioButton( "B", ref _radio, 1 ); Rec( "b" );
			}
			ImGui.End();
		}, Test_Radio );

		Add( "TreeNode + CollapsingHeader", () =>
		{
			if ( TestWindow( "T Tree", new Vector2( 300, 200 ) ) )
			{
				_headerOpen = ImGui.CollapsingHeader( "Header" ); Rec( "hdr" );
				_nodeOpen = ImGui.TreeNode( "Node" ); Rec( "node" );
				if ( _nodeOpen )
				{
					ImGui.Text( "Inside" );
					ImGui.TreePop();
				}
			}
			ImGui.End();
		}, Test_Tree );

		Add( "Selectable", () =>
		{
			if ( TestWindow( "T Selectable", new Vector2( 300, 200 ) ) )
			{
				for ( int i = 0; i < 3; i++ )
				{
					if ( ImGui.Selectable( $"Item {i}", _selected == i ) ) _selected = i;
					Rec( "sel" + i );
				}
			}
			ImGui.End();
		}, Test_Selectable );

		Add( "Window move/resize/collapse", () =>
		{
			if ( TestWindow( "T Window", new Vector2( 300, 200 ) ) )
			{
				ImGui.Text( "Drag my title" );
			}
			_winPos = ImGui.GetWindowPos();
			_winSize = ImGui.GetWindowSize();
			_winCollapsed = ImGui.IsWindowCollapsed();
			ImGui.End();
		}, Test_Window );

		Add( "Window close button", () =>
		{
			if ( !_winOpen ) return;
			ImGui.SetNextWindowPos( new Vector2( 60, 60 ), ImGuiCond.Appearing );
			ImGui.SetNextWindowSize( new Vector2( 300, 150 ), ImGuiCond.Appearing );
			if ( ImGui.Begin( "T Close", ref _winOpen, ImGuiWindowFlags.NoSavedSettings ) )
				ImGui.Text( "Close me" );
			_winPos = ImGui.GetWindowPos();
			_winSize = ImGui.GetWindowSize();
			ImGui.End();
		}, Test_Close );

		Add( "Combo", () =>
		{
			if ( TestWindow( "T Combo", new Vector2( 320, 150 ) ) )
			{
				ImGui.Combo( "Pick", ref _combo, new[] { "Apple", "Banana", "Cherry" } );
				Rec( "combo" );
			}
			ImGui.End();
		}, Test_Combo );

		Add( "Menu bar + menus", () =>
		{
			if ( TestWindow( "T Menu", new Vector2( 320, 200 ), ImGuiWindowFlags.MenuBar ) )
			{
				if ( ImGui.BeginMenuBar() )
				{
					if ( ImGui.BeginMenu( "File" ) )
					{
						if ( ImGui.MenuItem( "Open", "Ctrl+O" ) ) _menuClicked = "Open";
						Rec( "open" );
						if ( ImGui.BeginMenu( "Recent" ) )
						{
							if ( ImGui.MenuItem( "a.txt" ) ) _menuClicked = "a.txt";
							Rec( "recent_a" );
							ImGui.EndMenu();
						}
						Rec( "recent" );
						ImGui.EndMenu();
					}
					Rec( "file" );
					ImGui.EndMenuBar();
				}
				ImGui.Text( "Body" );
			}
			ImGui.End();
		}, Test_Menu );

		Add( "Popup + click outside closes", () =>
		{
			if ( TestWindow( "T Popup", new Vector2( 320, 200 ) ) )
			{
				if ( ImGui.Button( "Open popup" ) ) ImGui.OpenPopup( "my_popup" );
				Rec( "openbtn" );
				_popupOpen = false;
				if ( ImGui.BeginPopup( "my_popup" ) )
				{
					_popupOpen = true;
					ImGui.Text( "I'm a popup" );
					if ( ImGui.Selectable( "Choose" ) ) _menuClicked = "Choose";
					Rec( "choose" );
					ImGui.EndPopup();
				}
			}
			ImGui.End();
		}, Test_Popup );

		Add( "Modal popup", () =>
		{
			if ( TestWindow( "T Modal", new Vector2( 320, 200 ) ) )
			{
				if ( ImGui.Button( "Open modal" ) ) ImGui.OpenPopup( "Confirm" );
				Rec( "openbtn" );
				_modalOpen = false;
				if ( ImGui.BeginPopupModal( "Confirm", ImGuiWindowFlags.AlwaysAutoResize ) )
				{
					_modalOpen = true;
					ImGui.Text( "Are you sure?" );
					if ( ImGui.Button( "OK" ) ) ImGui.CloseCurrentPopup();
					Rec( "ok" );
					ImGui.EndPopup();
				}
			}
			ImGui.End();
		}, Test_Modal );

		Add( "Context menu (right click)", () =>
		{
			if ( TestWindow( "T Context", new Vector2( 320, 200 ) ) )
			{
				ImGui.Button( "Right-click me" );
				Rec( "btn" );
				_ctxOpen = false;
				if ( ImGui.BeginPopupContextItem( "ctx" ) )
				{
					_ctxOpen = true;
					if ( ImGui.MenuItem( "Delete" ) ) _menuClicked = "Delete";
					Rec( "del" );
					ImGui.EndPopup();
				}
			}
			ImGui.End();
		}, Test_Context );

		Add( "Child window mouse wheel scrolling", () =>
		{
			if ( TestWindow( "T Scroll", new Vector2( 320, 260 ) ) )
			{
				ImGui.BeginChild( "scroller", new Vector2( 0, 150 ), ImGuiChildFlags.Borders );
				for ( int i = 0; i < 50; i++ ) ImGui.Text( "Line {0}", i );
				_scrollY = ImGui.GetScrollY();
				ImGui.EndChild();
				Rec( "child" );
			}
			ImGui.End();
		}, Test_Scroll );

		Add( "Tooltip on hover", () =>
		{
			if ( TestWindow( "T Tooltip", new Vector2( 320, 150 ) ) )
			{
				ImGui.Button( "Hover me" );
				Rec( "btn" );
				_hoveredFlag = ImGui.IsItemHovered();
				if ( ImGui.IsItemHovered() )
				{
					ImGui.BeginTooltip();
					ImGui.Text( "Tooltip!" );
					ImGui.EndTooltip();
					_tooltipShown = true;
				}
			}
			ImGui.End();
		}, Test_Tooltip );

		Add( "Disabled items ignore clicks", () =>
		{
			if ( TestWindow( "T Disabled", new Vector2( 320, 150 ) ) )
			{
				ImGui.BeginDisabled();
				if ( ImGui.Button( "Can't click" ) ) _disabledClicks++;
				Rec( "btn" );
				ImGui.EndDisabled();
			}
			ImGui.End();
		}, Test_Disabled );

		Add( "ListBox", () =>
		{
			if ( TestWindow( "T ListBox", new Vector2( 320, 240 ) ) )
			{
				ImGui.ListBox( "Fruits", ref _listIdx, new[] { "Apple", "Banana", "Cherry", "Date" }, 4 );
				Rec( "lb" );
			}
			ImGui.End();
		}, Test_ListBox );
	}

	private static IEnumerator<object> Test_Button()
	{
		_clicks = 0;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "btn" ).Center ) ) yield return f;
		Check( _clicks == 1, $"expected 1 click, got {_clicks}" );
		foreach ( var f in Click( R( "btn" ).Center ) ) yield return f;
		Check( _clicks == 2, $"expected 2 clicks, got {_clicks}" );
		// Press inside, release outside: no click.
		IO.AddMousePosEvent( R( "btn" ).Center.x, R( "btn" ).Center.y );
		yield return null;
		IO.AddMouseButtonEvent( 0, true );
		yield return null;
		IO.AddMousePosEvent( R( "btn" ).Max.x + 200, R( "btn" ).Max.y + 200 );
		yield return null;
		IO.AddMouseButtonEvent( 0, false );
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _clicks == 2, $"release outside should not click, got {_clicks}" );
	}

	private static IEnumerator<object> Test_Checkbox()
	{
		_check = false;
		_flags = 0;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "cb" ).Center ) ) yield return f;
		Check( _check, "checkbox should be checked" );
		foreach ( var f in Click( R( "cb" ).Center ) ) yield return f;
		Check( !_check, "checkbox should be unchecked" );
		foreach ( var f in Click( R( "cbf" ).Center ) ) yield return f;
		Check( _flags == 2, $"CheckboxFlags should set flag 2, flags={_flags}" );
	}

	private static IEnumerator<object> Test_Radio()
	{
		_radio = 0;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "b" ).Center ) ) yield return f;
		Check( _radio == 1, $"radio should be 1, got {_radio}" );
		foreach ( var f in Click( R( "a" ).Center ) ) yield return f;
		Check( _radio == 0, $"radio should be 0, got {_radio}" );
	}

	private static IEnumerator<object> Test_Tree()
	{
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( !_headerOpen && !_nodeOpen, "header/node should start closed" );
		foreach ( var f in Click( R( "node" ).Min + new Vector2( 10, 5 ) ) ) yield return f;
		Check( _nodeOpen, "tree node should open on click" );
		foreach ( var f in Click( R( "hdr" ).Center ) ) yield return f;
		Check( _headerOpen, "collapsing header should open on click" );
		foreach ( var f in Click( R( "hdr" ).Center ) ) yield return f;
		Check( !_headerOpen, "collapsing header should close on second click" );
	}

	private static IEnumerator<object> Test_Selectable()
	{
		_selected = -1;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "sel1" ).Center ) ) yield return f;
		Check( _selected == 1, $"selected should be 1, got {_selected}" );
		foreach ( var f in Click( R( "sel2" ).Center ) ) yield return f;
		Check( _selected == 2, $"selected should be 2, got {_selected}" );
	}

	private static IEnumerator<object> Test_Window()
	{
		foreach ( var f in Frames( 4 ) ) yield return f;
		var startPos = _winPos;
		var titleY = _winPos.y + ImGui.GetFrameHeight() * 0.5f;
		// Move by dragging the title bar (away from the collapse arrow).
		foreach ( var f in Drag( new Vector2( _winPos.x + 150, titleY ), new Vector2( _winPos.x + 250, titleY + 80 ) ) ) yield return f;
		var moved = _winPos - startPos;
		Check( MathF.Abs( moved.x - 100 ) < 2 && MathF.Abs( moved.y - 80 ) < 2, $"window should move by (100,80), moved {moved}" );

		// Resize with the bottom-right grip.
		var startSize = _winSize;
		var grip = _winPos + _winSize - new Vector2( 4, 4 );
		foreach ( var f in Drag( grip, grip + new Vector2( 60, 40 ) ) ) yield return f;
		var grown = _winSize - startSize;
		Check( MathF.Abs( grown.x - 60 ) < 3 && MathF.Abs( grown.y - 40 ) < 3, $"window should grow by (60,40), grew {grown}" );

		// Collapse with the arrow.
		var arrow = _winPos + new Vector2( ImGui.GetStyle().FramePadding.x + ImGui.GetFontSize() * 0.5f, ImGui.GetFrameHeight() * 0.5f );
		foreach ( var f in Click( arrow ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _winCollapsed, "window should collapse" );
		foreach ( var f in Click( arrow ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( !_winCollapsed, "window should expand" );
	}

	private static IEnumerator<object> Test_Close()
	{
		_winOpen = true;
		foreach ( var f in Frames( 4 ) ) yield return f;
		var closeBtn = new Vector2( _winPos.x + _winSize.x - ImGui.GetStyle().FramePadding.x - ImGui.GetFontSize() * 0.5f - 1, _winPos.y + ImGui.GetFrameHeight() * 0.5f );
		foreach ( var f in Click( closeBtn ) ) yield return f;
		Check( !_winOpen, "close button should set open=false" );
	}

	private static IEnumerator<object> Test_Combo()
	{
		_combo = 0;
		foreach ( var f in Frames( 3 ) ) yield return f;
		var combo = R( "combo" );
		foreach ( var f in Click( new Vector2( combo.Min.x + 20, combo.Center.y ) ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		// Items are listed below the combo, one line each.
		float line = ImGui.GetTextLineHeightWithSpacing();
		var thirdItem = new Vector2( combo.Min.x + 20, combo.Max.y + ImGui.GetStyle().WindowPadding.y + line * 2.5f );
		foreach ( var f in Click( thirdItem ) ) yield return f;
		Check( _combo == 2, $"combo should select Cherry (2), got {_combo}" );
	}

	private static IEnumerator<object> Test_Menu()
	{
		_menuClicked = null;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "file" ).Center ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( Has( "open" ), "File menu should open on click" );
		if ( !Has( "open" ) ) yield break;
		// Hover "Recent" to open the sub-menu, then click the item.
		foreach ( var f in MoveTo( R( "recent" ).Center ) ) yield return f;
		foreach ( var f in Frames( 4 ) ) yield return f;
		Check( Has( "recent_a" ), "Recent sub-menu should open on hover" );
		if ( !Has( "recent_a" ) ) yield break;
		var target = R( "recent_a" ).Center;
		// Move horizontally first so the hover-intent triangle keeps the submenu open.
		foreach ( var f in MoveTo( new Vector2( target.x, R( "recent" ).Center.y ) ) ) yield return f;
		foreach ( var f in Click( target ) ) yield return f;
		Check( _menuClicked == "a.txt", $"sub-menu item should be clicked, got '{_menuClicked}'" );
		_rects.Remove( "open" );
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( !Has( "open" ), "menu should close after selecting an item" );
	}

	private static IEnumerator<object> Test_Popup()
	{
		_menuClicked = null;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "openbtn" ).Center ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( _popupOpen, "popup should open" );
		// Click outside of everything closes it.
		foreach ( var f in Click( new Vector2( 1000, 600 ) ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( !_popupOpen, "popup should close when clicking outside" );
		// Reopen and choose an item: closes automatically.
		foreach ( var f in Click( R( "openbtn" ).Center ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( _popupOpen && Has( "choose" ), "popup should reopen" );
		if ( !Has( "choose" ) ) yield break;
		foreach ( var f in Click( R( "choose" ).Center ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _menuClicked == "Choose" && !_popupOpen, $"selectable in popup should fire and close it (clicked={_menuClicked}, open={_popupOpen})" );
	}

	private static IEnumerator<object> Test_Modal()
	{
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "openbtn" ).Center ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( _modalOpen, "modal should open" );
		foreach ( var f in Click( new Vector2( 5, 5 ) ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _modalOpen, "modal should stay open when clicking outside" );
		if ( !Has( "ok" ) ) yield break;
		foreach ( var f in Click( R( "ok" ).Center ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( !_modalOpen, "modal should close via CloseCurrentPopup" );
	}

	private static IEnumerator<object> Test_Context()
	{
		_menuClicked = null;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "btn" ).Center, 1 ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( _ctxOpen, "context menu should open on right click" );
		if ( !Has( "del" ) ) yield break;
		foreach ( var f in Click( R( "del" ).Center ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _menuClicked == "Delete" && !_ctxOpen, $"context menu item should fire and close (clicked={_menuClicked})" );
	}

	private static IEnumerator<object> Test_Scroll()
	{
		foreach ( var f in Frames( 4 ) ) yield return f;
		Check( _scrollY == 0, $"scroll should start at 0, got {_scrollY}" );
		foreach ( var f in Wheel( R( "child" ).Center, -2 ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _scrollY > 0, $"mouse wheel down should scroll the child, scrollY={_scrollY}" );
		var after = _scrollY;
		foreach ( var f in Wheel( R( "child" ).Center, 1 ) ) yield return f;
		foreach ( var f in Frames( 2 ) ) yield return f;
		Check( _scrollY < after, $"mouse wheel up should scroll back, scrollY={_scrollY}" );
	}

	private static IEnumerator<object> Test_Tooltip()
	{
		_tooltipShown = false;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in MoveTo( R( "btn" ).Center ) ) yield return f;
		foreach ( var f in Frames( 3 ) ) yield return f;
		Check( _hoveredFlag, "IsItemHovered should be true over the button" );
		Check( _tooltipShown, "tooltip should be submitted while hovered" );
		foreach ( var f in MoveTo( new Vector2( 1000, 600 ) ) ) yield return f;
		Check( !_hoveredFlag, "IsItemHovered should be false away from the button" );
	}

	private static IEnumerator<object> Test_Disabled()
	{
		_disabledClicks = 0;
		foreach ( var f in Frames( 3 ) ) yield return f;
		foreach ( var f in Click( R( "btn" ).Center ) ) yield return f;
		Check( _disabledClicks == 0, "disabled button must not be clickable" );
	}

	private static IEnumerator<object> Test_ListBox()
	{
		_listIdx = 0;
		foreach ( var f in Frames( 4 ) ) yield return f;
		var lb = R( "lb" );
		float line = ImGui.GetTextLineHeightWithSpacing();
		var third = new Vector2( lb.Min.x + 30, lb.Min.y + ImGui.GetStyle().FramePadding.y + line * 2.5f );
		foreach ( var f in Click( third ) ) yield return f;
		Check( _listIdx == 2, $"list box should select index 2, got {_listIdx}" );
	}
}
