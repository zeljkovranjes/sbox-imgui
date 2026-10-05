namespace Duccsoft.ImGui;

public static partial class ImGui
{
	private static int _styleSelectorIdx = -1;
	private static string _styleFilter = "";
	private static int _metricsSelectedWindow = -1;

	/// <summary>Create a Metrics/Debugger window: internal state, windows, draw lists, items.</summary>
	public static void ShowMetricsWindow( ref bool open )
	{
		var g = G;
		var io = g.IO;
		if ( !Begin( "Dear ImGui Metrics/Debugger", ref open ) )
		{
			End();
			return;
		}

		Text( "Dear ImGui (s&box port)" );
		Text( "Application average {0:F3} ms/frame ({1:F1} FPS)", 1000.0f / MathF.Max( io.Framerate, 0.001f ), io.Framerate );
		Text( "{0} active windows ({1} draw lists)", io.MetricsActiveWindows, io.MetricsRenderWindows );
		Text( "Display size: {0:F0} x {1:F0}, UI scale: {2:F2}, font: {3} {4:F1}px (line {5:F0}px)", io.DisplaySize.x, io.DisplaySize.y, g.AppliedStyleScale, g.FontName, g.FontPointSize, g.FontSize );
		Separator();

		if ( TreeNode( "Windows", "Windows ({0})", g.Windows.Count ) )
		{
			for ( int i = g.Windows.Count - 1; i >= 0; i-- )
			{
				var w = g.Windows[i];
				if ( (w.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
					continue;
				MetricsNodeWindow( w );
			}
			TreePop();
		}

		if ( TreeNode( "Popups", "Popups ({0})", g.OpenPopupStack.Count ) )
		{
			foreach ( var popup in g.OpenPopupStack )
				BulletText( "PopupID: {0:X8}, Window: '{1}'", popup.PopupId, popup.Window?.Name ?? "NULL" );
			TreePop();
		}

		if ( TreeNode( "Internal state" ) )
		{
			Text( "HoveredWindow: '{0}'", g.HoveredWindow?.Name ?? "NULL" );
			Text( "MovingWindow: '{0}'", g.MovingWindow?.Name ?? "NULL" );
			Text( "NavWindow (focused): '{0}'", g.NavWindow?.Name ?? "NULL" );
			Text( "HoveredId: 0x{0:X8}/0x{1:X8} ({2:F2} sec), AllowOverlap: {3}", g.HoveredId, g.HoveredIdPreviousFrame, g.HoveredIdTimer, g.HoveredIdAllowOverlap );
			Text( "ActiveId: 0x{0:X8}/0x{1:X8} ({2:F2} sec), AllowOverlap: {3}", g.ActiveId, g.ActiveIdPreviousFrame, g.ActiveIdTimer, g.ActiveIdAllowOverlap );
			Text( "ActiveIdWindow: '{0}'", g.ActiveIdWindow?.Name ?? "NULL" );
			Text( "DragDropActive: {0}", g.DragDropActive );
			Text( "WantCaptureMouse: {0}, WantCaptureKeyboard: {1}, WantTextInput: {2}", io.WantCaptureMouse, io.WantCaptureKeyboard, io.WantTextInput );
			TreePop();
		}

		if ( TreeNode( "Tools" ) )
		{
			Checkbox( "Show windows rectangles", ref _metricsShowWindowRects );
			TreePop();
		}

		if ( _metricsShowWindowRects )
		{
			foreach ( var w in g.Windows )
			{
				if ( !w.WasActive ) continue;
				var dl = GetForegroundDrawList();
				dl.AddRect( w.Pos, w.Pos + w.Size, new Color32( 255, 0, 128, 255 ) );
				dl.AddRect( w.InnerRect.Min, w.InnerRect.Max, new Color32( 0, 255, 128, 160 ) );
			}
		}

		End();
	}

	private static bool _metricsShowWindowRects;

	private static void MetricsNodeWindow( ImGuiWindow window )
	{
		bool isActive = window.WasActive;
		if ( !isActive )
			PushStyleColor( ImGuiCol.Text, GetStyleColorVec4( ImGuiCol.TextDisabled ) );
		bool open = TreeNode( window.Name, "{0} '{1}'{2}", (window.Flags & ImGuiWindowFlags.ChildWindow) != 0 ? "Child" : "Window", LabelText( window.Name ), isActive ? "" : " *Inactive*" );
		if ( !isActive )
			PopStyleColor();
		if ( IsItemHovered() && isActive )
			GetForegroundDrawList().AddRect( window.Pos, window.Pos + window.Size, new Color32( 255, 255, 0, 255 ) );
		if ( !open )
			return;

		BulletText( "Pos: ({0:F1},{1:F1}), Size: ({2:F1},{3:F1}), ContentSize ({4:F1},{5:F1})", window.Pos.x, window.Pos.y, window.Size.x, window.Size.y, window.ContentSize.x, window.ContentSize.y );
		BulletText( "Flags: {0}", window.Flags );
		BulletText( "Scroll: ({0:F2}/{1:F2},{2:F2}/{3:F2}) Scrollbar:{4}{5}", window.Scroll.x, window.ScrollMax.x, window.Scroll.y, window.ScrollMax.y, window.ScrollbarX ? "X" : "", window.ScrollbarY ? "Y" : "" );
		BulletText( "Active: {0}/{1}, WriteAccessed: {2}, BeginOrderWithinContext: {3}", window.Active, window.WasActive, window.WriteAccessed, window.BeginOrderWithinContext );
		BulletText( "Appearing: {0}, Hidden: {1} (CanSkip {2} Cannot {3}), SkipItems: {4}", window.Appearing, window.Hidden, window.HiddenFramesCanSkipItems, window.HiddenFramesCannotSkipItems, window.SkipItems );
		BulletText( "DrawList: {0} commands", window.DrawList.CommandCount );
		if ( window.ParentWindow is not null )
			BulletText( "ParentWindow: '{0}'", window.ParentWindow.Name );
		if ( window.DC.ChildWindows.Count > 0 && TreeNode( "ChildWindows", "Child windows ({0})", window.DC.ChildWindows.Count ) )
		{
			foreach ( var child in window.DC.ChildWindows )
				MetricsNodeWindow( child );
			TreePop();
		}
		TreePop();
	}

	/// <summary>Create an About window: version, credits.</summary>
	public static void ShowAboutWindow( ref bool open )
	{
		if ( !Begin( "About Dear ImGui", ref open, ImGuiWindowFlags.AlwaysAutoResize ) )
		{
			End();
			return;
		}
		Text( "Dear ImGui for s&box" );
		TextLinkOpenURL( "Source", "https://github.com/zeljkovranjes/sbox-imgui" );
		Separator();
		Text( "A C# port of the Dear ImGui 1.91 API by Omar Cornut and all Dear ImGui contributors," );
		Text( "rendered through s&box's Painter API. Original s&box library by Duccsoft." );
		Text( "Dear ImGui is licensed under the MIT License, see LICENSE for more information." );
		End();
	}

	/// <summary>Add a style selector combo (Dark / Light / Classic). Returns true when the style changed.</summary>
	public static bool ShowStyleSelector( string label )
	{
		string[] names = { "Dark", "Light", "Classic" };
		if ( _styleSelectorIdx < 0 ) _styleSelectorIdx = 0;
		if ( Combo( label, ref _styleSelectorIdx, names ) )
		{
			switch ( _styleSelectorIdx )
			{
				case 0: StyleColorsDark(); break;
				case 1: StyleColorsLight(); break;
				case 2: StyleColorsClassic(); break;
			}
			return true;
		}
		return false;
	}

	private static int _fontSelectorIdx;

	/// <summary>Add a font family selector combo for fonts shipped with s&amp;box.</summary>
	public static void ShowFontSelector( string label )
	{
		string[] fonts = { "Roboto Mono", "Roboto", "Inter", "Poppins", "Roboto Condensed" };
		var io = GetIO();
		_fontSelectorIdx = Math.Max( 0, Array.IndexOf( fonts, io.FontName ) );
		if ( Combo( label, ref _fontSelectorIdx, fonts ) )
			io.FontName = fonts[_fontSelectorIdx];
	}

	/// <summary>Add a style editor block (not a window). You can pass a style to edit; defaults to the current style.</summary>
	public static void ShowStyleEditor( ImGuiStyle style = null )
	{
		style ??= GetStyle();

		ShowStyleSelector( "Colors##Selector" );
		ShowFontSelector( "Fonts##Selector" );

		if ( SliderFloat( "FrameRounding", ref style.FrameRounding, 0.0f, 12.0f, "%.0f" ) )
			style.GrabRounding = style.FrameRounding;
		{
			bool border = style.WindowBorderSize > 0.0f;
			if ( Checkbox( "WindowBorder", ref border ) ) style.WindowBorderSize = border ? 1.0f : 0.0f;
		}
		SameLine();
		{
			bool border = style.FrameBorderSize > 0.0f;
			if ( Checkbox( "FrameBorder", ref border ) ) style.FrameBorderSize = border ? 1.0f : 0.0f;
		}
		SameLine();
		{
			bool border = style.PopupBorderSize > 0.0f;
			if ( Checkbox( "PopupBorder", ref border ) ) style.PopupBorderSize = border ? 1.0f : 0.0f;
		}
		Separator();

		if ( BeginTabBar( "##tabs" ) )
		{
			if ( BeginTabItem( "Sizes" ) )
			{
				SeparatorText( "Main" );
				SliderFloat2( "WindowPadding", ref style.WindowPadding, 0.0f, 20.0f, "%.0f" );
				SliderFloat2( "FramePadding", ref style.FramePadding, 0.0f, 20.0f, "%.0f" );
				SliderFloat2( "ItemSpacing", ref style.ItemSpacing, 0.0f, 20.0f, "%.0f" );
				SliderFloat2( "ItemInnerSpacing", ref style.ItemInnerSpacing, 0.0f, 20.0f, "%.0f" );
				SliderFloat2( "TouchExtraPadding", ref style.TouchExtraPadding, 0.0f, 10.0f, "%.0f" );
				SliderFloat( "IndentSpacing", ref style.IndentSpacing, 0.0f, 30.0f, "%.0f" );
				SliderFloat( "ScrollbarSize", ref style.ScrollbarSize, 1.0f, 20.0f, "%.0f" );
				SliderFloat( "GrabMinSize", ref style.GrabMinSize, 1.0f, 20.0f, "%.0f" );

				SeparatorText( "Borders" );
				SliderFloat( "WindowBorderSize", ref style.WindowBorderSize, 0.0f, 1.0f, "%.0f" );
				SliderFloat( "ChildBorderSize", ref style.ChildBorderSize, 0.0f, 1.0f, "%.0f" );
				SliderFloat( "PopupBorderSize", ref style.PopupBorderSize, 0.0f, 1.0f, "%.0f" );
				SliderFloat( "FrameBorderSize", ref style.FrameBorderSize, 0.0f, 1.0f, "%.0f" );
				SliderFloat( "TabBorderSize", ref style.TabBorderSize, 0.0f, 1.0f, "%.0f" );
				SliderFloat( "TabBarBorderSize", ref style.TabBarBorderSize, 0.0f, 2.0f, "%.0f" );

				SeparatorText( "Rounding" );
				SliderFloat( "WindowRounding", ref style.WindowRounding, 0.0f, 12.0f, "%.0f" );
				SliderFloat( "ChildRounding", ref style.ChildRounding, 0.0f, 12.0f, "%.0f" );
				SliderFloat( "FrameRounding", ref style.FrameRounding, 0.0f, 12.0f, "%.0f" );
				SliderFloat( "PopupRounding", ref style.PopupRounding, 0.0f, 12.0f, "%.0f" );
				SliderFloat( "ScrollbarRounding", ref style.ScrollbarRounding, 0.0f, 12.0f, "%.0f" );
				SliderFloat( "GrabRounding", ref style.GrabRounding, 0.0f, 12.0f, "%.0f" );
				SliderFloat( "TabRounding", ref style.TabRounding, 0.0f, 12.0f, "%.0f" );

				SeparatorText( "Widgets" );
				SliderFloat2( "WindowTitleAlign", ref style.WindowTitleAlign, 0.0f, 1.0f, "%.2f" );
				int windowMenuButtonPosition = (int)style.WindowMenuButtonPosition + 1;
				if ( Combo( "WindowMenuButtonPosition", ref windowMenuButtonPosition, "None\0Left\0Right\0" ) )
					style.WindowMenuButtonPosition = (ImGuiDir)(windowMenuButtonPosition - 1);
				SliderFloat2( "ButtonTextAlign", ref style.ButtonTextAlign, 0.0f, 1.0f, "%.2f" );
				SliderFloat2( "SelectableTextAlign", ref style.SelectableTextAlign, 0.0f, 1.0f, "%.2f" );
				SliderFloat( "SeparatorTextBorderSize", ref style.SeparatorTextBorderSize, 0.0f, 10.0f, "%.0f" );
				SliderFloat2( "SeparatorTextAlign", ref style.SeparatorTextAlign, 0.0f, 1.0f, "%.2f" );
				SliderFloat2( "SeparatorTextPadding", ref style.SeparatorTextPadding, 0.0f, 40.0f, "%.0f" );
				SliderFloat( "LogSliderDeadzone", ref style.LogSliderDeadzone, 0.0f, 12.0f, "%.0f" );
				SliderFloat( "ImageBorderSize", ref style.ImageBorderSize, 0.0f, 1.0f, "%.0f" );
				SliderFloat( "Alpha", ref style.Alpha, 0.2f, 1.0f, "%.2f" );
				SliderFloat( "DisabledAlpha", ref style.DisabledAlpha, 0.0f, 1.0f, "%.2f" );
				EndTabItem();
			}

			if ( BeginTabItem( "Colors" ) )
			{
				InputTextWithHint( "Filter colors", "e.g. Frame", ref _styleFilter );
				SetNextWindowSizeConstraints( new Vector2( 0.0f, GetTextLineHeightWithSpacing() * 10 ), new Vector2( float.MaxValue, float.MaxValue ) );
				BeginChild( "##colors", Vector2.Zero, ImGuiChildFlags.Borders | ImGuiChildFlags.NavFlattened, ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar );
				PushItemWidth( GetFontSize() * -12 );
				for ( int i = 0; i < (int)ImGuiCol.COUNT; i++ )
				{
					var name = GetStyleColorName( (ImGuiCol)i );
					if ( !string.IsNullOrEmpty( _styleFilter ) && !name.Contains( _styleFilter, StringComparison.OrdinalIgnoreCase ) )
						continue;
					PushID( i );
					ColorEdit4( "##color", ref style.Colors[i], ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf );
					SameLine( 0.0f, style.ItemInnerSpacing.x );
					TextUnformatted( name );
					PopID();
				}
				PopItemWidth();
				EndChild();
				EndTabItem();
			}

			if ( BeginTabItem( "Rendering" ) )
			{
				var io = GetIO();
				Checkbox( "io.AutoScale", ref io.AutoScale );
				DragFloat( "io.FontGlobalScale", ref io.FontGlobalScale, 0.005f, 0.3f, 3.0f, "%.2f" );
				DragFloat( "io.FontSize", ref io.FontSize, 0.1f, 6.0f, 48.0f, "%.1f" );
				DragFloat( "Global Alpha", ref style.Alpha, 0.005f, 0.20f, 1.0f, "%.2f" );
				EndTabItem();
			}
			EndTabBar();
		}
	}
}
