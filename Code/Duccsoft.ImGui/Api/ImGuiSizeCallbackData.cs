namespace Duccsoft.ImGui;

/// <summary>
/// Data passed to a size constraint callback set by <see cref="ImGui.SetNextWindowSizeConstraints(Vector2, Vector2, Action{ImGuiSizeCallbackData})"/>.
/// </summary>
public class ImGuiSizeCallbackData
{
	public Vector2 Pos;
	public Vector2 CurrentSize;
	public Vector2 DesiredSize;
}
