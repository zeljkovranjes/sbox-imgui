namespace Duccsoft.ImGui.Engine;

internal class ImGuiGroupData
{
	public int WindowID;
	public Vector2 BackupCursorPos;
	public Vector2 BackupCursorMaxPos;
	public Vector2 BackupCursorPosPrevLine;
	public float BackupIndent;
	public float BackupGroupOffset;
	public Vector2 BackupCurrLineSize;
	public float BackupCurrLineTextBaseOffset;
	public int BackupActiveIdIsAlive;
	public bool BackupActiveIdPreviousFrameIsAlive;
	public bool BackupHoveredIdIsAlive;
	public bool BackupIsSameLine;
	public bool EmitItem;
}
