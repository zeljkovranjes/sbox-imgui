using Duccsoft.ImGui.Engine;

namespace Duccsoft.ImGui;

public static partial class ImGui
{
	#region ID stack
	public static void PushID( string strId )
	{
		var window = G.CurrentWindow;
		window.IDStack.Add( window.GetID( strId ) );
	}

	public static void PushID( int intId )
	{
		var window = G.CurrentWindow;
		window.IDStack.Add( window.GetID( intId ) );
	}

	/// <summary>Push an id derived from an object's hash code (equivalent of Dear ImGui's PushID(const void*)).</summary>
	public static void PushID( object obj ) => PushID( obj?.GetHashCode() ?? 0 );

	internal static void PushOverrideID( int id ) => G.CurrentWindow.IDStack.Add( id );

	public static void PopID()
	{
		var window = G.CurrentWindow;
		if ( window.IDStack.Count <= 1 )
		{
			Log.Warning( "ImGui: PopID() called too many times" );
			return;
		}
		window.IDStack.RemoveAt( window.IDStack.Count - 1 );
	}

	public static int GetID( string strId ) => G.CurrentWindow.GetID( strId );
	public static int GetID( int intId ) => G.CurrentWindow.GetID( intId );
	public static int GetID( object obj ) => G.CurrentWindow.GetID( obj?.GetHashCode() ?? 0 );
	#endregion

