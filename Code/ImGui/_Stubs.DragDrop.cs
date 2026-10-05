namespace Duccsoft.ImGui;

// TEMPORARY stub: replaced by the real drag and drop implementation.
internal partial class ImGuiContext
{
	public bool DragDropActive;
	public ImGuiDragDropFlags DragDropSourceFlags;
	public int DragDropHoldJustPressedId;
}
public static partial class ImGui
{
	internal static void NewFrameDragDrop() { }
	internal static void EndFrameDragDrop() { }
}
