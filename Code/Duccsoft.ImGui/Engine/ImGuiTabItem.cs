namespace Duccsoft.ImGui.Engine;

internal class ImGuiTabItem
{
	public int ID;
	public ImGuiTabItemFlags Flags;
	public int LastFrameVisible = -1;
	public int LastFrameSelected = -1;
	public float Offset;
	public float Width;
	public float ContentWidth;
	public float RequestedWidth = -1f;
	public int BeginOrder = -1;
	public int IndexDuringLayout = -1;
	public bool WantClose;
	public string Label;
}
