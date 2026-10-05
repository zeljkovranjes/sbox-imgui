using Duccsoft.ImGui.Rendering;

namespace Duccsoft.ImGui;

/// <summary>Callback for InputText() with the ImGuiInputTextFlags.Callback* flags. Return non-zero from a CharFilter callback to discard the character.</summary>
public delegate int ImGuiInputTextCallback( ImGuiInputTextCallbackData data );

/// <summary>
/// Shared state passed to an <see cref="ImGuiInputTextCallback"/>. Edit <see cref="Buf"/> via InsertChars/DeleteChars (or assign it and set BufDirty).
/// </summary>
public class ImGuiInputTextCallbackData
{
	/// <summary>One of ImGuiInputTextFlags.Callback* - the event that triggered this callback.</summary>
	public ImGuiInputTextFlags EventFlag;
	/// <summary>The flags passed to InputText().</summary>
	public ImGuiInputTextFlags Flags;
	public object UserData;

	/// <summary>CharFilter: character input. Replace it with another character, or set to 0 to discard.</summary>
	public char EventChar;
	/// <summary>Completion/History: key pressed (Tab, UpArrow or DownArrow).</summary>
	public ImGuiKey EventKey;

	private string _buf = string.Empty;
	/// <summary>Text buffer. If you replace it directly, set BufDirty = true.</summary>
	public string Buf
	{
		get => _buf;
		set
		{
			_buf = value ?? string.Empty;
			BufDirty = true;
		}
	}
	public int BufTextLen => _buf.Length;
	public int BufSize;
	public bool BufDirty;
	public int CursorPos;
	public int SelectionStart;
	public int SelectionEnd;

	internal void SetBufSilently( string text ) => _buf = text ?? string.Empty;

	public void DeleteChars( int pos, int bytesCount )
	{
		pos = Math.Clamp( pos, 0, _buf.Length );
		bytesCount = Math.Clamp( bytesCount, 0, _buf.Length - pos );
		if ( bytesCount == 0 ) return;
		_buf = _buf.Remove( pos, bytesCount );
		if ( CursorPos >= pos + bytesCount ) CursorPos -= bytesCount;
		else if ( CursorPos >= pos ) CursorPos = pos;
		SelectionStart = SelectionEnd = CursorPos;
		BufDirty = true;
	}

	public void InsertChars( int pos, string text )
	{
		if ( string.IsNullOrEmpty( text ) ) return;
		pos = Math.Clamp( pos, 0, _buf.Length );
		_buf = _buf.Insert( pos, text );
		if ( CursorPos >= pos ) CursorPos += text.Length;
		SelectionStart = SelectionEnd = CursorPos;
		BufDirty = true;
	}

	public void SelectAll()
	{
		SelectionStart = 0;
		CursorPos = SelectionEnd = _buf.Length;
	}

	public void ClearSelection() => SelectionStart = SelectionEnd = _buf.Length;
	public bool HasSelection => SelectionStart != SelectionEnd;
}

/// <summary>Internal state of the currently active (or last active) text input.</summary>
internal class ImGuiInputTextState
{
	public int ID;
	public string Text = string.Empty;
	public string InitialText = string.Empty;
	public int Cursor;
	public int SelectStart;
	public float ScrollX;
	public float CursorAnim;
	public bool CursorFollow;
	public bool SelectedAllMouseLock;
	public bool Edited;
	public ImGuiInputTextFlags Flags;
	public int MaxLength;
	public readonly List<(string Text, int Cursor, int Select)> UndoStack = new();
	public readonly List<(string Text, int Cursor, int Select)> RedoStack = new();

	public bool HasSelection => SelectStart != Cursor;
	public int SelMin => Math.Min( SelectStart, Cursor );
	public int SelMax => Math.Max( SelectStart, Cursor );

	public void ClampPositions()
	{
		Cursor = Math.Clamp( Cursor, 0, Text.Length );
		SelectStart = Math.Clamp( SelectStart, 0, Text.Length );
	}

	public void ClearSelection() => SelectStart = Cursor;

	public void SelectAll()
	{
		SelectStart = 0;
		Cursor = Text.Length;
	}

	public void PushUndo()
	{
		UndoStack.Add( (Text, Cursor, SelectStart) );
		if ( UndoStack.Count > 100 )
			UndoStack.RemoveAt( 0 );
		RedoStack.Clear();
	}
}

internal partial class ImGuiContext
{
	public ImGuiInputTextState InputTextState = new();
	public int TempInputId;
	public bool InputTextNoMarkEdited;
	public bool InputTextClipboardHandledByPanel;
}

public static partial class ImGui
{
	internal static bool IsTextInputActive()
	{
		var g = G;
		return g is not null && g.ActiveId != 0 && g.InputTextState.ID == g.ActiveId;
	}

	internal static bool TempInputIsActive( int id )
	{
		var g = G;
		return g.ActiveId == id && g.TempInputId == id;
	}

	private static bool _clipboardProviderInstalled;

	private static void EnsureClipboardProvider()
	{
		if ( _clipboardProviderInstalled )
			return;
		_clipboardProviderInstalled = true;
		ImGuiSystem.ClipboardCopyProvider = InputTextClipboardCopy;
	}

