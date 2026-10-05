using Duccsoft.ImGui;
using System.Text;

namespace ImGuiTests;

/// <summary>
/// Scripted in-engine self-test. Each test draws its own window every frame and runs a coroutine that
/// injects simulated mouse/keyboard input through <see cref="ImGuiIO"/>, then asserts on the widget state.
/// Start from MCP with invoke_static ImGuiTests.ImGuiSelfTest.Start, poll ImGuiTests.ImGuiSelfTest.Report.
/// </summary>
public static partial class ImGuiSelfTest
{
	public sealed class TestCase
	{
		public string Name;
		public Action Draw;
		public Func<IEnumerator<object>> Run;
		public string Result; // null = not run, "PASS" or "FAIL: ..."
	}

	private static readonly List<TestCase> _tests = new();
	private static readonly Dictionary<string, ImRect> _rects = new();
	private static IEnumerator<object> _runner;
	private static int _current = -1;
	private static string _filter;
	private static bool _running;
	private static bool _finished;
	private static string _failure;

	public static bool IsRunning => _running;

	/// <summary>Start (or restart) the self-test. Optional filter: only tests whose name contains it.</summary>
	public static string Start( string filter = null )
	{
		_filter = string.IsNullOrWhiteSpace( filter ) ? null : filter;
		_tests.Clear();
		RegisterAll();
		if ( _filter is not null )
			_tests.RemoveAll( t => !t.Name.Contains( _filter, StringComparison.OrdinalIgnoreCase ) );
		_current = -1;
		_runner = null;
		_running = true;
		_finished = false;
		ImGuiTestHarness.Mode = "selftest";
		return $"started {_tests.Count} tests";
	}

	public static string Report()
	{
		var sb = new StringBuilder();
		int pass = _tests.Count( t => t.Result == "PASS" );
		int fail = _tests.Count( t => t.Result is not null && t.Result != "PASS" );
		sb.Append( $"{(_finished ? "DONE" : "RUNNING")} pass={pass} fail={fail} total={_tests.Count} updates={_updates} current={_current} mode={ImGuiTestHarness.Mode} diag={_diag} err={ImGuiTestHarness.LastError}\n" );
		foreach ( var t in _tests )
			sb.Append( $"{t.Result ?? "pending"} | {t.Name}\n" );
		return sb.ToString();
	}

	private static int _updates;
	private static string _diag;

	public static void Update()
	{
		_updates++;
		var system = ImGuiSystem.Current;
		if ( system is null )
		{
			_diag = "ImGuiSystem.Current is null";
			return;
		}

		if ( !_running )
		{
			system.SimulateInput = false;
			return;
		}
		system.SimulateInput = true;

		if ( _current >= 0 && _current < _tests.Count )
		{
			try
			{
				_tests[_current].Draw();
			}
			catch ( Exception e )
			{
				_failure ??= "draw threw " + e.GetType().Name + ": " + e.Message;
			}
		}

		// Advance the coroutine once per frame (after drawing, so rects are current).
		if ( _runner is null || _failure is not null || !StepRunner() )
		{
			if ( _current >= 0 && _current < _tests.Count && _tests[_current].Result is null )
				_tests[_current].Result = _failure is null ? "PASS" : "FAIL: " + _failure;

			_current++;
			_failure = null;
			_rects.Clear();
			ResetInput();
			if ( _current >= _tests.Count )
			{
				_running = false;
				_finished = true;
				system.SimulateInput = false;
				Log.Info( "[imgui-test] " + Report().Replace( "\n", " || " ) );
				return;
			}
			_runner = _tests[_current].Run();
		}
	}

	private static bool StepRunner()
	{
		try
		{
			return _runner.MoveNext();
		}
		catch ( Exception e )
		{
			_failure = "threw " + e.GetType().Name + ": " + e.Message;
			return false;
		}
	}

	#region Helpers for tests
	private static ImGuiIO IO => ImGuiSystem.Current.IO;

	private static void Add( string name, Action draw, Func<IEnumerator<object>> run )
		=> _tests.Add( new TestCase { Name = name, Draw = draw, Run = run } );

