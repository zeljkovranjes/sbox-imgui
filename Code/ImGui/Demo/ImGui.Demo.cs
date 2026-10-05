namespace Duccsoft.ImGui;

/// <summary>
/// Port of Dear ImGui's demo window (imgui_demo.cpp). Call ImGui.ShowDemoWindow() from any component's OnUpdate.
/// </summary>
public static partial class ImGui
{
	private static partial class Demo
	{
		// Window toggles
		public static bool ShowAppMainMenuBar;
		public static bool ShowAppConsole;
		public static bool ShowAppLog;
		public static bool ShowAppLayout;
		public static bool ShowAppPropertyEditor;
		public static bool ShowAppLongText;
		public static bool ShowAppAutoResize;
		public static bool ShowAppConstrainedResize;
		public static bool ShowAppSimpleOverlay;
		public static bool ShowAppFullscreen;
		public static bool ShowAppWindowTitles;
		public static bool ShowAppCustomRendering;
		public static bool ShowAppDocuments;
		public static bool ShowMetrics;
		public static bool ShowStyleEditor;
		public static bool ShowAbout;

		// Window flags
		public static bool NoTitlebar, NoScrollbar, NoMenu, NoMove, NoResize, NoCollapse, NoClose, NoNav, NoBackground, NoBringToFront, UnsavedDocument;
	}

	/// <summary>Create a demo window demonstrating most ImGui features.</summary>
	public static void ShowDemoWindow()
	{
		bool open = true;
		ShowDemoWindow( ref open );
	}

