namespace Duccsoft.ImGui;

public static partial class ImGui
{
	private static partial class Demo
	{
		#region Main menu bar
		public static void ShowExampleAppMainMenuBar()
		{
			if ( BeginMainMenuBar() )
			{
				if ( BeginMenu( "File" ) )
				{
					ShowExampleMenuFile();
					EndMenu();
				}
				if ( BeginMenu( "Edit" ) )
				{
					if ( MenuItem( "Undo", "CTRL+Z" ) ) { }
					if ( MenuItem( "Redo", "CTRL+Y", false, false ) ) { }
					Separator();
					if ( MenuItem( "Cut", "CTRL+X" ) ) { }
					if ( MenuItem( "Copy", "CTRL+C" ) ) { }
					if ( MenuItem( "Paste", "CTRL+V" ) ) { }
					EndMenu();
				}
				EndMainMenuBar();
			}
		}
		#endregion

		#region Console
		private static readonly List<string> _consoleItems = new() { "Welcome to Dear ImGui!" };
		private static string _consoleInput = "";
		private static bool _consoleAutoScroll = true;
		private static bool _consoleScrollToBottom;
		private static readonly List<string> _consoleHistory = new();
		private static int _consoleHistoryPos = -1;

		public static void ShowExampleAppConsole( ref bool open )
		{
			SetNextWindowSize( new Vector2( 520, 600 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( !Begin( "Example: Console", ref open ) )
			{
				End();
				return;
			}

			TextWrapped( "This example implements a console with basic coloring, completion (TAB key) and history (Up/Down keys). A more elaborate implementation may want to store entries along with extra data such as timestamp, emitter, etc." );
			TextWrapped( "Enter 'HELP' for help." );

			if ( SmallButton( "Add Debug Text" ) )
				_consoleItems.Add( $"{_consoleItems.Count} some text" );
			SameLine();
			if ( SmallButton( "Add Debug Error" ) )
				_consoleItems.Add( "[error] something went wrong" );
			SameLine();
			if ( SmallButton( "Clear" ) )
				_consoleItems.Clear();
			Separator();

			float footerHeightToReserve = GetStyle().ItemSpacing.y + GetFrameHeightWithSpacing();
			if ( BeginChild( "ScrollingRegion", new Vector2( 0, -footerHeightToReserve ), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar ) )
			{
				if ( BeginPopupContextWindow() )
				{
					if ( Selectable( "Clear" ) ) _consoleItems.Clear();
					EndPopup();
				}
				PushStyleVar( ImGuiStyleVar.ItemSpacing, new Vector2( 4, 1 ) );
				foreach ( var item in _consoleItems )
				{
					Vector4? color = null;
					if ( item.Contains( "[error]" ) ) color = new Vector4( 1.0f, 0.4f, 0.4f, 1.0f );
					else if ( item.StartsWith( "# " ) ) color = new Vector4( 1.0f, 0.8f, 0.6f, 1.0f );
					if ( color.HasValue ) PushStyleColor( ImGuiCol.Text, color.Value );
					TextUnformatted( item );
					if ( color.HasValue ) PopStyleColor();
				}
				if ( _consoleScrollToBottom || (_consoleAutoScroll && GetScrollY() >= GetScrollMaxY()) )
					SetScrollHereY( 1.0f );
				_consoleScrollToBottom = false;
				PopStyleVar();
			}
			EndChild();
			Separator();

			bool reclaimFocus = false;
			var inputFlags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.EscapeClearsAll | ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.CallbackHistory;
			if ( InputText( "Input", ref _consoleInput, inputFlags, ConsoleCallback ) )
			{
				var s = _consoleInput.Trim();
				if ( s.Length > 0 )
					ExecCommand( s );
				_consoleInput = "";
				reclaimFocus = true;
			}
			SetItemDefaultFocus();
			if ( reclaimFocus )
				SetKeyboardFocusHere( -1 );

			Checkbox( "Auto-scroll", ref _consoleAutoScroll );
			End();
		}

		private static void ExecCommand( string cmd )
		{
			_consoleItems.Add( $"# {cmd}" );
			_consoleHistoryPos = -1;
			_consoleHistory.Remove( cmd );
			_consoleHistory.Add( cmd );

			switch ( cmd.ToUpperInvariant() )
			{
				case "CLEAR":
					_consoleItems.Clear();
					break;
				case "HELP":
					_consoleItems.Add( "Commands:" );
					_consoleItems.Add( "- CLEAR" );
					_consoleItems.Add( "- HELP" );
					_consoleItems.Add( "- HISTORY" );
					break;
				case "HISTORY":
					int first = Math.Max( 0, _consoleHistory.Count - 10 );
					for ( int i = first; i < _consoleHistory.Count; i++ )
						_consoleItems.Add( $"{i,3}: {_consoleHistory[i]}" );
					break;
				default:
					_consoleItems.Add( $"Unknown command: '{cmd}'" );
					break;
			}
			_consoleScrollToBottom = true;
		}

		private static int ConsoleCallback( ImGuiInputTextCallbackData data )
		{
			if ( data.EventFlag == ImGuiInputTextFlags.CallbackCompletion )
			{
				string[] commands = { "HELP", "HISTORY", "CLEAR" };
				var word = data.Buf ?? "";
				var candidates = commands.Where( c => c.StartsWith( word, StringComparison.OrdinalIgnoreCase ) ).ToList();
				if ( candidates.Count == 1 )
				{
					data.DeleteChars( 0, data.BufTextLen );
					data.InsertChars( 0, candidates[0] + " " );
				}
				else if ( candidates.Count > 1 )
				{
					_consoleItems.Add( "Possible matches:" );
					foreach ( var c in candidates )
						_consoleItems.Add( "- " + c );
				}
			}
			else if ( data.EventFlag == ImGuiInputTextFlags.CallbackHistory )
			{
				int prevHistoryPos = _consoleHistoryPos;
				if ( data.EventKey == ImGuiKey.UpArrow )
				{
					if ( _consoleHistoryPos == -1 )
						_consoleHistoryPos = _consoleHistory.Count - 1;
					else if ( _consoleHistoryPos > 0 )
						_consoleHistoryPos--;
				}
				else if ( data.EventKey == ImGuiKey.DownArrow )
				{
					if ( _consoleHistoryPos != -1 )
						if ( ++_consoleHistoryPos >= _consoleHistory.Count )
							_consoleHistoryPos = -1;
				}
				if ( prevHistoryPos != _consoleHistoryPos )
				{
					var historyStr = _consoleHistoryPos >= 0 ? _consoleHistory[_consoleHistoryPos] : "";
					data.DeleteChars( 0, data.BufTextLen );
					data.InsertChars( 0, historyStr );
				}
			}
			return 0;
		}
		#endregion

		#region Log
		private static readonly List<string> _logLines = new();
		private static string _logFilter = "";
		private static bool _logAutoScroll = true;
		private static double _lastLogTime;
		private static int _logCounter;

		public static void ShowExampleAppLog( ref bool open )
		{
			SetNextWindowSize( new Vector2( 500, 400 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( !Begin( "Example: Log", ref open ) )
			{
				End();
				return;
			}

			if ( SmallButton( "[Debug] Add 5 entries" ) )
			{
				string[] categories = { "info", "warn", "error" };
				string[] words = { "Bumfuzzled", "Cattywampus", "Snickersnee", "Abibliophobia", "Absquatulate", "Nincompoop", "Pauciloquent" };
				for ( int n = 0; n < 5; n++ )
				{
					_logLines.Add( $"[{GetFrameCount():D5}] [{categories[_logCounter % categories.Length]}] Hello, current time is {GetTime():F1}, here's a word: '{words[_logCounter % words.Length]}'" );
					_logCounter++;
				}
			}
			if ( GetTime() - _lastLogTime > 1.0 )
			{
				_lastLogTime = GetTime();
				_logLines.Add( $"[{GetFrameCount():D5}] [info] tick at {GetTime():F1}" );
			}

			if ( BeginPopup( "Options" ) )
			{
				Checkbox( "Auto-scroll", ref _logAutoScroll );
				EndPopup();
			}
			if ( Button( "Options" ) )
				OpenPopup( "Options" );
			SameLine();
			bool clear = Button( "Clear" );
			SameLine();
			bool copy = Button( "Copy" );
			SameLine();
			SetNextItemWidth( -100 );
			InputText( "Filter", ref _logFilter );
			Separator();

			if ( clear ) _logLines.Clear();

			if ( BeginChild( "scrolling", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar ) )
			{
				PushStyleVar( ImGuiStyleVar.ItemSpacing, new Vector2( 0, 0 ) );
				var lines = string.IsNullOrEmpty( _logFilter ) ? _logLines : _logLines.Where( l => l.Contains( _logFilter, StringComparison.OrdinalIgnoreCase ) ).ToList();
				if ( copy )
					Sandbox.UI.Clipboard.SetText( string.Join( "\n", lines ) );
				foreach ( var line in lines )
					TextUnformatted( line );
				PopStyleVar();
				if ( _logAutoScroll && GetScrollY() >= GetScrollMaxY() )
					SetScrollHereY( 1.0f );
			}
			EndChild();
			End();
		}
		#endregion

		#region Layout
		private static int _layoutSelected;

		public static void ShowExampleAppLayout( ref bool open )
		{
			SetNextWindowSize( new Vector2( 500, 440 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( Begin( "Example: Simple layout", ref open, ImGuiWindowFlags.MenuBar ) )
			{
				if ( BeginMenuBar() )
				{
					if ( BeginMenu( "File" ) )
					{
						if ( MenuItem( "Close", "Ctrl+W" ) ) open = false;
						EndMenu();
					}
					EndMenuBar();
				}

				BeginChild( "left pane", new Vector2( 150, 0 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX );
				for ( int i = 0; i < 100; i++ )
					if ( Selectable( $"MyObject {i}", _layoutSelected == i ) )
						_layoutSelected = i;
				EndChild();
				SameLine();

				BeginGroup();
				BeginChild( "item view", new Vector2( 0, -GetFrameHeightWithSpacing() ) );
				Text( "MyObject: {0}", _layoutSelected );
				Separator();
				if ( BeginTabBar( "##Tabs" ) )
				{
					if ( BeginTabItem( "Description" ) )
					{
						TextWrapped( "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " );
						EndTabItem();
					}
					if ( BeginTabItem( "Details" ) )
					{
						Text( "ID: 0123456789" );
						EndTabItem();
					}
					EndTabBar();
				}
				EndChild();
				if ( Button( "Revert" ) ) { }
				SameLine();
				if ( Button( "Save" ) ) { }
				EndGroup();
			}
			End();
		}
		#endregion

		#region Property editor
		private static readonly float[] _propValues = { 0.0f, 0.5f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f };

		public static void ShowExampleAppPropertyEditor( ref bool open )
		{
			SetNextWindowSize( new Vector2( 430, 450 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( !Begin( "Example: Property editor", ref open ) )
			{
				End();
				return;
			}

			HelpMarker( "This example shows how you may implement a property editor using two columns.\nAll objects/fields data are dummies here." );
			PushStyleVar( ImGuiStyleVar.FramePadding, new Vector2( 2, 2 ) );
			if ( BeginTable( "##split", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY ) )
			{
				TableSetupScrollFreeze( 0, 1 );
				TableSetupColumn( "Object" );
				TableSetupColumn( "Contents" );
				TableHeadersRow();

				for ( int objI = 0; objI < 4; objI++ )
				{
					PushID( objI );
					TableNextRow();
					TableSetColumnIndex( 0 );
					AlignTextToFramePadding();
					bool nodeOpen = TreeNode( "Object", "Object_{0}", objI );
					TableSetColumnIndex( 1 );
					Text( "my sailor is rich" );

					if ( nodeOpen )
					{
						for ( int i = 0; i < 8; i++ )
						{
							PushID( i );
							TableNextRow();
							TableSetColumnIndex( 0 );
							AlignTextToFramePadding();
							TreeNodeEx( "Field", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Bullet, "Field_{0}", i );
							TableSetColumnIndex( 1 );
							SetNextItemWidth( -1.17549435E-38f );
							if ( i >= 5 )
								InputFloat( "##value", ref _propValues[i], 1.0f );
							else
								DragFloat( "##value", ref _propValues[i], 0.01f );
							PopID();
						}
						TreePop();
					}
					PopID();
				}
				EndTable();
			}
			PopStyleVar();
			End();
		}
		#endregion

		#region Long text
		private static int _longTextLines;

		public static void ShowExampleAppLongText( ref bool open )
		{
			SetNextWindowSize( new Vector2( 520, 600 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( !Begin( "Example: Long text display", ref open ) )
			{
				End();
				return;
			}
			if ( Button( "Add 1000 lines" ) )
				_longTextLines += 1000;
			SameLine();
			if ( Button( "Clear" ) )
				_longTextLines = 0;
			BeginChild( "Log" );
			// Manual clipping: only submit visible lines.
			float lineHeight = GetTextLineHeightWithSpacing();
			int first = Math.Max( 0, (int)(GetScrollY() / lineHeight) );
			int visible = (int)(GetWindowHeight() / lineHeight) + 2;
			SetCursorPosY( first * lineHeight );
			for ( int i = first; i < Math.Min( _longTextLines, first + visible ); i++ )
				Text( "{0} The quick brown fox jumps over the lazy dog", i );
			SetCursorPosY( _longTextLines * lineHeight );
			Dummy( Vector2.Zero );
			EndChild();
			End();
		}
		#endregion

		#region Auto resize
		private static int _autoResizeLines = 10;

		public static void ShowExampleAppAutoResize( ref bool open )
		{
			if ( !Begin( "Example: Auto-resizing window", ref open, ImGuiWindowFlags.AlwaysAutoResize ) )
			{
				End();
				return;
			}
			Text( "Window will resize every-frame to the size of its content.\nNote that you probably don't want to query the window size to\noutput your content because that would create a feedback loop." );
			SliderInt( "Number of lines", ref _autoResizeLines, 1, 20 );
			for ( int i = 0; i < _autoResizeLines; i++ )
				Text( "{0}This is line {1}", new string( ' ', i * 4 ), i );
			End();
		}
		#endregion

		#region Constrained resize
		private static int _constraintType;

		public static void ShowExampleAppConstrainedResize( ref bool open )
		{
			if ( _constraintType == 0 ) SetNextWindowSizeConstraints( new Vector2( 400, 0 ), new Vector2( 1000, float.MaxValue ) );
			if ( _constraintType == 1 ) SetNextWindowSizeConstraints( new Vector2( 0, 200 ), new Vector2( float.MaxValue, 600 ) );
			if ( _constraintType == 2 ) SetNextWindowSizeConstraints( new Vector2( 400, 300 ), new Vector2( 800, 600 ) );
			if ( _constraintType == 3 ) SetNextWindowSizeConstraints( Vector2.Zero, new Vector2( float.MaxValue, float.MaxValue ), d => d.DesiredSize = new Vector2( MathF.Max( d.DesiredSize.x, d.DesiredSize.y ), MathF.Max( d.DesiredSize.x, d.DesiredSize.y ) ) );
			if ( _constraintType == 4 ) SetNextWindowSizeConstraints( Vector2.Zero, new Vector2( float.MaxValue, float.MaxValue ), d => d.DesiredSize = new Vector2( MathF.Round( d.DesiredSize.x / 100 ) * 100, MathF.Round( d.DesiredSize.y / 100 ) * 100 ) );

			if ( Begin( "Example: Constrained Resize", ref open ) )
			{
				string[] testDesc = { "Between 400 and 1000 width", "Height between 200 and 600", "Size between 400x300 and 800x600", "Custom: Always Square", "Custom: Fixed Steps (100)" };
				SetNextItemWidth( GetFontSize() * 20 );
				Combo( "Constraint", ref _constraintType, testDesc );
				Text( "Window size: {0:F0} x {1:F0}", GetWindowWidth(), GetWindowHeight() );
			}
			End();
		}
		#endregion

		#region Simple overlay
		private static int _overlayLocation;

		public static void ShowExampleAppSimpleOverlay( ref bool open )
		{
			var io = GetIO();
			var windowFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
			if ( _overlayLocation >= 0 )
			{
				const float pad = 10.0f;
				var workSize = io.DisplaySize;
				var windowPos = new Vector2( (_overlayLocation & 1) != 0 ? workSize.x - pad : pad, (_overlayLocation & 2) != 0 ? workSize.y - pad : pad );
				var windowPosPivot = new Vector2( (_overlayLocation & 1) != 0 ? 1.0f : 0.0f, (_overlayLocation & 2) != 0 ? 1.0f : 0.0f );
				SetNextWindowPos( windowPos, ImGuiCond.Always, windowPosPivot );
				windowFlags |= ImGuiWindowFlags.NoMove;
			}
			SetNextWindowBgAlpha( 0.35f );
			if ( Begin( "Example: Simple overlay", ref open, windowFlags ) )
			{
				Text( "Simple overlay\n(right-click to change position)" );
				Separator();
				if ( IsMousePosValid() )
					Text( "Mouse Position: ({0:F1},{1:F1})", io.MousePos.x, io.MousePos.y );
				else
					Text( "Mouse Position: <invalid>" );
				Text( "FPS: {0:F0}", io.Framerate );
				if ( BeginPopupContextWindow() )
				{
					if ( MenuItem( "Custom", null, _overlayLocation == -1 ) ) _overlayLocation = -1;
					if ( MenuItem( "Top-left", null, _overlayLocation == 0 ) ) _overlayLocation = 0;
					if ( MenuItem( "Top-right", null, _overlayLocation == 1 ) ) _overlayLocation = 1;
					if ( MenuItem( "Bottom-left", null, _overlayLocation == 2 ) ) _overlayLocation = 2;
					if ( MenuItem( "Bottom-right", null, _overlayLocation == 3 ) ) _overlayLocation = 3;
					if ( MenuItem( "Close" ) ) open = false;
					EndPopup();
				}
			}
			End();
		}
		#endregion

		#region Fullscreen
		public static void ShowExampleAppFullscreen( ref bool open )
		{
			var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;
			SetNextWindowPos( Vector2.Zero );
			SetNextWindowSize( GetIO().DisplaySize );
			if ( Begin( "Example: Fullscreen window", ref open, flags ) )
			{
				Text( "This window covers the whole screen." );
				if ( Button( "Close this window" ) )
					open = false;
			}
			End();
		}
		#endregion

		#region Window titles
		public static void ShowExampleAppWindowTitles()
		{
			var basePos = new Vector2( 100, 100 ) * G.AppliedStyleScale;
			SetNextWindowPos( basePos, ImGuiCond.FirstUseEver );
			Begin( "Same title as another window##1" );
			Text( "This is window 1.\nMy title is the same as window 2, but my identifier is unique." );
			End();

			SetNextWindowPos( basePos + new Vector2( 0, 100 ), ImGuiCond.FirstUseEver );
			Begin( "Same title as another window##2" );
			Text( "This is window 2.\nMy title is the same as window 1, but my identifier is unique." );
			End();

			SetNextWindowPos( basePos + new Vector2( 0, 200 ), ImGuiCond.FirstUseEver );
			Begin( $"Animated title {"|/-\\"[(int)(GetTime() / 0.25f) & 3]} {GetFrameCount()}###AnimatedTitle" );
			Text( "This window has a changing title." );
			End();
		}
		#endregion

		#region Custom rendering
		private static float _crSize = 36.0f;
		private static float _crThickness = 3.0f;
		private static int _crNgonSides = 6;
		private static bool _crCurveSegmentsOverride;
		private static Vector4 _crColf = new( 1.0f, 1.0f, 0.4f, 1.0f );
		private static readonly List<Vector2> _crPoints = new();
		private static Vector2 _crScrolling;
		private static bool _crAddingLine;

		public static void ShowExampleAppCustomRendering( ref bool open )
		{
			if ( !Begin( "Example: Custom rendering", ref open ) )
			{
				End();
				return;
			}

			if ( BeginTabBar( "##TabBar" ) )
			{
				if ( BeginTabItem( "Primitives" ) )
				{
					PushItemWidth( -GetFontSize() * 15 );
					var drawList = GetWindowDrawList();
					SeparatorText( "Gradients" );
					var gradientSize = new Vector2( CalcItemWidth(), GetFrameHeight() );
					{
						var p0 = GetCursorScreenPos();
						var p1 = new Vector2( p0.x + gradientSize.x, p0.y + gradientSize.y );
						var colA = new Color32( 0, 0, 0, 255 );
						var colB = new Color32( 255, 255, 255, 255 );
						drawList.AddRectFilledMultiColor( p0, p1, colA, colB, colB, colA );
						InvisibleButton( "##gradient1", gradientSize );
					}
					{
						var p0 = GetCursorScreenPos();
						var p1 = new Vector2( p0.x + gradientSize.x, p0.y + gradientSize.y );
						var colA = new Color32( 0, 255, 0, 255 );
						var colB = new Color32( 255, 0, 0, 255 );
						drawList.AddRectFilledMultiColor( p0, p1, colA, colB, colB, colA );
						InvisibleButton( "##gradient2", gradientSize );
					}

					SeparatorText( "All primitives" );
					DragFloat( "Size", ref _crSize, 0.2f, 2.0f, 100.0f, "%.0f" );
					DragFloat( "Thickness", ref _crThickness, 0.05f, 1.0f, 8.0f, "%.02f" );
					SliderInt( "N-gon sides", ref _crNgonSides, 3, 12 );
					Checkbox( "##curvessegmentoverride", ref _crCurveSegmentsOverride );
					ColorEdit4( "Color", ref _crColf );

					var p = GetCursorScreenPos();
					var col = ColorConvertFloat4ToU32( _crColf );
					float spacing = 10.0f;
					float rounding = _crSize / 5.0f;
					float x = p.x + 4.0f;
					float y = p.y + 4.0f;
					float sz = _crSize;
					for ( int n = 0; n < 2; n++ )
					{
						float th = n == 0 ? 1.0f : _crThickness;
						drawList.AddNgon( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), sz * 0.5f, col, _crNgonSides, th ); x += sz + spacing;
						drawList.AddCircle( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), sz * 0.5f, col, 0, th ); x += sz + spacing;
						drawList.AddEllipse( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), new Vector2( sz * 0.5f, sz * 0.3f ), col, -0.3f, 32, th ); x += sz + spacing;
						drawList.AddRect( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, 0.0f, ImDrawFlags.None, th ); x += sz + spacing;
						drawList.AddRect( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, rounding, ImDrawFlags.None, th ); x += sz + spacing;
						drawList.AddRect( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, rounding, ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersBottomRight, th ); x += sz + spacing;
						drawList.AddTriangle( new Vector2( x + sz * 0.5f, y ), new Vector2( x + sz, y + sz - 0.5f ), new Vector2( x, y + sz - 0.5f ), col, th ); x += sz + spacing;
						drawList.AddLine( new Vector2( x, y ), new Vector2( x + sz, y ), col, th ); x += sz + spacing;
						drawList.AddLine( new Vector2( x, y ), new Vector2( x, y + sz ), col, th ); x += spacing;
						drawList.AddLine( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, th ); x += sz + spacing;

						var cp4 = new[] { new Vector2( x, y ), new Vector2( x + sz * 1.3f, y + sz * 0.3f ), new Vector2( x + sz - sz * 1.3f, y + sz - sz * 0.3f ), new Vector2( x + sz, y + sz ) };
						drawList.AddBezierCubic( cp4[0], cp4[1], cp4[2], cp4[3], col, th ); x += sz + spacing;
						x = p.x + 4;
						y += sz + spacing;
					}

					drawList.AddNgonFilled( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), sz * 0.5f, col, _crNgonSides ); x += sz + spacing;
					drawList.AddCircleFilled( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), sz * 0.5f, col ); x += sz + spacing;
					drawList.AddEllipseFilled( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), new Vector2( sz * 0.5f, sz * 0.3f ), col, -0.3f, 32 ); x += sz + spacing;
					drawList.AddRectFilled( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col ); x += sz + spacing;
					drawList.AddRectFilled( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, 10.0f ); x += sz + spacing;
					drawList.AddRectFilled( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, 10.0f, ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersBottomRight ); x += sz + spacing;
					drawList.AddTriangleFilled( new Vector2( x + sz * 0.5f, y ), new Vector2( x + sz, y + sz - 0.5f ), new Vector2( x, y + sz - 0.5f ), col ); x += sz + spacing;
					drawList.AddRectFilledMultiColor( new Vector2( x, y ), new Vector2( x + sz, y + sz ), new Color32( 0, 0, 0, 255 ), new Color32( 255, 0, 0, 255 ), new Color32( 255, 255, 0, 255 ), new Color32( 0, 255, 0, 255 ) );

					Dummy( new Vector2( (sz + spacing) * 10.2f, (sz + spacing) * 3.0f ) );
					PopItemWidth();
					EndTabItem();
				}

				if ( BeginTabItem( "Canvas" ) )
				{
					Checkbox( "Enable context menu", ref _crCurveSegmentsOverride );
					Text( "Mouse Left: drag to add lines,\nMouse Right: drag to scroll, click for context menu." );

					var canvasP0 = GetCursorScreenPos();
					var canvasSz = GetContentRegionAvail();
					if ( canvasSz.x < 50.0f ) canvasSz.x = 50.0f;
					if ( canvasSz.y < 50.0f ) canvasSz.y = 50.0f;
					var canvasP1 = canvasP0 + canvasSz;

					var io = GetIO();
					var drawList = GetWindowDrawList();
					drawList.AddRectFilled( canvasP0, canvasP1, new Color32( 50, 50, 50, 255 ) );
					drawList.AddRect( canvasP0, canvasP1, new Color32( 255, 255, 255, 255 ) );

					InvisibleButton( "canvas", canvasSz, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight );
					bool isHovered = IsItemHovered();
					bool isActive = IsItemActive();
					var origin = canvasP0 + _crScrolling;
					var mousePosInCanvas = io.MousePos - origin;

					if ( isHovered && !_crAddingLine && IsMouseClicked( ImGuiMouseButton.Left ) )
					{
						_crPoints.Add( mousePosInCanvas );
						_crPoints.Add( mousePosInCanvas );
						_crAddingLine = true;
					}
					if ( _crAddingLine )
					{
						_crPoints[^1] = mousePosInCanvas;
						if ( !IsMouseDown( ImGuiMouseButton.Left ) )
							_crAddingLine = false;
					}
					if ( isActive && IsMouseDragging( ImGuiMouseButton.Right, 0.0f ) )
						_crScrolling += io.MouseDelta;

					var dragDelta = GetMouseDragDelta( ImGuiMouseButton.Right );
					if ( dragDelta.x == 0.0f && dragDelta.y == 0.0f )
						OpenPopupOnItemClick( "context", ImGuiPopupFlags.MouseButtonRight );
					if ( BeginPopup( "context" ) )
					{
						if ( _crAddingLine )
							_crPoints.RemoveRange( _crPoints.Count - 2, 2 );
						_crAddingLine = false;
						if ( MenuItem( "Remove one", null, false, _crPoints.Count > 0 ) ) _crPoints.RemoveRange( _crPoints.Count - 2, 2 );
						if ( MenuItem( "Remove all", null, false, _crPoints.Count > 0 ) ) _crPoints.Clear();
						EndPopup();
					}

					drawList.PushClipRect( canvasP0, canvasP1, true );
					const float gridStep = 64.0f;
					for ( float gx = _crScrolling.x % gridStep; gx < canvasSz.x; gx += gridStep )
						drawList.AddLine( new Vector2( canvasP0.x + gx, canvasP0.y ), new Vector2( canvasP0.x + gx, canvasP1.y ), new Color32( 200, 200, 200, 40 ) );
					for ( float gy = _crScrolling.y % gridStep; gy < canvasSz.y; gy += gridStep )
						drawList.AddLine( new Vector2( canvasP0.x, canvasP0.y + gy ), new Vector2( canvasP1.x, canvasP0.y + gy ), new Color32( 200, 200, 200, 40 ) );
					for ( int n = 0; n + 1 < _crPoints.Count; n += 2 )
						drawList.AddLine( origin + _crPoints[n], origin + _crPoints[n + 1], new Color32( 255, 255, 0, 255 ), 2.0f );
					drawList.PopClipRect();
					EndTabItem();
				}

				if ( BeginTabItem( "BG/FG draw lists" ) )
				{
					var windowCenter = GetWindowPos() + GetWindowSize() * 0.5f;
					GetBackgroundDrawList().AddCircle( windowCenter, GetWindowSize().x * 0.6f, new Color32( 255, 0, 0, 200 ), 0, 10 + 4 );
					GetForegroundDrawList().AddCircle( windowCenter, GetWindowSize().y * 0.6f, new Color32( 0, 255, 0, 200 ), 0, 10 );
					Text( "The background draw list renders behind all windows, the foreground draw list on top." );
					EndTabItem();
				}
				EndTabBar();
			}
			End();
		}
		#endregion

		#region Documents
		private class MyDocument
		{
			public string Name;
			public bool Open = true;
			public bool OpenPrev = true;
			public bool Dirty;
			public Vector4 Color = Vector4.One;
		}

		private static readonly List<MyDocument> _documents = new()
		{
			new MyDocument { Name = "Lettuce", Color = new Vector4( 0.4f, 0.8f, 0.4f, 1.0f ) },
			new MyDocument { Name = "Eggplant", Color = new Vector4( 0.8f, 0.5f, 1.0f, 1.0f ) },
			new MyDocument { Name = "Carrot", Color = new Vector4( 1.0f, 0.8f, 0.5f, 1.0f ) },
			new MyDocument { Name = "Tomato", Color = new Vector4( 1.0f, 0.3f, 0.4f, 1.0f ) },
			new MyDocument { Name = "A Rather Long Title", Color = new Vector4( 0.4f, 0.8f, 0.8f, 1.0f ) },
		};

		public static void ShowExampleAppDocuments( ref bool open )
		{
			if ( !Begin( "Example: Documents", ref open, ImGuiWindowFlags.MenuBar ) )
			{
				End();
				return;
			}

			if ( BeginMenuBar() )
			{
				if ( BeginMenu( "File" ) )
				{
					int openCount = _documents.Count( d => d.Open );
					if ( BeginMenu( "Open", openCount < _documents.Count ) )
					{
						foreach ( var doc in _documents )
							if ( !doc.Open && MenuItem( doc.Name ) )
								doc.Open = true;
						EndMenu();
					}
					if ( MenuItem( "Close All Documents", null, false, openCount > 0 ) )
						foreach ( var doc in _documents ) doc.Open = false;
					if ( MenuItem( "Exit" ) )
						open = false;
					EndMenu();
				}
				EndMenuBar();
			}

			foreach ( var doc in _documents )
			{
				if ( doc != _documents[0] ) SameLine();
				PushID( doc.Name );
				Checkbox( doc.Name, ref doc.Open );
				PopID();
			}
			Separator();

			if ( BeginTabBar( "##tabs", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.FittingPolicyResizeDown | ImGuiTabBarFlags.TabListPopupButton ) )
			{
				foreach ( var doc in _documents )
				{
					if ( !doc.Open )
						continue;
					var tabFlags = doc.Dirty ? ImGuiTabItemFlags.UnsavedDocument : ImGuiTabItemFlags.None;
					bool visible = BeginTabItem( doc.Name, ref doc.Open, tabFlags );
					if ( visible )
					{
						PushID( doc.Name );
						PushStyleColor( ImGuiCol.Text, doc.Color );
						TextWrapped( "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua." );
						PopStyleColor();
						if ( Button( "Modify" ) ) doc.Dirty = true;
						SameLine();
						if ( Button( "Save" ) ) doc.Dirty = false;
						ColorEdit3( "color", ref doc.Color );
						PopID();
						EndTabItem();
					}
				}
				EndTabBar();
			}
			End();
		}
		#endregion
	}
}