	/// <summary>Record the last item's rectangle under a key.</summary>
	private static void Rec( string key ) => _rects[key] = new ImRect( ImGui.GetItemRectMin(), ImGui.GetItemRectMax() );

	private static ImRect R( string key )
	{
		if ( !_rects.TryGetValue( key, out var r ) )
			throw new Exception( $"no rect recorded for '{key}'" );
		return r;
	}

	private static bool Has( string key ) => _rects.ContainsKey( key );

	private static void Check( bool condition, string message )
	{
		if ( !condition && _failure is null )
			_failure = message;
	}

	private static void ResetInput()
	{
		var io = IO;
		for ( int i = 0; i < 3; i++ )
			if ( io.MouseDown[i] )
				io.AddMouseButtonEvent( i, false );
		io.ClearInputKeys();
		io.AddMousePosEvent( 5, 5 );
	}

	/// <summary>Begin a test window at a fixed position, always on top.</summary>
	private static bool TestWindow( string name, Vector2 size, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		ImGui.SetNextWindowPos( new Vector2( 60, 60 ), ImGuiCond.Appearing );
		ImGui.SetNextWindowSize( size, ImGuiCond.Appearing );
		return ImGui.Begin( name, flags | ImGuiWindowFlags.NoSavedSettings );
	}

	private static IEnumerable<object> Frames( int n )
	{
		for ( int i = 0; i < n; i++ )
			yield return null;
	}

	private static IEnumerable<object> MoveTo( Vector2 pos )
	{
		IO.AddMousePosEvent( pos.x, pos.y );
		yield return null;
		yield return null;
	}

	private static IEnumerable<object> Click( Vector2 pos, int button = 0 )
	{
		IO.AddMousePosEvent( pos.x, pos.y );
		yield return null;
		yield return null;
		IO.AddMouseButtonEvent( button, true );
		yield return null;
		IO.AddMouseButtonEvent( button, false );
		yield return null;
		yield return null;
	}

	private static IEnumerable<object> DoubleClick( Vector2 pos )
	{
		IO.AddMousePosEvent( pos.x, pos.y );
		yield return null;
		IO.AddMouseButtonEvent( 0, true );
		IO.AddMouseButtonEvent( 0, false );
		IO.AddMouseButtonEvent( 0, true );
		IO.AddMouseButtonEvent( 0, false );
		foreach ( var f in Frames( 6 ) ) yield return f;
	}

	private static IEnumerable<object> Drag( Vector2 from, Vector2 to, int steps = 6 )
	{
		IO.AddMousePosEvent( from.x, from.y );
		yield return null;
		yield return null;
		IO.AddMouseButtonEvent( 0, true );
		yield return null;
		for ( int i = 1; i <= steps; i++ )
		{
			var p = Vector2.Lerp( from, to, i / (float)steps );
			IO.AddMousePosEvent( p.x, p.y );
			yield return null;
		}
		yield return null;
		IO.AddMouseButtonEvent( 0, false );
		yield return null;
		yield return null;
	}

	private static IEnumerable<object> Type( string text )
	{
		foreach ( var c in text )
		{
			IO.AddInputCharacter( c );
			yield return null;
		}
		yield return null;
	}

	private static IEnumerable<object> Key( ImGuiKey key, bool ctrl = false, bool shift = false )
	{
		IO.AddKeyTyped( key, ctrl, shift, false );
		IO.AddKeyEvent( key, true );
		yield return null;
		IO.AddKeyEvent( key, false );
		yield return null;
	}

	private static IEnumerable<object> Wheel( Vector2 pos, float y )
	{
		IO.AddMousePosEvent( pos.x, pos.y );
		yield return null;
		IO.AddMouseWheelEvent( 0, y );
		yield return null;
		yield return null;
	}
	#endregion

	/// <summary>Registers every test (split across partial files by widget family).</summary>
	private static void RegisterAll()
	{
		RegisterCoreTests();
		RegisterExtendedTests();
	}

	static partial void RegisterExtendedTestsImpl();
	private static void RegisterExtendedTests() => RegisterExtendedTestsImpl();
}
