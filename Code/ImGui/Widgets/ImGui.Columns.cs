namespace Duccsoft.ImGui;

[Flags]
public enum ImGuiOldColumnFlags
{
	None = 0,
	NoBorder = 1 << 0,
	NoResize = 1 << 1,
	NoPreserveWidths = 1 << 2,
	NoForceWithinWindow = 1 << 3,
	GrowParentContentsSize = 1 << 4,
}

internal class ImGuiOldColumnData
{
	public float OffsetNorm;
	public float OffsetNormBeforeResize;
	public ImGuiOldColumnFlags Flags;
	public ImRect ClipRect;
}

/// <summary>State of a legacy Columns() set. Prefer tables for new code.</summary>
internal class ImGuiOldColumns
{
	public int ID;
	public ImGuiOldColumnFlags Flags;
	public bool IsFirstFrame;
	public bool IsBeingResized;
	public bool UsesChannels;
	public int Current;
	public int Count;
	public float OffMinX, OffMaxX;
	public float LineMinY, LineMaxY;
	public float HostCursorPosY;
	public float HostCursorMaxPosX;
	public ImRect HostInitialClipRect;
	public ImRect HostBackupClipRect;
	public ImRect HostBackupParentWorkRect;
	public readonly List<ImGuiOldColumnData> Columns = new();
}

public static partial class ImGui
{
	private const float COLUMNS_HIT_RECT_HALF_THICKNESS = 4.0f;

	private static int GetColumnsID( string strId, int columnsCount )
	{
		var window = G.CurrentWindow;
		PushID( 0x11223347 + (strId is not null ? 0 : columnsCount) );
		int id = window.GetID( strId ?? "columns" );
		PopID();
		return id;
	}

	private static ImGuiOldColumns FindOrCreateColumns( ImGuiWindow window, int id )
	{
		foreach ( var c in window.ColumnsStorage )
			if ( c.ID == id )
				return c;
		var columns = new ImGuiOldColumns { ID = id };
		window.ColumnsStorage.Add( columns );
		return columns;
	}

	private static float GetColumnOffsetFromNorm( ImGuiOldColumns columns, float offsetNorm ) => offsetNorm * (columns.OffMaxX - columns.OffMinX);
	private static float GetColumnNormFromOffset( ImGuiOldColumns columns, float offset ) => offset / (columns.OffMaxX - columns.OffMinX);

	private static float GetColumnWidthEx( ImGuiOldColumns columns, int columnIndex, bool beforeResize = false )
	{
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		float offsetNorm = beforeResize
			? columns.Columns[columnIndex + 1].OffsetNormBeforeResize - columns.Columns[columnIndex].OffsetNormBeforeResize
			: columns.Columns[columnIndex + 1].OffsetNorm - columns.Columns[columnIndex].OffsetNorm;
		return GetColumnOffsetFromNorm( columns, offsetNorm );
	}

	/// <summary>Legacy columns API (2020: prefer using Tables!). Columns(1) ends the current column set.</summary>
	public static void Columns( int count = 1, string id = null, bool borders = true )
	{
		var window = GetCurrentWindow();
		var flags = borders ? ImGuiOldColumnFlags.None : ImGuiOldColumnFlags.NoBorder;
		var columns = window.DC.CurrentColumns;
		if ( columns is not null && columns.Count == count && columns.Flags == flags )
			return;
		if ( columns is not null )
			EndColumns();
		if ( count != 1 )
			BeginColumns( id, count, flags );
	}

