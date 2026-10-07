namespace Duccsoft.ImGui;

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
