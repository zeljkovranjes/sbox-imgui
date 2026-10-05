using Duccsoft.ImGui.Rendering;

namespace Duccsoft.ImGui;

public static partial class ImGui
{
	private const float FLT_MAX = float.MaxValue;
	private const float WINDOWS_RESIZE_FROM_EDGES_FEEDBACK_TIMER = 0.04f;

	private static float WindowsHoverPadding => MathF.Max( G.Style.TouchExtraPadding.x, G.Style.WindowBorderHoverPadding );

	internal static float Axis( Vector2 v, int axis ) => axis == 0 ? v.x : v.y;
	internal static Vector2 WithAxis( Vector2 v, int axis, float value ) => axis == 0 ? new Vector2( value, v.y ) : new Vector2( v.x, value );

	private struct ResizeGripDef
	{
		public Vector2 CornerPosN;
		public Vector2 InnerDir;
		public int AngleMin12, AngleMax12;
	}

	private static readonly ResizeGripDef[] ResizeGripDefs =
	{
		new() { CornerPosN = new Vector2( 1, 1 ), InnerDir = new Vector2( -1, -1 ), AngleMin12 = 0, AngleMax12 = 3 }, // Lower-right
		new() { CornerPosN = new Vector2( 0, 1 ), InnerDir = new Vector2( +1, -1 ), AngleMin12 = 3, AngleMax12 = 6 }, // Lower-left
	};

	private struct ResizeBorderDef
	{
		public Vector2 SegmentN1, SegmentN2;
	}

	private static readonly ResizeBorderDef[] ResizeBorderDefs =
	{
		new() { SegmentN1 = new Vector2( 0, 1 ), SegmentN2 = new Vector2( 0, 0 ) }, // Left
		new() { SegmentN1 = new Vector2( 1, 0 ), SegmentN2 = new Vector2( 1, 1 ) }, // Right
		new() { SegmentN1 = new Vector2( 0, 0 ), SegmentN2 = new Vector2( 1, 0 ) }, // Up
		new() { SegmentN1 = new Vector2( 1, 1 ), SegmentN2 = new Vector2( 0, 1 ) }, // Down
	};

	#region Window lookup / creation
	internal static ImGuiWindow FindWindowByID( int id )
	{
		G.WindowsById.TryGetValue( id, out var window );
		return window;
	}

	internal static ImGuiWindow FindWindowByName( string name ) => FindWindowByID( ImHashStr( name, 0 ) );

	private static ImGuiWindow CreateNewWindow( string name, ImGuiWindowFlags flags )
	{
		var g = G;
		var window = new ImGuiWindow( g, name ) { Flags = flags };
		g.WindowsById[window.ID] = window;

		window.Pos = ImTrunc( new Vector2( 60, 60 ) * g.AppliedStyleScale );

		if ( (flags & ImGuiWindowFlags.NoSavedSettings) == 0 && g.SettingsWindows.TryGetValue( window.ID, out var settings ) )
		{
			window.Pos = settings.Pos;
			window.Size = window.SizeFull = settings.Size;
			window.Collapsed = settings.Collapsed;
			SetWindowConditionAllowFlags( window, ImGuiCond.FirstUseEver, false );
		}

		if ( (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 )
		{
			window.AutoFitFramesX = window.AutoFitFramesY = 2;
			window.AutoFitOnlyGrows = false;
		}
		else
		{
			if ( window.Size.x <= 0.0f ) window.AutoFitFramesX = 2;
			if ( window.Size.y <= 0.0f ) window.AutoFitFramesY = 2;
			window.AutoFitOnlyGrows = window.AutoFitFramesX > 0 || window.AutoFitFramesY > 0;
		}

		if ( (flags & ImGuiWindowFlags.ChildWindow) == 0 )
		{
			g.WindowsFocusOrder.Add( window );
			window.FocusOrder = g.WindowsFocusOrder.Count - 1;
		}

		if ( (flags & ImGuiWindowFlags.NoBringToFrontOnFocus) != 0 )
			g.Windows.Insert( 0, window );
		else
			g.Windows.Add( window );

		return window;
	}

	private static void SetWindowConditionAllowFlags( ImGuiWindow window, ImGuiCond flags, bool enabled )
	{
		if ( enabled )
		{
			window.SetWindowPosAllowFlags |= flags;
			window.SetWindowSizeAllowFlags |= flags;
			window.SetWindowCollapsedAllowFlags |= flags;
		}
		else
		{
			window.SetWindowPosAllowFlags &= ~flags;
			window.SetWindowSizeAllowFlags &= ~flags;
			window.SetWindowCollapsedAllowFlags &= ~flags;
		}
	}

	private static void UpdateWindowParentAndRootLinks( ImGuiWindow window, ImGuiWindowFlags flags, ImGuiWindow parentWindow )
	{
		window.ParentWindow = parentWindow;
		window.RootWindow = window.RootWindowPopupTree = window.RootWindowForTitleBarHighlight = window;
		if ( parentWindow is not null && (flags & ImGuiWindowFlags.ChildWindow) != 0 && (flags & ImGuiWindowFlags.Tooltip) == 0 )
			window.RootWindow = parentWindow.RootWindow;
		if ( parentWindow is not null && (flags & ImGuiWindowFlags.Popup) != 0 )
			window.RootWindowPopupTree = parentWindow.RootWindowPopupTree;
		if ( parentWindow is not null && (flags & ImGuiWindowFlags.Modal) == 0 && (flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Popup)) != 0 )
			window.RootWindowForTitleBarHighlight = parentWindow.RootWindowForTitleBarHighlight;
	}
	#endregion

	#region Window sizing
	private static void CalcWindowContentSizes( ImGuiWindow window, out Vector2 contentSizeCurrent, out Vector2 contentSizeIdeal )
	{
		if ( window.Collapsed && window.AutoFitFramesX <= 0 && window.AutoFitFramesY <= 0 )
		{
			contentSizeCurrent = window.ContentSize;
			contentSizeIdeal = window.ContentSizeIdeal;
			return;
		}
		if ( window.HiddenFramesCannotSkipItems == 0 && window.HiddenFramesCanSkipItems > 0 )
		{
			contentSizeCurrent = window.ContentSize;
			contentSizeIdeal = window.ContentSizeIdeal;
			return;
		}

		var dc = window.DC;
		contentSizeCurrent = new Vector2(
			window.ContentSizeExplicit.x != 0.0f ? window.ContentSizeExplicit.x : ImTrunc( dc.CursorMaxPos.x - dc.CursorStartPos.x ),
			window.ContentSizeExplicit.y != 0.0f ? window.ContentSizeExplicit.y : ImTrunc( dc.CursorMaxPos.y - dc.CursorStartPos.y ) );
		contentSizeIdeal = new Vector2(
			window.ContentSizeExplicit.x != 0.0f ? window.ContentSizeExplicit.x : ImTrunc( MathF.Max( dc.CursorMaxPos.x, dc.IdealMaxPos.x ) - dc.CursorStartPos.x ),
			window.ContentSizeExplicit.y != 0.0f ? window.ContentSizeExplicit.y : ImTrunc( MathF.Max( dc.CursorMaxPos.y, dc.IdealMaxPos.y ) - dc.CursorStartPos.y ) );
	}

