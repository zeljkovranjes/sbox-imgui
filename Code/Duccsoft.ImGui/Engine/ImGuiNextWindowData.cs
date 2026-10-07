namespace Duccsoft.ImGui.Engine;

internal struct ImGuiNextWindowData
{
	public ImGuiNextWindowDataFlags Flags;
	public ImGuiCond PosCond;
	public ImGuiCond SizeCond;
	public ImGuiCond CollapsedCond;
	public Vector2 PosVal;
	public Vector2 PosPivotVal;
	public Vector2 SizeVal;
	public Vector2 ContentSizeVal;
	public Vector2 ScrollVal;
	public ImGuiChildFlags ChildFlags;
	public bool CollapsedVal;
	public ImRect SizeConstraintRect;
	public Action<ImGuiSizeCallbackData> SizeCallback;
	public float BgAlphaVal;
	public Vector2 MenuBarOffsetMinVal;

	public void ClearFlags() => Flags = ImGuiNextWindowDataFlags.None;
}
