namespace Duccsoft.ImGui.Engine;

internal partial class ImGuiContext
{
	public readonly Dictionary<int, ImGuiTable> Tables = new();
	public readonly List<ImGuiTable> TablesStack = new();
	/// <summary>Draw lists currently split into channels by a table or legacy columns (channels can't nest).</summary>
	public readonly HashSet<ImDrawList> SplitDrawLists = new();
}
