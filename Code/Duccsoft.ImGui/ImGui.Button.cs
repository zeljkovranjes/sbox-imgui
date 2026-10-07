namespace Duccsoft.ImGui;

public static partial class ImGui
{
	internal static bool ButtonEx( string label, Vector2 sizeArg, ImGuiButtonFlags flags = ImGuiButtonFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		var labelSize = CalcTextSize( label, true );

		var pos = window.DC.CursorPos;
		if ( (flags & ImGuiButtonFlags.AlignTextBaseLine) != 0 && style.FramePadding.y < window.DC.CurrLineTextBaseOffset )
			pos.y += window.DC.CurrLineTextBaseOffset - style.FramePadding.y;
		var size = CalcItemSize( sizeArg, labelSize.x + style.FramePadding.x * 2.0f, labelSize.y + style.FramePadding.y * 2.0f );

		var bb = new ImRect( pos, pos + size );
		ItemSize( size, style.FramePadding.y );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, flags );

		var col = GetColorU32Internal( held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
		RenderFrame( bb.Min, bb.Max, col, true, style.FrameRounding );
		RenderTextClipped( bb.Min + style.FramePadding, bb.Max - style.FramePadding, label, labelSize, style.ButtonTextAlign, bb );

		return pressed;
	}

	/// <summary>A button. Returns true when clicked. size 0 = auto-fit, &lt;0 = align to the right edge.</summary>
	public static bool Button( string label, Vector2 size = default ) => ButtonEx( label, size );

	/// <summary>Button with FramePadding.y = 0, to easily embed within text.</summary>
	public static bool SmallButton( string label )
	{
		var g = G;
		float backupPaddingY = g.Style.FramePadding.y;
		g.Style.FramePadding = new Vector2( g.Style.FramePadding.x, 0.0f );
		bool pressed = ButtonEx( label, Vector2.Zero, ImGuiButtonFlags.AlignTextBaseLine );
		g.Style.FramePadding = new Vector2( g.Style.FramePadding.x, backupPaddingY );
		return pressed;
	}

	/// <summary>Flexible button behavior without the visuals; useful to build custom behaviors.</summary>
	public static bool InvisibleButton( string strId, Vector2 sizeArg, ImGuiButtonFlags flags = ImGuiButtonFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( strId );
		var size = CalcItemSize( sizeArg, 0.0f, 0.0f );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + size );
		ItemSize( size );
		if ( !ItemAdd( bb, id ) )
			return false;

