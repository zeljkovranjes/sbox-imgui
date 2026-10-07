namespace Duccsoft.ImGui.Engine;

[Flags]
internal enum ImGuiNextItemDataFlags
{
	None = 0,
	HasWidth = 1 << 0,
	HasOpen = 1 << 1,
}
