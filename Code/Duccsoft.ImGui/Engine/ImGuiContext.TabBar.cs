namespace Duccsoft.ImGui.Engine;

internal partial class ImGuiContext
{
	public readonly Dictionary<int, ImGuiTabBar> TabBars = new();
	public readonly List<ImGuiTabBar> TabBarStack = new();
	public ImGuiTabBar CurrentTabBar => TabBarStack.Count > 0 ? TabBarStack[^1] : null;
}
