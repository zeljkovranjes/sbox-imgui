using Duccsoft.ImGui.Engine;

namespace Duccsoft.ImGui;

public static partial class ImGui
{
	private const float FLT_MIN_NORMAL = 1.17549435E-38f;
	private const float TABLE_RESIZE_HIT_HALF = 4.0f;

	internal static ImGuiTable CurrentTable
	{
		get
		{
			var g = G;
			var window = g?.CurrentWindow;
			if ( window is null )
				return null;
			int idx = window.DC.CurrentTableIdx;
			return idx >= 0 && idx < g.TablesStack.Count ? g.TablesStack[idx] : null;
		}
	}

	internal static void NewFrameTables()
	{
		var g = G;
		g.TablesStack.Clear();
		g.SplitDrawLists.Clear();
		g.TabBarStack.Clear();
	}

	#region Begin/End
	public static bool BeginTable( string strId, int columns, ImGuiTableFlags flags = ImGuiTableFlags.None, Vector2 outerSize = default, float innerWidth = 0.0f )
	{
		var g = G;
		var outerWindow = GetCurrentWindow();
		if ( outerWindow.SkipItems )
			return false;
		if ( columns <= 0 || columns > 512 )
		{
			Log.Warning( "ImGui: BeginTable() requires 1..512 columns" );
			return false;
		}

		int id = outerWindow.GetID( strId );
		bool useChildWindow = (flags & (ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY)) != 0;
		var availSize = GetContentRegionAvail();
		var actualOuterSize = CalcItemSize( outerSize, MathF.Max( availSize.x, 1.0f ), useChildWindow ? MathF.Max( availSize.y, 1.0f ) : 0.0f );
		var outerRect = new ImRect( outerWindow.DC.CursorPos, outerWindow.DC.CursorPos + actualOuterSize );

		if ( useChildWindow && IsClippedEx( outerRect, 0 ) )
		{
			ItemSize( outerRect );
			ItemAdd( outerRect, id );
			return false;
		}

		if ( !g.Tables.TryGetValue( id, out var table ) )
		{
			table = new ImGuiTable { ID = id };
			g.Tables[id] = table;
		}
		table.IsFirstFrame = table.ColumnsCount != columns;
		if ( table.ColumnsCount != columns )
		{
			table.Columns.Clear();
			for ( int i = 0; i < columns; i++ )
				table.Columns.Add( new ImGuiTableColumn { DisplayOrder = i } );
			table.ColumnsCount = columns;
		}

		// Flags fix-ups
		if ( (flags & ImGuiTableFlags.SizingMask_) == 0 )
			flags |= ((flags & ImGuiTableFlags.ScrollX) != 0 || (outerWindow.Flags & ImGuiWindowFlags.AlwaysAutoResize) != 0) ? ImGuiTableFlags.SizingFixedFit : ImGuiTableFlags.SizingStretchSame;
		if ( (flags & ImGuiTableFlags.ScrollX) == 0 && (flags & ImGuiTableFlags.ScrollY) == 0 )
		{
			// nothing
		}

		table.Flags = flags;
		table.OuterWindow = outerWindow;
		table.InnerWidth = innerWidth;
		table.IsLayoutLocked = false;
		table.IsInsideRow = false;
		table.DeclColumnsCount = 0;
		table.CurrentRow = -1;
		table.CurrentColumn = -1;
		table.RowBgColorCounter = 0;
		table.RowLines.Clear();
		table.HasHeaders = false;
		table.HeaderBottomY = float.MinValue;
		table.FreezeRowsRequest = table.FreezeColumnsRequest = 0;
		table.CellPaddingY = g.Style.CellPadding.y;
		table.ReorderColumn = -1;
		foreach ( var c in table.Columns )
		{
			c.ContentWidthPrev = c.ContentWidthThisFrame;
			c.ContentWidthThisFrame = 0f;
		}

		// Push table on the stack
		g.TablesStack.Add( table );
		int tableIdx = g.TablesStack.Count - 1;
		table.HostBackupCurrentTableIdx = outerWindow.DC.CurrentTableIdx;

		if ( useChildWindow )
		{
			var childFlags = (flags & ImGuiTableFlags.ScrollX) != 0 ? ImGuiWindowFlags.HorizontalScrollbar : ImGuiWindowFlags.None;
			BeginChildEx( null, ImHashStr( "##TableChild", id ), outerRect.Size, ImGuiChildFlags.None, childFlags | ImGuiWindowFlags.NoSavedSettings );
			table.InnerWindow = g.CurrentWindow;
			table.OuterRect = table.InnerWindow.Rect();
			table.InnerRect = table.InnerWindow.InnerRect;
			table.WorkRect = table.InnerWindow.WorkRect;
			table.InnerClipRect = table.InnerWindow.InnerClipRect;
			table.StartY = table.InnerWindow.DC.CursorPos.y;
		}
		else
		{
			table.InnerWindow = outerWindow;
			table.OuterRect = outerRect;
			table.InnerRect = outerRect;
			table.WorkRect = outerRect;
			var clip = outerWindow.ClipRect;
			table.InnerClipRect = new ImRect( MathF.Max( clip.Min.x, outerRect.Min.x ), clip.Min.y, MathF.Min( clip.Max.x, outerRect.Max.x ), clip.Max.y );
			table.StartY = outerRect.Min.y;
		}

		var inner = table.InnerWindow;
		table.InnerBackupCurrentTableIdx = inner.DC.CurrentTableIdx;
		inner.DC.CurrentTableIdx = tableIdx;
		if ( !useChildWindow )
			outerWindow.DC.CurrentTableIdx = tableIdx;

		table.HostBackupCursorMaxPos = inner.DC.CursorMaxPos;
		table.HostBackupWorkRect = inner.WorkRect;
		table.HostBackupParentWorkRect = inner.ParentWorkRect;
		table.HostBackupColumnsOffset = inner.DC.ColumnsOffset;
		table.HostBackupItemWidth = inner.DC.ItemWidth;
		table.HostBackupItemWidthStackCount = inner.DC.ItemWidthStack.Count;
		table.HostBackupCurrLineSize = inner.DC.CurrLineSize;
		table.HostBackupPrevLineSize = inner.DC.PrevLineSize;
		table.HostSkipItems = inner.SkipItems;
		inner.ParentWorkRect = table.WorkRect;

		table.RowLogicalY = table.StartY;
		table.RowPosY2 = table.StartY;
		table.FrozenBottomY = table.InnerClipRect.Min.y;
		table.ContextPopupId = ImHashStr( "##ContextMenu", id );

		PushOverrideID( id );

		// Channels: 0 = body bg, 1 = body content, 2 = frozen bg, 3 = frozen content.
		table.UsesChannels = !g.SplitDrawLists.Contains( inner.DrawList );
		if ( table.UsesChannels )
		{
			g.SplitDrawLists.Add( inner.DrawList );
			inner.DrawList.ChannelsSplit( 4 );
			inner.DrawList.ChannelsSetCurrent( 1 );
		}

		return true;
	}

	public static void EndTable()
	{
		var g = G;
		var table = CurrentTable;
		if ( table is null )
		{
			Log.Warning( "ImGui: EndTable() called without a matching BeginTable()" );
			return;
		}

		if ( table.IsInsideRow )
			TableEndRow( table );
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );

		var inner = table.InnerWindow;
		var outer = table.OuterWindow;
		bool useChild = inner != outer;

		float rowsEndY = table.RowPosY2;
		table.LastHeight = rowsEndY - table.StartY;

