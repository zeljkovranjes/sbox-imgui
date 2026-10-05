namespace Duccsoft.ImGui;

// TEMPORARY: replaced by the real implementations (tables, columns, text input, drag & drop).
internal class ImGuiTable { public ImRect WorkRect; }
internal class ImGuiOldColumns { }
internal partial class ImGuiContext
{
	public bool DragDropActive;
	public ImGuiDragDropFlags DragDropSourceFlags;
	public int DragDropHoldJustPressedId;
}
public static partial class ImGui
{
	internal static ImGuiTable CurrentTable => null;
	internal static void NewFrameTables() { }
	internal static void NewFrameDragDrop() { }
	internal static void EndFrameDragDrop() { }
	internal static bool IsTextInputActive() => false;
	public static void EndColumns() { }
	internal static void PushColumnsBackground() { }
	internal static void PopColumnsBackground() { }
}