	private static Vector2 CalcWindowSizeAfterConstraint( ImGuiWindow window, Vector2 sizeDesired )
	{
		var g = G;
		var newSize = sizeDesired;
		if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSizeConstraint) != 0 )
		{
			var cr = g.NextWindowData.SizeConstraintRect;
			newSize.x = (cr.Min.x >= 0 && cr.Max.x >= 0) ? ImClamp( newSize.x, cr.Min.x, cr.Max.x ) : window.SizeFull.x;
			newSize.y = (cr.Min.y >= 0 && cr.Max.y >= 0) ? ImClamp( newSize.y, cr.Min.y, cr.Max.y ) : window.SizeFull.y;
			if ( g.NextWindowData.SizeCallback is not null )
			{
				var data = new ImGuiSizeCallbackData { Pos = window.Pos, CurrentSize = window.SizeFull, DesiredSize = newSize };
				g.NextWindowData.SizeCallback( data );
				newSize = data.DesiredSize;
			}
			newSize = ImTrunc( newSize );
		}

		if ( (window.Flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.AlwaysAutoResize)) == 0 )
		{
			newSize = ImMax( newSize, g.Style.WindowMinSize );
			float minimumHeight = window.TitleBarHeight + window.MenuBarHeight + MathF.Max( 0.0f, g.Style.WindowRounding - 1.0f );
			newSize.y = MathF.Max( newSize.y, minimumHeight );
		}
		return newSize;
	}

	private static Vector2 CalcWindowAutoFitSize( ImGuiWindow window, Vector2 sizeContents )
	{
		var g = G;
		var style = g.Style;
		var sizeDecorations = new Vector2( window.DecoOuterSizeX1 + window.DecoOuterSizeX2, window.DecoOuterSizeY1 + window.DecoOuterSizeY2 );
		var sizePad = window.WindowPadding * 2.0f;
		var sizeDesired = sizeContents + sizePad + sizeDecorations;
		if ( (window.Flags & ImGuiWindowFlags.Tooltip) != 0 )
			return sizeDesired;

		var sizeMin = style.WindowMinSize;
		if ( (window.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.ChildWindow)) != 0 )
			sizeMin = ImMin( sizeMin, new Vector2( 4.0f, 4.0f ) );

		var availSize = g.IO.DisplaySize;
		var sizeAutoFit = ImClamp( sizeDesired, sizeMin, ImMax( sizeMin, availSize - style.DisplaySafeAreaPadding * 2.0f ) );

		var sizeAutoFitAfterConstraint = CalcWindowSizeAfterConstraint( window, sizeAutoFit );
		bool willHaveScrollbarX = (sizeAutoFitAfterConstraint.x - sizePad.x - sizeDecorations.x < sizeContents.x && (window.Flags & ImGuiWindowFlags.NoScrollbar) == 0 && (window.Flags & ImGuiWindowFlags.HorizontalScrollbar) != 0) || (window.Flags & ImGuiWindowFlags.AlwaysHorizontalScrollbar) != 0;
		bool willHaveScrollbarY = (sizeAutoFitAfterConstraint.y - sizePad.y - sizeDecorations.y < sizeContents.y && (window.Flags & ImGuiWindowFlags.NoScrollbar) == 0) || (window.Flags & ImGuiWindowFlags.AlwaysVerticalScrollbar) != 0;
		if ( willHaveScrollbarX ) sizeAutoFit.y += style.ScrollbarSize;
		if ( willHaveScrollbarY ) sizeAutoFit.x += style.ScrollbarSize;
		return sizeAutoFit;
	}

	internal static Vector2 CalcWindowNextAutoFitSize( ImGuiWindow window )
	{
		CalcWindowContentSizes( window, out _, out var sizeContentsIdeal );
		var sizeAutoFit = CalcWindowAutoFitSize( window, sizeContentsIdeal );
		return CalcWindowSizeAfterConstraint( window, sizeAutoFit );
	}

	private static void ClampWindowPos( ImGuiWindow window, ImRect visibilityRect )
	{
		var g = G;
		var sizeForClamping = window.Size;
		if ( g.IO.ConfigWindowsMoveFromTitleBarOnly && (window.Flags & ImGuiWindowFlags.NoTitleBar) == 0 )
			sizeForClamping.y = GetFrameHeight();
		window.Pos = ImClamp( window.Pos, visibilityRect.Min - sizeForClamping, visibilityRect.Max );
	}

	internal static void SetWindowPos( ImGuiWindow window, Vector2 pos, ImGuiCond cond = ImGuiCond.None )
	{
		if ( cond != ImGuiCond.None && (window.SetWindowPosAllowFlags & cond) == 0 )
			return;

		window.SetWindowPosAllowFlags &= ~(ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing);
		window.SetWindowPosVal = new Vector2( FLT_MAX, FLT_MAX );

		var oldPos = window.Pos;
		window.Pos = ImTrunc( pos );
		var offset = window.Pos - oldPos;
		if ( offset.x == 0f && offset.y == 0f )
			return;
		window.DC.CursorPos += offset;
		window.DC.CursorMaxPos += offset;
		window.DC.IdealMaxPos += offset;
		window.DC.CursorStartPos += offset;
	}

	internal static void SetWindowSize( ImGuiWindow window, Vector2 size, ImGuiCond cond = ImGuiCond.None )
	{
		if ( cond != ImGuiCond.None && (window.SetWindowSizeAllowFlags & cond) == 0 )
			return;

		window.SetWindowSizeAllowFlags &= ~(ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing);

		bool canAutoFit = (window.Flags & ImGuiWindowFlags.ChildWindow) == 0 || window.Appearing || (window.ChildFlags & ImGuiChildFlags.AlwaysAutoResize) != 0;
		if ( canAutoFit )
		{
			window.AutoFitFramesX = size.x <= 0.0f ? 2 : 0;
			window.AutoFitFramesY = size.y <= 0.0f ? 2 : 0;
		}

		if ( size.x <= 0.0f )
			window.AutoFitOnlyGrows = false;
		else
			window.SizeFull.x = ImTrunc( size.x );
		if ( size.y <= 0.0f )
			window.AutoFitOnlyGrows = false;
		else
			window.SizeFull.y = ImTrunc( size.y );
	}

	internal static void SetWindowCollapsed( ImGuiWindow window, bool collapsed, ImGuiCond cond = ImGuiCond.None )
	{
		if ( cond != ImGuiCond.None && (window.SetWindowCollapsedAllowFlags & cond) == 0 )
			return;
		window.SetWindowCollapsedAllowFlags &= ~(ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing);
		window.Collapsed = collapsed;
	}

	private static ImRect GetResizeBorderRect( ImGuiWindow window, int borderN, float perpPadding, float thickness )
	{
		var rect = window.Rect();
		if ( thickness == 0.0f )
			rect.Max -= Vector2.One;
		return borderN switch
		{
			0 => new ImRect( rect.Min.x - thickness, rect.Min.y + perpPadding, rect.Min.x + thickness, rect.Max.y - perpPadding ),
			1 => new ImRect( rect.Max.x - thickness, rect.Min.y + perpPadding, rect.Max.x + thickness, rect.Max.y - perpPadding ),
			2 => new ImRect( rect.Min.x + perpPadding, rect.Min.y - thickness, rect.Max.x - perpPadding, rect.Min.y + thickness ),
			_ => new ImRect( rect.Min.x + perpPadding, rect.Max.y - thickness, rect.Max.x - perpPadding, rect.Max.y + thickness ),
		};
	}

	private static void CalcResizePosSizeFromAnyCorner( ImGuiWindow window, Vector2 cornerTarget, Vector2 cornerNorm, out Vector2 outPos, out Vector2 outSize )
	{
		var posMin = ImLerp( cornerTarget, window.Pos, cornerNorm );
		var posMax = ImLerp( window.Pos + window.Size, cornerTarget, cornerNorm );
		var sizeExpected = posMax - posMin;
		var sizeConstrained = CalcWindowSizeAfterConstraint( window, sizeExpected );
		outPos = posMin;
		if ( cornerNorm.x == 0.0f ) outPos.x -= sizeConstrained.x - sizeExpected.x;
		if ( cornerNorm.y == 0.0f ) outPos.y -= sizeConstrained.y - sizeExpected.y;
		outSize = sizeConstrained;
	}

	private static bool UpdateWindowManualResize( ImGuiWindow window, Vector2 sizeAutoFit, ref int borderHovered, ref int borderHeld, int resizeGripCount, Color32[] resizeGripCol, ImRect visibilityRect )
	{
		var g = G;
		var flags = window.Flags;

		if ( (flags & ImGuiWindowFlags.NoResize) != 0 || (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 || window.AutoFitFramesX > 0 || window.AutoFitFramesY > 0 )
			return false;
		if ( !window.WasActive )
			return false;

		bool retAutoFit = false;
		int resizeBorderCount = g.IO.ConfigWindowsResizeFromEdges ? 4 : 0;
		float gripDrawSize = ImTrunc( MathF.Max( g.FontSize * 1.35f, window.WindowRounding + 1.0f + g.FontSize * 0.2f ) );
		float gripHoverInnerSize = ImTrunc( gripDrawSize * 0.75f );
		float gripHoverOuterSize = g.IO.ConfigWindowsResizeFromEdges ? WindowsHoverPadding : 0.0f;

		var posTarget = new Vector2( FLT_MAX, FLT_MAX );
		var sizeTarget = new Vector2( FLT_MAX, FLT_MAX );

		window.IDStack.Add( window.GetID( "#RESIZE" ) );
		for ( int resizeGripN = 0; resizeGripN < resizeGripCount; resizeGripN++ )
		{
			var def = ResizeGripDefs[resizeGripN];
			var corner = ImLerp( window.Pos, window.Pos + window.Size, def.CornerPosN );

			var resizeRect = new ImRect( corner - def.InnerDir * gripHoverOuterSize, corner + def.InnerDir * gripHoverInnerSize );
			if ( resizeRect.Min.x > resizeRect.Max.x ) (resizeRect.Min.x, resizeRect.Max.x) = (resizeRect.Max.x, resizeRect.Min.x);
			if ( resizeRect.Min.y > resizeRect.Max.y ) (resizeRect.Min.y, resizeRect.Max.y) = (resizeRect.Max.y, resizeRect.Min.y);
			int resizeGripId = window.GetID( resizeGripN );
			ItemAdd( resizeRect, resizeGripId, null, ImGuiItemFlags.NoNav );
			ButtonBehavior( resizeRect, resizeGripId, out bool hovered, out bool held, ImGuiButtonFlags.FlattenChildren | ImGuiButtonFlags.NoNavFocus );
			if ( hovered || held )
				g.MouseCursor = (resizeGripN & 1) != 0 ? ImGuiMouseCursor.ResizeNESW : ImGuiMouseCursor.ResizeNWSE;

			if ( held && g.IO.MouseClickedCount[0] == 2 && resizeGripN == 0 )
			{
				sizeTarget = CalcWindowSizeAfterConstraint( window, sizeAutoFit );
				retAutoFit = true;
				ClearActiveID();
			}
			else if ( held )
			{
				var clampMin = new Vector2( def.CornerPosN.x == 1.0f ? visibilityRect.Min.x : -FLT_MAX, def.CornerPosN.y == 1.0f ? visibilityRect.Min.y : -FLT_MAX );
				var clampMax = new Vector2( def.CornerPosN.x == 0.0f ? visibilityRect.Max.x : FLT_MAX, def.CornerPosN.y == 0.0f ? visibilityRect.Max.y : FLT_MAX );
				var cornerTarget = g.IO.MousePos - g.ActiveIdClickOffset + ImLerp( def.InnerDir * gripHoverOuterSize, def.InnerDir * -gripHoverInnerSize, def.CornerPosN );
				cornerTarget = new Vector2( Math.Clamp( cornerTarget.x, clampMin.x, clampMax.x ), Math.Clamp( cornerTarget.y, clampMin.y, clampMax.y ) );
				CalcResizePosSizeFromAnyCorner( window, cornerTarget, def.CornerPosN, out posTarget, out sizeTarget );
			}

			if ( resizeGripN == 0 || held || hovered )
				resizeGripCol[resizeGripN] = GetColorU32Internal( held ? ImGuiCol.ResizeGripActive : hovered ? ImGuiCol.ResizeGripHovered : ImGuiCol.ResizeGrip );
		}

		for ( int borderN = 0; borderN < resizeBorderCount; borderN++ )
		{
			var def = ResizeBorderDefs[borderN];
			int axis = borderN == 0 || borderN == 1 ? 0 : 1;

			var borderRect = GetResizeBorderRect( window, borderN, gripHoverInnerSize, WindowsHoverPadding );
			int borderId = window.GetID( borderN + 4 );
			ItemAdd( borderRect, borderId, null, ImGuiItemFlags.NoNav );
			ButtonBehavior( borderRect, borderId, out bool hovered, out bool held, ImGuiButtonFlags.FlattenChildren | ImGuiButtonFlags.NoNavFocus );
			if ( hovered && g.HoveredIdTimer <= WINDOWS_RESIZE_FROM_EDGES_FEEDBACK_TIMER )
				hovered = false;
			if ( hovered || held )
				g.MouseCursor = axis == 0 ? ImGuiMouseCursor.ResizeEW : ImGuiMouseCursor.ResizeNS;
			if ( held )
			{
				var clampMin = new Vector2( borderN == 1 ? visibilityRect.Min.x : -FLT_MAX, borderN == 3 ? visibilityRect.Min.y : -FLT_MAX );
				var clampMax = new Vector2( borderN == 0 ? visibilityRect.Max.x : FLT_MAX, borderN == 2 ? visibilityRect.Max.y : FLT_MAX );
				var borderTarget = WithAxis( window.Pos, axis, Axis( g.IO.MousePos, axis ) - Axis( g.ActiveIdClickOffset, axis ) + WindowsHoverPadding );
				borderTarget = new Vector2( Math.Clamp( borderTarget.x, clampMin.x, clampMax.x ), Math.Clamp( borderTarget.y, clampMin.y, clampMax.y ) );
				CalcResizePosSizeFromAnyCorner( window, borderTarget, ImMin( def.SegmentN1, def.SegmentN2 ), out posTarget, out sizeTarget );
			}
			if ( hovered ) borderHovered = borderN;
			if ( held ) borderHeld = borderN;
		}
		window.IDStack.RemoveAt( window.IDStack.Count - 1 );

		if ( sizeTarget.x != FLT_MAX )
		{
			window.SizeFull = sizeTarget;
			MarkIniSettingsDirty( window );
		}
		if ( posTarget.x != FLT_MAX )
		{
			window.Pos = ImTrunc( posTarget );
			MarkIniSettingsDirty( window );
		}

		window.Size = window.SizeFull;
		return retAutoFit;
	}

	internal static void MarkIniSettingsDirty( ImGuiWindow window )
	{
		if ( (window.Flags & (ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 )
			return;
		G.SettingsWindows[window.ID] = new ImGuiContext.WindowSettings { Pos = window.Pos, Size = window.SizeFull, Collapsed = window.Collapsed };
	}
	#endregion

	#region Popup positioning
	internal enum ImGuiPopupPositionPolicy
	{
		Default,
		ComboBox,
		Tooltip,
	}

	internal static ImRect GetPopupAllowedExtentRect()
	{
		var g = G;
		var r = new ImRect( Vector2.Zero, g.IO.DisplaySize );
		var padding = g.Style.DisplaySafeAreaPadding;
		r.Expand( new Vector2( r.Width > padding.x * 2 ? -padding.x : 0.0f, r.Height > padding.y * 2 ? -padding.y : 0.0f ) );
		return r;
	}

	internal static Vector2 FindBestWindowPosForPopup( ImGuiWindow window )
	{
		var g = G;
		var rOuter = GetPopupAllowedExtentRect();
		if ( (window.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
		{
			var parentWindow = window.ParentWindow;
			float horizontalOverlap = g.Style.ItemInnerSpacing.x;
			ImRect rAvoid;
			if ( parentWindow.DC.MenuBarAppending > 0 )
				rAvoid = new ImRect( -FLT_MAX, parentWindow.ClipRect.Min.y, FLT_MAX, parentWindow.ClipRect.Max.y );
			else
				rAvoid = new ImRect( parentWindow.Pos.x + horizontalOverlap, -FLT_MAX, parentWindow.Pos.x + parentWindow.Size.x - horizontalOverlap - parentWindow.ScrollbarSizes.x, FLT_MAX );
			return FindBestWindowPosForPopupEx( window.Pos, window.Size, ref window.AutoPosLastDirection, rOuter, rAvoid, ImGuiPopupPositionPolicy.Default );
		}
		if ( (window.Flags & ImGuiWindowFlags.Popup) != 0 )
			return FindBestWindowPosForPopupEx( window.Pos, window.Size, ref window.AutoPosLastDirection, rOuter, new ImRect( window.Pos, window.Pos ), ImGuiPopupPositionPolicy.Default );
		if ( (window.Flags & ImGuiWindowFlags.Tooltip) != 0 )
		{
			float sc = g.Style.MouseCursorScale;
			var refPos = g.IO.MousePos;
			var rAvoid = new ImRect( refPos.x - 16, refPos.y - 8, refPos.x + 24 * sc, refPos.y + 24 * sc );
			return FindBestWindowPosForPopupEx( refPos, window.Size, ref window.AutoPosLastDirection, rOuter, rAvoid, ImGuiPopupPositionPolicy.Tooltip );
		}
		return window.Pos;
	}

	internal static Vector2 FindBestWindowPosForPopupEx( Vector2 refPos, Vector2 size, ref ImGuiDir lastDir, ImRect rOuter, ImRect rAvoid, ImGuiPopupPositionPolicy policy )
	{
		var basePosClamped = ImClamp( refPos, rOuter.Min, rOuter.Max - size );

		if ( policy == ImGuiPopupPositionPolicy.ComboBox )
		{
			ImGuiDir[] dirPreferedOrder = { ImGuiDir.Down, ImGuiDir.Right, ImGuiDir.Left, ImGuiDir.Up };
			for ( int n = lastDir != ImGuiDir.None ? -1 : 0; n < 4; n++ )
			{
				var dir = n == -1 ? lastDir : dirPreferedOrder[n];
				if ( n != -1 && dir == lastDir )
					continue;
				Vector2 pos = default;
				if ( dir == ImGuiDir.Down ) pos = new Vector2( rAvoid.Min.x, rAvoid.Max.y );
				if ( dir == ImGuiDir.Right ) pos = new Vector2( rAvoid.Min.x, rAvoid.Min.y - size.y );
				if ( dir == ImGuiDir.Left ) pos = new Vector2( rAvoid.Max.x - size.x, rAvoid.Max.y );
				if ( dir == ImGuiDir.Up ) pos = new Vector2( rAvoid.Max.x - size.x, rAvoid.Min.y - size.y );
				if ( !rOuter.Contains( new ImRect( pos, pos + size ) ) )
					continue;
				lastDir = dir;
				return pos;
			}
		}

		if ( policy == ImGuiPopupPositionPolicy.Tooltip || policy == ImGuiPopupPositionPolicy.Default )
		{
			ImGuiDir[] dirPreferedOrder = { ImGuiDir.Right, ImGuiDir.Down, ImGuiDir.Up, ImGuiDir.Left };
			for ( int n = lastDir != ImGuiDir.None ? -1 : 0; n < 4; n++ )
			{
				var dir = n == -1 ? lastDir : dirPreferedOrder[n];
				if ( n != -1 && dir == lastDir )
					continue;

				float availW = (dir == ImGuiDir.Left ? rAvoid.Min.x : rOuter.Max.x) - (dir == ImGuiDir.Right ? rAvoid.Max.x : rOuter.Min.x);
				float availH = (dir == ImGuiDir.Up ? rAvoid.Min.y : rOuter.Max.y) - (dir == ImGuiDir.Down ? rAvoid.Max.y : rOuter.Min.y);
				if ( availW < size.x && (dir == ImGuiDir.Left || dir == ImGuiDir.Right) )
					continue;
				if ( availH < size.y && (dir == ImGuiDir.Up || dir == ImGuiDir.Down) )
					continue;

				var pos = new Vector2(
					dir == ImGuiDir.Left ? rAvoid.Min.x - size.x : (dir == ImGuiDir.Right ? rAvoid.Max.x : basePosClamped.x),
					dir == ImGuiDir.Up ? rAvoid.Min.y - size.y : (dir == ImGuiDir.Down ? rAvoid.Max.y : basePosClamped.y) );
				pos.x = MathF.Max( pos.x, rOuter.Min.x );
				pos.y = MathF.Max( pos.y, rOuter.Min.y );
				lastDir = dir;
				return pos;
			}
		}

		lastDir = ImGuiDir.None;
		if ( policy == ImGuiPopupPositionPolicy.Tooltip )
			return refPos + new Vector2( 2, 2 );

		var fallback = refPos;
		fallback.x = MathF.Max( MathF.Min( fallback.x + size.x, rOuter.Max.x ) - size.x, rOuter.Min.x );
		fallback.y = MathF.Max( MathF.Min( fallback.y + size.y, rOuter.Max.y ) - size.y, rOuter.Min.y );
		return fallback;
	}
	#endregion

	#region Begin / End
	/// <summary>
	/// Push a window to the stack and start appending to it. Always call End() even if this returns false
	/// (false means the window is collapsed or fully clipped, and you can skip submitting contents).
	/// </summary>
	public static bool Begin( string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		bool dummy = true;
		return BeginImpl( name, false, ref dummy, flags );
	}

	/// <summary>
	/// Begin a window with a close button. When the close button is clicked, <paramref name="open"/> is set to false.
	/// </summary>
	public static bool Begin( string name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		return BeginImpl( name, true, ref open, flags );
	}

	internal static bool BeginImpl( string name, bool hasCloseButton, ref bool pOpen, ImGuiWindowFlags flags )
	{
		var g = G;
		var style = g.Style;

		if ( string.IsNullOrEmpty( name ) )
			throw new ArgumentException( "Window name cannot be empty", nameof( name ) );
		if ( !g.WithinFrameScope )
			throw new InvalidOperationException( "ImGui.Begin() called outside of a frame. Call ImGui functions from OnUpdate." );

		var window = FindWindowByName( name );
		bool windowJustCreated = window is null;
		if ( windowJustCreated )
			window = CreateNewWindow( name, flags );

		if ( (flags & ImGuiWindowFlags.NoInputs) == ImGuiWindowFlags.NoInputs )
			flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

		int currentFrame = g.FrameCount;
		bool firstBeginOfTheFrame = window.LastFrameActive != currentFrame;
		window.IsFallbackWindow = g.CurrentWindowStack.Count == 0 && g.WithinFrameScopeWithImplicitWindow;

		bool windowJustActivatedByUser = window.LastFrameActive < currentFrame - 1;
		if ( (flags & ImGuiWindowFlags.Popup) != 0 )
		{
			var popupRef0 = g.OpenPopupStack[g.BeginPopupStack.Count];
			windowJustActivatedByUser |= window.PopupId != popupRef0.PopupId;
			windowJustActivatedByUser |= window != popupRef0.Window;
		}

		if ( firstBeginOfTheFrame )
		{
			window.Flags = flags;
			window.ChildFlags = (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasChildFlags) != 0 ? g.NextWindowData.ChildFlags : ImGuiChildFlags.None;
			window.LastFrameActive = currentFrame;
			window.LastTimeActive = (float)g.Time;
			window.BeginOrderWithinParent = 0;
			window.BeginOrderWithinContext = g.WindowsActiveCount++;
		}
		else
		{
			flags = window.Flags;
		}

		var parentWindowInStack = g.CurrentWindowStack.Count > 0 ? g.CurrentWindowStack[^1].Window : null;
		var parentWindow = firstBeginOfTheFrame
			? ((flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Popup)) != 0 ? parentWindowInStack : null)
			: window.ParentWindow;

		window.Appearing = windowJustActivatedByUser;
		if ( window.Appearing )
			SetWindowConditionAllowFlags( window, ImGuiCond.Appearing, true );

		// Add to stack
		g.CurrentWindow = window;
		g.CurrentWindowStack.Add( new ImGuiWindowStackData
		{
			Window = window,
			ParentLastItemDataBackup = g.LastItemData,
			StackSizesColorStack = g.ColorStack.Count,
			StackSizesStyleVarStack = g.StyleVarStack.Count,
			StackSizesItemFlagsStack = g.ItemFlagsStack.Count,
			StackSizesGroupStack = g.GroupStack.Count,
			StackSizesBeginPopupStack = g.BeginPopupStack.Count,
			StackSizesDisabledStack = g.DisabledStackSize,
			BackupItemFlags = g.CurrentItemFlags,
		} );
		if ( (flags & ImGuiWindowFlags.ChildMenu) != 0 )
			g.BeginMenuDepth++;

		if ( (flags & ImGuiWindowFlags.Popup) != 0 )
		{
			var popupRef = g.OpenPopupStack[g.BeginPopupStack.Count];
			popupRef.Window = window;
			g.BeginPopupStack.Add( popupRef );
			window.PopupId = popupRef.PopupId;
		}

		// Process SetNextWindow***() calls
		bool windowPosSetByApi = false;
		bool windowSizeXSetByApi = false, windowSizeYSetByApi = false;
		var nwd = g.NextWindowData;
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasPos) != 0 )
		{
			windowPosSetByApi = (window.SetWindowPosAllowFlags & nwd.PosCond) != 0;
			if ( windowPosSetByApi && ImLengthSqr( nwd.PosPivotVal ) > 0.00001f )
			{
				window.SetWindowPosVal = nwd.PosVal;
				window.SetWindowPosPivot = nwd.PosPivotVal;
				window.SetWindowPosAllowFlags &= ~(ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing);
			}
			else
			{
				SetWindowPos( window, nwd.PosVal, nwd.PosCond );
			}
		}
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasSize) != 0 )
		{
			windowSizeXSetByApi = (window.SetWindowSizeAllowFlags & nwd.SizeCond) != 0 && nwd.SizeVal.x > 0.0f;
			windowSizeYSetByApi = (window.SetWindowSizeAllowFlags & nwd.SizeCond) != 0 && nwd.SizeVal.y > 0.0f;
			var sizeVal = nwd.SizeVal;
			// Child windows resized by the user keep their user size.
			if ( (window.ChildFlags & ImGuiChildFlags.ResizeX) != 0 && window.SizeFull.x > 0 && !windowJustCreated )
				sizeVal.x = window.SizeFull.x;
			if ( (window.ChildFlags & ImGuiChildFlags.ResizeY) != 0 && window.SizeFull.y > 0 && !windowJustCreated )
				sizeVal.y = window.SizeFull.y;
			SetWindowSize( window, sizeVal, nwd.SizeCond );
		}
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasScroll) != 0 )
		{
			if ( nwd.ScrollVal.x >= 0.0f )
			{
				window.ScrollTarget.x = nwd.ScrollVal.x;
				window.ScrollTargetCenterRatio.x = 0.0f;
			}
			if ( nwd.ScrollVal.y >= 0.0f )
			{
				window.ScrollTarget.y = nwd.ScrollVal.y;
				window.ScrollTargetCenterRatio.y = 0.0f;
			}
		}
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasContentSize) != 0 )
			window.ContentSizeExplicit = nwd.ContentSizeVal;
		else if ( firstBeginOfTheFrame )
			window.ContentSizeExplicit = Vector2.Zero;
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasCollapsed) != 0 )
			SetWindowCollapsed( window, nwd.CollapsedVal, nwd.CollapsedCond );
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasFocus) != 0 )
			FocusWindow( window );
		if ( window.Appearing )
			SetWindowConditionAllowFlags( window, ImGuiCond.Appearing, false );

		if ( firstBeginOfTheFrame )
		{
			bool windowIsChildTooltip = (flags & ImGuiWindowFlags.ChildWindow) != 0 && (flags & ImGuiWindowFlags.Tooltip) != 0;

			UpdateWindowParentAndRootLinks( window, flags, parentWindow );
			window.ParentWindowInBeginStack = parentWindowInStack;

			window.Active = true;
			window.HasCloseButton = hasCloseButton;
			window.ClipRect = new ImRect( -FLT_MAX, -FLT_MAX, FLT_MAX, FLT_MAX );
			window.IDStack.Clear();
			window.IDStack.Add( window.ID );
			window.DrawList.ResetForNewFrame();
			window.Name = name;

			// UPDATE CONTENTS SIZE, UPDATE HIDDEN STATUS
			CalcWindowContentSizes( window, out window.ContentSize, out window.ContentSizeIdeal );
			if ( window.HiddenFramesCanSkipItems > 0 ) window.HiddenFramesCanSkipItems--;
			if ( window.HiddenFramesCannotSkipItems > 0 ) window.HiddenFramesCannotSkipItems--;
			if ( window.HiddenFramesForRenderOnly > 0 ) window.HiddenFramesForRenderOnly--;

			if ( windowJustCreated && (!windowSizeXSetByApi || !windowSizeYSetByApi) )
				window.HiddenFramesCannotSkipItems = 1;

			if ( windowJustActivatedByUser && (flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 )
			{
				window.HiddenFramesCannotSkipItems = 1;
				if ( (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 )
				{
					if ( !windowSizeXSetByApi ) { window.Size.x = window.SizeFull.x = 0f; }
					if ( !windowSizeYSetByApi ) { window.Size.y = window.SizeFull.y = 0f; }
					window.ContentSize = window.ContentSizeIdeal = Vector2.Zero;
				}
			}

			// UPDATE DECORATION SIZES
			window.WindowPadding = style.WindowPadding;
			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 && (window.ChildFlags & (ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.Borders | ImGuiChildFlags.FrameStyle)) == 0 && (flags & ImGuiWindowFlags.Popup) == 0 )
				window.WindowPadding = new Vector2( 0.0f, (flags & ImGuiWindowFlags.MenuBar) != 0 ? style.WindowPadding.y : 0.0f );

			window.DC.MenuBarOffset = new Vector2(
				MathF.Max( MathF.Max( window.WindowPadding.x, style.ItemSpacing.x ), nwd.MenuBarOffsetMinVal.x ),
				nwd.MenuBarOffsetMinVal.y );
			window.TitleBarHeight = (flags & ImGuiWindowFlags.NoTitleBar) != 0 ? 0.0f : g.FontSize + style.FramePadding.y * 2.0f;
			window.MenuBarHeight = (flags & ImGuiWindowFlags.MenuBar) != 0 ? window.DC.MenuBarOffset.y + g.FontSize + style.FramePadding.y * 2.0f : 0.0f;

			// Collapse window by double-clicking on title bar
			if ( (flags & ImGuiWindowFlags.NoTitleBar) == 0 && (flags & ImGuiWindowFlags.NoCollapse) == 0 )
			{
				var titleBarRect0 = window.TitleBarRect();
				if ( g.HoveredWindow == window && g.HoveredId == 0 && g.HoveredIdPreviousFrame == 0 && g.ActiveId == 0 && IsMouseHoveringRect( titleBarRect0.Min, titleBarRect0.Max ) && g.IO.MouseClicked[0] && g.IO.MouseClickedCount[0] == 2 )
					window.WantCollapseToggle = true;
				if ( window.WantCollapseToggle )
				{
					window.Collapsed = !window.Collapsed;
					MarkIniSettingsDirty( window );
				}
			}
			else
			{
				window.Collapsed = false;
			}
			window.WantCollapseToggle = false;

			// SIZE
			window.DecoOuterSizeX1 = 0.0f;
			window.DecoOuterSizeX2 = 0.0f;
			window.DecoOuterSizeY1 = window.TitleBarHeight + window.MenuBarHeight;
			window.DecoOuterSizeY2 = 0.0f;
			var scrollbarSizesFromLastFrame = window.ScrollbarSizes;
			window.ScrollbarSizes = Vector2.Zero;

			var sizeAutoFit = CalcWindowAutoFitSize( window, window.ContentSizeIdeal );
			bool useCurrentSizeForScrollbarX = windowJustCreated;
			bool useCurrentSizeForScrollbarY = windowJustCreated;
			{
				bool autoFitXAlways = !windowSizeXSetByApi && (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 && !window.Collapsed;
				bool autoFitYAlways = !windowSizeYSetByApi && (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 && !window.Collapsed;
				bool autoFitXCurrent = !windowSizeXSetByApi && window.AutoFitFramesX > 0;
				bool autoFitYCurrent = !windowSizeYSetByApi && window.AutoFitFramesY > 0;
				if ( autoFitXAlways || autoFitXCurrent )
				{
					window.SizeFull.x = window.AutoFitOnlyGrows && !autoFitXAlways ? MathF.Max( window.SizeFull.x, sizeAutoFit.x ) : sizeAutoFit.x;
					useCurrentSizeForScrollbarX = true;
				}
				if ( autoFitYAlways || autoFitYCurrent )
				{
					window.SizeFull.y = window.AutoFitOnlyGrows && !autoFitYAlways ? MathF.Max( window.SizeFull.y, sizeAutoFit.y ) : sizeAutoFit.y;
					useCurrentSizeForScrollbarY = true;
				}
			}

			window.SizeFull = CalcWindowSizeAfterConstraint( window, window.SizeFull );
			window.Size = window.Collapsed && (flags & ImGuiWindowFlags.ChildWindow) == 0 ? window.TitleBarRect().Size : window.SizeFull;

			// POSITION
			if ( windowJustActivatedByUser )
			{
				window.AutoPosLastDirection = ImGuiDir.None;
				if ( (flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiWindowFlags.Modal) == 0 && !windowPosSetByApi )
					window.Pos = g.BeginPopupStack[^1].OpenPopupPos;
			}

			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 )
			{
				parentWindow.DC.ChildWindows.Add( window );
				window.BeginOrderWithinParent = parentWindow.DC.ChildWindows.Count - 1;
				if ( (flags & ImGuiWindowFlags.Popup) == 0 && !windowPosSetByApi && !windowIsChildTooltip )
					window.Pos = parentWindow.DC.CursorPos;
			}

			bool windowPosWithPivot = window.SetWindowPosVal.x != FLT_MAX && window.HiddenFramesCannotSkipItems == 0;
			if ( windowPosWithPivot )
				SetWindowPos( window, window.SetWindowPosVal - window.Size * window.SetWindowPosPivot );
			else if ( (flags & ImGuiWindowFlags.ChildMenu) != 0 )
				window.Pos = FindBestWindowPosForPopup( window );
			else if ( (flags & ImGuiWindowFlags.Popup) != 0 && !windowPosSetByApi )
				window.Pos = FindBestWindowPosForPopup( window );
			else if ( (flags & ImGuiWindowFlags.Tooltip) != 0 && !windowPosSetByApi && !windowIsChildTooltip )
				window.Pos = FindBestWindowPosForPopup( window );

			// Clamp position so the window stays visible
			var viewportRect = new ImRect( Vector2.Zero, g.IO.DisplaySize );
			var visibilityPadding = ImMax( style.DisplayWindowPadding, style.DisplaySafeAreaPadding );
			var visibilityRect = new ImRect( viewportRect.Min + visibilityPadding, viewportRect.Max - visibilityPadding );
			if ( visibilityRect.Min.x >= visibilityRect.Max.x || visibilityRect.Min.y >= visibilityRect.Max.y )
				visibilityRect = viewportRect;
			if ( !windowPosSetByApi && (flags & ImGuiWindowFlags.ChildWindow) == 0 )
				if ( g.IO.DisplaySize.x > 0.0f && g.IO.DisplaySize.y > 0.0f )
					ClampWindowPos( window, visibilityRect );
			window.Pos = ImTrunc( window.Pos );

			// Lock window rounding/border for the frame
			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 )
				window.WindowRounding = (window.ChildFlags & ImGuiChildFlags.FrameStyle) != 0 ? style.FrameRounding : style.ChildRounding;
			else
				window.WindowRounding = (flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiWindowFlags.Modal) == 0 ? style.PopupRounding : style.WindowRounding;
			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 )
				window.WindowBorderSize = (window.ChildFlags & (ImGuiChildFlags.Borders | ImGuiChildFlags.FrameStyle)) != 0 ? style.ChildBorderSize : 0.0f;
			else
				window.WindowBorderSize = (flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 && (flags & ImGuiWindowFlags.Modal) == 0 ? style.PopupBorderSize : style.WindowBorderSize;

			// Apply window focus (new and reactivated windows are moved to front)
			bool wantFocus = false;
			if ( windowJustActivatedByUser && (flags & ImGuiWindowFlags.NoFocusOnAppearing) == 0 )
			{
				if ( (flags & ImGuiWindowFlags.Popup) != 0 )
					wantFocus = true;
				else if ( (flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Tooltip)) == 0 )
					wantFocus = true;
			}

			// Handle manual resize
			int borderHovered = -1, borderHeld = -1;
			var resizeGripCol = new Color32[2];
			int resizeGripCount = (flags & ImGuiWindowFlags.ChildWindow) != 0 ? 0 : (g.IO.ConfigWindowsResizeFromEdges ? 2 : 1);
			float resizeGripDrawSize = ImTrunc( MathF.Max( g.FontSize * 1.10f, window.WindowRounding + 1.0f + g.FontSize * 0.2f ) );
			if ( !window.Collapsed )
				if ( UpdateWindowManualResize( window, sizeAutoFit, ref borderHovered, ref borderHeld, resizeGripCount, resizeGripCol, visibilityRect ) )
					useCurrentSizeForScrollbarX = useCurrentSizeForScrollbarY = true;
			window.ResizeBorderHovered = borderHovered;
			window.ResizeBorderHeld = borderHeld;

			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 && (window.ChildFlags & (ImGuiChildFlags.ResizeX | ImGuiChildFlags.ResizeY)) != 0 && !window.Collapsed )
				UpdateChildWindowResize( window );

			// SCROLLBAR VISIBILITY
			if ( !window.Collapsed )
			{
				var availSizeFromCurrentFrame = new Vector2( window.SizeFull.x, window.SizeFull.y - (window.DecoOuterSizeY1 + window.DecoOuterSizeY2) );
				var availSizeFromLastFrame = window.InnerRect.Size + scrollbarSizesFromLastFrame;
				var neededSizeFromLastFrame = windowJustCreated ? Vector2.Zero : window.ContentSize + window.WindowPadding * 2.0f;
				float sizeXForScrollbars = useCurrentSizeForScrollbarX ? availSizeFromCurrentFrame.x : availSizeFromLastFrame.x;
				float sizeYForScrollbars = useCurrentSizeForScrollbarY ? availSizeFromCurrentFrame.y : availSizeFromLastFrame.y;
				window.ScrollbarY = (flags & ImGuiWindowFlags.AlwaysVerticalScrollbar) != 0 || (neededSizeFromLastFrame.y > sizeYForScrollbars && (flags & ImGuiWindowFlags.NoScrollbar) == 0);
				window.ScrollbarX = (flags & ImGuiWindowFlags.AlwaysHorizontalScrollbar) != 0 || (neededSizeFromLastFrame.x > sizeXForScrollbars - (window.ScrollbarY ? style.ScrollbarSize : 0.0f) && (flags & ImGuiWindowFlags.NoScrollbar) == 0 && (flags & ImGuiWindowFlags.HorizontalScrollbar) != 0);
				if ( window.ScrollbarX && !window.ScrollbarY )
					window.ScrollbarY = neededSizeFromLastFrame.y > sizeYForScrollbars - style.ScrollbarSize && (flags & ImGuiWindowFlags.NoScrollbar) == 0;
				window.ScrollbarSizes = new Vector2( window.ScrollbarY ? style.ScrollbarSize : 0.0f, window.ScrollbarX ? style.ScrollbarSize : 0.0f );
				window.DecoOuterSizeX2 = window.ScrollbarSizes.x;
				window.DecoOuterSizeY2 = window.ScrollbarSizes.y;
			}

			// UPDATE RECTANGLES (1- THOSE NOT AFFECTED BY SCROLLING)
			var outerRect = window.Rect();
			var titleBarRect = window.TitleBarRect();
			var hostRect = (flags & ImGuiWindowFlags.ChildWindow) != 0 && (flags & ImGuiWindowFlags.Popup) == 0 && !windowIsChildTooltip ? parentWindow.ClipRect : viewportRect;
			window.OuterRectClipped = outerRect;
			window.OuterRectClipped.ClipWith( hostRect );

			window.InnerRect = new ImRect(
				window.Pos.x + window.DecoOuterSizeX1,
				window.Pos.y + window.DecoOuterSizeY1,
				window.Pos.x + window.Size.x - window.DecoOuterSizeX2,
				window.Pos.y + window.Size.y - window.DecoOuterSizeY2 );

			float topBorderSize = (flags & ImGuiWindowFlags.MenuBar) != 0 || (flags & ImGuiWindowFlags.NoTitleBar) == 0 ? style.FrameBorderSize : window.WindowBorderSize;
			window.InnerClipRect = new ImRect(
				ImTrunc( 0.5f + window.InnerRect.Min.x + window.WindowBorderSize * 0.5f ),
				ImTrunc( 0.5f + window.InnerRect.Min.y + topBorderSize * 0.5f ),
				ImTrunc( window.InnerRect.Max.x - window.WindowBorderSize * 0.5f ),
				ImTrunc( window.InnerRect.Max.y - window.WindowBorderSize * 0.5f ) );
			window.InnerClipRect.ClipWithFull( hostRect );

			// SCROLLING
			window.ScrollMax = new Vector2(
				MathF.Max( 0.0f, window.ContentSize.x + window.WindowPadding.x * 2.0f - window.InnerRect.Width ),
				MathF.Max( 0.0f, window.ContentSize.y + window.WindowPadding.y * 2.0f - window.InnerRect.Height ) );
			window.Scroll = CalcNextScrollFromScrollTargetAndClamp( window );
			window.ScrollTarget = new Vector2( FLT_MAX, FLT_MAX );
			window.DecoInnerSizeX1 = window.DecoInnerSizeY1 = 0.0f;

			// DRAWING
			window.DrawList.PushClipRect( hostRect.Min, hostRect.Max, false );
			{
				var windowToHighlight = g.NavWindow;
				bool titleBarIsHighlight = wantFocus || (windowToHighlight is not null && window.RootWindowForTitleBarHighlight == windowToHighlight.RootWindowForTitleBarHighlight);
				RenderWindowDecorations( window, titleBarRect, titleBarIsHighlight, resizeGripCount, resizeGripCol, resizeGripDrawSize );
			}

			// UPDATE RECTANGLES (2- THOSE AFFECTED BY SCROLLING)
			bool allowScrollbarX = (flags & ImGuiWindowFlags.NoScrollbar) == 0 && (flags & ImGuiWindowFlags.HorizontalScrollbar) != 0;
			bool allowScrollbarY = (flags & ImGuiWindowFlags.NoScrollbar) == 0;
			float workRectSizeX = window.ContentSizeExplicit.x != 0.0f ? window.ContentSizeExplicit.x : MathF.Max( allowScrollbarX ? window.ContentSize.x : 0.0f, window.Size.x - window.WindowPadding.x * 2.0f - (window.DecoOuterSizeX1 + window.DecoOuterSizeX2) );
			float workRectSizeY = window.ContentSizeExplicit.y != 0.0f ? window.ContentSizeExplicit.y : MathF.Max( allowScrollbarY ? window.ContentSize.y : 0.0f, window.Size.y - window.WindowPadding.y * 2.0f - (window.DecoOuterSizeY1 + window.DecoOuterSizeY2) );
			window.WorkRect.Min = new Vector2(
				ImTrunc( window.InnerRect.Min.x - window.Scroll.x + MathF.Max( window.WindowPadding.x, window.WindowBorderSize ) ),
				ImTrunc( window.InnerRect.Min.y - window.Scroll.y + MathF.Max( window.WindowPadding.y, window.WindowBorderSize ) ) );
			window.WorkRect.Max = window.WorkRect.Min + new Vector2( workRectSizeX, workRectSizeY );
			window.ParentWorkRect = window.WorkRect;

			window.ContentRegionRect.Min = new Vector2(
				window.Pos.x - window.Scroll.x + window.WindowPadding.x + window.DecoOuterSizeX1,
				window.Pos.y - window.Scroll.y + window.WindowPadding.y + window.DecoOuterSizeY1 );
			window.ContentRegionRect.Max = window.ContentRegionRect.Min + new Vector2(
				window.ContentSizeExplicit.x != 0.0f ? window.ContentSizeExplicit.x : (window.Size.x - window.WindowPadding.x * 2.0f - (window.DecoOuterSizeX1 + window.DecoOuterSizeX2)),
				window.ContentSizeExplicit.y != 0.0f ? window.ContentSizeExplicit.y : (window.Size.y - window.WindowPadding.y * 2.0f - (window.DecoOuterSizeY1 + window.DecoOuterSizeY2)) );

			// Setup drawing context
			var dc = window.DC;
			dc.Indent = window.DecoOuterSizeX1 + window.WindowPadding.x - window.Scroll.x;
			dc.GroupOffset = 0.0f;
			dc.ColumnsOffset = 0.0f;
			dc.CursorStartPos = new Vector2(
				window.Pos.x + window.WindowPadding.x - window.Scroll.x + window.DecoOuterSizeX1,
				window.Pos.y + window.WindowPadding.y - window.Scroll.y + window.DecoOuterSizeY1 );
			dc.CursorPos = dc.CursorStartPos;
			dc.CursorPosPrevLine = dc.CursorPos;
			dc.CursorMaxPos = dc.CursorStartPos;
			dc.IdealMaxPos = dc.CursorStartPos;
			dc.CurrLineSize = dc.PrevLineSize = Vector2.Zero;
			dc.CurrLineTextBaseOffset = dc.PrevLineTextBaseOffset = 0.0f;
			dc.IsSameLine = dc.IsSetPos = false;

			dc.MenuBarAppending = 0;
			dc.MenuColumns.Update( style.ItemSpacing.x, windowJustActivatedByUser );
			dc.TreeDepth = 0;
			dc.TreeHasStackDataDepthMask = 0;
			dc.ChildWindows.Clear();
			dc.StateStorage = window.StateStorage;
			dc.CurrentColumns = null;
			dc.CurrentTableIdx = -1;
			dc.LayoutType = ImGuiLayoutType.Vertical;
			dc.ParentLayoutType = parentWindow?.DC.LayoutType ?? ImGuiLayoutType.Vertical;

			if ( window.Size.x > 0.0f && (flags & ImGuiWindowFlags.Tooltip) == 0 && (flags & ImGuiWindowFlags.AlwaysAutoResize) == 0 )
				window.ItemWidthDefault = ImTrunc( window.Size.x * 0.65f );
			else
				window.ItemWidthDefault = ImTrunc( g.FontSize * 16.0f );
			dc.ItemWidth = window.ItemWidthDefault;
			dc.TextWrapPos = -1.0f;
			dc.ItemWidthStack.Clear();
			dc.TextWrapPosStack.Clear();

			if ( window.AutoFitFramesX > 0 ) window.AutoFitFramesX--;
			if ( window.AutoFitFramesY > 0 ) window.AutoFitFramesY--;

			if ( wantFocus )
				FocusWindow( window );

			// Title bar
			if ( (flags & ImGuiWindowFlags.NoTitleBar) == 0 )
				RenderWindowTitleBarContents( window, new ImRect( titleBarRect.Min.x + window.WindowBorderSize, titleBarRect.Min.y, titleBarRect.Max.x - window.WindowBorderSize, titleBarRect.Max.y ), name, hasCloseButton, ref pOpen );

			// Fill last item data with the title bar, so IsItemHovered()/IsItemActive() work right after Begin().
			g.LastItemData.ID = window.MoveId;
			g.LastItemData.InFlags = g.CurrentItemFlags;
			g.LastItemData.StatusFlags = IsMouseHoveringRect( titleBarRect.Min, titleBarRect.Max, false ) ? ImGuiItemStatusFlags.HoveredRect : ImGuiItemStatusFlags.None;
			g.LastItemData.Rect = titleBarRect;
		}

		// Clip contents to the inner area of the window
		PushClipRect( window.InnerClipRect.Min, window.InnerClipRect.Max, true );

		window.WriteAccessed = false;
		window.BeginCount++;
		g.NextWindowData.ClearFlags();

		if ( firstBeginOfTheFrame )
		{
			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 && (flags & ImGuiWindowFlags.ChildMenu) == 0 )
			{
				if ( window.OuterRectClipped.Min.x >= window.OuterRectClipped.Max.x || window.OuterRectClipped.Min.y >= window.OuterRectClipped.Max.y )
					window.HiddenFramesCanSkipItems = 1;
				if ( parentWindow is not null && (parentWindow.Collapsed || parentWindow.HiddenFramesCanSkipItems > 0) )
					window.HiddenFramesCanSkipItems = 1;
				if ( parentWindow is not null && parentWindow.HiddenFramesCannotSkipItems > 0 )
					window.HiddenFramesCannotSkipItems = 1;
			}

			if ( style.Alpha <= 0.0f )
				window.HiddenFramesCanSkipItems = 1;

			bool hiddenRegular = window.HiddenFramesCanSkipItems > 0 || window.HiddenFramesCannotSkipItems > 0;
			window.Hidden = hiddenRegular || window.HiddenFramesForRenderOnly > 0;

			bool skipItems = false;
			if ( window.Collapsed || !window.Active || hiddenRegular )
				if ( window.AutoFitFramesX <= 0 && window.AutoFitFramesY <= 0 && window.HiddenFramesCannotSkipItems <= 0 )
					skipItems = true;
			window.SkipItems = skipItems;
		}

		return !window.SkipItems;
	}

	private static void UpdateChildWindowResize( ImGuiWindow window )
	{
		var g = G;
		float handleSize = MathF.Max( 4.0f, g.Style.WindowBorderHoverPadding );
		for ( int axis = 0; axis < 2; axis++ )
		{
			if ( axis == 0 && (window.ChildFlags & ImGuiChildFlags.ResizeX) == 0 ) continue;
			if ( axis == 1 && (window.ChildFlags & ImGuiChildFlags.ResizeY) == 0 ) continue;
			var r = window.Rect();
			var bb = axis == 0
				? new ImRect( r.Max.x - handleSize, r.Min.y, r.Max.x + handleSize, r.Max.y )
				: new ImRect( r.Min.x, r.Max.y - handleSize, r.Max.x, r.Max.y + handleSize );
			int id = window.GetID( axis == 0 ? "#CHILDRESIZEX" : "#CHILDRESIZEY" );
			var backupWindow = g.CurrentWindow;
			g.CurrentWindow = window.ParentWindow;
			ItemAdd( bb, id, null, ImGuiItemFlags.NoNav );
			ButtonBehavior( bb, id, out bool hovered, out bool held, ImGuiButtonFlags.FlattenChildren | ImGuiButtonFlags.NoNavFocus );
			g.CurrentWindow = backupWindow;
			if ( hovered || held )
				g.MouseCursor = axis == 0 ? ImGuiMouseCursor.ResizeEW : ImGuiMouseCursor.ResizeNS;
			if ( held )
			{
				float target = Axis( g.IO.MousePos, axis ) - Axis( g.ActiveIdClickOffset, axis ) + handleSize - Axis( window.Pos, axis );
				target = MathF.Max( target, g.FontSize );
				window.SizeFull = WithAxis( window.SizeFull, axis, ImTrunc( target ) );
				window.Size = window.SizeFull;
			}
			if ( hovered || held )
				window.ParentWindow.DrawList.AddLine(
					axis == 0 ? new Vector2( r.Max.x, r.Min.y ) : new Vector2( r.Min.x, r.Max.y ), r.Max,
					GetColorU32Internal( held ? ImGuiCol.SeparatorActive : ImGuiCol.SeparatorHovered ), 2f );
		}
	}

	private static ImGuiCol GetWindowBgColorIdx( ImGuiWindow window )
	{
		if ( (window.Flags & (ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.Popup)) != 0 )
			return ImGuiCol.PopupBg;
		if ( (window.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
			return ImGuiCol.ChildBg;
		return ImGuiCol.WindowBg;
	}

	private static void RenderWindowDecorations( ImGuiWindow window, ImRect titleBarRect, bool titleBarIsHighlight, int resizeGripCount, Color32[] resizeGripCol, float resizeGripDrawSize )
	{
		var g = G;
		var style = g.Style;
		var flags = window.Flags;

		window.SkipItems = false;
		float windowRounding = window.WindowRounding;
		float windowBorderSize = window.WindowBorderSize;

		if ( window.Collapsed )
		{
			var titleBarCol = GetColorU32Internal( titleBarIsHighlight ? ImGuiCol.TitleBgActive : ImGuiCol.TitleBgCollapsed );
			window.DrawList.AddRectFilled( titleBarRect.Min, titleBarRect.Max, titleBarCol, windowRounding );
			if ( windowBorderSize > 0f )
				window.DrawList.AddRect( titleBarRect.Min, titleBarRect.Max, GetColorU32Internal( ImGuiCol.Border ), windowRounding, ImDrawFlags.None, windowBorderSize );
			return;
		}

		// Window background
		if ( (flags & ImGuiWindowFlags.NoBackground) == 0 )
		{
			var bgCol = GetColorU32Internal( GetWindowBgColorIdx( window ) );
			if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasBgAlpha) != 0 )
				bgCol = bgCol with { a = (byte)(ImSaturate( g.NextWindowData.BgAlphaVal ) * 255f) };
			window.DrawList.AddRectFilled( window.Pos + new Vector2( 0, window.TitleBarHeight ), window.Pos + window.Size, bgCol, windowRounding,
				(flags & ImGuiWindowFlags.NoTitleBar) != 0 ? ImDrawFlags.None : ImDrawFlags.RoundCornersBottom );
		}

		// Title bar
		if ( (flags & ImGuiWindowFlags.NoTitleBar) == 0 )
		{
			var titleBarCol = GetColorU32Internal( titleBarIsHighlight ? ImGuiCol.TitleBgActive : ImGuiCol.TitleBg );
			window.DrawList.AddRectFilled( titleBarRect.Min, titleBarRect.Max, titleBarCol, windowRounding, ImDrawFlags.RoundCornersTop );
		}

		// Menu bar
		if ( (flags & ImGuiWindowFlags.MenuBar) != 0 )
		{
			var menuBarRect = window.MenuBarRect();
			menuBarRect.ClipWith( window.Rect() );
			window.DrawList.AddRectFilled( menuBarRect.Min + new Vector2( windowBorderSize, 0 ), menuBarRect.Max - new Vector2( windowBorderSize, 0 ), GetColorU32Internal( ImGuiCol.MenuBarBg ),
				(flags & ImGuiWindowFlags.NoTitleBar) != 0 ? windowRounding : 0.0f, ImDrawFlags.RoundCornersTop );
			if ( style.FrameBorderSize > 0.0f && menuBarRect.Max.y < window.Pos.y + window.Size.y )
				window.DrawList.AddLine( menuBarRect.BL, menuBarRect.BR, GetColorU32Internal( ImGuiCol.Border ), style.FrameBorderSize );
		}

		// Scrollbars
		if ( window.ScrollbarX ) Scrollbar( ImGuiAxis.X );
		if ( window.ScrollbarY ) Scrollbar( ImGuiAxis.Y );

		// Resize grips
		if ( (flags & ImGuiWindowFlags.NoResize) == 0 )
		{
			for ( int resizeGripN = 0; resizeGripN < resizeGripCount; resizeGripN++ )
			{
				var col = resizeGripCol[resizeGripN];
				if ( col.a == 0 )
					continue;
				var grip = ResizeGripDefs[resizeGripN];
				var corner = ImLerp( window.Pos, window.Pos + window.Size, grip.CornerPosN );
				var dl = window.DrawList;
				dl.PathLineTo( corner + ImMul( grip.InnerDir, (resizeGripN & 1) != 0 ? new Vector2( windowBorderSize, resizeGripDrawSize ) : new Vector2( resizeGripDrawSize, windowBorderSize ) ) );
				dl.PathLineTo( corner + ImMul( grip.InnerDir, (resizeGripN & 1) != 0 ? new Vector2( resizeGripDrawSize, windowBorderSize ) : new Vector2( windowBorderSize, resizeGripDrawSize ) ) );
				dl.PathArcToFast( new Vector2( corner.x + grip.InnerDir.x * (windowRounding + windowBorderSize), corner.y + grip.InnerDir.y * (windowRounding + windowBorderSize) ), windowRounding, grip.AngleMin12, grip.AngleMax12 );
				dl.PathFillConvex( col );
			}
		}

		RenderWindowOuterBorders( window );
	}

	private static void RenderWindowOuterBorders( ImGuiWindow window )
	{
		var g = G;
		float rounding = window.WindowRounding;
		float borderSize = window.WindowBorderSize;
		if ( borderSize > 0.0f && (window.Flags & ImGuiWindowFlags.NoBackground) == 0 )
			window.DrawList.AddRect( window.Pos, window.Pos + window.Size, GetColorU32Internal( ImGuiCol.Border ), rounding, ImDrawFlags.None, borderSize );

		int borderHeld = window.ResizeBorderHeld;
		int border = borderHeld != -1 ? borderHeld : window.ResizeBorderHovered;
		if ( border != -1 )
		{
			var r = window.Rect();
			Vector2 a, b;
			switch ( border )
			{
				case 0: a = r.TL; b = r.BL; break;
				case 1: a = r.TR; b = r.BR; break;
				case 2: a = r.TL; b = r.TR; break;
				default: a = r.BL; b = r.BR; break;
			}
			window.DrawList.AddLine( a, b, GetColorU32Internal( borderHeld != -1 ? ImGuiCol.SeparatorActive : ImGuiCol.SeparatorHovered ), MathF.Max( 2.0f, borderSize ) );
		}

		if ( g.Style.FrameBorderSize > 0 && (window.Flags & ImGuiWindowFlags.NoTitleBar) == 0 )
		{
			float y = window.Pos.y + window.TitleBarHeight - 1;
			window.DrawList.AddLine( new Vector2( window.Pos.x + borderSize, y ), new Vector2( window.Pos.x + window.Size.x - borderSize, y ), GetColorU32Internal( ImGuiCol.Border ), g.Style.FrameBorderSize );
		}
	}

	private static void RenderWindowTitleBarContents( ImGuiWindow window, ImRect titleBarRect, string name, bool hasCloseButton, ref bool pOpen )
	{
		var g = G;
		var style = g.Style;
		var flags = window.Flags;

		bool hasCollapseButton = (flags & ImGuiWindowFlags.NoCollapse) == 0 && style.WindowMenuButtonPosition != ImGuiDir.None;

		var itemFlagsBackup = g.CurrentItemFlags;
		g.CurrentItemFlags |= ImGuiItemFlags.NoNavDefaultFocus;

		float padL = style.FramePadding.x;
		float padR = style.FramePadding.x;
		float buttonSz = g.FontSize;
		Vector2 closeButtonPos = default, collapseButtonPos = default;
		if ( hasCloseButton )
		{
			closeButtonPos = new Vector2( titleBarRect.Max.x - padR - buttonSz, titleBarRect.Min.y + style.FramePadding.y );
			padR += buttonSz + style.ItemInnerSpacing.x;
		}
		if ( hasCollapseButton && style.WindowMenuButtonPosition == ImGuiDir.Right )
		{
			collapseButtonPos = new Vector2( titleBarRect.Max.x - padR - buttonSz, titleBarRect.Min.y + style.FramePadding.y );
			padR += buttonSz + style.ItemInnerSpacing.x;
		}
		if ( hasCollapseButton && style.WindowMenuButtonPosition == ImGuiDir.Left )
		{
			collapseButtonPos = new Vector2( titleBarRect.Min.x + padL, titleBarRect.Min.y + style.FramePadding.y );
			padL += buttonSz + style.ItemInnerSpacing.x;
		}

		if ( hasCollapseButton )
			if ( CollapseButton( window.GetID( "#COLLAPSE" ), collapseButtonPos ) )
				window.WantCollapseToggle = true;

		if ( hasCloseButton )
			if ( CloseButton( window.GetID( "#CLOSE" ), closeButtonPos ) )
				pOpen = false;

		g.CurrentItemFlags = itemFlagsBackup;

		float markerSizeX = (flags & ImGuiWindowFlags.UnsavedDocument) != 0 ? buttonSz * 0.80f : 0.0f;
		var textSize = CalcTextSize( name, true ) + new Vector2( markerSizeX, 0.0f );

		if ( padL > style.FramePadding.x ) padL += style.ItemInnerSpacing.x;
		if ( padR > style.FramePadding.x ) padR += style.ItemInnerSpacing.x;
		if ( style.WindowTitleAlign.x > 0.0f && style.WindowTitleAlign.x < 1.0f )
		{
			float centerness = ImSaturate( 1.0f - MathF.Abs( style.WindowTitleAlign.x - 0.5f ) * 2.0f );
			float padExtend = MathF.Min( MathF.Max( padL, padR ), titleBarRect.Width - padL - padR - textSize.x );
			padL = MathF.Max( padL, padExtend * centerness );
			padR = MathF.Max( padR, padExtend * centerness );
		}

		var layoutR = new ImRect( titleBarRect.Min.x + padL, titleBarRect.Min.y, titleBarRect.Max.x - padR, titleBarRect.Max.y );
		var clipR = new ImRect( layoutR.Min.x, layoutR.Min.y, MathF.Min( layoutR.Max.x + style.ItemInnerSpacing.x, titleBarRect.Max.x ), layoutR.Max.y );
		if ( (flags & ImGuiWindowFlags.UnsavedDocument) != 0 )
		{
			var markerPos = new Vector2( MathF.Max( layoutR.Min.x, layoutR.Min.x + (layoutR.Width - textSize.x) * style.WindowTitleAlign.x ) + textSize.x - markerSizeX * 0.5f, layoutR.Center.y );
			RenderBullet( window.DrawList, markerPos, GetColorU32Internal( ImGuiCol.Text ) );
		}
		RenderTextClippedEx( window.DrawList, layoutR.Min, layoutR.Max, LabelText( name ), textSize - new Vector2( markerSizeX, 0f ), style.WindowTitleAlign, clipR );
	}

	internal static bool CollapseButton( int id, Vector2 pos )
	{
		var g = G;
		var window = g.CurrentWindow;
		var bb = new ImRect( pos, pos + new Vector2( g.FontSize, g.FontSize ) );
		bool isClipped = !ItemAdd( bb, id );
		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, ImGuiButtonFlags.None );
		if ( isClipped )
			return pressed;

		var bgCol = GetColorU32Internal( held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
		var textCol = GetColorU32Internal( ImGuiCol.Text );
		if ( hovered || held )
			window.DrawList.AddCircleFilled( bb.Center, g.FontSize * 0.5f + 1f, bgCol );
		RenderArrow( window.DrawList, bb.Min, textCol, window.Collapsed ? ImGuiDir.Right : ImGuiDir.Down, 1.0f );

		if ( IsItemActive() && IsMouseDragging( ImGuiMouseButton.Left ) )
			StartMouseMovingWindow( window );
		return pressed;
	}

	internal static bool CloseButton( int id, Vector2 pos )
	{
		var g = G;
		var window = g.CurrentWindow;
		var bb = new ImRect( pos, pos + new Vector2( g.FontSize, g.FontSize ) );
		var bbInteract = bb;
		float areaToVisibleRatio = window.OuterRectClipped.Area / MathF.Max( 1f, bb.Area );
		if ( areaToVisibleRatio < 1.5f )
			bbInteract.Expand( ImTrunc( bbInteract.Size * -0.25f ) );

		bool isClipped = !ItemAdd( bbInteract, id );
		bool pressed = ButtonBehavior( bbInteract, id, out bool hovered, out bool held );
		if ( isClipped )
			return pressed;

		var bgCol = GetColorU32Internal( held ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered );
		if ( hovered )
			window.DrawList.AddCircleFilled( bb.Center, g.FontSize * 0.5f + 1f, bgCol );
		float crossExtent = g.FontSize * 0.5f * 0.7071f - 1.0f;
		var crossCol = GetColorU32Internal( ImGuiCol.Text );
		var crossCenter = bb.Center - new Vector2( 0.5f, 0.5f );
		window.DrawList.AddLine( crossCenter + new Vector2( +crossExtent, +crossExtent ), crossCenter + new Vector2( -crossExtent, -crossExtent ), crossCol, 1.0f );
		window.DrawList.AddLine( crossCenter + new Vector2( +crossExtent, -crossExtent ), crossCenter + new Vector2( -crossExtent, +crossExtent ), crossCol, 1.0f );
		return pressed;
	}

	public static void End()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window is null )
			throw new InvalidOperationException( "ImGui.End() called without a matching Begin()." );
		if ( g.CurrentWindowStack.Count <= 1 && g.WithinFrameScopeWithImplicitWindow )
			throw new InvalidOperationException( "ImGui.End() called too many times!" );

		if ( window.DC.CurrentColumns is not null )
			EndColumns();
		PopClipRect();

		var stackData = g.CurrentWindowStack[^1];
		ErrorCheckEndWindowRecover( stackData );

		g.LastItemData = stackData.ParentLastItemDataBackup;
		if ( (window.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
			g.BeginMenuDepth--;
		if ( (window.Flags & ImGuiWindowFlags.Popup) != 0 )
			g.BeginPopupStack.RemoveAt( g.BeginPopupStack.Count - 1 );
		g.CurrentWindowStack.RemoveAt( g.CurrentWindowStack.Count - 1 );
		g.CurrentWindow = g.CurrentWindowStack.Count == 0 ? null : g.CurrentWindowStack[^1].Window;
	}

	/// <summary>
	/// Gracefully recover from unbalanced Push/Pop calls inside a window instead of corrupting state.
	/// </summary>
	private static void ErrorCheckEndWindowRecover( ImGuiWindowStackData stackData )
	{
		var g = G;
		var window = g.CurrentWindow;
		while ( g.GroupStack.Count > stackData.StackSizesGroupStack )
		{
			Log.Warning( $"ImGui: missing EndGroup() in '{window.Name}'" );
			EndGroup();
		}
		while ( g.ColorStack.Count > stackData.StackSizesColorStack )
		{
			Log.Warning( $"ImGui: missing PopStyleColor() in '{window.Name}'" );
			PopStyleColor();
		}
		while ( g.StyleVarStack.Count > stackData.StackSizesStyleVarStack )
		{
			Log.Warning( $"ImGui: missing PopStyleVar() in '{window.Name}'" );
			PopStyleVar();
		}
		while ( g.DisabledStackSize > stackData.StackSizesDisabledStack )
		{
			Log.Warning( $"ImGui: missing EndDisabled() in '{window.Name}'" );
			EndDisabled();
		}
		while ( g.ItemFlagsStack.Count > stackData.StackSizesItemFlagsStack )
		{
			Log.Warning( $"ImGui: missing PopItemFlag() in '{window.Name}'" );
			PopItemFlag();
		}
		while ( window.DC.TreeDepth > 0 )
		{
			Log.Warning( $"ImGui: missing TreePop() in '{window.Name}'" );
			TreePop();
		}
		while ( window.IDStack.Count > 1 )
		{
			Log.Warning( $"ImGui: missing PopID() in '{window.Name}'" );
			window.IDStack.RemoveAt( window.IDStack.Count - 1 );
		}
	}
	#endregion

	#region Child windows
	public static bool BeginChild( string strId, Vector2 size = default, ImGuiChildFlags childFlags = ImGuiChildFlags.None, ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None )
	{
		var window = GetCurrentWindow();
		return BeginChildEx( strId, window.GetID( strId ), size, childFlags, windowFlags );
	}

	public static bool BeginChild( int id, Vector2 size = default, ImGuiChildFlags childFlags = ImGuiChildFlags.None, ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None )
		=> BeginChildEx( null, id, size, childFlags, windowFlags );

	/// <summary>Legacy overload using a bool for borders.</summary>
	public static bool BeginChild( string strId, Vector2 size, bool border, ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None )
		=> BeginChild( strId, size, border ? ImGuiChildFlags.Borders : ImGuiChildFlags.None, windowFlags );

	internal static bool BeginChildEx( string name, int id, Vector2 sizeArg, ImGuiChildFlags childFlags, ImGuiWindowFlags windowFlags )
	{
		var g = G;
		var parentWindow = g.CurrentWindow;

		windowFlags |= ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoTitleBar;
		windowFlags |= parentWindow.Flags & ImGuiWindowFlags.NoMove;

		if ( (childFlags & ImGuiChildFlags.AlwaysAutoResize) != 0 && (childFlags & (ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY)) != 0 )
			windowFlags |= ImGuiWindowFlags.AlwaysAutoResize;
		if ( (childFlags & (ImGuiChildFlags.ResizeX | ImGuiChildFlags.ResizeY)) != 0 )
			childFlags |= ImGuiChildFlags.Borders;

		if ( (childFlags & ImGuiChildFlags.FrameStyle) != 0 )
		{
			PushStyleColor( ImGuiCol.ChildBg, g.Style.Colors[(int)ImGuiCol.FrameBg] );
			PushStyleVar( ImGuiStyleVar.ChildRounding, g.Style.FrameRounding );
			PushStyleVar( ImGuiStyleVar.ChildBorderSize, g.Style.FrameBorderSize );
			PushStyleVar( ImGuiStyleVar.WindowPadding, g.Style.FramePadding );
			childFlags |= ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysUseWindowPadding;
			windowFlags |= ImGuiWindowFlags.NoMove;
		}

		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasChildFlags;
		g.NextWindowData.ChildFlags = childFlags;

		var sizeAvail = GetContentRegionAvail();
		var sizeDefault = new Vector2( (childFlags & ImGuiChildFlags.AutoResizeX) != 0 ? 0.0f : sizeAvail.x, (childFlags & ImGuiChildFlags.AutoResizeY) != 0 ? 0.0f : sizeAvail.y );
		var size = CalcItemSize( sizeArg, sizeDefault.x, sizeDefault.y );
		if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSize) == 0 )
			SetNextWindowSize( size );

		string tempWindowName = name is not null
			? $"{parentWindow.Name}/{name}_{id:X8}"
			: $"{parentWindow.Name}/{id:X8}";

		float backupBorderSize = g.Style.ChildBorderSize;
		if ( (childFlags & ImGuiChildFlags.Borders) == 0 )
			g.Style.ChildBorderSize = 0.0f;

		bool ret = Begin( tempWindowName, windowFlags );

		g.Style.ChildBorderSize = backupBorderSize;
		if ( (childFlags & ImGuiChildFlags.FrameStyle) != 0 )
		{
			// The child window is now current: pop style changes made for its creation without warnings.
			PopStyleVar( 3 );
			PopStyleColor();
		}

		var childWindow = g.CurrentWindow;
		childWindow.ChildId = id;

		if ( childWindow.BeginCount == 1 )
			parentWindow.DC.CursorPos = childWindow.Pos;

		return ret;
	}

	public static void EndChild()
	{
		var g = G;
		var childWindow = g.CurrentWindow;
		if ( (childWindow.Flags & ImGuiWindowFlags.ChildWindow) == 0 )
			throw new InvalidOperationException( "EndChild() called without a matching BeginChild()." );

		var childSize = childWindow.Size;
		End();
		if ( childWindow.BeginCount == 1 )
		{
			var parentWindow = g.CurrentWindow;
			var bb = new ImRect( parentWindow.DC.CursorPos, parentWindow.DC.CursorPos + childSize );
			ItemSize( childSize );
			ItemAdd( bb, childWindow.ChildId, null, ImGuiItemFlags.NoNav );
			if ( g.HoveredWindow == childWindow )
				g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredWindow;
		}
	}
	#endregion

	#region Scrollbars
	internal static ImRect GetWindowScrollbarRect( ImGuiWindow window, ImGuiAxis axis )
	{
		var outerRect = window.Rect();
		var innerRect = window.InnerRect;
		float borderSize = window.WindowBorderSize;
		float scrollbarSize = axis == ImGuiAxis.X ? window.ScrollbarSizes.y : window.ScrollbarSizes.x;
		if ( axis == ImGuiAxis.X )
			return new ImRect( innerRect.Min.x, MathF.Max( outerRect.Min.y, outerRect.Max.y - borderSize - scrollbarSize ), innerRect.Max.x - borderSize, outerRect.Max.y - borderSize );
		return new ImRect( MathF.Max( outerRect.Min.x, outerRect.Max.x - borderSize - scrollbarSize ), innerRect.Min.y, outerRect.Max.x - borderSize, innerRect.Max.y - borderSize );
	}

	internal static void Scrollbar( ImGuiAxis axis )
	{
		var g = G;
		var window = g.CurrentWindow;
		int id = window.GetID( axis == ImGuiAxis.X ? "#SCROLLX" : "#SCROLLY" );

		var bb = GetWindowScrollbarRect( window, axis );
		var roundingCorners = ImDrawFlags.RoundCornersNone;
		if ( axis == ImGuiAxis.X )
		{
			roundingCorners |= ImDrawFlags.RoundCornersBottomLeft;
			if ( !window.ScrollbarY )
				roundingCorners |= ImDrawFlags.RoundCornersBottomRight;
		}
		else
		{
			if ( (window.Flags & ImGuiWindowFlags.NoTitleBar) != 0 && (window.Flags & ImGuiWindowFlags.MenuBar) == 0 )
				roundingCorners |= ImDrawFlags.RoundCornersTopRight;
			if ( !window.ScrollbarX )
				roundingCorners |= ImDrawFlags.RoundCornersBottomRight;
		}

		int a = (int)axis;
		float sizeVisible = Axis( window.InnerRect.Max, a ) - Axis( window.InnerRect.Min, a );
		float sizeContents = Axis( window.ContentSize, a ) + Axis( window.WindowPadding, a ) * 2.0f;
		float scroll = Axis( window.Scroll, a );
		ScrollbarEx( bb, id, axis, ref scroll, sizeVisible, sizeContents, roundingCorners );
		window.Scroll = WithAxis( window.Scroll, a, scroll );
	}

	internal static bool ScrollbarEx( ImRect bbFrame, int id, ImGuiAxis axis, ref float pScrollV, float sizeVisibleV, float sizeContentsV, ImDrawFlags drawRoundingFlags )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return false;

		float bbFrameWidth = bbFrame.Width;
		float bbFrameHeight = bbFrame.Height;
		if ( bbFrameWidth <= 0.0f || bbFrameHeight <= 0.0f )
			return false;

		float alpha = 1.0f;
		if ( axis == ImGuiAxis.Y && bbFrameHeight < g.FontSize + g.Style.FramePadding.y * 2.0f )
			alpha = ImSaturate( (bbFrameHeight - g.FontSize) / (g.Style.FramePadding.y * 2.0f) );
		if ( alpha <= 0.0f )
			return false;

		var style = g.Style;
		bool allowInteraction = alpha >= 1.0f;

		var bb = bbFrame;
		bb.Expand( new Vector2( -Math.Clamp( ImTrunc( (bbFrameWidth - 2.0f) * 0.5f ), 0.0f, 3.0f ), -Math.Clamp( ImTrunc( (bbFrameHeight - 2.0f) * 0.5f ), 0.0f, 3.0f ) ) );

		int a = (int)axis;
		float scrollbarSizeV = axis == ImGuiAxis.X ? bb.Width : bb.Height;
		float winSizeV = MathF.Max( MathF.Max( sizeContentsV, sizeVisibleV ), 1.0f );
		float grabHPixels = Math.Clamp( scrollbarSizeV * (sizeVisibleV / winSizeV), MathF.Min( style.GrabMinSize, scrollbarSizeV ), scrollbarSizeV );
		float grabHNorm = grabHPixels / scrollbarSizeV;

		ItemAdd( bbFrame, id, null, ImGuiItemFlags.NoNav );
		ButtonBehavior( bb, id, out bool hovered, out bool held, ImGuiButtonFlags.NoNavFocus );

		float scrollMax = MathF.Max( 1.0f, sizeContentsV - sizeVisibleV );
		float scrollRatio = ImSaturate( pScrollV / scrollMax );
		float grabVNorm = scrollRatio * (scrollbarSizeV - grabHPixels) / scrollbarSizeV;
		if ( held && allowInteraction && grabHNorm < 1.0f )
		{
			float scrollbarPosV = Axis( bb.Min, a );
			float mousePosV = Axis( g.IO.MousePos, a );
			float clickedVNorm = ImSaturate( (mousePosV - scrollbarPosV) / scrollbarSizeV );
			int heldDir = clickedVNorm < grabVNorm ? -1 : (clickedVNorm > grabVNorm + grabHNorm ? +1 : 0);
			if ( g.ActiveIdIsJustActivated )
				g.ScrollbarClickDeltaToGrabCenterF = (heldDir == 0 && !g.IO.KeyShift) ? clickedVNorm - grabVNorm - grabHNorm * 0.5f : 0.0f;

			float scrollVNorm = ImSaturate( (clickedVNorm - g.ScrollbarClickDeltaToGrabCenterF - grabHNorm * 0.5f) / (1.0f - grabHNorm) );
			pScrollV = MathF.Round( scrollVNorm * scrollMax );

			scrollRatio = ImSaturate( pScrollV / scrollMax );
			grabVNorm = scrollRatio * (scrollbarSizeV - grabHPixels) / scrollbarSizeV;
		}

		var bgCol = GetColorU32Internal( ImGuiCol.ScrollbarBg );
		var grabCol = GetColorU32Internal( held ? ImGuiCol.ScrollbarGrabActive : hovered ? ImGuiCol.ScrollbarGrabHovered : ImGuiCol.ScrollbarGrab, alpha );
		window.DrawList.AddRectFilled( bbFrame.Min, bbFrame.Max, bgCol, window.WindowRounding, drawRoundingFlags );
		ImRect grabRect;
		if ( axis == ImGuiAxis.X )
			grabRect = new ImRect( ImLerp( bb.Min.x, bb.Max.x, grabVNorm ), bb.Min.y, ImLerp( bb.Min.x, bb.Max.x, grabVNorm ) + grabHPixels, bb.Max.y );
		else
			grabRect = new ImRect( bb.Min.x, ImLerp( bb.Min.y, bb.Max.y, grabVNorm ), bb.Max.x, ImLerp( bb.Min.y, bb.Max.y, grabVNorm ) + grabHPixels );
		window.DrawList.AddRectFilled( grabRect.Min, grabRect.Max, grabCol, style.ScrollbarRounding );

		return held;
	}
	#endregion

	#region Scrolling
	private static float CalcScrollEdgeSnap( float target, float snapMin, float snapMax, float snapThreshold, float centerRatio )
	{
		if ( target <= snapMin + snapThreshold )
			return ImLerp( snapMin, target, centerRatio );
		if ( target >= snapMax - snapThreshold )
			return ImLerp( target, snapMax, centerRatio );
		return target;
	}

	private static Vector2 CalcNextScrollFromScrollTargetAndClamp( ImGuiWindow window )
	{
		var scroll = window.Scroll;
		var decorationSize = new Vector2( window.DecoOuterSizeX1 + window.DecoInnerSizeX1 + window.DecoOuterSizeX2, window.DecoOuterSizeY1 + window.DecoInnerSizeY1 + window.DecoOuterSizeY2 );
		for ( int axis = 0; axis < 2; axis++ )
		{
			float target = Axis( window.ScrollTarget, axis );
			if ( target < FLT_MAX )
			{
				float centerRatio = Axis( window.ScrollTargetCenterRatio, axis );
				float scrollTarget = target;
				if ( Axis( window.ScrollTargetEdgeSnapDist, axis ) > 0.0f )
				{
					float snapMax = Axis( window.ScrollMax, axis ) + Axis( window.SizeFull, axis ) - Axis( decorationSize, axis );
					scrollTarget = CalcScrollEdgeSnap( scrollTarget, 0.0f, snapMax, Axis( window.ScrollTargetEdgeSnapDist, axis ), centerRatio );
				}
				scroll = WithAxis( scroll, axis, scrollTarget - centerRatio * (Axis( window.SizeFull, axis ) - Axis( decorationSize, axis )) );
			}
			scroll = WithAxis( scroll, axis, MathF.Round( MathF.Max( Axis( scroll, axis ), 0.0f ) ) );
			if ( !window.Collapsed && !window.SkipItems )
				scroll = WithAxis( scroll, axis, MathF.Min( Axis( scroll, axis ), Axis( window.ScrollMax, axis ) ) );
		}
		return scroll;
	}

	public static float GetScrollX() => G.CurrentWindow.Scroll.x;
	public static float GetScrollY() => G.CurrentWindow.Scroll.y;
	public static float GetScrollMaxX() => G.CurrentWindow.ScrollMax.x;
	public static float GetScrollMaxY() => G.CurrentWindow.ScrollMax.y;

	internal static void SetScrollX( ImGuiWindow window, float scrollX )
	{
		window.ScrollTarget.x = scrollX;
		window.ScrollTargetCenterRatio.x = 0.0f;
		window.ScrollTargetEdgeSnapDist.x = 0.0f;
	}

	internal static void SetScrollY( ImGuiWindow window, float scrollY )
	{
		window.ScrollTarget.y = scrollY;
		window.ScrollTargetCenterRatio.y = 0.0f;
		window.ScrollTargetEdgeSnapDist.y = 0.0f;
	}

	public static void SetScrollX( float scrollX ) => SetScrollX( G.CurrentWindow, scrollX );
	public static void SetScrollY( float scrollY ) => SetScrollY( G.CurrentWindow, scrollY );

	internal static void SetScrollFromPosX( ImGuiWindow window, float localX, float centerXRatio )
	{
		window.ScrollTarget.x = ImTrunc( localX - window.DecoOuterSizeX1 - window.DecoInnerSizeX1 + window.Scroll.x );
		window.ScrollTargetCenterRatio.x = centerXRatio;
		window.ScrollTargetEdgeSnapDist.x = 0.0f;
	}

	internal static void SetScrollFromPosY( ImGuiWindow window, float localY, float centerYRatio )
	{
		window.ScrollTarget.y = ImTrunc( localY - window.DecoOuterSizeY1 - window.DecoInnerSizeY1 + window.Scroll.y );
		window.ScrollTargetCenterRatio.y = centerYRatio;
		window.ScrollTargetEdgeSnapDist.y = 0.0f;
	}

	public static void SetScrollFromPosX( float localX, float centerXRatio = 0.5f ) => SetScrollFromPosX( G.CurrentWindow, localX, centerXRatio );
	public static void SetScrollFromPosY( float localY, float centerYRatio = 0.5f ) => SetScrollFromPosY( G.CurrentWindow, localY, centerYRatio );

	public static void SetScrollHereX( float centerXRatio = 0.5f )
	{
		var g = G;
		var window = g.CurrentWindow;
		float spacingX = MathF.Max( window.WindowPadding.x, g.Style.ItemSpacing.x );
		float targetPosX = ImLerp( g.LastItemData.Rect.Min.x - spacingX, g.LastItemData.Rect.Max.x + spacingX, centerXRatio );
		SetScrollFromPosX( window, targetPosX - window.Pos.x, centerXRatio );
		window.ScrollTargetEdgeSnapDist.x = MathF.Max( 0.0f, window.WindowPadding.x - spacingX );
	}

	public static void SetScrollHereY( float centerYRatio = 0.5f )
	{
		var g = G;
		var window = g.CurrentWindow;
		float spacingY = MathF.Max( window.WindowPadding.y, g.Style.ItemSpacing.y );
		float targetPosY = ImLerp( window.DC.CursorPosPrevLine.y - spacingY, window.DC.CursorPosPrevLine.y + window.DC.PrevLineSize.y + spacingY, centerYRatio );
		SetScrollFromPosY( window, targetPosY - window.Pos.y, centerYRatio );
		window.ScrollTargetEdgeSnapDist.y = MathF.Max( 0.0f, window.WindowPadding.y - spacingY );
	}

	/// <summary>Scroll so that the given rect becomes visible in the window.</summary>
	internal static void ScrollToRect( ImGuiWindow window, ImRect rect )
	{
		var windowRect = new ImRect( window.InnerRect.Min - Vector2.One, window.InnerRect.Max + Vector2.One );
		if ( windowRect.Contains( rect ) )
			return;
		if ( rect.Min.y < windowRect.Min.y )
			SetScrollFromPosY( window, rect.Min.y - window.Pos.y - G.Style.ItemSpacing.y, 0.0f );
		else if ( rect.Max.y >= windowRect.Max.y )
			SetScrollFromPosY( window, rect.Max.y - window.Pos.y + G.Style.ItemSpacing.y, 1.0f );
		if ( rect.Min.x < windowRect.Min.x )
			SetScrollFromPosX( window, rect.Min.x - window.Pos.x - G.Style.ItemSpacing.x, 0.0f );
		else if ( rect.Max.x >= windowRect.Max.x )
			SetScrollFromPosX( window, rect.Max.x - window.Pos.x + G.Style.ItemSpacing.x, 1.0f );
	}
	#endregion

	#region Window queries / setters
	public static bool IsWindowAppearing() => G.CurrentWindow?.Appearing == true;
	public static bool IsWindowCollapsed() => G.CurrentWindow?.Collapsed == true;

	public static bool IsWindowFocused( ImGuiFocusedFlags flags = ImGuiFocusedFlags.None )
	{
		var g = G;
		var refWindow = g.NavWindow;
		var curWindow = g.CurrentWindow;
		if ( refWindow is null )
			return false;
		if ( (flags & ImGuiFocusedFlags.AnyWindow) != 0 )
			return true;
		if ( curWindow is null )
			return false;

		bool popupHierarchy = (flags & ImGuiFocusedFlags.NoPopupHierarchy) == 0;
		if ( (flags & ImGuiFocusedFlags.RootWindow) != 0 )
			curWindow = popupHierarchy ? curWindow.RootWindowPopupTree : curWindow.RootWindow;

		if ( (flags & ImGuiFocusedFlags.ChildWindows) != 0 )
			return IsWindowChildOf( refWindow, curWindow, popupHierarchy );
		return refWindow == curWindow;
	}

	public static bool IsWindowHovered( ImGuiHoveredFlags flags = ImGuiHoveredFlags.None )
	{
		var g = G;
		var refWindow = g.HoveredWindow;
		var curWindow = g.CurrentWindow;
		if ( refWindow is null )
			return false;

		if ( (flags & ImGuiHoveredFlags.AnyWindow) == 0 )
		{
			if ( curWindow is null )
				return false;
			bool popupHierarchy = (flags & ImGuiHoveredFlags.NoPopupHierarchy) == 0;
			if ( (flags & ImGuiHoveredFlags.RootWindow) != 0 )
				curWindow = popupHierarchy ? curWindow.RootWindowPopupTree : curWindow.RootWindow;

			bool result = (flags & ImGuiHoveredFlags.ChildWindows) != 0
				? IsWindowChildOf( refWindow, curWindow, popupHierarchy )
				: refWindow == curWindow;
			if ( !result )
				return false;
		}

		if ( !IsWindowContentHoverable( refWindow, flags ) )
			return false;
		if ( (flags & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) == 0 )
			if ( g.ActiveId != 0 && !g.ActiveIdAllowOverlap && g.ActiveId != refWindow.MoveId )
				return false;
		return true;
	}

	public static ImDrawList GetWindowDrawList() => GetCurrentWindow().DrawList;
	public static Vector2 GetWindowPos() => G.CurrentWindow.Pos;
	public static Vector2 GetWindowSize() => G.CurrentWindow.Size;
	public static float GetWindowWidth() => G.CurrentWindow.Size.x;
	public static float GetWindowHeight() => G.CurrentWindow.Size.y;

	public static void SetNextWindowPos( Vector2 pos, ImGuiCond cond = ImGuiCond.None, Vector2 pivot = default )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasPos;
		g.NextWindowData.PosVal = pos;
		g.NextWindowData.PosPivotVal = pivot;
		g.NextWindowData.PosCond = cond != ImGuiCond.None ? cond : ImGuiCond.Always;
	}

	public static void SetNextWindowSize( Vector2 size, ImGuiCond cond = ImGuiCond.None )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasSize;
		g.NextWindowData.SizeVal = size;
		g.NextWindowData.SizeCond = cond != ImGuiCond.None ? cond : ImGuiCond.Always;
	}

	/// <summary>
	/// Set next window size limits. Use -1 on an axis to preserve the current size. Use float.MaxValue for no maximum.
	/// </summary>
	public static void SetNextWindowSizeConstraints( Vector2 sizeMin, Vector2 sizeMax, Action<ImGuiSizeCallbackData> customCallback = null )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasSizeConstraint;
		g.NextWindowData.SizeConstraintRect = new ImRect( sizeMin, sizeMax );
		g.NextWindowData.SizeCallback = customCallback;
	}

	public static void SetNextWindowContentSize( Vector2 size )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasContentSize;
		g.NextWindowData.ContentSizeVal = ImTrunc( size );
	}

	public static void SetNextWindowCollapsed( bool collapsed, ImGuiCond cond = ImGuiCond.None )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasCollapsed;
		g.NextWindowData.CollapsedVal = collapsed;
		g.NextWindowData.CollapsedCond = cond != ImGuiCond.None ? cond : ImGuiCond.Always;
	}

	/// <summary>Causes the next window to be focused. Should be called before Begin().</summary>
	public static void SetNextWindowFocus()
	{
		G.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasFocus;
	}

	public static void SetNextWindowScroll( Vector2 scroll )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasScroll;
		g.NextWindowData.ScrollVal = scroll;
	}

	public static void SetNextWindowBgAlpha( float alpha )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasBgAlpha;
		g.NextWindowData.BgAlphaVal = alpha;
	}

	public static void SetWindowPos( Vector2 pos, ImGuiCond cond = ImGuiCond.None ) => SetWindowPos( G.CurrentWindow, pos, cond );
	public static void SetWindowSize( Vector2 size, ImGuiCond cond = ImGuiCond.None ) => SetWindowSize( G.CurrentWindow, size, cond );
	public static void SetWindowCollapsed( bool collapsed, ImGuiCond cond = ImGuiCond.None ) => SetWindowCollapsed( G.CurrentWindow, collapsed, cond );
	public static void SetWindowFocus() => FocusWindow( G.CurrentWindow );

	public static void SetWindowPos( string name, Vector2 pos, ImGuiCond cond = ImGuiCond.None )
	{
		var window = FindWindowByName( name );
		if ( window is not null ) SetWindowPos( window, pos, cond );
	}

	public static void SetWindowSize( string name, Vector2 size, ImGuiCond cond = ImGuiCond.None )
	{
		var window = FindWindowByName( name );
		if ( window is not null ) SetWindowSize( window, size, cond );
	}

	public static void SetWindowCollapsed( string name, bool collapsed, ImGuiCond cond = ImGuiCond.None )
	{
		var window = FindWindowByName( name );
		if ( window is not null ) SetWindowCollapsed( window, collapsed, cond );
	}

	public static void SetWindowFocus( string name )
	{
		if ( name is null )
		{
			FocusWindow( null );
			return;
		}
		var window = FindWindowByName( name );
		if ( window is not null ) FocusWindow( window );
	}

	/// <summary>Per-window font scale. Prefer <see cref="ImGuiIO.FontGlobalScale"/> or PushFont for most uses.</summary>
	public static void SetWindowFontScale( float scale )
	{
		var g = G;
		var window = GetCurrentWindow();
		window.FontWindowScale = scale;
		g.FontSize = window.CalcFontSize();
	}
	#endregion

	#region Focus
	internal static void FocusWindow( ImGuiWindow window )
	{
		var g = G;

		if ( g.NavWindow != window )
		{
			g.NavWindow = window;
			g.NavId = 0;
		}

		ClosePopupsOverWindow( window, false );

		if ( window is null )
			return;

		var focusFrontWindow = window.RootWindow;
		var displayFrontWindow = window.RootWindow;

		if ( g.ActiveId != 0 && g.ActiveIdWindow is not null && g.ActiveIdWindow.RootWindow != focusFrontWindow )
			if ( !g.ActiveIdNoClearOnFocusLoss )
				ClearActiveID();

		BringWindowToFocusFront( focusFrontWindow );
		if ( ((window.Flags | displayFrontWindow.Flags) & ImGuiWindowFlags.NoBringToFrontOnFocus) == 0 )
			BringWindowToDisplayFront( displayFrontWindow );
	}

	internal static void FocusTopMostWindowUnderOne( ImGuiWindow underThisWindow, ImGuiWindow ignoreWindow )
	{
		var g = G;
		int startIdx = g.WindowsFocusOrder.Count - 1;
		if ( underThisWindow is not null )
		{
			int offset = -1;
			while ( (underThisWindow.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
			{
				underThisWindow = underThisWindow.ParentWindow;
				offset = 0;
			}
			startIdx = underThisWindow.FocusOrder + offset;
		}
		for ( int i = Math.Min( startIdx, g.WindowsFocusOrder.Count - 1 ); i >= 0; i-- )
		{
			var window = g.WindowsFocusOrder[i];
			if ( window == ignoreWindow || !window.WasActive )
				continue;
			if ( (window.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 )
				continue;
			if ( (window.Flags & ImGuiWindowFlags.NoMouseInputs) == 0 )
			{
				FocusWindow( window );
				return;
			}
		}
		FocusWindow( null );
	}

	private static void BringWindowToFocusFront( ImGuiWindow window )
	{
		var g = G;
		if ( g.WindowsFocusOrder.Count == 0 || g.WindowsFocusOrder[^1] == window )
			return;
		int idx = g.WindowsFocusOrder.IndexOf( window );
		if ( idx < 0 )
			return;
		g.WindowsFocusOrder.RemoveAt( idx );
		g.WindowsFocusOrder.Add( window );
		for ( int i = idx; i < g.WindowsFocusOrder.Count; i++ )
			g.WindowsFocusOrder[i].FocusOrder = i;
	}

	private static void BringWindowToDisplayFront( ImGuiWindow window )
	{
		var g = G;
		if ( g.Windows.Count == 0 || g.Windows[^1] == window )
			return;
		int idx = g.Windows.IndexOf( window );
		if ( idx < 0 )
			return;
		g.Windows.RemoveAt( idx );
		g.Windows.Add( window );
	}

	internal static void StartMouseMovingWindow( ImGuiWindow window )
	{
		var g = G;
		FocusWindow( window );
		SetActiveID( window.MoveId, window );
		g.ActiveIdClickOffset = g.IO.MouseClickedPos[0] - window.RootWindow.Pos;
		g.ActiveIdNoClearOnFocusLoss = true;

		bool canMoveWindow = (window.Flags & ImGuiWindowFlags.NoMove) == 0 && (window.RootWindow.Flags & ImGuiWindowFlags.NoMove) == 0;
		if ( canMoveWindow )
			g.MovingWindow = window;
	}

	internal static void StopMouseMovingWindow()
	{
		var g = G;
		var window = g.MovingWindow;
		if ( window?.RootWindow is not null )
			MarkIniSettingsDirty( window.RootWindow );
		g.MovingWindow = null;
		ClearActiveID();
	}
	#endregion
}