		// Context menu in body
		if ( (table.Flags & ImGuiTableFlags.ContextMenuInBody) != 0 && g.HoveredWindow == inner && IsMouseReleased( ImGuiMouseButton.Right ) && !IsAnyItemHovered() )
		{
			var bodyRect = new ImRect( table.ColumnsMinX, table.StartY, table.ColumnsMaxX, rowsEndY );
			if ( bodyRect.Contains( g.IO.MousePos ) )
			{
				table.ContextMenuColumn = table.HoveredColumnBody;
				OpenPopupEx( table.ContextPopupId );
			}
		}

		if ( table.UsesChannels )
		{
			inner.DrawList.ChannelsMerge();
			g.SplitDrawLists.Remove( inner.DrawList );
		}

		// Borders (drawn on top of everything)
		TableDrawBorders( table, rowsEndY );

		// Context menu
		if ( IsPopupOpen( table.ContextPopupId, ImGuiPopupFlags.None ) )
		{
			if ( BeginPopupEx( table.ContextPopupId, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings ) )
			{
				TableDrawDefaultContextMenu( table );
				EndPopup();
			}
		}

		PopID();

		// Restore host state
		inner.WorkRect = table.HostBackupWorkRect;
		inner.ParentWorkRect = table.HostBackupParentWorkRect;
		inner.DC.ColumnsOffset = table.HostBackupColumnsOffset;
		inner.DC.ItemWidth = table.HostBackupItemWidth;
		while ( inner.DC.ItemWidthStack.Count > table.HostBackupItemWidthStackCount )
			inner.DC.ItemWidthStack.RemoveAt( inner.DC.ItemWidthStack.Count - 1 );
		inner.DC.CurrLineSize = table.HostBackupCurrLineSize;
		inner.DC.PrevLineSize = table.HostBackupPrevLineSize;
		inner.SkipItems = table.HostSkipItems;
		inner.DC.CurrentTableIdx = table.InnerBackupCurrentTableIdx;
		inner.DC.IsSameLine = false;

		if ( useChild )
		{
			// Content size of the scrolling child: full columns width, logical rows height.
			inner.DC.CursorPos = new Vector2( inner.Pos.x + inner.DC.Indent, rowsEndY );
			inner.DC.CursorMaxPos = new Vector2(
				MathF.Max( table.HostBackupCursorMaxPos.x, table.WorkRect.Min.x + table.ColumnsTotalWidth ),
				MathF.Max( table.HostBackupCursorMaxPos.y, rowsEndY ) );
			inner.DC.CursorPosPrevLine = inner.DC.CursorPos;
			EndChild();
			outer.DC.CurrentTableIdx = table.HostBackupCurrentTableIdx;

			if ( (table.Flags & ImGuiTableFlags.BordersOuter) != 0 )
			{
				var r = table.OuterRect;
				var col = GetColorU32Internal( ImGuiCol.TableBorderStrong );
				if ( (table.Flags & ImGuiTableFlags.BordersOuterV) != 0 )
				{
					outer.DrawList.AddLine( r.TL, r.BL, col );
					outer.DrawList.AddLine( new Vector2( r.Max.x - 1, r.Min.y ), new Vector2( r.Max.x - 1, r.Max.y ), col );
				}
				if ( (table.Flags & ImGuiTableFlags.BordersOuterH) != 0 )
				{
					outer.DrawList.AddLine( r.TL, r.TR, col );
					outer.DrawList.AddLine( new Vector2( r.Min.x, r.Max.y - 1 ), new Vector2( r.Max.x, r.Max.y - 1 ), col );
				}
			}
		}
		else
		{
			table.OuterRect.Max.y = MathF.Max( table.OuterRect.Min.y, rowsEndY );
			outer.DC.CursorPos = table.OuterRect.Min;
			outer.DC.CursorPosPrevLine = table.OuterRect.Min;
			outer.DC.CursorMaxPos = table.HostBackupCursorMaxPos;
			ItemSize( table.OuterRect.Size );
			ItemAdd( table.OuterRect, 0 );
			outer.DC.CurrentTableIdx = table.HostBackupCurrentTableIdx;
		}

