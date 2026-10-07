namespace Duccsoft.ImGui.Engine;

internal class ImGuiTableColumn
{
	public string Name;
	public int UserID;
	public ImGuiTableColumnFlags DeclFlags;
	public ImGuiTableColumnFlags Flags; // effective flags (with width policy resolved)
	public float InitWidthOrWeight;
	public float WidthRequest = -1f;
	public float StretchWeight = -1f;
	public float WidthGiven;
	public float ContentWidthPrev;
	public float ContentWidthThisFrame;
	public float MinX, MaxX, WorkMinX, WorkMaxX;
	public float ClipMinX, ClipMaxX;
	public float ItemWidth;
	public bool IsUserEnabled = true;
	public bool IsUserEnabledNextFrame = true;
	public bool IsEnabled = true;
	public bool IsVisibleX = true;
	public bool IsFrozen;
	public int DisplayOrder;
	public int DisplayIndex;
	public int SortOrder = -1;
	public ImGuiSortDirection SortDirection = ImGuiSortDirection.None;
	public bool IsStretch => (Flags & ImGuiTableColumnFlags.WidthStretch) != 0;
}
