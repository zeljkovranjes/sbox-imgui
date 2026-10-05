namespace Duccsoft.ImGui;

public static partial class ImGui
{
	public static ImGuiIO GetIO() => G.IO;

	#region Mouse
	public static bool IsMouseDown( ImGuiMouseButton button ) => G.IO.MouseDown[(int)button];

	public static bool IsMouseClicked( ImGuiMouseButton button, bool repeat = false )
	{
		var g = G;
		int b = (int)button;
		float t = g.IO.MouseDownDuration[b];
		if ( t == 0.0f )
			return true;
		if ( repeat && t > g.IO.KeyRepeatDelay )
			return CalcTypematicRepeatAmount( t - g.IO.DeltaTime, t, g.IO.KeyRepeatDelay, g.IO.KeyRepeatRate ) > 0;
		return false;
	}

	public static bool IsMouseReleased( ImGuiMouseButton button ) => G.IO.MouseReleased[(int)button];
	public static bool IsMouseDoubleClicked( ImGuiMouseButton button ) => G.IO.MouseClickedCount[(int)button] == 2 && G.IO.MouseClicked[(int)button];
	public static int GetMouseClickedCount( ImGuiMouseButton button ) => G.IO.MouseClickedCount[(int)button];

	/// <summary>Is mouse hovering given bounding rect (in screen space), clipped by the current clipping settings.</summary>
	public static bool IsMouseHoveringRect( Vector2 rMin, Vector2 rMax, bool clip = true )
	{
		var g = G;
		var rectClipped = new ImRect( rMin, rMax );
		if ( clip && g.CurrentWindow is not null )
			rectClipped.ClipWith( g.CurrentWindow.ClipRect );

		var rectForTouch = rectClipped;
		rectForTouch.Expand( g.Style.TouchExtraPadding );
		return rectForTouch.Contains( g.IO.MousePos );
	}

	public static bool IsMousePosValid( Vector2? mousePos = null )
	{
		const float MOUSE_INVALID = -256000.0f;
		var p = mousePos ?? G.IO.MousePos;
		return p.x >= MOUSE_INVALID && p.y >= MOUSE_INVALID;
	}

	public static bool IsAnyMouseDown()
	{
		var io = G.IO;
		for ( int n = 0; n < io.MouseDown.Length; n++ )
			if ( io.MouseDown[n] ) return true;
		return false;
	}

	public static Vector2 GetMousePos() => G.IO.MousePos;

	public static Vector2 GetMousePosOnOpeningCurrentPopup()
	{
		var g = G;
		if ( g.BeginPopupStack.Count > 0 )
			return g.OpenPopupStack[g.BeginPopupStack.Count - 1].OpenMousePos;
		return g.IO.MousePos;
	}

	public static bool IsMouseDragging( ImGuiMouseButton button, float lockThreshold = -1.0f )
	{
		var g = G;
		if ( !g.IO.MouseDown[(int)button] )
			return false;
		return IsMouseDragPastThreshold( button, lockThreshold );
	}

	internal static bool IsMouseDragPastThreshold( ImGuiMouseButton button, float lockThreshold = -1.0f )
	{
		var g = G;
		if ( lockThreshold < 0.0f )
			lockThreshold = g.IO.MouseDragThreshold;
		return g.IO.MouseDragMaxDistanceSqr[(int)button] >= lockThreshold * lockThreshold;
	}

	public static Vector2 GetMouseDragDelta( ImGuiMouseButton button = ImGuiMouseButton.Left, float lockThreshold = -1.0f )
	{
		var g = G;
		int b = (int)button;
		if ( lockThreshold < 0.0f )
			lockThreshold = g.IO.MouseDragThreshold;
		if ( g.IO.MouseDown[b] || g.IO.MouseReleased[b] )
			if ( g.IO.MouseDragMaxDistanceSqr[b] >= lockThreshold * lockThreshold )
				if ( IsMousePosValid( g.IO.MousePos ) && IsMousePosValid( g.IO.MouseClickedPos[b] ) )
					return g.IO.MousePos - g.IO.MouseClickedPos[b];
		return Vector2.Zero;
	}

	public static void ResetMouseDragDelta( ImGuiMouseButton button = ImGuiMouseButton.Left )
	{
		var g = G;
		g.IO.MouseClickedPos[(int)button] = g.IO.MousePos;
	}

	public static ImGuiMouseCursor GetMouseCursor() => G.MouseCursor;
	public static void SetMouseCursor( ImGuiMouseCursor cursorType ) => G.MouseCursor = cursorType;

