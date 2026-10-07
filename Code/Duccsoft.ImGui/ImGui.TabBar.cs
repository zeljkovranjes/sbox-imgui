using Duccsoft.ImGui.Engine;

namespace Duccsoft.ImGui;

public static partial class ImGui
{
	/// <summary>Internal: tab item without a close button (p_open == null).</summary>
	private const ImGuiTabItemFlags TabItemNoCloseButton = (ImGuiTabItemFlags)(1 << 20);
	private const ImGuiTabItemFlags TabItemSectionMask = ImGuiTabItemFlags.Leading | ImGuiTabItemFlags.Trailing;

	private struct ShrinkWidthItem
	{
		public int Index;
		public float Width;
		public float InitialWidth;
	}

	private struct TabBarSection
	{
		public int TabCount;
		public float Width;
		public float Spacing;
	}

	#region Tab bar
	/// <summary>Create and append into a TabBar.</summary>
	public static bool BeginTabBar( string strId, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( strId );
		if ( !g.TabBars.TryGetValue( id, out var tabBar ) )
		{
			tabBar = new ImGuiTabBar { ID = id };
			g.TabBars[id] = tabBar;
		}
		var tabBarBb = new ImRect( window.DC.CursorPos.x, window.DC.CursorPos.y, window.WorkRect.Max.x, window.DC.CursorPos.y + g.FontSize + g.Style.FramePadding.y * 2 );
		tabBar.ID = id;
		tabBar.SeparatorMinX = tabBarBb.Min.x - ImTrunc( window.WindowPadding.x * 0.5f );
		tabBar.SeparatorMaxX = tabBarBb.Max.x + ImTrunc( window.WindowPadding.x * 0.5f );
		return BeginTabBarEx( tabBar, tabBarBb, flags );
	}

	private static bool BeginTabBarEx( ImGuiTabBar tabBar, ImRect tabBarBb, ImGuiTabBarFlags flags )
	{
		var g = G;
		var window = g.CurrentWindow;

		g.TabBarStack.Add( tabBar );
		PushOverrideID( tabBar.ID );

		tabBar.BackupCursorPos = window.DC.CursorPos;
		if ( tabBar.CurrFrameVisible == g.FrameCount )
		{
			window.DC.CursorPos = new Vector2( tabBar.BarRect.Min.x, tabBar.BarRect.Max.y + tabBar.ItemSpacingY );
			tabBar.BeginCount++;
			return true;
		}

		if ( (flags & ImGuiTabBarFlags.Reorderable) != (tabBar.Flags & ImGuiTabBarFlags.Reorderable) || (tabBar.TabsAddedNew && (flags & ImGuiTabBarFlags.Reorderable) == 0) )
			tabBar.Tabs.Sort( ( a, b ) => a.BeginOrder.CompareTo( b.BeginOrder ) );
		tabBar.TabsAddedNew = false;

		if ( (flags & ImGuiTabBarFlags.FittingPolicyMask_) == 0 )
			flags |= ImGuiTabBarFlags.FittingPolicyDefault_;

		tabBar.Flags = flags;
		tabBar.BarRect = tabBarBb;
		tabBar.WantLayout = true;
		tabBar.PrevFrameVisible = tabBar.CurrFrameVisible;
		tabBar.CurrFrameVisible = g.FrameCount;
		tabBar.PrevTabsContentsHeight = tabBar.CurrTabsContentsHeight;
		tabBar.CurrTabsContentsHeight = 0.0f;
		tabBar.ItemSpacingY = g.Style.ItemSpacing.y;
		tabBar.FramePadding = g.Style.FramePadding;
		tabBar.TabsActiveCount = 0;
		tabBar.LastTabItemIdx = -1;
		tabBar.BeginCount = 1;

		window.DC.CursorPos = new Vector2( tabBar.BarRect.Min.x, tabBar.BarRect.Max.y + tabBar.ItemSpacingY );

		var col = GetColorU32Internal( ImGuiCol.TabSelected );
		if ( g.Style.TabBarBorderSize > 0.0f )
		{
			float y = tabBar.BarRect.Max.y;
			window.DrawList.AddRectFilled( new Vector2( tabBar.SeparatorMinX, y - g.Style.TabBarBorderSize ), new Vector2( tabBar.SeparatorMaxX, y ), col );
		}
		return true;
	}

	/// <summary>Only call EndTabBar() if BeginTabBar() returns true!</summary>
	public static void EndTabBar()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;

		var tabBar = g.CurrentTabBar;
		if ( tabBar is null )
		{
			Log.Warning( "ImGui: EndTabBar() called without a matching BeginTabBar()" );
			return;
		}

		if ( tabBar.WantLayout )
			TabBarLayout( tabBar );