		return ButtonBehavior( bb, id, out _, out _, flags );
	}

	internal static bool ArrowButtonEx( string strId, ImGuiDir dir, Vector2 size, ImGuiButtonFlags flags = ImGuiButtonFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( strId );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + size );
		float defaultSize = GetFrameHeight();
		ItemSize( size, size.y >= defaultSize ? g.Style.FramePadding.y : -1.0f );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, flags );

		var bgCol = GetColorU32Internal( held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
		var textCol = GetColorU32Internal( ImGuiCol.Text );
		RenderFrame( bb.Min, bb.Max, bgCol, true, g.Style.FrameRounding );
		RenderArrow( window.DrawList, bb.Min + new Vector2( MathF.Max( 0.0f, (size.x - g.FontSize) * 0.5f ), MathF.Max( 0.0f, (size.y - g.FontSize) * 0.5f ) ), textCol, dir );

		return pressed;
	}

	/// <summary>Square button with an arrow shape.</summary>
	public static bool ArrowButton( string strId, ImGuiDir dir )
	{
		float sz = GetFrameHeight();
		return ArrowButtonEx( strId, dir, new Vector2( sz, sz ) );
	}

	public static bool Checkbox( string label, ref bool v )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		var labelSize = CalcTextSize( label, true );

		float squareSz = GetFrameHeight();
		var pos = window.DC.CursorPos;
		var totalBb = new ImRect( pos, pos + new Vector2( squareSz + (labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f), labelSize.y + style.FramePadding.y * 2.0f ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id ) )
			return false;

		bool pressed = ButtonBehavior( totalBb, id, out bool hovered, out bool held );
		if ( pressed )
		{
			v = !v;
			MarkItemEdited( id );
		}

		var checkBb = new ImRect( pos, pos + new Vector2( squareSz, squareSz ) );
		RenderFrame( checkBb.Min, checkBb.Max, GetColorU32Internal( held && hovered ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg ), true, style.FrameRounding );
		var checkCol = GetColorU32Internal( ImGuiCol.CheckMark );
		bool mixedValue = (g.LastItemData.InFlags & ImGuiItemFlags.MixedValue) != 0;
		if ( mixedValue )
		{
			var padMixed = new Vector2( MathF.Max( 1.0f, ImTrunc( squareSz / 3.6f ) ) );
			window.DrawList.AddRectFilled( checkBb.Min + padMixed, checkBb.Max - padMixed, checkCol, style.FrameRounding );
		}
		else if ( v )
		{
			float pad = MathF.Max( 1.0f, ImTrunc( squareSz / 6.0f ) );
			RenderCheckMark( window.DrawList, checkBb.Min + new Vector2( pad, pad ), checkCol, squareSz - pad * 2.0f );
		}

		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Checkable | (v ? ImGuiItemStatusFlags.Checked : ImGuiItemStatusFlags.None);
		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( checkBb.Max.x + style.ItemInnerSpacing.x, checkBb.Min.y + style.FramePadding.y ), label );

		return pressed;
	}

	public static bool CheckboxFlags( string label, ref int flags, int flagsValue )
	{
		bool allOn = (flags & flagsValue) == flagsValue;
		bool anyOn = (flags & flagsValue) != 0;
		bool pressed;
		if ( !allOn && anyOn )
		{
			var g = G;
			PushItemFlag( ImGuiItemFlags.MixedValue, true );
			pressed = Checkbox( label, ref allOn );
			PopItemFlag();
		}
		else
		{
			pressed = Checkbox( label, ref allOn );
		}
		if ( pressed )
		{
			if ( allOn )
				flags |= flagsValue;
			else
				flags &= ~flagsValue;
		}
		return pressed;
	}

	public static bool CheckboxFlags( string label, ref uint flags, uint flagsValue )
	{
		int f = unchecked((int)flags);
		bool pressed = CheckboxFlags( label, ref f, unchecked((int)flagsValue) );
		flags = unchecked((uint)f);
		return pressed;
	}

	/// <summary>Checkbox bound to an enum flags value.</summary>
	public static bool CheckboxFlags<T>( string label, ref T flags, T flagsValue ) where T : struct, Enum
	{
		int f = Convert.ToInt32( flags );
		bool pressed = CheckboxFlags( label, ref f, Convert.ToInt32( flagsValue ) );
		if ( pressed )
			flags = (T)Enum.ToObject( typeof( T ), f );
		return pressed;
	}

	/// <summary>Radio button. Use with "if (RadioButton("one", myValue == 1)) myValue = 1;"</summary>
	public static bool RadioButton( string label, bool active )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		var labelSize = CalcTextSize( label, true );

		float squareSz = GetFrameHeight();
		var pos = window.DC.CursorPos;
		var checkBb = new ImRect( pos, pos + new Vector2( squareSz, squareSz ) );
		var totalBb = new ImRect( pos, pos + new Vector2( squareSz + (labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f), labelSize.y + style.FramePadding.y * 2.0f ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id ) )
			return false;

		var center = checkBb.Center;
		float radius = (squareSz - 1.0f) * 0.5f;

		bool pressed = ButtonBehavior( totalBb, id, out bool hovered, out bool held );
		if ( pressed )
			MarkItemEdited( id );

		window.DrawList.AddCircleFilled( center, radius, GetColorU32Internal( held && hovered ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg ) );
		if ( active )
		{
			float pad = MathF.Max( 1.0f, ImTrunc( squareSz / 6.0f ) );
			window.DrawList.AddCircleFilled( center, radius - pad, GetColorU32Internal( ImGuiCol.CheckMark ) );
		}

		if ( style.FrameBorderSize > 0.0f )
		{
			window.DrawList.AddCircle( center + Vector2.One, radius, GetColorU32Internal( ImGuiCol.BorderShadow ), 0, style.FrameBorderSize );
			window.DrawList.AddCircle( center, radius, GetColorU32Internal( ImGuiCol.Border ), 0, style.FrameBorderSize );
		}

		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( checkBb.Max.x + style.ItemInnerSpacing.x, checkBb.Min.y + style.FramePadding.y ), label );

		return pressed;
	}

	/// <summary>Shortcut to handle the above pattern when value is an integer.</summary>
	public static bool RadioButton( string label, ref int v, int vButton )
	{
		bool pressed = RadioButton( label, v == vButton );
		if ( pressed )
			v = vButton;
		return pressed;
	}

	/// <summary>Progress bar. fraction in [0,1]; a negative fraction shows an indeterminate animation.</summary>
	public static void ProgressBar( float fraction, Vector2 sizeArg = default, string overlay = null )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		if ( sizeArg == default )
			sizeArg = new Vector2( -1.17549435E-38f, 0.0f ); // -FLT_MIN: fill available width

		var style = g.Style;
		var pos = window.DC.CursorPos;
		var size = CalcItemSize( sizeArg, CalcItemWidth(), g.FontSize + style.FramePadding.y * 2.0f );
		var bb = new ImRect( pos, pos + size );
		ItemSize( size, style.FramePadding.y );
		if ( !ItemAdd( bb, 0 ) )
			return;

		bool isIndeterminate = fraction < 0.0f;
		if ( !isIndeterminate )
			fraction = ImSaturate( fraction );

		RenderFrame( bb.Min, bb.Max, GetColorU32Internal( ImGuiCol.FrameBg ), true, style.FrameRounding );
		bb.Expand( new Vector2( -style.FrameBorderSize, -style.FrameBorderSize ) );

		float fillN0 = 0.0f, fillN1 = fraction;
		if ( isIndeterminate )
		{
			float fillWidthN = 0.2f;
			fillN0 = ImFmod( -fraction, 1.0f ) * (1.0f + fillWidthN) - fillWidthN;
			fillN1 = ImSaturate( fillN0 + fillWidthN );
			fillN0 = ImSaturate( fillN0 );
		}

		RenderRectFilledRangeH( window.DrawList, bb, GetColorU32Internal( ImGuiCol.PlotHistogram ), fillN0, fillN1, style.FrameRounding );

		if ( isIndeterminate )
			return;

		overlay ??= $"{fraction * 100 + 0.01f:0}%";
		var overlaySize = CalcTextSize( overlay );
		if ( overlaySize.x > 0.0f )
		{
			float fillX = ImLerp( bb.Min.x, bb.Max.x, fillN1 );
			RenderTextClipped( new Vector2( Math.Clamp( fillX + style.ItemSpacing.x, bb.Min.x, bb.Max.x - overlaySize.x - style.ItemInnerSpacing.x ), bb.Min.y ), bb.Max, overlay, overlaySize, new Vector2( 0.0f, 0.5f ), bb );
		}
	}

	private static float ImFmod( float a, float b )
	{
		float r = a % b;
		return r < 0 ? r + b : r;
	}

	/// <summary>Draw a small circle + keep the cursor on the same line. Advance cursor x position by GetTreeNodeToLabelSpacing().</summary>
	public static void Bullet()
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var style = g.Style;
		float lineHeight = MathF.Max( MathF.Min( window.DC.CurrLineSize.y, g.FontSize + style.FramePadding.y * 2 ), g.FontSize );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + new Vector2( g.FontSize, lineHeight ) );
		ItemSize( bb );
		if ( !ItemAdd( bb, 0 ) )
		{
			SameLine( 0, style.FramePadding.x * 2 );
			return;
		}

		var textCol = GetColorU32Internal( ImGuiCol.Text );
		RenderBullet( window.DrawList, bb.Min + new Vector2( style.FramePadding.x + g.FontSize * 0.5f, lineHeight * 0.5f ), textCol );
		SameLine( 0, style.FramePadding.x * 2.0f );
	}

	/// <summary>Hyperlink text button, returns true when clicked.</summary>
	public static bool TextLink( string label )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( label );
		var pos = new Vector2( window.DC.CursorPos.x, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
		var size = CalcTextSize( label, true );
		var bb = new ImRect( pos, pos + size );
		ItemSize( size, 0.0f );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held );
		if ( hovered )
			g.MouseCursor = ImGuiMouseCursor.Hand;

		var textColor = g.Style.Colors[(int)ImGuiCol.TextLink];
		var lineColor = textColor;
		if ( hovered || held )
			lineColor = textColor = new Vector4( textColor.x * 1.2f, textColor.y * 1.2f, textColor.z * 1.2f, textColor.w );

		float lineY = bb.Max.y - 1.0f;
		window.DrawList.AddLine( new Vector2( bb.Min.x, lineY ), new Vector2( bb.Max.x, lineY ), GetColorU32( lineColor ) );

		PushStyleColor( ImGuiCol.Text, textColor );
		RenderText( bb.Min, label );
		PopStyleColor();

		return pressed;
	}

	/// <summary>
	/// Hyperlink for a URL. s&amp;box games cannot open a browser directly, so clicking copies the URL to the clipboard.
	/// </summary>
	public static void TextLinkOpenURL( string label, string url = null )
	{
		url ??= label;
		if ( TextLink( label ) )
			Sandbox.UI.Clipboard.SetText( url );
		SetItemTooltip( "{0} (click to copy)", url );
	}

	#region Images
	public static void Image( Texture texture, Vector2 imageSize, Vector2 uv0, Vector2 uv1, Vector4 tintCol, Vector4 borderCol )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		float borderSize = borderCol.w > 0.0f ? MathF.Max( 1.0f, g.Style.ImageBorderSize ) : g.Style.ImageBorderSize;
		var padding = new Vector2( borderSize, borderSize );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + imageSize + padding * 2.0f );
		ItemSize( bb );
		if ( !ItemAdd( bb, 0 ) )
			return;

		if ( borderSize > 0.0f )
			window.DrawList.AddRect( bb.Min, bb.Max, GetColorU32( borderCol ), 0.0f, ImDrawFlags.None, borderSize );
		window.DrawList.AddImage( texture, bb.Min + padding, bb.Max - padding, uv0, uv1, GetColorU32( tintCol ) );
	}

	public static void Image( Texture texture, Vector2 imageSize ) => Image( texture, imageSize, Vector2.Zero, Vector2.One, Vector4.One, Vector4.Zero );
	public static void Image( Texture texture, Vector2 imageSize, Vector2 uv0, Vector2 uv1 ) => Image( texture, imageSize, uv0, uv1, Vector4.One, Vector4.Zero );

	public static void Image( Texture texture, Vector2 imageSize, Vector2 uv0, Vector2 uv1, Color tintColor, Color borderColor )
		=> Image( texture, imageSize, uv0, uv1, new Vector4( tintColor.r, tintColor.g, tintColor.b, tintColor.a ), new Vector4( borderColor.r, borderColor.g, borderColor.b, borderColor.a ) );

	public static void Image( Texture texture, Vector2 imageSize, Color tintColor, Color borderColor )
		=> Image( texture, imageSize, Vector2.Zero, Vector2.One, tintColor, borderColor );

	/// <summary>Display an image with a background color, inside a button frame.</summary>
	public static bool ImageButton( string strId, Texture texture, Vector2 imageSize, Vector2 uv0 = default, Vector2 uv1 = default, Vector4 bgCol = default, Vector4? tintCol = null )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		if ( uv1 == default ) uv1 = Vector2.One;
		var tint = tintCol ?? Vector4.One;

		int id = window.GetID( strId );
		var padding = g.Style.FramePadding;
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + imageSize + padding * 2.0f );
		ItemSize( bb );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held );

		var col = GetColorU32Internal( held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
		RenderFrame( bb.Min, bb.Max, col, true, Math.Clamp( MathF.Min( padding.x, padding.y ), 0.0f, g.Style.FrameRounding ) );
		if ( bgCol.w > 0.0f )
			window.DrawList.AddRectFilled( bb.Min + padding, bb.Max - padding, GetColorU32( bgCol ) );
		window.DrawList.AddImage( texture, bb.Min + padding, bb.Max - padding, uv0, uv1, GetColorU32( tint ) );

		return pressed;
	}
	#endregion
}
