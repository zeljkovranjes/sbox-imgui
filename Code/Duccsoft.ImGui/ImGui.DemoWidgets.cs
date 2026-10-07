namespace Duccsoft.ImGui;

public static partial class ImGui
{
	private static partial class Demo
	{
		// Basic
		private static int _clicked;
		private static bool _check = true;
		private static int _e;
		private static int _counter;
		private static string _str0 = "Hello, world!";
		private static string _str1 = "";
		private static int _i0 = 123;
		private static float _f0 = 0.001f;
		private static double _d0 = 999999.00000001;
		private static float _f1 = 1e10f;
		private static Vector3 _vec4a = new( 0.10f, 0.20f, 0.30f );
		private static int _i1 = 50, _i2 = 42, _i3 = 128;
		private static float _f1d = 1.00f, _f2d = 0.0067f;
		private static int _si = 0;
		private static float _sf1 = 0.123f, _sf2 = 0.0f;
		private static float _angle = 0.0f;
		private static int _elem;
		private static Vector3 _col1 = new( 1.0f, 0.0f, 0.2f );
		private static Vector4 _col2 = new( 0.4f, 0.7f, 0.0f, 0.5f );
		private static int _itemCurrent;
		private static int _listCurrent = 1;

		// Trees / headers
		private static bool _closableGroup = true;
		private static ImGuiTreeNodeFlags _baseFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth;
		private static int _treeSelectionMask = 1 << 2;

		// Text
		private static float _wrapWidth = 200.0f;

		// Combo
		private static ImGuiComboFlags _comboFlags;
		private static int _comboItemSelectedIdx;

		// Selectables
		private static readonly bool[] _selection = { false, true, false, false };
		private static int _selectedSingle = -1;
		private static readonly bool[] _selGrid = new bool[16];

		// Text input
		private static string _multiline = "/*\n The Pentium F00F bug, shorthand for F0 0F C7 C8,\n the hexadecimal encoding of one offending instruction,\n more formally, the invalid operand with locked CMPXCHG8B\n instruction bug, is a design flaw in the majority of\n Intel Pentium, Pentium MMX, and Pentium OverDrive\n processors (all in the P5 microarchitecture).\n*/\n\nlabel:\n\tlock cmpxchg8b eax\n";
		private static ImGuiInputTextFlags _multilineFlags = ImGuiInputTextFlags.AllowTabInput;
		private static string _bufDefault = "", _bufDecimal = "", _bufHex = "", _bufUpper = "", _bufNoBlank = "", _password = "password123";
		private static string _hint = "";

		// Tabs
		private static ImGuiTabBarFlags _tabBarFlags = ImGuiTabBarFlags.Reorderable;
		private static readonly bool[] _tabOpened = { true, true, true, true };
		private static readonly List<int> _activeTabs = new() { 0, 1, 2 };
		private static int _nextTabId = 3;

		// Plots
		private static bool _animate = true;
		private static readonly float[] _plotArr = { 0.6f, 0.1f, 1.0f, 0.5f, 0.92f, 0.1f, 0.2f };
		private static readonly float[] _plotValues = new float[90];
		private static int _plotValuesOffset;
		private static double _refreshTime;
		private static float _phase;
		private static float _progress, _progressDir = 1.0f;

		// Color
		private static Vector4 _color = new( 114.0f / 255.0f, 144.0f / 255.0f, 154.0f / 255.0f, 200.0f / 255.0f );
		private static bool _alphaPreview = true, _alphaHalfPreview, _dragAndDrop = true, _optionsMenu = true, _hdr;
		private static ImGuiColorEditFlags _pickerFlags = ImGuiColorEditFlags.PickerHueBar;
		private static int _pickerMode;

		// Drags & sliders
		private static ImGuiSliderFlags _sliderFlags;
		private static float _dragF = 0.5f;
		private static int _dragI = 50;
		private static float _sliderF = 0.5f;
		private static int _sliderI = 50;
		private static float _beginF = 10, _endF = 90;
		private static int _beginI = 100, _endI = 1000;
		private static Vector4 _vec4f = new( 0.10f, 0.20f, 0.30f, 0.44f );
		private static int[] _vec4i = { 1, 5, 100, 255 };
		private static readonly float[] _vValues = { 0.0f, 0.60f, 0.35f, 0.9f, 0.70f, 0.20f, 0.0f };
		private static int _vInt;

		// Drag and drop
		private static readonly string[] _dndNames = { "Bobby", "Beatrice", "Betty", "Brianna", "Barry", "Bernard", "Bibi", "Blaine", "Bryn" };
		private static int _dndMode;

		// Item status
		private static int _itemType = 4;
		private static bool _itemDisabled;
		private static bool _bStatus;
		private static Vector4 _colStatus = new( 1, 0.5f, 0, 1 );
		private static string _strStatus = "";
		private static int _currentStatus = 1;

		// Disable
		private static bool _disableAll;

