namespace Duccsoft.ImGui.Engine;

internal partial class ImGuiContext
{
	public struct FontStackEntry
	{
		public float FontSize;
		public float FontPointSize;
		public string FontName;
		public int FontWeight;
	}

	public readonly List<FontStackEntry> FontStack = new();
}
