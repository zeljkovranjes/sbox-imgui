namespace Duccsoft.ImGui;

internal partial class ImGuiContext
{
	public struct FontStackEntry
	{
		public float FontSize;
		public float FontPointSize;
		public string FontName;
		public int FontWeight;
	}

	public readonly List<FontStackEntry> FontStack = new();
}

public static partial class ImGui
{
	#region Colors
	/// <summary>Get a style color with the style alpha applied, as a packed 32-bit color.</summary>
	public static Color32 GetColorU32( ImGuiCol idx, float alphaMul = 1.0f ) => GetColorU32Internal( idx, alphaMul );

	public static Color32 GetColorU32( Vector4 col )
	{
		col.w *= G.Style.Alpha;
		return ColorConvertFloat4ToU32( col );
	}

	public static Color32 GetColorU32( Color col ) => GetColorU32( new Vector4( col.r, col.g, col.b, col.a ) );

	public static Color32 GetColorU32( Color32 col, float alphaMul = 1.0f )
	{
		float styleAlpha = G.Style.Alpha * alphaMul;
		if ( styleAlpha >= 1.0f )
			return col;
		return col with { a = (byte)(col.a * styleAlpha) };
	}

	public static Vector4 GetStyleColorVec4( ImGuiCol idx ) => G.Style.Colors[(int)idx];

	public static string GetStyleColorName( ImGuiCol idx ) => idx.ToString();

	public static void PushStyleColor( ImGuiCol idx, Vector4 col )
	{
		var g = G;
		g.ColorStack.Add( new ImGuiColorMod { Col = idx, BackupValue = g.Style.Colors[(int)idx] } );
		g.Style.Colors[(int)idx] = col;
	}

	public static void PushStyleColor( ImGuiCol idx, Color32 col ) => PushStyleColor( idx, ColorConvertU32ToFloat4( col ) );
	public static void PushStyleColor( ImGuiCol idx, Color col ) => PushStyleColor( idx, new Vector4( col.r, col.g, col.b, col.a ) );

	public static void PopStyleColor( int count = 1 )
	{
		var g = G;
		if ( g.ColorStack.Count < count )
		{
			Log.Warning( "ImGui: PopStyleColor() called more times than PushStyleColor()" );
			count = g.ColorStack.Count;
		}
		while ( count > 0 )
		{
			var backup = g.ColorStack[^1];
			g.Style.Colors[(int)backup.Col] = backup.BackupValue;
			g.ColorStack.RemoveAt( g.ColorStack.Count - 1 );
			count--;
		}
	}
	#endregion

	#region Style vars
	private static bool IsVec2Var( ImGuiStyleVar idx ) => idx switch
	{
		ImGuiStyleVar.WindowPadding or ImGuiStyleVar.WindowMinSize or ImGuiStyleVar.WindowTitleAlign or
		ImGuiStyleVar.FramePadding or ImGuiStyleVar.ItemSpacing or ImGuiStyleVar.ItemInnerSpacing or
		ImGuiStyleVar.CellPadding or ImGuiStyleVar.TableAngledHeadersTextAlign or ImGuiStyleVar.ButtonTextAlign or
		ImGuiStyleVar.SelectableTextAlign or ImGuiStyleVar.SeparatorTextAlign or ImGuiStyleVar.SeparatorTextPadding => true,
		_ => false
	};