	public static void BeginColumns( string strId, int columnsCount, ImGuiOldColumnFlags flags = ImGuiOldColumnFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( columnsCount < 1 || window.DC.CurrentColumns is not null )
			return;

		int id = GetColumnsID( strId, columnsCount );
		var columns = FindOrCreateColumns( window, id );
		columns.Current = 0;
		columns.Count = columnsCount;
		columns.Flags = flags;
		window.DC.CurrentColumns = columns;

		columns.HostCursorPosY = window.DC.CursorPos.y;
		columns.HostCursorMaxPosX = window.DC.CursorMaxPos.x;
		columns.HostInitialClipRect = window.ClipRect;
		columns.HostBackupParentWorkRect = window.ParentWorkRect;
		window.ParentWorkRect = window.WorkRect;

		float columnPadding = g.Style.ItemSpacing.x;
		float halfClipExtendX = ImTrunc( MathF.Max( window.WindowPadding.x * 0.5f, window.WindowBorderSize ) );
		float max1 = window.WorkRect.Max.x + columnPadding - MathF.Max( columnPadding - window.WindowPadding.x, 0.0f );
		float max2 = window.WorkRect.Max.x + halfClipExtendX;
		columns.OffMinX = window.DC.Indent - columnPadding + MathF.Max( columnPadding - window.WindowPadding.x, 0.0f );
		columns.OffMaxX = MathF.Max( MathF.Min( max1, max2 ) - window.Pos.x, columns.OffMinX + 1.0f );
		columns.LineMinY = columns.LineMaxY = window.DC.CursorPos.y;

		if ( columns.Columns.Count != 0 && columns.Columns.Count != columnsCount + 1 )
			columns.Columns.Clear();

		columns.IsFirstFrame = columns.Columns.Count == 0;
		if ( columns.Columns.Count == 0 )
		{
			for ( int n = 0; n < columnsCount + 1; n++ )
				columns.Columns.Add( new ImGuiOldColumnData { OffsetNorm = n / (float)columnsCount } );
		}

		for ( int n = 0; n < columnsCount; n++ )
		{
			var column = columns.Columns[n];
			float clipX1 = MathF.Round( window.Pos.x + GetColumnOffset( n ) );
			float clipX2 = MathF.Round( window.Pos.x + GetColumnOffset( n + 1 ) - 1.0f );
			column.ClipRect = new ImRect( clipX1, -float.MaxValue, clipX2, float.MaxValue );
			column.ClipRect.ClipWithFull( window.ClipRect );
		}

		columns.UsesChannels = columns.Count > 1 && !g.SplitDrawLists.Contains( window.DrawList );
		if ( columns.Count > 1 )
		{
			if ( columns.UsesChannels )
			{
				g.SplitDrawLists.Add( window.DrawList );
				window.DrawList.ChannelsSplit( 1 + columns.Count );
				window.DrawList.ChannelsSetCurrent( 1 );
			}
			PushColumnClipRect( 0 );
		}

		float offset0 = GetColumnOffset( columns.Current );
		float offset1 = GetColumnOffset( columns.Current + 1 );
		float width = offset1 - offset0;
		PushItemWidth( width * 0.65f );
		window.DC.ColumnsOffset = MathF.Max( columnPadding - window.WindowPadding.x, 0.0f );
		window.DC.CursorPos = new Vector2( ImTrunc( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset ), window.DC.CursorPos.y );
		window.WorkRect.Max.x = window.Pos.x + offset1 - columnPadding;
		window.WorkRect.Max.y = window.ContentRegionRect.Max.y;
	}

