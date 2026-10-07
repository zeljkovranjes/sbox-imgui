namespace Duccsoft.ImGui.Engine;

internal class ImGuiPopupData
{
	public int PopupId;
	public ImGuiWindow Window;
	public ImGuiWindow RestoreNavWindow;
	public int ParentNavLayer;
	public int OpenFrameCount;
	public int OpenParentId;
	public Vector2 OpenPopupPos;
	public Vector2 OpenMousePos;
}
