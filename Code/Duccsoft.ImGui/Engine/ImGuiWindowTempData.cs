namespace Duccsoft.ImGui.Engine;

/// <summary>
/// Transient per-window layout state, reset at the beginning of each frame.
/// </summary>
internal class ImGuiWindowTempData
{
	public Vector2 CursorPos;
	public Vector2 CursorPosPrevLine;
	public Vector2 CursorStartPos;
	public Vector2 CursorMaxPos;
	public Vector2 IdealMaxPos;
	public Vector2 CurrLineSize;
	public Vector2 PrevLineSize;
	public float CurrLineTextBaseOffset;
	public float PrevLineTextBaseOffset;
	public bool IsSameLine;
	public bool IsSetPos;
	public float Indent;
	public float ColumnsOffset;
	public float GroupOffset;
	public Vector2 CursorStartPosLossyness;

	public ImGuiLayoutType LayoutType;
	public ImGuiLayoutType ParentLayoutType;
	public int TreeDepth;
	public uint TreeHasStackDataDepthMask;
	public readonly List<ImGuiWindow> ChildWindows = new();
	public int MenuBarAppending;
	public Vector2 MenuBarOffset;
	public ImGuiMenuColumns MenuColumns = new();
	public float ItemWidth;
	public float TextWrapPos;
	public readonly List<float> ItemWidthStack = new();
	public readonly List<float> TextWrapPosStack = new();
	public ImGuiOldColumns CurrentColumns;
	public int CurrentTableIdx = -1;
	public ImGuiStorage StateStorage;
}
