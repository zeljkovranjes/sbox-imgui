namespace Duccsoft.ImGui.Engine;

internal class ImGuiTabBar
{
	public int ID;
	public ImGuiTabBarFlags Flags;
	public readonly List<ImGuiTabItem> Tabs = new();
	public int SelectedTabId;
	public int NextSelectedTabId;
	public int VisibleTabId;
	public int CurrFrameVisible = -1;
	public int PrevFrameVisible = -1;
	public ImRect BarRect;
	public float BarRectPrevWidth;
	public float CurrTabsContentsHeight;
	public float PrevTabsContentsHeight;
	public float WidthAllTabs;
	public float WidthAllTabsIdeal;
	public float ScrollingAnim;
	public float ScrollingTarget;
	public float ScrollingTargetDistToVisibility;
	public float ScrollingSpeed;
	public float ScrollingRectMinX;
	public float ScrollingRectMaxX;
	public int ReorderRequestTabId;
	public int ReorderRequestOffset;
	public int BeginCount;
	public bool WantLayout;
	public bool VisibleTabWasSubmitted;
	public bool TabsAddedNew;
	public int TabsActiveCount;
	public int LastTabItemIdx = -1;
	public float ItemSpacingY;
	public Vector2 FramePadding;
	public Vector2 BackupCursorPos;
	public float SeparatorMinX, SeparatorMaxX;
}