		public static void ShowWidgets()
		{
			if ( !CollapsingHeader( "Widgets" ) )
				return;

			if ( _disableAll )
				BeginDisabled();

			if ( TreeNode( "Basic" ) )
			{
				SeparatorText( "General" );
				if ( Button( "Button" ) ) _clicked++;
				if ( (_clicked & 1) != 0 )
				{
					SameLine();
					Text( "Thanks for clicking me!" );
				}

				Checkbox( "checkbox", ref _check );

				RadioButton( "radio a", ref _e, 0 ); SameLine();
				RadioButton( "radio b", ref _e, 1 ); SameLine();
				RadioButton( "radio c", ref _e, 2 );

				// Colored buttons
				for ( int i = 0; i < 7; i++ )
				{
					if ( i > 0 ) SameLine();
					PushID( i );
					ColorConvertHSVtoRGB( i / 7.0f, 0.6f, 0.6f, out var r1, out var g1, out var b1 );
					ColorConvertHSVtoRGB( i / 7.0f, 0.7f, 0.7f, out var r2, out var g2, out var b2 );
					ColorConvertHSVtoRGB( i / 7.0f, 0.8f, 0.8f, out var r3, out var g3, out var b3 );
					PushStyleColor( ImGuiCol.Button, new Vector4( r1, g1, b1, 1 ) );
					PushStyleColor( ImGuiCol.ButtonHovered, new Vector4( r2, g2, b2, 1 ) );
					PushStyleColor( ImGuiCol.ButtonActive, new Vector4( r3, g3, b3, 1 ) );
					Button( "Click" );
					PopStyleColor( 3 );
					PopID();
				}

				AlignTextToFramePadding();
				Text( "Hold to repeat:" );
				SameLine();
				float spacing = GetStyle().ItemInnerSpacing.x;
				PushItemFlag( ImGuiItemFlags.ButtonRepeat, true );
				if ( ArrowButton( "##left", ImGuiDir.Left ) ) _counter--;
				SameLine( 0.0f, spacing );
				if ( ArrowButton( "##right", ImGuiDir.Right ) ) _counter++;
				PopItemFlag();
				SameLine();
				Text( "{0}", _counter );

				Button( "Tooltip" );
				SetItemTooltip( "I am a tooltip" );

				LabelText( "label", "Value" );

				SeparatorText( "Inputs" );
				InputText( "input text", ref _str0 );
				SameLine(); HelpMarker( "USER:\nHold SHIFT or use mouse to select text.\nCTRL+Left/Right to word jump.\nCTRL+A or Double-Click to select all.\nCTRL+X,CTRL+C,CTRL+V clipboard.\nCTRL+Z,CTRL+Y undo/redo.\nESCAPE to revert." );
				InputTextWithHint( "input text (w/ hint)", "enter text here", ref _str1 );
				InputInt( "input int", ref _i0 );
				InputFloat( "input float", ref _f0, 0.01f, 1.0f, "%.3f" );
				InputDouble( "input double", ref _d0, 0.01, 1.0, "%.8f" );
				InputFloat( "input scientific", ref _f1, 0.0f, 0.0f, "%e" );
				InputFloat3( "input float3", ref _vec4a );

				SeparatorText( "Drags" );
				DragInt( "drag int", ref _i1, 1 );
				SameLine(); HelpMarker( "Click and drag to edit value.\nHold SHIFT/ALT for faster/slower edit.\nDouble-click or CTRL+click to input value." );
				DragInt( "drag int 0..100", ref _i2, 1, 0, 100, "%d%%", ImGuiSliderFlags.AlwaysClamp );
				DragInt( "drag int wrap 100..200", ref _i3, 1, 100, 200, "%d", ImGuiSliderFlags.WrapAround );
				DragFloat( "drag float", ref _f1d, 0.005f );
				DragFloat( "drag small float", ref _f2d, 0.0001f, 0.0f, 0.0f, "%.06f ns" );

				SeparatorText( "Sliders" );
				SliderInt( "slider int", ref _si, -1, 3 );
				SliderFloat( "slider float", ref _sf1, 0.0f, 1.0f, "ratio = %.3f" );
				SliderFloat( "slider float (log)", ref _sf2, -10.0f, 10.0f, "%.4f", ImGuiSliderFlags.Logarithmic );
				SliderAngle( "slider angle", ref _angle );
				string[] elemsNames = { "Fire", "Earth", "Air", "Water" };
				string elemName = _elem >= 0 && _elem < 4 ? elemsNames[_elem] : "Unknown";
				SliderInt( "slider enum", ref _elem, 0, 3, elemName );

				SeparatorText( "Selectors/Pickers" );
				ColorEdit3( "color 1", ref _col1 );
				ColorEdit4( "color 2", ref _col2 );
				string[] items = { "AAAA", "BBBB", "CCCC", "DDDD", "EEEE", "FFFF", "GGGG", "HHHH", "IIIIIII", "JJJJ", "KKKKKKK" };
				Combo( "combo", ref _itemCurrent, items );
				string[] fruits = { "Apple", "Banana", "Cherry", "Kiwi", "Mango", "Orange", "Pineapple", "Strawberry", "Watermelon" };
				ListBox( "listbox", ref _listCurrent, fruits, 4 );

				TreePop();
			}

			if ( TreeNode( "Trees" ) )
			{
				if ( TreeNode( "Basic trees" ) )
				{
					for ( int i = 0; i < 5; i++ )
					{
						if ( i == 0 )
							SetNextItemOpen( true, ImGuiCond.Once );
						PushID( i );
						if ( TreeNode( "", "Child {0}", i ) )
						{
							Text( "blah blah" );
							SameLine();
							if ( SmallButton( "button" ) ) { }
							TreePop();
						}
						PopID();
					}
					TreePop();
				}

				if ( TreeNode( "Advanced, with Selectable nodes" ) )
				{
					CheckboxFlags( "ImGuiTreeNodeFlags_OpenOnArrow", ref _baseFlags, ImGuiTreeNodeFlags.OpenOnArrow );
					CheckboxFlags( "ImGuiTreeNodeFlags_OpenOnDoubleClick", ref _baseFlags, ImGuiTreeNodeFlags.OpenOnDoubleClick );
					CheckboxFlags( "ImGuiTreeNodeFlags_SpanAvailWidth", ref _baseFlags, ImGuiTreeNodeFlags.SpanAvailWidth );
					CheckboxFlags( "ImGuiTreeNodeFlags_SpanFullWidth", ref _baseFlags, ImGuiTreeNodeFlags.SpanFullWidth );

					int nodeClicked = -1;
					for ( int i = 0; i < 6; i++ )
					{
						var nodeFlags = _baseFlags;
						bool isSelected = (_treeSelectionMask & (1 << i)) != 0;
						if ( isSelected )
							nodeFlags |= ImGuiTreeNodeFlags.Selected;
						if ( i < 3 )
						{
							bool nodeOpen = TreeNodeEx( $"node{i}", nodeFlags, "Selectable Node {0}", i );
							if ( IsItemClicked() && !IsItemToggledOpen() )
								nodeClicked = i;
							if ( nodeOpen )
							{
								BulletText( "Blah blah\nBlah Blah" );
								TreePop();
							}
						}
						else
						{
							nodeFlags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
							TreeNodeEx( $"leaf{i}", nodeFlags, "Selectable Leaf {0}", i );
							if ( IsItemClicked() && !IsItemToggledOpen() )
								nodeClicked = i;
						}
					}
					if ( nodeClicked != -1 )
					{
						if ( GetIO().KeyCtrl )
							_treeSelectionMask ^= 1 << nodeClicked;
						else
							_treeSelectionMask = 1 << nodeClicked;
					}
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Collapsing Headers" ) )
			{
				Checkbox( "Show 2nd header", ref _closableGroup );
				if ( CollapsingHeader( "Header", ImGuiTreeNodeFlags.None ) )
				{
					Text( "IsItemHovered: {0}", IsItemHovered() );
					for ( int i = 0; i < 5; i++ )
						Text( "Some content {0}", i );
				}
				if ( CollapsingHeader( "Header with a close button", ref _closableGroup ) )
				{
					Text( "IsItemHovered: {0}", IsItemHovered() );
					for ( int i = 0; i < 5; i++ )
						Text( "More content {0}", i );
				}
				TreePop();
			}

			if ( TreeNode( "Bullets" ) )
			{
				BulletText( "Bullet point 1" );
				BulletText( "Bullet point 2\nOn multiple lines" );
				if ( TreeNode( "Tree node" ) )
				{
					BulletText( "Another bullet point" );
					TreePop();
				}
				Bullet(); Text( "Bullet point 3 (two calls)" );
				Bullet(); SmallButton( "Button" );
				TreePop();
			}

			if ( TreeNode( "Text" ) )
			{
				if ( TreeNode( "Colorful Text" ) )
				{
					TextColored( new Vector4( 1.0f, 0.0f, 1.0f, 1.0f ), "Pink" );
					TextColored( new Vector4( 1.0f, 1.0f, 0.0f, 1.0f ), "Yellow" );
					TextDisabled( "Disabled" );
					SameLine(); HelpMarker( "The TextDisabled color is stored in ImGuiStyle." );
					TreePop();
				}

				if ( TreeNode( "Word Wrapping" ) )
				{
					TextWrapped( "This text should automatically wrap on the edge of the window. The current implementation for text wrapping follows simple rules suitable for English and possibly other languages." );
					Spacing();
					SliderFloat( "Wrap width", ref _wrapWidth, -20, 600, "%.0f" );
					var drawList = GetWindowDrawList();
					for ( int n = 0; n < 2; n++ )
					{
						Text( "Test paragraph {0}:", n );
						var pos = GetCursorScreenPos();
						var markerMin = new Vector2( pos.x + _wrapWidth, pos.y );
						var markerMax = new Vector2( pos.x + _wrapWidth + 10, pos.y + GetTextLineHeight() );
						PushTextWrapPos( GetCursorPos().x + _wrapWidth );
						if ( n == 0 )
							Text( "The lazy dog is a good dog. This paragraph should fit within {0} pixels. Testing a 1 character word. The quick brown fox jumps over the lazy dog.", (int)_wrapWidth );
						else
							Text( "aaaaaaaa bbbbbbbb, c cccccccc,dddddddd. d eeeeeeee   ffffffff. gggggggg!hhhhhhhh" );
						drawList.AddRect( GetItemRectMin(), GetItemRectMax(), new Color32( 255, 255, 0, 255 ) );
						drawList.AddRectFilled( markerMin, markerMax, new Color32( 255, 0, 255, 255 ) );
						PopTextWrapPos();
					}
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Images" ) )
			{
				TextWrapped( "Images take an s&box Texture. Here is the white texture tinted, and a UV sub-rect." );
				var tex = Texture.White;
				Image( tex, new Vector2( 64, 64 ), Vector2.Zero, Vector2.One, new Vector4( 0.3f, 0.6f, 1, 1 ), new Vector4( 1, 1, 1, 0.5f ) );
				SameLine();
				if ( ImageButton( "imgbtn", tex, new Vector2( 32, 32 ), Vector2.Zero, Vector2.One, new Vector4( 0, 0, 0, 1 ), new Vector4( 1, 0.4f, 0.4f, 1 ) ) )
					_clicked++;
				TreePop();
			}

			if ( TreeNode( "Combo" ) )
			{
				CheckboxFlags( "ImGuiComboFlags_PopupAlignLeft", ref _comboFlags, ImGuiComboFlags.PopupAlignLeft );
				if ( CheckboxFlags( "ImGuiComboFlags_NoArrowButton", ref _comboFlags, ImGuiComboFlags.NoArrowButton ) )
					_comboFlags &= ~ImGuiComboFlags.NoPreview;
				if ( CheckboxFlags( "ImGuiComboFlags_NoPreview", ref _comboFlags, ImGuiComboFlags.NoPreview ) )
					_comboFlags &= ~(ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.WidthFitPreview);
				if ( CheckboxFlags( "ImGuiComboFlags_WidthFitPreview", ref _comboFlags, ImGuiComboFlags.WidthFitPreview ) )
					_comboFlags &= ~ImGuiComboFlags.NoPreview;

				string[] items = { "AAAA", "BBBB", "CCCC", "DDDD", "EEEE", "FFFF", "GGGG", "HHHH", "IIII", "JJJJ", "KKKK", "LLLLLLL", "MMMM", "OOOOOOO" };
				if ( BeginCombo( "combo 1", items[_comboItemSelectedIdx], _comboFlags ) )
				{
					for ( int n = 0; n < items.Length; n++ )
					{
						bool isSelected = _comboItemSelectedIdx == n;
						if ( Selectable( items[n], isSelected ) )
							_comboItemSelectedIdx = n;
						if ( isSelected )
							SetItemDefaultFocus();
					}
					EndCombo();
				}
				Combo( "combo 2 (one-liner)", ref _comboItemSelectedIdx, "aaaa\0bbbb\0cccc\0dddd\0eeee\0\0" );
				TreePop();
			}

			if ( TreeNode( "Selectables" ) )
			{
				if ( TreeNode( "Basic" ) )
				{
					Selectable( "1. I am selectable", ref _selection[0] );
					Selectable( "2. I am selectable", ref _selection[1] );
					Selectable( "3. I am selectable", ref _selection[2] );
					if ( Selectable( "4. I am double clickable", _selection[3], ImGuiSelectableFlags.AllowDoubleClick ) )
						if ( IsMouseDoubleClicked( ImGuiMouseButton.Left ) )
							_selection[3] = !_selection[3];
					TreePop();
				}
				if ( TreeNode( "Single-Select" ) )
				{
					for ( int n = 0; n < 5; n++ )
						if ( Selectable( $"Object {n}", _selectedSingle == n ) )
							_selectedSingle = n;
					TreePop();
				}
				if ( TreeNode( "Grid" ) )
				{
					for ( int y = 0; y < 4; y++ )
						for ( int x = 0; x < 4; x++ )
						{
							if ( x > 0 ) SameLine();
							PushID( y * 4 + x );
							if ( Selectable( "Sailor", _selGrid[y * 4 + x], ImGuiSelectableFlags.None, new Vector2( 50, 50 ) ) )
							{
								_selGrid[y * 4 + x] ^= true;
								if ( x > 0 ) _selGrid[y * 4 + x - 1] ^= true;
								if ( x < 3 ) _selGrid[y * 4 + x + 1] ^= true;
								if ( y > 0 ) _selGrid[(y - 1) * 4 + x] ^= true;
								if ( y < 3 ) _selGrid[(y + 1) * 4 + x] ^= true;
							}
							PopID();
						}
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Text Input" ) )
			{
				if ( TreeNode( "Multi-line Text Input" ) )
				{
					CheckboxFlags( "ImGuiInputTextFlags_ReadOnly", ref _multilineFlags, ImGuiInputTextFlags.ReadOnly );
					CheckboxFlags( "ImGuiInputTextFlags_AllowTabInput", ref _multilineFlags, ImGuiInputTextFlags.AllowTabInput );
					CheckboxFlags( "ImGuiInputTextFlags_CtrlEnterForNewLine", ref _multilineFlags, ImGuiInputTextFlags.CtrlEnterForNewLine );
					InputTextMultiline( "##source", ref _multiline, new Vector2( -1.17549435E-38f, GetTextLineHeight() * 16 ), _multilineFlags );
					TreePop();
				}
				if ( TreeNode( "Filtered Text Input" ) )
				{
					InputText( "default", ref _bufDefault );
					InputText( "decimal", ref _bufDecimal, ImGuiInputTextFlags.CharsDecimal );
					InputText( "hexadecimal", ref _bufHex, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase );
					InputText( "uppercase", ref _bufUpper, ImGuiInputTextFlags.CharsUppercase );
					InputText( "no blank", ref _bufNoBlank, ImGuiInputTextFlags.CharsNoBlank );
					TreePop();
				}
				if ( TreeNode( "Password Input" ) )
				{
					InputText( "password", ref _password, ImGuiInputTextFlags.Password );
					SameLine(); HelpMarker( "Display all characters as '*'." );
					InputTextWithHint( "password (w/ hint)", "<password>", ref _password, ImGuiInputTextFlags.Password );
					InputText( "password (clear)", ref _password );
					TreePop();
				}
				if ( TreeNode( "Hint" ) )
				{
					InputTextWithHint( "##hint", "type something...", ref _hint );
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Tabs" ) )
			{
				if ( TreeNode( "Basic" ) )
				{
					if ( BeginTabBar( "MyTabBar" ) )
					{
						if ( BeginTabItem( "Avocado" ) )
						{
							Text( "This is the Avocado tab!\nblah blah blah blah blah" );
							EndTabItem();
						}
						if ( BeginTabItem( "Broccoli" ) )
						{
							Text( "This is the Broccoli tab!\nblah blah blah blah blah" );
							EndTabItem();
						}
						if ( BeginTabItem( "Cucumber" ) )
						{
							Text( "This is the Cucumber tab!\nblah blah blah blah blah" );
							EndTabItem();
						}
						EndTabBar();
					}
					Separator();
					TreePop();
				}

				if ( TreeNode( "Advanced & Close Button" ) )
				{
					CheckboxFlags( "ImGuiTabBarFlags_Reorderable", ref _tabBarFlags, ImGuiTabBarFlags.Reorderable );
					CheckboxFlags( "ImGuiTabBarFlags_AutoSelectNewTabs", ref _tabBarFlags, ImGuiTabBarFlags.AutoSelectNewTabs );
					CheckboxFlags( "ImGuiTabBarFlags_TabListPopupButton", ref _tabBarFlags, ImGuiTabBarFlags.TabListPopupButton );
					CheckboxFlags( "ImGuiTabBarFlags_NoCloseWithMiddleMouseButton", ref _tabBarFlags, ImGuiTabBarFlags.NoCloseWithMiddleMouseButton );
					if ( (_tabBarFlags & ImGuiTabBarFlags.FittingPolicyMask_) == 0 )
						_tabBarFlags |= ImGuiTabBarFlags.FittingPolicyDefault_;
					if ( CheckboxFlags( "ImGuiTabBarFlags_FittingPolicyResizeDown", ref _tabBarFlags, ImGuiTabBarFlags.FittingPolicyResizeDown ) )
						_tabBarFlags &= ~(ImGuiTabBarFlags.FittingPolicyMask_ ^ ImGuiTabBarFlags.FittingPolicyResizeDown);
					if ( CheckboxFlags( "ImGuiTabBarFlags_FittingPolicyScroll", ref _tabBarFlags, ImGuiTabBarFlags.FittingPolicyScroll ) )
						_tabBarFlags &= ~(ImGuiTabBarFlags.FittingPolicyMask_ ^ ImGuiTabBarFlags.FittingPolicyScroll);

					string[] names = { "Artichoke", "Beetroot", "Celery", "Daikon" };
					for ( int n = 0; n < _tabOpened.Length; n++ )
					{
						if ( n > 0 ) SameLine();
						Checkbox( names[n], ref _tabOpened[n] );
					}

					if ( BeginTabBar( "MyTabBar2", _tabBarFlags ) )
					{
						for ( int n = 0; n < _tabOpened.Length; n++ )
						{
							if ( _tabOpened[n] && BeginTabItem( names[n], ref _tabOpened[n], ImGuiTabItemFlags.None ) )
							{
								Text( "This is the {0} tab!", names[n] );
								if ( (n & 1) != 0 )
									Text( "I am an odd tab." );
								EndTabItem();
							}
						}
						EndTabBar();
					}
					Separator();
					TreePop();
				}

				if ( TreeNode( "TabItemButton & Leading/Trailing flags" ) )
				{
					if ( BeginTabBar( "MyTabBar3", ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.FittingPolicyResizeDown ) )
					{
						if ( TabItemButton( "?", ImGuiTabItemFlags.Leading | ImGuiTabItemFlags.NoTooltip ) )
							OpenPopup( "MyHelpMenu" );
						if ( BeginPopup( "MyHelpMenu" ) )
						{
							Selectable( "Hello!" );
							EndPopup();
						}
						if ( TabItemButton( "+", ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip ) )
							_activeTabs.Add( _nextTabId++ );

						for ( int n = 0; n < _activeTabs.Count; )
						{
							bool open = true;
							string name = $"{_activeTabs[n]:D4}";
							if ( BeginTabItem( name, ref open, ImGuiTabItemFlags.None ) )
							{
								Text( "This is the {0} tab!", name );
								EndTabItem();
							}
							if ( !open )
								_activeTabs.RemoveAt( n );
							else
								n++;
						}
						EndTabBar();
					}
					Separator();
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Plotting" ) )
			{
				Checkbox( "Animate", ref _animate );
				PlotLines( "Frame Times", _plotArr );
				PlotHistogram( "Histogram", _plotArr, 0, null, 0.0f, 1.0f, new Vector2( 0, 80.0f ) );

				if ( !_animate || _refreshTime == 0.0 )
					_refreshTime = GetTime();
				while ( _refreshTime < GetTime() )
				{
					_plotValues[_plotValuesOffset] = MathF.Cos( _phase );
					_plotValuesOffset = (_plotValuesOffset + 1) % _plotValues.Length;
					_phase += 0.10f * _plotValuesOffset;
					_refreshTime += 1.0f / 60.0f;
				}
				float average = _plotValues.Average();
				PlotLines( "Lines", _plotValues, _plotValuesOffset, $"avg {average:F6}", -1.0f, 1.0f, new Vector2( 0, 80.0f ) );

				Func<int, float> sinFn = i => MathF.Sin( i * 0.1f );
				Func<int, float> sawFn = i => (i & 1) != 0 ? 1.0f : -1.0f;
				PlotLines( "Sin", sinFn, 70, 0, null, -1.0f, 1.0f, new Vector2( 0, 80 ) );
				PlotHistogram( "Saw", sawFn, 70, 0, null, -1.0f, 1.0f, new Vector2( 0, 80 ) );

				SeparatorText( "Progress Bars" );
				if ( _animate )
				{
					_progress += _progressDir * 0.4f * GetIO().DeltaTime;
					if ( _progress >= +1.1f ) { _progress = +1.1f; _progressDir *= -1.0f; }
					if ( _progress <= -0.1f ) { _progress = -0.1f; _progressDir *= -1.0f; }
				}
				ProgressBar( _progress, new Vector2( 0.0f, 0.0f ) );
				SameLine( 0.0f, GetStyle().ItemInnerSpacing.x );
				Text( "Progress Bar" );
				float progressSaturated = Math.Clamp( _progress, 0.0f, 1.0f );
				ProgressBar( _progress, new Vector2( 0, 0 ), $"{(int)(progressSaturated * 1753)}/1753" );
				ProgressBar( -1.0f * (float)GetTime(), new Vector2( 0.0f, 0.0f ), "Searching.." );
				TreePop();
			}

			if ( TreeNode( "Color/Picker Widgets" ) )
			{
				SeparatorText( "Options" );
				Checkbox( "With Alpha Preview", ref _alphaPreview );
				Checkbox( "With Half Alpha Preview", ref _alphaHalfPreview );
				Checkbox( "With Drag and Drop", ref _dragAndDrop );
				Checkbox( "With Options Menu", ref _optionsMenu ); SameLine(); HelpMarker( "Right-click on the individual color widget to show options." );
				Checkbox( "With HDR", ref _hdr );
				var miscFlags = (_hdr ? ImGuiColorEditFlags.HDR : 0) | (_dragAndDrop ? 0 : ImGuiColorEditFlags.NoDragDrop) | (_alphaHalfPreview ? ImGuiColorEditFlags.AlphaPreviewHalf : (_alphaPreview ? 0 : ImGuiColorEditFlags.AlphaOpaque)) | (_optionsMenu ? 0 : ImGuiColorEditFlags.NoOptions);

				SeparatorText( "Inline color editor" );
				ColorEdit3( "MyColor##1", ref _color, miscFlags );
				ColorEdit4( "MyColor##2", ref _color, ImGuiColorEditFlags.DisplayHSV | miscFlags );
				ColorEdit4( "MyColor##2f", ref _color, ImGuiColorEditFlags.Float | miscFlags );
				ColorEdit4( "MyColor##3", ref _color, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | miscFlags );
				SameLine(); Text( "Color button only" );

				SeparatorText( "Color button" );
				ColorButton( "MyColor##3c", _color, miscFlags, new Vector2( 80, 80 ) );

				SeparatorText( "Color picker" );
				Combo( "Display Mode", ref _pickerMode, "Auto/Current\0Hue bar + SV rect\0Hue wheel + SV triangle\0" );
				var flags = miscFlags | ImGuiColorEditFlags.AlphaBar;
				if ( _pickerMode == 1 ) flags |= ImGuiColorEditFlags.PickerHueBar;
				if ( _pickerMode == 2 ) flags |= ImGuiColorEditFlags.PickerHueWheel;
				ColorPicker4( "MyColor##4", ref _color, flags );
				TreePop();
			}

			if ( TreeNode( "Drag/Slider Flags" ) )
			{
				CheckboxFlags( "ImGuiSliderFlags_AlwaysClamp", ref _sliderFlags, ImGuiSliderFlags.AlwaysClamp );
				CheckboxFlags( "ImGuiSliderFlags_Logarithmic", ref _sliderFlags, ImGuiSliderFlags.Logarithmic );
				CheckboxFlags( "ImGuiSliderFlags_NoRoundToFormat", ref _sliderFlags, ImGuiSliderFlags.NoRoundToFormat );
				CheckboxFlags( "ImGuiSliderFlags_NoInput", ref _sliderFlags, ImGuiSliderFlags.NoInput );
				CheckboxFlags( "ImGuiSliderFlags_WrapAround", ref _sliderFlags, ImGuiSliderFlags.WrapAround );

				SeparatorText( "Drags" );
				DragFloat( "DragFloat (0 -> 1)", ref _dragF, 0.005f, 0.0f, 1.0f, "%.3f", _sliderFlags );
				DragFloat( "DragFloat (0 -> +inf)", ref _dragF, 0.005f, 0.0f, float.MaxValue, "%.3f", _sliderFlags );
				DragInt( "DragInt (0 -> 100)", ref _dragI, 0.5f, 0, 100, "%d", _sliderFlags );

				SeparatorText( "Sliders" );
				SliderFloat( "SliderFloat (0 -> 1)", ref _sliderF, 0.0f, 1.0f, "%.3f", _sliderFlags );
				SliderInt( "SliderInt (0 -> 100)", ref _sliderI, 0, 100, "%d", _sliderFlags );
				TreePop();
			}

			if ( TreeNode( "Range Widgets" ) )
			{
				DragFloatRange2( "range float", ref _beginF, ref _endF, 0.25f, 0.0f, 100.0f, "Min: %.1f %%", "Max: %.1f %%", ImGuiSliderFlags.AlwaysClamp );
				DragIntRange2( "range int", ref _beginI, ref _endI, 5, 0, 1000, "Min: %d units", "Max: %d units" );
				TreePop();
			}

			if ( TreeNode( "Multi-component Widgets" ) )
			{
				var v2 = new Vector2( _vec4f.x, _vec4f.y );
				var v3 = new Vector3( _vec4f.x, _vec4f.y, _vec4f.z );
				SeparatorText( "2-wide" );
				if ( InputFloat2( "input float2", ref v2 ) ) { _vec4f.x = v2.x; _vec4f.y = v2.y; }
				if ( DragFloat2( "drag float2", ref v2, 0.01f, 0.0f, 1.0f ) ) { _vec4f.x = v2.x; _vec4f.y = v2.y; }
				if ( SliderFloat2( "slider float2", ref v2, 0.0f, 1.0f ) ) { _vec4f.x = v2.x; _vec4f.y = v2.y; }
				SeparatorText( "3-wide" );
				if ( InputFloat3( "input float3", ref v3 ) ) { _vec4f.x = v3.x; _vec4f.y = v3.y; _vec4f.z = v3.z; }
				if ( DragFloat3( "drag float3", ref v3, 0.01f, 0.0f, 1.0f ) ) { _vec4f.x = v3.x; _vec4f.y = v3.y; _vec4f.z = v3.z; }
				if ( SliderFloat3( "slider float3", ref v3, 0.0f, 1.0f ) ) { _vec4f.x = v3.x; _vec4f.y = v3.y; _vec4f.z = v3.z; }
				SeparatorText( "4-wide" );
				InputFloat4( "input float4", ref _vec4f );
				DragFloat4( "drag float4", ref _vec4f, 0.01f, 0.0f, 1.0f );
				SliderFloat4( "slider float4", ref _vec4f, 0.0f, 1.0f );
				InputInt4( "input int4", _vec4i );
				DragInt4( "drag int4", _vec4i, 1, 0, 255 );
				SliderInt4( "slider int4", _vec4i, 0, 255 );
				TreePop();
			}

			if ( TreeNode( "Vertical Sliders" ) )
			{
				const float spacing = 4;
				PushStyleVar( ImGuiStyleVar.ItemSpacing, new Vector2( spacing, spacing ) );
				VSliderInt( "##int", new Vector2( 18, 160 ), ref _vInt, 0, 5 );
				SameLine();
				PushID( "set1" );
				for ( int i = 0; i < 7; i++ )
				{
					if ( i > 0 ) SameLine();
					PushID( i );
					ColorConvertHSVtoRGB( i / 7.0f, 0.5f, 0.5f, out var r, out var gg, out var b );
					PushStyleColor( ImGuiCol.FrameBg, new Vector4( r, gg, b, 1 ) );
					ColorConvertHSVtoRGB( i / 7.0f, 0.9f, 0.9f, out r, out gg, out b );
					PushStyleColor( ImGuiCol.SliderGrab, new Vector4( r, gg, b, 1 ) );
					VSliderFloat( "##v", new Vector2( 18, 160 ), ref _vValues[i], 0.0f, 1.0f, "" );
					if ( IsItemActive() || IsItemHovered() )
						SetTooltip( "{0:F3}", _vValues[i] );
					PopStyleColor( 2 );
					PopID();
				}
				PopID();
				PopStyleVar();
				TreePop();
			}

			if ( TreeNode( "Drag and Drop" ) )
			{
				if ( TreeNode( "Drag and drop in standard widgets" ) )
				{
					HelpMarker( "You can drag from the color squares." );
					ColorEdit3( "color 1", ref _col1 );
					ColorEdit4( "color 2", ref _col2 );
					TreePop();
				}

				if ( TreeNode( "Drag and drop to copy/swap items" ) )
				{
					RadioButton( "Copy", ref _dndMode, 0 ); SameLine();
					RadioButton( "Move", ref _dndMode, 1 ); SameLine();
					RadioButton( "Swap", ref _dndMode, 2 );
					for ( int n = 0; n < _dndNames.Length; n++ )
					{
						PushID( n );
						if ( (n % 3) != 0 )
							SameLine();
						Button( _dndNames[n], new Vector2( 60, 60 ) );

						if ( BeginDragDropSource( ImGuiDragDropFlags.None ) )
						{
							SetDragDropPayload( "DND_DEMO_CELL", n );
							if ( _dndMode == 0 ) Text( "Copy {0}", _dndNames[n] );
							if ( _dndMode == 1 ) Text( "Move {0}", _dndNames[n] );
							if ( _dndMode == 2 ) Text( "Swap {0}", _dndNames[n] );
							EndDragDropSource();
						}
						if ( BeginDragDropTarget() )
						{
							var payload = AcceptDragDropPayload( "DND_DEMO_CELL" );
							if ( payload is not null )
							{
								int nPayload = payload.GetData<int>();
								if ( _dndMode == 0 )
									_dndNames[n] = _dndNames[nPayload];
								if ( _dndMode == 1 )
								{
									_dndNames[n] = _dndNames[nPayload];
									_dndNames[nPayload] = "";
								}
								if ( _dndMode == 2 )
									(_dndNames[n], _dndNames[nPayload]) = (_dndNames[nPayload], _dndNames[n]);
							}
							EndDragDropTarget();
						}
						PopID();
					}
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Querying Item Status (Edited/Active/Hovered etc.)" ) )
			{
				string[] itemNames = { "Text", "Button", "Button (w/ repeat)", "Checkbox", "SliderFloat", "InputText", "InputTextMultiline", "InputFloat", "InputFloat3", "ColorEdit4", "Selectable", "MenuItem", "TreeNode", "TreeNode (w/ double-click)", "Combo", "ListBox" };
				Combo( "Item Type", ref _itemType, itemNames, itemNames.Length );
				SameLine();
				Checkbox( "Item Disabled", ref _itemDisabled );

				bool ret = false;
				if ( _itemDisabled ) BeginDisabled();
				if ( _itemType == 0 ) Text( "ITEM: Text" );
				if ( _itemType == 1 ) ret = Button( "ITEM: Button" );
				if ( _itemType == 2 ) { PushItemFlag( ImGuiItemFlags.ButtonRepeat, true ); ret = Button( "ITEM: Button" ); PopItemFlag(); }
				if ( _itemType == 3 ) ret = Checkbox( "ITEM: Checkbox", ref _bStatus );
				if ( _itemType == 4 ) { float fx = _colStatus.x; ret = SliderFloat( "ITEM: SliderFloat", ref fx, 0.0f, 1.0f ); _colStatus.x = fx; }
				if ( _itemType == 5 ) ret = InputText( "ITEM: InputText", ref _strStatus );
				if ( _itemType == 6 ) ret = InputTextMultiline( "ITEM: InputTextMultiline", ref _strStatus );
				if ( _itemType == 7 ) { float fx = _colStatus.x; ret = InputFloat( "ITEM: InputFloat", ref fx, 1.0f ); _colStatus.x = fx; }
				if ( _itemType == 8 ) { var v = new Vector3( _colStatus.x, _colStatus.y, _colStatus.z ); ret = InputFloat3( "ITEM: InputFloat3", ref v ); }
				if ( _itemType == 9 ) ret = ColorEdit4( "ITEM: ColorEdit4", ref _colStatus );
				if ( _itemType == 10 ) ret = Selectable( "ITEM: Selectable" );
				if ( _itemType == 11 ) ret = MenuItem( "ITEM: MenuItem" );
				if ( _itemType == 12 ) { ret = TreeNode( "ITEM: TreeNode" ); if ( ret ) TreePop(); }
				if ( _itemType == 13 ) ret = TreeNodeEx( "ITEM: TreeNode w/ ImGuiTreeNodeFlags_OpenOnDoubleClick", ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.NoTreePushOnOpen );
				if ( _itemType == 14 ) ret = Combo( "ITEM: Combo", ref _currentStatus, new[] { "Apple", "Banana", "Cherry", "Kiwi" } );
				if ( _itemType == 15 ) ret = ListBox( "ITEM: ListBox", ref _currentStatus, new[] { "Apple", "Banana", "Cherry", "Kiwi" } );

				bool hoveredDelayNone = IsItemHovered();
				bool hoveredDelayShort = IsItemHovered( ImGuiHoveredFlags.DelayShort );
				bool hoveredDelayNormal = IsItemHovered( ImGuiHoveredFlags.DelayNormal );
				BulletText( "Return value = {0}", ret );
				BulletText( "IsItemFocused() = {0}", IsItemFocused() );
				BulletText( "IsItemHovered() = {0}", IsItemHovered() );
				BulletText( "IsItemHovered(_AllowWhenBlockedByPopup) = {0}", IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup ) );
				BulletText( "IsItemHovered(_AllowWhenBlockedByActiveItem) = {0}", IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByActiveItem ) );
				BulletText( "IsItemHovered(_AllowWhenOverlappedByItem) = {0}", IsItemHovered( ImGuiHoveredFlags.AllowWhenOverlappedByItem ) );
				BulletText( "IsItemHovered(_RectOnly) = {0}", IsItemHovered( ImGuiHoveredFlags.RectOnly ) );
				BulletText( "IsItemActive() = {0}", IsItemActive() );
				BulletText( "IsItemEdited() = {0}", IsItemEdited() );
				BulletText( "IsItemActivated() = {0}", IsItemActivated() );
				BulletText( "IsItemDeactivated() = {0}", IsItemDeactivated() );
				BulletText( "IsItemDeactivatedAfterEdit() = {0}", IsItemDeactivatedAfterEdit() );
				BulletText( "IsItemVisible() = {0}", IsItemVisible() );
				BulletText( "IsItemClicked() = {0}", IsItemClicked() );
				BulletText( "IsItemToggledOpen() = {0}", IsItemToggledOpen() );
				BulletText( "GetItemRectMin() = ({0:F1}, {1:F1})", GetItemRectMin().x, GetItemRectMin().y );
				BulletText( "GetItemRectMax() = ({0:F1}, {1:F1})", GetItemRectMax().x, GetItemRectMax().y );
				BulletText( "GetItemRectSize() = ({0:F1}, {1:F1})", GetItemRectSize().x, GetItemRectSize().y );
				BulletText( "w/ Hovering Delay: None = {0}, Fast = {1}, Normal = {2}", hoveredDelayNone, hoveredDelayShort, hoveredDelayNormal );
				if ( _itemDisabled ) EndDisabled();
				TreePop();
			}

			if ( TreeNode( "Querying Window Status (Focused/Hovered etc.)" ) )
			{
				BulletText( "IsWindowFocused() = {0}", IsWindowFocused() );
				BulletText( "IsWindowFocused(_ChildWindows) = {0}", IsWindowFocused( ImGuiFocusedFlags.ChildWindows ) );
				BulletText( "IsWindowFocused(_RootWindow) = {0}", IsWindowFocused( ImGuiFocusedFlags.RootWindow ) );
				BulletText( "IsWindowFocused(_AnyWindow) = {0}", IsWindowFocused( ImGuiFocusedFlags.AnyWindow ) );
				BulletText( "IsWindowHovered() = {0}", IsWindowHovered() );
				BulletText( "IsWindowHovered(_ChildWindows) = {0}", IsWindowHovered( ImGuiHoveredFlags.ChildWindows ) );
				BulletText( "IsWindowHovered(_AnyWindow) = {0}", IsWindowHovered( ImGuiHoveredFlags.AnyWindow ) );
				BeginChild( "child", new Vector2( 0, 50 ), ImGuiChildFlags.Borders );
				Text( "This is another child window for testing the _ChildWindows flag." );
				EndChild();
				TreePop();
			}

			if ( _disableAll )
				EndDisabled();

			if ( TreeNode( "Disable block" ) )
			{
				Checkbox( "Disable entire section above", ref _disableAll );
				SameLine(); HelpMarker( "Demonstrate using BeginDisabled()/EndDisabled() across this section." );
				TreePop();
			}
		}
	}
}
