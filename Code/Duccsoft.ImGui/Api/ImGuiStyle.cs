namespace Duccsoft.ImGui;

/// <summary>
/// All sizes are in pixels. The style is automatically scaled with the screen resolution when
/// <see cref="ImGuiIO.AutoScale"/> is enabled (see <see cref="ScaleAllSizes"/>).
/// </summary>
public class ImGuiStyle
{
	/// <summary>
	/// The scale factor that maps the reference 1080p layout to the current screen.
	/// </summary>
	public static float UIScale => MathF.Max( 0.25f, MathF.Min( Screen.Width, Screen.Height ) / 1080f );

	public float Alpha = 1.0f;
	public float DisabledAlpha = 0.60f;
	public Vector2 WindowPadding = new( 8, 8 );
	public float WindowRounding = 0.0f;
	public float WindowBorderSize = 1.0f;
	public float WindowBorderHoverPadding = 4.0f;
	public Vector2 WindowMinSize = new( 32, 32 );
	public Vector2 WindowTitleAlign = new( 0.0f, 0.5f );
	public ImGuiDir WindowMenuButtonPosition = ImGuiDir.Left;
	public float ChildRounding = 0.0f;
	public float ChildBorderSize = 1.0f;
	public float PopupRounding = 0.0f;
	public float PopupBorderSize = 1.0f;
	public Vector2 FramePadding = new( 4, 3 );
	public float FrameRounding = 0.0f;
	public float FrameBorderSize = 0.0f;
	public Vector2 ItemSpacing = new( 8, 4 );
	public Vector2 ItemInnerSpacing = new( 4, 4 );
	public Vector2 CellPadding = new( 4, 2 );
	public Vector2 TouchExtraPadding = new( 0, 0 );
	public float IndentSpacing = 21.0f;
	public float ColumnsMinSpacing = 6.0f;
	public float ScrollbarSize = 14.0f;
	public float ScrollbarRounding = 9.0f;
	public float GrabMinSize = 12.0f;
	public float GrabRounding = 0.0f;
	public float LogSliderDeadzone = 4.0f;
	public float ImageBorderSize = 0.0f;
	public float TabRounding = 5.0f;
	public float TabBorderSize = 0.0f;
	public float TabCloseButtonMinWidthSelected = -1.0f;
	public float TabCloseButtonMinWidthUnselected = 0.0f;
	public float TabBarBorderSize = 1.0f;
	public float TabBarOverlineSize = 1.0f;
	public float TableAngledHeadersAngle = 35.0f * (MathF.PI / 180.0f);
	public Vector2 TableAngledHeadersTextAlign = new( 0.5f, 0.0f );
	public ImGuiDir ColorButtonPosition = ImGuiDir.Right;
	public Vector2 ButtonTextAlign = new( 0.5f, 0.5f );
	public Vector2 SelectableTextAlign = new( 0.0f, 0.0f );
	public float SeparatorTextBorderSize = 3.0f;
	public Vector2 SeparatorTextAlign = new( 0.0f, 0.5f );
	public Vector2 SeparatorTextPadding = new( 20.0f, 3.0f );
	public Vector2 DisplayWindowPadding = new( 19, 19 );
	public Vector2 DisplaySafeAreaPadding = new( 3, 3 );
	public float MouseCursorScale = 1.0f;
	public bool AntiAliasedLines = true;
	public bool AntiAliasedFill = true;
	public float CircleTessellationMaxError = 0.30f;

	public float HoverStationaryDelay = 0.15f;
	public float HoverDelayShort = 0.15f;
	public float HoverDelayNormal = 0.40f;
	public ImGuiHoveredFlags HoverFlagsForTooltipMouse = ImGuiHoveredFlags.Stationary | ImGuiHoveredFlags.DelayShort | ImGuiHoveredFlags.AllowWhenDisabled;
	public ImGuiHoveredFlags HoverFlagsForTooltipNav = ImGuiHoveredFlags.NoSharedDelay | ImGuiHoveredFlags.DelayNormal | ImGuiHoveredFlags.AllowWhenDisabled;

	/// <summary>
	/// Style colors, indexed by <see cref="ImGuiCol"/>. Components are in [0,1].
	/// </summary>
	public Vector4[] Colors = new Vector4[(int)ImGuiCol.COUNT];

	public ImGuiStyle()
	{
		ImGui.StyleColorsDark( this );
	}

