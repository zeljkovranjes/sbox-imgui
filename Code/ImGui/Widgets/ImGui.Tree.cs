namespace Duccsoft.ImGui;

public static partial class ImGui
{
	#region Tree nodes
	public static bool TreeNode( string label ) => TreeNodeEx( label, ImGuiTreeNodeFlags.None );

	/// <summary>Tree node with a separate id and formatted label.</summary>
	public static bool TreeNode( string strId, string fmt, params object[] args )
		=> TreeNodeBehavior( G.CurrentWindow.GetID( strId ), ImGuiTreeNodeFlags.None, Format( fmt, args ) );

	public static bool TreeNodeEx( string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None )
	{
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		return TreeNodeBehavior( window.GetID( label ), flags, label );
	}

	public static bool TreeNodeEx( string strId, ImGuiTreeNodeFlags flags, string fmt, params object[] args )
	{
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		return TreeNodeBehavior( window.GetID( strId ), flags, Format( fmt, args ) );
	}

	/// <summary>Set the open/collapsed state of the next TreeNode/CollapsingHeader/TabItem.</summary>
	public static void SetNextItemOpen( bool isOpen, ImGuiCond cond = ImGuiCond.None )
	{
		var g = G;
		if ( g.CurrentWindow.SkipItems )
			return;
		g.NextItemData.Flags |= ImGuiNextItemDataFlags.HasOpen;
		g.NextItemData.OpenVal = isOpen;
		g.NextItemData.OpenCond = cond != ImGuiCond.None ? cond : ImGuiCond.Always;
	}

	internal static bool TreeNodeUpdateNextOpen( int id, ImGuiTreeNodeFlags flags )
	{
		if ( (flags & ImGuiTreeNodeFlags.Leaf) != 0 )
			return true;

		var g = G;
		var window = g.CurrentWindow;
		var storage = window.DC.StateStorage;

		bool isOpen;
		if ( (g.NextItemData.Flags & ImGuiNextItemDataFlags.HasOpen) != 0 )
		{
			if ( (g.NextItemData.OpenCond & ImGuiCond.Always) != 0 )
			{
				isOpen = g.NextItemData.OpenVal;
				storage.SetInt( id, isOpen ? 1 : 0 );
			}
			else
			{
				int storedValue = storage.GetInt( id, -1 );
				if ( storedValue == -1 )
				{
					isOpen = g.NextItemData.OpenVal;
					storage.SetInt( id, isOpen ? 1 : 0 );
				}
				else
				{
					isOpen = storedValue != 0;
				}
			}
		}
		else
		{
			isOpen = storage.GetInt( id, (flags & ImGuiTreeNodeFlags.DefaultOpen) != 0 ? 1 : 0 ) != 0;
		}
		return isOpen;
	}

	internal static bool TreeNodeBehavior( int id, ImGuiTreeNodeFlags flags, string label )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		bool displayFrame = (flags & ImGuiTreeNodeFlags.Framed) != 0;
		var padding = displayFrame || (flags & ImGuiTreeNodeFlags.FramePadding) != 0 ? style.FramePadding : new Vector2( style.FramePadding.x, MathF.Min( window.DC.CurrLineTextBaseOffset, style.FramePadding.y ) );

		string labelText = LabelText( label );
		var labelSize = CalcTextSize( labelText, false );

		float textOffsetX = g.FontSize + (displayFrame ? padding.x * 3 : padding.x * 2);
		float textOffsetY = MathF.Max( padding.y, window.DC.CurrLineTextBaseOffset );
		float textWidth = g.FontSize + labelSize.x + padding.x * 2;

		float frameHeight = MathF.Max( MathF.Min( window.DC.CurrLineSize.y, g.FontSize + style.FramePadding.y * 2 ), labelSize.y + padding.y * 2 );
		bool spanAllColumns = (flags & ImGuiTreeNodeFlags.SpanAllColumns) != 0 && window.DC.CurrentTableIdx >= 0;
		var frameBb = new ImRect(
			(flags & (ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.SpanAllColumns)) != 0 ? window.WorkRect.Min.x : window.DC.CursorPos.x,
			window.DC.CursorPos.y,
			window.WorkRect.Max.x,
			window.DC.CursorPos.y + frameHeight );
		if ( spanAllColumns )
		{
			var table = CurrentTable;
			if ( table is not null )
			{
				frameBb.Min.x = table.WorkRect.Min.x;
				frameBb.Max.x = table.WorkRect.Max.x;
			}
		}
		if ( displayFrame )
		{
			float outerExtend = ImTrunc( window.WindowPadding.x * 0.5f );
			frameBb.Min.x -= outerExtend;
			frameBb.Max.x += outerExtend;
		}

