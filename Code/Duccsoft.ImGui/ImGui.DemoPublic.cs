namespace Duccsoft.ImGui;

public static partial class ImGui
{
	/// <summary>Example app from the demo: a console with history and completion.</summary>
	public static void ShowExampleAppConsole( ref bool open ) => Demo.ShowExampleAppConsole( ref open );
	/// <summary>Example app from the demo: a filtered, auto-scrolling log.</summary>
	public static void ShowExampleAppLog( ref bool open ) => Demo.ShowExampleAppLog( ref open );
	/// <summary>Example app from the demo: a two-column property editor built with a table.</summary>
	public static void ShowExampleAppPropertyEditor( ref bool open ) => Demo.ShowExampleAppPropertyEditor( ref open );
	/// <summary>Example app from the demo: tabbed documents with unsaved markers.</summary>
	public static void ShowExampleAppDocuments( ref bool open ) => Demo.ShowExampleAppDocuments( ref open );
	/// <summary>Example app from the demo: ImDrawList primitives and a canvas.</summary>
	public static void ShowExampleAppCustomRendering( ref bool open ) => Demo.ShowExampleAppCustomRendering( ref open );
	/// <summary>Example app from the demo: master/detail layout.</summary>
	public static void ShowExampleAppLayout( ref bool open ) => Demo.ShowExampleAppLayout( ref open );
	/// <summary>Example app from the demo: a full-screen main menu bar.</summary>
	public static void ShowExampleAppMainMenuBar() => Demo.ShowExampleAppMainMenuBar();
}
