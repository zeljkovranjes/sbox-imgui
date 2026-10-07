using Duccsoft.ImGui.Engine;

namespace Duccsoft.ImGui;

public static partial class ImGui
{
	#region Popup stack
	internal static bool IsPopupOpen( int id, ImGuiPopupFlags popupFlags )
	{
		var g = G;
		if ( (popupFlags & ImGuiPopupFlags.AnyPopupId) != 0 )
		{
			if ( (popupFlags & ImGuiPopupFlags.AnyPopupLevel) != 0 )
				return g.OpenPopupStack.Count > 0;
			return g.OpenPopupStack.Count > g.BeginPopupStack.Count;
		}
		if ( (popupFlags & ImGuiPopupFlags.AnyPopupLevel) != 0 )
		{
			foreach ( var p in g.OpenPopupStack )
				if ( p.PopupId == id )
					return true;
			return false;
		}
		return g.OpenPopupStack.Count > g.BeginPopupStack.Count && g.OpenPopupStack[g.BeginPopupStack.Count].PopupId == id;
	}

	/// <summary>Return true if the popup is open at the current BeginPopup() level of the popup stack.</summary>
	public static bool IsPopupOpen( string strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None )
	{
		var g = G;
		int id = (popupFlags & ImGuiPopupFlags.AnyPopupId) != 0 ? 0 : g.CurrentWindow.GetID( strId );
		return IsPopupOpen( id, popupFlags );
	}

	/// <summary>Mark a popup as open (don't call every frame!). Popups are closed when the user clicks outside, or when CloseCurrentPopup() is called.</summary>
	public static void OpenPopup( string strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None )
	{
		var g = G;
		OpenPopupEx( g.CurrentWindow.GetID( strId ), popupFlags );
	}

	public static void OpenPopup( int id, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None ) => OpenPopupEx( id, popupFlags );

	internal static void OpenPopupEx( int id, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None )
	{
		var g = G;
		var parentWindow = g.CurrentWindow;
		int currentStackSize = g.BeginPopupStack.Count;

		if ( (popupFlags & ImGuiPopupFlags.NoOpenOverExistingPopup) != 0 )
			if ( IsPopupOpen( 0, ImGuiPopupFlags.AnyPopupId ) )
				return;

		var popupRef = new ImGuiPopupData
		{
			PopupId = id,
			Window = null,
			RestoreNavWindow = g.NavWindow,
			ParentNavLayer = parentWindow.DC.MenuBarAppending > 0 ? 1 : 0,
			OpenFrameCount = g.FrameCount,
			OpenParentId = parentWindow.IDStack[^1],
			OpenMousePos = IsMousePosValid( g.IO.MousePos ) ? g.IO.MousePos : Vector2.Zero,
			OpenPopupPos = IsMousePosValid( g.IO.MousePos ) ? g.IO.MousePos : Vector2.Zero,
		};

		if ( g.OpenPopupStack.Count < currentStackSize + 1 )
		{
			g.OpenPopupStack.Add( popupRef );
		}
		else
		{
			bool keepExisting = false;
			var existing = g.OpenPopupStack[currentStackSize];
			if ( existing.PopupId == id && existing.OpenFrameCount == g.FrameCount - 1 )
				keepExisting = true;
			else if ( existing.PopupId == id && (popupFlags & ImGuiPopupFlags.NoReopen) != 0 )
				keepExisting = true;

			if ( keepExisting )
			{
				existing.OpenFrameCount = popupRef.OpenFrameCount;
			}
			else
			{
				ClosePopupToLevel( currentStackSize, true );
				g.OpenPopupStack.Add( popupRef );
			}
		}
	}

