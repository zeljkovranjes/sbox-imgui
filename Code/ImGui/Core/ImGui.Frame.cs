using Duccsoft.ImGui.Rendering;

namespace Duccsoft.ImGui;

public static partial class ImGui
{
	/// <summary>
	/// Start a new frame. Called automatically by <see cref="ImGuiSystem"/> at the start of every update.
	/// </summary>
	internal static void NewFrame()
	{
		var g = G;
		var io = g.IO;

		g.Time += io.DeltaTime;
		g.FrameCount++;
		g.TooltipOverrideCount = 0;
		g.WindowsActiveCount = 0;

		// Framerate
		g.FramerateSecPerFrameAccum += io.DeltaTime - g.FramerateSecPerFrame[g.FramerateSecPerFrameIdx];
		g.FramerateSecPerFrame[g.FramerateSecPerFrameIdx] = io.DeltaTime;
		g.FramerateSecPerFrameIdx = (g.FramerateSecPerFrameIdx + 1) % g.FramerateSecPerFrame.Length;
		g.FramerateSecPerFrameCount = Math.Min( g.FramerateSecPerFrameCount + 1, g.FramerateSecPerFrame.Length );
		io.Framerate = g.FramerateSecPerFrameAccum > 0.0f ? 1.0f / (g.FramerateSecPerFrameAccum / g.FramerateSecPerFrameCount) : float.MaxValue;

		UpdateFontsAndScale();

		// Active ID bookkeeping
		if ( g.ActiveId != 0 && g.ActiveIdIsAlive != g.ActiveId && g.ActiveIdPreviousFrame == g.ActiveId )
			ClearActiveID();
		if ( g.ActiveId != 0 )
			g.ActiveIdTimer += io.DeltaTime;
		g.LastActiveIdTimer += io.DeltaTime;
		g.ActiveIdPreviousFrame = g.ActiveId;
		g.ActiveIdPreviousFrameWindow = g.ActiveIdWindow;
		g.ActiveIdPreviousFrameHasBeenEditedBefore = g.ActiveIdHasBeenEditedBefore;
		g.ActiveIdIsAlive = 0;
		g.ActiveIdHasBeenEditedThisFrame = false;
		g.ActiveIdPreviousFrameIsAlive = false;
		g.ActiveIdIsJustActivated = false;

		// Hovered ID bookkeeping
		if ( g.HoveredId != 0 && g.HoveredId == g.HoveredIdPreviousFrame )
		{
			g.HoveredIdTimer += io.DeltaTime;
			if ( g.ActiveId != g.HoveredId )
				g.HoveredIdNotActiveTimer += io.DeltaTime;
		}
		else if ( g.HoveredId == 0 )
		{
			g.HoveredIdTimer = g.HoveredIdNotActiveTimer = 0f;
		}
		g.HoveredIdPreviousFrame = g.HoveredId;
		g.HoveredId = 0;
		g.HoveredIdAllowOverlap = false;
		g.HoveredIdDisabled = false;

		UpdateHoverDelay();

		g.FocusedTextInputRequestId = g.FocusRequestWindow is null ? g.FocusedTextInputRequestId : 0;
		g.FocusItemCounter = 0;

		// Drag and drop
		NewFrameDragDrop();

		// Inputs
		io.ProcessInputEvents();
		UpdateKeyboardInputs();
		UpdateMouseInputs();

		g.BackgroundDrawList.ResetForNewFrame();
		g.ForegroundDrawList.ResetForNewFrame();

		UpdateHoveredWindowAndCaptureFlags();
		UpdateMouseMovingWindowNewFrame();

		g.MouseCursor = ImGuiMouseCursor.Arrow;
		g.WantCaptureMouseNextFrame = g.WantCaptureKeyboardNextFrame = g.WantTextInputNextFrame = -1;

		UpdateMouseWheel();

		// Mark all windows as not visible
		foreach ( var window in g.Windows )
		{
			window.WasActive = window.Active;
			window.Active = false;
			window.WriteAccessed = false;
			window.BeginCount = 0;
		}

		// Closing the focused window restores focus to the top-most remaining window
		if ( g.NavWindow is not null && !g.NavWindow.WasActive )
			FocusTopMostWindowUnderOne( null, null );

		g.CurrentWindowStack.Clear();
		g.BeginPopupStack.Clear();
		g.ItemFlagsStack.Clear();
		g.CurrentItemFlags = ImGuiItemFlags.AutoClosePopups;
		g.GroupStack.Clear();
		g.ColorStack.Clear();
		g.StyleVarStack.Clear();
		g.DisabledStackSize = 0;
		g.FontStack.Clear();
		g.BeginMenuDepth = 0;
		g.BeginComboDepth = 0;
		g.MenusIdSubmittedThisFrame.Clear();
		NewFrameTables();

		g.WithinFrameScope = true;

		// Implicit "Debug" window, only rendered if something is submitted to it.
		g.WithinFrameScopeWithImplicitWindow = true;
		SetNextWindowSize( new Vector2( 400, 400 ) * g.AppliedStyleScale, ImGuiCond.FirstUseEver );
		Begin( "Debug##Default" );
	}

