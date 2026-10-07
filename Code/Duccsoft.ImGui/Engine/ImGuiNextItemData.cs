namespace Duccsoft.ImGui.Engine;

internal struct ImGuiNextItemData
{
	public ImGuiNextItemDataFlags Flags;
	public ImGuiItemFlags ItemFlags;
	public float Width;
	public ImGuiCond OpenCond;
	public bool OpenVal;

	public void ClearFlags()
	{
		Flags = ImGuiNextItemDataFlags.None;
		ItemFlags = ImGuiItemFlags.None;
	}
}
