namespace Duccsoft.ImGui;

/// <summary>
/// Data payload for drag and drop operations: <see cref="ImGui.AcceptDragDropPayload"/>, <see cref="ImGui.GetDragDropPayload"/>.
/// Unlike Dear ImGui, the payload holds a managed object reference instead of a copied byte buffer.
/// </summary>
public class ImGuiPayload
{
	/// <summary>The payload data, as passed to SetDragDropPayload().</summary>
	public object Data;
	/// <summary>Data type tag (short user-supplied string, 32 characters max in Dear ImGui).</summary>
	public string DataType;
	/// <summary>Source item id.</summary>
	public int SourceId;
	/// <summary>Source parent id (if available).</summary>
	public int SourceParentId;
	/// <summary>Data timestamp: the frame the payload was last set.</summary>
	public int DataFrameCount = -1;
	/// <summary>Set when AcceptDragDropPayload() was called and mouse has been hovering the target item (nb: handle overlapping drag targets).</summary>
	public bool Preview;
	/// <summary>Set when AcceptDragDropPayload() was called and mouse button is released over the target item.</summary>
	public bool Delivery;

	public void Clear()
	{
		Data = null;
		DataType = null;
		SourceId = SourceParentId = 0;
		DataFrameCount = -1;
		Preview = Delivery = false;
	}

	public bool IsDataType( string type ) => DataFrameCount != -1 && DataType == type;
	public bool IsPreview() => Preview;
	public bool IsDelivery() => Delivery;

	/// <summary>Get the payload data cast to a type, or default if it is not of that type.</summary>
	public T GetData<T>() => Data is T t ? t : default;
}

internal partial class ImGuiContext
{
	public bool DragDropActive;
	public bool DragDropWithinSource;
	public bool DragDropWithinTarget;
	public ImGuiDragDropFlags DragDropSourceFlags;
	public int DragDropSourceFrameCount = -1;
	public int DragDropMouseButton = -1;
	public readonly ImGuiPayload DragDropPayload = new();
	public ImRect DragDropTargetRect;
	public ImRect DragDropTargetClipRect;
	public int DragDropTargetId;
	public ImGuiDragDropFlags DragDropAcceptFlags;
	public float DragDropAcceptIdCurrRectSurface = float.MaxValue;
	public int DragDropAcceptIdCurr;
	public int DragDropAcceptIdPrev;
	public int DragDropAcceptFrameCount = -1;
	public int DragDropHoldJustPressedId;
}

public static partial class ImGui
{
	internal static void NewFrameDragDrop()
	{
		var g = G;
		g.DragDropAcceptIdPrev = g.DragDropAcceptIdCurr;
		g.DragDropAcceptIdCurr = 0;
		g.DragDropAcceptIdCurrRectSurface = float.MaxValue;
		g.DragDropWithinSource = false;
		g.DragDropWithinTarget = false;
		g.DragDropHoldJustPressedId = 0;

		// Keep the source alive so even if the source disappears our state is consistent.
		if ( g.DragDropActive && g.DragDropPayload.SourceId == g.ActiveId )
			KeepAliveID( g.DragDropPayload.SourceId );
	}

	internal static void EndFrameDragDrop()
	{
		var g = G;

		// Elapse payload (if delivered, or if the source stopped being submitted)
		if ( g.DragDropActive )
		{
			bool isDelivered = g.DragDropPayload.Delivery;
			bool isElapsed = g.DragDropSourceFrameCount + 1 < g.FrameCount
				&& ((g.DragDropSourceFlags & ImGuiDragDropFlags.PayloadAutoExpire) != 0 || g.DragDropMouseButton == -1 || !IsMouseDown( (ImGuiMouseButton)g.DragDropMouseButton ));
			if ( isDelivered || isElapsed )
				ClearDragDrop();
		}

		// Fallback for a source that stopped submitting its tooltip.
		if ( g.DragDropActive && g.DragDropSourceFrameCount + 1 < g.FrameCount && (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceNoPreviewTooltip) == 0 )
		{
			g.DragDropWithinSource = true;
			SetTooltip( "..." );
			g.DragDropWithinSource = false;
		}
	}