	/// <summary>Create a demo window demonstrating most ImGui features. Pass a bool to get a close button.</summary>
	public static void ShowDemoWindow( ref bool open )
	{
		if ( Demo.ShowAppMainMenuBar ) Demo.ShowExampleAppMainMenuBar();
		if ( Demo.ShowAppConsole ) Demo.ShowExampleAppConsole( ref Demo.ShowAppConsole );
		if ( Demo.ShowAppLog ) Demo.ShowExampleAppLog( ref Demo.ShowAppLog );
		if ( Demo.ShowAppLayout ) Demo.ShowExampleAppLayout( ref Demo.ShowAppLayout );
		if ( Demo.ShowAppPropertyEditor ) Demo.ShowExampleAppPropertyEditor( ref Demo.ShowAppPropertyEditor );
		if ( Demo.ShowAppLongText ) Demo.ShowExampleAppLongText( ref Demo.ShowAppLongText );
		if ( Demo.ShowAppAutoResize ) Demo.ShowExampleAppAutoResize( ref Demo.ShowAppAutoResize );
		if ( Demo.ShowAppConstrainedResize ) Demo.ShowExampleAppConstrainedResize( ref Demo.ShowAppConstrainedResize );
		if ( Demo.ShowAppSimpleOverlay ) Demo.ShowExampleAppSimpleOverlay( ref Demo.ShowAppSimpleOverlay );
		if ( Demo.ShowAppFullscreen ) Demo.ShowExampleAppFullscreen( ref Demo.ShowAppFullscreen );
		if ( Demo.ShowAppWindowTitles ) Demo.ShowExampleAppWindowTitles();
		if ( Demo.ShowAppCustomRendering ) Demo.ShowExampleAppCustomRendering( ref Demo.ShowAppCustomRendering );
		if ( Demo.ShowAppDocuments ) Demo.ShowExampleAppDocuments( ref Demo.ShowAppDocuments );

		if ( Demo.ShowMetrics ) ShowMetricsWindow( ref Demo.ShowMetrics );
		if ( Demo.ShowAbout ) ShowAboutWindow( ref Demo.ShowAbout );
		if ( Demo.ShowStyleEditor )
		{
			if ( Begin( "Dear ImGui Style Editor", ref Demo.ShowStyleEditor ) )
				ShowStyleEditor();
			End();
		}

		var windowFlags = ImGuiWindowFlags.None;
		if ( Demo.NoTitlebar ) windowFlags |= ImGuiWindowFlags.NoTitleBar;
		if ( Demo.NoScrollbar ) windowFlags |= ImGuiWindowFlags.NoScrollbar;
		if ( !Demo.NoMenu ) windowFlags |= ImGuiWindowFlags.MenuBar;
		if ( Demo.NoMove ) windowFlags |= ImGuiWindowFlags.NoMove;
		if ( Demo.NoResize ) windowFlags |= ImGuiWindowFlags.NoResize;
		if ( Demo.NoCollapse ) windowFlags |= ImGuiWindowFlags.NoCollapse;
		if ( Demo.NoNav ) windowFlags |= ImGuiWindowFlags.NoNav;
		if ( Demo.NoBackground ) windowFlags |= ImGuiWindowFlags.NoBackground;
		if ( Demo.NoBringToFront ) windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus;
		if ( Demo.UnsavedDocument ) windowFlags |= ImGuiWindowFlags.UnsavedDocument;

		var g = G;
		SetNextWindowPos( new Vector2( 650, 20 ) * g.AppliedStyleScale, ImGuiCond.FirstUseEver );
		SetNextWindowSize( new Vector2( 550, 680 ) * g.AppliedStyleScale, ImGuiCond.FirstUseEver );

		bool visible = Demo.NoClose ? Begin( "Dear ImGui Demo", windowFlags ) : Begin( "Dear ImGui Demo", ref open, windowFlags );
		if ( !visible )
		{
			End();
			return;
		}

		PushItemWidth( GetFontSize() * -12 );

		if ( BeginMenuBar() )
		{
			if ( BeginMenu( "Menu" ) )
			{
				Demo.ShowExampleMenuFile();
				EndMenu();
			}
			if ( BeginMenu( "Examples" ) )
			{
				MenuItem( "Main menu bar", null, ref Demo.ShowAppMainMenuBar );
				SeparatorText( "Mini apps" );
				MenuItem( "Console", null, ref Demo.ShowAppConsole );
				MenuItem( "Documents", null, ref Demo.ShowAppDocuments );
				MenuItem( "Log", null, ref Demo.ShowAppLog );
				MenuItem( "Property editor", null, ref Demo.ShowAppPropertyEditor );
				MenuItem( "Simple layout", null, ref Demo.ShowAppLayout );
				SeparatorText( "Concepts" );
				MenuItem( "Auto-resizing window", null, ref Demo.ShowAppAutoResize );
				MenuItem( "Constrained-resizing window", null, ref Demo.ShowAppConstrainedResize );
				MenuItem( "Custom rendering", null, ref Demo.ShowAppCustomRendering );
				MenuItem( "Fullscreen window", null, ref Demo.ShowAppFullscreen );
				MenuItem( "Long text display", null, ref Demo.ShowAppLongText );
				MenuItem( "Manipulating window titles", null, ref Demo.ShowAppWindowTitles );
				MenuItem( "Simple overlay", null, ref Demo.ShowAppSimpleOverlay );
				EndMenu();
			}
			if ( BeginMenu( "Tools" ) )
			{
				MenuItem( "Metrics/Debugger", null, ref Demo.ShowMetrics );
				MenuItem( "Style Editor", null, ref Demo.ShowStyleEditor );
				MenuItem( "About Dear ImGui", null, ref Demo.ShowAbout );
				EndMenu();
			}
			EndMenuBar();
		}

		Text( "dear imgui says hello! (s&box port, Dear ImGui 1.91 API)" );
		Spacing();

		if ( CollapsingHeader( "Help" ) )
		{
			SeparatorText( "ABOUT THIS DEMO:" );
			BulletText( "Sections below are demonstrating many aspects of the library." );
			BulletText( "The \"Examples\" menu above leads to more demo contents." );
			BulletText( "The \"Tools\" menu above gives access to: About Box, Style Editor,\nand Metrics/Debugger (general purpose Dear ImGui debugging tool)." );
			SeparatorText( "PROGRAMMER GUIDE:" );
			BulletText( "See the ShowDemoWindow() code in Code/ImGui/Demo. <- you are here!" );
			BulletText( "See docs.md in the repository for usage of every widget." );
			SeparatorText( "USER GUIDE:" );
			ShowUserGuide();
		}

		if ( CollapsingHeader( "Configuration" ) )
		{
			var io = GetIO();
			if ( TreeNode( "Configuration##2" ) )
			{
				SeparatorText( "General" );
				Checkbox( "io.AutoScale", ref io.AutoScale );
				SetItemTooltip( "Scale the UI with the screen resolution (reference 1080p)." );
				DragFloat( "io.FontGlobalScale", ref io.FontGlobalScale, 0.005f, 0.3f, 3.0f, "%.2f" );
				DragFloat( "io.FontSize", ref io.FontSize, 0.1f, 6f, 48f, "%.1f" );
				Checkbox( "io.ConfigWindowsResizeFromEdges", ref io.ConfigWindowsResizeFromEdges );
				Checkbox( "io.ConfigWindowsMoveFromTitleBarOnly", ref io.ConfigWindowsMoveFromTitleBarOnly );
				Checkbox( "io.ConfigInputTextCursorBlink", ref io.ConfigInputTextCursorBlink );
				Checkbox( "io.ConfigDrawCursorShape", ref io.ConfigDrawCursorShape );
				DragFloat( "io.MouseDoubleClickTime", ref io.MouseDoubleClickTime, 0.005f, 0.1f, 1.0f, "%.2f s" );
				DragFloat( "io.MouseDragThreshold", ref io.MouseDragThreshold, 0.1f, 0.0f, 20.0f, "%.1f px" );
				DragFloat( "io.KeyRepeatDelay", ref io.KeyRepeatDelay, 0.005f, 0.05f, 1.0f, "%.3f s" );
				DragFloat( "io.KeyRepeatRate", ref io.KeyRepeatRate, 0.005f, 0.01f, 1.0f, "%.3f s" );
				TreePop();
				Spacing();
			}
			if ( TreeNode( "Style" ) )
			{
				ShowStyleEditor();
				TreePop();
				Spacing();
			}
		}

		if ( CollapsingHeader( "Window options" ) )
		{
			if ( BeginTable( "split", 3 ) )
			{
				TableNextColumn(); Checkbox( "No titlebar", ref Demo.NoTitlebar );
				TableNextColumn(); Checkbox( "No scrollbar", ref Demo.NoScrollbar );
				TableNextColumn(); Checkbox( "No menu", ref Demo.NoMenu );
				TableNextColumn(); Checkbox( "No move", ref Demo.NoMove );
				TableNextColumn(); Checkbox( "No resize", ref Demo.NoResize );
				TableNextColumn(); Checkbox( "No collapse", ref Demo.NoCollapse );
				TableNextColumn(); Checkbox( "No close", ref Demo.NoClose );
				TableNextColumn(); Checkbox( "No nav", ref Demo.NoNav );
				TableNextColumn(); Checkbox( "No background", ref Demo.NoBackground );
				TableNextColumn(); Checkbox( "No bring to front", ref Demo.NoBringToFront );
				TableNextColumn(); Checkbox( "Unsaved document", ref Demo.UnsavedDocument );
				EndTable();
			}
		}

		Demo.ShowWidgets();
		Demo.ShowLayout();
		Demo.ShowPopups();
		Demo.ShowTables();
		Demo.ShowInputs();

		PopItemWidth();
		End();
	}

