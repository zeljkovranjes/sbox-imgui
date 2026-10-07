using Duccsoft.ImGui.Engine;

namespace Duccsoft.ImGui;

public static partial class ImGui
{
	#region Combo
	private static float CalcMaxPopupHeightFromItemCount( int itemsCount )
	{
		var g = G;
		if ( itemsCount <= 0 )
			return float.MaxValue;
		return (g.FontSize + g.Style.ItemSpacing.y) * itemsCount - g.Style.ItemSpacing.y + g.Style.WindowPadding.y * 2;
	}

	/// <summary>Begin a combo box (dropdown). Only call EndCombo() if this returns true.</summary>
	public static bool BeginCombo( string label, string previewValue, ImGuiComboFlags flags = ImGuiComboFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();

		var backupNextWindowDataFlags = g.NextWindowData.Flags;
		g.NextWindowData.ClearFlags();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );

		float arrowSize = (flags & ImGuiComboFlags.NoArrowButton) != 0 ? 0.0f : GetFrameHeight();
		var labelSize = CalcTextSize( label, true );
		float previewWidth = (flags & ImGuiComboFlags.WidthFitPreview) != 0 && previewValue is not null ? CalcTextSize( previewValue, true ).x : 0.0f;
		float w = (flags & ImGuiComboFlags.NoPreview) != 0 ? arrowSize : ((flags & ImGuiComboFlags.WidthFitPreview) != 0 ? arrowSize + previewWidth + style.FramePadding.x * 2.0f : CalcItemWidth());
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + new Vector2( w, labelSize.y + style.FramePadding.y * 2.0f ) );
		var totalBb = new ImRect( bb.Min, bb.Max + new Vector2( labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f, 0.0f ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id, bb ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out _ );
		int popupId = ImHashStr( "##ComboPopup", id );
		bool popupOpen = IsPopupOpen( popupId, ImGuiPopupFlags.None );
		if ( pressed && !popupOpen )
		{
			OpenPopupEx( popupId );
			popupOpen = true;
		}

		var frameCol = GetColorU32Internal( hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg );
		float valueX2 = MathF.Max( bb.Min.x, bb.Max.x - arrowSize );
		if ( (flags & ImGuiComboFlags.NoPreview) == 0 )
			window.DrawList.AddRectFilled( bb.Min, new Vector2( valueX2, bb.Max.y ), frameCol, style.FrameRounding, (flags & ImGuiComboFlags.NoArrowButton) != 0 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersLeft );
		if ( (flags & ImGuiComboFlags.NoArrowButton) == 0 )
		{
			var bgCol = GetColorU32Internal( popupOpen || hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
			var textCol = GetColorU32Internal( ImGuiCol.Text );
			window.DrawList.AddRectFilled( new Vector2( valueX2, bb.Min.y ), bb.Max, bgCol, style.FrameRounding, w <= arrowSize ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersRight );
			if ( valueX2 + arrowSize - style.FramePadding.x <= bb.Max.x )
				RenderArrow( window.DrawList, new Vector2( valueX2 + style.FramePadding.y, bb.Min.y + style.FramePadding.y ), textCol, ImGuiDir.Down, 1.0f );
		}
		RenderFrameBorder( bb.Min, bb.Max, style.FrameRounding );

		if ( previewValue is not null && (flags & ImGuiComboFlags.NoPreview) == 0 )
			RenderTextClipped( bb.Min + style.FramePadding, new Vector2( valueX2, bb.Max.y ), previewValue, null, Vector2.Zero );
		if ( labelSize.x > 0 )
			RenderText( new Vector2( bb.Max.x + style.ItemInnerSpacing.x, bb.Min.y + style.FramePadding.y ), label );

		if ( !popupOpen )
			return false;

		g.NextWindowData.Flags = backupNextWindowDataFlags;
		return BeginComboPopup( popupId, bb, flags );
	}

	internal static bool BeginComboPopup( int popupId, ImRect bb, ImGuiComboFlags flags )
	{
		var g = G;
		if ( !IsPopupOpen( popupId, ImGuiPopupFlags.None ) )
		{
			g.NextWindowData.ClearFlags();
			return false;
		}

		float w = bb.Width;
		if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSizeConstraint) != 0 )
		{
			g.NextWindowData.SizeConstraintRect.Min.x = MathF.Max( g.NextWindowData.SizeConstraintRect.Min.x, w );
		}
		else
		{
			if ( (flags & ImGuiComboFlags.HeightMask_) == 0 )
				flags |= ImGuiComboFlags.HeightRegular;
			int popupMaxHeightInItems = -1;
			if ( (flags & ImGuiComboFlags.HeightRegular) != 0 ) popupMaxHeightInItems = 8;
			else if ( (flags & ImGuiComboFlags.HeightSmall) != 0 ) popupMaxHeightInItems = 4;
			else if ( (flags & ImGuiComboFlags.HeightLarge) != 0 ) popupMaxHeightInItems = 20;
			var constraintMin = new Vector2( 0.0f, 0.0f );
			var constraintMax = new Vector2( float.MaxValue, float.MaxValue );
			if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSize) == 0 || g.NextWindowData.SizeVal.x <= 0.0f )
				constraintMin.x = w;
			if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSize) == 0 || g.NextWindowData.SizeVal.y <= 0.0f )
				constraintMax.y = CalcMaxPopupHeightFromItemCount( popupMaxHeightInItems );
			SetNextWindowSizeConstraints( constraintMin, constraintMax );
		}

		string name = $"##Combo_{g.BeginComboDepth:D2}";
		var popupWindow = FindWindowByName( name );
		if ( popupWindow is not null && popupWindow.WasActive )
		{
			var sizeExpected = CalcWindowNextAutoFitSize( popupWindow );
			popupWindow.AutoPosLastDirection = (flags & ImGuiComboFlags.PopupAlignLeft) != 0 ? ImGuiDir.Left : ImGuiDir.Down;
			var rOuter = GetPopupAllowedExtentRect();
			var pos = FindBestWindowPosForPopupEx( bb.BL, sizeExpected, ref popupWindow.AutoPosLastDirection, rOuter, bb, ImGuiPopupPositionPolicy.ComboBox );
			SetNextWindowPos( pos );
		}
		else
		{
			SetNextWindowPos( bb.BL );
		}

		var windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove;
		PushStyleVarX( ImGuiStyleVar.WindowPadding, g.Style.FramePadding.x );
		bool ret = Begin( name, windowFlags );
		PopStyleVar();
		if ( !ret )
		{
			EndPopup();
			return false;
		}
		g.BeginComboDepth++;
		return true;
	}

	/// <summary>Only call EndCombo() if BeginCombo() returns true!</summary>
	public static void EndCombo()
	{
		var g = G;
		g.BeginComboDepth--;
		EndPopup();
	}

	public static bool Combo( string label, ref int currentItem, string[] items, int popupMaxHeightInItems = -1 )
	{
		var g = G;
		string previewValue = currentItem >= 0 && currentItem < items.Length ? items[currentItem] : null;

		if ( popupMaxHeightInItems != -1 && (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSizeConstraint) == 0 )
			SetNextWindowSizeConstraints( Vector2.Zero, new Vector2( float.MaxValue, CalcMaxPopupHeightFromItemCount( popupMaxHeightInItems ) ) );

		if ( !BeginCombo( label, previewValue ) )
			return false;

		bool valueChanged = false;
		for ( int i = 0; i < items.Length; i++ )
		{
			PushID( i );
			bool itemSelected = i == currentItem;
			if ( Selectable( items[i] ?? "*Unknown item*", itemSelected ) && currentItem != i )
			{
				valueChanged = true;
				currentItem = i;
			}
			if ( itemSelected )
				SetItemDefaultFocus();
			PopID();
		}

		EndCombo();
		if ( valueChanged )
			MarkItemEdited( g.LastItemData.ID );
		return valueChanged;
	}

	/// <summary>Combo with items separated by '\0' inside the string, e.g. "One\0Two\0Three\0".</summary>
	public static bool Combo( string label, ref int currentItem, string itemsSeparatedByZeros, int popupMaxHeightInItems = -1 )
	{
		var items = itemsSeparatedByZeros.Split( '\0' ).Where( x => x.Length > 0 ).ToArray();
		return Combo( label, ref currentItem, items, popupMaxHeightInItems );
	}

	public static bool Combo( string label, ref int currentItem, Func<int, string> getter, int itemsCount, int popupMaxHeightInItems = -1 )
	{
		var items = new string[itemsCount];
		for ( int i = 0; i < itemsCount; i++ )
			items[i] = getter( i );
		return Combo( label, ref currentItem, items, popupMaxHeightInItems );
	}

	/// <summary>Combo for any enum type.</summary>
	public static bool Combo<T>( string label, ref T value, int popupMaxHeightInItems = -1 ) where T : struct, Enum
	{
		var names = Enum.GetNames( typeof( T ) );
		var values = (T[])Enum.GetValues( typeof( T ) );
		int current = Array.IndexOf( values, value );
		if ( Combo( label, ref current, names, popupMaxHeightInItems ) && current >= 0 )
		{
			value = values[current];
			return true;
		}
		return false;
	}
	#endregion

	#region Menus
	/// <summary>Append to the menu-bar of the current window (requires ImGuiWindowFlags.MenuBar flag set on the parent window).</summary>
	public static bool BeginMenuBar()
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		if ( (window.Flags & ImGuiWindowFlags.MenuBar) == 0 )
			return false;

		BeginGroup();
		PushID( "##menubar" );

		var barRect = window.MenuBarRect();
		var clipRect = new ImRect(
			MathF.Round( barRect.Min.x + window.WindowBorderSize ),
			MathF.Round( barRect.Min.y + window.WindowBorderSize ),
			MathF.Round( MathF.Max( barRect.Min.x, barRect.Max.x - MathF.Max( window.WindowRounding, window.WindowBorderSize ) ) ),
			MathF.Round( barRect.Max.y ) );
		clipRect.ClipWith( window.OuterRectClipped );
		PushClipRect( clipRect.Min, clipRect.Max, false );

		window.DC.CursorPos = window.DC.CursorMaxPos = new Vector2( barRect.Min.x + window.DC.MenuBarOffset.x, barRect.Min.y + window.DC.MenuBarOffset.y );
		window.DC.LayoutType = ImGuiLayoutType.Horizontal;
		window.DC.IsSameLine = false;
		window.DC.MenuBarAppending = 1;
		AlignTextToFramePadding();
		return true;
	}

	/// <summary>Only call EndMenuBar() if BeginMenuBar() returns true!</summary>
	public static void EndMenuBar()
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		PopClipRect();
		PopID();
		window.DC.MenuBarOffset = new Vector2( window.DC.CursorPos.x - window.Pos.x, window.DC.MenuBarOffset.y );

		var groupData = g.GroupStack[^1];
		groupData.EmitItem = false;
		var restoreCursorMaxPos = groupData.BackupCursorMaxPos;
		window.DC.IdealMaxPos = new Vector2( MathF.Max( window.DC.IdealMaxPos.x, window.DC.CursorMaxPos.x - window.Scroll.x ), window.DC.IdealMaxPos.y );
		EndGroup();
		window.DC.LayoutType = ImGuiLayoutType.Vertical;
		window.DC.IsSameLine = false;
		window.DC.MenuBarAppending = 0;
		window.DC.CursorMaxPos = restoreCursorMaxPos;
	}

	/// <summary>Create and append to a full screen menu-bar at the top of the screen.</summary>
	public static bool BeginMainMenuBar()
	{
		var g = G;
		g.NextWindowData.MenuBarOffsetMinVal = new Vector2( g.Style.DisplaySafeAreaPadding.x, MathF.Max( g.Style.DisplaySafeAreaPadding.y - g.Style.FramePadding.y, 0.0f ) );
		var windowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;
		float height = GetFrameHeight();

		SetNextWindowPos( Vector2.Zero );
		SetNextWindowSize( new Vector2( g.IO.DisplaySize.x, height + g.NextWindowData.MenuBarOffsetMinVal.y ) );
		PushStyleVar( ImGuiStyleVar.WindowRounding, 0.0f );
		PushStyleVar( ImGuiStyleVar.WindowMinSize, Vector2.Zero );
		PushStyleVar( ImGuiStyleVar.WindowBorderSize, 0.0f );
		bool isOpen = Begin( "##MainMenuBar", windowFlags );
		PopStyleVar( 3 );
		g.NextWindowData.MenuBarOffsetMinVal = Vector2.Zero;

		if ( !isOpen )
		{
			End();
			return false;
		}

		BeginMenuBar();
		return true;
	}

	public static void EndMainMenuBar()
	{
		EndMenuBar();
		End();
	}

	private static bool IsRootOfOpenMenuSet()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( g.OpenPopupStack.Count <= g.BeginPopupStack.Count || (window.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
			return false;

		var upperPopup = g.OpenPopupStack[g.BeginPopupStack.Count];
		int layer = window.DC.MenuBarAppending > 0 ? 1 : 0;
		if ( layer != upperPopup.ParentNavLayer )
			return false;
		return upperPopup.Window is not null && (upperPopup.Window.Flags & ImGuiWindowFlags.ChildMenu) != 0 && IsWindowChildOf( upperPopup.Window, window, true );
	}

	private static bool BeginPopupMenuEx( int id, string label, ImGuiWindowFlags extraWindowFlags )
	{
		var g = G;
		if ( !IsPopupOpen( id, ImGuiPopupFlags.None ) )
		{
			g.NextWindowData.ClearFlags();
			return false;
		}

		string name = $"{LabelText( label )}###Menu_{g.BeginMenuDepth:D2}";
		var flags = extraWindowFlags | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings;
		bool isOpen = Begin( name, flags | ImGuiWindowFlags.Popup );
		if ( !isOpen )
			EndPopup();
		return isOpen;
	}

	private static bool ImTriangleContainsPoint( Vector2 a, Vector2 b, Vector2 c, Vector2 p )
	{
		bool b1 = ((p.x - b.x) * (a.y - b.y) - (p.y - b.y) * (a.x - b.x)) < 0.0f;
		bool b2 = ((p.x - c.x) * (b.y - c.y) - (p.y - c.y) * (b.x - c.x)) < 0.0f;
		bool b3 = ((p.x - a.x) * (c.y - a.y) - (p.y - a.y) * (c.x - a.x)) < 0.0f;
		return (b1 == b2) && (b2 == b3);
	}

	/// <summary>Create a sub-menu entry. Only call EndMenu() if this returns true!</summary>
	public static bool BeginMenu( string label, bool enabled = true ) => BeginMenuEx( label, null, enabled );

	internal static bool BeginMenuEx( string label, string icon, bool enabled )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		bool menuIsOpen = IsPopupOpen( id, ImGuiPopupFlags.None );

		var windowFlags = ImGuiWindowFlags.ChildMenu | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNavFocus;
		if ( (window.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
			windowFlags |= ImGuiWindowFlags.ChildWindow;

		// Appending to a menu that was already submitted this frame.
		if ( g.MenusIdSubmittedThisFrame.Contains( id ) )
		{
			if ( menuIsOpen )
				menuIsOpen = BeginPopupMenuEx( id, label, windowFlags );
			else
				g.NextWindowData.ClearFlags();
			return menuIsOpen;
		}
		g.MenusIdSubmittedThisFrame.Add( id );

		var labelSize = CalcTextSize( label, true );

		bool menusetIsOpen = IsRootOfOpenMenuSet();
		if ( menusetIsOpen )
			PushItemFlag( ImGuiItemFlags.NoWindowHoverableCheck, true );

		Vector2 popupPos;
		var pos = window.DC.CursorPos;
		PushID( label );
		int selectableId = GetID( "" );
		if ( !enabled )
			BeginDisabled();
		var offsets = window.DC.MenuColumns;
		bool pressed;
		var selectableFlags = ImGuiSelectableFlags.NoHoldingActiveID | ImGuiSelectableFlags.SelectOnClick | ImGuiSelectableFlags.NoAutoClosePopups;
		if ( window.DC.LayoutType == ImGuiLayoutType.Horizontal )
		{
			popupPos = new Vector2( pos.x - 1.0f - ImTrunc( style.ItemSpacing.x * 0.5f ), pos.y - style.FramePadding.y + window.MenuBarHeight );
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x + ImTrunc( style.ItemSpacing.x * 0.5f ), window.DC.CursorPos.y );
			PushStyleVarX( ImGuiStyleVar.ItemSpacing, style.ItemSpacing.x * 2.0f );
			float w = labelSize.x;
			var textPos = new Vector2( window.DC.CursorPos.x + offsets.OffsetLabel, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
			pressed = Selectable( "", menuIsOpen, selectableFlags, new Vector2( w, labelSize.y ) );
			RenderText( textPos, label );
			PopStyleVar();
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x + ImTrunc( style.ItemSpacing.x * (-1.0f + 0.5f) ), window.DC.CursorPos.y );
		}
		else
		{
			popupPos = new Vector2( pos.x, pos.y - style.WindowPadding.y );
			float iconW = !string.IsNullOrEmpty( icon ) ? CalcTextSize( icon ).x : 0.0f;
			float checkmarkW = ImTrunc( g.FontSize * 1.20f );
			float minW = offsets.DeclColumns( iconW, labelSize.x, 0.0f, checkmarkW );
			float extraW = MathF.Max( 0.0f, GetContentRegionAvail().x - minW );
			var textPos = new Vector2( window.DC.CursorPos.x + offsets.OffsetLabel, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
			pressed = Selectable( "", menuIsOpen, selectableFlags | ImGuiSelectableFlags.SpanAvailWidth, new Vector2( minW, labelSize.y ) );
			RenderText( textPos, label );
			if ( iconW > 0.0f )
				RenderText( pos + new Vector2( offsets.OffsetIcon, 0.0f ), icon );
			RenderArrow( window.DrawList, pos + new Vector2( offsets.OffsetMark + extraW + g.FontSize * 0.30f, 0.0f ), GetColorU32Internal( ImGuiCol.Text ), ImGuiDir.Right );
		}
		if ( !enabled )
			EndDisabled();

		bool hovered = g.HoveredId == selectableId && enabled;
		if ( menusetIsOpen )
			PopItemFlag();

		bool wantOpen = false;
		bool wantClose = false;
		if ( window.DC.LayoutType == ImGuiLayoutType.Vertical )
		{
			bool movingTowardChildMenu = false;
			var childPopup = g.BeginPopupStack.Count < g.OpenPopupStack.Count ? g.OpenPopupStack[g.BeginPopupStack.Count] : null;
			var childMenuWindow = childPopup?.Window is not null && childPopup.Window.ParentWindow == window ? childPopup.Window : null;
			if ( g.HoveredWindow == window && childMenuWindow is not null )
			{
				float refUnit = g.FontSize;
				float childDir = window.Pos.x < childMenuWindow.Pos.x ? 1.0f : -1.0f;
				var nextWindowRect = childMenuWindow.Rect();
				var ta = g.IO.MousePos - g.IO.MouseDelta;
				var tb = childDir > 0.0f ? nextWindowRect.TL : nextWindowRect.TR;
				var tc = childDir > 0.0f ? nextWindowRect.BL : nextWindowRect.BR;
				float padFarmostH = Math.Clamp( MathF.Abs( ta.x - tb.x ) * 0.30f, refUnit * 0.5f, refUnit * 2.5f );
				ta.x += childDir * -0.5f;
				tb.x += childDir * refUnit;
				tc.x += childDir * refUnit;
				tb.y = ta.y + MathF.Max( (tb.y - padFarmostH) - ta.y, -refUnit * 8.0f );
				tc.y = ta.y + MathF.Min( (tc.y + padFarmostH) - ta.y, +refUnit * 8.0f );
				movingTowardChildMenu = ImTriangleContainsPoint( ta, tb, tc, g.IO.MousePos );
			}

			if ( menuIsOpen && !hovered && g.HoveredWindow == window && !movingTowardChildMenu && g.ActiveId == 0 )
				wantClose = true;

			if ( !menuIsOpen && pressed )
				wantOpen = true;
			else if ( !menuIsOpen && hovered && !movingTowardChildMenu )
				wantOpen = true;
			else if ( !menuIsOpen && hovered && g.HoveredIdTimer >= 0.30f && g.MouseStationaryTimer >= 0.30f )
				wantOpen = true;
		}
		else
		{
			if ( menuIsOpen && pressed && menusetIsOpen )
			{
				wantClose = true;
				wantOpen = menuIsOpen = false;
			}
			else if ( pressed || (hovered && menusetIsOpen && !menuIsOpen) )
			{
				wantOpen = true;
			}
		}

		if ( !enabled )
			wantClose = true;
		if ( wantClose && IsPopupOpen( id, ImGuiPopupFlags.None ) )
			ClosePopupToLevel( g.BeginPopupStack.Count, true );

		PopID();

		if ( wantOpen && !menuIsOpen && g.OpenPopupStack.Count > g.BeginPopupStack.Count )
		{
			// Don't reopen/recycle the same menu level in the same frame: close the other menu first and yield a frame.
			OpenPopupEx( id );
		}
		else if ( wantOpen )
		{
			menuIsOpen = true;
			OpenPopupEx( id );
		}

		if ( menuIsOpen )
		{
			var lastItemInParent = g.LastItemData;
			SetNextWindowPos( popupPos );
			PushStyleVar( ImGuiStyleVar.ChildRounding, style.PopupRounding );
			menuIsOpen = BeginPopupMenuEx( id, label, windowFlags );
			PopStyleVar();
			if ( menuIsOpen )
			{
				g.LastItemData = lastItemInParent;
				if ( g.HoveredWindow == window )
					g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredWindow;
			}
		}
		else
		{
			g.NextWindowData.ClearFlags();
		}

		return menuIsOpen;
	}

	/// <summary>Only call EndMenu() if BeginMenu() returns true!</summary>
	public static void EndMenu()
	{
		EndPopup();
	}

	/// <summary>Return true when activated. Shortcuts are displayed for convenience but not processed.</summary>
	public static bool MenuItem( string label, string shortcut = null, bool selected = false, bool enabled = true )
		=> MenuItemEx( label, null, shortcut, selected, enabled );

	/// <summary>Return true when activated, and toggle <paramref name="selected"/>.</summary>
	public static bool MenuItem( string label, string shortcut, ref bool selected, bool enabled = true )
	{
		if ( MenuItemEx( label, null, shortcut, selected, enabled ) )
		{
			selected = !selected;
			return true;
		}
		return false;
	}

	internal static bool MenuItemEx( string label, string icon, string shortcut, bool selected, bool enabled )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		var pos = window.DC.CursorPos;
		var labelSize = CalcTextSize( label, true );

		bool menusetIsOpen = IsRootOfOpenMenuSet();
		if ( menusetIsOpen )
			PushItemFlag( ImGuiItemFlags.NoWindowHoverableCheck, true );

		bool pressed;
		PushID( label );
		if ( !enabled )
			BeginDisabled();

		var selectableFlags = ImGuiSelectableFlags.SelectOnRelease | ImGuiSelectableFlags.SetNavIdOnHover;
		var offsets = window.DC.MenuColumns;
		if ( window.DC.LayoutType == ImGuiLayoutType.Horizontal )
		{
			float w = labelSize.x;
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x + ImTrunc( style.ItemSpacing.x * 0.5f ), window.DC.CursorPos.y );
			var textPos = new Vector2( window.DC.CursorPos.x + offsets.OffsetLabel, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
			PushStyleVarX( ImGuiStyleVar.ItemSpacing, style.ItemSpacing.x * 2.0f );
			pressed = Selectable( "", selected, selectableFlags, new Vector2( w, 0.0f ) );
			PopStyleVar();
			if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.Visible) != 0 )
				RenderText( textPos, label );
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x + ImTrunc( style.ItemSpacing.x * (-1.0f + 0.5f) ), window.DC.CursorPos.y );
		}
		else
		{
			float iconW = !string.IsNullOrEmpty( icon ) ? CalcTextSize( icon ).x : 0.0f;
			float shortcutW = !string.IsNullOrEmpty( shortcut ) ? CalcTextSize( shortcut ).x : 0.0f;
			float checkmarkW = ImTrunc( g.FontSize * 1.20f );
			float minW = offsets.DeclColumns( iconW, labelSize.x, shortcutW, checkmarkW );
			float stretchW = MathF.Max( 0.0f, GetContentRegionAvail().x - minW );
			pressed = Selectable( "", false, selectableFlags | ImGuiSelectableFlags.SpanAvailWidth, new Vector2( minW, labelSize.y ) );
			if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.Visible) != 0 )
			{
				RenderText( pos + new Vector2( offsets.OffsetLabel, 0.0f ), label );
				if ( iconW > 0.0f )
					RenderText( pos + new Vector2( offsets.OffsetIcon, 0.0f ), icon );
				if ( shortcutW > 0.0f )
				{
					PushStyleColor( ImGuiCol.Text, style.Colors[(int)ImGuiCol.TextDisabled] );
					RenderText( pos + new Vector2( offsets.OffsetShortcut + stretchW, 0.0f ), shortcut, false );
					PopStyleColor();
				}
				if ( selected )
					RenderCheckMark( window.DrawList, pos + new Vector2( offsets.OffsetMark + stretchW + g.FontSize * 0.40f, g.FontSize * 0.134f * 0.5f ), GetColorU32Internal( ImGuiCol.Text ), g.FontSize * 0.866f );
			}
		}
		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Checkable | (selected ? ImGuiItemStatusFlags.Checked : ImGuiItemStatusFlags.None);
		if ( !enabled )
			EndDisabled();
		PopID();
		if ( menusetIsOpen )
			PopItemFlag();

		return pressed;
	}
	#endregion
}