	#region Cursor / layout
	public static void Separator()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;
		var flags = window.DC.LayoutType == ImGuiLayoutType.Horizontal ? 0 : 1;
		SeparatorEx( flags == 1, 1.0f );
	}

	internal static void SeparatorEx( bool horizontal, float thickness )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		if ( !horizontal )
		{
			// Vertical separator, for menu bars and horizontal layouts.
			float y1 = window.DC.CursorPos.y;
			float y2 = window.DC.CursorPos.y + window.DC.CurrLineSize.y;
			var bbV = new ImRect( new Vector2( window.DC.CursorPos.x, y1 ), new Vector2( window.DC.CursorPos.x + thickness, y2 ) );
			ItemSize( new Vector2( thickness, 0.0f ) );
			if ( !ItemAdd( bbV, 0 ) )
				return;
			window.DrawList.AddRectFilled( bbV.Min, bbV.Max, GetColorU32Internal( ImGuiCol.Separator ) );
			return;
		}

		float x1 = window.DC.CursorPos.x;
		float x2 = window.WorkRect.Max.x;
		var columns = window.DC.CurrentColumns;
		if ( window.DC.CurrentTableIdx < 0 && columns is null )
			x1 = window.Pos.x + window.DC.Indent;

		float thicknessForLayout = thickness == 1.0f ? 0.0f : thickness;
		var bb = new ImRect( new Vector2( x1, window.DC.CursorPos.y ), new Vector2( x2, window.DC.CursorPos.y + thickness ) );
		ItemSize( new Vector2( 0.0f, thicknessForLayout ) );
		if ( ItemAdd( bb, 0 ) )
			window.DrawList.AddRectFilled( bb.Min, bb.Max, GetColorU32Internal( ImGuiCol.Separator ) );
	}

	public static void SameLine( float offsetFromStartX = 0.0f, float spacing = -1.0f )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;

		var dc = window.DC;
		if ( offsetFromStartX != 0.0f )
		{
			if ( spacing < 0.0f )
				spacing = 0.0f;
			dc.CursorPos = new Vector2( window.Pos.x - window.Scroll.x + offsetFromStartX + spacing + dc.GroupOffset + dc.ColumnsOffset, dc.CursorPosPrevLine.y );
		}
		else
		{
			if ( spacing < 0.0f )
				spacing = g.Style.ItemSpacing.x;
			dc.CursorPos = new Vector2( dc.CursorPosPrevLine.x + spacing, dc.CursorPosPrevLine.y );
		}
		dc.CurrLineSize = dc.PrevLineSize;
		dc.CurrLineTextBaseOffset = dc.PrevLineTextBaseOffset;
		dc.IsSameLine = true;
	}

	public static void NewLine()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;

		var backupLayoutType = window.DC.LayoutType;
		window.DC.LayoutType = ImGuiLayoutType.Vertical;
		window.DC.IsSameLine = false;
		if ( window.DC.CurrLineSize.y > 0.0f )
			ItemSize( new Vector2( 0, 0 ) );
		else
			ItemSize( new Vector2( 0.0f, g.FontSize ) );
		window.DC.LayoutType = backupLayoutType;
	}

	public static void Spacing()
	{
		var window = G.CurrentWindow;
		if ( window.SkipItems )
			return;
		ItemSize( new Vector2( 0, 0 ) );
	}

	public static void Dummy( Vector2 size )
	{
		var window = G.CurrentWindow;
		if ( window.SkipItems )
			return;
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + size );
		ItemSize( size );
		ItemAdd( bb, 0 );
	}

	public static void Indent( float indentW = 0.0f )
	{
		var g = G;
		var window = GetCurrentWindow();
		window.DC.Indent += indentW != 0.0f ? indentW : g.Style.IndentSpacing;
		window.DC.CursorPos = new Vector2( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset, window.DC.CursorPos.y );
	}

	public static void Unindent( float indentW = 0.0f )
	{
		var g = G;
		var window = GetCurrentWindow();
		window.DC.Indent -= indentW != 0.0f ? indentW : g.Style.IndentSpacing;
		window.DC.CursorPos = new Vector2( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset, window.DC.CursorPos.y );
	}

	public static void AlignTextToFramePadding()
	{
		var g = G;
		var window = G.CurrentWindow;
		if ( window.SkipItems )
			return;
		window.DC.CurrLineSize = new Vector2( window.DC.CurrLineSize.x, MathF.Max( window.DC.CurrLineSize.y, g.FontSize + g.Style.FramePadding.y * 2 ) );
		window.DC.CurrLineTextBaseOffset = MathF.Max( window.DC.CurrLineTextBaseOffset, g.Style.FramePadding.y );
	}

	public static Vector2 GetCursorScreenPos() => G.CurrentWindow.DC.CursorPos;

	public static void SetCursorScreenPos( Vector2 pos )
	{
		var window = G.CurrentWindow;
		window.DC.CursorPos = pos;
		window.DC.IsSetPos = true;
	}

	public static Vector2 GetCursorPos()
	{
		var window = G.CurrentWindow;
		return window.DC.CursorPos - window.Pos + window.Scroll;
	}

	public static float GetCursorPosX() => GetCursorPos().x;
	public static float GetCursorPosY() => GetCursorPos().y;

	public static void SetCursorPos( Vector2 localPos )
	{
		var window = G.CurrentWindow;
		window.DC.CursorPos = window.Pos - window.Scroll + localPos;
		window.DC.IsSetPos = true;
	}

	public static void SetCursorPosX( float x )
	{
		var window = G.CurrentWindow;
		window.DC.CursorPos = new Vector2( window.Pos.x - window.Scroll.x + x, window.DC.CursorPos.y );
		window.DC.IsSetPos = true;
	}

	public static void SetCursorPosY( float y )
	{
		var window = G.CurrentWindow;
		window.DC.CursorPos = new Vector2( window.DC.CursorPos.x, window.Pos.y - window.Scroll.y + y );
		window.DC.IsSetPos = true;
	}

	public static Vector2 GetCursorStartPos()
	{
		var window = G.CurrentWindow;
		return window.DC.CursorStartPos - window.Pos;
	}

	/// <summary>Available space from the current cursor position to the edge of the content region.</summary>
	public static Vector2 GetContentRegionAvail()
	{
		var window = G.CurrentWindow;
		var mx = GetContentRegionMaxAbs();
		return mx - window.DC.CursorPos;
	}

	/// <summary>Legacy: content region max in window-local coordinates.</summary>
	public static Vector2 GetContentRegionMax()
	{
		var window = G.CurrentWindow;
		return GetContentRegionMaxAbs() - window.Pos;
	}

	public static Vector2 GetWindowContentRegionMin()
	{
		var window = G.CurrentWindow;
		return window.ContentRegionRect.Min - window.Pos;
	}

	public static Vector2 GetWindowContentRegionMax()
	{
		var window = G.CurrentWindow;
		return window.ContentRegionRect.Max - window.Pos;
	}

	public static float GetTextLineHeight() => G.FontSize;
	public static float GetTextLineHeightWithSpacing() => G.FontSize + G.Style.ItemSpacing.y;
	public static float GetFrameHeight() => G.FontSize + G.Style.FramePadding.y * 2.0f;
	public static float GetFrameHeightWithSpacing() => G.FontSize + G.Style.FramePadding.y * 2.0f + G.Style.ItemSpacing.y;
	public static float GetFontSize() => G.FontSize;

	public static void BeginGroup()
	{
		var g = G;
		var window = g.CurrentWindow;

		var groupData = new ImGuiGroupData
		{
			WindowID = window.ID,
			BackupCursorPos = window.DC.CursorPos,
			BackupCursorPosPrevLine = window.DC.CursorPosPrevLine,
			BackupCursorMaxPos = window.DC.CursorMaxPos,
			BackupIndent = window.DC.Indent,
			BackupGroupOffset = window.DC.GroupOffset,
			BackupCurrLineSize = window.DC.CurrLineSize,
			BackupCurrLineTextBaseOffset = window.DC.CurrLineTextBaseOffset,
			BackupActiveIdIsAlive = g.ActiveIdIsAlive,
			BackupHoveredIdIsAlive = g.HoveredId != 0,
			BackupIsSameLine = window.DC.IsSameLine,
			BackupActiveIdPreviousFrameIsAlive = g.ActiveIdPreviousFrameIsAlive,
			EmitItem = true,
		};
		g.GroupStack.Add( groupData );

		window.DC.GroupOffset = window.DC.CursorPos.x - window.Pos.x - window.DC.ColumnsOffset;
		window.DC.Indent = window.DC.GroupOffset;
		window.DC.CursorMaxPos = window.DC.CursorPos;
		window.DC.CurrLineSize = new Vector2( 0.0f, 0.0f );
	}

	public static void EndGroup()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( g.GroupStack.Count == 0 )
		{
			Log.Warning( "ImGui: EndGroup() called without BeginGroup()" );
			return;
		}

		var groupData = g.GroupStack[^1];
		var groupBb = new ImRect( groupData.BackupCursorPos, ImMax( ImMax( window.DC.CursorMaxPos, groupData.BackupCursorPos ), window.DC.CursorMaxPos ) );
		g.GroupStack.RemoveAt( g.GroupStack.Count - 1 );

		window.DC.CursorPos = groupData.BackupCursorPos;
		window.DC.CursorPosPrevLine = groupData.BackupCursorPosPrevLine;
		window.DC.CursorMaxPos = ImMax( groupData.BackupCursorMaxPos, groupBb.Max );
		window.DC.Indent = groupData.BackupIndent;
		window.DC.GroupOffset = groupData.BackupGroupOffset;
		window.DC.CurrLineSize = groupData.BackupCurrLineSize;
		window.DC.CurrLineTextBaseOffset = groupData.BackupCurrLineTextBaseOffset;
		window.DC.IsSameLine = groupData.BackupIsSameLine;

		if ( !groupData.EmitItem )
			return;

		window.DC.CurrLineTextBaseOffset = MathF.Max( window.DC.PrevLineTextBaseOffset, groupData.BackupCurrLineTextBaseOffset );
		ItemSize( groupBb.Size );
		ItemAdd( groupBb, 0 );

		// If the current ActiveId was declared within the boundary of our group, we copy it to LastItemId so IsItemActive()/IsItemDeactivated() can be used on the group.
		bool groupContainsCurrActiveId = groupData.BackupActiveIdIsAlive != g.ActiveId && g.ActiveIdIsAlive == g.ActiveId && g.ActiveId != 0;
		bool groupContainsPrevActiveId = !groupData.BackupActiveIdPreviousFrameIsAlive && g.ActiveIdPreviousFrameIsAlive;
		if ( groupContainsCurrActiveId )
			g.LastItemData.ID = g.ActiveId;
		else if ( groupContainsPrevActiveId )
			g.LastItemData.ID = g.ActiveIdPreviousFrame;
		g.LastItemData.Rect = groupBb;
		if ( groupContainsPrevActiveId && g.ActiveId != g.ActiveIdPreviousFrame )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Deactivated;
		if ( IsMouseHoveringRect( groupBb.Min, groupBb.Max ) )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredRect;
	}
	#endregion

	#region Parameter stacks (current window)
	public static void PushItemWidth( float itemWidth )
	{
		var g = G;
		var window = g.CurrentWindow;
		window.DC.ItemWidthStack.Add( window.DC.ItemWidth );
		window.DC.ItemWidth = itemWidth == 0.0f ? window.ItemWidthDefault : itemWidth;
		g.NextItemData.Flags &= ~ImGuiNextItemDataFlags.HasWidth;
	}

	internal static void PushMultiItemsWidths( int components, float wFull )
	{
		var g = G;
		var window = g.CurrentWindow;
		float wItemOne = MathF.Max( 1.0f, ImTrunc( (wFull - g.Style.ItemInnerSpacing.x * (components - 1)) / components ) );
		float wItemLast = MathF.Max( 1.0f, ImTrunc( wFull - (wItemOne + g.Style.ItemInnerSpacing.x) * (components - 1) ) );
		window.DC.ItemWidthStack.Add( window.DC.ItemWidth );
		window.DC.ItemWidthStack.Add( wItemLast );
		for ( int i = 0; i < components - 2; i++ )
			window.DC.ItemWidthStack.Add( wItemOne );
		window.DC.ItemWidth = components == 1 ? wItemLast : wItemOne;
		g.NextItemData.Flags &= ~ImGuiNextItemDataFlags.HasWidth;
	}

	public static void PopItemWidth()
	{
		var window = G.CurrentWindow;
		if ( window.DC.ItemWidthStack.Count == 0 )
			return;
		window.DC.ItemWidth = window.DC.ItemWidthStack[^1];
		window.DC.ItemWidthStack.RemoveAt( window.DC.ItemWidthStack.Count - 1 );
	}

	public static void SetNextItemWidth( float itemWidth )
	{
		var g = G;
		g.NextItemData.Flags |= ImGuiNextItemDataFlags.HasWidth;
		g.NextItemData.Width = itemWidth;
	}

	public static void PushTextWrapPos( float wrapLocalPosX = 0.0f )
	{
		var window = G.CurrentWindow;
		window.DC.TextWrapPosStack.Add( window.DC.TextWrapPos );
		window.DC.TextWrapPos = wrapLocalPosX;
	}

	public static void PopTextWrapPos()
	{
		var window = G.CurrentWindow;
		if ( window.DC.TextWrapPosStack.Count == 0 )
			return;
		window.DC.TextWrapPos = window.DC.TextWrapPosStack[^1];
		window.DC.TextWrapPosStack.RemoveAt( window.DC.TextWrapPosStack.Count - 1 );
	}

	public static void PushItemFlag( ImGuiItemFlags option, bool enabled )
	{
		var g = G;
		var itemFlags = g.CurrentItemFlags;
		if ( enabled )
			itemFlags |= option;
		else
			itemFlags &= ~option;
		g.ItemFlagsStack.Add( g.CurrentItemFlags );
		g.CurrentItemFlags = itemFlags;
	}

	public static void PopItemFlag()
	{
		var g = G;
		if ( g.ItemFlagsStack.Count == 0 )
			return;
		g.CurrentItemFlags = g.ItemFlagsStack[^1];
		g.ItemFlagsStack.RemoveAt( g.ItemFlagsStack.Count - 1 );
	}

	public static void PushTabStop( bool tabStop ) => PushItemFlag( ImGuiItemFlags.NoTabStop, !tabStop );
	public static void PopTabStop() => PopItemFlag();
	public static void PushButtonRepeat( bool repeat ) => PushItemFlag( ImGuiItemFlags.ButtonRepeat, repeat );
	public static void PopButtonRepeat() => PopItemFlag();

	/// <summary>Disable all user interactions and dim items visuals until EndDisabled().</summary>
	public static void BeginDisabled( bool disabled = true )
	{
		var g = G;
		bool wasDisabled = (g.CurrentItemFlags & ImGuiItemFlags.Disabled) != 0;
		if ( !wasDisabled && disabled )
		{
			g.DisabledAlphaBackup = g.Style.Alpha;
			g.Style.Alpha *= g.Style.DisabledAlpha;
		}
		if ( wasDisabled || disabled )
			g.CurrentItemFlags |= ImGuiItemFlags.Disabled;
		g.ItemFlagsStack.Add( wasDisabled ? g.CurrentItemFlags : (g.CurrentItemFlags & ~ImGuiItemFlags.Disabled) );
		g.DisabledStackSize++;
	}

	public static void EndDisabled()
	{
		var g = G;
		if ( g.DisabledStackSize <= 0 )
			return;
		g.DisabledStackSize--;
		bool wasDisabled = (g.CurrentItemFlags & ImGuiItemFlags.Disabled) != 0;
		PopItemFlag();
		if ( wasDisabled && (g.CurrentItemFlags & ImGuiItemFlags.Disabled) == 0 )
			g.Style.Alpha = g.DisabledAlphaBackup;
	}

	public static void PushClipRect( Vector2 clipRectMin, Vector2 clipRectMax, bool intersectWithCurrentClipRect )
	{
		var window = G.CurrentWindow;
		window.DrawList.PushClipRect( clipRectMin, clipRectMax, intersectWithCurrentClipRect );
		window.ClipRect = window.DrawList.CurrentClipRect;
	}

	public static void PopClipRect()
	{
		var window = G.CurrentWindow;
		window.DrawList.PopClipRect();
		window.ClipRect = window.DrawList.CurrentClipRect;
	}

	public static bool IsRectVisible( Vector2 size )
	{
		var window = G.CurrentWindow;
		return window.ClipRect.Overlaps( new ImRect( window.DC.CursorPos, window.DC.CursorPos + size ) );
	}

	public static bool IsRectVisible( Vector2 rectMin, Vector2 rectMax ) => G.CurrentWindow.ClipRect.Overlaps( new ImRect( rectMin, rectMax ) );
	#endregion

	#region Item queries
	public static bool IsItemHovered( ImGuiHoveredFlags flags = ImGuiHoveredFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;

		if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredRect) == 0 )
			return false;

		if ( (flags & ImGuiHoveredFlags.ForTooltip) != 0 )
			flags |= g.Style.HoverFlagsForTooltipMouse;

		if ( (flags & ImGuiHoveredFlags.AllowWhenOverlappedByWindow) == 0 )
		{
			if ( g.HoveredWindow != window && (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredWindow) == 0 )
				return false;
		}

		int id = g.LastItemData.ID;
		if ( (flags & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) == 0 )
			if ( g.ActiveId != 0 && g.ActiveId != id && !g.ActiveIdAllowOverlap && g.ActiveId != window.MoveId )
				return false;

		if ( !IsWindowContentHoverable( window, flags ) && (g.LastItemData.InFlags & ImGuiItemFlags.NoWindowHoverableCheck) == 0 )
			return false;

		if ( (g.LastItemData.InFlags & ImGuiItemFlags.Disabled) != 0 && (flags & ImGuiHoveredFlags.AllowWhenDisabled) == 0 )
			return false;

		if ( id == window.MoveId && window.WriteAccessed )
			return false;

		// Overlapping items: only the one hovered last frame (or allowing overlap) reports hovered.
		if ( (flags & ImGuiHoveredFlags.AllowWhenOverlappedByItem) == 0 && id != 0 && g.HoveredIdPreviousFrame != 0 && g.HoveredIdPreviousFrame != id && g.HoveredId != id )
			if ( (g.LastItemData.InFlags & ImGuiItemFlags.AllowOverlap) != 0 )
				return false;

		// Delays (tooltips)
		if ( (flags & (ImGuiHoveredFlags.DelayShort | ImGuiHoveredFlags.DelayNormal | ImGuiHoveredFlags.Stationary)) != 0 )
		{
			int hoverDelayId = id != 0 ? id : window.GetIDFromRectangle( g.LastItemData.Rect );
			if ( (flags & ImGuiHoveredFlags.NoSharedDelay) != 0 && g.HoverItemDelayIdPreviousFrame != hoverDelayId )
				g.HoverItemDelayTimer = 0.0f;
			g.HoverItemDelayId = hoverDelayId;

			if ( (flags & ImGuiHoveredFlags.Stationary) != 0 && g.HoverItemUnlockedStationaryId != hoverDelayId )
			{
				if ( g.MouseStationaryTimer < g.Style.HoverStationaryDelay )
					return false;
				g.HoverItemUnlockedStationaryId = hoverDelayId;
			}

			float delay = (flags & ImGuiHoveredFlags.DelayNormal) != 0 ? g.Style.HoverDelayNormal : (flags & ImGuiHoveredFlags.DelayShort) != 0 ? g.Style.HoverDelayShort : 0.0f;
			if ( g.HoverItemDelayTimer < delay )
				return false;
		}

		return true;
	}

	public static bool IsItemActive()
	{
		var g = G;
		return g.ActiveId != 0 && g.ActiveId == g.LastItemData.ID;
	}

	public static bool IsItemActivated()
	{
		var g = G;
		return g.ActiveId != 0 && g.ActiveId == g.LastItemData.ID && g.ActiveIdPreviousFrame != g.LastItemData.ID;
	}

	public static bool IsItemDeactivated()
	{
		var g = G;
		if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HasDeactivated) != 0 )
			return (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.Deactivated) != 0;
		return g.ActiveIdPreviousFrame == g.LastItemData.ID && g.ActiveIdPreviousFrame != 0 && g.ActiveId != g.LastItemData.ID;
	}

	public static bool IsItemDeactivatedAfterEdit()
	{
		var g = G;
		return IsItemDeactivated() && (g.ActiveIdPreviousFrameHasBeenEditedBefore || (g.ActiveId == 0 && g.ActiveIdHasBeenEditedBefore));
	}

	public static bool IsItemFocused()
	{
		var g = G;
		return g.NavId != 0 && g.NavId == g.LastItemData.ID && g.NavWindow == g.CurrentWindow;
	}

	public static bool IsItemClicked( ImGuiMouseButton mouseButton = ImGuiMouseButton.Left )
	{
		return IsMouseClicked( mouseButton ) && IsItemHovered( ImGuiHoveredFlags.None );
	}

	public static bool IsItemVisible() => (G.LastItemData.StatusFlags & ImGuiItemStatusFlags.Visible) != 0;
	public static bool IsItemEdited() => (G.LastItemData.StatusFlags & ImGuiItemStatusFlags.Edited) != 0;
	public static bool IsItemToggledOpen() => (G.LastItemData.StatusFlags & ImGuiItemStatusFlags.ToggledOpen) != 0;
	public static bool IsItemToggledSelection() => (G.LastItemData.StatusFlags & ImGuiItemStatusFlags.ToggledSelection) != 0;
	public static bool IsAnyItemHovered() => G.HoveredId != 0 || G.HoveredIdPreviousFrame != 0;
	public static bool IsAnyItemActive() => G.ActiveId != 0;
	public static bool IsAnyItemFocused() => G.NavId != 0;
	public static int GetItemID() => G.LastItemData.ID;
	public static Vector2 GetItemRectMin() => G.LastItemData.Rect.Min;
	public static Vector2 GetItemRectMax() => G.LastItemData.Rect.Max;
	public static Vector2 GetItemRectSize() => G.LastItemData.Rect.Size;

	/// <summary>Allow the next item to be overlapped by a subsequent item.</summary>
	public static void SetNextItemAllowOverlap()
	{
		G.NextItemData.ItemFlags |= ImGuiItemFlags.AllowOverlap;
	}

	/// <summary>Make the last item the default focused item of a newly appearing window.</summary>
	public static void SetItemDefaultFocus()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.Appearing )
		{
			g.NavId = g.LastItemData.ID;
			ScrollToRect( window, g.LastItemData.Rect );
		}
	}

	[Obsolete( "Use SetNextItemAllowOverlap() before the item" )]
	public static void SetItemAllowOverlap()
	{
		var g = G;
		int id = g.LastItemData.ID;
		if ( g.HoveredId == id )
			g.HoveredIdAllowOverlap = true;
		if ( g.ActiveId == id )
			g.ActiveIdAllowOverlap = true;
	}
	#endregion
}