		var textPos = new Vector2( window.DC.CursorPos.x + textOffsetX, window.DC.CursorPos.y + textOffsetY );
		ItemSize( new Vector2( textWidth, frameHeight ), padding.y );

		var interactBb = frameBb;
		if ( (flags & (ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.SpanAllColumns)) == 0 )
			interactBb.Max.x = frameBb.Min.x + textWidth + (labelSize.x > 0.0f ? style.ItemSpacing.x * 2.0f : 0.0f);
		if ( (flags & ImGuiTreeNodeFlags.SpanTextWidth) != 0 )
			interactBb.Max.x = frameBb.Min.x + textWidth;

		bool isLeaf = (flags & ImGuiTreeNodeFlags.Leaf) != 0;
		bool isOpen = TreeNodeUpdateNextOpen( id, flags );

		bool itemAdd = ItemAdd( interactBb, id );
		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HasDisplayRect;
		g.LastItemData.DisplayRect = frameBb;

		if ( !itemAdd )
		{
			if ( isOpen && (flags & ImGuiTreeNodeFlags.NoTreePushOnOpen) == 0 )
				TreePushOverrideID( id );
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Openable | (isOpen ? ImGuiItemStatusFlags.Opened : ImGuiItemStatusFlags.None);
			return isOpen;
		}

		var buttonFlags = ImGuiButtonFlags.None;
		if ( (flags & ImGuiTreeNodeFlags.AllowOverlap) != 0 )
			buttonFlags |= ImGuiButtonFlags.AllowOverlap;
		if ( !isLeaf )
			buttonFlags |= ImGuiButtonFlags.PressedOnDragDropHold;

		float arrowHitX1 = textPos.x - textOffsetX - style.TouchExtraPadding.x;
		float arrowHitX2 = textPos.x - textOffsetX + (g.FontSize + padding.x * 2.0f) + style.TouchExtraPadding.x;
		bool isMouseXOverArrow = g.IO.MousePos.x >= arrowHitX1 && g.IO.MousePos.x < arrowHitX2;

		if ( (flags & (ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick)) != 0 )
			buttonFlags |= isMouseXOverArrow ? ImGuiButtonFlags.PressedOnClick : ImGuiButtonFlags.PressedOnClickRelease;
		else
			buttonFlags |= ImGuiButtonFlags.PressedOnClickRelease;
		if ( (flags & ImGuiTreeNodeFlags.OpenOnDoubleClick) != 0 )
			buttonFlags |= ImGuiButtonFlags.PressedOnDoubleClick;

		bool selected = (flags & ImGuiTreeNodeFlags.Selected) != 0;
		bool wasSelected = selected;

		bool pressed = ButtonBehavior( interactBb, id, out bool hovered, out bool held, buttonFlags );
		bool toggled = false;
		if ( !isLeaf )
		{
			if ( pressed && g.DragDropHoldJustPressedId != id )
			{
				if ( (flags & (ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick)) == 0 )
					toggled = true;
				if ( (flags & ImGuiTreeNodeFlags.OpenOnArrow) != 0 )
					toggled |= isMouseXOverArrow;
				if ( (flags & ImGuiTreeNodeFlags.OpenOnDoubleClick) != 0 && g.IO.MouseClickedCount[0] == 2 )
					toggled = true;
			}
			else if ( pressed && g.DragDropHoldJustPressedId == id )
			{
				if ( !isOpen )
					toggled = true;
			}

			if ( toggled )
			{
				isOpen = !isOpen;
				window.DC.StateStorage.SetInt( id, isOpen ? 1 : 0 );
				g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.ToggledOpen;
			}
		}

		if ( selected != wasSelected )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.ToggledSelection;