	public ref Vector4 this[ImGuiCol idx] => ref Colors[(int)idx];

	/// <summary>
	/// Scale all spacing/padding/thickness values by a factor. Colors are untouched.
	/// </summary>
	public void ScaleAllSizes( float scale )
	{
		WindowPadding = ImGui.ImTrunc( WindowPadding * scale );
		WindowRounding = ImGui.ImTrunc( WindowRounding * scale );
		WindowMinSize = ImGui.ImTrunc( WindowMinSize * scale );
		WindowBorderHoverPadding = ImGui.ImTrunc( WindowBorderHoverPadding * scale );
		ChildRounding = ImGui.ImTrunc( ChildRounding * scale );
		PopupRounding = ImGui.ImTrunc( PopupRounding * scale );
		FramePadding = ImGui.ImTrunc( FramePadding * scale );
		FrameRounding = ImGui.ImTrunc( FrameRounding * scale );
		ItemSpacing = ImGui.ImTrunc( ItemSpacing * scale );
		ItemInnerSpacing = ImGui.ImTrunc( ItemInnerSpacing * scale );
		CellPadding = ImGui.ImTrunc( CellPadding * scale );
		TouchExtraPadding = ImGui.ImTrunc( TouchExtraPadding * scale );
		IndentSpacing = ImGui.ImTrunc( IndentSpacing * scale );
		ColumnsMinSpacing = ImGui.ImTrunc( ColumnsMinSpacing * scale );
		ScrollbarSize = ImGui.ImTrunc( ScrollbarSize * scale );
		ScrollbarRounding = ImGui.ImTrunc( ScrollbarRounding * scale );
		GrabMinSize = ImGui.ImTrunc( GrabMinSize * scale );
		GrabRounding = ImGui.ImTrunc( GrabRounding * scale );
		LogSliderDeadzone = ImGui.ImTrunc( LogSliderDeadzone * scale );
		TabRounding = ImGui.ImTrunc( TabRounding * scale );
		if ( TabCloseButtonMinWidthSelected > 0 && TabCloseButtonMinWidthSelected != float.MaxValue )
			TabCloseButtonMinWidthSelected = ImGui.ImTrunc( TabCloseButtonMinWidthSelected * scale );
		if ( TabCloseButtonMinWidthUnselected > 0 && TabCloseButtonMinWidthUnselected != float.MaxValue )
			TabCloseButtonMinWidthUnselected = ImGui.ImTrunc( TabCloseButtonMinWidthUnselected * scale );
		TabBarOverlineSize = ImGui.ImTrunc( TabBarOverlineSize * scale );
		SeparatorTextPadding = ImGui.ImTrunc( SeparatorTextPadding * scale );
		DisplayWindowPadding = ImGui.ImTrunc( DisplayWindowPadding * scale );
		DisplaySafeAreaPadding = ImGui.ImTrunc( DisplaySafeAreaPadding * scale );
		MouseCursorScale = ImGui.ImTrunc( MouseCursorScale * scale );
	}

	/// <summary>
	/// Like <see cref="ScaleAllSizes"/> but without truncation, so that repeated rescaling does not drift.
	/// </summary>
	internal void ScaleAllSizesExact( float scale )
	{
		WindowPadding *= scale;
		WindowRounding *= scale;
		WindowMinSize *= scale;
		WindowBorderHoverPadding *= scale;
		ChildRounding *= scale;
		PopupRounding *= scale;
		FramePadding *= scale;
		FrameRounding *= scale;
		ItemSpacing *= scale;
		ItemInnerSpacing *= scale;
		CellPadding *= scale;
		TouchExtraPadding *= scale;
		IndentSpacing *= scale;
		ColumnsMinSpacing *= scale;
		ScrollbarSize *= scale;
		ScrollbarRounding *= scale;
		GrabMinSize *= scale;
		GrabRounding *= scale;
		LogSliderDeadzone *= scale;
		TabRounding *= scale;
		TabBarOverlineSize *= scale;
		SeparatorTextPadding *= scale;
		SeparatorTextBorderSize *= scale;
		DisplayWindowPadding *= scale;
		DisplaySafeAreaPadding *= scale;
		WindowBorderSize *= scale;
		ChildBorderSize *= scale;
		PopupBorderSize *= scale;
		FrameBorderSize *= scale;
		TabBorderSize *= scale;
		TabBarBorderSize *= scale;
		ImageBorderSize *= scale;
	}
}
