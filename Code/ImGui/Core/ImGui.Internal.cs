using Duccsoft.ImGui.Rendering;

namespace Duccsoft.ImGui;

public static partial class ImGui
{
	internal static ImGuiContext G => ImGuiContext.Current;

	#region Hashing
	/// <summary>
	/// FNV-1a hash of a string, seeded. Supports "###" to reset the hash so that only the part after "###" counts.
	/// </summary>
	internal static int ImHashStr( string str, int seed )
	{
		unchecked
		{
			uint crc = (uint)seed ^ 2166136261u;
			uint seedU = crc;
			if ( str is null )
				return (int)crc;

			for ( int i = 0; i < str.Length; i++ )
			{
				char c = str[i];
				if ( c == '#' && i + 2 < str.Length && str[i + 1] == '#' && str[i + 2] == '#' )
					crc = seedU;
				crc ^= c;
				crc *= 16777619u;
			}
			int result = (int)crc;
			return result == 0 ? 1 : result;
		}
	}

	internal static int ImHashInt( int value, int seed )
	{
		unchecked
		{
			uint crc = (uint)seed ^ 2166136261u;
			for ( int i = 0; i < 4; i++ )
			{
				crc ^= (uint)((value >> (i * 8)) & 0xFF);
				crc *= 16777619u;
			}
			// Distinguish int ids from string ids of the same bytes.
			crc ^= 0x9E3779B9u;
			crc *= 16777619u;
			int result = (int)crc;
			return result == 0 ? 1 : result;
		}
	}
	#endregion

	#region Text utilities
	/// <summary>Returns the index of the end of the visible label (before "##"), or text.Length.</summary>
	internal static int FindRenderedTextEnd( string text )
	{
		if ( text is null ) return 0;
		int idx = text.IndexOf( "##", StringComparison.Ordinal );
		return idx < 0 ? text.Length : idx;
	}

	/// <summary>Returns the visible part of a label, stripping everything from "##".</summary>
	internal static string LabelText( string label )
	{
		if ( label is null ) return string.Empty;
		int end = FindRenderedTextEnd( label );
		return end == label.Length ? label : label.Substring( 0, end );
	}

	/// <summary>Measure a single line of text at a given font point size, in pixels.</summary>
	internal static float MeasureTextWidth( string line, float pointSize )
	{
		if ( string.IsNullOrEmpty( line ) ) return 0f;
		var g = G;
		string font = g?.FontName ?? "Roboto Mono";
		int weight = g?.FontWeight ?? 400;

		if ( g is not null )
		{
			if ( g.TextSizeCache.Count > 4096 )
				g.TextSizeCache.Clear();
			var key = string.Concat( line, "\u0001", pointSize.ToString( "F2" ) );
			if ( g.TextSizeCache.TryGetValue( key, out var cached ) )
				return cached.x;
			var size = MeasureTextRaw( line, pointSize, font, weight );
			g.TextSizeCache[key] = size;
			return size.x;
		}
		return MeasureTextRaw( line, pointSize, font, weight ).x;
	}

	private static bool _textMeasureUnavailable;

	internal static Vector2 MeasureTextRaw( string text, float pointSize, string font, int weight )
	{
		if ( !_textMeasureUnavailable )
		{
			try
			{
				var scope = new TextRendering.Scope( text, Color.White, pointSize, font, weight );
				var size = scope.Measure();
				if ( size.y > 0f || text.Length == 0 )
					return size;
			}
			catch ( Exception )
			{
				// Text rendering is unavailable (e.g. headless unit tests). Fall back to a monospace estimate.
				_textMeasureUnavailable = true;
			}
		}
		return new Vector2( text.Length * pointSize * 0.6f, MathF.Ceiling( pointSize * 1.17f ) );
	}

	/// <summary>Measure the natural line height of the font at a given point size.</summary>
	internal static float MeasureLineHeight( float pointSize, string font, int weight )
	{
		var size = MeasureTextRaw( "Hgjy|", pointSize, font, weight );
		return MathF.Max( 1f, MathF.Ceiling( size.y ) );
	}

