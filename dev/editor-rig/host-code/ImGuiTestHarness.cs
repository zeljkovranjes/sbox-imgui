using Duccsoft.ImGui;

namespace ImGuiTests;

/// <summary>
/// Test-host component: draws a gallery of ImGui windows for screenshots, and can run the scripted
/// self-test (see <see cref="ImGuiSelfTest"/>). Lives in the scratch host project, not in the library.
/// </summary>
public sealed class ImGuiTestHarness : Component
{
	/// <summary>"gallery" (default), "demo", or "selftest". Can be changed at runtime with the imgui_test_mode convar.</summary>
	[ConVar( "imgui_test_mode" )]
	public static string Mode { get; set; } = "gallery";

	/// <summary>Index of the gallery page to show.</summary>
	[ConVar( "imgui_test_page" )]
	public static int Page { get; set; } = 0;

	private int _clicks;
	private bool _check = true;
	private int _radio = 1;
	private bool _demoOpen = true;

	protected override void OnUpdate()
	{
		Mouse.Visible = true;

		switch ( Mode )
		{
			case "demo":
				// ImGui.ShowDemoWindow( ref _demoOpen ); (enabled once the demo window exists)
				break;
			case "selftest":
				ImGuiSelfTest.Update();
				break;
			default:
				DrawBasicGallery();
				break;
		}
	}

	private void DrawBasicGallery()
	{
		ImGui.SetNextWindowPos( new Vector2( 40, 40 ), ImGuiCond.FirstUseEver );
		ImGui.SetNextWindowSize( new Vector2( 420, 360 ), ImGuiCond.FirstUseEver );
		if ( ImGui.Begin( "Basic Widgets" ) )
		{
			ImGui.Text( "Hello from Dear ImGui on s&box!" );
			ImGui.TextColored( new Vector4( 1, 0.8f, 0.2f, 1 ), "Colored text" );
			ImGui.TextDisabled( "Disabled text" );
			ImGui.BulletText( "Bullet text" );
			ImGui.SeparatorText( "Buttons" );
			if ( ImGui.Button( "Click me" ) )
				_clicks++;
			ImGui.SameLine();
			ImGui.Text( "Clicked {0} times", _clicks );
			ImGui.Checkbox( "Checkbox", ref _check );
			ImGui.RadioButton( "One", ref _radio, 0 ); ImGui.SameLine();
			ImGui.RadioButton( "Two", ref _radio, 1 ); ImGui.SameLine();
			ImGui.RadioButton( "Three", ref _radio, 2 );
			ImGui.ProgressBar( (Time.Now * 0.2f) % 1f );
			if ( ImGui.CollapsingHeader( "Collapsing header", ImGuiTreeNodeFlags.DefaultOpen ) )
			{
				if ( ImGui.TreeNode( "Tree node" ) )
				{
					ImGui.Selectable( "Selectable A" );
					ImGui.Selectable( "Selectable B", true );
					ImGui.TreePop();
				}
			}
		}
		ImGui.End();
	}
}