	public static void SetNextFrameWantCaptureMouse( bool wantCaptureMouse ) => G.WantCaptureMouseNextFrame = wantCaptureMouse ? 1 : 0;
	public static void SetNextFrameWantCaptureKeyboard( bool wantCaptureKeyboard ) => G.WantCaptureKeyboardNextFrame = wantCaptureKeyboard ? 1 : 0;
	#endregion

	#region Keyboard
	public static bool IsKeyDown( ImGuiKey key )
	{
		if ( TryModKey( key, out var down ) ) return down;
		int idx = (int)key;
		if ( idx <= 0 || idx >= (int)ImGuiKey.COUNT ) return false;
		return G.IO.KeysData[idx].Down;
	}

	public static bool IsKeyPressed( ImGuiKey key, bool repeat = true )
	{
		int idx = (int)key;
		if ( idx <= 0 || idx >= (int)ImGuiKey.COUNT ) return false;
		var io = G.IO;
		var data = io.KeysData[idx];
		float t = data.DownDuration;
		if ( t < 0.0f ) return false;
		if ( t == 0.0f ) return true;
		if ( repeat && t > io.KeyRepeatDelay )
			return CalcTypematicRepeatAmount( t - io.DeltaTime, t, io.KeyRepeatDelay, io.KeyRepeatRate ) > 0;
		return false;
	}

	public static bool IsKeyReleased( ImGuiKey key )
	{
		int idx = (int)key;
		if ( idx <= 0 || idx >= (int)ImGuiKey.COUNT ) return false;
		var data = G.IO.KeysData[idx];
		return data.DownDurationPrev >= 0.0f && !data.Down;
	}

	public static bool IsKeyChordPressed( ImGuiKey keyChord )
	{
		var io = G.IO;
		var mods = (ImGuiKey)((int)keyChord & (int)ImGuiKey.ImGuiMod_Mask_);
		var key = (ImGuiKey)((int)keyChord & ~(int)ImGuiKey.ImGuiMod_Mask_);
		if ( ((mods & ImGuiKey.ImGuiMod_Ctrl) != 0) != io.KeyCtrl ) return false;
		if ( ((mods & ImGuiKey.ImGuiMod_Shift) != 0) != io.KeyShift ) return false;
		if ( ((mods & ImGuiKey.ImGuiMod_Alt) != 0) != io.KeyAlt ) return false;
		return key == ImGuiKey.None || IsKeyPressed( key, false );
	}

	public static bool Shortcut( ImGuiKey keyChord ) => IsKeyChordPressed( keyChord );

	public static int GetKeyPressedAmount( ImGuiKey key, float repeatDelay, float rate )
	{
		int idx = (int)key;
		if ( idx <= 0 || idx >= (int)ImGuiKey.COUNT ) return 0;
		var data = G.IO.KeysData[idx];
		if ( !data.Down ) return 0;
		float t = data.DownDuration;
		return CalcTypematicRepeatAmount( t - G.IO.DeltaTime, t, repeatDelay, rate );
	}

	public static string GetKeyName( ImGuiKey key )
	{
		if ( key == ImGuiKey.None ) return "None";
		var name = key.ToString();
		return name.StartsWith( '_' ) ? name.Substring( 1 ) : name;
	}

	private static bool TryModKey( ImGuiKey key, out bool down )
	{
		var io = G.IO;
		switch ( key )
		{
			case ImGuiKey.ImGuiMod_Ctrl: down = io.KeyCtrl; return true;
			case ImGuiKey.ImGuiMod_Shift: down = io.KeyShift; return true;
			case ImGuiKey.ImGuiMod_Alt: down = io.KeyAlt; return true;
			case ImGuiKey.ImGuiMod_Super: down = io.KeySuper; return true;
		}
		down = false;
		return false;
	}

	internal static int CalcTypematicRepeatAmount( float t0, float t1, float repeatDelay, float repeatRate )
	{
		if ( t1 == 0.0f )
			return 1;
		if ( t0 >= t1 )
			return 0;
		if ( repeatRate <= 0.0f )
			return (t0 < repeatDelay && t1 >= repeatDelay) ? 1 : 0;
		int countT0 = (t0 < repeatDelay) ? -1 : (int)((t0 - repeatDelay) / repeatRate);
		int countT1 = (t1 < repeatDelay) ? -1 : (int)((t1 - repeatDelay) / repeatRate);
		return countT1 - countT0;
	}

