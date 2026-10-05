namespace Duccsoft.ImGui;

// TEMPORARY stub: replaced by the real tables / columns / tab bar implementation.
internal class ImGuiTable { public ImRect WorkRect; }
internal class ImGuiOldColumns { }
public static partial class ImGui
{
	internal static ImGuiTable CurrentTable => null;
	internal static void NewFrameTables() { }
	public static void EndColumns() { }
	internal static void PushColumnsBackground() { }
	internal static void PopColumnsBackground() { }
}