		// Render
		var textCol = GetColorU32Internal( ImGuiCol.Text );
		if ( displayFrame )
		{
			var bgCol = GetColorU32Internal( held && hovered ? ImGuiCol.HeaderActive : hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header );
			RenderFrame( frameBb.Min, frameBb.Max, bgCol, true, style.FrameRounding );
			if ( (flags & ImGuiTreeNodeFlags.Bullet) != 0 )
				RenderBullet( window.DrawList, new Vector2( textPos.x - textOffsetX * 0.60f, textPos.y + g.FontSize * 0.5f ), textCol );
			else if ( !isLeaf )
				RenderArrow( window.DrawList, new Vector2( textPos.x - textOffsetX + padding.x, textPos.y ), textCol,
					isOpen ? ((flags & ImGuiTreeNodeFlags.UpsideDownArrow) != 0 ? ImGuiDir.Up : ImGuiDir.Down) : ImGuiDir.Right, 1.0f );
			else
				textPos.x -= textOffsetX - padding.x;

			if ( (flags & ImGuiTreeNodeFlags.ClipLabelForTrailingButton) != 0 )
				frameBb.Max.x -= g.FontSize + style.FramePadding.x;
			RenderTextClipped( textPos, frameBb.Max, labelText, labelSize, Vector2.Zero, frameBb );
		}
		else
		{
			if ( hovered || selected )
			{
				var bgCol = GetColorU32Internal( held && hovered ? ImGuiCol.HeaderActive : hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header );
				RenderFrame( frameBb.Min, frameBb.Max, bgCol, false );
			}
			if ( (flags & ImGuiTreeNodeFlags.Bullet) != 0 )
				RenderBullet( window.DrawList, new Vector2( textPos.x - textOffsetX * 0.5f, textPos.y + g.FontSize * 0.5f ), textCol );
			else if ( !isLeaf )
				RenderArrow( window.DrawList, new Vector2( textPos.x - textOffsetX + padding.x, textPos.y + g.FontSize * 0.15f ), textCol,
					isOpen ? ((flags & ImGuiTreeNodeFlags.UpsideDownArrow) != 0 ? ImGuiDir.Up : ImGuiDir.Down) : ImGuiDir.Right, 0.70f );
			RenderText( textPos, labelText, false );
		}

