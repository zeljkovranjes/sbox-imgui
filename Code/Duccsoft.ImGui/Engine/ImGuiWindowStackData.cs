namespace Duccsoft.ImGui.Engine;

internal struct ImGuiWindowStackData
{
	public ImGuiWindow Window;
	public ImGuiLastItemData ParentLastItemDataBackup;
	public int StackSizesColorStack;
	public int StackSizesStyleVarStack;
	public int StackSizesItemFlagsStack;
	public int StackSizesGroupStack;
	public int StackSizesBeginPopupStack;
	public int StackSizesDisabledStack;
	public ImGuiItemFlags BackupItemFlags;
}