	/// <summary>Focus the next (or offset-th next) text input widget so it receives keyboard input.</summary>
	public static void SetKeyboardFocusHere( int offset = 0 )
	{
		var g = G;
		g.FocusRequestWindow = g.CurrentWindow;
		g.FocusRequestCounter = offset;
		g.FocusItemCounter = 0;
	}
	#endregion

	#region Frame input update
	internal static void UpdateMouseInputs()
	{
		var g = G;
		var io = g.IO;

		if ( IsMousePosValid( io.MousePos ) )
			io.MousePos = g.MouseLastValidPos = ImFloor( io.MousePos );

		if ( IsMousePosValid( io.MousePos ) && IsMousePosValid( io.MousePosPrev ) )
			io.MouseDelta = io.MousePos - io.MousePosPrev;
		else
			io.MouseDelta = Vector2.Zero;

		const float mouseStationaryThreshold = 2.0f;
		bool mouseStationary = ImLengthSqr( io.MouseDelta ) <= mouseStationaryThreshold * mouseStationaryThreshold;
		g.MouseStationaryTimer = mouseStationary ? g.MouseStationaryTimer + io.DeltaTime : 0.0f;

		io.MousePosPrev = io.MousePos;
		for ( int i = 0; i < io.MouseDown.Length; i++ )
		{
			io.MouseClicked[i] = io.MouseDown[i] && io.MouseDownDuration[i] < 0.0f;
			io.MouseClickedCount[i] = 0;
			io.MouseReleased[i] = !io.MouseDown[i] && io.MouseDownDuration[i] >= 0.0f;
			io.MouseDownDurationPrev[i] = io.MouseDownDuration[i];
			io.MouseDownDuration[i] = io.MouseDown[i] ? (io.MouseDownDuration[i] < 0.0f ? 0.0f : io.MouseDownDuration[i] + io.DeltaTime) : -1.0f;
			if ( io.MouseClicked[i] )
			{
				bool isRepeatedClick = false;
				if ( (float)(g.Time - io.MouseClickedTime[i]) < io.MouseDoubleClickTime )
				{
					var deltaFromClickPos = IsMousePosValid( io.MousePos ) ? (io.MousePos - io.MouseClickedPos[i]) : Vector2.Zero;
					if ( ImLengthSqr( deltaFromClickPos ) < io.MouseDoubleClickMaxDist * io.MouseDoubleClickMaxDist )
						isRepeatedClick = true;
				}
				if ( isRepeatedClick )
					io.MouseClickedLastCount[i]++;
				else
					io.MouseClickedLastCount[i] = 1;
				io.MouseClickedTime[i] = g.Time;
				io.MouseClickedPos[i] = io.MousePos;
				io.MouseClickedCount[i] = io.MouseClickedLastCount[i];
				io.MouseDragMaxDistanceSqr[i] = 0.0f;
			}
			else if ( io.MouseDown[i] )
			{
				var deltaFromClickPos = IsMousePosValid( io.MousePos ) ? (io.MousePos - io.MouseClickedPos[i]) : Vector2.Zero;
				io.MouseDragMaxDistanceSqr[i] = MathF.Max( io.MouseDragMaxDistanceSqr[i], ImLengthSqr( deltaFromClickPos ) );
			}
			io.MouseDoubleClicked[i] = io.MouseClickedCount[i] == 2;
		}
	}

	internal static void UpdateKeyboardInputs()
	{
		var g = G;
		var io = g.IO;
		for ( int k = (int)ImGuiKey.Tab; k < (int)ImGuiKey.COUNT; k++ )
		{
			var key = (ImGuiKey)k;
			bool down = io.KeysDownFromEvents[k] || ImGuiSystem.PollKeyDown( key );
			ref var data = ref io.KeysData[k];
			data.Down = down;
			data.DownDurationPrev = data.DownDuration;
			data.DownDuration = down ? (data.DownDuration < 0.0f ? 0.0f : data.DownDuration + io.DeltaTime) : -1.0f;
		}

		io.KeyCtrl = io.KeysData[(int)ImGuiKey.LeftCtrl].Down || io.KeysData[(int)ImGuiKey.RightCtrl].Down;
		io.KeyShift = io.KeysData[(int)ImGuiKey.LeftShift].Down || io.KeysData[(int)ImGuiKey.RightShift].Down;
		io.KeyAlt = io.KeysData[(int)ImGuiKey.LeftAlt].Down || io.KeysData[(int)ImGuiKey.RightAlt].Down;
		io.KeySuper = io.KeysData[(int)ImGuiKey.LeftSuper].Down || io.KeysData[(int)ImGuiKey.RightSuper].Down;
	}
	#endregion
}