	private static void UpdateFontsAndScale()
	{
		var g = G;
		var io = g.IO;
		io.DisplaySize = new Vector2( Screen.Width, Screen.Height );
		if ( io.DisplaySize.x <= 0 || io.DisplaySize.y <= 0 )
			io.DisplaySize = new Vector2( 1920, 1080 );

		float targetScale = (io.AutoScale ? MathF.Max( 0.25f, MathF.Min( io.DisplaySize.x, io.DisplaySize.y ) / 1080f ) : 1f) * MathF.Max( 0.1f, io.FontGlobalScale );
		if ( MathF.Abs( targetScale - g.AppliedStyleScale ) > 0.0001f )
		{
			g.Style.ScaleAllSizesExact( targetScale / g.AppliedStyleScale );
			g.AppliedStyleScale = targetScale;
			g.TextSizeCache.Clear();
		}

		var fontPointSize = io.FontSize * targetScale;
		if ( g.FontName != io.FontName || g.FontPointSize != fontPointSize || g.FontWeight != io.FontWeight || g.FontBaseSize <= 0 )
		{
			g.FontName = io.FontName;
			g.FontWeight = io.FontWeight;
			g.FontPointSize = fontPointSize;
			g.FontBaseSize = MeasureLineHeight( fontPointSize, g.FontName, g.FontWeight );
			g.TextSizeCache.Clear();
		}
		g.FontSize = g.FontBaseSize;
	}

	private static void UpdateHoverDelay()
	{
		var g = G;
		if ( g.HoverItemDelayId != 0 && g.HoverItemDelayIdPreviousFrame == g.HoverItemDelayId )
		{
			g.HoverItemDelayTimer += g.IO.DeltaTime;
			g.HoverItemDelayClearTimer = 0.0f;
		}
		else
		{
			g.HoverItemDelayClearTimer += g.IO.DeltaTime;
			if ( g.HoverItemDelayClearTimer >= MathF.Max( 0.25f, g.IO.DeltaTime * 2.0f ) )
			{
				g.HoverItemDelayTimer = 0.0f;
				g.HoverItemDelayClearTimer = 0.0f;
			}
		}
		g.HoverItemDelayIdPreviousFrame = g.HoverItemDelayId;
		g.HoverItemDelayId = 0;
	}

	/// <summary>Find the window under the mouse, using the window rectangles of the previous frame.</summary>
	private static void FindHoveredWindow()
	{
		var g = G;
		ImGuiWindow hoveredWindow = null;
		ImGuiWindow hoveredWindowUnderMoving = null;

		if ( g.MovingWindow is not null && (g.MovingWindow.Flags & ImGuiWindowFlags.NoMouseInputs) == 0 )
			hoveredWindow = g.MovingWindow;

		var mousePos = g.IO.MousePos;
		var paddingRegular = new Vector2( WindowsHoverPadding, WindowsHoverPadding );
		foreach ( var window in g.WindowsHoverOrder )
		{
			if ( !window.WasActive || window.Hidden )
				continue;
			if ( (window.Flags & ImGuiWindowFlags.NoMouseInputs) != 0 )
				continue;

			var bb = window.OuterRectClipped;
			if ( (window.Flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)) == 0 )
				bb.Expand( paddingRegular );
			if ( !bb.Contains( mousePos ) )
				continue;

			if ( hoveredWindow is null )
				hoveredWindow = window;
			if ( hoveredWindowUnderMoving is null && (g.MovingWindow is null || window.RootWindow != g.MovingWindow.RootWindow) )
				hoveredWindowUnderMoving = window;
			if ( hoveredWindow is not null && hoveredWindowUnderMoving is not null )
				break;
		}