	/// <summary>
	/// Split text into lines that fit within wrapWidth (pixels), breaking on spaces where possible.
	/// </summary>
	internal static List<string> WrapText( string text, float wrapWidth, float pointSize )
	{
		var result = new List<string>();
		if ( text is null ) return result;
		var paragraphs = text.Split( '\n' );
		foreach ( var para in paragraphs )
		{
			if ( para.Length == 0 )
			{
				result.Add( string.Empty );
				continue;
			}

			int lineStart = 0;
			while ( lineStart < para.Length )
			{
				// Find the longest prefix that fits.
				int lastBreak = -1;
				int i = lineStart;
				int fitEnd = lineStart;
				while ( i < para.Length )
				{
					int next = i + 1;
					float w = MeasureTextWidth( para.Substring( lineStart, next - lineStart ), pointSize );
					if ( w > wrapWidth && next - lineStart > 1 )
						break;
					if ( para[i] == ' ' )
						lastBreak = i;
					fitEnd = next;
					i = next;
				}

				if ( fitEnd >= para.Length )
				{
					result.Add( para.Substring( lineStart ) );
					break;
				}

				int breakAt = lastBreak > lineStart ? lastBreak : fitEnd;
				result.Add( para.Substring( lineStart, breakAt - lineStart ).TrimEnd() );
				lineStart = breakAt;
				while ( lineStart < para.Length && para[lineStart] == ' ' )
					lineStart++;
			}
		}
		return result;
	}

	/// <summary>
	/// Calculate the size of a text, in pixels. Handles multiple lines and "##" hiding.
	/// </summary>
	public static Vector2 CalcTextSize( string text, bool hideTextAfterDoubleHash = false, float wrapWidth = -1.0f )
	{
		var g = G;
		if ( string.IsNullOrEmpty( text ) )
			return new Vector2( 0f, g?.FontSize ?? 0f );

		if ( hideTextAfterDoubleHash )
			text = LabelText( text );

		float lineHeight = g.FontSize;
		float pointSize = g.FontPointSize;
		if ( text.Length == 0 )
			return new Vector2( 0f, lineHeight );

		if ( wrapWidth > 0f )
		{
			var lines = WrapText( text, wrapWidth, pointSize );
			float maxW = 0f;
			foreach ( var l in lines )
				maxW = MathF.Max( maxW, MeasureTextWidth( l, pointSize ) );
			return new Vector2( MathF.Ceiling( maxW ), lines.Count * lineHeight );
		}

		float width = 0f;
		int lineCount = 0;
		int start = 0;
		while ( true )
		{
			int nl = text.IndexOf( '\n', start );
			int end = nl < 0 ? text.Length : nl;
			width = MathF.Max( width, MeasureTextWidth( text.Substring( start, end - start ), pointSize ) );
			lineCount++;
			if ( nl < 0 ) break;
			start = nl + 1;
		}
		return new Vector2( MathF.Ceiling( width ), lineCount * lineHeight );
	}

	/// <summary>Legacy signature kept for API compatibility.</summary>
	public static Vector2 CalcTextSize( string text, string textEnd, bool hideTextAfterDoubleHash = false, float wrapWidth = -1.0f )
		=> CalcTextSize( text, hideTextAfterDoubleHash, wrapWidth );

	internal static string Format( string fmt, params object[] args )
	{
		if ( args is null || args.Length == 0 )
			return fmt ?? string.Empty;
		try
		{
			return string.Format( fmt, args );
		}
		catch ( FormatException )
		{
			return fmt;
		}
	}
	#endregion

	#region Window / item helpers
	internal static ImGuiWindow GetCurrentWindow()
	{
		var g = G;
		g.CurrentWindow.WriteAccessed = true;
		return g.CurrentWindow;
	}

	internal static ImGuiWindow GetCurrentWindowRead() => G.CurrentWindow;

	internal static void SetActiveID( int id, ImGuiWindow window )
	{
		var g = G;
		g.ActiveIdIsJustActivated = g.ActiveId != id;
		if ( g.ActiveIdIsJustActivated )
		{
			if ( g.ActiveId != 0 )
				g.DeactivatedItemDataId = g.ActiveId;
			g.ActiveIdTimer = 0f;
			g.ActiveIdHasBeenPressedBefore = false;
			g.ActiveIdHasBeenEditedBefore = false;
			g.ActiveIdMouseButton = -1;
			if ( id != 0 )
			{
				g.LastActiveId = id;
				g.LastActiveIdTimer = 0f;
			}
		}
		g.ActiveId = id;
		g.ActiveIdAllowOverlap = false;
		g.ActiveIdNoClearOnFocusLoss = false;
		g.ActiveIdWindow = window;
		g.ActiveIdHasBeenEditedThisFrame = false;
		if ( id != 0 )
			g.ActiveIdIsAlive = id;
	}

	internal static void ClearActiveID() => SetActiveID( 0, null );

	internal static void SetHoveredID( int id )
	{
		var g = G;
		g.HoveredId = id;
		g.HoveredIdAllowOverlap = false;
		if ( id != 0 && g.HoveredIdPreviousFrame != id )
			g.HoveredIdTimer = g.HoveredIdNotActiveTimer = 0f;
	}

