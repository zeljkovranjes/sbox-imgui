namespace Duccsoft.ImGui.Engine;

/// <summary>State of a legacy Columns() set. Prefer tables for new code.</summary>
internal class ImGuiOldColumns
{
	public int ID;
	public ImGuiOldColumnFlags Flags;
	public bool IsFirstFrame;
	public bool IsBeingResized;
	public bool UsesChannels;
	public int Current;
	public int Count;
	public float OffMinX, OffMaxX;
	public float LineMinY, LineMaxY;
	public float HostCursorPosY;
	public float HostCursorMaxPosX;
	public ImRect HostInitialClipRect;
	public ImRect HostBackupClipRect;
	public ImRect HostBackupParentWorkRect;
	public readonly List<ImGuiOldColumnData> Columns = new();
}