	/// <summary>Basic help about controls.</summary>
	public static void ShowUserGuide()
	{
		BulletText( "Double-click on title bar to collapse window." );
		BulletText( "Click and drag on lower corner to resize window\n(double-click to auto fit window to its contents)." );
		BulletText( "Click and drag on any empty space to move window." );
		BulletText( "CTRL+Click on a slider or drag box to input value as text." );
		BulletText( "TAB/SHIFT+TAB to cycle through keyboard editable fields." );
		BulletText( "While inputting text:\n" );
		Indent();
		BulletText( "CTRL+A or double-click to select all." );
		BulletText( "CTRL+X/C/V to use clipboard cut/copy/paste." );
		BulletText( "CTRL+Z,CTRL+Y to undo/redo." );
		BulletText( "ESCAPE to revert." );
		Unindent();
		BulletText( "Mouse wheel scrolls windows; SHIFT+wheel scrolls horizontally." );
	}

	private static partial class Demo
	{
		public static void HelpMarker( string desc )
		{
			TextDisabled( "(?)" );
			if ( BeginItemTooltip() )
			{
				PushTextWrapPos( GetFontSize() * 35.0f );
				TextUnformatted( desc );
				PopTextWrapPos();
				EndTooltip();
			}
		}

		private static bool _menuEnabled = true;
		private static float _menuF = 0.5f;
		private static int _menuN;
		private static bool _menuB = true;

		public static void ShowExampleMenuFile()
		{
			MenuItem( "(demo menu)", null, false, false );
			if ( MenuItem( "New" ) ) { }
			if ( MenuItem( "Open", "Ctrl+O" ) ) { }
			if ( BeginMenu( "Open Recent" ) )
			{
				MenuItem( "fish_hat.c" );
				MenuItem( "fish_hat.inl" );
				MenuItem( "fish_hat.h" );
				if ( BeginMenu( "More.." ) )
				{
					MenuItem( "Hello" );
					MenuItem( "Sailor" );
					if ( BeginMenu( "Recurse.." ) )
					{
						ShowExampleMenuFile();
						EndMenu();
					}
					EndMenu();
				}
				EndMenu();
			}
			if ( MenuItem( "Save", "Ctrl+S" ) ) { }
			if ( MenuItem( "Save As.." ) ) { }

			Separator();
			if ( BeginMenu( "Options" ) )
			{
				MenuItem( "Enabled", "", ref _menuEnabled );
				BeginChild( "child", new Vector2( 0, 60 ), ImGuiChildFlags.Borders );
				for ( int i = 0; i < 10; i++ )
					Text( "Scrolling Text {0}", i );
				EndChild();
				SliderFloat( "Value", ref _menuF, 0.0f, 1.0f );
				InputFloat( "Input", ref _menuF, 0.1f );
				Combo( "Combo", ref _menuN, "Yes\0No\0Maybe\0\0" );
				EndMenu();
			}

			if ( BeginMenu( "Colors" ) )
			{
				float sz = GetTextLineHeight();
				for ( int i = 0; i < (int)ImGuiCol.COUNT; i++ )
				{
					var name = GetStyleColorName( (ImGuiCol)i );
					var p = GetCursorScreenPos();
					GetWindowDrawList().AddRectFilled( p, new Vector2( p.x + sz, p.y + sz ), GetColorU32( (ImGuiCol)i ) );
					Dummy( new Vector2( sz, sz ) );
					SameLine();
					MenuItem( name );
				}
				EndMenu();
			}

			if ( BeginMenu( "Options##2" ) )
			{
				Checkbox( "SomeOption", ref _menuB );
				EndMenu();
			}
			if ( BeginMenu( "Disabled", false ) ) { }
			if ( MenuItem( "Checked", null, true ) ) { }
			Separator();
			if ( MenuItem( "Quit", "Alt+F4" ) ) { }
		}
	}
}