	internal static void KeepAliveID( int id )
	{
		var g = G;
		if ( g.ActiveId == id )
			g.ActiveIdIsAlive = id;
		if ( g.ActiveIdPreviousFrame == id )
			g.ActiveIdPreviousFrameIsAlive = true;
	}

	internal static void MarkItemEdited( int id )
	{
		var g = G;
		if ( g.ActiveId == id || g.ActiveId == 0 )
		{
			g.ActiveIdHasBeenEditedThisFrame = true;
			g.ActiveIdHasBeenEditedBefore = true;
		}
		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Edited;
	}

	/// <summary>
	/// Advance the layout cursor after an item of the given size. Text baseline is used to vertically align items on the same line.
	/// </summary>
	internal static void ItemSize( Vector2 size, float textBaselineY = -1.0f )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;

		var dc = window.DC;
		float offsetToMatchBaselineY = textBaselineY >= 0 ? MathF.Max( 0.0f, dc.CurrLineTextBaseOffset - textBaselineY ) : 0.0f;
		float lineY1 = dc.IsSameLine ? dc.CursorPosPrevLine.y : dc.CursorPos.y;
		float lineHeight = MathF.Max( dc.CurrLineSize.y, dc.CursorPos.y - lineY1 + size.y + offsetToMatchBaselineY );

		dc.CursorPosPrevLine = new Vector2( dc.CursorPos.x + size.x, lineY1 );
		dc.CursorPos = new Vector2(
			ImTrunc( window.Pos.x + dc.Indent + dc.ColumnsOffset ),
			ImTrunc( lineY1 + lineHeight + g.Style.ItemSpacing.y ) );
		dc.CursorMaxPos = new Vector2( MathF.Max( dc.CursorMaxPos.x, dc.CursorPosPrevLine.x ), MathF.Max( dc.CursorMaxPos.y, dc.CursorPos.y - g.Style.ItemSpacing.y ) );

		dc.PrevLineSize = new Vector2( dc.PrevLineSize.x, lineHeight );
		dc.CurrLineSize = new Vector2( dc.CurrLineSize.x, 0f );
		dc.PrevLineTextBaseOffset = MathF.Max( dc.CurrLineTextBaseOffset, textBaselineY );
		dc.CurrLineTextBaseOffset = 0.0f;
		dc.IsSameLine = dc.IsSetPos = false;