		g.HoveredWindow = hoveredWindow;
		g.HoveredWindowUnderMovingWindow = hoveredWindowUnderMoving;
	}

	private static void UpdateHoveredWindowAndCaptureFlags()
	{
		var g = G;
		var io = g.IO;

		FindHoveredWindow();

		// Modal windows prevent hovering behind them.
		var modalWindow = GetTopMostPopupModal();
		if ( modalWindow is not null && g.HoveredWindow is not null && !IsWindowWithinBeginStackOf( g.HoveredWindow.RootWindow, modalWindow ) )
			g.HoveredWindow = null;

		// Track click ownership: clicks that started outside of ImGui belong to the game.
		bool hasOpenPopup = g.OpenPopupStack.Count > 0;
		bool hasOpenModal = modalWindow is not null;
		int mouseEarliestDown = -1;
		bool mouseAnyDown = false;
		for ( int i = 0; i < io.MouseDown.Length; i++ )
		{
			if ( io.MouseClicked[i] )
				io.MouseDownOwned[i] = g.HoveredWindow is not null || hasOpenPopup;
			mouseAnyDown |= io.MouseDown[i];
			if ( io.MouseDown[i] && (mouseEarliestDown == -1 || io.MouseClickedTime[i] < io.MouseClickedTime[mouseEarliestDown]) )
				mouseEarliestDown = i;
		}
		bool mouseAvail = mouseEarliestDown == -1 || io.MouseDownOwned[mouseEarliestDown];
		bool draggingExternPayload = g.DragDropActive && (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceExtern) != 0;
		if ( !mouseAvail && !draggingExternPayload )
			g.HoveredWindow = g.HoveredWindowUnderMovingWindow = null;

		if ( g.WantCaptureMouseNextFrame != -1 )
			io.WantCaptureMouse = g.WantCaptureMouseNextFrame != 0;
		else
			io.WantCaptureMouse = (mouseAvail && (g.HoveredWindow is not null || mouseAnyDown)) || hasOpenModal;

		if ( g.WantCaptureKeyboardNextFrame != -1 )
			io.WantCaptureKeyboard = g.WantCaptureKeyboardNextFrame != 0;
		else
			io.WantCaptureKeyboard = g.ActiveId != 0 && IsTextInputActive() || modalWindow is not null;

		io.WantTextInput = g.WantTextInputNextFrame != -1 ? g.WantTextInputNextFrame != 0 : IsTextInputActive();
	}

	private static void UpdateMouseMovingWindowNewFrame()
	{
		var g = G;
		if ( g.MovingWindow is not null )
		{
			KeepAliveID( g.ActiveId );
			var movingWindow = g.MovingWindow.RootWindow;
			if ( g.IO.MouseDown[0] && IsMousePosValid( g.IO.MousePos ) )
			{
				var pos = g.IO.MousePos - g.ActiveIdClickOffset;
				SetWindowPos( movingWindow, pos );
				FocusWindow( g.MovingWindow );
			}
			else
			{
				StopMouseMovingWindow();
			}
		}
		else
		{
			if ( g.ActiveIdWindow is not null && g.ActiveIdWindow.MoveId == g.ActiveId )
			{
				KeepAliveID( g.ActiveId );
				if ( !g.IO.MouseDown[0] )
					ClearActiveID();
			}
		}
	}

	private static void UpdateMouseMovingWindowEndFrame()
	{
		var g = G;
		if ( g.ActiveId != 0 || g.HoveredId != 0 )
			return;
		if ( g.NavWindow is not null && g.NavWindow.Appearing )
			return;

		if ( g.IO.MouseClicked[0] )
		{
			var rootWindow = g.HoveredWindow?.RootWindow;
			bool isClosedPopup = rootWindow is not null && (rootWindow.Flags & ImGuiWindowFlags.Popup) != 0 && !IsPopupOpen( rootWindow.PopupId, ImGuiPopupFlags.AnyPopupLevel );

			if ( rootWindow is not null && !isClosedPopup )
			{
				StartMouseMovingWindow( g.HoveredWindow );
				if ( g.IO.ConfigWindowsMoveFromTitleBarOnly && (rootWindow.Flags & ImGuiWindowFlags.NoTitleBar) == 0 )
					if ( !rootWindow.TitleBarRect().Contains( g.IO.MouseClickedPos[0] ) )
						g.MovingWindow = null;
				if ( g.HoveredIdDisabled )
					g.MovingWindow = null;
			}
			else if ( rootWindow is null && g.NavWindow is not null && GetTopMostPopupModal() is null )
			{
				// Clicking on void removes focus (and closes popups).
				FocusWindow( null );
			}
		}

		if ( g.IO.MouseClicked[1] )
		{
			var modal = GetTopMostPopupModal();
			bool hoveredWindowAboveModal = g.HoveredWindow is not null && (modal is null || IsWindowAbove( g.HoveredWindow, modal ));
			ClosePopupsOverWindow( hoveredWindowAboveModal ? g.HoveredWindow : modal, true );
		}
	}

	internal static bool IsWindowAbove( ImGuiWindow potentialAbove, ImGuiWindow potentialBelow )
	{
		var g = G;
		int a = g.WindowsHoverOrder.IndexOf( potentialAbove.RootWindow );
		int b = g.WindowsHoverOrder.IndexOf( potentialBelow.RootWindow );
		if ( a < 0 ) return false;
		if ( b < 0 ) return true;
		return a < b;
	}

	private static void UpdateMouseWheel()
	{
		var g = G;
		var io = g.IO;
		float wheelY = io.MouseWheel;
		float wheelX = io.MouseWheelH;
		if ( wheelX == 0.0f && wheelY == 0.0f )
			return;

		var window = g.HoveredWindow;
		if ( window is null || window.Collapsed )
			return;

		// Ctrl + wheel scales the font of the hovered window.
		if ( io.KeyCtrl && !io.KeyShift && wheelY != 0f )
			return;

		if ( io.KeyShift && wheelX == 0.0f )
		{
			wheelX = wheelY;
			wheelY = 0.0f;
		}

		if ( wheelY != 0.0f )
		{
			var w = window;
			while ( (w.Flags & ImGuiWindowFlags.ChildWindow) != 0 && (w.ScrollMax.y == 0.0f || ((w.Flags & ImGuiWindowFlags.NoScrollWithMouse) != 0 && (w.Flags & ImGuiWindowFlags.NoMouseInputs) == 0)) )
				w = w.ParentWindow;
			if ( (w.Flags & ImGuiWindowFlags.NoScrollWithMouse) == 0 && (w.Flags & ImGuiWindowFlags.NoMouseInputs) == 0 )
			{
				float maxStep = w.InnerRect.Height * 0.67f;
				float scrollStep = ImTrunc( MathF.Min( 5 * w.CalcFontSize(), maxStep ) );
				SetScrollY( w, w.Scroll.y - wheelY * scrollStep );
			}
		}
		if ( wheelX != 0.0f )
		{
			var w = window;
			while ( (w.Flags & ImGuiWindowFlags.ChildWindow) != 0 && (w.ScrollMax.x == 0.0f || ((w.Flags & ImGuiWindowFlags.NoScrollWithMouse) != 0 && (w.Flags & ImGuiWindowFlags.NoMouseInputs) == 0)) )
				w = w.ParentWindow;
			if ( (w.Flags & ImGuiWindowFlags.NoScrollWithMouse) == 0 && (w.Flags & ImGuiWindowFlags.NoMouseInputs) == 0 )
			{
				float maxStep = w.InnerRect.Width * 0.67f;
				float scrollStep = ImTrunc( MathF.Min( 2 * w.CalcFontSize(), maxStep ) );
				SetScrollX( w, w.Scroll.x - wheelX * scrollStep );
			}
		}
	}

	/// <summary>
	/// End the frame. Called automatically by <see cref="ImGuiSystem"/>.
	/// </summary>
	internal static void EndFrame()
	{
		var g = G;
		if ( g.FrameCountEnded == g.FrameCount )
			return;
		if ( !g.WithinFrameScope )
			return;

		// Unwind windows the user forgot to End()
		while ( g.CurrentWindowStack.Count > 1 )
		{
			var w = g.CurrentWindow;
			Log.Warning( $"ImGui: missing End() for window '{w.Name}'" );
			if ( (w.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
				EndChild();
			else
				End();
		}

		EndFrameDragDrop();

		// Hide implicit "Debug" window if it hasn't been used
		g.WithinFrameScopeWithImplicitWindow = false;
		if ( g.CurrentWindow is not null && !g.CurrentWindow.WriteAccessed )
			g.CurrentWindow.Active = false;
		End();

		g.WithinFrameScope = false;
		g.FrameCountEnded = g.FrameCount;

		UpdateMouseMovingWindowEndFrame();

		// Close popups whose window stopped being submitted.
		for ( int n = 0; n < g.OpenPopupStack.Count; n++ )
		{
			var popup = g.OpenPopupStack[n];
			if ( popup.Window is not null && !popup.Window.Active && popup.OpenFrameCount < g.FrameCount )
			{
				ClosePopupToLevel( n, false );
				break;
			}
		}

		// Clear input data for next frame
		g.IO.InputQueueCharacters.Clear();
		g.IO.InputQueueKeys.Clear();
		g.IO.PastedText = null;
	}

	/// <summary>
	/// Build the ordered list of draw lists to render this frame (back to front), and update the hover order for next frame.
	/// </summary>
	internal static List<ImDrawList> Render()
	{
		var g = G;
		if ( g.FrameCountEnded != g.FrameCount )
			EndFrame();
		g.FrameCountRendered = g.FrameCount;

		var result = new List<ImDrawList>();
		var renderOrder = new List<ImGuiWindow>();

		void AddWindowRecursive( ImGuiWindow window )
		{
			if ( !IsWindowActiveAndVisible( window ) )
				return;
			renderOrder.Add( window );
			foreach ( var child in window.DC.ChildWindows )
				if ( (child.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) == 0 )
					AddWindowRecursive( child );
		}

		result.Add( g.BackgroundDrawList );

		// Layer 0: regular windows in z-order
		foreach ( var window in g.Windows )
		{
			if ( (window.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
				continue;
			if ( (window.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 )
				continue;
			AddWindowRecursive( window );
		}

		// Layer 1: popups, in the order they were opened (modal dim background behind the top-most modal)
		var modal = GetTopMostPopupModal();
		var renderedPopups = new HashSet<ImGuiWindow>();
		foreach ( var popup in g.OpenPopupStack )
		{
			var w = popup.Window;
			if ( w is null || !renderedPopups.Add( w ) )
				continue;
			if ( w == modal && IsWindowActiveAndVisible( w ) )
			{
				var dim = new ImDrawList( "##ModalDim" );
				dim.AddRectFilled( Vector2.Zero, g.IO.DisplaySize, GetColorU32Internal( ImGuiCol.ModalWindowDimBg ) );
				result.AddRange( renderOrder.Select( x => x.DrawList ) );
				renderOrder.Clear();
				result.Add( dim );
			}
			AddWindowRecursive( w );
		}

		// Layer 2: tooltips
		foreach ( var window in g.Windows )
		{
			if ( (window.Flags & ImGuiWindowFlags.Tooltip) != 0 && (window.Flags & ImGuiWindowFlags.ChildWindow) == 0 )
				AddWindowRecursive( window );
		}

		result.AddRange( renderOrder.Select( x => x.DrawList ) );
		result.Add( g.ForegroundDrawList );

		// Build front-to-back hover order for the next frame (children are in front of their parents).
		g.WindowsHoverOrder.Clear();
		var allOrdered = new List<ImGuiWindow>();
		foreach ( var window in g.Windows )
		{
			if ( (window.Flags & ImGuiWindowFlags.ChildWindow) != 0 ) continue;
			if ( (window.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 ) continue;
			CollectRecursive( window, allOrdered );
		}
		foreach ( var popup in g.OpenPopupStack )
			if ( popup.Window is not null && !allOrdered.Contains( popup.Window ) )
				CollectRecursive( popup.Window, allOrdered );
		for ( int i = allOrdered.Count - 1; i >= 0; i-- )
			g.WindowsHoverOrder.Add( allOrdered[i] );

		g.IO.MetricsRenderWindows = result.Count;
		g.IO.MetricsActiveWindows = g.WindowsActiveCount;
		return result;
	}

	private static void CollectRecursive( ImGuiWindow window, List<ImGuiWindow> output )
	{
		if ( !window.Active )
			return;
		output.Add( window );
		foreach ( var child in window.DC.ChildWindows )
			CollectRecursive( child, output );
	}

	internal static bool IsWindowActiveAndVisible( ImGuiWindow window ) => window.Active && !window.Hidden;

	/// <summary>Background draw list: drawn behind all windows.</summary>
	public static ImDrawList GetBackgroundDrawList() => G.BackgroundDrawList;

	/// <summary>Foreground draw list: drawn on top of all windows.</summary>
	public static ImDrawList GetForegroundDrawList() => G.ForegroundDrawList;

	public static double GetTime() => G.Time;
	public static int GetFrameCount() => G.FrameCount;

	public static ImGuiStyle GetStyle() => G.Style;
}
