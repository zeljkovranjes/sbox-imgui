namespace Duccsoft.ImGui;

public class ImGuiIO
{
	public ImGuiIO()
	{
		for ( int i = 0; i < KeysData.Length; i++ )
			KeysData[i] = new KeyData { DownDuration = -1f, DownDurationPrev = -1f };
	}

	#region Configuration
	/// <summary>Main display size in pixels. Updated every frame from <see cref="Screen.Size"/>.</summary>
	public Vector2 DisplaySize;
	/// <summary>Time elapsed since last frame, in seconds.</summary>
	public float DeltaTime = 1.0f / 60.0f;
	/// <summary>When enabled, style sizes and font size are scaled so the UI looks the same at any resolution (reference: 1080p).</summary>
	public bool AutoScale = true;
	/// <summary>Additional global scale applied on top of <see cref="AutoScale"/>.</summary>
	public float FontGlobalScale = 1.0f;
	/// <summary>Font family used to render all text.</summary>
	public string FontName = "Roboto Mono";
	/// <summary>Font size in pixels at the reference resolution (1080p).</summary>
	public float FontSize = 15.0f;
	public int FontWeight = 400;

	public float MouseDoubleClickTime = 0.30f;
	public float MouseDoubleClickMaxDist = 6.0f;
	public float MouseDragThreshold = 6.0f;
	public float KeyRepeatDelay = 0.275f;
	public float KeyRepeatRate = 0.050f;

	public bool ConfigInputTextCursorBlink = true;
	public bool ConfigInputTextEnterKeepActive = false;
	public bool ConfigDragClickToInputText = false;
	public bool ConfigWindowsResizeFromEdges = true;
	public bool ConfigWindowsMoveFromTitleBarOnly = false;
	public float ConfigMemoryCompactTimer = 60.0f;
	/// <summary>When true, the mouse cursor shape is driven by ImGui (e.g. text beam over text inputs).</summary>
	public bool ConfigDrawCursorShape = true;
	#endregion

	#region Input (written by the backend, or by you to inject input)
	public Vector2 MousePos = new( -float.MaxValue, -float.MaxValue );
	public bool[] MouseDown = new bool[5];
	public float MouseWheel;
	public float MouseWheelH;
	public bool KeyCtrl;
	public bool KeyShift;
	public bool KeyAlt;
	public bool KeySuper;
	#endregion

	#region Output
	/// <summary>Set when ImGui wants to use the mouse; your game should not process mouse input when true.</summary>
	public bool WantCaptureMouse;
	/// <summary>Set when ImGui wants to use the keyboard (e.g. a text field is active).</summary>
	public bool WantCaptureKeyboard;
	/// <summary>Set when a text input widget is active.</summary>
	public bool WantTextInput;
	public float Framerate;
	public int MetricsRenderVertices;
	public int MetricsRenderWindows;
	public int MetricsActiveWindows;
	public Vector2 MouseDelta;
	#endregion

	#region Internal state
	internal Vector2 MousePosPrev = new( -float.MaxValue, -float.MaxValue );
	internal Vector2[] MouseClickedPos = new Vector2[5];
	internal double[] MouseClickedTime = new double[5] { -1000, -1000, -1000, -1000, -1000 };
	internal bool[] MouseClicked = new bool[5];
	internal bool[] MouseDoubleClicked = new bool[5];
	internal int[] MouseClickedCount = new int[5];
	internal int[] MouseClickedLastCount = new int[5];
	internal bool[] MouseReleased = new bool[5];
	internal bool[] MouseDownOwned = new bool[5];
	internal float[] MouseDownDuration = new float[5] { -1, -1, -1, -1, -1 };
	internal float[] MouseDownDurationPrev = new float[5] { -1, -1, -1, -1, -1 };
	internal float[] MouseDragMaxDistanceSqr = new float[5];

	internal readonly List<char> InputQueueCharacters = new();

	internal struct KeyTyped
	{
		public ImGuiKey Key;
		public bool Ctrl;
		public bool Shift;
		public bool Alt;
	}

	/// <summary>Keys typed this frame (including OS key repeats), consumed by text input widgets.</summary>
	internal readonly List<KeyTyped> InputQueueKeys = new();
	internal string PastedText;

	internal struct KeyData
	{
		public bool Down;
		public float DownDuration;
		public float DownDurationPrev;
	}

	internal readonly KeyData[] KeysData = new KeyData[(int)ImGuiKey.COUNT];
	internal readonly bool[] KeysDownFromEvents = new bool[(int)ImGuiKey.COUNT];

	private struct InputEvent
	{
		public int Type; // 0 = mouse button, 1 = wheel
		public int Button;
		public bool Down;
		public Vector2 Wheel;
	}

	private readonly List<InputEvent> _inputEvents = new();
	#endregion

	#region Input API
	/// <summary>Queue a mouse button change. Events are trickled so that fast clicks are never lost.</summary>
	public void AddMouseButtonEvent( int button, bool down )
	{
		if ( button < 0 || button >= MouseDown.Length )
			return;
		_inputEvents.Add( new InputEvent { Type = 0, Button = button, Down = down } );
	}

	public void AddMouseWheelEvent( float wheelX, float wheelY )
	{
		_inputEvents.Add( new InputEvent { Type = 1, Wheel = new Vector2( wheelX, wheelY ) } );
	}

	public void AddMousePosEvent( float x, float y ) => MousePos = new Vector2( x, y );

	public void AddInputCharacter( char c )
	{
		if ( c != 0 )
			InputQueueCharacters.Add( c );
	}

	public void AddInputCharactersUTF8( string str )
	{
		if ( str is null ) return;
		foreach ( var c in str ) AddInputCharacter( c );
	}

	public void AddKeyEvent( ImGuiKey key, bool down )
	{
		var idx = (int)key;
		if ( idx <= 0 || idx >= KeysDownFromEvents.Length )
			return;
		KeysDownFromEvents[idx] = down;
	}

	/// <summary>Queue a typed key (including OS auto-repeat) for text editing widgets.</summary>
	public void AddKeyTyped( ImGuiKey key, bool ctrl = false, bool shift = false, bool alt = false )
	{
		if ( key == ImGuiKey.None ) return;
		InputQueueKeys.Add( new KeyTyped { Key = key, Ctrl = ctrl, Shift = shift, Alt = alt } );
	}

	/// <summary>Queue text to be pasted into the active text input.</summary>
	public void AddPasteEvent( string text ) => PastedText = text;

	public void ClearInputKeys()
	{
		Array.Clear( KeysDownFromEvents );
		InputQueueCharacters.Clear();
		InputQueueKeys.Clear();
	}

	/// <summary>
	/// Apply queued mouse events. A button that changes twice in a frame leaves the second change for the next frame.
	/// </summary>
	internal void ProcessInputEvents()
	{
		MouseWheel = 0;
		MouseWheelH = 0;
		Span<bool> changed = stackalloc bool[5];
		int consumed = 0;
		for ( ; consumed < _inputEvents.Count; consumed++ )
		{
			var e = _inputEvents[consumed];
			if ( e.Type == 0 )
			{
				if ( changed[e.Button] )
					break;
				if ( MouseDown[e.Button] != e.Down )
				{
					MouseDown[e.Button] = e.Down;
					changed[e.Button] = true;
				}
			}
			else
			{
				MouseWheelH += e.Wheel.x;
				MouseWheel += e.Wheel.y;
			}
		}
		_inputEvents.RemoveRange( 0, consumed );
	}
	#endregion
}
