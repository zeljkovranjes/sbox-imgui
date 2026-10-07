namespace Duccsoft.ImGui.Engine;

internal class ImGuiWindow
{
	public ImGuiWindow( ImGuiContext ctx, string name )
	{
		Name = name;
		ID = ImGui.ImHashStr( name, 0 );
		IDStack.Add( ID );
		MoveId = GetID( "#MOVE" );
		ScrollTarget = new Vector2( float.MaxValue, float.MaxValue );
		ScrollTargetCenterRatio = new Vector2( 0.5f, 0.5f );
		AutoFitFramesX = AutoFitFramesY = -1;
		AutoPosLastDirection = ImGuiDir.None;
		SetWindowPosAllowFlags = SetWindowSizeAllowFlags = SetWindowCollapsedAllowFlags = ImGuiCond.Always | ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing;
		SetWindowPosVal = new Vector2( float.MaxValue, float.MaxValue );
		SetWindowPosPivot = new Vector2( float.MaxValue, float.MaxValue );
		LastFrameActive = -1;
		LastTimeActive = -1f;
		FontWindowScale = 1.0f;
		DrawList = new ImDrawList( name );
		DC.StateStorage = StateStorage;
	}

	public string Name;
	public int ID;
	public ImGuiWindowFlags Flags;
	public ImGuiChildFlags ChildFlags;
	public Vector2 Pos;
	public Vector2 Size;
	public Vector2 SizeFull;
	public Vector2 ContentSize;
	public Vector2 ContentSizeIdeal;
	public Vector2 ContentSizeExplicit;
	public Vector2 WindowPadding;
	public float WindowRounding;
	public float WindowBorderSize;
	public float TitleBarHeight;
	public float MenuBarHeight;
	public float DecoOuterSizeX1, DecoOuterSizeY1, DecoOuterSizeX2, DecoOuterSizeY2;
	public float DecoInnerSizeX1, DecoInnerSizeY1;
	public int MoveId;
	public int ChildId;
	public int PopupId;
	public Vector2 Scroll;
	public Vector2 ScrollMax;
	public Vector2 ScrollTarget;
	public Vector2 ScrollTargetCenterRatio;
	public Vector2 ScrollTargetEdgeSnapDist;
	public Vector2 ScrollbarSizes;
	public bool ScrollbarX, ScrollbarY;
	public bool Active;
	public bool WasActive;
	public bool WriteAccessed;
	public bool Collapsed;
	public bool WantCollapseToggle;
	public bool SkipItems;
	public bool Appearing;
	public bool Hidden;
	public bool IsFallbackWindow;
	public bool HasCloseButton;
	public int ResizeBorderHovered = -1;
	public int ResizeBorderHeld = -1;
	public int BeginCount;
	public int BeginOrderWithinParent;
	public int BeginOrderWithinContext;
	public int FocusOrder = -1;
	public int AutoFitFramesX, AutoFitFramesY;
	public bool AutoFitOnlyGrows;
	public ImGuiDir AutoPosLastDirection;
	public int HiddenFramesCanSkipItems;
	public int HiddenFramesCannotSkipItems;
	public int HiddenFramesForRenderOnly;
	public ImGuiCond SetWindowPosAllowFlags;
	public ImGuiCond SetWindowSizeAllowFlags;
	public ImGuiCond SetWindowCollapsedAllowFlags;
	public Vector2 SetWindowPosVal;
	public Vector2 SetWindowPosPivot;
	public float FontWindowScale;

	public readonly List<int> IDStack = new();
	public readonly ImGuiWindowTempData DC = new();

	public ImRect OuterRectClipped;
	public ImRect InnerRect;
	public ImRect InnerClipRect;
	public ImRect WorkRect;
	public ImRect ParentWorkRect;
	public ImRect ClipRect;
	public ImRect ContentRegionRect;

	public int LastFrameActive;
	public int LastFrameJustFocused;
	public float LastTimeActive;
	public float ItemWidthDefault;
	public readonly ImGuiStorage StateStorage = new();
	public readonly List<ImGuiOldColumns> ColumnsStorage = new();

	public ImDrawList DrawList;
	public ImGuiWindow ParentWindow;
	public ImGuiWindow ParentWindowInBeginStack;
	public ImGuiWindow RootWindow;
	public ImGuiWindow RootWindowPopupTree;
	public ImGuiWindow RootWindowForTitleBarHighlight;

	public float CalcFontSize() => ImGuiContext.Current.FontBaseSize * FontWindowScale;
	public ImRect Rect() => new( Pos.x, Pos.y, Pos.x + Size.x, Pos.y + Size.y );
	public ImRect TitleBarRect() => new( Pos, new Vector2( Pos.x + SizeFull.x, Pos.y + TitleBarHeight ) );
	public ImRect MenuBarRect()
	{
		float y1 = Pos.y + TitleBarHeight;
		return new ImRect( Pos.x, y1, Pos.x + SizeFull.x, y1 + MenuBarHeight );
	}

	public int GetID( string str )
	{
		int seed = IDStack[^1];
		return ImGui.ImHashStr( str, seed );
	}

	public int GetID( int n )
	{
		int seed = IDStack[^1];
		return ImGui.ImHashInt( n, seed );
	}

	public int GetIDFromRectangle( ImRect r )
	{
		int seed = IDStack[^1];
		int h = ImGui.ImHashInt( (int)r.Min.x, seed );
		h = ImGui.ImHashInt( (int)r.Min.y, h );
		h = ImGui.ImHashInt( (int)r.Max.x, h );
		return ImGui.ImHashInt( (int)r.Max.y, h );
	}

	public override string ToString() => $"Window '{Name}' ({ID:X8})";
}