	private static Vector2 GetStyleVar( ImGuiStyleVar idx )
	{
		var s = G.Style;
		return idx switch
		{
			ImGuiStyleVar.Alpha => new Vector2( s.Alpha, 0 ),
			ImGuiStyleVar.DisabledAlpha => new Vector2( s.DisabledAlpha, 0 ),
			ImGuiStyleVar.WindowPadding => s.WindowPadding,
			ImGuiStyleVar.WindowRounding => new Vector2( s.WindowRounding, 0 ),
			ImGuiStyleVar.WindowBorderSize => new Vector2( s.WindowBorderSize, 0 ),
			ImGuiStyleVar.WindowMinSize => s.WindowMinSize,
			ImGuiStyleVar.WindowTitleAlign => s.WindowTitleAlign,
			ImGuiStyleVar.ChildRounding => new Vector2( s.ChildRounding, 0 ),
			ImGuiStyleVar.ChildBorderSize => new Vector2( s.ChildBorderSize, 0 ),
			ImGuiStyleVar.PopupRounding => new Vector2( s.PopupRounding, 0 ),
			ImGuiStyleVar.PopupBorderSize => new Vector2( s.PopupBorderSize, 0 ),
			ImGuiStyleVar.FramePadding => s.FramePadding,
			ImGuiStyleVar.FrameRounding => new Vector2( s.FrameRounding, 0 ),
			ImGuiStyleVar.FrameBorderSize => new Vector2( s.FrameBorderSize, 0 ),
			ImGuiStyleVar.ItemSpacing => s.ItemSpacing,
			ImGuiStyleVar.ItemInnerSpacing => s.ItemInnerSpacing,
			ImGuiStyleVar.IndentSpacing => new Vector2( s.IndentSpacing, 0 ),
			ImGuiStyleVar.CellPadding => s.CellPadding,
			ImGuiStyleVar.ScrollbarSize => new Vector2( s.ScrollbarSize, 0 ),
			ImGuiStyleVar.ScrollbarRounding => new Vector2( s.ScrollbarRounding, 0 ),
			ImGuiStyleVar.GrabMinSize => new Vector2( s.GrabMinSize, 0 ),
			ImGuiStyleVar.GrabRounding => new Vector2( s.GrabRounding, 0 ),
			ImGuiStyleVar.TabRounding => new Vector2( s.TabRounding, 0 ),
			ImGuiStyleVar.TabBorderSize => new Vector2( s.TabBorderSize, 0 ),
			ImGuiStyleVar.TabBarBorderSize => new Vector2( s.TabBarBorderSize, 0 ),
			ImGuiStyleVar.TabBarOverlineSize => new Vector2( s.TabBarOverlineSize, 0 ),
			ImGuiStyleVar.TableAngledHeadersAngle => new Vector2( s.TableAngledHeadersAngle, 0 ),
			ImGuiStyleVar.TableAngledHeadersTextAlign => s.TableAngledHeadersTextAlign,
			ImGuiStyleVar.ButtonTextAlign => s.ButtonTextAlign,
			ImGuiStyleVar.SelectableTextAlign => s.SelectableTextAlign,
			ImGuiStyleVar.SeparatorTextBorderSize => new Vector2( s.SeparatorTextBorderSize, 0 ),
			ImGuiStyleVar.SeparatorTextAlign => s.SeparatorTextAlign,
			ImGuiStyleVar.SeparatorTextPadding => s.SeparatorTextPadding,
			_ => Vector2.Zero
		};
	}

