namespace Duccsoft.ImGui.Engine;

internal partial class ImGuiContext
{
	public ImGuiColorEditFlags ColorEditOptions = ImGuiColorEditFlags.DefaultOptions_;
	public int ColorEditCurrentID;
	public int ColorEditSavedID;
	public float ColorEditSavedHue;
	public float ColorEditSavedSat;
	public Color32 ColorEditSavedColor;
	public Vector4 ColorPickerRef;
}
