using Duccsoft.ImGui.Engine;
using Duccsoft.ImGui.Systems;
using Sandbox.Rendering;
using Sandbox.UI;

namespace Duccsoft.ImGui.UI;

/// <summary>
/// Invisible full-screen panel that receives mouse/keyboard events while ImGui wants them,
/// and lets them pass through to the game otherwise.
/// </summary>
internal sealed class ImGuiInputPanel : RootPanel
{
	private readonly ImGuiSystem _system;

	public ImGuiInputPanel( ImGuiSystem system )
	{
		_system = system;
		Style.PointerEvents = PointerEvents.None;
		Style.Position = PositionMode.Absolute;
		Style.Width = Length.Percent( 100 );
		Style.Height = Length.Percent( 100 );
		AcceptsFocus = true;
		RenderedManually = false;
	}

	public bool WantsKeyboard
	{
		get => _wantsKeyboard;
		set
		{
			if ( _wantsKeyboard == value )
				return;
			_wantsKeyboard = value;
			if ( value )
				Focus();
			else if ( HasFocus )
				Blur();
		}
	}
	private bool _wantsKeyboard;

	private ImGuiIO IO => _system.Context.IO;

	public override void Tick()
	{
		if ( !Game.IsPlaying )
			Delete();
	}

	public override void OnButtonEvent( ButtonEvent e )
	{
		switch ( e.Button )
		{
			case "mouseleft":
				IO.AddMouseButtonEvent( 0, e.Pressed );
				break;
			case "mouseright":
				IO.AddMouseButtonEvent( 1, e.Pressed );
				break;
			case "mousemiddle":
				IO.AddMouseButtonEvent( 2, e.Pressed );
				break;
			default:
				var key = ImGuiKeyNames.FromSboxName( e.Button );
				if ( key != ImGuiKey.None )
					IO.AddKeyEvent( key, e.Pressed );
				break;
		}

		if ( IO.WantCaptureMouse || IO.WantTextInput )
			e.StopPropagation = true;
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		var key = ImGuiKeyNames.FromSboxName( e.Button );
		if ( key != ImGuiKey.None )
			IO.AddKeyTyped( key, e.HasCtrl, e.HasShift, e.HasAlt );
		if ( IO.WantTextInput )
			e.StopPropagation = true;
	}

	public override void OnKeyTyped( char k )
	{
		if ( k < 32 && k != '\t' && k != '\n' )
			return;
		IO.AddInputCharacter( k );
	}

	public override void OnPaste( string text )
	{
		IO.AddPasteEvent( text );
	}

	public override string GetClipboardValue( bool cut )
	{
		return ImGuiSystem.ClipboardCopyProvider?.Invoke( cut );
	}

	public override void OnMouseWheel( Vector2 value )
	{
		IO.AddMouseWheelEvent( -value.x, -value.y );
	}
}