	private static void SetStyleVar( ImGuiStyleVar idx, Vector2 v )
	{
		var s = G.Style;
		switch ( idx )
		{
			case ImGuiStyleVar.Alpha: s.Alpha = v.x; break;
			case ImGuiStyleVar.DisabledAlpha: s.DisabledAlpha = v.x; break;
			case ImGuiStyleVar.WindowPadding: s.WindowPadding = v; break;
			case ImGuiStyleVar.WindowRounding: s.WindowRounding = v.x; break;
			case ImGuiStyleVar.WindowBorderSize: s.WindowBorderSize = v.x; break;
			case ImGuiStyleVar.WindowMinSize: s.WindowMinSize = v; break;
			case ImGuiStyleVar.WindowTitleAlign: s.WindowTitleAlign = v; break;
			case ImGuiStyleVar.ChildRounding: s.ChildRounding = v.x; break;
			case ImGuiStyleVar.ChildBorderSize: s.ChildBorderSize = v.x; break;
			case ImGuiStyleVar.PopupRounding: s.PopupRounding = v.x; break;
			case ImGuiStyleVar.PopupBorderSize: s.PopupBorderSize = v.x; break;
			case ImGuiStyleVar.FramePadding: s.FramePadding = v; break;
			case ImGuiStyleVar.FrameRounding: s.FrameRounding = v.x; break;
			case ImGuiStyleVar.FrameBorderSize: s.FrameBorderSize = v.x; break;
			case ImGuiStyleVar.ItemSpacing: s.ItemSpacing = v; break;
			case ImGuiStyleVar.ItemInnerSpacing: s.ItemInnerSpacing = v; break;
			case ImGuiStyleVar.IndentSpacing: s.IndentSpacing = v.x; break;
			case ImGuiStyleVar.CellPadding: s.CellPadding = v; break;
			case ImGuiStyleVar.ScrollbarSize: s.ScrollbarSize = v.x; break;
			case ImGuiStyleVar.ScrollbarRounding: s.ScrollbarRounding = v.x; break;
			case ImGuiStyleVar.GrabMinSize: s.GrabMinSize = v.x; break;
			case ImGuiStyleVar.GrabRounding: s.GrabRounding = v.x; break;
			case ImGuiStyleVar.TabRounding: s.TabRounding = v.x; break;
			case ImGuiStyleVar.TabBorderSize: s.TabBorderSize = v.x; break;
			case ImGuiStyleVar.TabBarBorderSize: s.TabBarBorderSize = v.x; break;
			case ImGuiStyleVar.TabBarOverlineSize: s.TabBarOverlineSize = v.x; break;
			case ImGuiStyleVar.TableAngledHeadersAngle: s.TableAngledHeadersAngle = v.x; break;
			case ImGuiStyleVar.TableAngledHeadersTextAlign: s.TableAngledHeadersTextAlign = v; break;
			case ImGuiStyleVar.ButtonTextAlign: s.ButtonTextAlign = v; break;
			case ImGuiStyleVar.SelectableTextAlign: s.SelectableTextAlign = v; break;
			case ImGuiStyleVar.SeparatorTextBorderSize: s.SeparatorTextBorderSize = v.x; break;
			case ImGuiStyleVar.SeparatorTextAlign: s.SeparatorTextAlign = v; break;
			case ImGuiStyleVar.SeparatorTextPadding: s.SeparatorTextPadding = v; break;
		}
	}

	public static void PushStyleVar( ImGuiStyleVar idx, float val )
	{
		if ( IsVec2Var( idx ) )
		{
			Log.Warning( $"ImGui: PushStyleVar({idx}) expects a Vector2" );
			return;
		}
		var g = G;
		g.StyleVarStack.Add( new ImGuiStyleMod { VarIdx = idx, BackupValue = GetStyleVar( idx ) } );
		SetStyleVar( idx, new Vector2( val, 0 ) );
	}

	public static void PushStyleVar( ImGuiStyleVar idx, Vector2 val )
	{
		if ( !IsVec2Var( idx ) )
		{
			Log.Warning( $"ImGui: PushStyleVar({idx}) expects a float" );
			return;
		}
		var g = G;
		g.StyleVarStack.Add( new ImGuiStyleMod { VarIdx = idx, BackupValue = GetStyleVar( idx ) } );
		SetStyleVar( idx, val );
	}

	public static void PushStyleVarX( ImGuiStyleVar idx, float valX ) => PushStyleVar( idx, new Vector2( valX, GetStyleVar( idx ).y ) );
	public static void PushStyleVarY( ImGuiStyleVar idx, float valY ) => PushStyleVar( idx, new Vector2( GetStyleVar( idx ).x, valY ) );

	public static void PopStyleVar( int count = 1 )
	{
		var g = G;
		if ( g.StyleVarStack.Count < count )
		{
			Log.Warning( "ImGui: PopStyleVar() called more times than PushStyleVar()" );
			count = g.StyleVarStack.Count;
		}
		while ( count > 0 )
		{
			var backup = g.StyleVarStack[^1];
			SetStyleVar( backup.VarIdx, backup.BackupValue );
			g.StyleVarStack.RemoveAt( g.StyleVarStack.Count - 1 );
			count--;
		}
	}
	#endregion

	#region Fonts
	/// <summary>
	/// Change the font for subsequent text. <paramref name="sizePixels"/> is the font size at the reference
	/// resolution (scaled like the rest of the UI); 0 keeps the current size. <paramref name="fontName"/> null keeps the current family.
	/// </summary>
	public static void PushFont( string fontName = null, float sizePixels = 0f, int weight = 0 )
	{
		var g = G;
		g.FontStack.Add( new ImGuiContext.FontStackEntry { FontSize = g.FontSize, FontPointSize = g.FontPointSize, FontName = g.FontName, FontWeight = g.FontWeight } );
		if ( fontName is not null ) g.FontName = fontName;
		if ( weight > 0 ) g.FontWeight = weight;
		if ( sizePixels > 0f ) g.FontPointSize = sizePixels * g.AppliedStyleScale;
		g.FontSize = MeasureLineHeight( g.FontPointSize, g.FontName, g.FontWeight );
		g.TextSizeCache.Clear();
	}