	/// <summary>Called by the input panel when the OS asks for clipboard contents (Ctrl+C / Ctrl+X).</summary>
	private static string InputTextClipboardCopy( bool cut )
	{
		var g = G;
		if ( g is null || !IsTextInputActive() )
			return null;
		var state = g.InputTextState;
		if ( !state.HasSelection )
			return null;
		bool isPassword = (state.Flags & ImGuiInputTextFlags.Password) != 0;
		if ( isPassword )
			return null;
		string selected = state.Text.Substring( state.SelMin, state.SelMax - state.SelMin );
		if ( cut && (state.Flags & ImGuiInputTextFlags.ReadOnly) == 0 )
		{
			state.PushUndo();
			int min = state.SelMin;
			state.Text = state.Text.Remove( min, state.SelMax - min );
			state.Cursor = state.SelectStart = min;
			state.Edited = true;
		}
		g.InputTextClipboardHandledByPanel = true;
		return selected;
	}

	#region Public API
	public static bool InputText( string label, ref string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null )
		=> InputTextEx( label, null, ref text, 0, Vector2.Zero, flags & ~ImGuiInputTextFlags.Multiline, callback );

	/// <summary>Text input limited to <paramref name="maxLength"/> characters (equivalent of Dear ImGui's buffer size).</summary>
	public static bool InputText( string label, ref string text, int maxLength, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null )
		=> InputTextEx( label, null, ref text, maxLength, Vector2.Zero, flags & ~ImGuiInputTextFlags.Multiline, callback );

	public static bool InputTextMultiline( string label, ref string text, Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null )
		=> InputTextEx( label, null, ref text, 0, size, flags | ImGuiInputTextFlags.Multiline, callback );

	public static bool InputTextWithHint( string label, string hint, ref string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null )
		=> InputTextEx( label, hint, ref text, 0, Vector2.Zero, flags & ~ImGuiInputTextFlags.Multiline, callback );
	#endregion

	/// <summary>
	/// Turn an existing item (e.g. a drag or slider) into a text field for keyboard input. Used for Ctrl+click / double-click editing.
	/// </summary>
	internal static bool TempInputText( ImRect bb, int id, string label, ref string text, ImGuiInputTextFlags flags )
	{
		var g = G;
		bool init = g.TempInputId != id;
		if ( init )
			ClearActiveID();

		g.CurrentWindow.DC.CursorPos = bb.Min;
		bool valueChanged = InputTextEx( label, null, ref text, 0, bb.Size, flags | ImGuiInputTextFlags.MergedItem, null, init, id, bb );
		if ( init )
			g.TempInputId = g.ActiveId;
		return valueChanged;
	}

	/// <summary>Text-input version of a numeric widget. Returns true when the value changed.</summary>
	internal static bool TempInputScalar( ImRect bb, int id, string label, ref double value, string format, bool isInteger, bool hasClamp, double clampMin, double clampMax )
	{
		var g = G;
		var fmt = ParseFormatTrimDecorations( format );
		if ( string.IsNullOrEmpty( fmt ) )
			fmt = isInteger ? "%d" : "%.3f";
		string buf = FormatNumber( fmt, value, isInteger ).Trim();

		var flags = ImGuiInputTextFlags.AutoSelectAll | InputScalarDefaultCharsFilter( isInteger, format );
		bool valueChanged = false;
		if ( TempInputText( bb, id, label, ref buf, flags ) )
		{
			double old = value;
			double v = ApplyExpression( buf, old, format );
			if ( isInteger )
				v = Math.Round( v, MidpointRounding.AwayFromZero );
			if ( hasClamp )
			{
				if ( clampMin < clampMax ) v = Math.Clamp( v, clampMin, clampMax );
				else if ( clampMin == clampMax ) v = clampMin;
			}
			value = v;
			valueChanged = v != old;
			if ( valueChanged )
				MarkItemEdited( id );
		}
		return valueChanged;
	}

	internal static ImGuiInputTextFlags InputScalarDefaultCharsFilter( bool isInteger, string format )
	{
		if ( !isInteger )
			return ImGuiInputTextFlags.CharsScientific;
		if ( FormatIsHex( format ) )
			return ImGuiInputTextFlags.CharsHexadecimal;
		return ImGuiInputTextFlags.CharsDecimal;
	}