	private static void PushColumnClipRect( int columnIndex )
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		var column = columns.Columns[columnIndex];
		PushClipRect( column.ClipRect.Min, column.ClipRect.Max, false );
	}

	public static void NextColumn()
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems || window.DC.CurrentColumns is null )
			return;

		var columns = window.DC.CurrentColumns;
		if ( columns.Count == 1 )
		{
			window.DC.CursorPos = new Vector2( ImTrunc( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset ), window.DC.CursorPos.y );
			return;
		}

		if ( ++columns.Current == columns.Count )
			columns.Current = 0;

		PopItemWidth();
		PopClipRect();
		if ( columns.UsesChannels )
			window.DrawList.ChannelsSetCurrent( columns.Current + 1 );
		PushColumnClipRect( columns.Current );

		float columnPadding = g.Style.ItemSpacing.x;
		columns.LineMaxY = MathF.Max( columns.LineMaxY, window.DC.CursorPos.y );
		if ( columns.Current > 0 )
		{
			window.DC.ColumnsOffset = GetColumnOffset( columns.Current ) - window.DC.Indent + columnPadding;
		}
		else
		{
			window.DC.ColumnsOffset = MathF.Max( columnPadding - window.WindowPadding.x, 0.0f );
			window.DC.IsSameLine = false;
			columns.LineMinY = columns.LineMaxY;
		}
		window.DC.CursorPos = new Vector2( ImTrunc( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset ), columns.LineMinY );
		window.DC.CurrLineSize = Vector2.Zero;
		window.DC.CurrLineTextBaseOffset = 0.0f;

		float offset0 = GetColumnOffset( columns.Current );
		float offset1 = GetColumnOffset( columns.Current + 1 );
		float width = offset1 - offset0;
		PushItemWidth( width * 0.65f );
		window.WorkRect.Max.x = window.Pos.x + offset1 - columnPadding;
	}

	public static void EndColumns()
	{
		var g = G;
		var window = G.CurrentWindow;
		var columns = window?.DC.CurrentColumns;
		if ( columns is null )
			return;

		PopItemWidth();
		if ( columns.Count > 1 )
		{
			PopClipRect();
			if ( columns.UsesChannels )
			{
				window.DrawList.ChannelsMerge();
				g.SplitDrawLists.Remove( window.DrawList );
			}
		}

		var flags = columns.Flags;
		columns.LineMaxY = MathF.Max( columns.LineMaxY, window.DC.CursorPos.y );
		window.DC.CursorPos = new Vector2( window.DC.CursorPos.x, columns.LineMaxY );
		if ( (flags & ImGuiOldColumnFlags.GrowParentContentsSize) == 0 )
			window.DC.CursorMaxPos = new Vector2( columns.HostCursorMaxPosX, window.DC.CursorMaxPos.y );

		bool isBeingResized = false;
		if ( (flags & ImGuiOldColumnFlags.NoBorder) == 0 && !window.SkipItems )
		{
			float y1 = MathF.Max( columns.HostCursorPosY, window.ClipRect.Min.y );
			float y2 = MathF.Min( window.DC.CursorPos.y, window.ClipRect.Max.y );
			int draggingColumn = -1;
			for ( int n = 1; n < columns.Count; n++ )
			{
				var column = columns.Columns[n];
				float x = window.Pos.x + GetColumnOffset( n );
				int columnId = unchecked(columns.ID + n);
				float hitHw = ImTrunc( COLUMNS_HIT_RECT_HALF_THICKNESS * g.AppliedStyleScale );
				var hitRect = new ImRect( new Vector2( x - hitHw, y1 ), new Vector2( x + hitHw, y2 ) );
				if ( !ItemAdd( hitRect, columnId, null, ImGuiItemFlags.NoNav ) )
					continue;

				bool hovered = false, held = false;
				if ( (flags & ImGuiOldColumnFlags.NoResize) == 0 )
				{
					ButtonBehavior( hitRect, columnId, out hovered, out held );
					if ( hovered || held )
						g.MouseCursor = ImGuiMouseCursor.ResizeEW;
					if ( held && (column.Flags & ImGuiOldColumnFlags.NoResize) == 0 )
						draggingColumn = n;
				}

				var col = GetColorU32Internal( held ? ImGuiCol.SeparatorActive : hovered ? ImGuiCol.SeparatorHovered : ImGuiCol.Separator );
				float xi = ImTrunc( x );
				window.DrawList.AddLine( new Vector2( xi, y1 + 1.0f ), new Vector2( xi, y2 ), col );
			}

			if ( draggingColumn != -1 )
			{
				if ( !columns.IsBeingResized )
					foreach ( var c in columns.Columns )
						c.OffsetNormBeforeResize = c.OffsetNorm;
				columns.IsBeingResized = isBeingResized = true;
				float x = GetDraggedColumnOffset( columns, draggingColumn );
				SetColumnOffset( draggingColumn, x );
			}
		}
		columns.IsBeingResized = isBeingResized;

		window.WorkRect = window.ParentWorkRect;
		window.ParentWorkRect = columns.HostBackupParentWorkRect;
		window.DC.CurrentColumns = null;
		window.DC.ColumnsOffset = 0.0f;
		window.DC.CursorPos = new Vector2( ImTrunc( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset ), window.DC.CursorPos.y );
	}

	private static float GetDraggedColumnOffset( ImGuiOldColumns columns, int columnIndex )
	{
		var g = G;
		var window = g.CurrentWindow;
		float x = g.IO.MousePos.x - g.ActiveIdClickOffset.x + ImTrunc( COLUMNS_HIT_RECT_HALF_THICKNESS * g.AppliedStyleScale ) - window.Pos.x;
		x = MathF.Max( x, GetColumnOffset( columnIndex - 1 ) + g.Style.ColumnsMinSpacing );
		if ( (columns.Flags & ImGuiOldColumnFlags.NoPreserveWidths) != 0 )
			x = MathF.Min( x, GetColumnOffset( columnIndex + 1 ) - g.Style.ColumnsMinSpacing );
		return x;
	}

	public static int GetColumnIndex() => G.CurrentWindow.DC.CurrentColumns?.Current ?? 0;
	public static int GetColumnsCount() => G.CurrentWindow.DC.CurrentColumns?.Count ?? 1;

	/// <summary>Get position of column line (in pixels, from the left side of the contents region).</summary>
	public static float GetColumnOffset( int columnIndex = -1 )
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null )
			return 0.0f;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		columnIndex = Math.Clamp( columnIndex, 0, columns.Columns.Count - 1 );
		float t = columns.Columns[columnIndex].OffsetNorm;
		return ImLerp( columns.OffMinX, columns.OffMaxX, t );
	}

	public static void SetColumnOffset( int columnIndex, float offset )
	{
		var g = G;
		var window = g.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null )
			return;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		if ( columnIndex >= columns.Columns.Count )
			return;

		bool preserveWidth = (columns.Flags & ImGuiOldColumnFlags.NoPreserveWidths) == 0 && columnIndex < columns.Count - 1;
		float width = preserveWidth ? GetColumnWidthEx( columns, columnIndex, columns.IsBeingResized ) : 0.0f;

		if ( (columns.Flags & ImGuiOldColumnFlags.NoForceWithinWindow) == 0 )
			offset = MathF.Min( offset, columns.OffMaxX - g.Style.ColumnsMinSpacing * (columns.Count - columnIndex) );
		columns.Columns[columnIndex].OffsetNorm = GetColumnNormFromOffset( columns, offset - columns.OffMinX );

		if ( preserveWidth )
			SetColumnOffset( columnIndex + 1, offset + MathF.Max( g.Style.ColumnsMinSpacing, width ) );
	}

	public static float GetColumnWidth( int columnIndex = -1 )
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null )
			return GetContentRegionAvail().x;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		return GetColumnOffsetFromNorm( columns, columns.Columns[columnIndex + 1].OffsetNorm - columns.Columns[columnIndex].OffsetNorm );
	}

	public static void SetColumnWidth( int columnIndex, float width )
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null )
			return;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		SetColumnOffset( columnIndex + 1, GetColumnOffset( columnIndex ) + width );
	}

	/// <summary>Draw into the background channel spanning all columns (used e.g. by Selectable with SpanAllColumns).</summary>
	internal static void PushColumnsBackground()
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null || columns.Count == 1 )
			return;
		columns.HostBackupClipRect = window.ClipRect;
		if ( columns.UsesChannels )
			window.DrawList.ChannelsSetCurrent( 0 );
		PushClipRect( columns.HostInitialClipRect.Min, columns.HostInitialClipRect.Max, false );
	}

	internal static void PopColumnsBackground()
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null || columns.Count == 1 )
			return;
		PopClipRect();
		if ( columns.UsesChannels )
			window.DrawList.ChannelsSetCurrent( columns.Current + 1 );
	}
}