		if ( dc.LayoutType == ImGuiLayoutType.Horizontal )
			SameLine();
	}

	internal static void ItemSize( ImRect bb, float textBaselineY = -1.0f ) => ItemSize( bb.Size, textBaselineY );

	/// <summary>
	/// Declare an item: sets the last item data, handles clipping. Returns false if the item is clipped and should not be drawn.
	/// </summary>
	internal static bool ItemAdd( ImRect bb, int id, ImRect? navBb = null, ImGuiItemFlags extraFlags = ImGuiItemFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;

		g.LastItemData.ID = id;
		g.LastItemData.Rect = bb;
		g.LastItemData.NavRect = navBb ?? bb;
		g.LastItemData.InFlags = g.CurrentItemFlags | g.NextItemData.ItemFlags | extraFlags;
		g.LastItemData.StatusFlags = ImGuiItemStatusFlags.None;
		g.NextItemData.ClearFlags();

		if ( id != 0 )
		{
			KeepAliveID( id );

			// Keyboard focus requests (SetKeyboardFocusHere)
			if ( g.FocusRequestWindow == window && (g.LastItemData.InFlags & ImGuiItemFlags.NoTabStop) == 0 && (g.LastItemData.InFlags & ImGuiItemFlags.Inputable) != 0 )
			{
				if ( g.FocusItemCounter == g.FocusRequestCounter )
				{
					g.FocusedTextInputRequestId = id;
					g.FocusRequestWindow = null;
				}
				g.FocusItemCounter++;
			}
		}

		bool isRectVisible = bb.Overlaps( window.ClipRect );
		if ( !isRectVisible )
		{
			// Active items still need to process input even when scrolled out of view.
			if ( id == 0 || (id != g.ActiveId && id != g.FocusedTextInputRequestId) )
				return false;
		}

		if ( isRectVisible )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Visible;
		if ( IsMouseHoveringRect( bb.Min, bb.Max ) )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredRect;
		return true;
	}

	internal static bool IsClippedEx( ImRect bb, int id )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( !bb.Overlaps( window.ClipRect ) )
			if ( id == 0 || (id != g.ActiveId && id != g.ActiveIdPreviousFrame) )
				return true;
		return false;
	}

	internal static bool IsWindowWithinBeginStackOf( ImGuiWindow window, ImGuiWindow potentialParent )
	{
		if ( window.RootWindow == potentialParent )
			return true;
		while ( window is not null )
		{
			if ( window == potentialParent )
				return true;
			window = window.ParentWindowInBeginStack;
		}
		return false;
	}

	internal static bool IsWindowChildOf( ImGuiWindow window, ImGuiWindow potentialParent, bool popupHierarchy )
	{
		var windowRoot = popupHierarchy ? window.RootWindowPopupTree : window.RootWindow;
		if ( windowRoot == potentialParent )
			return true;
		while ( window is not null )
		{
			if ( window == potentialParent )
				return true;
			if ( window == windowRoot )
				return false;
			window = window.ParentWindow;
		}
		return false;
	}

	internal static ImGuiWindow GetTopMostPopupModal()
	{
		var g = G;
		for ( int n = g.OpenPopupStack.Count - 1; n >= 0; n-- )
		{
			var popup = g.OpenPopupStack[n].Window;
			if ( popup is not null && (popup.Flags & ImGuiWindowFlags.Modal) != 0 && (popup.Active || popup.WasActive) )
				return popup;
		}
		return null;
	}

	internal static bool IsWindowContentHoverable( ImGuiWindow window, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None )
	{
		var g = G;
		var focusedRoot = g.NavWindow?.RootWindow;
		if ( focusedRoot is not null && focusedRoot.WasActive && focusedRoot != window.RootWindow )
		{
			bool wantInhibit = false;
			if ( (focusedRoot.Flags & ImGuiWindowFlags.Modal) != 0 )
				wantInhibit = true;
			else if ( (focusedRoot.Flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiHoveredFlags.AllowWhenBlockedByPopup) == 0 )
				wantInhibit = true;

			if ( wantInhibit && !IsWindowWithinBeginStackOf( window.RootWindow, focusedRoot ) )
				return false;
		}
		return true;
	}

	/// <summary>
	/// Internal hover test used by all interactive widgets: checks window, clipping, overlap, active item and popups.
	/// </summary>
	internal static bool ItemHoverable( ImRect bb, int id, ImGuiItemFlags itemFlags )
	{
		var g = G;
		var window = g.CurrentWindow;

		if ( g.HoveredWindow != window )
			return false;
		if ( !IsMouseHoveringRect( bb.Min, bb.Max ) )
			return false;
		if ( g.HoveredId != 0 && g.HoveredId != id && !g.HoveredIdAllowOverlap )
			return false;
		// An item that allows overlap yields hover to whichever overlapping item was hovered last frame.
		if ( (itemFlags & ImGuiItemFlags.AllowOverlap) != 0 && id != 0 && g.HoveredIdPreviousFrame != id && g.HoveredIdPreviousFrame != 0 )
			return false;
		if ( g.ActiveId != 0 && g.ActiveId != id && !g.ActiveIdAllowOverlap )
			return false;
		if ( (itemFlags & ImGuiItemFlags.NoWindowHoverableCheck) == 0 && !IsWindowContentHoverable( window ) )
			return false;

		if ( (itemFlags & ImGuiItemFlags.Disabled) != 0 )
		{
			if ( g.ActiveId == id && id != 0 )
				ClearActiveID();
			g.HoveredIdDisabled = true;
			return false;
		}

		if ( id != 0 )
		{
			SetHoveredID( id );
			if ( (itemFlags & ImGuiItemFlags.AllowOverlap) != 0 )
				g.HoveredIdAllowOverlap = true;
		}
		return true;
	}

	internal static bool ButtonBehavior( ImRect bb, int id, out bool outHovered, out bool outHeld, ImGuiButtonFlags flags = ImGuiButtonFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;
		var io = g.IO;

		var itemFlags = g.LastItemData.ID == id ? g.LastItemData.InFlags : g.CurrentItemFlags;
		if ( (flags & ImGuiButtonFlags.AllowOverlap) != 0 )
			itemFlags |= ImGuiItemFlags.AllowOverlap;

		if ( (flags & ImGuiButtonFlags.MouseButtonMask_) == 0 )
			flags |= ImGuiButtonFlags.MouseButtonLeft;
		if ( (flags & ImGuiButtonFlags.PressedOnMask_) == 0 )
			flags |= ImGuiButtonFlags.PressedOnDefault_;

		// FlattenChildren: allow hovering when the hovered window is a child of this one
		var backupHoveredWindow = g.HoveredWindow;
		bool flattenHoveredChildren = (flags & ImGuiButtonFlags.FlattenChildren) != 0 && g.HoveredWindow is not null && g.HoveredWindow.RootWindow == window.RootWindow;
		if ( flattenHoveredChildren )
			g.HoveredWindow = window;

		bool pressed = false;
		bool hovered = ItemHoverable( bb, id, itemFlags );

		// Drag and drop: hovering a button with a payload for a while activates it.
		if ( g.DragDropActive && (flags & ImGuiButtonFlags.PressedOnDragDropHold) != 0 && (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceNoHoldToOpenOthers) == 0 )
		{
			if ( g.HoveredWindow == window && IsMouseHoveringRect( bb.Min, bb.Max ) )
			{
				hovered = true;
				SetHoveredID( id );
				if ( g.HoveredIdTimer - io.DeltaTime <= 0.70f && g.HoveredIdTimer >= 0.70f )
				{
					pressed = true;
					g.DragDropHoldJustPressedId = id;
					FocusWindow( window );
				}
			}
		}

		if ( flattenHoveredChildren )
			g.HoveredWindow = backupHoveredWindow;

		if ( hovered )
		{
			int mouseButtonClicked = -1;
			int mouseButtonReleased = -1;
			for ( int button = 0; button < 3; button++ )
			{
				if ( ((int)flags & ((int)ImGuiButtonFlags.MouseButtonLeft << button)) == 0 )
					continue;
				if ( io.MouseClicked[button] && mouseButtonClicked == -1 ) mouseButtonClicked = button;
				if ( io.MouseReleased[button] && mouseButtonReleased == -1 ) mouseButtonReleased = button;
			}

			bool keyModsOk = (flags & ImGuiButtonFlags.NoKeyModsAllowed) == 0 || !(io.KeyCtrl || io.KeyShift || io.KeyAlt);
			if ( keyModsOk )
			{
				if ( mouseButtonClicked != -1 && g.ActiveId != id )
				{
					if ( (flags & (ImGuiButtonFlags.PressedOnClickRelease | ImGuiButtonFlags.PressedOnClickReleaseAnywhere)) != 0 )
					{
						SetActiveID( id, window );
						g.ActiveIdMouseButton = mouseButtonClicked;
						if ( (flags & ImGuiButtonFlags.NoNavFocus) == 0 )
							g.NavId = id;
						FocusWindow( window );
					}
					if ( (flags & ImGuiButtonFlags.PressedOnClick) != 0 || ((flags & ImGuiButtonFlags.PressedOnDoubleClick) != 0 && io.MouseClickedCount[mouseButtonClicked] == 2) )
					{
						pressed = true;
						if ( (flags & ImGuiButtonFlags.NoHoldingActiveId) != 0 )
							ClearActiveID();
						else
							SetActiveID( id, window );
						g.ActiveIdMouseButton = mouseButtonClicked;
						if ( (flags & ImGuiButtonFlags.NoNavFocus) == 0 )
							g.NavId = id;
						FocusWindow( window );
					}
				}
				if ( (flags & ImGuiButtonFlags.PressedOnRelease) != 0 && mouseButtonReleased != -1 )
				{
					bool hasRepeatedAtLeastOnce = (itemFlags & ImGuiItemFlags.ButtonRepeat) != 0 && io.MouseDownDurationPrev[mouseButtonReleased] >= io.KeyRepeatDelay;
					if ( !hasRepeatedAtLeastOnce )
						pressed = true;
					if ( (flags & ImGuiButtonFlags.NoNavFocus) == 0 )
						g.NavId = id;
					ClearActiveID();
				}

				// Repeat mode
				if ( g.ActiveId == id && (itemFlags & ImGuiItemFlags.ButtonRepeat) != 0 && g.ActiveIdMouseButton >= 0 )
					if ( io.MouseDownDuration[g.ActiveIdMouseButton] > 0f && IsMouseClicked( (ImGuiMouseButton)g.ActiveIdMouseButton, true ) )
						pressed = true;
			}
		}

		bool held = false;
		if ( g.ActiveId == id )
		{
			if ( g.ActiveIdIsJustActivated )
				g.ActiveIdClickOffset = io.MousePos - bb.Min;

			int mouseButton = g.ActiveIdMouseButton;
			if ( mouseButton == -1 )
			{
				// Activated by something else (e.g. from code); release on mouse up.
				if ( !io.MouseDown[0] )
					ClearActiveID();
			}
			else if ( io.MouseDown[mouseButton] )
			{
				held = true;
			}
			else
			{
				bool releaseIn = hovered && (flags & ImGuiButtonFlags.PressedOnClickRelease) != 0;
				bool releaseAnywhere = (flags & ImGuiButtonFlags.PressedOnClickReleaseAnywhere) != 0;
				if ( (releaseIn || releaseAnywhere) && !g.DragDropActive )
				{
					bool isDoubleClickRelease = (flags & ImGuiButtonFlags.PressedOnDoubleClick) != 0 && io.MouseReleased[mouseButton] && io.MouseClickedLastCount[mouseButton] == 2;
					bool isRepeatingAlready = (itemFlags & ImGuiItemFlags.ButtonRepeat) != 0 && io.MouseDownDurationPrev[mouseButton] >= io.KeyRepeatDelay;
					if ( !isDoubleClickRelease && !isRepeatingAlready )
						pressed = true;
				}
				ClearActiveID();
			}
		}

		if ( pressed )
			g.ActiveIdHasBeenPressedBefore = true;

		outHovered = hovered;
		outHeld = held;
		return pressed;
	}

	internal static float CalcWrapWidthForPos( Vector2 pos, float wrapPosX )
	{
		if ( wrapPosX < 0.0f )
			return 0.0f;

		var g = G;
		var window = g.CurrentWindow;
		if ( wrapPosX == 0.0f )
			wrapPosX = window.WorkRect.Max.x;
		else if ( wrapPosX > 0.0f )
			wrapPosX += window.Pos.x - window.Scroll.x;

		return MathF.Max( wrapPosX - pos.x, 1.0f );
	}

	/// <summary>
	/// Calculate an item size from a requested size: 0 = default, &lt;0 = relative to the right edge of the content region.
	/// </summary>
	internal static Vector2 CalcItemSize( Vector2 size, float defaultW, float defaultH )
	{
		var window = G.CurrentWindow;
		Vector2 regionMax = default;
		if ( size.x < 0.0f || size.y < 0.0f )
			regionMax = GetContentRegionMaxAbs();

		if ( size.x == 0.0f )
			size.x = defaultW;
		else if ( size.x < 0.0f )
			size.x = MathF.Max( 4.0f, regionMax.x - window.DC.CursorPos.x + size.x );

		if ( size.y == 0.0f )
			size.y = defaultH;
		else if ( size.y < 0.0f )
			size.y = MathF.Max( 4.0f, regionMax.y - window.DC.CursorPos.y + size.y );

		return size;
	}

	internal static Vector2 GetContentRegionMaxAbs()
	{
		var window = G.CurrentWindow;
		var mx = window.ContentRegionRect.Max;
		if ( window.DC.CurrentColumns is not null || window.DC.CurrentTableIdx >= 0 )
			mx.x = window.WorkRect.Max.x;
		return mx;
	}

	/// <summary>Width of the next item, from SetNextItemWidth / PushItemWidth.</summary>
	public static float CalcItemWidth()
	{
		var g = G;
		var window = g.CurrentWindow;
		float w;
		if ( (g.NextItemData.Flags & ImGuiNextItemDataFlags.HasWidth) != 0 )
			w = g.NextItemData.Width;
		else
			w = window.DC.ItemWidth;

		if ( w < 0.0f )
		{
			float regionMaxX = GetContentRegionMaxAbs().x;
			w = MathF.Max( 1.0f, regionMaxX - window.DC.CursorPos.x + w );
		}
		return ImTrunc( w );
	}
	#endregion

	#region Render helpers
	internal static Color32 GetColorU32Internal( ImGuiCol idx, float alphaMul = 1.0f )
	{
		var g = G;
		var c = g.Style.Colors[(int)idx];
		c.w *= g.Style.Alpha * alphaMul;
		return ColorConvertFloat4ToU32( c );
	}

	internal static void RenderText( Vector2 pos, string text, bool hideTextAfterHash = true )
	{
		var window = G.CurrentWindow;
		if ( string.IsNullOrEmpty( text ) ) return;
		if ( hideTextAfterHash )
			text = LabelText( text );
		if ( text.Length == 0 ) return;
		window.DrawList.AddText( 0f, pos, GetColorU32Internal( ImGuiCol.Text ), text );
	}

	internal static void RenderTextWrapped( Vector2 pos, string text, float wrapWidth )
	{
		var window = G.CurrentWindow;
		if ( string.IsNullOrEmpty( text ) ) return;
		window.DrawList.AddText( 0f, pos, GetColorU32Internal( ImGuiCol.Text ), text, wrapWidth );
	}

	/// <summary>
	/// Render text aligned within a rect, clipped to clipRect (or the rect itself).
	/// </summary>
	internal static void RenderTextClipped( Vector2 posMin, Vector2 posMax, string text, Vector2? textSizeIfKnown, Vector2 align, ImRect? clipRect = null )
	{
		if ( string.IsNullOrEmpty( text ) ) return;
		text = LabelText( text );
		if ( text.Length == 0 ) return;
		var window = G.CurrentWindow;
		RenderTextClippedEx( window.DrawList, posMin, posMax, text, textSizeIfKnown, align, clipRect );
	}

	internal static void RenderTextClippedEx( ImDrawList drawList, Vector2 posMin, Vector2 posMax, string text, Vector2? textSizeIfKnown, Vector2 align, ImRect? clipRect, Color32? color = null )
	{
		var textSize = textSizeIfKnown ?? CalcTextSize( text );
		var pos = posMin;
		var clipMin = clipRect?.Min ?? posMin;
		var clipMax = clipRect?.Max ?? posMax;
		bool needClipping = pos.x + textSize.x >= clipMax.x || pos.y + textSize.y >= clipMax.y;
		if ( clipRect.HasValue )
			needClipping |= pos.x < clipMin.x || pos.y < clipMin.y;

		if ( align.x > 0.0f ) pos.x = MathF.Max( pos.x, pos.x + (posMax.x - pos.x - textSize.x) * align.x );
		if ( align.y > 0.0f ) pos.y = MathF.Max( pos.y, pos.y + (posMax.y - pos.y - textSize.y) * align.y );

		var col = color ?? GetColorU32Internal( ImGuiCol.Text );
		if ( needClipping )
			drawList.AddText( 0f, ImFloor( pos ), col, text, 0f, new ImRect( clipMin, clipMax ) );
		else
			drawList.AddText( 0f, ImFloor( pos ), col, text );
	}

	/// <summary>
	/// Render text clipped to posMax.x, replacing the end with an ellipsis if it does not fit.
	/// </summary>
	internal static void RenderTextEllipsis( ImDrawList drawList, Vector2 posMin, Vector2 posMax, float clipMaxX, float ellipsisMaxX, string text, Vector2? textSizeIfKnown )
	{
		text = LabelText( text );
		var textSize = textSizeIfKnown ?? CalcTextSize( text );
		if ( textSize.x > posMax.x - posMin.x )
		{
			const string ellipsis = "...";
			float ellipsisWidth = CalcTextSize( ellipsis ).x;
			float available = MathF.Max( 0f, ellipsisMaxX - posMin.x - ellipsisWidth );
			int len = text.Length;
			while ( len > 0 && CalcTextSize( text.Substring( 0, len ) ).x > available )
				len--;
			var clipped = text.Substring( 0, len ).TrimEnd() + ellipsis;
			RenderTextClippedEx( drawList, posMin, new Vector2( clipMaxX, posMax.y ), clipped, null, Vector2.Zero, null );
		}
		else
		{
			RenderTextClippedEx( drawList, posMin, new Vector2( clipMaxX, posMax.y ), text, textSize, Vector2.Zero, null );
		}
	}

	internal static void RenderFrame( Vector2 pMin, Vector2 pMax, Color32 fillCol, bool border = true, float rounding = 0.0f )
	{
		var g = G;
		var window = g.CurrentWindow;
		window.DrawList.AddRectFilled( pMin, pMax, fillCol, rounding );
		float borderSize = g.Style.FrameBorderSize;
		if ( border && borderSize > 0.0f )
		{
			window.DrawList.AddRect( pMin + Vector2.One, pMax + Vector2.One, GetColorU32Internal( ImGuiCol.BorderShadow ), rounding, ImDrawFlags.None, borderSize );
			window.DrawList.AddRect( pMin, pMax, GetColorU32Internal( ImGuiCol.Border ), rounding, ImDrawFlags.None, borderSize );
		}
	}

	internal static void RenderFrameBorder( Vector2 pMin, Vector2 pMax, float rounding = 0.0f )
	{
		var g = G;
		var window = g.CurrentWindow;
		float borderSize = g.Style.FrameBorderSize;
		if ( borderSize > 0.0f )
		{
			window.DrawList.AddRect( pMin + Vector2.One, pMax + Vector2.One, GetColorU32Internal( ImGuiCol.BorderShadow ), rounding, ImDrawFlags.None, borderSize );
			window.DrawList.AddRect( pMin, pMax, GetColorU32Internal( ImGuiCol.Border ), rounding, ImDrawFlags.None, borderSize );
		}
	}

	/// <summary>Render an arrow (triangle) pointing in a direction. scale 1.0 = font size.</summary>
	internal static void RenderArrow( ImDrawList drawList, Vector2 pos, Color32 col, ImGuiDir dir, float scale = 1.0f )
	{
		var g = G;
		float h = g.FontSize;
		float r = h * 0.40f * scale;
		var center = pos + new Vector2( h * 0.50f, h * 0.50f * scale );

		Vector2 a, b, c;
		switch ( dir )
		{
			case ImGuiDir.Up:
			case ImGuiDir.Down:
				if ( dir == ImGuiDir.Up ) r = -r;
				a = new Vector2( +0.000f, +0.750f ) * r;
				b = new Vector2( -0.866f, -0.750f ) * r;
				c = new Vector2( +0.866f, -0.750f ) * r;
				break;
			case ImGuiDir.Left:
			case ImGuiDir.Right:
				if ( dir == ImGuiDir.Left ) r = -r;
				a = new Vector2( +0.750f, +0.000f ) * r;
				b = new Vector2( -0.750f, +0.866f ) * r;
				c = new Vector2( -0.750f, -0.866f ) * r;
				break;
			default:
				return;
		}
		drawList.AddTriangleFilled( center + a, center + b, center + c, col );
	}

	internal static void RenderBullet( ImDrawList drawList, Vector2 pos, Color32 col )
	{
		drawList.AddCircleFilled( pos, G.FontSize * 0.20f, col );
	}

	internal static void RenderCheckMark( ImDrawList drawList, Vector2 pos, Color32 col, float sz )
	{
		float thickness = MathF.Max( sz / 5.0f, 1.0f );
		sz -= thickness * 0.5f;
		pos += new Vector2( thickness * 0.25f, thickness * 0.25f );

		float third = sz / 3.0f;
		float bx = pos.x + third;
		float by = pos.y + sz - third * 0.5f;
		drawList.AddPolyline( new[]
		{
			new Vector2( bx - third, by - third ),
			new Vector2( bx, by ),
			new Vector2( bx + third * 2.0f, by - third * 2.0f ),
		}, col, ImDrawFlags.None, thickness );
	}

	internal static void RenderRectFilledRangeH( ImDrawList drawList, ImRect rect, Color32 col, float xStartNorm, float xEndNorm, float rounding )
	{
		if ( xEndNorm == xStartNorm ) return;
		if ( xStartNorm > xEndNorm ) (xStartNorm, xEndNorm) = (xEndNorm, xStartNorm);
		var p0 = new Vector2( ImLerp( rect.Min.x, rect.Max.x, xStartNorm ), rect.Min.y );
		var p1 = new Vector2( ImLerp( rect.Min.x, rect.Max.x, xEndNorm ), rect.Max.y );
		drawList.PushClipRect( p0, p1, true );
		drawList.AddRectFilled( rect.Min, rect.Max, col, rounding );
		drawList.PopClipRect();
	}

	/// <summary>Checkerboard background for colors with alpha.</summary>
	internal static void RenderColorRectWithAlphaCheckerboard( ImDrawList drawList, Vector2 pMin, Vector2 pMax, Color32 col, float gridStep, Vector2 gridOff, float rounding = 0.0f, ImDrawFlags flags = ImDrawFlags.None )
	{
		if ( col.a < 255 )
		{
			var colBg1 = new Color32( 204, 204, 204, 255 );
			var colBg2 = new Color32( 128, 128, 128, 255 );
			drawList.AddRectFilled( pMin, pMax, colBg1, rounding, flags );
			drawList.PushClipRect( pMin, pMax, true );
			int yi = 0;
			for ( float y = pMin.y + gridOff.y; y < pMax.y; y += gridStep, yi++ )
			{
				float y1 = Math.Clamp( y, pMin.y, pMax.y ), y2 = MathF.Min( y + gridStep, pMax.y );
				if ( y2 <= y1 ) continue;
				for ( float x = pMin.x + gridOff.x + (yi & 1) * gridStep; x < pMax.x; x += gridStep * 2.0f )
				{
					float x1 = Math.Clamp( x, pMin.x, pMax.x ), x2 = MathF.Min( x + gridStep, pMax.x );
					if ( x2 <= x1 ) continue;
					drawList.AddRectFilled( new Vector2( x1, y1 ), new Vector2( x2, y2 ), colBg2 );
				}
			}
			drawList.PopClipRect();
		}
		drawList.AddRectFilled( pMin, pMax, col, rounding, flags );
	}
	#endregion
}
