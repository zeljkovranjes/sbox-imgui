namespace Duccsoft.ImGui.Engine;

/// <summary>
/// The full state of an ImGui instance. One context exists per scene (owned by <c>Systems.ImGuiSystem</c>).
/// </summary>
internal partial class ImGuiContext
{
	/// <summary>The context that ImGui calls currently operate on.</summary>
	public static ImGuiContext Current { get; set; }

	public ImGuiIO IO = new();
	public ImGuiStyle Style = new();
	public float AppliedStyleScale = 1f;

	public string FontName = "Roboto Mono";
	/// <summary>Font point size used for rendering, in pixels.</summary>
	public float FontPointSize = 15f;
	/// <summary>Height of a line of text in pixels. Used for all layout computations.</summary>
	public float FontSize = 15f;
	public float FontBaseSize = 15f;
	public int FontWeight = 400;

	public double Time;
	public int FrameCount;
	public int FrameCountEnded = -1;
	public int FrameCountRendered = -1;
	public bool WithinFrameScope;
	public bool WithinFrameScopeWithImplicitWindow;

	public readonly List<ImGuiWindow> Windows = new();
	public readonly List<ImGuiWindow> WindowsFocusOrder = new();
	public readonly Dictionary<int, ImGuiWindow> WindowsById = new();
	public readonly List<ImGuiWindowStackData> CurrentWindowStack = new();
	/// <summary>Windows in front-to-back order as rendered last frame (including child windows), used for hovering.</summary>
	public readonly List<ImGuiWindow> WindowsHoverOrder = new();
	public int WindowsActiveCount;
	public ImGuiWindow CurrentWindow;
	public ImGuiWindow HoveredWindow;
	public ImGuiWindow HoveredWindowUnderMovingWindow;
	public ImGuiWindow MovingWindow;
	public int BeginMenuDepth;
	public int BeginComboDepth;

	public struct WindowSettings
	{
		public Vector2 Pos;
		public Vector2 Size;
		public bool Collapsed;
	}

	/// <summary>Window positions/sizes remembered for the lifetime of the context (like Dear ImGui's .ini settings).</summary>
	public readonly Dictionary<int, WindowSettings> SettingsWindows = new();

	// Item / widget state
	public int HoveredId;
	public int HoveredIdPreviousFrame;
	public bool HoveredIdAllowOverlap;
	public bool HoveredIdDisabled;
	public float HoveredIdTimer;
	public float HoveredIdNotActiveTimer;
	public int HoverItemDelayId;
	public int HoverItemDelayIdPreviousFrame;
	public float HoverItemDelayTimer;
	public float HoverItemDelayClearTimer;
	public int HoverItemUnlockedStationaryId;
	public int HoverWindowUnlockedStationaryId;
	public float MouseStationaryTimer;
	public Vector2 MouseLastValidPos;

	public int ActiveId;
	public int ActiveIdIsAlive;
	public float ActiveIdTimer;
	public bool ActiveIdIsJustActivated;
	public bool ActiveIdAllowOverlap;
	public bool ActiveIdNoClearOnFocusLoss;
	public bool ActiveIdHasBeenPressedBefore;
	public bool ActiveIdHasBeenEditedBefore;
	public bool ActiveIdHasBeenEditedThisFrame;
	public Vector2 ActiveIdClickOffset;
	public ImGuiWindow ActiveIdWindow;
	public int ActiveIdMouseButton = -1;
	public int ActiveIdPreviousFrame;
	public bool ActiveIdPreviousFrameIsAlive;
	public bool ActiveIdPreviousFrameHasBeenEditedBefore;
	public ImGuiWindow ActiveIdPreviousFrameWindow;
	public int LastActiveId;
	public float LastActiveIdTimer;
	public int DeactivatedItemDataId;

	public ImGuiItemFlags CurrentItemFlags;
	public readonly List<ImGuiItemFlags> ItemFlagsStack = new();
	public ImGuiNextItemData NextItemData;
	public ImGuiLastItemData LastItemData;
	public ImGuiNextWindowData NextWindowData;

	public readonly List<ImGuiColorMod> ColorStack = new();
	public readonly List<ImGuiStyleMod> StyleVarStack = new();
	public readonly List<ImGuiGroupData> GroupStack = new();
	public readonly List<ImGuiPopupData> OpenPopupStack = new();
	public readonly List<ImGuiPopupData> BeginPopupStack = new();
	public int DisabledStackSize;
	public float DisabledAlphaBackup;

	// Focus (simplified: no gamepad/keyboard navigation between items)
	public ImGuiWindow NavWindow;
	public int NavId;
	/// <summary>Index of the inputable item (counted from SetKeyboardFocusHere) that should get keyboard focus.</summary>
	public int FocusRequestCounter = -1;
	public ImGuiWindow FocusRequestWindow;
	public int FocusItemCounter;
	public int FocusedTextInputRequestId;

	// Render
	public ImDrawList BackgroundDrawList = new( "##Background" );
	public ImDrawList ForegroundDrawList = new( "##Foreground" );
	public ImGuiMouseCursor MouseCursor = ImGuiMouseCursor.Arrow;

	// Widget behavior state
	public float DragCurrentAccum;
	public bool DragCurrentAccumDirty;
	public float DragSpeedDefaultRatio = 1.0f / 100.0f;
	public float SliderGrabClickOffset;
	public float SliderCurrentAccum;
	public bool SliderCurrentAccumDirty;
	public float ScrollbarClickDeltaToGrabCenterF;

	// Misc
	public int TooltipOverrideCount;
	public int WantCaptureMouseNextFrame = -1;
	public int WantCaptureKeyboardNextFrame = -1;
	public int WantTextInputNextFrame = -1;
	public readonly float[] FramerateSecPerFrame = new float[60];
	public int FramerateSecPerFrameIdx;
	public int FramerateSecPerFrameCount;
	public float FramerateSecPerFrameAccum;

	/// <summary>Arbitrary storage for widgets that need state not attached to a window.</summary>
	public readonly ImGuiStorage GlobalStorage = new();
	public readonly Dictionary<string, Vector2> TextSizeCache = new();
}
