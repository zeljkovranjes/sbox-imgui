namespace Duccsoft.ImGui.Engine;

[Flags]
internal enum ImGuiNextWindowDataFlags
{
	None = 0,
	HasPos = 1 << 0,
	HasSize = 1 << 1,
	HasContentSize = 1 << 2,
	HasCollapsed = 1 << 3,
	HasSizeConstraint = 1 << 4,
	HasFocus = 1 << 5,
	HasBgAlpha = 1 << 6,
	HasScroll = 1 << 7,
	HasChildFlags = 1 << 8,
}