	public static void PopFont()
	{
		var g = G;
		if ( g.FontStack.Count == 0 )
			return;
		var e = g.FontStack[^1];
		g.FontStack.RemoveAt( g.FontStack.Count - 1 );
		g.FontSize = e.FontSize;
		g.FontPointSize = e.FontPointSize;
		g.FontName = e.FontName;
		g.FontWeight = e.FontWeight;
		g.TextSizeCache.Clear();
	}
	#endregion

	#region Style presets
	private static Vector4 V4( float r, float g, float b, float a ) => new( r, g, b, a );

	public static void StyleColorsDark( ImGuiStyle dst = null )
	{
		var style = dst ?? G.Style;
		var c = style.Colors;
		c[(int)ImGuiCol.Text] = V4( 1.00f, 1.00f, 1.00f, 1.00f );
		c[(int)ImGuiCol.TextDisabled] = V4( 0.50f, 0.50f, 0.50f, 1.00f );
		c[(int)ImGuiCol.WindowBg] = V4( 0.06f, 0.06f, 0.06f, 0.94f );
		c[(int)ImGuiCol.ChildBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.PopupBg] = V4( 0.08f, 0.08f, 0.08f, 0.94f );
		c[(int)ImGuiCol.Border] = V4( 0.43f, 0.43f, 0.50f, 0.50f );
		c[(int)ImGuiCol.BorderShadow] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.FrameBg] = V4( 0.16f, 0.29f, 0.48f, 0.54f );
		c[(int)ImGuiCol.FrameBgHovered] = V4( 0.26f, 0.59f, 0.98f, 0.40f );
		c[(int)ImGuiCol.FrameBgActive] = V4( 0.26f, 0.59f, 0.98f, 0.67f );
		c[(int)ImGuiCol.TitleBg] = V4( 0.04f, 0.04f, 0.04f, 1.00f );
		c[(int)ImGuiCol.TitleBgActive] = V4( 0.16f, 0.29f, 0.48f, 1.00f );
		c[(int)ImGuiCol.TitleBgCollapsed] = V4( 0.00f, 0.00f, 0.00f, 0.51f );
		c[(int)ImGuiCol.MenuBarBg] = V4( 0.14f, 0.14f, 0.14f, 1.00f );
		c[(int)ImGuiCol.ScrollbarBg] = V4( 0.02f, 0.02f, 0.02f, 0.53f );
		c[(int)ImGuiCol.ScrollbarGrab] = V4( 0.31f, 0.31f, 0.31f, 1.00f );
		c[(int)ImGuiCol.ScrollbarGrabHovered] = V4( 0.41f, 0.41f, 0.41f, 1.00f );
		c[(int)ImGuiCol.ScrollbarGrabActive] = V4( 0.51f, 0.51f, 0.51f, 1.00f );
		c[(int)ImGuiCol.CheckMark] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.SliderGrab] = V4( 0.24f, 0.52f, 0.88f, 1.00f );
		c[(int)ImGuiCol.SliderGrabActive] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Button] = V4( 0.26f, 0.59f, 0.98f, 0.40f );
		c[(int)ImGuiCol.ButtonHovered] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.ButtonActive] = V4( 0.06f, 0.53f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Header] = V4( 0.26f, 0.59f, 0.98f, 0.31f );
		c[(int)ImGuiCol.HeaderHovered] = V4( 0.26f, 0.59f, 0.98f, 0.80f );
		c[(int)ImGuiCol.HeaderActive] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Separator] = c[(int)ImGuiCol.Border];
		c[(int)ImGuiCol.SeparatorHovered] = V4( 0.10f, 0.40f, 0.75f, 0.78f );
		c[(int)ImGuiCol.SeparatorActive] = V4( 0.10f, 0.40f, 0.75f, 1.00f );
		c[(int)ImGuiCol.ResizeGrip] = V4( 0.26f, 0.59f, 0.98f, 0.20f );
		c[(int)ImGuiCol.ResizeGripHovered] = V4( 0.26f, 0.59f, 0.98f, 0.67f );
		c[(int)ImGuiCol.ResizeGripActive] = V4( 0.26f, 0.59f, 0.98f, 0.95f );
		c[(int)ImGuiCol.InputTextCursor] = c[(int)ImGuiCol.Text];
		c[(int)ImGuiCol.TabHovered] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.Tab] = Vector4.Lerp( c[(int)ImGuiCol.Header], c[(int)ImGuiCol.TitleBgActive], 0.80f );
		c[(int)ImGuiCol.TabSelected] = Vector4.Lerp( c[(int)ImGuiCol.HeaderActive], c[(int)ImGuiCol.TitleBgActive], 0.60f );
		c[(int)ImGuiCol.TabSelectedOverline] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TabDimmed] = Vector4.Lerp( c[(int)ImGuiCol.Tab], c[(int)ImGuiCol.TitleBg], 0.80f );
		c[(int)ImGuiCol.TabDimmedSelected] = Vector4.Lerp( c[(int)ImGuiCol.TabSelected], c[(int)ImGuiCol.TitleBg], 0.40f );
		c[(int)ImGuiCol.TabDimmedSelectedOverline] = V4( 0.50f, 0.50f, 0.50f, 0.00f );
		c[(int)ImGuiCol.PlotLines] = V4( 0.61f, 0.61f, 0.61f, 1.00f );
		c[(int)ImGuiCol.PlotLinesHovered] = V4( 1.00f, 0.43f, 0.35f, 1.00f );
		c[(int)ImGuiCol.PlotHistogram] = V4( 0.90f, 0.70f, 0.00f, 1.00f );
		c[(int)ImGuiCol.PlotHistogramHovered] = V4( 1.00f, 0.60f, 0.00f, 1.00f );
		c[(int)ImGuiCol.TableHeaderBg] = V4( 0.19f, 0.19f, 0.20f, 1.00f );
		c[(int)ImGuiCol.TableBorderStrong] = V4( 0.31f, 0.31f, 0.35f, 1.00f );
		c[(int)ImGuiCol.TableBorderLight] = V4( 0.23f, 0.23f, 0.25f, 1.00f );
		c[(int)ImGuiCol.TableRowBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.TableRowBgAlt] = V4( 1.00f, 1.00f, 1.00f, 0.06f );
		c[(int)ImGuiCol.TextLink] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TextSelectedBg] = V4( 0.26f, 0.59f, 0.98f, 0.35f );
		c[(int)ImGuiCol.DragDropTarget] = V4( 1.00f, 1.00f, 0.00f, 0.90f );
		c[(int)ImGuiCol.NavCursor] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.NavWindowingHighlight] = V4( 1.00f, 1.00f, 1.00f, 0.70f );
		c[(int)ImGuiCol.NavWindowingDimBg] = V4( 0.80f, 0.80f, 0.80f, 0.20f );
		c[(int)ImGuiCol.ModalWindowDimBg] = V4( 0.80f, 0.80f, 0.80f, 0.35f );
	}

	public static void StyleColorsLight( ImGuiStyle dst = null )
	{
		var style = dst ?? G.Style;
		var c = style.Colors;
		c[(int)ImGuiCol.Text] = V4( 0.00f, 0.00f, 0.00f, 1.00f );
		c[(int)ImGuiCol.TextDisabled] = V4( 0.60f, 0.60f, 0.60f, 1.00f );
		c[(int)ImGuiCol.WindowBg] = V4( 0.94f, 0.94f, 0.94f, 1.00f );
		c[(int)ImGuiCol.ChildBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.PopupBg] = V4( 1.00f, 1.00f, 1.00f, 0.98f );
		c[(int)ImGuiCol.Border] = V4( 0.00f, 0.00f, 0.00f, 0.30f );
		c[(int)ImGuiCol.BorderShadow] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.FrameBg] = V4( 1.00f, 1.00f, 1.00f, 1.00f );
		c[(int)ImGuiCol.FrameBgHovered] = V4( 0.26f, 0.59f, 0.98f, 0.40f );
		c[(int)ImGuiCol.FrameBgActive] = V4( 0.26f, 0.59f, 0.98f, 0.67f );
		c[(int)ImGuiCol.TitleBg] = V4( 0.96f, 0.96f, 0.96f, 1.00f );
		c[(int)ImGuiCol.TitleBgActive] = V4( 0.82f, 0.82f, 0.82f, 1.00f );
		c[(int)ImGuiCol.TitleBgCollapsed] = V4( 1.00f, 1.00f, 1.00f, 0.51f );
		c[(int)ImGuiCol.MenuBarBg] = V4( 0.86f, 0.86f, 0.86f, 1.00f );
		c[(int)ImGuiCol.ScrollbarBg] = V4( 0.98f, 0.98f, 0.98f, 0.53f );
		c[(int)ImGuiCol.ScrollbarGrab] = V4( 0.69f, 0.69f, 0.69f, 0.80f );
		c[(int)ImGuiCol.ScrollbarGrabHovered] = V4( 0.49f, 0.49f, 0.49f, 0.80f );
		c[(int)ImGuiCol.ScrollbarGrabActive] = V4( 0.49f, 0.49f, 0.49f, 1.00f );
		c[(int)ImGuiCol.CheckMark] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.SliderGrab] = V4( 0.26f, 0.59f, 0.98f, 0.78f );
		c[(int)ImGuiCol.SliderGrabActive] = V4( 0.46f, 0.54f, 0.80f, 0.60f );
		c[(int)ImGuiCol.Button] = V4( 0.26f, 0.59f, 0.98f, 0.40f );
		c[(int)ImGuiCol.ButtonHovered] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.ButtonActive] = V4( 0.06f, 0.53f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Header] = V4( 0.26f, 0.59f, 0.98f, 0.31f );
		c[(int)ImGuiCol.HeaderHovered] = V4( 0.26f, 0.59f, 0.98f, 0.80f );
		c[(int)ImGuiCol.HeaderActive] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Separator] = V4( 0.39f, 0.39f, 0.39f, 0.62f );
		c[(int)ImGuiCol.SeparatorHovered] = V4( 0.14f, 0.44f, 0.80f, 0.78f );
		c[(int)ImGuiCol.SeparatorActive] = V4( 0.14f, 0.44f, 0.80f, 1.00f );
		c[(int)ImGuiCol.ResizeGrip] = V4( 0.35f, 0.35f, 0.35f, 0.17f );
		c[(int)ImGuiCol.ResizeGripHovered] = V4( 0.26f, 0.59f, 0.98f, 0.67f );
		c[(int)ImGuiCol.ResizeGripActive] = V4( 0.26f, 0.59f, 0.98f, 0.95f );
		c[(int)ImGuiCol.InputTextCursor] = c[(int)ImGuiCol.Text];
		c[(int)ImGuiCol.TabHovered] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.Tab] = Vector4.Lerp( c[(int)ImGuiCol.Header], c[(int)ImGuiCol.TitleBgActive], 0.90f );
		c[(int)ImGuiCol.TabSelected] = Vector4.Lerp( c[(int)ImGuiCol.HeaderActive], c[(int)ImGuiCol.TitleBgActive], 0.60f );
		c[(int)ImGuiCol.TabSelectedOverline] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TabDimmed] = Vector4.Lerp( c[(int)ImGuiCol.Tab], c[(int)ImGuiCol.TitleBg], 0.80f );
		c[(int)ImGuiCol.TabDimmedSelected] = Vector4.Lerp( c[(int)ImGuiCol.TabSelected], c[(int)ImGuiCol.TitleBg], 0.40f );
		c[(int)ImGuiCol.TabDimmedSelectedOverline] = V4( 0.26f, 0.59f, 1.00f, 0.00f );
		c[(int)ImGuiCol.PlotLines] = V4( 0.39f, 0.39f, 0.39f, 1.00f );
		c[(int)ImGuiCol.PlotLinesHovered] = V4( 1.00f, 0.43f, 0.35f, 1.00f );
		c[(int)ImGuiCol.PlotHistogram] = V4( 0.90f, 0.70f, 0.00f, 1.00f );
		c[(int)ImGuiCol.PlotHistogramHovered] = V4( 1.00f, 0.45f, 0.00f, 1.00f );
		c[(int)ImGuiCol.TableHeaderBg] = V4( 0.78f, 0.87f, 0.98f, 1.00f );
		c[(int)ImGuiCol.TableBorderStrong] = V4( 0.57f, 0.57f, 0.64f, 1.00f );
		c[(int)ImGuiCol.TableBorderLight] = V4( 0.68f, 0.68f, 0.74f, 1.00f );
		c[(int)ImGuiCol.TableRowBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.TableRowBgAlt] = V4( 0.30f, 0.30f, 0.30f, 0.09f );
		c[(int)ImGuiCol.TextLink] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TextSelectedBg] = V4( 0.26f, 0.59f, 0.98f, 0.35f );
		c[(int)ImGuiCol.DragDropTarget] = V4( 0.26f, 0.59f, 0.98f, 0.95f );
		c[(int)ImGuiCol.NavCursor] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.NavWindowingHighlight] = V4( 0.70f, 0.70f, 0.70f, 0.70f );
		c[(int)ImGuiCol.NavWindowingDimBg] = V4( 0.20f, 0.20f, 0.20f, 0.20f );
		c[(int)ImGuiCol.ModalWindowDimBg] = V4( 0.20f, 0.20f, 0.20f, 0.35f );
	}

	public static void StyleColorsClassic( ImGuiStyle dst = null )
	{
		var style = dst ?? G.Style;
		var c = style.Colors;
		c[(int)ImGuiCol.Text] = V4( 0.90f, 0.90f, 0.90f, 1.00f );
		c[(int)ImGuiCol.TextDisabled] = V4( 0.60f, 0.60f, 0.60f, 1.00f );
		c[(int)ImGuiCol.WindowBg] = V4( 0.00f, 0.00f, 0.00f, 0.85f );
		c[(int)ImGuiCol.ChildBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.PopupBg] = V4( 0.11f, 0.11f, 0.14f, 0.92f );
		c[(int)ImGuiCol.Border] = V4( 0.50f, 0.50f, 0.50f, 0.50f );
		c[(int)ImGuiCol.BorderShadow] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.FrameBg] = V4( 0.43f, 0.43f, 0.43f, 0.39f );
		c[(int)ImGuiCol.FrameBgHovered] = V4( 0.47f, 0.47f, 0.69f, 0.40f );
		c[(int)ImGuiCol.FrameBgActive] = V4( 0.42f, 0.41f, 0.64f, 0.69f );
		c[(int)ImGuiCol.TitleBg] = V4( 0.27f, 0.27f, 0.54f, 0.83f );
		c[(int)ImGuiCol.TitleBgActive] = V4( 0.32f, 0.32f, 0.63f, 0.87f );
		c[(int)ImGuiCol.TitleBgCollapsed] = V4( 0.40f, 0.40f, 0.80f, 0.20f );
		c[(int)ImGuiCol.MenuBarBg] = V4( 0.40f, 0.40f, 0.55f, 0.80f );
		c[(int)ImGuiCol.ScrollbarBg] = V4( 0.20f, 0.25f, 0.30f, 0.60f );
		c[(int)ImGuiCol.ScrollbarGrab] = V4( 0.40f, 0.40f, 0.80f, 0.30f );
		c[(int)ImGuiCol.ScrollbarGrabHovered] = V4( 0.40f, 0.40f, 0.80f, 0.40f );
		c[(int)ImGuiCol.ScrollbarGrabActive] = V4( 0.41f, 0.39f, 0.80f, 0.60f );
		c[(int)ImGuiCol.CheckMark] = V4( 0.90f, 0.90f, 0.90f, 0.50f );
		c[(int)ImGuiCol.SliderGrab] = V4( 1.00f, 1.00f, 1.00f, 0.30f );
		c[(int)ImGuiCol.SliderGrabActive] = V4( 0.41f, 0.39f, 0.80f, 0.60f );
		c[(int)ImGuiCol.Button] = V4( 0.35f, 0.40f, 0.61f, 0.62f );
		c[(int)ImGuiCol.ButtonHovered] = V4( 0.40f, 0.48f, 0.71f, 0.79f );
		c[(int)ImGuiCol.ButtonActive] = V4( 0.46f, 0.54f, 0.80f, 1.00f );
		c[(int)ImGuiCol.Header] = V4( 0.40f, 0.40f, 0.90f, 0.45f );
		c[(int)ImGuiCol.HeaderHovered] = V4( 0.45f, 0.45f, 0.90f, 0.80f );
		c[(int)ImGuiCol.HeaderActive] = V4( 0.53f, 0.53f, 0.87f, 0.80f );
		c[(int)ImGuiCol.Separator] = V4( 0.50f, 0.50f, 0.50f, 0.60f );
		c[(int)ImGuiCol.SeparatorHovered] = V4( 0.60f, 0.60f, 0.70f, 1.00f );
		c[(int)ImGuiCol.SeparatorActive] = V4( 0.70f, 0.70f, 0.90f, 1.00f );
		c[(int)ImGuiCol.ResizeGrip] = V4( 1.00f, 1.00f, 1.00f, 0.10f );
		c[(int)ImGuiCol.ResizeGripHovered] = V4( 0.78f, 0.82f, 1.00f, 0.60f );
		c[(int)ImGuiCol.ResizeGripActive] = V4( 0.78f, 0.82f, 1.00f, 0.90f );
		c[(int)ImGuiCol.InputTextCursor] = c[(int)ImGuiCol.Text];
		c[(int)ImGuiCol.TabHovered] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.Tab] = Vector4.Lerp( c[(int)ImGuiCol.Header], c[(int)ImGuiCol.TitleBgActive], 0.80f );
		c[(int)ImGuiCol.TabSelected] = Vector4.Lerp( c[(int)ImGuiCol.HeaderActive], c[(int)ImGuiCol.TitleBgActive], 0.60f );
		c[(int)ImGuiCol.TabSelectedOverline] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TabDimmed] = Vector4.Lerp( c[(int)ImGuiCol.Tab], c[(int)ImGuiCol.TitleBg], 0.80f );
		c[(int)ImGuiCol.TabDimmedSelected] = Vector4.Lerp( c[(int)ImGuiCol.TabSelected], c[(int)ImGuiCol.TitleBg], 0.40f );
		c[(int)ImGuiCol.TabDimmedSelectedOverline] = V4( 0.53f, 0.53f, 0.87f, 0.00f );
		c[(int)ImGuiCol.PlotLines] = V4( 1.00f, 1.00f, 1.00f, 1.00f );
		c[(int)ImGuiCol.PlotLinesHovered] = V4( 0.90f, 0.70f, 0.00f, 1.00f );
		c[(int)ImGuiCol.PlotHistogram] = V4( 0.90f, 0.70f, 0.00f, 1.00f );
		c[(int)ImGuiCol.PlotHistogramHovered] = V4( 1.00f, 0.60f, 0.00f, 1.00f );
		c[(int)ImGuiCol.TableHeaderBg] = V4( 0.27f, 0.27f, 0.38f, 1.00f );
		c[(int)ImGuiCol.TableBorderStrong] = V4( 0.31f, 0.31f, 0.45f, 1.00f );
		c[(int)ImGuiCol.TableBorderLight] = V4( 0.26f, 0.26f, 0.28f, 1.00f );
		c[(int)ImGuiCol.TableRowBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.TableRowBgAlt] = V4( 1.00f, 1.00f, 1.00f, 0.07f );
		c[(int)ImGuiCol.TextLink] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TextSelectedBg] = V4( 0.00f, 0.00f, 1.00f, 0.35f );
		c[(int)ImGuiCol.DragDropTarget] = V4( 1.00f, 1.00f, 0.00f, 0.90f );
		c[(int)ImGuiCol.NavCursor] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.NavWindowingHighlight] = V4( 1.00f, 1.00f, 1.00f, 0.70f );
		c[(int)ImGuiCol.NavWindowingDimBg] = V4( 0.80f, 0.80f, 0.80f, 0.20f );
		c[(int)ImGuiCol.ModalWindowDimBg] = V4( 0.20f, 0.20f, 0.20f, 0.35f );
	}
	#endregion
}
