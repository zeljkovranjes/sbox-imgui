using Duccsoft.ImGui.Engine;
using Duccsoft.ImGui.UI;
using Sandbox.Rendering;
using Sandbox.UI;

namespace Duccsoft.ImGui.Systems;

/// <summary>
/// Drives ImGui for a scene: starts a frame before components update, renders all windows after they update,
/// and feeds mouse/keyboard input through an invisible UI panel.
/// </summary>
[Alias( "Duccsoft.ImGui.ImGuiSystem" )]
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

	/// <summary>
	/// When true, the system stops reading the real mouse/keyboard and you drive input yourself through
	/// <see cref="ImGui.GetIO"/> (AddMousePosEvent, AddMouseButtonEvent, AddInputCharacter, AddKeyTyped...). Useful for automated tests.
	/// </summary>
	public bool SimulateInput { get; set; }

	/// <summary>The IO of this scene's ImGui context, usable outside of a frame (e.g. to inject input).</summary>
	public ImGuiIO IO => Context.IO;

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

		if ( !SimulateInput )
		{
			UpdateInputPanel();
			io.MousePos = Mouse.Active || Mouse.Visible ? Mouse.Position : new Vector2( -float.MaxValue, -float.MaxValue );
		}

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
		if ( !Game.IsPlaying || Current is { SimulateInput: true } )
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