	#region Character filtering / word boundaries
	private static bool InputTextFilterCharacter( ref char c, ImGuiInputTextFlags flags, ImGuiInputTextCallback callback, bool fromClipboard, ImGuiInputTextState state )
	{
		bool applyNamedFilters = true;
		if ( c < 0x20 )
		{
			bool pass = (c == '\n' && (flags & ImGuiInputTextFlags.Multiline) != 0) || (c == '\t' && (flags & ImGuiInputTextFlags.AllowTabInput) != 0);
			if ( !pass )
				return false;
			applyNamedFilters = false;
		}

		if ( !fromClipboard )
		{
			if ( c == 127 )
				return false;
			if ( c >= 0xE000 && c <= 0xF8FF )
				return false;
		}

		if ( applyNamedFilters && (flags & (ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase | ImGuiInputTextFlags.CharsNoBlank | ImGuiInputTextFlags.CharsScientific)) != 0 )
		{
			if ( (flags & ImGuiInputTextFlags.CharsDecimal) != 0 )
				if ( !(c >= '0' && c <= '9') && c != '.' && c != '-' && c != '+' && c != '*' && c != '/' )
					return false;
			if ( (flags & ImGuiInputTextFlags.CharsScientific) != 0 )
				if ( !(c >= '0' && c <= '9') && c != '.' && c != '-' && c != '+' && c != '*' && c != '/' && c != 'e' && c != 'E' )
					return false;
			if ( (flags & ImGuiInputTextFlags.CharsHexadecimal) != 0 )
				if ( !(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f') && !(c >= 'A' && c <= 'F') )
					return false;
			if ( (flags & ImGuiInputTextFlags.CharsUppercase) != 0 && c >= 'a' && c <= 'z' )
				c = (char)(c + ('A' - 'a'));
			if ( (flags & ImGuiInputTextFlags.CharsNoBlank) != 0 && (c == ' ' || c == '\t' || c == '　') )
				return false;
		}

		if ( (flags & ImGuiInputTextFlags.CallbackCharFilter) != 0 && callback is not null )
		{
			var data = new ImGuiInputTextCallbackData
			{
				EventFlag = ImGuiInputTextFlags.CallbackCharFilter,
				EventChar = c,
				Flags = flags,
			};
			data.SetBufSilently( state?.Text );
			if ( callback( data ) != 0 )
				return false;
			if ( data.EventChar == 0 )
				return false;
			c = data.EventChar;
		}
		return true;
	}

	private static bool IsWordSeparator( char c )
		=> char.IsWhiteSpace( c ) || ",;(){}[]|.!?\"'`/\\:<>=+-*&^%$#@~".IndexOf( c ) >= 0;

	private static int WordLeft( string text, int pos )
	{
		pos = Math.Clamp( pos, 0, text.Length );
		while ( pos > 0 && IsWordSeparator( text[pos - 1] ) ) pos--;
		while ( pos > 0 && !IsWordSeparator( text[pos - 1] ) ) pos--;
		return pos;
	}

	private static int WordRight( string text, int pos )
	{
		pos = Math.Clamp( pos, 0, text.Length );
		while ( pos < text.Length && !IsWordSeparator( text[pos] ) ) pos++;
		while ( pos < text.Length && IsWordSeparator( text[pos] ) && text[pos] != '\n' ) pos++;
		return pos;
	}

	private static int LineStart( string text, int pos )
	{
		pos = Math.Clamp( pos, 0, text.Length );
		while ( pos > 0 && text[pos - 1] != '\n' ) pos--;
		return pos;
	}

	private static int LineEnd( string text, int pos )
	{
		pos = Math.Clamp( pos, 0, text.Length );
		while ( pos < text.Length && text[pos] != '\n' ) pos++;
		return pos;
	}
	#endregion

	#region Text layout helpers
	private static float TextWidthPrefix( string line, int count )
	{
		if ( count <= 0 || string.IsNullOrEmpty( line ) )
			return 0f;
		count = Math.Min( count, line.Length );
		return MeasureTextWidth( line.Substring( 0, count ), G.FontPointSize );
	}

	/// <summary>Column within a line closest to a pixel x offset (binary search over prefix widths).</summary>
	private static int ColumnFromX( string line, float x )
	{
		if ( string.IsNullOrEmpty( line ) || x <= 0f )
			return 0;
		int lo = 0, hi = line.Length;
		while ( lo < hi )
		{
			int mid = (lo + hi + 1) / 2;
			if ( TextWidthPrefix( line, mid ) <= x ) lo = mid;
			else hi = mid - 1;
		}
		if ( lo < line.Length )
		{
			float a = TextWidthPrefix( line, lo );
			float b = TextWidthPrefix( line, lo + 1 );
			if ( x - a > b - x )
				lo++;
		}
		return lo;
	}

	/// <summary>Convert a character index to (line, column).</summary>
	private static void IndexToLineCol( string text, int index, out int line, out int col, out int lineStartIdx )
	{
		line = 0;
		lineStartIdx = 0;
		index = Math.Clamp( index, 0, text.Length );
		for ( int i = 0; i < index; i++ )
		{
			if ( text[i] == '\n' )
			{
				line++;
				lineStartIdx = i + 1;
			}
		}
		col = index - lineStartIdx;
	}

	private static int LocateIndex( string displayText, Vector2 rel, bool multiline )
	{
		var g = G;
		if ( !multiline )
			return ColumnFromX( displayText, rel.x );

		var lines = displayText.Split( '\n' );
		int lineIdx = Math.Clamp( (int)MathF.Floor( rel.y / g.FontSize ), 0, lines.Length - 1 );
		int idx = 0;
		for ( int i = 0; i < lineIdx; i++ )
			idx += lines[i].Length + 1;
		return idx + ColumnFromX( lines[lineIdx], rel.x );
	}
	#endregion

	/// <summary>
	/// Core text input implementation (port of Dear ImGui's InputTextEx).
	/// </summary>
	internal static bool InputTextEx( string label, string hint, ref string text, int maxLength, Vector2 sizeArg, ImGuiInputTextFlags flags, ImGuiInputTextCallback callback,
		bool forceActivate = false, int mergedId = 0, ImRect? mergedBb = null )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		EnsureClipboardProvider();
		text ??= string.Empty;

		var io = g.IO;
		var style = g.Style;

		bool isMultiline = (flags & ImGuiInputTextFlags.Multiline) != 0;
		bool isReadOnly = (flags & ImGuiInputTextFlags.ReadOnly) != 0 || (g.CurrentItemFlags & ImGuiItemFlags.ReadOnly) != 0;
		bool isPassword = (flags & ImGuiInputTextFlags.Password) != 0 && !isMultiline;
		bool isMerged = (flags & ImGuiInputTextFlags.MergedItem) != 0;

		if ( isMultiline )
			BeginGroup();

		int id = isMerged && mergedId != 0 ? mergedId : window.GetID( label );
		var labelSize = CalcTextSize( label, true );
		var frameSize = CalcItemSize( sizeArg, CalcItemWidth(), (isMultiline ? g.FontSize * 8.0f : labelSize.y) + style.FramePadding.y * 2.0f );
		var totalSize = new Vector2( frameSize.x + (labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f), frameSize.y );

		var frameBb = isMerged && mergedBb.HasValue ? mergedBb.Value : new ImRect( window.DC.CursorPos, window.DC.CursorPos + frameSize );
		var totalBb = new ImRect( frameBb.Min, frameBb.Min + totalSize );

		var drawWindow = window;
		var innerSize = frameSize;
		var itemFlagsBackup = ImGuiItemFlags.None;
		var itemStatusBackup = ImGuiItemStatusFlags.None;

		if ( isMultiline )
		{
			var backupPos = window.DC.CursorPos;
			ItemSize( totalBb, style.FramePadding.y );
			if ( !ItemAdd( totalBb, id, frameBb, ImGuiItemFlags.Inputable ) )
			{
				EndGroup();
				return false;
			}
			itemFlagsBackup = g.LastItemData.InFlags;
			itemStatusBackup = g.LastItemData.StatusFlags;
			window.DC.CursorPos = backupPos;

			PushStyleColor( ImGuiCol.ChildBg, style.Colors[(int)ImGuiCol.FrameBg] );
			PushStyleVar( ImGuiStyleVar.ChildRounding, style.FrameRounding );
			PushStyleVar( ImGuiStyleVar.ChildBorderSize, style.FrameBorderSize );
			PushStyleVar( ImGuiStyleVar.WindowPadding, Vector2.Zero );
			bool childVisible = BeginChildEx( label, id, frameBb.Size, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoMove );
			PopStyleVar( 3 );
			PopStyleColor();
			if ( !childVisible )
			{
				EndChild();
				EndGroup();
				return false;
			}
			drawWindow = g.CurrentWindow;
			drawWindow.DC.CursorPos += style.FramePadding;
			innerSize.x -= drawWindow.ScrollbarSizes.x;
		}
		else if ( !isMerged )
		{
			ItemSize( totalBb, style.FramePadding.y );
			if ( !ItemAdd( totalBb, id, frameBb, ImGuiItemFlags.Inputable ) )
				return false;
			itemFlagsBackup = g.LastItemData.InFlags;
		}
		else
		{
			itemFlagsBackup = g.LastItemData.InFlags;
		}

		bool hovered = ItemHoverable( frameBb, id, itemFlagsBackup );
		if ( hovered )
			g.MouseCursor = ImGuiMouseCursor.TextInput;

		var state = g.InputTextState;
		bool stateIsOurs = state.ID == id;

		bool focusRequestedByCode = g.FocusedTextInputRequestId == id;
		if ( focusRequestedByCode )
			g.FocusedTextInputRequestId = 0;

		bool userClicked = hovered && io.MouseClicked[0];
		int scrollbarId = isMultiline ? drawWindow.GetID( "#SCROLLY" ) : 0;
		bool userScrollActive = isMultiline && g.ActiveId == scrollbarId && scrollbarId != 0;
		bool userScrollFinish = isMultiline && g.ActiveIdPreviousFrame == scrollbarId && scrollbarId != 0 && g.ActiveId != scrollbarId && stateIsOurs;

		bool initMakeActive = userClicked || userScrollFinish || focusRequestedByCode || forceActivate;
		bool initState = initMakeActive || userScrollActive;
		bool selectAll = false;

		if ( initState && g.ActiveId != id )
		{
			bool recycle = stateIsOurs && state.Text == text;
			if ( !recycle )
			{
				state.Text = text;
				state.Cursor = state.SelectStart = 0;
				state.ScrollX = 0f;
				state.UndoStack.Clear();
				state.RedoStack.Clear();
			}
			state.ID = id;
			state.InitialText = text;
			state.Flags = flags;
			state.MaxLength = maxLength;
			state.Edited = false;
			state.CursorAnim = 0f;
			stateIsOurs = true;

			if ( (flags & ImGuiInputTextFlags.AutoSelectAll) != 0 || focusRequestedByCode )
				selectAll = true;
			if ( isMerged && forceActivate )
				selectAll = true;
			if ( !recycle && !selectAll )
				state.Cursor = state.SelectStart = text.Length;
		}

		if ( g.ActiveId != id && initMakeActive )
		{
			SetActiveID( id, window );
			FocusWindow( window );
		}

		bool clearActiveId = false;
		if ( g.ActiveId == id && io.MouseClicked[0] && !initState && !initMakeActive && !hovered )
			clearActiveId = true;

		bool valueChanged = false;
		bool validated = false;
		bool renderCursor = g.ActiveId == id || userScrollActive;
		bool renderSelection = renderCursor;

		if ( g.ActiveId == id && stateIsOurs )
		{
			state.Flags = flags;
			state.MaxLength = maxLength;
			state.ClampPositions();
			g.WantTextInputNextFrame = 1;
			// Allow clicking other widgets while we are focused (but not while selecting with the mouse).
			g.ActiveIdAllowOverlap = !io.MouseDown[0];
			KeepAliveID( id );

			string displayText = isPassword ? new string( '*', state.Text.Length ) : state.Text;
			var textOrigin = isMultiline
				? drawWindow.DC.CursorPos
				: frameBb.Min + style.FramePadding - new Vector2( state.ScrollX, 0f );
			if ( !isMultiline && labelSize.y > 0 )
				textOrigin.y = frameBb.Min.y + style.FramePadding.y;
			var mouseRel = io.MousePos - textOrigin;

			// Mouse handling
			if ( selectAll )
			{
				state.SelectAll();
				state.SelectedAllMouseLock = true;
				state.CursorFollow = true;
			}
			else if ( hovered && io.MouseClicked[0] && io.MouseClickedCount[0] >= 2 && !io.KeyShift )
			{
				int idx = LocateIndex( displayText, mouseRel, isMultiline );
				if ( io.MouseClickedCount[0] >= 3 )
				{
					state.SelectStart = LineStart( state.Text, idx );
					state.Cursor = LineEnd( state.Text, idx );
				}
				else
				{
					int ws = idx, we = idx;
					while ( ws > 0 && !IsWordSeparator( state.Text[ws - 1] ) ) ws--;
					while ( we < state.Text.Length && !IsWordSeparator( state.Text[we] ) ) we++;
					state.SelectStart = ws;
					state.Cursor = we;
				}
				state.SelectedAllMouseLock = true;
				state.CursorAnim = -0.30f;
			}
			else if ( io.MouseClicked[0] && !state.SelectedAllMouseLock )
			{
				if ( hovered )
				{
					int idx = LocateIndex( displayText, mouseRel, isMultiline );
					state.Cursor = idx;
					if ( !io.KeyShift )
						state.SelectStart = idx;
					state.CursorAnim = -0.30f;
					state.CursorFollow = true;
				}
			}
			else if ( io.MouseDown[0] && !state.SelectedAllMouseLock && (io.MouseDelta.x != 0f || io.MouseDelta.y != 0f) )
			{
				state.Cursor = LocateIndex( displayText, mouseRel, isMultiline );
				state.CursorAnim = -0.30f;
				state.CursorFollow = true;
			}
			if ( state.SelectedAllMouseLock && !io.MouseDown[0] )
				state.SelectedAllMouseLock = false;

			// Keyboard: typed characters
			if ( !isReadOnly )
			{
				foreach ( var ch in io.InputQueueCharacters )
				{
					if ( ch == '\n' || ch == '\r' || ch == '\t' )
						continue; // handled through keys
					char c = ch;
					if ( !InputTextFilterCharacter( ref c, flags, callback, false, state ) )
						continue;
					InsertText( state, c.ToString(), flags );
				}
			}

			// Keyboard: editing keys
			string beforeKeys = state.Text;
			foreach ( var k in io.InputQueueKeys )
			{
				bool shift = k.Shift;
				bool ctrl = k.Ctrl;
				switch ( k.Key )
				{
					case ImGuiKey.LeftArrow:
						if ( state.HasSelection && !shift )
							state.Cursor = state.SelMin;
						else
							state.Cursor = ctrl ? WordLeft( state.Text, state.Cursor ) : Math.Max( 0, state.Cursor - 1 );
						if ( !shift ) state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.RightArrow:
						if ( state.HasSelection && !shift )
							state.Cursor = state.SelMax;
						else
							state.Cursor = ctrl ? WordRight( state.Text, state.Cursor ) : Math.Min( state.Text.Length, state.Cursor + 1 );
						if ( !shift ) state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.UpArrow:
					case ImGuiKey.DownArrow:
						if ( isMultiline )
						{
							MoveCursorVertical( state, displayText, k.Key == ImGuiKey.UpArrow ? -1 : 1 );
							if ( !shift ) state.ClearSelection();
						}
						else if ( (flags & ImGuiInputTextFlags.CallbackHistory) != 0 && callback is not null )
						{
							RunCallback( state, flags, callback, ImGuiInputTextFlags.CallbackHistory, k.Key );
						}
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.PageUp:
					case ImGuiKey.PageDown:
						if ( isMultiline )
						{
							int pageLines = Math.Max( 1, (int)(innerSize.y / g.FontSize) - 1 );
							for ( int n = 0; n < pageLines; n++ )
								MoveCursorVertical( state, displayText, k.Key == ImGuiKey.PageUp ? -1 : 1 );
							if ( !shift ) state.ClearSelection();
							state.CursorFollow = true;
						}
						break;
					case ImGuiKey.Home:
						state.Cursor = ctrl || !isMultiline ? 0 : LineStart( state.Text, state.Cursor );
						if ( !shift ) state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.End:
						state.Cursor = ctrl || !isMultiline ? state.Text.Length : LineEnd( state.Text, state.Cursor );
						if ( !shift ) state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.Delete:
						if ( isReadOnly ) break;
						state.PushUndo();
						if ( state.HasSelection )
							DeleteSelection( state );
						else if ( state.Cursor < state.Text.Length )
						{
							int end = ctrl ? WordRight( state.Text, state.Cursor ) : state.Cursor + 1;
							state.Text = state.Text.Remove( state.Cursor, end - state.Cursor );
						}
						state.ClearSelection();
						state.CursorAnim = -0.30f;
						break;
					case ImGuiKey.Backspace:
						if ( isReadOnly ) break;
						state.PushUndo();
						if ( state.HasSelection )
							DeleteSelection( state );
						else if ( state.Cursor > 0 )
						{
							int start = ctrl ? WordLeft( state.Text, state.Cursor ) : state.Cursor - 1;
							state.Text = state.Text.Remove( start, state.Cursor - start );
							state.Cursor = start;
						}
						state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.Enter:
					case ImGuiKey.KeypadEnter:
						{
							bool ctrlEnterForNewLine = (flags & ImGuiInputTextFlags.CtrlEnterForNewLine) != 0;
							if ( !isMultiline || (ctrlEnterForNewLine && !ctrl) || (!ctrlEnterForNewLine && ctrl) )
							{
								validated = true;
								if ( !io.ConfigInputTextEnterKeepActive || !isMultiline )
									clearActiveId = !io.ConfigInputTextEnterKeepActive;
								if ( io.ConfigInputTextEnterKeepActive && !isMultiline )
								{
									state.SelectAll();
									clearActiveId = false;
								}
							}
							else if ( !isReadOnly )
							{
								char c = '\n';
								if ( InputTextFilterCharacter( ref c, flags, callback, false, state ) )
								{
									state.PushUndo();
									InsertText( state, c.ToString(), flags );
								}
							}
							break;
						}
					case ImGuiKey.Escape:
						if ( (flags & ImGuiInputTextFlags.EscapeClearsAll) != 0 )
						{
							if ( state.Text.Length > 0 && !isReadOnly )
							{
								state.PushUndo();
								state.Text = string.Empty;
								state.Cursor = state.SelectStart = 0;
							}
							else
							{
								clearActiveId = true;
								renderCursor = renderSelection = false;
							}
						}
						else
						{
							if ( !isReadOnly && state.Text != state.InitialText )
							{
								state.PushUndo();
								state.Text = state.InitialText;
								state.Cursor = state.SelectStart = state.Text.Length;
							}
							clearActiveId = true;
							renderCursor = renderSelection = false;
						}
						break;
					case ImGuiKey.Tab:
						if ( (flags & ImGuiInputTextFlags.CallbackCompletion) != 0 && callback is not null && !isReadOnly )
						{
							RunCallback( state, flags, callback, ImGuiInputTextFlags.CallbackCompletion, ImGuiKey.Tab );
						}
						else if ( (flags & ImGuiInputTextFlags.AllowTabInput) != 0 && !isReadOnly )
						{
							char c = '\t';
							if ( InputTextFilterCharacter( ref c, flags, callback, false, state ) )
							{
								state.PushUndo();
								InsertText( state, c.ToString(), flags );
							}
						}
						break;
					case ImGuiKey.A:
						if ( ctrl && !shift )
						{
							state.SelectAll();
							state.CursorFollow = true;
						}
						break;
					case ImGuiKey.C:
					case ImGuiKey.X:
						if ( ctrl && !g.InputTextClipboardHandledByPanel && state.HasSelection && !isPassword )
						{
							string sel = state.Text.Substring( state.SelMin, state.SelMax - state.SelMin );
							Sandbox.UI.Clipboard.SetText( sel );
							if ( k.Key == ImGuiKey.X && !isReadOnly )
							{
								state.PushUndo();
								DeleteSelection( state );
							}
						}
						break;
					case ImGuiKey.Z:
						if ( ctrl && !isReadOnly && (flags & ImGuiInputTextFlags.NoUndoRedo) == 0 )
						{
							if ( shift ) Redo( state ); else Undo( state );
						}
						break;
					case ImGuiKey.Y:
						if ( ctrl && !isReadOnly && (flags & ImGuiInputTextFlags.NoUndoRedo) == 0 )
							Redo( state );
						break;
				}
			}
			g.InputTextClipboardHandledByPanel = false;

			// Paste
			if ( io.PastedText is not null && !isReadOnly )
			{
				var sb = new System.Text.StringBuilder();
				foreach ( var ch in io.PastedText )
				{
					char c = ch;
					if ( c == '\r' ) continue;
					if ( c == '\n' && !isMultiline ) { c = ' '; }
					if ( InputTextFilterCharacter( ref c, flags, callback, true, state ) )
						sb.Append( c );
				}
				if ( sb.Length > 0 )
				{
					state.PushUndo();
					InsertText( state, sb.ToString(), flags );
				}
			}

			if ( isReadOnly )
				state.Text = text;

			// Always callback
			if ( (flags & ImGuiInputTextFlags.CallbackAlways) != 0 && callback is not null )
				RunCallback( state, flags, callback, ImGuiInputTextFlags.CallbackAlways, ImGuiKey.None );

			state.ClampPositions();

			if ( state.Text != text && !isReadOnly )
			{
				if ( (flags & ImGuiInputTextFlags.CallbackEdit) != 0 && callback is not null && state.Text != beforeKeys )
					RunCallback( state, flags, callback, ImGuiInputTextFlags.CallbackEdit, ImGuiKey.None );
				text = state.Text;
				valueChanged = true;
			}
		}

		if ( clearActiveId && g.ActiveId == id )
			ClearActiveID();

		// ---- Render ----
		var drawList = drawWindow.DrawList;
		if ( !isMultiline )
		{
			RenderFrame( frameBb.Min, frameBb.Max, GetColorU32Internal( ImGuiCol.FrameBg ), true, style.FrameRounding );
		}

		bool isActive = g.ActiveId == id && stateIsOurs;
		string bufDisplay = isActive || (stateIsOurs && renderCursor) ? state.Text : text;
		if ( isPassword )
			bufDisplay = new string( '*', bufDisplay.Length );

		var clipRect = isMultiline
			? new ImRect( drawWindow.InnerClipRect.Min, drawWindow.InnerClipRect.Max )
			: new ImRect( frameBb.Min.x + style.FramePadding.x * 0.5f, frameBb.Min.y, frameBb.Max.x - style.FramePadding.x * 0.5f, frameBb.Max.y );

		var drawPos = isMultiline ? drawWindow.DC.CursorPos : frameBb.Min + style.FramePadding;
		var lines = bufDisplay.Split( '\n' );
		float textMaxWidth = 0f;
		if ( isMultiline )
		{
			foreach ( var l in lines )
				textMaxWidth = MathF.Max( textMaxWidth, TextWidthPrefix( l, l.Length ) );
		}
		var textSize = new Vector2( textMaxWidth, lines.Length * g.FontSize );

		float scrollX = 0f;
		if ( isActive )
		{
			IndexToLineCol( bufDisplay, state.Cursor, out int curLine, out int curCol, out _ );
			var cursorOffset = new Vector2( TextWidthPrefix( lines[curLine], curCol ), (curLine + 1) * g.FontSize );

			if ( state.CursorFollow )
			{
				if ( !isMultiline && (flags & ImGuiInputTextFlags.NoHorizontalScroll) == 0 )
				{
					float scrollIncrementX = innerSize.x * 0.25f;
					float visibleWidth = innerSize.x - style.FramePadding.x;
					if ( cursorOffset.x < state.ScrollX )
						state.ScrollX = ImTrunc( MathF.Max( 0.0f, cursorOffset.x - scrollIncrementX ) );
					else if ( cursorOffset.x - visibleWidth >= state.ScrollX )
						state.ScrollX = ImTrunc( cursorOffset.x - visibleWidth + scrollIncrementX );
				}
				else
				{
					state.ScrollX = 0f;
				}

				if ( isMultiline )
				{
					float scrollY = drawWindow.Scroll.y;
					if ( cursorOffset.y - g.FontSize < scrollY )
						scrollY = MathF.Max( 0.0f, cursorOffset.y - g.FontSize );
					else if ( cursorOffset.y - (innerSize.y - style.FramePadding.y * 2.0f) >= scrollY )
						scrollY = cursorOffset.y - innerSize.y + style.FramePadding.y * 2.0f;
					float scrollMaxY = MathF.Max( (textSize.y + style.FramePadding.y * 2.0f) - innerSize.y, 0.0f );
					scrollY = Math.Clamp( scrollY, 0.0f, scrollMaxY );
					drawPos.y += drawWindow.Scroll.y - scrollY;
					drawWindow.Scroll = new Vector2( drawWindow.Scroll.x, scrollY );
				}
				state.CursorFollow = false;
			}
			scrollX = isMultiline ? 0f : state.ScrollX;

			// Selection
			if ( renderSelection && state.HasSelection )
			{
				var selCol = GetColorU32Internal( ImGuiCol.TextSelectedBg, renderCursor ? 1.0f : 0.6f );
				IndexToLineCol( bufDisplay, state.SelMin, out int l0, out int c0, out _ );
				IndexToLineCol( bufDisplay, state.SelMax, out int l1, out int c1, out _ );
				drawList.PushClipRect( clipRect.Min, clipRect.Max, true );
				for ( int ln = l0; ln <= l1; ln++ )
				{
					float y = drawPos.y + ln * g.FontSize;
					if ( y > clipRect.Max.y + g.FontSize ) break;
					if ( y + g.FontSize < clipRect.Min.y ) continue;
					string line = lines[ln];
					float x0 = ln == l0 ? TextWidthPrefix( line, c0 ) : 0f;
					float x1 = ln == l1 ? TextWidthPrefix( line, c1 ) : TextWidthPrefix( line, line.Length ) + MathF.Max( 2f, g.FontSize * 0.25f );
					var rMin = new Vector2( drawPos.x - scrollX + x0, y );
					var rMax = new Vector2( drawPos.x - scrollX + x1, y + g.FontSize );
					drawList.AddRectFilled( rMin, rMax, selCol );
				}
				drawList.PopClipRect();
			}

			// Text
			RenderTextLines( drawList, drawPos - new Vector2( scrollX, 0 ), lines, GetColorU32Internal( ImGuiCol.Text ), clipRect );

			// Cursor
			if ( renderCursor )
			{
				state.CursorAnim += io.DeltaTime;
				bool cursorIsVisible = !io.ConfigInputTextCursorBlink || state.CursorAnim <= 0.0f || ImFmod( state.CursorAnim, 1.20f ) <= 0.80f;
				var cursorScreenPos = ImFloor( drawPos + cursorOffset - new Vector2( scrollX, 0f ) );
				var cursorRect = new ImRect( cursorScreenPos.x, cursorScreenPos.y - g.FontSize + 0.5f, cursorScreenPos.x + 1.0f, cursorScreenPos.y - 1.5f );
				if ( cursorIsVisible && cursorRect.Overlaps( clipRect ) )
					drawList.AddLine( cursorRect.Min, cursorRect.BL, GetColorU32Internal( ImGuiCol.InputTextCursor ) );
			}
		}
		else
		{
			if ( bufDisplay.Length == 0 && hint is not null )
			{
				RenderTextLines( drawList, drawPos, LabelText( hint ).Split( '\n' ), GetColorU32Internal( ImGuiCol.TextDisabled ), clipRect );
			}
			else
			{
				RenderTextLines( drawList, drawPos, lines, GetColorU32Internal( ImGuiCol.Text ), clipRect );
			}
		}

		if ( isActive && bufDisplay.Length == 0 && hint is not null )
			RenderTextLines( drawList, drawPos, LabelText( hint ).Split( '\n' ), GetColorU32Internal( ImGuiCol.TextDisabled ), clipRect );

		if ( isMultiline )
		{
			Dummy( new Vector2( textSize.x, textSize.y + style.FramePadding.y ) );
			EndChild();
			EndGroup();
			g.LastItemData.ID = id;
			g.LastItemData.InFlags = itemFlagsBackup;
			g.LastItemData.StatusFlags = itemStatusBackup | (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredWindow);
		}

		if ( labelSize.x > 0 && !isMerged )
			RenderText( new Vector2( frameBb.Max.x + style.ItemInnerSpacing.x, frameBb.Min.y + style.FramePadding.y ), label );

		if ( valueChanged && !g.InputTextNoMarkEdited )
			MarkItemEdited( id );
		else if ( valueChanged )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Edited;

		if ( (flags & ImGuiInputTextFlags.EnterReturnsTrue) != 0 )
			return validated;
		return valueChanged;
	}

	private static void RenderTextLines( ImDrawList drawList, Vector2 pos, string[] lines, Color32 col, ImRect clip )
	{
		var g = G;
		drawList.PushClipRect( clip.Min, clip.Max, true );
		for ( int i = 0; i < lines.Length; i++ )
		{
			float y = pos.y + i * g.FontSize;
			if ( y > clip.Max.y ) break;
			if ( y + g.FontSize < clip.Min.y ) continue;
			if ( lines[i].Length > 0 )
				drawList.AddText( 0f, new Vector2( pos.x, y ), col, lines[i] );
		}
		drawList.PopClipRect();
	}

	private static void InsertText( ImGuiInputTextState state, string s, ImGuiInputTextFlags flags )
	{
		if ( state.HasSelection )
			DeleteSelection( state );
		if ( state.MaxLength > 0 )
		{
			int room = state.MaxLength - state.Text.Length;
			if ( room <= 0 ) return;
			if ( s.Length > room ) s = s.Substring( 0, room );
		}
		bool overwrite = (flags & ImGuiInputTextFlags.AlwaysOverwrite) != 0;
		if ( overwrite && state.Cursor < state.Text.Length && s.Length == 1 && state.Text[state.Cursor] != '\n' )
			state.Text = state.Text.Remove( state.Cursor, 1 );
		state.Text = state.Text.Insert( state.Cursor, s );
		state.Cursor += s.Length;
		state.SelectStart = state.Cursor;
		state.CursorAnim = -0.30f;
		state.CursorFollow = true;
	}

	private static void DeleteSelection( ImGuiInputTextState state )
	{
		if ( !state.HasSelection ) return;
		int min = state.SelMin;
		state.Text = state.Text.Remove( min, state.SelMax - min );
		state.Cursor = state.SelectStart = min;
	}

	private static void Undo( ImGuiInputTextState state )
	{
		if ( state.UndoStack.Count == 0 ) return;
		state.RedoStack.Add( (state.Text, state.Cursor, state.SelectStart) );
		var e = state.UndoStack[^1];
		state.UndoStack.RemoveAt( state.UndoStack.Count - 1 );
		state.Text = e.Text;
		state.Cursor = e.Cursor;
		state.SelectStart = e.Select;
		state.ClampPositions();
		state.CursorFollow = true;
	}

	private static void Redo( ImGuiInputTextState state )
	{
		if ( state.RedoStack.Count == 0 ) return;
		state.UndoStack.Add( (state.Text, state.Cursor, state.SelectStart) );
		var e = state.RedoStack[^1];
		state.RedoStack.RemoveAt( state.RedoStack.Count - 1 );
		state.Text = e.Text;
		state.Cursor = e.Cursor;
		state.SelectStart = e.Select;
		state.ClampPositions();
		state.CursorFollow = true;
	}

	private static void MoveCursorVertical( ImGuiInputTextState state, string displayText, int dir )
	{
		IndexToLineCol( displayText, state.Cursor, out int line, out int col, out int lineStartIdx );
		var lines = displayText.Split( '\n' );
		int target = line + dir;
		if ( target < 0 )
		{
			state.Cursor = 0;
			return;
		}
		if ( target >= lines.Length )
		{
			state.Cursor = displayText.Length;
			return;
		}
		float x = TextWidthPrefix( lines[line], col );
		int idx = 0;
		for ( int i = 0; i < target; i++ )
			idx += lines[i].Length + 1;
		state.Cursor = idx + ColumnFromX( lines[target], x );
	}

	private static void RunCallback( ImGuiInputTextState state, ImGuiInputTextFlags flags, ImGuiInputTextCallback callback, ImGuiInputTextFlags eventFlag, ImGuiKey key )
	{
		var data = new ImGuiInputTextCallbackData
		{
			EventFlag = eventFlag,
			Flags = flags,
			EventKey = key,
			CursorPos = state.Cursor,
			SelectionStart = state.SelectStart,
			SelectionEnd = state.Cursor,
			BufSize = state.MaxLength > 0 ? state.MaxLength : int.MaxValue,
		};
		data.SetBufSilently( state.Text );
		callback( data );

		bool isReadOnly = (flags & ImGuiInputTextFlags.ReadOnly) != 0;
		if ( data.BufDirty && !isReadOnly )
		{
			state.PushUndo();
			var newText = data.Buf;
			if ( state.MaxLength > 0 && newText.Length > state.MaxLength )
				newText = newText.Substring( 0, state.MaxLength );
			state.Text = newText;
			state.CursorFollow = true;
		}
		state.Cursor = Math.Clamp( data.CursorPos, 0, state.Text.Length );
		state.SelectStart = Math.Clamp( data.SelectionStart == data.SelectionEnd ? state.Cursor : data.SelectionStart, 0, state.Text.Length );
		if ( data.SelectionStart != data.SelectionEnd )
			state.Cursor = Math.Clamp( data.SelectionEnd, 0, state.Text.Length );
	}
}
