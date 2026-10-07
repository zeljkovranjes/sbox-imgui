namespace Duccsoft.ImGui;

public static partial class ImGui
{
	private static partial class Demo
	{
		// Layout
		private static bool _disableMouseWheel, _disableMenu;
		private static int _offsetX;
		private static float _itemWidthF = 0.5f;
		private static int _widthMode;
		private static bool _scrollToOff, _scrollToPos;
		private static float _scrollToOffPx = 0.0f, _scrollToPosPx = 200.0f;
		private static int _trackItem = 50;
		private static bool _enableTrack = true;
		private static Vector2 _clipSize = new( 100.0f, 100.0f );
		private static Vector2 _clipOffset = new( 30.0f, 30.0f );
		private static float _groupValues0 = 0.5f;

		// Popups
		private static int _selectedFish = -1;
		private static readonly bool[] _toggles = { true, false, false, false, false };
		private static string _contextName = "Label1";
		private static float _ctxValue = 0.5f;
		private static bool _dontAskMeNextTime;
		private static int _modalItem = 1;
		private static Vector4 _modalColor = new( 0.4f, 0.7f, 0.0f, 0.5f );

		// Tables
		private static ImGuiTableFlags _tableFlags1 = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
		private static ImGuiTableFlags _tableFlagsResize = ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.ContextMenuInBody;
		private static ImGuiTableFlags _tableFlagsScroll = ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable;
		private static ImGuiTableFlags _tableFlagsSort = ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable | ImGuiTableFlags.SortMulti | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.ScrollY;
		private static List<(int Id, string Name, int Quantity)> _sortItems;
		private static int _columnsCount = 3;
		private static bool _columnsBorders = true;

