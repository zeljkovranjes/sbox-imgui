namespace Duccsoft.ImGui.Engine;

/// <summary>
/// Simple column measurement, used by menus to align shortcuts and check marks.
/// </summary>
internal class ImGuiMenuColumns
{
	public int TotalWidth;
	public int NextTotalWidth;
	public int Spacing;
	public int OffsetIcon;
	public int OffsetLabel;
	public int OffsetShortcut;
	public int OffsetMark;
	public int[] Widths = new int[4];

	public void Update( float spacing, bool windowReappearing )
	{
		if ( windowReappearing )
			Array.Clear( Widths );
		Spacing = (int)spacing;
		CalcNextTotalWidth( true );
		Array.Clear( Widths );
		TotalWidth = NextTotalWidth;
		NextTotalWidth = 0;
	}

	public void CalcNextTotalWidth( bool updateOffsets )
	{
		int offset = 0;
		bool wantSpacing = false;
		for ( int i = 0; i < Widths.Length; i++ )
		{
			int width = Widths[i];
			if ( wantSpacing && width > 0 )
				offset += Spacing;
			wantSpacing |= width > 0;
			if ( updateOffsets )
			{
				if ( i == 1 ) OffsetLabel = offset;
				if ( i == 2 ) OffsetShortcut = offset;
				if ( i == 3 ) OffsetMark = offset;
			}
			offset += width;
		}
		NextTotalWidth = offset;
	}

	public float DeclColumns( float wIcon, float wLabel, float wShortcut, float wMark )
	{
		Widths[0] = Math.Max( Widths[0], (int)wIcon );
		Widths[1] = Math.Max( Widths[1], (int)wLabel );
		Widths[2] = Math.Max( Widths[2], (int)wShortcut );
		Widths[3] = Math.Max( Widths[3], (int)wMark );
		CalcNextTotalWidth( false );
		return Math.Max( TotalWidth, NextTotalWidth );
	}
}
