namespace Duccsoft.ImGui.Engine;

internal partial class ImGuiContext
{
	public bool DragDropActive;
	public bool DragDropWithinSource;
	public bool DragDropWithinTarget;
	public ImGuiDragDropFlags DragDropSourceFlags;
	public int DragDropSourceFrameCount = -1;
	public int DragDropMouseButton = -1;
	public readonly ImGuiPayload DragDropPayload = new();
	public ImRect DragDropTargetRect;
	public ImRect DragDropTargetClipRect;
	public int DragDropTargetId;
	public ImGuiDragDropFlags DragDropAcceptFlags;
	public float DragDropAcceptIdCurrRectSurface = float.MaxValue;
	public int DragDropAcceptIdCurr;
	public int DragDropAcceptIdPrev;
	public int DragDropAcceptFrameCount = -1;
	public int DragDropHoldJustPressedId;
}