		public static void ShowLayout()
		{
			if ( !CollapsingHeader( "Layout & Scrolling" ) )
				return;

			if ( TreeNode( "Child windows" ) )
			{
				SeparatorText( "Child windows" );
				HelpMarker( "Use child windows to begin into a self-contained independent scrolling/clipping regions within a host window." );
				Checkbox( "Disable Mouse Wheel", ref _disableMouseWheel );
				Checkbox( "Disable Menu", ref _disableMenu );

				{
					var windowFlags = ImGuiWindowFlags.HorizontalScrollbar;
					if ( _disableMouseWheel )
						windowFlags |= ImGuiWindowFlags.NoScrollWithMouse;
					BeginChild( "ChildL", new Vector2( GetContentRegionAvail().x * 0.5f, 260 ), ImGuiChildFlags.None, windowFlags );
					for ( int i = 0; i < 100; i++ )
						Text( "{0:D4}: scrollable region", i );
					EndChild();
				}

				SameLine();

				{
					var windowFlags = ImGuiWindowFlags.None;
					if ( _disableMouseWheel )
						windowFlags |= ImGuiWindowFlags.NoScrollWithMouse;
					if ( !_disableMenu )
						windowFlags |= ImGuiWindowFlags.MenuBar;
					PushStyleVar( ImGuiStyleVar.ChildRounding, 5.0f );
					BeginChild( "ChildR", new Vector2( 0, 260 ), ImGuiChildFlags.Borders, windowFlags );
					if ( !_disableMenu && BeginMenuBar() )
					{
						if ( BeginMenu( "Menu" ) )
						{
							ShowExampleMenuFile();
							EndMenu();
						}
						EndMenuBar();
					}
					if ( BeginTable( "split", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings ) )
					{
						for ( int i = 0; i < 100; i++ )
						{
							TableNextColumn();
							Button( $"{i:D3}", new Vector2( -1.17549435E-38f, 0.0f ) );
						}
						EndTable();
					}
					EndChild();
					PopStyleVar();
				}

				SeparatorText( "Manual-resize" );
				HelpMarker( "Drag bottom border to resize. Double-click bottom border to auto-fit to vertical contents." );
				PushStyleColor( ImGuiCol.ChildBg, GetStyleColorVec4( ImGuiCol.FrameBg ) );
				if ( BeginChild( "ResizableChild", new Vector2( -1.17549435E-38f, GetTextLineHeightWithSpacing() * 8 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY ) )
					for ( int n = 0; n < 10; n++ )
						Text( "Line {0:D4}", n );
				PopStyleColor();
				EndChild();

				SeparatorText( "Auto-resize with constraints" );
				SetNextWindowSizeConstraints( new Vector2( 0.0f, GetTextLineHeightWithSpacing() * 1 ), new Vector2( float.MaxValue, GetTextLineHeightWithSpacing() * 6 ) );
				if ( BeginChild( "ConstrainedChild", new Vector2( -1.17549435E-38f, 0.0f ), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY ) )
					for ( int n = 0; n < 4; n++ )
						Text( "Line {0:D4}", n );
				EndChild();

				SeparatorText( "Misc/Advanced" );
				SetNextItemWidth( GetFontSize() * 8 );
				DragInt( "Offset X", ref _offsetX, 1.0f, -1000, 1000 );
				SetCursorPosX( GetCursorPosX() + _offsetX );
				PushStyleColor( ImGuiCol.ChildBg, new Color32( 255, 0, 0, 100 ) );
				BeginChild( "Red", new Vector2( 200, 100 ), ImGuiChildFlags.Borders, ImGuiWindowFlags.None );
				for ( int n = 0; n < 50; n++ )
					Text( "Some test {0}", n );
				EndChild();
				bool childIsHovered = IsItemHovered();
				var childRectMin = GetItemRectMin();
				var childRectMax = GetItemRectMax();
				PopStyleColor();
				Text( "Hovered: {0}", childIsHovered );
				Text( "Rect of child window is: ({0:F0},{1:F0}) ({2:F0},{3:F0})", childRectMin.x, childRectMin.y, childRectMax.x, childRectMax.y );

				TreePop();
			}

			if ( TreeNode( "Widgets Width" ) )
			{
				Text( "SetNextItemWidth/PushItemWidth(100)" );
				SameLine(); HelpMarker( "Fixed width." );
				PushItemWidth( 100 );
				DragFloat( "float##1b", ref _itemWidthF );
				PopItemWidth();

				Text( "SetNextItemWidth/PushItemWidth(-100)" );
				SameLine(); HelpMarker( "Align to right edge minus 100" );
				PushItemWidth( -100 );
				DragFloat( "float##2a", ref _itemWidthF );
				PopItemWidth();

				Text( "SetNextItemWidth/PushItemWidth(GetContentRegionAvail().x * 0.5f)" );
				PushItemWidth( GetContentRegionAvail().x * 0.5f );
				DragFloat( "float##3a", ref _itemWidthF );
				PopItemWidth();

				Text( "SetNextItemWidth/PushItemWidth(-FLT_MIN)" );
				SameLine(); HelpMarker( "Align to right edge" );
				PushItemWidth( -1.17549435E-38f );
				DragFloat( "##float5a", ref _itemWidthF );
				PopItemWidth();
				TreePop();
			}

			if ( TreeNode( "Basic Horizontal Layout" ) )
			{
				TextWrapped( "(Use ImGui.SameLine() to keep adding items to the right of the preceding item)" );

				Text( "Two items: Hello" ); SameLine();
				TextColored( new Vector4( 1, 1, 0, 1 ), "Sailor" );

				Text( "More spacing: Hello" ); SameLine( 0, 20 );
				TextColored( new Vector4( 1, 1, 0, 1 ), "Sailor" );

				AlignTextToFramePadding();
				Text( "Normal buttons" ); SameLine();
				Button( "Banana" ); SameLine();
				Button( "Apple" ); SameLine();
				Button( "Corniflower" );

				Text( "Small buttons" ); SameLine();
				SmallButton( "Like this one" ); SameLine();
				Text( "can fit within a text block." );

				Text( "Aligned" );
				SameLine( 150 ); Text( "x=150" );
				SameLine( 300 ); Text( "x=300" );
				Text( "Aligned" );
				SameLine( 150 ); SmallButton( "x=150" );
				SameLine( 300 ); SmallButton( "x=300" );

				Text( "Manual wrapping:" );
				var style = GetStyle();
				int buttonsCount = 20;
				float windowVisibleX2 = GetCursorScreenPos().x + GetContentRegionAvail().x;
				for ( int n = 0; n < buttonsCount; n++ )
				{
					var buttonSz = new Vector2( 40, 40 );
					PushID( n );
					Button( "Box", buttonSz );
					float lastButtonX2 = GetItemRectMax().x;
					float nextButtonX2 = lastButtonX2 + style.ItemSpacing.x + buttonSz.x;
					if ( n + 1 < buttonsCount && nextButtonX2 < windowVisibleX2 )
						SameLine();
					PopID();
				}
				TreePop();
			}

			if ( TreeNode( "Groups" ) )
			{
				HelpMarker( "BeginGroup() basically locks the horizontal position for new line.\nEndGroup() bundles the whole group so that you can use \"item\" functions such as IsItemHovered()/IsItemActive() or SameLine() etc. on the whole group." );
				BeginGroup();
				{
					BeginGroup();
					Button( "AAA" );
					SameLine();
					Button( "BBB" );
					SameLine();
					BeginGroup();
					Button( "CCC" );
					Button( "DDD" );
					EndGroup();
					SameLine();
					Button( "EEE" );
					EndGroup();
					SetItemTooltip( "First group hovered" );
				}
				var size = GetItemRectSize();
				float[] values = { 0.5f, 0.20f, 0.80f, 0.60f, 0.25f };
				PlotHistogram( "##values", values, 0, null, 0.0f, 1.0f, size );

				Button( "ACTION", new Vector2( (size.x - GetStyle().ItemSpacing.x) * 0.5f, size.y ) );
				SameLine();
				Button( "REACTION", new Vector2( (size.x - GetStyle().ItemSpacing.x) * 0.5f, size.y ) );
				EndGroup();
				SameLine();

				Button( "LEVERAGE\nBUZZWORD", size );
				SameLine();

				if ( BeginListBox( "List", size ) )
				{
					Selectable( "Selected", true );
					Selectable( "Not Selected", false );
					EndListBox();
				}
				TreePop();
			}

			if ( TreeNode( "Text Baseline Alignment" ) )
			{
				BulletText( "Text annotation for SmallButton() and other aligned widgets" );
				Text( "One\nTwo\nThree" ); SameLine();
				Text( "Hello\nWorld" ); SameLine();
				Text( "Banana" );

				Button( "HOP##1" ); SameLine();
				Text( "Banana" ); SameLine();
				Text( "Hello\nWorld" ); SameLine();
				Text( "Banana" );

				AlignTextToFramePadding();
				Text( "Hello" ); SameLine();
				Button( "Button" ); SameLine();
				SliderFloat( "##slider", ref _groupValues0, 0, 1 );
				TreePop();
			}

			if ( TreeNode( "Scrolling" ) )
			{
				HelpMarker( "Use SetScrollHereY() or SetScrollFromPosY() to scroll to a given vertical position." );
				Checkbox( "Decoration", ref _enableTrack );
				Checkbox( "Track", ref _enableTrack );
				PushItemWidth( 100 );
				SameLine( 140 );
				if ( DragInt( "##item", ref _trackItem, 0.25f, 0, 99, "Item = %d" ) )
					_enableTrack = true;

				bool scrollToOff = Button( "Scroll Offset" );
				SameLine( 140 );
				scrollToOff |= DragFloat( "##off", ref _scrollToOffPx, 1.00f, 0, float.MaxValue, "+%.0f px" );

				bool scrollToPos = Button( "Scroll To Pos" );
				SameLine( 140 );
				scrollToPos |= DragFloat( "##pos", ref _scrollToPosPx, 1.00f, -10, float.MaxValue, "X/Y = %.0f px" );
				PopItemWidth();

				if ( scrollToOff || scrollToPos )
					_enableTrack = false;

				var style = GetStyle();
				float childW = (GetContentRegionAvail().x - 4 * style.ItemSpacing.x) / 5;
				if ( childW < 1.0f )
					childW = 1.0f;
				PushID( "##VerticalScrolling" );
				for ( int i = 0; i < 5; i++ )
				{
					if ( i > 0 ) SameLine();
					BeginGroup();
					string[] names = { "Top", "25%", "Center", "75%", "Bottom" };
					TextUnformatted( names[i] );

					bool childIsVisible = BeginChild( GetID( i ), new Vector2( childW, 200.0f ), ImGuiChildFlags.Borders, ImGuiWindowFlags.MenuBar );
					if ( BeginMenuBar() )
					{
						TextUnformatted( "abc" );
						EndMenuBar();
					}
					if ( scrollToOff )
						SetScrollY( _scrollToOffPx );
					if ( scrollToPos )
						SetScrollFromPosY( GetCursorStartPos().y + _scrollToPosPx, i * 0.25f );
					if ( childIsVisible )
					{
						for ( int item = 0; item < 100; item++ )
						{
							if ( _enableTrack && item == _trackItem )
							{
								TextColored( new Vector4( 1, 1, 0, 1 ), "Item {0}", item );
								SetScrollHereY( i * 0.25f );
							}
							else
							{
								Text( "Item {0}", item );
							}
						}
					}
					float scrollY = GetScrollY();
					float scrollMaxY = GetScrollMaxY();
					EndChild();
					Text( "{0:F0}/{1:F0}", scrollY, scrollMaxY );
					EndGroup();
				}
				PopID();
				TreePop();
			}

			if ( TreeNode( "Clipping" ) )
			{
				DragFloat2( "size", ref _clipSize, 0.5f, 1.0f, 200.0f, "%.0f" );
				TextWrapped( "(Click and drag to scroll)" );
				for ( int n = 0; n < 3; n++ )
				{
					if ( n > 0 ) SameLine();
					PushID( n );
					InvisibleButton( "##canvas", _clipSize );
					if ( IsItemActive() && IsMouseDragging( ImGuiMouseButton.Left ) )
						_clipOffset += GetIO().MouseDelta;
					PopID();
					if ( !IsItemVisible() )
						continue;
					var p0 = GetItemRectMin();
					var p1 = GetItemRectMax();
					string textStr = "Line 1 hello\nLine 2 clip me!";
					var textPos = p0 + _clipOffset;
					var drawList = GetWindowDrawList();
					switch ( n )
					{
						case 0:
							PushClipRect( p0, p1, true );
							drawList.AddRectFilled( p0, p1, new Color32( 90, 90, 120, 255 ) );
							drawList.AddText( textPos, new Color32( 255, 255, 255, 255 ), textStr );
							PopClipRect();
							break;
						case 1:
							drawList.PushClipRect( p0, p1, true );
							drawList.AddRectFilled( p0, p1, new Color32( 90, 90, 120, 255 ) );
							drawList.AddText( textPos, new Color32( 255, 255, 255, 255 ), textStr );
							drawList.PopClipRect();
							break;
						case 2:
							drawList.AddRectFilled( p0, p1, new Color32( 90, 90, 120, 255 ) );
							drawList.AddText( 0f, textPos, new Color32( 255, 255, 255, 255 ), textStr, 0.0f, new ImRect( p0, p1 ) );
							break;
					}
				}
				TreePop();
			}
		}

		public static void ShowPopups()
		{
			if ( !CollapsingHeader( "Popups & Modal windows" ) )
				return;

			if ( TreeNode( "Popups" ) )
			{
				TextWrapped( "When a popup is active, it inhibits interacting with windows that are behind the popup. Clicking outside the popup closes it." );
				string[] names = { "Bream", "Haddock", "Mackerel", "Pollock", "Tilefish" };

				if ( Button( "Select.." ) )
					OpenPopup( "my_select_popup" );
				SameLine();
				TextUnformatted( _selectedFish == -1 ? "<None>" : names[_selectedFish] );
				if ( BeginPopup( "my_select_popup" ) )
				{
					SeparatorText( "Aquarium" );
					for ( int i = 0; i < names.Length; i++ )
						if ( Selectable( names[i] ) )
							_selectedFish = i;
					EndPopup();
				}

				if ( Button( "Toggle.." ) )
					OpenPopup( "my_toggle_popup" );
				if ( BeginPopup( "my_toggle_popup" ) )
				{
					for ( int i = 0; i < names.Length; i++ )
						MenuItem( names[i], "", ref _toggles[i] );
					if ( BeginMenu( "Sub-menu" ) )
					{
						MenuItem( "Click me" );
						EndMenu();
					}
					Separator();
					Text( "Tooltip here" );
					SetItemTooltip( "I am a tooltip over a popup" );
					if ( Button( "Stacked Popup" ) )
						OpenPopup( "another popup" );
					if ( BeginPopup( "another popup" ) )
					{
						for ( int i = 0; i < names.Length; i++ )
							MenuItem( names[i], "", ref _toggles[i] );
						EndPopup();
					}
					EndPopup();
				}

				if ( Button( "With a menu.." ) )
					OpenPopup( "my_file_popup" );
				if ( BeginPopup( "my_file_popup", ImGuiWindowFlags.MenuBar ) )
				{
					if ( BeginMenuBar() )
					{
						if ( BeginMenu( "File" ) )
						{
							ShowExampleMenuFile();
							EndMenu();
						}
						if ( BeginMenu( "Edit" ) )
						{
							MenuItem( "Dummy" );
							EndMenu();
						}
						EndMenuBar();
					}
					Text( "Hello from popup!" );
					Button( "This is a dummy button.." );
					EndPopup();
				}
				TreePop();
			}

			if ( TreeNode( "Context menus" ) )
			{
				HelpMarker( "\"Context\" functions are simple helpers to associate a Popup to a given Item or Window identifier." );
				{
					string[] names = { "Label1", "Label2", "Label3", "Label4", "Label5" };
					for ( int n = 0; n < 5; n++ )
					{
						Selectable( names[n], _selectedFish == n );
						if ( BeginPopupContextItem() )
						{
							_selectedFish = n;
							Text( "This a popup for \"{0}\"!", names[n] );
							if ( Button( "Close" ) )
								CloseCurrentPopup();
							EndPopup();
						}
						SetItemTooltip( "Right-click to open popup" );
					}
				}

				{
					Text( "Value = {0:F3} <-- (1) right-click this text", _ctxValue );
					if ( BeginPopupContextItem( "my popup" ) )
					{
						if ( Selectable( "Set to zero" ) ) _ctxValue = 0.0f;
						if ( Selectable( "Set to PI" ) ) _ctxValue = 3.1415f;
						SetNextItemWidth( -1.17549435E-38f );
						DragFloat( "##Value", ref _ctxValue, 0.1f, 0.0f, 0.0f );
						EndPopup();
					}
					Text( "(2) Or right-click this text" );
					OpenPopupOnItemClick( "my popup", ImGuiPopupFlags.MouseButtonRight );
					if ( Button( "(3) Or click this button" ) )
						OpenPopup( "my popup" );
				}

				{
					HelpMarker( "Showcase using a popup ID linked to item ID, with the item having a changing label + stable ID using the ### operator." );
					Button( $"Button: {_contextName}###Button" );
					if ( BeginPopupContextItem() )
					{
						Text( "Edit name:" );
						InputText( "##edit", ref _contextName );
						if ( Button( "Close" ) )
							CloseCurrentPopup();
						EndPopup();
					}
					SameLine(); Text( "(<-- right-click here)" );
				}
				TreePop();
			}

			if ( TreeNode( "Modals" ) )
			{
				TextWrapped( "Modal windows are like popups but the user cannot close them by clicking outside." );

				if ( Button( "Delete.." ) )
					OpenPopup( "Delete?" );

				var center = GetIO().DisplaySize * 0.5f;
				SetNextWindowPos( center, ImGuiCond.Appearing, new Vector2( 0.5f, 0.5f ) );
				if ( BeginPopupModal( "Delete?", ImGuiWindowFlags.AlwaysAutoResize ) )
				{
					Text( "All those beautiful files will be deleted.\nThis operation cannot be undone!" );
					Separator();
					PushStyleVar( ImGuiStyleVar.FramePadding, new Vector2( 0, 0 ) );
					Checkbox( "Don't ask me next time", ref _dontAskMeNextTime );
					PopStyleVar();
					if ( Button( "OK", new Vector2( 120, 0 ) ) ) CloseCurrentPopup();
					SetItemDefaultFocus();
					SameLine();
					if ( Button( "Cancel", new Vector2( 120, 0 ) ) ) CloseCurrentPopup();
					EndPopup();
				}

				if ( Button( "Stacked modals.." ) )
					OpenPopup( "Stacked 1" );
				if ( BeginPopupModal( "Stacked 1", ImGuiWindowFlags.MenuBar ) )
				{
					if ( BeginMenuBar() )
					{
						if ( BeginMenu( "File" ) )
						{
							if ( MenuItem( "Some menu item" ) ) { }
							EndMenu();
						}
						EndMenuBar();
					}
					Text( "Hello from Stacked The First\nUsing style.Colors[ImGuiCol.ModalWindowDimBg] behind it." );
					Combo( "Combo", ref _modalItem, "aaaa\0bbbb\0cccc\0dddd\0eeee\0\0" );
					ColorEdit4( "Color", ref _modalColor );

					if ( Button( "Add another modal.." ) )
						OpenPopup( "Stacked 2" );

					bool unusedOpen = true;
					if ( BeginPopupModal( "Stacked 2", ref unusedOpen ) )
					{
						Text( "Hello from Stacked The Second!" );
						ColorEdit4( "Color", ref _modalColor );
						if ( Button( "Close" ) )
							CloseCurrentPopup();
						EndPopup();
					}

					if ( Button( "Close" ) )
						CloseCurrentPopup();
					EndPopup();
				}
				TreePop();
			}

			if ( TreeNode( "Menus inside a regular window" ) )
			{
				TextWrapped( "Below we are testing adding menu items to a regular window. It's rather unusual but should work!" );
				Separator();
				MenuItem( "Menu item", "CTRL+M" );
				if ( BeginMenu( "Menu inside a regular window" ) )
				{
					ShowExampleMenuFile();
					EndMenu();
				}
				Separator();
				TreePop();
			}
		}

		public static void ShowTables()
		{
			if ( !CollapsingHeader( "Tables & Columns" ) )
				return;

			if ( TreeNode( "Basic" ) )
			{
				if ( BeginTable( "table1", 3 ) )
				{
					for ( int row = 0; row < 4; row++ )
					{
						TableNextRow();
						for ( int column = 0; column < 3; column++ )
						{
							TableSetColumnIndex( column );
							Text( "Row {0} Column {1}", row, column );
						}
					}
					EndTable();
				}

				if ( BeginTable( "table2", 3 ) )
				{
					for ( int row = 0; row < 4; row++ )
					{
						TableNextRow();
						TableNextColumn(); Text( "Row {0}", row );
						TableNextColumn(); Text( "Some contents" );
						TableNextColumn(); Text( "123.456" );
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Borders, background" ) )
			{
				CheckboxFlags( "ImGuiTableFlags_RowBg", ref _tableFlags1, ImGuiTableFlags.RowBg );
				CheckboxFlags( "ImGuiTableFlags_Borders", ref _tableFlags1, ImGuiTableFlags.Borders );
				Indent();
				CheckboxFlags( "ImGuiTableFlags_BordersH", ref _tableFlags1, ImGuiTableFlags.BordersH );
				CheckboxFlags( "ImGuiTableFlags_BordersV", ref _tableFlags1, ImGuiTableFlags.BordersV );
				CheckboxFlags( "ImGuiTableFlags_BordersOuter", ref _tableFlags1, ImGuiTableFlags.BordersOuter );
				CheckboxFlags( "ImGuiTableFlags_BordersInner", ref _tableFlags1, ImGuiTableFlags.BordersInner );
				Unindent();

				if ( BeginTable( "table1", 3, _tableFlags1 ) )
				{
					TableSetupColumn( "One" );
					TableSetupColumn( "Two" );
					TableSetupColumn( "Three" );
					TableHeadersRow();
					for ( int row = 0; row < 5; row++ )
					{
						TableNextRow();
						for ( int column = 0; column < 3; column++ )
						{
							TableSetColumnIndex( column );
							Text( "Hello {0},{1}", column, row );
						}
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Resizable, stretch" ) )
			{
				CheckboxFlags( "ImGuiTableFlags_Resizable", ref _tableFlagsResize, ImGuiTableFlags.Resizable );
				CheckboxFlags( "ImGuiTableFlags_BordersV", ref _tableFlagsResize, ImGuiTableFlags.BordersV );
				SameLine(); HelpMarker( "Using the _Resizable flag automatically enables the _BordersInnerV flag as well, this is why the resize borders are still showing when unchecking this." );
				if ( BeginTable( "table1", 3, _tableFlagsResize ) )
				{
					for ( int row = 0; row < 5; row++ )
					{
						TableNextRow();
						for ( int column = 0; column < 3; column++ )
						{
							TableSetColumnIndex( column );
							Text( "Hello {0},{1}", column, row );
						}
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Vertical scrolling, with clipping" ) )
			{
				HelpMarker( "Here we activate ScrollY, which will create a child window container to allow hosting scrollable contents. The header row stays frozen at the top." );
				CheckboxFlags( "ImGuiTableFlags_ScrollY", ref _tableFlagsScroll, ImGuiTableFlags.ScrollY );
				var outerSize = new Vector2( 0.0f, GetTextLineHeightWithSpacing() * 8 );
				if ( BeginTable( "table_scrolly", 3, _tableFlagsScroll, outerSize ) )
				{
					TableSetupScrollFreeze( 0, 1 );
					TableSetupColumn( "One", ImGuiTableColumnFlags.None );
					TableSetupColumn( "Two", ImGuiTableColumnFlags.None );
					TableSetupColumn( "Three", ImGuiTableColumnFlags.None );
					TableHeadersRow();
					for ( int row = 0; row < 100; row++ )
					{
						TableNextRow();
						for ( int column = 0; column < 3; column++ )
						{
							TableSetColumnIndex( column );
							Text( "Hello {0},{1}", column, row );
						}
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Sorting" ) )
			{
				_sortItems ??= Enumerable.Range( 0, 50 ).Select( n => (n, new[] { "Apple", "Banana", "Cherry", "Kiwi", "Mango", "Orange", "Pear", "Pineapple", "Strawberry", "Watermelon" }[n % 10], (n * n - n) % 20) ).ToList();
				if ( BeginTable( "table_sorting", 4, _tableFlagsSort, new Vector2( 0.0f, GetTextLineHeightWithSpacing() * 15 ) ) )
				{
					TableSetupColumn( "ID", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 0.0f, 0 );
					TableSetupColumn( "Name", ImGuiTableColumnFlags.WidthFixed, 0.0f, 1 );
					TableSetupColumn( "Action", ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthFixed, 0.0f, 2 );
					TableSetupColumn( "Quantity", ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthStretch, 0.0f, 3 );
					TableSetupScrollFreeze( 0, 1 );
					TableHeadersRow();

					var sortSpecs = TableGetSortSpecs();
					if ( sortSpecs is not null && sortSpecs.SpecsDirty )
					{
						IEnumerable<(int Id, string Name, int Quantity)> sorted = _sortItems;
						IOrderedEnumerable<(int Id, string Name, int Quantity)> ordered = null;
						for ( int n = 0; n < sortSpecs.SpecsCount; n++ )
						{
							var spec = sortSpecs.Specs[n];
							bool asc = spec.SortDirection == ImGuiSortDirection.Ascending;
							Func<(int Id, string Name, int Quantity), object> key = spec.ColumnUserID switch
							{
								1 => x => x.Name,
								3 => x => x.Quantity,
								_ => x => x.Id,
							};
							ordered = ordered is null
								? (asc ? sorted.OrderBy( key ) : sorted.OrderByDescending( key ))
								: (asc ? ordered.ThenBy( key ) : ordered.ThenByDescending( key ));
						}
						if ( ordered is not null )
							_sortItems = ordered.ToList();
						sortSpecs.SpecsDirty = false;
					}

					foreach ( var item in _sortItems )
					{
						PushID( item.Id );
						TableNextRow();
						TableNextColumn(); Text( "{0:D4}", item.Id );
						TableNextColumn(); TextUnformatted( item.Name );
						TableNextColumn(); SmallButton( "None" );
						TableNextColumn(); Text( "{0}", item.Quantity );
						PopID();
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Legacy Columns API" ) )
			{
				HelpMarker( "Columns() is an old API! Prefer using the more flexible and powerful BeginTable() API!" );
				SliderInt( "Columns", ref _columnsCount, 1, 6 );
				Checkbox( "Borders", ref _columnsBorders );
				Columns( _columnsCount, null, _columnsBorders );
				for ( int i = 0; i < _columnsCount * 3; i++ )
				{
					if ( _columnsBorders && GetColumnIndex() == 0 )
						Separator();
					Text( "{0}{1}", (char)('a' + i), i );
					Text( "Width {0:F2}", GetColumnWidth() );
					Text( "Avail {0:F2}", GetContentRegionAvail().x );
					Text( "Offset {0:F2}", GetColumnOffset() );
					Text( "Long text that is likely to clip" );
					Button( "Button", new Vector2( -1.17549435E-38f, 0.0f ) );
					NextColumn();
				}
				Columns( 1 );
				if ( _columnsBorders )
					Separator();
				TreePop();
			}
		}

		public static void ShowInputs()
		{
			if ( !CollapsingHeader( "Inputs & Focus" ) )
				return;

			var io = GetIO();
			if ( TreeNode( "Inputs" ) )
			{
				if ( IsMousePosValid() )
					Text( "Mouse pos: ({0:F1},{1:F1})", io.MousePos.x, io.MousePos.y );
				else
					Text( "Mouse pos: <INVALID>" );
				Text( "Mouse delta: ({0:F1},{1:F1})", io.MouseDelta.x, io.MouseDelta.y );
				Text( "Mouse down:" );
				for ( int i = 0; i < io.MouseDown.Length; i++ )
					if ( IsMouseDown( (ImGuiMouseButton)Math.Min( i, 2 ) ) && i < 3 )
					{
						SameLine();
						Text( "b{0}", i );
					}
				Text( "Mouse wheel: {0:F1}", io.MouseWheel );
				Text( "Keys down:" );
				for ( var key = ImGuiKey.Tab; key < ImGuiKey.MouseLeft; key++ )
				{
					if ( !IsKeyDown( key ) ) continue;
					SameLine();
					Text( "\"{0}\"", GetKeyName( key ) );
				}
				Text( "Keys mods: {0}{1}{2}", io.KeyCtrl ? "CTRL " : "", io.KeyShift ? "SHIFT " : "", io.KeyAlt ? "ALT " : "" );
				TreePop();
			}

			if ( TreeNode( "WantCapture override" ) )
			{
				Text( "io.WantCaptureMouse: {0}", io.WantCaptureMouse );
				Text( "io.WantCaptureKeyboard: {0}", io.WantCaptureKeyboard );
				Text( "io.WantTextInput: {0}", io.WantTextInput );
				HelpMarker( "Your game should not react to mouse/keyboard input when these are true." );
				TreePop();
			}

			if ( TreeNode( "Mouse Cursors" ) )
			{
				string[] cursorNames = Enum.GetNames( typeof( ImGuiMouseCursor ) );
				Text( "Current mouse cursor = {0}", GetMouseCursor() );
				Text( "Hover to see mouse cursors:" );
				for ( int i = 1; i < cursorNames.Length; i++ )
				{
					Bullet(); Selectable( $"Mouse cursor {i - 1}: {cursorNames[i]}" );
					if ( IsItemHovered() )
						SetMouseCursor( (ImGuiMouseCursor)(i - 1) );
				}
				TreePop();
			}

			if ( TreeNode( "Focus from code" ) )
			{
				bool focus1 = Button( "Focus on 1" ); SameLine();
				bool focus2 = Button( "Focus on 2" ); SameLine();
				bool focus3 = Button( "Focus on 3" );
				if ( focus1 ) SetKeyboardFocusHere();
				InputText( "1", ref _str0 );
				if ( focus2 ) SetKeyboardFocusHere();
				InputText( "2", ref _str1 );
				if ( focus3 ) SetKeyboardFocusHere();
				InputText( "3 (tab skip)", ref _hint );
				TreePop();
			}

			if ( TreeNode( "Dragging" ) )
			{
				TextWrapped( "You can use GetMouseDragDelta(0) to query for the dragged amount on any widget." );
				Button( "Drag Me" );
				if ( IsItemActive() )
					GetForegroundDrawList().AddLine( io.MouseClickedPos[0], io.MousePos, GetColorU32( ImGuiCol.Button ), 4.0f );
				var valueRaw = GetMouseDragDelta( 0, 0.0f );
				var valueWithLockThreshold = GetMouseDragDelta( 0 );
				var mouseDelta = io.MouseDelta;
				Text( "GetMouseDragDelta(0):" );
				Text( "  w/ default threshold: ({0:F1}, {1:F1})", valueWithLockThreshold.x, valueWithLockThreshold.y );
				Text( "  w/ zero threshold: ({0:F1}, {1:F1})", valueRaw.x, valueRaw.y );
				Text( "io.MouseDelta: ({0:F1}, {1:F1})", mouseDelta.x, mouseDelta.y );
				TreePop();
			}
		}
	}
}
