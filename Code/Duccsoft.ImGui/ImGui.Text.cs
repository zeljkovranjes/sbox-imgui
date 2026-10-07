namespace Duccsoft.ImGui;

public static partial class ImGui
{
	/// <summary>Raw text without formatting. Faster than Text(), recommended for long chunks of text.</summary>
	public static void TextUnformatted( string text )
	{
		TextEx( text ?? string.Empty, false );
	}

	internal static void TextEx( string text, bool hideAfterHash )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var textPos = new Vector2( window.DC.CursorPos.x, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
		float wrapPosX = window.DC.TextWrapPos;
		bool wrapEnabled = wrapPosX >= 0.0f;
		float wrapWidth = wrapEnabled ? CalcWrapWidthForPos( window.DC.CursorPos, wrapPosX ) : 0.0f;
		if ( hideAfterHash )
			text = LabelText( text );

		var textSize = CalcTextSize( text, false, wrapWidth );
		var bb = new ImRect( textPos, textPos + textSize );
		ItemSize( textSize, 0.0f );
		if ( !ItemAdd( bb, 0 ) )
			return;

		window.DrawList.AddText( 0f, bb.Min, GetColorU32Internal( ImGuiCol.Text ), text, wrapWidth );
	}

	/// <summary>Formatted text (uses .NET composite formatting: "Value: {0}").</summary>
	public static void Text( string fmt, params object[] args )
	{
		TextEx( Format( fmt, args ), false );
	}

	public static void TextColored( Vector4 col, string fmt, params object[] args )
	{
		PushStyleColor( ImGuiCol.Text, col );
		Text( fmt, args );
		PopStyleColor();
	}

	public static void TextColored( Color col, string fmt, params object[] args )
		=> TextColored( new Vector4( col.r, col.g, col.b, col.a ), fmt, args );

	public static void TextDisabled( string fmt, params object[] args )
	{
		PushStyleColor( ImGuiCol.Text, G.Style.Colors[(int)ImGuiCol.TextDisabled] );
		Text( fmt, args );
		PopStyleColor();
	}

	/// <summary>Text with word-wrapping at the end of the window (or column).</summary>
	public static void TextWrapped( string fmt, params object[] args )
	{
		var g = G;
		bool needBackup = g.CurrentWindow.DC.TextWrapPos < 0.0f;
		if ( needBackup )
			PushTextWrapPos( 0.0f );
		Text( fmt, args );
		if ( needBackup )
			PopTextWrapPos();
	}

	/// <summary>Display text+label aligned the same way as value+label widgets.</summary>
	public static void LabelText( string label, string fmt, params object[] args )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var style = g.Style;
		float w = CalcItemWidth();
		string valueText = Format( fmt, args );
		var valueSize = CalcTextSize( valueText );
		var labelSize = CalcTextSize( label, true );

		var pos = window.DC.CursorPos;
		var valueBb = new ImRect( pos, pos + new Vector2( w, valueSize.y + style.FramePadding.y * 2 ) );
		var totalBb = new ImRect( pos, pos + new Vector2( w + (labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f), MathF.Max( valueSize.y, labelSize.y ) + style.FramePadding.y * 2 ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, 0 ) )
			return;

		RenderTextClipped( valueBb.Min + style.FramePadding, valueBb.Max, valueText, valueSize, new Vector2( 0.0f, 0.0f ) );
		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( valueBb.Max.x + style.ItemInnerSpacing.x, valueBb.Min.y + style.FramePadding.y ), label );
	}

	/// <summary>Shortcut for Bullet() + Text().</summary>
	public static void BulletText( string fmt, params object[] args )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var style = g.Style;
		string text = Format( fmt, args );
		var labelSize = CalcTextSize( text, false );
		var totalSize = new Vector2( g.FontSize + (labelSize.x > 0.0f ? labelSize.x + style.FramePadding.x * 2 : 0.0f), labelSize.y );
		var pos = window.DC.CursorPos;
		pos.y += window.DC.CurrLineTextBaseOffset;
		ItemSize( totalSize, 0.0f );
		var bb = new ImRect( pos, pos + totalSize );
		if ( !ItemAdd( bb, 0 ) )
			return;

		var textCol = GetColorU32Internal( ImGuiCol.Text );
		RenderBullet( window.DrawList, bb.Min + new Vector2( style.FramePadding.x + g.FontSize * 0.5f, g.FontSize * 0.5f ), textCol );
		RenderText( bb.Min + new Vector2( g.FontSize + style.FramePadding.x * 2, 0.0f ), text, false );
	}

	/// <summary>Text separated by a horizontal line, e.g. "--- Section ---".</summary>
	public static void SeparatorText( string label )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var style = g.Style;
		var labelSize = CalcTextSize( label, true );
		var pos = window.DC.CursorPos;
		var padding = style.SeparatorTextPadding;

		float separatorThickness = style.SeparatorTextBorderSize;
		var minSize = new Vector2( labelSize.x + padding.x * 2.0f, MathF.Max( labelSize.y + padding.y * 2.0f, separatorThickness ) );
		var bb = new ImRect( pos, new Vector2( window.WorkRect.Max.x, pos.y + minSize.y ) );
		float textBaselineY = ImTrunc( (bb.Height - labelSize.y) * style.SeparatorTextAlign.y );
		ItemSize( minSize, textBaselineY );
		if ( !ItemAdd( bb, 0 ) )
			return;

		float sepY = (bb.Min.y + bb.Max.y) * 0.5f;
		float sepPos = ImLerp( bb.Min.x + padding.x, bb.Max.x - padding.x - labelSize.x, style.SeparatorTextAlign.x );
		var labelPos = new Vector2( ImFloor( sepPos ), bb.Min.y + textBaselineY );

		var sepCol = GetColorU32Internal( ImGuiCol.Separator );
		if ( labelSize.x > 0.0f )
		{
			float sep1X2 = labelPos.x - style.ItemSpacing.x;
			float sep2X1 = labelPos.x + labelSize.x + style.ItemSpacing.x;
			if ( sep1X2 > bb.Min.x && separatorThickness > 0.0f )
				window.DrawList.AddLine( new Vector2( bb.Min.x, sepY ), new Vector2( sep1X2, sepY ), sepCol, separatorThickness );
			if ( sep2X1 < bb.Max.x && separatorThickness > 0.0f )
				window.DrawList.AddLine( new Vector2( sep2X1, sepY ), new Vector2( bb.Max.x, sepY ), sepCol, separatorThickness );
			RenderText( labelPos, label );
		}
		else if ( separatorThickness > 0.0f )
		{
			window.DrawList.AddLine( new Vector2( bb.Min.x, sepY ), new Vector2( bb.Max.x, sepY ), sepCol, separatorThickness );
		}
	}

	#region Value helpers
	public static void Value( string prefix, bool b ) => Text( "{0}: {1}", prefix, b ? "true" : "false" );
	public static void Value( string prefix, int v ) => Text( "{0}: {1}", prefix, v );
	public static void Value( string prefix, uint v ) => Text( "{0}: {1}", prefix, v );
	public static void Value( string prefix, float v, string floatFormat = null )
	{
		if ( floatFormat is not null )
			Text( "{0}: {1}", prefix, v.ToString( floatFormat ) );
		else
			Text( "{0}: {1:0.000}", prefix, v );
	}
	#endregion
}