	internal static void ClearDragDrop()
	{
		var g = G;
		g.DragDropActive = false;
		g.DragDropPayload.Clear();
		g.DragDropAcceptFlags = ImGuiDragDropFlags.None;
		g.DragDropAcceptIdCurr = g.DragDropAcceptIdPrev = 0;
		g.DragDropAcceptIdCurrRectSurface = float.MaxValue;
		g.DragDropAcceptFrameCount = -1;
		g.DragDropMouseButton = -1;
	}

	public static bool IsDragDropActive() => G.DragDropActive;

	/// <summary>
	/// Call after submitting an item which may be dragged. When this returns true, you can call SetDragDropPayload() + EndDragDropSource().
	/// </summary>
	public static bool BeginDragDropSource( ImGuiDragDropFlags flags = ImGuiDragDropFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;

		int mouseButton = (int)ImGuiMouseButton.Left;
		bool sourceDragActive;
		int sourceId;
		int sourceParentId = 0;

		if ( (flags & ImGuiDragDropFlags.SourceExtern) == 0 )
		{
			sourceId = g.LastItemData.ID;
			if ( sourceId != 0 )
			{
				// Common path: items with an ID
				if ( g.ActiveId != sourceId )
					return false;
				if ( g.ActiveIdMouseButton != -1 )
					mouseButton = g.ActiveIdMouseButton;
				if ( !g.IO.MouseDown[mouseButton] || window.SkipItems )
					return false;
				g.ActiveIdAllowOverlap = false;
			}
			else
			{
				// Uncommon path: items without an ID (e.g. Text(), Image())
				if ( !g.IO.MouseDown[mouseButton] || window.SkipItems )
					return false;
				if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredRect) == 0 && (g.ActiveId == 0 || g.ActiveIdWindow != window) )
					return false;
				if ( (flags & ImGuiDragDropFlags.SourceAllowNullID) == 0 )
				{
					Log.Warning( "ImGui: BeginDragDropSource() on an item without ID requires ImGuiDragDropFlags.SourceAllowNullID" );
					return false;
				}

				// Build a throwaway ID from the ID stack + the item rectangle. It won't survive the item moving.
				sourceId = g.LastItemData.ID = window.GetIDFromRectangle( g.LastItemData.Rect );
				KeepAliveID( sourceId );
				bool isHovered = ItemHoverable( g.LastItemData.Rect, sourceId, g.LastItemData.InFlags );
				if ( isHovered && g.IO.MouseClicked[mouseButton] )
				{
					SetActiveID( sourceId, window );
					g.ActiveIdMouseButton = mouseButton;
					FocusWindow( window );
				}
				if ( g.ActiveId == sourceId )
					g.ActiveIdAllowOverlap = isHovered;
			}
			if ( g.ActiveId != sourceId )
				return false;
			sourceParentId = window.IDStack[^1];
			sourceDragActive = IsMouseDragging( (ImGuiMouseButton)mouseButton );
		}
		else
		{
			// External source (e.g. driven by the game): always active while the button is down.
			window = null;
			sourceId = ImHashStr( "#SourceExtern", 0 );
			sourceDragActive = true;
			mouseButton = g.IO.MouseDown[0] ? 0 : -1;
			KeepAliveID( sourceId );
			SetActiveID( sourceId, null );
		}

		if ( !sourceDragActive )
			return false;

		// Activate drag and drop
		if ( !g.DragDropActive )
		{
			ClearDragDrop();
			var payload = g.DragDropPayload;
			payload.SourceId = sourceId;
			payload.SourceParentId = sourceParentId;
			g.DragDropActive = true;
			g.DragDropSourceFlags = flags;
			g.DragDropMouseButton = mouseButton;
			if ( payload.SourceId == g.ActiveId )
				g.ActiveIdNoClearOnFocusLoss = true;
		}
		g.DragDropSourceFrameCount = g.FrameCount;
		g.DragDropWithinSource = true;

		if ( (flags & ImGuiDragDropFlags.SourceNoPreviewTooltip) == 0 )
		{
			// Drag and drop tooltips are offset from the mouse cursor and semi-transparent.
			var tooltipPos = g.IO.MousePos + new Vector2( 16, 10 ) * g.Style.MouseCursorScale;
			SetNextWindowPos( tooltipPos );
			SetNextWindowBgAlpha( g.Style.Colors[(int)ImGuiCol.PopupBg].w * 0.60f );
			BeginTooltipEx();
			if ( g.DragDropAcceptIdPrev != 0 && (g.DragDropAcceptFlags & ImGuiDragDropFlags.AcceptNoPreviewTooltip) != 0 )
			{
				// The target asked us not to display the tooltip: keep it alive but hidden.
				g.CurrentWindow.Hidden = true;
				g.CurrentWindow.HiddenFramesCanSkipItems = 1;
			}
		}

		if ( (flags & ImGuiDragDropFlags.SourceNoDisableHover) == 0 && (flags & ImGuiDragDropFlags.SourceExtern) == 0 )
			g.LastItemData.StatusFlags &= ~ImGuiItemStatusFlags.HoveredRect;

		return true;
	}

	/// <summary>Only call EndDragDropSource() if BeginDragDropSource() returns true!</summary>
	public static void EndDragDropSource()
	{
		var g = G;
		if ( !g.DragDropActive || !g.DragDropWithinSource )
		{
			Log.Warning( "ImGui: EndDragDropSource() called without BeginDragDropSource()" );
			return;
		}

		if ( (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceNoPreviewTooltip) == 0 )
			EndTooltip();

		// Discard the drag if SetDragDropPayload() was never called.
		if ( g.DragDropPayload.DataFrameCount == -1 )
			ClearDragDrop();
		g.DragDropWithinSource = false;
	}

	/// <summary>
	/// Set the payload of the current drag source. type is a user defined tag (types starting with '_' are reserved).
	/// Returns true when the payload has been accepted by a target.
	/// </summary>
	public static bool SetDragDropPayload( string type, object data, ImGuiCond cond = ImGuiCond.None )
	{
		var g = G;
		var payload = g.DragDropPayload;
		if ( cond == ImGuiCond.None )
			cond = ImGuiCond.Always;

		if ( cond == ImGuiCond.Always || payload.DataFrameCount == -1 )
		{
			payload.DataType = type;
			payload.Data = data;
		}
		payload.DataFrameCount = g.FrameCount;

		return g.DragDropAcceptFrameCount == g.FrameCount || g.DragDropAcceptFrameCount == g.FrameCount - 1;
	}

	public static bool SetDragDropPayload<T>( string type, T data, ImGuiCond cond = ImGuiCond.None )
		=> SetDragDropPayload( type, (object)data, cond );

	/// <summary>Call after submitting an item that may receive a payload. If this returns true, you can call AcceptDragDropPayload() + EndDragDropTarget().</summary>
	public static bool BeginDragDropTarget()
	{
		var g = G;
		if ( !g.DragDropActive )
			return false;

		var window = g.CurrentWindow;
		if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredRect) == 0 )
			return false;
		var hoveredWindow = g.HoveredWindowUnderMovingWindow;
		if ( hoveredWindow is null || window.RootWindow != hoveredWindow.RootWindow || window.SkipItems )
			return false;

		var displayRect = (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HasDisplayRect) != 0 ? g.LastItemData.DisplayRect : g.LastItemData.Rect;
		int id = g.LastItemData.ID;
		if ( id == 0 )
		{
			id = window.GetIDFromRectangle( displayRect );
			KeepAliveID( id );
		}
		if ( g.DragDropPayload.SourceId == id )
			return false;

		g.DragDropTargetRect = displayRect;
		g.DragDropTargetClipRect = window.ClipRect;
		g.DragDropTargetId = id;
		g.DragDropWithinTarget = true;
		return true;
	}

	/// <summary>
	/// Accept contents of a given type. Returns the payload when it is delivered (or while previewing with AcceptBeforeDelivery), else null.
	/// </summary>
	public static ImGuiPayload AcceptDragDropPayload( string type, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None )
	{
		var g = G;
		var payload = g.DragDropPayload;
		if ( !g.DragDropActive || payload.DataFrameCount == -1 )
			return null;
		if ( type is not null && !payload.IsDataType( type ) )
			return null;

		// Accept the smallest drag target bounding box, so drag targets can be nested.
		bool wasAcceptedPreviously = g.DragDropAcceptIdPrev == g.DragDropTargetId;
		var r = g.DragDropTargetRect;
		float rSurface = r.Width * r.Height;
		if ( rSurface > g.DragDropAcceptIdCurrRectSurface )
			return null;

		g.DragDropAcceptFlags = flags;
		g.DragDropAcceptIdCurr = g.DragDropTargetId;
		g.DragDropAcceptIdCurrRectSurface = rSurface;

		payload.Preview = wasAcceptedPreviously;
		flags |= g.DragDropSourceFlags & ImGuiDragDropFlags.AcceptNoDrawDefaultRect;
		if ( (flags & ImGuiDragDropFlags.AcceptNoDrawDefaultRect) == 0 && payload.Preview )
			RenderDragDropTargetRect( r, g.DragDropTargetClipRect );

		g.DragDropAcceptFrameCount = g.FrameCount;
		if ( (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceExtern) != 0 && g.DragDropMouseButton == -1 )
			payload.Delivery = wasAcceptedPreviously && g.DragDropSourceFrameCount < g.FrameCount;
		else
			payload.Delivery = wasAcceptedPreviously && g.DragDropMouseButton >= 0 && !IsMouseDown( (ImGuiMouseButton)g.DragDropMouseButton );

		if ( !payload.Delivery && (flags & ImGuiDragDropFlags.AcceptBeforeDelivery) == 0 )
			return null;

		return payload;
	}

	internal static void RenderDragDropTargetRect( ImRect bb, ImRect itemClipRect )
	{
		var g = G;
		var window = g.CurrentWindow;
		var bbDisplay = bb;
		bbDisplay.ClipWith( itemClipRect );
		bbDisplay.Expand( 3.5f );
		bool pushClipRect = !window.ClipRect.Contains( bbDisplay );
		if ( pushClipRect )
			window.DrawList.PushClipRectFullScreen();
		window.DrawList.AddRect( bbDisplay.Min, bbDisplay.Max, GetColorU32Internal( ImGuiCol.DragDropTarget ), 0.0f, ImDrawFlags.None, 2.0f );
		if ( pushClipRect )
			window.DrawList.PopClipRect();
	}

	/// <summary>Only call EndDragDropTarget() if BeginDragDropTarget() returns true!</summary>
	public static void EndDragDropTarget()
	{
		var g = G;
		if ( !g.DragDropActive || !g.DragDropWithinTarget )
		{
			Log.Warning( "ImGui: EndDragDropTarget() called without BeginDragDropTarget()" );
			return;
		}
		g.DragDropWithinTarget = false;

		// Clear the payload right after delivery.
		if ( g.DragDropPayload.Delivery )
			ClearDragDrop();
	}

	/// <summary>Peek directly into the current payload from anywhere. Returns null when drag and drop is finished or inactive.</summary>
	public static ImGuiPayload GetDragDropPayload()
	{
		var g = G;
		return g.DragDropActive && g.DragDropPayload.DataFrameCount != -1 ? g.DragDropPayload : null;
	}
}