		if ( g.TablesStack.Count > 0 && g.TablesStack[^1] == table )
			g.TablesStack.RemoveAt( g.TablesStack.Count - 1 );
		table.IsFirstFrame = false;
	}
	#endregion

	#region Setup
	public static void TableSetupColumn( string label, ImGuiTableColumnFlags flags = ImGuiTableColumnFlags.None, float initWidthOrWeight = 0.0f, int userId = 0 )
	{
		var table = CurrentTable;
		if ( table is null )
		{
			Log.Warning( "ImGui: TableSetupColumn() called outside of a table" );
			return;
		}
		if ( table.IsLayoutLocked || table.DeclColumnsCount >= table.ColumnsCount )
		{
			Log.Warning( "ImGui: TableSetupColumn() called too late or too many times" );
			return;
		}

		var column = table.Columns[table.DeclColumnsCount++];
		bool flagsChanged = column.DeclFlags != flags;
		column.Name = label;
		column.UserID = userId;
		column.DeclFlags = flags;
		if ( table.IsFirstFrame || flagsChanged || column.InitWidthOrWeight != initWidthOrWeight )
		{
			column.InitWidthOrWeight = initWidthOrWeight;
			if ( table.IsFirstFrame )
			{
				column.IsUserEnabled = column.IsUserEnabledNextFrame = (flags & ImGuiTableColumnFlags.DefaultHide) == 0;
				if ( (flags & ImGuiTableColumnFlags.DefaultSort) != 0 && (table.Flags & ImGuiTableFlags.Sortable) != 0 )
				{
					column.SortOrder = 0;
					column.SortDirection = (flags & ImGuiTableColumnFlags.PreferSortDescending) != 0 ? ImGuiSortDirection.Descending : ImGuiSortDirection.Ascending;
					table.SortSpecsDirtyInternal = true;
				}
			}
		}
	}

	/// <summary>Lock columns/rows so they stay visible when scrolled.</summary>
	public static void TableSetupScrollFreeze( int cols, int rows )
	{
		var table = CurrentTable;
		if ( table is null )
			return;
		table.FreezeColumnsRequest = (table.Flags & ImGuiTableFlags.ScrollX) != 0 ? Math.Clamp( cols, 0, table.ColumnsCount ) : 0;
		table.FreezeRowsRequest = (table.Flags & ImGuiTableFlags.ScrollY) != 0 ? Math.Max( rows, 0 ) : 0;
	}
	#endregion

	#region Layout
	private static float TableGetMinColumnWidth() => MathF.Max( 1.0f, G.Style.FramePadding.x );

	private static void TableUpdateLayout( ImGuiTable table )
	{
		var g = G;
		var style = g.Style;
		table.IsLayoutLocked = true;
		var inner = table.InnerWindow;
		bool useChild = inner != table.OuterWindow;

		table.FreezeRows = useChild ? table.FreezeRowsRequest : 0;
		table.FreezeColumns = useChild ? table.FreezeColumnsRequest : 0;

		// Effective flags & enabled state
		var sizing = table.Flags & ImGuiTableFlags.SizingMask_;
		bool scrollXNoInner = (table.Flags & ImGuiTableFlags.ScrollX) != 0 && table.InnerWidth <= 0f;
		for ( int n = 0; n < table.ColumnsCount; n++ )
		{
			var c = table.Columns[n];
			if ( n >= table.DeclColumnsCount )
			{
				c.DeclFlags = ImGuiTableColumnFlags.None;
				c.Name ??= null;
			}
			var f = c.DeclFlags;
			if ( (f & ImGuiTableColumnFlags.WidthMask_) == 0 )
			{
				bool fixedPolicy = sizing == ImGuiTableFlags.SizingFixedFit || sizing == ImGuiTableFlags.SizingFixedSame || scrollXNoInner;
				f |= fixedPolicy ? ImGuiTableColumnFlags.WidthFixed : ImGuiTableColumnFlags.WidthStretch;
			}
			else if ( scrollXNoInner && (f & ImGuiTableColumnFlags.WidthStretch) != 0 )
			{
				f = (f & ~ImGuiTableColumnFlags.WidthMask_) | ImGuiTableColumnFlags.WidthFixed;
			}
			if ( (table.Flags & ImGuiTableFlags.Resizable) == 0 )
				f |= ImGuiTableColumnFlags.NoResize;
			c.Flags = f;

			if ( (table.Flags & ImGuiTableFlags.Hideable) == 0 && !c.IsUserEnabledNextFrame && c.IsUserEnabled )
				c.IsUserEnabledNextFrame = true;
			c.IsUserEnabled = c.IsUserEnabledNextFrame;
			c.IsEnabled = c.IsUserEnabled && (f & ImGuiTableColumnFlags.Disabled) == 0;
		}

		// Display order
		table.DisplayOrderToIndex.Clear();
		for ( int n = 0; n < table.ColumnsCount; n++ )
			table.DisplayOrderToIndex.Add( n );
		table.DisplayOrderToIndex.Sort( ( a, b ) =>
		{
			int r = table.Columns[a].DisplayOrder.CompareTo( table.Columns[b].DisplayOrder );
			return r != 0 ? r : a.CompareTo( b );
		} );
		for ( int d = 0; d < table.ColumnsCount; d++ )
		{
			table.Columns[table.DisplayOrderToIndex[d]].DisplayOrder = d;
			table.Columns[table.DisplayOrderToIndex[d]].DisplayIndex = d;
		}

		var enabled = new List<ImGuiTableColumn>();
		foreach ( int idx in table.DisplayOrderToIndex )
			if ( table.Columns[idx].IsEnabled )
				enabled.Add( table.Columns[idx] );

		// Padding
		float cp = style.CellPadding.x;
		bool padOuter = (table.Flags & ImGuiTableFlags.NoPadOuterX) != 0 ? false : (table.Flags & ImGuiTableFlags.PadOuterX) != 0 || (table.Flags & ImGuiTableFlags.BordersOuterV) != 0;
		bool padInner = (table.Flags & ImGuiTableFlags.NoPadInnerX) == 0;
		float minW = TableGetMinColumnWidth();

		var padL = new float[enabled.Count];
		var padR = new float[enabled.Count];
		float sumPads = 0f;
		for ( int k = 0; k < enabled.Count; k++ )
		{
			padL[k] = k == 0 ? (padOuter ? cp : 0f) : (padInner ? cp : 0f);
			padR[k] = k == enabled.Count - 1 ? (padOuter ? cp : 0f) : (padInner ? cp : 0f);
			sumPads += padL[k] + padR[k];
		}

		// Fixed widths
		float maxAutoFixed = 0f;
		foreach ( var c in enabled )
			if ( !c.IsStretch )
				maxAutoFixed = MathF.Max( maxAutoFixed, MathF.Max( c.ContentWidthPrev, minW ) );

		float sumFixed = 0f;
		float sumWeights = 0f;
		int stretchCount = 0;
		foreach ( var c in enabled )
		{
			float autoW = MathF.Max( c.ContentWidthPrev, minW );
			if ( !c.IsStretch )
			{
				float w;
				if ( c.WidthRequest > 0f )
					w = c.WidthRequest;
				else if ( c.InitWidthOrWeight > 0f )
					w = c.InitWidthOrWeight;
				else
					w = sizing == ImGuiTableFlags.SizingFixedSame ? maxAutoFixed : autoW;
				c.WidthGiven = MathF.Max( minW, ImTrunc( w ) );
				sumFixed += c.WidthGiven;
			}
			else
			{
				if ( c.StretchWeight <= 0f )
					c.StretchWeight = c.InitWidthOrWeight > 0f ? c.InitWidthOrWeight : (sizing == ImGuiTableFlags.SizingStretchProp ? autoW : 1.0f);
				sumWeights += c.StretchWeight;
				stretchCount++;
			}
		}

		float tableWidth = table.WorkRect.Width;
		if ( (table.Flags & ImGuiTableFlags.ScrollX) != 0 && table.InnerWidth > 0f )
			tableWidth = table.InnerWidth;

		if ( stretchCount > 0 )
		{
			float avail = MathF.Max( 0f, tableWidth - sumPads - sumFixed );
			float used = 0f;
			ImGuiTableColumn last = null;
			foreach ( var c in enabled )
			{
				if ( !c.IsStretch )
					continue;
				c.WidthGiven = MathF.Max( minW, ImTrunc( avail * c.StretchWeight / MathF.Max( sumWeights, 1e-6f ) ) );
				used += c.WidthGiven;
				last = c;
			}
			if ( last is not null )
				last.WidthGiven = MathF.Max( minW, last.WidthGiven + (avail - used) );
		}

		// Positions
		var inner0 = table.InnerWindow;
		float scrollX = useChild ? inner0.Scroll.x : 0f;
		float x = table.WorkRect.Min.x;
		table.FrozenRightX = float.MinValue;
		for ( int k = 0; k < enabled.Count; k++ )
		{
			var c = enabled[k];
			c.IsFrozen = k < table.FreezeColumns;
			float off = c.IsFrozen ? scrollX : 0f;
			c.MinX = x + off;
			c.WorkMinX = c.MinX + padL[k];
			c.WorkMaxX = c.WorkMinX + c.WidthGiven;
			c.MaxX = c.WorkMaxX + padR[k];
			c.ItemWidth = ImTrunc( MathF.Max( 1.0f, c.WidthGiven * 0.65f ) );
			x += padL[k] + c.WidthGiven + padR[k];
			if ( c.IsFrozen )
				table.FrozenRightX = MathF.Max( table.FrozenRightX, c.MaxX );
		}
		table.ColumnsTotalWidth = x - table.WorkRect.Min.x;

		// Fixed-only table that doesn't extend to the host width
		if ( stretchCount == 0 && !useChild && (table.Flags & ImGuiTableFlags.NoHostExtendX) != 0 )
		{
			table.OuterRect.Max.x = MathF.Min( table.OuterRect.Max.x, table.OuterRect.Min.x + table.ColumnsTotalWidth );
			table.WorkRect.Max.x = table.OuterRect.Max.x;
			table.InnerRect.Max.x = table.OuterRect.Max.x;
			table.InnerClipRect.Max.x = MathF.Min( table.InnerClipRect.Max.x, table.OuterRect.Max.x );
		}

		table.ColumnsMinX = enabled.Count > 0 ? enabled.Min( c => c.MinX ) : table.WorkRect.Min.x;
		table.ColumnsMaxX = enabled.Count > 0 ? enabled.Max( c => c.MaxX ) : table.WorkRect.Min.x;

		// Clipping per column
		var clip = table.InnerClipRect;
		foreach ( var c in table.Columns )
		{
			if ( !c.IsEnabled )
			{
				c.ClipMinX = c.ClipMaxX = clip.Min.x;
				c.IsVisibleX = false;
				continue;
			}
			float cmin = (c.Flags & ImGuiTableColumnFlags.NoClip) != 0 ? clip.Min.x : MathF.Max( c.MinX, clip.Min.x );
			float cmax = (c.Flags & ImGuiTableColumnFlags.NoClip) != 0 ? clip.Max.x : MathF.Min( c.MaxX, clip.Max.x );
			if ( !c.IsFrozen && table.FreezeColumns > 0 )
				cmin = MathF.Max( cmin, table.FrozenRightX );
			c.ClipMinX = cmin;
			c.ClipMaxX = MathF.Max( cmin, cmax );
			c.IsVisibleX = c.ClipMaxX > c.ClipMinX;
		}

		// Hovered column
		table.HoveredColumnBody = -1;
		float hoverMaxY = table.StartY + MathF.Max( table.LastHeight, g.FontSize );
		if ( g.HoveredWindow == inner && g.IO.MousePos.y >= table.StartY && g.IO.MousePos.y < hoverMaxY )
		{
			for ( int n = 0; n < table.ColumnsCount; n++ )
			{
				var c = table.Columns[n];
				if ( c.IsEnabled && g.IO.MousePos.x >= MathF.Max( c.MinX, c.ClipMinX ) && g.IO.MousePos.x < c.MaxX )
				{
					table.HoveredColumnBody = n;
					break;
				}
			}
		}

		TableUpdateBorders( table, enabled );
		TableUpdateSort( table );
	}

	private static void TableUpdateBorders( ImGuiTable table, List<ImGuiTableColumn> enabled )
	{
		var g = G;
		table.HoveredBorderColumn = -1;
		table.HeldBorderColumn = -1;
		if ( (table.Flags & ImGuiTableFlags.Resizable) == 0 || enabled.Count == 0 )
			return;

		var window = table.InnerWindow;
		bool useChild = window != table.OuterWindow;
		float y1 = useChild ? window.InnerRect.Min.y : table.StartY;
		float y2 = useChild ? window.InnerRect.Max.y : table.StartY + MathF.Max( table.LastHeight, g.FontSize + table.CellPaddingY * 2 );
		float minW = TableGetMinColumnWidth();
		float hw = MathF.Max( 2.0f, TABLE_RESIZE_HIT_HALF * g.AppliedStyleScale );

		for ( int k = 0; k < enabled.Count; k++ )
		{
			var c = enabled[k];
			bool isLast = k == enabled.Count - 1;
			if ( (c.Flags & ImGuiTableColumnFlags.NoResize) != 0 )
				continue;
			if ( isLast && (c.IsStretch || (!useChild && (table.Flags & ImGuiTableFlags.NoHostExtendX) == 0)) )
				continue;

			int colIdx = table.Columns.IndexOf( c );
			int id = ImHashInt( colIdx, table.ID ^ 0x5A5A1234 );
			var hit = new ImRect( c.MaxX - hw, y1, c.MaxX + hw, y2 );
			if ( !ItemAdd( hit, id, null, ImGuiItemFlags.NoNav ) )
				continue;
			bool pressed = ButtonBehavior( hit, id, out bool hovered, out bool held, ImGuiButtonFlags.PressedOnClick | ImGuiButtonFlags.PressedOnDoubleClick );
			if ( hovered || held )
				g.MouseCursor = ImGuiMouseCursor.ResizeEW;
			if ( hovered && g.HoveredIdTimer < 0.06f && !held )
				hovered = false;
			if ( hovered ) table.HoveredBorderColumn = colIdx;
			if ( held ) table.HeldBorderColumn = colIdx;

			if ( pressed && g.IO.MouseClickedCount[0] == 2 )
			{
				// Double-click: auto fit
				if ( c.IsStretch )
					c.StretchWeight = -1f;
				else
					c.WidthRequest = -1f;
				ClearActiveID();
				continue;
			}

			if ( held && !g.ActiveIdIsJustActivated )
			{
				float borderX = g.IO.MousePos.x - g.ActiveIdClickOffset.x + hw;
				float padR = c.MaxX - c.WorkMaxX;
				float newW = MathF.Max( minW, ImTrunc( borderX - padR - c.WorkMinX ) );
				if ( !c.IsStretch )
				{
					c.WidthRequest = newW;
				}
				else
				{
					ImGuiTableColumn next = null;
					for ( int j = k + 1; j < enabled.Count; j++ )
						if ( enabled[j].IsStretch ) { next = enabled[j]; break; }
					float sumStretchW = 0f, sumStretchWeight = 0f;
					foreach ( var s in enabled )
						if ( s.IsStretch ) { sumStretchW += s.WidthGiven; sumStretchWeight += MathF.Max( s.StretchWeight, 1e-6f ); }
					float kRatio = sumStretchWeight / MathF.Max( sumStretchW, 1f );
					if ( next is not null )
					{
						float pair = c.WidthGiven + next.WidthGiven;
						newW = Math.Clamp( newW, minW, MathF.Max( minW, pair - minW ) );
						c.StretchWeight = newW * kRatio;
						next.StretchWeight = (pair - newW) * kRatio;
					}
					else
					{
						c.StretchWeight = newW * kRatio;
					}
				}
			}
		}
	}

	private static ImGuiSortDirection TableGetColumnNextSortDirection( ImGuiTable table, ImGuiTableColumn column )
	{
		var dirs = new List<ImGuiSortDirection>();
		bool preferDesc = (column.DeclFlags & ImGuiTableColumnFlags.PreferSortDescending) != 0;
		if ( preferDesc )
		{
			if ( (column.DeclFlags & ImGuiTableColumnFlags.NoSortDescending) == 0 ) dirs.Add( ImGuiSortDirection.Descending );
			if ( (column.DeclFlags & ImGuiTableColumnFlags.NoSortAscending) == 0 ) dirs.Add( ImGuiSortDirection.Ascending );
		}
		else
		{
			if ( (column.DeclFlags & ImGuiTableColumnFlags.NoSortAscending) == 0 ) dirs.Add( ImGuiSortDirection.Ascending );
			if ( (column.DeclFlags & ImGuiTableColumnFlags.NoSortDescending) == 0 ) dirs.Add( ImGuiSortDirection.Descending );
		}
		if ( (table.Flags & ImGuiTableFlags.SortTristate) != 0 )
			dirs.Add( ImGuiSortDirection.None );
		if ( dirs.Count == 0 )
			return ImGuiSortDirection.None;
		if ( column.SortOrder < 0 || column.SortDirection == ImGuiSortDirection.None )
			return dirs[0];
		int idx = dirs.IndexOf( column.SortDirection );
		return dirs[(idx + 1) % dirs.Count];
	}

	private static void TableSetColumnSortDirection( ImGuiTable table, int columnN, ImGuiSortDirection dir, bool appendToSortSpecs )
	{
		if ( (table.Flags & ImGuiTableFlags.SortMulti) == 0 )
			appendToSortSpecs = false;
		if ( (table.Flags & ImGuiTableFlags.SortTristate) == 0 && dir == ImGuiSortDirection.None )
			dir = ImGuiSortDirection.Ascending;

		var column = table.Columns[columnN];
		int maxOrder = -1;
		if ( appendToSortSpecs )
			foreach ( var c in table.Columns )
				maxOrder = Math.Max( maxOrder, c.SortOrder );

		for ( int n = 0; n < table.ColumnsCount; n++ )
		{
			var other = table.Columns[n];
			if ( other != column && !appendToSortSpecs )
				other.SortOrder = -1;
		}

		if ( dir == ImGuiSortDirection.None )
		{
			column.SortOrder = -1;
			column.SortDirection = ImGuiSortDirection.None;
		}
		else
		{
			if ( column.SortOrder == -1 || !appendToSortSpecs )
				column.SortOrder = appendToSortSpecs ? maxOrder + 1 : 0;
			column.SortDirection = dir;
		}

		// Compact sort orders
		var sorted = table.Columns.Where( c => c.SortOrder >= 0 ).OrderBy( c => c.SortOrder ).ToList();
		for ( int i = 0; i < sorted.Count; i++ )
			sorted[i].SortOrder = i;
		table.SortSpecsDirtyInternal = true;
	}

	private static void TableUpdateSort( ImGuiTable table )
	{
		if ( (table.Flags & ImGuiTableFlags.Sortable) == 0 )
			return;

		int sortedCount = 0;
		foreach ( var c in table.Columns )
			if ( c.SortOrder >= 0 )
			{
				if ( (c.DeclFlags & ImGuiTableColumnFlags.NoSort) != 0 )
				{
					c.SortOrder = -1;
					table.SortSpecsDirtyInternal = true;
				}
				else
					sortedCount++;
			}

		if ( sortedCount > 1 && (table.Flags & ImGuiTableFlags.SortMulti) == 0 )
		{
			var keep = table.Columns.Where( c => c.SortOrder >= 0 ).OrderBy( c => c.SortOrder ).First();
			foreach ( var c in table.Columns )
				if ( c != keep ) c.SortOrder = -1;
			keep.SortOrder = 0;
			sortedCount = 1;
			table.SortSpecsDirtyInternal = true;
		}

		if ( sortedCount == 0 && (table.Flags & ImGuiTableFlags.SortTristate) == 0 )
		{
			for ( int n = 0; n < table.ColumnsCount; n++ )
			{
				var c = table.Columns[n];
				if ( c.IsEnabled && (c.DeclFlags & ImGuiTableColumnFlags.NoSort) == 0 )
				{
					c.SortOrder = 0;
					c.SortDirection = TableGetColumnNextSortDirection( table, c );
					if ( c.SortDirection == ImGuiSortDirection.None )
						c.SortDirection = ImGuiSortDirection.Ascending;
					table.SortSpecsDirtyInternal = true;
					break;
				}
			}
		}

		if ( table.SortSpecsDirtyInternal )
		{
			var sorted = new List<(int idx, ImGuiTableColumn c)>();
			for ( int n = 0; n < table.ColumnsCount; n++ )
				if ( table.Columns[n].SortOrder >= 0 )
					sorted.Add( (n, table.Columns[n]) );
			sorted.Sort( ( a, b ) => a.c.SortOrder.CompareTo( b.c.SortOrder ) );
			var specs = new ImGuiTableColumnSortSpecs[sorted.Count];
			for ( int i = 0; i < sorted.Count; i++ )
				specs[i] = new ImGuiTableColumnSortSpecs { ColumnIndex = sorted[i].idx, ColumnUserID = sorted[i].c.UserID, SortOrder = i, SortDirection = sorted[i].c.SortDirection };
			table.SortSpecs.Specs = specs;
			table.SortSpecs.SpecsCount = specs.Length;
			table.SortSpecs.SpecsDirty = true;
			table.SortSpecsDirtyInternal = false;
		}
	}
	#endregion

	#region Rows & cells
	public static void TableNextRow( ImGuiTableRowFlags rowFlags = ImGuiTableRowFlags.None, float minRowHeight = 0.0f )
	{
		var table = CurrentTable;
		if ( table is null )
		{
			Log.Warning( "ImGui: TableNextRow() called outside of a table" );
			return;
		}
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );
		if ( table.IsInsideRow )
			TableEndRow( table );

		table.LastRowFlags = table.RowFlags;
		table.RowFlags = rowFlags;
		table.CurrentRow++;
		table.RowMinHeight = minRowHeight;
		TableBeginRow( table );
	}

	private static void TableBeginRow( ImGuiTable table )
	{
		var g = G;
		var window = table.InnerWindow;
		table.IsInsideRow = true;
		table.CurrentColumn = -1;
		table.RowIsFrozen = table.CurrentRow < table.FreezeRows;
		table.RowFrozenOffset = table.RowIsFrozen ? window.Scroll.y : 0f;
		table.RowPosY1 = table.RowLogicalY + table.RowFrozenOffset;
		table.RowPosY2 = table.RowPosY1 + MathF.Max( table.RowMinHeight, table.CellPaddingY * 2.0f );
		table.RowMaxY = table.RowPosY2;
		table.RowTextBaseline = 0.0f;
		table.RowBgColor0 = default;
		table.RowBgColor1 = default;
		table.CellBgColors.Clear();

		if ( table.CurrentRow > 0 && (table.Flags & ImGuiTableFlags.BordersInnerH) != 0 && (table.LastRowFlags & ImGuiTableRowFlags.Headers) == 0 )
			if ( (table.Flags & ImGuiTableFlags.NoBordersInBody) == 0 && table.CurrentRow != table.FreezeRows )
				table.RowLines.Add( (table.RowPosY1, false) );

		// Opaque background for frozen rows so scrolled content does not show through.
		if ( table.RowIsFrozen && table.UsesChannels )
		{
			table.FrozenRowHeights.TryGetValue( table.CurrentRow, out float predicted );
			predicted = MathF.Max( predicted, table.RowPosY2 - table.RowPosY1 );
			window.DrawList.ChannelsSetCurrent( 2 );
			window.DrawList.PushClipRect( table.InnerClipRect.Min, table.InnerClipRect.Max, false );
			window.DrawList.AddRectFilled( new Vector2( table.ColumnsMinX, table.RowPosY1 ), new Vector2( MathF.Max( table.ColumnsMaxX, table.InnerClipRect.Max.x ), table.RowPosY1 + predicted ), GetColorU32Internal( ImGuiCol.WindowBg ) );
			window.DrawList.PopClipRect();
			window.DrawList.ChannelsSetCurrent( 1 );
		}
	}

	private static void TableEndRow( ImGuiTable table )
	{
		var g = G;
		var window = table.InnerWindow;
		if ( table.CurrentColumn != -1 )
			TableEndCell( table );

		table.RowPosY2 = MathF.Max( table.RowPosY2, table.RowMaxY );
		float height = table.RowPosY2 - table.RowPosY1;
		bool isHeader = (table.RowFlags & ImGuiTableRowFlags.Headers) != 0;

		if ( table.RowIsFrozen )
		{
			table.FrozenRowHeights[table.CurrentRow] = height;
			table.FrozenBottomY = MathF.Max( table.FrozenBottomY, table.RowPosY2 );
		}

		// Row background colors
		Color32 bgCol0 = table.RowBgColor0;
		if ( bgCol0.a == 0 && (table.Flags & ImGuiTableFlags.RowBg) != 0 && !isHeader )
			bgCol0 = GetColorU32Internal( (table.RowBgColorCounter & 1) != 0 ? ImGuiCol.TableRowBgAlt : ImGuiCol.TableRowBg );
		Color32 bgCol1 = table.RowBgColor1;

		bool anyBg = bgCol0.a != 0 || bgCol1.a != 0 || table.CellBgColors.Count > 0;
		bool needFrozenColumnBg = !table.RowIsFrozen && table.FreezeColumns > 0 && table.UsesChannels;
		if ( anyBg || needFrozenColumnBg )
		{
			var dl = window.DrawList;
			float clipTop = table.RowIsFrozen ? table.InnerClipRect.Min.y : MathF.Max( table.InnerClipRect.Min.y, table.FreezeRows > 0 ? table.FrozenBottomY : table.InnerClipRect.Min.y );
			var clipMin = new Vector2( table.InnerClipRect.Min.x, clipTop );
			var clipMax = table.InnerClipRect.Max;

			void DrawBgs( float minX, float maxX )
			{
				var a = new Vector2( minX, table.RowPosY1 );
				var b = new Vector2( maxX, table.RowPosY2 );
				if ( bgCol0.a != 0 ) dl.AddRectFilled( a, b, bgCol0 );
				if ( bgCol1.a != 0 ) dl.AddRectFilled( a, b, bgCol1 );
				foreach ( var (colN, col) in table.CellBgColors )
				{
					var c = table.Columns[colN];
					if ( !c.IsEnabled ) continue;
					float x1 = MathF.Max( minX, c.MinX ), x2 = MathF.Min( maxX, c.MaxX );
					if ( x2 > x1 )
						dl.AddRectFilled( new Vector2( x1, table.RowPosY1 ), new Vector2( x2, table.RowPosY2 ), col );
				}
			}

			if ( table.UsesChannels )
				dl.ChannelsSetCurrent( table.RowIsFrozen ? 2 : 0 );
			dl.PushClipRect( clipMin, clipMax, false );
			if ( anyBg )
				DrawBgs( table.ColumnsMinX, table.ColumnsMaxX );
			dl.PopClipRect();

			if ( needFrozenColumnBg )
			{
				// Frozen columns are drawn above the scrolling body: give them an opaque background.
				dl.ChannelsSetCurrent( 2 );
				dl.PushClipRect( clipMin, clipMax, false );
				float fx1 = table.ColumnsMinX, fx2 = table.FrozenRightX;
				foreach ( var c in table.Columns )
					if ( c.IsFrozen && c.IsEnabled ) fx1 = MathF.Min( fx1, c.MinX );
				if ( fx2 > fx1 )
				{
					dl.AddRectFilled( new Vector2( fx1, table.RowPosY1 ), new Vector2( fx2, table.RowPosY2 ), GetColorU32Internal( ImGuiCol.WindowBg ) );
					if ( anyBg )
						DrawBgs( fx1, fx2 );
				}
				dl.PopClipRect();
			}
			if ( table.UsesChannels )
				dl.ChannelsSetCurrent( 1 );
		}

		if ( isHeader )
		{
			table.HasHeaders = true;
			table.HeaderBottomY = MathF.Max( table.HeaderBottomY, table.RowPosY2 );
			if ( (table.Flags & (ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersOuterH)) != 0 )
				table.RowLines.Add( (table.RowPosY2, true) );
		}
		else
		{
			table.RowBgColorCounter++;
		}

		// Continue layout from the logical (unscrolled) position, so frozen rows don't push content down.
		table.RowLogicalY = table.RowPosY1 - table.RowFrozenOffset + height;
		table.RowPosY2 = table.RowLogicalY;
		table.IsInsideRow = false;
		table.CurrentColumn = -1;
	}

	/// <summary>Append into the next column (or first column of next row if currently in last column). Return true when column is visible.</summary>
	public static bool TableNextColumn()
	{
		var table = CurrentTable;
		if ( table is null )
			return false;

		if ( table.IsInsideRow && table.CurrentColumn + 1 < table.ColumnsCount )
		{
			if ( table.CurrentColumn != -1 )
				TableEndCell( table );
			TableBeginCell( table, table.CurrentColumn + 1 );
		}
		else
		{
			TableNextRow();
			TableBeginCell( table, 0 );
		}
		var c = table.Columns[table.CurrentColumn];
		return c.IsEnabled && c.IsVisibleX;
	}

	/// <summary>Append into the specified column. Return true when column is visible.</summary>
	public static bool TableSetColumnIndex( int columnN )
	{
		var table = CurrentTable;
		if ( table is null || columnN < 0 || columnN >= table.ColumnsCount )
			return false;

		if ( !table.IsInsideRow )
			TableNextRow();
		if ( table.CurrentColumn != columnN )
		{
			if ( table.CurrentColumn != -1 )
				TableEndCell( table );
			TableBeginCell( table, columnN );
		}
		var c = table.Columns[columnN];
		return c.IsEnabled && c.IsVisibleX;
	}

	private static void TableBeginCell( ImGuiTable table, int columnN )
	{
		var g = G;
		var window = table.InnerWindow;
		var column = table.Columns[columnN];
		table.CurrentColumn = columnN;

		float startX = column.WorkMinX;
		float startY = table.RowPosY1 + table.CellPaddingY;
		window.DC.CursorPos = new Vector2( startX, startY );
		window.DC.CursorPosPrevLine = window.DC.CursorPos;
		window.DC.CursorMaxPos = window.DC.CursorPos;
		window.DC.ColumnsOffset = startX - window.Pos.x - window.DC.Indent;
		window.DC.CurrLineTextBaseOffset = table.RowTextBaseline;
		window.DC.CurrLineSize = Vector2.Zero;
		window.DC.PrevLineSize = Vector2.Zero;
		window.DC.IsSameLine = false;
		window.WorkRect = new ImRect( column.WorkMinX, table.WorkRect.Min.y, column.WorkMaxX, table.WorkRect.Max.y );
		window.DC.ItemWidth = column.ItemWidth;
		window.SkipItems = table.HostSkipItems || !column.IsEnabled || !column.IsVisibleX;

		if ( table.UsesChannels )
			window.DrawList.ChannelsSetCurrent( table.RowIsFrozen || column.IsFrozen ? 3 : 1 );

		float minY = table.InnerClipRect.Min.y;
		if ( !table.RowIsFrozen && table.FreezeRows > 0 )
			minY = MathF.Max( minY, table.FrozenBottomY );
		PushClipRect( new Vector2( column.ClipMinX, minY ), new Vector2( column.ClipMaxX, MathF.Max( minY, table.InnerClipRect.Max.y ) ), false );
	}

	private static void TableEndCell( ImGuiTable table )
	{
		var window = table.InnerWindow;
		var column = table.Columns[table.CurrentColumn];
		PopClipRect();

		if ( !window.SkipItems )
		{
			column.ContentWidthThisFrame = MathF.Max( column.ContentWidthThisFrame, window.DC.CursorMaxPos.x - column.WorkMinX );
			table.RowMaxY = MathF.Max( table.RowMaxY, window.DC.CursorMaxPos.y + table.CellPaddingY );
		}
		window.SkipItems = table.HostSkipItems;
	}
	#endregion

	#region Headers
	private static float TableGetHeaderRowHeight()
	{
		var g = G;
		return g.FontSize + g.Style.CellPadding.y * 2.0f;
	}

	/// <summary>Submit a row with header cells based on data provided to TableSetupColumn() + submit context menu.</summary>
	public static void TableHeadersRow()
	{
		var table = CurrentTable;
		if ( table is null )
		{
			Log.Warning( "ImGui: TableHeadersRow() called outside of a table" );
			return;
		}
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );

		float rowHeight = TableGetHeaderRowHeight();
		TableNextRow( ImGuiTableRowFlags.Headers, rowHeight );
		for ( int n = 0; n < table.ColumnsCount; n++ )
		{
			if ( !TableSetColumnIndex( n ) )
				continue;
			string name = (table.Columns[n].DeclFlags & ImGuiTableColumnFlags.NoHeaderLabel) != 0 ? "" : (table.Columns[n].Name ?? "");
			PushID( n );
			TableHeader( name );
			PopID();
		}
	}

	/// <summary>Angled headers are rendered as regular headers (text rotation is not supported).</summary>
	public static void TableAngledHeadersRow()
	{
		var table = CurrentTable;
		if ( table is null )
			return;
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );
		TableNextRow( ImGuiTableRowFlags.Headers, TableGetHeaderRowHeight() );
		for ( int n = 0; n < table.ColumnsCount; n++ )
		{
			if ( !TableSetColumnIndex( n ) )
				continue;
			if ( (table.Columns[n].DeclFlags & ImGuiTableColumnFlags.AngledHeader) == 0 )
				continue;
			PushID( n );
			TableHeader( table.Columns[n].Name ?? "" );
			PopID();
		}
	}

	/// <summary>Submit one header cell manually (rarely used).</summary>
	public static void TableHeader( string label )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;
		var table = CurrentTable;
		if ( table is null || table.CurrentColumn < 0 )
		{
			Log.Warning( "ImGui: TableHeader() needs to be called inside a table cell" );
			return;
		}

		int columnN = table.CurrentColumn;
		var column = table.Columns[columnN];
		label ??= "";
		var labelSize = CalcTextSize( label, true );
		var labelPos = window.DC.CursorPos;

		var cellR = new ImRect( column.MinX, table.RowPosY1, column.MaxX, table.RowPosY2 );
		float labelHeight = MathF.Max( labelSize.y, table.RowMinHeight - table.CellPaddingY * 2.0f );

		const float ARROW_SCALE = 0.65f;
		float wArrow = 0f, wSortText = 0f;
		string sortOrderSuffix = null;
		bool sortable = (table.Flags & ImGuiTableFlags.Sortable) != 0 && (column.DeclFlags & ImGuiTableColumnFlags.NoSort) == 0;
		if ( sortable )
		{
			wArrow = MathF.Max( g.FontSize * ARROW_SCALE + g.Style.FramePadding.x, g.FontSize * ARROW_SCALE );
			if ( column.SortOrder > 0 )
			{
				sortOrderSuffix = (column.SortOrder + 1).ToString();
				wSortText = g.Style.ItemInnerSpacing.x + CalcTextSize( sortOrderSuffix ).x;
			}
		}

		float maxPosX = labelPos.x + labelSize.x + wSortText + wArrow;
		column.ContentWidthThisFrame = MathF.Max( column.ContentWidthThisFrame, maxPosX - column.WorkMinX );

		int id = window.GetID( label );
		var bb = new ImRect( cellR.Min.x, cellR.Min.y, cellR.Max.x, MathF.Max( cellR.Max.y, cellR.Min.y + labelHeight + table.CellPaddingY * 2.0f ) );
		ItemSize( new Vector2( 0.0f, labelHeight ) );
		if ( !ItemAdd( bb, id ) )
			return;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, ImGuiButtonFlags.AllowOverlap );
		bool contextOpen = IsPopupOpen( table.ContextPopupId, ImGuiPopupFlags.None ) && table.ContextMenuColumn == columnN;
		bool highlightColumn = (table.Flags & ImGuiTableFlags.HighlightHoveredColumn) != 0 && table.HoveredColumnBody == columnN;

		var bgCol = GetColorU32Internal( ImGuiCol.TableHeaderBg );
		window.DrawList.AddRectFilled( bb.Min, bb.Max, bgCol );
		if ( held || hovered || contextOpen || highlightColumn )
		{
			var col = GetColorU32Internal( held ? ImGuiCol.HeaderActive : (hovered || highlightColumn) ? ImGuiCol.HeaderHovered : ImGuiCol.Header );
			window.DrawList.AddRectFilled( bb.Min, bb.Max, col );
		}

		// Drag to reorder
		if ( held && (table.Flags & ImGuiTableFlags.Reorderable) != 0 && IsMouseDragging( ImGuiMouseButton.Left ) && !g.DragDropActive && (column.DeclFlags & ImGuiTableColumnFlags.NoReorder) == 0 )
		{
			table.ReorderColumn = columnN;
			int dir = g.IO.MousePos.x < cellR.Min.x ? -1 : (g.IO.MousePos.x > cellR.Max.x ? 1 : 0);
			if ( dir != 0 )
			{
				int targetOrder = column.DisplayOrder + dir;
				if ( targetOrder >= 0 && targetOrder < table.ColumnsCount )
				{
					var other = table.Columns[table.DisplayOrderToIndex[targetOrder]];
					if ( (other.DeclFlags & ImGuiTableColumnFlags.NoReorder) == 0 )
					{
						(other.DisplayOrder, column.DisplayOrder) = (column.DisplayOrder, other.DisplayOrder);
						table.DisplayOrderToIndex[column.DisplayOrder] = columnN;
						table.DisplayOrderToIndex[other.DisplayOrder] = table.Columns.IndexOf( other );
					}
				}
			}
		}

		// Sort arrow
		float ellipsisMax = cellR.Max.x - wArrow - wSortText;
		if ( sortable )
		{
			if ( column.SortOrder != -1 && column.SortDirection != ImGuiSortDirection.None )
			{
				float x = MathF.Max( cellR.Min.x, cellR.Max.x - wArrow - wSortText );
				float y = labelPos.y;
				if ( sortOrderSuffix is not null )
				{
					var textCol = g.Style.Colors[(int)ImGuiCol.Text];
					PushStyleColor( ImGuiCol.Text, new Vector4( textCol.x, textCol.y, textCol.z, textCol.w * 0.70f ) );
					RenderText( new Vector2( x + g.Style.ItemInnerSpacing.x, y ), sortOrderSuffix );
					PopStyleColor();
					x += wSortText;
				}
				RenderArrow( window.DrawList, new Vector2( x, y + g.FontSize * (1f - ARROW_SCALE) * 0.5f ), GetColorU32Internal( ImGuiCol.Text ), column.SortDirection == ImGuiSortDirection.Ascending ? ImGuiDir.Up : ImGuiDir.Down, ARROW_SCALE );
			}

			if ( pressed && table.ReorderColumn != columnN )
			{
				var dir = TableGetColumnNextSortDirection( table, column );
				TableSetColumnSortDirection( table, columnN, dir, g.IO.KeyShift );
			}
		}

		RenderTextEllipsis( window.DrawList, labelPos, new Vector2( ellipsisMax, labelPos.y + labelHeight + g.Style.FramePadding.y ), ellipsisMax, ellipsisMax, label, labelSize );

		bool textClipped = labelSize.x > ellipsisMax - labelPos.x;
		if ( textClipped && hovered && g.ActiveId == 0 )
			SetItemTooltip( "{0}", LabelText( label ) );

		if ( IsMouseReleased( ImGuiMouseButton.Right ) && IsItemHovered() )
		{
			table.ContextMenuColumn = columnN;
			OpenPopupEx( table.ContextPopupId );
		}
	}

	private static void TableDrawDefaultContextMenu( ImGuiTable table )
	{
		bool wantSeparator = false;
		int columnN = table.ContextMenuColumn >= 0 && table.ContextMenuColumn < table.ColumnsCount ? table.ContextMenuColumn : -1;
		var column = columnN >= 0 ? table.Columns[columnN] : null;

		if ( (table.Flags & ImGuiTableFlags.Resizable) != 0 )
		{
			bool canResize = column is not null && (column.Flags & ImGuiTableColumnFlags.NoResize) == 0 && column.IsEnabled;
			if ( MenuItem( "Size column to fit", null, false, canResize ) && column is not null )
			{
				if ( column.IsStretch ) column.StretchWeight = -1f;
				else column.WidthRequest = -1f;
			}
			if ( MenuItem( "Size all columns to fit", null ) )
			{
				foreach ( var c in table.Columns )
				{
					c.WidthRequest = -1f;
					c.StretchWeight = -1f;
				}
			}
			wantSeparator = true;
		}

		if ( (table.Flags & ImGuiTableFlags.Reorderable) != 0 )
		{
			if ( MenuItem( "Reset order", null ) )
				for ( int n = 0; n < table.ColumnsCount; n++ )
					table.Columns[n].DisplayOrder = n;
			wantSeparator = true;
		}

		if ( (table.Flags & ImGuiTableFlags.Hideable) != 0 )
		{
			if ( wantSeparator )
				Separator();
			PushItemFlag( ImGuiItemFlags.AutoClosePopups, false );
			int enabledCount = table.Columns.Count( c => c.IsUserEnabled );
			for ( int n = 0; n < table.ColumnsCount; n++ )
			{
				var c = table.Columns[n];
				if ( (c.DeclFlags & ImGuiTableColumnFlags.Disabled) != 0 )
					continue;
				string name = string.IsNullOrEmpty( c.Name ) ? "<Unknown>" : LabelText( c.Name );
				bool active = (c.DeclFlags & ImGuiTableColumnFlags.NoHide) == 0 && !(enabledCount <= 1 && c.IsUserEnabled);
				PushID( n );
				if ( MenuItem( name, null, c.IsUserEnabled, active ) )
					c.IsUserEnabledNextFrame = !c.IsUserEnabled;
				PopID();
			}
			PopItemFlag();
		}
	}
	#endregion

	#region Borders
	private static void TableDrawBorders( ImGuiTable table, float rowsEndY )
	{
		var window = table.InnerWindow;
		bool useChild = window != table.OuterWindow;
		var dl = window.DrawList;
		var flags = table.Flags;
		var strong = GetColorU32Internal( ImGuiCol.TableBorderStrong );
		var light = GetColorU32Internal( ImGuiCol.TableBorderLight );

		float topY = useChild ? window.InnerRect.Min.y : table.StartY;
		float bottomY = useChild ? MathF.Min( rowsEndY, window.InnerRect.Max.y ) : rowsEndY;
		if ( useChild && (flags & ImGuiTableFlags.NoHostExtendY) == 0 )
			bottomY = MathF.Min( MathF.Max( rowsEndY, topY ), window.InnerRect.Max.y );
		if ( bottomY <= topY )
			return;

		dl.PushClipRect( table.InnerClipRect.Min, table.InnerClipRect.Max, false );

		// Horizontal row lines
		foreach ( var (y, isStrong) in table.RowLines )
			if ( y > topY && y < bottomY + 0.5f )
				dl.AddLine( new Vector2( table.ColumnsMinX, y ), new Vector2( table.ColumnsMaxX, y ), isStrong ? strong : light );

		// Inner vertical borders
		if ( (flags & ImGuiTableFlags.BordersInnerV) != 0 || table.HoveredBorderColumn != -1 || table.HeldBorderColumn != -1 )
		{
			var enabled = table.DisplayOrderToIndex.Select( i => table.Columns[i] ).Where( c => c.IsEnabled ).ToList();
			for ( int k = 0; k < enabled.Count; k++ )
			{
				var c = enabled[k];
				int colIdx = table.Columns.IndexOf( c );
				bool isLast = k == enabled.Count - 1;
				bool isHeld = table.HeldBorderColumn == colIdx;
				bool isHovered = table.HoveredBorderColumn == colIdx;
				if ( isLast && !isHeld && !isHovered )
					continue;
				if ( (flags & ImGuiTableFlags.BordersInnerV) == 0 && !isHeld && !isHovered )
					continue;
				float y2 = bottomY;
				bool bodyBorders = (flags & ImGuiTableFlags.NoBordersInBody) == 0;
				if ( !bodyBorders && !isHeld && !isHovered )
				{
					if ( !table.HasHeaders )
						continue;
					y2 = MathF.Min( bottomY, table.HeaderBottomY );
				}
				var col = isHeld ? GetColorU32Internal( ImGuiCol.SeparatorActive ) : isHovered ? GetColorU32Internal( ImGuiCol.SeparatorHovered ) : (bodyBorders ? light : strong);
				if ( table.HasHeaders && bodyBorders && !isHeld && !isHovered )
				{
					dl.AddLine( new Vector2( c.MaxX, topY ), new Vector2( c.MaxX, MathF.Min( y2, table.HeaderBottomY ) ), strong );
					dl.AddLine( new Vector2( c.MaxX, MathF.Min( y2, table.HeaderBottomY ) ), new Vector2( c.MaxX, y2 ), light );
				}
				else
				{
					dl.AddLine( new Vector2( c.MaxX, topY ), new Vector2( c.MaxX, y2 ), col, isHeld || isHovered ? 2f : 1f );
				}
			}
		}

		dl.PopClipRect();

		// Outer borders (non-scrolling tables; scrolling tables get them drawn on the host window)
		if ( !useChild )
		{
			var r = new ImRect( table.OuterRect.Min.x, topY, table.OuterRect.Max.x, bottomY );
			if ( (flags & ImGuiTableFlags.BordersOuterV) != 0 )
			{
				dl.AddLine( r.TL, r.BL, strong );
				dl.AddLine( new Vector2( r.Max.x - 1, r.Min.y ), new Vector2( r.Max.x - 1, r.Max.y ), strong );
			}
			if ( (flags & ImGuiTableFlags.BordersOuterH) != 0 )
			{
				dl.AddLine( r.TL, r.TR, strong );
				dl.AddLine( new Vector2( r.Min.x, r.Max.y - 1 ), new Vector2( r.Max.x, r.Max.y - 1 ), strong );
			}
		}
	}
	#endregion

	#region Queries
	public static ImGuiTableSortSpecs TableGetSortSpecs()
	{
		var table = CurrentTable;
		if ( table is null || (table.Flags & ImGuiTableFlags.Sortable) == 0 )
			return null;
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );
		TableUpdateSort( table );
		return table.SortSpecs;
	}

	public static int TableGetColumnCount() => CurrentTable?.ColumnsCount ?? 0;
	public static int TableGetColumnIndex() => CurrentTable?.CurrentColumn ?? 0;
	public static int TableGetRowIndex() => CurrentTable?.CurrentRow ?? 0;
	public static int TableGetHoveredColumn() => CurrentTable?.HoveredColumnBody ?? -1;

	public static string TableGetColumnName( int columnN = -1 )
	{
		var table = CurrentTable;
		if ( table is null )
			return null;
		if ( columnN < 0 )
			columnN = table.CurrentColumn;
		if ( columnN < 0 || columnN >= table.ColumnsCount )
			return null;
		return table.Columns[columnN].Name ?? "";
	}

	public static ImGuiTableColumnFlags TableGetColumnFlags( int columnN = -1 )
	{
		var table = CurrentTable;
		if ( table is null )
			return ImGuiTableColumnFlags.None;
		if ( columnN < 0 )
			columnN = table.CurrentColumn;
		if ( columnN == table.ColumnsCount )
			return table.HoveredColumnBody == -1 ? ImGuiTableColumnFlags.IsHovered : ImGuiTableColumnFlags.None;
		if ( columnN < 0 || columnN >= table.ColumnsCount )
			return ImGuiTableColumnFlags.None;
		var c = table.Columns[columnN];
		var flags = c.DeclFlags | (c.Flags & ImGuiTableColumnFlags.WidthMask_);
		if ( c.IsEnabled ) flags |= ImGuiTableColumnFlags.IsEnabled;
		if ( c.IsEnabled && c.IsVisibleX ) flags |= ImGuiTableColumnFlags.IsVisible;
		if ( c.SortOrder >= 0 ) flags |= ImGuiTableColumnFlags.IsSorted;
		if ( table.HoveredColumnBody == columnN ) flags |= ImGuiTableColumnFlags.IsHovered;
		return flags;
	}

	/// <summary>Change user accessible enabled/disabled state of a column (applied next frame).</summary>
	public static void TableSetColumnEnabled( int columnN, bool v )
	{
		var table = CurrentTable;
		if ( table is null )
			return;
		if ( columnN < 0 )
			columnN = table.CurrentColumn;
		if ( columnN < 0 || columnN >= table.ColumnsCount )
			return;
		table.Columns[columnN].IsUserEnabledNextFrame = v;
	}

	/// <summary>Change the color of a cell, row, or column.</summary>
	public static void TableSetBgColor( ImGuiTableBgTarget target, Color32 color, int columnN = -1 )
	{
		var table = CurrentTable;
		if ( table is null || target == ImGuiTableBgTarget.None )
			return;
		switch ( target )
		{
			case ImGuiTableBgTarget.RowBg0:
				table.RowBgColor0 = color;
				break;
			case ImGuiTableBgTarget.RowBg1:
				table.RowBgColor1 = color;
				break;
			case ImGuiTableBgTarget.CellBg:
				if ( columnN < 0 )
					columnN = table.CurrentColumn;
				if ( columnN >= 0 && columnN < table.ColumnsCount && color.a != 0 )
					table.CellBgColors.Add( (columnN, color) );
				break;
		}
	}

	public static void TableSetBgColor( ImGuiTableBgTarget target, Vector4 color, int columnN = -1 )
		=> TableSetBgColor( target, GetColorU32( color ), columnN );
	#endregion
}