		if ( isOpen && (flags & ImGuiTreeNodeFlags.NoTreePushOnOpen) == 0 )
			TreePushOverrideID( id );

		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Openable | (isOpen ? ImGuiItemStatusFlags.Opened : ImGuiItemStatusFlags.None);
		return isOpen;
	}

	/// <summary>Indent() + PushID(). Already called by TreeNode() when returning true.</summary>
	public static void TreePush( string strId )
	{
		var window = GetCurrentWindow();
		Indent();
		window.DC.TreeDepth++;
		PushID( strId );
	}

	public static void TreePush( int intId )
	{
		var window = GetCurrentWindow();
		Indent();
		window.DC.TreeDepth++;
		PushID( intId );
	}

	internal static void TreePushOverrideID( int id )
	{
		var window = GetCurrentWindow();
		Indent();
		window.DC.TreeDepth++;
		PushOverrideID( id );
	}

	/// <summary>Unindent() + PopID().</summary>
	public static void TreePop()
	{
		var window = G.CurrentWindow;
		if ( window.DC.TreeDepth <= 0 )
		{
			Log.Warning( "ImGui: TreePop() called without TreeNode/TreePush" );
			return;
		}
		Unindent();
		window.DC.TreeDepth--;
		PopID();
	}

	/// <summary>Horizontal distance preceding label when using TreeNode*() or Bullet().</summary>
	public static float GetTreeNodeToLabelSpacing()
	{
		var g = G;
		return g.FontSize + g.Style.FramePadding.x * 2.0f;
	}

	/// <summary>If returning 'true' the header is open. Doesn't indent nor push on the ID stack.</summary>
	public static bool CollapsingHeader( string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None )
	{
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		return TreeNodeBehavior( window.GetID( label ), flags | ImGuiTreeNodeFlags.CollapsingHeader, label );
	}

	/// <summary>Collapsing header with a close button. When clicked, <paramref name="visible"/> is set to false.</summary>
	public static bool CollapsingHeader( string label, ref bool visible, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		if ( !visible )
			return false;

		int id = window.GetID( label );
		flags |= ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.AllowOverlap | ImGuiTreeNodeFlags.ClipLabelForTrailingButton;
		bool isOpen = TreeNodeBehavior( id, flags, label );

		var lastItemBackup = g.LastItemData;
		float buttonSize = g.FontSize;
		float buttonX = MathF.Max( g.LastItemData.Rect.Min.x, g.LastItemData.Rect.Max.x - g.Style.FramePadding.x - buttonSize );
		float buttonY = g.LastItemData.Rect.Min.y + g.Style.FramePadding.y;
		int closeButtonId = ImHashStr( "#CLOSE", id );
		if ( CloseButton( closeButtonId, new Vector2( buttonX, buttonY ) ) )
			visible = false;
		g.LastItemData = lastItemBackup;

		return isOpen;
	}
	#endregion

	#region Selectable
	/// <summary>A selectable highlights when hovered, and can display another color when selected.</summary>
	public static bool Selectable( string label, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None, Vector2 sizeArg = default )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		var labelSize = CalcTextSize( label, true );
		var size = new Vector2( sizeArg.x != 0.0f ? sizeArg.x : labelSize.x, sizeArg.y != 0.0f ? sizeArg.y : labelSize.y );
		var pos = window.DC.CursorPos;
		pos.y += window.DC.CurrLineTextBaseOffset;
		ItemSize( size, 0.0f );

		// Fill horizontal space
		bool spanAllColumns = (flags & ImGuiSelectableFlags.SpanAllColumns) != 0;
		float minX = spanAllColumns ? window.ParentWorkRect.Min.x : pos.x;
		float maxX = spanAllColumns ? window.ParentWorkRect.Max.x : window.WorkRect.Max.x;
		if ( spanAllColumns && CurrentTable is { } table )
		{
			minX = table.WorkRect.Min.x;
			maxX = table.WorkRect.Max.x;
		}
		if ( sizeArg.x == 0.0f || (flags & ImGuiSelectableFlags.SpanAvailWidth) != 0 )
			size.x = MathF.Max( labelSize.x, maxX - minX );

		var textMin = pos;
		var textMax = new Vector2( minX + size.x, pos.y + size.y );

		var bb = new ImRect( minX, pos.y, textMax.x, textMax.y );
		if ( (flags & ImGuiSelectableFlags.NoPadWithHalfSpacing) == 0 )
		{
			float spacingX = spanAllColumns ? 0.0f : style.ItemSpacing.x;
			float spacingY = style.ItemSpacing.y;
			float spacingL = ImTrunc( spacingX * 0.50f );
			float spacingU = ImTrunc( spacingY * 0.50f );
			bb.Min.x -= spacingL;
			bb.Min.y -= spacingU;
			bb.Max.x += spacingX - spacingL;
			bb.Max.y += spacingY - spacingU;
		}

		bool disabledItem = (flags & ImGuiSelectableFlags.Disabled) != 0;
		bool disabledGlobal = (g.CurrentItemFlags & ImGuiItemFlags.Disabled) != 0;
		if ( disabledItem && !disabledGlobal )
			BeginDisabled();

		bool isVisible = ItemAdd( bb, id, null, disabledItem ? ImGuiItemFlags.Disabled : ImGuiItemFlags.None );
		if ( !isVisible )
		{
			if ( disabledItem && !disabledGlobal )
				EndDisabled();
			return false;
		}

		var buttonFlags = ImGuiButtonFlags.None;
		if ( (flags & ImGuiSelectableFlags.NoHoldingActiveID) != 0 ) buttonFlags |= ImGuiButtonFlags.NoHoldingActiveId;
		if ( (flags & ImGuiSelectableFlags.SelectOnClick) != 0 ) buttonFlags |= ImGuiButtonFlags.PressedOnClick;
		if ( (flags & ImGuiSelectableFlags.SelectOnRelease) != 0 ) buttonFlags |= ImGuiButtonFlags.PressedOnRelease;
		if ( (flags & ImGuiSelectableFlags.AllowDoubleClick) != 0 ) buttonFlags |= ImGuiButtonFlags.PressedOnClickRelease | ImGuiButtonFlags.PressedOnDoubleClick;
		if ( (flags & ImGuiSelectableFlags.AllowOverlap) != 0 || (g.LastItemData.InFlags & ImGuiItemFlags.AllowOverlap) != 0 ) buttonFlags |= ImGuiButtonFlags.AllowOverlap;

		bool wasSelected = selected;
		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, buttonFlags );

		if ( pressed )
			MarkItemEdited( id );
		if ( selected != wasSelected )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.ToggledSelection;

		// Render
		if ( (flags & ImGuiSelectableFlags.Highlight) != 0 )
			hovered = true;
		if ( hovered || selected )
		{
			var col = GetColorU32Internal( held && hovered ? ImGuiCol.HeaderActive : hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header );
			RenderFrame( bb.Min, bb.Max, col, false, 0.0f );
		}

		if ( spanAllColumns && window.DC.CurrentColumns is not null )
			PushColumnsBackground();
		RenderTextClipped( textMin, textMax, label, labelSize, style.SelectableTextAlign, bb );
		if ( spanAllColumns && window.DC.CurrentColumns is not null )
			PopColumnsBackground();

		// Automatically close popups
		if ( pressed && (window.Flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiSelectableFlags.NoAutoClosePopups) == 0 && (g.LastItemData.InFlags & ImGuiItemFlags.AutoClosePopups) != 0 )
			CloseCurrentPopup();

		if ( disabledItem && !disabledGlobal )
			EndDisabled();

		return pressed;
	}

	/// <summary>"bool* p_selected" variant: toggles <paramref name="selected"/> when clicked.</summary>
	public static bool Selectable( string label, ref bool selected, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None, Vector2 sizeArg = default )
	{
		if ( Selectable( label, selected, flags, sizeArg ) )
		{
			selected = !selected;
			return true;
		}
		return false;
	}
	#endregion

	#region List box
	/// <summary>Open a framed scrolling region. You can submit contents and manage your selection state however you want.</summary>
	public static bool BeginListBox( string label, Vector2 sizeArg = default )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = GetID( label );
		var labelSize = CalcTextSize( label, true );

		// Size default to hold ~7.25 items.
		var size = ImTrunc( CalcItemSize( sizeArg, CalcItemWidth(), GetTextLineHeightWithSpacing() * 7.25f + style.FramePadding.y * 2.0f ) );
		var frameSize = new Vector2( size.x, MathF.Max( size.y, labelSize.y ) );
		var frameBb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + frameSize );
		var bb = new ImRect( frameBb.Min, frameBb.Max + new Vector2( labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f, 0.0f ) );
		g.NextItemData.ClearFlags();

		if ( !IsRectVisible( bb.Min, bb.Max ) )
		{
			ItemSize( bb.Size, style.FramePadding.y );
			ItemAdd( bb, 0, frameBb );
			g.NextWindowData.ClearFlags();
			return false;
		}

		BeginGroup();
		if ( labelSize.x > 0.0f )
		{
			var labelPos = new Vector2( frameBb.Max.x + style.ItemInnerSpacing.x, frameBb.Min.y + style.FramePadding.y );
			RenderText( labelPos, label );
			window.DC.CursorMaxPos = ImMax( window.DC.CursorMaxPos, labelPos + labelSize );
		}

		BeginChildEx( label, id, frameBb.Size, ImGuiChildFlags.FrameStyle, ImGuiWindowFlags.None );
		return true;
	}

	public static void EndListBox()
	{
		EndChild();
		EndGroup();
	}

	/// <summary>Simple list box from an array of strings. Returns true when the selection changed.</summary>
	public static bool ListBox( string label, ref int currentItem, string[] items, int heightInItems = -1 )
	{
		var g = G;
		if ( heightInItems < 0 )
			heightInItems = Math.Min( items.Length, 7 );
		float heightInItemsF = heightInItems + 0.25f;
		var size = new Vector2( 0.0f, ImTrunc( GetTextLineHeightWithSpacing() * heightInItemsF + g.Style.FramePadding.y * 2.0f ) );

		if ( !BeginListBox( label, size ) )
			return false;

		bool valueChanged = false;
		for ( int i = 0; i < items.Length; i++ )
		{
			PushID( i );
			bool itemSelected = i == currentItem;
			if ( Selectable( items[i] ?? "*Unknown item*", itemSelected ) )
			{
				currentItem = i;
				valueChanged = true;
			}
			if ( itemSelected )
				SetItemDefaultFocus();
			PopID();
		}
		EndListBox();

		if ( valueChanged )
			MarkItemEdited( g.LastItemData.ID );
		return valueChanged;
	}

	public static bool ListBox( string label, ref int currentItem, Func<int, string> getter, int itemsCount, int heightInItems = -1 )
	{
		var items = new string[itemsCount];
		for ( int i = 0; i < itemsCount; i++ )
			items[i] = getter( i );
		return ListBox( label, ref currentItem, items, heightInItems );
	}
	#endregion
}
