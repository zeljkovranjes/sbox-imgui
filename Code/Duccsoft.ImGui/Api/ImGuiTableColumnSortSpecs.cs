namespace Duccsoft.ImGui;

/// <summary>Sorting specification for one column of a table.</summary>
public class ImGuiTableColumnSortSpecs
{
	/// <summary>User id of the column (if specified by a TableSetupColumn() call).</summary>
	public int ColumnUserID;
	/// <summary>Index of the column.</summary>
	public int ColumnIndex;
	/// <summary>Index within parent ImGuiTableSortSpecs (always stored in order starting from 0, tables sorted on a single criteria will always have a 0 here).</summary>
	public int SortOrder;
	public ImGuiSortDirection SortDirection;
}