	/// <summary>Close popups that are not ancestors of refWindow.</summary>
	internal static void ClosePopupsOverWindow( ImGuiWindow refWindow, bool restoreFocusToWindowUnderPopup )
	{
		var g = G;
		if ( g.OpenPopupStack.Count == 0 )
			return;

		int popupCountToKeep = 0;
		if ( refWindow is not null )
		{
			for ( ; popupCountToKeep < g.OpenPopupStack.Count; popupCountToKeep++ )
			{
				var popup = g.OpenPopupStack[popupCountToKeep];
				if ( popup.Window is null )
					continue;
				if ( (popup.Window.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
					continue;

				bool refWindowIsDescendentOfPopup = false;
				for ( int n = popupCountToKeep; n < g.OpenPopupStack.Count; n++ )
				{
					var popupWindow = g.OpenPopupStack[n].Window;
					if ( popupWindow is not null && IsWindowWithinBeginStackOf( refWindow, popupWindow ) )
					{
						refWindowIsDescendentOfPopup = true;
						break;
					}
				}
				if ( !refWindowIsDescendentOfPopup )
					break;
			}
		}

		if ( popupCountToKeep < g.OpenPopupStack.Count )
			ClosePopupToLevel( popupCountToKeep, restoreFocusToWindowUnderPopup );
	}

	internal static void ClosePopupToLevel( int remaining, bool restoreFocusToWindowUnderPopup )
	{
		var g = G;
		if ( remaining < 0 || remaining >= g.OpenPopupStack.Count )
			return;

		var popupWindow = g.OpenPopupStack[remaining].Window;
		var focusWindow = g.OpenPopupStack[remaining].RestoreNavWindow;
		g.OpenPopupStack.RemoveRange( remaining, g.OpenPopupStack.Count - remaining );

		if ( restoreFocusToWindowUnderPopup )
		{
			if ( focusWindow is not null && focusWindow.WasActive )
				FocusWindow( focusWindow );
			else if ( popupWindow is not null )
				FocusTopMostWindowUnderOne( popupWindow, null );
		}
	}

	/// <summary>Close the popup we have begin-ed into. Typically called from a menu item or button inside the popup.</summary>
	public static void CloseCurrentPopup()
	{
		var g = G;
		int popupIdx = g.BeginPopupStack.Count - 1;
		if ( popupIdx < 0 || popupIdx >= g.OpenPopupStack.Count || g.BeginPopupStack[popupIdx].PopupId != g.OpenPopupStack[popupIdx].PopupId )
			return;

		// Closing a menu closes its top-most parent popup (unless a modal)
		while ( popupIdx > 0 )
		{
			var popupWindow = g.OpenPopupStack[popupIdx].Window;
			var parentPopupWindow = g.OpenPopupStack[popupIdx - 1].Window;
			bool closeParent = false;
			if ( popupWindow is not null && (popupWindow.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
				if ( parentPopupWindow is not null && (parentPopupWindow.Flags & ImGuiWindowFlags.MenuBar) == 0 )
					closeParent = true;
			if ( !closeParent )
				break;
			popupIdx--;
		}
		ClosePopupToLevel( popupIdx, true );

		// A common pattern is to close a popup when selecting a menu item; make sure the menu's parent window keeps its focus.
		if ( g.NavWindow is not null )
			g.NavWindow.DC.MenuBarAppending = g.NavWindow.DC.MenuBarAppending;
	}
	#endregion

	#region BeginPopup
	internal static bool BeginPopupEx( int id, ImGuiWindowFlags flags )
	{
		var g = G;
		if ( !IsPopupOpen( id, ImGuiPopupFlags.None ) )
		{
			g.NextWindowData.ClearFlags();
			return false;
		}

		string name = (flags & ImGuiWindowFlags.ChildMenu) != 0
			? $"##Menu_{g.BeginMenuDepth:D2}"
			: $"##Popup_{id:X8}";

		flags |= ImGuiWindowFlags.Popup;
		bool isOpen = Begin( name, flags );
		if ( !isOpen )
			EndPopup();
		return isOpen;
	}

	/// <summary>Return true if the popup is open, and you can start outputting to it. Call EndPopup() only if this returns true.</summary>
	public static bool BeginPopup( string strId, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		var g = G;
		if ( g.OpenPopupStack.Count <= g.BeginPopupStack.Count )
		{
			g.NextWindowData.ClearFlags();
			return false;
		}
		flags |= ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings;
		int id = g.CurrentWindow.GetID( strId );
		return BeginPopupEx( id, flags );
	}

	/// <summary>
	/// Modal popups block interactions behind them and can't be closed by clicking outside. Supports a close button through <paramref name="open"/>.
	/// </summary>
	public static bool BeginPopupModal( string name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
		=> BeginPopupModalImpl( name, true, ref open, flags );

	public static bool BeginPopupModal( string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		bool dummy = true;
		return BeginPopupModalImpl( name, false, ref dummy, flags );
	}

	private static bool BeginPopupModalImpl( string name, bool hasClose, ref bool pOpen, ImGuiWindowFlags flags )
	{
		var g = G;
		var window = g.CurrentWindow;
		int id = window.GetID( name );
		if ( !IsPopupOpen( id, ImGuiPopupFlags.None ) )
		{
			g.NextWindowData.ClearFlags();
			if ( hasClose && pOpen )
				pOpen = true;
			return false;
		}

		// Center modal windows by default for increased visibility
		if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasPos) == 0 )
			SetNextWindowPos( g.IO.DisplaySize * 0.5f, ImGuiCond.FirstUseEver, new Vector2( 0.5f, 0.5f ) );

		flags |= ImGuiWindowFlags.Popup | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse;
		bool isOpen = BeginImpl( name, hasClose, ref pOpen, flags );
		if ( !isOpen || (hasClose && !pOpen) )
		{
			EndPopup();
			if ( isOpen )
				ClosePopupToLevel( g.BeginPopupStack.Count, true );
			return false;
		}
		return isOpen;
	}

	/// <summary>Only call EndPopup() if BeginPopupXXX() returns true!</summary>
	public static void EndPopup()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( (window.Flags & ImGuiWindowFlags.Popup) == 0 || g.BeginPopupStack.Count == 0 )
		{
			Log.Warning( "ImGui: EndPopup() called without a matching BeginPopup()" );
			return;
		}
		End();
	}

	/// <summary>Helper to open a popup when clicked on the last item. Default to the right mouse button.</summary>
	public static void OpenPopupOnItemClick( string strId = null, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonRight )
	{
		var g = G;
		var window = g.CurrentWindow;
		int mouseButton = (int)(popupFlags & ImGuiPopupFlags.MouseButtonMask_);
		if ( IsMouseReleased( (ImGuiMouseButton)mouseButton ) && IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup ) )
		{
			int id = strId is not null ? window.GetID( strId ) : g.LastItemData.ID;
			OpenPopupEx( id, popupFlags );
		}
	}

	/// <summary>Open and begin a popup when clicked on the last item. Use with an empty str_id for a popup tied to the item's ID.</summary>
	public static bool BeginPopupContextItem( string strId = null, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonRight )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return false;
		int id = strId is not null ? window.GetID( strId ) : g.LastItemData.ID;
		if ( id == 0 )
			return false;
		int mouseButton = (int)(popupFlags & ImGuiPopupFlags.MouseButtonMask_);
		if ( IsMouseReleased( (ImGuiMouseButton)mouseButton ) && IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup ) )
			OpenPopupEx( id, popupFlags );
		return BeginPopupEx( id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings );
	}

	/// <summary>Open and begin a popup when clicked on the current window.</summary>
	public static bool BeginPopupContextWindow( string strId = null, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonRight )
	{
		var g = G;
		var window = g.CurrentWindow;
		strId ??= "window_context";
		int id = window.GetID( strId );
		int mouseButton = (int)(popupFlags & ImGuiPopupFlags.MouseButtonMask_);
		if ( IsMouseReleased( (ImGuiMouseButton)mouseButton ) && IsWindowHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup ) )
			if ( (popupFlags & ImGuiPopupFlags.NoOpenOverItems) == 0 || !IsAnyItemHovered() )
				OpenPopupEx( id, popupFlags );
		return BeginPopupEx( id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings );
	}

	/// <summary>Open and begin a popup when clicked in void (where there are no windows).</summary>
	public static bool BeginPopupContextVoid( string strId = null, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonRight )
	{
		var g = G;
		var window = g.CurrentWindow;
		strId ??= "void_context";
		int id = window.GetID( strId );
		int mouseButton = (int)(popupFlags & ImGuiPopupFlags.MouseButtonMask_);
		if ( IsMouseReleased( (ImGuiMouseButton)mouseButton ) && !IsWindowHovered( ImGuiHoveredFlags.AnyWindow ) )
			if ( GetTopMostPopupModal() is null )
				OpenPopupEx( id, popupFlags );
		return BeginPopupEx( id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings );
	}
	#endregion

	#region Tooltips
	internal static bool BeginTooltipEx( bool overridePrevious = true )
	{
		var g = G;

		string windowName = $"##Tooltip_{g.TooltipOverrideCount:D2}";
		var window = FindWindowByName( windowName );
		if ( window is not null && window.Active && overridePrevious )
		{
			// Hide previous tooltip from being displayed. We can't easily "reset" the content of a window so we create a new one.
			window.Hidden = true;
			window.HiddenFramesCanSkipItems = 1;
			windowName = $"##Tooltip_{++g.TooltipOverrideCount:D2}";
		}

		var flags = ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;
		Begin( windowName, flags );
		return true;
	}

	/// <summary>Begin/append a tooltip window. Always call EndTooltip() when this returns true.</summary>
	public static bool BeginTooltip() => BeginTooltipEx();

	public static void EndTooltip()
	{
		var window = G.CurrentWindow;
		if ( (window.Flags & ImGuiWindowFlags.Tooltip) == 0 )
		{
			Log.Warning( "ImGui: EndTooltip() called without BeginTooltip()" );
			return;
		}
		End();
	}

	/// <summary>Set a text-only tooltip. Often used after an ImGui.IsItemHovered() check.</summary>
	public static void SetTooltip( string fmt, params object[] args )
	{
		if ( !BeginTooltipEx() )
			return;
		TextUnformatted( Format( fmt, args ) );
		EndTooltip();
	}

	/// <summary>Begin a tooltip if the last item is hovered for long enough (uses ImGuiHoveredFlags.ForTooltip).</summary>
	public static bool BeginItemTooltip()
	{
		if ( !IsItemHovered( ImGuiHoveredFlags.ForTooltip ) )
			return false;
		return BeginTooltipEx();
	}

	/// <summary>Set a text-only tooltip if the last item is hovered for long enough.</summary>
	public static void SetItemTooltip( string fmt, params object[] args )
	{
		if ( BeginItemTooltip() )
		{
			TextUnformatted( Format( fmt, args ) );
			EndTooltip();
		}
	}
	#endregion
}
