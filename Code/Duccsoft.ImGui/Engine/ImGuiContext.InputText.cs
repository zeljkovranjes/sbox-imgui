namespace Duccsoft.ImGui.Engine;

internal partial class ImGuiContext
{
	public ImGuiInputTextState InputTextState = new();
	public int TempInputId;
	public bool InputTextNoMarkEdited;
	public bool InputTextClipboardHandledByPanel;
}
