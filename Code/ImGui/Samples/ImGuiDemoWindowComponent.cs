namespace Duccsoft.ImGui.Samples;

/// <summary>
/// Shows the built-in Dear ImGui demo, metrics, style editor and about windows. Drop it on any GameObject.
/// </summary>
[Title( "ImGui Demo Window" ), Category( "ImGui" ), Icon( "widgets" )]
public sealed class ImGuiDemoWindowComponent : Component
{
	[Property] public bool ShowDemoWindow { get; set; } = true;
	[Property] public bool ShowMetricsWindow { get; set; }
	[Property] public bool ShowStyleEditor { get; set; }
	[Property] public bool ShowAboutWindow { get; set; }
	/// <summary>Make the mouse cursor visible so the windows can be used.</summary>
	[Property] public bool ShowCursor { get; set; } = true;

	protected override void OnUpdate()
	{
		if ( ShowCursor )
			Mouse.Visible = true;

		if ( ShowDemoWindow )
		{
			bool open = true;
			ImGui.ShowDemoWindow( ref open );
			ShowDemoWindow = open;
		}
		if ( ShowMetricsWindow )
		{
			bool open = true;
			ImGui.ShowMetricsWindow( ref open );
			ShowMetricsWindow = open;
		}
		if ( ShowStyleEditor )
		{
			bool open = true;
			if ( ImGui.Begin( "Dear ImGui Style Editor", ref open ) )
				ImGui.ShowStyleEditor();
			ImGui.End();
			ShowStyleEditor = open;
		}
		if ( ShowAboutWindow )
		{
			bool open = true;
			ImGui.ShowAboutWindow( ref open );
			ShowAboutWindow = open;
		}
	}
}
