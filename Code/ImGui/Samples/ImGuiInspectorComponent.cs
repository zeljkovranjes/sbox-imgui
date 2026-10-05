namespace Duccsoft.ImGui.Samples;

/// <summary>
/// Draws an ImGui inspector window for another component's [Property] members.
/// </summary>
[Title( "ImGui Inspector" ), Category( "ImGui" ), Icon( "manage_search" )]
public sealed class ImGuiInspectorComponent : Component
{
	/// <summary>The component to inspect.</summary>
	[Property] public Component Target { get; set; }

	protected override void OnUpdate()
	{
		Target?.ImGuiInspector();
	}
}
