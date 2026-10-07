namespace Duccsoft.ImGui.Engine;

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