		bool tabBarAppearing = tabBar.PrevFrameVisible + 1 < g.FrameCount;
		if ( tabBar.VisibleTabWasSubmitted || tabBar.VisibleTabId == 0 || tabBarAppearing )
		{
			tabBar.CurrTabsContentsHeight = MathF.Max( window.DC.CursorPos.y - tabBar.BarRect.Max.y, tabBar.CurrTabsContentsHeight );
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x, tabBar.BarRect.Max.y + tabBar.CurrTabsContentsHeight );
		}
		else
		{
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x, tabBar.BarRect.Max.y + tabBar.PrevTabsContentsHeight );
		}
		if ( tabBar.BeginCount > 1 )
			window.DC.CursorPos = tabBar.BackupCursorPos;

		tabBar.LastTabItemIdx = -1;
		PopID();
		g.TabBarStack.RemoveAt( g.TabBarStack.Count - 1 );
	}

	private static int TabItemGetSectionIdx( ImGuiTabItem tab )
		=> (tab.Flags & ImGuiTabItemFlags.Leading) != 0 ? 0 : (tab.Flags & ImGuiTabItemFlags.Trailing) != 0 ? 2 : 1;

	private static ImGuiTabItem TabBarFindTabByID( ImGuiTabBar tabBar, int tabId )
	{
		if ( tabId == 0 )
			return null;
		foreach ( var t in tabBar.Tabs )
			if ( t.ID == tabId )
				return t;
		return null;
	}

	private static float TabBarCalcMaxTabWidth() => G.FontSize * 20.0f;

	private static Vector2 TabItemCalcSize( string label, bool hasCloseButtonOrUnsavedMarker )
	{
		var g = G;
		var labelSize = CalcTextSize( label, true );
		var size = new Vector2( labelSize.x + g.Style.FramePadding.x, labelSize.y + g.Style.FramePadding.y * 2.0f );
		if ( hasCloseButtonOrUnsavedMarker )
			size.x += g.Style.FramePadding.x + (g.Style.ItemInnerSpacing.x + g.FontSize);
		else
			size.x += g.Style.FramePadding.x + 1.0f;
		return new Vector2( MathF.Min( size.x, TabBarCalcMaxTabWidth() ), size.y );
	}

	private static void ShrinkWidths( ShrinkWidthItem[] items, int offset, int count, float widthExcess )
	{
		if ( count <= 0 )
			return;
		if ( count == 1 )
		{
			if ( items[offset].Width >= 0.0f )
				items[offset].Width = MathF.Max( items[offset].Width - widthExcess, 1.0f );
			return;
		}

		Array.Sort( items, offset, count, Comparer<ShrinkWidthItem>.Create( ( a, b ) =>
		{
			int d = b.Width.CompareTo( a.Width );
			return d != 0 ? d : a.Index.CompareTo( b.Index );
		} ) );

		int countSameWidth = 1;
		while ( widthExcess > 0.0f && countSameWidth < count )
		{
			while ( countSameWidth < count && items[offset].Width <= items[offset + countSameWidth].Width )
				countSameWidth++;
			float maxWidthToRemovePerItem = (countSameWidth < count && items[offset + countSameWidth].Width >= 0.0f)
				? items[offset].Width - items[offset + countSameWidth].Width
				: items[offset].Width - 1.0f;
			if ( maxWidthToRemovePerItem <= 0.0f )
				break;
			float widthToRemovePerItem = MathF.Min( widthExcess / countSameWidth, maxWidthToRemovePerItem );
			for ( int n = 0; n < countSameWidth; n++ )
				items[offset + n].Width -= widthToRemovePerItem;
			widthExcess -= widthToRemovePerItem * countSameWidth;
		}

		if ( widthExcess > 0.0f && countSameWidth == count )
		{
			float perItem = widthExcess / count;
			for ( int n = 0; n < count; n++ )
				items[offset + n].Width = MathF.Max( 1.0f, items[offset + n].Width - perItem );
		}

		// Round width and redistribute remainder
		widthExcess = 0.0f;
		for ( int n = 0; n < count; n++ )
		{
			float rounded = ImTrunc( items[offset + n].Width );
			widthExcess += items[offset + n].Width - rounded;
			items[offset + n].Width = rounded;
		}
		for ( int guard = 0; widthExcess > 0.0f && guard < 64; guard++ )
		{
			bool any = false;
			for ( int n = 0; n < count && widthExcess > 0.0f; n++ )
			{
				float add = MathF.Min( items[offset + n].InitialWidth - items[offset + n].Width, 1.0f );
				if ( add <= 0f ) continue;
				items[offset + n].Width += add;
				widthExcess -= add;
				any = true;
			}
			if ( !any ) break;
		}
	}

	private static void TabBarLayout( ImGuiTabBar tabBar )
	{
		var g = G;
		tabBar.WantLayout = false;

		bool scrollToSelectedTab = tabBar.BarRectPrevWidth > tabBar.BarRect.Width;
		tabBar.BarRectPrevWidth = tabBar.BarRect.Width;

		// Garbage collect tabs that were not submitted
		var sections = new TabBarSection[3];
		bool needSortBySection = false;
		for ( int i = tabBar.Tabs.Count - 1; i >= 0; i-- )
		{
			var tab = tabBar.Tabs[i];
			if ( tab.LastFrameVisible < tabBar.PrevFrameVisible || tab.WantClose )
			{
				if ( tabBar.VisibleTabId == tab.ID ) tabBar.VisibleTabId = 0;
				if ( tabBar.SelectedTabId == tab.ID ) tabBar.SelectedTabId = 0;
				if ( tabBar.NextSelectedTabId == tab.ID ) tabBar.NextSelectedTabId = 0;
				tabBar.Tabs.RemoveAt( i );
			}
		}
		for ( int n = 0; n < tabBar.Tabs.Count; n++ )
		{
			var tab = tabBar.Tabs[n];
			tab.IndexDuringLayout = n;
			int sec = TabItemGetSectionIdx( tab );
			if ( n > 0 )
			{
				int prevSec = TabItemGetSectionIdx( tabBar.Tabs[n - 1] );
				if ( sec == 0 && prevSec != 0 ) needSortBySection = true;
				if ( prevSec == 2 && sec != 2 ) needSortBySection = true;
			}
			sections[sec].TabCount++;
		}
		if ( needSortBySection )
		{
			var sorted = tabBar.Tabs.OrderBy( TabItemGetSectionIdx ).ThenBy( t => t.IndexDuringLayout ).ToList();
			tabBar.Tabs.Clear();
			tabBar.Tabs.AddRange( sorted );
		}

		sections[0].Spacing = sections[0].TabCount > 0 && (sections[1].TabCount + sections[2].TabCount) > 0 ? g.Style.ItemInnerSpacing.x : 0.0f;
		sections[1].Spacing = sections[1].TabCount > 0 && sections[2].TabCount > 0 ? g.Style.ItemInnerSpacing.x : 0.0f;

		int scrollToTabId = 0;
		if ( tabBar.NextSelectedTabId != 0 )
		{
			tabBar.SelectedTabId = tabBar.NextSelectedTabId;
			tabBar.NextSelectedTabId = 0;
			scrollToTabId = tabBar.SelectedTabId;
		}

		if ( tabBar.ReorderRequestTabId != 0 )
		{
			if ( TabBarProcessReorder( tabBar ) && tabBar.ReorderRequestTabId == tabBar.SelectedTabId )
				scrollToTabId = tabBar.ReorderRequestTabId;
			tabBar.ReorderRequestTabId = 0;
		}

		if ( (tabBar.Flags & ImGuiTabBarFlags.TabListPopupButton) != 0 )
		{
			var tabToSelect = TabBarTabListPopupButton( tabBar );
			if ( tabToSelect is not null )
				scrollToTabId = tabBar.SelectedTabId = tabToSelect.ID;
		}

		var shrinkBufferIndexes = new int[] { 0, sections[0].TabCount + sections[2].TabCount, sections[0].TabCount };
		var shrinkBuffer = new ShrinkWidthItem[tabBar.Tabs.Count];

		ImGuiTabItem mostRecentlySelectedTab = null;
		int currSectionN = -1;
		bool foundSelectedTabId = false;
		for ( int tabN = 0; tabN < tabBar.Tabs.Count; tabN++ )
		{
			var tab = tabBar.Tabs[tabN];
			if ( (mostRecentlySelectedTab is null || mostRecentlySelectedTab.LastFrameSelected < tab.LastFrameSelected) && (tab.Flags & ImGuiTabItemFlags.Button) == 0 )
				mostRecentlySelectedTab = tab;
			if ( tab.ID == tabBar.SelectedTabId )
				foundSelectedTabId = true;

			bool hasCloseOrMarker = (tab.Flags & TabItemNoCloseButton) == 0 || (tab.Flags & ImGuiTabItemFlags.UnsavedDocument) != 0;
			tab.ContentWidth = tab.RequestedWidth >= 0.0f ? tab.RequestedWidth : TabItemCalcSize( tab.Label ?? "", hasCloseOrMarker ).x;

			int sectionN = TabItemGetSectionIdx( tab );
			sections[sectionN].Width += tab.ContentWidth + (sectionN == currSectionN ? g.Style.ItemInnerSpacing.x : 0.0f);
			currSectionN = sectionN;

			int bi = shrinkBufferIndexes[sectionN]++;
			shrinkBuffer[bi] = new ShrinkWidthItem { Index = tabN, Width = tab.ContentWidth, InitialWidth = tab.ContentWidth };
			tab.Width = MathF.Max( tab.ContentWidth, 1.0f );
		}

		tabBar.WidthAllTabsIdeal = 0.0f;
		for ( int s = 0; s < 3; s++ )
			tabBar.WidthAllTabsIdeal += sections[s].Width + sections[s].Spacing;

		// Horizontal scrolling buttons
		if ( tabBar.WidthAllTabsIdeal > tabBar.BarRect.Width && tabBar.Tabs.Count > 1 && (tabBar.Flags & ImGuiTabBarFlags.NoTabListScrollingButtons) == 0 && (tabBar.Flags & ImGuiTabBarFlags.FittingPolicyScroll) != 0 )
		{
			var scrollAndSelectTab = TabBarScrollingButtons( tabBar );
			if ( scrollAndSelectTab is not null )
			{
				scrollToTabId = scrollAndSelectTab.ID;
				if ( (scrollAndSelectTab.Flags & ImGuiTabItemFlags.Button) == 0 )
					tabBar.SelectedTabId = scrollToTabId;
			}
		}

		// Shrink widths if full tabs don't fit in their allocated space
		float section0W = sections[0].Width + sections[0].Spacing;
		float section1W = sections[1].Width + sections[1].Spacing;
		float section2W = sections[2].Width + sections[2].Spacing;
		bool centralSectionIsVisible = section0W + section2W < tabBar.BarRect.Width;
		float widthExcess = centralSectionIsVisible
			? MathF.Max( section1W - (tabBar.BarRect.Width - section0W - section2W), 0.0f )
			: (section0W + section2W) - tabBar.BarRect.Width;

		if ( widthExcess >= 1.0f && ((tabBar.Flags & ImGuiTabBarFlags.FittingPolicyResizeDown) != 0 || !centralSectionIsVisible) )
		{
			int shrinkDataCount = centralSectionIsVisible ? sections[1].TabCount : sections[0].TabCount + sections[2].TabCount;
			int shrinkDataOffset = centralSectionIsVisible ? sections[0].TabCount + sections[2].TabCount : 0;
			ShrinkWidths( shrinkBuffer, shrinkDataOffset, shrinkDataCount, widthExcess );

			for ( int n = shrinkDataOffset; n < shrinkDataOffset + shrinkDataCount; n++ )
			{
				var tab = tabBar.Tabs[shrinkBuffer[n].Index];
				float shrinkedWidth = ImTrunc( shrinkBuffer[n].Width );
				if ( shrinkedWidth < 0.0f )
					continue;
				shrinkedWidth = MathF.Max( 1.0f, shrinkedWidth );
				int sectionN = TabItemGetSectionIdx( tab );
				sections[sectionN].Width -= tab.Width - shrinkedWidth;
				tab.Width = shrinkedWidth;
			}
		}

		// Layout all active tabs
		int sectionTabIndex = 0;
		float tabOffset = 0.0f;
		tabBar.WidthAllTabs = 0.0f;
		for ( int s = 0; s < 3; s++ )
		{
			var section = sections[s];
			if ( s == 2 )
				tabOffset = MathF.Min( MathF.Max( 0.0f, tabBar.BarRect.Width - section.Width ), tabOffset );
			for ( int tabN = 0; tabN < section.TabCount; tabN++ )
			{
				var tab = tabBar.Tabs[sectionTabIndex + tabN];
				tab.Offset = tabOffset;
				tabOffset += tab.Width + (tabN < section.TabCount - 1 ? g.Style.ItemInnerSpacing.x : 0.0f);
			}
			tabBar.WidthAllTabs += MathF.Max( section.Width + section.Spacing, 0.0f );
			tabOffset += section.Spacing;
			sectionTabIndex += section.TabCount;
		}

		if ( !foundSelectedTabId )
			tabBar.SelectedTabId = 0;
		if ( tabBar.SelectedTabId == 0 && tabBar.NextSelectedTabId == 0 && mostRecentlySelectedTab is not null )
			scrollToTabId = tabBar.SelectedTabId = mostRecentlySelectedTab.ID;

		tabBar.VisibleTabId = tabBar.SelectedTabId;
		tabBar.VisibleTabWasSubmitted = false;

		if ( scrollToTabId != 0 )
			TabBarScrollToTab( tabBar, scrollToTabId, sections );
		else if ( scrollToSelectedTab && tabBar.SelectedTabId != 0 )
			TabBarScrollToTab( tabBar, tabBar.SelectedTabId, sections );

		// Update scrolling
		tabBar.ScrollingAnim = TabBarScrollClamp( tabBar, tabBar.ScrollingAnim );
		tabBar.ScrollingTarget = TabBarScrollClamp( tabBar, tabBar.ScrollingTarget );
		if ( tabBar.ScrollingAnim != tabBar.ScrollingTarget )
		{
			tabBar.ScrollingSpeed = MathF.Max( tabBar.ScrollingSpeed, 70.0f * g.FontSize );
			tabBar.ScrollingSpeed = MathF.Max( tabBar.ScrollingSpeed, MathF.Abs( tabBar.ScrollingTarget - tabBar.ScrollingAnim ) / 0.3f );
			bool teleport = tabBar.PrevFrameVisible + 1 < g.FrameCount || tabBar.ScrollingTargetDistToVisibility > 10.0f * g.FontSize;
			tabBar.ScrollingAnim = teleport ? tabBar.ScrollingTarget : ImLinearSweep( tabBar.ScrollingAnim, tabBar.ScrollingTarget, g.IO.DeltaTime * tabBar.ScrollingSpeed );
		}
		else
		{
			tabBar.ScrollingSpeed = 0.0f;
		}
		tabBar.ScrollingRectMinX = tabBar.BarRect.Min.x + sections[0].Width + sections[0].Spacing;
		tabBar.ScrollingRectMaxX = tabBar.BarRect.Max.x - sections[2].Width - sections[1].Spacing;

		// Actual layout in host window
		var window = g.CurrentWindow;
		window.DC.CursorPos = tabBar.BarRect.Min;
		ItemSize( new Vector2( tabBar.WidthAllTabs, tabBar.BarRect.Height ), tabBar.FramePadding.y );
		window.DC.IdealMaxPos = new Vector2( MathF.Max( window.DC.IdealMaxPos.x, tabBar.BarRect.Min.x + tabBar.WidthAllTabsIdeal ), window.DC.IdealMaxPos.y );
	}

	private static float TabBarScrollClamp( ImGuiTabBar tabBar, float scrolling )
	{
		scrolling = MathF.Min( scrolling, tabBar.WidthAllTabs - tabBar.BarRect.Width );
		return MathF.Max( scrolling, 0.0f );
	}

	private static void TabBarScrollToTab( ImGuiTabBar tabBar, int tabId, TabBarSection[] sections )
	{
		var g = G;
		var tab = TabBarFindTabByID( tabBar, tabId );
		if ( tab is null || (tab.Flags & TabItemSectionMask) != 0 )
			return;

		float margin = g.FontSize * 1.0f;
		int order = tabBar.Tabs.IndexOf( tab );
		float scrollableWidth = tabBar.BarRect.Width - sections[0].Width - sections[2].Width - sections[1].Spacing;

		float tabX1 = tab.Offset - sections[0].Width + (order > sections[0].TabCount - 1 ? -margin : 0.0f);
		float tabX2 = tab.Offset - sections[0].Width + tab.Width + (order + 1 < tabBar.Tabs.Count - sections[2].TabCount ? margin : 1.0f);
		tabBar.ScrollingTargetDistToVisibility = 0.0f;
		if ( tabBar.ScrollingTarget > tabX1 || tabX2 - tabX1 >= scrollableWidth )
		{
			tabBar.ScrollingTargetDistToVisibility = MathF.Max( tabBar.ScrollingAnim - tabX2, 0.0f );
			tabBar.ScrollingTarget = tabX1;
		}
		else if ( tabBar.ScrollingTarget < tabX2 - scrollableWidth )
		{
			tabBar.ScrollingTargetDistToVisibility = MathF.Max( (tabX1 - scrollableWidth) - tabBar.ScrollingAnim, 0.0f );
			tabBar.ScrollingTarget = tabX2 - scrollableWidth;
		}
	}

	private static void TabBarQueueReorderFromMousePos( ImGuiTabBar tabBar, ImGuiTabItem srcTab, Vector2 mousePos )
	{
		var g = G;
		if ( (tabBar.Flags & ImGuiTabBarFlags.Reorderable) == 0 )
			return;

		bool isCentralSection = (srcTab.Flags & TabItemSectionMask) == 0;
		float barOffset = tabBar.BarRect.Min.x - (isCentralSection ? tabBar.ScrollingTarget : 0);
		int dir = barOffset + srcTab.Offset > mousePos.x ? -1 : +1;
		int srcIdx = tabBar.Tabs.IndexOf( srcTab );
		int dstIdx = srcIdx;
		for ( int i = srcIdx; i >= 0 && i < tabBar.Tabs.Count; i += dir )
		{
			var dstTab = tabBar.Tabs[i];
			if ( (dstTab.Flags & ImGuiTabItemFlags.NoReorder) != 0 )
				break;
			if ( (dstTab.Flags & TabItemSectionMask) != (srcTab.Flags & TabItemSectionMask) )
				break;
			dstIdx = i;
			float x1 = barOffset + dstTab.Offset - g.Style.ItemInnerSpacing.x;
			float x2 = barOffset + dstTab.Offset + dstTab.Width + g.Style.ItemInnerSpacing.x;
			if ( (dir < 0 && mousePos.x > x1) || (dir > 0 && mousePos.x < x2) )
				break;
		}

		if ( dstIdx != srcIdx )
		{
			tabBar.ReorderRequestTabId = srcTab.ID;
			tabBar.ReorderRequestOffset = dstIdx - srcIdx;
		}
	}

	private static bool TabBarProcessReorder( ImGuiTabBar tabBar )
	{
		var tab1 = TabBarFindTabByID( tabBar, tabBar.ReorderRequestTabId );
		if ( tab1 is null || (tab1.Flags & ImGuiTabItemFlags.NoReorder) != 0 )
			return false;
		int idx1 = tabBar.Tabs.IndexOf( tab1 );
		int tab2Order = idx1 + tabBar.ReorderRequestOffset;
		if ( tab2Order < 0 || tab2Order >= tabBar.Tabs.Count )
			return false;
		var tab2 = tabBar.Tabs[tab2Order];
		if ( (tab2.Flags & ImGuiTabItemFlags.NoReorder) != 0 )
			return false;
		if ( (tab1.Flags & TabItemSectionMask) != (tab2.Flags & TabItemSectionMask) )
			return false;
		tabBar.Tabs.RemoveAt( idx1 );
		tabBar.Tabs.Insert( tab2Order, tab1 );
		return true;
	}

	private static ImGuiTabItem TabBarScrollingButtons( ImGuiTabBar tabBar )
	{
		var g = G;
		var window = g.CurrentWindow;

		var arrowButtonSize = new Vector2( g.FontSize - 2.0f, g.FontSize + g.Style.FramePadding.y * 2.0f );
		float scrollingButtonsWidth = arrowButtonSize.x * 2.0f;
		var backupCursorPos = window.DC.CursorPos;

		int selectDir = 0;
		var arrowCol = g.Style.Colors[(int)ImGuiCol.Text];
		arrowCol.w *= 0.5f;

		PushStyleColor( ImGuiCol.Text, arrowCol );
		PushStyleColor( ImGuiCol.Button, new Vector4( 0, 0, 0, 0 ) );
		PushItemFlag( ImGuiItemFlags.ButtonRepeat | ImGuiItemFlags.NoNav, true );
		float backupRepeatDelay = g.IO.KeyRepeatDelay;
		float backupRepeatRate = g.IO.KeyRepeatRate;
		g.IO.KeyRepeatDelay = 0.250f;
		g.IO.KeyRepeatRate = 0.200f;
		float x = MathF.Max( tabBar.BarRect.Min.x, tabBar.BarRect.Max.x - scrollingButtonsWidth );
		window.DC.CursorPos = new Vector2( x, tabBar.BarRect.Min.y );
		if ( ArrowButtonEx( "##<", ImGuiDir.Left, arrowButtonSize, ImGuiButtonFlags.PressedOnClick ) )
			selectDir = -1;
		window.DC.CursorPos = new Vector2( x + arrowButtonSize.x, tabBar.BarRect.Min.y );
		if ( ArrowButtonEx( "##>", ImGuiDir.Right, arrowButtonSize, ImGuiButtonFlags.PressedOnClick ) )
			selectDir = +1;
		PopItemFlag();
		PopStyleColor( 2 );
		g.IO.KeyRepeatRate = backupRepeatRate;
		g.IO.KeyRepeatDelay = backupRepeatDelay;

		ImGuiTabItem tabToScrollTo = null;
		if ( selectDir != 0 )
		{
			var tabItem = TabBarFindTabByID( tabBar, tabBar.SelectedTabId );
			if ( tabItem is not null )
			{
				int selectedOrder = tabBar.Tabs.IndexOf( tabItem );
				int targetOrder = selectedOrder + selectDir;
				for ( int guard = 0; tabToScrollTo is null && guard < tabBar.Tabs.Count + 2; guard++ )
				{
					tabToScrollTo = tabBar.Tabs[(targetOrder >= 0 && targetOrder < tabBar.Tabs.Count) ? targetOrder : Math.Clamp( selectedOrder, 0, tabBar.Tabs.Count - 1 )];
					if ( (tabToScrollTo.Flags & ImGuiTabItemFlags.Button) != 0 )
					{
						targetOrder += selectDir;
						selectedOrder += selectDir;
						tabToScrollTo = (targetOrder < 0 || targetOrder >= tabBar.Tabs.Count) ? tabToScrollTo : null;
					}
				}
			}
		}
		window.DC.CursorPos = backupCursorPos;
		tabBar.BarRect.Max.x -= scrollingButtonsWidth + 1.0f;
		return tabToScrollTo;
	}

	private static ImGuiTabItem TabBarTabListPopupButton( ImGuiTabBar tabBar )
	{
		var g = G;
		var window = g.CurrentWindow;

		float tabListPopupButtonWidth = g.FontSize + g.Style.FramePadding.y;
		var backupCursorPos = window.DC.CursorPos;
		window.DC.CursorPos = new Vector2( tabBar.BarRect.Min.x - g.Style.FramePadding.y, tabBar.BarRect.Min.y );
		tabBar.BarRect.Min.x += tabListPopupButtonWidth;

		var arrowCol = g.Style.Colors[(int)ImGuiCol.Text];
		arrowCol.w *= 0.5f;
		PushStyleColor( ImGuiCol.Text, arrowCol );
		PushStyleColor( ImGuiCol.Button, new Vector4( 0, 0, 0, 0 ) );
		bool open = BeginCombo( "##v", null, ImGuiComboFlags.NoPreview | ImGuiComboFlags.HeightLargest );
		PopStyleColor( 2 );

		ImGuiTabItem tabToSelect = null;
		if ( open )
		{
			foreach ( var tab in tabBar.Tabs )
			{
				if ( (tab.Flags & ImGuiTabItemFlags.Button) != 0 )
					continue;
				PushID( tab.ID );
				if ( Selectable( LabelText( tab.Label ?? "" ), tabBar.SelectedTabId == tab.ID ) )
					tabToSelect = tab;
				PopID();
			}
			EndCombo();
		}

		window.DC.CursorPos = backupCursorPos;
		return tabToSelect;
	}
	#endregion

	#region Tab items
	/// <summary>Create a Tab. Returns true if the Tab is selected.</summary>
	public static bool BeginTabItem( string label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None )
	{
		bool dummy = true;
		return BeginTabItemImpl( label, false, ref dummy, flags );
	}

	/// <summary>Create a Tab with a close button. When closed, <paramref name="open"/> is set to false.</summary>
	public static bool BeginTabItem( string label, ref bool open, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None )
		=> BeginTabItemImpl( label, true, ref open, flags );

	private static bool BeginTabItemImpl( string label, bool hasOpen, ref bool open, ImGuiTabItemFlags flags )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return false;

		var tabBar = g.CurrentTabBar;
		if ( tabBar is null )
		{
			Log.Warning( "ImGui: BeginTabItem() needs to be called between BeginTabBar() and EndTabBar()" );
			return false;
		}
		if ( (flags & ImGuiTabItemFlags.Button) != 0 )
			flags &= ~ImGuiTabItemFlags.Button;

		bool ret = TabItemEx( tabBar, label, hasOpen, ref open, flags );
		if ( ret && (flags & ImGuiTabItemFlags.NoPushId) == 0 )
		{
			var tab = tabBar.Tabs[tabBar.LastTabItemIdx];
			PushOverrideID( tab.ID );
		}
		return ret;
	}

	/// <summary>Only call EndTabItem() if BeginTabItem() returns true!</summary>
	public static void EndTabItem()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;
		var tabBar = g.CurrentTabBar;
		if ( tabBar is null || tabBar.LastTabItemIdx < 0 || tabBar.LastTabItemIdx >= tabBar.Tabs.Count )
		{
			Log.Warning( "ImGui: EndTabItem() needs to be called between BeginTabBar() and EndTabBar()" );
			return;
		}
		var tab = tabBar.Tabs[tabBar.LastTabItemIdx];
		if ( (tab.Flags & ImGuiTabItemFlags.NoPushId) == 0 )
			PopID();
	}

	/// <summary>Create a Tab behaving like a button. Return true when clicked. Cannot be selected in the tab bar.</summary>
	public static bool TabItemButton( string label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return false;
		var tabBar = g.CurrentTabBar;
		if ( tabBar is null )
		{
			Log.Warning( "ImGui: TabItemButton() needs to be called between BeginTabBar() and EndTabBar()" );
			return false;
		}
		bool dummy = true;
		return TabItemEx( tabBar, label, false, ref dummy, flags | ImGuiTabItemFlags.Button | ImGuiTabItemFlags.NoReorder );
	}

	/// <summary>Notify TabBar or Docking system of a closed tab/window ahead (useful to reduce visual flicker on reorderable tab bars).</summary>
	public static void SetTabItemClosed( string tabOrDockedWindowLabel )
	{
		var g = G;
		var tabBar = g.CurrentTabBar;
		if ( tabBar is null )
			return;
		int tabId = g.CurrentWindow.GetID( tabOrDockedWindowLabel );
		var tab = TabBarFindTabByID( tabBar, tabId );
		if ( tab is not null )
			tab.WantClose = true;
	}

	private static void TabBarCloseTab( ImGuiTabBar tabBar, ImGuiTabItem tab )
	{
		if ( (tab.Flags & ImGuiTabItemFlags.Button) != 0 )
			return;
		if ( (tab.Flags & (ImGuiTabItemFlags.UnsavedDocument | ImGuiTabItemFlags.NoAssumedClosure)) == 0 )
		{
			tab.WantClose = true;
			if ( tabBar.VisibleTabId == tab.ID )
			{
				tab.LastFrameVisible = -1;
				tabBar.SelectedTabId = tabBar.NextSelectedTabId = 0;
			}
		}
		else
		{
			if ( tabBar.VisibleTabId != tab.ID )
				tabBar.NextSelectedTabId = tab.ID;
		}
	}

	private static bool TabItemEx( ImGuiTabBar tabBar, string label, bool hasOpen, ref bool open, ImGuiTabItemFlags flags )
	{
		var g = G;
		var window = g.CurrentWindow;

		if ( tabBar.WantLayout )
		{
			var backupNextItemData = g.NextItemData;
			TabBarLayout( tabBar );
			g.NextItemData = backupNextItemData;
		}
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );

		if ( hasOpen && !open )
		{
			ItemAdd( new ImRect(), id, null, ImGuiItemFlags.NoNav );
			return false;
		}

		if ( !hasOpen )
			flags |= TabItemNoCloseButton;

		var tab = TabBarFindTabByID( tabBar, id );
		bool tabIsNew = false;
		if ( tab is null )
		{
			tab = new ImGuiTabItem { ID = id };
			tabBar.Tabs.Add( tab );
			tabBar.TabsAddedNew = tabIsNew = true;
		}
		tabBar.LastTabItemIdx = tabBar.Tabs.IndexOf( tab );

		var size = TabItemCalcSize( label, hasOpen || (flags & ImGuiTabItemFlags.UnsavedDocument) != 0 );
		tab.RequestedWidth = -1.0f;
		if ( (g.NextItemData.Flags & ImGuiNextItemDataFlags.HasWidth) != 0 )
			size.x = tab.RequestedWidth = g.NextItemData.Width;
		if ( tabIsNew )
			tab.Width = MathF.Max( 1.0f, size.x );
		tab.ContentWidth = size.x;
		tab.BeginOrder = tabBar.TabsActiveCount++;

		bool tabBarAppearing = tabBar.PrevFrameVisible + 1 < g.FrameCount;
		bool tabAppearing = tab.LastFrameVisible + 1 < g.FrameCount;
		bool tabJustUnsaved = (flags & ImGuiTabItemFlags.UnsavedDocument) != 0 && (tab.Flags & ImGuiTabItemFlags.UnsavedDocument) == 0;
		bool isTabButton = (flags & ImGuiTabItemFlags.Button) != 0;
		tab.LastFrameVisible = g.FrameCount;
		tab.Flags = flags;
		tab.Label = label;

		if ( !isTabButton )
		{
			if ( tabAppearing && (tabBar.Flags & ImGuiTabBarFlags.AutoSelectNewTabs) != 0 && tabBar.NextSelectedTabId == 0 )
				if ( !tabBarAppearing || tabBar.SelectedTabId == 0 )
					tabBar.NextSelectedTabId = id;
			if ( (flags & ImGuiTabItemFlags.SetSelected) != 0 && tabBar.SelectedTabId != id )
				tabBar.NextSelectedTabId = id;
		}

		bool tabContentsVisible = tabBar.VisibleTabId == id;
		if ( tabContentsVisible )
			tabBar.VisibleTabWasSubmitted = true;

		if ( !tabContentsVisible && tabBar.SelectedTabId == 0 && tabBarAppearing )
			if ( tabBar.Tabs.Count == 1 && (tabBar.Flags & ImGuiTabBarFlags.AutoSelectNewTabs) == 0 )
				tabContentsVisible = true;

		if ( tabAppearing && (!tabBarAppearing || tabIsNew) )
		{
			ItemAdd( new ImRect(), id, null, ImGuiItemFlags.NoNav );
			if ( isTabButton )
				return false;
			return tabContentsVisible;
		}

		if ( tabBar.SelectedTabId == id )
			tab.LastFrameSelected = g.FrameCount;

		var backupMainCursorPos = window.DC.CursorPos;

		bool isCentralSection = (tab.Flags & TabItemSectionMask) == 0;
		size.x = tab.Width;
		if ( isCentralSection )
			window.DC.CursorPos = tabBar.BarRect.Min + new Vector2( ImTrunc( tab.Offset - tabBar.ScrollingAnim ), 0.0f );
		else
			window.DC.CursorPos = tabBar.BarRect.Min + new Vector2( tab.Offset, 0.0f );
		var pos = window.DC.CursorPos;
		var bb = new ImRect( pos, pos + size );

		bool wantClipRect = isCentralSection && (bb.Min.x < tabBar.ScrollingRectMinX || bb.Max.x > tabBar.ScrollingRectMaxX);
		if ( wantClipRect )
			PushClipRect( new Vector2( MathF.Max( bb.Min.x, tabBar.ScrollingRectMinX ), bb.Min.y - 1 ), new Vector2( tabBar.ScrollingRectMaxX, bb.Max.y ), true );

		var backupCursorMaxPos = window.DC.CursorMaxPos;
		ItemSize( bb.Size, style.FramePadding.y );
		window.DC.CursorMaxPos = backupCursorMaxPos;

		if ( !ItemAdd( bb, id ) )
		{
			if ( wantClipRect )
				PopClipRect();
			window.DC.CursorPos = backupMainCursorPos;
			return tabContentsVisible;
		}

		int closeButtonId = hasOpen ? ImHashStr( "#CLOSE", id ) : 0;

		var buttonFlags = (isTabButton ? ImGuiButtonFlags.PressedOnClickRelease : ImGuiButtonFlags.PressedOnClick) | ImGuiButtonFlags.AllowOverlap;
		if ( g.DragDropActive )
			buttonFlags |= ImGuiButtonFlags.PressedOnDragDropHold;

		bool hovered = false, held = false, pressed = false;
		// Let the close button (submitted after us) win the hover when it was hovered last frame.
		bool closeButtonOnTop = closeButtonId != 0 && (g.HoveredIdPreviousFrame == closeButtonId || g.ActiveId == closeButtonId) && g.ActiveId != id;
		if ( !closeButtonOnTop )
			pressed = ButtonBehavior( bb, id, out hovered, out held, buttonFlags );
		if ( pressed && !isTabButton )
			tabBar.NextSelectedTabId = id;

		// Drag to reorder
		if ( held && !tabAppearing && IsMouseDragging( ImGuiMouseButton.Left ) )
		{
			if ( !g.DragDropActive && (tabBar.Flags & ImGuiTabBarFlags.Reorderable) != 0 )
			{
				if ( g.IO.MouseDelta.x < 0.0f && g.IO.MousePos.x < bb.Min.x )
					TabBarQueueReorderFromMousePos( tabBar, tab, g.IO.MousePos );
				else if ( g.IO.MouseDelta.x > 0.0f && g.IO.MousePos.x > bb.Max.x )
					TabBarQueueReorderFromMousePos( tabBar, tab, g.IO.MousePos );
			}
		}

		// Render tab shape
		var displayDrawList = window.DrawList;
		bool isHoveredAny = hovered || held || g.HoveredId == closeButtonId && closeButtonId != 0;
		var tabCol = GetColorU32Internal( (held || isHoveredAny) ? ImGuiCol.TabHovered : tabContentsVisible ? ImGuiCol.TabSelected : ImGuiCol.Tab );
		TabItemBackground( displayDrawList, bb, flags, tabCol );
		if ( tabContentsVisible && (tabBar.Flags & ImGuiTabBarFlags.DrawSelectedOverline) != 0 && style.TabBarOverlineSize > 0.0f )
		{
			var overlineCol = GetColorU32Internal( ImGuiCol.TabSelectedOverline );
			displayDrawList.AddRectFilled( bb.TL, new Vector2( bb.Max.x, bb.Min.y + style.TabBarOverlineSize ), overlineCol, style.TabRounding, ImDrawFlags.RoundCornersTop );
		}

		// Select with right mouse button (common idiom for context menus)
		bool hoveredUnblocked = IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup );
		if ( tabBar.SelectedTabId != tab.ID && hoveredUnblocked && (IsMouseClicked( ImGuiMouseButton.Right ) || IsMouseReleased( ImGuiMouseButton.Right )) && !isTabButton )
			tabBar.NextSelectedTabId = id;

		if ( (tabBar.Flags & ImGuiTabBarFlags.NoCloseWithMiddleMouseButton) != 0 )
			flags |= ImGuiTabItemFlags.NoCloseWithMiddleMouseButton;

		TabItemLabelAndCloseButton( displayDrawList, bb, tabJustUnsaved ? (flags & ~ImGuiTabItemFlags.UnsavedDocument) : flags, tabBar.FramePadding, label, id, closeButtonId, tabContentsVisible, out bool justClosed, out bool textClipped );
		if ( justClosed && hasOpen )
		{
			open = false;
			TabBarCloseTab( tabBar, tab );
		}

		if ( wantClipRect )
			PopClipRect();
		window.DC.CursorPos = backupMainCursorPos;

		// Tooltip with full label when truncated
		if ( textClipped && g.HoveredId == id && !held )
			if ( (tabBar.Flags & ImGuiTabBarFlags.NoTooltip) == 0 && (tab.Flags & ImGuiTabItemFlags.NoTooltip) == 0 )
				SetItemTooltip( "{0}", LabelText( label ) );

		if ( isTabButton )
			return pressed;
		return tabContentsVisible;
	}

	private static void TabItemBackground( ImDrawList drawList, ImRect bb, ImGuiTabItemFlags flags, Color32 col )
	{
		var g = G;
		float width = bb.Width;
		float rounding = MathF.Max( 0.0f, MathF.Min( (flags & ImGuiTabItemFlags.Button) != 0 ? g.Style.FrameRounding : g.Style.TabRounding, width * 0.5f - 1.0f ) );
		float y1 = bb.Min.y + 1.0f;
		float y2 = bb.Max.y - g.Style.TabBarBorderSize;
		drawList.AddRectFilled( new Vector2( bb.Min.x, y1 ), new Vector2( bb.Max.x, y2 ), col, rounding, ImDrawFlags.RoundCornersTop );
		if ( g.Style.TabBorderSize > 0.0f )
			drawList.AddRect( new Vector2( bb.Min.x, y1 ), new Vector2( bb.Max.x, y2 ), GetColorU32Internal( ImGuiCol.Border ), rounding, ImDrawFlags.RoundCornersTop, g.Style.TabBorderSize );
	}

	private static void TabItemLabelAndCloseButton( ImDrawList drawList, ImRect bb, ImGuiTabItemFlags flags, Vector2 framePadding, string label, int tabId, int closeButtonId, bool isContentsVisible, out bool outJustClosed, out bool outTextClipped )
	{
		var g = G;
		var labelSize = CalcTextSize( label, true );
		outJustClosed = false;
		outTextClipped = false;
		if ( bb.Width <= 1.0f )
			return;

		var textEllipsisClipBb = new ImRect( bb.Min.x + framePadding.x, bb.Min.y + framePadding.y, bb.Max.x - framePadding.x, bb.Max.y );
		outTextClipped = textEllipsisClipBb.Min.x + labelSize.x > textEllipsisClipBb.Max.x;

		float buttonSz = g.FontSize;
		var buttonPos = new Vector2( MathF.Max( bb.Min.x, bb.Max.x - framePadding.x - buttonSz ), bb.Min.y + framePadding.y );

		bool closeButtonPressed = false;
		bool closeButtonVisible = false;
		bool isHovered = g.HoveredId == tabId || (closeButtonId != 0 && g.HoveredId == closeButtonId) || g.ActiveId == tabId || (closeButtonId != 0 && g.ActiveId == closeButtonId);
		if ( closeButtonId != 0 )
		{
			if ( isContentsVisible )
				closeButtonVisible = g.Style.TabCloseButtonMinWidthSelected < 0.0f || (isHovered && bb.Width >= MathF.Max( buttonSz, g.Style.TabCloseButtonMinWidthSelected ));
			else
				closeButtonVisible = g.Style.TabCloseButtonMinWidthUnselected < 0.0f || (isHovered && bb.Width >= MathF.Max( buttonSz, g.Style.TabCloseButtonMinWidthUnselected ));
		}

		bool unsavedMarkerVisible = (flags & ImGuiTabItemFlags.UnsavedDocument) != 0 && buttonPos.x + buttonSz <= bb.Max.x && (!closeButtonVisible || !isHovered);
		if ( unsavedMarkerVisible )
		{
			var bulletBb = new ImRect( buttonPos, buttonPos + new Vector2( buttonSz, buttonSz ) );
			RenderBullet( drawList, bulletBb.Center, GetColorU32Internal( ImGuiCol.Text ) );
		}
		else if ( closeButtonVisible )
		{
			var lastItemBackup = g.LastItemData;
			if ( CloseButton( closeButtonId, buttonPos ) )
				closeButtonPressed = true;
			g.LastItemData = lastItemBackup;

			if ( isHovered && (flags & ImGuiTabItemFlags.NoCloseWithMiddleMouseButton) == 0 && IsMouseClicked( ImGuiMouseButton.Middle ) )
				closeButtonPressed = true;
		}

		if ( closeButtonVisible || unsavedMarkerVisible )
			textEllipsisClipBb.Max.x -= buttonSz * (unsavedMarkerVisible ? 0.80f : 1.0f);
		float ellipsisMaxX = textEllipsisClipBb.Max.x;

		RenderTextEllipsis( drawList, textEllipsisClipBb.Min, textEllipsisClipBb.Max, textEllipsisClipBb.Max.x, ellipsisMaxX, label, labelSize );

		outJustClosed = closeButtonPressed;
	}
	#endregion
}
