using Duccsoft.ImGui.Rendering;
using Sandbox.Rendering;
using Sandbox.UI;

namespace Duccsoft.ImGui;

/// <summary>
/// Drives ImGui for a scene: starts a frame before components update, renders all windows after they update,
/// and feeds mouse/keyboard input through an invisible UI panel.
/// </summary>
public sealed class ImGuiSystem : GameObjectSystem<ImGuiSystem>
{
	[ConVar( "imgui_mouse_capture", Help = "Whether ImGui captures mouse input over its windows." )]
	public static bool EnableMouseCapture { get; set; } = true;

	[ConVar( "imgui_enabled", Help = "Whether ImGui windows are processed and drawn." )]
	public static bool Enabled { get; set; } = true;

	internal ImGuiContext Context { get; } = new();

	/// <summary>The command list that all ImGui drawing is recorded into, attached to the main camera.</summary>
	public CommandList MainCommandList { get; } = new( "ImGui" ) { Flags = CommandList.Flag.Hud };

	/// <summary>When false, ImGui runs (and receives input) but is not drawn, e.g. while you render it yourself.</summary>
	public bool RenderEnabled { get; set; } = true;

	private CameraComponent _targetCamera;
	private ImGuiInputPanel _inputPanel;
	private bool _frameStarted;

	public ImGuiSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, -100, StartUpdate, "ImGui NewFrame" );
		Listen( Stage.FinishUpdate, 100, FinishUpdate, "ImGui Render" );
	}

	/// <summary>Make this scene's context the current one, so ImGui calls can be made outside of the update loop.</summary>
	public void MakeCurrent() => ImGuiContext.Current = Context;

	private void StartUpdate()
	{
		_frameStarted = false;
		if ( !Game.IsPlaying || !Enabled )
			return;

		MakeCurrent();
		var io = Context.IO;
		io.DeltaTime = MathF.Max( RealTime.Delta, 0.0001f );

		UpdateInputPanel();
		io.MousePos = Mouse.Active || Mouse.Visible ? Mouse.Position : new Vector2( -float.MaxValue, -float.MaxValue );

		try
		{
			ImGui.NewFrame();
			_frameStarted = true;
		}
		catch ( Exception e )
		{
			Log.Error( e, $"ImGui NewFrame failed: {e.Message}" );
		}
	}

	private void FinishUpdate()
	{
		if ( !_frameStarted )
		{
			MainCommandList.Reset();
			return;
		}

		MakeCurrent();
		List<ImDrawList> drawLists;
		try
		{
			drawLists = ImGui.Render();
		}
		catch ( Exception e )
		{
			Log.Error( e, $"ImGui Render failed: {e.Message}" );
			return;
		}

		UpdateCursorAndCapture();
		UpdateTargetCamera();

		MainCommandList.Reset();
		if ( !RenderEnabled )
			return;

		using var painter = Painter.Begin( MainCommandList );
		foreach ( var dl in drawLists )
			dl.Render( painter );
	}

	private void UpdateTargetCamera()
	{
		var sceneCamera = Scene.Camera;
		if ( _targetCamera.IsValid() && _targetCamera == sceneCamera )
			return;

		if ( _targetCamera.IsValid() )
			_targetCamera.RemoveCommandList( MainCommandList );

		_targetCamera = null;
		if ( sceneCamera.IsValid() )
		{
			_targetCamera = sceneCamera;
			_targetCamera.AddCommandList( MainCommandList, Sandbox.Rendering.Stage.AfterUI, 10_000 );
		}
	}

	#region Input
	private void UpdateInputPanel()
	{
		if ( !EnableMouseCapture )
		{
			_inputPanel?.Delete();
			_inputPanel = null;
			return;
		}

		if ( _inputPanel is null || !_inputPanel.IsValid )
		{
			_inputPanel = new ImGuiInputPanel( this ) { Scene = Scene };
		}
	}

	private void UpdateCursorAndCapture()
	{
		if ( _inputPanel is null )
			return;

		var io = Context.IO;
		bool wantMouse = io.WantCaptureMouse || ImGui.IsAnyMouseDown() && Context.ActiveId != 0;
		_inputPanel.Style.PointerEvents = wantMouse ? PointerEvents.All : PointerEvents.None;
		_inputPanel.WantsKeyboard = io.WantTextInput;

		if ( io.ConfigDrawCursorShape )
		{
			_inputPanel.Style.Cursor = Context.MouseCursor switch
			{
				ImGuiMouseCursor.TextInput => "text",
				ImGuiMouseCursor.ResizeAll => "move",
				ImGuiMouseCursor.ResizeNS => "ns-resize",
				ImGuiMouseCursor.ResizeEW => "ew-resize",
				ImGuiMouseCursor.ResizeNESW => "nesw-resize",
				ImGuiMouseCursor.ResizeNWSE => "nwse-resize",
				ImGuiMouseCursor.Hand => "pointer",
				ImGuiMouseCursor.NotAllowed => "not-allowed",
				_ => null
			};
		}
	}

	private static readonly HashSet<string> _unpollableKeys = new();

	/// <summary>Poll a key's state through s&amp;box's keyboard input (used when the ImGui panel does not have focus).</summary>
	internal static bool PollKeyDown( ImGuiKey key )
	{
		if ( !Game.IsPlaying )
			return false;
		var name = ImGuiKeyNames.ToSboxName( key );
		if ( name is null || _unpollableKeys.Contains( name ) )
			return false;
		if ( key >= ImGuiKey.MouseLeft )
			return false;
		try
		{
			return Input.Keyboard.Down( name );
		}
		catch ( Exception )
		{
			_unpollableKeys.Add( name );
			return false;
		}
	}

	/// <summary>Text that should be copied when the user presses Ctrl+C/Ctrl+X while a text field is focused.</summary>
	internal static Func<bool, string> ClipboardCopyProvider { get; set; }
	#endregion
}

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
