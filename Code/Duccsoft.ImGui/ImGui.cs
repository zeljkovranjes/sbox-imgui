using Duccsoft.ImGui.Engine;
using Duccsoft.ImGui.Systems;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Duccsoft.ImGui;

#region ImGui (Core/ImGui.DataFormat)

public static partial class ImGui
{
	private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

	/// <summary>
	/// Format a number with a printf-style format ("%.3f", "%d", "%.0f deg", "%08.3f", "%x", "%%") or,
	/// when the format contains no '%', a .NET numeric format string ("F3", "N2", "0.00").
	/// </summary>
	internal static string FormatNumber( string format, double value, bool isInteger )
	{
		if ( string.IsNullOrEmpty( format ) )
			format = isInteger ? "%d" : "%.3f";

		if ( format.IndexOf( '%' ) < 0 )
		{
			try
			{
				if ( isInteger )
					return ((long)value).ToString( format, Invariant );
				return value.ToString( format, Invariant );
			}
			catch ( FormatException )
			{
				return isInteger ? ((long)value).ToString( Invariant ) : value.ToString( "F3", Invariant );
			}
		}

		var sb = new StringBuilder( format.Length + 16 );
		int i = 0;
		while ( i < format.Length )
		{
			char c = format[i];
			if ( c != '%' )
			{
				sb.Append( c );
				i++;
				continue;
			}

			if ( i + 1 < format.Length && format[i + 1] == '%' )
			{
				sb.Append( '%' );
				i += 2;
				continue;
			}

			int start = i;
			i++;
			bool leftAlign = false, plus = false, space = false, zeroPad = false, alt = false;
			while ( i < format.Length )
			{
				char f = format[i];
				if ( f == '-' ) leftAlign = true;
				else if ( f == '+' ) plus = true;
				else if ( f == ' ' ) space = true;
				else if ( f == '0' ) zeroPad = true;
				else if ( f == '#' ) alt = true;
				else if ( f == '\'' ) { }
				else break;
				i++;
			}

			int width = 0;
			while ( i < format.Length && char.IsDigit( format[i] ) )
				width = width * 10 + (format[i++] - '0');

			int precision = -1;
			if ( i < format.Length && format[i] == '.' )
			{
				i++;
				precision = 0;
				while ( i < format.Length && char.IsDigit( format[i] ) )
					precision = precision * 10 + (format[i++] - '0');
			}

			// Length modifiers
			while ( i < format.Length && "hlLqjzt".IndexOf( format[i] ) >= 0 )
				i++;

			if ( i >= format.Length )
			{
				sb.Append( format, start, format.Length - start );
				break;
			}

			char conv = format[i++];
			string body;
			bool numeric = true;
			switch ( conv )
			{
				case 'd':
				case 'i':
				case 'u':
					{
						long l = (long)Math.Round( value, MidpointRounding.AwayFromZero );
						if ( isInteger ) l = (long)value;
						body = Math.Abs( (decimal)l ).ToString( Invariant );
						if ( precision > 0 ) body = body.PadLeft( precision, '0' );
						if ( l < 0 ) body = "-" + body;
						break;
					}
				case 'f':
				case 'F':
					body = FormatFixed( value, precision < 0 ? 6 : precision );
					break;
				case 'e':
				case 'E':
					body = FormatExponent( value, precision < 0 ? 6 : precision, conv == 'E' );
					break;
				case 'g':
				case 'G':
					{
						int p = precision < 0 ? 6 : (precision == 0 ? 1 : precision);
						body = value.ToString( "G" + p, Invariant );
						if ( body.Contains( 'E' ) )
						{
							// .NET uses E+006, C uses e+06
							int ePos = body.IndexOf( 'E' );
							string mant = body.Substring( 0, ePos );
							string exp = body.Substring( ePos + 1 );
							int expV = int.Parse( exp, Invariant );
							body = mant + (conv == 'G' ? "E" : "e") + (expV < 0 ? "-" : "+") + Math.Abs( expV ).ToString( "00", Invariant );
						}
						if ( alt && !body.Contains( '.' ) ) body += ".";
						break;
					}
				case 'x':
				case 'X':
					{
						long l = (long)value;
						body = l.ToString( conv == 'x' ? "x" : "X", Invariant );
						if ( precision > 0 ) body = body.PadLeft( precision, '0' );
						if ( alt && l != 0 ) body = (conv == 'x' ? "0x" : "0X") + body;
						break;
					}
				case 'o':
					body = Convert.ToString( (long)value, 8 );
					break;
				case 's':
					numeric = false;
					body = value.ToString( Invariant );
					break;
				default:
					numeric = false;
					body = format.Substring( start, i - start );
					break;
			}

			if ( numeric && !body.StartsWith( '-' ) )
			{
				if ( plus ) body = "+" + body;
				else if ( space ) body = " " + body;
			}

			if ( width > body.Length )
			{
				if ( leftAlign )
					body = body.PadRight( width );
				else if ( zeroPad && numeric )
				{
					int signLen = body.Length > 0 && (body[0] == '-' || body[0] == '+' || body[0] == ' ') ? 1 : 0;
					body = body.Substring( 0, signLen ) + body.Substring( signLen ).PadLeft( width - signLen, '0' );
				}
				else
					body = body.PadLeft( width );
			}

			sb.Append( body );
		}
		return sb.ToString();
	}

	private static string FormatFixed( double value, int precision )
	{
		if ( double.IsNaN( value ) ) return "nan";
		if ( double.IsInfinity( value ) ) return value > 0 ? "inf" : "-inf";
		precision = Math.Clamp( precision, 0, 15 );
		var s = value.ToString( "F" + precision, Invariant );
		// Avoid "-0.000"
		if ( s.StartsWith( '-' ) && s.Trim( '-', '0', '.' ).Length == 0 )
			s = s.Substring( 1 );
		return s;
	}

	private static string FormatExponent( double value, int precision, bool upper )
	{
		if ( value == 0 )
			return "0" + (precision > 0 ? "." + new string( '0', precision ) : "") + (upper ? "E+00" : "e+00");
		int exp = (int)Math.Floor( Math.Log10( Math.Abs( value ) ) );
		double mant = value / Math.Pow( 10, exp );
		var mantStr = mant.ToString( "F" + Math.Clamp( precision, 0, 15 ), Invariant );
		// Rounding may produce 10.000
		if ( Math.Abs( double.Parse( mantStr, Invariant ) ) >= 10.0 )
		{
			exp++;
			mant = value / Math.Pow( 10, exp );
			mantStr = mant.ToString( "F" + Math.Clamp( precision, 0, 15 ), Invariant );
		}
		return mantStr + (upper ? "E" : "e") + (exp < 0 ? "-" : "+") + Math.Abs( exp ).ToString( "00", Invariant );
	}

	/// <summary>Returns the precision of the first float conversion in a format string ("%.3f" = 3), or defaultPrecision.</summary>
	internal static int ParseFormatPrecision( string format, int defaultPrecision )
	{
		if ( string.IsNullOrEmpty( format ) )
			return defaultPrecision;

		int p = format.IndexOf( '%' );
		if ( p < 0 )
		{
			// .NET format: "F3", "N2", "0.000"
			if ( format.Length >= 2 && (format[0] == 'F' || format[0] == 'f' || format[0] == 'N' || format[0] == 'n') && int.TryParse( format.AsSpan( 1 ), out var np ) )
				return np;
			int dot = format.IndexOf( '.' );
			if ( dot >= 0 )
			{
				int count = 0;
				for ( int k = dot + 1; k < format.Length && (format[k] == '0' || format[k] == '#'); k++ ) count++;
				return count;
			}
			return defaultPrecision;
		}

		while ( p >= 0 && p + 1 < format.Length && format[p + 1] == '%' )
			p = format.IndexOf( '%', p + 2 );
		if ( p < 0 )
			return defaultPrecision;

		int i = p + 1;
		while ( i < format.Length && "-+ 0#'".IndexOf( format[i] ) >= 0 ) i++;
		while ( i < format.Length && char.IsDigit( format[i] ) ) i++;
		int precision = int.MaxValue;
		if ( i < format.Length && format[i] == '.' )
		{
			i++;
			precision = 0;
			while ( i < format.Length && char.IsDigit( format[i] ) )
				precision = precision * 10 + (format[i++] - '0');
		}
		while ( i < format.Length && "hlLqjzt".IndexOf( format[i] ) >= 0 ) i++;
		if ( i < format.Length )
		{
			char conv = format[i];
			if ( conv == 'e' || conv == 'E' )
				precision = -1;
			if ( (conv == 'g' || conv == 'G') && precision == int.MaxValue )
				precision = -1;
			if ( conv == 'd' || conv == 'i' || conv == 'u' || conv == 'x' || conv == 'X' )
				precision = 0;
		}
		return precision == int.MaxValue ? defaultPrecision : precision;
	}

	/// <summary>Round a floating point value to the precision shown by the format.</summary>
	internal static double RoundScalarWithFormat( string format, double value )
	{
		int precision = ParseFormatPrecision( format, 3 );
		if ( precision < 0 || precision > 15 )
			return value;
		if ( double.IsNaN( value ) || double.IsInfinity( value ) )
			return value;
		return Math.Round( value, precision, MidpointRounding.AwayFromZero );
	}

	/// <summary>Remove the prefix/suffix around the first printf conversion, e.g. "%.0f deg" -> "%.0f".</summary>
	internal static string ParseFormatTrimDecorations( string format )
	{
		if ( string.IsNullOrEmpty( format ) )
			return format;
		int p = format.IndexOf( '%' );
		while ( p >= 0 && p + 1 < format.Length && format[p + 1] == '%' )
			p = format.IndexOf( '%', p + 2 );
		if ( p < 0 )
			return format.IndexOf( '%' ) < 0 ? format : "";
		int i = p + 1;
		while ( i < format.Length && !char.IsLetter( format[i] ) ) i++;
		while ( i < format.Length && "hlLqjzt".IndexOf( format[i] ) >= 0 ) i++;
		if ( i < format.Length ) i++;
		return format.Substring( p, i - p );
	}

	internal static bool FormatIsHex( string format )
	{
		var trimmed = ParseFormatTrimDecorations( format );
		return !string.IsNullOrEmpty( trimmed ) && trimmed.Length > 1 && (trimmed[^1] == 'x' || trimmed[^1] == 'X');
	}

	/// <summary>Parse a number from text, tolerating surrounding decorations (e.g. "45 deg").</summary>
	internal static bool TryParseNumber( string text, out double value )
	{
		value = 0;
		if ( string.IsNullOrWhiteSpace( text ) )
			return false;
		text = text.Trim();
		if ( double.TryParse( text, NumberStyles.Float, Invariant, out value ) )
			return true;

		// Find the first numeric run
		int start = -1;
		for ( int k = 0; k < text.Length; k++ )
		{
			char c = text[k];
			if ( char.IsDigit( c ) || ((c == '-' || c == '+' || c == '.') && k + 1 < text.Length && (char.IsDigit( text[k + 1] ) || text[k + 1] == '.')) )
			{
				start = k;
				break;
			}
		}
		if ( start < 0 )
			return false;

		int end = start + 1;
		while ( end < text.Length && "0123456789.eE+-".IndexOf( text[end] ) >= 0 )
			end++;
		for ( int e = end; e > start; e-- )
		{
			if ( double.TryParse( text.AsSpan( start, e - start ), NumberStyles.Float, Invariant, out value ) )
				return true;
		}
		return false;
	}

	/// <summary>
	/// Apply user text to a numeric value. Supports "*2" and "/2" (relative to the old value), hex for hex formats, and plain numbers.
	/// Returns oldValue when the text cannot be parsed.
	/// </summary>
	internal static double ApplyExpression( string text, double oldValue, string format = null )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return oldValue;
		text = text.Trim();

		if ( text.Length > 1 && (text[0] == '*' || text[0] == '/') )
		{
			if ( TryParseNumber( text.Substring( 1 ), out var arg ) )
			{
				if ( text[0] == '*' ) return oldValue * arg;
				if ( arg != 0 ) return oldValue / arg;
			}
			return oldValue;
		}

		if ( format is not null && FormatIsHex( format ) )
		{
			var hex = text.StartsWith( "0x", StringComparison.OrdinalIgnoreCase ) ? text.Substring( 2 ) : text;
			if ( long.TryParse( hex, NumberStyles.HexNumber, Invariant, out var hv ) )
				return hv;
			return oldValue;
		}

		return TryParseNumber( text, out var v ) ? v : oldValue;
	}

	#region Generic numeric helpers
	internal static bool IsIntegerType<T>() where T : struct, INumber<T>
		=> T.CreateTruncating( 0.5 ) == T.Zero;

	internal static double ToDouble<T>( T v ) where T : struct, INumber<T> => double.CreateTruncating( v );

	internal static T FromDouble<T>( double v ) where T : struct, INumber<T>
	{
		if ( double.IsNaN( v ) )
			return T.Zero;
		if ( IsIntegerType<T>() )
			v = Math.Round( v, MidpointRounding.AwayFromZero );
		return T.CreateSaturating( v );
	}

	/// <summary>Minimum/maximum representable values for a numeric type, as doubles.</summary>
	internal static void GetTypeLimits<T>( out double min, out double max ) where T : struct, INumber<T>
	{
		if ( !IsIntegerType<T>() )
		{
			if ( typeof( T ) == typeof( float ) )
			{
				min = -float.MaxValue;
				max = float.MaxValue;
			}
			else
			{
				min = -double.MaxValue;
				max = double.MaxValue;
			}
			return;
		}
		min = double.CreateTruncating( T.CreateSaturating( double.MinValue ) );
		max = double.CreateTruncating( T.CreateSaturating( double.MaxValue ) );
	}
	#endregion
}

#endregion

#region ImGui (Core/ImGui.Frame)

public static partial class ImGui
{
	/// <summary>
	/// Start a new frame. Called automatically by <see cref="ImGuiSystem"/> at the start of every update.
	/// </summary>
	internal static void NewFrame()
	{
		var g = G;
		var io = g.IO;

		g.Time += io.DeltaTime;
		g.FrameCount++;
		g.TooltipOverrideCount = 0;
		g.WindowsActiveCount = 0;

		// Framerate
		g.FramerateSecPerFrameAccum += io.DeltaTime - g.FramerateSecPerFrame[g.FramerateSecPerFrameIdx];
		g.FramerateSecPerFrame[g.FramerateSecPerFrameIdx] = io.DeltaTime;
		g.FramerateSecPerFrameIdx = (g.FramerateSecPerFrameIdx + 1) % g.FramerateSecPerFrame.Length;
		g.FramerateSecPerFrameCount = Math.Min( g.FramerateSecPerFrameCount + 1, g.FramerateSecPerFrame.Length );
		io.Framerate = g.FramerateSecPerFrameAccum > 0.0f ? 1.0f / (g.FramerateSecPerFrameAccum / g.FramerateSecPerFrameCount) : float.MaxValue;

		UpdateFontsAndScale();

		// Active ID bookkeeping
		if ( g.ActiveId != 0 && g.ActiveIdIsAlive != g.ActiveId && g.ActiveIdPreviousFrame == g.ActiveId )
			ClearActiveID();
		if ( g.ActiveId != 0 )
			g.ActiveIdTimer += io.DeltaTime;
		g.LastActiveIdTimer += io.DeltaTime;
		g.ActiveIdPreviousFrame = g.ActiveId;
		g.ActiveIdPreviousFrameWindow = g.ActiveIdWindow;
		g.ActiveIdPreviousFrameHasBeenEditedBefore = g.ActiveIdHasBeenEditedBefore;
		g.ActiveIdIsAlive = 0;
		g.ActiveIdHasBeenEditedThisFrame = false;
		g.ActiveIdPreviousFrameIsAlive = false;
		g.ActiveIdIsJustActivated = false;

		// Hovered ID bookkeeping
		if ( g.HoveredId != 0 && g.HoveredId == g.HoveredIdPreviousFrame )
		{
			g.HoveredIdTimer += io.DeltaTime;
			if ( g.ActiveId != g.HoveredId )
				g.HoveredIdNotActiveTimer += io.DeltaTime;
		}
		else if ( g.HoveredId == 0 )
		{
			g.HoveredIdTimer = g.HoveredIdNotActiveTimer = 0f;
		}
		g.HoveredIdPreviousFrame = g.HoveredId;
		g.HoveredId = 0;
		g.HoveredIdAllowOverlap = false;
		g.HoveredIdDisabled = false;

		UpdateHoverDelay();

		g.FocusedTextInputRequestId = g.FocusRequestWindow is null ? g.FocusedTextInputRequestId : 0;
		g.FocusItemCounter = 0;

		// Drag and drop
		NewFrameDragDrop();

		// Inputs
		io.ProcessInputEvents();
		UpdateKeyboardInputs();
		UpdateMouseInputs();

		g.BackgroundDrawList.ResetForNewFrame();
		g.ForegroundDrawList.ResetForNewFrame();

		UpdateHoveredWindowAndCaptureFlags();
		UpdateMouseMovingWindowNewFrame();

		g.MouseCursor = ImGuiMouseCursor.Arrow;
		g.WantCaptureMouseNextFrame = g.WantCaptureKeyboardNextFrame = g.WantTextInputNextFrame = -1;

		UpdateMouseWheel();

		// Mark all windows as not visible
		foreach ( var window in g.Windows )
		{
			window.WasActive = window.Active;
			window.Active = false;
			window.WriteAccessed = false;
			window.BeginCount = 0;
		}

		// Closing the focused window restores focus to the top-most remaining window
		if ( g.NavWindow is not null && !g.NavWindow.WasActive )
			FocusTopMostWindowUnderOne( null, null );

		g.CurrentWindowStack.Clear();
		g.BeginPopupStack.Clear();
		g.ItemFlagsStack.Clear();
		g.CurrentItemFlags = ImGuiItemFlags.AutoClosePopups;
		g.GroupStack.Clear();
		g.ColorStack.Clear();
		g.StyleVarStack.Clear();
		g.DisabledStackSize = 0;
		g.FontStack.Clear();
		g.BeginMenuDepth = 0;
		g.BeginComboDepth = 0;
		g.MenusIdSubmittedThisFrame.Clear();
		NewFrameTables();

		g.WithinFrameScope = true;

		// Implicit "Debug" window, only rendered if something is submitted to it.
		g.WithinFrameScopeWithImplicitWindow = true;
		SetNextWindowSize( new Vector2( 400, 400 ) * g.AppliedStyleScale, ImGuiCond.FirstUseEver );
		Begin( "Debug##Default" );
	}

	private static void UpdateFontsAndScale()
	{
		var g = G;
		var io = g.IO;
		io.DisplaySize = new Vector2( Screen.Width, Screen.Height );
		if ( io.DisplaySize.x <= 0 || io.DisplaySize.y <= 0 )
			io.DisplaySize = new Vector2( 1920, 1080 );

		float targetScale = (io.AutoScale ? MathF.Max( 0.25f, MathF.Min( io.DisplaySize.x, io.DisplaySize.y ) / 1080f ) : 1f) * MathF.Max( 0.1f, io.FontGlobalScale );
		if ( MathF.Abs( targetScale - g.AppliedStyleScale ) > 0.0001f )
		{
			g.Style.ScaleAllSizesExact( targetScale / g.AppliedStyleScale );
			g.AppliedStyleScale = targetScale;
			g.TextSizeCache.Clear();
		}

		var fontPointSize = io.FontSize * targetScale;
		if ( g.FontName != io.FontName || g.FontPointSize != fontPointSize || g.FontWeight != io.FontWeight || g.FontBaseSize <= 0 )
		{
			g.FontName = io.FontName;
			g.FontWeight = io.FontWeight;
			g.FontPointSize = fontPointSize;
			g.FontBaseSize = MeasureLineHeight( fontPointSize, g.FontName, g.FontWeight );
			g.TextSizeCache.Clear();
		}
		g.FontSize = g.FontBaseSize;
	}

	private static void UpdateHoverDelay()
	{
		var g = G;
		if ( g.HoverItemDelayId != 0 && g.HoverItemDelayIdPreviousFrame == g.HoverItemDelayId )
		{
			g.HoverItemDelayTimer += g.IO.DeltaTime;
			g.HoverItemDelayClearTimer = 0.0f;
		}
		else
		{
			g.HoverItemDelayClearTimer += g.IO.DeltaTime;
			if ( g.HoverItemDelayClearTimer >= MathF.Max( 0.25f, g.IO.DeltaTime * 2.0f ) )
			{
				g.HoverItemDelayTimer = 0.0f;
				g.HoverItemDelayClearTimer = 0.0f;
			}
		}
		g.HoverItemDelayIdPreviousFrame = g.HoverItemDelayId;
		g.HoverItemDelayId = 0;
	}

	/// <summary>Find the window under the mouse, using the window rectangles of the previous frame.</summary>
	private static void FindHoveredWindow()
	{
		var g = G;
		ImGuiWindow hoveredWindow = null;
		ImGuiWindow hoveredWindowUnderMoving = null;

		if ( g.MovingWindow is not null && (g.MovingWindow.Flags & ImGuiWindowFlags.NoMouseInputs) == 0 )
			hoveredWindow = g.MovingWindow;

		var mousePos = g.IO.MousePos;
		var paddingRegular = new Vector2( WindowsHoverPadding, WindowsHoverPadding );
		foreach ( var window in g.WindowsHoverOrder )
		{
			if ( !window.WasActive || window.Hidden )
				continue;
			if ( (window.Flags & ImGuiWindowFlags.NoMouseInputs) != 0 )
				continue;

			var bb = window.OuterRectClipped;
			if ( (window.Flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)) == 0 )
				bb.Expand( paddingRegular );
			if ( !bb.Contains( mousePos ) )
				continue;

			if ( hoveredWindow is null )
				hoveredWindow = window;
			if ( hoveredWindowUnderMoving is null && (g.MovingWindow is null || window.RootWindow != g.MovingWindow.RootWindow) )
				hoveredWindowUnderMoving = window;
			if ( hoveredWindow is not null && hoveredWindowUnderMoving is not null )
				break;
		}

		g.HoveredWindow = hoveredWindow;
		g.HoveredWindowUnderMovingWindow = hoveredWindowUnderMoving;
	}

	private static void UpdateHoveredWindowAndCaptureFlags()
	{
		var g = G;
		var io = g.IO;

		FindHoveredWindow();

		// Modal windows prevent hovering behind them.
		var modalWindow = GetTopMostPopupModal();
		if ( modalWindow is not null && g.HoveredWindow is not null && !IsWindowWithinBeginStackOf( g.HoveredWindow.RootWindow, modalWindow ) )
			g.HoveredWindow = null;

		// Track click ownership: clicks that started outside of ImGui belong to the game.
		bool hasOpenPopup = g.OpenPopupStack.Count > 0;
		bool hasOpenModal = modalWindow is not null;
		int mouseEarliestDown = -1;
		bool mouseAnyDown = false;
		for ( int i = 0; i < io.MouseDown.Length; i++ )
		{
			if ( io.MouseClicked[i] )
				io.MouseDownOwned[i] = g.HoveredWindow is not null || hasOpenPopup;
			mouseAnyDown |= io.MouseDown[i];
			if ( io.MouseDown[i] && (mouseEarliestDown == -1 || io.MouseClickedTime[i] < io.MouseClickedTime[mouseEarliestDown]) )
				mouseEarliestDown = i;
		}
		bool mouseAvail = mouseEarliestDown == -1 || io.MouseDownOwned[mouseEarliestDown];
		bool draggingExternPayload = g.DragDropActive && (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceExtern) != 0;
		if ( !mouseAvail && !draggingExternPayload )
			g.HoveredWindow = g.HoveredWindowUnderMovingWindow = null;

		if ( g.WantCaptureMouseNextFrame != -1 )
			io.WantCaptureMouse = g.WantCaptureMouseNextFrame != 0;
		else
			io.WantCaptureMouse = (mouseAvail && (g.HoveredWindow is not null || mouseAnyDown)) || hasOpenModal;

		if ( g.WantCaptureKeyboardNextFrame != -1 )
			io.WantCaptureKeyboard = g.WantCaptureKeyboardNextFrame != 0;
		else
			io.WantCaptureKeyboard = g.ActiveId != 0 && IsTextInputActive() || modalWindow is not null;

		io.WantTextInput = g.WantTextInputNextFrame != -1 ? g.WantTextInputNextFrame != 0 : IsTextInputActive();
	}

	private static void UpdateMouseMovingWindowNewFrame()
	{
		var g = G;
		if ( g.MovingWindow is not null )
		{
			KeepAliveID( g.ActiveId );
			var movingWindow = g.MovingWindow.RootWindow;
			if ( g.IO.MouseDown[0] && IsMousePosValid( g.IO.MousePos ) )
			{
				var pos = g.IO.MousePos - g.ActiveIdClickOffset;
				SetWindowPos( movingWindow, pos );
				FocusWindow( g.MovingWindow );
			}
			else
			{
				StopMouseMovingWindow();
			}
		}
		else
		{
			if ( g.ActiveIdWindow is not null && g.ActiveIdWindow.MoveId == g.ActiveId )
			{
				KeepAliveID( g.ActiveId );
				if ( !g.IO.MouseDown[0] )
					ClearActiveID();
			}
		}
	}

	private static void UpdateMouseMovingWindowEndFrame()
	{
		var g = G;
		if ( g.ActiveId != 0 || g.HoveredId != 0 )
			return;
		if ( g.NavWindow is not null && g.NavWindow.Appearing )
			return;

		if ( g.IO.MouseClicked[0] )
		{
			var rootWindow = g.HoveredWindow?.RootWindow;
			bool isClosedPopup = rootWindow is not null && (rootWindow.Flags & ImGuiWindowFlags.Popup) != 0 && !IsPopupOpen( rootWindow.PopupId, ImGuiPopupFlags.AnyPopupLevel );

			if ( rootWindow is not null && !isClosedPopup )
			{
				StartMouseMovingWindow( g.HoveredWindow );
				if ( g.IO.ConfigWindowsMoveFromTitleBarOnly && (rootWindow.Flags & ImGuiWindowFlags.NoTitleBar) == 0 )
					if ( !rootWindow.TitleBarRect().Contains( g.IO.MouseClickedPos[0] ) )
						g.MovingWindow = null;
				if ( g.HoveredIdDisabled )
					g.MovingWindow = null;
			}
			else if ( rootWindow is null && g.NavWindow is not null && GetTopMostPopupModal() is null )
			{
				// Clicking on void removes focus (and closes popups).
				FocusWindow( null );
			}
		}

		if ( g.IO.MouseClicked[1] )
		{
			var modal = GetTopMostPopupModal();
			bool hoveredWindowAboveModal = g.HoveredWindow is not null && (modal is null || IsWindowAbove( g.HoveredWindow, modal ));
			ClosePopupsOverWindow( hoveredWindowAboveModal ? g.HoveredWindow : modal, true );
		}
	}

	internal static bool IsWindowAbove( ImGuiWindow potentialAbove, ImGuiWindow potentialBelow )
	{
		var g = G;
		int a = g.WindowsHoverOrder.IndexOf( potentialAbove.RootWindow );
		int b = g.WindowsHoverOrder.IndexOf( potentialBelow.RootWindow );
		if ( a < 0 ) return false;
		if ( b < 0 ) return true;
		return a < b;
	}

	private static void UpdateMouseWheel()
	{
		var g = G;
		var io = g.IO;
		float wheelY = io.MouseWheel;
		float wheelX = io.MouseWheelH;
		if ( wheelX == 0.0f && wheelY == 0.0f )
			return;

		var window = g.HoveredWindow;
		if ( window is null || window.Collapsed )
			return;

		// Ctrl + wheel scales the font of the hovered window.
		if ( io.KeyCtrl && !io.KeyShift && wheelY != 0f )
			return;

		if ( io.KeyShift && wheelX == 0.0f )
		{
			wheelX = wheelY;
			wheelY = 0.0f;
		}

		if ( wheelY != 0.0f )
		{
			var w = window;
			while ( (w.Flags & ImGuiWindowFlags.ChildWindow) != 0 && (w.ScrollMax.y == 0.0f || ((w.Flags & ImGuiWindowFlags.NoScrollWithMouse) != 0 && (w.Flags & ImGuiWindowFlags.NoMouseInputs) == 0)) )
				w = w.ParentWindow;
			if ( (w.Flags & ImGuiWindowFlags.NoScrollWithMouse) == 0 && (w.Flags & ImGuiWindowFlags.NoMouseInputs) == 0 )
			{
				float maxStep = w.InnerRect.Height * 0.67f;
				float scrollStep = ImTrunc( MathF.Min( 5 * w.CalcFontSize(), maxStep ) );
				SetScrollY( w, w.Scroll.y - wheelY * scrollStep );
			}
		}
		if ( wheelX != 0.0f )
		{
			var w = window;
			while ( (w.Flags & ImGuiWindowFlags.ChildWindow) != 0 && (w.ScrollMax.x == 0.0f || ((w.Flags & ImGuiWindowFlags.NoScrollWithMouse) != 0 && (w.Flags & ImGuiWindowFlags.NoMouseInputs) == 0)) )
				w = w.ParentWindow;
			if ( (w.Flags & ImGuiWindowFlags.NoScrollWithMouse) == 0 && (w.Flags & ImGuiWindowFlags.NoMouseInputs) == 0 )
			{
				float maxStep = w.InnerRect.Width * 0.67f;
				float scrollStep = ImTrunc( MathF.Min( 2 * w.CalcFontSize(), maxStep ) );
				SetScrollX( w, w.Scroll.x - wheelX * scrollStep );
			}
		}
	}

	/// <summary>
	/// End the frame. Called automatically by <see cref="ImGuiSystem"/>.
	/// </summary>
	internal static void EndFrame()
	{
		var g = G;
		if ( g.FrameCountEnded == g.FrameCount )
			return;
		if ( !g.WithinFrameScope )
			return;

		// Unwind windows the user forgot to End()
		while ( g.CurrentWindowStack.Count > 1 )
		{
			var w = g.CurrentWindow;
			Log.Warning( $"ImGui: missing End() for window '{w.Name}'" );
			if ( (w.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
				EndChild();
			else
				End();
		}

		EndFrameDragDrop();

		// Hide implicit "Debug" window if it hasn't been used
		g.WithinFrameScopeWithImplicitWindow = false;
		if ( g.CurrentWindow is not null && !g.CurrentWindow.WriteAccessed )
			g.CurrentWindow.Active = false;
		End();

		g.WithinFrameScope = false;
		g.FrameCountEnded = g.FrameCount;

		UpdateMouseMovingWindowEndFrame();

		// Close popups whose window stopped being submitted.
		for ( int n = 0; n < g.OpenPopupStack.Count; n++ )
		{
			var popup = g.OpenPopupStack[n];
			if ( popup.Window is not null && !popup.Window.Active && popup.OpenFrameCount < g.FrameCount )
			{
				ClosePopupToLevel( n, false );
				break;
			}
		}

		// Clear input data for next frame
		g.IO.InputQueueCharacters.Clear();
		g.IO.InputQueueKeys.Clear();
		g.IO.PastedText = null;
	}

	/// <summary>
	/// Build the ordered list of draw lists to render this frame (back to front), and update the hover order for next frame.
	/// </summary>
	internal static List<ImDrawList> Render()
	{
		var g = G;
		if ( g.FrameCountEnded != g.FrameCount )
			EndFrame();
		g.FrameCountRendered = g.FrameCount;

		var result = new List<ImDrawList>();
		var renderOrder = new List<ImGuiWindow>();

		void AddWindowRecursive( ImGuiWindow window )
		{
			if ( !IsWindowActiveAndVisible( window ) )
				return;
			renderOrder.Add( window );
			foreach ( var child in window.DC.ChildWindows )
				if ( (child.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) == 0 )
					AddWindowRecursive( child );
		}

		result.Add( g.BackgroundDrawList );

		// Layer 0: regular windows in z-order
		foreach ( var window in g.Windows )
		{
			if ( (window.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
				continue;
			if ( (window.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 )
				continue;
			AddWindowRecursive( window );
		}

		// Layer 1: popups, in the order they were opened (modal dim background behind the top-most modal)
		var modal = GetTopMostPopupModal();
		var renderedPopups = new HashSet<ImGuiWindow>();
		foreach ( var popup in g.OpenPopupStack )
		{
			var w = popup.Window;
			if ( w is null || !renderedPopups.Add( w ) )
				continue;
			if ( w == modal && IsWindowActiveAndVisible( w ) )
			{
				var dim = new ImDrawList( "##ModalDim" );
				dim.AddRectFilled( Vector2.Zero, g.IO.DisplaySize, GetColorU32Internal( ImGuiCol.ModalWindowDimBg ) );
				result.AddRange( renderOrder.Select( x => x.DrawList ) );
				renderOrder.Clear();
				result.Add( dim );
			}
			AddWindowRecursive( w );
		}

		// Layer 2: tooltips
		foreach ( var window in g.Windows )
		{
			if ( (window.Flags & ImGuiWindowFlags.Tooltip) != 0 && (window.Flags & ImGuiWindowFlags.ChildWindow) == 0 )
				AddWindowRecursive( window );
		}

		result.AddRange( renderOrder.Select( x => x.DrawList ) );
		result.Add( g.ForegroundDrawList );

		// Build front-to-back hover order for the next frame (children are in front of their parents).
		g.WindowsHoverOrder.Clear();
		var allOrdered = new List<ImGuiWindow>();
		foreach ( var window in g.Windows )
		{
			if ( (window.Flags & ImGuiWindowFlags.ChildWindow) != 0 ) continue;
			if ( (window.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 ) continue;
			CollectRecursive( window, allOrdered );
		}
		foreach ( var popup in g.OpenPopupStack )
			if ( popup.Window is not null && !allOrdered.Contains( popup.Window ) )
				CollectRecursive( popup.Window, allOrdered );
		for ( int i = allOrdered.Count - 1; i >= 0; i-- )
			g.WindowsHoverOrder.Add( allOrdered[i] );

		g.IO.MetricsRenderWindows = result.Count;
		g.IO.MetricsActiveWindows = g.WindowsActiveCount;
		return result;
	}

	private static void CollectRecursive( ImGuiWindow window, List<ImGuiWindow> output )
	{
		if ( !window.Active )
			return;
		output.Add( window );
		foreach ( var child in window.DC.ChildWindows )
			CollectRecursive( child, output );
	}

	internal static bool IsWindowActiveAndVisible( ImGuiWindow window ) => window.Active && !window.Hidden;

	/// <summary>Background draw list: drawn behind all windows.</summary>
	public static ImDrawList GetBackgroundDrawList() => G.BackgroundDrawList;

	/// <summary>Foreground draw list: drawn on top of all windows.</summary>
	public static ImDrawList GetForegroundDrawList() => G.ForegroundDrawList;

	public static double GetTime() => G.Time;
	public static int GetFrameCount() => G.FrameCount;

	public static ImGuiStyle GetStyle() => G.Style;
}

#endregion

#region ImGui (Core/ImGui.Internal)

public static partial class ImGui
{
	internal static ImGuiContext G => ImGuiContext.Current;

	#region Hashing
	/// <summary>
	/// FNV-1a hash of a string, seeded. Supports "###" to reset the hash so that only the part after "###" counts.
	/// </summary>
	internal static int ImHashStr( string str, int seed )
	{
		unchecked
		{
			uint crc = (uint)seed ^ 2166136261u;
			uint seedU = crc;
			if ( str is null )
				return (int)crc;

			for ( int i = 0; i < str.Length; i++ )
			{
				char c = str[i];
				if ( c == '#' && i + 2 < str.Length && str[i + 1] == '#' && str[i + 2] == '#' )
					crc = seedU;
				crc ^= c;
				crc *= 16777619u;
			}
			int result = (int)crc;
			return result == 0 ? 1 : result;
		}
	}

	internal static int ImHashInt( int value, int seed )
	{
		unchecked
		{
			uint crc = (uint)seed ^ 2166136261u;
			for ( int i = 0; i < 4; i++ )
			{
				crc ^= (uint)((value >> (i * 8)) & 0xFF);
				crc *= 16777619u;
			}
			// Distinguish int ids from string ids of the same bytes.
			crc ^= 0x9E3779B9u;
			crc *= 16777619u;
			int result = (int)crc;
			return result == 0 ? 1 : result;
		}
	}
	#endregion

	#region Text utilities
	/// <summary>Returns the index of the end of the visible label (before "##"), or text.Length.</summary>
	internal static int FindRenderedTextEnd( string text )
	{
		if ( text is null ) return 0;
		int idx = text.IndexOf( "##", StringComparison.Ordinal );
		return idx < 0 ? text.Length : idx;
	}

	/// <summary>Returns the visible part of a label, stripping everything from "##".</summary>
	internal static string LabelText( string label )
	{
		if ( label is null ) return string.Empty;
		int end = FindRenderedTextEnd( label );
		return end == label.Length ? label : label.Substring( 0, end );
	}

	/// <summary>Measure a single line of text at a given font point size, in pixels.</summary>
	internal static float MeasureTextWidth( string line, float pointSize )
	{
		if ( string.IsNullOrEmpty( line ) ) return 0f;
		var g = G;
		string font = g?.FontName ?? "Roboto Mono";
		int weight = g?.FontWeight ?? 400;

		if ( g is not null )
		{
			if ( g.TextSizeCache.Count > 4096 )
				g.TextSizeCache.Clear();
			var key = string.Concat( line, "\u0001", pointSize.ToString( "F2" ) );
			if ( g.TextSizeCache.TryGetValue( key, out var cached ) )
				return cached.x;
			var size = MeasureTextRaw( line, pointSize, font, weight );
			g.TextSizeCache[key] = size;
			return size.x;
		}
		return MeasureTextRaw( line, pointSize, font, weight ).x;
	}

	private static bool _textMeasureUnavailable;

	internal static Vector2 MeasureTextRaw( string text, float pointSize, string font, int weight )
	{
		if ( !_textMeasureUnavailable )
		{
			try
			{
				var scope = new TextRendering.Scope( text, Color.White, pointSize, font, weight );
				var size = scope.Measure();
				if ( size.y > 0f || text.Length == 0 )
					return size;
			}
			catch ( Exception )
			{
				// Text rendering is unavailable (e.g. headless unit tests). Fall back to a monospace estimate.
				_textMeasureUnavailable = true;
			}
		}
		return new Vector2( text.Length * pointSize * 0.6f, MathF.Ceiling( pointSize * 1.17f ) );
	}

	/// <summary>Measure the natural line height of the font at a given point size.</summary>
	internal static float MeasureLineHeight( float pointSize, string font, int weight )
	{
		var size = MeasureTextRaw( "Hgjy|", pointSize, font, weight );
		return MathF.Max( 1f, MathF.Ceiling( size.y ) );
	}

	/// <summary>
	/// Split text into lines that fit within wrapWidth (pixels), breaking on spaces where possible.
	/// </summary>
	internal static List<string> WrapText( string text, float wrapWidth, float pointSize )
	{
		var result = new List<string>();
		if ( text is null ) return result;
		var paragraphs = text.Split( '\n' );
		foreach ( var para in paragraphs )
		{
			if ( para.Length == 0 )
			{
				result.Add( string.Empty );
				continue;
			}

			int lineStart = 0;
			while ( lineStart < para.Length )
			{
				// Find the longest prefix that fits.
				int lastBreak = -1;
				int i = lineStart;
				int fitEnd = lineStart;
				while ( i < para.Length )
				{
					int next = i + 1;
					float w = MeasureTextWidth( para.Substring( lineStart, next - lineStart ), pointSize );
					if ( w > wrapWidth && next - lineStart > 1 )
						break;
					if ( para[i] == ' ' )
						lastBreak = i;
					fitEnd = next;
					i = next;
				}

				if ( fitEnd >= para.Length )
				{
					result.Add( para.Substring( lineStart ) );
					break;
				}

				int breakAt = lastBreak > lineStart ? lastBreak : fitEnd;
				result.Add( para.Substring( lineStart, breakAt - lineStart ).TrimEnd() );
				lineStart = breakAt;
				while ( lineStart < para.Length && para[lineStart] == ' ' )
					lineStart++;
			}
		}
		return result;
	}

	/// <summary>
	/// Calculate the size of a text, in pixels. Handles multiple lines and "##" hiding.
	/// </summary>
	public static Vector2 CalcTextSize( string text, bool hideTextAfterDoubleHash = false, float wrapWidth = -1.0f )
	{
		var g = G;
		if ( string.IsNullOrEmpty( text ) )
			return new Vector2( 0f, g?.FontSize ?? 0f );

		if ( hideTextAfterDoubleHash )
			text = LabelText( text );

		float lineHeight = g.FontSize;
		float pointSize = g.FontPointSize;
		if ( text.Length == 0 )
			return new Vector2( 0f, lineHeight );

		if ( wrapWidth > 0f )
		{
			var lines = WrapText( text, wrapWidth, pointSize );
			float maxW = 0f;
			foreach ( var l in lines )
				maxW = MathF.Max( maxW, MeasureTextWidth( l, pointSize ) );
			return new Vector2( MathF.Ceiling( maxW ), lines.Count * lineHeight );
		}

		float width = 0f;
		int lineCount = 0;
		int start = 0;
		while ( true )
		{
			int nl = text.IndexOf( '\n', start );
			int end = nl < 0 ? text.Length : nl;
			width = MathF.Max( width, MeasureTextWidth( text.Substring( start, end - start ), pointSize ) );
			lineCount++;
			if ( nl < 0 ) break;
			start = nl + 1;
		}
		return new Vector2( MathF.Ceiling( width ), lineCount * lineHeight );
	}

	/// <summary>Legacy signature kept for API compatibility.</summary>
	public static Vector2 CalcTextSize( string text, string textEnd, bool hideTextAfterDoubleHash = false, float wrapWidth = -1.0f )
		=> CalcTextSize( text, hideTextAfterDoubleHash, wrapWidth );

	internal static string Format( string fmt, params object[] args )
	{
		if ( args is null || args.Length == 0 )
			return fmt ?? string.Empty;
		try
		{
			return string.Format( fmt, args );
		}
		catch ( FormatException )
		{
			return fmt;
		}
	}
	#endregion

	#region Window / item helpers
	internal static ImGuiWindow GetCurrentWindow()
	{
		var g = G;
		g.CurrentWindow.WriteAccessed = true;
		return g.CurrentWindow;
	}

	internal static ImGuiWindow GetCurrentWindowRead() => G.CurrentWindow;

	internal static void SetActiveID( int id, ImGuiWindow window )
	{
		var g = G;
		g.ActiveIdIsJustActivated = g.ActiveId != id;
		if ( g.ActiveIdIsJustActivated )
		{
			if ( g.ActiveId != 0 )
				g.DeactivatedItemDataId = g.ActiveId;
			g.ActiveIdTimer = 0f;
			g.ActiveIdHasBeenPressedBefore = false;
			g.ActiveIdHasBeenEditedBefore = false;
			g.ActiveIdMouseButton = -1;
			if ( id != 0 )
			{
				g.LastActiveId = id;
				g.LastActiveIdTimer = 0f;
			}
		}
		g.ActiveId = id;
		g.ActiveIdAllowOverlap = false;
		g.ActiveIdNoClearOnFocusLoss = false;
		g.ActiveIdWindow = window;
		g.ActiveIdHasBeenEditedThisFrame = false;
		if ( id != 0 )
			g.ActiveIdIsAlive = id;
	}

	internal static void ClearActiveID() => SetActiveID( 0, null );

	internal static void SetHoveredID( int id )
	{
		var g = G;
		g.HoveredId = id;
		g.HoveredIdAllowOverlap = false;
		if ( id != 0 && g.HoveredIdPreviousFrame != id )
			g.HoveredIdTimer = g.HoveredIdNotActiveTimer = 0f;
	}

	internal static void KeepAliveID( int id )
	{
		var g = G;
		if ( g.ActiveId == id )
			g.ActiveIdIsAlive = id;
		if ( g.ActiveIdPreviousFrame == id )
			g.ActiveIdPreviousFrameIsAlive = true;
	}

	internal static void MarkItemEdited( int id )
	{
		var g = G;
		if ( g.ActiveId == id || g.ActiveId == 0 )
		{
			g.ActiveIdHasBeenEditedThisFrame = true;
			g.ActiveIdHasBeenEditedBefore = true;
		}
		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Edited;
	}

	/// <summary>
	/// Advance the layout cursor after an item of the given size. Text baseline is used to vertically align items on the same line.
	/// </summary>
	internal static void ItemSize( Vector2 size, float textBaselineY = -1.0f )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;

		var dc = window.DC;
		float offsetToMatchBaselineY = textBaselineY >= 0 ? MathF.Max( 0.0f, dc.CurrLineTextBaseOffset - textBaselineY ) : 0.0f;
		float lineY1 = dc.IsSameLine ? dc.CursorPosPrevLine.y : dc.CursorPos.y;
		float lineHeight = MathF.Max( dc.CurrLineSize.y, dc.CursorPos.y - lineY1 + size.y + offsetToMatchBaselineY );

		dc.CursorPosPrevLine = new Vector2( dc.CursorPos.x + size.x, lineY1 );
		dc.CursorPos = new Vector2(
			ImTrunc( window.Pos.x + dc.Indent + dc.ColumnsOffset ),
			ImTrunc( lineY1 + lineHeight + g.Style.ItemSpacing.y ) );
		dc.CursorMaxPos = new Vector2( MathF.Max( dc.CursorMaxPos.x, dc.CursorPosPrevLine.x ), MathF.Max( dc.CursorMaxPos.y, dc.CursorPos.y - g.Style.ItemSpacing.y ) );

		dc.PrevLineSize = new Vector2( dc.PrevLineSize.x, lineHeight );
		dc.CurrLineSize = new Vector2( dc.CurrLineSize.x, 0f );
		dc.PrevLineTextBaseOffset = MathF.Max( dc.CurrLineTextBaseOffset, textBaselineY );
		dc.CurrLineTextBaseOffset = 0.0f;
		dc.IsSameLine = dc.IsSetPos = false;

		if ( dc.LayoutType == ImGuiLayoutType.Horizontal )
			SameLine();
	}

	internal static void ItemSize( ImRect bb, float textBaselineY = -1.0f ) => ItemSize( bb.Size, textBaselineY );

	/// <summary>
	/// Declare an item: sets the last item data, handles clipping. Returns false if the item is clipped and should not be drawn.
	/// </summary>
	internal static bool ItemAdd( ImRect bb, int id, ImRect? navBb = null, ImGuiItemFlags extraFlags = ImGuiItemFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;

		g.LastItemData.ID = id;
		g.LastItemData.Rect = bb;
		g.LastItemData.NavRect = navBb ?? bb;
		g.LastItemData.InFlags = g.CurrentItemFlags | g.NextItemData.ItemFlags | extraFlags;
		g.LastItemData.StatusFlags = ImGuiItemStatusFlags.None;
		g.NextItemData.ClearFlags();

		if ( id != 0 )
		{
			KeepAliveID( id );

			// Keyboard focus requests (SetKeyboardFocusHere)
			if ( g.FocusRequestWindow == window && (g.LastItemData.InFlags & ImGuiItemFlags.NoTabStop) == 0 && (g.LastItemData.InFlags & ImGuiItemFlags.Inputable) != 0 )
			{
				if ( g.FocusItemCounter == g.FocusRequestCounter )
				{
					g.FocusedTextInputRequestId = id;
					g.FocusRequestWindow = null;
				}
				g.FocusItemCounter++;
			}
		}

		bool isRectVisible = bb.Overlaps( window.ClipRect );
		if ( !isRectVisible )
		{
			// Active items still need to process input even when scrolled out of view.
			if ( id == 0 || (id != g.ActiveId && id != g.FocusedTextInputRequestId) )
				return false;
		}

		if ( isRectVisible )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Visible;
		if ( IsMouseHoveringRect( bb.Min, bb.Max ) )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredRect;
		return true;
	}

	internal static bool IsClippedEx( ImRect bb, int id )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( !bb.Overlaps( window.ClipRect ) )
			if ( id == 0 || (id != g.ActiveId && id != g.ActiveIdPreviousFrame) )
				return true;
		return false;
	}

	internal static bool IsWindowWithinBeginStackOf( ImGuiWindow window, ImGuiWindow potentialParent )
	{
		if ( window.RootWindow == potentialParent )
			return true;
		while ( window is not null )
		{
			if ( window == potentialParent )
				return true;
			window = window.ParentWindowInBeginStack;
		}
		return false;
	}

	internal static bool IsWindowChildOf( ImGuiWindow window, ImGuiWindow potentialParent, bool popupHierarchy )
	{
		var windowRoot = popupHierarchy ? window.RootWindowPopupTree : window.RootWindow;
		if ( windowRoot == potentialParent )
			return true;
		while ( window is not null )
		{
			if ( window == potentialParent )
				return true;
			if ( window == windowRoot )
				return false;
			window = window.ParentWindow;
		}
		return false;
	}

	internal static ImGuiWindow GetTopMostPopupModal()
	{
		var g = G;
		for ( int n = g.OpenPopupStack.Count - 1; n >= 0; n-- )
		{
			var popup = g.OpenPopupStack[n].Window;
			if ( popup is not null && (popup.Flags & ImGuiWindowFlags.Modal) != 0 && (popup.Active || popup.WasActive) )
				return popup;
		}
		return null;
	}

	internal static bool IsWindowContentHoverable( ImGuiWindow window, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None )
	{
		var g = G;
		var focusedRoot = g.NavWindow?.RootWindow;
		if ( focusedRoot is not null && focusedRoot.WasActive && focusedRoot != window.RootWindow )
		{
			bool wantInhibit = false;
			if ( (focusedRoot.Flags & ImGuiWindowFlags.Modal) != 0 )
				wantInhibit = true;
			else if ( (focusedRoot.Flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiHoveredFlags.AllowWhenBlockedByPopup) == 0 )
				wantInhibit = true;

			if ( wantInhibit && !IsWindowWithinBeginStackOf( window.RootWindow, focusedRoot ) )
				return false;
		}
		return true;
	}

	/// <summary>
	/// Internal hover test used by all interactive widgets: checks window, clipping, overlap, active item and popups.
	/// </summary>
	internal static bool ItemHoverable( ImRect bb, int id, ImGuiItemFlags itemFlags )
	{
		var g = G;
		var window = g.CurrentWindow;

		if ( g.HoveredWindow != window )
			return false;
		if ( !IsMouseHoveringRect( bb.Min, bb.Max ) )
			return false;
		if ( g.HoveredId != 0 && g.HoveredId != id && !g.HoveredIdAllowOverlap )
			return false;
		// An item that allows overlap yields hover to whichever overlapping item was hovered last frame.
		if ( (itemFlags & ImGuiItemFlags.AllowOverlap) != 0 && id != 0 && g.HoveredIdPreviousFrame != id && g.HoveredIdPreviousFrame != 0 )
			return false;
		if ( g.ActiveId != 0 && g.ActiveId != id && !g.ActiveIdAllowOverlap )
			return false;
		if ( (itemFlags & ImGuiItemFlags.NoWindowHoverableCheck) == 0 && !IsWindowContentHoverable( window ) )
			return false;

		if ( (itemFlags & ImGuiItemFlags.Disabled) != 0 )
		{
			if ( g.ActiveId == id && id != 0 )
				ClearActiveID();
			g.HoveredIdDisabled = true;
			return false;
		}

		if ( id != 0 )
		{
			SetHoveredID( id );
			if ( (itemFlags & ImGuiItemFlags.AllowOverlap) != 0 )
				g.HoveredIdAllowOverlap = true;
		}
		return true;
	}

	internal static bool ButtonBehavior( ImRect bb, int id, out bool outHovered, out bool outHeld, ImGuiButtonFlags flags = ImGuiButtonFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;
		var io = g.IO;

		var itemFlags = g.LastItemData.ID == id ? g.LastItemData.InFlags : g.CurrentItemFlags;
		if ( (flags & ImGuiButtonFlags.AllowOverlap) != 0 )
			itemFlags |= ImGuiItemFlags.AllowOverlap;

		if ( (flags & ImGuiButtonFlags.MouseButtonMask_) == 0 )
			flags |= ImGuiButtonFlags.MouseButtonLeft;
		if ( (flags & ImGuiButtonFlags.PressedOnMask_) == 0 )
			flags |= ImGuiButtonFlags.PressedOnDefault_;

		// FlattenChildren: allow hovering when the hovered window is a child of this one
		var backupHoveredWindow = g.HoveredWindow;
		bool flattenHoveredChildren = (flags & ImGuiButtonFlags.FlattenChildren) != 0 && g.HoveredWindow is not null && g.HoveredWindow.RootWindow == window.RootWindow;
		if ( flattenHoveredChildren )
			g.HoveredWindow = window;

		bool pressed = false;
		bool hovered = ItemHoverable( bb, id, itemFlags );

		// Drag and drop: hovering a button with a payload for a while activates it.
		if ( g.DragDropActive && (flags & ImGuiButtonFlags.PressedOnDragDropHold) != 0 && (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceNoHoldToOpenOthers) == 0 )
		{
			if ( g.HoveredWindow == window && IsMouseHoveringRect( bb.Min, bb.Max ) )
			{
				hovered = true;
				SetHoveredID( id );
				if ( g.HoveredIdTimer - io.DeltaTime <= 0.70f && g.HoveredIdTimer >= 0.70f )
				{
					pressed = true;
					g.DragDropHoldJustPressedId = id;
					FocusWindow( window );
				}
			}
		}

		if ( flattenHoveredChildren )
			g.HoveredWindow = backupHoveredWindow;

		if ( hovered )
		{
			int mouseButtonClicked = -1;
			int mouseButtonReleased = -1;
			for ( int button = 0; button < 3; button++ )
			{
				if ( ((int)flags & ((int)ImGuiButtonFlags.MouseButtonLeft << button)) == 0 )
					continue;
				if ( io.MouseClicked[button] && mouseButtonClicked == -1 ) mouseButtonClicked = button;
				if ( io.MouseReleased[button] && mouseButtonReleased == -1 ) mouseButtonReleased = button;
			}

			bool keyModsOk = (flags & ImGuiButtonFlags.NoKeyModsAllowed) == 0 || !(io.KeyCtrl || io.KeyShift || io.KeyAlt);
			if ( keyModsOk )
			{
				if ( mouseButtonClicked != -1 && g.ActiveId != id )
				{
					if ( (flags & (ImGuiButtonFlags.PressedOnClickRelease | ImGuiButtonFlags.PressedOnClickReleaseAnywhere)) != 0 )
					{
						SetActiveID( id, window );
						g.ActiveIdMouseButton = mouseButtonClicked;
						if ( (flags & ImGuiButtonFlags.NoNavFocus) == 0 )
							g.NavId = id;
						FocusWindow( window );
					}
					if ( (flags & ImGuiButtonFlags.PressedOnClick) != 0 || ((flags & ImGuiButtonFlags.PressedOnDoubleClick) != 0 && io.MouseClickedCount[mouseButtonClicked] == 2) )
					{
						pressed = true;
						if ( (flags & ImGuiButtonFlags.NoHoldingActiveId) != 0 )
							ClearActiveID();
						else
							SetActiveID( id, window );
						g.ActiveIdMouseButton = mouseButtonClicked;
						if ( (flags & ImGuiButtonFlags.NoNavFocus) == 0 )
							g.NavId = id;
						FocusWindow( window );
					}
				}
				if ( (flags & ImGuiButtonFlags.PressedOnRelease) != 0 && mouseButtonReleased != -1 )
				{
					bool hasRepeatedAtLeastOnce = (itemFlags & ImGuiItemFlags.ButtonRepeat) != 0 && io.MouseDownDurationPrev[mouseButtonReleased] >= io.KeyRepeatDelay;
					if ( !hasRepeatedAtLeastOnce )
						pressed = true;
					if ( (flags & ImGuiButtonFlags.NoNavFocus) == 0 )
						g.NavId = id;
					ClearActiveID();
				}

				// Repeat mode
				if ( g.ActiveId == id && (itemFlags & ImGuiItemFlags.ButtonRepeat) != 0 && g.ActiveIdMouseButton >= 0 )
					if ( io.MouseDownDuration[g.ActiveIdMouseButton] > 0f && IsMouseClicked( (ImGuiMouseButton)g.ActiveIdMouseButton, true ) )
						pressed = true;
			}
		}

		bool held = false;
		if ( g.ActiveId == id )
		{
			if ( g.ActiveIdIsJustActivated )
				g.ActiveIdClickOffset = io.MousePos - bb.Min;

			int mouseButton = g.ActiveIdMouseButton;
			if ( mouseButton == -1 )
			{
				// Activated by something else (e.g. from code); release on mouse up.
				if ( !io.MouseDown[0] )
					ClearActiveID();
			}
			else if ( io.MouseDown[mouseButton] )
			{
				held = true;
			}
			else
			{
				bool releaseIn = hovered && (flags & ImGuiButtonFlags.PressedOnClickRelease) != 0;
				bool releaseAnywhere = (flags & ImGuiButtonFlags.PressedOnClickReleaseAnywhere) != 0;
				if ( (releaseIn || releaseAnywhere) && !g.DragDropActive )
				{
					bool isDoubleClickRelease = (flags & ImGuiButtonFlags.PressedOnDoubleClick) != 0 && io.MouseReleased[mouseButton] && io.MouseClickedLastCount[mouseButton] == 2;
					bool isRepeatingAlready = (itemFlags & ImGuiItemFlags.ButtonRepeat) != 0 && io.MouseDownDurationPrev[mouseButton] >= io.KeyRepeatDelay;
					if ( !isDoubleClickRelease && !isRepeatingAlready )
						pressed = true;
				}
				ClearActiveID();
			}
		}

		if ( pressed )
			g.ActiveIdHasBeenPressedBefore = true;

		outHovered = hovered;
		outHeld = held;
		return pressed;
	}

	internal static float CalcWrapWidthForPos( Vector2 pos, float wrapPosX )
	{
		if ( wrapPosX < 0.0f )
			return 0.0f;

		var g = G;
		var window = g.CurrentWindow;
		if ( wrapPosX == 0.0f )
			wrapPosX = window.WorkRect.Max.x;
		else if ( wrapPosX > 0.0f )
			wrapPosX += window.Pos.x - window.Scroll.x;

		return MathF.Max( wrapPosX - pos.x, 1.0f );
	}

	/// <summary>
	/// Calculate an item size from a requested size: 0 = default, &lt;0 = relative to the right edge of the content region.
	/// </summary>
	internal static Vector2 CalcItemSize( Vector2 size, float defaultW, float defaultH )
	{
		var window = G.CurrentWindow;
		Vector2 regionMax = default;
		if ( size.x < 0.0f || size.y < 0.0f )
			regionMax = GetContentRegionMaxAbs();

		if ( size.x == 0.0f )
			size.x = defaultW;
		else if ( size.x < 0.0f )
			size.x = MathF.Max( 4.0f, regionMax.x - window.DC.CursorPos.x + size.x );

		if ( size.y == 0.0f )
			size.y = defaultH;
		else if ( size.y < 0.0f )
			size.y = MathF.Max( 4.0f, regionMax.y - window.DC.CursorPos.y + size.y );

		return size;
	}

	internal static Vector2 GetContentRegionMaxAbs()
	{
		var window = G.CurrentWindow;
		var mx = window.ContentRegionRect.Max;
		if ( window.DC.CurrentColumns is not null || window.DC.CurrentTableIdx >= 0 )
			mx.x = window.WorkRect.Max.x;
		return mx;
	}

	/// <summary>Width of the next item, from SetNextItemWidth / PushItemWidth.</summary>
	public static float CalcItemWidth()
	{
		var g = G;
		var window = g.CurrentWindow;
		float w;
		if ( (g.NextItemData.Flags & ImGuiNextItemDataFlags.HasWidth) != 0 )
			w = g.NextItemData.Width;
		else
			w = window.DC.ItemWidth;

		if ( w < 0.0f )
		{
			float regionMaxX = GetContentRegionMaxAbs().x;
			w = MathF.Max( 1.0f, regionMaxX - window.DC.CursorPos.x + w );
		}
		return ImTrunc( w );
	}
	#endregion

	#region Render helpers
	internal static Color32 GetColorU32Internal( ImGuiCol idx, float alphaMul = 1.0f )
	{
		var g = G;
		var c = g.Style.Colors[(int)idx];
		c.w *= g.Style.Alpha * alphaMul;
		return ColorConvertFloat4ToU32( c );
	}

	internal static void RenderText( Vector2 pos, string text, bool hideTextAfterHash = true )
	{
		var window = G.CurrentWindow;
		if ( string.IsNullOrEmpty( text ) ) return;
		if ( hideTextAfterHash )
			text = LabelText( text );
		if ( text.Length == 0 ) return;
		window.DrawList.AddText( 0f, pos, GetColorU32Internal( ImGuiCol.Text ), text );
	}

	internal static void RenderTextWrapped( Vector2 pos, string text, float wrapWidth )
	{
		var window = G.CurrentWindow;
		if ( string.IsNullOrEmpty( text ) ) return;
		window.DrawList.AddText( 0f, pos, GetColorU32Internal( ImGuiCol.Text ), text, wrapWidth );
	}

	/// <summary>
	/// Render text aligned within a rect, clipped to clipRect (or the rect itself).
	/// </summary>
	internal static void RenderTextClipped( Vector2 posMin, Vector2 posMax, string text, Vector2? textSizeIfKnown, Vector2 align, ImRect? clipRect = null )
	{
		if ( string.IsNullOrEmpty( text ) ) return;
		text = LabelText( text );
		if ( text.Length == 0 ) return;
		var window = G.CurrentWindow;
		RenderTextClippedEx( window.DrawList, posMin, posMax, text, textSizeIfKnown, align, clipRect );
	}

	internal static void RenderTextClippedEx( ImDrawList drawList, Vector2 posMin, Vector2 posMax, string text, Vector2? textSizeIfKnown, Vector2 align, ImRect? clipRect, Color32? color = null )
	{
		var textSize = textSizeIfKnown ?? CalcTextSize( text );
		var pos = posMin;
		var clipMin = clipRect?.Min ?? posMin;
		var clipMax = clipRect?.Max ?? posMax;
		bool needClipping = pos.x + textSize.x >= clipMax.x || pos.y + textSize.y >= clipMax.y;
		if ( clipRect.HasValue )
			needClipping |= pos.x < clipMin.x || pos.y < clipMin.y;

		if ( align.x > 0.0f ) pos.x = MathF.Max( pos.x, pos.x + (posMax.x - pos.x - textSize.x) * align.x );
		if ( align.y > 0.0f ) pos.y = MathF.Max( pos.y, pos.y + (posMax.y - pos.y - textSize.y) * align.y );

		var col = color ?? GetColorU32Internal( ImGuiCol.Text );
		if ( needClipping )
			drawList.AddText( 0f, ImFloor( pos ), col, text, 0f, new ImRect( clipMin, clipMax ) );
		else
			drawList.AddText( 0f, ImFloor( pos ), col, text );
	}

	/// <summary>
	/// Render text clipped to posMax.x, replacing the end with an ellipsis if it does not fit.
	/// </summary>
	internal static void RenderTextEllipsis( ImDrawList drawList, Vector2 posMin, Vector2 posMax, float clipMaxX, float ellipsisMaxX, string text, Vector2? textSizeIfKnown )
	{
		text = LabelText( text );
		var textSize = textSizeIfKnown ?? CalcTextSize( text );
		if ( textSize.x > posMax.x - posMin.x )
		{
			const string ellipsis = "...";
			float ellipsisWidth = CalcTextSize( ellipsis ).x;
			float available = MathF.Max( 0f, ellipsisMaxX - posMin.x - ellipsisWidth );
			int len = text.Length;
			while ( len > 0 && CalcTextSize( text.Substring( 0, len ) ).x > available )
				len--;
			var clipped = text.Substring( 0, len ).TrimEnd() + ellipsis;
			RenderTextClippedEx( drawList, posMin, new Vector2( clipMaxX, posMax.y ), clipped, null, Vector2.Zero, null );
		}
		else
		{
			RenderTextClippedEx( drawList, posMin, new Vector2( clipMaxX, posMax.y ), text, textSize, Vector2.Zero, null );
		}
	}

	internal static void RenderFrame( Vector2 pMin, Vector2 pMax, Color32 fillCol, bool border = true, float rounding = 0.0f )
	{
		var g = G;
		var window = g.CurrentWindow;
		window.DrawList.AddRectFilled( pMin, pMax, fillCol, rounding );
		float borderSize = g.Style.FrameBorderSize;
		if ( border && borderSize > 0.0f )
		{
			window.DrawList.AddRect( pMin + Vector2.One, pMax + Vector2.One, GetColorU32Internal( ImGuiCol.BorderShadow ), rounding, ImDrawFlags.None, borderSize );
			window.DrawList.AddRect( pMin, pMax, GetColorU32Internal( ImGuiCol.Border ), rounding, ImDrawFlags.None, borderSize );
		}
	}

	internal static void RenderFrameBorder( Vector2 pMin, Vector2 pMax, float rounding = 0.0f )
	{
		var g = G;
		var window = g.CurrentWindow;
		float borderSize = g.Style.FrameBorderSize;
		if ( borderSize > 0.0f )
		{
			window.DrawList.AddRect( pMin + Vector2.One, pMax + Vector2.One, GetColorU32Internal( ImGuiCol.BorderShadow ), rounding, ImDrawFlags.None, borderSize );
			window.DrawList.AddRect( pMin, pMax, GetColorU32Internal( ImGuiCol.Border ), rounding, ImDrawFlags.None, borderSize );
		}
	}

	/// <summary>Render an arrow (triangle) pointing in a direction. scale 1.0 = font size.</summary>
	internal static void RenderArrow( ImDrawList drawList, Vector2 pos, Color32 col, ImGuiDir dir, float scale = 1.0f )
	{
		var g = G;
		float h = g.FontSize;
		float r = h * 0.40f * scale;
		var center = pos + new Vector2( h * 0.50f, h * 0.50f * scale );

		Vector2 a, b, c;
		switch ( dir )
		{
			case ImGuiDir.Up:
			case ImGuiDir.Down:
				if ( dir == ImGuiDir.Up ) r = -r;
				a = new Vector2( +0.000f, +0.750f ) * r;
				b = new Vector2( -0.866f, -0.750f ) * r;
				c = new Vector2( +0.866f, -0.750f ) * r;
				break;
			case ImGuiDir.Left:
			case ImGuiDir.Right:
				if ( dir == ImGuiDir.Left ) r = -r;
				a = new Vector2( +0.750f, +0.000f ) * r;
				b = new Vector2( -0.750f, +0.866f ) * r;
				c = new Vector2( -0.750f, -0.866f ) * r;
				break;
			default:
				return;
		}
		drawList.AddTriangleFilled( center + a, center + b, center + c, col );
	}

	internal static void RenderBullet( ImDrawList drawList, Vector2 pos, Color32 col )
	{
		drawList.AddCircleFilled( pos, G.FontSize * 0.20f, col );
	}

	internal static void RenderCheckMark( ImDrawList drawList, Vector2 pos, Color32 col, float sz )
	{
		float thickness = MathF.Max( sz / 5.0f, 1.0f );
		sz -= thickness * 0.5f;
		pos += new Vector2( thickness * 0.25f, thickness * 0.25f );

		float third = sz / 3.0f;
		float bx = pos.x + third;
		float by = pos.y + sz - third * 0.5f;
		drawList.AddPolyline( new[]
		{
			new Vector2( bx - third, by - third ),
			new Vector2( bx, by ),
			new Vector2( bx + third * 2.0f, by - third * 2.0f ),
		}, col, ImDrawFlags.None, thickness );
	}

	internal static void RenderRectFilledRangeH( ImDrawList drawList, ImRect rect, Color32 col, float xStartNorm, float xEndNorm, float rounding )
	{
		if ( xEndNorm == xStartNorm ) return;
		if ( xStartNorm > xEndNorm ) (xStartNorm, xEndNorm) = (xEndNorm, xStartNorm);
		var p0 = new Vector2( ImLerp( rect.Min.x, rect.Max.x, xStartNorm ), rect.Min.y );
		var p1 = new Vector2( ImLerp( rect.Min.x, rect.Max.x, xEndNorm ), rect.Max.y );
		drawList.PushClipRect( p0, p1, true );
		drawList.AddRectFilled( rect.Min, rect.Max, col, rounding );
		drawList.PopClipRect();
	}

	/// <summary>Checkerboard background for colors with alpha.</summary>
	internal static void RenderColorRectWithAlphaCheckerboard( ImDrawList drawList, Vector2 pMin, Vector2 pMax, Color32 col, float gridStep, Vector2 gridOff, float rounding = 0.0f, ImDrawFlags flags = ImDrawFlags.None )
	{
		if ( col.a < 255 )
		{
			var colBg1 = new Color32( 204, 204, 204, 255 );
			var colBg2 = new Color32( 128, 128, 128, 255 );
			drawList.AddRectFilled( pMin, pMax, colBg1, rounding, flags );
			drawList.PushClipRect( pMin, pMax, true );
			int yi = 0;
			for ( float y = pMin.y + gridOff.y; y < pMax.y; y += gridStep, yi++ )
			{
				float y1 = Math.Clamp( y, pMin.y, pMax.y ), y2 = MathF.Min( y + gridStep, pMax.y );
				if ( y2 <= y1 ) continue;
				for ( float x = pMin.x + gridOff.x + (yi & 1) * gridStep; x < pMax.x; x += gridStep * 2.0f )
				{
					float x1 = Math.Clamp( x, pMin.x, pMax.x ), x2 = MathF.Min( x + gridStep, pMax.x );
					if ( x2 <= x1 ) continue;
					drawList.AddRectFilled( new Vector2( x1, y1 ), new Vector2( x2, y2 ), colBg2 );
				}
			}
			drawList.PopClipRect();
		}
		drawList.AddRectFilled( pMin, pMax, col, rounding, flags );
	}
	#endregion
}

#endregion

#region ImGui (Core/ImGui.Math)

public static partial class ImGui
{
	#region Math helpers
	internal static Vector2 ImMin( Vector2 a, Vector2 b ) => new( MathF.Min( a.x, b.x ), MathF.Min( a.y, b.y ) );
	internal static Vector2 ImMax( Vector2 a, Vector2 b ) => new( MathF.Max( a.x, b.x ), MathF.Max( a.y, b.y ) );
	internal static Vector2 ImClamp( Vector2 v, Vector2 mn, Vector2 mx ) => new( Math.Clamp( v.x, mn.x, MathF.Max( mn.x, mx.x ) ), Math.Clamp( v.y, mn.y, MathF.Max( mn.y, mx.y ) ) );
	internal static float ImClamp( float v, float mn, float mx ) => v < mn ? mn : (v > mx ? mx : v);
	internal static int ImClamp( int v, int mn, int mx ) => v < mn ? mn : (v > mx ? mx : v);
	internal static float ImSaturate( float f ) => f < 0f ? 0f : (f > 1f ? 1f : f);
	internal static float ImLerp( float a, float b, float t ) => a + (b - a) * t;
	internal static Vector2 ImLerp( Vector2 a, Vector2 b, float t ) => a + (b - a) * t;
	internal static Vector2 ImLerp( Vector2 a, Vector2 b, Vector2 t ) => new( a.x + (b.x - a.x) * t.x, a.y + (b.y - a.y) * t.y );
	internal static float ImTrunc( float f ) => MathF.Truncate( f );
	internal static Vector2 ImTrunc( Vector2 v ) => new( MathF.Truncate( v.x ), MathF.Truncate( v.y ) );
	internal static float ImFloor( float f ) => MathF.Floor( f );
	internal static Vector2 ImFloor( Vector2 v ) => new( MathF.Floor( v.x ), MathF.Floor( v.y ) );
	internal static float ImLengthSqr( Vector2 v ) => v.x * v.x + v.y * v.y;
	internal static Vector2 ImMul( Vector2 a, Vector2 b ) => new( a.x * b.x, a.y * b.y );
	internal static float ImLinearSweep( float current, float target, float speed )
	{
		if ( current < target ) return MathF.Min( current + speed, target );
		if ( current > target ) return MathF.Max( current - speed, target );
		return current;
	}
	internal static float ImRound( float f ) => MathF.Round( f );
	#endregion

	#region Color helpers
	/// <summary>
	/// Convert an RGBA color with components in [0,1] to a <see cref="Color32"/>.
	/// </summary>
	public static Color32 ColorConvertFloat4ToU32( Vector4 col )
	{
		return new Color32(
			(byte)(ImSaturate( col.x ) * 255f + 0.5f),
			(byte)(ImSaturate( col.y ) * 255f + 0.5f),
			(byte)(ImSaturate( col.z ) * 255f + 0.5f),
			(byte)(ImSaturate( col.w ) * 255f + 0.5f) );
	}

	public static Vector4 ColorConvertU32ToFloat4( Color32 col )
		=> new( col.r / 255f, col.g / 255f, col.b / 255f, col.a / 255f );

	public static void ColorConvertRGBtoHSV( float r, float g, float b, out float h, out float s, out float v )
	{
		float k = 0f;
		if ( g < b )
		{
			(g, b) = (b, g);
			k = -1f;
		}
		if ( r < g )
		{
			(r, g) = (g, r);
			k = -2f / 6f - k;
		}
		float chroma = r - (g < b ? g : b);
		h = MathF.Abs( k + (g - b) / (6f * chroma + 1e-20f) );
		s = chroma / (r + 1e-20f);
		v = r;
	}

	public static void ColorConvertHSVtoRGB( float h, float s, float v, out float r, out float g, out float b )
	{
		if ( s == 0f )
		{
			r = g = b = v;
			return;
		}

		h = (h % 1f) / (60f / 360f);
		if ( h < 0 ) h += 6f;
		int i = (int)h;
		float f = h - i;
		float p = v * (1f - s);
		float q = v * (1f - s * f);
		float t = v * (1f - s * (1f - f));

		switch ( i )
		{
			case 0: r = v; g = t; b = p; break;
			case 1: r = q; g = v; b = p; break;
			case 2: r = p; g = v; b = t; break;
			case 3: r = p; g = q; b = v; break;
			case 4: r = t; g = p; b = v; break;
			default: r = v; g = p; b = q; break;
		}
	}

	internal static Color32 ColorWithAlpha( Color32 col, float alphaMul )
		=> col with { a = (byte)Math.Clamp( col.a * alphaMul, 0f, 255f ) };

	internal static Color ToColor( Color32 c ) => new( c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f );
	#endregion
}

#endregion

#region ImGui (Demo/ImGui.Demo)

/// <summary>
/// Port of Dear ImGui's demo window (imgui_demo.cpp). Call ImGui.ShowDemoWindow() from any component's OnUpdate.
/// </summary>
public static partial class ImGui
{
	private static partial class Demo
	{
		// Window toggles
		public static bool ShowAppMainMenuBar;
		public static bool ShowAppConsole;
		public static bool ShowAppLog;
		public static bool ShowAppLayout;
		public static bool ShowAppPropertyEditor;
		public static bool ShowAppLongText;
		public static bool ShowAppAutoResize;
		public static bool ShowAppConstrainedResize;
		public static bool ShowAppSimpleOverlay;
		public static bool ShowAppFullscreen;
		public static bool ShowAppWindowTitles;
		public static bool ShowAppCustomRendering;
		public static bool ShowAppDocuments;
		public static bool ShowMetrics;
		public static bool ShowStyleEditor;
		public static bool ShowAbout;

		// Window flags
		public static bool NoTitlebar, NoScrollbar, NoMenu, NoMove, NoResize, NoCollapse, NoClose, NoNav, NoBackground, NoBringToFront, UnsavedDocument;
	}

	/// <summary>Create a demo window demonstrating most ImGui features.</summary>
	public static void ShowDemoWindow()
	{
		bool open = true;
		ShowDemoWindow( ref open );
	}

	/// <summary>Create a demo window demonstrating most ImGui features. Pass a bool to get a close button.</summary>
	public static void ShowDemoWindow( ref bool open )
	{
		// Closed with the X button: stay closed until the caller sets open = true again.
		if ( !open )
			return;

		if ( Demo.ShowAppMainMenuBar ) Demo.ShowExampleAppMainMenuBar();
		if ( Demo.ShowAppConsole ) Demo.ShowExampleAppConsole( ref Demo.ShowAppConsole );
		if ( Demo.ShowAppLog ) Demo.ShowExampleAppLog( ref Demo.ShowAppLog );
		if ( Demo.ShowAppLayout ) Demo.ShowExampleAppLayout( ref Demo.ShowAppLayout );
		if ( Demo.ShowAppPropertyEditor ) Demo.ShowExampleAppPropertyEditor( ref Demo.ShowAppPropertyEditor );
		if ( Demo.ShowAppLongText ) Demo.ShowExampleAppLongText( ref Demo.ShowAppLongText );
		if ( Demo.ShowAppAutoResize ) Demo.ShowExampleAppAutoResize( ref Demo.ShowAppAutoResize );
		if ( Demo.ShowAppConstrainedResize ) Demo.ShowExampleAppConstrainedResize( ref Demo.ShowAppConstrainedResize );
		if ( Demo.ShowAppSimpleOverlay ) Demo.ShowExampleAppSimpleOverlay( ref Demo.ShowAppSimpleOverlay );
		if ( Demo.ShowAppFullscreen ) Demo.ShowExampleAppFullscreen( ref Demo.ShowAppFullscreen );
		if ( Demo.ShowAppWindowTitles ) Demo.ShowExampleAppWindowTitles();
		if ( Demo.ShowAppCustomRendering ) Demo.ShowExampleAppCustomRendering( ref Demo.ShowAppCustomRendering );
		if ( Demo.ShowAppDocuments ) Demo.ShowExampleAppDocuments( ref Demo.ShowAppDocuments );

		if ( Demo.ShowMetrics ) ShowMetricsWindow( ref Demo.ShowMetrics );
		if ( Demo.ShowAbout ) ShowAboutWindow( ref Demo.ShowAbout );
		if ( Demo.ShowStyleEditor )
		{
			if ( Begin( "Dear ImGui Style Editor", ref Demo.ShowStyleEditor ) )
				ShowStyleEditor();
			End();
		}

		var windowFlags = ImGuiWindowFlags.None;
		if ( Demo.NoTitlebar ) windowFlags |= ImGuiWindowFlags.NoTitleBar;
		if ( Demo.NoScrollbar ) windowFlags |= ImGuiWindowFlags.NoScrollbar;
		if ( !Demo.NoMenu ) windowFlags |= ImGuiWindowFlags.MenuBar;
		if ( Demo.NoMove ) windowFlags |= ImGuiWindowFlags.NoMove;
		if ( Demo.NoResize ) windowFlags |= ImGuiWindowFlags.NoResize;
		if ( Demo.NoCollapse ) windowFlags |= ImGuiWindowFlags.NoCollapse;
		if ( Demo.NoNav ) windowFlags |= ImGuiWindowFlags.NoNav;
		if ( Demo.NoBackground ) windowFlags |= ImGuiWindowFlags.NoBackground;
		if ( Demo.NoBringToFront ) windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus;
		if ( Demo.UnsavedDocument ) windowFlags |= ImGuiWindowFlags.UnsavedDocument;

		var g = G;
		SetNextWindowPos( new Vector2( 650, 20 ) * g.AppliedStyleScale, ImGuiCond.FirstUseEver );
		SetNextWindowSize( new Vector2( 550, 680 ) * g.AppliedStyleScale, ImGuiCond.FirstUseEver );

		bool visible = Demo.NoClose ? Begin( "Dear ImGui Demo", windowFlags ) : Begin( "Dear ImGui Demo", ref open, windowFlags );
		if ( !visible )
		{
			End();
			return;
		}

		PushItemWidth( GetFontSize() * -12 );

		if ( BeginMenuBar() )
		{
			if ( BeginMenu( "Menu" ) )
			{
				Demo.ShowExampleMenuFile();
				EndMenu();
			}
			if ( BeginMenu( "Examples" ) )
			{
				MenuItem( "Main menu bar", null, ref Demo.ShowAppMainMenuBar );
				SeparatorText( "Mini apps" );
				MenuItem( "Console", null, ref Demo.ShowAppConsole );
				MenuItem( "Documents", null, ref Demo.ShowAppDocuments );
				MenuItem( "Log", null, ref Demo.ShowAppLog );
				MenuItem( "Property editor", null, ref Demo.ShowAppPropertyEditor );
				MenuItem( "Simple layout", null, ref Demo.ShowAppLayout );
				SeparatorText( "Concepts" );
				MenuItem( "Auto-resizing window", null, ref Demo.ShowAppAutoResize );
				MenuItem( "Constrained-resizing window", null, ref Demo.ShowAppConstrainedResize );
				MenuItem( "Custom rendering", null, ref Demo.ShowAppCustomRendering );
				MenuItem( "Fullscreen window", null, ref Demo.ShowAppFullscreen );
				MenuItem( "Long text display", null, ref Demo.ShowAppLongText );
				MenuItem( "Manipulating window titles", null, ref Demo.ShowAppWindowTitles );
				MenuItem( "Simple overlay", null, ref Demo.ShowAppSimpleOverlay );
				EndMenu();
			}
			if ( BeginMenu( "Tools" ) )
			{
				MenuItem( "Metrics/Debugger", null, ref Demo.ShowMetrics );
				MenuItem( "Style Editor", null, ref Demo.ShowStyleEditor );
				MenuItem( "About Dear ImGui", null, ref Demo.ShowAbout );
				EndMenu();
			}
			EndMenuBar();
		}

		Text( "dear imgui says hello! (s&box port, Dear ImGui 1.91 API)" );
		Spacing();

		if ( CollapsingHeader( "Help" ) )
		{
			SeparatorText( "ABOUT THIS DEMO:" );
			BulletText( "Sections below are demonstrating many aspects of the library." );
			BulletText( "The \"Examples\" menu above leads to more demo contents." );
			BulletText( "The \"Tools\" menu above gives access to: About Box, Style Editor,\nand Metrics/Debugger (general purpose Dear ImGui debugging tool)." );
			SeparatorText( "PROGRAMMER GUIDE:" );
			BulletText( "See the ShowDemoWindow() code in Code/ImGui/Demo. <- you are here!" );
			BulletText( "See docs.md in the repository for usage of every widget." );
			SeparatorText( "USER GUIDE:" );
			ShowUserGuide();
		}

		if ( CollapsingHeader( "Configuration" ) )
		{
			var io = GetIO();
			if ( TreeNode( "Configuration##2" ) )
			{
				SeparatorText( "General" );
				Checkbox( "io.AutoScale", ref io.AutoScale );
				SetItemTooltip( "Scale the UI with the screen resolution (reference 1080p)." );
				DragFloat( "io.FontGlobalScale", ref io.FontGlobalScale, 0.005f, 0.3f, 3.0f, "%.2f" );
				DragFloat( "io.FontSize", ref io.FontSize, 0.1f, 6f, 48f, "%.1f" );
				Checkbox( "io.ConfigWindowsResizeFromEdges", ref io.ConfigWindowsResizeFromEdges );
				Checkbox( "io.ConfigWindowsMoveFromTitleBarOnly", ref io.ConfigWindowsMoveFromTitleBarOnly );
				Checkbox( "io.ConfigInputTextCursorBlink", ref io.ConfigInputTextCursorBlink );
				Checkbox( "io.ConfigDrawCursorShape", ref io.ConfigDrawCursorShape );
				DragFloat( "io.MouseDoubleClickTime", ref io.MouseDoubleClickTime, 0.005f, 0.1f, 1.0f, "%.2f s" );
				DragFloat( "io.MouseDragThreshold", ref io.MouseDragThreshold, 0.1f, 0.0f, 20.0f, "%.1f px" );
				DragFloat( "io.KeyRepeatDelay", ref io.KeyRepeatDelay, 0.005f, 0.05f, 1.0f, "%.3f s" );
				DragFloat( "io.KeyRepeatRate", ref io.KeyRepeatRate, 0.005f, 0.01f, 1.0f, "%.3f s" );
				TreePop();
				Spacing();
			}
			if ( TreeNode( "Style" ) )
			{
				ShowStyleEditor();
				TreePop();
				Spacing();
			}
		}

		if ( CollapsingHeader( "Window options" ) )
		{
			if ( BeginTable( "split", 3 ) )
			{
				TableNextColumn(); Checkbox( "No titlebar", ref Demo.NoTitlebar );
				TableNextColumn(); Checkbox( "No scrollbar", ref Demo.NoScrollbar );
				TableNextColumn(); Checkbox( "No menu", ref Demo.NoMenu );
				TableNextColumn(); Checkbox( "No move", ref Demo.NoMove );
				TableNextColumn(); Checkbox( "No resize", ref Demo.NoResize );
				TableNextColumn(); Checkbox( "No collapse", ref Demo.NoCollapse );
				TableNextColumn(); Checkbox( "No close", ref Demo.NoClose );
				TableNextColumn(); Checkbox( "No nav", ref Demo.NoNav );
				TableNextColumn(); Checkbox( "No background", ref Demo.NoBackground );
				TableNextColumn(); Checkbox( "No bring to front", ref Demo.NoBringToFront );
				TableNextColumn(); Checkbox( "Unsaved document", ref Demo.UnsavedDocument );
				EndTable();
			}
		}

		Demo.ShowWidgets();
		Demo.ShowLayout();
		Demo.ShowPopups();
		Demo.ShowTables();
		Demo.ShowInputs();

		PopItemWidth();
		End();
	}

	/// <summary>Basic help about controls.</summary>
	public static void ShowUserGuide()
	{
		BulletText( "Double-click on title bar to collapse window." );
		BulletText( "Click and drag on lower corner to resize window\n(double-click to auto fit window to its contents)." );
		BulletText( "Click and drag on any empty space to move window." );
		BulletText( "CTRL+Click on a slider or drag box to input value as text." );
		BulletText( "TAB/SHIFT+TAB to cycle through keyboard editable fields." );
		BulletText( "While inputting text:\n" );
		Indent();
		BulletText( "CTRL+A or double-click to select all." );
		BulletText( "CTRL+X/C/V to use clipboard cut/copy/paste." );
		BulletText( "CTRL+Z,CTRL+Y to undo/redo." );
		BulletText( "ESCAPE to revert." );
		Unindent();
		BulletText( "Mouse wheel scrolls windows; SHIFT+wheel scrolls horizontally." );
	}

	private static partial class Demo
	{
		public static void HelpMarker( string desc )
		{
			TextDisabled( "(?)" );
			if ( BeginItemTooltip() )
			{
				PushTextWrapPos( GetFontSize() * 35.0f );
				TextUnformatted( desc );
				PopTextWrapPos();
				EndTooltip();
			}
		}

		private static bool _menuEnabled = true;
		private static float _menuF = 0.5f;
		private static int _menuN;
		private static bool _menuB = true;

		public static void ShowExampleMenuFile()
		{
			MenuItem( "(demo menu)", null, false, false );
			if ( MenuItem( "New" ) ) { }
			if ( MenuItem( "Open", "Ctrl+O" ) ) { }
			if ( BeginMenu( "Open Recent" ) )
			{
				MenuItem( "fish_hat.c" );
				MenuItem( "fish_hat.inl" );
				MenuItem( "fish_hat.h" );
				if ( BeginMenu( "More.." ) )
				{
					MenuItem( "Hello" );
					MenuItem( "Sailor" );
					if ( BeginMenu( "Recurse.." ) )
					{
						ShowExampleMenuFile();
						EndMenu();
					}
					EndMenu();
				}
				EndMenu();
			}
			if ( MenuItem( "Save", "Ctrl+S" ) ) { }
			if ( MenuItem( "Save As.." ) ) { }

			Separator();
			if ( BeginMenu( "Options" ) )
			{
				MenuItem( "Enabled", "", ref _menuEnabled );
				BeginChild( "child", new Vector2( 0, 60 ), ImGuiChildFlags.Borders );
				for ( int i = 0; i < 10; i++ )
					Text( "Scrolling Text {0}", i );
				EndChild();
				SliderFloat( "Value", ref _menuF, 0.0f, 1.0f );
				InputFloat( "Input", ref _menuF, 0.1f );
				Combo( "Combo", ref _menuN, "Yes\0No\0Maybe\0\0" );
				EndMenu();
			}

			if ( BeginMenu( "Colors" ) )
			{
				float sz = GetTextLineHeight();
				for ( int i = 0; i < (int)ImGuiCol.COUNT; i++ )
				{
					var name = GetStyleColorName( (ImGuiCol)i );
					var p = GetCursorScreenPos();
					GetWindowDrawList().AddRectFilled( p, new Vector2( p.x + sz, p.y + sz ), GetColorU32( (ImGuiCol)i ) );
					Dummy( new Vector2( sz, sz ) );
					SameLine();
					MenuItem( name );
				}
				EndMenu();
			}

			if ( BeginMenu( "Options##2" ) )
			{
				Checkbox( "SomeOption", ref _menuB );
				EndMenu();
			}
			if ( BeginMenu( "Disabled", false ) ) { }
			if ( MenuItem( "Checked", null, true ) ) { }
			Separator();
			if ( MenuItem( "Quit", "Alt+F4" ) ) { }
		}
	}
}

#endregion

#region ImGui (Demo/ImGui.Demo.Examples)

public static partial class ImGui
{
	private static partial class Demo
	{
		#region Main menu bar
		public static void ShowExampleAppMainMenuBar()
		{
			if ( BeginMainMenuBar() )
			{
				if ( BeginMenu( "File" ) )
				{
					ShowExampleMenuFile();
					EndMenu();
				}
				if ( BeginMenu( "Edit" ) )
				{
					if ( MenuItem( "Undo", "CTRL+Z" ) ) { }
					if ( MenuItem( "Redo", "CTRL+Y", false, false ) ) { }
					Separator();
					if ( MenuItem( "Cut", "CTRL+X" ) ) { }
					if ( MenuItem( "Copy", "CTRL+C" ) ) { }
					if ( MenuItem( "Paste", "CTRL+V" ) ) { }
					EndMenu();
				}
				EndMainMenuBar();
			}
		}
		#endregion

		#region Console
		private static readonly List<string> _consoleItems = new() { "Welcome to Dear ImGui!" };
		private static string _consoleInput = "";
		private static bool _consoleAutoScroll = true;
		private static bool _consoleScrollToBottom;
		private static readonly List<string> _consoleHistory = new();
		private static int _consoleHistoryPos = -1;

		public static void ShowExampleAppConsole( ref bool open )
		{
			SetNextWindowSize( new Vector2( 520, 600 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( !Begin( "Example: Console", ref open ) )
			{
				End();
				return;
			}

			TextWrapped( "This example implements a console with basic coloring, completion (TAB key) and history (Up/Down keys). A more elaborate implementation may want to store entries along with extra data such as timestamp, emitter, etc." );
			TextWrapped( "Enter 'HELP' for help." );

			if ( SmallButton( "Add Debug Text" ) )
				_consoleItems.Add( $"{_consoleItems.Count} some text" );
			SameLine();
			if ( SmallButton( "Add Debug Error" ) )
				_consoleItems.Add( "[error] something went wrong" );
			SameLine();
			if ( SmallButton( "Clear" ) )
				_consoleItems.Clear();
			Separator();

			float footerHeightToReserve = GetStyle().ItemSpacing.y + GetFrameHeightWithSpacing();
			if ( BeginChild( "ScrollingRegion", new Vector2( 0, -footerHeightToReserve ), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar ) )
			{
				if ( BeginPopupContextWindow() )
				{
					if ( Selectable( "Clear" ) ) _consoleItems.Clear();
					EndPopup();
				}
				PushStyleVar( ImGuiStyleVar.ItemSpacing, new Vector2( 4, 1 ) );
				foreach ( var item in _consoleItems )
				{
					Vector4? color = null;
					if ( item.Contains( "[error]" ) ) color = new Vector4( 1.0f, 0.4f, 0.4f, 1.0f );
					else if ( item.StartsWith( "# " ) ) color = new Vector4( 1.0f, 0.8f, 0.6f, 1.0f );
					if ( color.HasValue ) PushStyleColor( ImGuiCol.Text, color.Value );
					TextUnformatted( item );
					if ( color.HasValue ) PopStyleColor();
				}
				if ( _consoleScrollToBottom || (_consoleAutoScroll && GetScrollY() >= GetScrollMaxY()) )
					SetScrollHereY( 1.0f );
				_consoleScrollToBottom = false;
				PopStyleVar();
			}
			EndChild();
			Separator();

			bool reclaimFocus = false;
			var inputFlags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.EscapeClearsAll | ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.CallbackHistory;
			if ( InputText( "Input", ref _consoleInput, inputFlags, ConsoleCallback ) )
			{
				var s = _consoleInput.Trim();
				if ( s.Length > 0 )
					ExecCommand( s );
				_consoleInput = "";
				reclaimFocus = true;
			}
			SetItemDefaultFocus();
			if ( reclaimFocus )
				SetKeyboardFocusHere( -1 );

			Checkbox( "Auto-scroll", ref _consoleAutoScroll );
			End();
		}

		private static void ExecCommand( string cmd )
		{
			_consoleItems.Add( $"# {cmd}" );
			_consoleHistoryPos = -1;
			_consoleHistory.Remove( cmd );
			_consoleHistory.Add( cmd );

			switch ( cmd.ToUpperInvariant() )
			{
				case "CLEAR":
					_consoleItems.Clear();
					break;
				case "HELP":
					_consoleItems.Add( "Commands:" );
					_consoleItems.Add( "- CLEAR" );
					_consoleItems.Add( "- HELP" );
					_consoleItems.Add( "- HISTORY" );
					break;
				case "HISTORY":
					int first = Math.Max( 0, _consoleHistory.Count - 10 );
					for ( int i = first; i < _consoleHistory.Count; i++ )
						_consoleItems.Add( $"{i,3}: {_consoleHistory[i]}" );
					break;
				default:
					_consoleItems.Add( $"Unknown command: '{cmd}'" );
					break;
			}
			_consoleScrollToBottom = true;
		}

		private static int ConsoleCallback( ImGuiInputTextCallbackData data )
		{
			if ( data.EventFlag == ImGuiInputTextFlags.CallbackCompletion )
			{
				string[] commands = { "HELP", "HISTORY", "CLEAR" };
				var word = data.Buf ?? "";
				var candidates = commands.Where( c => c.StartsWith( word, StringComparison.OrdinalIgnoreCase ) ).ToList();
				if ( candidates.Count == 1 )
				{
					data.DeleteChars( 0, data.BufTextLen );
					data.InsertChars( 0, candidates[0] + " " );
				}
				else if ( candidates.Count > 1 )
				{
					_consoleItems.Add( "Possible matches:" );
					foreach ( var c in candidates )
						_consoleItems.Add( "- " + c );
				}
			}
			else if ( data.EventFlag == ImGuiInputTextFlags.CallbackHistory )
			{
				int prevHistoryPos = _consoleHistoryPos;
				if ( data.EventKey == ImGuiKey.UpArrow )
				{
					if ( _consoleHistoryPos == -1 )
						_consoleHistoryPos = _consoleHistory.Count - 1;
					else if ( _consoleHistoryPos > 0 )
						_consoleHistoryPos--;
				}
				else if ( data.EventKey == ImGuiKey.DownArrow )
				{
					if ( _consoleHistoryPos != -1 )
						if ( ++_consoleHistoryPos >= _consoleHistory.Count )
							_consoleHistoryPos = -1;
				}
				if ( prevHistoryPos != _consoleHistoryPos )
				{
					var historyStr = _consoleHistoryPos >= 0 ? _consoleHistory[_consoleHistoryPos] : "";
					data.DeleteChars( 0, data.BufTextLen );
					data.InsertChars( 0, historyStr );
				}
			}
			return 0;
		}
		#endregion

		#region Log
		private static readonly List<string> _logLines = new();
		private static string _logFilter = "";
		private static bool _logAutoScroll = true;
		private static double _lastLogTime;
		private static int _logCounter;

		public static void ShowExampleAppLog( ref bool open )
		{
			SetNextWindowSize( new Vector2( 500, 400 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( !Begin( "Example: Log", ref open ) )
			{
				End();
				return;
			}

			if ( SmallButton( "[Debug] Add 5 entries" ) )
			{
				string[] categories = { "info", "warn", "error" };
				string[] words = { "Bumfuzzled", "Cattywampus", "Snickersnee", "Abibliophobia", "Absquatulate", "Nincompoop", "Pauciloquent" };
				for ( int n = 0; n < 5; n++ )
				{
					_logLines.Add( $"[{GetFrameCount():D5}] [{categories[_logCounter % categories.Length]}] Hello, current time is {GetTime():F1}, here's a word: '{words[_logCounter % words.Length]}'" );
					_logCounter++;
				}
			}
			if ( GetTime() - _lastLogTime > 1.0 )
			{
				_lastLogTime = GetTime();
				_logLines.Add( $"[{GetFrameCount():D5}] [info] tick at {GetTime():F1}" );
			}

			if ( BeginPopup( "Options" ) )
			{
				Checkbox( "Auto-scroll", ref _logAutoScroll );
				EndPopup();
			}
			if ( Button( "Options" ) )
				OpenPopup( "Options" );
			SameLine();
			bool clear = Button( "Clear" );
			SameLine();
			bool copy = Button( "Copy" );
			SameLine();
			SetNextItemWidth( -100 );
			InputText( "Filter", ref _logFilter );
			Separator();

			if ( clear ) _logLines.Clear();

			if ( BeginChild( "scrolling", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar ) )
			{
				PushStyleVar( ImGuiStyleVar.ItemSpacing, new Vector2( 0, 0 ) );
				var lines = string.IsNullOrEmpty( _logFilter ) ? _logLines : _logLines.Where( l => l.Contains( _logFilter, StringComparison.OrdinalIgnoreCase ) ).ToList();
				if ( copy )
					Sandbox.UI.Clipboard.SetText( string.Join( "\n", lines ) );
				foreach ( var line in lines )
					TextUnformatted( line );
				PopStyleVar();
				if ( _logAutoScroll && GetScrollY() >= GetScrollMaxY() )
					SetScrollHereY( 1.0f );
			}
			EndChild();
			End();
		}
		#endregion

		#region Layout
		private static int _layoutSelected;

		public static void ShowExampleAppLayout( ref bool open )
		{
			SetNextWindowSize( new Vector2( 500, 440 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( Begin( "Example: Simple layout", ref open, ImGuiWindowFlags.MenuBar ) )
			{
				if ( BeginMenuBar() )
				{
					if ( BeginMenu( "File" ) )
					{
						if ( MenuItem( "Close", "Ctrl+W" ) ) open = false;
						EndMenu();
					}
					EndMenuBar();
				}

				BeginChild( "left pane", new Vector2( 150, 0 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeX );
				for ( int i = 0; i < 100; i++ )
					if ( Selectable( $"MyObject {i}", _layoutSelected == i ) )
						_layoutSelected = i;
				EndChild();
				SameLine();

				BeginGroup();
				BeginChild( "item view", new Vector2( 0, -GetFrameHeightWithSpacing() ) );
				Text( "MyObject: {0}", _layoutSelected );
				Separator();
				if ( BeginTabBar( "##Tabs" ) )
				{
					if ( BeginTabItem( "Description" ) )
					{
						TextWrapped( "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " );
						EndTabItem();
					}
					if ( BeginTabItem( "Details" ) )
					{
						Text( "ID: 0123456789" );
						EndTabItem();
					}
					EndTabBar();
				}
				EndChild();
				if ( Button( "Revert" ) ) { }
				SameLine();
				if ( Button( "Save" ) ) { }
				EndGroup();
			}
			End();
		}
		#endregion

		#region Property editor
		private static readonly float[] _propValues = { 0.0f, 0.5f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f };

		public static void ShowExampleAppPropertyEditor( ref bool open )
		{
			SetNextWindowSize( new Vector2( 430, 450 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( !Begin( "Example: Property editor", ref open ) )
			{
				End();
				return;
			}

			HelpMarker( "This example shows how you may implement a property editor using two columns.\nAll objects/fields data are dummies here." );
			PushStyleVar( ImGuiStyleVar.FramePadding, new Vector2( 2, 2 ) );
			if ( BeginTable( "##split", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY ) )
			{
				TableSetupScrollFreeze( 0, 1 );
				TableSetupColumn( "Object" );
				TableSetupColumn( "Contents" );
				TableHeadersRow();

				for ( int objI = 0; objI < 4; objI++ )
				{
					PushID( objI );
					TableNextRow();
					TableSetColumnIndex( 0 );
					AlignTextToFramePadding();
					bool nodeOpen = TreeNode( "Object", "Object_{0}", objI );
					TableSetColumnIndex( 1 );
					Text( "my sailor is rich" );

					if ( nodeOpen )
					{
						for ( int i = 0; i < 8; i++ )
						{
							PushID( i );
							TableNextRow();
							TableSetColumnIndex( 0 );
							AlignTextToFramePadding();
							TreeNodeEx( "Field", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.Bullet, "Field_{0}", i );
							TableSetColumnIndex( 1 );
							SetNextItemWidth( -1.17549435E-38f );
							if ( i >= 5 )
								InputFloat( "##value", ref _propValues[i], 1.0f );
							else
								DragFloat( "##value", ref _propValues[i], 0.01f );
							PopID();
						}
						TreePop();
					}
					PopID();
				}
				EndTable();
			}
			PopStyleVar();
			End();
		}
		#endregion

		#region Long text
		private static int _longTextLines;

		public static void ShowExampleAppLongText( ref bool open )
		{
			SetNextWindowSize( new Vector2( 520, 600 ) * G.AppliedStyleScale, ImGuiCond.FirstUseEver );
			if ( !Begin( "Example: Long text display", ref open ) )
			{
				End();
				return;
			}
			if ( Button( "Add 1000 lines" ) )
				_longTextLines += 1000;
			SameLine();
			if ( Button( "Clear" ) )
				_longTextLines = 0;
			BeginChild( "Log" );
			// Manual clipping: only submit visible lines.
			float lineHeight = GetTextLineHeightWithSpacing();
			int first = Math.Max( 0, (int)(GetScrollY() / lineHeight) );
			int visible = (int)(GetWindowHeight() / lineHeight) + 2;
			SetCursorPosY( first * lineHeight );
			for ( int i = first; i < Math.Min( _longTextLines, first + visible ); i++ )
				Text( "{0} The quick brown fox jumps over the lazy dog", i );
			SetCursorPosY( _longTextLines * lineHeight );
			Dummy( Vector2.Zero );
			EndChild();
			End();
		}
		#endregion

		#region Auto resize
		private static int _autoResizeLines = 10;

		public static void ShowExampleAppAutoResize( ref bool open )
		{
			if ( !Begin( "Example: Auto-resizing window", ref open, ImGuiWindowFlags.AlwaysAutoResize ) )
			{
				End();
				return;
			}
			Text( "Window will resize every-frame to the size of its content.\nNote that you probably don't want to query the window size to\noutput your content because that would create a feedback loop." );
			SliderInt( "Number of lines", ref _autoResizeLines, 1, 20 );
			for ( int i = 0; i < _autoResizeLines; i++ )
				Text( "{0}This is line {1}", new string( ' ', i * 4 ), i );
			End();
		}
		#endregion

		#region Constrained resize
		private static int _constraintType;

		public static void ShowExampleAppConstrainedResize( ref bool open )
		{
			if ( _constraintType == 0 ) SetNextWindowSizeConstraints( new Vector2( 400, 0 ), new Vector2( 1000, float.MaxValue ) );
			if ( _constraintType == 1 ) SetNextWindowSizeConstraints( new Vector2( 0, 200 ), new Vector2( float.MaxValue, 600 ) );
			if ( _constraintType == 2 ) SetNextWindowSizeConstraints( new Vector2( 400, 300 ), new Vector2( 800, 600 ) );
			if ( _constraintType == 3 ) SetNextWindowSizeConstraints( Vector2.Zero, new Vector2( float.MaxValue, float.MaxValue ), d => d.DesiredSize = new Vector2( MathF.Max( d.DesiredSize.x, d.DesiredSize.y ), MathF.Max( d.DesiredSize.x, d.DesiredSize.y ) ) );
			if ( _constraintType == 4 ) SetNextWindowSizeConstraints( Vector2.Zero, new Vector2( float.MaxValue, float.MaxValue ), d => d.DesiredSize = new Vector2( MathF.Round( d.DesiredSize.x / 100 ) * 100, MathF.Round( d.DesiredSize.y / 100 ) * 100 ) );

			if ( Begin( "Example: Constrained Resize", ref open ) )
			{
				string[] testDesc = { "Between 400 and 1000 width", "Height between 200 and 600", "Size between 400x300 and 800x600", "Custom: Always Square", "Custom: Fixed Steps (100)" };
				SetNextItemWidth( GetFontSize() * 20 );
				Combo( "Constraint", ref _constraintType, testDesc );
				Text( "Window size: {0:F0} x {1:F0}", GetWindowWidth(), GetWindowHeight() );
			}
			End();
		}
		#endregion

		#region Simple overlay
		private static int _overlayLocation;

		public static void ShowExampleAppSimpleOverlay( ref bool open )
		{
			var io = GetIO();
			var windowFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
			if ( _overlayLocation >= 0 )
			{
				const float pad = 10.0f;
				var workSize = io.DisplaySize;
				var windowPos = new Vector2( (_overlayLocation & 1) != 0 ? workSize.x - pad : pad, (_overlayLocation & 2) != 0 ? workSize.y - pad : pad );
				var windowPosPivot = new Vector2( (_overlayLocation & 1) != 0 ? 1.0f : 0.0f, (_overlayLocation & 2) != 0 ? 1.0f : 0.0f );
				SetNextWindowPos( windowPos, ImGuiCond.Always, windowPosPivot );
				windowFlags |= ImGuiWindowFlags.NoMove;
			}
			SetNextWindowBgAlpha( 0.35f );
			if ( Begin( "Example: Simple overlay", ref open, windowFlags ) )
			{
				Text( "Simple overlay\n(right-click to change position)" );
				Separator();
				if ( IsMousePosValid() )
					Text( "Mouse Position: ({0:F1},{1:F1})", io.MousePos.x, io.MousePos.y );
				else
					Text( "Mouse Position: <invalid>" );
				Text( "FPS: {0:F0}", io.Framerate );
				if ( BeginPopupContextWindow() )
				{
					if ( MenuItem( "Custom", null, _overlayLocation == -1 ) ) _overlayLocation = -1;
					if ( MenuItem( "Top-left", null, _overlayLocation == 0 ) ) _overlayLocation = 0;
					if ( MenuItem( "Top-right", null, _overlayLocation == 1 ) ) _overlayLocation = 1;
					if ( MenuItem( "Bottom-left", null, _overlayLocation == 2 ) ) _overlayLocation = 2;
					if ( MenuItem( "Bottom-right", null, _overlayLocation == 3 ) ) _overlayLocation = 3;
					if ( MenuItem( "Close" ) ) open = false;
					EndPopup();
				}
			}
			End();
		}
		#endregion

		#region Fullscreen
		public static void ShowExampleAppFullscreen( ref bool open )
		{
			var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;
			SetNextWindowPos( Vector2.Zero );
			SetNextWindowSize( GetIO().DisplaySize );
			if ( Begin( "Example: Fullscreen window", ref open, flags ) )
			{
				Text( "This window covers the whole screen." );
				if ( Button( "Close this window" ) )
					open = false;
			}
			End();
		}
		#endregion

		#region Window titles
		public static void ShowExampleAppWindowTitles()
		{
			var basePos = new Vector2( 100, 100 ) * G.AppliedStyleScale;
			SetNextWindowPos( basePos, ImGuiCond.FirstUseEver );
			Begin( "Same title as another window##1" );
			Text( "This is window 1.\nMy title is the same as window 2, but my identifier is unique." );
			End();

			SetNextWindowPos( basePos + new Vector2( 0, 100 ), ImGuiCond.FirstUseEver );
			Begin( "Same title as another window##2" );
			Text( "This is window 2.\nMy title is the same as window 1, but my identifier is unique." );
			End();

			SetNextWindowPos( basePos + new Vector2( 0, 200 ), ImGuiCond.FirstUseEver );
			Begin( $"Animated title {"|/-\\"[(int)(GetTime() / 0.25f) & 3]} {GetFrameCount()}###AnimatedTitle" );
			Text( "This window has a changing title." );
			End();
		}
		#endregion

		#region Custom rendering
		private static float _crSize = 36.0f;
		private static float _crThickness = 3.0f;
		private static int _crNgonSides = 6;
		private static bool _crCurveSegmentsOverride;
		private static Vector4 _crColf = new( 1.0f, 1.0f, 0.4f, 1.0f );
		private static readonly List<Vector2> _crPoints = new();
		private static Vector2 _crScrolling;
		private static bool _crAddingLine;

		public static void ShowExampleAppCustomRendering( ref bool open )
		{
			if ( !Begin( "Example: Custom rendering", ref open ) )
			{
				End();
				return;
			}

			if ( BeginTabBar( "##TabBar" ) )
			{
				if ( BeginTabItem( "Primitives" ) )
				{
					PushItemWidth( -GetFontSize() * 15 );
					var drawList = GetWindowDrawList();
					SeparatorText( "Gradients" );
					var gradientSize = new Vector2( CalcItemWidth(), GetFrameHeight() );
					{
						var p0 = GetCursorScreenPos();
						var p1 = new Vector2( p0.x + gradientSize.x, p0.y + gradientSize.y );
						var colA = new Color32( 0, 0, 0, 255 );
						var colB = new Color32( 255, 255, 255, 255 );
						drawList.AddRectFilledMultiColor( p0, p1, colA, colB, colB, colA );
						InvisibleButton( "##gradient1", gradientSize );
					}
					{
						var p0 = GetCursorScreenPos();
						var p1 = new Vector2( p0.x + gradientSize.x, p0.y + gradientSize.y );
						var colA = new Color32( 0, 255, 0, 255 );
						var colB = new Color32( 255, 0, 0, 255 );
						drawList.AddRectFilledMultiColor( p0, p1, colA, colB, colB, colA );
						InvisibleButton( "##gradient2", gradientSize );
					}

					SeparatorText( "All primitives" );
					DragFloat( "Size", ref _crSize, 0.2f, 2.0f, 100.0f, "%.0f" );
					DragFloat( "Thickness", ref _crThickness, 0.05f, 1.0f, 8.0f, "%.02f" );
					SliderInt( "N-gon sides", ref _crNgonSides, 3, 12 );
					Checkbox( "##curvessegmentoverride", ref _crCurveSegmentsOverride );
					ColorEdit4( "Color", ref _crColf );

					var p = GetCursorScreenPos();
					var col = ColorConvertFloat4ToU32( _crColf );
					float spacing = 10.0f;
					float rounding = _crSize / 5.0f;
					float x = p.x + 4.0f;
					float y = p.y + 4.0f;
					float sz = _crSize;
					for ( int n = 0; n < 2; n++ )
					{
						float th = n == 0 ? 1.0f : _crThickness;
						drawList.AddNgon( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), sz * 0.5f, col, _crNgonSides, th ); x += sz + spacing;
						drawList.AddCircle( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), sz * 0.5f, col, 0, th ); x += sz + spacing;
						drawList.AddEllipse( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), new Vector2( sz * 0.5f, sz * 0.3f ), col, -0.3f, 32, th ); x += sz + spacing;
						drawList.AddRect( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, 0.0f, ImDrawFlags.None, th ); x += sz + spacing;
						drawList.AddRect( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, rounding, ImDrawFlags.None, th ); x += sz + spacing;
						drawList.AddRect( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, rounding, ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersBottomRight, th ); x += sz + spacing;
						drawList.AddTriangle( new Vector2( x + sz * 0.5f, y ), new Vector2( x + sz, y + sz - 0.5f ), new Vector2( x, y + sz - 0.5f ), col, th ); x += sz + spacing;
						drawList.AddLine( new Vector2( x, y ), new Vector2( x + sz, y ), col, th ); x += sz + spacing;
						drawList.AddLine( new Vector2( x, y ), new Vector2( x, y + sz ), col, th ); x += spacing;
						drawList.AddLine( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, th ); x += sz + spacing;

						var cp4 = new[] { new Vector2( x, y ), new Vector2( x + sz * 1.3f, y + sz * 0.3f ), new Vector2( x + sz - sz * 1.3f, y + sz - sz * 0.3f ), new Vector2( x + sz, y + sz ) };
						drawList.AddBezierCubic( cp4[0], cp4[1], cp4[2], cp4[3], col, th ); x += sz + spacing;
						x = p.x + 4;
						y += sz + spacing;
					}

					drawList.AddNgonFilled( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), sz * 0.5f, col, _crNgonSides ); x += sz + spacing;
					drawList.AddCircleFilled( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), sz * 0.5f, col ); x += sz + spacing;
					drawList.AddEllipseFilled( new Vector2( x + sz * 0.5f, y + sz * 0.5f ), new Vector2( sz * 0.5f, sz * 0.3f ), col, -0.3f, 32 ); x += sz + spacing;
					drawList.AddRectFilled( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col ); x += sz + spacing;
					drawList.AddRectFilled( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, 10.0f ); x += sz + spacing;
					drawList.AddRectFilled( new Vector2( x, y ), new Vector2( x + sz, y + sz ), col, 10.0f, ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersBottomRight ); x += sz + spacing;
					drawList.AddTriangleFilled( new Vector2( x + sz * 0.5f, y ), new Vector2( x + sz, y + sz - 0.5f ), new Vector2( x, y + sz - 0.5f ), col ); x += sz + spacing;
					drawList.AddRectFilledMultiColor( new Vector2( x, y ), new Vector2( x + sz, y + sz ), new Color32( 0, 0, 0, 255 ), new Color32( 255, 0, 0, 255 ), new Color32( 255, 255, 0, 255 ), new Color32( 0, 255, 0, 255 ) );

					Dummy( new Vector2( (sz + spacing) * 10.2f, (sz + spacing) * 3.0f ) );
					PopItemWidth();
					EndTabItem();
				}

				if ( BeginTabItem( "Canvas" ) )
				{
					Checkbox( "Enable context menu", ref _crCurveSegmentsOverride );
					Text( "Mouse Left: drag to add lines,\nMouse Right: drag to scroll, click for context menu." );

					var canvasP0 = GetCursorScreenPos();
					var canvasSz = GetContentRegionAvail();
					if ( canvasSz.x < 50.0f ) canvasSz.x = 50.0f;
					if ( canvasSz.y < 50.0f ) canvasSz.y = 50.0f;
					var canvasP1 = canvasP0 + canvasSz;

					var io = GetIO();
					var drawList = GetWindowDrawList();
					drawList.AddRectFilled( canvasP0, canvasP1, new Color32( 50, 50, 50, 255 ) );
					drawList.AddRect( canvasP0, canvasP1, new Color32( 255, 255, 255, 255 ) );

					InvisibleButton( "canvas", canvasSz, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight );
					bool isHovered = IsItemHovered();
					bool isActive = IsItemActive();
					var origin = canvasP0 + _crScrolling;
					var mousePosInCanvas = io.MousePos - origin;

					if ( isHovered && !_crAddingLine && IsMouseClicked( ImGuiMouseButton.Left ) )
					{
						_crPoints.Add( mousePosInCanvas );
						_crPoints.Add( mousePosInCanvas );
						_crAddingLine = true;
					}
					if ( _crAddingLine )
					{
						_crPoints[^1] = mousePosInCanvas;
						if ( !IsMouseDown( ImGuiMouseButton.Left ) )
							_crAddingLine = false;
					}
					if ( isActive && IsMouseDragging( ImGuiMouseButton.Right, 0.0f ) )
						_crScrolling += io.MouseDelta;

					var dragDelta = GetMouseDragDelta( ImGuiMouseButton.Right );
					if ( dragDelta.x == 0.0f && dragDelta.y == 0.0f )
						OpenPopupOnItemClick( "context", ImGuiPopupFlags.MouseButtonRight );
					if ( BeginPopup( "context" ) )
					{
						if ( _crAddingLine )
							_crPoints.RemoveRange( _crPoints.Count - 2, 2 );
						_crAddingLine = false;
						if ( MenuItem( "Remove one", null, false, _crPoints.Count > 0 ) ) _crPoints.RemoveRange( _crPoints.Count - 2, 2 );
						if ( MenuItem( "Remove all", null, false, _crPoints.Count > 0 ) ) _crPoints.Clear();
						EndPopup();
					}

					drawList.PushClipRect( canvasP0, canvasP1, true );
					const float gridStep = 64.0f;
					for ( float gx = _crScrolling.x % gridStep; gx < canvasSz.x; gx += gridStep )
						drawList.AddLine( new Vector2( canvasP0.x + gx, canvasP0.y ), new Vector2( canvasP0.x + gx, canvasP1.y ), new Color32( 200, 200, 200, 40 ) );
					for ( float gy = _crScrolling.y % gridStep; gy < canvasSz.y; gy += gridStep )
						drawList.AddLine( new Vector2( canvasP0.x, canvasP0.y + gy ), new Vector2( canvasP1.x, canvasP0.y + gy ), new Color32( 200, 200, 200, 40 ) );
					for ( int n = 0; n + 1 < _crPoints.Count; n += 2 )
						drawList.AddLine( origin + _crPoints[n], origin + _crPoints[n + 1], new Color32( 255, 255, 0, 255 ), 2.0f );
					drawList.PopClipRect();
					EndTabItem();
				}

				if ( BeginTabItem( "BG/FG draw lists" ) )
				{
					var windowCenter = GetWindowPos() + GetWindowSize() * 0.5f;
					GetBackgroundDrawList().AddCircle( windowCenter, GetWindowSize().x * 0.6f, new Color32( 255, 0, 0, 200 ), 0, 10 + 4 );
					GetForegroundDrawList().AddCircle( windowCenter, GetWindowSize().y * 0.6f, new Color32( 0, 255, 0, 200 ), 0, 10 );
					Text( "The background draw list renders behind all windows, the foreground draw list on top." );
					EndTabItem();
				}
				EndTabBar();
			}
			End();
		}
		#endregion

		#region Documents
		private class MyDocument
		{
			public string Name;
			public bool Open = true;
			public bool OpenPrev = true;
			public bool Dirty;
			public Vector4 Color = Vector4.One;
		}

		private static readonly List<MyDocument> _documents = new()
		{
			new MyDocument { Name = "Lettuce", Color = new Vector4( 0.4f, 0.8f, 0.4f, 1.0f ) },
			new MyDocument { Name = "Eggplant", Color = new Vector4( 0.8f, 0.5f, 1.0f, 1.0f ) },
			new MyDocument { Name = "Carrot", Color = new Vector4( 1.0f, 0.8f, 0.5f, 1.0f ) },
			new MyDocument { Name = "Tomato", Color = new Vector4( 1.0f, 0.3f, 0.4f, 1.0f ) },
			new MyDocument { Name = "A Rather Long Title", Color = new Vector4( 0.4f, 0.8f, 0.8f, 1.0f ) },
		};

		public static void ShowExampleAppDocuments( ref bool open )
		{
			if ( !Begin( "Example: Documents", ref open, ImGuiWindowFlags.MenuBar ) )
			{
				End();
				return;
			}

			if ( BeginMenuBar() )
			{
				if ( BeginMenu( "File" ) )
				{
					int openCount = _documents.Count( d => d.Open );
					if ( BeginMenu( "Open", openCount < _documents.Count ) )
					{
						foreach ( var doc in _documents )
							if ( !doc.Open && MenuItem( doc.Name ) )
								doc.Open = true;
						EndMenu();
					}
					if ( MenuItem( "Close All Documents", null, false, openCount > 0 ) )
						foreach ( var doc in _documents ) doc.Open = false;
					if ( MenuItem( "Exit" ) )
						open = false;
					EndMenu();
				}
				EndMenuBar();
			}

			foreach ( var doc in _documents )
			{
				if ( doc != _documents[0] ) SameLine();
				PushID( doc.Name );
				Checkbox( doc.Name, ref doc.Open );
				PopID();
			}
			Separator();

			if ( BeginTabBar( "##tabs", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.FittingPolicyResizeDown | ImGuiTabBarFlags.TabListPopupButton ) )
			{
				foreach ( var doc in _documents )
				{
					if ( !doc.Open )
						continue;
					var tabFlags = doc.Dirty ? ImGuiTabItemFlags.UnsavedDocument : ImGuiTabItemFlags.None;
					bool visible = BeginTabItem( doc.Name, ref doc.Open, tabFlags );
					if ( visible )
					{
						PushID( doc.Name );
						PushStyleColor( ImGuiCol.Text, doc.Color );
						TextWrapped( "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua." );
						PopStyleColor();
						if ( Button( "Modify" ) ) doc.Dirty = true;
						SameLine();
						if ( Button( "Save" ) ) doc.Dirty = false;
						ColorEdit3( "color", ref doc.Color );
						PopID();
						EndTabItem();
					}
				}
				EndTabBar();
			}
			End();
		}
		#endregion
	}
}

#endregion

#region ImGui (Demo/ImGui.Demo.Public)

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

#endregion

#region ImGui (Demo/ImGui.Demo.Sections)

public static partial class ImGui
{
	private static partial class Demo
	{
		// Layout
		private static bool _disableMouseWheel, _disableMenu;
		private static int _offsetX;
		private static float _itemWidthF = 0.5f;
		private static int _widthMode;
		private static bool _scrollToOff, _scrollToPos;
		private static float _scrollToOffPx = 0.0f, _scrollToPosPx = 200.0f;
		private static int _trackItem = 50;
		private static bool _enableTrack = true;
		private static Vector2 _clipSize = new( 100.0f, 100.0f );
		private static Vector2 _clipOffset = new( 30.0f, 30.0f );
		private static float _groupValues0 = 0.5f;

		// Popups
		private static int _selectedFish = -1;
		private static readonly bool[] _toggles = { true, false, false, false, false };
		private static string _contextName = "Label1";
		private static float _ctxValue = 0.5f;
		private static bool _dontAskMeNextTime;
		private static int _modalItem = 1;
		private static Vector4 _modalColor = new( 0.4f, 0.7f, 0.0f, 0.5f );

		// Tables
		private static ImGuiTableFlags _tableFlags1 = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
		private static ImGuiTableFlags _tableFlagsResize = ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.ContextMenuInBody;
		private static ImGuiTableFlags _tableFlagsScroll = ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable;
		private static ImGuiTableFlags _tableFlagsSort = ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable | ImGuiTableFlags.SortMulti | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV | ImGuiTableFlags.ScrollY;
		private static List<(int Id, string Name, int Quantity)> _sortItems;
		private static int _columnsCount = 3;
		private static bool _columnsBorders = true;

		public static void ShowLayout()
		{
			if ( !CollapsingHeader( "Layout & Scrolling" ) )
				return;

			if ( TreeNode( "Child windows" ) )
			{
				SeparatorText( "Child windows" );
				HelpMarker( "Use child windows to begin into a self-contained independent scrolling/clipping regions within a host window." );
				Checkbox( "Disable Mouse Wheel", ref _disableMouseWheel );
				Checkbox( "Disable Menu", ref _disableMenu );

				{
					var windowFlags = ImGuiWindowFlags.HorizontalScrollbar;
					if ( _disableMouseWheel )
						windowFlags |= ImGuiWindowFlags.NoScrollWithMouse;
					BeginChild( "ChildL", new Vector2( GetContentRegionAvail().x * 0.5f, 260 ), ImGuiChildFlags.None, windowFlags );
					for ( int i = 0; i < 100; i++ )
						Text( "{0:D4}: scrollable region", i );
					EndChild();
				}

				SameLine();

				{
					var windowFlags = ImGuiWindowFlags.None;
					if ( _disableMouseWheel )
						windowFlags |= ImGuiWindowFlags.NoScrollWithMouse;
					if ( !_disableMenu )
						windowFlags |= ImGuiWindowFlags.MenuBar;
					PushStyleVar( ImGuiStyleVar.ChildRounding, 5.0f );
					BeginChild( "ChildR", new Vector2( 0, 260 ), ImGuiChildFlags.Borders, windowFlags );
					if ( !_disableMenu && BeginMenuBar() )
					{
						if ( BeginMenu( "Menu" ) )
						{
							ShowExampleMenuFile();
							EndMenu();
						}
						EndMenuBar();
					}
					if ( BeginTable( "split", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings ) )
					{
						for ( int i = 0; i < 100; i++ )
						{
							TableNextColumn();
							Button( $"{i:D3}", new Vector2( -1.17549435E-38f, 0.0f ) );
						}
						EndTable();
					}
					EndChild();
					PopStyleVar();
				}

				SeparatorText( "Manual-resize" );
				HelpMarker( "Drag bottom border to resize. Double-click bottom border to auto-fit to vertical contents." );
				PushStyleColor( ImGuiCol.ChildBg, GetStyleColorVec4( ImGuiCol.FrameBg ) );
				if ( BeginChild( "ResizableChild", new Vector2( -1.17549435E-38f, GetTextLineHeightWithSpacing() * 8 ), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY ) )
					for ( int n = 0; n < 10; n++ )
						Text( "Line {0:D4}", n );
				PopStyleColor();
				EndChild();

				SeparatorText( "Auto-resize with constraints" );
				SetNextWindowSizeConstraints( new Vector2( 0.0f, GetTextLineHeightWithSpacing() * 1 ), new Vector2( float.MaxValue, GetTextLineHeightWithSpacing() * 6 ) );
				if ( BeginChild( "ConstrainedChild", new Vector2( -1.17549435E-38f, 0.0f ), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY ) )
					for ( int n = 0; n < 4; n++ )
						Text( "Line {0:D4}", n );
				EndChild();

				SeparatorText( "Misc/Advanced" );
				SetNextItemWidth( GetFontSize() * 8 );
				DragInt( "Offset X", ref _offsetX, 1.0f, -1000, 1000 );
				SetCursorPosX( GetCursorPosX() + _offsetX );
				PushStyleColor( ImGuiCol.ChildBg, new Color32( 255, 0, 0, 100 ) );
				BeginChild( "Red", new Vector2( 200, 100 ), ImGuiChildFlags.Borders, ImGuiWindowFlags.None );
				for ( int n = 0; n < 50; n++ )
					Text( "Some test {0}", n );
				EndChild();
				bool childIsHovered = IsItemHovered();
				var childRectMin = GetItemRectMin();
				var childRectMax = GetItemRectMax();
				PopStyleColor();
				Text( "Hovered: {0}", childIsHovered );
				Text( "Rect of child window is: ({0:F0},{1:F0}) ({2:F0},{3:F0})", childRectMin.x, childRectMin.y, childRectMax.x, childRectMax.y );

				TreePop();
			}

			if ( TreeNode( "Widgets Width" ) )
			{
				Text( "SetNextItemWidth/PushItemWidth(100)" );
				SameLine(); HelpMarker( "Fixed width." );
				PushItemWidth( 100 );
				DragFloat( "float##1b", ref _itemWidthF );
				PopItemWidth();

				Text( "SetNextItemWidth/PushItemWidth(-100)" );
				SameLine(); HelpMarker( "Align to right edge minus 100" );
				PushItemWidth( -100 );
				DragFloat( "float##2a", ref _itemWidthF );
				PopItemWidth();

				Text( "SetNextItemWidth/PushItemWidth(GetContentRegionAvail().x * 0.5f)" );
				PushItemWidth( GetContentRegionAvail().x * 0.5f );
				DragFloat( "float##3a", ref _itemWidthF );
				PopItemWidth();

				Text( "SetNextItemWidth/PushItemWidth(-FLT_MIN)" );
				SameLine(); HelpMarker( "Align to right edge" );
				PushItemWidth( -1.17549435E-38f );
				DragFloat( "##float5a", ref _itemWidthF );
				PopItemWidth();
				TreePop();
			}

			if ( TreeNode( "Basic Horizontal Layout" ) )
			{
				TextWrapped( "(Use ImGui.SameLine() to keep adding items to the right of the preceding item)" );

				Text( "Two items: Hello" ); SameLine();
				TextColored( new Vector4( 1, 1, 0, 1 ), "Sailor" );

				Text( "More spacing: Hello" ); SameLine( 0, 20 );
				TextColored( new Vector4( 1, 1, 0, 1 ), "Sailor" );

				AlignTextToFramePadding();
				Text( "Normal buttons" ); SameLine();
				Button( "Banana" ); SameLine();
				Button( "Apple" ); SameLine();
				Button( "Corniflower" );

				Text( "Small buttons" ); SameLine();
				SmallButton( "Like this one" ); SameLine();
				Text( "can fit within a text block." );

				Text( "Aligned" );
				SameLine( 150 ); Text( "x=150" );
				SameLine( 300 ); Text( "x=300" );
				Text( "Aligned" );
				SameLine( 150 ); SmallButton( "x=150" );
				SameLine( 300 ); SmallButton( "x=300" );

				Text( "Manual wrapping:" );
				var style = GetStyle();
				int buttonsCount = 20;
				float windowVisibleX2 = GetCursorScreenPos().x + GetContentRegionAvail().x;
				for ( int n = 0; n < buttonsCount; n++ )
				{
					var buttonSz = new Vector2( 40, 40 );
					PushID( n );
					Button( "Box", buttonSz );
					float lastButtonX2 = GetItemRectMax().x;
					float nextButtonX2 = lastButtonX2 + style.ItemSpacing.x + buttonSz.x;
					if ( n + 1 < buttonsCount && nextButtonX2 < windowVisibleX2 )
						SameLine();
					PopID();
				}
				TreePop();
			}

			if ( TreeNode( "Groups" ) )
			{
				HelpMarker( "BeginGroup() basically locks the horizontal position for new line.\nEndGroup() bundles the whole group so that you can use \"item\" functions such as IsItemHovered()/IsItemActive() or SameLine() etc. on the whole group." );
				BeginGroup();
				{
					BeginGroup();
					Button( "AAA" );
					SameLine();
					Button( "BBB" );
					SameLine();
					BeginGroup();
					Button( "CCC" );
					Button( "DDD" );
					EndGroup();
					SameLine();
					Button( "EEE" );
					EndGroup();
					SetItemTooltip( "First group hovered" );
				}
				var size = GetItemRectSize();
				float[] values = { 0.5f, 0.20f, 0.80f, 0.60f, 0.25f };
				PlotHistogram( "##values", values, 0, null, 0.0f, 1.0f, size );

				Button( "ACTION", new Vector2( (size.x - GetStyle().ItemSpacing.x) * 0.5f, size.y ) );
				SameLine();
				Button( "REACTION", new Vector2( (size.x - GetStyle().ItemSpacing.x) * 0.5f, size.y ) );
				EndGroup();
				SameLine();

				Button( "LEVERAGE\nBUZZWORD", size );
				SameLine();

				if ( BeginListBox( "List", size ) )
				{
					Selectable( "Selected", true );
					Selectable( "Not Selected", false );
					EndListBox();
				}
				TreePop();
			}

			if ( TreeNode( "Text Baseline Alignment" ) )
			{
				BulletText( "Text annotation for SmallButton() and other aligned widgets" );
				Text( "One\nTwo\nThree" ); SameLine();
				Text( "Hello\nWorld" ); SameLine();
				Text( "Banana" );

				Button( "HOP##1" ); SameLine();
				Text( "Banana" ); SameLine();
				Text( "Hello\nWorld" ); SameLine();
				Text( "Banana" );

				AlignTextToFramePadding();
				Text( "Hello" ); SameLine();
				Button( "Button" ); SameLine();
				SliderFloat( "##slider", ref _groupValues0, 0, 1 );
				TreePop();
			}

			if ( TreeNode( "Scrolling" ) )
			{
				HelpMarker( "Use SetScrollHereY() or SetScrollFromPosY() to scroll to a given vertical position." );
				Checkbox( "Decoration", ref _enableTrack );
				Checkbox( "Track", ref _enableTrack );
				PushItemWidth( 100 );
				SameLine( 140 );
				if ( DragInt( "##item", ref _trackItem, 0.25f, 0, 99, "Item = %d" ) )
					_enableTrack = true;

				bool scrollToOff = Button( "Scroll Offset" );
				SameLine( 140 );
				scrollToOff |= DragFloat( "##off", ref _scrollToOffPx, 1.00f, 0, float.MaxValue, "+%.0f px" );

				bool scrollToPos = Button( "Scroll To Pos" );
				SameLine( 140 );
				scrollToPos |= DragFloat( "##pos", ref _scrollToPosPx, 1.00f, -10, float.MaxValue, "X/Y = %.0f px" );
				PopItemWidth();

				if ( scrollToOff || scrollToPos )
					_enableTrack = false;

				var style = GetStyle();
				float childW = (GetContentRegionAvail().x - 4 * style.ItemSpacing.x) / 5;
				if ( childW < 1.0f )
					childW = 1.0f;
				PushID( "##VerticalScrolling" );
				for ( int i = 0; i < 5; i++ )
				{
					if ( i > 0 ) SameLine();
					BeginGroup();
					string[] names = { "Top", "25%", "Center", "75%", "Bottom" };
					TextUnformatted( names[i] );

					bool childIsVisible = BeginChild( GetID( i ), new Vector2( childW, 200.0f ), ImGuiChildFlags.Borders, ImGuiWindowFlags.MenuBar );
					if ( BeginMenuBar() )
					{
						TextUnformatted( "abc" );
						EndMenuBar();
					}
					if ( scrollToOff )
						SetScrollY( _scrollToOffPx );
					if ( scrollToPos )
						SetScrollFromPosY( GetCursorStartPos().y + _scrollToPosPx, i * 0.25f );
					if ( childIsVisible )
					{
						for ( int item = 0; item < 100; item++ )
						{
							if ( _enableTrack && item == _trackItem )
							{
								TextColored( new Vector4( 1, 1, 0, 1 ), "Item {0}", item );
								SetScrollHereY( i * 0.25f );
							}
							else
							{
								Text( "Item {0}", item );
							}
						}
					}
					float scrollY = GetScrollY();
					float scrollMaxY = GetScrollMaxY();
					EndChild();
					Text( "{0:F0}/{1:F0}", scrollY, scrollMaxY );
					EndGroup();
				}
				PopID();
				TreePop();
			}

			if ( TreeNode( "Clipping" ) )
			{
				DragFloat2( "size", ref _clipSize, 0.5f, 1.0f, 200.0f, "%.0f" );
				TextWrapped( "(Click and drag to scroll)" );
				for ( int n = 0; n < 3; n++ )
				{
					if ( n > 0 ) SameLine();
					PushID( n );
					InvisibleButton( "##canvas", _clipSize );
					if ( IsItemActive() && IsMouseDragging( ImGuiMouseButton.Left ) )
						_clipOffset += GetIO().MouseDelta;
					PopID();
					if ( !IsItemVisible() )
						continue;
					var p0 = GetItemRectMin();
					var p1 = GetItemRectMax();
					string textStr = "Line 1 hello\nLine 2 clip me!";
					var textPos = p0 + _clipOffset;
					var drawList = GetWindowDrawList();
					switch ( n )
					{
						case 0:
							PushClipRect( p0, p1, true );
							drawList.AddRectFilled( p0, p1, new Color32( 90, 90, 120, 255 ) );
							drawList.AddText( textPos, new Color32( 255, 255, 255, 255 ), textStr );
							PopClipRect();
							break;
						case 1:
							drawList.PushClipRect( p0, p1, true );
							drawList.AddRectFilled( p0, p1, new Color32( 90, 90, 120, 255 ) );
							drawList.AddText( textPos, new Color32( 255, 255, 255, 255 ), textStr );
							drawList.PopClipRect();
							break;
						case 2:
							drawList.AddRectFilled( p0, p1, new Color32( 90, 90, 120, 255 ) );
							drawList.AddText( 0f, textPos, new Color32( 255, 255, 255, 255 ), textStr, 0.0f, new ImRect( p0, p1 ) );
							break;
					}
				}
				TreePop();
			}
		}

		public static void ShowPopups()
		{
			if ( !CollapsingHeader( "Popups & Modal windows" ) )
				return;

			if ( TreeNode( "Popups" ) )
			{
				TextWrapped( "When a popup is active, it inhibits interacting with windows that are behind the popup. Clicking outside the popup closes it." );
				string[] names = { "Bream", "Haddock", "Mackerel", "Pollock", "Tilefish" };

				if ( Button( "Select.." ) )
					OpenPopup( "my_select_popup" );
				SameLine();
				TextUnformatted( _selectedFish == -1 ? "<None>" : names[_selectedFish] );
				if ( BeginPopup( "my_select_popup" ) )
				{
					SeparatorText( "Aquarium" );
					for ( int i = 0; i < names.Length; i++ )
						if ( Selectable( names[i] ) )
							_selectedFish = i;
					EndPopup();
				}

				if ( Button( "Toggle.." ) )
					OpenPopup( "my_toggle_popup" );
				if ( BeginPopup( "my_toggle_popup" ) )
				{
					for ( int i = 0; i < names.Length; i++ )
						MenuItem( names[i], "", ref _toggles[i] );
					if ( BeginMenu( "Sub-menu" ) )
					{
						MenuItem( "Click me" );
						EndMenu();
					}
					Separator();
					Text( "Tooltip here" );
					SetItemTooltip( "I am a tooltip over a popup" );
					if ( Button( "Stacked Popup" ) )
						OpenPopup( "another popup" );
					if ( BeginPopup( "another popup" ) )
					{
						for ( int i = 0; i < names.Length; i++ )
							MenuItem( names[i], "", ref _toggles[i] );
						EndPopup();
					}
					EndPopup();
				}

				if ( Button( "With a menu.." ) )
					OpenPopup( "my_file_popup" );
				if ( BeginPopup( "my_file_popup", ImGuiWindowFlags.MenuBar ) )
				{
					if ( BeginMenuBar() )
					{
						if ( BeginMenu( "File" ) )
						{
							ShowExampleMenuFile();
							EndMenu();
						}
						if ( BeginMenu( "Edit" ) )
						{
							MenuItem( "Dummy" );
							EndMenu();
						}
						EndMenuBar();
					}
					Text( "Hello from popup!" );
					Button( "This is a dummy button.." );
					EndPopup();
				}
				TreePop();
			}

			if ( TreeNode( "Context menus" ) )
			{
				HelpMarker( "\"Context\" functions are simple helpers to associate a Popup to a given Item or Window identifier." );
				{
					string[] names = { "Label1", "Label2", "Label3", "Label4", "Label5" };
					for ( int n = 0; n < 5; n++ )
					{
						Selectable( names[n], _selectedFish == n );
						if ( BeginPopupContextItem() )
						{
							_selectedFish = n;
							Text( "This a popup for \"{0}\"!", names[n] );
							if ( Button( "Close" ) )
								CloseCurrentPopup();
							EndPopup();
						}
						SetItemTooltip( "Right-click to open popup" );
					}
				}

				{
					Text( "Value = {0:F3} <-- (1) right-click this text", _ctxValue );
					if ( BeginPopupContextItem( "my popup" ) )
					{
						if ( Selectable( "Set to zero" ) ) _ctxValue = 0.0f;
						if ( Selectable( "Set to PI" ) ) _ctxValue = 3.1415f;
						SetNextItemWidth( -1.17549435E-38f );
						DragFloat( "##Value", ref _ctxValue, 0.1f, 0.0f, 0.0f );
						EndPopup();
					}
					Text( "(2) Or right-click this text" );
					OpenPopupOnItemClick( "my popup", ImGuiPopupFlags.MouseButtonRight );
					if ( Button( "(3) Or click this button" ) )
						OpenPopup( "my popup" );
				}

				{
					HelpMarker( "Showcase using a popup ID linked to item ID, with the item having a changing label + stable ID using the ### operator." );
					Button( $"Button: {_contextName}###Button" );
					if ( BeginPopupContextItem() )
					{
						Text( "Edit name:" );
						InputText( "##edit", ref _contextName );
						if ( Button( "Close" ) )
							CloseCurrentPopup();
						EndPopup();
					}
					SameLine(); Text( "(<-- right-click here)" );
				}
				TreePop();
			}

			if ( TreeNode( "Modals" ) )
			{
				TextWrapped( "Modal windows are like popups but the user cannot close them by clicking outside." );

				if ( Button( "Delete.." ) )
					OpenPopup( "Delete?" );

				var center = GetIO().DisplaySize * 0.5f;
				SetNextWindowPos( center, ImGuiCond.Appearing, new Vector2( 0.5f, 0.5f ) );
				if ( BeginPopupModal( "Delete?", ImGuiWindowFlags.AlwaysAutoResize ) )
				{
					Text( "All those beautiful files will be deleted.\nThis operation cannot be undone!" );
					Separator();
					PushStyleVar( ImGuiStyleVar.FramePadding, new Vector2( 0, 0 ) );
					Checkbox( "Don't ask me next time", ref _dontAskMeNextTime );
					PopStyleVar();
					if ( Button( "OK", new Vector2( 120, 0 ) ) ) CloseCurrentPopup();
					SetItemDefaultFocus();
					SameLine();
					if ( Button( "Cancel", new Vector2( 120, 0 ) ) ) CloseCurrentPopup();
					EndPopup();
				}

				if ( Button( "Stacked modals.." ) )
					OpenPopup( "Stacked 1" );
				if ( BeginPopupModal( "Stacked 1", ImGuiWindowFlags.MenuBar ) )
				{
					if ( BeginMenuBar() )
					{
						if ( BeginMenu( "File" ) )
						{
							if ( MenuItem( "Some menu item" ) ) { }
							EndMenu();
						}
						EndMenuBar();
					}
					Text( "Hello from Stacked The First\nUsing style.Colors[ImGuiCol.ModalWindowDimBg] behind it." );
					Combo( "Combo", ref _modalItem, "aaaa\0bbbb\0cccc\0dddd\0eeee\0\0" );
					ColorEdit4( "Color", ref _modalColor );

					if ( Button( "Add another modal.." ) )
						OpenPopup( "Stacked 2" );

					bool unusedOpen = true;
					if ( BeginPopupModal( "Stacked 2", ref unusedOpen ) )
					{
						Text( "Hello from Stacked The Second!" );
						ColorEdit4( "Color", ref _modalColor );
						if ( Button( "Close" ) )
							CloseCurrentPopup();
						EndPopup();
					}

					if ( Button( "Close" ) )
						CloseCurrentPopup();
					EndPopup();
				}
				TreePop();
			}

			if ( TreeNode( "Menus inside a regular window" ) )
			{
				TextWrapped( "Below we are testing adding menu items to a regular window. It's rather unusual but should work!" );
				Separator();
				MenuItem( "Menu item", "CTRL+M" );
				if ( BeginMenu( "Menu inside a regular window" ) )
				{
					ShowExampleMenuFile();
					EndMenu();
				}
				Separator();
				TreePop();
			}
		}

		public static void ShowTables()
		{
			if ( !CollapsingHeader( "Tables & Columns" ) )
				return;

			if ( TreeNode( "Basic" ) )
			{
				if ( BeginTable( "table1", 3 ) )
				{
					for ( int row = 0; row < 4; row++ )
					{
						TableNextRow();
						for ( int column = 0; column < 3; column++ )
						{
							TableSetColumnIndex( column );
							Text( "Row {0} Column {1}", row, column );
						}
					}
					EndTable();
				}

				if ( BeginTable( "table2", 3 ) )
				{
					for ( int row = 0; row < 4; row++ )
					{
						TableNextRow();
						TableNextColumn(); Text( "Row {0}", row );
						TableNextColumn(); Text( "Some contents" );
						TableNextColumn(); Text( "123.456" );
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Borders, background" ) )
			{
				CheckboxFlags( "ImGuiTableFlags_RowBg", ref _tableFlags1, ImGuiTableFlags.RowBg );
				CheckboxFlags( "ImGuiTableFlags_Borders", ref _tableFlags1, ImGuiTableFlags.Borders );
				Indent();
				CheckboxFlags( "ImGuiTableFlags_BordersH", ref _tableFlags1, ImGuiTableFlags.BordersH );
				CheckboxFlags( "ImGuiTableFlags_BordersV", ref _tableFlags1, ImGuiTableFlags.BordersV );
				CheckboxFlags( "ImGuiTableFlags_BordersOuter", ref _tableFlags1, ImGuiTableFlags.BordersOuter );
				CheckboxFlags( "ImGuiTableFlags_BordersInner", ref _tableFlags1, ImGuiTableFlags.BordersInner );
				Unindent();

				if ( BeginTable( "table1", 3, _tableFlags1 ) )
				{
					TableSetupColumn( "One" );
					TableSetupColumn( "Two" );
					TableSetupColumn( "Three" );
					TableHeadersRow();
					for ( int row = 0; row < 5; row++ )
					{
						TableNextRow();
						for ( int column = 0; column < 3; column++ )
						{
							TableSetColumnIndex( column );
							Text( "Hello {0},{1}", column, row );
						}
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Resizable, stretch" ) )
			{
				CheckboxFlags( "ImGuiTableFlags_Resizable", ref _tableFlagsResize, ImGuiTableFlags.Resizable );
				CheckboxFlags( "ImGuiTableFlags_BordersV", ref _tableFlagsResize, ImGuiTableFlags.BordersV );
				SameLine(); HelpMarker( "Using the _Resizable flag automatically enables the _BordersInnerV flag as well, this is why the resize borders are still showing when unchecking this." );
				if ( BeginTable( "table1", 3, _tableFlagsResize ) )
				{
					for ( int row = 0; row < 5; row++ )
					{
						TableNextRow();
						for ( int column = 0; column < 3; column++ )
						{
							TableSetColumnIndex( column );
							Text( "Hello {0},{1}", column, row );
						}
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Vertical scrolling, with clipping" ) )
			{
				HelpMarker( "Here we activate ScrollY, which will create a child window container to allow hosting scrollable contents. The header row stays frozen at the top." );
				CheckboxFlags( "ImGuiTableFlags_ScrollY", ref _tableFlagsScroll, ImGuiTableFlags.ScrollY );
				var outerSize = new Vector2( 0.0f, GetTextLineHeightWithSpacing() * 8 );
				if ( BeginTable( "table_scrolly", 3, _tableFlagsScroll, outerSize ) )
				{
					TableSetupScrollFreeze( 0, 1 );
					TableSetupColumn( "One", ImGuiTableColumnFlags.None );
					TableSetupColumn( "Two", ImGuiTableColumnFlags.None );
					TableSetupColumn( "Three", ImGuiTableColumnFlags.None );
					TableHeadersRow();
					for ( int row = 0; row < 100; row++ )
					{
						TableNextRow();
						for ( int column = 0; column < 3; column++ )
						{
							TableSetColumnIndex( column );
							Text( "Hello {0},{1}", column, row );
						}
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Sorting" ) )
			{
				_sortItems ??= Enumerable.Range( 0, 50 ).Select( n => (n, new[] { "Apple", "Banana", "Cherry", "Kiwi", "Mango", "Orange", "Pear", "Pineapple", "Strawberry", "Watermelon" }[n % 10], (n * n - n) % 20) ).ToList();
				if ( BeginTable( "table_sorting", 4, _tableFlagsSort, new Vector2( 0.0f, GetTextLineHeightWithSpacing() * 15 ) ) )
				{
					TableSetupColumn( "ID", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 0.0f, 0 );
					TableSetupColumn( "Name", ImGuiTableColumnFlags.WidthFixed, 0.0f, 1 );
					TableSetupColumn( "Action", ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthFixed, 0.0f, 2 );
					TableSetupColumn( "Quantity", ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthStretch, 0.0f, 3 );
					TableSetupScrollFreeze( 0, 1 );
					TableHeadersRow();

					var sortSpecs = TableGetSortSpecs();
					if ( sortSpecs is not null && sortSpecs.SpecsDirty )
					{
						IEnumerable<(int Id, string Name, int Quantity)> sorted = _sortItems;
						IOrderedEnumerable<(int Id, string Name, int Quantity)> ordered = null;
						for ( int n = 0; n < sortSpecs.SpecsCount; n++ )
						{
							var spec = sortSpecs.Specs[n];
							bool asc = spec.SortDirection == ImGuiSortDirection.Ascending;
							Func<(int Id, string Name, int Quantity), object> key = spec.ColumnUserID switch
							{
								1 => x => x.Name,
								3 => x => x.Quantity,
								_ => x => x.Id,
							};
							ordered = ordered is null
								? (asc ? sorted.OrderBy( key ) : sorted.OrderByDescending( key ))
								: (asc ? ordered.ThenBy( key ) : ordered.ThenByDescending( key ));
						}
						if ( ordered is not null )
							_sortItems = ordered.ToList();
						sortSpecs.SpecsDirty = false;
					}

					foreach ( var item in _sortItems )
					{
						PushID( item.Id );
						TableNextRow();
						TableNextColumn(); Text( "{0:D4}", item.Id );
						TableNextColumn(); TextUnformatted( item.Name );
						TableNextColumn(); SmallButton( "None" );
						TableNextColumn(); Text( "{0}", item.Quantity );
						PopID();
					}
					EndTable();
				}
				TreePop();
			}

			if ( TreeNode( "Legacy Columns API" ) )
			{
				HelpMarker( "Columns() is an old API! Prefer using the more flexible and powerful BeginTable() API!" );
				SliderInt( "Columns", ref _columnsCount, 1, 6 );
				Checkbox( "Borders", ref _columnsBorders );
				Columns( _columnsCount, null, _columnsBorders );
				for ( int i = 0; i < _columnsCount * 3; i++ )
				{
					if ( _columnsBorders && GetColumnIndex() == 0 )
						Separator();
					Text( "{0}{1}", (char)('a' + i), i );
					Text( "Width {0:F2}", GetColumnWidth() );
					Text( "Avail {0:F2}", GetContentRegionAvail().x );
					Text( "Offset {0:F2}", GetColumnOffset() );
					Text( "Long text that is likely to clip" );
					Button( "Button", new Vector2( -1.17549435E-38f, 0.0f ) );
					NextColumn();
				}
				Columns( 1 );
				if ( _columnsBorders )
					Separator();
				TreePop();
			}
		}

		public static void ShowInputs()
		{
			if ( !CollapsingHeader( "Inputs & Focus" ) )
				return;

			var io = GetIO();
			if ( TreeNode( "Inputs" ) )
			{
				if ( IsMousePosValid() )
					Text( "Mouse pos: ({0:F1},{1:F1})", io.MousePos.x, io.MousePos.y );
				else
					Text( "Mouse pos: <INVALID>" );
				Text( "Mouse delta: ({0:F1},{1:F1})", io.MouseDelta.x, io.MouseDelta.y );
				Text( "Mouse down:" );
				for ( int i = 0; i < io.MouseDown.Length; i++ )
					if ( IsMouseDown( (ImGuiMouseButton)Math.Min( i, 2 ) ) && i < 3 )
					{
						SameLine();
						Text( "b{0}", i );
					}
				Text( "Mouse wheel: {0:F1}", io.MouseWheel );
				Text( "Keys down:" );
				for ( var key = ImGuiKey.Tab; key < ImGuiKey.MouseLeft; key++ )
				{
					if ( !IsKeyDown( key ) ) continue;
					SameLine();
					Text( "\"{0}\"", GetKeyName( key ) );
				}
				Text( "Keys mods: {0}{1}{2}", io.KeyCtrl ? "CTRL " : "", io.KeyShift ? "SHIFT " : "", io.KeyAlt ? "ALT " : "" );
				TreePop();
			}

			if ( TreeNode( "WantCapture override" ) )
			{
				Text( "io.WantCaptureMouse: {0}", io.WantCaptureMouse );
				Text( "io.WantCaptureKeyboard: {0}", io.WantCaptureKeyboard );
				Text( "io.WantTextInput: {0}", io.WantTextInput );
				HelpMarker( "Your game should not react to mouse/keyboard input when these are true." );
				TreePop();
			}

			if ( TreeNode( "Mouse Cursors" ) )
			{
				string[] cursorNames = Enum.GetNames( typeof( ImGuiMouseCursor ) );
				Text( "Current mouse cursor = {0}", GetMouseCursor() );
				Text( "Hover to see mouse cursors:" );
				for ( int i = 1; i < cursorNames.Length; i++ )
				{
					Bullet(); Selectable( $"Mouse cursor {i - 1}: {cursorNames[i]}" );
					if ( IsItemHovered() )
						SetMouseCursor( (ImGuiMouseCursor)(i - 1) );
				}
				TreePop();
			}

			if ( TreeNode( "Focus from code" ) )
			{
				bool focus1 = Button( "Focus on 1" ); SameLine();
				bool focus2 = Button( "Focus on 2" ); SameLine();
				bool focus3 = Button( "Focus on 3" );
				if ( focus1 ) SetKeyboardFocusHere();
				InputText( "1", ref _str0 );
				if ( focus2 ) SetKeyboardFocusHere();
				InputText( "2", ref _str1 );
				if ( focus3 ) SetKeyboardFocusHere();
				InputText( "3 (tab skip)", ref _hint );
				TreePop();
			}

			if ( TreeNode( "Dragging" ) )
			{
				TextWrapped( "You can use GetMouseDragDelta(0) to query for the dragged amount on any widget." );
				Button( "Drag Me" );
				if ( IsItemActive() )
					GetForegroundDrawList().AddLine( io.MouseClickedPos[0], io.MousePos, GetColorU32( ImGuiCol.Button ), 4.0f );
				var valueRaw = GetMouseDragDelta( 0, 0.0f );
				var valueWithLockThreshold = GetMouseDragDelta( 0 );
				var mouseDelta = io.MouseDelta;
				Text( "GetMouseDragDelta(0):" );
				Text( "  w/ default threshold: ({0:F1}, {1:F1})", valueWithLockThreshold.x, valueWithLockThreshold.y );
				Text( "  w/ zero threshold: ({0:F1}, {1:F1})", valueRaw.x, valueRaw.y );
				Text( "io.MouseDelta: ({0:F1}, {1:F1})", mouseDelta.x, mouseDelta.y );
				TreePop();
			}
		}
	}
}

#endregion

#region ImGui (Demo/ImGui.Demo.Widgets)

public static partial class ImGui
{
	private static partial class Demo
	{
		// Basic
		private static int _clicked;
		private static bool _check = true;
		private static int _e;
		private static int _counter;
		private static string _str0 = "Hello, world!";
		private static string _str1 = "";
		private static int _i0 = 123;
		private static float _f0 = 0.001f;
		private static double _d0 = 999999.00000001;
		private static float _f1 = 1e10f;
		private static Vector3 _vec4a = new( 0.10f, 0.20f, 0.30f );
		private static int _i1 = 50, _i2 = 42, _i3 = 128;
		private static float _f1d = 1.00f, _f2d = 0.0067f;
		private static int _si = 0;
		private static float _sf1 = 0.123f, _sf2 = 0.0f;
		private static float _angle = 0.0f;
		private static int _elem;
		private static Vector3 _col1 = new( 1.0f, 0.0f, 0.2f );
		private static Vector4 _col2 = new( 0.4f, 0.7f, 0.0f, 0.5f );
		private static int _itemCurrent;
		private static int _listCurrent = 1;

		// Trees / headers
		private static bool _closableGroup = true;
		private static ImGuiTreeNodeFlags _baseFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth;
		private static int _treeSelectionMask = 1 << 2;

		// Text
		private static float _wrapWidth = 200.0f;

		// Combo
		private static ImGuiComboFlags _comboFlags;
		private static int _comboItemSelectedIdx;

		// Selectables
		private static readonly bool[] _selection = { false, true, false, false };
		private static int _selectedSingle = -1;
		private static readonly bool[] _selGrid = new bool[16];

		// Text input
		private static string _multiline = "/*\n The Pentium F00F bug, shorthand for F0 0F C7 C8,\n the hexadecimal encoding of one offending instruction,\n more formally, the invalid operand with locked CMPXCHG8B\n instruction bug, is a design flaw in the majority of\n Intel Pentium, Pentium MMX, and Pentium OverDrive\n processors (all in the P5 microarchitecture).\n*/\n\nlabel:\n\tlock cmpxchg8b eax\n";
		private static ImGuiInputTextFlags _multilineFlags = ImGuiInputTextFlags.AllowTabInput;
		private static string _bufDefault = "", _bufDecimal = "", _bufHex = "", _bufUpper = "", _bufNoBlank = "", _password = "password123";
		private static string _hint = "";

		// Tabs
		private static ImGuiTabBarFlags _tabBarFlags = ImGuiTabBarFlags.Reorderable;
		private static readonly bool[] _tabOpened = { true, true, true, true };
		private static readonly List<int> _activeTabs = new() { 0, 1, 2 };
		private static int _nextTabId = 3;

		// Plots
		private static bool _animate = true;
		private static readonly float[] _plotArr = { 0.6f, 0.1f, 1.0f, 0.5f, 0.92f, 0.1f, 0.2f };
		private static readonly float[] _plotValues = new float[90];
		private static int _plotValuesOffset;
		private static double _refreshTime;
		private static float _phase;
		private static float _progress, _progressDir = 1.0f;

		// Color
		private static Vector4 _color = new( 114.0f / 255.0f, 144.0f / 255.0f, 154.0f / 255.0f, 200.0f / 255.0f );
		private static bool _alphaPreview = true, _alphaHalfPreview, _dragAndDrop = true, _optionsMenu = true, _hdr;
		private static ImGuiColorEditFlags _pickerFlags = ImGuiColorEditFlags.PickerHueBar;
		private static int _pickerMode;

		// Drags & sliders
		private static ImGuiSliderFlags _sliderFlags;
		private static float _dragF = 0.5f;
		private static int _dragI = 50;
		private static float _sliderF = 0.5f;
		private static int _sliderI = 50;
		private static float _beginF = 10, _endF = 90;
		private static int _beginI = 100, _endI = 1000;
		private static Vector4 _vec4f = new( 0.10f, 0.20f, 0.30f, 0.44f );
		private static int[] _vec4i = { 1, 5, 100, 255 };
		private static readonly float[] _vValues = { 0.0f, 0.60f, 0.35f, 0.9f, 0.70f, 0.20f, 0.0f };
		private static int _vInt;

		// Drag and drop
		private static readonly string[] _dndNames = { "Bobby", "Beatrice", "Betty", "Brianna", "Barry", "Bernard", "Bibi", "Blaine", "Bryn" };
		private static int _dndMode;

		// Item status
		private static int _itemType = 4;
		private static bool _itemDisabled;
		private static bool _bStatus;
		private static Vector4 _colStatus = new( 1, 0.5f, 0, 1 );
		private static string _strStatus = "";
		private static int _currentStatus = 1;

		// Disable
		private static bool _disableAll;

		public static void ShowWidgets()
		{
			if ( !CollapsingHeader( "Widgets" ) )
				return;

			if ( _disableAll )
				BeginDisabled();

			if ( TreeNode( "Basic" ) )
			{
				SeparatorText( "General" );
				if ( Button( "Button" ) ) _clicked++;
				if ( (_clicked & 1) != 0 )
				{
					SameLine();
					Text( "Thanks for clicking me!" );
				}

				Checkbox( "checkbox", ref _check );

				RadioButton( "radio a", ref _e, 0 ); SameLine();
				RadioButton( "radio b", ref _e, 1 ); SameLine();
				RadioButton( "radio c", ref _e, 2 );

				// Colored buttons
				for ( int i = 0; i < 7; i++ )
				{
					if ( i > 0 ) SameLine();
					PushID( i );
					ColorConvertHSVtoRGB( i / 7.0f, 0.6f, 0.6f, out var r1, out var g1, out var b1 );
					ColorConvertHSVtoRGB( i / 7.0f, 0.7f, 0.7f, out var r2, out var g2, out var b2 );
					ColorConvertHSVtoRGB( i / 7.0f, 0.8f, 0.8f, out var r3, out var g3, out var b3 );
					PushStyleColor( ImGuiCol.Button, new Vector4( r1, g1, b1, 1 ) );
					PushStyleColor( ImGuiCol.ButtonHovered, new Vector4( r2, g2, b2, 1 ) );
					PushStyleColor( ImGuiCol.ButtonActive, new Vector4( r3, g3, b3, 1 ) );
					Button( "Click" );
					PopStyleColor( 3 );
					PopID();
				}

				AlignTextToFramePadding();
				Text( "Hold to repeat:" );
				SameLine();
				float spacing = GetStyle().ItemInnerSpacing.x;
				PushItemFlag( ImGuiItemFlags.ButtonRepeat, true );
				if ( ArrowButton( "##left", ImGuiDir.Left ) ) _counter--;
				SameLine( 0.0f, spacing );
				if ( ArrowButton( "##right", ImGuiDir.Right ) ) _counter++;
				PopItemFlag();
				SameLine();
				Text( "{0}", _counter );

				Button( "Tooltip" );
				SetItemTooltip( "I am a tooltip" );

				LabelText( "label", "Value" );

				SeparatorText( "Inputs" );
				InputText( "input text", ref _str0 );
				SameLine(); HelpMarker( "USER:\nHold SHIFT or use mouse to select text.\nCTRL+Left/Right to word jump.\nCTRL+A or Double-Click to select all.\nCTRL+X,CTRL+C,CTRL+V clipboard.\nCTRL+Z,CTRL+Y undo/redo.\nESCAPE to revert." );
				InputTextWithHint( "input text (w/ hint)", "enter text here", ref _str1 );
				InputInt( "input int", ref _i0 );
				InputFloat( "input float", ref _f0, 0.01f, 1.0f, "%.3f" );
				InputDouble( "input double", ref _d0, 0.01, 1.0, "%.8f" );
				InputFloat( "input scientific", ref _f1, 0.0f, 0.0f, "%e" );
				InputFloat3( "input float3", ref _vec4a );

				SeparatorText( "Drags" );
				DragInt( "drag int", ref _i1, 1 );
				SameLine(); HelpMarker( "Click and drag to edit value.\nHold SHIFT/ALT for faster/slower edit.\nDouble-click or CTRL+click to input value." );
				DragInt( "drag int 0..100", ref _i2, 1, 0, 100, "%d%%", ImGuiSliderFlags.AlwaysClamp );
				DragInt( "drag int wrap 100..200", ref _i3, 1, 100, 200, "%d", ImGuiSliderFlags.WrapAround );
				DragFloat( "drag float", ref _f1d, 0.005f );
				DragFloat( "drag small float", ref _f2d, 0.0001f, 0.0f, 0.0f, "%.06f ns" );

				SeparatorText( "Sliders" );
				SliderInt( "slider int", ref _si, -1, 3 );
				SliderFloat( "slider float", ref _sf1, 0.0f, 1.0f, "ratio = %.3f" );
				SliderFloat( "slider float (log)", ref _sf2, -10.0f, 10.0f, "%.4f", ImGuiSliderFlags.Logarithmic );
				SliderAngle( "slider angle", ref _angle );
				string[] elemsNames = { "Fire", "Earth", "Air", "Water" };
				string elemName = _elem >= 0 && _elem < 4 ? elemsNames[_elem] : "Unknown";
				SliderInt( "slider enum", ref _elem, 0, 3, elemName );

				SeparatorText( "Selectors/Pickers" );
				ColorEdit3( "color 1", ref _col1 );
				ColorEdit4( "color 2", ref _col2 );
				string[] items = { "AAAA", "BBBB", "CCCC", "DDDD", "EEEE", "FFFF", "GGGG", "HHHH", "IIIIIII", "JJJJ", "KKKKKKK" };
				Combo( "combo", ref _itemCurrent, items );
				string[] fruits = { "Apple", "Banana", "Cherry", "Kiwi", "Mango", "Orange", "Pineapple", "Strawberry", "Watermelon" };
				ListBox( "listbox", ref _listCurrent, fruits, 4 );

				TreePop();
			}

			if ( TreeNode( "Trees" ) )
			{
				if ( TreeNode( "Basic trees" ) )
				{
					for ( int i = 0; i < 5; i++ )
					{
						if ( i == 0 )
							SetNextItemOpen( true, ImGuiCond.Once );
						PushID( i );
						if ( TreeNode( "", "Child {0}", i ) )
						{
							Text( "blah blah" );
							SameLine();
							if ( SmallButton( "button" ) ) { }
							TreePop();
						}
						PopID();
					}
					TreePop();
				}

				if ( TreeNode( "Advanced, with Selectable nodes" ) )
				{
					CheckboxFlags( "ImGuiTreeNodeFlags_OpenOnArrow", ref _baseFlags, ImGuiTreeNodeFlags.OpenOnArrow );
					CheckboxFlags( "ImGuiTreeNodeFlags_OpenOnDoubleClick", ref _baseFlags, ImGuiTreeNodeFlags.OpenOnDoubleClick );
					CheckboxFlags( "ImGuiTreeNodeFlags_SpanAvailWidth", ref _baseFlags, ImGuiTreeNodeFlags.SpanAvailWidth );
					CheckboxFlags( "ImGuiTreeNodeFlags_SpanFullWidth", ref _baseFlags, ImGuiTreeNodeFlags.SpanFullWidth );

					int nodeClicked = -1;
					for ( int i = 0; i < 6; i++ )
					{
						var nodeFlags = _baseFlags;
						bool isSelected = (_treeSelectionMask & (1 << i)) != 0;
						if ( isSelected )
							nodeFlags |= ImGuiTreeNodeFlags.Selected;
						if ( i < 3 )
						{
							bool nodeOpen = TreeNodeEx( $"node{i}", nodeFlags, "Selectable Node {0}", i );
							if ( IsItemClicked() && !IsItemToggledOpen() )
								nodeClicked = i;
							if ( nodeOpen )
							{
								BulletText( "Blah blah\nBlah Blah" );
								TreePop();
							}
						}
						else
						{
							nodeFlags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
							TreeNodeEx( $"leaf{i}", nodeFlags, "Selectable Leaf {0}", i );
							if ( IsItemClicked() && !IsItemToggledOpen() )
								nodeClicked = i;
						}
					}
					if ( nodeClicked != -1 )
					{
						if ( GetIO().KeyCtrl )
							_treeSelectionMask ^= 1 << nodeClicked;
						else
							_treeSelectionMask = 1 << nodeClicked;
					}
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Collapsing Headers" ) )
			{
				Checkbox( "Show 2nd header", ref _closableGroup );
				if ( CollapsingHeader( "Header", ImGuiTreeNodeFlags.None ) )
				{
					Text( "IsItemHovered: {0}", IsItemHovered() );
					for ( int i = 0; i < 5; i++ )
						Text( "Some content {0}", i );
				}
				if ( CollapsingHeader( "Header with a close button", ref _closableGroup ) )
				{
					Text( "IsItemHovered: {0}", IsItemHovered() );
					for ( int i = 0; i < 5; i++ )
						Text( "More content {0}", i );
				}
				TreePop();
			}

			if ( TreeNode( "Bullets" ) )
			{
				BulletText( "Bullet point 1" );
				BulletText( "Bullet point 2\nOn multiple lines" );
				if ( TreeNode( "Tree node" ) )
				{
					BulletText( "Another bullet point" );
					TreePop();
				}
				Bullet(); Text( "Bullet point 3 (two calls)" );
				Bullet(); SmallButton( "Button" );
				TreePop();
			}

			if ( TreeNode( "Text" ) )
			{
				if ( TreeNode( "Colorful Text" ) )
				{
					TextColored( new Vector4( 1.0f, 0.0f, 1.0f, 1.0f ), "Pink" );
					TextColored( new Vector4( 1.0f, 1.0f, 0.0f, 1.0f ), "Yellow" );
					TextDisabled( "Disabled" );
					SameLine(); HelpMarker( "The TextDisabled color is stored in ImGuiStyle." );
					TreePop();
				}

				if ( TreeNode( "Word Wrapping" ) )
				{
					TextWrapped( "This text should automatically wrap on the edge of the window. The current implementation for text wrapping follows simple rules suitable for English and possibly other languages." );
					Spacing();
					SliderFloat( "Wrap width", ref _wrapWidth, -20, 600, "%.0f" );
					var drawList = GetWindowDrawList();
					for ( int n = 0; n < 2; n++ )
					{
						Text( "Test paragraph {0}:", n );
						var pos = GetCursorScreenPos();
						var markerMin = new Vector2( pos.x + _wrapWidth, pos.y );
						var markerMax = new Vector2( pos.x + _wrapWidth + 10, pos.y + GetTextLineHeight() );
						PushTextWrapPos( GetCursorPos().x + _wrapWidth );
						if ( n == 0 )
							Text( "The lazy dog is a good dog. This paragraph should fit within {0} pixels. Testing a 1 character word. The quick brown fox jumps over the lazy dog.", (int)_wrapWidth );
						else
							Text( "aaaaaaaa bbbbbbbb, c cccccccc,dddddddd. d eeeeeeee   ffffffff. gggggggg!hhhhhhhh" );
						drawList.AddRect( GetItemRectMin(), GetItemRectMax(), new Color32( 255, 255, 0, 255 ) );
						drawList.AddRectFilled( markerMin, markerMax, new Color32( 255, 0, 255, 255 ) );
						PopTextWrapPos();
					}
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Images" ) )
			{
				TextWrapped( "Images take an s&box Texture. Here is the white texture tinted, and a UV sub-rect." );
				var tex = Texture.White;
				Image( tex, new Vector2( 64, 64 ), Vector2.Zero, Vector2.One, new Vector4( 0.3f, 0.6f, 1, 1 ), new Vector4( 1, 1, 1, 0.5f ) );
				SameLine();
				if ( ImageButton( "imgbtn", tex, new Vector2( 32, 32 ), Vector2.Zero, Vector2.One, new Vector4( 0, 0, 0, 1 ), new Vector4( 1, 0.4f, 0.4f, 1 ) ) )
					_clicked++;
				TreePop();
			}

			if ( TreeNode( "Combo" ) )
			{
				CheckboxFlags( "ImGuiComboFlags_PopupAlignLeft", ref _comboFlags, ImGuiComboFlags.PopupAlignLeft );
				if ( CheckboxFlags( "ImGuiComboFlags_NoArrowButton", ref _comboFlags, ImGuiComboFlags.NoArrowButton ) )
					_comboFlags &= ~ImGuiComboFlags.NoPreview;
				if ( CheckboxFlags( "ImGuiComboFlags_NoPreview", ref _comboFlags, ImGuiComboFlags.NoPreview ) )
					_comboFlags &= ~(ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.WidthFitPreview);
				if ( CheckboxFlags( "ImGuiComboFlags_WidthFitPreview", ref _comboFlags, ImGuiComboFlags.WidthFitPreview ) )
					_comboFlags &= ~ImGuiComboFlags.NoPreview;

				string[] items = { "AAAA", "BBBB", "CCCC", "DDDD", "EEEE", "FFFF", "GGGG", "HHHH", "IIII", "JJJJ", "KKKK", "LLLLLLL", "MMMM", "OOOOOOO" };
				if ( BeginCombo( "combo 1", items[_comboItemSelectedIdx], _comboFlags ) )
				{
					for ( int n = 0; n < items.Length; n++ )
					{
						bool isSelected = _comboItemSelectedIdx == n;
						if ( Selectable( items[n], isSelected ) )
							_comboItemSelectedIdx = n;
						if ( isSelected )
							SetItemDefaultFocus();
					}
					EndCombo();
				}
				Combo( "combo 2 (one-liner)", ref _comboItemSelectedIdx, "aaaa\0bbbb\0cccc\0dddd\0eeee\0\0" );
				TreePop();
			}

			if ( TreeNode( "Selectables" ) )
			{
				if ( TreeNode( "Basic" ) )
				{
					Selectable( "1. I am selectable", ref _selection[0] );
					Selectable( "2. I am selectable", ref _selection[1] );
					Selectable( "3. I am selectable", ref _selection[2] );
					if ( Selectable( "4. I am double clickable", _selection[3], ImGuiSelectableFlags.AllowDoubleClick ) )
						if ( IsMouseDoubleClicked( ImGuiMouseButton.Left ) )
							_selection[3] = !_selection[3];
					TreePop();
				}
				if ( TreeNode( "Single-Select" ) )
				{
					for ( int n = 0; n < 5; n++ )
						if ( Selectable( $"Object {n}", _selectedSingle == n ) )
							_selectedSingle = n;
					TreePop();
				}
				if ( TreeNode( "Grid" ) )
				{
					for ( int y = 0; y < 4; y++ )
						for ( int x = 0; x < 4; x++ )
						{
							if ( x > 0 ) SameLine();
							PushID( y * 4 + x );
							if ( Selectable( "Sailor", _selGrid[y * 4 + x], ImGuiSelectableFlags.None, new Vector2( 50, 50 ) ) )
							{
								_selGrid[y * 4 + x] ^= true;
								if ( x > 0 ) _selGrid[y * 4 + x - 1] ^= true;
								if ( x < 3 ) _selGrid[y * 4 + x + 1] ^= true;
								if ( y > 0 ) _selGrid[(y - 1) * 4 + x] ^= true;
								if ( y < 3 ) _selGrid[(y + 1) * 4 + x] ^= true;
							}
							PopID();
						}
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Text Input" ) )
			{
				if ( TreeNode( "Multi-line Text Input" ) )
				{
					CheckboxFlags( "ImGuiInputTextFlags_ReadOnly", ref _multilineFlags, ImGuiInputTextFlags.ReadOnly );
					CheckboxFlags( "ImGuiInputTextFlags_AllowTabInput", ref _multilineFlags, ImGuiInputTextFlags.AllowTabInput );
					CheckboxFlags( "ImGuiInputTextFlags_CtrlEnterForNewLine", ref _multilineFlags, ImGuiInputTextFlags.CtrlEnterForNewLine );
					InputTextMultiline( "##source", ref _multiline, new Vector2( -1.17549435E-38f, GetTextLineHeight() * 16 ), _multilineFlags );
					TreePop();
				}
				if ( TreeNode( "Filtered Text Input" ) )
				{
					InputText( "default", ref _bufDefault );
					InputText( "decimal", ref _bufDecimal, ImGuiInputTextFlags.CharsDecimal );
					InputText( "hexadecimal", ref _bufHex, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase );
					InputText( "uppercase", ref _bufUpper, ImGuiInputTextFlags.CharsUppercase );
					InputText( "no blank", ref _bufNoBlank, ImGuiInputTextFlags.CharsNoBlank );
					TreePop();
				}
				if ( TreeNode( "Password Input" ) )
				{
					InputText( "password", ref _password, ImGuiInputTextFlags.Password );
					SameLine(); HelpMarker( "Display all characters as '*'." );
					InputTextWithHint( "password (w/ hint)", "<password>", ref _password, ImGuiInputTextFlags.Password );
					InputText( "password (clear)", ref _password );
					TreePop();
				}
				if ( TreeNode( "Hint" ) )
				{
					InputTextWithHint( "##hint", "type something...", ref _hint );
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Tabs" ) )
			{
				if ( TreeNode( "Basic" ) )
				{
					if ( BeginTabBar( "MyTabBar" ) )
					{
						if ( BeginTabItem( "Avocado" ) )
						{
							Text( "This is the Avocado tab!\nblah blah blah blah blah" );
							EndTabItem();
						}
						if ( BeginTabItem( "Broccoli" ) )
						{
							Text( "This is the Broccoli tab!\nblah blah blah blah blah" );
							EndTabItem();
						}
						if ( BeginTabItem( "Cucumber" ) )
						{
							Text( "This is the Cucumber tab!\nblah blah blah blah blah" );
							EndTabItem();
						}
						EndTabBar();
					}
					Separator();
					TreePop();
				}

				if ( TreeNode( "Advanced & Close Button" ) )
				{
					CheckboxFlags( "ImGuiTabBarFlags_Reorderable", ref _tabBarFlags, ImGuiTabBarFlags.Reorderable );
					CheckboxFlags( "ImGuiTabBarFlags_AutoSelectNewTabs", ref _tabBarFlags, ImGuiTabBarFlags.AutoSelectNewTabs );
					CheckboxFlags( "ImGuiTabBarFlags_TabListPopupButton", ref _tabBarFlags, ImGuiTabBarFlags.TabListPopupButton );
					CheckboxFlags( "ImGuiTabBarFlags_NoCloseWithMiddleMouseButton", ref _tabBarFlags, ImGuiTabBarFlags.NoCloseWithMiddleMouseButton );
					if ( (_tabBarFlags & ImGuiTabBarFlags.FittingPolicyMask_) == 0 )
						_tabBarFlags |= ImGuiTabBarFlags.FittingPolicyDefault_;
					if ( CheckboxFlags( "ImGuiTabBarFlags_FittingPolicyResizeDown", ref _tabBarFlags, ImGuiTabBarFlags.FittingPolicyResizeDown ) )
						_tabBarFlags &= ~(ImGuiTabBarFlags.FittingPolicyMask_ ^ ImGuiTabBarFlags.FittingPolicyResizeDown);
					if ( CheckboxFlags( "ImGuiTabBarFlags_FittingPolicyScroll", ref _tabBarFlags, ImGuiTabBarFlags.FittingPolicyScroll ) )
						_tabBarFlags &= ~(ImGuiTabBarFlags.FittingPolicyMask_ ^ ImGuiTabBarFlags.FittingPolicyScroll);

					string[] names = { "Artichoke", "Beetroot", "Celery", "Daikon" };
					for ( int n = 0; n < _tabOpened.Length; n++ )
					{
						if ( n > 0 ) SameLine();
						Checkbox( names[n], ref _tabOpened[n] );
					}

					if ( BeginTabBar( "MyTabBar2", _tabBarFlags ) )
					{
						for ( int n = 0; n < _tabOpened.Length; n++ )
						{
							if ( _tabOpened[n] && BeginTabItem( names[n], ref _tabOpened[n], ImGuiTabItemFlags.None ) )
							{
								Text( "This is the {0} tab!", names[n] );
								if ( (n & 1) != 0 )
									Text( "I am an odd tab." );
								EndTabItem();
							}
						}
						EndTabBar();
					}
					Separator();
					TreePop();
				}

				if ( TreeNode( "TabItemButton & Leading/Trailing flags" ) )
				{
					if ( BeginTabBar( "MyTabBar3", ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.FittingPolicyResizeDown ) )
					{
						if ( TabItemButton( "?", ImGuiTabItemFlags.Leading | ImGuiTabItemFlags.NoTooltip ) )
							OpenPopup( "MyHelpMenu" );
						if ( BeginPopup( "MyHelpMenu" ) )
						{
							Selectable( "Hello!" );
							EndPopup();
						}
						if ( TabItemButton( "+", ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip ) )
							_activeTabs.Add( _nextTabId++ );

						for ( int n = 0; n < _activeTabs.Count; )
						{
							bool open = true;
							string name = $"{_activeTabs[n]:D4}";
							if ( BeginTabItem( name, ref open, ImGuiTabItemFlags.None ) )
							{
								Text( "This is the {0} tab!", name );
								EndTabItem();
							}
							if ( !open )
								_activeTabs.RemoveAt( n );
							else
								n++;
						}
						EndTabBar();
					}
					Separator();
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Plotting" ) )
			{
				Checkbox( "Animate", ref _animate );
				PlotLines( "Frame Times", _plotArr );
				PlotHistogram( "Histogram", _plotArr, 0, null, 0.0f, 1.0f, new Vector2( 0, 80.0f ) );

				if ( !_animate || _refreshTime == 0.0 )
					_refreshTime = GetTime();
				while ( _refreshTime < GetTime() )
				{
					_plotValues[_plotValuesOffset] = MathF.Cos( _phase );
					_plotValuesOffset = (_plotValuesOffset + 1) % _plotValues.Length;
					_phase += 0.10f * _plotValuesOffset;
					_refreshTime += 1.0f / 60.0f;
				}
				float average = _plotValues.Average();
				PlotLines( "Lines", _plotValues, _plotValuesOffset, $"avg {average:F6}", -1.0f, 1.0f, new Vector2( 0, 80.0f ) );

				Func<int, float> sinFn = i => MathF.Sin( i * 0.1f );
				Func<int, float> sawFn = i => (i & 1) != 0 ? 1.0f : -1.0f;
				PlotLines( "Sin", sinFn, 70, 0, null, -1.0f, 1.0f, new Vector2( 0, 80 ) );
				PlotHistogram( "Saw", sawFn, 70, 0, null, -1.0f, 1.0f, new Vector2( 0, 80 ) );

				SeparatorText( "Progress Bars" );
				if ( _animate )
				{
					_progress += _progressDir * 0.4f * GetIO().DeltaTime;
					if ( _progress >= +1.1f ) { _progress = +1.1f; _progressDir *= -1.0f; }
					if ( _progress <= -0.1f ) { _progress = -0.1f; _progressDir *= -1.0f; }
				}
				ProgressBar( _progress, new Vector2( 0.0f, 0.0f ) );
				SameLine( 0.0f, GetStyle().ItemInnerSpacing.x );
				Text( "Progress Bar" );
				float progressSaturated = Math.Clamp( _progress, 0.0f, 1.0f );
				ProgressBar( _progress, new Vector2( 0, 0 ), $"{(int)(progressSaturated * 1753)}/1753" );
				ProgressBar( -1.0f * (float)GetTime(), new Vector2( 0.0f, 0.0f ), "Searching.." );
				TreePop();
			}

			if ( TreeNode( "Color/Picker Widgets" ) )
			{
				SeparatorText( "Options" );
				Checkbox( "With Alpha Preview", ref _alphaPreview );
				Checkbox( "With Half Alpha Preview", ref _alphaHalfPreview );
				Checkbox( "With Drag and Drop", ref _dragAndDrop );
				Checkbox( "With Options Menu", ref _optionsMenu ); SameLine(); HelpMarker( "Right-click on the individual color widget to show options." );
				Checkbox( "With HDR", ref _hdr );
				var miscFlags = (_hdr ? ImGuiColorEditFlags.HDR : 0) | (_dragAndDrop ? 0 : ImGuiColorEditFlags.NoDragDrop) | (_alphaHalfPreview ? ImGuiColorEditFlags.AlphaPreviewHalf : (_alphaPreview ? 0 : ImGuiColorEditFlags.AlphaOpaque)) | (_optionsMenu ? 0 : ImGuiColorEditFlags.NoOptions);

				SeparatorText( "Inline color editor" );
				ColorEdit3( "MyColor##1", ref _color, miscFlags );
				ColorEdit4( "MyColor##2", ref _color, ImGuiColorEditFlags.DisplayHSV | miscFlags );
				ColorEdit4( "MyColor##2f", ref _color, ImGuiColorEditFlags.Float | miscFlags );
				ColorEdit4( "MyColor##3", ref _color, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | miscFlags );
				SameLine(); Text( "Color button only" );

				SeparatorText( "Color button" );
				ColorButton( "MyColor##3c", _color, miscFlags, new Vector2( 80, 80 ) );

				SeparatorText( "Color picker" );
				Combo( "Display Mode", ref _pickerMode, "Auto/Current\0Hue bar + SV rect\0Hue wheel + SV triangle\0" );
				var flags = miscFlags | ImGuiColorEditFlags.AlphaBar;
				if ( _pickerMode == 1 ) flags |= ImGuiColorEditFlags.PickerHueBar;
				if ( _pickerMode == 2 ) flags |= ImGuiColorEditFlags.PickerHueWheel;
				ColorPicker4( "MyColor##4", ref _color, flags );
				TreePop();
			}

			if ( TreeNode( "Drag/Slider Flags" ) )
			{
				CheckboxFlags( "ImGuiSliderFlags_AlwaysClamp", ref _sliderFlags, ImGuiSliderFlags.AlwaysClamp );
				CheckboxFlags( "ImGuiSliderFlags_Logarithmic", ref _sliderFlags, ImGuiSliderFlags.Logarithmic );
				CheckboxFlags( "ImGuiSliderFlags_NoRoundToFormat", ref _sliderFlags, ImGuiSliderFlags.NoRoundToFormat );
				CheckboxFlags( "ImGuiSliderFlags_NoInput", ref _sliderFlags, ImGuiSliderFlags.NoInput );
				CheckboxFlags( "ImGuiSliderFlags_WrapAround", ref _sliderFlags, ImGuiSliderFlags.WrapAround );

				SeparatorText( "Drags" );
				DragFloat( "DragFloat (0 -> 1)", ref _dragF, 0.005f, 0.0f, 1.0f, "%.3f", _sliderFlags );
				DragFloat( "DragFloat (0 -> +inf)", ref _dragF, 0.005f, 0.0f, float.MaxValue, "%.3f", _sliderFlags );
				DragInt( "DragInt (0 -> 100)", ref _dragI, 0.5f, 0, 100, "%d", _sliderFlags );

				SeparatorText( "Sliders" );
				SliderFloat( "SliderFloat (0 -> 1)", ref _sliderF, 0.0f, 1.0f, "%.3f", _sliderFlags );
				SliderInt( "SliderInt (0 -> 100)", ref _sliderI, 0, 100, "%d", _sliderFlags );
				TreePop();
			}

			if ( TreeNode( "Range Widgets" ) )
			{
				DragFloatRange2( "range float", ref _beginF, ref _endF, 0.25f, 0.0f, 100.0f, "Min: %.1f %%", "Max: %.1f %%", ImGuiSliderFlags.AlwaysClamp );
				DragIntRange2( "range int", ref _beginI, ref _endI, 5, 0, 1000, "Min: %d units", "Max: %d units" );
				TreePop();
			}

			if ( TreeNode( "Multi-component Widgets" ) )
			{
				var v2 = new Vector2( _vec4f.x, _vec4f.y );
				var v3 = new Vector3( _vec4f.x, _vec4f.y, _vec4f.z );
				SeparatorText( "2-wide" );
				if ( InputFloat2( "input float2", ref v2 ) ) { _vec4f.x = v2.x; _vec4f.y = v2.y; }
				if ( DragFloat2( "drag float2", ref v2, 0.01f, 0.0f, 1.0f ) ) { _vec4f.x = v2.x; _vec4f.y = v2.y; }
				if ( SliderFloat2( "slider float2", ref v2, 0.0f, 1.0f ) ) { _vec4f.x = v2.x; _vec4f.y = v2.y; }
				SeparatorText( "3-wide" );
				if ( InputFloat3( "input float3", ref v3 ) ) { _vec4f.x = v3.x; _vec4f.y = v3.y; _vec4f.z = v3.z; }
				if ( DragFloat3( "drag float3", ref v3, 0.01f, 0.0f, 1.0f ) ) { _vec4f.x = v3.x; _vec4f.y = v3.y; _vec4f.z = v3.z; }
				if ( SliderFloat3( "slider float3", ref v3, 0.0f, 1.0f ) ) { _vec4f.x = v3.x; _vec4f.y = v3.y; _vec4f.z = v3.z; }
				SeparatorText( "4-wide" );
				InputFloat4( "input float4", ref _vec4f );
				DragFloat4( "drag float4", ref _vec4f, 0.01f, 0.0f, 1.0f );
				SliderFloat4( "slider float4", ref _vec4f, 0.0f, 1.0f );
				InputInt4( "input int4", _vec4i );
				DragInt4( "drag int4", _vec4i, 1, 0, 255 );
				SliderInt4( "slider int4", _vec4i, 0, 255 );
				TreePop();
			}

			if ( TreeNode( "Vertical Sliders" ) )
			{
				const float spacing = 4;
				PushStyleVar( ImGuiStyleVar.ItemSpacing, new Vector2( spacing, spacing ) );
				VSliderInt( "##int", new Vector2( 18, 160 ), ref _vInt, 0, 5 );
				SameLine();
				PushID( "set1" );
				for ( int i = 0; i < 7; i++ )
				{
					if ( i > 0 ) SameLine();
					PushID( i );
					ColorConvertHSVtoRGB( i / 7.0f, 0.5f, 0.5f, out var r, out var gg, out var b );
					PushStyleColor( ImGuiCol.FrameBg, new Vector4( r, gg, b, 1 ) );
					ColorConvertHSVtoRGB( i / 7.0f, 0.9f, 0.9f, out r, out gg, out b );
					PushStyleColor( ImGuiCol.SliderGrab, new Vector4( r, gg, b, 1 ) );
					VSliderFloat( "##v", new Vector2( 18, 160 ), ref _vValues[i], 0.0f, 1.0f, "" );
					if ( IsItemActive() || IsItemHovered() )
						SetTooltip( "{0:F3}", _vValues[i] );
					PopStyleColor( 2 );
					PopID();
				}
				PopID();
				PopStyleVar();
				TreePop();
			}

			if ( TreeNode( "Drag and Drop" ) )
			{
				if ( TreeNode( "Drag and drop in standard widgets" ) )
				{
					HelpMarker( "You can drag from the color squares." );
					ColorEdit3( "color 1", ref _col1 );
					ColorEdit4( "color 2", ref _col2 );
					TreePop();
				}

				if ( TreeNode( "Drag and drop to copy/swap items" ) )
				{
					RadioButton( "Copy", ref _dndMode, 0 ); SameLine();
					RadioButton( "Move", ref _dndMode, 1 ); SameLine();
					RadioButton( "Swap", ref _dndMode, 2 );
					for ( int n = 0; n < _dndNames.Length; n++ )
					{
						PushID( n );
						if ( (n % 3) != 0 )
							SameLine();
						Button( _dndNames[n], new Vector2( 60, 60 ) );

						if ( BeginDragDropSource( ImGuiDragDropFlags.None ) )
						{
							SetDragDropPayload( "DND_DEMO_CELL", n );
							if ( _dndMode == 0 ) Text( "Copy {0}", _dndNames[n] );
							if ( _dndMode == 1 ) Text( "Move {0}", _dndNames[n] );
							if ( _dndMode == 2 ) Text( "Swap {0}", _dndNames[n] );
							EndDragDropSource();
						}
						if ( BeginDragDropTarget() )
						{
							var payload = AcceptDragDropPayload( "DND_DEMO_CELL" );
							if ( payload is not null )
							{
								int nPayload = payload.GetData<int>();
								if ( _dndMode == 0 )
									_dndNames[n] = _dndNames[nPayload];
								if ( _dndMode == 1 )
								{
									_dndNames[n] = _dndNames[nPayload];
									_dndNames[nPayload] = "";
								}
								if ( _dndMode == 2 )
									(_dndNames[n], _dndNames[nPayload]) = (_dndNames[nPayload], _dndNames[n]);
							}
							EndDragDropTarget();
						}
						PopID();
					}
					TreePop();
				}
				TreePop();
			}

			if ( TreeNode( "Querying Item Status (Edited/Active/Hovered etc.)" ) )
			{
				string[] itemNames = { "Text", "Button", "Button (w/ repeat)", "Checkbox", "SliderFloat", "InputText", "InputTextMultiline", "InputFloat", "InputFloat3", "ColorEdit4", "Selectable", "MenuItem", "TreeNode", "TreeNode (w/ double-click)", "Combo", "ListBox" };
				Combo( "Item Type", ref _itemType, itemNames, itemNames.Length );
				SameLine();
				Checkbox( "Item Disabled", ref _itemDisabled );

				bool ret = false;
				if ( _itemDisabled ) BeginDisabled();
				if ( _itemType == 0 ) Text( "ITEM: Text" );
				if ( _itemType == 1 ) ret = Button( "ITEM: Button" );
				if ( _itemType == 2 ) { PushItemFlag( ImGuiItemFlags.ButtonRepeat, true ); ret = Button( "ITEM: Button" ); PopItemFlag(); }
				if ( _itemType == 3 ) ret = Checkbox( "ITEM: Checkbox", ref _bStatus );
				if ( _itemType == 4 ) { float fx = _colStatus.x; ret = SliderFloat( "ITEM: SliderFloat", ref fx, 0.0f, 1.0f ); _colStatus.x = fx; }
				if ( _itemType == 5 ) ret = InputText( "ITEM: InputText", ref _strStatus );
				if ( _itemType == 6 ) ret = InputTextMultiline( "ITEM: InputTextMultiline", ref _strStatus );
				if ( _itemType == 7 ) { float fx = _colStatus.x; ret = InputFloat( "ITEM: InputFloat", ref fx, 1.0f ); _colStatus.x = fx; }
				if ( _itemType == 8 ) { var v = new Vector3( _colStatus.x, _colStatus.y, _colStatus.z ); ret = InputFloat3( "ITEM: InputFloat3", ref v ); }
				if ( _itemType == 9 ) ret = ColorEdit4( "ITEM: ColorEdit4", ref _colStatus );
				if ( _itemType == 10 ) ret = Selectable( "ITEM: Selectable" );
				if ( _itemType == 11 ) ret = MenuItem( "ITEM: MenuItem" );
				if ( _itemType == 12 ) { ret = TreeNode( "ITEM: TreeNode" ); if ( ret ) TreePop(); }
				if ( _itemType == 13 ) ret = TreeNodeEx( "ITEM: TreeNode w/ ImGuiTreeNodeFlags_OpenOnDoubleClick", ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.NoTreePushOnOpen );
				if ( _itemType == 14 ) ret = Combo( "ITEM: Combo", ref _currentStatus, new[] { "Apple", "Banana", "Cherry", "Kiwi" } );
				if ( _itemType == 15 ) ret = ListBox( "ITEM: ListBox", ref _currentStatus, new[] { "Apple", "Banana", "Cherry", "Kiwi" } );

				bool hoveredDelayNone = IsItemHovered();
				bool hoveredDelayShort = IsItemHovered( ImGuiHoveredFlags.DelayShort );
				bool hoveredDelayNormal = IsItemHovered( ImGuiHoveredFlags.DelayNormal );
				BulletText( "Return value = {0}", ret );
				BulletText( "IsItemFocused() = {0}", IsItemFocused() );
				BulletText( "IsItemHovered() = {0}", IsItemHovered() );
				BulletText( "IsItemHovered(_AllowWhenBlockedByPopup) = {0}", IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup ) );
				BulletText( "IsItemHovered(_AllowWhenBlockedByActiveItem) = {0}", IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByActiveItem ) );
				BulletText( "IsItemHovered(_AllowWhenOverlappedByItem) = {0}", IsItemHovered( ImGuiHoveredFlags.AllowWhenOverlappedByItem ) );
				BulletText( "IsItemHovered(_RectOnly) = {0}", IsItemHovered( ImGuiHoveredFlags.RectOnly ) );
				BulletText( "IsItemActive() = {0}", IsItemActive() );
				BulletText( "IsItemEdited() = {0}", IsItemEdited() );
				BulletText( "IsItemActivated() = {0}", IsItemActivated() );
				BulletText( "IsItemDeactivated() = {0}", IsItemDeactivated() );
				BulletText( "IsItemDeactivatedAfterEdit() = {0}", IsItemDeactivatedAfterEdit() );
				BulletText( "IsItemVisible() = {0}", IsItemVisible() );
				BulletText( "IsItemClicked() = {0}", IsItemClicked() );
				BulletText( "IsItemToggledOpen() = {0}", IsItemToggledOpen() );
				BulletText( "GetItemRectMin() = ({0:F1}, {1:F1})", GetItemRectMin().x, GetItemRectMin().y );
				BulletText( "GetItemRectMax() = ({0:F1}, {1:F1})", GetItemRectMax().x, GetItemRectMax().y );
				BulletText( "GetItemRectSize() = ({0:F1}, {1:F1})", GetItemRectSize().x, GetItemRectSize().y );
				BulletText( "w/ Hovering Delay: None = {0}, Fast = {1}, Normal = {2}", hoveredDelayNone, hoveredDelayShort, hoveredDelayNormal );
				if ( _itemDisabled ) EndDisabled();
				TreePop();
			}

			if ( TreeNode( "Querying Window Status (Focused/Hovered etc.)" ) )
			{
				BulletText( "IsWindowFocused() = {0}", IsWindowFocused() );
				BulletText( "IsWindowFocused(_ChildWindows) = {0}", IsWindowFocused( ImGuiFocusedFlags.ChildWindows ) );
				BulletText( "IsWindowFocused(_RootWindow) = {0}", IsWindowFocused( ImGuiFocusedFlags.RootWindow ) );
				BulletText( "IsWindowFocused(_AnyWindow) = {0}", IsWindowFocused( ImGuiFocusedFlags.AnyWindow ) );
				BulletText( "IsWindowHovered() = {0}", IsWindowHovered() );
				BulletText( "IsWindowHovered(_ChildWindows) = {0}", IsWindowHovered( ImGuiHoveredFlags.ChildWindows ) );
				BulletText( "IsWindowHovered(_AnyWindow) = {0}", IsWindowHovered( ImGuiHoveredFlags.AnyWindow ) );
				BeginChild( "child", new Vector2( 0, 50 ), ImGuiChildFlags.Borders );
				Text( "This is another child window for testing the _ChildWindows flag." );
				EndChild();
				TreePop();
			}

			if ( _disableAll )
				EndDisabled();

			if ( TreeNode( "Disable block" ) )
			{
				Checkbox( "Disable entire section above", ref _disableAll );
				SameLine(); HelpMarker( "Demonstrate using BeginDisabled()/EndDisabled() across this section." );
				TreePop();
			}
		}
	}
}

#endregion

#region ImGui (Demo/ImGui.Tools)

public static partial class ImGui
{
	private static int _styleSelectorIdx = -1;
	private static string _styleFilter = "";
	private static int _metricsSelectedWindow = -1;

	/// <summary>Create a Metrics/Debugger window: internal state, windows, draw lists, items.</summary>
	public static void ShowMetricsWindow( ref bool open )
	{
		if ( !open )
			return;
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
		if ( !open )
			return;
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

#endregion

#region ImGui (ImGui.Input)

public static partial class ImGui
{
	public static ImGuiIO GetIO() => G.IO;

	#region Mouse
	public static bool IsMouseDown( ImGuiMouseButton button ) => G.IO.MouseDown[(int)button];

	public static bool IsMouseClicked( ImGuiMouseButton button, bool repeat = false )
	{
		var g = G;
		int b = (int)button;
		float t = g.IO.MouseDownDuration[b];
		if ( t == 0.0f )
			return true;
		if ( repeat && t > g.IO.KeyRepeatDelay )
			return CalcTypematicRepeatAmount( t - g.IO.DeltaTime, t, g.IO.KeyRepeatDelay, g.IO.KeyRepeatRate ) > 0;
		return false;
	}

	public static bool IsMouseReleased( ImGuiMouseButton button ) => G.IO.MouseReleased[(int)button];
	public static bool IsMouseDoubleClicked( ImGuiMouseButton button ) => G.IO.MouseClickedCount[(int)button] == 2 && G.IO.MouseClicked[(int)button];
	public static int GetMouseClickedCount( ImGuiMouseButton button ) => G.IO.MouseClickedCount[(int)button];

	/// <summary>Is mouse hovering given bounding rect (in screen space), clipped by the current clipping settings.</summary>
	public static bool IsMouseHoveringRect( Vector2 rMin, Vector2 rMax, bool clip = true )
	{
		var g = G;
		var rectClipped = new ImRect( rMin, rMax );
		if ( clip && g.CurrentWindow is not null )
			rectClipped.ClipWith( g.CurrentWindow.ClipRect );

		var rectForTouch = rectClipped;
		rectForTouch.Expand( g.Style.TouchExtraPadding );
		return rectForTouch.Contains( g.IO.MousePos );
	}

	public static bool IsMousePosValid( Vector2? mousePos = null )
	{
		const float MOUSE_INVALID = -256000.0f;
		var p = mousePos ?? G.IO.MousePos;
		return p.x >= MOUSE_INVALID && p.y >= MOUSE_INVALID;
	}

	public static bool IsAnyMouseDown()
	{
		var io = G.IO;
		for ( int n = 0; n < io.MouseDown.Length; n++ )
			if ( io.MouseDown[n] ) return true;
		return false;
	}

	public static Vector2 GetMousePos() => G.IO.MousePos;

	public static Vector2 GetMousePosOnOpeningCurrentPopup()
	{
		var g = G;
		if ( g.BeginPopupStack.Count > 0 )
			return g.OpenPopupStack[g.BeginPopupStack.Count - 1].OpenMousePos;
		return g.IO.MousePos;
	}

	public static bool IsMouseDragging( ImGuiMouseButton button, float lockThreshold = -1.0f )
	{
		var g = G;
		if ( !g.IO.MouseDown[(int)button] )
			return false;
		return IsMouseDragPastThreshold( button, lockThreshold );
	}

	internal static bool IsMouseDragPastThreshold( ImGuiMouseButton button, float lockThreshold = -1.0f )
	{
		var g = G;
		if ( lockThreshold < 0.0f )
			lockThreshold = g.IO.MouseDragThreshold;
		return g.IO.MouseDragMaxDistanceSqr[(int)button] >= lockThreshold * lockThreshold;
	}

	public static Vector2 GetMouseDragDelta( ImGuiMouseButton button = ImGuiMouseButton.Left, float lockThreshold = -1.0f )
	{
		var g = G;
		int b = (int)button;
		if ( lockThreshold < 0.0f )
			lockThreshold = g.IO.MouseDragThreshold;
		if ( g.IO.MouseDown[b] || g.IO.MouseReleased[b] )
			if ( g.IO.MouseDragMaxDistanceSqr[b] >= lockThreshold * lockThreshold )
				if ( IsMousePosValid( g.IO.MousePos ) && IsMousePosValid( g.IO.MouseClickedPos[b] ) )
					return g.IO.MousePos - g.IO.MouseClickedPos[b];
		return Vector2.Zero;
	}

	public static void ResetMouseDragDelta( ImGuiMouseButton button = ImGuiMouseButton.Left )
	{
		var g = G;
		g.IO.MouseClickedPos[(int)button] = g.IO.MousePos;
	}

	public static ImGuiMouseCursor GetMouseCursor() => G.MouseCursor;
	public static void SetMouseCursor( ImGuiMouseCursor cursorType ) => G.MouseCursor = cursorType;

	public static void SetNextFrameWantCaptureMouse( bool wantCaptureMouse ) => G.WantCaptureMouseNextFrame = wantCaptureMouse ? 1 : 0;
	public static void SetNextFrameWantCaptureKeyboard( bool wantCaptureKeyboard ) => G.WantCaptureKeyboardNextFrame = wantCaptureKeyboard ? 1 : 0;
	#endregion

	#region Keyboard
	public static bool IsKeyDown( ImGuiKey key )
	{
		if ( TryModKey( key, out var down ) ) return down;
		int idx = (int)key;
		if ( idx <= 0 || idx >= (int)ImGuiKey.COUNT ) return false;
		return G.IO.KeysData[idx].Down;
	}

	public static bool IsKeyPressed( ImGuiKey key, bool repeat = true )
	{
		int idx = (int)key;
		if ( idx <= 0 || idx >= (int)ImGuiKey.COUNT ) return false;
		var io = G.IO;
		var data = io.KeysData[idx];
		float t = data.DownDuration;
		if ( t < 0.0f ) return false;
		if ( t == 0.0f ) return true;
		if ( repeat && t > io.KeyRepeatDelay )
			return CalcTypematicRepeatAmount( t - io.DeltaTime, t, io.KeyRepeatDelay, io.KeyRepeatRate ) > 0;
		return false;
	}

	public static bool IsKeyReleased( ImGuiKey key )
	{
		int idx = (int)key;
		if ( idx <= 0 || idx >= (int)ImGuiKey.COUNT ) return false;
		var data = G.IO.KeysData[idx];
		return data.DownDurationPrev >= 0.0f && !data.Down;
	}

	public static bool IsKeyChordPressed( ImGuiKey keyChord )
	{
		var io = G.IO;
		var mods = (ImGuiKey)((int)keyChord & (int)ImGuiKey.ImGuiMod_Mask_);
		var key = (ImGuiKey)((int)keyChord & ~(int)ImGuiKey.ImGuiMod_Mask_);
		if ( ((mods & ImGuiKey.ImGuiMod_Ctrl) != 0) != io.KeyCtrl ) return false;
		if ( ((mods & ImGuiKey.ImGuiMod_Shift) != 0) != io.KeyShift ) return false;
		if ( ((mods & ImGuiKey.ImGuiMod_Alt) != 0) != io.KeyAlt ) return false;
		return key == ImGuiKey.None || IsKeyPressed( key, false );
	}

	public static bool Shortcut( ImGuiKey keyChord ) => IsKeyChordPressed( keyChord );

	public static int GetKeyPressedAmount( ImGuiKey key, float repeatDelay, float rate )
	{
		int idx = (int)key;
		if ( idx <= 0 || idx >= (int)ImGuiKey.COUNT ) return 0;
		var data = G.IO.KeysData[idx];
		if ( !data.Down ) return 0;
		float t = data.DownDuration;
		return CalcTypematicRepeatAmount( t - G.IO.DeltaTime, t, repeatDelay, rate );
	}

	public static string GetKeyName( ImGuiKey key )
	{
		if ( key == ImGuiKey.None ) return "None";
		var name = key.ToString();
		return name.StartsWith( '_' ) ? name.Substring( 1 ) : name;
	}

	private static bool TryModKey( ImGuiKey key, out bool down )
	{
		var io = G.IO;
		switch ( key )
		{
			case ImGuiKey.ImGuiMod_Ctrl: down = io.KeyCtrl; return true;
			case ImGuiKey.ImGuiMod_Shift: down = io.KeyShift; return true;
			case ImGuiKey.ImGuiMod_Alt: down = io.KeyAlt; return true;
			case ImGuiKey.ImGuiMod_Super: down = io.KeySuper; return true;
		}
		down = false;
		return false;
	}

	internal static int CalcTypematicRepeatAmount( float t0, float t1, float repeatDelay, float repeatRate )
	{
		if ( t1 == 0.0f )
			return 1;
		if ( t0 >= t1 )
			return 0;
		if ( repeatRate <= 0.0f )
			return (t0 < repeatDelay && t1 >= repeatDelay) ? 1 : 0;
		int countT0 = (t0 < repeatDelay) ? -1 : (int)((t0 - repeatDelay) / repeatRate);
		int countT1 = (t1 < repeatDelay) ? -1 : (int)((t1 - repeatDelay) / repeatRate);
		return countT1 - countT0;
	}

	/// <summary>Focus the next (or offset-th next) text input widget so it receives keyboard input.</summary>
	public static void SetKeyboardFocusHere( int offset = 0 )
	{
		var g = G;
		g.FocusRequestWindow = g.CurrentWindow;
		g.FocusRequestCounter = offset;
		g.FocusItemCounter = 0;
	}
	#endregion

	#region Frame input update
	internal static void UpdateMouseInputs()
	{
		var g = G;
		var io = g.IO;

		if ( IsMousePosValid( io.MousePos ) )
			io.MousePos = g.MouseLastValidPos = ImFloor( io.MousePos );

		if ( IsMousePosValid( io.MousePos ) && IsMousePosValid( io.MousePosPrev ) )
			io.MouseDelta = io.MousePos - io.MousePosPrev;
		else
			io.MouseDelta = Vector2.Zero;

		const float mouseStationaryThreshold = 2.0f;
		bool mouseStationary = ImLengthSqr( io.MouseDelta ) <= mouseStationaryThreshold * mouseStationaryThreshold;
		g.MouseStationaryTimer = mouseStationary ? g.MouseStationaryTimer + io.DeltaTime : 0.0f;

		io.MousePosPrev = io.MousePos;
		for ( int i = 0; i < io.MouseDown.Length; i++ )
		{
			io.MouseClicked[i] = io.MouseDown[i] && io.MouseDownDuration[i] < 0.0f;
			io.MouseClickedCount[i] = 0;
			io.MouseReleased[i] = !io.MouseDown[i] && io.MouseDownDuration[i] >= 0.0f;
			io.MouseDownDurationPrev[i] = io.MouseDownDuration[i];
			io.MouseDownDuration[i] = io.MouseDown[i] ? (io.MouseDownDuration[i] < 0.0f ? 0.0f : io.MouseDownDuration[i] + io.DeltaTime) : -1.0f;
			if ( io.MouseClicked[i] )
			{
				bool isRepeatedClick = false;
				if ( (float)(g.Time - io.MouseClickedTime[i]) < io.MouseDoubleClickTime )
				{
					var deltaFromClickPos = IsMousePosValid( io.MousePos ) ? (io.MousePos - io.MouseClickedPos[i]) : Vector2.Zero;
					if ( ImLengthSqr( deltaFromClickPos ) < io.MouseDoubleClickMaxDist * io.MouseDoubleClickMaxDist )
						isRepeatedClick = true;
				}
				if ( isRepeatedClick )
					io.MouseClickedLastCount[i]++;
				else
					io.MouseClickedLastCount[i] = 1;
				io.MouseClickedTime[i] = g.Time;
				io.MouseClickedPos[i] = io.MousePos;
				io.MouseClickedCount[i] = io.MouseClickedLastCount[i];
				io.MouseDragMaxDistanceSqr[i] = 0.0f;
			}
			else if ( io.MouseDown[i] )
			{
				var deltaFromClickPos = IsMousePosValid( io.MousePos ) ? (io.MousePos - io.MouseClickedPos[i]) : Vector2.Zero;
				io.MouseDragMaxDistanceSqr[i] = MathF.Max( io.MouseDragMaxDistanceSqr[i], ImLengthSqr( deltaFromClickPos ) );
			}
			io.MouseDoubleClicked[i] = io.MouseClickedCount[i] == 2;
		}
	}

	internal static void UpdateKeyboardInputs()
	{
		var g = G;
		var io = g.IO;
		for ( int k = (int)ImGuiKey.Tab; k < (int)ImGuiKey.COUNT; k++ )
		{
			var key = (ImGuiKey)k;
			bool down = io.KeysDownFromEvents[k] || ImGuiSystem.PollKeyDown( key );
			ref var data = ref io.KeysData[k];
			data.Down = down;
			data.DownDurationPrev = data.DownDuration;
			data.DownDuration = down ? (data.DownDuration < 0.0f ? 0.0f : data.DownDuration + io.DeltaTime) : -1.0f;
		}

		io.KeyCtrl = io.KeysData[(int)ImGuiKey.LeftCtrl].Down || io.KeysData[(int)ImGuiKey.RightCtrl].Down;
		io.KeyShift = io.KeysData[(int)ImGuiKey.LeftShift].Down || io.KeysData[(int)ImGuiKey.RightShift].Down;
		io.KeyAlt = io.KeysData[(int)ImGuiKey.LeftAlt].Down || io.KeysData[(int)ImGuiKey.RightAlt].Down;
		io.KeySuper = io.KeysData[(int)ImGuiKey.LeftSuper].Down || io.KeysData[(int)ImGuiKey.RightSuper].Down;
	}
	#endregion
}

#endregion

#region ImGui (ImGui.Layout)

public static partial class ImGui
{
	#region ID stack
	public static void PushID( string strId )
	{
		var window = G.CurrentWindow;
		window.IDStack.Add( window.GetID( strId ) );
	}

	public static void PushID( int intId )
	{
		var window = G.CurrentWindow;
		window.IDStack.Add( window.GetID( intId ) );
	}

	/// <summary>Push an id derived from an object's hash code (equivalent of Dear ImGui's PushID(const void*)).</summary>
	public static void PushID( object obj ) => PushID( obj?.GetHashCode() ?? 0 );

	internal static void PushOverrideID( int id ) => G.CurrentWindow.IDStack.Add( id );

	public static void PopID()
	{
		var window = G.CurrentWindow;
		if ( window.IDStack.Count <= 1 )
		{
			Log.Warning( "ImGui: PopID() called too many times" );
			return;
		}
		window.IDStack.RemoveAt( window.IDStack.Count - 1 );
	}

	public static int GetID( string strId ) => G.CurrentWindow.GetID( strId );
	public static int GetID( int intId ) => G.CurrentWindow.GetID( intId );
	public static int GetID( object obj ) => G.CurrentWindow.GetID( obj?.GetHashCode() ?? 0 );
	#endregion

	#region Cursor / layout
	public static void Separator()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;
		var flags = window.DC.LayoutType == ImGuiLayoutType.Horizontal ? 0 : 1;
		SeparatorEx( flags == 1, 1.0f );
	}

	internal static void SeparatorEx( bool horizontal, float thickness )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		if ( !horizontal )
		{
			// Vertical separator, for menu bars and horizontal layouts.
			float y1 = window.DC.CursorPos.y;
			float y2 = window.DC.CursorPos.y + window.DC.CurrLineSize.y;
			var bbV = new ImRect( new Vector2( window.DC.CursorPos.x, y1 ), new Vector2( window.DC.CursorPos.x + thickness, y2 ) );
			ItemSize( new Vector2( thickness, 0.0f ) );
			if ( !ItemAdd( bbV, 0 ) )
				return;
			window.DrawList.AddRectFilled( bbV.Min, bbV.Max, GetColorU32Internal( ImGuiCol.Separator ) );
			return;
		}

		float x1 = window.DC.CursorPos.x;
		float x2 = window.WorkRect.Max.x;
		var columns = window.DC.CurrentColumns;
		if ( window.DC.CurrentTableIdx < 0 && columns is null )
			x1 = window.Pos.x + window.DC.Indent;

		float thicknessForLayout = thickness == 1.0f ? 0.0f : thickness;
		var bb = new ImRect( new Vector2( x1, window.DC.CursorPos.y ), new Vector2( x2, window.DC.CursorPos.y + thickness ) );
		ItemSize( new Vector2( 0.0f, thicknessForLayout ) );
		if ( ItemAdd( bb, 0 ) )
			window.DrawList.AddRectFilled( bb.Min, bb.Max, GetColorU32Internal( ImGuiCol.Separator ) );
	}

	public static void SameLine( float offsetFromStartX = 0.0f, float spacing = -1.0f )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;

		var dc = window.DC;
		if ( offsetFromStartX != 0.0f )
		{
			if ( spacing < 0.0f )
				spacing = 0.0f;
			dc.CursorPos = new Vector2( window.Pos.x - window.Scroll.x + offsetFromStartX + spacing + dc.GroupOffset + dc.ColumnsOffset, dc.CursorPosPrevLine.y );
		}
		else
		{
			if ( spacing < 0.0f )
				spacing = g.Style.ItemSpacing.x;
			dc.CursorPos = new Vector2( dc.CursorPosPrevLine.x + spacing, dc.CursorPosPrevLine.y );
		}
		dc.CurrLineSize = dc.PrevLineSize;
		dc.CurrLineTextBaseOffset = dc.PrevLineTextBaseOffset;
		dc.IsSameLine = true;
	}

	public static void NewLine()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;

		var backupLayoutType = window.DC.LayoutType;
		window.DC.LayoutType = ImGuiLayoutType.Vertical;
		window.DC.IsSameLine = false;
		if ( window.DC.CurrLineSize.y > 0.0f )
			ItemSize( new Vector2( 0, 0 ) );
		else
			ItemSize( new Vector2( 0.0f, g.FontSize ) );
		window.DC.LayoutType = backupLayoutType;
	}

	public static void Spacing()
	{
		var window = G.CurrentWindow;
		if ( window.SkipItems )
			return;
		ItemSize( new Vector2( 0, 0 ) );
	}

	public static void Dummy( Vector2 size )
	{
		var window = G.CurrentWindow;
		if ( window.SkipItems )
			return;
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + size );
		ItemSize( size );
		ItemAdd( bb, 0 );
	}

	public static void Indent( float indentW = 0.0f )
	{
		var g = G;
		var window = GetCurrentWindow();
		window.DC.Indent += indentW != 0.0f ? indentW : g.Style.IndentSpacing;
		window.DC.CursorPos = new Vector2( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset, window.DC.CursorPos.y );
	}

	public static void Unindent( float indentW = 0.0f )
	{
		var g = G;
		var window = GetCurrentWindow();
		window.DC.Indent -= indentW != 0.0f ? indentW : g.Style.IndentSpacing;
		window.DC.CursorPos = new Vector2( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset, window.DC.CursorPos.y );
	}

	public static void AlignTextToFramePadding()
	{
		var g = G;
		var window = G.CurrentWindow;
		if ( window.SkipItems )
			return;
		window.DC.CurrLineSize = new Vector2( window.DC.CurrLineSize.x, MathF.Max( window.DC.CurrLineSize.y, g.FontSize + g.Style.FramePadding.y * 2 ) );
		window.DC.CurrLineTextBaseOffset = MathF.Max( window.DC.CurrLineTextBaseOffset, g.Style.FramePadding.y );
	}

	public static Vector2 GetCursorScreenPos() => G.CurrentWindow.DC.CursorPos;

	public static void SetCursorScreenPos( Vector2 pos )
	{
		var window = G.CurrentWindow;
		window.DC.CursorPos = pos;
		window.DC.IsSetPos = true;
	}

	public static Vector2 GetCursorPos()
	{
		var window = G.CurrentWindow;
		return window.DC.CursorPos - window.Pos + window.Scroll;
	}

	public static float GetCursorPosX() => GetCursorPos().x;
	public static float GetCursorPosY() => GetCursorPos().y;

	public static void SetCursorPos( Vector2 localPos )
	{
		var window = G.CurrentWindow;
		window.DC.CursorPos = window.Pos - window.Scroll + localPos;
		window.DC.IsSetPos = true;
	}

	public static void SetCursorPosX( float x )
	{
		var window = G.CurrentWindow;
		window.DC.CursorPos = new Vector2( window.Pos.x - window.Scroll.x + x, window.DC.CursorPos.y );
		window.DC.IsSetPos = true;
	}

	public static void SetCursorPosY( float y )
	{
		var window = G.CurrentWindow;
		window.DC.CursorPos = new Vector2( window.DC.CursorPos.x, window.Pos.y - window.Scroll.y + y );
		window.DC.IsSetPos = true;
	}

	public static Vector2 GetCursorStartPos()
	{
		var window = G.CurrentWindow;
		return window.DC.CursorStartPos - window.Pos;
	}

	/// <summary>Available space from the current cursor position to the edge of the content region.</summary>
	public static Vector2 GetContentRegionAvail()
	{
		var window = G.CurrentWindow;
		var mx = GetContentRegionMaxAbs();
		return mx - window.DC.CursorPos;
	}

	/// <summary>Legacy: content region max in window-local coordinates.</summary>
	public static Vector2 GetContentRegionMax()
	{
		var window = G.CurrentWindow;
		return GetContentRegionMaxAbs() - window.Pos;
	}

	public static Vector2 GetWindowContentRegionMin()
	{
		var window = G.CurrentWindow;
		return window.ContentRegionRect.Min - window.Pos;
	}

	public static Vector2 GetWindowContentRegionMax()
	{
		var window = G.CurrentWindow;
		return window.ContentRegionRect.Max - window.Pos;
	}

	public static float GetTextLineHeight() => G.FontSize;
	public static float GetTextLineHeightWithSpacing() => G.FontSize + G.Style.ItemSpacing.y;
	public static float GetFrameHeight() => G.FontSize + G.Style.FramePadding.y * 2.0f;
	public static float GetFrameHeightWithSpacing() => G.FontSize + G.Style.FramePadding.y * 2.0f + G.Style.ItemSpacing.y;
	public static float GetFontSize() => G.FontSize;

	public static void BeginGroup()
	{
		var g = G;
		var window = g.CurrentWindow;

		var groupData = new ImGuiGroupData
		{
			WindowID = window.ID,
			BackupCursorPos = window.DC.CursorPos,
			BackupCursorPosPrevLine = window.DC.CursorPosPrevLine,
			BackupCursorMaxPos = window.DC.CursorMaxPos,
			BackupIndent = window.DC.Indent,
			BackupGroupOffset = window.DC.GroupOffset,
			BackupCurrLineSize = window.DC.CurrLineSize,
			BackupCurrLineTextBaseOffset = window.DC.CurrLineTextBaseOffset,
			BackupActiveIdIsAlive = g.ActiveIdIsAlive,
			BackupHoveredIdIsAlive = g.HoveredId != 0,
			BackupIsSameLine = window.DC.IsSameLine,
			BackupActiveIdPreviousFrameIsAlive = g.ActiveIdPreviousFrameIsAlive,
			EmitItem = true,
		};
		g.GroupStack.Add( groupData );

		window.DC.GroupOffset = window.DC.CursorPos.x - window.Pos.x - window.DC.ColumnsOffset;
		window.DC.Indent = window.DC.GroupOffset;
		window.DC.CursorMaxPos = window.DC.CursorPos;
		window.DC.CurrLineSize = new Vector2( 0.0f, 0.0f );
	}

	public static void EndGroup()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( g.GroupStack.Count == 0 )
		{
			Log.Warning( "ImGui: EndGroup() called without BeginGroup()" );
			return;
		}

		var groupData = g.GroupStack[^1];
		var groupBb = new ImRect( groupData.BackupCursorPos, ImMax( ImMax( window.DC.CursorMaxPos, groupData.BackupCursorPos ), window.DC.CursorMaxPos ) );
		g.GroupStack.RemoveAt( g.GroupStack.Count - 1 );

		window.DC.CursorPos = groupData.BackupCursorPos;
		window.DC.CursorPosPrevLine = groupData.BackupCursorPosPrevLine;
		window.DC.CursorMaxPos = ImMax( groupData.BackupCursorMaxPos, groupBb.Max );
		window.DC.Indent = groupData.BackupIndent;
		window.DC.GroupOffset = groupData.BackupGroupOffset;
		window.DC.CurrLineSize = groupData.BackupCurrLineSize;
		window.DC.CurrLineTextBaseOffset = groupData.BackupCurrLineTextBaseOffset;
		window.DC.IsSameLine = groupData.BackupIsSameLine;

		if ( !groupData.EmitItem )
			return;

		window.DC.CurrLineTextBaseOffset = MathF.Max( window.DC.PrevLineTextBaseOffset, groupData.BackupCurrLineTextBaseOffset );
		ItemSize( groupBb.Size );
		ItemAdd( groupBb, 0 );

		// If the current ActiveId was declared within the boundary of our group, we copy it to LastItemId so IsItemActive()/IsItemDeactivated() can be used on the group.
		bool groupContainsCurrActiveId = groupData.BackupActiveIdIsAlive != g.ActiveId && g.ActiveIdIsAlive == g.ActiveId && g.ActiveId != 0;
		bool groupContainsPrevActiveId = !groupData.BackupActiveIdPreviousFrameIsAlive && g.ActiveIdPreviousFrameIsAlive;
		if ( groupContainsCurrActiveId )
			g.LastItemData.ID = g.ActiveId;
		else if ( groupContainsPrevActiveId )
			g.LastItemData.ID = g.ActiveIdPreviousFrame;
		g.LastItemData.Rect = groupBb;
		if ( groupContainsPrevActiveId && g.ActiveId != g.ActiveIdPreviousFrame )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Deactivated;
		if ( IsMouseHoveringRect( groupBb.Min, groupBb.Max ) )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredRect;
	}
	#endregion

	#region Parameter stacks (current window)
	public static void PushItemWidth( float itemWidth )
	{
		var g = G;
		var window = g.CurrentWindow;
		window.DC.ItemWidthStack.Add( window.DC.ItemWidth );
		window.DC.ItemWidth = itemWidth == 0.0f ? window.ItemWidthDefault : itemWidth;
		g.NextItemData.Flags &= ~ImGuiNextItemDataFlags.HasWidth;
	}

	internal static void PushMultiItemsWidths( int components, float wFull )
	{
		var g = G;
		var window = g.CurrentWindow;
		float wItemOne = MathF.Max( 1.0f, ImTrunc( (wFull - g.Style.ItemInnerSpacing.x * (components - 1)) / components ) );
		float wItemLast = MathF.Max( 1.0f, ImTrunc( wFull - (wItemOne + g.Style.ItemInnerSpacing.x) * (components - 1) ) );
		window.DC.ItemWidthStack.Add( window.DC.ItemWidth );
		window.DC.ItemWidthStack.Add( wItemLast );
		for ( int i = 0; i < components - 2; i++ )
			window.DC.ItemWidthStack.Add( wItemOne );
		window.DC.ItemWidth = components == 1 ? wItemLast : wItemOne;
		g.NextItemData.Flags &= ~ImGuiNextItemDataFlags.HasWidth;
	}

	public static void PopItemWidth()
	{
		var window = G.CurrentWindow;
		if ( window.DC.ItemWidthStack.Count == 0 )
			return;
		window.DC.ItemWidth = window.DC.ItemWidthStack[^1];
		window.DC.ItemWidthStack.RemoveAt( window.DC.ItemWidthStack.Count - 1 );
	}

	public static void SetNextItemWidth( float itemWidth )
	{
		var g = G;
		g.NextItemData.Flags |= ImGuiNextItemDataFlags.HasWidth;
		g.NextItemData.Width = itemWidth;
	}

	public static void PushTextWrapPos( float wrapLocalPosX = 0.0f )
	{
		var window = G.CurrentWindow;
		window.DC.TextWrapPosStack.Add( window.DC.TextWrapPos );
		window.DC.TextWrapPos = wrapLocalPosX;
	}

	public static void PopTextWrapPos()
	{
		var window = G.CurrentWindow;
		if ( window.DC.TextWrapPosStack.Count == 0 )
			return;
		window.DC.TextWrapPos = window.DC.TextWrapPosStack[^1];
		window.DC.TextWrapPosStack.RemoveAt( window.DC.TextWrapPosStack.Count - 1 );
	}

	public static void PushItemFlag( ImGuiItemFlags option, bool enabled )
	{
		var g = G;
		var itemFlags = g.CurrentItemFlags;
		if ( enabled )
			itemFlags |= option;
		else
			itemFlags &= ~option;
		g.ItemFlagsStack.Add( g.CurrentItemFlags );
		g.CurrentItemFlags = itemFlags;
	}

	public static void PopItemFlag()
	{
		var g = G;
		if ( g.ItemFlagsStack.Count == 0 )
			return;
		g.CurrentItemFlags = g.ItemFlagsStack[^1];
		g.ItemFlagsStack.RemoveAt( g.ItemFlagsStack.Count - 1 );
	}

	public static void PushTabStop( bool tabStop ) => PushItemFlag( ImGuiItemFlags.NoTabStop, !tabStop );
	public static void PopTabStop() => PopItemFlag();
	public static void PushButtonRepeat( bool repeat ) => PushItemFlag( ImGuiItemFlags.ButtonRepeat, repeat );
	public static void PopButtonRepeat() => PopItemFlag();

	/// <summary>Disable all user interactions and dim items visuals until EndDisabled().</summary>
	public static void BeginDisabled( bool disabled = true )
	{
		var g = G;
		bool wasDisabled = (g.CurrentItemFlags & ImGuiItemFlags.Disabled) != 0;
		if ( !wasDisabled && disabled )
		{
			g.DisabledAlphaBackup = g.Style.Alpha;
			g.Style.Alpha *= g.Style.DisabledAlpha;
		}
		if ( wasDisabled || disabled )
			g.CurrentItemFlags |= ImGuiItemFlags.Disabled;
		g.ItemFlagsStack.Add( wasDisabled ? g.CurrentItemFlags : (g.CurrentItemFlags & ~ImGuiItemFlags.Disabled) );
		g.DisabledStackSize++;
	}

	public static void EndDisabled()
	{
		var g = G;
		if ( g.DisabledStackSize <= 0 )
			return;
		g.DisabledStackSize--;
		bool wasDisabled = (g.CurrentItemFlags & ImGuiItemFlags.Disabled) != 0;
		PopItemFlag();
		if ( wasDisabled && (g.CurrentItemFlags & ImGuiItemFlags.Disabled) == 0 )
			g.Style.Alpha = g.DisabledAlphaBackup;
	}

	public static void PushClipRect( Vector2 clipRectMin, Vector2 clipRectMax, bool intersectWithCurrentClipRect )
	{
		var window = G.CurrentWindow;
		window.DrawList.PushClipRect( clipRectMin, clipRectMax, intersectWithCurrentClipRect );
		window.ClipRect = window.DrawList.CurrentClipRect;
	}

	public static void PopClipRect()
	{
		var window = G.CurrentWindow;
		window.DrawList.PopClipRect();
		window.ClipRect = window.DrawList.CurrentClipRect;
	}

	public static bool IsRectVisible( Vector2 size )
	{
		var window = G.CurrentWindow;
		return window.ClipRect.Overlaps( new ImRect( window.DC.CursorPos, window.DC.CursorPos + size ) );
	}

	public static bool IsRectVisible( Vector2 rectMin, Vector2 rectMax ) => G.CurrentWindow.ClipRect.Overlaps( new ImRect( rectMin, rectMax ) );
	#endregion

	#region Item queries
	public static bool IsItemHovered( ImGuiHoveredFlags flags = ImGuiHoveredFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;

		if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredRect) == 0 )
			return false;

		if ( (flags & ImGuiHoveredFlags.ForTooltip) != 0 )
			flags |= g.Style.HoverFlagsForTooltipMouse;

		if ( (flags & ImGuiHoveredFlags.AllowWhenOverlappedByWindow) == 0 )
		{
			if ( g.HoveredWindow != window && (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredWindow) == 0 )
				return false;
		}

		int id = g.LastItemData.ID;
		if ( (flags & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) == 0 )
			if ( g.ActiveId != 0 && g.ActiveId != id && !g.ActiveIdAllowOverlap && g.ActiveId != window.MoveId )
				return false;

		if ( !IsWindowContentHoverable( window, flags ) && (g.LastItemData.InFlags & ImGuiItemFlags.NoWindowHoverableCheck) == 0 )
			return false;

		if ( (g.LastItemData.InFlags & ImGuiItemFlags.Disabled) != 0 && (flags & ImGuiHoveredFlags.AllowWhenDisabled) == 0 )
			return false;

		if ( id == window.MoveId && window.WriteAccessed )
			return false;

		// Overlapping items: only the one hovered last frame (or allowing overlap) reports hovered.
		if ( (flags & ImGuiHoveredFlags.AllowWhenOverlappedByItem) == 0 && id != 0 && g.HoveredIdPreviousFrame != 0 && g.HoveredIdPreviousFrame != id && g.HoveredId != id )
			if ( (g.LastItemData.InFlags & ImGuiItemFlags.AllowOverlap) != 0 )
				return false;

		// Delays (tooltips)
		if ( (flags & (ImGuiHoveredFlags.DelayShort | ImGuiHoveredFlags.DelayNormal | ImGuiHoveredFlags.Stationary)) != 0 )
		{
			int hoverDelayId = id != 0 ? id : window.GetIDFromRectangle( g.LastItemData.Rect );
			if ( (flags & ImGuiHoveredFlags.NoSharedDelay) != 0 && g.HoverItemDelayIdPreviousFrame != hoverDelayId )
				g.HoverItemDelayTimer = 0.0f;
			g.HoverItemDelayId = hoverDelayId;

			if ( (flags & ImGuiHoveredFlags.Stationary) != 0 && g.HoverItemUnlockedStationaryId != hoverDelayId )
			{
				if ( g.MouseStationaryTimer < g.Style.HoverStationaryDelay )
					return false;
				g.HoverItemUnlockedStationaryId = hoverDelayId;
			}

			float delay = (flags & ImGuiHoveredFlags.DelayNormal) != 0 ? g.Style.HoverDelayNormal : (flags & ImGuiHoveredFlags.DelayShort) != 0 ? g.Style.HoverDelayShort : 0.0f;
			if ( g.HoverItemDelayTimer < delay )
				return false;
		}

		return true;
	}

	public static bool IsItemActive()
	{
		var g = G;
		return g.ActiveId != 0 && g.ActiveId == g.LastItemData.ID;
	}

	public static bool IsItemActivated()
	{
		var g = G;
		return g.ActiveId != 0 && g.ActiveId == g.LastItemData.ID && g.ActiveIdPreviousFrame != g.LastItemData.ID;
	}

	public static bool IsItemDeactivated()
	{
		var g = G;
		if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HasDeactivated) != 0 )
			return (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.Deactivated) != 0;
		return g.ActiveIdPreviousFrame == g.LastItemData.ID && g.ActiveIdPreviousFrame != 0 && g.ActiveId != g.LastItemData.ID;
	}

	public static bool IsItemDeactivatedAfterEdit()
	{
		var g = G;
		return IsItemDeactivated() && (g.ActiveIdPreviousFrameHasBeenEditedBefore || (g.ActiveId == 0 && g.ActiveIdHasBeenEditedBefore));
	}

	public static bool IsItemFocused()
	{
		var g = G;
		return g.NavId != 0 && g.NavId == g.LastItemData.ID && g.NavWindow == g.CurrentWindow;
	}

	public static bool IsItemClicked( ImGuiMouseButton mouseButton = ImGuiMouseButton.Left )
	{
		return IsMouseClicked( mouseButton ) && IsItemHovered( ImGuiHoveredFlags.None );
	}

	public static bool IsItemVisible() => (G.LastItemData.StatusFlags & ImGuiItemStatusFlags.Visible) != 0;
	public static bool IsItemEdited() => (G.LastItemData.StatusFlags & ImGuiItemStatusFlags.Edited) != 0;
	public static bool IsItemToggledOpen() => (G.LastItemData.StatusFlags & ImGuiItemStatusFlags.ToggledOpen) != 0;
	public static bool IsItemToggledSelection() => (G.LastItemData.StatusFlags & ImGuiItemStatusFlags.ToggledSelection) != 0;
	public static bool IsAnyItemHovered() => G.HoveredId != 0 || G.HoveredIdPreviousFrame != 0;
	public static bool IsAnyItemActive() => G.ActiveId != 0;
	public static bool IsAnyItemFocused() => G.NavId != 0;
	public static int GetItemID() => G.LastItemData.ID;
	public static Vector2 GetItemRectMin() => G.LastItemData.Rect.Min;
	public static Vector2 GetItemRectMax() => G.LastItemData.Rect.Max;
	public static Vector2 GetItemRectSize() => G.LastItemData.Rect.Size;

	/// <summary>Allow the next item to be overlapped by a subsequent item.</summary>
	public static void SetNextItemAllowOverlap()
	{
		G.NextItemData.ItemFlags |= ImGuiItemFlags.AllowOverlap;
	}

	/// <summary>Make the last item the default focused item of a newly appearing window.</summary>
	public static void SetItemDefaultFocus()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.Appearing )
		{
			g.NavId = g.LastItemData.ID;
			ScrollToRect( window, g.LastItemData.Rect );
		}
	}

	[Obsolete( "Use SetNextItemAllowOverlap() before the item" )]
	public static void SetItemAllowOverlap()
	{
		var g = G;
		int id = g.LastItemData.ID;
		if ( g.HoveredId == id )
			g.HoveredIdAllowOverlap = true;
		if ( g.ActiveId == id )
			g.ActiveIdAllowOverlap = true;
	}
	#endregion
}

#endregion

#region ImGui (ImGui.Popup)

public static partial class ImGui
{
	#region Popup stack
	internal static bool IsPopupOpen( int id, ImGuiPopupFlags popupFlags )
	{
		var g = G;
		if ( (popupFlags & ImGuiPopupFlags.AnyPopupId) != 0 )
		{
			if ( (popupFlags & ImGuiPopupFlags.AnyPopupLevel) != 0 )
				return g.OpenPopupStack.Count > 0;
			return g.OpenPopupStack.Count > g.BeginPopupStack.Count;
		}
		if ( (popupFlags & ImGuiPopupFlags.AnyPopupLevel) != 0 )
		{
			foreach ( var p in g.OpenPopupStack )
				if ( p.PopupId == id )
					return true;
			return false;
		}
		return g.OpenPopupStack.Count > g.BeginPopupStack.Count && g.OpenPopupStack[g.BeginPopupStack.Count].PopupId == id;
	}

	/// <summary>Return true if the popup is open at the current BeginPopup() level of the popup stack.</summary>
	public static bool IsPopupOpen( string strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None )
	{
		var g = G;
		int id = (popupFlags & ImGuiPopupFlags.AnyPopupId) != 0 ? 0 : g.CurrentWindow.GetID( strId );
		return IsPopupOpen( id, popupFlags );
	}

	/// <summary>Mark a popup as open (don't call every frame!). Popups are closed when the user clicks outside, or when CloseCurrentPopup() is called.</summary>
	public static void OpenPopup( string strId, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None )
	{
		var g = G;
		OpenPopupEx( g.CurrentWindow.GetID( strId ), popupFlags );
	}

	public static void OpenPopup( int id, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None ) => OpenPopupEx( id, popupFlags );

	internal static void OpenPopupEx( int id, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.None )
	{
		var g = G;
		var parentWindow = g.CurrentWindow;
		int currentStackSize = g.BeginPopupStack.Count;

		if ( (popupFlags & ImGuiPopupFlags.NoOpenOverExistingPopup) != 0 )
			if ( IsPopupOpen( 0, ImGuiPopupFlags.AnyPopupId ) )
				return;

		var popupRef = new ImGuiPopupData
		{
			PopupId = id,
			Window = null,
			RestoreNavWindow = g.NavWindow,
			ParentNavLayer = parentWindow.DC.MenuBarAppending > 0 ? 1 : 0,
			OpenFrameCount = g.FrameCount,
			OpenParentId = parentWindow.IDStack[^1],
			OpenMousePos = IsMousePosValid( g.IO.MousePos ) ? g.IO.MousePos : Vector2.Zero,
			OpenPopupPos = IsMousePosValid( g.IO.MousePos ) ? g.IO.MousePos : Vector2.Zero,
		};

		if ( g.OpenPopupStack.Count < currentStackSize + 1 )
		{
			g.OpenPopupStack.Add( popupRef );
		}
		else
		{
			bool keepExisting = false;
			var existing = g.OpenPopupStack[currentStackSize];
			if ( existing.PopupId == id && existing.OpenFrameCount == g.FrameCount - 1 )
				keepExisting = true;
			else if ( existing.PopupId == id && (popupFlags & ImGuiPopupFlags.NoReopen) != 0 )
				keepExisting = true;

			if ( keepExisting )
			{
				existing.OpenFrameCount = popupRef.OpenFrameCount;
			}
			else
			{
				ClosePopupToLevel( currentStackSize, true );
				g.OpenPopupStack.Add( popupRef );
			}
		}
	}

	/// <summary>Close popups that are not ancestors of refWindow.</summary>
	internal static void ClosePopupsOverWindow( ImGuiWindow refWindow, bool restoreFocusToWindowUnderPopup )
	{
		var g = G;
		if ( g.OpenPopupStack.Count == 0 )
			return;

		int popupCountToKeep = 0;
		if ( refWindow is not null )
		{
			for ( ; popupCountToKeep < g.OpenPopupStack.Count; popupCountToKeep++ )
			{
				var popup = g.OpenPopupStack[popupCountToKeep];
				if ( popup.Window is null )
					continue;
				if ( (popup.Window.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
					continue;

				bool refWindowIsDescendentOfPopup = false;
				for ( int n = popupCountToKeep; n < g.OpenPopupStack.Count; n++ )
				{
					var popupWindow = g.OpenPopupStack[n].Window;
					if ( popupWindow is not null && IsWindowWithinBeginStackOf( refWindow, popupWindow ) )
					{
						refWindowIsDescendentOfPopup = true;
						break;
					}
				}
				if ( !refWindowIsDescendentOfPopup )
					break;
			}
		}

		if ( popupCountToKeep < g.OpenPopupStack.Count )
			ClosePopupToLevel( popupCountToKeep, restoreFocusToWindowUnderPopup );
	}

	internal static void ClosePopupToLevel( int remaining, bool restoreFocusToWindowUnderPopup )
	{
		var g = G;
		if ( remaining < 0 || remaining >= g.OpenPopupStack.Count )
			return;

		var popupWindow = g.OpenPopupStack[remaining].Window;
		var focusWindow = g.OpenPopupStack[remaining].RestoreNavWindow;
		g.OpenPopupStack.RemoveRange( remaining, g.OpenPopupStack.Count - remaining );

		if ( restoreFocusToWindowUnderPopup )
		{
			if ( focusWindow is not null && focusWindow.WasActive )
				FocusWindow( focusWindow );
			else if ( popupWindow is not null )
				FocusTopMostWindowUnderOne( popupWindow, null );
		}
	}

	/// <summary>Close the popup we have begin-ed into. Typically called from a menu item or button inside the popup.</summary>
	public static void CloseCurrentPopup()
	{
		var g = G;
		int popupIdx = g.BeginPopupStack.Count - 1;
		if ( popupIdx < 0 || popupIdx >= g.OpenPopupStack.Count || g.BeginPopupStack[popupIdx].PopupId != g.OpenPopupStack[popupIdx].PopupId )
			return;

		// Closing a menu closes its top-most parent popup (unless a modal)
		while ( popupIdx > 0 )
		{
			var popupWindow = g.OpenPopupStack[popupIdx].Window;
			var parentPopupWindow = g.OpenPopupStack[popupIdx - 1].Window;
			bool closeParent = false;
			if ( popupWindow is not null && (popupWindow.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
				if ( parentPopupWindow is not null && (parentPopupWindow.Flags & ImGuiWindowFlags.MenuBar) == 0 )
					closeParent = true;
			if ( !closeParent )
				break;
			popupIdx--;
		}
		ClosePopupToLevel( popupIdx, true );

		// A common pattern is to close a popup when selecting a menu item; make sure the menu's parent window keeps its focus.
		if ( g.NavWindow is not null )
			g.NavWindow.DC.MenuBarAppending = g.NavWindow.DC.MenuBarAppending;
	}
	#endregion

	#region BeginPopup
	internal static bool BeginPopupEx( int id, ImGuiWindowFlags flags )
	{
		var g = G;
		if ( !IsPopupOpen( id, ImGuiPopupFlags.None ) )
		{
			g.NextWindowData.ClearFlags();
			return false;
		}

		string name = (flags & ImGuiWindowFlags.ChildMenu) != 0
			? $"##Menu_{g.BeginMenuDepth:D2}"
			: $"##Popup_{id:X8}";

		flags |= ImGuiWindowFlags.Popup;
		bool isOpen = Begin( name, flags );
		if ( !isOpen )
			EndPopup();
		return isOpen;
	}

	/// <summary>Return true if the popup is open, and you can start outputting to it. Call EndPopup() only if this returns true.</summary>
	public static bool BeginPopup( string strId, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		var g = G;
		if ( g.OpenPopupStack.Count <= g.BeginPopupStack.Count )
		{
			g.NextWindowData.ClearFlags();
			return false;
		}
		flags |= ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings;
		int id = g.CurrentWindow.GetID( strId );
		return BeginPopupEx( id, flags );
	}

	/// <summary>
	/// Modal popups block interactions behind them and can't be closed by clicking outside. Supports a close button through <paramref name="open"/>.
	/// </summary>
	public static bool BeginPopupModal( string name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
		=> BeginPopupModalImpl( name, true, ref open, flags );

	public static bool BeginPopupModal( string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		bool dummy = true;
		return BeginPopupModalImpl( name, false, ref dummy, flags );
	}

	private static bool BeginPopupModalImpl( string name, bool hasClose, ref bool pOpen, ImGuiWindowFlags flags )
	{
		var g = G;
		var window = g.CurrentWindow;
		int id = window.GetID( name );
		if ( !IsPopupOpen( id, ImGuiPopupFlags.None ) )
		{
			g.NextWindowData.ClearFlags();
			if ( hasClose && pOpen )
				pOpen = true;
			return false;
		}

		// Center modal windows by default for increased visibility
		if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasPos) == 0 )
			SetNextWindowPos( g.IO.DisplaySize * 0.5f, ImGuiCond.FirstUseEver, new Vector2( 0.5f, 0.5f ) );

		flags |= ImGuiWindowFlags.Popup | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse;
		bool isOpen = BeginImpl( name, hasClose, ref pOpen, flags );
		if ( !isOpen || (hasClose && !pOpen) )
		{
			EndPopup();
			if ( isOpen )
				ClosePopupToLevel( g.BeginPopupStack.Count, true );
			return false;
		}
		return isOpen;
	}

	/// <summary>Only call EndPopup() if BeginPopupXXX() returns true!</summary>
	public static void EndPopup()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( (window.Flags & ImGuiWindowFlags.Popup) == 0 || g.BeginPopupStack.Count == 0 )
		{
			Log.Warning( "ImGui: EndPopup() called without a matching BeginPopup()" );
			return;
		}
		End();
	}

	/// <summary>Helper to open a popup when clicked on the last item. Default to the right mouse button.</summary>
	public static void OpenPopupOnItemClick( string strId = null, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonRight )
	{
		var g = G;
		var window = g.CurrentWindow;
		int mouseButton = (int)(popupFlags & ImGuiPopupFlags.MouseButtonMask_);
		if ( IsMouseReleased( (ImGuiMouseButton)mouseButton ) && IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup ) )
		{
			int id = strId is not null ? window.GetID( strId ) : g.LastItemData.ID;
			OpenPopupEx( id, popupFlags );
		}
	}

	/// <summary>Open and begin a popup when clicked on the last item. Use with an empty str_id for a popup tied to the item's ID.</summary>
	public static bool BeginPopupContextItem( string strId = null, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonRight )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return false;
		int id = strId is not null ? window.GetID( strId ) : g.LastItemData.ID;
		if ( id == 0 )
			return false;
		int mouseButton = (int)(popupFlags & ImGuiPopupFlags.MouseButtonMask_);
		if ( IsMouseReleased( (ImGuiMouseButton)mouseButton ) && IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup ) )
			OpenPopupEx( id, popupFlags );
		return BeginPopupEx( id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings );
	}

	/// <summary>Open and begin a popup when clicked on the current window.</summary>
	public static bool BeginPopupContextWindow( string strId = null, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonRight )
	{
		var g = G;
		var window = g.CurrentWindow;
		strId ??= "window_context";
		int id = window.GetID( strId );
		int mouseButton = (int)(popupFlags & ImGuiPopupFlags.MouseButtonMask_);
		if ( IsMouseReleased( (ImGuiMouseButton)mouseButton ) && IsWindowHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup ) )
			if ( (popupFlags & ImGuiPopupFlags.NoOpenOverItems) == 0 || !IsAnyItemHovered() )
				OpenPopupEx( id, popupFlags );
		return BeginPopupEx( id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings );
	}

	/// <summary>Open and begin a popup when clicked in void (where there are no windows).</summary>
	public static bool BeginPopupContextVoid( string strId = null, ImGuiPopupFlags popupFlags = ImGuiPopupFlags.MouseButtonRight )
	{
		var g = G;
		var window = g.CurrentWindow;
		strId ??= "void_context";
		int id = window.GetID( strId );
		int mouseButton = (int)(popupFlags & ImGuiPopupFlags.MouseButtonMask_);
		if ( IsMouseReleased( (ImGuiMouseButton)mouseButton ) && !IsWindowHovered( ImGuiHoveredFlags.AnyWindow ) )
			if ( GetTopMostPopupModal() is null )
				OpenPopupEx( id, popupFlags );
		return BeginPopupEx( id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings );
	}
	#endregion

	#region Tooltips
	internal static bool BeginTooltipEx( bool overridePrevious = true )
	{
		var g = G;

		string windowName = $"##Tooltip_{g.TooltipOverrideCount:D2}";
		var window = FindWindowByName( windowName );
		if ( window is not null && window.Active && overridePrevious )
		{
			// Hide previous tooltip from being displayed. We can't easily "reset" the content of a window so we create a new one.
			window.Hidden = true;
			window.HiddenFramesCanSkipItems = 1;
			windowName = $"##Tooltip_{++g.TooltipOverrideCount:D2}";
		}

		var flags = ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;
		Begin( windowName, flags );
		return true;
	}

	/// <summary>Begin/append a tooltip window. Always call EndTooltip() when this returns true.</summary>
	public static bool BeginTooltip() => BeginTooltipEx();

	public static void EndTooltip()
	{
		var window = G.CurrentWindow;
		if ( (window.Flags & ImGuiWindowFlags.Tooltip) == 0 )
		{
			Log.Warning( "ImGui: EndTooltip() called without BeginTooltip()" );
			return;
		}
		End();
	}

	/// <summary>Set a text-only tooltip. Often used after an ImGui.IsItemHovered() check.</summary>
	public static void SetTooltip( string fmt, params object[] args )
	{
		if ( !BeginTooltipEx() )
			return;
		TextUnformatted( Format( fmt, args ) );
		EndTooltip();
	}

	/// <summary>Begin a tooltip if the last item is hovered for long enough (uses ImGuiHoveredFlags.ForTooltip).</summary>
	public static bool BeginItemTooltip()
	{
		if ( !IsItemHovered( ImGuiHoveredFlags.ForTooltip ) )
			return false;
		return BeginTooltipEx();
	}

	/// <summary>Set a text-only tooltip if the last item is hovered for long enough.</summary>
	public static void SetItemTooltip( string fmt, params object[] args )
	{
		if ( BeginItemTooltip() )
		{
			TextUnformatted( Format( fmt, args ) );
			EndTooltip();
		}
	}
	#endregion
}

#endregion

#region ImGui (ImGui.Style)

public static partial class ImGui
{
	#region Colors
	/// <summary>Get a style color with the style alpha applied, as a packed 32-bit color.</summary>
	public static Color32 GetColorU32( ImGuiCol idx, float alphaMul = 1.0f ) => GetColorU32Internal( idx, alphaMul );

	public static Color32 GetColorU32( Vector4 col )
	{
		col.w *= G.Style.Alpha;
		return ColorConvertFloat4ToU32( col );
	}

	public static Color32 GetColorU32( Color col ) => GetColorU32( new Vector4( col.r, col.g, col.b, col.a ) );

	public static Color32 GetColorU32( Color32 col, float alphaMul = 1.0f )
	{
		float styleAlpha = G.Style.Alpha * alphaMul;
		if ( styleAlpha >= 1.0f )
			return col;
		return col with { a = (byte)(col.a * styleAlpha) };
	}

	public static Vector4 GetStyleColorVec4( ImGuiCol idx ) => G.Style.Colors[(int)idx];

	public static string GetStyleColorName( ImGuiCol idx ) => idx.ToString();

	public static void PushStyleColor( ImGuiCol idx, Vector4 col )
	{
		var g = G;
		g.ColorStack.Add( new ImGuiColorMod { Col = idx, BackupValue = g.Style.Colors[(int)idx] } );
		g.Style.Colors[(int)idx] = col;
	}

	public static void PushStyleColor( ImGuiCol idx, Color32 col ) => PushStyleColor( idx, ColorConvertU32ToFloat4( col ) );
	public static void PushStyleColor( ImGuiCol idx, Color col ) => PushStyleColor( idx, new Vector4( col.r, col.g, col.b, col.a ) );

	public static void PopStyleColor( int count = 1 )
	{
		var g = G;
		if ( g.ColorStack.Count < count )
		{
			Log.Warning( "ImGui: PopStyleColor() called more times than PushStyleColor()" );
			count = g.ColorStack.Count;
		}
		while ( count > 0 )
		{
			var backup = g.ColorStack[^1];
			g.Style.Colors[(int)backup.Col] = backup.BackupValue;
			g.ColorStack.RemoveAt( g.ColorStack.Count - 1 );
			count--;
		}
	}
	#endregion

	#region Style vars
	private static bool IsVec2Var( ImGuiStyleVar idx ) => idx switch
	{
		ImGuiStyleVar.WindowPadding or ImGuiStyleVar.WindowMinSize or ImGuiStyleVar.WindowTitleAlign or
		ImGuiStyleVar.FramePadding or ImGuiStyleVar.ItemSpacing or ImGuiStyleVar.ItemInnerSpacing or
		ImGuiStyleVar.CellPadding or ImGuiStyleVar.TableAngledHeadersTextAlign or ImGuiStyleVar.ButtonTextAlign or
		ImGuiStyleVar.SelectableTextAlign or ImGuiStyleVar.SeparatorTextAlign or ImGuiStyleVar.SeparatorTextPadding => true,
		_ => false
	};

	private static Vector2 GetStyleVar( ImGuiStyleVar idx )
	{
		var s = G.Style;
		return idx switch
		{
			ImGuiStyleVar.Alpha => new Vector2( s.Alpha, 0 ),
			ImGuiStyleVar.DisabledAlpha => new Vector2( s.DisabledAlpha, 0 ),
			ImGuiStyleVar.WindowPadding => s.WindowPadding,
			ImGuiStyleVar.WindowRounding => new Vector2( s.WindowRounding, 0 ),
			ImGuiStyleVar.WindowBorderSize => new Vector2( s.WindowBorderSize, 0 ),
			ImGuiStyleVar.WindowMinSize => s.WindowMinSize,
			ImGuiStyleVar.WindowTitleAlign => s.WindowTitleAlign,
			ImGuiStyleVar.ChildRounding => new Vector2( s.ChildRounding, 0 ),
			ImGuiStyleVar.ChildBorderSize => new Vector2( s.ChildBorderSize, 0 ),
			ImGuiStyleVar.PopupRounding => new Vector2( s.PopupRounding, 0 ),
			ImGuiStyleVar.PopupBorderSize => new Vector2( s.PopupBorderSize, 0 ),
			ImGuiStyleVar.FramePadding => s.FramePadding,
			ImGuiStyleVar.FrameRounding => new Vector2( s.FrameRounding, 0 ),
			ImGuiStyleVar.FrameBorderSize => new Vector2( s.FrameBorderSize, 0 ),
			ImGuiStyleVar.ItemSpacing => s.ItemSpacing,
			ImGuiStyleVar.ItemInnerSpacing => s.ItemInnerSpacing,
			ImGuiStyleVar.IndentSpacing => new Vector2( s.IndentSpacing, 0 ),
			ImGuiStyleVar.CellPadding => s.CellPadding,
			ImGuiStyleVar.ScrollbarSize => new Vector2( s.ScrollbarSize, 0 ),
			ImGuiStyleVar.ScrollbarRounding => new Vector2( s.ScrollbarRounding, 0 ),
			ImGuiStyleVar.GrabMinSize => new Vector2( s.GrabMinSize, 0 ),
			ImGuiStyleVar.GrabRounding => new Vector2( s.GrabRounding, 0 ),
			ImGuiStyleVar.TabRounding => new Vector2( s.TabRounding, 0 ),
			ImGuiStyleVar.TabBorderSize => new Vector2( s.TabBorderSize, 0 ),
			ImGuiStyleVar.TabBarBorderSize => new Vector2( s.TabBarBorderSize, 0 ),
			ImGuiStyleVar.TabBarOverlineSize => new Vector2( s.TabBarOverlineSize, 0 ),
			ImGuiStyleVar.TableAngledHeadersAngle => new Vector2( s.TableAngledHeadersAngle, 0 ),
			ImGuiStyleVar.TableAngledHeadersTextAlign => s.TableAngledHeadersTextAlign,
			ImGuiStyleVar.ButtonTextAlign => s.ButtonTextAlign,
			ImGuiStyleVar.SelectableTextAlign => s.SelectableTextAlign,
			ImGuiStyleVar.SeparatorTextBorderSize => new Vector2( s.SeparatorTextBorderSize, 0 ),
			ImGuiStyleVar.SeparatorTextAlign => s.SeparatorTextAlign,
			ImGuiStyleVar.SeparatorTextPadding => s.SeparatorTextPadding,
			_ => Vector2.Zero
		};
	}

	private static void SetStyleVar( ImGuiStyleVar idx, Vector2 v )
	{
		var s = G.Style;
		switch ( idx )
		{
			case ImGuiStyleVar.Alpha: s.Alpha = v.x; break;
			case ImGuiStyleVar.DisabledAlpha: s.DisabledAlpha = v.x; break;
			case ImGuiStyleVar.WindowPadding: s.WindowPadding = v; break;
			case ImGuiStyleVar.WindowRounding: s.WindowRounding = v.x; break;
			case ImGuiStyleVar.WindowBorderSize: s.WindowBorderSize = v.x; break;
			case ImGuiStyleVar.WindowMinSize: s.WindowMinSize = v; break;
			case ImGuiStyleVar.WindowTitleAlign: s.WindowTitleAlign = v; break;
			case ImGuiStyleVar.ChildRounding: s.ChildRounding = v.x; break;
			case ImGuiStyleVar.ChildBorderSize: s.ChildBorderSize = v.x; break;
			case ImGuiStyleVar.PopupRounding: s.PopupRounding = v.x; break;
			case ImGuiStyleVar.PopupBorderSize: s.PopupBorderSize = v.x; break;
			case ImGuiStyleVar.FramePadding: s.FramePadding = v; break;
			case ImGuiStyleVar.FrameRounding: s.FrameRounding = v.x; break;
			case ImGuiStyleVar.FrameBorderSize: s.FrameBorderSize = v.x; break;
			case ImGuiStyleVar.ItemSpacing: s.ItemSpacing = v; break;
			case ImGuiStyleVar.ItemInnerSpacing: s.ItemInnerSpacing = v; break;
			case ImGuiStyleVar.IndentSpacing: s.IndentSpacing = v.x; break;
			case ImGuiStyleVar.CellPadding: s.CellPadding = v; break;
			case ImGuiStyleVar.ScrollbarSize: s.ScrollbarSize = v.x; break;
			case ImGuiStyleVar.ScrollbarRounding: s.ScrollbarRounding = v.x; break;
			case ImGuiStyleVar.GrabMinSize: s.GrabMinSize = v.x; break;
			case ImGuiStyleVar.GrabRounding: s.GrabRounding = v.x; break;
			case ImGuiStyleVar.TabRounding: s.TabRounding = v.x; break;
			case ImGuiStyleVar.TabBorderSize: s.TabBorderSize = v.x; break;
			case ImGuiStyleVar.TabBarBorderSize: s.TabBarBorderSize = v.x; break;
			case ImGuiStyleVar.TabBarOverlineSize: s.TabBarOverlineSize = v.x; break;
			case ImGuiStyleVar.TableAngledHeadersAngle: s.TableAngledHeadersAngle = v.x; break;
			case ImGuiStyleVar.TableAngledHeadersTextAlign: s.TableAngledHeadersTextAlign = v; break;
			case ImGuiStyleVar.ButtonTextAlign: s.ButtonTextAlign = v; break;
			case ImGuiStyleVar.SelectableTextAlign: s.SelectableTextAlign = v; break;
			case ImGuiStyleVar.SeparatorTextBorderSize: s.SeparatorTextBorderSize = v.x; break;
			case ImGuiStyleVar.SeparatorTextAlign: s.SeparatorTextAlign = v; break;
			case ImGuiStyleVar.SeparatorTextPadding: s.SeparatorTextPadding = v; break;
		}
	}

	public static void PushStyleVar( ImGuiStyleVar idx, float val )
	{
		if ( IsVec2Var( idx ) )
		{
			Log.Warning( $"ImGui: PushStyleVar({idx}) expects a Vector2" );
			return;
		}
		var g = G;
		g.StyleVarStack.Add( new ImGuiStyleMod { VarIdx = idx, BackupValue = GetStyleVar( idx ) } );
		SetStyleVar( idx, new Vector2( val, 0 ) );
	}

	public static void PushStyleVar( ImGuiStyleVar idx, Vector2 val )
	{
		if ( !IsVec2Var( idx ) )
		{
			Log.Warning( $"ImGui: PushStyleVar({idx}) expects a float" );
			return;
		}
		var g = G;
		g.StyleVarStack.Add( new ImGuiStyleMod { VarIdx = idx, BackupValue = GetStyleVar( idx ) } );
		SetStyleVar( idx, val );
	}

	public static void PushStyleVarX( ImGuiStyleVar idx, float valX ) => PushStyleVar( idx, new Vector2( valX, GetStyleVar( idx ).y ) );
	public static void PushStyleVarY( ImGuiStyleVar idx, float valY ) => PushStyleVar( idx, new Vector2( GetStyleVar( idx ).x, valY ) );

	public static void PopStyleVar( int count = 1 )
	{
		var g = G;
		if ( g.StyleVarStack.Count < count )
		{
			Log.Warning( "ImGui: PopStyleVar() called more times than PushStyleVar()" );
			count = g.StyleVarStack.Count;
		}
		while ( count > 0 )
		{
			var backup = g.StyleVarStack[^1];
			SetStyleVar( backup.VarIdx, backup.BackupValue );
			g.StyleVarStack.RemoveAt( g.StyleVarStack.Count - 1 );
			count--;
		}
	}
	#endregion

	#region Fonts
	/// <summary>
	/// Change the font for subsequent text. <paramref name="sizePixels"/> is the font size at the reference
	/// resolution (scaled like the rest of the UI); 0 keeps the current size. <paramref name="fontName"/> null keeps the current family.
	/// </summary>
	public static void PushFont( string fontName = null, float sizePixels = 0f, int weight = 0 )
	{
		var g = G;
		g.FontStack.Add( new ImGuiContext.FontStackEntry { FontSize = g.FontSize, FontPointSize = g.FontPointSize, FontName = g.FontName, FontWeight = g.FontWeight } );
		if ( fontName is not null ) g.FontName = fontName;
		if ( weight > 0 ) g.FontWeight = weight;
		if ( sizePixels > 0f ) g.FontPointSize = sizePixels * g.AppliedStyleScale;
		g.FontSize = MeasureLineHeight( g.FontPointSize, g.FontName, g.FontWeight );
		g.TextSizeCache.Clear();
	}

	public static void PopFont()
	{
		var g = G;
		if ( g.FontStack.Count == 0 )
			return;
		var e = g.FontStack[^1];
		g.FontStack.RemoveAt( g.FontStack.Count - 1 );
		g.FontSize = e.FontSize;
		g.FontPointSize = e.FontPointSize;
		g.FontName = e.FontName;
		g.FontWeight = e.FontWeight;
		g.TextSizeCache.Clear();
	}
	#endregion

	#region Style presets
	private static Vector4 V4( float r, float g, float b, float a ) => new( r, g, b, a );

	public static void StyleColorsDark( ImGuiStyle dst = null )
	{
		var style = dst ?? G.Style;
		var c = style.Colors;
		c[(int)ImGuiCol.Text] = V4( 1.00f, 1.00f, 1.00f, 1.00f );
		c[(int)ImGuiCol.TextDisabled] = V4( 0.50f, 0.50f, 0.50f, 1.00f );
		c[(int)ImGuiCol.WindowBg] = V4( 0.06f, 0.06f, 0.06f, 0.94f );
		c[(int)ImGuiCol.ChildBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.PopupBg] = V4( 0.08f, 0.08f, 0.08f, 0.94f );
		c[(int)ImGuiCol.Border] = V4( 0.43f, 0.43f, 0.50f, 0.50f );
		c[(int)ImGuiCol.BorderShadow] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.FrameBg] = V4( 0.16f, 0.29f, 0.48f, 0.54f );
		c[(int)ImGuiCol.FrameBgHovered] = V4( 0.26f, 0.59f, 0.98f, 0.40f );
		c[(int)ImGuiCol.FrameBgActive] = V4( 0.26f, 0.59f, 0.98f, 0.67f );
		c[(int)ImGuiCol.TitleBg] = V4( 0.04f, 0.04f, 0.04f, 1.00f );
		c[(int)ImGuiCol.TitleBgActive] = V4( 0.16f, 0.29f, 0.48f, 1.00f );
		c[(int)ImGuiCol.TitleBgCollapsed] = V4( 0.00f, 0.00f, 0.00f, 0.51f );
		c[(int)ImGuiCol.MenuBarBg] = V4( 0.14f, 0.14f, 0.14f, 1.00f );
		c[(int)ImGuiCol.ScrollbarBg] = V4( 0.02f, 0.02f, 0.02f, 0.53f );
		c[(int)ImGuiCol.ScrollbarGrab] = V4( 0.31f, 0.31f, 0.31f, 1.00f );
		c[(int)ImGuiCol.ScrollbarGrabHovered] = V4( 0.41f, 0.41f, 0.41f, 1.00f );
		c[(int)ImGuiCol.ScrollbarGrabActive] = V4( 0.51f, 0.51f, 0.51f, 1.00f );
		c[(int)ImGuiCol.CheckMark] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.SliderGrab] = V4( 0.24f, 0.52f, 0.88f, 1.00f );
		c[(int)ImGuiCol.SliderGrabActive] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Button] = V4( 0.26f, 0.59f, 0.98f, 0.40f );
		c[(int)ImGuiCol.ButtonHovered] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.ButtonActive] = V4( 0.06f, 0.53f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Header] = V4( 0.26f, 0.59f, 0.98f, 0.31f );
		c[(int)ImGuiCol.HeaderHovered] = V4( 0.26f, 0.59f, 0.98f, 0.80f );
		c[(int)ImGuiCol.HeaderActive] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Separator] = c[(int)ImGuiCol.Border];
		c[(int)ImGuiCol.SeparatorHovered] = V4( 0.10f, 0.40f, 0.75f, 0.78f );
		c[(int)ImGuiCol.SeparatorActive] = V4( 0.10f, 0.40f, 0.75f, 1.00f );
		c[(int)ImGuiCol.ResizeGrip] = V4( 0.26f, 0.59f, 0.98f, 0.20f );
		c[(int)ImGuiCol.ResizeGripHovered] = V4( 0.26f, 0.59f, 0.98f, 0.67f );
		c[(int)ImGuiCol.ResizeGripActive] = V4( 0.26f, 0.59f, 0.98f, 0.95f );
		c[(int)ImGuiCol.InputTextCursor] = c[(int)ImGuiCol.Text];
		c[(int)ImGuiCol.TabHovered] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.Tab] = Vector4.Lerp( c[(int)ImGuiCol.Header], c[(int)ImGuiCol.TitleBgActive], 0.80f );
		c[(int)ImGuiCol.TabSelected] = Vector4.Lerp( c[(int)ImGuiCol.HeaderActive], c[(int)ImGuiCol.TitleBgActive], 0.60f );
		c[(int)ImGuiCol.TabSelectedOverline] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TabDimmed] = Vector4.Lerp( c[(int)ImGuiCol.Tab], c[(int)ImGuiCol.TitleBg], 0.80f );
		c[(int)ImGuiCol.TabDimmedSelected] = Vector4.Lerp( c[(int)ImGuiCol.TabSelected], c[(int)ImGuiCol.TitleBg], 0.40f );
		c[(int)ImGuiCol.TabDimmedSelectedOverline] = V4( 0.50f, 0.50f, 0.50f, 0.00f );
		c[(int)ImGuiCol.PlotLines] = V4( 0.61f, 0.61f, 0.61f, 1.00f );
		c[(int)ImGuiCol.PlotLinesHovered] = V4( 1.00f, 0.43f, 0.35f, 1.00f );
		c[(int)ImGuiCol.PlotHistogram] = V4( 0.90f, 0.70f, 0.00f, 1.00f );
		c[(int)ImGuiCol.PlotHistogramHovered] = V4( 1.00f, 0.60f, 0.00f, 1.00f );
		c[(int)ImGuiCol.TableHeaderBg] = V4( 0.19f, 0.19f, 0.20f, 1.00f );
		c[(int)ImGuiCol.TableBorderStrong] = V4( 0.31f, 0.31f, 0.35f, 1.00f );
		c[(int)ImGuiCol.TableBorderLight] = V4( 0.23f, 0.23f, 0.25f, 1.00f );
		c[(int)ImGuiCol.TableRowBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.TableRowBgAlt] = V4( 1.00f, 1.00f, 1.00f, 0.06f );
		c[(int)ImGuiCol.TextLink] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TextSelectedBg] = V4( 0.26f, 0.59f, 0.98f, 0.35f );
		c[(int)ImGuiCol.DragDropTarget] = V4( 1.00f, 1.00f, 0.00f, 0.90f );
		c[(int)ImGuiCol.NavCursor] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.NavWindowingHighlight] = V4( 1.00f, 1.00f, 1.00f, 0.70f );
		c[(int)ImGuiCol.NavWindowingDimBg] = V4( 0.80f, 0.80f, 0.80f, 0.20f );
		c[(int)ImGuiCol.ModalWindowDimBg] = V4( 0.80f, 0.80f, 0.80f, 0.35f );
	}

	public static void StyleColorsLight( ImGuiStyle dst = null )
	{
		var style = dst ?? G.Style;
		var c = style.Colors;
		c[(int)ImGuiCol.Text] = V4( 0.00f, 0.00f, 0.00f, 1.00f );
		c[(int)ImGuiCol.TextDisabled] = V4( 0.60f, 0.60f, 0.60f, 1.00f );
		c[(int)ImGuiCol.WindowBg] = V4( 0.94f, 0.94f, 0.94f, 1.00f );
		c[(int)ImGuiCol.ChildBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.PopupBg] = V4( 1.00f, 1.00f, 1.00f, 0.98f );
		c[(int)ImGuiCol.Border] = V4( 0.00f, 0.00f, 0.00f, 0.30f );
		c[(int)ImGuiCol.BorderShadow] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.FrameBg] = V4( 1.00f, 1.00f, 1.00f, 1.00f );
		c[(int)ImGuiCol.FrameBgHovered] = V4( 0.26f, 0.59f, 0.98f, 0.40f );
		c[(int)ImGuiCol.FrameBgActive] = V4( 0.26f, 0.59f, 0.98f, 0.67f );
		c[(int)ImGuiCol.TitleBg] = V4( 0.96f, 0.96f, 0.96f, 1.00f );
		c[(int)ImGuiCol.TitleBgActive] = V4( 0.82f, 0.82f, 0.82f, 1.00f );
		c[(int)ImGuiCol.TitleBgCollapsed] = V4( 1.00f, 1.00f, 1.00f, 0.51f );
		c[(int)ImGuiCol.MenuBarBg] = V4( 0.86f, 0.86f, 0.86f, 1.00f );
		c[(int)ImGuiCol.ScrollbarBg] = V4( 0.98f, 0.98f, 0.98f, 0.53f );
		c[(int)ImGuiCol.ScrollbarGrab] = V4( 0.69f, 0.69f, 0.69f, 0.80f );
		c[(int)ImGuiCol.ScrollbarGrabHovered] = V4( 0.49f, 0.49f, 0.49f, 0.80f );
		c[(int)ImGuiCol.ScrollbarGrabActive] = V4( 0.49f, 0.49f, 0.49f, 1.00f );
		c[(int)ImGuiCol.CheckMark] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.SliderGrab] = V4( 0.26f, 0.59f, 0.98f, 0.78f );
		c[(int)ImGuiCol.SliderGrabActive] = V4( 0.46f, 0.54f, 0.80f, 0.60f );
		c[(int)ImGuiCol.Button] = V4( 0.26f, 0.59f, 0.98f, 0.40f );
		c[(int)ImGuiCol.ButtonHovered] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.ButtonActive] = V4( 0.06f, 0.53f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Header] = V4( 0.26f, 0.59f, 0.98f, 0.31f );
		c[(int)ImGuiCol.HeaderHovered] = V4( 0.26f, 0.59f, 0.98f, 0.80f );
		c[(int)ImGuiCol.HeaderActive] = V4( 0.26f, 0.59f, 0.98f, 1.00f );
		c[(int)ImGuiCol.Separator] = V4( 0.39f, 0.39f, 0.39f, 0.62f );
		c[(int)ImGuiCol.SeparatorHovered] = V4( 0.14f, 0.44f, 0.80f, 0.78f );
		c[(int)ImGuiCol.SeparatorActive] = V4( 0.14f, 0.44f, 0.80f, 1.00f );
		c[(int)ImGuiCol.ResizeGrip] = V4( 0.35f, 0.35f, 0.35f, 0.17f );
		c[(int)ImGuiCol.ResizeGripHovered] = V4( 0.26f, 0.59f, 0.98f, 0.67f );
		c[(int)ImGuiCol.ResizeGripActive] = V4( 0.26f, 0.59f, 0.98f, 0.95f );
		c[(int)ImGuiCol.InputTextCursor] = c[(int)ImGuiCol.Text];
		c[(int)ImGuiCol.TabHovered] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.Tab] = Vector4.Lerp( c[(int)ImGuiCol.Header], c[(int)ImGuiCol.TitleBgActive], 0.90f );
		c[(int)ImGuiCol.TabSelected] = Vector4.Lerp( c[(int)ImGuiCol.HeaderActive], c[(int)ImGuiCol.TitleBgActive], 0.60f );
		c[(int)ImGuiCol.TabSelectedOverline] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TabDimmed] = Vector4.Lerp( c[(int)ImGuiCol.Tab], c[(int)ImGuiCol.TitleBg], 0.80f );
		c[(int)ImGuiCol.TabDimmedSelected] = Vector4.Lerp( c[(int)ImGuiCol.TabSelected], c[(int)ImGuiCol.TitleBg], 0.40f );
		c[(int)ImGuiCol.TabDimmedSelectedOverline] = V4( 0.26f, 0.59f, 1.00f, 0.00f );
		c[(int)ImGuiCol.PlotLines] = V4( 0.39f, 0.39f, 0.39f, 1.00f );
		c[(int)ImGuiCol.PlotLinesHovered] = V4( 1.00f, 0.43f, 0.35f, 1.00f );
		c[(int)ImGuiCol.PlotHistogram] = V4( 0.90f, 0.70f, 0.00f, 1.00f );
		c[(int)ImGuiCol.PlotHistogramHovered] = V4( 1.00f, 0.45f, 0.00f, 1.00f );
		c[(int)ImGuiCol.TableHeaderBg] = V4( 0.78f, 0.87f, 0.98f, 1.00f );
		c[(int)ImGuiCol.TableBorderStrong] = V4( 0.57f, 0.57f, 0.64f, 1.00f );
		c[(int)ImGuiCol.TableBorderLight] = V4( 0.68f, 0.68f, 0.74f, 1.00f );
		c[(int)ImGuiCol.TableRowBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.TableRowBgAlt] = V4( 0.30f, 0.30f, 0.30f, 0.09f );
		c[(int)ImGuiCol.TextLink] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TextSelectedBg] = V4( 0.26f, 0.59f, 0.98f, 0.35f );
		c[(int)ImGuiCol.DragDropTarget] = V4( 0.26f, 0.59f, 0.98f, 0.95f );
		c[(int)ImGuiCol.NavCursor] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.NavWindowingHighlight] = V4( 0.70f, 0.70f, 0.70f, 0.70f );
		c[(int)ImGuiCol.NavWindowingDimBg] = V4( 0.20f, 0.20f, 0.20f, 0.20f );
		c[(int)ImGuiCol.ModalWindowDimBg] = V4( 0.20f, 0.20f, 0.20f, 0.35f );
	}

	public static void StyleColorsClassic( ImGuiStyle dst = null )
	{
		var style = dst ?? G.Style;
		var c = style.Colors;
		c[(int)ImGuiCol.Text] = V4( 0.90f, 0.90f, 0.90f, 1.00f );
		c[(int)ImGuiCol.TextDisabled] = V4( 0.60f, 0.60f, 0.60f, 1.00f );
		c[(int)ImGuiCol.WindowBg] = V4( 0.00f, 0.00f, 0.00f, 0.85f );
		c[(int)ImGuiCol.ChildBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.PopupBg] = V4( 0.11f, 0.11f, 0.14f, 0.92f );
		c[(int)ImGuiCol.Border] = V4( 0.50f, 0.50f, 0.50f, 0.50f );
		c[(int)ImGuiCol.BorderShadow] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.FrameBg] = V4( 0.43f, 0.43f, 0.43f, 0.39f );
		c[(int)ImGuiCol.FrameBgHovered] = V4( 0.47f, 0.47f, 0.69f, 0.40f );
		c[(int)ImGuiCol.FrameBgActive] = V4( 0.42f, 0.41f, 0.64f, 0.69f );
		c[(int)ImGuiCol.TitleBg] = V4( 0.27f, 0.27f, 0.54f, 0.83f );
		c[(int)ImGuiCol.TitleBgActive] = V4( 0.32f, 0.32f, 0.63f, 0.87f );
		c[(int)ImGuiCol.TitleBgCollapsed] = V4( 0.40f, 0.40f, 0.80f, 0.20f );
		c[(int)ImGuiCol.MenuBarBg] = V4( 0.40f, 0.40f, 0.55f, 0.80f );
		c[(int)ImGuiCol.ScrollbarBg] = V4( 0.20f, 0.25f, 0.30f, 0.60f );
		c[(int)ImGuiCol.ScrollbarGrab] = V4( 0.40f, 0.40f, 0.80f, 0.30f );
		c[(int)ImGuiCol.ScrollbarGrabHovered] = V4( 0.40f, 0.40f, 0.80f, 0.40f );
		c[(int)ImGuiCol.ScrollbarGrabActive] = V4( 0.41f, 0.39f, 0.80f, 0.60f );
		c[(int)ImGuiCol.CheckMark] = V4( 0.90f, 0.90f, 0.90f, 0.50f );
		c[(int)ImGuiCol.SliderGrab] = V4( 1.00f, 1.00f, 1.00f, 0.30f );
		c[(int)ImGuiCol.SliderGrabActive] = V4( 0.41f, 0.39f, 0.80f, 0.60f );
		c[(int)ImGuiCol.Button] = V4( 0.35f, 0.40f, 0.61f, 0.62f );
		c[(int)ImGuiCol.ButtonHovered] = V4( 0.40f, 0.48f, 0.71f, 0.79f );
		c[(int)ImGuiCol.ButtonActive] = V4( 0.46f, 0.54f, 0.80f, 1.00f );
		c[(int)ImGuiCol.Header] = V4( 0.40f, 0.40f, 0.90f, 0.45f );
		c[(int)ImGuiCol.HeaderHovered] = V4( 0.45f, 0.45f, 0.90f, 0.80f );
		c[(int)ImGuiCol.HeaderActive] = V4( 0.53f, 0.53f, 0.87f, 0.80f );
		c[(int)ImGuiCol.Separator] = V4( 0.50f, 0.50f, 0.50f, 0.60f );
		c[(int)ImGuiCol.SeparatorHovered] = V4( 0.60f, 0.60f, 0.70f, 1.00f );
		c[(int)ImGuiCol.SeparatorActive] = V4( 0.70f, 0.70f, 0.90f, 1.00f );
		c[(int)ImGuiCol.ResizeGrip] = V4( 1.00f, 1.00f, 1.00f, 0.10f );
		c[(int)ImGuiCol.ResizeGripHovered] = V4( 0.78f, 0.82f, 1.00f, 0.60f );
		c[(int)ImGuiCol.ResizeGripActive] = V4( 0.78f, 0.82f, 1.00f, 0.90f );
		c[(int)ImGuiCol.InputTextCursor] = c[(int)ImGuiCol.Text];
		c[(int)ImGuiCol.TabHovered] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.Tab] = Vector4.Lerp( c[(int)ImGuiCol.Header], c[(int)ImGuiCol.TitleBgActive], 0.80f );
		c[(int)ImGuiCol.TabSelected] = Vector4.Lerp( c[(int)ImGuiCol.HeaderActive], c[(int)ImGuiCol.TitleBgActive], 0.60f );
		c[(int)ImGuiCol.TabSelectedOverline] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TabDimmed] = Vector4.Lerp( c[(int)ImGuiCol.Tab], c[(int)ImGuiCol.TitleBg], 0.80f );
		c[(int)ImGuiCol.TabDimmedSelected] = Vector4.Lerp( c[(int)ImGuiCol.TabSelected], c[(int)ImGuiCol.TitleBg], 0.40f );
		c[(int)ImGuiCol.TabDimmedSelectedOverline] = V4( 0.53f, 0.53f, 0.87f, 0.00f );
		c[(int)ImGuiCol.PlotLines] = V4( 1.00f, 1.00f, 1.00f, 1.00f );
		c[(int)ImGuiCol.PlotLinesHovered] = V4( 0.90f, 0.70f, 0.00f, 1.00f );
		c[(int)ImGuiCol.PlotHistogram] = V4( 0.90f, 0.70f, 0.00f, 1.00f );
		c[(int)ImGuiCol.PlotHistogramHovered] = V4( 1.00f, 0.60f, 0.00f, 1.00f );
		c[(int)ImGuiCol.TableHeaderBg] = V4( 0.27f, 0.27f, 0.38f, 1.00f );
		c[(int)ImGuiCol.TableBorderStrong] = V4( 0.31f, 0.31f, 0.45f, 1.00f );
		c[(int)ImGuiCol.TableBorderLight] = V4( 0.26f, 0.26f, 0.28f, 1.00f );
		c[(int)ImGuiCol.TableRowBg] = V4( 0.00f, 0.00f, 0.00f, 0.00f );
		c[(int)ImGuiCol.TableRowBgAlt] = V4( 1.00f, 1.00f, 1.00f, 0.07f );
		c[(int)ImGuiCol.TextLink] = c[(int)ImGuiCol.HeaderActive];
		c[(int)ImGuiCol.TextSelectedBg] = V4( 0.00f, 0.00f, 1.00f, 0.35f );
		c[(int)ImGuiCol.DragDropTarget] = V4( 1.00f, 1.00f, 0.00f, 0.90f );
		c[(int)ImGuiCol.NavCursor] = c[(int)ImGuiCol.HeaderHovered];
		c[(int)ImGuiCol.NavWindowingHighlight] = V4( 1.00f, 1.00f, 1.00f, 0.70f );
		c[(int)ImGuiCol.NavWindowingDimBg] = V4( 0.80f, 0.80f, 0.80f, 0.20f );
		c[(int)ImGuiCol.ModalWindowDimBg] = V4( 0.20f, 0.20f, 0.20f, 0.35f );
	}
	#endregion
}

#endregion

#region ImGui (ImGui.Window)

public static partial class ImGui
{
	private const float FLT_MAX = float.MaxValue;
	private const float WINDOWS_RESIZE_FROM_EDGES_FEEDBACK_TIMER = 0.04f;

	private static float WindowsHoverPadding => MathF.Max( G.Style.TouchExtraPadding.x, G.Style.WindowBorderHoverPadding );

	internal static float Axis( Vector2 v, int axis ) => axis == 0 ? v.x : v.y;
	internal static Vector2 WithAxis( Vector2 v, int axis, float value ) => axis == 0 ? new Vector2( value, v.y ) : new Vector2( v.x, value );

	private struct ResizeGripDef
	{
		public Vector2 CornerPosN;
		public Vector2 InnerDir;
		public int AngleMin12, AngleMax12;
	}

	private static readonly ResizeGripDef[] ResizeGripDefs =
	{
		new() { CornerPosN = new Vector2( 1, 1 ), InnerDir = new Vector2( -1, -1 ), AngleMin12 = 0, AngleMax12 = 3 }, // Lower-right
		new() { CornerPosN = new Vector2( 0, 1 ), InnerDir = new Vector2( +1, -1 ), AngleMin12 = 3, AngleMax12 = 6 }, // Lower-left
	};

	private struct ResizeBorderDef
	{
		public Vector2 SegmentN1, SegmentN2;
	}

	private static readonly ResizeBorderDef[] ResizeBorderDefs =
	{
		new() { SegmentN1 = new Vector2( 0, 1 ), SegmentN2 = new Vector2( 0, 0 ) }, // Left
		new() { SegmentN1 = new Vector2( 1, 0 ), SegmentN2 = new Vector2( 1, 1 ) }, // Right
		new() { SegmentN1 = new Vector2( 0, 0 ), SegmentN2 = new Vector2( 1, 0 ) }, // Up
		new() { SegmentN1 = new Vector2( 1, 1 ), SegmentN2 = new Vector2( 0, 1 ) }, // Down
	};

	#region Window lookup / creation
	internal static ImGuiWindow FindWindowByID( int id )
	{
		G.WindowsById.TryGetValue( id, out var window );
		return window;
	}

	internal static ImGuiWindow FindWindowByName( string name ) => FindWindowByID( ImHashStr( name, 0 ) );

	private static ImGuiWindow CreateNewWindow( string name, ImGuiWindowFlags flags )
	{
		var g = G;
		var window = new ImGuiWindow( g, name ) { Flags = flags };
		g.WindowsById[window.ID] = window;

		window.Pos = ImTrunc( new Vector2( 60, 60 ) * g.AppliedStyleScale );

		if ( (flags & ImGuiWindowFlags.NoSavedSettings) == 0 && g.SettingsWindows.TryGetValue( window.ID, out var settings ) )
		{
			window.Pos = settings.Pos;
			window.Size = window.SizeFull = settings.Size;
			window.Collapsed = settings.Collapsed;
			SetWindowConditionAllowFlags( window, ImGuiCond.FirstUseEver, false );
		}

		if ( (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 )
		{
			window.AutoFitFramesX = window.AutoFitFramesY = 2;
			window.AutoFitOnlyGrows = false;
		}
		else
		{
			if ( window.Size.x <= 0.0f ) window.AutoFitFramesX = 2;
			if ( window.Size.y <= 0.0f ) window.AutoFitFramesY = 2;
			window.AutoFitOnlyGrows = window.AutoFitFramesX > 0 || window.AutoFitFramesY > 0;
		}

		if ( (flags & ImGuiWindowFlags.ChildWindow) == 0 )
		{
			g.WindowsFocusOrder.Add( window );
			window.FocusOrder = g.WindowsFocusOrder.Count - 1;
		}

		if ( (flags & ImGuiWindowFlags.NoBringToFrontOnFocus) != 0 )
			g.Windows.Insert( 0, window );
		else
			g.Windows.Add( window );

		return window;
	}

	private static void SetWindowConditionAllowFlags( ImGuiWindow window, ImGuiCond flags, bool enabled )
	{
		if ( enabled )
		{
			window.SetWindowPosAllowFlags |= flags;
			window.SetWindowSizeAllowFlags |= flags;
			window.SetWindowCollapsedAllowFlags |= flags;
		}
		else
		{
			window.SetWindowPosAllowFlags &= ~flags;
			window.SetWindowSizeAllowFlags &= ~flags;
			window.SetWindowCollapsedAllowFlags &= ~flags;
		}
	}

	private static void UpdateWindowParentAndRootLinks( ImGuiWindow window, ImGuiWindowFlags flags, ImGuiWindow parentWindow )
	{
		window.ParentWindow = parentWindow;
		window.RootWindow = window.RootWindowPopupTree = window.RootWindowForTitleBarHighlight = window;
		if ( parentWindow is not null && (flags & ImGuiWindowFlags.ChildWindow) != 0 && (flags & ImGuiWindowFlags.Tooltip) == 0 )
			window.RootWindow = parentWindow.RootWindow;
		if ( parentWindow is not null && (flags & ImGuiWindowFlags.Popup) != 0 )
			window.RootWindowPopupTree = parentWindow.RootWindowPopupTree;
		if ( parentWindow is not null && (flags & ImGuiWindowFlags.Modal) == 0 && (flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Popup)) != 0 )
			window.RootWindowForTitleBarHighlight = parentWindow.RootWindowForTitleBarHighlight;
	}
	#endregion

	#region Window sizing
	private static void CalcWindowContentSizes( ImGuiWindow window, out Vector2 contentSizeCurrent, out Vector2 contentSizeIdeal )
	{
		if ( window.Collapsed && window.AutoFitFramesX <= 0 && window.AutoFitFramesY <= 0 )
		{
			contentSizeCurrent = window.ContentSize;
			contentSizeIdeal = window.ContentSizeIdeal;
			return;
		}
		if ( window.HiddenFramesCannotSkipItems == 0 && window.HiddenFramesCanSkipItems > 0 )
		{
			contentSizeCurrent = window.ContentSize;
			contentSizeIdeal = window.ContentSizeIdeal;
			return;
		}

		var dc = window.DC;
		contentSizeCurrent = new Vector2(
			window.ContentSizeExplicit.x != 0.0f ? window.ContentSizeExplicit.x : ImTrunc( dc.CursorMaxPos.x - dc.CursorStartPos.x ),
			window.ContentSizeExplicit.y != 0.0f ? window.ContentSizeExplicit.y : ImTrunc( dc.CursorMaxPos.y - dc.CursorStartPos.y ) );
		contentSizeIdeal = new Vector2(
			window.ContentSizeExplicit.x != 0.0f ? window.ContentSizeExplicit.x : ImTrunc( MathF.Max( dc.CursorMaxPos.x, dc.IdealMaxPos.x ) - dc.CursorStartPos.x ),
			window.ContentSizeExplicit.y != 0.0f ? window.ContentSizeExplicit.y : ImTrunc( MathF.Max( dc.CursorMaxPos.y, dc.IdealMaxPos.y ) - dc.CursorStartPos.y ) );
	}

	private static Vector2 CalcWindowSizeAfterConstraint( ImGuiWindow window, Vector2 sizeDesired )
	{
		var g = G;
		var newSize = sizeDesired;
		if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSizeConstraint) != 0 )
		{
			var cr = g.NextWindowData.SizeConstraintRect;
			newSize.x = (cr.Min.x >= 0 && cr.Max.x >= 0) ? ImClamp( newSize.x, cr.Min.x, cr.Max.x ) : window.SizeFull.x;
			newSize.y = (cr.Min.y >= 0 && cr.Max.y >= 0) ? ImClamp( newSize.y, cr.Min.y, cr.Max.y ) : window.SizeFull.y;
			if ( g.NextWindowData.SizeCallback is not null )
			{
				var data = new ImGuiSizeCallbackData { Pos = window.Pos, CurrentSize = window.SizeFull, DesiredSize = newSize };
				g.NextWindowData.SizeCallback( data );
				newSize = data.DesiredSize;
			}
			newSize = ImTrunc( newSize );
		}

		if ( (window.Flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.AlwaysAutoResize)) == 0 )
		{
			newSize = ImMax( newSize, g.Style.WindowMinSize );
			float minimumHeight = window.TitleBarHeight + window.MenuBarHeight + MathF.Max( 0.0f, g.Style.WindowRounding - 1.0f );
			newSize.y = MathF.Max( newSize.y, minimumHeight );
		}
		return newSize;
	}

	private static Vector2 CalcWindowAutoFitSize( ImGuiWindow window, Vector2 sizeContents )
	{
		var g = G;
		var style = g.Style;
		var sizeDecorations = new Vector2( window.DecoOuterSizeX1 + window.DecoOuterSizeX2, window.DecoOuterSizeY1 + window.DecoOuterSizeY2 );
		var sizePad = window.WindowPadding * 2.0f;
		var sizeDesired = sizeContents + sizePad + sizeDecorations;
		if ( (window.Flags & ImGuiWindowFlags.Tooltip) != 0 )
			return sizeDesired;

		var sizeMin = style.WindowMinSize;
		if ( (window.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.ChildWindow)) != 0 )
			sizeMin = ImMin( sizeMin, new Vector2( 4.0f, 4.0f ) );

		var availSize = g.IO.DisplaySize;
		var sizeAutoFit = ImClamp( sizeDesired, sizeMin, ImMax( sizeMin, availSize - style.DisplaySafeAreaPadding * 2.0f ) );

		var sizeAutoFitAfterConstraint = CalcWindowSizeAfterConstraint( window, sizeAutoFit );
		bool willHaveScrollbarX = (sizeAutoFitAfterConstraint.x - sizePad.x - sizeDecorations.x < sizeContents.x && (window.Flags & ImGuiWindowFlags.NoScrollbar) == 0 && (window.Flags & ImGuiWindowFlags.HorizontalScrollbar) != 0) || (window.Flags & ImGuiWindowFlags.AlwaysHorizontalScrollbar) != 0;
		bool willHaveScrollbarY = (sizeAutoFitAfterConstraint.y - sizePad.y - sizeDecorations.y < sizeContents.y && (window.Flags & ImGuiWindowFlags.NoScrollbar) == 0) || (window.Flags & ImGuiWindowFlags.AlwaysVerticalScrollbar) != 0;
		if ( willHaveScrollbarX ) sizeAutoFit.y += style.ScrollbarSize;
		if ( willHaveScrollbarY ) sizeAutoFit.x += style.ScrollbarSize;
		return sizeAutoFit;
	}

	internal static Vector2 CalcWindowNextAutoFitSize( ImGuiWindow window )
	{
		CalcWindowContentSizes( window, out _, out var sizeContentsIdeal );
		var sizeAutoFit = CalcWindowAutoFitSize( window, sizeContentsIdeal );
		return CalcWindowSizeAfterConstraint( window, sizeAutoFit );
	}

	private static void ClampWindowPos( ImGuiWindow window, ImRect visibilityRect )
	{
		var g = G;
		var sizeForClamping = window.Size;
		if ( g.IO.ConfigWindowsMoveFromTitleBarOnly && (window.Flags & ImGuiWindowFlags.NoTitleBar) == 0 )
			sizeForClamping.y = GetFrameHeight();
		window.Pos = ImClamp( window.Pos, visibilityRect.Min - sizeForClamping, visibilityRect.Max );
	}

	internal static void SetWindowPos( ImGuiWindow window, Vector2 pos, ImGuiCond cond = ImGuiCond.None )
	{
		if ( cond != ImGuiCond.None && (window.SetWindowPosAllowFlags & cond) == 0 )
			return;

		window.SetWindowPosAllowFlags &= ~(ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing);
		window.SetWindowPosVal = new Vector2( FLT_MAX, FLT_MAX );

		var oldPos = window.Pos;
		window.Pos = ImTrunc( pos );
		var offset = window.Pos - oldPos;
		if ( offset.x == 0f && offset.y == 0f )
			return;
		window.DC.CursorPos += offset;
		window.DC.CursorMaxPos += offset;
		window.DC.IdealMaxPos += offset;
		window.DC.CursorStartPos += offset;
	}

	internal static void SetWindowSize( ImGuiWindow window, Vector2 size, ImGuiCond cond = ImGuiCond.None )
	{
		if ( cond != ImGuiCond.None && (window.SetWindowSizeAllowFlags & cond) == 0 )
			return;

		window.SetWindowSizeAllowFlags &= ~(ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing);

		bool canAutoFit = (window.Flags & ImGuiWindowFlags.ChildWindow) == 0 || window.Appearing || (window.ChildFlags & ImGuiChildFlags.AlwaysAutoResize) != 0;
		if ( canAutoFit )
		{
			window.AutoFitFramesX = size.x <= 0.0f ? 2 : 0;
			window.AutoFitFramesY = size.y <= 0.0f ? 2 : 0;
		}

		if ( size.x <= 0.0f )
			window.AutoFitOnlyGrows = false;
		else
			window.SizeFull.x = ImTrunc( size.x );
		if ( size.y <= 0.0f )
			window.AutoFitOnlyGrows = false;
		else
			window.SizeFull.y = ImTrunc( size.y );
	}

	internal static void SetWindowCollapsed( ImGuiWindow window, bool collapsed, ImGuiCond cond = ImGuiCond.None )
	{
		if ( cond != ImGuiCond.None && (window.SetWindowCollapsedAllowFlags & cond) == 0 )
			return;
		window.SetWindowCollapsedAllowFlags &= ~(ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing);
		window.Collapsed = collapsed;
	}

	private static ImRect GetResizeBorderRect( ImGuiWindow window, int borderN, float perpPadding, float thickness )
	{
		var rect = window.Rect();
		if ( thickness == 0.0f )
			rect.Max -= Vector2.One;
		return borderN switch
		{
			0 => new ImRect( rect.Min.x - thickness, rect.Min.y + perpPadding, rect.Min.x + thickness, rect.Max.y - perpPadding ),
			1 => new ImRect( rect.Max.x - thickness, rect.Min.y + perpPadding, rect.Max.x + thickness, rect.Max.y - perpPadding ),
			2 => new ImRect( rect.Min.x + perpPadding, rect.Min.y - thickness, rect.Max.x - perpPadding, rect.Min.y + thickness ),
			_ => new ImRect( rect.Min.x + perpPadding, rect.Max.y - thickness, rect.Max.x - perpPadding, rect.Max.y + thickness ),
		};
	}

	private static void CalcResizePosSizeFromAnyCorner( ImGuiWindow window, Vector2 cornerTarget, Vector2 cornerNorm, out Vector2 outPos, out Vector2 outSize )
	{
		var posMin = ImLerp( cornerTarget, window.Pos, cornerNorm );
		var posMax = ImLerp( window.Pos + window.Size, cornerTarget, cornerNorm );
		var sizeExpected = posMax - posMin;
		var sizeConstrained = CalcWindowSizeAfterConstraint( window, sizeExpected );
		outPos = posMin;
		if ( cornerNorm.x == 0.0f ) outPos.x -= sizeConstrained.x - sizeExpected.x;
		if ( cornerNorm.y == 0.0f ) outPos.y -= sizeConstrained.y - sizeExpected.y;
		outSize = sizeConstrained;
	}

	private static bool UpdateWindowManualResize( ImGuiWindow window, Vector2 sizeAutoFit, ref int borderHovered, ref int borderHeld, int resizeGripCount, Color32[] resizeGripCol, ImRect visibilityRect )
	{
		var g = G;
		var flags = window.Flags;

		if ( (flags & ImGuiWindowFlags.NoResize) != 0 || (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 || window.AutoFitFramesX > 0 || window.AutoFitFramesY > 0 )
			return false;
		if ( !window.WasActive )
			return false;

		bool retAutoFit = false;
		int resizeBorderCount = g.IO.ConfigWindowsResizeFromEdges ? 4 : 0;
		float gripDrawSize = ImTrunc( MathF.Max( g.FontSize * 1.35f, window.WindowRounding + 1.0f + g.FontSize * 0.2f ) );
		float gripHoverInnerSize = ImTrunc( gripDrawSize * 0.75f );
		float gripHoverOuterSize = g.IO.ConfigWindowsResizeFromEdges ? WindowsHoverPadding : 0.0f;

		var posTarget = new Vector2( FLT_MAX, FLT_MAX );
		var sizeTarget = new Vector2( FLT_MAX, FLT_MAX );

		window.IDStack.Add( window.GetID( "#RESIZE" ) );
		for ( int resizeGripN = 0; resizeGripN < resizeGripCount; resizeGripN++ )
		{
			var def = ResizeGripDefs[resizeGripN];
			var corner = ImLerp( window.Pos, window.Pos + window.Size, def.CornerPosN );

			var resizeRect = new ImRect( corner - def.InnerDir * gripHoverOuterSize, corner + def.InnerDir * gripHoverInnerSize );
			if ( resizeRect.Min.x > resizeRect.Max.x ) (resizeRect.Min.x, resizeRect.Max.x) = (resizeRect.Max.x, resizeRect.Min.x);
			if ( resizeRect.Min.y > resizeRect.Max.y ) (resizeRect.Min.y, resizeRect.Max.y) = (resizeRect.Max.y, resizeRect.Min.y);
			int resizeGripId = window.GetID( resizeGripN );
			ItemAdd( resizeRect, resizeGripId, null, ImGuiItemFlags.NoNav );
			ButtonBehavior( resizeRect, resizeGripId, out bool hovered, out bool held, ImGuiButtonFlags.FlattenChildren | ImGuiButtonFlags.NoNavFocus );
			if ( hovered || held )
				g.MouseCursor = (resizeGripN & 1) != 0 ? ImGuiMouseCursor.ResizeNESW : ImGuiMouseCursor.ResizeNWSE;

			if ( held && g.IO.MouseClickedCount[0] == 2 && resizeGripN == 0 )
			{
				sizeTarget = CalcWindowSizeAfterConstraint( window, sizeAutoFit );
				retAutoFit = true;
				ClearActiveID();
			}
			else if ( held )
			{
				var clampMin = new Vector2( def.CornerPosN.x == 1.0f ? visibilityRect.Min.x : -FLT_MAX, def.CornerPosN.y == 1.0f ? visibilityRect.Min.y : -FLT_MAX );
				var clampMax = new Vector2( def.CornerPosN.x == 0.0f ? visibilityRect.Max.x : FLT_MAX, def.CornerPosN.y == 0.0f ? visibilityRect.Max.y : FLT_MAX );
				var cornerTarget = g.IO.MousePos - g.ActiveIdClickOffset + ImLerp( def.InnerDir * gripHoverOuterSize, def.InnerDir * -gripHoverInnerSize, def.CornerPosN );
				cornerTarget = new Vector2( Math.Clamp( cornerTarget.x, clampMin.x, clampMax.x ), Math.Clamp( cornerTarget.y, clampMin.y, clampMax.y ) );
				CalcResizePosSizeFromAnyCorner( window, cornerTarget, def.CornerPosN, out posTarget, out sizeTarget );
			}

			if ( resizeGripN == 0 || held || hovered )
				resizeGripCol[resizeGripN] = GetColorU32Internal( held ? ImGuiCol.ResizeGripActive : hovered ? ImGuiCol.ResizeGripHovered : ImGuiCol.ResizeGrip );
		}

		for ( int borderN = 0; borderN < resizeBorderCount; borderN++ )
		{
			var def = ResizeBorderDefs[borderN];
			int axis = borderN == 0 || borderN == 1 ? 0 : 1;

			var borderRect = GetResizeBorderRect( window, borderN, gripHoverInnerSize, WindowsHoverPadding );
			int borderId = window.GetID( borderN + 4 );
			ItemAdd( borderRect, borderId, null, ImGuiItemFlags.NoNav );
			ButtonBehavior( borderRect, borderId, out bool hovered, out bool held, ImGuiButtonFlags.FlattenChildren | ImGuiButtonFlags.NoNavFocus );
			if ( hovered && g.HoveredIdTimer <= WINDOWS_RESIZE_FROM_EDGES_FEEDBACK_TIMER )
				hovered = false;
			if ( hovered || held )
				g.MouseCursor = axis == 0 ? ImGuiMouseCursor.ResizeEW : ImGuiMouseCursor.ResizeNS;
			if ( held )
			{
				var clampMin = new Vector2( borderN == 1 ? visibilityRect.Min.x : -FLT_MAX, borderN == 3 ? visibilityRect.Min.y : -FLT_MAX );
				var clampMax = new Vector2( borderN == 0 ? visibilityRect.Max.x : FLT_MAX, borderN == 2 ? visibilityRect.Max.y : FLT_MAX );
				var borderTarget = WithAxis( window.Pos, axis, Axis( g.IO.MousePos, axis ) - Axis( g.ActiveIdClickOffset, axis ) + WindowsHoverPadding );
				borderTarget = new Vector2( Math.Clamp( borderTarget.x, clampMin.x, clampMax.x ), Math.Clamp( borderTarget.y, clampMin.y, clampMax.y ) );
				CalcResizePosSizeFromAnyCorner( window, borderTarget, ImMin( def.SegmentN1, def.SegmentN2 ), out posTarget, out sizeTarget );
			}
			if ( hovered ) borderHovered = borderN;
			if ( held ) borderHeld = borderN;
		}
		window.IDStack.RemoveAt( window.IDStack.Count - 1 );

		if ( sizeTarget.x != FLT_MAX )
		{
			window.SizeFull = sizeTarget;
			MarkIniSettingsDirty( window );
		}
		if ( posTarget.x != FLT_MAX )
		{
			window.Pos = ImTrunc( posTarget );
			MarkIniSettingsDirty( window );
		}

		window.Size = window.SizeFull;
		return retAutoFit;
	}

	internal static void MarkIniSettingsDirty( ImGuiWindow window )
	{
		if ( (window.Flags & (ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 )
			return;
		G.SettingsWindows[window.ID] = new ImGuiContext.WindowSettings { Pos = window.Pos, Size = window.SizeFull, Collapsed = window.Collapsed };
	}
	#endregion

	#region Popup positioning
	internal enum ImGuiPopupPositionPolicy
	{
		Default,
		ComboBox,
		Tooltip,
	}

	internal static ImRect GetPopupAllowedExtentRect()
	{
		var g = G;
		var r = new ImRect( Vector2.Zero, g.IO.DisplaySize );
		var padding = g.Style.DisplaySafeAreaPadding;
		r.Expand( new Vector2( r.Width > padding.x * 2 ? -padding.x : 0.0f, r.Height > padding.y * 2 ? -padding.y : 0.0f ) );
		return r;
	}

	internal static Vector2 FindBestWindowPosForPopup( ImGuiWindow window )
	{
		var g = G;
		var rOuter = GetPopupAllowedExtentRect();
		if ( (window.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
		{
			var parentWindow = window.ParentWindow;
			float horizontalOverlap = g.Style.ItemInnerSpacing.x;
			ImRect rAvoid;
			if ( parentWindow.DC.MenuBarAppending > 0 )
				rAvoid = new ImRect( -FLT_MAX, parentWindow.ClipRect.Min.y, FLT_MAX, parentWindow.ClipRect.Max.y );
			else
				rAvoid = new ImRect( parentWindow.Pos.x + horizontalOverlap, -FLT_MAX, parentWindow.Pos.x + parentWindow.Size.x - horizontalOverlap - parentWindow.ScrollbarSizes.x, FLT_MAX );
			return FindBestWindowPosForPopupEx( window.Pos, window.Size, ref window.AutoPosLastDirection, rOuter, rAvoid, ImGuiPopupPositionPolicy.Default );
		}
		if ( (window.Flags & ImGuiWindowFlags.Popup) != 0 )
			return FindBestWindowPosForPopupEx( window.Pos, window.Size, ref window.AutoPosLastDirection, rOuter, new ImRect( window.Pos, window.Pos ), ImGuiPopupPositionPolicy.Default );
		if ( (window.Flags & ImGuiWindowFlags.Tooltip) != 0 )
		{
			float sc = g.Style.MouseCursorScale;
			var refPos = g.IO.MousePos;
			var rAvoid = new ImRect( refPos.x - 16, refPos.y - 8, refPos.x + 24 * sc, refPos.y + 24 * sc );
			return FindBestWindowPosForPopupEx( refPos, window.Size, ref window.AutoPosLastDirection, rOuter, rAvoid, ImGuiPopupPositionPolicy.Tooltip );
		}
		return window.Pos;
	}

	internal static Vector2 FindBestWindowPosForPopupEx( Vector2 refPos, Vector2 size, ref ImGuiDir lastDir, ImRect rOuter, ImRect rAvoid, ImGuiPopupPositionPolicy policy )
	{
		var basePosClamped = ImClamp( refPos, rOuter.Min, rOuter.Max - size );

		if ( policy == ImGuiPopupPositionPolicy.ComboBox )
		{
			ImGuiDir[] dirPreferedOrder = { ImGuiDir.Down, ImGuiDir.Right, ImGuiDir.Left, ImGuiDir.Up };
			for ( int n = lastDir != ImGuiDir.None ? -1 : 0; n < 4; n++ )
			{
				var dir = n == -1 ? lastDir : dirPreferedOrder[n];
				if ( n != -1 && dir == lastDir )
					continue;
				Vector2 pos = default;
				if ( dir == ImGuiDir.Down ) pos = new Vector2( rAvoid.Min.x, rAvoid.Max.y );
				if ( dir == ImGuiDir.Right ) pos = new Vector2( rAvoid.Min.x, rAvoid.Min.y - size.y );
				if ( dir == ImGuiDir.Left ) pos = new Vector2( rAvoid.Max.x - size.x, rAvoid.Max.y );
				if ( dir == ImGuiDir.Up ) pos = new Vector2( rAvoid.Max.x - size.x, rAvoid.Min.y - size.y );
				if ( !rOuter.Contains( new ImRect( pos, pos + size ) ) )
					continue;
				lastDir = dir;
				return pos;
			}
		}

		if ( policy == ImGuiPopupPositionPolicy.Tooltip || policy == ImGuiPopupPositionPolicy.Default )
		{
			ImGuiDir[] dirPreferedOrder = { ImGuiDir.Right, ImGuiDir.Down, ImGuiDir.Up, ImGuiDir.Left };
			for ( int n = lastDir != ImGuiDir.None ? -1 : 0; n < 4; n++ )
			{
				var dir = n == -1 ? lastDir : dirPreferedOrder[n];
				if ( n != -1 && dir == lastDir )
					continue;

				float availW = (dir == ImGuiDir.Left ? rAvoid.Min.x : rOuter.Max.x) - (dir == ImGuiDir.Right ? rAvoid.Max.x : rOuter.Min.x);
				float availH = (dir == ImGuiDir.Up ? rAvoid.Min.y : rOuter.Max.y) - (dir == ImGuiDir.Down ? rAvoid.Max.y : rOuter.Min.y);
				if ( availW < size.x && (dir == ImGuiDir.Left || dir == ImGuiDir.Right) )
					continue;
				if ( availH < size.y && (dir == ImGuiDir.Up || dir == ImGuiDir.Down) )
					continue;

				var pos = new Vector2(
					dir == ImGuiDir.Left ? rAvoid.Min.x - size.x : (dir == ImGuiDir.Right ? rAvoid.Max.x : basePosClamped.x),
					dir == ImGuiDir.Up ? rAvoid.Min.y - size.y : (dir == ImGuiDir.Down ? rAvoid.Max.y : basePosClamped.y) );
				pos.x = MathF.Max( pos.x, rOuter.Min.x );
				pos.y = MathF.Max( pos.y, rOuter.Min.y );
				lastDir = dir;
				return pos;
			}
		}

		lastDir = ImGuiDir.None;
		if ( policy == ImGuiPopupPositionPolicy.Tooltip )
			return refPos + new Vector2( 2, 2 );

		var fallback = refPos;
		fallback.x = MathF.Max( MathF.Min( fallback.x + size.x, rOuter.Max.x ) - size.x, rOuter.Min.x );
		fallback.y = MathF.Max( MathF.Min( fallback.y + size.y, rOuter.Max.y ) - size.y, rOuter.Min.y );
		return fallback;
	}
	#endregion

	#region Begin / End
	/// <summary>
	/// Push a window to the stack and start appending to it. Always call End() even if this returns false
	/// (false means the window is collapsed or fully clipped, and you can skip submitting contents).
	/// </summary>
	public static bool Begin( string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		bool dummy = true;
		return BeginImpl( name, false, ref dummy, flags );
	}

	/// <summary>
	/// Begin a window with a close button. When the close button is clicked, <paramref name="open"/> is set to false.
	/// </summary>
	public static bool Begin( string name, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None )
	{
		return BeginImpl( name, true, ref open, flags );
	}

	internal static bool BeginImpl( string name, bool hasCloseButton, ref bool pOpen, ImGuiWindowFlags flags )
	{
		var g = G;
		var style = g.Style;

		if ( string.IsNullOrEmpty( name ) )
			throw new ArgumentException( "Window name cannot be empty", nameof( name ) );
		if ( !g.WithinFrameScope )
			throw new InvalidOperationException( "ImGui.Begin() called outside of a frame. Call ImGui functions from OnUpdate." );

		var window = FindWindowByName( name );
		bool windowJustCreated = window is null;
		if ( windowJustCreated )
			window = CreateNewWindow( name, flags );

		if ( (flags & ImGuiWindowFlags.NoInputs) == ImGuiWindowFlags.NoInputs )
			flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

		int currentFrame = g.FrameCount;
		bool firstBeginOfTheFrame = window.LastFrameActive != currentFrame;
		window.IsFallbackWindow = g.CurrentWindowStack.Count == 0 && g.WithinFrameScopeWithImplicitWindow;

		bool windowJustActivatedByUser = window.LastFrameActive < currentFrame - 1;
		if ( (flags & ImGuiWindowFlags.Popup) != 0 )
		{
			var popupRef0 = g.OpenPopupStack[g.BeginPopupStack.Count];
			windowJustActivatedByUser |= window.PopupId != popupRef0.PopupId;
			windowJustActivatedByUser |= window != popupRef0.Window;
		}

		if ( firstBeginOfTheFrame )
		{
			window.Flags = flags;
			window.ChildFlags = (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasChildFlags) != 0 ? g.NextWindowData.ChildFlags : ImGuiChildFlags.None;
			window.LastFrameActive = currentFrame;
			window.LastTimeActive = (float)g.Time;
			window.BeginOrderWithinParent = 0;
			window.BeginOrderWithinContext = g.WindowsActiveCount++;
		}
		else
		{
			flags = window.Flags;
		}

		var parentWindowInStack = g.CurrentWindowStack.Count > 0 ? g.CurrentWindowStack[^1].Window : null;
		var parentWindow = firstBeginOfTheFrame
			? ((flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Popup)) != 0 ? parentWindowInStack : null)
			: window.ParentWindow;

		window.Appearing = windowJustActivatedByUser;
		if ( window.Appearing )
			SetWindowConditionAllowFlags( window, ImGuiCond.Appearing, true );

		// Add to stack
		g.CurrentWindow = window;
		g.CurrentWindowStack.Add( new ImGuiWindowStackData
		{
			Window = window,
			ParentLastItemDataBackup = g.LastItemData,
			StackSizesColorStack = g.ColorStack.Count,
			StackSizesStyleVarStack = g.StyleVarStack.Count,
			StackSizesItemFlagsStack = g.ItemFlagsStack.Count,
			StackSizesGroupStack = g.GroupStack.Count,
			StackSizesBeginPopupStack = g.BeginPopupStack.Count,
			StackSizesDisabledStack = g.DisabledStackSize,
			BackupItemFlags = g.CurrentItemFlags,
		} );
		if ( (flags & ImGuiWindowFlags.ChildMenu) != 0 )
			g.BeginMenuDepth++;

		if ( (flags & ImGuiWindowFlags.Popup) != 0 )
		{
			var popupRef = g.OpenPopupStack[g.BeginPopupStack.Count];
			popupRef.Window = window;
			g.BeginPopupStack.Add( popupRef );
			window.PopupId = popupRef.PopupId;
		}

		// Process SetNextWindow***() calls
		bool windowPosSetByApi = false;
		bool windowSizeXSetByApi = false, windowSizeYSetByApi = false;
		var nwd = g.NextWindowData;
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasPos) != 0 )
		{
			windowPosSetByApi = (window.SetWindowPosAllowFlags & nwd.PosCond) != 0;
			if ( windowPosSetByApi && ImLengthSqr( nwd.PosPivotVal ) > 0.00001f )
			{
				window.SetWindowPosVal = nwd.PosVal;
				window.SetWindowPosPivot = nwd.PosPivotVal;
				window.SetWindowPosAllowFlags &= ~(ImGuiCond.Once | ImGuiCond.FirstUseEver | ImGuiCond.Appearing);
			}
			else
			{
				SetWindowPos( window, nwd.PosVal, nwd.PosCond );
			}
		}
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasSize) != 0 )
		{
			windowSizeXSetByApi = (window.SetWindowSizeAllowFlags & nwd.SizeCond) != 0 && nwd.SizeVal.x > 0.0f;
			windowSizeYSetByApi = (window.SetWindowSizeAllowFlags & nwd.SizeCond) != 0 && nwd.SizeVal.y > 0.0f;
			var sizeVal = nwd.SizeVal;
			// Child windows resized by the user keep their user size.
			if ( (window.ChildFlags & ImGuiChildFlags.ResizeX) != 0 && window.SizeFull.x > 0 && !windowJustCreated )
				sizeVal.x = window.SizeFull.x;
			if ( (window.ChildFlags & ImGuiChildFlags.ResizeY) != 0 && window.SizeFull.y > 0 && !windowJustCreated )
				sizeVal.y = window.SizeFull.y;
			SetWindowSize( window, sizeVal, nwd.SizeCond );
		}
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasScroll) != 0 )
		{
			if ( nwd.ScrollVal.x >= 0.0f )
			{
				window.ScrollTarget.x = nwd.ScrollVal.x;
				window.ScrollTargetCenterRatio.x = 0.0f;
			}
			if ( nwd.ScrollVal.y >= 0.0f )
			{
				window.ScrollTarget.y = nwd.ScrollVal.y;
				window.ScrollTargetCenterRatio.y = 0.0f;
			}
		}
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasContentSize) != 0 )
			window.ContentSizeExplicit = nwd.ContentSizeVal;
		else if ( firstBeginOfTheFrame )
			window.ContentSizeExplicit = Vector2.Zero;
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasCollapsed) != 0 )
			SetWindowCollapsed( window, nwd.CollapsedVal, nwd.CollapsedCond );
		if ( (nwd.Flags & ImGuiNextWindowDataFlags.HasFocus) != 0 )
			FocusWindow( window );
		if ( window.Appearing )
			SetWindowConditionAllowFlags( window, ImGuiCond.Appearing, false );

		if ( firstBeginOfTheFrame )
		{
			bool windowIsChildTooltip = (flags & ImGuiWindowFlags.ChildWindow) != 0 && (flags & ImGuiWindowFlags.Tooltip) != 0;

			UpdateWindowParentAndRootLinks( window, flags, parentWindow );
			window.ParentWindowInBeginStack = parentWindowInStack;

			window.Active = true;
			window.HasCloseButton = hasCloseButton;
			window.ClipRect = new ImRect( -FLT_MAX, -FLT_MAX, FLT_MAX, FLT_MAX );
			window.IDStack.Clear();
			window.IDStack.Add( window.ID );
			window.DrawList.ResetForNewFrame();
			window.Name = name;

			// UPDATE CONTENTS SIZE, UPDATE HIDDEN STATUS
			CalcWindowContentSizes( window, out window.ContentSize, out window.ContentSizeIdeal );
			if ( window.HiddenFramesCanSkipItems > 0 ) window.HiddenFramesCanSkipItems--;
			if ( window.HiddenFramesCannotSkipItems > 0 ) window.HiddenFramesCannotSkipItems--;
			if ( window.HiddenFramesForRenderOnly > 0 ) window.HiddenFramesForRenderOnly--;

			if ( windowJustCreated && (!windowSizeXSetByApi || !windowSizeYSetByApi) )
				window.HiddenFramesCannotSkipItems = 1;

			if ( windowJustActivatedByUser && (flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 )
			{
				window.HiddenFramesCannotSkipItems = 1;
				if ( (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 )
				{
					if ( !windowSizeXSetByApi ) { window.Size.x = window.SizeFull.x = 0f; }
					if ( !windowSizeYSetByApi ) { window.Size.y = window.SizeFull.y = 0f; }
					window.ContentSize = window.ContentSizeIdeal = Vector2.Zero;
				}
			}

			// UPDATE DECORATION SIZES
			window.WindowPadding = style.WindowPadding;
			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 && (window.ChildFlags & (ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.Borders | ImGuiChildFlags.FrameStyle)) == 0 && (flags & ImGuiWindowFlags.Popup) == 0 )
				window.WindowPadding = new Vector2( 0.0f, (flags & ImGuiWindowFlags.MenuBar) != 0 ? style.WindowPadding.y : 0.0f );

			window.DC.MenuBarOffset = new Vector2(
				MathF.Max( MathF.Max( window.WindowPadding.x, style.ItemSpacing.x ), nwd.MenuBarOffsetMinVal.x ),
				nwd.MenuBarOffsetMinVal.y );
			window.TitleBarHeight = (flags & ImGuiWindowFlags.NoTitleBar) != 0 ? 0.0f : g.FontSize + style.FramePadding.y * 2.0f;
			window.MenuBarHeight = (flags & ImGuiWindowFlags.MenuBar) != 0 ? window.DC.MenuBarOffset.y + g.FontSize + style.FramePadding.y * 2.0f : 0.0f;

			// Collapse window by double-clicking on title bar
			if ( (flags & ImGuiWindowFlags.NoTitleBar) == 0 && (flags & ImGuiWindowFlags.NoCollapse) == 0 )
			{
				var titleBarRect0 = window.TitleBarRect();
				if ( g.HoveredWindow == window && g.HoveredId == 0 && g.HoveredIdPreviousFrame == 0 && g.ActiveId == 0 && IsMouseHoveringRect( titleBarRect0.Min, titleBarRect0.Max ) && g.IO.MouseClicked[0] && g.IO.MouseClickedCount[0] == 2 )
					window.WantCollapseToggle = true;
				if ( window.WantCollapseToggle )
				{
					window.Collapsed = !window.Collapsed;
					MarkIniSettingsDirty( window );
				}
			}
			else
			{
				window.Collapsed = false;
			}
			window.WantCollapseToggle = false;

			// SIZE
			window.DecoOuterSizeX1 = 0.0f;
			window.DecoOuterSizeX2 = 0.0f;
			window.DecoOuterSizeY1 = window.TitleBarHeight + window.MenuBarHeight;
			window.DecoOuterSizeY2 = 0.0f;
			var scrollbarSizesFromLastFrame = window.ScrollbarSizes;
			window.ScrollbarSizes = Vector2.Zero;

			var sizeAutoFit = CalcWindowAutoFitSize( window, window.ContentSizeIdeal );
			bool useCurrentSizeForScrollbarX = windowJustCreated;
			bool useCurrentSizeForScrollbarY = windowJustCreated;
			{
				bool autoFitXAlways = !windowSizeXSetByApi && (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 && !window.Collapsed;
				bool autoFitYAlways = !windowSizeYSetByApi && (flags & ImGuiWindowFlags.AlwaysAutoResize) != 0 && !window.Collapsed;
				bool autoFitXCurrent = !windowSizeXSetByApi && window.AutoFitFramesX > 0;
				bool autoFitYCurrent = !windowSizeYSetByApi && window.AutoFitFramesY > 0;
				if ( autoFitXAlways || autoFitXCurrent )
				{
					window.SizeFull.x = window.AutoFitOnlyGrows && !autoFitXAlways ? MathF.Max( window.SizeFull.x, sizeAutoFit.x ) : sizeAutoFit.x;
					useCurrentSizeForScrollbarX = true;
				}
				if ( autoFitYAlways || autoFitYCurrent )
				{
					window.SizeFull.y = window.AutoFitOnlyGrows && !autoFitYAlways ? MathF.Max( window.SizeFull.y, sizeAutoFit.y ) : sizeAutoFit.y;
					useCurrentSizeForScrollbarY = true;
				}
			}

			window.SizeFull = CalcWindowSizeAfterConstraint( window, window.SizeFull );
			window.Size = window.Collapsed && (flags & ImGuiWindowFlags.ChildWindow) == 0 ? window.TitleBarRect().Size : window.SizeFull;

			// POSITION
			if ( windowJustActivatedByUser )
			{
				window.AutoPosLastDirection = ImGuiDir.None;
				if ( (flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiWindowFlags.Modal) == 0 && !windowPosSetByApi )
					window.Pos = g.BeginPopupStack[^1].OpenPopupPos;
			}

			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 )
			{
				parentWindow.DC.ChildWindows.Add( window );
				window.BeginOrderWithinParent = parentWindow.DC.ChildWindows.Count - 1;
				if ( (flags & ImGuiWindowFlags.Popup) == 0 && !windowPosSetByApi && !windowIsChildTooltip )
					window.Pos = parentWindow.DC.CursorPos;
			}

			bool windowPosWithPivot = window.SetWindowPosVal.x != FLT_MAX && window.HiddenFramesCannotSkipItems == 0;
			if ( windowPosWithPivot )
				SetWindowPos( window, window.SetWindowPosVal - window.Size * window.SetWindowPosPivot );
			else if ( (flags & ImGuiWindowFlags.ChildMenu) != 0 )
				window.Pos = FindBestWindowPosForPopup( window );
			else if ( (flags & ImGuiWindowFlags.Popup) != 0 && !windowPosSetByApi )
				window.Pos = FindBestWindowPosForPopup( window );
			else if ( (flags & ImGuiWindowFlags.Tooltip) != 0 && !windowPosSetByApi && !windowIsChildTooltip )
				window.Pos = FindBestWindowPosForPopup( window );

			// Clamp position so the window stays visible
			var viewportRect = new ImRect( Vector2.Zero, g.IO.DisplaySize );
			var visibilityPadding = ImMax( style.DisplayWindowPadding, style.DisplaySafeAreaPadding );
			var visibilityRect = new ImRect( viewportRect.Min + visibilityPadding, viewportRect.Max - visibilityPadding );
			if ( visibilityRect.Min.x >= visibilityRect.Max.x || visibilityRect.Min.y >= visibilityRect.Max.y )
				visibilityRect = viewportRect;
			if ( !windowPosSetByApi && (flags & ImGuiWindowFlags.ChildWindow) == 0 )
				if ( g.IO.DisplaySize.x > 0.0f && g.IO.DisplaySize.y > 0.0f )
					ClampWindowPos( window, visibilityRect );
			window.Pos = ImTrunc( window.Pos );

			// Lock window rounding/border for the frame
			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 )
				window.WindowRounding = (window.ChildFlags & ImGuiChildFlags.FrameStyle) != 0 ? style.FrameRounding : style.ChildRounding;
			else
				window.WindowRounding = (flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiWindowFlags.Modal) == 0 ? style.PopupRounding : style.WindowRounding;
			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 )
				window.WindowBorderSize = (window.ChildFlags & (ImGuiChildFlags.Borders | ImGuiChildFlags.FrameStyle)) != 0 ? style.ChildBorderSize : 0.0f;
			else
				window.WindowBorderSize = (flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 && (flags & ImGuiWindowFlags.Modal) == 0 ? style.PopupBorderSize : style.WindowBorderSize;

			// Apply window focus (new and reactivated windows are moved to front)
			bool wantFocus = false;
			if ( windowJustActivatedByUser && (flags & ImGuiWindowFlags.NoFocusOnAppearing) == 0 )
			{
				if ( (flags & ImGuiWindowFlags.Popup) != 0 )
					wantFocus = true;
				else if ( (flags & (ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.Tooltip)) == 0 )
					wantFocus = true;
			}

			// Handle manual resize
			int borderHovered = -1, borderHeld = -1;
			var resizeGripCol = new Color32[2];
			int resizeGripCount = (flags & ImGuiWindowFlags.ChildWindow) != 0 ? 0 : (g.IO.ConfigWindowsResizeFromEdges ? 2 : 1);
			float resizeGripDrawSize = ImTrunc( MathF.Max( g.FontSize * 1.10f, window.WindowRounding + 1.0f + g.FontSize * 0.2f ) );
			if ( !window.Collapsed )
				if ( UpdateWindowManualResize( window, sizeAutoFit, ref borderHovered, ref borderHeld, resizeGripCount, resizeGripCol, visibilityRect ) )
					useCurrentSizeForScrollbarX = useCurrentSizeForScrollbarY = true;
			window.ResizeBorderHovered = borderHovered;
			window.ResizeBorderHeld = borderHeld;

			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 && (window.ChildFlags & (ImGuiChildFlags.ResizeX | ImGuiChildFlags.ResizeY)) != 0 && !window.Collapsed )
				UpdateChildWindowResize( window );

			// SCROLLBAR VISIBILITY
			if ( !window.Collapsed )
			{
				var availSizeFromCurrentFrame = new Vector2( window.SizeFull.x, window.SizeFull.y - (window.DecoOuterSizeY1 + window.DecoOuterSizeY2) );
				var availSizeFromLastFrame = window.InnerRect.Size + scrollbarSizesFromLastFrame;
				var neededSizeFromLastFrame = windowJustCreated ? Vector2.Zero : window.ContentSize + window.WindowPadding * 2.0f;
				float sizeXForScrollbars = useCurrentSizeForScrollbarX ? availSizeFromCurrentFrame.x : availSizeFromLastFrame.x;
				float sizeYForScrollbars = useCurrentSizeForScrollbarY ? availSizeFromCurrentFrame.y : availSizeFromLastFrame.y;
				window.ScrollbarY = (flags & ImGuiWindowFlags.AlwaysVerticalScrollbar) != 0 || (neededSizeFromLastFrame.y > sizeYForScrollbars && (flags & ImGuiWindowFlags.NoScrollbar) == 0);
				window.ScrollbarX = (flags & ImGuiWindowFlags.AlwaysHorizontalScrollbar) != 0 || (neededSizeFromLastFrame.x > sizeXForScrollbars - (window.ScrollbarY ? style.ScrollbarSize : 0.0f) && (flags & ImGuiWindowFlags.NoScrollbar) == 0 && (flags & ImGuiWindowFlags.HorizontalScrollbar) != 0);
				if ( window.ScrollbarX && !window.ScrollbarY )
					window.ScrollbarY = neededSizeFromLastFrame.y > sizeYForScrollbars - style.ScrollbarSize && (flags & ImGuiWindowFlags.NoScrollbar) == 0;
				window.ScrollbarSizes = new Vector2( window.ScrollbarY ? style.ScrollbarSize : 0.0f, window.ScrollbarX ? style.ScrollbarSize : 0.0f );
				window.DecoOuterSizeX2 = window.ScrollbarSizes.x;
				window.DecoOuterSizeY2 = window.ScrollbarSizes.y;
			}

			// UPDATE RECTANGLES (1- THOSE NOT AFFECTED BY SCROLLING)
			var outerRect = window.Rect();
			var titleBarRect = window.TitleBarRect();
			var hostRect = (flags & ImGuiWindowFlags.ChildWindow) != 0 && (flags & ImGuiWindowFlags.Popup) == 0 && !windowIsChildTooltip ? parentWindow.ClipRect : viewportRect;
			window.OuterRectClipped = outerRect;
			window.OuterRectClipped.ClipWith( hostRect );

			window.InnerRect = new ImRect(
				window.Pos.x + window.DecoOuterSizeX1,
				window.Pos.y + window.DecoOuterSizeY1,
				window.Pos.x + window.Size.x - window.DecoOuterSizeX2,
				window.Pos.y + window.Size.y - window.DecoOuterSizeY2 );

			float topBorderSize = (flags & ImGuiWindowFlags.MenuBar) != 0 || (flags & ImGuiWindowFlags.NoTitleBar) == 0 ? style.FrameBorderSize : window.WindowBorderSize;
			window.InnerClipRect = new ImRect(
				ImTrunc( 0.5f + window.InnerRect.Min.x + window.WindowBorderSize * 0.5f ),
				ImTrunc( 0.5f + window.InnerRect.Min.y + topBorderSize * 0.5f ),
				ImTrunc( window.InnerRect.Max.x - window.WindowBorderSize * 0.5f ),
				ImTrunc( window.InnerRect.Max.y - window.WindowBorderSize * 0.5f ) );
			window.InnerClipRect.ClipWithFull( hostRect );

			// SCROLLING
			window.ScrollMax = new Vector2(
				MathF.Max( 0.0f, window.ContentSize.x + window.WindowPadding.x * 2.0f - window.InnerRect.Width ),
				MathF.Max( 0.0f, window.ContentSize.y + window.WindowPadding.y * 2.0f - window.InnerRect.Height ) );
			window.Scroll = CalcNextScrollFromScrollTargetAndClamp( window );
			window.ScrollTarget = new Vector2( FLT_MAX, FLT_MAX );
			window.DecoInnerSizeX1 = window.DecoInnerSizeY1 = 0.0f;

			// DRAWING
			window.DrawList.PushClipRect( hostRect.Min, hostRect.Max, false );
			{
				var windowToHighlight = g.NavWindow;
				bool titleBarIsHighlight = wantFocus || (windowToHighlight is not null && window.RootWindowForTitleBarHighlight == windowToHighlight.RootWindowForTitleBarHighlight);
				RenderWindowDecorations( window, titleBarRect, titleBarIsHighlight, resizeGripCount, resizeGripCol, resizeGripDrawSize );
			}

			// UPDATE RECTANGLES (2- THOSE AFFECTED BY SCROLLING)
			bool allowScrollbarX = (flags & ImGuiWindowFlags.NoScrollbar) == 0 && (flags & ImGuiWindowFlags.HorizontalScrollbar) != 0;
			bool allowScrollbarY = (flags & ImGuiWindowFlags.NoScrollbar) == 0;
			float workRectSizeX = window.ContentSizeExplicit.x != 0.0f ? window.ContentSizeExplicit.x : MathF.Max( allowScrollbarX ? window.ContentSize.x : 0.0f, window.Size.x - window.WindowPadding.x * 2.0f - (window.DecoOuterSizeX1 + window.DecoOuterSizeX2) );
			float workRectSizeY = window.ContentSizeExplicit.y != 0.0f ? window.ContentSizeExplicit.y : MathF.Max( allowScrollbarY ? window.ContentSize.y : 0.0f, window.Size.y - window.WindowPadding.y * 2.0f - (window.DecoOuterSizeY1 + window.DecoOuterSizeY2) );
			window.WorkRect.Min = new Vector2(
				ImTrunc( window.InnerRect.Min.x - window.Scroll.x + MathF.Max( window.WindowPadding.x, window.WindowBorderSize ) ),
				ImTrunc( window.InnerRect.Min.y - window.Scroll.y + MathF.Max( window.WindowPadding.y, window.WindowBorderSize ) ) );
			window.WorkRect.Max = window.WorkRect.Min + new Vector2( workRectSizeX, workRectSizeY );
			window.ParentWorkRect = window.WorkRect;

			window.ContentRegionRect.Min = new Vector2(
				window.Pos.x - window.Scroll.x + window.WindowPadding.x + window.DecoOuterSizeX1,
				window.Pos.y - window.Scroll.y + window.WindowPadding.y + window.DecoOuterSizeY1 );
			window.ContentRegionRect.Max = window.ContentRegionRect.Min + new Vector2(
				window.ContentSizeExplicit.x != 0.0f ? window.ContentSizeExplicit.x : (window.Size.x - window.WindowPadding.x * 2.0f - (window.DecoOuterSizeX1 + window.DecoOuterSizeX2)),
				window.ContentSizeExplicit.y != 0.0f ? window.ContentSizeExplicit.y : (window.Size.y - window.WindowPadding.y * 2.0f - (window.DecoOuterSizeY1 + window.DecoOuterSizeY2)) );

			// Setup drawing context
			var dc = window.DC;
			dc.Indent = window.DecoOuterSizeX1 + window.WindowPadding.x - window.Scroll.x;
			dc.GroupOffset = 0.0f;
			dc.ColumnsOffset = 0.0f;
			dc.CursorStartPos = new Vector2(
				window.Pos.x + window.WindowPadding.x - window.Scroll.x + window.DecoOuterSizeX1,
				window.Pos.y + window.WindowPadding.y - window.Scroll.y + window.DecoOuterSizeY1 );
			dc.CursorPos = dc.CursorStartPos;
			dc.CursorPosPrevLine = dc.CursorPos;
			dc.CursorMaxPos = dc.CursorStartPos;
			dc.IdealMaxPos = dc.CursorStartPos;
			dc.CurrLineSize = dc.PrevLineSize = Vector2.Zero;
			dc.CurrLineTextBaseOffset = dc.PrevLineTextBaseOffset = 0.0f;
			dc.IsSameLine = dc.IsSetPos = false;

			dc.MenuBarAppending = 0;
			dc.MenuColumns.Update( style.ItemSpacing.x, windowJustActivatedByUser );
			dc.TreeDepth = 0;
			dc.TreeHasStackDataDepthMask = 0;
			dc.ChildWindows.Clear();
			dc.StateStorage = window.StateStorage;
			dc.CurrentColumns = null;
			dc.CurrentTableIdx = -1;
			dc.LayoutType = ImGuiLayoutType.Vertical;
			dc.ParentLayoutType = parentWindow?.DC.LayoutType ?? ImGuiLayoutType.Vertical;

			if ( window.Size.x > 0.0f && (flags & ImGuiWindowFlags.Tooltip) == 0 && (flags & ImGuiWindowFlags.AlwaysAutoResize) == 0 )
				window.ItemWidthDefault = ImTrunc( window.Size.x * 0.65f );
			else
				window.ItemWidthDefault = ImTrunc( g.FontSize * 16.0f );
			dc.ItemWidth = window.ItemWidthDefault;
			dc.TextWrapPos = -1.0f;
			dc.ItemWidthStack.Clear();
			dc.TextWrapPosStack.Clear();

			if ( window.AutoFitFramesX > 0 ) window.AutoFitFramesX--;
			if ( window.AutoFitFramesY > 0 ) window.AutoFitFramesY--;

			if ( wantFocus )
				FocusWindow( window );

			// Title bar
			if ( (flags & ImGuiWindowFlags.NoTitleBar) == 0 )
				RenderWindowTitleBarContents( window, new ImRect( titleBarRect.Min.x + window.WindowBorderSize, titleBarRect.Min.y, titleBarRect.Max.x - window.WindowBorderSize, titleBarRect.Max.y ), name, hasCloseButton, ref pOpen );

			// Fill last item data with the title bar, so IsItemHovered()/IsItemActive() work right after Begin().
			g.LastItemData.ID = window.MoveId;
			g.LastItemData.InFlags = g.CurrentItemFlags;
			g.LastItemData.StatusFlags = IsMouseHoveringRect( titleBarRect.Min, titleBarRect.Max, false ) ? ImGuiItemStatusFlags.HoveredRect : ImGuiItemStatusFlags.None;
			g.LastItemData.Rect = titleBarRect;
		}

		// Clip contents to the inner area of the window
		PushClipRect( window.InnerClipRect.Min, window.InnerClipRect.Max, true );

		window.WriteAccessed = false;
		window.BeginCount++;
		g.NextWindowData.ClearFlags();

		if ( firstBeginOfTheFrame )
		{
			if ( (flags & ImGuiWindowFlags.ChildWindow) != 0 && (flags & ImGuiWindowFlags.ChildMenu) == 0 )
			{
				if ( window.OuterRectClipped.Min.x >= window.OuterRectClipped.Max.x || window.OuterRectClipped.Min.y >= window.OuterRectClipped.Max.y )
					window.HiddenFramesCanSkipItems = 1;
				if ( parentWindow is not null && (parentWindow.Collapsed || parentWindow.HiddenFramesCanSkipItems > 0) )
					window.HiddenFramesCanSkipItems = 1;
				if ( parentWindow is not null && parentWindow.HiddenFramesCannotSkipItems > 0 )
					window.HiddenFramesCannotSkipItems = 1;
			}

			if ( style.Alpha <= 0.0f )
				window.HiddenFramesCanSkipItems = 1;

			bool hiddenRegular = window.HiddenFramesCanSkipItems > 0 || window.HiddenFramesCannotSkipItems > 0;
			window.Hidden = hiddenRegular || window.HiddenFramesForRenderOnly > 0;

			bool skipItems = false;
			if ( window.Collapsed || !window.Active || hiddenRegular )
				if ( window.AutoFitFramesX <= 0 && window.AutoFitFramesY <= 0 && window.HiddenFramesCannotSkipItems <= 0 )
					skipItems = true;
			window.SkipItems = skipItems;
		}

		return !window.SkipItems;
	}

	private static void UpdateChildWindowResize( ImGuiWindow window )
	{
		var g = G;
		float handleSize = MathF.Max( 4.0f, g.Style.WindowBorderHoverPadding );
		for ( int axis = 0; axis < 2; axis++ )
		{
			if ( axis == 0 && (window.ChildFlags & ImGuiChildFlags.ResizeX) == 0 ) continue;
			if ( axis == 1 && (window.ChildFlags & ImGuiChildFlags.ResizeY) == 0 ) continue;
			var r = window.Rect();
			var bb = axis == 0
				? new ImRect( r.Max.x - handleSize, r.Min.y, r.Max.x + handleSize, r.Max.y )
				: new ImRect( r.Min.x, r.Max.y - handleSize, r.Max.x, r.Max.y + handleSize );
			int id = window.GetID( axis == 0 ? "#CHILDRESIZEX" : "#CHILDRESIZEY" );
			var backupWindow = g.CurrentWindow;
			g.CurrentWindow = window.ParentWindow;
			ItemAdd( bb, id, null, ImGuiItemFlags.NoNav );
			ButtonBehavior( bb, id, out bool hovered, out bool held, ImGuiButtonFlags.FlattenChildren | ImGuiButtonFlags.NoNavFocus );
			g.CurrentWindow = backupWindow;
			if ( hovered || held )
				g.MouseCursor = axis == 0 ? ImGuiMouseCursor.ResizeEW : ImGuiMouseCursor.ResizeNS;
			if ( held )
			{
				float target = Axis( g.IO.MousePos, axis ) - Axis( g.ActiveIdClickOffset, axis ) + handleSize - Axis( window.Pos, axis );
				target = MathF.Max( target, g.FontSize );
				window.SizeFull = WithAxis( window.SizeFull, axis, ImTrunc( target ) );
				window.Size = window.SizeFull;
			}
			if ( hovered || held )
				window.ParentWindow.DrawList.AddLine(
					axis == 0 ? new Vector2( r.Max.x, r.Min.y ) : new Vector2( r.Min.x, r.Max.y ), r.Max,
					GetColorU32Internal( held ? ImGuiCol.SeparatorActive : ImGuiCol.SeparatorHovered ), 2f );
		}
	}

	private static ImGuiCol GetWindowBgColorIdx( ImGuiWindow window )
	{
		if ( (window.Flags & (ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.Popup)) != 0 )
			return ImGuiCol.PopupBg;
		if ( (window.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
			return ImGuiCol.ChildBg;
		return ImGuiCol.WindowBg;
	}

	private static void RenderWindowDecorations( ImGuiWindow window, ImRect titleBarRect, bool titleBarIsHighlight, int resizeGripCount, Color32[] resizeGripCol, float resizeGripDrawSize )
	{
		var g = G;
		var style = g.Style;
		var flags = window.Flags;

		window.SkipItems = false;
		float windowRounding = window.WindowRounding;
		float windowBorderSize = window.WindowBorderSize;

		if ( window.Collapsed )
		{
			var titleBarCol = GetColorU32Internal( titleBarIsHighlight ? ImGuiCol.TitleBgActive : ImGuiCol.TitleBgCollapsed );
			window.DrawList.AddRectFilled( titleBarRect.Min, titleBarRect.Max, titleBarCol, windowRounding );
			if ( windowBorderSize > 0f )
				window.DrawList.AddRect( titleBarRect.Min, titleBarRect.Max, GetColorU32Internal( ImGuiCol.Border ), windowRounding, ImDrawFlags.None, windowBorderSize );
			return;
		}

		// Window background
		if ( (flags & ImGuiWindowFlags.NoBackground) == 0 )
		{
			var bgCol = GetColorU32Internal( GetWindowBgColorIdx( window ) );
			if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasBgAlpha) != 0 )
				bgCol = bgCol with { a = (byte)(ImSaturate( g.NextWindowData.BgAlphaVal ) * 255f) };
			window.DrawList.AddRectFilled( window.Pos + new Vector2( 0, window.TitleBarHeight ), window.Pos + window.Size, bgCol, windowRounding,
				(flags & ImGuiWindowFlags.NoTitleBar) != 0 ? ImDrawFlags.None : ImDrawFlags.RoundCornersBottom );
		}

		// Title bar
		if ( (flags & ImGuiWindowFlags.NoTitleBar) == 0 )
		{
			var titleBarCol = GetColorU32Internal( titleBarIsHighlight ? ImGuiCol.TitleBgActive : ImGuiCol.TitleBg );
			window.DrawList.AddRectFilled( titleBarRect.Min, titleBarRect.Max, titleBarCol, windowRounding, ImDrawFlags.RoundCornersTop );
		}

		// Menu bar
		if ( (flags & ImGuiWindowFlags.MenuBar) != 0 )
		{
			var menuBarRect = window.MenuBarRect();
			menuBarRect.ClipWith( window.Rect() );
			window.DrawList.AddRectFilled( menuBarRect.Min + new Vector2( windowBorderSize, 0 ), menuBarRect.Max - new Vector2( windowBorderSize, 0 ), GetColorU32Internal( ImGuiCol.MenuBarBg ),
				(flags & ImGuiWindowFlags.NoTitleBar) != 0 ? windowRounding : 0.0f, ImDrawFlags.RoundCornersTop );
			if ( style.FrameBorderSize > 0.0f && menuBarRect.Max.y < window.Pos.y + window.Size.y )
				window.DrawList.AddLine( menuBarRect.BL, menuBarRect.BR, GetColorU32Internal( ImGuiCol.Border ), style.FrameBorderSize );
		}

		// Scrollbars
		if ( window.ScrollbarX ) Scrollbar( ImGuiAxis.X );
		if ( window.ScrollbarY ) Scrollbar( ImGuiAxis.Y );

		// Resize grips
		if ( (flags & ImGuiWindowFlags.NoResize) == 0 )
		{
			for ( int resizeGripN = 0; resizeGripN < resizeGripCount; resizeGripN++ )
			{
				var col = resizeGripCol[resizeGripN];
				if ( col.a == 0 )
					continue;
				var grip = ResizeGripDefs[resizeGripN];
				var corner = ImLerp( window.Pos, window.Pos + window.Size, grip.CornerPosN );
				var dl = window.DrawList;
				dl.PathLineTo( corner + ImMul( grip.InnerDir, (resizeGripN & 1) != 0 ? new Vector2( windowBorderSize, resizeGripDrawSize ) : new Vector2( resizeGripDrawSize, windowBorderSize ) ) );
				dl.PathLineTo( corner + ImMul( grip.InnerDir, (resizeGripN & 1) != 0 ? new Vector2( resizeGripDrawSize, windowBorderSize ) : new Vector2( windowBorderSize, resizeGripDrawSize ) ) );
				dl.PathArcToFast( new Vector2( corner.x + grip.InnerDir.x * (windowRounding + windowBorderSize), corner.y + grip.InnerDir.y * (windowRounding + windowBorderSize) ), windowRounding, grip.AngleMin12, grip.AngleMax12 );
				dl.PathFillConvex( col );
			}
		}

		RenderWindowOuterBorders( window );
	}

	private static void RenderWindowOuterBorders( ImGuiWindow window )
	{
		var g = G;
		float rounding = window.WindowRounding;
		float borderSize = window.WindowBorderSize;
		if ( borderSize > 0.0f && (window.Flags & ImGuiWindowFlags.NoBackground) == 0 )
			window.DrawList.AddRect( window.Pos, window.Pos + window.Size, GetColorU32Internal( ImGuiCol.Border ), rounding, ImDrawFlags.None, borderSize );

		int borderHeld = window.ResizeBorderHeld;
		int border = borderHeld != -1 ? borderHeld : window.ResizeBorderHovered;
		if ( border != -1 )
		{
			var r = window.Rect();
			Vector2 a, b;
			switch ( border )
			{
				case 0: a = r.TL; b = r.BL; break;
				case 1: a = r.TR; b = r.BR; break;
				case 2: a = r.TL; b = r.TR; break;
				default: a = r.BL; b = r.BR; break;
			}
			window.DrawList.AddLine( a, b, GetColorU32Internal( borderHeld != -1 ? ImGuiCol.SeparatorActive : ImGuiCol.SeparatorHovered ), MathF.Max( 2.0f, borderSize ) );
		}

		if ( g.Style.FrameBorderSize > 0 && (window.Flags & ImGuiWindowFlags.NoTitleBar) == 0 )
		{
			float y = window.Pos.y + window.TitleBarHeight - 1;
			window.DrawList.AddLine( new Vector2( window.Pos.x + borderSize, y ), new Vector2( window.Pos.x + window.Size.x - borderSize, y ), GetColorU32Internal( ImGuiCol.Border ), g.Style.FrameBorderSize );
		}
	}

	private static void RenderWindowTitleBarContents( ImGuiWindow window, ImRect titleBarRect, string name, bool hasCloseButton, ref bool pOpen )
	{
		var g = G;
		var style = g.Style;
		var flags = window.Flags;

		bool hasCollapseButton = (flags & ImGuiWindowFlags.NoCollapse) == 0 && style.WindowMenuButtonPosition != ImGuiDir.None;

		var itemFlagsBackup = g.CurrentItemFlags;
		g.CurrentItemFlags |= ImGuiItemFlags.NoNavDefaultFocus;

		float padL = style.FramePadding.x;
		float padR = style.FramePadding.x;
		float buttonSz = g.FontSize;
		Vector2 closeButtonPos = default, collapseButtonPos = default;
		if ( hasCloseButton )
		{
			closeButtonPos = new Vector2( titleBarRect.Max.x - padR - buttonSz, titleBarRect.Min.y + style.FramePadding.y );
			padR += buttonSz + style.ItemInnerSpacing.x;
		}
		if ( hasCollapseButton && style.WindowMenuButtonPosition == ImGuiDir.Right )
		{
			collapseButtonPos = new Vector2( titleBarRect.Max.x - padR - buttonSz, titleBarRect.Min.y + style.FramePadding.y );
			padR += buttonSz + style.ItemInnerSpacing.x;
		}
		if ( hasCollapseButton && style.WindowMenuButtonPosition == ImGuiDir.Left )
		{
			collapseButtonPos = new Vector2( titleBarRect.Min.x + padL, titleBarRect.Min.y + style.FramePadding.y );
			padL += buttonSz + style.ItemInnerSpacing.x;
		}

		if ( hasCollapseButton )
			if ( CollapseButton( window.GetID( "#COLLAPSE" ), collapseButtonPos ) )
				window.WantCollapseToggle = true;

		if ( hasCloseButton )
			if ( CloseButton( window.GetID( "#CLOSE" ), closeButtonPos ) )
				pOpen = false;

		g.CurrentItemFlags = itemFlagsBackup;

		float markerSizeX = (flags & ImGuiWindowFlags.UnsavedDocument) != 0 ? buttonSz * 0.80f : 0.0f;
		var textSize = CalcTextSize( name, true ) + new Vector2( markerSizeX, 0.0f );

		if ( padL > style.FramePadding.x ) padL += style.ItemInnerSpacing.x;
		if ( padR > style.FramePadding.x ) padR += style.ItemInnerSpacing.x;
		if ( style.WindowTitleAlign.x > 0.0f && style.WindowTitleAlign.x < 1.0f )
		{
			float centerness = ImSaturate( 1.0f - MathF.Abs( style.WindowTitleAlign.x - 0.5f ) * 2.0f );
			float padExtend = MathF.Min( MathF.Max( padL, padR ), titleBarRect.Width - padL - padR - textSize.x );
			padL = MathF.Max( padL, padExtend * centerness );
			padR = MathF.Max( padR, padExtend * centerness );
		}

		var layoutR = new ImRect( titleBarRect.Min.x + padL, titleBarRect.Min.y, titleBarRect.Max.x - padR, titleBarRect.Max.y );
		var clipR = new ImRect( layoutR.Min.x, layoutR.Min.y, MathF.Min( layoutR.Max.x + style.ItemInnerSpacing.x, titleBarRect.Max.x ), layoutR.Max.y );
		if ( (flags & ImGuiWindowFlags.UnsavedDocument) != 0 )
		{
			var markerPos = new Vector2( MathF.Max( layoutR.Min.x, layoutR.Min.x + (layoutR.Width - textSize.x) * style.WindowTitleAlign.x ) + textSize.x - markerSizeX * 0.5f, layoutR.Center.y );
			RenderBullet( window.DrawList, markerPos, GetColorU32Internal( ImGuiCol.Text ) );
		}
		RenderTextClippedEx( window.DrawList, layoutR.Min, layoutR.Max, LabelText( name ), textSize - new Vector2( markerSizeX, 0f ), style.WindowTitleAlign, clipR );
	}

	internal static bool CollapseButton( int id, Vector2 pos )
	{
		var g = G;
		var window = g.CurrentWindow;
		var bb = new ImRect( pos, pos + new Vector2( g.FontSize, g.FontSize ) );
		bool isClipped = !ItemAdd( bb, id );
		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, ImGuiButtonFlags.None );
		if ( isClipped )
			return pressed;

		var bgCol = GetColorU32Internal( held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
		var textCol = GetColorU32Internal( ImGuiCol.Text );
		if ( hovered || held )
			window.DrawList.AddCircleFilled( bb.Center, g.FontSize * 0.5f + 1f, bgCol );
		RenderArrow( window.DrawList, bb.Min, textCol, window.Collapsed ? ImGuiDir.Right : ImGuiDir.Down, 1.0f );

		if ( IsItemActive() && IsMouseDragging( ImGuiMouseButton.Left ) )
			StartMouseMovingWindow( window );
		return pressed;
	}

	internal static bool CloseButton( int id, Vector2 pos )
	{
		var g = G;
		var window = g.CurrentWindow;
		var bb = new ImRect( pos, pos + new Vector2( g.FontSize, g.FontSize ) );
		var bbInteract = bb;
		float areaToVisibleRatio = window.OuterRectClipped.Area / MathF.Max( 1f, bb.Area );
		if ( areaToVisibleRatio < 1.5f )
			bbInteract.Expand( ImTrunc( bbInteract.Size * -0.25f ) );

		bool isClipped = !ItemAdd( bbInteract, id );
		bool pressed = ButtonBehavior( bbInteract, id, out bool hovered, out bool held );
		if ( isClipped )
			return pressed;

		var bgCol = GetColorU32Internal( held ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered );
		if ( hovered )
			window.DrawList.AddCircleFilled( bb.Center, g.FontSize * 0.5f + 1f, bgCol );
		float crossExtent = g.FontSize * 0.5f * 0.7071f - 1.0f;
		var crossCol = GetColorU32Internal( ImGuiCol.Text );
		var crossCenter = bb.Center - new Vector2( 0.5f, 0.5f );
		window.DrawList.AddLine( crossCenter + new Vector2( +crossExtent, +crossExtent ), crossCenter + new Vector2( -crossExtent, -crossExtent ), crossCol, 1.0f );
		window.DrawList.AddLine( crossCenter + new Vector2( +crossExtent, -crossExtent ), crossCenter + new Vector2( -crossExtent, +crossExtent ), crossCol, 1.0f );
		return pressed;
	}

	public static void End()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window is null )
			throw new InvalidOperationException( "ImGui.End() called without a matching Begin()." );
		if ( g.CurrentWindowStack.Count <= 1 && g.WithinFrameScopeWithImplicitWindow )
			throw new InvalidOperationException( "ImGui.End() called too many times!" );

		if ( window.DC.CurrentColumns is not null )
			EndColumns();
		PopClipRect();

		var stackData = g.CurrentWindowStack[^1];
		ErrorCheckEndWindowRecover( stackData );

		g.LastItemData = stackData.ParentLastItemDataBackup;
		if ( (window.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
			g.BeginMenuDepth--;
		if ( (window.Flags & ImGuiWindowFlags.Popup) != 0 )
			g.BeginPopupStack.RemoveAt( g.BeginPopupStack.Count - 1 );
		g.CurrentWindowStack.RemoveAt( g.CurrentWindowStack.Count - 1 );
		g.CurrentWindow = g.CurrentWindowStack.Count == 0 ? null : g.CurrentWindowStack[^1].Window;
	}

	/// <summary>
	/// Gracefully recover from unbalanced Push/Pop calls inside a window instead of corrupting state.
	/// </summary>
	private static void ErrorCheckEndWindowRecover( ImGuiWindowStackData stackData )
	{
		var g = G;
		var window = g.CurrentWindow;
		while ( g.GroupStack.Count > stackData.StackSizesGroupStack )
		{
			Log.Warning( $"ImGui: missing EndGroup() in '{window.Name}'" );
			EndGroup();
		}
		while ( g.ColorStack.Count > stackData.StackSizesColorStack )
		{
			Log.Warning( $"ImGui: missing PopStyleColor() in '{window.Name}'" );
			PopStyleColor();
		}
		while ( g.StyleVarStack.Count > stackData.StackSizesStyleVarStack )
		{
			Log.Warning( $"ImGui: missing PopStyleVar() in '{window.Name}'" );
			PopStyleVar();
		}
		while ( g.DisabledStackSize > stackData.StackSizesDisabledStack )
		{
			Log.Warning( $"ImGui: missing EndDisabled() in '{window.Name}'" );
			EndDisabled();
		}
		while ( g.ItemFlagsStack.Count > stackData.StackSizesItemFlagsStack )
		{
			Log.Warning( $"ImGui: missing PopItemFlag() in '{window.Name}'" );
			PopItemFlag();
		}
		while ( window.DC.TreeDepth > 0 )
		{
			Log.Warning( $"ImGui: missing TreePop() in '{window.Name}'" );
			TreePop();
		}
		while ( window.IDStack.Count > 1 )
		{
			Log.Warning( $"ImGui: missing PopID() in '{window.Name}'" );
			window.IDStack.RemoveAt( window.IDStack.Count - 1 );
		}
	}
	#endregion

	#region Child windows
	public static bool BeginChild( string strId, Vector2 size = default, ImGuiChildFlags childFlags = ImGuiChildFlags.None, ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None )
	{
		var window = GetCurrentWindow();
		return BeginChildEx( strId, window.GetID( strId ), size, childFlags, windowFlags );
	}

	public static bool BeginChild( int id, Vector2 size = default, ImGuiChildFlags childFlags = ImGuiChildFlags.None, ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None )
		=> BeginChildEx( null, id, size, childFlags, windowFlags );

	/// <summary>Legacy overload using a bool for borders.</summary>
	public static bool BeginChild( string strId, Vector2 size, bool border, ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None )
		=> BeginChild( strId, size, border ? ImGuiChildFlags.Borders : ImGuiChildFlags.None, windowFlags );

	internal static bool BeginChildEx( string name, int id, Vector2 sizeArg, ImGuiChildFlags childFlags, ImGuiWindowFlags windowFlags )
	{
		var g = G;
		var parentWindow = g.CurrentWindow;

		windowFlags |= ImGuiWindowFlags.ChildWindow | ImGuiWindowFlags.NoTitleBar;
		windowFlags |= parentWindow.Flags & ImGuiWindowFlags.NoMove;

		if ( (childFlags & ImGuiChildFlags.AlwaysAutoResize) != 0 && (childFlags & (ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY)) != 0 )
			windowFlags |= ImGuiWindowFlags.AlwaysAutoResize;
		if ( (childFlags & (ImGuiChildFlags.ResizeX | ImGuiChildFlags.ResizeY)) != 0 )
			childFlags |= ImGuiChildFlags.Borders;

		if ( (childFlags & ImGuiChildFlags.FrameStyle) != 0 )
		{
			PushStyleColor( ImGuiCol.ChildBg, g.Style.Colors[(int)ImGuiCol.FrameBg] );
			PushStyleVar( ImGuiStyleVar.ChildRounding, g.Style.FrameRounding );
			PushStyleVar( ImGuiStyleVar.ChildBorderSize, g.Style.FrameBorderSize );
			PushStyleVar( ImGuiStyleVar.WindowPadding, g.Style.FramePadding );
			childFlags |= ImGuiChildFlags.Borders | ImGuiChildFlags.AlwaysUseWindowPadding;
			windowFlags |= ImGuiWindowFlags.NoMove;
		}

		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasChildFlags;
		g.NextWindowData.ChildFlags = childFlags;

		var sizeAvail = GetContentRegionAvail();
		var sizeDefault = new Vector2( (childFlags & ImGuiChildFlags.AutoResizeX) != 0 ? 0.0f : sizeAvail.x, (childFlags & ImGuiChildFlags.AutoResizeY) != 0 ? 0.0f : sizeAvail.y );
		var size = CalcItemSize( sizeArg, sizeDefault.x, sizeDefault.y );
		if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSize) == 0 )
			SetNextWindowSize( size );

		string tempWindowName = name is not null
			? $"{parentWindow.Name}/{name}_{id:X8}"
			: $"{parentWindow.Name}/{id:X8}";

		float backupBorderSize = g.Style.ChildBorderSize;
		if ( (childFlags & ImGuiChildFlags.Borders) == 0 )
			g.Style.ChildBorderSize = 0.0f;

		bool ret = Begin( tempWindowName, windowFlags );

		g.Style.ChildBorderSize = backupBorderSize;
		if ( (childFlags & ImGuiChildFlags.FrameStyle) != 0 )
		{
			// The child window is now current: pop style changes made for its creation without warnings.
			PopStyleVar( 3 );
			PopStyleColor();
		}

		var childWindow = g.CurrentWindow;
		childWindow.ChildId = id;

		if ( childWindow.BeginCount == 1 )
			parentWindow.DC.CursorPos = childWindow.Pos;

		return ret;
	}

	public static void EndChild()
	{
		var g = G;
		var childWindow = g.CurrentWindow;
		if ( (childWindow.Flags & ImGuiWindowFlags.ChildWindow) == 0 )
			throw new InvalidOperationException( "EndChild() called without a matching BeginChild()." );

		var childSize = childWindow.Size;
		End();
		if ( childWindow.BeginCount == 1 )
		{
			var parentWindow = g.CurrentWindow;
			var bb = new ImRect( parentWindow.DC.CursorPos, parentWindow.DC.CursorPos + childSize );
			ItemSize( childSize );
			ItemAdd( bb, childWindow.ChildId, null, ImGuiItemFlags.NoNav );
			if ( g.HoveredWindow == childWindow )
				g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredWindow;
		}
	}
	#endregion

	#region Scrollbars
	internal static ImRect GetWindowScrollbarRect( ImGuiWindow window, ImGuiAxis axis )
	{
		var outerRect = window.Rect();
		var innerRect = window.InnerRect;
		float borderSize = window.WindowBorderSize;
		float scrollbarSize = axis == ImGuiAxis.X ? window.ScrollbarSizes.y : window.ScrollbarSizes.x;
		if ( axis == ImGuiAxis.X )
			return new ImRect( innerRect.Min.x, MathF.Max( outerRect.Min.y, outerRect.Max.y - borderSize - scrollbarSize ), innerRect.Max.x - borderSize, outerRect.Max.y - borderSize );
		return new ImRect( MathF.Max( outerRect.Min.x, outerRect.Max.x - borderSize - scrollbarSize ), innerRect.Min.y, outerRect.Max.x - borderSize, innerRect.Max.y - borderSize );
	}

	internal static void Scrollbar( ImGuiAxis axis )
	{
		var g = G;
		var window = g.CurrentWindow;
		int id = window.GetID( axis == ImGuiAxis.X ? "#SCROLLX" : "#SCROLLY" );

		var bb = GetWindowScrollbarRect( window, axis );
		var roundingCorners = ImDrawFlags.RoundCornersNone;
		if ( axis == ImGuiAxis.X )
		{
			roundingCorners |= ImDrawFlags.RoundCornersBottomLeft;
			if ( !window.ScrollbarY )
				roundingCorners |= ImDrawFlags.RoundCornersBottomRight;
		}
		else
		{
			if ( (window.Flags & ImGuiWindowFlags.NoTitleBar) != 0 && (window.Flags & ImGuiWindowFlags.MenuBar) == 0 )
				roundingCorners |= ImDrawFlags.RoundCornersTopRight;
			if ( !window.ScrollbarX )
				roundingCorners |= ImDrawFlags.RoundCornersBottomRight;
		}

		int a = (int)axis;
		float sizeVisible = Axis( window.InnerRect.Max, a ) - Axis( window.InnerRect.Min, a );
		float sizeContents = Axis( window.ContentSize, a ) + Axis( window.WindowPadding, a ) * 2.0f;
		float scroll = Axis( window.Scroll, a );
		ScrollbarEx( bb, id, axis, ref scroll, sizeVisible, sizeContents, roundingCorners );
		window.Scroll = WithAxis( window.Scroll, a, scroll );
	}

	internal static bool ScrollbarEx( ImRect bbFrame, int id, ImGuiAxis axis, ref float pScrollV, float sizeVisibleV, float sizeContentsV, ImDrawFlags drawRoundingFlags )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return false;

		float bbFrameWidth = bbFrame.Width;
		float bbFrameHeight = bbFrame.Height;
		if ( bbFrameWidth <= 0.0f || bbFrameHeight <= 0.0f )
			return false;

		float alpha = 1.0f;
		if ( axis == ImGuiAxis.Y && bbFrameHeight < g.FontSize + g.Style.FramePadding.y * 2.0f )
			alpha = ImSaturate( (bbFrameHeight - g.FontSize) / (g.Style.FramePadding.y * 2.0f) );
		if ( alpha <= 0.0f )
			return false;

		var style = g.Style;
		bool allowInteraction = alpha >= 1.0f;

		var bb = bbFrame;
		bb.Expand( new Vector2( -Math.Clamp( ImTrunc( (bbFrameWidth - 2.0f) * 0.5f ), 0.0f, 3.0f ), -Math.Clamp( ImTrunc( (bbFrameHeight - 2.0f) * 0.5f ), 0.0f, 3.0f ) ) );

		int a = (int)axis;
		float scrollbarSizeV = axis == ImGuiAxis.X ? bb.Width : bb.Height;
		float winSizeV = MathF.Max( MathF.Max( sizeContentsV, sizeVisibleV ), 1.0f );
		float grabHPixels = Math.Clamp( scrollbarSizeV * (sizeVisibleV / winSizeV), MathF.Min( style.GrabMinSize, scrollbarSizeV ), scrollbarSizeV );
		float grabHNorm = grabHPixels / scrollbarSizeV;

		ItemAdd( bbFrame, id, null, ImGuiItemFlags.NoNav );
		ButtonBehavior( bb, id, out bool hovered, out bool held, ImGuiButtonFlags.NoNavFocus );

		float scrollMax = MathF.Max( 1.0f, sizeContentsV - sizeVisibleV );
		float scrollRatio = ImSaturate( pScrollV / scrollMax );
		float grabVNorm = scrollRatio * (scrollbarSizeV - grabHPixels) / scrollbarSizeV;
		if ( held && allowInteraction && grabHNorm < 1.0f )
		{
			float scrollbarPosV = Axis( bb.Min, a );
			float mousePosV = Axis( g.IO.MousePos, a );
			float clickedVNorm = ImSaturate( (mousePosV - scrollbarPosV) / scrollbarSizeV );
			int heldDir = clickedVNorm < grabVNorm ? -1 : (clickedVNorm > grabVNorm + grabHNorm ? +1 : 0);
			if ( g.ActiveIdIsJustActivated )
				g.ScrollbarClickDeltaToGrabCenterF = (heldDir == 0 && !g.IO.KeyShift) ? clickedVNorm - grabVNorm - grabHNorm * 0.5f : 0.0f;

			float scrollVNorm = ImSaturate( (clickedVNorm - g.ScrollbarClickDeltaToGrabCenterF - grabHNorm * 0.5f) / (1.0f - grabHNorm) );
			pScrollV = MathF.Round( scrollVNorm * scrollMax );

			scrollRatio = ImSaturate( pScrollV / scrollMax );
			grabVNorm = scrollRatio * (scrollbarSizeV - grabHPixels) / scrollbarSizeV;
		}

		var bgCol = GetColorU32Internal( ImGuiCol.ScrollbarBg );
		var grabCol = GetColorU32Internal( held ? ImGuiCol.ScrollbarGrabActive : hovered ? ImGuiCol.ScrollbarGrabHovered : ImGuiCol.ScrollbarGrab, alpha );
		window.DrawList.AddRectFilled( bbFrame.Min, bbFrame.Max, bgCol, window.WindowRounding, drawRoundingFlags );
		ImRect grabRect;
		if ( axis == ImGuiAxis.X )
			grabRect = new ImRect( ImLerp( bb.Min.x, bb.Max.x, grabVNorm ), bb.Min.y, ImLerp( bb.Min.x, bb.Max.x, grabVNorm ) + grabHPixels, bb.Max.y );
		else
			grabRect = new ImRect( bb.Min.x, ImLerp( bb.Min.y, bb.Max.y, grabVNorm ), bb.Max.x, ImLerp( bb.Min.y, bb.Max.y, grabVNorm ) + grabHPixels );
		window.DrawList.AddRectFilled( grabRect.Min, grabRect.Max, grabCol, style.ScrollbarRounding );

		return held;
	}
	#endregion

	#region Scrolling
	private static float CalcScrollEdgeSnap( float target, float snapMin, float snapMax, float snapThreshold, float centerRatio )
	{
		if ( target <= snapMin + snapThreshold )
			return ImLerp( snapMin, target, centerRatio );
		if ( target >= snapMax - snapThreshold )
			return ImLerp( target, snapMax, centerRatio );
		return target;
	}

	private static Vector2 CalcNextScrollFromScrollTargetAndClamp( ImGuiWindow window )
	{
		var scroll = window.Scroll;
		var decorationSize = new Vector2( window.DecoOuterSizeX1 + window.DecoInnerSizeX1 + window.DecoOuterSizeX2, window.DecoOuterSizeY1 + window.DecoInnerSizeY1 + window.DecoOuterSizeY2 );
		for ( int axis = 0; axis < 2; axis++ )
		{
			float target = Axis( window.ScrollTarget, axis );
			if ( target < FLT_MAX )
			{
				float centerRatio = Axis( window.ScrollTargetCenterRatio, axis );
				float scrollTarget = target;
				if ( Axis( window.ScrollTargetEdgeSnapDist, axis ) > 0.0f )
				{
					float snapMax = Axis( window.ScrollMax, axis ) + Axis( window.SizeFull, axis ) - Axis( decorationSize, axis );
					scrollTarget = CalcScrollEdgeSnap( scrollTarget, 0.0f, snapMax, Axis( window.ScrollTargetEdgeSnapDist, axis ), centerRatio );
				}
				scroll = WithAxis( scroll, axis, scrollTarget - centerRatio * (Axis( window.SizeFull, axis ) - Axis( decorationSize, axis )) );
			}
			scroll = WithAxis( scroll, axis, MathF.Round( MathF.Max( Axis( scroll, axis ), 0.0f ) ) );
			if ( !window.Collapsed && !window.SkipItems )
				scroll = WithAxis( scroll, axis, MathF.Min( Axis( scroll, axis ), Axis( window.ScrollMax, axis ) ) );
		}
		return scroll;
	}

	public static float GetScrollX() => G.CurrentWindow.Scroll.x;
	public static float GetScrollY() => G.CurrentWindow.Scroll.y;
	public static float GetScrollMaxX() => G.CurrentWindow.ScrollMax.x;
	public static float GetScrollMaxY() => G.CurrentWindow.ScrollMax.y;

	internal static void SetScrollX( ImGuiWindow window, float scrollX )
	{
		window.ScrollTarget.x = scrollX;
		window.ScrollTargetCenterRatio.x = 0.0f;
		window.ScrollTargetEdgeSnapDist.x = 0.0f;
	}

	internal static void SetScrollY( ImGuiWindow window, float scrollY )
	{
		window.ScrollTarget.y = scrollY;
		window.ScrollTargetCenterRatio.y = 0.0f;
		window.ScrollTargetEdgeSnapDist.y = 0.0f;
	}

	public static void SetScrollX( float scrollX ) => SetScrollX( G.CurrentWindow, scrollX );
	public static void SetScrollY( float scrollY ) => SetScrollY( G.CurrentWindow, scrollY );

	internal static void SetScrollFromPosX( ImGuiWindow window, float localX, float centerXRatio )
	{
		window.ScrollTarget.x = ImTrunc( localX - window.DecoOuterSizeX1 - window.DecoInnerSizeX1 + window.Scroll.x );
		window.ScrollTargetCenterRatio.x = centerXRatio;
		window.ScrollTargetEdgeSnapDist.x = 0.0f;
	}

	internal static void SetScrollFromPosY( ImGuiWindow window, float localY, float centerYRatio )
	{
		window.ScrollTarget.y = ImTrunc( localY - window.DecoOuterSizeY1 - window.DecoInnerSizeY1 + window.Scroll.y );
		window.ScrollTargetCenterRatio.y = centerYRatio;
		window.ScrollTargetEdgeSnapDist.y = 0.0f;
	}

	public static void SetScrollFromPosX( float localX, float centerXRatio = 0.5f ) => SetScrollFromPosX( G.CurrentWindow, localX, centerXRatio );
	public static void SetScrollFromPosY( float localY, float centerYRatio = 0.5f ) => SetScrollFromPosY( G.CurrentWindow, localY, centerYRatio );

	public static void SetScrollHereX( float centerXRatio = 0.5f )
	{
		var g = G;
		var window = g.CurrentWindow;
		float spacingX = MathF.Max( window.WindowPadding.x, g.Style.ItemSpacing.x );
		float targetPosX = ImLerp( g.LastItemData.Rect.Min.x - spacingX, g.LastItemData.Rect.Max.x + spacingX, centerXRatio );
		SetScrollFromPosX( window, targetPosX - window.Pos.x, centerXRatio );
		window.ScrollTargetEdgeSnapDist.x = MathF.Max( 0.0f, window.WindowPadding.x - spacingX );
	}

	public static void SetScrollHereY( float centerYRatio = 0.5f )
	{
		var g = G;
		var window = g.CurrentWindow;
		float spacingY = MathF.Max( window.WindowPadding.y, g.Style.ItemSpacing.y );
		float targetPosY = ImLerp( window.DC.CursorPosPrevLine.y - spacingY, window.DC.CursorPosPrevLine.y + window.DC.PrevLineSize.y + spacingY, centerYRatio );
		SetScrollFromPosY( window, targetPosY - window.Pos.y, centerYRatio );
		window.ScrollTargetEdgeSnapDist.y = MathF.Max( 0.0f, window.WindowPadding.y - spacingY );
	}

	/// <summary>Scroll so that the given rect becomes visible in the window.</summary>
	internal static void ScrollToRect( ImGuiWindow window, ImRect rect )
	{
		var windowRect = new ImRect( window.InnerRect.Min - Vector2.One, window.InnerRect.Max + Vector2.One );
		if ( windowRect.Contains( rect ) )
			return;
		if ( rect.Min.y < windowRect.Min.y )
			SetScrollFromPosY( window, rect.Min.y - window.Pos.y - G.Style.ItemSpacing.y, 0.0f );
		else if ( rect.Max.y >= windowRect.Max.y )
			SetScrollFromPosY( window, rect.Max.y - window.Pos.y + G.Style.ItemSpacing.y, 1.0f );
		if ( rect.Min.x < windowRect.Min.x )
			SetScrollFromPosX( window, rect.Min.x - window.Pos.x - G.Style.ItemSpacing.x, 0.0f );
		else if ( rect.Max.x >= windowRect.Max.x )
			SetScrollFromPosX( window, rect.Max.x - window.Pos.x + G.Style.ItemSpacing.x, 1.0f );
	}
	#endregion

	#region Window queries / setters
	public static bool IsWindowAppearing() => G.CurrentWindow?.Appearing == true;
	public static bool IsWindowCollapsed() => G.CurrentWindow?.Collapsed == true;

	public static bool IsWindowFocused( ImGuiFocusedFlags flags = ImGuiFocusedFlags.None )
	{
		var g = G;
		var refWindow = g.NavWindow;
		var curWindow = g.CurrentWindow;
		if ( refWindow is null )
			return false;
		if ( (flags & ImGuiFocusedFlags.AnyWindow) != 0 )
			return true;
		if ( curWindow is null )
			return false;

		bool popupHierarchy = (flags & ImGuiFocusedFlags.NoPopupHierarchy) == 0;
		if ( (flags & ImGuiFocusedFlags.RootWindow) != 0 )
			curWindow = popupHierarchy ? curWindow.RootWindowPopupTree : curWindow.RootWindow;

		if ( (flags & ImGuiFocusedFlags.ChildWindows) != 0 )
			return IsWindowChildOf( refWindow, curWindow, popupHierarchy );
		return refWindow == curWindow;
	}

	public static bool IsWindowHovered( ImGuiHoveredFlags flags = ImGuiHoveredFlags.None )
	{
		var g = G;
		var refWindow = g.HoveredWindow;
		var curWindow = g.CurrentWindow;
		if ( refWindow is null )
			return false;

		if ( (flags & ImGuiHoveredFlags.AnyWindow) == 0 )
		{
			if ( curWindow is null )
				return false;
			bool popupHierarchy = (flags & ImGuiHoveredFlags.NoPopupHierarchy) == 0;
			if ( (flags & ImGuiHoveredFlags.RootWindow) != 0 )
				curWindow = popupHierarchy ? curWindow.RootWindowPopupTree : curWindow.RootWindow;

			bool result = (flags & ImGuiHoveredFlags.ChildWindows) != 0
				? IsWindowChildOf( refWindow, curWindow, popupHierarchy )
				: refWindow == curWindow;
			if ( !result )
				return false;
		}

		if ( !IsWindowContentHoverable( refWindow, flags ) )
			return false;
		if ( (flags & ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) == 0 )
			if ( g.ActiveId != 0 && !g.ActiveIdAllowOverlap && g.ActiveId != refWindow.MoveId )
				return false;
		return true;
	}

	public static ImDrawList GetWindowDrawList() => GetCurrentWindow().DrawList;
	public static Vector2 GetWindowPos() => G.CurrentWindow.Pos;
	public static Vector2 GetWindowSize() => G.CurrentWindow.Size;
	public static float GetWindowWidth() => G.CurrentWindow.Size.x;
	public static float GetWindowHeight() => G.CurrentWindow.Size.y;

	public static void SetNextWindowPos( Vector2 pos, ImGuiCond cond = ImGuiCond.None, Vector2 pivot = default )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasPos;
		g.NextWindowData.PosVal = pos;
		g.NextWindowData.PosPivotVal = pivot;
		g.NextWindowData.PosCond = cond != ImGuiCond.None ? cond : ImGuiCond.Always;
	}

	public static void SetNextWindowSize( Vector2 size, ImGuiCond cond = ImGuiCond.None )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasSize;
		g.NextWindowData.SizeVal = size;
		g.NextWindowData.SizeCond = cond != ImGuiCond.None ? cond : ImGuiCond.Always;
	}

	/// <summary>
	/// Set next window size limits. Use -1 on an axis to preserve the current size. Use float.MaxValue for no maximum.
	/// </summary>
	public static void SetNextWindowSizeConstraints( Vector2 sizeMin, Vector2 sizeMax, Action<ImGuiSizeCallbackData> customCallback = null )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasSizeConstraint;
		g.NextWindowData.SizeConstraintRect = new ImRect( sizeMin, sizeMax );
		g.NextWindowData.SizeCallback = customCallback;
	}

	public static void SetNextWindowContentSize( Vector2 size )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasContentSize;
		g.NextWindowData.ContentSizeVal = ImTrunc( size );
	}

	public static void SetNextWindowCollapsed( bool collapsed, ImGuiCond cond = ImGuiCond.None )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasCollapsed;
		g.NextWindowData.CollapsedVal = collapsed;
		g.NextWindowData.CollapsedCond = cond != ImGuiCond.None ? cond : ImGuiCond.Always;
	}

	/// <summary>Causes the next window to be focused. Should be called before Begin().</summary>
	public static void SetNextWindowFocus()
	{
		G.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasFocus;
	}

	public static void SetNextWindowScroll( Vector2 scroll )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasScroll;
		g.NextWindowData.ScrollVal = scroll;
	}

	public static void SetNextWindowBgAlpha( float alpha )
	{
		var g = G;
		g.NextWindowData.Flags |= ImGuiNextWindowDataFlags.HasBgAlpha;
		g.NextWindowData.BgAlphaVal = alpha;
	}

	public static void SetWindowPos( Vector2 pos, ImGuiCond cond = ImGuiCond.None ) => SetWindowPos( G.CurrentWindow, pos, cond );
	public static void SetWindowSize( Vector2 size, ImGuiCond cond = ImGuiCond.None ) => SetWindowSize( G.CurrentWindow, size, cond );
	public static void SetWindowCollapsed( bool collapsed, ImGuiCond cond = ImGuiCond.None ) => SetWindowCollapsed( G.CurrentWindow, collapsed, cond );
	public static void SetWindowFocus() => FocusWindow( G.CurrentWindow );

	public static void SetWindowPos( string name, Vector2 pos, ImGuiCond cond = ImGuiCond.None )
	{
		var window = FindWindowByName( name );
		if ( window is not null ) SetWindowPos( window, pos, cond );
	}

	public static void SetWindowSize( string name, Vector2 size, ImGuiCond cond = ImGuiCond.None )
	{
		var window = FindWindowByName( name );
		if ( window is not null ) SetWindowSize( window, size, cond );
	}

	public static void SetWindowCollapsed( string name, bool collapsed, ImGuiCond cond = ImGuiCond.None )
	{
		var window = FindWindowByName( name );
		if ( window is not null ) SetWindowCollapsed( window, collapsed, cond );
	}

	public static void SetWindowFocus( string name )
	{
		if ( name is null )
		{
			FocusWindow( null );
			return;
		}
		var window = FindWindowByName( name );
		if ( window is not null ) FocusWindow( window );
	}

	/// <summary>Per-window font scale. Prefer <see cref="ImGuiIO.FontGlobalScale"/> or PushFont for most uses.</summary>
	public static void SetWindowFontScale( float scale )
	{
		var g = G;
		var window = GetCurrentWindow();
		window.FontWindowScale = scale;
		g.FontSize = window.CalcFontSize();
	}
	#endregion

	#region Focus
	internal static void FocusWindow( ImGuiWindow window )
	{
		var g = G;

		if ( g.NavWindow != window )
		{
			g.NavWindow = window;
			g.NavId = 0;
		}

		ClosePopupsOverWindow( window, false );

		if ( window is null )
			return;

		var focusFrontWindow = window.RootWindow;
		var displayFrontWindow = window.RootWindow;

		if ( g.ActiveId != 0 && g.ActiveIdWindow is not null && g.ActiveIdWindow.RootWindow != focusFrontWindow )
			if ( !g.ActiveIdNoClearOnFocusLoss )
				ClearActiveID();

		BringWindowToFocusFront( focusFrontWindow );
		if ( ((window.Flags | displayFrontWindow.Flags) & ImGuiWindowFlags.NoBringToFrontOnFocus) == 0 )
			BringWindowToDisplayFront( displayFrontWindow );
	}

	internal static void FocusTopMostWindowUnderOne( ImGuiWindow underThisWindow, ImGuiWindow ignoreWindow )
	{
		var g = G;
		int startIdx = g.WindowsFocusOrder.Count - 1;
		if ( underThisWindow is not null )
		{
			int offset = -1;
			while ( (underThisWindow.Flags & ImGuiWindowFlags.ChildWindow) != 0 )
			{
				underThisWindow = underThisWindow.ParentWindow;
				offset = 0;
			}
			startIdx = underThisWindow.FocusOrder + offset;
		}
		for ( int i = Math.Min( startIdx, g.WindowsFocusOrder.Count - 1 ); i >= 0; i-- )
		{
			var window = g.WindowsFocusOrder[i];
			if ( window == ignoreWindow || !window.WasActive )
				continue;
			if ( (window.Flags & (ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip)) != 0 )
				continue;
			if ( (window.Flags & ImGuiWindowFlags.NoMouseInputs) == 0 )
			{
				FocusWindow( window );
				return;
			}
		}
		FocusWindow( null );
	}

	private static void BringWindowToFocusFront( ImGuiWindow window )
	{
		var g = G;
		if ( g.WindowsFocusOrder.Count == 0 || g.WindowsFocusOrder[^1] == window )
			return;
		int idx = g.WindowsFocusOrder.IndexOf( window );
		if ( idx < 0 )
			return;
		g.WindowsFocusOrder.RemoveAt( idx );
		g.WindowsFocusOrder.Add( window );
		for ( int i = idx; i < g.WindowsFocusOrder.Count; i++ )
			g.WindowsFocusOrder[i].FocusOrder = i;
	}

	private static void BringWindowToDisplayFront( ImGuiWindow window )
	{
		var g = G;
		if ( g.Windows.Count == 0 || g.Windows[^1] == window )
			return;
		int idx = g.Windows.IndexOf( window );
		if ( idx < 0 )
			return;
		g.Windows.RemoveAt( idx );
		g.Windows.Add( window );
	}

	internal static void StartMouseMovingWindow( ImGuiWindow window )
	{
		var g = G;
		FocusWindow( window );
		SetActiveID( window.MoveId, window );
		g.ActiveIdClickOffset = g.IO.MouseClickedPos[0] - window.RootWindow.Pos;
		g.ActiveIdNoClearOnFocusLoss = true;

		bool canMoveWindow = (window.Flags & ImGuiWindowFlags.NoMove) == 0 && (window.RootWindow.Flags & ImGuiWindowFlags.NoMove) == 0;
		if ( canMoveWindow )
			g.MovingWindow = window;
	}

	internal static void StopMouseMovingWindow()
	{
		var g = G;
		var window = g.MovingWindow;
		if ( window?.RootWindow is not null )
			MarkIniSettingsDirty( window.RootWindow );
		g.MovingWindow = null;
		ClearActiveID();
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.Button)

public static partial class ImGui
{
	internal static bool ButtonEx( string label, Vector2 sizeArg, ImGuiButtonFlags flags = ImGuiButtonFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		var labelSize = CalcTextSize( label, true );

		var pos = window.DC.CursorPos;
		if ( (flags & ImGuiButtonFlags.AlignTextBaseLine) != 0 && style.FramePadding.y < window.DC.CurrLineTextBaseOffset )
			pos.y += window.DC.CurrLineTextBaseOffset - style.FramePadding.y;
		var size = CalcItemSize( sizeArg, labelSize.x + style.FramePadding.x * 2.0f, labelSize.y + style.FramePadding.y * 2.0f );

		var bb = new ImRect( pos, pos + size );
		ItemSize( size, style.FramePadding.y );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, flags );

		var col = GetColorU32Internal( held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
		RenderFrame( bb.Min, bb.Max, col, true, style.FrameRounding );
		RenderTextClipped( bb.Min + style.FramePadding, bb.Max - style.FramePadding, label, labelSize, style.ButtonTextAlign, bb );

		return pressed;
	}

	/// <summary>A button. Returns true when clicked. size 0 = auto-fit, &lt;0 = align to the right edge.</summary>
	public static bool Button( string label, Vector2 size = default ) => ButtonEx( label, size );

	/// <summary>Button with FramePadding.y = 0, to easily embed within text.</summary>
	public static bool SmallButton( string label )
	{
		var g = G;
		float backupPaddingY = g.Style.FramePadding.y;
		g.Style.FramePadding = new Vector2( g.Style.FramePadding.x, 0.0f );
		bool pressed = ButtonEx( label, Vector2.Zero, ImGuiButtonFlags.AlignTextBaseLine );
		g.Style.FramePadding = new Vector2( g.Style.FramePadding.x, backupPaddingY );
		return pressed;
	}

	/// <summary>Flexible button behavior without the visuals; useful to build custom behaviors.</summary>
	public static bool InvisibleButton( string strId, Vector2 sizeArg, ImGuiButtonFlags flags = ImGuiButtonFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( strId );
		var size = CalcItemSize( sizeArg, 0.0f, 0.0f );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + size );
		ItemSize( size );
		if ( !ItemAdd( bb, id ) )
			return false;

		return ButtonBehavior( bb, id, out _, out _, flags );
	}

	internal static bool ArrowButtonEx( string strId, ImGuiDir dir, Vector2 size, ImGuiButtonFlags flags = ImGuiButtonFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( strId );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + size );
		float defaultSize = GetFrameHeight();
		ItemSize( size, size.y >= defaultSize ? g.Style.FramePadding.y : -1.0f );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, flags );

		var bgCol = GetColorU32Internal( held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
		var textCol = GetColorU32Internal( ImGuiCol.Text );
		RenderFrame( bb.Min, bb.Max, bgCol, true, g.Style.FrameRounding );
		RenderArrow( window.DrawList, bb.Min + new Vector2( MathF.Max( 0.0f, (size.x - g.FontSize) * 0.5f ), MathF.Max( 0.0f, (size.y - g.FontSize) * 0.5f ) ), textCol, dir );

		return pressed;
	}

	/// <summary>Square button with an arrow shape.</summary>
	public static bool ArrowButton( string strId, ImGuiDir dir )
	{
		float sz = GetFrameHeight();
		return ArrowButtonEx( strId, dir, new Vector2( sz, sz ) );
	}

	public static bool Checkbox( string label, ref bool v )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		var labelSize = CalcTextSize( label, true );

		float squareSz = GetFrameHeight();
		var pos = window.DC.CursorPos;
		var totalBb = new ImRect( pos, pos + new Vector2( squareSz + (labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f), labelSize.y + style.FramePadding.y * 2.0f ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id ) )
			return false;

		bool pressed = ButtonBehavior( totalBb, id, out bool hovered, out bool held );
		if ( pressed )
		{
			v = !v;
			MarkItemEdited( id );
		}

		var checkBb = new ImRect( pos, pos + new Vector2( squareSz, squareSz ) );
		RenderFrame( checkBb.Min, checkBb.Max, GetColorU32Internal( held && hovered ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg ), true, style.FrameRounding );
		var checkCol = GetColorU32Internal( ImGuiCol.CheckMark );
		bool mixedValue = (g.LastItemData.InFlags & ImGuiItemFlags.MixedValue) != 0;
		if ( mixedValue )
		{
			var padMixed = new Vector2( MathF.Max( 1.0f, ImTrunc( squareSz / 3.6f ) ) );
			window.DrawList.AddRectFilled( checkBb.Min + padMixed, checkBb.Max - padMixed, checkCol, style.FrameRounding );
		}
		else if ( v )
		{
			float pad = MathF.Max( 1.0f, ImTrunc( squareSz / 6.0f ) );
			RenderCheckMark( window.DrawList, checkBb.Min + new Vector2( pad, pad ), checkCol, squareSz - pad * 2.0f );
		}

		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Checkable | (v ? ImGuiItemStatusFlags.Checked : ImGuiItemStatusFlags.None);
		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( checkBb.Max.x + style.ItemInnerSpacing.x, checkBb.Min.y + style.FramePadding.y ), label );

		return pressed;
	}

	public static bool CheckboxFlags( string label, ref int flags, int flagsValue )
	{
		bool allOn = (flags & flagsValue) == flagsValue;
		bool anyOn = (flags & flagsValue) != 0;
		bool pressed;
		if ( !allOn && anyOn )
		{
			var g = G;
			PushItemFlag( ImGuiItemFlags.MixedValue, true );
			pressed = Checkbox( label, ref allOn );
			PopItemFlag();
		}
		else
		{
			pressed = Checkbox( label, ref allOn );
		}
		if ( pressed )
		{
			if ( allOn )
				flags |= flagsValue;
			else
				flags &= ~flagsValue;
		}
		return pressed;
	}

	public static bool CheckboxFlags( string label, ref uint flags, uint flagsValue )
	{
		int f = unchecked((int)flags);
		bool pressed = CheckboxFlags( label, ref f, unchecked((int)flagsValue) );
		flags = unchecked((uint)f);
		return pressed;
	}

	/// <summary>Checkbox bound to an enum flags value.</summary>
	public static bool CheckboxFlags<T>( string label, ref T flags, T flagsValue ) where T : struct, Enum
	{
		int f = Convert.ToInt32( flags );
		bool pressed = CheckboxFlags( label, ref f, Convert.ToInt32( flagsValue ) );
		if ( pressed )
			flags = (T)Enum.ToObject( typeof( T ), f );
		return pressed;
	}

	/// <summary>Radio button. Use with "if (RadioButton("one", myValue == 1)) myValue = 1;"</summary>
	public static bool RadioButton( string label, bool active )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		var labelSize = CalcTextSize( label, true );

		float squareSz = GetFrameHeight();
		var pos = window.DC.CursorPos;
		var checkBb = new ImRect( pos, pos + new Vector2( squareSz, squareSz ) );
		var totalBb = new ImRect( pos, pos + new Vector2( squareSz + (labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f), labelSize.y + style.FramePadding.y * 2.0f ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id ) )
			return false;

		var center = checkBb.Center;
		float radius = (squareSz - 1.0f) * 0.5f;

		bool pressed = ButtonBehavior( totalBb, id, out bool hovered, out bool held );
		if ( pressed )
			MarkItemEdited( id );

		window.DrawList.AddCircleFilled( center, radius, GetColorU32Internal( held && hovered ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg ) );
		if ( active )
		{
			float pad = MathF.Max( 1.0f, ImTrunc( squareSz / 6.0f ) );
			window.DrawList.AddCircleFilled( center, radius - pad, GetColorU32Internal( ImGuiCol.CheckMark ) );
		}

		if ( style.FrameBorderSize > 0.0f )
		{
			window.DrawList.AddCircle( center + Vector2.One, radius, GetColorU32Internal( ImGuiCol.BorderShadow ), 0, style.FrameBorderSize );
			window.DrawList.AddCircle( center, radius, GetColorU32Internal( ImGuiCol.Border ), 0, style.FrameBorderSize );
		}

		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( checkBb.Max.x + style.ItemInnerSpacing.x, checkBb.Min.y + style.FramePadding.y ), label );

		return pressed;
	}

	/// <summary>Shortcut to handle the above pattern when value is an integer.</summary>
	public static bool RadioButton( string label, ref int v, int vButton )
	{
		bool pressed = RadioButton( label, v == vButton );
		if ( pressed )
			v = vButton;
		return pressed;
	}

	/// <summary>Progress bar. fraction in [0,1]; a negative fraction shows an indeterminate animation.</summary>
	public static void ProgressBar( float fraction, Vector2 sizeArg = default, string overlay = null )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		if ( sizeArg == default )
			sizeArg = new Vector2( -1.17549435E-38f, 0.0f ); // -FLT_MIN: fill available width

		var style = g.Style;
		var pos = window.DC.CursorPos;
		var size = CalcItemSize( sizeArg, CalcItemWidth(), g.FontSize + style.FramePadding.y * 2.0f );
		var bb = new ImRect( pos, pos + size );
		ItemSize( size, style.FramePadding.y );
		if ( !ItemAdd( bb, 0 ) )
			return;

		bool isIndeterminate = fraction < 0.0f;
		if ( !isIndeterminate )
			fraction = ImSaturate( fraction );

		RenderFrame( bb.Min, bb.Max, GetColorU32Internal( ImGuiCol.FrameBg ), true, style.FrameRounding );
		bb.Expand( new Vector2( -style.FrameBorderSize, -style.FrameBorderSize ) );

		float fillN0 = 0.0f, fillN1 = fraction;
		if ( isIndeterminate )
		{
			float fillWidthN = 0.2f;
			fillN0 = ImFmod( -fraction, 1.0f ) * (1.0f + fillWidthN) - fillWidthN;
			fillN1 = ImSaturate( fillN0 + fillWidthN );
			fillN0 = ImSaturate( fillN0 );
		}

		RenderRectFilledRangeH( window.DrawList, bb, GetColorU32Internal( ImGuiCol.PlotHistogram ), fillN0, fillN1, style.FrameRounding );

		if ( isIndeterminate )
			return;

		overlay ??= $"{fraction * 100 + 0.01f:0}%";
		var overlaySize = CalcTextSize( overlay );
		if ( overlaySize.x > 0.0f )
		{
			float fillX = ImLerp( bb.Min.x, bb.Max.x, fillN1 );
			RenderTextClipped( new Vector2( Math.Clamp( fillX + style.ItemSpacing.x, bb.Min.x, bb.Max.x - overlaySize.x - style.ItemInnerSpacing.x ), bb.Min.y ), bb.Max, overlay, overlaySize, new Vector2( 0.0f, 0.5f ), bb );
		}
	}

	private static float ImFmod( float a, float b )
	{
		float r = a % b;
		return r < 0 ? r + b : r;
	}

	/// <summary>Draw a small circle + keep the cursor on the same line. Advance cursor x position by GetTreeNodeToLabelSpacing().</summary>
	public static void Bullet()
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var style = g.Style;
		float lineHeight = MathF.Max( MathF.Min( window.DC.CurrLineSize.y, g.FontSize + style.FramePadding.y * 2 ), g.FontSize );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + new Vector2( g.FontSize, lineHeight ) );
		ItemSize( bb );
		if ( !ItemAdd( bb, 0 ) )
		{
			SameLine( 0, style.FramePadding.x * 2 );
			return;
		}

		var textCol = GetColorU32Internal( ImGuiCol.Text );
		RenderBullet( window.DrawList, bb.Min + new Vector2( style.FramePadding.x + g.FontSize * 0.5f, lineHeight * 0.5f ), textCol );
		SameLine( 0, style.FramePadding.x * 2.0f );
	}

	/// <summary>Hyperlink text button, returns true when clicked.</summary>
	public static bool TextLink( string label )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( label );
		var pos = new Vector2( window.DC.CursorPos.x, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
		var size = CalcTextSize( label, true );
		var bb = new ImRect( pos, pos + size );
		ItemSize( size, 0.0f );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held );
		if ( hovered )
			g.MouseCursor = ImGuiMouseCursor.Hand;

		var textColor = g.Style.Colors[(int)ImGuiCol.TextLink];
		var lineColor = textColor;
		if ( hovered || held )
			lineColor = textColor = new Vector4( textColor.x * 1.2f, textColor.y * 1.2f, textColor.z * 1.2f, textColor.w );

		float lineY = bb.Max.y - 1.0f;
		window.DrawList.AddLine( new Vector2( bb.Min.x, lineY ), new Vector2( bb.Max.x, lineY ), GetColorU32( lineColor ) );

		PushStyleColor( ImGuiCol.Text, textColor );
		RenderText( bb.Min, label );
		PopStyleColor();

		return pressed;
	}

	/// <summary>
	/// Hyperlink for a URL. s&amp;box games cannot open a browser directly, so clicking copies the URL to the clipboard.
	/// </summary>
	public static void TextLinkOpenURL( string label, string url = null )
	{
		url ??= label;
		if ( TextLink( label ) )
			Sandbox.UI.Clipboard.SetText( url );
		SetItemTooltip( "{0} (click to copy)", url );
	}

	#region Images
	public static void Image( Texture texture, Vector2 imageSize, Vector2 uv0, Vector2 uv1, Vector4 tintCol, Vector4 borderCol )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		float borderSize = borderCol.w > 0.0f ? MathF.Max( 1.0f, g.Style.ImageBorderSize ) : g.Style.ImageBorderSize;
		var padding = new Vector2( borderSize, borderSize );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + imageSize + padding * 2.0f );
		ItemSize( bb );
		if ( !ItemAdd( bb, 0 ) )
			return;

		if ( borderSize > 0.0f )
			window.DrawList.AddRect( bb.Min, bb.Max, GetColorU32( borderCol ), 0.0f, ImDrawFlags.None, borderSize );
		window.DrawList.AddImage( texture, bb.Min + padding, bb.Max - padding, uv0, uv1, GetColorU32( tintCol ) );
	}

	public static void Image( Texture texture, Vector2 imageSize ) => Image( texture, imageSize, Vector2.Zero, Vector2.One, Vector4.One, Vector4.Zero );
	public static void Image( Texture texture, Vector2 imageSize, Vector2 uv0, Vector2 uv1 ) => Image( texture, imageSize, uv0, uv1, Vector4.One, Vector4.Zero );

	public static void Image( Texture texture, Vector2 imageSize, Vector2 uv0, Vector2 uv1, Color tintColor, Color borderColor )
		=> Image( texture, imageSize, uv0, uv1, new Vector4( tintColor.r, tintColor.g, tintColor.b, tintColor.a ), new Vector4( borderColor.r, borderColor.g, borderColor.b, borderColor.a ) );

	public static void Image( Texture texture, Vector2 imageSize, Color tintColor, Color borderColor )
		=> Image( texture, imageSize, Vector2.Zero, Vector2.One, tintColor, borderColor );

	/// <summary>Display an image with a background color, inside a button frame.</summary>
	public static bool ImageButton( string strId, Texture texture, Vector2 imageSize, Vector2 uv0 = default, Vector2 uv1 = default, Vector4 bgCol = default, Vector4? tintCol = null )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		if ( uv1 == default ) uv1 = Vector2.One;
		var tint = tintCol ?? Vector4.One;

		int id = window.GetID( strId );
		var padding = g.Style.FramePadding;
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + imageSize + padding * 2.0f );
		ItemSize( bb );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held );

		var col = GetColorU32Internal( held && hovered ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
		RenderFrame( bb.Min, bb.Max, col, true, Math.Clamp( MathF.Min( padding.x, padding.y ), 0.0f, g.Style.FrameRounding ) );
		if ( bgCol.w > 0.0f )
			window.DrawList.AddRectFilled( bb.Min + padding, bb.Max - padding, GetColorU32( bgCol ) );
		window.DrawList.AddImage( texture, bb.Min + padding, bb.Max - padding, uv0, uv1, GetColorU32( tint ) );

		return pressed;
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.Color)

public static partial class ImGui
{
	/// <summary>Drag and drop payload type for an RGB color (data: Vector4 with alpha ignored).</summary>
	public const string PAYLOAD_TYPE_COLOR_3F = "_COL3F";
	/// <summary>Drag and drop payload type for an RGBA color (data: Vector4).</summary>
	public const string PAYLOAD_TYPE_COLOR_4F = "_COL4F";

	private static int F32ToInt8Unbound( float v ) => (int)(v * 255.0f + (v >= 0 ? 0.5f : -0.5f));
	private static int F32ToInt8Sat( float v ) => (int)(ImSaturate( v ) * 255.0f + 0.5f);

	#region Public API
	public static bool ColorEdit3( string label, ref Vector3 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None )
	{
		var c = new float[] { col.x, col.y, col.z, 1.0f };
		bool changed = ColorEdit4Impl( label, c, flags | ImGuiColorEditFlags.NoAlpha );
		if ( changed ) col = new Vector3( c[0], c[1], c[2] );
		return changed;
	}

	public static bool ColorEdit4( string label, ref Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None )
	{
		var c = new float[] { col.x, col.y, col.z, col.w };
		bool changed = ColorEdit4Impl( label, c, flags );
		if ( changed ) col = new Vector4( c[0], c[1], c[2], c[3] );
		return changed;
	}

	/// <summary>Edit an s&amp;box <see cref="Color"/> without its alpha channel.</summary>
	/// <summary>Edit the RGB part of a Vector4 color, keeping its alpha.</summary>
	public static bool ColorEdit3( string label, ref Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None )
	{
		var rgb = new Vector3( col.x, col.y, col.z );
		if ( !ColorEdit3( label, ref rgb, flags ) )
			return false;
		col = new Vector4( rgb.x, rgb.y, rgb.z, col.w );
		return true;
	}

	public static bool ColorEdit3( string label, ref Color col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None )
	{
		var c = new float[] { col.r, col.g, col.b, 1.0f };
		bool changed = ColorEdit4Impl( label, c, flags | ImGuiColorEditFlags.NoAlpha );
		if ( changed ) col = new Color( c[0], c[1], c[2], col.a );
		return changed;
	}

	/// <summary>Edit an s&amp;box <see cref="Color"/> including alpha.</summary>
	public static bool ColorEdit4( string label, ref Color col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None )
	{
		var c = new float[] { col.r, col.g, col.b, col.a };
		bool changed = ColorEdit4Impl( label, c, flags );
		if ( changed ) col = new Color( c[0], c[1], c[2], c[3] );
		return changed;
	}

	public static bool ColorPicker3( string label, ref Vector3 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None )
	{
		var c = new float[] { col.x, col.y, col.z, 1.0f };
		bool changed = ColorPicker4Impl( label, c, flags | ImGuiColorEditFlags.NoAlpha, null );
		if ( changed ) col = new Vector3( c[0], c[1], c[2] );
		return changed;
	}

	public static bool ColorPicker4( string label, ref Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, Vector4? refCol = null )
	{
		var c = new float[] { col.x, col.y, col.z, col.w };
		float[] r = refCol.HasValue ? new float[] { refCol.Value.x, refCol.Value.y, refCol.Value.z, refCol.Value.w } : null;
		bool changed = ColorPicker4Impl( label, c, flags, r );
		if ( changed ) col = new Vector4( c[0], c[1], c[2], c[3] );
		return changed;
	}

	public static bool ColorPicker3( string label, ref Color col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None )
	{
		var c = new float[] { col.r, col.g, col.b, 1.0f };
		bool changed = ColorPicker4Impl( label, c, flags | ImGuiColorEditFlags.NoAlpha, null );
		if ( changed ) col = new Color( c[0], c[1], c[2], col.a );
		return changed;
	}

	public static bool ColorPicker4( string label, ref Color col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, Color? refCol = null )
	{
		var c = new float[] { col.r, col.g, col.b, col.a };
		float[] r = refCol.HasValue ? new float[] { refCol.Value.r, refCol.Value.g, refCol.Value.b, refCol.Value.a } : null;
		bool changed = ColorPicker4Impl( label, c, flags, r );
		if ( changed ) col = new Color( c[0], c[1], c[2], c[3] );
		return changed;
	}

	public static bool ColorButton( string descId, Color col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, Vector2 size = default )
		=> ColorButton( descId, new Vector4( col.r, col.g, col.b, col.a ), flags, size );

	/// <summary>
	/// Initialize the current color edit options (generally on application startup). Options are user-changeable through the right-click context menu.
	/// </summary>
	public static void SetColorEditOptions( ImGuiColorEditFlags flags )
	{
		var g = G;
		if ( (flags & ImGuiColorEditFlags.DisplayMask_) == 0 )
			flags |= ImGuiColorEditFlags.DefaultOptions_ & ImGuiColorEditFlags.DisplayMask_;
		if ( (flags & ImGuiColorEditFlags.DataTypeMask_) == 0 )
			flags |= ImGuiColorEditFlags.DefaultOptions_ & ImGuiColorEditFlags.DataTypeMask_;
		if ( (flags & ImGuiColorEditFlags.PickerMask_) == 0 )
			flags |= ImGuiColorEditFlags.DefaultOptions_ & ImGuiColorEditFlags.PickerMask_;
		if ( (flags & ImGuiColorEditFlags.InputMask_) == 0 )
			flags |= ImGuiColorEditFlags.DefaultOptions_ & ImGuiColorEditFlags.InputMask_;
		g.ColorEditOptions = flags;
	}
	#endregion

	#region HSV helpers
	/// <summary>Hue is lost when converting from grayscale rgb (saturation=0). Restore it from the last edit of this widget.</summary>
	private static void ColorEditRestoreHS( float[] col, ref float h, ref float s, ref float v )
	{
		var g = G;
		if ( g.ColorEditSavedID != g.ColorEditCurrentID || g.ColorEditSavedColor != ColorConvertFloat4ToU32( new Vector4( col[0], col[1], col[2], 0 ) ) )
			return;
		if ( s == 0.0f || (h == 0.0f && g.ColorEditSavedHue == 1) )
			h = g.ColorEditSavedHue;
		if ( v == 0.0f )
			s = g.ColorEditSavedSat;
	}

	private static void ColorEditRestoreH( float[] col, ref float h )
	{
		var g = G;
		if ( g.ColorEditSavedID != g.ColorEditCurrentID || g.ColorEditSavedColor != ColorConvertFloat4ToU32( new Vector4( col[0], col[1], col[2], 0 ) ) )
			return;
		h = g.ColorEditSavedHue;
	}
	#endregion

	#region ColorEdit
	private static readonly string[] ColorEditIds = { "##X", "##Y", "##Z", "##W" };
	private static readonly string[][] ColorEditFmtInt =
	{
		new[] { "%3d", "%3d", "%3d", "%3d" },
		new[] { "R:%3d", "G:%3d", "B:%3d", "A:%3d" },
		new[] { "H:%3d", "S:%3d", "V:%3d", "A:%3d" },
	};
	private static readonly string[][] ColorEditFmtFloat =
	{
		new[] { "%0.3f", "%0.3f", "%0.3f", "%0.3f" },
		new[] { "R:%0.3f", "G:%0.3f", "B:%0.3f", "A:%0.3f" },
		new[] { "H:%0.3f", "S:%0.3f", "V:%0.3f", "A:%0.3f" },
	};

	internal static bool ColorEdit4Impl( string label, float[] col, ImGuiColorEditFlags flags )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		float squareSz = GetFrameHeight();
		string labelDisplay = LabelText( label );
		float wFull = CalcItemWidth();
		g.NextItemData.ClearFlags();

		BeginGroup();
		PushID( label );
		bool setCurrentColorEditId = g.ColorEditCurrentID == 0;
		if ( setCurrentColorEditId )
			g.ColorEditCurrentID = window.IDStack[^1];

		var flagsUntouched = flags;
		if ( (flags & ImGuiColorEditFlags.NoInputs) != 0 )
			flags = (flags & ~ImGuiColorEditFlags.DisplayMask_) | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.NoOptions;

		// Context menu: display and modify options (before defaults are applied)
		if ( (flags & ImGuiColorEditFlags.NoOptions) == 0 )
			ColorEditOptionsPopup( col, flags );

		// Read stored options
		if ( (flags & ImGuiColorEditFlags.DisplayMask_) == 0 )
			flags |= g.ColorEditOptions & ImGuiColorEditFlags.DisplayMask_;
		if ( (flags & ImGuiColorEditFlags.DataTypeMask_) == 0 )
			flags |= g.ColorEditOptions & ImGuiColorEditFlags.DataTypeMask_;
		if ( (flags & ImGuiColorEditFlags.PickerMask_) == 0 )
			flags |= g.ColorEditOptions & ImGuiColorEditFlags.PickerMask_;
		if ( (flags & ImGuiColorEditFlags.InputMask_) == 0 )
			flags |= g.ColorEditOptions & ImGuiColorEditFlags.InputMask_;
		flags |= g.ColorEditOptions & ~(ImGuiColorEditFlags.DisplayMask_ | ImGuiColorEditFlags.DataTypeMask_ | ImGuiColorEditFlags.PickerMask_ | ImGuiColorEditFlags.InputMask_);

		bool alpha = (flags & ImGuiColorEditFlags.NoAlpha) == 0;
		bool hdr = (flags & ImGuiColorEditFlags.HDR) != 0;
		int components = alpha ? 4 : 3;
		float wButton = (flags & ImGuiColorEditFlags.NoSmallPreview) != 0 ? 0.0f : squareSz + style.ItemInnerSpacing.x;
		float wInputs = MathF.Max( wFull - wButton, 1.0f );
		wFull = wInputs + wButton;

		// Convert to the formats we need
		var f = new float[] { col[0], col[1], col[2], alpha ? col[3] : 1.0f };
		if ( (flags & ImGuiColorEditFlags.InputHSV) != 0 && (flags & ImGuiColorEditFlags.DisplayRGB) != 0 )
		{
			ColorConvertHSVtoRGB( f[0], f[1], f[2], out f[0], out f[1], out f[2] );
		}
		else if ( (flags & ImGuiColorEditFlags.InputRGB) != 0 && (flags & ImGuiColorEditFlags.DisplayHSV) != 0 )
		{
			ColorConvertRGBtoHSV( f[0], f[1], f[2], out f[0], out f[1], out f[2] );
			ColorEditRestoreHS( col, ref f[0], ref f[1], ref f[2] );
		}
		var i = new int[] { F32ToInt8Unbound( f[0] ), F32ToInt8Unbound( f[1] ), F32ToInt8Unbound( f[2] ), F32ToInt8Unbound( f[3] ) };

		bool valueChanged = false;
		bool valueChangedAsFloat = false;

		var pos = window.DC.CursorPos;
		float inputsOffsetX = style.ColorButtonPosition == ImGuiDir.Left ? wButton : 0.0f;
		window.DC.CursorPos = new Vector2( pos.x + inputsOffsetX, window.DC.CursorPos.y );

		if ( (flags & (ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.DisplayHSV)) != 0 && (flags & ImGuiColorEditFlags.NoInputs) == 0 )
		{
			// RGB/HSV 0..255 Sliders
			float wItems = wInputs - style.ItemInnerSpacing.x * (components - 1);
			bool hidePrefix = ImTrunc( wItems / components ) <= CalcTextSize( (flags & ImGuiColorEditFlags.Float) != 0 ? "M:0.000" : "M:000" ).x;
			int fmtIdx = hidePrefix ? 0 : ((flags & ImGuiColorEditFlags.DisplayHSV) != 0 ? 2 : 1);

			float prevSplit = 0.0f;
			for ( int n = 0; n < components; n++ )
			{
				if ( n > 0 )
					SameLine( 0, style.ItemInnerSpacing.x );
				float nextSplit = ImTrunc( wItems * (n + 1) / components );
				SetNextItemWidth( MathF.Max( nextSplit - prevSplit, 1.0f ) );
				prevSplit = nextSplit;

				if ( (flags & ImGuiColorEditFlags.Float) != 0 )
				{
					valueChanged |= DragFloat( ColorEditIds[n], ref f[n], 1.0f / 255.0f, 0.0f, hdr ? 0.0f : 1.0f, ColorEditFmtFloat[fmtIdx][n] );
					valueChangedAsFloat |= valueChanged;
				}
				else
				{
					valueChanged |= DragInt( ColorEditIds[n], ref i[n], 1.0f, 0, hdr ? 0 : 255, ColorEditFmtInt[fmtIdx][n] );
				}
				if ( (flags & ImGuiColorEditFlags.NoOptions) == 0 )
					OpenPopupOnItemClick( "context", ImGuiPopupFlags.MouseButtonRight );
			}
		}
		else if ( (flags & ImGuiColorEditFlags.DisplayHex) != 0 && (flags & ImGuiColorEditFlags.NoInputs) == 0 )
		{
			// RGB Hexadecimal Input
			string buf = alpha
				? $"#{Math.Clamp( i[0], 0, 255 ):X2}{Math.Clamp( i[1], 0, 255 ):X2}{Math.Clamp( i[2], 0, 255 ):X2}{Math.Clamp( i[3], 0, 255 ):X2}"
				: $"#{Math.Clamp( i[0], 0, 255 ):X2}{Math.Clamp( i[1], 0, 255 ):X2}{Math.Clamp( i[2], 0, 255 ):X2}";
			SetNextItemWidth( wInputs );
			if ( InputText( "##Text", ref buf, ImGuiInputTextFlags.CharsUppercase ) )
			{
				valueChanged = true;
				var p = (buf ?? string.Empty).TrimStart( '#', ' ', '\t' );
				i[0] = i[1] = i[2] = 0;
				i[3] = 0xFF;
				for ( int n = 0; n < (alpha ? 4 : 3); n++ )
				{
					if ( p.Length < n * 2 + 2 )
						break;
					if ( int.TryParse( p.AsSpan( n * 2, 2 ), System.Globalization.NumberStyles.HexNumber, null, out int parsed ) )
						i[n] = parsed;
					else
						break;
				}
			}
			if ( (flags & ImGuiColorEditFlags.NoOptions) == 0 )
				OpenPopupOnItemClick( "context", ImGuiPopupFlags.MouseButtonRight );
		}

		ImGuiWindow pickerActiveWindow = null;
		if ( (flags & ImGuiColorEditFlags.NoSmallPreview) == 0 )
		{
			float buttonOffsetX = (flags & ImGuiColorEditFlags.NoInputs) != 0 || style.ColorButtonPosition == ImGuiDir.Left ? 0.0f : wInputs + style.ItemInnerSpacing.x;
			window.DC.CursorPos = new Vector2( pos.x + buttonOffsetX, pos.y );

			var colV4 = new Vector4( col[0], col[1], col[2], alpha ? col[3] : 1.0f );
			if ( ColorButton( "##ColorButton", colV4, flags ) )
			{
				if ( (flags & ImGuiColorEditFlags.NoPicker) == 0 )
				{
					// Store current color and open a picker
					g.ColorPickerRef = colV4;
					OpenPopup( "picker" );
					SetNextWindowPos( g.LastItemData.Rect.BL + new Vector2( 0.0f, style.ItemSpacing.y ) );
				}
			}
			if ( (flags & ImGuiColorEditFlags.NoOptions) == 0 )
				OpenPopupOnItemClick( "context", ImGuiPopupFlags.MouseButtonRight );

			if ( BeginPopup( "picker" ) )
			{
				if ( g.CurrentWindow.BeginCount == 1 )
				{
					pickerActiveWindow = g.CurrentWindow;
					if ( labelDisplay.Length > 0 )
					{
						TextEx( labelDisplay, false );
						Spacing();
					}
					var pickerFlagsToForward = ImGuiColorEditFlags.DataTypeMask_ | ImGuiColorEditFlags.PickerMask_ | ImGuiColorEditFlags.InputMask_ | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.AlphaMask_ | ImGuiColorEditFlags.AlphaBar;
					var pickerFlags = (flagsUntouched & pickerFlagsToForward) | ImGuiColorEditFlags.DisplayMask_ | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreviewHalf;
					SetNextItemWidth( squareSz * 12.0f );
					var refCol = new float[] { g.ColorPickerRef.x, g.ColorPickerRef.y, g.ColorPickerRef.z, g.ColorPickerRef.w };
					valueChanged |= ColorPicker4Impl( "##picker", col, pickerFlags, refCol );
				}
				EndPopup();
			}
		}

		if ( labelDisplay.Length > 0 && (flags & ImGuiColorEditFlags.NoLabel) == 0 )
		{
			SameLine( 0.0f, style.ItemInnerSpacing.x );
			window.DC.CursorPos = new Vector2( pos.x + ((flags & ImGuiColorEditFlags.NoInputs) != 0 ? wButton : wFull + style.ItemInnerSpacing.x), window.DC.CursorPos.y );
			TextEx( labelDisplay, false );
		}

		// Convert back
		if ( valueChanged && pickerActiveWindow is null )
		{
			if ( !valueChangedAsFloat )
				for ( int n = 0; n < 4; n++ )
					f[n] = i[n] / 255.0f;
			if ( (flags & ImGuiColorEditFlags.DisplayHSV) != 0 && (flags & ImGuiColorEditFlags.InputRGB) != 0 )
			{
				g.ColorEditSavedHue = f[0];
				g.ColorEditSavedSat = f[1];
				ColorConvertHSVtoRGB( f[0], f[1], f[2], out f[0], out f[1], out f[2] );
				g.ColorEditSavedID = g.ColorEditCurrentID;
				g.ColorEditSavedColor = ColorConvertFloat4ToU32( new Vector4( f[0], f[1], f[2], 0 ) );
			}
			if ( (flags & ImGuiColorEditFlags.DisplayRGB) != 0 && (flags & ImGuiColorEditFlags.InputHSV) != 0 )
				ColorConvertRGBtoHSV( f[0], f[1], f[2], out f[0], out f[1], out f[2] );

			col[0] = f[0];
			col[1] = f[1];
			col[2] = f[2];
			if ( alpha )
				col[3] = f[3];
		}

		if ( setCurrentColorEditId )
			g.ColorEditCurrentID = 0;
		PopID();
		EndGroup();

		// Drag and Drop Target
		if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredRect) != 0 && (g.LastItemData.InFlags & ImGuiItemFlags.ReadOnly) == 0 && (flags & ImGuiColorEditFlags.NoDragDrop) == 0 && BeginDragDropTarget() )
		{
			bool acceptedDragDrop = false;
			var payload3 = AcceptDragDropPayload( PAYLOAD_TYPE_COLOR_3F );
			if ( payload3 is not null && payload3.Data is Vector4 c3 )
			{
				col[0] = c3.x; col[1] = c3.y; col[2] = c3.z; // Preserve alpha if any
				valueChanged = acceptedDragDrop = true;
			}
			var payload4 = AcceptDragDropPayload( PAYLOAD_TYPE_COLOR_4F );
			if ( payload4 is not null && payload4.Data is Vector4 c4 )
			{
				col[0] = c4.x; col[1] = c4.y; col[2] = c4.z;
				if ( components > 3 ) col[3] = c4.w;
				valueChanged = acceptedDragDrop = true;
			}

			// Drag-drop payloads are always RGB
			if ( acceptedDragDrop && (flags & ImGuiColorEditFlags.InputHSV) != 0 )
				ColorConvertRGBtoHSV( col[0], col[1], col[2], out col[0], out col[1], out col[2] );
			EndDragDropTarget();
		}

		// When picker is being actively used, use its active id so IsItemActive() will function on ColorEdit4().
		if ( pickerActiveWindow is not null && g.ActiveId != 0 && g.ActiveIdWindow == pickerActiveWindow )
			g.LastItemData.ID = g.ActiveId;

		if ( valueChanged && g.LastItemData.ID != 0 )
			MarkItemEdited( g.LastItemData.ID );

		return valueChanged;
	}
	#endregion

	#region ColorPicker
	private static Vector2 ImRotate( Vector2 v, float cosA, float sinA ) => new( v.x * cosA - v.y * sinA, v.x * sinA + v.y * cosA );

	private static Vector2 ImLineClosestPoint( Vector2 a, Vector2 b, Vector2 p )
	{
		var ap = p - a;
		var abDir = b - a;
		float dot = ap.x * abDir.x + ap.y * abDir.y;
		if ( dot < 0.0f )
			return a;
		float abLenSqr = abDir.x * abDir.x + abDir.y * abDir.y;
		if ( dot > abLenSqr )
			return b;
		return a + abDir * dot / abLenSqr;
	}

	private static Vector2 ImTriangleClosestPoint( Vector2 a, Vector2 b, Vector2 c, Vector2 p )
	{
		var projAB = ImLineClosestPoint( a, b, p );
		var projBC = ImLineClosestPoint( b, c, p );
		var projCA = ImLineClosestPoint( c, a, p );
		float distAB = ImLengthSqr( p - projAB );
		float distBC = ImLengthSqr( p - projBC );
		float distCA = ImLengthSqr( p - projCA );
		float m = MathF.Min( distAB, MathF.Min( distBC, distCA ) );
		if ( m == distAB ) return projAB;
		if ( m == distBC ) return projBC;
		return projCA;
	}

	private static void ImTriangleBarycentricCoords( Vector2 a, Vector2 b, Vector2 c, Vector2 p, out float u, out float v, out float w )
	{
		var v0 = b - a;
		var v1 = c - a;
		var v2 = p - a;
		float denom = v0.x * v1.y - v1.x * v0.y;
		v = (v2.x * v1.y - v1.x * v2.y) / denom;
		w = (v0.x * v2.y - v2.x * v0.y) / denom;
		u = 1.0f - v - w;
	}

	private static void RenderArrowPointingAt( ImDrawList drawList, Vector2 pos, Vector2 halfSz, ImGuiDir direction, Color32 col )
	{
		switch ( direction )
		{
			case ImGuiDir.Left: drawList.AddTriangleFilled( new Vector2( pos.x + halfSz.x, pos.y - halfSz.y ), new Vector2( pos.x + halfSz.x, pos.y + halfSz.y ), pos, col ); return;
			case ImGuiDir.Right: drawList.AddTriangleFilled( new Vector2( pos.x - halfSz.x, pos.y + halfSz.y ), new Vector2( pos.x - halfSz.x, pos.y - halfSz.y ), pos, col ); return;
			case ImGuiDir.Up: drawList.AddTriangleFilled( new Vector2( pos.x + halfSz.x, pos.y + halfSz.y ), new Vector2( pos.x - halfSz.x, pos.y + halfSz.y ), pos, col ); return;
			case ImGuiDir.Down: drawList.AddTriangleFilled( new Vector2( pos.x - halfSz.x, pos.y - halfSz.y ), new Vector2( pos.x + halfSz.x, pos.y - halfSz.y ), pos, col ); return;
		}
	}

	private static void RenderArrowsForVerticalBar( ImDrawList drawList, Vector2 pos, Vector2 halfSz, float barW, float alpha )
	{
		byte alpha8 = (byte)F32ToInt8Sat( alpha );
		RenderArrowPointingAt( drawList, new Vector2( pos.x + halfSz.x + 1, pos.y ), new Vector2( halfSz.x + 2, halfSz.y + 1 ), ImGuiDir.Right, new Color32( 0, 0, 0, alpha8 ) );
		RenderArrowPointingAt( drawList, new Vector2( pos.x + halfSz.x, pos.y ), halfSz, ImGuiDir.Right, new Color32( 255, 255, 255, alpha8 ) );
		RenderArrowPointingAt( drawList, new Vector2( pos.x + barW - halfSz.x - 1, pos.y ), new Vector2( halfSz.x + 2, halfSz.y + 1 ), ImGuiDir.Left, new Color32( 0, 0, 0, alpha8 ) );
		RenderArrowPointingAt( drawList, new Vector2( pos.x + barW - halfSz.x, pos.y ), halfSz, ImGuiDir.Left, new Color32( 255, 255, 255, alpha8 ) );
	}

	private static Color32 LerpColor32( Color32 a, Color32 b, float t )
	{
		return new Color32(
			(byte)(a.r + (b.r - a.r) * t),
			(byte)(a.g + (b.g - a.g) * t),
			(byte)(a.b + (b.b - a.b) * t),
			(byte)(a.a + (b.a - a.a) * t) );
	}

	internal static bool ColorPicker4Impl( string label, float[] col, ImGuiColorEditFlags flags, float[] refCol )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var drawList = window.DrawList;
		var style = g.Style;
		var io = g.IO;

		float width = CalcItemWidth();
		bool isReadonly = ((g.NextItemData.ItemFlags | g.CurrentItemFlags) & ImGuiItemFlags.ReadOnly) != 0;
		g.NextItemData.ClearFlags();

		PushID( label );
		bool setCurrentColorEditId = g.ColorEditCurrentID == 0;
		if ( setCurrentColorEditId )
			g.ColorEditCurrentID = window.IDStack[^1];
		BeginGroup();

		if ( (flags & ImGuiColorEditFlags.NoSidePreview) == 0 )
			flags |= ImGuiColorEditFlags.NoSmallPreview;

		// Context menu: display and store options.
		if ( (flags & ImGuiColorEditFlags.NoOptions) == 0 )
			ColorPickerOptionsPopup( col, flags );

		// Read stored options
		if ( (flags & ImGuiColorEditFlags.PickerMask_) == 0 )
			flags |= ((g.ColorEditOptions & ImGuiColorEditFlags.PickerMask_) != 0 ? g.ColorEditOptions : ImGuiColorEditFlags.DefaultOptions_) & ImGuiColorEditFlags.PickerMask_;
		if ( (flags & ImGuiColorEditFlags.InputMask_) == 0 )
			flags |= ((g.ColorEditOptions & ImGuiColorEditFlags.InputMask_) != 0 ? g.ColorEditOptions : ImGuiColorEditFlags.DefaultOptions_) & ImGuiColorEditFlags.InputMask_;
		if ( (flags & ImGuiColorEditFlags.NoOptions) == 0 )
			flags |= g.ColorEditOptions & ImGuiColorEditFlags.AlphaBar;

		// Setup
		int components = (flags & ImGuiColorEditFlags.NoAlpha) != 0 ? 3 : 4;
		bool alphaBar = (flags & ImGuiColorEditFlags.AlphaBar) != 0 && (flags & ImGuiColorEditFlags.NoAlpha) == 0;
		var pickerPos = window.DC.CursorPos;
		float squareSz = GetFrameHeight();
		float barsWidth = squareSz;
		float svPickerSize = MathF.Max( barsWidth * 1, width - (alphaBar ? 2 : 1) * (barsWidth + style.ItemInnerSpacing.x) );
		float bar0PosX = pickerPos.x + svPickerSize + style.ItemInnerSpacing.x;
		float bar1PosX = bar0PosX + barsWidth + style.ItemInnerSpacing.x;
		float barsTrianglesHalfSz = ImTrunc( barsWidth * 0.20f );

		var backupInitialCol = new float[4];
		for ( int n = 0; n < components; n++ )
			backupInitialCol[n] = col[n];

		float wheelThickness = svPickerSize * 0.08f;
		float wheelROuter = svPickerSize * 0.50f;
		float wheelRInner = wheelROuter - wheelThickness;
		var wheelCenter = new Vector2( pickerPos.x + (svPickerSize + barsWidth) * 0.5f, pickerPos.y + svPickerSize * 0.5f );

		// Note: the triangle is displayed rotated with trianglePa pointing to Hue, but most coordinates stay unrotated for logic.
		float triangleR = wheelRInner - (int)(svPickerSize * 0.027f);
		var trianglePa = new Vector2( triangleR, 0.0f ); // Hue point.
		var trianglePb = new Vector2( triangleR * -0.5f, triangleR * -0.866025f ); // Black point.
		var trianglePc = new Vector2( triangleR * -0.5f, triangleR * +0.866025f ); // White point.

		float H = col[0], S = col[1], V = col[2];
		float R = col[0], Gc = col[1], B = col[2];
		if ( (flags & ImGuiColorEditFlags.InputRGB) != 0 )
		{
			ColorConvertRGBtoHSV( R, Gc, B, out H, out S, out V );
			ColorEditRestoreHS( col, ref H, ref S, ref V );
		}
		else if ( (flags & ImGuiColorEditFlags.InputHSV) != 0 )
		{
			ColorConvertHSVtoRGB( H, S, V, out R, out Gc, out B );
		}

		bool valueChanged = false, valueChangedH = false, valueChangedSv = false;

		PushItemFlag( ImGuiItemFlags.NoNav, true );
		if ( (flags & ImGuiColorEditFlags.PickerHueWheel) != 0 )
		{
			// Hue wheel + SV triangle logic
			InvisibleButton( "hsv", new Vector2( svPickerSize + style.ItemInnerSpacing.x + barsWidth, svPickerSize ) );
			if ( IsItemActive() && !isReadonly )
			{
				var initialOff = io.MouseClickedPos[0] - wheelCenter;
				var currentOff = io.MousePos - wheelCenter;
				float initialDist2 = ImLengthSqr( initialOff );
				if ( initialDist2 >= (wheelRInner - 1) * (wheelRInner - 1) && initialDist2 <= (wheelROuter + 1) * (wheelROuter + 1) )
				{
					// Interactive with Hue wheel
					H = MathF.Atan2( currentOff.y, currentOff.x ) / MathF.PI * 0.5f;
					if ( H < 0.0f )
						H += 1.0f;
					valueChanged = valueChangedH = true;
				}
				float cosHueAngle = MathF.Cos( -H * 2.0f * MathF.PI );
				float sinHueAngle = MathF.Sin( -H * 2.0f * MathF.PI );
				if ( ImTriangleContainsPoint( trianglePa, trianglePb, trianglePc, ImRotate( initialOff, cosHueAngle, sinHueAngle ) ) )
				{
					// Interacting with SV triangle
					var currentOffUnrotated = ImRotate( currentOff, cosHueAngle, sinHueAngle );
					if ( !ImTriangleContainsPoint( trianglePa, trianglePb, trianglePc, currentOffUnrotated ) )
						currentOffUnrotated = ImTriangleClosestPoint( trianglePa, trianglePb, trianglePc, currentOffUnrotated );
					ImTriangleBarycentricCoords( trianglePa, trianglePb, trianglePc, currentOffUnrotated, out float uu, out float vv, out _ );
					V = Math.Clamp( 1.0f - vv, 0.0001f, 1.0f );
					S = Math.Clamp( uu / V, 0.0001f, 1.0f );
					valueChanged = valueChangedSv = true;
				}
			}
			if ( (flags & ImGuiColorEditFlags.NoOptions) == 0 )
				OpenPopupOnItemClick( "context", ImGuiPopupFlags.MouseButtonRight );
		}
		else if ( (flags & ImGuiColorEditFlags.PickerHueBar) != 0 )
		{
			// SV rectangle logic
			InvisibleButton( "sv", new Vector2( svPickerSize, svPickerSize ) );
			if ( IsItemActive() && !isReadonly )
			{
				S = ImSaturate( (io.MousePos.x - pickerPos.x) / (svPickerSize - 1) );
				V = 1.0f - ImSaturate( (io.MousePos.y - pickerPos.y) / (svPickerSize - 1) );
				ColorEditRestoreH( col, ref H ); // Greatly reduces hue jitter and reset to 0 when hue == 255.
				valueChanged = valueChangedSv = true;
			}
			if ( (flags & ImGuiColorEditFlags.NoOptions) == 0 )
				OpenPopupOnItemClick( "context", ImGuiPopupFlags.MouseButtonRight );

			// Hue bar logic
			SetCursorScreenPos( new Vector2( bar0PosX, pickerPos.y ) );
			InvisibleButton( "hue", new Vector2( barsWidth, svPickerSize ) );
			if ( IsItemActive() && !isReadonly )
			{
				H = ImSaturate( (io.MousePos.y - pickerPos.y) / (svPickerSize - 1) );
				valueChanged = valueChangedH = true;
			}
		}

		// Alpha bar logic
		if ( alphaBar )
		{
			SetCursorScreenPos( new Vector2( bar1PosX, pickerPos.y ) );
			InvisibleButton( "alpha", new Vector2( barsWidth, svPickerSize ) );
			if ( IsItemActive() )
			{
				col[3] = 1.0f - ImSaturate( (io.MousePos.y - pickerPos.y) / (svPickerSize - 1) );
				valueChanged = true;
			}
		}
		PopItemFlag();

		if ( (flags & ImGuiColorEditFlags.NoSidePreview) == 0 )
		{
			SameLine( 0, style.ItemInnerSpacing.x );
			BeginGroup();
		}

		if ( (flags & ImGuiColorEditFlags.NoLabel) == 0 )
		{
			string labelDisplay = LabelText( label );
			if ( labelDisplay.Length > 0 )
			{
				if ( (flags & ImGuiColorEditFlags.NoSidePreview) != 0 )
					SameLine( 0, style.ItemInnerSpacing.x );
				TextEx( labelDisplay, false );
			}
		}

		if ( (flags & ImGuiColorEditFlags.NoSidePreview) == 0 )
		{
			PushItemFlag( ImGuiItemFlags.NoNavDefaultFocus, true );
			var colV4 = new Vector4( col[0], col[1], col[2], (flags & ImGuiColorEditFlags.NoAlpha) != 0 ? 1.0f : col[3] );
			if ( (flags & ImGuiColorEditFlags.NoLabel) != 0 )
				TextUnformatted( "Current" );

			var subFlagsToForward = ImGuiColorEditFlags.InputMask_ | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.AlphaMask_ | ImGuiColorEditFlags.NoTooltip;
			ColorButton( "##current", colV4, flags & subFlagsToForward, new Vector2( squareSz * 3, squareSz * 2 ) );
			if ( refCol is not null )
			{
				TextUnformatted( "Original" );
				var refColV4 = new Vector4( refCol[0], refCol[1], refCol[2], (flags & ImGuiColorEditFlags.NoAlpha) != 0 ? 1.0f : refCol[3] );
				if ( ColorButton( "##original", refColV4, flags & subFlagsToForward, new Vector2( squareSz * 3, squareSz * 2 ) ) )
				{
					for ( int n = 0; n < components; n++ )
						col[n] = refCol[n];
					valueChanged = true;
				}
			}
			PopItemFlag();
			EndGroup();
		}

		// Convert back color to RGB
		if ( valueChangedH || valueChangedSv )
		{
			if ( (flags & ImGuiColorEditFlags.InputRGB) != 0 )
			{
				ColorConvertHSVtoRGB( H, S, V, out col[0], out col[1], out col[2] );
				g.ColorEditSavedHue = H;
				g.ColorEditSavedSat = S;
				g.ColorEditSavedID = g.ColorEditCurrentID;
				g.ColorEditSavedColor = ColorConvertFloat4ToU32( new Vector4( col[0], col[1], col[2], 0 ) );
			}
			else if ( (flags & ImGuiColorEditFlags.InputHSV) != 0 )
			{
				col[0] = H;
				col[1] = S;
				col[2] = V;
			}
		}

		// R,G,B and H,S,V slider color editor
		bool valueChangedFixHueWrap = false;
		if ( (flags & ImGuiColorEditFlags.NoInputs) == 0 )
		{
			PushItemWidth( (alphaBar ? bar1PosX : bar0PosX) + barsWidth - pickerPos.x );
			var subFlagsToForward = ImGuiColorEditFlags.DataTypeMask_ | ImGuiColorEditFlags.InputMask_ | ImGuiColorEditFlags.HDR | ImGuiColorEditFlags.AlphaMask_ | ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoSmallPreview;
			var subFlags = (flags & subFlagsToForward) | ImGuiColorEditFlags.NoPicker;
			if ( (flags & ImGuiColorEditFlags.DisplayRGB) != 0 || (flags & ImGuiColorEditFlags.DisplayMask_) == 0 )
			{
				if ( ColorEdit4Impl( "##rgb", col, subFlags | ImGuiColorEditFlags.DisplayRGB ) )
				{
					// Differentiate using the DragInt (ActiveId != 0 && !ActiveIdAllowOverlap) vs. using the InputText or DropTarget.
					valueChangedFixHueWrap = g.ActiveId != 0 && !g.ActiveIdAllowOverlap;
					valueChanged = true;
				}
			}
			if ( (flags & ImGuiColorEditFlags.DisplayHSV) != 0 || (flags & ImGuiColorEditFlags.DisplayMask_) == 0 )
				valueChanged |= ColorEdit4Impl( "##hsv", col, subFlags | ImGuiColorEditFlags.DisplayHSV );
			if ( (flags & ImGuiColorEditFlags.DisplayHex) != 0 || (flags & ImGuiColorEditFlags.DisplayMask_) == 0 )
				valueChanged |= ColorEdit4Impl( "##hex", col, subFlags | ImGuiColorEditFlags.DisplayHex );
			PopItemWidth();
		}

		// Try to cancel hue wrap (after ColorEdit4 call), if any
		if ( valueChangedFixHueWrap && (flags & ImGuiColorEditFlags.InputRGB) != 0 )
		{
			ColorConvertRGBtoHSV( col[0], col[1], col[2], out float newH, out float newS, out float newV );
			if ( newH <= 0 && H > 0 )
			{
				if ( newV <= 0 && V != newV )
					ColorConvertHSVtoRGB( H, S, newV <= 0 ? V * 0.5f : newV, out col[0], out col[1], out col[2] );
				else if ( newS <= 0 )
					ColorConvertHSVtoRGB( H, newS <= 0 ? S * 0.5f : newS, newV, out col[0], out col[1], out col[2] );
			}
		}

		if ( valueChanged )
		{
			if ( (flags & ImGuiColorEditFlags.InputRGB) != 0 )
			{
				R = col[0];
				Gc = col[1];
				B = col[2];
				ColorConvertRGBtoHSV( R, Gc, B, out H, out S, out V );
				ColorEditRestoreHS( col, ref H, ref S, ref V );
			}
			else if ( (flags & ImGuiColorEditFlags.InputHSV) != 0 )
			{
				H = col[0];
				S = col[1];
				V = col[2];
				ColorConvertHSVtoRGB( H, S, V, out R, out Gc, out B );
			}
		}

		byte styleAlpha8 = (byte)F32ToInt8Sat( style.Alpha );
		var colBlack = new Color32( 0, 0, 0, styleAlpha8 );
		var colWhite = new Color32( 255, 255, 255, styleAlpha8 );
		var colMidgrey = new Color32( 128, 128, 128, styleAlpha8 );
		Color32[] colHues =
		{
			new( 255, 0, 0, styleAlpha8 ), new( 255, 255, 0, styleAlpha8 ), new( 0, 255, 0, styleAlpha8 ),
			new( 0, 255, 255, styleAlpha8 ), new( 0, 0, 255, styleAlpha8 ), new( 255, 0, 255, styleAlpha8 ), new( 255, 0, 0, styleAlpha8 ),
		};

		ColorConvertHSVtoRGB( H, 1, 1, out float hr, out float hg, out float hb );
		var hueColor32 = ColorConvertFloat4ToU32( new Vector4( hr, hg, hb, style.Alpha ) );
		var userCol32StripedOfAlpha = ColorConvertFloat4ToU32( new Vector4( R, Gc, B, style.Alpha ) );

		Vector2 svCursorPos;
		if ( (flags & ImGuiColorEditFlags.PickerHueWheel) != 0 )
		{
			// Render Hue Wheel as small quads, each with a single interpolated hue.
			int segments = Math.Max( 48, (int)(wheelROuter / 2) );
			for ( int n = 0; n < segments; n++ )
			{
				float a0 = n / (float)segments * 2.0f * MathF.PI;
				float a1 = (n + 1.0f) / segments * 2.0f * MathF.PI + 0.5f / wheelROuter;
				float hueT = (n + 0.5f) / segments * 6.0f;
				int hueIdx = Math.Min( (int)hueT, 5 );
				var segCol = LerpColor32( colHues[hueIdx], colHues[hueIdx + 1], hueT - hueIdx );
				var p0 = wheelCenter + new Vector2( MathF.Cos( a0 ), MathF.Sin( a0 ) ) * wheelRInner;
				var p1 = wheelCenter + new Vector2( MathF.Cos( a0 ), MathF.Sin( a0 ) ) * wheelROuter;
				var p2 = wheelCenter + new Vector2( MathF.Cos( a1 ), MathF.Sin( a1 ) ) * wheelROuter;
				var p3 = wheelCenter + new Vector2( MathF.Cos( a1 ), MathF.Sin( a1 ) ) * wheelRInner;
				drawList.AddQuadFilled( p0, p1, p2, p3, segCol );
			}

			// Render Cursor + preview on Hue Wheel
			float cosHueAngle = MathF.Cos( H * 2.0f * MathF.PI );
			float sinHueAngle = MathF.Sin( H * 2.0f * MathF.PI );
			var hueCursorPos = new Vector2( wheelCenter.x + cosHueAngle * (wheelRInner + wheelROuter) * 0.5f, wheelCenter.y + sinHueAngle * (wheelRInner + wheelROuter) * 0.5f );
			float hueCursorRad = valueChangedH ? wheelThickness * 0.65f : wheelThickness * 0.55f;
			drawList.AddCircleFilled( hueCursorPos, hueCursorRad, hueColor32 );
			drawList.AddCircle( hueCursorPos, hueCursorRad + 1, colMidgrey );
			drawList.AddCircle( hueCursorPos, hueCursorRad, colWhite );

			// Render SV triangle (rotated according to hue): subdivide into small triangles, each shaded at its centroid.
			var tra = wheelCenter + ImRotate( trianglePa, cosHueAngle, sinHueAngle );
			var trb = wheelCenter + ImRotate( trianglePb, cosHueAngle, sinHueAngle );
			var trc = wheelCenter + ImRotate( trianglePc, cosHueAngle, sinHueAngle );
			const int sub = 14;
			// Grid point with barycentric weights: black = ib/sub, white = ia/sub, hue = remainder.
			Vector2 P( int ia, int ib ) => tra + (trb - tra) * (ib / (float)sub) + (trc - tra) * (ia / (float)sub);
			Color32 Shade( float wa, float wb, float wc )
			{
				return new Color32(
					(byte)Math.Clamp( hueColor32.r * wa + colBlack.r * wb + colWhite.r * wc, 0, 255 ),
					(byte)Math.Clamp( hueColor32.g * wa + colBlack.g * wb + colWhite.g * wc, 0, 255 ),
					(byte)Math.Clamp( hueColor32.b * wa + colBlack.b * wb + colWhite.b * wc, 0, 255 ),
					styleAlpha8 );
			}
			for ( int ia = 0; ia < sub; ia++ )
			{
				for ( int ib = 0; ib < sub - ia; ib++ )
				{
					// Up-pointing cell
					var q0 = P( ia, ib );
					var q1 = P( ia, ib + 1 );
					var q2 = P( ia + 1, ib );
					float wb0 = (ib + 1.0f / 3.0f) / sub, wc0 = (ia + 1.0f / 3.0f) / sub;
					drawList.AddTriangleFilled( q0, q1, q2, Shade( 1 - wb0 - wc0, wb0, wc0 ) );
					// Down-pointing cell
					if ( ib + ia + 1 < sub )
					{
						var q3 = P( ia + 1, ib + 1 );
						float wb1 = (ib + 2.0f / 3.0f) / sub, wc1 = (ia + 2.0f / 3.0f) / sub;
						drawList.AddTriangleFilled( q1, q3, q2, Shade( 1 - wb1 - wc1, wb1, wc1 ) );
					}
				}
			}
			drawList.AddTriangle( tra, trb, trc, colMidgrey, 1.5f );
			svCursorPos = ImLerp( ImLerp( trc, tra, S ), trb, 1 - V );
		}
		else
		{
			// Render SV Square
			drawList.AddRectFilledMultiColor( pickerPos, pickerPos + new Vector2( svPickerSize, svPickerSize ), colWhite, hueColor32, hueColor32, colWhite );
			drawList.AddRectFilledMultiColor( pickerPos, pickerPos + new Vector2( svPickerSize, svPickerSize ), new Color32( 0, 0, 0, 0 ), new Color32( 0, 0, 0, 0 ), colBlack, colBlack );
			RenderFrameBorder( pickerPos, pickerPos + new Vector2( svPickerSize, svPickerSize ), 0.0f );
			svCursorPos = new Vector2(
				Math.Clamp( MathF.Round( pickerPos.x + ImSaturate( S ) * svPickerSize ), pickerPos.x + 2, pickerPos.x + svPickerSize - 2 ),
				Math.Clamp( MathF.Round( pickerPos.y + ImSaturate( 1 - V ) * svPickerSize ), pickerPos.y + 2, pickerPos.y + svPickerSize - 2 ) );

			// Render Hue Bar
			for ( int n = 0; n < 6; ++n )
				drawList.AddRectFilledMultiColor(
					new Vector2( bar0PosX, pickerPos.y + n * (svPickerSize / 6) ),
					new Vector2( bar0PosX + barsWidth, pickerPos.y + (n + 1) * (svPickerSize / 6) ),
					colHues[n], colHues[n], colHues[n + 1], colHues[n + 1] );
			float bar0LineY = MathF.Round( pickerPos.y + H * svPickerSize );
			RenderFrameBorder( new Vector2( bar0PosX, pickerPos.y ), new Vector2( bar0PosX + barsWidth, pickerPos.y + svPickerSize ), 0.0f );
			RenderArrowsForVerticalBar( drawList, new Vector2( bar0PosX - 1, bar0LineY ), new Vector2( barsTrianglesHalfSz + 1, barsTrianglesHalfSz ), barsWidth + 2.0f, style.Alpha );
		}

		// Render cursor/preview circle
		float svCursorRad = valueChangedSv ? wheelThickness * 0.55f : wheelThickness * 0.40f;
		drawList.AddCircleFilled( svCursorPos, svCursorRad, userCol32StripedOfAlpha );
		drawList.AddCircle( svCursorPos, svCursorRad + 1, colMidgrey );
		drawList.AddCircle( svCursorPos, svCursorRad, colWhite );

		// Render alpha bar
		if ( alphaBar )
		{
			float alpha = ImSaturate( col[3] );
			var bar1Bb = new ImRect( bar1PosX, pickerPos.y, bar1PosX + barsWidth, pickerPos.y + svPickerSize );
			RenderColorRectWithAlphaCheckerboard( drawList, bar1Bb.Min, bar1Bb.Max, new Color32( 0, 0, 0, 0 ), bar1Bb.Width / 2.0f, Vector2.Zero );
			var transparentUserCol = userCol32StripedOfAlpha with { a = 0 };
			drawList.AddRectFilledMultiColor( bar1Bb.Min, bar1Bb.Max, userCol32StripedOfAlpha, userCol32StripedOfAlpha, transparentUserCol, transparentUserCol );
			float bar1LineY = MathF.Round( pickerPos.y + (1.0f - alpha) * svPickerSize );
			RenderFrameBorder( bar1Bb.Min, bar1Bb.Max, 0.0f );
			RenderArrowsForVerticalBar( drawList, new Vector2( bar1PosX - 1, bar1LineY ), new Vector2( barsTrianglesHalfSz + 1, barsTrianglesHalfSz ), barsWidth + 2.0f, style.Alpha );
		}

		EndGroup();

		if ( valueChanged )
		{
			bool same = true;
			for ( int n = 0; n < components; n++ )
				if ( backupInitialCol[n] != col[n] )
					same = false;
			if ( same )
				valueChanged = false;
		}
		if ( valueChanged && g.LastItemData.ID != 0 )
			MarkItemEdited( g.LastItemData.ID );

		if ( setCurrentColorEditId )
			g.ColorEditCurrentID = 0;
		PopID();

		return valueChanged;
	}
	#endregion

	#region ColorButton
	/// <summary>
	/// Display a color square/button, hover for details, return true when pressed. Can be dragged to a ColorEdit to drop the color.
	/// </summary>
	public static bool ColorButton( string descId, Vector4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, Vector2 sizeArg = default )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( descId );
		float defaultSize = GetFrameHeight();
		var size = new Vector2( sizeArg.x == 0.0f ? defaultSize : sizeArg.x, sizeArg.y == 0.0f ? defaultSize : sizeArg.y );
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + size );
		ItemSize( bb, size.y >= defaultSize ? g.Style.FramePadding.y : 0.0f );
		if ( !ItemAdd( bb, id ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out _ );

		if ( (flags & (ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.AlphaOpaque)) != 0 )
			flags &= ~(ImGuiColorEditFlags.AlphaNoBg | ImGuiColorEditFlags.AlphaPreviewHalf);

		var colRgb = col;
		if ( (flags & ImGuiColorEditFlags.InputHSV) != 0 )
		{
			ColorConvertHSVtoRGB( colRgb.x, colRgb.y, colRgb.z, out float r, out float gg, out float b );
			colRgb = new Vector4( r, gg, b, colRgb.w );
		}

		var colRgbWithoutAlpha = new Vector4( colRgb.x, colRgb.y, colRgb.z, 1.0f );
		float gridStep = MathF.Min( size.x, size.y ) / 2.99f;
		float rounding = MathF.Min( g.Style.FrameRounding, gridStep * 0.5f );
		var bbInner = bb;
		float off = 0.0f;
		if ( (flags & ImGuiColorEditFlags.NoBorder) == 0 )
		{
			off = -0.75f;
			bbInner.Expand( off );
		}
		if ( (flags & ImGuiColorEditFlags.AlphaPreviewHalf) != 0 && colRgb.w < 1.0f )
		{
			float midX = MathF.Round( (bbInner.Min.x + bbInner.Max.x) * 0.5f );
			if ( (flags & ImGuiColorEditFlags.AlphaNoBg) == 0 )
				RenderColorRectWithAlphaCheckerboard( window.DrawList, new Vector2( bbInner.Min.x + gridStep, bbInner.Min.y ), bbInner.Max, GetColorU32( colRgb ), gridStep, new Vector2( -gridStep + off, off ), rounding, ImDrawFlags.RoundCornersRight );
			else
				window.DrawList.AddRectFilled( new Vector2( bbInner.Min.x + gridStep, bbInner.Min.y ), bbInner.Max, GetColorU32( colRgb ), rounding, ImDrawFlags.RoundCornersRight );
			window.DrawList.AddRectFilled( bbInner.Min, new Vector2( midX, bbInner.Max.y ), GetColorU32( colRgbWithoutAlpha ), rounding, ImDrawFlags.RoundCornersLeft );
		}
		else
		{
			var colSource = (flags & ImGuiColorEditFlags.AlphaOpaque) != 0 ? colRgbWithoutAlpha : colRgb;
			if ( colSource.w < 1.0f && (flags & ImGuiColorEditFlags.AlphaNoBg) == 0 )
				RenderColorRectWithAlphaCheckerboard( window.DrawList, bbInner.Min, bbInner.Max, GetColorU32( colSource ), gridStep, new Vector2( off, off ), rounding );
			else
				window.DrawList.AddRectFilled( bbInner.Min, bbInner.Max, GetColorU32( colSource ), rounding );
		}

		if ( (flags & ImGuiColorEditFlags.NoBorder) == 0 )
		{
			if ( g.Style.FrameBorderSize > 0.0f )
				RenderFrameBorder( bb.Min, bb.Max, rounding );
			else
				window.DrawList.AddRect( bb.Min, bb.Max, GetColorU32Internal( ImGuiCol.FrameBg ), rounding );
		}

		// Drag and Drop Source
		if ( g.ActiveId == id && (flags & ImGuiColorEditFlags.NoDragDrop) == 0 && BeginDragDropSource() )
		{
			if ( (flags & ImGuiColorEditFlags.NoAlpha) != 0 )
				SetDragDropPayload( PAYLOAD_TYPE_COLOR_3F, colRgb, ImGuiCond.Once );
			else
				SetDragDropPayload( PAYLOAD_TYPE_COLOR_4F, colRgb, ImGuiCond.Once );
			ColorButton( descId, col, flags );
			SameLine();
			TextEx( "Color", false );
			EndDragDropSource();
			hovered = false;
		}

		// Tooltip
		if ( (flags & ImGuiColorEditFlags.NoTooltip) == 0 && hovered && IsItemHovered( ImGuiHoveredFlags.ForTooltip ) )
			ColorTooltip( descId, new[] { col.x, col.y, col.z, col.w }, flags & (ImGuiColorEditFlags.InputMask_ | ImGuiColorEditFlags.AlphaMask_) );

		return pressed;
	}
	#endregion

	#region Tooltip / options
	internal static void ColorTooltip( string text, float[] col, ImGuiColorEditFlags flags )
	{
		var g = G;
		if ( !BeginTooltipEx() )
			return;

		string textDisplay = LabelText( text );
		if ( textDisplay.Length > 0 )
		{
			TextEx( textDisplay, false );
			Separator();
		}

		var sz = new Vector2( g.FontSize * 3 + g.Style.FramePadding.y * 2, g.FontSize * 3 + g.Style.FramePadding.y * 2 );
		var cf = new Vector4( col[0], col[1], col[2], (flags & ImGuiColorEditFlags.NoAlpha) != 0 ? 1.0f : col[3] );
		int cr = F32ToInt8Sat( col[0] ), cg = F32ToInt8Sat( col[1] ), cb = F32ToInt8Sat( col[2] ), ca = (flags & ImGuiColorEditFlags.NoAlpha) != 0 ? 255 : F32ToInt8Sat( col[3] );
		var flagsToForward = ImGuiColorEditFlags.InputMask_ | ImGuiColorEditFlags.AlphaMask_;
		ColorButton( "##preview", cf, (flags & flagsToForward) | ImGuiColorEditFlags.NoTooltip, sz );
		SameLine();
		if ( (flags & ImGuiColorEditFlags.InputRGB) != 0 || (flags & ImGuiColorEditFlags.InputMask_) == 0 )
		{
			if ( (flags & ImGuiColorEditFlags.NoAlpha) != 0 )
				TextUnformatted( $"#{cr:X2}{cg:X2}{cb:X2}\nR: {cr}, G: {cg}, B: {cb}\n({col[0]:0.000}, {col[1]:0.000}, {col[2]:0.000})" );
			else
				TextUnformatted( $"#{cr:X2}{cg:X2}{cb:X2}{ca:X2}\nR:{cr}, G:{cg}, B:{cb}, A:{ca}\n({col[0]:0.000}, {col[1]:0.000}, {col[2]:0.000}, {col[3]:0.000})" );
		}
		else if ( (flags & ImGuiColorEditFlags.InputHSV) != 0 )
		{
			if ( (flags & ImGuiColorEditFlags.NoAlpha) != 0 )
				TextUnformatted( $"H: {col[0]:0.000}, S: {col[1]:0.000}, V: {col[2]:0.000}" );
			else
				TextUnformatted( $"H: {col[0]:0.000}, S: {col[1]:0.000}, V: {col[2]:0.000}, A: {col[3]:0.000}" );
		}
		EndTooltip();
	}

	internal static void ColorEditOptionsPopup( float[] col, ImGuiColorEditFlags flags )
	{
		bool allowOptInputs = (flags & ImGuiColorEditFlags.DisplayMask_) == 0;
		bool allowOptDatatype = (flags & ImGuiColorEditFlags.DataTypeMask_) == 0;
		if ( (!allowOptInputs && !allowOptDatatype) || !BeginPopup( "context" ) )
			return;

		var g = G;
		var opts = g.ColorEditOptions;
		if ( allowOptInputs )
		{
			if ( RadioButton( "RGB", (opts & ImGuiColorEditFlags.DisplayRGB) != 0 ) ) opts = (opts & ~ImGuiColorEditFlags.DisplayMask_) | ImGuiColorEditFlags.DisplayRGB;
			if ( RadioButton( "HSV", (opts & ImGuiColorEditFlags.DisplayHSV) != 0 ) ) opts = (opts & ~ImGuiColorEditFlags.DisplayMask_) | ImGuiColorEditFlags.DisplayHSV;
			if ( RadioButton( "Hex", (opts & ImGuiColorEditFlags.DisplayHex) != 0 ) ) opts = (opts & ~ImGuiColorEditFlags.DisplayMask_) | ImGuiColorEditFlags.DisplayHex;
		}
		if ( allowOptDatatype )
		{
			if ( allowOptInputs ) Separator();
			if ( RadioButton( "0..255", (opts & ImGuiColorEditFlags.Uint8) != 0 ) ) opts = (opts & ~ImGuiColorEditFlags.DataTypeMask_) | ImGuiColorEditFlags.Uint8;
			if ( RadioButton( "0.00..1.00", (opts & ImGuiColorEditFlags.Float) != 0 ) ) opts = (opts & ~ImGuiColorEditFlags.DataTypeMask_) | ImGuiColorEditFlags.Float;
		}

		if ( allowOptInputs || allowOptDatatype )
			Separator();
		if ( Button( "Copy as..", new Vector2( -1, 0 ) ) )
			OpenPopup( "Copy" );
		if ( BeginPopup( "Copy" ) )
		{
			int cr = F32ToInt8Sat( col[0] ), cg = F32ToInt8Sat( col[1] ), cb = F32ToInt8Sat( col[2] ), ca = (flags & ImGuiColorEditFlags.NoAlpha) != 0 ? 255 : F32ToInt8Sat( col[3] );
			string buf = $"({col[0]:0.000}f, {col[1]:0.000}f, {col[2]:0.000}f, {((flags & ImGuiColorEditFlags.NoAlpha) != 0 ? 1.0f : col[3]):0.000}f)";
			if ( Selectable( buf ) ) Sandbox.UI.Clipboard.SetText( buf );
			buf = $"({cr},{cg},{cb},{ca})";
			if ( Selectable( buf ) ) Sandbox.UI.Clipboard.SetText( buf );
			buf = $"#{cr:X2}{cg:X2}{cb:X2}";
			if ( Selectable( buf ) ) Sandbox.UI.Clipboard.SetText( buf );
			if ( (flags & ImGuiColorEditFlags.NoAlpha) == 0 )
			{
				buf = $"#{cr:X2}{cg:X2}{cb:X2}{ca:X2}";
				if ( Selectable( buf ) ) Sandbox.UI.Clipboard.SetText( buf );
			}
			EndPopup();
		}

		g.ColorEditOptions = opts;
		EndPopup();
	}

	internal static void ColorPickerOptionsPopup( float[] refCol, ImGuiColorEditFlags flags )
	{
		bool allowOptPicker = (flags & ImGuiColorEditFlags.PickerMask_) == 0;
		bool allowOptAlphaBar = (flags & ImGuiColorEditFlags.NoAlpha) == 0 && (flags & ImGuiColorEditFlags.AlphaBar) == 0;
		if ( (!allowOptPicker && !allowOptAlphaBar) || !BeginPopup( "context" ) )
			return;

		var g = G;
		if ( allowOptPicker )
		{
			var pickerSize = new Vector2( g.FontSize * 8, MathF.Max( g.FontSize * 8 - (GetFrameHeight() + g.Style.ItemInnerSpacing.x), 1.0f ) );
			PushItemWidth( pickerSize.x );
			for ( int pickerType = 0; pickerType < 2; pickerType++ )
			{
				// Draw a small version of each picker type (over an invisible button for selection)
				if ( pickerType > 0 ) Separator();
				PushID( pickerType );
				var pickerFlags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoOptions | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.NoSidePreview | (flags & ImGuiColorEditFlags.NoAlpha);
				if ( pickerType == 0 ) pickerFlags |= ImGuiColorEditFlags.PickerHueBar;
				if ( pickerType == 1 ) pickerFlags |= ImGuiColorEditFlags.PickerHueWheel;
				var backupPos = GetCursorScreenPos();
				if ( Selectable( "##selectable", false, ImGuiSelectableFlags.None, pickerSize ) )
					g.ColorEditOptions = (g.ColorEditOptions & ~ImGuiColorEditFlags.PickerMask_) | (pickerFlags & ImGuiColorEditFlags.PickerMask_);
				SetCursorScreenPos( backupPos );
				var previewingRefCol = new float[] { refCol[0], refCol[1], refCol[2], (pickerFlags & ImGuiColorEditFlags.NoAlpha) != 0 ? 1.0f : refCol[3] };
				ColorPicker4Impl( "##previewing_picker", previewingRefCol, pickerFlags, null );
				PopID();
			}
			PopItemWidth();
		}
		if ( allowOptAlphaBar )
		{
			if ( allowOptPicker ) Separator();
			CheckboxFlags( "Alpha Bar", ref g.ColorEditOptions, ImGuiColorEditFlags.AlphaBar );
		}
		EndPopup();
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.Columns)

public static partial class ImGui
{
	private const float COLUMNS_HIT_RECT_HALF_THICKNESS = 4.0f;

	private static int GetColumnsID( string strId, int columnsCount )
	{
		var window = G.CurrentWindow;
		PushID( 0x11223347 + (strId is not null ? 0 : columnsCount) );
		int id = window.GetID( strId ?? "columns" );
		PopID();
		return id;
	}

	private static ImGuiOldColumns FindOrCreateColumns( ImGuiWindow window, int id )
	{
		foreach ( var c in window.ColumnsStorage )
			if ( c.ID == id )
				return c;
		var columns = new ImGuiOldColumns { ID = id };
		window.ColumnsStorage.Add( columns );
		return columns;
	}

	private static float GetColumnOffsetFromNorm( ImGuiOldColumns columns, float offsetNorm ) => offsetNorm * (columns.OffMaxX - columns.OffMinX);
	private static float GetColumnNormFromOffset( ImGuiOldColumns columns, float offset ) => offset / (columns.OffMaxX - columns.OffMinX);

	private static float GetColumnWidthEx( ImGuiOldColumns columns, int columnIndex, bool beforeResize = false )
	{
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		float offsetNorm = beforeResize
			? columns.Columns[columnIndex + 1].OffsetNormBeforeResize - columns.Columns[columnIndex].OffsetNormBeforeResize
			: columns.Columns[columnIndex + 1].OffsetNorm - columns.Columns[columnIndex].OffsetNorm;
		return GetColumnOffsetFromNorm( columns, offsetNorm );
	}

	/// <summary>Legacy columns API (2020: prefer using Tables!). Columns(1) ends the current column set.</summary>
	public static void Columns( int count = 1, string id = null, bool borders = true )
	{
		var window = GetCurrentWindow();
		var flags = borders ? ImGuiOldColumnFlags.None : ImGuiOldColumnFlags.NoBorder;
		var columns = window.DC.CurrentColumns;
		if ( columns is not null && columns.Count == count && columns.Flags == flags )
			return;
		if ( columns is not null )
			EndColumns();
		if ( count != 1 )
			BeginColumns( id, count, flags );
	}

	public static void BeginColumns( string strId, int columnsCount, ImGuiOldColumnFlags flags = ImGuiOldColumnFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( columnsCount < 1 || window.DC.CurrentColumns is not null )
			return;

		int id = GetColumnsID( strId, columnsCount );
		var columns = FindOrCreateColumns( window, id );
		columns.Current = 0;
		columns.Count = columnsCount;
		columns.Flags = flags;
		window.DC.CurrentColumns = columns;

		columns.HostCursorPosY = window.DC.CursorPos.y;
		columns.HostCursorMaxPosX = window.DC.CursorMaxPos.x;
		columns.HostInitialClipRect = window.ClipRect;
		columns.HostBackupParentWorkRect = window.ParentWorkRect;
		window.ParentWorkRect = window.WorkRect;

		float columnPadding = g.Style.ItemSpacing.x;
		float halfClipExtendX = ImTrunc( MathF.Max( window.WindowPadding.x * 0.5f, window.WindowBorderSize ) );
		float max1 = window.WorkRect.Max.x + columnPadding - MathF.Max( columnPadding - window.WindowPadding.x, 0.0f );
		float max2 = window.WorkRect.Max.x + halfClipExtendX;
		columns.OffMinX = window.DC.Indent - columnPadding + MathF.Max( columnPadding - window.WindowPadding.x, 0.0f );
		columns.OffMaxX = MathF.Max( MathF.Min( max1, max2 ) - window.Pos.x, columns.OffMinX + 1.0f );
		columns.LineMinY = columns.LineMaxY = window.DC.CursorPos.y;

		if ( columns.Columns.Count != 0 && columns.Columns.Count != columnsCount + 1 )
			columns.Columns.Clear();

		columns.IsFirstFrame = columns.Columns.Count == 0;
		if ( columns.Columns.Count == 0 )
		{
			for ( int n = 0; n < columnsCount + 1; n++ )
				columns.Columns.Add( new ImGuiOldColumnData { OffsetNorm = n / (float)columnsCount } );
		}

		for ( int n = 0; n < columnsCount; n++ )
		{
			var column = columns.Columns[n];
			float clipX1 = MathF.Round( window.Pos.x + GetColumnOffset( n ) );
			float clipX2 = MathF.Round( window.Pos.x + GetColumnOffset( n + 1 ) - 1.0f );
			column.ClipRect = new ImRect( clipX1, -float.MaxValue, clipX2, float.MaxValue );
			column.ClipRect.ClipWithFull( window.ClipRect );
		}

		columns.UsesChannels = columns.Count > 1 && !g.SplitDrawLists.Contains( window.DrawList );
		if ( columns.Count > 1 )
		{
			if ( columns.UsesChannels )
			{
				g.SplitDrawLists.Add( window.DrawList );
				window.DrawList.ChannelsSplit( 1 + columns.Count );
				window.DrawList.ChannelsSetCurrent( 1 );
			}
			PushColumnClipRect( 0 );
		}

		float offset0 = GetColumnOffset( columns.Current );
		float offset1 = GetColumnOffset( columns.Current + 1 );
		float width = offset1 - offset0;
		PushItemWidth( width * 0.65f );
		window.DC.ColumnsOffset = MathF.Max( columnPadding - window.WindowPadding.x, 0.0f );
		window.DC.CursorPos = new Vector2( ImTrunc( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset ), window.DC.CursorPos.y );
		window.WorkRect.Max.x = window.Pos.x + offset1 - columnPadding;
		window.WorkRect.Max.y = window.ContentRegionRect.Max.y;
	}

	private static void PushColumnClipRect( int columnIndex )
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		var column = columns.Columns[columnIndex];
		PushClipRect( column.ClipRect.Min, column.ClipRect.Max, false );
	}

	public static void NextColumn()
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems || window.DC.CurrentColumns is null )
			return;

		var columns = window.DC.CurrentColumns;
		if ( columns.Count == 1 )
		{
			window.DC.CursorPos = new Vector2( ImTrunc( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset ), window.DC.CursorPos.y );
			return;
		}

		if ( ++columns.Current == columns.Count )
			columns.Current = 0;

		PopItemWidth();
		PopClipRect();
		if ( columns.UsesChannels )
			window.DrawList.ChannelsSetCurrent( columns.Current + 1 );
		PushColumnClipRect( columns.Current );

		float columnPadding = g.Style.ItemSpacing.x;
		columns.LineMaxY = MathF.Max( columns.LineMaxY, window.DC.CursorPos.y );
		if ( columns.Current > 0 )
		{
			window.DC.ColumnsOffset = GetColumnOffset( columns.Current ) - window.DC.Indent + columnPadding;
		}
		else
		{
			window.DC.ColumnsOffset = MathF.Max( columnPadding - window.WindowPadding.x, 0.0f );
			window.DC.IsSameLine = false;
			columns.LineMinY = columns.LineMaxY;
		}
		window.DC.CursorPos = new Vector2( ImTrunc( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset ), columns.LineMinY );
		window.DC.CurrLineSize = Vector2.Zero;
		window.DC.CurrLineTextBaseOffset = 0.0f;

		float offset0 = GetColumnOffset( columns.Current );
		float offset1 = GetColumnOffset( columns.Current + 1 );
		float width = offset1 - offset0;
		PushItemWidth( width * 0.65f );
		window.WorkRect.Max.x = window.Pos.x + offset1 - columnPadding;
	}

	public static void EndColumns()
	{
		var g = G;
		var window = G.CurrentWindow;
		var columns = window?.DC.CurrentColumns;
		if ( columns is null )
			return;

		PopItemWidth();
		if ( columns.Count > 1 )
		{
			PopClipRect();
			if ( columns.UsesChannels )
			{
				window.DrawList.ChannelsMerge();
				g.SplitDrawLists.Remove( window.DrawList );
			}
		}

		var flags = columns.Flags;
		columns.LineMaxY = MathF.Max( columns.LineMaxY, window.DC.CursorPos.y );
		window.DC.CursorPos = new Vector2( window.DC.CursorPos.x, columns.LineMaxY );
		if ( (flags & ImGuiOldColumnFlags.GrowParentContentsSize) == 0 )
			window.DC.CursorMaxPos = new Vector2( columns.HostCursorMaxPosX, window.DC.CursorMaxPos.y );

		bool isBeingResized = false;
		if ( (flags & ImGuiOldColumnFlags.NoBorder) == 0 && !window.SkipItems )
		{
			float y1 = MathF.Max( columns.HostCursorPosY, window.ClipRect.Min.y );
			float y2 = MathF.Min( window.DC.CursorPos.y, window.ClipRect.Max.y );
			int draggingColumn = -1;
			for ( int n = 1; n < columns.Count; n++ )
			{
				var column = columns.Columns[n];
				float x = window.Pos.x + GetColumnOffset( n );
				int columnId = unchecked(columns.ID + n);
				float hitHw = ImTrunc( COLUMNS_HIT_RECT_HALF_THICKNESS * g.AppliedStyleScale );
				var hitRect = new ImRect( new Vector2( x - hitHw, y1 ), new Vector2( x + hitHw, y2 ) );
				if ( !ItemAdd( hitRect, columnId, null, ImGuiItemFlags.NoNav ) )
					continue;

				bool hovered = false, held = false;
				if ( (flags & ImGuiOldColumnFlags.NoResize) == 0 )
				{
					ButtonBehavior( hitRect, columnId, out hovered, out held );
					if ( hovered || held )
						g.MouseCursor = ImGuiMouseCursor.ResizeEW;
					if ( held && (column.Flags & ImGuiOldColumnFlags.NoResize) == 0 )
						draggingColumn = n;
				}

				var col = GetColorU32Internal( held ? ImGuiCol.SeparatorActive : hovered ? ImGuiCol.SeparatorHovered : ImGuiCol.Separator );
				float xi = ImTrunc( x );
				window.DrawList.AddLine( new Vector2( xi, y1 + 1.0f ), new Vector2( xi, y2 ), col );
			}

			if ( draggingColumn != -1 )
			{
				if ( !columns.IsBeingResized )
					foreach ( var c in columns.Columns )
						c.OffsetNormBeforeResize = c.OffsetNorm;
				columns.IsBeingResized = isBeingResized = true;
				float x = GetDraggedColumnOffset( columns, draggingColumn );
				SetColumnOffset( draggingColumn, x );
			}
		}
		columns.IsBeingResized = isBeingResized;

		window.WorkRect = window.ParentWorkRect;
		window.ParentWorkRect = columns.HostBackupParentWorkRect;
		window.DC.CurrentColumns = null;
		window.DC.ColumnsOffset = 0.0f;
		window.DC.CursorPos = new Vector2( ImTrunc( window.Pos.x + window.DC.Indent + window.DC.ColumnsOffset ), window.DC.CursorPos.y );
	}

	private static float GetDraggedColumnOffset( ImGuiOldColumns columns, int columnIndex )
	{
		var g = G;
		var window = g.CurrentWindow;
		float x = g.IO.MousePos.x - g.ActiveIdClickOffset.x + ImTrunc( COLUMNS_HIT_RECT_HALF_THICKNESS * g.AppliedStyleScale ) - window.Pos.x;
		x = MathF.Max( x, GetColumnOffset( columnIndex - 1 ) + g.Style.ColumnsMinSpacing );
		if ( (columns.Flags & ImGuiOldColumnFlags.NoPreserveWidths) != 0 )
			x = MathF.Min( x, GetColumnOffset( columnIndex + 1 ) - g.Style.ColumnsMinSpacing );
		return x;
	}

	public static int GetColumnIndex() => G.CurrentWindow.DC.CurrentColumns?.Current ?? 0;
	public static int GetColumnsCount() => G.CurrentWindow.DC.CurrentColumns?.Count ?? 1;

	/// <summary>Get position of column line (in pixels, from the left side of the contents region).</summary>
	public static float GetColumnOffset( int columnIndex = -1 )
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null )
			return 0.0f;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		columnIndex = Math.Clamp( columnIndex, 0, columns.Columns.Count - 1 );
		float t = columns.Columns[columnIndex].OffsetNorm;
		return ImLerp( columns.OffMinX, columns.OffMaxX, t );
	}

	public static void SetColumnOffset( int columnIndex, float offset )
	{
		var g = G;
		var window = g.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null )
			return;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		if ( columnIndex >= columns.Columns.Count )
			return;

		bool preserveWidth = (columns.Flags & ImGuiOldColumnFlags.NoPreserveWidths) == 0 && columnIndex < columns.Count - 1;
		float width = preserveWidth ? GetColumnWidthEx( columns, columnIndex, columns.IsBeingResized ) : 0.0f;

		if ( (columns.Flags & ImGuiOldColumnFlags.NoForceWithinWindow) == 0 )
			offset = MathF.Min( offset, columns.OffMaxX - g.Style.ColumnsMinSpacing * (columns.Count - columnIndex) );
		columns.Columns[columnIndex].OffsetNorm = GetColumnNormFromOffset( columns, offset - columns.OffMinX );

		if ( preserveWidth )
			SetColumnOffset( columnIndex + 1, offset + MathF.Max( g.Style.ColumnsMinSpacing, width ) );
	}

	public static float GetColumnWidth( int columnIndex = -1 )
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null )
			return GetContentRegionAvail().x;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		return GetColumnOffsetFromNorm( columns, columns.Columns[columnIndex + 1].OffsetNorm - columns.Columns[columnIndex].OffsetNorm );
	}

	public static void SetColumnWidth( int columnIndex, float width )
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null )
			return;
		if ( columnIndex < 0 )
			columnIndex = columns.Current;
		SetColumnOffset( columnIndex + 1, GetColumnOffset( columnIndex ) + width );
	}

	/// <summary>Draw into the background channel spanning all columns (used e.g. by Selectable with SpanAllColumns).</summary>
	internal static void PushColumnsBackground()
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null || columns.Count == 1 )
			return;
		columns.HostBackupClipRect = window.ClipRect;
		if ( columns.UsesChannels )
			window.DrawList.ChannelsSetCurrent( 0 );
		PushClipRect( columns.HostInitialClipRect.Min, columns.HostInitialClipRect.Max, false );
	}

	internal static void PopColumnsBackground()
	{
		var window = G.CurrentWindow;
		var columns = window.DC.CurrentColumns;
		if ( columns is null || columns.Count == 1 )
			return;
		PopClipRect();
		if ( columns.UsesChannels )
			window.DrawList.ChannelsSetCurrent( columns.Current + 1 );
	}
}

#endregion

#region ImGui (Widgets/ImGui.Drag)

public static partial class ImGui
{
	private const float DRAG_MOUSE_THRESHOLD_FACTOR = 0.50f;

	#region Ratio <-> value (shared with sliders)
	internal static float ScaleRatioFromValue( double v, double vMin, double vMax, bool isLogarithmic, float logarithmicZeroEpsilon, float zeroDeadzoneHalfsize )
	{
		if ( vMin == vMax )
			return 0.0f;

		double vClamped = vMin < vMax ? Math.Clamp( v, vMin, vMax ) : Math.Clamp( v, vMax, vMin );
		if ( isLogarithmic )
		{
			bool flipped = vMax < vMin;
			if ( flipped )
				(vMin, vMax) = (vMax, vMin);

			double vMinFudged = Math.Abs( vMin ) < logarithmicZeroEpsilon ? (vMin < 0.0 ? -logarithmicZeroEpsilon : logarithmicZeroEpsilon) : vMin;
			double vMaxFudged = Math.Abs( vMax ) < logarithmicZeroEpsilon ? (vMax < 0.0 ? -logarithmicZeroEpsilon : logarithmicZeroEpsilon) : vMax;

			if ( vMin == 0.0 && vMax < 0.0 )
				vMinFudged = -logarithmicZeroEpsilon;
			else if ( vMax == 0.0 && vMin < 0.0 )
				vMaxFudged = -logarithmicZeroEpsilon;

			float result;
			if ( vClamped <= vMinFudged )
				result = 0.0f;
			else if ( vClamped >= vMaxFudged )
				result = 1.0f;
			else if ( vMin * vMax < 0.0 )
			{
				float zeroPointCenter = (float)(-vMin / (vMax - vMin));
				float zeroPointSnapL = zeroPointCenter - zeroDeadzoneHalfsize;
				float zeroPointSnapR = zeroPointCenter + zeroDeadzoneHalfsize;
				if ( v == 0.0 )
					result = zeroPointCenter;
				else if ( v < 0.0 )
					result = (1.0f - (float)(Math.Log( -vClamped / logarithmicZeroEpsilon ) / Math.Log( -vMinFudged / logarithmicZeroEpsilon ))) * zeroPointSnapL;
				else
					result = zeroPointSnapR + (float)(Math.Log( vClamped / logarithmicZeroEpsilon ) / Math.Log( vMaxFudged / logarithmicZeroEpsilon )) * (1.0f - zeroPointSnapR);
			}
			else if ( vMin < 0.0 || vMax < 0.0 )
				result = 1.0f - (float)(Math.Log( -vClamped / -vMaxFudged ) / Math.Log( -vMinFudged / -vMaxFudged ));
			else
				result = (float)(Math.Log( vClamped / vMinFudged ) / Math.Log( vMaxFudged / vMinFudged ));

			return flipped ? 1.0f - result : result;
		}

		return (float)((vClamped - vMin) / (vMax - vMin));
	}

	internal static double ScaleValueFromRatio( float t, double vMin, double vMax, bool isInteger, bool isLogarithmic, float logarithmicZeroEpsilon, float zeroDeadzoneHalfsize )
	{
		if ( t <= 0.0f || vMin == vMax )
			return vMin;
		if ( t >= 1.0f )
			return vMax;

		double result;
		if ( isLogarithmic )
		{
			double vMinFudged = Math.Abs( vMin ) < logarithmicZeroEpsilon ? (vMin < 0.0 ? -logarithmicZeroEpsilon : logarithmicZeroEpsilon) : vMin;
			double vMaxFudged = Math.Abs( vMax ) < logarithmicZeroEpsilon ? (vMax < 0.0 ? -logarithmicZeroEpsilon : logarithmicZeroEpsilon) : vMax;
			bool flipped = vMax < vMin;
			if ( flipped )
				(vMinFudged, vMaxFudged) = (vMaxFudged, vMinFudged);

			if ( vMax == 0.0 && vMin < 0.0 )
				vMaxFudged = -logarithmicZeroEpsilon;

			float tWithFlip = flipped ? 1.0f - t : t;

			if ( vMin * vMax < 0.0 )
			{
				float zeroPointCenter = (float)(-Math.Min( vMin, vMax ) / Math.Abs( vMax - vMin ));
				float zeroPointSnapL = zeroPointCenter - zeroDeadzoneHalfsize;
				float zeroPointSnapR = zeroPointCenter + zeroDeadzoneHalfsize;
				if ( tWithFlip >= zeroPointSnapL && tWithFlip <= zeroPointSnapR )
					result = 0.0;
				else if ( tWithFlip < zeroPointCenter )
					result = -(logarithmicZeroEpsilon * Math.Pow( -vMinFudged / logarithmicZeroEpsilon, 1.0f - tWithFlip / zeroPointSnapL ));
				else
					result = logarithmicZeroEpsilon * Math.Pow( vMaxFudged / logarithmicZeroEpsilon, (tWithFlip - zeroPointSnapR) / (1.0f - zeroPointSnapR) );
			}
			else if ( vMin < 0.0 || vMax < 0.0 )
				result = -(-vMaxFudged * Math.Pow( -vMinFudged / -vMaxFudged, 1.0f - tWithFlip ));
			else
				result = vMinFudged * Math.Pow( vMaxFudged / vMinFudged, tWithFlip );

			if ( isInteger )
				result = Math.Round( result, MidpointRounding.AwayFromZero );
			return result;
		}

		if ( !isInteger )
			return vMin + (vMax - vMin) * t;

		// Integer: round so the clicking position matches the grab box.
		double vNewOff = (vMax - vMin) * t;
		return vMin + Math.Truncate( vNewOff + (vMin > vMax ? -0.5 : 0.5) );
	}
	#endregion

	#region Drag behavior
	/// <summary>Port of Dear ImGui's DragBehaviorT, operating on doubles.</summary>
	internal static bool DragBehavior( int id, ref double v, float vSpeed, double vMin, double vMax, string format, ImGuiSliderFlags flags, bool isInteger )
	{
		var g = G;
		if ( g.ActiveId == id )
		{
			if ( !g.IO.MouseDown[0] )
				ClearActiveID();
		}
		if ( g.ActiveId != id )
			return false;
		if ( (g.LastItemData.InFlags & ImGuiItemFlags.ReadOnly) != 0 || (flags & ImGuiSliderFlags.ReadOnly) != 0 )
			return false;

		int axis = (flags & ImGuiSliderFlags.Vertical) != 0 ? 1 : 0;
		bool isBounded = vMin < vMax || (vMin == vMax && (vMin != 0.0 || (flags & ImGuiSliderFlags.ClampZeroRange) != 0));
		bool isWrapped = isBounded && (flags & ImGuiSliderFlags.WrapAround) != 0;
		bool isLogarithmic = (flags & ImGuiSliderFlags.Logarithmic) != 0;
		bool isFloatingPoint = !isInteger;

		if ( vSpeed == 0.0f && isBounded && vMax - vMin < float.MaxValue )
			vSpeed = (float)((vMax - vMin) * g.DragSpeedDefaultRatio);

		float adjustDelta = 0.0f;
		if ( IsMousePosValid() && IsMouseDragPastThreshold( ImGuiMouseButton.Left, g.IO.MouseDragThreshold * DRAG_MOUSE_THRESHOLD_FACTOR ) )
		{
			adjustDelta = Axis( g.IO.MouseDelta, axis );
			if ( g.IO.KeyAlt && (flags & ImGuiSliderFlags.NoSpeedTweaks) == 0 )
				adjustDelta *= 1.0f / 100.0f;
			if ( g.IO.KeyShift && (flags & ImGuiSliderFlags.NoSpeedTweaks) == 0 )
				adjustDelta *= 10.0f;
		}
		adjustDelta *= vSpeed;

		if ( axis == 1 )
			adjustDelta = -adjustDelta;

		if ( isLogarithmic && vMax - vMin < float.MaxValue && vMax - vMin > 0.000001f )
			adjustDelta /= (float)(vMax - vMin);

		bool isJustActivated = g.ActiveIdIsJustActivated;
		bool isAlreadyPastLimitsAndPushingOutward = isBounded && !isWrapped && ((v >= vMax && adjustDelta > 0.0f) || (v <= vMin && adjustDelta < 0.0f));
		if ( isJustActivated || isAlreadyPastLimitsAndPushingOutward )
		{
			g.DragCurrentAccum = 0.0f;
			g.DragCurrentAccumDirty = false;
		}
		else if ( adjustDelta != 0.0f )
		{
			g.DragCurrentAccum += adjustDelta;
			g.DragCurrentAccumDirty = true;
		}

		if ( !g.DragCurrentAccumDirty )
			return false;

		double vCur = v;
		double vOldRefForAccumRemainder = 0.0;

		float logarithmicZeroEpsilon = 0.0f;
		const float zeroDeadzoneHalfsize = 0.0f;
		if ( isLogarithmic )
		{
			int decimalPrecision = isFloatingPoint ? ParseFormatPrecision( format, 3 ) : 1;
			logarithmicZeroEpsilon = MathF.Pow( 0.1f, Math.Max( 0, decimalPrecision ) );

			float vOldParametric = ScaleRatioFromValue( vCur, vMin, vMax, true, logarithmicZeroEpsilon, zeroDeadzoneHalfsize );
			float vNewParametric = vOldParametric + g.DragCurrentAccum;
			vCur = ScaleValueFromRatio( vNewParametric, vMin, vMax, isInteger, true, logarithmicZeroEpsilon, zeroDeadzoneHalfsize );
			vOldRefForAccumRemainder = vOldParametric;
		}
		else
		{
			vCur += isInteger ? Math.Truncate( g.DragCurrentAccum ) : g.DragCurrentAccum;
		}

		if ( isFloatingPoint && (flags & ImGuiSliderFlags.NoRoundToFormat) == 0 )
			vCur = RoundScalarWithFormat( format, vCur );

		g.DragCurrentAccumDirty = false;
		if ( isLogarithmic )
		{
			float vNewParametric = ScaleRatioFromValue( vCur, vMin, vMax, true, logarithmicZeroEpsilon, zeroDeadzoneHalfsize );
			g.DragCurrentAccum -= (float)(vNewParametric - vOldRefForAccumRemainder);
		}
		else
		{
			g.DragCurrentAccum -= (float)(vCur - v);
		}

		if ( vCur == 0.0 )
			vCur = 0.0;

		if ( v != vCur && isBounded )
		{
			if ( isWrapped )
			{
				if ( vCur < vMin )
					vCur += vMax - vMin + (isFloatingPoint ? 0 : 1);
				if ( vCur > vMax )
					vCur -= vMax - vMin + (isFloatingPoint ? 0 : 1);
			}
			else
			{
				if ( vCur < vMin )
					vCur = vMin;
				if ( vCur > vMax )
					vCur = vMax;
			}
		}

		if ( v == vCur )
			return false;
		v = vCur;
		return true;
	}
	#endregion

	#region DragScalar core
	/// <summary>Single-value drag widget over a double. Returns true when the value changed.</summary>
	internal static bool DragScalarCore( string label, ref double value, float vSpeed, double vMin, double vMax, bool hasMin, bool hasMax, string format, ImGuiSliderFlags flags, bool isInteger )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		float w = CalcItemWidth();

		var labelSize = CalcTextSize( label, true );
		var frameBb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + new Vector2( w, labelSize.y + style.FramePadding.y * 2.0f ) );
		var totalBb = new ImRect( frameBb.Min, frameBb.Max + new Vector2( labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f, 0.0f ) );

		bool tempInputAllowed = (flags & ImGuiSliderFlags.NoInput) == 0;
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id, frameBb, tempInputAllowed ? ImGuiItemFlags.Inputable : ImGuiItemFlags.None ) )
			return false;

		if ( string.IsNullOrEmpty( format ) )
			format = isInteger ? "%d" : "%.3f";

		bool hovered = ItemHoverable( frameBb, id, g.LastItemData.InFlags );
		if ( hovered )
			g.MouseCursor = ImGuiMouseCursor.ResizeEW;
		bool tempInputIsActive = tempInputAllowed && TempInputIsActive( id );
		if ( !tempInputIsActive )
		{
			bool focusRequested = tempInputAllowed && g.FocusedTextInputRequestId == id;
			if ( focusRequested )
				g.FocusedTextInputRequestId = 0;

			bool clicked = hovered && g.IO.MouseClicked[0];
			bool doubleClicked = hovered && g.IO.MouseClicked[0] && g.IO.MouseClickedCount[0] == 2;
			bool makeActive = clicked || doubleClicked || focusRequested;
			if ( makeActive && tempInputAllowed )
				if ( (clicked && g.IO.KeyCtrl) || doubleClicked || focusRequested )
					tempInputIsActive = true;

			if ( g.IO.ConfigDragClickToInputText && tempInputAllowed && !tempInputIsActive )
				if ( g.ActiveId == id && hovered && g.IO.MouseReleased[0] && !IsMouseDragPastThreshold( ImGuiMouseButton.Left, g.IO.MouseDragThreshold * DRAG_MOUSE_THRESHOLD_FACTOR ) )
					tempInputIsActive = true;

			if ( makeActive && !tempInputIsActive )
			{
				SetActiveID( id, window );
				g.ActiveIdMouseButton = 0;
				g.NavId = id;
				FocusWindow( window );
			}
		}

		if ( tempInputIsActive )
		{
			bool clampEnabled = false;
			if ( (flags & ImGuiSliderFlags.ClampOnInput) != 0 && (hasMin || hasMax) )
			{
				if ( vMin < vMax ) clampEnabled = true;
				else if ( vMin == vMax ) clampEnabled = vMin != 0.0 || (flags & ImGuiSliderFlags.ClampZeroRange) != 0;
			}
			return TempInputScalar( frameBb, id, label, ref value, format, isInteger, clampEnabled, vMin, vMax );
		}

		var frameCol = GetColorU32Internal( g.ActiveId == id ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg );
		RenderFrame( frameBb.Min, frameBb.Max, frameCol, true, style.FrameRounding );

		bool valueChanged = DragBehavior( id, ref value, vSpeed, vMin, vMax, format, flags, isInteger );
		if ( valueChanged )
			MarkItemEdited( id );

		string valueText = FormatNumber( format, value, isInteger );
		RenderTextClippedEx( window.DrawList, frameBb.Min, frameBb.Max, valueText, null, new Vector2( 0.5f, 0.5f ), null );

		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( frameBb.Max.x + style.ItemInnerSpacing.x, frameBb.Min.y + style.FramePadding.y ), label );

		return valueChanged;
	}

	/// <summary>Draw N components side by side (shared by Drag/Slider/Input N variants).</summary>
	internal static bool ScalarNCore( string label, int components, Func<int, bool> componentWidget )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		bool valueChanged = false;
		BeginGroup();
		PushID( label );
		PushMultiItemsWidths( components, CalcItemWidth() );
		for ( int i = 0; i < components; i++ )
		{
			PushID( i );
			if ( i > 0 )
				SameLine( 0, g.Style.ItemInnerSpacing.x );
			valueChanged |= componentWidget( i );
			PopID();
			PopItemWidth();
		}
		PopID();

		if ( FindRenderedTextEnd( label ) > 0 )
		{
			SameLine( 0, g.Style.ItemInnerSpacing.x );
			TextEx( label, true );
		}
		EndGroup();
		return valueChanged;
	}
	#endregion

	#region Generic drag API
	public static bool DragScalar<T>( string label, ref T v, float vSpeed = 1f, T? min = null, T? max = null, string format = null, ImGuiSliderFlags flags = ImGuiSliderFlags.None ) where T : struct, INumber<T>
	{
		bool isInteger = IsIntegerType<T>();
		GetTypeLimits<T>( out var typeMin, out var typeMax );
		double vMin = min.HasValue ? ToDouble( min.Value ) : typeMin;
		double vMax = max.HasValue ? ToDouble( max.Value ) : typeMax;
		double d = ToDouble( v );
		bool changed = DragScalarCore( label, ref d, vSpeed, vMin, vMax, min.HasValue, max.HasValue, format, flags, isInteger );
		if ( changed )
		{
			var nv = FromDouble<T>( d );
			changed = nv != v;
			v = nv;
		}
		return changed;
	}

	public static bool DragScalarN<T>( string label, T[] v, float vSpeed = 1f, T? min = null, T? max = null, string format = null, ImGuiSliderFlags flags = ImGuiSliderFlags.None ) where T : struct, INumber<T>
	{
		return ScalarNCore( label, v.Length, i => DragScalar( "", ref v[i], vSpeed, min, max, format, flags ) );
	}
	#endregion

	#region Float drags
	public static bool DragFloat( string label, ref float v, float vSpeed = 1.0f, float vMin = 0f, float vMax = 0f, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		double d = v;
		bool changed = DragScalarCore( label, ref d, vSpeed, vMin, vMax, true, true, format, flags, false );
		if ( changed )
			v = (float)d;
		return changed;
	}

	public static bool DragFloat2( string label, float[] v, float vSpeed = 1.0f, float vMin = 0f, float vMax = 0f, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
		=> DragFloatN( label, v, 2, vSpeed, vMin, vMax, format, flags );
	public static bool DragFloat3( string label, float[] v, float vSpeed = 1.0f, float vMin = 0f, float vMax = 0f, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
		=> DragFloatN( label, v, 3, vSpeed, vMin, vMax, format, flags );
	public static bool DragFloat4( string label, float[] v, float vSpeed = 1.0f, float vMin = 0f, float vMax = 0f, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
		=> DragFloatN( label, v, 4, vSpeed, vMin, vMax, format, flags );

	private static bool DragFloatN( string label, float[] v, int n, float vSpeed, float vMin, float vMax, string format, ImGuiSliderFlags flags )
		=> ScalarNCore( label, Math.Min( n, v.Length ), i => DragFloat( "", ref v[i], vSpeed, vMin, vMax, format, flags ) );

	public static bool DragFloat2( string label, ref Vector2 v, float vSpeed = 1.0f, float vMin = 0f, float vMax = 0f, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y };
		bool changed = DragFloatN( label, a, 2, vSpeed, vMin, vMax, format, flags );
		if ( changed ) v = new Vector2( a[0], a[1] );
		return changed;
	}

	public static bool DragFloat3( string label, ref Vector3 v, float vSpeed = 1.0f, float vMin = 0f, float vMax = 0f, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y, v.z };
		bool changed = DragFloatN( label, a, 3, vSpeed, vMin, vMax, format, flags );
		if ( changed ) v = new Vector3( a[0], a[1], a[2] );
		return changed;
	}

	public static bool DragFloat4( string label, ref Vector4 v, float vSpeed = 1.0f, float vMin = 0f, float vMax = 0f, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y, v.z, v.w };
		bool changed = DragFloatN( label, a, 4, vSpeed, vMin, vMax, format, flags );
		if ( changed ) v = new Vector4( a[0], a[1], a[2], a[3] );
		return changed;
	}

	public static bool DragFloatRange2( string label, ref float vCurrentMin, ref float vCurrentMax, float vSpeed = 1.0f, float vMin = 0f, float vMax = 0f, string format = "%.3f", string formatMax = null, ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		PushID( label );
		BeginGroup();
		PushMultiItemsWidths( 2, CalcItemWidth() );

		float minMin = vMin >= vMax ? -float.MaxValue : vMin;
		float minMax = vMin >= vMax ? vCurrentMax : MathF.Min( vMax, vCurrentMax );
		var minFlags = flags | (minMin == minMax ? ImGuiSliderFlags.ReadOnly : ImGuiSliderFlags.None);
		double dMin = vCurrentMin;
		bool valueChanged = DragScalarCore( "##min", ref dMin, vSpeed, minMin, minMax, true, true, format, minFlags, false );
		if ( valueChanged ) vCurrentMin = (float)dMin;
		PopItemWidth();
		SameLine( 0, g.Style.ItemInnerSpacing.x );

		float maxMin = vMin >= vMax ? vCurrentMin : MathF.Max( vMin, vCurrentMin );
		float maxMax = vMin >= vMax ? float.MaxValue : vMax;
		var maxFlags = flags | (maxMin == maxMax ? ImGuiSliderFlags.ReadOnly : ImGuiSliderFlags.None);
		double dMax = vCurrentMax;
		bool maxChanged = DragScalarCore( "##max", ref dMax, vSpeed, maxMin, maxMax, true, true, formatMax ?? format, maxFlags, false );
		if ( maxChanged ) vCurrentMax = (float)dMax;
		valueChanged |= maxChanged;
		PopItemWidth();
		SameLine( 0, g.Style.ItemInnerSpacing.x );

		TextEx( label, true );
		EndGroup();
		PopID();
		return valueChanged;
	}
	#endregion

	#region Int drags
	public static bool DragInt( string label, ref int v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		double d = v;
		double lo = vMin, hi = vMax;
		bool bounded = vMin != 0 || vMax != 0 || (flags & ImGuiSliderFlags.ClampZeroRange) != 0;
		if ( !bounded )
		{
			lo = int.MinValue;
			hi = int.MaxValue;
		}
		bool changed = DragScalarCore( label, ref d, vSpeed, lo, hi, bounded, bounded, format, flags, true );
		if ( changed )
		{
			int nv = (int)Math.Clamp( Math.Round( d ), int.MinValue, int.MaxValue );
			changed = nv != v;
			v = nv;
		}
		return changed;
	}

	private static bool DragIntN( string label, int[] v, int n, float vSpeed, int vMin, int vMax, string format, ImGuiSliderFlags flags )
		=> ScalarNCore( label, Math.Min( n, v.Length ), i => DragInt( "", ref v[i], vSpeed, vMin, vMax, format, flags ) );

	public static bool DragInt2( string label, int[] v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
		=> DragIntN( label, v, 2, vSpeed, vMin, vMax, format, flags );
	public static bool DragInt3( string label, int[] v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
		=> DragIntN( label, v, 3, vSpeed, vMin, vMax, format, flags );
	public static bool DragInt4( string label, int[] v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
		=> DragIntN( label, v, 4, vSpeed, vMin, vMax, format, flags );

	public static bool DragInt2( string label, ref Vector2Int v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y };
		bool changed = DragIntN( label, a, 2, vSpeed, vMin, vMax, format, flags );
		if ( changed ) v = new Vector2Int( a[0], a[1] );
		return changed;
	}

	public static bool DragInt3( string label, ref Vector3Int v, float vSpeed = 1.0f, int vMin = 0, int vMax = 0, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y, v.z };
		bool changed = DragIntN( label, a, 3, vSpeed, vMin, vMax, format, flags );
		if ( changed ) v = new Vector3Int( a[0], a[1], a[2] );
		return changed;
	}

	public static bool DragIntRange2( string label, ref int vCurrentMin, ref int vCurrentMax, float vSpeed = 1.0f, int vMin = 0, int vMax = 0, string format = "%d", string formatMax = null, ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		PushID( label );
		BeginGroup();
		PushMultiItemsWidths( 2, CalcItemWidth() );

		int minMin = vMin >= vMax ? int.MinValue : vMin;
		int minMax = vMin >= vMax ? vCurrentMax : Math.Min( vMax, vCurrentMax );
		var minFlags = flags | (minMin == minMax ? ImGuiSliderFlags.ReadOnly : ImGuiSliderFlags.None);
		double dMin = vCurrentMin;
		bool valueChanged = DragScalarCore( "##min", ref dMin, vSpeed, minMin, minMax, true, true, format, minFlags, true );
		if ( valueChanged ) vCurrentMin = (int)Math.Round( dMin );
		PopItemWidth();
		SameLine( 0, g.Style.ItemInnerSpacing.x );

		int maxMin = vMin >= vMax ? vCurrentMin : Math.Max( vMin, vCurrentMin );
		int maxMax = vMin >= vMax ? int.MaxValue : vMax;
		var maxFlags = flags | (maxMin == maxMax ? ImGuiSliderFlags.ReadOnly : ImGuiSliderFlags.None);
		double dMax = vCurrentMax;
		bool maxChanged = DragScalarCore( "##max", ref dMax, vSpeed, maxMin, maxMax, true, true, formatMax ?? format, maxFlags, true );
		if ( maxChanged ) vCurrentMax = (int)Math.Round( dMax );
		valueChanged |= maxChanged;
		PopItemWidth();
		SameLine( 0, g.Style.ItemInnerSpacing.x );

		TextEx( label, true );
		EndGroup();
		PopID();
		return valueChanged;
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.DragDrop)

public static partial class ImGui
{
	internal static void NewFrameDragDrop()
	{
		var g = G;
		g.DragDropAcceptIdPrev = g.DragDropAcceptIdCurr;
		g.DragDropAcceptIdCurr = 0;
		g.DragDropAcceptIdCurrRectSurface = float.MaxValue;
		g.DragDropWithinSource = false;
		g.DragDropWithinTarget = false;
		g.DragDropHoldJustPressedId = 0;

		// Keep the source alive so even if the source disappears our state is consistent.
		if ( g.DragDropActive && g.DragDropPayload.SourceId == g.ActiveId )
			KeepAliveID( g.DragDropPayload.SourceId );
	}

	internal static void EndFrameDragDrop()
	{
		var g = G;

		// Elapse payload (if delivered, or if the source stopped being submitted)
		if ( g.DragDropActive )
		{
			bool isDelivered = g.DragDropPayload.Delivery;
			bool isElapsed = g.DragDropSourceFrameCount + 1 < g.FrameCount
				&& ((g.DragDropSourceFlags & ImGuiDragDropFlags.PayloadAutoExpire) != 0 || g.DragDropMouseButton == -1 || !IsMouseDown( (ImGuiMouseButton)g.DragDropMouseButton ));
			if ( isDelivered || isElapsed )
				ClearDragDrop();
		}

		// Fallback for a source that stopped submitting its tooltip.
		if ( g.DragDropActive && g.DragDropSourceFrameCount + 1 < g.FrameCount && (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceNoPreviewTooltip) == 0 )
		{
			g.DragDropWithinSource = true;
			SetTooltip( "..." );
			g.DragDropWithinSource = false;
		}
	}

	internal static void ClearDragDrop()
	{
		var g = G;
		g.DragDropActive = false;
		g.DragDropPayload.Clear();
		g.DragDropAcceptFlags = ImGuiDragDropFlags.None;
		g.DragDropAcceptIdCurr = g.DragDropAcceptIdPrev = 0;
		g.DragDropAcceptIdCurrRectSurface = float.MaxValue;
		g.DragDropAcceptFrameCount = -1;
		g.DragDropMouseButton = -1;
	}

	public static bool IsDragDropActive() => G.DragDropActive;

	/// <summary>
	/// Call after submitting an item which may be dragged. When this returns true, you can call SetDragDropPayload() + EndDragDropSource().
	/// </summary>
	public static bool BeginDragDropSource( ImGuiDragDropFlags flags = ImGuiDragDropFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;

		int mouseButton = (int)ImGuiMouseButton.Left;
		bool sourceDragActive;
		int sourceId;
		int sourceParentId = 0;

		if ( (flags & ImGuiDragDropFlags.SourceExtern) == 0 )
		{
			sourceId = g.LastItemData.ID;
			if ( sourceId != 0 )
			{
				// Common path: items with an ID
				if ( g.ActiveId != sourceId )
					return false;
				if ( g.ActiveIdMouseButton != -1 )
					mouseButton = g.ActiveIdMouseButton;
				if ( !g.IO.MouseDown[mouseButton] || window.SkipItems )
					return false;
				g.ActiveIdAllowOverlap = false;
			}
			else
			{
				// Uncommon path: items without an ID (e.g. Text(), Image())
				if ( !g.IO.MouseDown[mouseButton] || window.SkipItems )
					return false;
				if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredRect) == 0 && (g.ActiveId == 0 || g.ActiveIdWindow != window) )
					return false;
				if ( (flags & ImGuiDragDropFlags.SourceAllowNullID) == 0 )
				{
					Log.Warning( "ImGui: BeginDragDropSource() on an item without ID requires ImGuiDragDropFlags.SourceAllowNullID" );
					return false;
				}

				// Build a throwaway ID from the ID stack + the item rectangle. It won't survive the item moving.
				sourceId = g.LastItemData.ID = window.GetIDFromRectangle( g.LastItemData.Rect );
				KeepAliveID( sourceId );
				bool isHovered = ItemHoverable( g.LastItemData.Rect, sourceId, g.LastItemData.InFlags );
				if ( isHovered && g.IO.MouseClicked[mouseButton] )
				{
					SetActiveID( sourceId, window );
					g.ActiveIdMouseButton = mouseButton;
					FocusWindow( window );
				}
				if ( g.ActiveId == sourceId )
					g.ActiveIdAllowOverlap = isHovered;
			}
			if ( g.ActiveId != sourceId )
				return false;
			sourceParentId = window.IDStack[^1];
			sourceDragActive = IsMouseDragging( (ImGuiMouseButton)mouseButton );
		}
		else
		{
			// External source (e.g. driven by the game): always active while the button is down.
			window = null;
			sourceId = ImHashStr( "#SourceExtern", 0 );
			sourceDragActive = true;
			mouseButton = g.IO.MouseDown[0] ? 0 : -1;
			KeepAliveID( sourceId );
			SetActiveID( sourceId, null );
		}

		if ( !sourceDragActive )
			return false;

		// Activate drag and drop
		if ( !g.DragDropActive )
		{
			ClearDragDrop();
			var payload = g.DragDropPayload;
			payload.SourceId = sourceId;
			payload.SourceParentId = sourceParentId;
			g.DragDropActive = true;
			g.DragDropSourceFlags = flags;
			g.DragDropMouseButton = mouseButton;
			if ( payload.SourceId == g.ActiveId )
				g.ActiveIdNoClearOnFocusLoss = true;
		}
		g.DragDropSourceFrameCount = g.FrameCount;
		g.DragDropWithinSource = true;

		if ( (flags & ImGuiDragDropFlags.SourceNoPreviewTooltip) == 0 )
		{
			// Drag and drop tooltips are offset from the mouse cursor and semi-transparent.
			var tooltipPos = g.IO.MousePos + new Vector2( 16, 10 ) * g.Style.MouseCursorScale;
			SetNextWindowPos( tooltipPos );
			SetNextWindowBgAlpha( g.Style.Colors[(int)ImGuiCol.PopupBg].w * 0.60f );
			BeginTooltipEx();
			if ( g.DragDropAcceptIdPrev != 0 && (g.DragDropAcceptFlags & ImGuiDragDropFlags.AcceptNoPreviewTooltip) != 0 )
			{
				// The target asked us not to display the tooltip: keep it alive but hidden.
				g.CurrentWindow.Hidden = true;
				g.CurrentWindow.HiddenFramesCanSkipItems = 1;
			}
		}

		if ( (flags & ImGuiDragDropFlags.SourceNoDisableHover) == 0 && (flags & ImGuiDragDropFlags.SourceExtern) == 0 )
			g.LastItemData.StatusFlags &= ~ImGuiItemStatusFlags.HoveredRect;

		return true;
	}

	/// <summary>Only call EndDragDropSource() if BeginDragDropSource() returns true!</summary>
	public static void EndDragDropSource()
	{
		var g = G;
		if ( !g.DragDropActive || !g.DragDropWithinSource )
		{
			Log.Warning( "ImGui: EndDragDropSource() called without BeginDragDropSource()" );
			return;
		}

		if ( (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceNoPreviewTooltip) == 0 )
			EndTooltip();

		// Discard the drag if SetDragDropPayload() was never called.
		if ( g.DragDropPayload.DataFrameCount == -1 )
			ClearDragDrop();
		g.DragDropWithinSource = false;
	}

	/// <summary>
	/// Set the payload of the current drag source. type is a user defined tag (types starting with '_' are reserved).
	/// Returns true when the payload has been accepted by a target.
	/// </summary>
	public static bool SetDragDropPayload( string type, object data, ImGuiCond cond = ImGuiCond.None )
	{
		var g = G;
		var payload = g.DragDropPayload;
		if ( cond == ImGuiCond.None )
			cond = ImGuiCond.Always;

		if ( cond == ImGuiCond.Always || payload.DataFrameCount == -1 )
		{
			payload.DataType = type;
			payload.Data = data;
		}
		payload.DataFrameCount = g.FrameCount;

		return g.DragDropAcceptFrameCount == g.FrameCount || g.DragDropAcceptFrameCount == g.FrameCount - 1;
	}

	public static bool SetDragDropPayload<T>( string type, T data, ImGuiCond cond = ImGuiCond.None )
		=> SetDragDropPayload( type, (object)data, cond );

	/// <summary>Call after submitting an item that may receive a payload. If this returns true, you can call AcceptDragDropPayload() + EndDragDropTarget().</summary>
	public static bool BeginDragDropTarget()
	{
		var g = G;
		if ( !g.DragDropActive )
			return false;

		var window = g.CurrentWindow;
		if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredRect) == 0 )
			return false;
		var hoveredWindow = g.HoveredWindowUnderMovingWindow;
		if ( hoveredWindow is null || window.RootWindow != hoveredWindow.RootWindow || window.SkipItems )
			return false;

		var displayRect = (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HasDisplayRect) != 0 ? g.LastItemData.DisplayRect : g.LastItemData.Rect;
		int id = g.LastItemData.ID;
		if ( id == 0 )
		{
			id = window.GetIDFromRectangle( displayRect );
			KeepAliveID( id );
		}
		if ( g.DragDropPayload.SourceId == id )
			return false;

		g.DragDropTargetRect = displayRect;
		g.DragDropTargetClipRect = window.ClipRect;
		g.DragDropTargetId = id;
		g.DragDropWithinTarget = true;
		return true;
	}

	/// <summary>
	/// Accept contents of a given type. Returns the payload when it is delivered (or while previewing with AcceptBeforeDelivery), else null.
	/// </summary>
	public static ImGuiPayload AcceptDragDropPayload( string type, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None )
	{
		var g = G;
		var payload = g.DragDropPayload;
		if ( !g.DragDropActive || payload.DataFrameCount == -1 )
			return null;
		if ( type is not null && !payload.IsDataType( type ) )
			return null;

		// Accept the smallest drag target bounding box, so drag targets can be nested.
		bool wasAcceptedPreviously = g.DragDropAcceptIdPrev == g.DragDropTargetId;
		var r = g.DragDropTargetRect;
		float rSurface = r.Width * r.Height;
		if ( rSurface > g.DragDropAcceptIdCurrRectSurface )
			return null;

		g.DragDropAcceptFlags = flags;
		g.DragDropAcceptIdCurr = g.DragDropTargetId;
		g.DragDropAcceptIdCurrRectSurface = rSurface;

		payload.Preview = wasAcceptedPreviously;
		flags |= g.DragDropSourceFlags & ImGuiDragDropFlags.AcceptNoDrawDefaultRect;
		if ( (flags & ImGuiDragDropFlags.AcceptNoDrawDefaultRect) == 0 && payload.Preview )
			RenderDragDropTargetRect( r, g.DragDropTargetClipRect );

		g.DragDropAcceptFrameCount = g.FrameCount;
		if ( (g.DragDropSourceFlags & ImGuiDragDropFlags.SourceExtern) != 0 && g.DragDropMouseButton == -1 )
			payload.Delivery = wasAcceptedPreviously && g.DragDropSourceFrameCount < g.FrameCount;
		else
			payload.Delivery = wasAcceptedPreviously && g.DragDropMouseButton >= 0 && !IsMouseDown( (ImGuiMouseButton)g.DragDropMouseButton );

		if ( !payload.Delivery && (flags & ImGuiDragDropFlags.AcceptBeforeDelivery) == 0 )
			return null;

		return payload;
	}

	internal static void RenderDragDropTargetRect( ImRect bb, ImRect itemClipRect )
	{
		var g = G;
		var window = g.CurrentWindow;
		var bbDisplay = bb;
		bbDisplay.ClipWith( itemClipRect );
		bbDisplay.Expand( 3.5f );
		bool pushClipRect = !window.ClipRect.Contains( bbDisplay );
		if ( pushClipRect )
			window.DrawList.PushClipRectFullScreen();
		window.DrawList.AddRect( bbDisplay.Min, bbDisplay.Max, GetColorU32Internal( ImGuiCol.DragDropTarget ), 0.0f, ImDrawFlags.None, 2.0f );
		if ( pushClipRect )
			window.DrawList.PopClipRect();
	}

	/// <summary>Only call EndDragDropTarget() if BeginDragDropTarget() returns true!</summary>
	public static void EndDragDropTarget()
	{
		var g = G;
		if ( !g.DragDropActive || !g.DragDropWithinTarget )
		{
			Log.Warning( "ImGui: EndDragDropTarget() called without BeginDragDropTarget()" );
			return;
		}
		g.DragDropWithinTarget = false;

		// Clear the payload right after delivery.
		if ( g.DragDropPayload.Delivery )
			ClearDragDrop();
	}

	/// <summary>Peek directly into the current payload from anywhere. Returns null when drag and drop is finished or inactive.</summary>
	public static ImGuiPayload GetDragDropPayload()
	{
		var g = G;
		return g.DragDropActive && g.DragDropPayload.DataFrameCount != -1 ? g.DragDropPayload : null;
	}
}

#endregion

#region ImGui (Widgets/ImGui.InputScalar)

public static partial class ImGui
{
	/// <summary>Numeric text input over a double. step &lt;= 0 hides the +/- buttons.</summary>
	internal static bool InputScalarCore( string label, ref double value, double step, double stepFast, string format, ImGuiInputTextFlags flags, bool isInteger )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		if ( string.IsNullOrEmpty( format ) )
			format = isInteger ? "%d" : "%.3f";

		string buf = (flags & ImGuiInputTextFlags.DisplayEmptyRefVal) != 0 && value == 0.0 ? "" : FormatNumber( format, value, isInteger );

		flags |= ImGuiInputTextFlags.AutoSelectAll;
		if ( (flags & (ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsScientific)) == 0 )
			flags |= InputScalarDefaultCharsFilter( isInteger, format );
		flags &= ~ImGuiInputTextFlags.EnterReturnsTrue;

		bool backupNoMark = g.InputTextNoMarkEdited;
		g.InputTextNoMarkEdited = true;

		bool valueChanged = false;
		double old = value;
		if ( step <= 0.0 )
		{
			if ( InputText( label, ref buf, flags ) )
				valueChanged = ApplyText( buf, ref value, format, isInteger, flags );
		}
		else
		{
			float buttonSize = GetFrameHeight();
			BeginGroup();
			PushID( label );
			SetNextItemWidth( MathF.Max( 1.0f, CalcItemWidth() - (buttonSize + style.ItemInnerSpacing.x) * 2 ) );
			if ( InputText( "", ref buf, flags ) )
				valueChanged = ApplyText( buf, ref value, format, isInteger, flags );

			var backupFramePadding = style.FramePadding;
			style.FramePadding = new Vector2( style.FramePadding.y, style.FramePadding.y );
			bool readOnly = (flags & ImGuiInputTextFlags.ReadOnly) != 0;
			if ( readOnly )
				BeginDisabled();
			PushItemFlag( ImGuiItemFlags.ButtonRepeat, true );
			SameLine( 0, style.ItemInnerSpacing.x );
			if ( ButtonEx( "-", new Vector2( buttonSize, buttonSize ) ) )
			{
				value -= g.IO.KeyCtrl && stepFast > 0 ? stepFast : step;
				valueChanged = true;
			}
			SameLine( 0, style.ItemInnerSpacing.x );
			if ( ButtonEx( "+", new Vector2( buttonSize, buttonSize ) ) )
			{
				value += g.IO.KeyCtrl && stepFast > 0 ? stepFast : step;
				valueChanged = true;
			}
			PopItemFlag();
			if ( readOnly )
				EndDisabled();

			if ( FindRenderedTextEnd( label ) > 0 )
			{
				SameLine( 0, style.ItemInnerSpacing.x );
				TextEx( label, true );
			}
			style.FramePadding = backupFramePadding;
			PopID();
			EndGroup();
		}

		g.InputTextNoMarkEdited = backupNoMark;
		valueChanged = valueChanged && value != old;
		if ( valueChanged )
			MarkItemEdited( g.LastItemData.ID );
		return valueChanged;
	}

	private static bool ApplyText( string buf, ref double value, string format, bool isInteger, ImGuiInputTextFlags flags )
	{
		if ( string.IsNullOrWhiteSpace( buf ) )
		{
			if ( (flags & ImGuiInputTextFlags.ParseEmptyRefVal) != 0 )
			{
				bool changed = value != 0.0;
				value = 0.0;
				return changed;
			}
			return false;
		}
		double old = value;
		double v = ApplyExpression( buf, old, format );
		if ( isInteger )
			v = Math.Round( v, MidpointRounding.AwayFromZero );
		value = v;
		return v != old;
	}

	#region Generic
	public static bool InputScalar<T>( string label, ref T v, T? step = null, T? stepFast = null, string format = null, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None ) where T : struct, INumber<T>
	{
		bool isInteger = IsIntegerType<T>();
		double d = ToDouble( v );
		bool changed = InputScalarCore( label, ref d, step.HasValue ? ToDouble( step.Value ) : 0, stepFast.HasValue ? ToDouble( stepFast.Value ) : 0, format, flags, isInteger );
		if ( changed )
		{
			var nv = FromDouble<T>( d );
			changed = nv != v;
			v = nv;
		}
		return changed;
	}

	public static bool InputScalarN<T>( string label, T[] v, T? step = null, T? stepFast = null, string format = null, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None ) where T : struct, INumber<T>
		=> ScalarNCore( label, v.Length, i => InputScalar( "", ref v[i], step, stepFast, format, flags ) );
	#endregion

	#region Float
	public static bool InputFloat( string label, ref float v, float step = 0f, float stepFast = 0f, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		double d = v;
		bool changed = InputScalarCore( label, ref d, step, stepFast, format, flags, false );
		if ( changed )
			v = (float)d;
		return changed;
	}

	private static bool InputFloatN( string label, float[] v, int n, string format, ImGuiInputTextFlags flags )
		=> ScalarNCore( label, Math.Min( n, v.Length ), i => InputFloat( "", ref v[i], 0f, 0f, format, flags ) );

	public static bool InputFloat2( string label, float[] v, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None ) => InputFloatN( label, v, 2, format, flags );
	public static bool InputFloat3( string label, float[] v, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None ) => InputFloatN( label, v, 3, format, flags );
	public static bool InputFloat4( string label, float[] v, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None ) => InputFloatN( label, v, 4, format, flags );

	public static bool InputFloat2( string label, ref Vector2 v, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		var a = new[] { v.x, v.y };
		bool changed = InputFloatN( label, a, 2, format, flags );
		if ( changed ) v = new Vector2( a[0], a[1] );
		return changed;
	}

	public static bool InputFloat3( string label, ref Vector3 v, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		var a = new[] { v.x, v.y, v.z };
		bool changed = InputFloatN( label, a, 3, format, flags );
		if ( changed ) v = new Vector3( a[0], a[1], a[2] );
		return changed;
	}

	public static bool InputFloat4( string label, ref Vector4 v, string format = "%.3f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		var a = new[] { v.x, v.y, v.z, v.w };
		bool changed = InputFloatN( label, a, 4, format, flags );
		if ( changed ) v = new Vector4( a[0], a[1], a[2], a[3] );
		return changed;
	}

	public static bool InputDouble( string label, ref double v, double step = 0, double stepFast = 0, string format = "%.6f", ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
		=> InputScalarCore( label, ref v, step, stepFast, format, flags, false );
	#endregion

	#region Int
	public static bool InputInt( string label, ref int v, int step = 1, int stepFast = 100, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		string format = (flags & ImGuiInputTextFlags.CharsHexadecimal) != 0 ? "%08X" : "%d";
		double d = v;
		bool changed = InputScalarCore( label, ref d, step, stepFast, format, flags, true );
		if ( changed )
		{
			int nv = (int)Math.Clamp( Math.Round( d ), int.MinValue, int.MaxValue );
			changed = nv != v;
			v = nv;
		}
		return changed;
	}

	private static bool InputIntN( string label, int[] v, int n, ImGuiInputTextFlags flags )
		=> ScalarNCore( label, Math.Min( n, v.Length ), i => InputInt( "", ref v[i], 0, 0, flags ) );

	public static bool InputInt2( string label, int[] v, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None ) => InputIntN( label, v, 2, flags );
	public static bool InputInt3( string label, int[] v, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None ) => InputIntN( label, v, 3, flags );
	public static bool InputInt4( string label, int[] v, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None ) => InputIntN( label, v, 4, flags );

	public static bool InputInt2( string label, ref Vector2Int v, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		var a = new[] { v.x, v.y };
		bool changed = InputIntN( label, a, 2, flags );
		if ( changed ) v = new Vector2Int( a[0], a[1] );
		return changed;
	}

	public static bool InputInt3( string label, ref Vector3Int v, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None )
	{
		var a = new[] { v.x, v.y, v.z };
		bool changed = InputIntN( label, a, 3, flags );
		if ( changed ) v = new Vector3Int( a[0], a[1], a[2] );
		return changed;
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.InputText)

public static partial class ImGui
{
	internal static bool IsTextInputActive()
	{
		var g = G;
		return g is not null && g.ActiveId != 0 && g.InputTextState.ID == g.ActiveId;
	}

	internal static bool TempInputIsActive( int id )
	{
		var g = G;
		return g.ActiveId == id && g.TempInputId == id;
	}

	private static bool _clipboardProviderInstalled;

	private static void EnsureClipboardProvider()
	{
		if ( _clipboardProviderInstalled )
			return;
		_clipboardProviderInstalled = true;
		ImGuiSystem.ClipboardCopyProvider = InputTextClipboardCopy;
	}

	/// <summary>Called by the input panel when the OS asks for clipboard contents (Ctrl+C / Ctrl+X).</summary>
	private static string InputTextClipboardCopy( bool cut )
	{
		var g = G;
		if ( g is null || !IsTextInputActive() )
			return null;
		var state = g.InputTextState;
		if ( !state.HasSelection )
			return null;
		bool isPassword = (state.Flags & ImGuiInputTextFlags.Password) != 0;
		if ( isPassword )
			return null;
		string selected = state.Text.Substring( state.SelMin, state.SelMax - state.SelMin );
		if ( cut && (state.Flags & ImGuiInputTextFlags.ReadOnly) == 0 )
		{
			state.PushUndo();
			int min = state.SelMin;
			state.Text = state.Text.Remove( min, state.SelMax - min );
			state.Cursor = state.SelectStart = min;
			state.Edited = true;
		}
		g.InputTextClipboardHandledByPanel = true;
		return selected;
	}

	#region Public API
	public static bool InputText( string label, ref string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null )
		=> InputTextEx( label, null, ref text, 0, Vector2.Zero, flags & ~ImGuiInputTextFlags.Multiline, callback );

	/// <summary>Text input limited to <paramref name="maxLength"/> characters (equivalent of Dear ImGui's buffer size).</summary>
	public static bool InputText( string label, ref string text, int maxLength, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null )
		=> InputTextEx( label, null, ref text, maxLength, Vector2.Zero, flags & ~ImGuiInputTextFlags.Multiline, callback );

	public static bool InputTextMultiline( string label, ref string text, Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null )
		=> InputTextEx( label, null, ref text, 0, size, flags | ImGuiInputTextFlags.Multiline, callback );

	public static bool InputTextWithHint( string label, string hint, ref string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null )
		=> InputTextEx( label, hint, ref text, 0, Vector2.Zero, flags & ~ImGuiInputTextFlags.Multiline, callback );
	#endregion

	/// <summary>
	/// Turn an existing item (e.g. a drag or slider) into a text field for keyboard input. Used for Ctrl+click / double-click editing.
	/// </summary>
	internal static bool TempInputText( ImRect bb, int id, string label, ref string text, ImGuiInputTextFlags flags )
	{
		var g = G;
		bool init = g.TempInputId != id;
		if ( init )
			ClearActiveID();

		g.CurrentWindow.DC.CursorPos = bb.Min;
		bool valueChanged = InputTextEx( label, null, ref text, 0, bb.Size, flags | ImGuiInputTextFlags.MergedItem, null, init, id, bb );
		if ( init )
			g.TempInputId = g.ActiveId;
		return valueChanged;
	}

	/// <summary>Text-input version of a numeric widget. Returns true when the value changed.</summary>
	internal static bool TempInputScalar( ImRect bb, int id, string label, ref double value, string format, bool isInteger, bool hasClamp, double clampMin, double clampMax )
	{
		var g = G;
		var fmt = ParseFormatTrimDecorations( format );
		if ( string.IsNullOrEmpty( fmt ) )
			fmt = isInteger ? "%d" : "%.3f";
		string buf = FormatNumber( fmt, value, isInteger ).Trim();

		var flags = ImGuiInputTextFlags.AutoSelectAll | InputScalarDefaultCharsFilter( isInteger, format );
		bool valueChanged = false;
		if ( TempInputText( bb, id, label, ref buf, flags ) )
		{
			double old = value;
			double v = ApplyExpression( buf, old, format );
			if ( isInteger )
				v = Math.Round( v, MidpointRounding.AwayFromZero );
			if ( hasClamp )
			{
				if ( clampMin < clampMax ) v = Math.Clamp( v, clampMin, clampMax );
				else if ( clampMin == clampMax ) v = clampMin;
			}
			value = v;
			valueChanged = v != old;
			if ( valueChanged )
				MarkItemEdited( id );
		}
		return valueChanged;
	}

	internal static ImGuiInputTextFlags InputScalarDefaultCharsFilter( bool isInteger, string format )
	{
		if ( !isInteger )
			return ImGuiInputTextFlags.CharsScientific;
		if ( FormatIsHex( format ) )
			return ImGuiInputTextFlags.CharsHexadecimal;
		return ImGuiInputTextFlags.CharsDecimal;
	}

	#region Character filtering / word boundaries
	private static bool InputTextFilterCharacter( ref char c, ImGuiInputTextFlags flags, ImGuiInputTextCallback callback, bool fromClipboard, ImGuiInputTextState state )
	{
		bool applyNamedFilters = true;
		if ( c < 0x20 )
		{
			bool pass = (c == '\n' && (flags & ImGuiInputTextFlags.Multiline) != 0) || (c == '\t' && (flags & ImGuiInputTextFlags.AllowTabInput) != 0);
			if ( !pass )
				return false;
			applyNamedFilters = false;
		}

		if ( !fromClipboard )
		{
			if ( c == 127 )
				return false;
			if ( c >= 0xE000 && c <= 0xF8FF )
				return false;
		}

		if ( applyNamedFilters && (flags & (ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase | ImGuiInputTextFlags.CharsNoBlank | ImGuiInputTextFlags.CharsScientific)) != 0 )
		{
			if ( (flags & ImGuiInputTextFlags.CharsDecimal) != 0 )
				if ( !(c >= '0' && c <= '9') && c != '.' && c != '-' && c != '+' && c != '*' && c != '/' )
					return false;
			if ( (flags & ImGuiInputTextFlags.CharsScientific) != 0 )
				if ( !(c >= '0' && c <= '9') && c != '.' && c != '-' && c != '+' && c != '*' && c != '/' && c != 'e' && c != 'E' )
					return false;
			if ( (flags & ImGuiInputTextFlags.CharsHexadecimal) != 0 )
				if ( !(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f') && !(c >= 'A' && c <= 'F') )
					return false;
			if ( (flags & ImGuiInputTextFlags.CharsUppercase) != 0 && c >= 'a' && c <= 'z' )
				c = (char)(c + ('A' - 'a'));
			if ( (flags & ImGuiInputTextFlags.CharsNoBlank) != 0 && (c == ' ' || c == '\t' || c == '　') )
				return false;
		}

		if ( (flags & ImGuiInputTextFlags.CallbackCharFilter) != 0 && callback is not null )
		{
			var data = new ImGuiInputTextCallbackData
			{
				EventFlag = ImGuiInputTextFlags.CallbackCharFilter,
				EventChar = c,
				Flags = flags,
			};
			data.SetBufSilently( state?.Text );
			if ( callback( data ) != 0 )
				return false;
			if ( data.EventChar == 0 )
				return false;
			c = data.EventChar;
		}
		return true;
	}

	private static bool IsWordSeparator( char c )
		=> char.IsWhiteSpace( c ) || ",;(){}[]|.!?\"'`/\\:<>=+-*&^%$#@~".IndexOf( c ) >= 0;

	private static int WordLeft( string text, int pos )
	{
		pos = Math.Clamp( pos, 0, text.Length );
		while ( pos > 0 && IsWordSeparator( text[pos - 1] ) ) pos--;
		while ( pos > 0 && !IsWordSeparator( text[pos - 1] ) ) pos--;
		return pos;
	}

	private static int WordRight( string text, int pos )
	{
		pos = Math.Clamp( pos, 0, text.Length );
		while ( pos < text.Length && !IsWordSeparator( text[pos] ) ) pos++;
		while ( pos < text.Length && IsWordSeparator( text[pos] ) && text[pos] != '\n' ) pos++;
		return pos;
	}

	private static int LineStart( string text, int pos )
	{
		pos = Math.Clamp( pos, 0, text.Length );
		while ( pos > 0 && text[pos - 1] != '\n' ) pos--;
		return pos;
	}

	private static int LineEnd( string text, int pos )
	{
		pos = Math.Clamp( pos, 0, text.Length );
		while ( pos < text.Length && text[pos] != '\n' ) pos++;
		return pos;
	}
	#endregion

	#region Text layout helpers
	private static float TextWidthPrefix( string line, int count )
	{
		if ( count <= 0 || string.IsNullOrEmpty( line ) )
			return 0f;
		count = Math.Min( count, line.Length );
		return MeasureTextWidth( line.Substring( 0, count ), G.FontPointSize );
	}

	/// <summary>Column within a line closest to a pixel x offset (binary search over prefix widths).</summary>
	private static int ColumnFromX( string line, float x )
	{
		if ( string.IsNullOrEmpty( line ) || x <= 0f )
			return 0;
		int lo = 0, hi = line.Length;
		while ( lo < hi )
		{
			int mid = (lo + hi + 1) / 2;
			if ( TextWidthPrefix( line, mid ) <= x ) lo = mid;
			else hi = mid - 1;
		}
		if ( lo < line.Length )
		{
			float a = TextWidthPrefix( line, lo );
			float b = TextWidthPrefix( line, lo + 1 );
			if ( x - a > b - x )
				lo++;
		}
		return lo;
	}

	/// <summary>Convert a character index to (line, column).</summary>
	private static void IndexToLineCol( string text, int index, out int line, out int col, out int lineStartIdx )
	{
		line = 0;
		lineStartIdx = 0;
		index = Math.Clamp( index, 0, text.Length );
		for ( int i = 0; i < index; i++ )
		{
			if ( text[i] == '\n' )
			{
				line++;
				lineStartIdx = i + 1;
			}
		}
		col = index - lineStartIdx;
	}

	private static int LocateIndex( string displayText, Vector2 rel, bool multiline )
	{
		var g = G;
		if ( !multiline )
			return ColumnFromX( displayText, rel.x );

		var lines = displayText.Split( '\n' );
		int lineIdx = Math.Clamp( (int)MathF.Floor( rel.y / g.FontSize ), 0, lines.Length - 1 );
		int idx = 0;
		for ( int i = 0; i < lineIdx; i++ )
			idx += lines[i].Length + 1;
		return idx + ColumnFromX( lines[lineIdx], rel.x );
	}
	#endregion

	/// <summary>
	/// Core text input implementation (port of Dear ImGui's InputTextEx).
	/// </summary>
	internal static bool InputTextEx( string label, string hint, ref string text, int maxLength, Vector2 sizeArg, ImGuiInputTextFlags flags, ImGuiInputTextCallback callback,
		bool forceActivate = false, int mergedId = 0, ImRect? mergedBb = null )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		EnsureClipboardProvider();
		text ??= string.Empty;

		var io = g.IO;
		var style = g.Style;

		bool isMultiline = (flags & ImGuiInputTextFlags.Multiline) != 0;
		bool isReadOnly = (flags & ImGuiInputTextFlags.ReadOnly) != 0 || (g.CurrentItemFlags & ImGuiItemFlags.ReadOnly) != 0;
		bool isPassword = (flags & ImGuiInputTextFlags.Password) != 0 && !isMultiline;
		bool isMerged = (flags & ImGuiInputTextFlags.MergedItem) != 0;

		if ( isMultiline )
			BeginGroup();

		int id = isMerged && mergedId != 0 ? mergedId : window.GetID( label );
		var labelSize = CalcTextSize( label, true );
		var frameSize = CalcItemSize( sizeArg, CalcItemWidth(), (isMultiline ? g.FontSize * 8.0f : labelSize.y) + style.FramePadding.y * 2.0f );
		var totalSize = new Vector2( frameSize.x + (labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f), frameSize.y );

		var frameBb = isMerged && mergedBb.HasValue ? mergedBb.Value : new ImRect( window.DC.CursorPos, window.DC.CursorPos + frameSize );
		var totalBb = new ImRect( frameBb.Min, frameBb.Min + totalSize );

		var drawWindow = window;
		var innerSize = frameSize;
		var itemFlagsBackup = ImGuiItemFlags.None;
		var itemStatusBackup = ImGuiItemStatusFlags.None;

		if ( isMultiline )
		{
			var backupPos = window.DC.CursorPos;
			ItemSize( totalBb, style.FramePadding.y );
			if ( !ItemAdd( totalBb, id, frameBb, ImGuiItemFlags.Inputable ) )
			{
				EndGroup();
				return false;
			}
			itemFlagsBackup = g.LastItemData.InFlags;
			itemStatusBackup = g.LastItemData.StatusFlags;
			window.DC.CursorPos = backupPos;

			PushStyleColor( ImGuiCol.ChildBg, style.Colors[(int)ImGuiCol.FrameBg] );
			PushStyleVar( ImGuiStyleVar.ChildRounding, style.FrameRounding );
			PushStyleVar( ImGuiStyleVar.ChildBorderSize, style.FrameBorderSize );
			PushStyleVar( ImGuiStyleVar.WindowPadding, Vector2.Zero );
			bool childVisible = BeginChildEx( label, id, frameBb.Size, ImGuiChildFlags.Borders, ImGuiWindowFlags.NoMove );
			PopStyleVar( 3 );
			PopStyleColor();
			if ( !childVisible )
			{
				EndChild();
				EndGroup();
				return false;
			}
			drawWindow = g.CurrentWindow;
			drawWindow.DC.CursorPos += style.FramePadding;
			innerSize.x -= drawWindow.ScrollbarSizes.x;
		}
		else if ( !isMerged )
		{
			ItemSize( totalBb, style.FramePadding.y );
			if ( !ItemAdd( totalBb, id, frameBb, ImGuiItemFlags.Inputable ) )
				return false;
			itemFlagsBackup = g.LastItemData.InFlags;
		}
		else
		{
			itemFlagsBackup = g.LastItemData.InFlags;
		}

		bool hovered = ItemHoverable( frameBb, id, itemFlagsBackup );
		if ( hovered )
			g.MouseCursor = ImGuiMouseCursor.TextInput;

		var state = g.InputTextState;
		bool stateIsOurs = state.ID == id;

		bool focusRequestedByCode = g.FocusedTextInputRequestId == id;
		if ( focusRequestedByCode )
			g.FocusedTextInputRequestId = 0;

		bool userClicked = hovered && io.MouseClicked[0];
		int scrollbarId = isMultiline ? drawWindow.GetID( "#SCROLLY" ) : 0;
		bool userScrollActive = isMultiline && g.ActiveId == scrollbarId && scrollbarId != 0;
		bool userScrollFinish = isMultiline && g.ActiveIdPreviousFrame == scrollbarId && scrollbarId != 0 && g.ActiveId != scrollbarId && stateIsOurs;

		bool initMakeActive = userClicked || userScrollFinish || focusRequestedByCode || forceActivate;
		bool initState = initMakeActive || userScrollActive;
		bool selectAll = false;

		if ( initState && g.ActiveId != id )
		{
			bool recycle = stateIsOurs && state.Text == text;
			if ( !recycle )
			{
				state.Text = text;
				state.Cursor = state.SelectStart = 0;
				state.ScrollX = 0f;
				state.UndoStack.Clear();
				state.RedoStack.Clear();
			}
			state.ID = id;
			state.InitialText = text;
			state.Flags = flags;
			state.MaxLength = maxLength;
			state.Edited = false;
			state.CursorAnim = 0f;
			stateIsOurs = true;

			if ( (flags & ImGuiInputTextFlags.AutoSelectAll) != 0 || focusRequestedByCode )
				selectAll = true;
			if ( isMerged && forceActivate )
				selectAll = true;
			if ( !recycle && !selectAll )
				state.Cursor = state.SelectStart = text.Length;
		}

		if ( g.ActiveId != id && initMakeActive )
		{
			SetActiveID( id, window );
			FocusWindow( window );
		}

		bool clearActiveId = false;
		if ( g.ActiveId == id && io.MouseClicked[0] && !initState && !initMakeActive && !hovered )
			clearActiveId = true;

		bool valueChanged = false;
		bool validated = false;
		bool renderCursor = g.ActiveId == id || userScrollActive;
		bool renderSelection = renderCursor;

		if ( g.ActiveId == id && stateIsOurs )
		{
			state.Flags = flags;
			state.MaxLength = maxLength;
			state.ClampPositions();
			g.WantTextInputNextFrame = 1;
			// Allow clicking other widgets while we are focused (but not while selecting with the mouse).
			g.ActiveIdAllowOverlap = !io.MouseDown[0];
			KeepAliveID( id );

			string displayText = isPassword ? new string( '*', state.Text.Length ) : state.Text;
			var textOrigin = isMultiline
				? drawWindow.DC.CursorPos
				: frameBb.Min + style.FramePadding - new Vector2( state.ScrollX, 0f );
			if ( !isMultiline && labelSize.y > 0 )
				textOrigin.y = frameBb.Min.y + style.FramePadding.y;
			var mouseRel = io.MousePos - textOrigin;

			// Mouse handling
			if ( selectAll )
			{
				state.SelectAll();
				state.SelectedAllMouseLock = true;
				state.CursorFollow = true;
			}
			else if ( hovered && io.MouseClicked[0] && io.MouseClickedCount[0] >= 2 && !io.KeyShift )
			{
				int idx = LocateIndex( displayText, mouseRel, isMultiline );
				if ( io.MouseClickedCount[0] >= 3 )
				{
					state.SelectStart = LineStart( state.Text, idx );
					state.Cursor = LineEnd( state.Text, idx );
				}
				else
				{
					int ws = idx, we = idx;
					while ( ws > 0 && !IsWordSeparator( state.Text[ws - 1] ) ) ws--;
					while ( we < state.Text.Length && !IsWordSeparator( state.Text[we] ) ) we++;
					state.SelectStart = ws;
					state.Cursor = we;
				}
				state.SelectedAllMouseLock = true;
				state.CursorAnim = -0.30f;
			}
			else if ( io.MouseClicked[0] && !state.SelectedAllMouseLock )
			{
				if ( hovered )
				{
					int idx = LocateIndex( displayText, mouseRel, isMultiline );
					state.Cursor = idx;
					if ( !io.KeyShift )
						state.SelectStart = idx;
					state.CursorAnim = -0.30f;
					state.CursorFollow = true;
				}
			}
			else if ( io.MouseDown[0] && !state.SelectedAllMouseLock && (io.MouseDelta.x != 0f || io.MouseDelta.y != 0f) )
			{
				state.Cursor = LocateIndex( displayText, mouseRel, isMultiline );
				state.CursorAnim = -0.30f;
				state.CursorFollow = true;
			}
			if ( state.SelectedAllMouseLock && !io.MouseDown[0] )
				state.SelectedAllMouseLock = false;

			// Keyboard: typed characters
			if ( !isReadOnly )
			{
				foreach ( var ch in io.InputQueueCharacters )
				{
					if ( ch == '\n' || ch == '\r' || ch == '\t' )
						continue; // handled through keys
					char c = ch;
					if ( !InputTextFilterCharacter( ref c, flags, callback, false, state ) )
						continue;
					InsertText( state, c.ToString(), flags );
				}
			}

			// Keyboard: editing keys
			string beforeKeys = state.Text;
			foreach ( var k in io.InputQueueKeys )
			{
				bool shift = k.Shift;
				bool ctrl = k.Ctrl;
				switch ( k.Key )
				{
					case ImGuiKey.LeftArrow:
						if ( state.HasSelection && !shift )
							state.Cursor = state.SelMin;
						else
							state.Cursor = ctrl ? WordLeft( state.Text, state.Cursor ) : Math.Max( 0, state.Cursor - 1 );
						if ( !shift ) state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.RightArrow:
						if ( state.HasSelection && !shift )
							state.Cursor = state.SelMax;
						else
							state.Cursor = ctrl ? WordRight( state.Text, state.Cursor ) : Math.Min( state.Text.Length, state.Cursor + 1 );
						if ( !shift ) state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.UpArrow:
					case ImGuiKey.DownArrow:
						if ( isMultiline )
						{
							MoveCursorVertical( state, displayText, k.Key == ImGuiKey.UpArrow ? -1 : 1 );
							if ( !shift ) state.ClearSelection();
						}
						else if ( (flags & ImGuiInputTextFlags.CallbackHistory) != 0 && callback is not null )
						{
							RunCallback( state, flags, callback, ImGuiInputTextFlags.CallbackHistory, k.Key );
						}
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.PageUp:
					case ImGuiKey.PageDown:
						if ( isMultiline )
						{
							int pageLines = Math.Max( 1, (int)(innerSize.y / g.FontSize) - 1 );
							for ( int n = 0; n < pageLines; n++ )
								MoveCursorVertical( state, displayText, k.Key == ImGuiKey.PageUp ? -1 : 1 );
							if ( !shift ) state.ClearSelection();
							state.CursorFollow = true;
						}
						break;
					case ImGuiKey.Home:
						state.Cursor = ctrl || !isMultiline ? 0 : LineStart( state.Text, state.Cursor );
						if ( !shift ) state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.End:
						state.Cursor = ctrl || !isMultiline ? state.Text.Length : LineEnd( state.Text, state.Cursor );
						if ( !shift ) state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.Delete:
						if ( isReadOnly ) break;
						state.PushUndo();
						if ( state.HasSelection )
							DeleteSelection( state );
						else if ( state.Cursor < state.Text.Length )
						{
							int end = ctrl ? WordRight( state.Text, state.Cursor ) : state.Cursor + 1;
							state.Text = state.Text.Remove( state.Cursor, end - state.Cursor );
						}
						state.ClearSelection();
						state.CursorAnim = -0.30f;
						break;
					case ImGuiKey.Backspace:
						if ( isReadOnly ) break;
						state.PushUndo();
						if ( state.HasSelection )
							DeleteSelection( state );
						else if ( state.Cursor > 0 )
						{
							int start = ctrl ? WordLeft( state.Text, state.Cursor ) : state.Cursor - 1;
							state.Text = state.Text.Remove( start, state.Cursor - start );
							state.Cursor = start;
						}
						state.ClearSelection();
						state.CursorAnim = -0.30f;
						state.CursorFollow = true;
						break;
					case ImGuiKey.Enter:
					case ImGuiKey.KeypadEnter:
						{
							bool ctrlEnterForNewLine = (flags & ImGuiInputTextFlags.CtrlEnterForNewLine) != 0;
							if ( !isMultiline || (ctrlEnterForNewLine && !ctrl) || (!ctrlEnterForNewLine && ctrl) )
							{
								validated = true;
								if ( !io.ConfigInputTextEnterKeepActive || !isMultiline )
									clearActiveId = !io.ConfigInputTextEnterKeepActive;
								if ( io.ConfigInputTextEnterKeepActive && !isMultiline )
								{
									state.SelectAll();
									clearActiveId = false;
								}
							}
							else if ( !isReadOnly )
							{
								char c = '\n';
								if ( InputTextFilterCharacter( ref c, flags, callback, false, state ) )
								{
									state.PushUndo();
									InsertText( state, c.ToString(), flags );
								}
							}
							break;
						}
					case ImGuiKey.Escape:
						if ( (flags & ImGuiInputTextFlags.EscapeClearsAll) != 0 )
						{
							if ( state.Text.Length > 0 && !isReadOnly )
							{
								state.PushUndo();
								state.Text = string.Empty;
								state.Cursor = state.SelectStart = 0;
							}
							else
							{
								clearActiveId = true;
								renderCursor = renderSelection = false;
							}
						}
						else
						{
							if ( !isReadOnly && state.Text != state.InitialText )
							{
								state.PushUndo();
								state.Text = state.InitialText;
								state.Cursor = state.SelectStart = state.Text.Length;
							}
							clearActiveId = true;
							renderCursor = renderSelection = false;
						}
						break;
					case ImGuiKey.Tab:
						if ( (flags & ImGuiInputTextFlags.CallbackCompletion) != 0 && callback is not null && !isReadOnly )
						{
							RunCallback( state, flags, callback, ImGuiInputTextFlags.CallbackCompletion, ImGuiKey.Tab );
						}
						else if ( (flags & ImGuiInputTextFlags.AllowTabInput) != 0 && !isReadOnly )
						{
							char c = '\t';
							if ( InputTextFilterCharacter( ref c, flags, callback, false, state ) )
							{
								state.PushUndo();
								InsertText( state, c.ToString(), flags );
							}
						}
						break;
					case ImGuiKey.A:
						if ( ctrl && !shift )
						{
							state.SelectAll();
							state.CursorFollow = true;
						}
						break;
					case ImGuiKey.C:
					case ImGuiKey.X:
						if ( ctrl && !g.InputTextClipboardHandledByPanel && state.HasSelection && !isPassword )
						{
							string sel = state.Text.Substring( state.SelMin, state.SelMax - state.SelMin );
							Sandbox.UI.Clipboard.SetText( sel );
							if ( k.Key == ImGuiKey.X && !isReadOnly )
							{
								state.PushUndo();
								DeleteSelection( state );
							}
						}
						break;
					case ImGuiKey.Z:
						if ( ctrl && !isReadOnly && (flags & ImGuiInputTextFlags.NoUndoRedo) == 0 )
						{
							if ( shift ) Redo( state ); else Undo( state );
						}
						break;
					case ImGuiKey.Y:
						if ( ctrl && !isReadOnly && (flags & ImGuiInputTextFlags.NoUndoRedo) == 0 )
							Redo( state );
						break;
				}
			}
			g.InputTextClipboardHandledByPanel = false;

			// Paste
			if ( io.PastedText is not null && !isReadOnly )
			{
				var sb = new System.Text.StringBuilder();
				foreach ( var ch in io.PastedText )
				{
					char c = ch;
					if ( c == '\r' ) continue;
					if ( c == '\n' && !isMultiline ) { c = ' '; }
					if ( InputTextFilterCharacter( ref c, flags, callback, true, state ) )
						sb.Append( c );
				}
				if ( sb.Length > 0 )
				{
					state.PushUndo();
					InsertText( state, sb.ToString(), flags );
				}
			}

			if ( isReadOnly )
				state.Text = text;

			// Always callback
			if ( (flags & ImGuiInputTextFlags.CallbackAlways) != 0 && callback is not null )
				RunCallback( state, flags, callback, ImGuiInputTextFlags.CallbackAlways, ImGuiKey.None );

			state.ClampPositions();

			if ( state.Text != text && !isReadOnly )
			{
				if ( (flags & ImGuiInputTextFlags.CallbackEdit) != 0 && callback is not null && state.Text != beforeKeys )
					RunCallback( state, flags, callback, ImGuiInputTextFlags.CallbackEdit, ImGuiKey.None );
				text = state.Text;
				valueChanged = true;
			}
		}

		if ( clearActiveId && g.ActiveId == id )
			ClearActiveID();

		// ---- Render ----
		var drawList = drawWindow.DrawList;
		if ( !isMultiline )
		{
			RenderFrame( frameBb.Min, frameBb.Max, GetColorU32Internal( ImGuiCol.FrameBg ), true, style.FrameRounding );
		}

		bool isActive = g.ActiveId == id && stateIsOurs;
		string bufDisplay = isActive || (stateIsOurs && renderCursor) ? state.Text : text;
		if ( isPassword )
			bufDisplay = new string( '*', bufDisplay.Length );

		var clipRect = isMultiline
			? new ImRect( drawWindow.InnerClipRect.Min, drawWindow.InnerClipRect.Max )
			: new ImRect( frameBb.Min.x + style.FramePadding.x * 0.5f, frameBb.Min.y, frameBb.Max.x - style.FramePadding.x * 0.5f, frameBb.Max.y );

		var drawPos = isMultiline ? drawWindow.DC.CursorPos : frameBb.Min + style.FramePadding;
		var lines = bufDisplay.Split( '\n' );
		float textMaxWidth = 0f;
		if ( isMultiline )
		{
			foreach ( var l in lines )
				textMaxWidth = MathF.Max( textMaxWidth, TextWidthPrefix( l, l.Length ) );
		}
		var textSize = new Vector2( textMaxWidth, lines.Length * g.FontSize );

		float scrollX = 0f;
		if ( isActive )
		{
			IndexToLineCol( bufDisplay, state.Cursor, out int curLine, out int curCol, out _ );
			var cursorOffset = new Vector2( TextWidthPrefix( lines[curLine], curCol ), (curLine + 1) * g.FontSize );

			if ( state.CursorFollow )
			{
				if ( !isMultiline && (flags & ImGuiInputTextFlags.NoHorizontalScroll) == 0 )
				{
					float scrollIncrementX = innerSize.x * 0.25f;
					float visibleWidth = innerSize.x - style.FramePadding.x;
					if ( cursorOffset.x < state.ScrollX )
						state.ScrollX = ImTrunc( MathF.Max( 0.0f, cursorOffset.x - scrollIncrementX ) );
					else if ( cursorOffset.x - visibleWidth >= state.ScrollX )
						state.ScrollX = ImTrunc( cursorOffset.x - visibleWidth + scrollIncrementX );
				}
				else
				{
					state.ScrollX = 0f;
				}

				if ( isMultiline )
				{
					float scrollY = drawWindow.Scroll.y;
					if ( cursorOffset.y - g.FontSize < scrollY )
						scrollY = MathF.Max( 0.0f, cursorOffset.y - g.FontSize );
					else if ( cursorOffset.y - (innerSize.y - style.FramePadding.y * 2.0f) >= scrollY )
						scrollY = cursorOffset.y - innerSize.y + style.FramePadding.y * 2.0f;
					float scrollMaxY = MathF.Max( (textSize.y + style.FramePadding.y * 2.0f) - innerSize.y, 0.0f );
					scrollY = Math.Clamp( scrollY, 0.0f, scrollMaxY );
					drawPos.y += drawWindow.Scroll.y - scrollY;
					drawWindow.Scroll = new Vector2( drawWindow.Scroll.x, scrollY );
				}
				state.CursorFollow = false;
			}
			scrollX = isMultiline ? 0f : state.ScrollX;

			// Selection
			if ( renderSelection && state.HasSelection )
			{
				var selCol = GetColorU32Internal( ImGuiCol.TextSelectedBg, renderCursor ? 1.0f : 0.6f );
				IndexToLineCol( bufDisplay, state.SelMin, out int l0, out int c0, out _ );
				IndexToLineCol( bufDisplay, state.SelMax, out int l1, out int c1, out _ );
				drawList.PushClipRect( clipRect.Min, clipRect.Max, true );
				for ( int ln = l0; ln <= l1; ln++ )
				{
					float y = drawPos.y + ln * g.FontSize;
					if ( y > clipRect.Max.y + g.FontSize ) break;
					if ( y + g.FontSize < clipRect.Min.y ) continue;
					string line = lines[ln];
					float x0 = ln == l0 ? TextWidthPrefix( line, c0 ) : 0f;
					float x1 = ln == l1 ? TextWidthPrefix( line, c1 ) : TextWidthPrefix( line, line.Length ) + MathF.Max( 2f, g.FontSize * 0.25f );
					var rMin = new Vector2( drawPos.x - scrollX + x0, y );
					var rMax = new Vector2( drawPos.x - scrollX + x1, y + g.FontSize );
					drawList.AddRectFilled( rMin, rMax, selCol );
				}
				drawList.PopClipRect();
			}

			// Text
			RenderTextLines( drawList, drawPos - new Vector2( scrollX, 0 ), lines, GetColorU32Internal( ImGuiCol.Text ), clipRect );

			// Cursor
			if ( renderCursor )
			{
				state.CursorAnim += io.DeltaTime;
				bool cursorIsVisible = !io.ConfigInputTextCursorBlink || state.CursorAnim <= 0.0f || ImFmod( state.CursorAnim, 1.20f ) <= 0.80f;
				var cursorScreenPos = ImFloor( drawPos + cursorOffset - new Vector2( scrollX, 0f ) );
				var cursorRect = new ImRect( cursorScreenPos.x, cursorScreenPos.y - g.FontSize + 0.5f, cursorScreenPos.x + 1.0f, cursorScreenPos.y - 1.5f );
				if ( cursorIsVisible && cursorRect.Overlaps( clipRect ) )
					drawList.AddLine( cursorRect.Min, cursorRect.BL, GetColorU32Internal( ImGuiCol.InputTextCursor ) );
			}
		}
		else
		{
			if ( bufDisplay.Length == 0 && hint is not null )
			{
				RenderTextLines( drawList, drawPos, LabelText( hint ).Split( '\n' ), GetColorU32Internal( ImGuiCol.TextDisabled ), clipRect );
			}
			else
			{
				RenderTextLines( drawList, drawPos, lines, GetColorU32Internal( ImGuiCol.Text ), clipRect );
			}
		}

		if ( isActive && bufDisplay.Length == 0 && hint is not null )
			RenderTextLines( drawList, drawPos, LabelText( hint ).Split( '\n' ), GetColorU32Internal( ImGuiCol.TextDisabled ), clipRect );

		if ( isMultiline )
		{
			Dummy( new Vector2( textSize.x, textSize.y + style.FramePadding.y ) );
			EndChild();
			EndGroup();
			g.LastItemData.ID = id;
			g.LastItemData.InFlags = itemFlagsBackup;
			g.LastItemData.StatusFlags = itemStatusBackup | (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.HoveredWindow);
		}

		if ( labelSize.x > 0 && !isMerged )
			RenderText( new Vector2( frameBb.Max.x + style.ItemInnerSpacing.x, frameBb.Min.y + style.FramePadding.y ), label );

		if ( valueChanged && !g.InputTextNoMarkEdited )
			MarkItemEdited( id );
		else if ( valueChanged )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Edited;

		if ( (flags & ImGuiInputTextFlags.EnterReturnsTrue) != 0 )
			return validated;
		return valueChanged;
	}

	private static void RenderTextLines( ImDrawList drawList, Vector2 pos, string[] lines, Color32 col, ImRect clip )
	{
		var g = G;
		drawList.PushClipRect( clip.Min, clip.Max, true );
		for ( int i = 0; i < lines.Length; i++ )
		{
			float y = pos.y + i * g.FontSize;
			if ( y > clip.Max.y ) break;
			if ( y + g.FontSize < clip.Min.y ) continue;
			if ( lines[i].Length > 0 )
				drawList.AddText( 0f, new Vector2( pos.x, y ), col, lines[i] );
		}
		drawList.PopClipRect();
	}

	private static void InsertText( ImGuiInputTextState state, string s, ImGuiInputTextFlags flags )
	{
		if ( state.HasSelection )
			DeleteSelection( state );
		if ( state.MaxLength > 0 )
		{
			int room = state.MaxLength - state.Text.Length;
			if ( room <= 0 ) return;
			if ( s.Length > room ) s = s.Substring( 0, room );
		}
		bool overwrite = (flags & ImGuiInputTextFlags.AlwaysOverwrite) != 0;
		if ( overwrite && state.Cursor < state.Text.Length && s.Length == 1 && state.Text[state.Cursor] != '\n' )
			state.Text = state.Text.Remove( state.Cursor, 1 );
		state.Text = state.Text.Insert( state.Cursor, s );
		state.Cursor += s.Length;
		state.SelectStart = state.Cursor;
		state.CursorAnim = -0.30f;
		state.CursorFollow = true;
	}

	private static void DeleteSelection( ImGuiInputTextState state )
	{
		if ( !state.HasSelection ) return;
		int min = state.SelMin;
		state.Text = state.Text.Remove( min, state.SelMax - min );
		state.Cursor = state.SelectStart = min;
	}

	private static void Undo( ImGuiInputTextState state )
	{
		if ( state.UndoStack.Count == 0 ) return;
		state.RedoStack.Add( (state.Text, state.Cursor, state.SelectStart) );
		var e = state.UndoStack[^1];
		state.UndoStack.RemoveAt( state.UndoStack.Count - 1 );
		state.Text = e.Text;
		state.Cursor = e.Cursor;
		state.SelectStart = e.Select;
		state.ClampPositions();
		state.CursorFollow = true;
	}

	private static void Redo( ImGuiInputTextState state )
	{
		if ( state.RedoStack.Count == 0 ) return;
		state.UndoStack.Add( (state.Text, state.Cursor, state.SelectStart) );
		var e = state.RedoStack[^1];
		state.RedoStack.RemoveAt( state.RedoStack.Count - 1 );
		state.Text = e.Text;
		state.Cursor = e.Cursor;
		state.SelectStart = e.Select;
		state.ClampPositions();
		state.CursorFollow = true;
	}

	private static void MoveCursorVertical( ImGuiInputTextState state, string displayText, int dir )
	{
		IndexToLineCol( displayText, state.Cursor, out int line, out int col, out int lineStartIdx );
		var lines = displayText.Split( '\n' );
		int target = line + dir;
		if ( target < 0 )
		{
			state.Cursor = 0;
			return;
		}
		if ( target >= lines.Length )
		{
			state.Cursor = displayText.Length;
			return;
		}
		float x = TextWidthPrefix( lines[line], col );
		int idx = 0;
		for ( int i = 0; i < target; i++ )
			idx += lines[i].Length + 1;
		state.Cursor = idx + ColumnFromX( lines[target], x );
	}

	private static void RunCallback( ImGuiInputTextState state, ImGuiInputTextFlags flags, ImGuiInputTextCallback callback, ImGuiInputTextFlags eventFlag, ImGuiKey key )
	{
		var data = new ImGuiInputTextCallbackData
		{
			EventFlag = eventFlag,
			Flags = flags,
			EventKey = key,
			CursorPos = state.Cursor,
			SelectionStart = state.SelectStart,
			SelectionEnd = state.Cursor,
			BufSize = state.MaxLength > 0 ? state.MaxLength : int.MaxValue,
		};
		data.SetBufSilently( state.Text );
		callback( data );

		bool isReadOnly = (flags & ImGuiInputTextFlags.ReadOnly) != 0;
		if ( data.BufDirty && !isReadOnly )
		{
			state.PushUndo();
			var newText = data.Buf;
			if ( state.MaxLength > 0 && newText.Length > state.MaxLength )
				newText = newText.Substring( 0, state.MaxLength );
			state.Text = newText;
			state.CursorFollow = true;
		}
		state.Cursor = Math.Clamp( data.CursorPos, 0, state.Text.Length );
		state.SelectStart = Math.Clamp( data.SelectionStart == data.SelectionEnd ? state.Cursor : data.SelectionStart, 0, state.Text.Length );
		if ( data.SelectionStart != data.SelectionEnd )
			state.Cursor = Math.Clamp( data.SelectionEnd, 0, state.Text.Length );
	}
}

#endregion

#region ImGui (Widgets/ImGui.Menu)

public static partial class ImGui
{
	#region Combo
	private static float CalcMaxPopupHeightFromItemCount( int itemsCount )
	{
		var g = G;
		if ( itemsCount <= 0 )
			return float.MaxValue;
		return (g.FontSize + g.Style.ItemSpacing.y) * itemsCount - g.Style.ItemSpacing.y + g.Style.WindowPadding.y * 2;
	}

	/// <summary>Begin a combo box (dropdown). Only call EndCombo() if this returns true.</summary>
	public static bool BeginCombo( string label, string previewValue, ImGuiComboFlags flags = ImGuiComboFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();

		var backupNextWindowDataFlags = g.NextWindowData.Flags;
		g.NextWindowData.ClearFlags();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );

		float arrowSize = (flags & ImGuiComboFlags.NoArrowButton) != 0 ? 0.0f : GetFrameHeight();
		var labelSize = CalcTextSize( label, true );
		float previewWidth = (flags & ImGuiComboFlags.WidthFitPreview) != 0 && previewValue is not null ? CalcTextSize( previewValue, true ).x : 0.0f;
		float w = (flags & ImGuiComboFlags.NoPreview) != 0 ? arrowSize : ((flags & ImGuiComboFlags.WidthFitPreview) != 0 ? arrowSize + previewWidth + style.FramePadding.x * 2.0f : CalcItemWidth());
		var bb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + new Vector2( w, labelSize.y + style.FramePadding.y * 2.0f ) );
		var totalBb = new ImRect( bb.Min, bb.Max + new Vector2( labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f, 0.0f ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id, bb ) )
			return false;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out _ );
		int popupId = ImHashStr( "##ComboPopup", id );
		bool popupOpen = IsPopupOpen( popupId, ImGuiPopupFlags.None );
		if ( pressed && !popupOpen )
		{
			OpenPopupEx( popupId );
			popupOpen = true;
		}

		var frameCol = GetColorU32Internal( hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg );
		float valueX2 = MathF.Max( bb.Min.x, bb.Max.x - arrowSize );
		if ( (flags & ImGuiComboFlags.NoPreview) == 0 )
			window.DrawList.AddRectFilled( bb.Min, new Vector2( valueX2, bb.Max.y ), frameCol, style.FrameRounding, (flags & ImGuiComboFlags.NoArrowButton) != 0 ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersLeft );
		if ( (flags & ImGuiComboFlags.NoArrowButton) == 0 )
		{
			var bgCol = GetColorU32Internal( popupOpen || hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button );
			var textCol = GetColorU32Internal( ImGuiCol.Text );
			window.DrawList.AddRectFilled( new Vector2( valueX2, bb.Min.y ), bb.Max, bgCol, style.FrameRounding, w <= arrowSize ? ImDrawFlags.RoundCornersAll : ImDrawFlags.RoundCornersRight );
			if ( valueX2 + arrowSize - style.FramePadding.x <= bb.Max.x )
				RenderArrow( window.DrawList, new Vector2( valueX2 + style.FramePadding.y, bb.Min.y + style.FramePadding.y ), textCol, ImGuiDir.Down, 1.0f );
		}
		RenderFrameBorder( bb.Min, bb.Max, style.FrameRounding );

		if ( previewValue is not null && (flags & ImGuiComboFlags.NoPreview) == 0 )
			RenderTextClipped( bb.Min + style.FramePadding, new Vector2( valueX2, bb.Max.y ), previewValue, null, Vector2.Zero );
		if ( labelSize.x > 0 )
			RenderText( new Vector2( bb.Max.x + style.ItemInnerSpacing.x, bb.Min.y + style.FramePadding.y ), label );

		if ( !popupOpen )
			return false;

		g.NextWindowData.Flags = backupNextWindowDataFlags;
		return BeginComboPopup( popupId, bb, flags );
	}

	internal static bool BeginComboPopup( int popupId, ImRect bb, ImGuiComboFlags flags )
	{
		var g = G;
		if ( !IsPopupOpen( popupId, ImGuiPopupFlags.None ) )
		{
			g.NextWindowData.ClearFlags();
			return false;
		}

		float w = bb.Width;
		if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSizeConstraint) != 0 )
		{
			g.NextWindowData.SizeConstraintRect.Min.x = MathF.Max( g.NextWindowData.SizeConstraintRect.Min.x, w );
		}
		else
		{
			if ( (flags & ImGuiComboFlags.HeightMask_) == 0 )
				flags |= ImGuiComboFlags.HeightRegular;
			int popupMaxHeightInItems = -1;
			if ( (flags & ImGuiComboFlags.HeightRegular) != 0 ) popupMaxHeightInItems = 8;
			else if ( (flags & ImGuiComboFlags.HeightSmall) != 0 ) popupMaxHeightInItems = 4;
			else if ( (flags & ImGuiComboFlags.HeightLarge) != 0 ) popupMaxHeightInItems = 20;
			var constraintMin = new Vector2( 0.0f, 0.0f );
			var constraintMax = new Vector2( float.MaxValue, float.MaxValue );
			if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSize) == 0 || g.NextWindowData.SizeVal.x <= 0.0f )
				constraintMin.x = w;
			if ( (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSize) == 0 || g.NextWindowData.SizeVal.y <= 0.0f )
				constraintMax.y = CalcMaxPopupHeightFromItemCount( popupMaxHeightInItems );
			SetNextWindowSizeConstraints( constraintMin, constraintMax );
		}

		string name = $"##Combo_{g.BeginComboDepth:D2}";
		var popupWindow = FindWindowByName( name );
		if ( popupWindow is not null && popupWindow.WasActive )
		{
			var sizeExpected = CalcWindowNextAutoFitSize( popupWindow );
			popupWindow.AutoPosLastDirection = (flags & ImGuiComboFlags.PopupAlignLeft) != 0 ? ImGuiDir.Left : ImGuiDir.Down;
			var rOuter = GetPopupAllowedExtentRect();
			var pos = FindBestWindowPosForPopupEx( bb.BL, sizeExpected, ref popupWindow.AutoPosLastDirection, rOuter, bb, ImGuiPopupPositionPolicy.ComboBox );
			SetNextWindowPos( pos );
		}
		else
		{
			SetNextWindowPos( bb.BL );
		}

		var windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoMove;
		PushStyleVarX( ImGuiStyleVar.WindowPadding, g.Style.FramePadding.x );
		bool ret = Begin( name, windowFlags );
		PopStyleVar();
		if ( !ret )
		{
			EndPopup();
			return false;
		}
		g.BeginComboDepth++;
		return true;
	}

	/// <summary>Only call EndCombo() if BeginCombo() returns true!</summary>
	public static void EndCombo()
	{
		var g = G;
		g.BeginComboDepth--;
		EndPopup();
	}

	public static bool Combo( string label, ref int currentItem, string[] items, int popupMaxHeightInItems = -1 )
	{
		var g = G;
		string previewValue = currentItem >= 0 && currentItem < items.Length ? items[currentItem] : null;

		if ( popupMaxHeightInItems != -1 && (g.NextWindowData.Flags & ImGuiNextWindowDataFlags.HasSizeConstraint) == 0 )
			SetNextWindowSizeConstraints( Vector2.Zero, new Vector2( float.MaxValue, CalcMaxPopupHeightFromItemCount( popupMaxHeightInItems ) ) );

		if ( !BeginCombo( label, previewValue ) )
			return false;

		bool valueChanged = false;
		for ( int i = 0; i < items.Length; i++ )
		{
			PushID( i );
			bool itemSelected = i == currentItem;
			if ( Selectable( items[i] ?? "*Unknown item*", itemSelected ) && currentItem != i )
			{
				valueChanged = true;
				currentItem = i;
			}
			if ( itemSelected )
				SetItemDefaultFocus();
			PopID();
		}

		EndCombo();
		if ( valueChanged )
			MarkItemEdited( g.LastItemData.ID );
		return valueChanged;
	}

	/// <summary>Combo with items separated by '\0' inside the string, e.g. "One\0Two\0Three\0".</summary>
	public static bool Combo( string label, ref int currentItem, string itemsSeparatedByZeros, int popupMaxHeightInItems = -1 )
	{
		var items = itemsSeparatedByZeros.Split( '\0' ).Where( x => x.Length > 0 ).ToArray();
		return Combo( label, ref currentItem, items, popupMaxHeightInItems );
	}

	public static bool Combo( string label, ref int currentItem, Func<int, string> getter, int itemsCount, int popupMaxHeightInItems = -1 )
	{
		var items = new string[itemsCount];
		for ( int i = 0; i < itemsCount; i++ )
			items[i] = getter( i );
		return Combo( label, ref currentItem, items, popupMaxHeightInItems );
	}

	/// <summary>Combo for any enum type.</summary>
	public static bool Combo<T>( string label, ref T value, int popupMaxHeightInItems = -1 ) where T : struct, Enum
	{
		var names = Enum.GetNames( typeof( T ) );
		var values = (T[])Enum.GetValues( typeof( T ) );
		int current = Array.IndexOf( values, value );
		if ( Combo( label, ref current, names, popupMaxHeightInItems ) && current >= 0 )
		{
			value = values[current];
			return true;
		}
		return false;
	}
	#endregion

	#region Menus
	/// <summary>Append to the menu-bar of the current window (requires ImGuiWindowFlags.MenuBar flag set on the parent window).</summary>
	public static bool BeginMenuBar()
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		if ( (window.Flags & ImGuiWindowFlags.MenuBar) == 0 )
			return false;

		BeginGroup();
		PushID( "##menubar" );

		var barRect = window.MenuBarRect();
		var clipRect = new ImRect(
			MathF.Round( barRect.Min.x + window.WindowBorderSize ),
			MathF.Round( barRect.Min.y + window.WindowBorderSize ),
			MathF.Round( MathF.Max( barRect.Min.x, barRect.Max.x - MathF.Max( window.WindowRounding, window.WindowBorderSize ) ) ),
			MathF.Round( barRect.Max.y ) );
		clipRect.ClipWith( window.OuterRectClipped );
		PushClipRect( clipRect.Min, clipRect.Max, false );

		window.DC.CursorPos = window.DC.CursorMaxPos = new Vector2( barRect.Min.x + window.DC.MenuBarOffset.x, barRect.Min.y + window.DC.MenuBarOffset.y );
		window.DC.LayoutType = ImGuiLayoutType.Horizontal;
		window.DC.IsSameLine = false;
		window.DC.MenuBarAppending = 1;
		AlignTextToFramePadding();
		return true;
	}

	/// <summary>Only call EndMenuBar() if BeginMenuBar() returns true!</summary>
	public static void EndMenuBar()
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		PopClipRect();
		PopID();
		window.DC.MenuBarOffset = new Vector2( window.DC.CursorPos.x - window.Pos.x, window.DC.MenuBarOffset.y );

		var groupData = g.GroupStack[^1];
		groupData.EmitItem = false;
		var restoreCursorMaxPos = groupData.BackupCursorMaxPos;
		window.DC.IdealMaxPos = new Vector2( MathF.Max( window.DC.IdealMaxPos.x, window.DC.CursorMaxPos.x - window.Scroll.x ), window.DC.IdealMaxPos.y );
		EndGroup();
		window.DC.LayoutType = ImGuiLayoutType.Vertical;
		window.DC.IsSameLine = false;
		window.DC.MenuBarAppending = 0;
		window.DC.CursorMaxPos = restoreCursorMaxPos;
	}

	/// <summary>Create and append to a full screen menu-bar at the top of the screen.</summary>
	public static bool BeginMainMenuBar()
	{
		var g = G;
		g.NextWindowData.MenuBarOffsetMinVal = new Vector2( g.Style.DisplaySafeAreaPadding.x, MathF.Max( g.Style.DisplaySafeAreaPadding.y - g.Style.FramePadding.y, 0.0f ) );
		var windowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;
		float height = GetFrameHeight();

		SetNextWindowPos( Vector2.Zero );
		SetNextWindowSize( new Vector2( g.IO.DisplaySize.x, height + g.NextWindowData.MenuBarOffsetMinVal.y ) );
		PushStyleVar( ImGuiStyleVar.WindowRounding, 0.0f );
		PushStyleVar( ImGuiStyleVar.WindowMinSize, Vector2.Zero );
		PushStyleVar( ImGuiStyleVar.WindowBorderSize, 0.0f );
		bool isOpen = Begin( "##MainMenuBar", windowFlags );
		PopStyleVar( 3 );
		g.NextWindowData.MenuBarOffsetMinVal = Vector2.Zero;

		if ( !isOpen )
		{
			End();
			return false;
		}

		BeginMenuBar();
		return true;
	}

	public static void EndMainMenuBar()
	{
		EndMenuBar();
		End();
	}

	private static bool IsRootOfOpenMenuSet()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( g.OpenPopupStack.Count <= g.BeginPopupStack.Count || (window.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
			return false;

		var upperPopup = g.OpenPopupStack[g.BeginPopupStack.Count];
		int layer = window.DC.MenuBarAppending > 0 ? 1 : 0;
		if ( layer != upperPopup.ParentNavLayer )
			return false;
		return upperPopup.Window is not null && (upperPopup.Window.Flags & ImGuiWindowFlags.ChildMenu) != 0 && IsWindowChildOf( upperPopup.Window, window, true );
	}

	private static bool BeginPopupMenuEx( int id, string label, ImGuiWindowFlags extraWindowFlags )
	{
		var g = G;
		if ( !IsPopupOpen( id, ImGuiPopupFlags.None ) )
		{
			g.NextWindowData.ClearFlags();
			return false;
		}

		string name = $"{LabelText( label )}###Menu_{g.BeginMenuDepth:D2}";
		var flags = extraWindowFlags | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings;
		bool isOpen = Begin( name, flags | ImGuiWindowFlags.Popup );
		if ( !isOpen )
			EndPopup();
		return isOpen;
	}

	private static bool ImTriangleContainsPoint( Vector2 a, Vector2 b, Vector2 c, Vector2 p )
	{
		bool b1 = ((p.x - b.x) * (a.y - b.y) - (p.y - b.y) * (a.x - b.x)) < 0.0f;
		bool b2 = ((p.x - c.x) * (b.y - c.y) - (p.y - c.y) * (b.x - c.x)) < 0.0f;
		bool b3 = ((p.x - a.x) * (c.y - a.y) - (p.y - a.y) * (c.x - a.x)) < 0.0f;
		return (b1 == b2) && (b2 == b3);
	}

	/// <summary>Create a sub-menu entry. Only call EndMenu() if this returns true!</summary>
	public static bool BeginMenu( string label, bool enabled = true ) => BeginMenuEx( label, null, enabled );

	internal static bool BeginMenuEx( string label, string icon, bool enabled )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		bool menuIsOpen = IsPopupOpen( id, ImGuiPopupFlags.None );

		var windowFlags = ImGuiWindowFlags.ChildMenu | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNavFocus;
		if ( (window.Flags & ImGuiWindowFlags.ChildMenu) != 0 )
			windowFlags |= ImGuiWindowFlags.ChildWindow;

		// Appending to a menu that was already submitted this frame.
		if ( g.MenusIdSubmittedThisFrame.Contains( id ) )
		{
			if ( menuIsOpen )
				menuIsOpen = BeginPopupMenuEx( id, label, windowFlags );
			else
				g.NextWindowData.ClearFlags();
			return menuIsOpen;
		}
		g.MenusIdSubmittedThisFrame.Add( id );

		var labelSize = CalcTextSize( label, true );

		bool menusetIsOpen = IsRootOfOpenMenuSet();
		if ( menusetIsOpen )
			PushItemFlag( ImGuiItemFlags.NoWindowHoverableCheck, true );

		Vector2 popupPos;
		var pos = window.DC.CursorPos;
		PushID( label );
		int selectableId = GetID( "" );
		if ( !enabled )
			BeginDisabled();
		var offsets = window.DC.MenuColumns;
		bool pressed;
		var selectableFlags = ImGuiSelectableFlags.NoHoldingActiveID | ImGuiSelectableFlags.SelectOnClick | ImGuiSelectableFlags.NoAutoClosePopups;
		if ( window.DC.LayoutType == ImGuiLayoutType.Horizontal )
		{
			popupPos = new Vector2( pos.x - 1.0f - ImTrunc( style.ItemSpacing.x * 0.5f ), pos.y - style.FramePadding.y + window.MenuBarHeight );
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x + ImTrunc( style.ItemSpacing.x * 0.5f ), window.DC.CursorPos.y );
			PushStyleVarX( ImGuiStyleVar.ItemSpacing, style.ItemSpacing.x * 2.0f );
			float w = labelSize.x;
			var textPos = new Vector2( window.DC.CursorPos.x + offsets.OffsetLabel, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
			pressed = Selectable( "", menuIsOpen, selectableFlags, new Vector2( w, labelSize.y ) );
			RenderText( textPos, label );
			PopStyleVar();
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x + ImTrunc( style.ItemSpacing.x * (-1.0f + 0.5f) ), window.DC.CursorPos.y );
		}
		else
		{
			popupPos = new Vector2( pos.x, pos.y - style.WindowPadding.y );
			float iconW = !string.IsNullOrEmpty( icon ) ? CalcTextSize( icon ).x : 0.0f;
			float checkmarkW = ImTrunc( g.FontSize * 1.20f );
			float minW = offsets.DeclColumns( iconW, labelSize.x, 0.0f, checkmarkW );
			float extraW = MathF.Max( 0.0f, GetContentRegionAvail().x - minW );
			var textPos = new Vector2( window.DC.CursorPos.x + offsets.OffsetLabel, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
			pressed = Selectable( "", menuIsOpen, selectableFlags | ImGuiSelectableFlags.SpanAvailWidth, new Vector2( minW, labelSize.y ) );
			RenderText( textPos, label );
			if ( iconW > 0.0f )
				RenderText( pos + new Vector2( offsets.OffsetIcon, 0.0f ), icon );
			RenderArrow( window.DrawList, pos + new Vector2( offsets.OffsetMark + extraW + g.FontSize * 0.30f, 0.0f ), GetColorU32Internal( ImGuiCol.Text ), ImGuiDir.Right );
		}
		if ( !enabled )
			EndDisabled();

		bool hovered = g.HoveredId == selectableId && enabled;
		if ( menusetIsOpen )
			PopItemFlag();

		bool wantOpen = false;
		bool wantClose = false;
		if ( window.DC.LayoutType == ImGuiLayoutType.Vertical )
		{
			bool movingTowardChildMenu = false;
			var childPopup = g.BeginPopupStack.Count < g.OpenPopupStack.Count ? g.OpenPopupStack[g.BeginPopupStack.Count] : null;
			var childMenuWindow = childPopup?.Window is not null && childPopup.Window.ParentWindow == window ? childPopup.Window : null;
			if ( g.HoveredWindow == window && childMenuWindow is not null )
			{
				float refUnit = g.FontSize;
				float childDir = window.Pos.x < childMenuWindow.Pos.x ? 1.0f : -1.0f;
				var nextWindowRect = childMenuWindow.Rect();
				var ta = g.IO.MousePos - g.IO.MouseDelta;
				var tb = childDir > 0.0f ? nextWindowRect.TL : nextWindowRect.TR;
				var tc = childDir > 0.0f ? nextWindowRect.BL : nextWindowRect.BR;
				float padFarmostH = Math.Clamp( MathF.Abs( ta.x - tb.x ) * 0.30f, refUnit * 0.5f, refUnit * 2.5f );
				ta.x += childDir * -0.5f;
				tb.x += childDir * refUnit;
				tc.x += childDir * refUnit;
				tb.y = ta.y + MathF.Max( (tb.y - padFarmostH) - ta.y, -refUnit * 8.0f );
				tc.y = ta.y + MathF.Min( (tc.y + padFarmostH) - ta.y, +refUnit * 8.0f );
				movingTowardChildMenu = ImTriangleContainsPoint( ta, tb, tc, g.IO.MousePos );
			}

			if ( menuIsOpen && !hovered && g.HoveredWindow == window && !movingTowardChildMenu && g.ActiveId == 0 )
				wantClose = true;

			if ( !menuIsOpen && pressed )
				wantOpen = true;
			else if ( !menuIsOpen && hovered && !movingTowardChildMenu )
				wantOpen = true;
			else if ( !menuIsOpen && hovered && g.HoveredIdTimer >= 0.30f && g.MouseStationaryTimer >= 0.30f )
				wantOpen = true;
		}
		else
		{
			if ( menuIsOpen && pressed && menusetIsOpen )
			{
				wantClose = true;
				wantOpen = menuIsOpen = false;
			}
			else if ( pressed || (hovered && menusetIsOpen && !menuIsOpen) )
			{
				wantOpen = true;
			}
		}

		if ( !enabled )
			wantClose = true;
		if ( wantClose && IsPopupOpen( id, ImGuiPopupFlags.None ) )
			ClosePopupToLevel( g.BeginPopupStack.Count, true );

		PopID();

		if ( wantOpen && !menuIsOpen && g.OpenPopupStack.Count > g.BeginPopupStack.Count )
		{
			// Don't reopen/recycle the same menu level in the same frame: close the other menu first and yield a frame.
			OpenPopupEx( id );
		}
		else if ( wantOpen )
		{
			menuIsOpen = true;
			OpenPopupEx( id );
		}

		if ( menuIsOpen )
		{
			var lastItemInParent = g.LastItemData;
			SetNextWindowPos( popupPos );
			PushStyleVar( ImGuiStyleVar.ChildRounding, style.PopupRounding );
			menuIsOpen = BeginPopupMenuEx( id, label, windowFlags );
			PopStyleVar();
			if ( menuIsOpen )
			{
				g.LastItemData = lastItemInParent;
				if ( g.HoveredWindow == window )
					g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HoveredWindow;
			}
		}
		else
		{
			g.NextWindowData.ClearFlags();
		}

		return menuIsOpen;
	}

	/// <summary>Only call EndMenu() if BeginMenu() returns true!</summary>
	public static void EndMenu()
	{
		EndPopup();
	}

	/// <summary>Return true when activated. Shortcuts are displayed for convenience but not processed.</summary>
	public static bool MenuItem( string label, string shortcut = null, bool selected = false, bool enabled = true )
		=> MenuItemEx( label, null, shortcut, selected, enabled );

	/// <summary>Return true when activated, and toggle <paramref name="selected"/>.</summary>
	public static bool MenuItem( string label, string shortcut, ref bool selected, bool enabled = true )
	{
		if ( MenuItemEx( label, null, shortcut, selected, enabled ) )
		{
			selected = !selected;
			return true;
		}
		return false;
	}

	internal static bool MenuItemEx( string label, string icon, string shortcut, bool selected, bool enabled )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		var pos = window.DC.CursorPos;
		var labelSize = CalcTextSize( label, true );

		bool menusetIsOpen = IsRootOfOpenMenuSet();
		if ( menusetIsOpen )
			PushItemFlag( ImGuiItemFlags.NoWindowHoverableCheck, true );

		bool pressed;
		PushID( label );
		if ( !enabled )
			BeginDisabled();

		var selectableFlags = ImGuiSelectableFlags.SelectOnRelease | ImGuiSelectableFlags.SetNavIdOnHover;
		var offsets = window.DC.MenuColumns;
		if ( window.DC.LayoutType == ImGuiLayoutType.Horizontal )
		{
			float w = labelSize.x;
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x + ImTrunc( style.ItemSpacing.x * 0.5f ), window.DC.CursorPos.y );
			var textPos = new Vector2( window.DC.CursorPos.x + offsets.OffsetLabel, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
			PushStyleVarX( ImGuiStyleVar.ItemSpacing, style.ItemSpacing.x * 2.0f );
			pressed = Selectable( "", selected, selectableFlags, new Vector2( w, 0.0f ) );
			PopStyleVar();
			if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.Visible) != 0 )
				RenderText( textPos, label );
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x + ImTrunc( style.ItemSpacing.x * (-1.0f + 0.5f) ), window.DC.CursorPos.y );
		}
		else
		{
			float iconW = !string.IsNullOrEmpty( icon ) ? CalcTextSize( icon ).x : 0.0f;
			float shortcutW = !string.IsNullOrEmpty( shortcut ) ? CalcTextSize( shortcut ).x : 0.0f;
			float checkmarkW = ImTrunc( g.FontSize * 1.20f );
			float minW = offsets.DeclColumns( iconW, labelSize.x, shortcutW, checkmarkW );
			float stretchW = MathF.Max( 0.0f, GetContentRegionAvail().x - minW );
			pressed = Selectable( "", false, selectableFlags | ImGuiSelectableFlags.SpanAvailWidth, new Vector2( minW, labelSize.y ) );
			if ( (g.LastItemData.StatusFlags & ImGuiItemStatusFlags.Visible) != 0 )
			{
				RenderText( pos + new Vector2( offsets.OffsetLabel, 0.0f ), label );
				if ( iconW > 0.0f )
					RenderText( pos + new Vector2( offsets.OffsetIcon, 0.0f ), icon );
				if ( shortcutW > 0.0f )
				{
					PushStyleColor( ImGuiCol.Text, style.Colors[(int)ImGuiCol.TextDisabled] );
					RenderText( pos + new Vector2( offsets.OffsetShortcut + stretchW, 0.0f ), shortcut, false );
					PopStyleColor();
				}
				if ( selected )
					RenderCheckMark( window.DrawList, pos + new Vector2( offsets.OffsetMark + stretchW + g.FontSize * 0.40f, g.FontSize * 0.134f * 0.5f ), GetColorU32Internal( ImGuiCol.Text ), g.FontSize * 0.866f );
			}
		}
		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Checkable | (selected ? ImGuiItemStatusFlags.Checked : ImGuiItemStatusFlags.None);
		if ( !enabled )
			EndDisabled();
		PopID();
		if ( menusetIsOpen )
			PopItemFlag();

		return pressed;
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.Plot)

public static partial class ImGui
{
	internal enum ImGuiPlotType
	{
		Lines,
		Histogram,
	}

	/// <summary>Returns the hovered value index, or -1.</summary>
	internal static int PlotEx( ImGuiPlotType plotType, string label, Func<int, float> valuesGetter, int valuesCount, int valuesOffset, string overlayText, float scaleMin, float scaleMax, Vector2 sizeArg )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return -1;

		var style = g.Style;
		int id = window.GetID( label );

		var labelSize = CalcTextSize( label, true );
		var frameSize = CalcItemSize( sizeArg, CalcItemWidth(), labelSize.y + style.FramePadding.y * 2.0f );

		var frameBb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + frameSize );
		var innerBb = new ImRect( frameBb.Min + style.FramePadding, frameBb.Max - style.FramePadding );
		var totalBb = new ImRect( frameBb.Min, frameBb.Max + new Vector2( labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f, 0 ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id, frameBb, ImGuiItemFlags.NoNav ) )
			return -1;
		ButtonBehavior( frameBb, id, out bool hovered, out _ );

		// Determine scale from values if not specified
		if ( scaleMin == float.MaxValue || scaleMax == float.MaxValue )
		{
			float vMin = float.MaxValue;
			float vMax = -float.MaxValue;
			for ( int i = 0; i < valuesCount; i++ )
			{
				float v = valuesGetter( i );
				if ( float.IsNaN( v ) )
					continue;
				vMin = MathF.Min( vMin, v );
				vMax = MathF.Max( vMax, v );
			}
			if ( scaleMin == float.MaxValue ) scaleMin = vMin;
			if ( scaleMax == float.MaxValue ) scaleMax = vMax;
		}

		RenderFrame( frameBb.Min, frameBb.Max, GetColorU32Internal( ImGuiCol.FrameBg ), true, style.FrameRounding );

		int valuesCountMin = plotType == ImGuiPlotType.Lines ? 2 : 1;
		int idxHovered = -1;
		if ( valuesCount >= valuesCountMin )
		{
			int resW = Math.Min( (int)frameSize.x, valuesCount ) + (plotType == ImGuiPlotType.Lines ? -1 : 0);
			int itemCount = valuesCount + (plotType == ImGuiPlotType.Lines ? -1 : 0);
			if ( resW < 1 ) resW = 1;

			// Tooltip on hover
			if ( hovered && innerBb.Contains( g.IO.MousePos ) )
			{
				float t = Math.Clamp( (g.IO.MousePos.x - innerBb.Min.x) / (innerBb.Max.x - innerBb.Min.x), 0.0f, 0.9999f );
				int vIdx = (int)(t * itemCount);
				float v0 = valuesGetter( (vIdx + valuesOffset) % valuesCount );
				float v1 = valuesGetter( (vIdx + 1 + valuesOffset) % valuesCount );
				if ( plotType == ImGuiPlotType.Lines )
					SetTooltip( "{0}: {1}\n{2}: {3}", vIdx, v0.ToString( "G4" ), vIdx + 1, v1.ToString( "G4" ) );
				else
					SetTooltip( "{0}: {1}", vIdx, v0.ToString( "G4" ) );
				idxHovered = vIdx;
			}

			float tStep = 1.0f / resW;
			float invScale = scaleMin == scaleMax ? 0.0f : 1.0f / (scaleMax - scaleMin);

			float firstV = valuesGetter( valuesOffset % valuesCount );
			float t0 = 0.0f;
			var tp0 = new Vector2( t0, 1.0f - ImSaturate( (firstV - scaleMin) * invScale ) );
			float histogramZeroLineT = scaleMin * scaleMax < 0.0f ? (1 + scaleMin * invScale) : (scaleMin < 0.0f ? 0.0f : 1.0f);

			var colBase = GetColorU32Internal( plotType == ImGuiPlotType.Lines ? ImGuiCol.PlotLines : ImGuiCol.PlotHistogram );
			var colHovered = GetColorU32Internal( plotType == ImGuiPlotType.Lines ? ImGuiCol.PlotLinesHovered : ImGuiCol.PlotHistogramHovered );

			var linePoints = plotType == ImGuiPlotType.Lines ? new List<Vector2>( resW + 1 ) : null;
			Vector2 hoveredA = default, hoveredB = default;
			bool hasHoveredSegment = false;

			for ( int n = 0; n < resW; n++ )
			{
				float t1 = t0 + tStep;
				int v1Idx = (int)(t0 * itemCount + 0.5f);
				float v1 = valuesGetter( (v1Idx + valuesOffset + 1) % valuesCount );
				var tp1 = new Vector2( t1, 1.0f - ImSaturate( (v1 - scaleMin) * invScale ) );

				var pos0 = ImLerp( innerBb.Min, innerBb.Max, tp0 );
				var pos1 = ImLerp( innerBb.Min, innerBb.Max, plotType == ImGuiPlotType.Lines ? tp1 : new Vector2( tp1.x, histogramZeroLineT ) );
				if ( plotType == ImGuiPlotType.Lines )
				{
					if ( n == 0 )
						linePoints.Add( pos0 );
					linePoints.Add( pos1 );
					if ( idxHovered == v1Idx )
					{
						hoveredA = pos0;
						hoveredB = pos1;
						hasHoveredSegment = true;
					}
				}
				else
				{
					if ( pos1.x >= pos0.x + 2.0f )
						pos1.x -= 1.0f;
					var rMin = ImMin( pos0, pos1 );
					var rMax = ImMax( pos0, pos1 );
					if ( rMax.y - rMin.y < 1.0f ) rMax.y = rMin.y + 1.0f;
					window.DrawList.AddRectFilled( rMin, rMax, idxHovered == v1Idx ? colHovered : colBase );
				}

				t0 = t1;
				tp0 = tp1;
			}

			if ( linePoints is not null )
			{
				window.DrawList.AddPolyline( linePoints.ToArray(), colBase, ImDrawFlags.None, 1.0f );
				if ( hasHoveredSegment )
					window.DrawList.AddLine( hoveredA, hoveredB, colHovered, 1.0f );
			}
		}

		// Text overlay
		if ( overlayText is not null )
			RenderTextClipped( new Vector2( frameBb.Min.x, frameBb.Min.y + style.FramePadding.y ), frameBb.Max, overlayText, null, new Vector2( 0.5f, 0.0f ) );

		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( frameBb.Max.x + style.ItemInnerSpacing.x, innerBb.Min.y ), label );

		return idxHovered;
	}

	public static void PlotLines( string label, float[] values, int valuesOffset = 0, string overlayText = null, float scaleMin = float.MaxValue, float scaleMax = float.MaxValue, Vector2 graphSize = default )
	{
		if ( values is null ) return;
		PlotEx( ImGuiPlotType.Lines, label, i => values[i], values.Length, valuesOffset, overlayText, scaleMin, scaleMax, graphSize );
	}

	/// <summary>Plot the first <paramref name="valuesCount"/> values of an array (Dear ImGui values_count parameter).</summary>
	public static void PlotLines( string label, float[] values, int valuesCount, int valuesOffset, string overlayText = null, float scaleMin = float.MaxValue, float scaleMax = float.MaxValue, Vector2 graphSize = default )
	{
		if ( values is null ) return;
		valuesCount = Math.Min( valuesCount, values.Length );
		PlotEx( ImGuiPlotType.Lines, label, i => values[i], valuesCount, valuesOffset, overlayText, scaleMin, scaleMax, graphSize );
	}

	public static void PlotLines( string label, Func<int, float> valuesGetter, int valuesCount, int valuesOffset = 0, string overlayText = null, float scaleMin = float.MaxValue, float scaleMax = float.MaxValue, Vector2 graphSize = default )
	{
		if ( valuesGetter is null ) return;
		PlotEx( ImGuiPlotType.Lines, label, valuesGetter, valuesCount, valuesOffset, overlayText, scaleMin, scaleMax, graphSize );
	}

	public static void PlotHistogram( string label, float[] values, int valuesOffset = 0, string overlayText = null, float scaleMin = float.MaxValue, float scaleMax = float.MaxValue, Vector2 graphSize = default )
	{
		if ( values is null ) return;
		PlotEx( ImGuiPlotType.Histogram, label, i => values[i], values.Length, valuesOffset, overlayText, scaleMin, scaleMax, graphSize );
	}

	public static void PlotHistogram( string label, float[] values, int valuesCount, int valuesOffset, string overlayText = null, float scaleMin = float.MaxValue, float scaleMax = float.MaxValue, Vector2 graphSize = default )
	{
		if ( values is null ) return;
		valuesCount = Math.Min( valuesCount, values.Length );
		PlotEx( ImGuiPlotType.Histogram, label, i => values[i], valuesCount, valuesOffset, overlayText, scaleMin, scaleMax, graphSize );
	}

	public static void PlotHistogram( string label, Func<int, float> valuesGetter, int valuesCount, int valuesOffset = 0, string overlayText = null, float scaleMin = float.MaxValue, float scaleMax = float.MaxValue, Vector2 graphSize = default )
	{
		if ( valuesGetter is null ) return;
		PlotEx( ImGuiPlotType.Histogram, label, valuesGetter, valuesCount, valuesOffset, overlayText, scaleMin, scaleMax, graphSize );
	}
}

#endregion

#region ImGui (Widgets/ImGui.Slider)

public static partial class ImGui
{
	#region Slider behavior
	/// <summary>Port of Dear ImGui's SliderBehaviorT, operating on doubles. Outputs the grab rectangle for rendering.</summary>
	internal static bool SliderBehavior( ImRect bb, int id, ref double v, double vMin, double vMax, string format, ImGuiSliderFlags flags, bool isInteger, out ImRect outGrabBb )
	{
		var g = G;
		var style = g.Style;

		int axis = (flags & ImGuiSliderFlags.Vertical) != 0 ? 1 : 0;
		bool isLogarithmic = (flags & ImGuiSliderFlags.Logarithmic) != 0;
		bool isFloatingPoint = !isInteger;
		float vRangeF = (float)(vMin < vMax ? vMax - vMin : vMin - vMax);

		const float grabPadding = 2.0f;
		float sliderSz = (Axis( bb.Max, axis ) - Axis( bb.Min, axis )) - grabPadding * 2.0f;
		float grabSz = style.GrabMinSize;
		if ( !isFloatingPoint && vRangeF >= 0.0f )
			grabSz = MathF.Max( sliderSz / (vRangeF + 1), style.GrabMinSize );
		grabSz = MathF.Min( grabSz, sliderSz );
		float sliderUsableSz = sliderSz - grabSz;
		float sliderUsablePosMin = Axis( bb.Min, axis ) + grabPadding + grabSz * 0.5f;
		float sliderUsablePosMax = Axis( bb.Max, axis ) - grabPadding - grabSz * 0.5f;

		float logarithmicZeroEpsilon = 0.0f;
		float zeroDeadzoneHalfsize = 0.0f;
		if ( isLogarithmic )
		{
			int decimalPrecision = isFloatingPoint ? ParseFormatPrecision( format, 3 ) : 1;
			logarithmicZeroEpsilon = MathF.Pow( 0.1f, Math.Max( 0, decimalPrecision ) );
			zeroDeadzoneHalfsize = style.LogSliderDeadzone * 0.5f / MathF.Max( sliderUsableSz, 1.0f );
		}

		bool valueChanged = false;
		if ( g.ActiveId == id )
		{
			bool setNewValue = false;
			float clickedT = 0.0f;
			if ( !g.IO.MouseDown[0] )
			{
				ClearActiveID();
			}
			else
			{
				float mouseAbsPos = Axis( g.IO.MousePos, axis );
				if ( g.ActiveIdIsJustActivated )
				{
					float grabT0 = ScaleRatioFromValue( v, vMin, vMax, isLogarithmic, logarithmicZeroEpsilon, zeroDeadzoneHalfsize );
					if ( axis == 1 )
						grabT0 = 1.0f - grabT0;
					float grabPos0 = ImLerp( sliderUsablePosMin, sliderUsablePosMax, grabT0 );
					bool clickedAroundGrab = mouseAbsPos >= grabPos0 - grabSz * 0.5f - 1.0f && mouseAbsPos <= grabPos0 + grabSz * 0.5f + 1.0f;
					g.SliderGrabClickOffset = clickedAroundGrab && isFloatingPoint ? mouseAbsPos - grabPos0 : 0.0f;
				}
				if ( sliderUsableSz > 0.0f )
					clickedT = ImSaturate( (mouseAbsPos - g.SliderGrabClickOffset - sliderUsablePosMin) / sliderUsableSz );
				if ( axis == 1 )
					clickedT = 1.0f - clickedT;
				setNewValue = true;
			}

			if ( setNewValue && ((g.LastItemData.InFlags & ImGuiItemFlags.ReadOnly) != 0 || (flags & ImGuiSliderFlags.ReadOnly) != 0) )
				setNewValue = false;

			if ( setNewValue )
			{
				double vNew = ScaleValueFromRatio( clickedT, vMin, vMax, isInteger, isLogarithmic, logarithmicZeroEpsilon, zeroDeadzoneHalfsize );
				if ( isFloatingPoint && (flags & ImGuiSliderFlags.NoRoundToFormat) == 0 )
					vNew = RoundScalarWithFormat( format, vNew );
				if ( v != vNew )
				{
					v = vNew;
					valueChanged = true;
				}
			}
		}

		if ( sliderSz < 1.0f )
		{
			outGrabBb = new ImRect( bb.Min, bb.Min );
		}
		else
		{
			float grabT = ScaleRatioFromValue( v, vMin, vMax, isLogarithmic, logarithmicZeroEpsilon, zeroDeadzoneHalfsize );
			if ( axis == 1 )
				grabT = 1.0f - grabT;
			float grabPos = ImLerp( sliderUsablePosMin, sliderUsablePosMax, grabT );
			outGrabBb = axis == 0
				? new ImRect( grabPos - grabSz * 0.5f, bb.Min.y + grabPadding, grabPos + grabSz * 0.5f, bb.Max.y - grabPadding )
				: new ImRect( bb.Min.x + grabPadding, grabPos - grabSz * 0.5f, bb.Max.x - grabPadding, grabPos + grabSz * 0.5f );
		}
		return valueChanged;
	}
	#endregion

	#region SliderScalar core
	internal static bool SliderScalarCore( string label, ref double value, double vMin, double vMax, string format, ImGuiSliderFlags flags, bool isInteger )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		float w = CalcItemWidth();

		var labelSize = CalcTextSize( label, true );
		var frameBb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + new Vector2( w, labelSize.y + style.FramePadding.y * 2.0f ) );
		var totalBb = new ImRect( frameBb.Min, frameBb.Max + new Vector2( labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f, 0.0f ) );

		bool tempInputAllowed = (flags & ImGuiSliderFlags.NoInput) == 0;
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, id, frameBb, tempInputAllowed ? ImGuiItemFlags.Inputable : ImGuiItemFlags.None ) )
			return false;

		if ( string.IsNullOrEmpty( format ) )
			format = isInteger ? "%d" : "%.3f";

		bool hovered = ItemHoverable( frameBb, id, g.LastItemData.InFlags );
		bool tempInputIsActive = tempInputAllowed && TempInputIsActive( id );
		if ( !tempInputIsActive )
		{
			bool focusRequested = tempInputAllowed && g.FocusedTextInputRequestId == id;
			if ( focusRequested )
				g.FocusedTextInputRequestId = 0;

			bool clicked = hovered && g.IO.MouseClicked[0];
			bool makeActive = clicked || focusRequested;
			if ( makeActive && tempInputAllowed )
				if ( (clicked && g.IO.KeyCtrl) || focusRequested )
					tempInputIsActive = true;

			if ( makeActive && !tempInputIsActive )
			{
				SetActiveID( id, window );
				g.ActiveIdMouseButton = 0;
				g.NavId = id;
				FocusWindow( window );
			}
		}

		if ( tempInputIsActive )
		{
			bool clampEnabled = (flags & ImGuiSliderFlags.ClampOnInput) != 0;
			return TempInputScalar( frameBb, id, label, ref value, format, isInteger, clampEnabled, Math.Min( vMin, vMax ), Math.Max( vMin, vMax ) );
		}

		var frameCol = GetColorU32Internal( g.ActiveId == id ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg );
		RenderFrame( frameBb.Min, frameBb.Max, frameCol, true, style.FrameRounding );

		bool valueChanged = SliderBehavior( frameBb, id, ref value, vMin, vMax, format, flags, isInteger, out var grabBb );
		if ( valueChanged )
			MarkItemEdited( id );

		if ( grabBb.Max.x > grabBb.Min.x )
			window.DrawList.AddRectFilled( grabBb.Min, grabBb.Max, GetColorU32Internal( g.ActiveId == id ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab ), style.GrabRounding );

		string valueText = FormatNumber( format, value, isInteger );
		RenderTextClippedEx( window.DrawList, frameBb.Min, frameBb.Max, valueText, null, new Vector2( 0.5f, 0.5f ), null );

		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( frameBb.Max.x + style.ItemInnerSpacing.x, frameBb.Min.y + style.FramePadding.y ), label );

		return valueChanged;
	}

	internal static bool VSliderScalarCore( string label, Vector2 size, ref double value, double vMin, double vMax, string format, ImGuiSliderFlags flags, bool isInteger )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );

		var labelSize = CalcTextSize( label, true );
		var frameBb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + size );
		var bb = new ImRect( frameBb.Min, frameBb.Max + new Vector2( labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f, 0.0f ) );

		ItemSize( bb, style.FramePadding.y );
		if ( !ItemAdd( frameBb, id ) )
			return false;

		if ( string.IsNullOrEmpty( format ) )
			format = isInteger ? "%d" : "%.3f";

		bool hovered = ItemHoverable( frameBb, id, g.LastItemData.InFlags );
		bool clicked = hovered && g.IO.MouseClicked[0];
		if ( clicked )
		{
			SetActiveID( id, window );
			g.ActiveIdMouseButton = 0;
			g.NavId = id;
			FocusWindow( window );
		}

		var frameCol = GetColorU32Internal( g.ActiveId == id ? ImGuiCol.FrameBgActive : hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg );
		RenderFrame( frameBb.Min, frameBb.Max, frameCol, true, style.FrameRounding );

		bool valueChanged = SliderBehavior( frameBb, id, ref value, vMin, vMax, format, flags | ImGuiSliderFlags.Vertical, isInteger, out var grabBb );
		if ( valueChanged )
			MarkItemEdited( id );

		if ( grabBb.Max.y > grabBb.Min.y )
			window.DrawList.AddRectFilled( grabBb.Min, grabBb.Max, GetColorU32Internal( g.ActiveId == id ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab ), style.GrabRounding );

		string valueText = FormatNumber( format, value, isInteger );
		RenderTextClippedEx( window.DrawList, new Vector2( frameBb.Min.x, frameBb.Min.y + style.FramePadding.y ), frameBb.Max, valueText, null, new Vector2( 0.5f, 0.0f ), null );
		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( frameBb.Max.x + style.ItemInnerSpacing.x, frameBb.Min.y + style.FramePadding.y ), label );

		return valueChanged;
	}
	#endregion

	#region Generic slider API
	public static bool SliderScalar<T>( string label, ref T v, T min, T max, string format = null, ImGuiSliderFlags flags = ImGuiSliderFlags.None ) where T : struct, INumber<T>
	{
		bool isInteger = IsIntegerType<T>();
		double d = ToDouble( v );
		bool changed = SliderScalarCore( label, ref d, ToDouble( min ), ToDouble( max ), format, flags, isInteger );
		if ( changed )
		{
			var nv = FromDouble<T>( d );
			changed = nv != v;
			v = nv;
		}
		return changed;
	}

	public static bool SliderScalarN<T>( string label, T[] v, T min, T max, string format = null, ImGuiSliderFlags flags = ImGuiSliderFlags.None ) where T : struct, INumber<T>
		=> ScalarNCore( label, v.Length, i => SliderScalar( "", ref v[i], min, max, format, flags ) );

	public static bool VSliderScalar<T>( string label, Vector2 size, ref T v, T min, T max, string format = null, ImGuiSliderFlags flags = ImGuiSliderFlags.None ) where T : struct, INumber<T>
	{
		bool isInteger = IsIntegerType<T>();
		double d = ToDouble( v );
		bool changed = VSliderScalarCore( label, size, ref d, ToDouble( min ), ToDouble( max ), format, flags, isInteger );
		if ( changed )
		{
			var nv = FromDouble<T>( d );
			changed = nv != v;
			v = nv;
		}
		return changed;
	}
	#endregion

	#region Float sliders
	public static bool SliderFloat( string label, ref float v, float vMin, float vMax, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		double d = v;
		bool changed = SliderScalarCore( label, ref d, vMin, vMax, format, flags, false );
		if ( changed )
			v = (float)d;
		return changed;
	}

	private static bool SliderFloatN( string label, float[] v, int n, float vMin, float vMax, string format, ImGuiSliderFlags flags )
		=> ScalarNCore( label, Math.Min( n, v.Length ), i => SliderFloat( "", ref v[i], vMin, vMax, format, flags ) );

	public static bool SliderFloat2( string label, float[] v, float vMin, float vMax, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None ) => SliderFloatN( label, v, 2, vMin, vMax, format, flags );
	public static bool SliderFloat3( string label, float[] v, float vMin, float vMax, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None ) => SliderFloatN( label, v, 3, vMin, vMax, format, flags );
	public static bool SliderFloat4( string label, float[] v, float vMin, float vMax, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None ) => SliderFloatN( label, v, 4, vMin, vMax, format, flags );

	public static bool SliderFloat2( string label, ref Vector2 v, float vMin, float vMax, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y };
		bool changed = SliderFloatN( label, a, 2, vMin, vMax, format, flags );
		if ( changed ) v = new Vector2( a[0], a[1] );
		return changed;
	}

	public static bool SliderFloat3( string label, ref Vector3 v, float vMin, float vMax, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y, v.z };
		bool changed = SliderFloatN( label, a, 3, vMin, vMax, format, flags );
		if ( changed ) v = new Vector3( a[0], a[1], a[2] );
		return changed;
	}

	public static bool SliderFloat4( string label, ref Vector4 v, float vMin, float vMax, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y, v.z, v.w };
		bool changed = SliderFloatN( label, a, 4, vMin, vMax, format, flags );
		if ( changed ) v = new Vector4( a[0], a[1], a[2], a[3] );
		return changed;
	}

	/// <summary>Slider editing an angle stored in radians, displayed in degrees.</summary>
	public static bool SliderAngle( string label, ref float vRad, float vDegreesMin = -360f, float vDegreesMax = 360f, string format = "%.0f deg", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		if ( string.IsNullOrEmpty( format ) )
			format = "%.0f deg";
		float vDeg = vRad * 360.0f / (2 * MathF.PI);
		bool valueChanged = SliderFloat( label, ref vDeg, vDegreesMin, vDegreesMax, format, flags );
		if ( valueChanged )
			vRad = vDeg * (2 * MathF.PI) / 360.0f;
		return valueChanged;
	}

	public static bool VSliderFloat( string label, Vector2 size, ref float v, float vMin, float vMax, string format = "%.3f", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		double d = v;
		bool changed = VSliderScalarCore( label, size, ref d, vMin, vMax, format, flags, false );
		if ( changed )
			v = (float)d;
		return changed;
	}
	#endregion

	#region Int sliders
	public static bool SliderInt( string label, ref int v, int vMin, int vMax, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		double d = v;
		bool changed = SliderScalarCore( label, ref d, vMin, vMax, format, flags, true );
		if ( changed )
		{
			int nv = (int)Math.Round( d );
			changed = nv != v;
			v = nv;
		}
		return changed;
	}

	private static bool SliderIntN( string label, int[] v, int n, int vMin, int vMax, string format, ImGuiSliderFlags flags )
		=> ScalarNCore( label, Math.Min( n, v.Length ), i => SliderInt( "", ref v[i], vMin, vMax, format, flags ) );

	public static bool SliderInt2( string label, int[] v, int vMin, int vMax, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None ) => SliderIntN( label, v, 2, vMin, vMax, format, flags );
	public static bool SliderInt3( string label, int[] v, int vMin, int vMax, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None ) => SliderIntN( label, v, 3, vMin, vMax, format, flags );
	public static bool SliderInt4( string label, int[] v, int vMin, int vMax, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None ) => SliderIntN( label, v, 4, vMin, vMax, format, flags );

	public static bool SliderInt2( string label, ref Vector2Int v, int vMin, int vMax, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y };
		bool changed = SliderIntN( label, a, 2, vMin, vMax, format, flags );
		if ( changed ) v = new Vector2Int( a[0], a[1] );
		return changed;
	}

	public static bool SliderInt3( string label, ref Vector3Int v, int vMin, int vMax, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		var a = new[] { v.x, v.y, v.z };
		bool changed = SliderIntN( label, a, 3, vMin, vMax, format, flags );
		if ( changed ) v = new Vector3Int( a[0], a[1], a[2] );
		return changed;
	}

	public static bool VSliderInt( string label, Vector2 size, ref int v, int vMin, int vMax, string format = "%d", ImGuiSliderFlags flags = ImGuiSliderFlags.None )
	{
		double d = v;
		bool changed = VSliderScalarCore( label, size, ref d, vMin, vMax, format, flags, true );
		if ( changed )
		{
			int nv = (int)Math.Round( d );
			changed = nv != v;
			v = nv;
		}
		return changed;
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.TabBar)

public static partial class ImGui
{
	/// <summary>Internal: tab item without a close button (p_open == null).</summary>
	private const ImGuiTabItemFlags TabItemNoCloseButton = (ImGuiTabItemFlags)(1 << 20);
	private const ImGuiTabItemFlags TabItemSectionMask = ImGuiTabItemFlags.Leading | ImGuiTabItemFlags.Trailing;

	private struct ShrinkWidthItem
	{
		public int Index;
		public float Width;
		public float InitialWidth;
	}

	private struct TabBarSection
	{
		public int TabCount;
		public float Width;
		public float Spacing;
	}

	#region Tab bar
	/// <summary>Create and append into a TabBar.</summary>
	public static bool BeginTabBar( string strId, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		int id = window.GetID( strId );
		if ( !g.TabBars.TryGetValue( id, out var tabBar ) )
		{
			tabBar = new ImGuiTabBar { ID = id };
			g.TabBars[id] = tabBar;
		}
		var tabBarBb = new ImRect( window.DC.CursorPos.x, window.DC.CursorPos.y, window.WorkRect.Max.x, window.DC.CursorPos.y + g.FontSize + g.Style.FramePadding.y * 2 );
		tabBar.ID = id;
		tabBar.SeparatorMinX = tabBarBb.Min.x - ImTrunc( window.WindowPadding.x * 0.5f );
		tabBar.SeparatorMaxX = tabBarBb.Max.x + ImTrunc( window.WindowPadding.x * 0.5f );
		return BeginTabBarEx( tabBar, tabBarBb, flags );
	}

	private static bool BeginTabBarEx( ImGuiTabBar tabBar, ImRect tabBarBb, ImGuiTabBarFlags flags )
	{
		var g = G;
		var window = g.CurrentWindow;

		g.TabBarStack.Add( tabBar );
		PushOverrideID( tabBar.ID );

		tabBar.BackupCursorPos = window.DC.CursorPos;
		if ( tabBar.CurrFrameVisible == g.FrameCount )
		{
			window.DC.CursorPos = new Vector2( tabBar.BarRect.Min.x, tabBar.BarRect.Max.y + tabBar.ItemSpacingY );
			tabBar.BeginCount++;
			return true;
		}

		if ( (flags & ImGuiTabBarFlags.Reorderable) != (tabBar.Flags & ImGuiTabBarFlags.Reorderable) || (tabBar.TabsAddedNew && (flags & ImGuiTabBarFlags.Reorderable) == 0) )
			tabBar.Tabs.Sort( ( a, b ) => a.BeginOrder.CompareTo( b.BeginOrder ) );
		tabBar.TabsAddedNew = false;

		if ( (flags & ImGuiTabBarFlags.FittingPolicyMask_) == 0 )
			flags |= ImGuiTabBarFlags.FittingPolicyDefault_;

		tabBar.Flags = flags;
		tabBar.BarRect = tabBarBb;
		tabBar.WantLayout = true;
		tabBar.PrevFrameVisible = tabBar.CurrFrameVisible;
		tabBar.CurrFrameVisible = g.FrameCount;
		tabBar.PrevTabsContentsHeight = tabBar.CurrTabsContentsHeight;
		tabBar.CurrTabsContentsHeight = 0.0f;
		tabBar.ItemSpacingY = g.Style.ItemSpacing.y;
		tabBar.FramePadding = g.Style.FramePadding;
		tabBar.TabsActiveCount = 0;
		tabBar.LastTabItemIdx = -1;
		tabBar.BeginCount = 1;

		window.DC.CursorPos = new Vector2( tabBar.BarRect.Min.x, tabBar.BarRect.Max.y + tabBar.ItemSpacingY );

		var col = GetColorU32Internal( ImGuiCol.TabSelected );
		if ( g.Style.TabBarBorderSize > 0.0f )
		{
			float y = tabBar.BarRect.Max.y;
			window.DrawList.AddRectFilled( new Vector2( tabBar.SeparatorMinX, y - g.Style.TabBarBorderSize ), new Vector2( tabBar.SeparatorMaxX, y ), col );
		}
		return true;
	}

	/// <summary>Only call EndTabBar() if BeginTabBar() returns true!</summary>
	public static void EndTabBar()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;

		var tabBar = g.CurrentTabBar;
		if ( tabBar is null )
		{
			Log.Warning( "ImGui: EndTabBar() called without a matching BeginTabBar()" );
			return;
		}

		if ( tabBar.WantLayout )
			TabBarLayout( tabBar );

		bool tabBarAppearing = tabBar.PrevFrameVisible + 1 < g.FrameCount;
		if ( tabBar.VisibleTabWasSubmitted || tabBar.VisibleTabId == 0 || tabBarAppearing )
		{
			tabBar.CurrTabsContentsHeight = MathF.Max( window.DC.CursorPos.y - tabBar.BarRect.Max.y, tabBar.CurrTabsContentsHeight );
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x, tabBar.BarRect.Max.y + tabBar.CurrTabsContentsHeight );
		}
		else
		{
			window.DC.CursorPos = new Vector2( window.DC.CursorPos.x, tabBar.BarRect.Max.y + tabBar.PrevTabsContentsHeight );
		}
		if ( tabBar.BeginCount > 1 )
			window.DC.CursorPos = tabBar.BackupCursorPos;

		tabBar.LastTabItemIdx = -1;
		PopID();
		g.TabBarStack.RemoveAt( g.TabBarStack.Count - 1 );
	}

	private static int TabItemGetSectionIdx( ImGuiTabItem tab )
		=> (tab.Flags & ImGuiTabItemFlags.Leading) != 0 ? 0 : (tab.Flags & ImGuiTabItemFlags.Trailing) != 0 ? 2 : 1;

	private static ImGuiTabItem TabBarFindTabByID( ImGuiTabBar tabBar, int tabId )
	{
		if ( tabId == 0 )
			return null;
		foreach ( var t in tabBar.Tabs )
			if ( t.ID == tabId )
				return t;
		return null;
	}

	private static float TabBarCalcMaxTabWidth() => G.FontSize * 20.0f;

	private static Vector2 TabItemCalcSize( string label, bool hasCloseButtonOrUnsavedMarker )
	{
		var g = G;
		var labelSize = CalcTextSize( label, true );
		var size = new Vector2( labelSize.x + g.Style.FramePadding.x, labelSize.y + g.Style.FramePadding.y * 2.0f );
		if ( hasCloseButtonOrUnsavedMarker )
			size.x += g.Style.FramePadding.x + (g.Style.ItemInnerSpacing.x + g.FontSize);
		else
			size.x += g.Style.FramePadding.x + 1.0f;
		return new Vector2( MathF.Min( size.x, TabBarCalcMaxTabWidth() ), size.y );
	}

	private static void ShrinkWidths( ShrinkWidthItem[] items, int offset, int count, float widthExcess )
	{
		if ( count <= 0 )
			return;
		if ( count == 1 )
		{
			if ( items[offset].Width >= 0.0f )
				items[offset].Width = MathF.Max( items[offset].Width - widthExcess, 1.0f );
			return;
		}

		Array.Sort( items, offset, count, Comparer<ShrinkWidthItem>.Create( ( a, b ) =>
		{
			int d = b.Width.CompareTo( a.Width );
			return d != 0 ? d : a.Index.CompareTo( b.Index );
		} ) );

		int countSameWidth = 1;
		while ( widthExcess > 0.0f && countSameWidth < count )
		{
			while ( countSameWidth < count && items[offset].Width <= items[offset + countSameWidth].Width )
				countSameWidth++;
			float maxWidthToRemovePerItem = (countSameWidth < count && items[offset + countSameWidth].Width >= 0.0f)
				? items[offset].Width - items[offset + countSameWidth].Width
				: items[offset].Width - 1.0f;
			if ( maxWidthToRemovePerItem <= 0.0f )
				break;
			float widthToRemovePerItem = MathF.Min( widthExcess / countSameWidth, maxWidthToRemovePerItem );
			for ( int n = 0; n < countSameWidth; n++ )
				items[offset + n].Width -= widthToRemovePerItem;
			widthExcess -= widthToRemovePerItem * countSameWidth;
		}

		if ( widthExcess > 0.0f && countSameWidth == count )
		{
			float perItem = widthExcess / count;
			for ( int n = 0; n < count; n++ )
				items[offset + n].Width = MathF.Max( 1.0f, items[offset + n].Width - perItem );
		}

		// Round width and redistribute remainder
		widthExcess = 0.0f;
		for ( int n = 0; n < count; n++ )
		{
			float rounded = ImTrunc( items[offset + n].Width );
			widthExcess += items[offset + n].Width - rounded;
			items[offset + n].Width = rounded;
		}
		for ( int guard = 0; widthExcess > 0.0f && guard < 64; guard++ )
		{
			bool any = false;
			for ( int n = 0; n < count && widthExcess > 0.0f; n++ )
			{
				float add = MathF.Min( items[offset + n].InitialWidth - items[offset + n].Width, 1.0f );
				if ( add <= 0f ) continue;
				items[offset + n].Width += add;
				widthExcess -= add;
				any = true;
			}
			if ( !any ) break;
		}
	}

	private static void TabBarLayout( ImGuiTabBar tabBar )
	{
		var g = G;
		tabBar.WantLayout = false;

		bool scrollToSelectedTab = tabBar.BarRectPrevWidth > tabBar.BarRect.Width;
		tabBar.BarRectPrevWidth = tabBar.BarRect.Width;

		// Garbage collect tabs that were not submitted
		var sections = new TabBarSection[3];
		bool needSortBySection = false;
		for ( int i = tabBar.Tabs.Count - 1; i >= 0; i-- )
		{
			var tab = tabBar.Tabs[i];
			if ( tab.LastFrameVisible < tabBar.PrevFrameVisible || tab.WantClose )
			{
				if ( tabBar.VisibleTabId == tab.ID ) tabBar.VisibleTabId = 0;
				if ( tabBar.SelectedTabId == tab.ID ) tabBar.SelectedTabId = 0;
				if ( tabBar.NextSelectedTabId == tab.ID ) tabBar.NextSelectedTabId = 0;
				tabBar.Tabs.RemoveAt( i );
			}
		}
		for ( int n = 0; n < tabBar.Tabs.Count; n++ )
		{
			var tab = tabBar.Tabs[n];
			tab.IndexDuringLayout = n;
			int sec = TabItemGetSectionIdx( tab );
			if ( n > 0 )
			{
				int prevSec = TabItemGetSectionIdx( tabBar.Tabs[n - 1] );
				if ( sec == 0 && prevSec != 0 ) needSortBySection = true;
				if ( prevSec == 2 && sec != 2 ) needSortBySection = true;
			}
			sections[sec].TabCount++;
		}
		if ( needSortBySection )
		{
			var sorted = tabBar.Tabs.OrderBy( TabItemGetSectionIdx ).ThenBy( t => t.IndexDuringLayout ).ToList();
			tabBar.Tabs.Clear();
			tabBar.Tabs.AddRange( sorted );
		}

		sections[0].Spacing = sections[0].TabCount > 0 && (sections[1].TabCount + sections[2].TabCount) > 0 ? g.Style.ItemInnerSpacing.x : 0.0f;
		sections[1].Spacing = sections[1].TabCount > 0 && sections[2].TabCount > 0 ? g.Style.ItemInnerSpacing.x : 0.0f;

		int scrollToTabId = 0;
		if ( tabBar.NextSelectedTabId != 0 )
		{
			tabBar.SelectedTabId = tabBar.NextSelectedTabId;
			tabBar.NextSelectedTabId = 0;
			scrollToTabId = tabBar.SelectedTabId;
		}

		if ( tabBar.ReorderRequestTabId != 0 )
		{
			if ( TabBarProcessReorder( tabBar ) && tabBar.ReorderRequestTabId == tabBar.SelectedTabId )
				scrollToTabId = tabBar.ReorderRequestTabId;
			tabBar.ReorderRequestTabId = 0;
		}

		if ( (tabBar.Flags & ImGuiTabBarFlags.TabListPopupButton) != 0 )
		{
			var tabToSelect = TabBarTabListPopupButton( tabBar );
			if ( tabToSelect is not null )
				scrollToTabId = tabBar.SelectedTabId = tabToSelect.ID;
		}

		var shrinkBufferIndexes = new int[] { 0, sections[0].TabCount + sections[2].TabCount, sections[0].TabCount };
		var shrinkBuffer = new ShrinkWidthItem[tabBar.Tabs.Count];

		ImGuiTabItem mostRecentlySelectedTab = null;
		int currSectionN = -1;
		bool foundSelectedTabId = false;
		for ( int tabN = 0; tabN < tabBar.Tabs.Count; tabN++ )
		{
			var tab = tabBar.Tabs[tabN];
			if ( (mostRecentlySelectedTab is null || mostRecentlySelectedTab.LastFrameSelected < tab.LastFrameSelected) && (tab.Flags & ImGuiTabItemFlags.Button) == 0 )
				mostRecentlySelectedTab = tab;
			if ( tab.ID == tabBar.SelectedTabId )
				foundSelectedTabId = true;

			bool hasCloseOrMarker = (tab.Flags & TabItemNoCloseButton) == 0 || (tab.Flags & ImGuiTabItemFlags.UnsavedDocument) != 0;
			tab.ContentWidth = tab.RequestedWidth >= 0.0f ? tab.RequestedWidth : TabItemCalcSize( tab.Label ?? "", hasCloseOrMarker ).x;

			int sectionN = TabItemGetSectionIdx( tab );
			sections[sectionN].Width += tab.ContentWidth + (sectionN == currSectionN ? g.Style.ItemInnerSpacing.x : 0.0f);
			currSectionN = sectionN;

			int bi = shrinkBufferIndexes[sectionN]++;
			shrinkBuffer[bi] = new ShrinkWidthItem { Index = tabN, Width = tab.ContentWidth, InitialWidth = tab.ContentWidth };
			tab.Width = MathF.Max( tab.ContentWidth, 1.0f );
		}

		tabBar.WidthAllTabsIdeal = 0.0f;
		for ( int s = 0; s < 3; s++ )
			tabBar.WidthAllTabsIdeal += sections[s].Width + sections[s].Spacing;

		// Horizontal scrolling buttons
		if ( tabBar.WidthAllTabsIdeal > tabBar.BarRect.Width && tabBar.Tabs.Count > 1 && (tabBar.Flags & ImGuiTabBarFlags.NoTabListScrollingButtons) == 0 && (tabBar.Flags & ImGuiTabBarFlags.FittingPolicyScroll) != 0 )
		{
			var scrollAndSelectTab = TabBarScrollingButtons( tabBar );
			if ( scrollAndSelectTab is not null )
			{
				scrollToTabId = scrollAndSelectTab.ID;
				if ( (scrollAndSelectTab.Flags & ImGuiTabItemFlags.Button) == 0 )
					tabBar.SelectedTabId = scrollToTabId;
			}
		}

		// Shrink widths if full tabs don't fit in their allocated space
		float section0W = sections[0].Width + sections[0].Spacing;
		float section1W = sections[1].Width + sections[1].Spacing;
		float section2W = sections[2].Width + sections[2].Spacing;
		bool centralSectionIsVisible = section0W + section2W < tabBar.BarRect.Width;
		float widthExcess = centralSectionIsVisible
			? MathF.Max( section1W - (tabBar.BarRect.Width - section0W - section2W), 0.0f )
			: (section0W + section2W) - tabBar.BarRect.Width;

		if ( widthExcess >= 1.0f && ((tabBar.Flags & ImGuiTabBarFlags.FittingPolicyResizeDown) != 0 || !centralSectionIsVisible) )
		{
			int shrinkDataCount = centralSectionIsVisible ? sections[1].TabCount : sections[0].TabCount + sections[2].TabCount;
			int shrinkDataOffset = centralSectionIsVisible ? sections[0].TabCount + sections[2].TabCount : 0;
			ShrinkWidths( shrinkBuffer, shrinkDataOffset, shrinkDataCount, widthExcess );

			for ( int n = shrinkDataOffset; n < shrinkDataOffset + shrinkDataCount; n++ )
			{
				var tab = tabBar.Tabs[shrinkBuffer[n].Index];
				float shrinkedWidth = ImTrunc( shrinkBuffer[n].Width );
				if ( shrinkedWidth < 0.0f )
					continue;
				shrinkedWidth = MathF.Max( 1.0f, shrinkedWidth );
				int sectionN = TabItemGetSectionIdx( tab );
				sections[sectionN].Width -= tab.Width - shrinkedWidth;
				tab.Width = shrinkedWidth;
			}
		}

		// Layout all active tabs
		int sectionTabIndex = 0;
		float tabOffset = 0.0f;
		tabBar.WidthAllTabs = 0.0f;
		for ( int s = 0; s < 3; s++ )
		{
			var section = sections[s];
			if ( s == 2 )
				tabOffset = MathF.Min( MathF.Max( 0.0f, tabBar.BarRect.Width - section.Width ), tabOffset );
			for ( int tabN = 0; tabN < section.TabCount; tabN++ )
			{
				var tab = tabBar.Tabs[sectionTabIndex + tabN];
				tab.Offset = tabOffset;
				tabOffset += tab.Width + (tabN < section.TabCount - 1 ? g.Style.ItemInnerSpacing.x : 0.0f);
			}
			tabBar.WidthAllTabs += MathF.Max( section.Width + section.Spacing, 0.0f );
			tabOffset += section.Spacing;
			sectionTabIndex += section.TabCount;
		}

		if ( !foundSelectedTabId )
			tabBar.SelectedTabId = 0;
		if ( tabBar.SelectedTabId == 0 && tabBar.NextSelectedTabId == 0 && mostRecentlySelectedTab is not null )
			scrollToTabId = tabBar.SelectedTabId = mostRecentlySelectedTab.ID;

		tabBar.VisibleTabId = tabBar.SelectedTabId;
		tabBar.VisibleTabWasSubmitted = false;

		if ( scrollToTabId != 0 )
			TabBarScrollToTab( tabBar, scrollToTabId, sections );
		else if ( scrollToSelectedTab && tabBar.SelectedTabId != 0 )
			TabBarScrollToTab( tabBar, tabBar.SelectedTabId, sections );

		// Update scrolling
		tabBar.ScrollingAnim = TabBarScrollClamp( tabBar, tabBar.ScrollingAnim );
		tabBar.ScrollingTarget = TabBarScrollClamp( tabBar, tabBar.ScrollingTarget );
		if ( tabBar.ScrollingAnim != tabBar.ScrollingTarget )
		{
			tabBar.ScrollingSpeed = MathF.Max( tabBar.ScrollingSpeed, 70.0f * g.FontSize );
			tabBar.ScrollingSpeed = MathF.Max( tabBar.ScrollingSpeed, MathF.Abs( tabBar.ScrollingTarget - tabBar.ScrollingAnim ) / 0.3f );
			bool teleport = tabBar.PrevFrameVisible + 1 < g.FrameCount || tabBar.ScrollingTargetDistToVisibility > 10.0f * g.FontSize;
			tabBar.ScrollingAnim = teleport ? tabBar.ScrollingTarget : ImLinearSweep( tabBar.ScrollingAnim, tabBar.ScrollingTarget, g.IO.DeltaTime * tabBar.ScrollingSpeed );
		}
		else
		{
			tabBar.ScrollingSpeed = 0.0f;
		}
		tabBar.ScrollingRectMinX = tabBar.BarRect.Min.x + sections[0].Width + sections[0].Spacing;
		tabBar.ScrollingRectMaxX = tabBar.BarRect.Max.x - sections[2].Width - sections[1].Spacing;

		// Actual layout in host window
		var window = g.CurrentWindow;
		window.DC.CursorPos = tabBar.BarRect.Min;
		ItemSize( new Vector2( tabBar.WidthAllTabs, tabBar.BarRect.Height ), tabBar.FramePadding.y );
		window.DC.IdealMaxPos = new Vector2( MathF.Max( window.DC.IdealMaxPos.x, tabBar.BarRect.Min.x + tabBar.WidthAllTabsIdeal ), window.DC.IdealMaxPos.y );
	}

	private static float TabBarScrollClamp( ImGuiTabBar tabBar, float scrolling )
	{
		scrolling = MathF.Min( scrolling, tabBar.WidthAllTabs - tabBar.BarRect.Width );
		return MathF.Max( scrolling, 0.0f );
	}

	private static void TabBarScrollToTab( ImGuiTabBar tabBar, int tabId, TabBarSection[] sections )
	{
		var g = G;
		var tab = TabBarFindTabByID( tabBar, tabId );
		if ( tab is null || (tab.Flags & TabItemSectionMask) != 0 )
			return;

		float margin = g.FontSize * 1.0f;
		int order = tabBar.Tabs.IndexOf( tab );
		float scrollableWidth = tabBar.BarRect.Width - sections[0].Width - sections[2].Width - sections[1].Spacing;

		float tabX1 = tab.Offset - sections[0].Width + (order > sections[0].TabCount - 1 ? -margin : 0.0f);
		float tabX2 = tab.Offset - sections[0].Width + tab.Width + (order + 1 < tabBar.Tabs.Count - sections[2].TabCount ? margin : 1.0f);
		tabBar.ScrollingTargetDistToVisibility = 0.0f;
		if ( tabBar.ScrollingTarget > tabX1 || tabX2 - tabX1 >= scrollableWidth )
		{
			tabBar.ScrollingTargetDistToVisibility = MathF.Max( tabBar.ScrollingAnim - tabX2, 0.0f );
			tabBar.ScrollingTarget = tabX1;
		}
		else if ( tabBar.ScrollingTarget < tabX2 - scrollableWidth )
		{
			tabBar.ScrollingTargetDistToVisibility = MathF.Max( (tabX1 - scrollableWidth) - tabBar.ScrollingAnim, 0.0f );
			tabBar.ScrollingTarget = tabX2 - scrollableWidth;
		}
	}

	private static void TabBarQueueReorderFromMousePos( ImGuiTabBar tabBar, ImGuiTabItem srcTab, Vector2 mousePos )
	{
		var g = G;
		if ( (tabBar.Flags & ImGuiTabBarFlags.Reorderable) == 0 )
			return;

		bool isCentralSection = (srcTab.Flags & TabItemSectionMask) == 0;
		float barOffset = tabBar.BarRect.Min.x - (isCentralSection ? tabBar.ScrollingTarget : 0);
		int dir = barOffset + srcTab.Offset > mousePos.x ? -1 : +1;
		int srcIdx = tabBar.Tabs.IndexOf( srcTab );
		int dstIdx = srcIdx;
		for ( int i = srcIdx; i >= 0 && i < tabBar.Tabs.Count; i += dir )
		{
			var dstTab = tabBar.Tabs[i];
			if ( (dstTab.Flags & ImGuiTabItemFlags.NoReorder) != 0 )
				break;
			if ( (dstTab.Flags & TabItemSectionMask) != (srcTab.Flags & TabItemSectionMask) )
				break;
			dstIdx = i;
			float x1 = barOffset + dstTab.Offset - g.Style.ItemInnerSpacing.x;
			float x2 = barOffset + dstTab.Offset + dstTab.Width + g.Style.ItemInnerSpacing.x;
			if ( (dir < 0 && mousePos.x > x1) || (dir > 0 && mousePos.x < x2) )
				break;
		}

		if ( dstIdx != srcIdx )
		{
			tabBar.ReorderRequestTabId = srcTab.ID;
			tabBar.ReorderRequestOffset = dstIdx - srcIdx;
		}
	}

	private static bool TabBarProcessReorder( ImGuiTabBar tabBar )
	{
		var tab1 = TabBarFindTabByID( tabBar, tabBar.ReorderRequestTabId );
		if ( tab1 is null || (tab1.Flags & ImGuiTabItemFlags.NoReorder) != 0 )
			return false;
		int idx1 = tabBar.Tabs.IndexOf( tab1 );
		int tab2Order = idx1 + tabBar.ReorderRequestOffset;
		if ( tab2Order < 0 || tab2Order >= tabBar.Tabs.Count )
			return false;
		var tab2 = tabBar.Tabs[tab2Order];
		if ( (tab2.Flags & ImGuiTabItemFlags.NoReorder) != 0 )
			return false;
		if ( (tab1.Flags & TabItemSectionMask) != (tab2.Flags & TabItemSectionMask) )
			return false;
		tabBar.Tabs.RemoveAt( idx1 );
		tabBar.Tabs.Insert( tab2Order, tab1 );
		return true;
	}

	private static ImGuiTabItem TabBarScrollingButtons( ImGuiTabBar tabBar )
	{
		var g = G;
		var window = g.CurrentWindow;

		var arrowButtonSize = new Vector2( g.FontSize - 2.0f, g.FontSize + g.Style.FramePadding.y * 2.0f );
		float scrollingButtonsWidth = arrowButtonSize.x * 2.0f;
		var backupCursorPos = window.DC.CursorPos;

		int selectDir = 0;
		var arrowCol = g.Style.Colors[(int)ImGuiCol.Text];
		arrowCol.w *= 0.5f;

		PushStyleColor( ImGuiCol.Text, arrowCol );
		PushStyleColor( ImGuiCol.Button, new Vector4( 0, 0, 0, 0 ) );
		PushItemFlag( ImGuiItemFlags.ButtonRepeat | ImGuiItemFlags.NoNav, true );
		float backupRepeatDelay = g.IO.KeyRepeatDelay;
		float backupRepeatRate = g.IO.KeyRepeatRate;
		g.IO.KeyRepeatDelay = 0.250f;
		g.IO.KeyRepeatRate = 0.200f;
		float x = MathF.Max( tabBar.BarRect.Min.x, tabBar.BarRect.Max.x - scrollingButtonsWidth );
		window.DC.CursorPos = new Vector2( x, tabBar.BarRect.Min.y );
		if ( ArrowButtonEx( "##<", ImGuiDir.Left, arrowButtonSize, ImGuiButtonFlags.PressedOnClick ) )
			selectDir = -1;
		window.DC.CursorPos = new Vector2( x + arrowButtonSize.x, tabBar.BarRect.Min.y );
		if ( ArrowButtonEx( "##>", ImGuiDir.Right, arrowButtonSize, ImGuiButtonFlags.PressedOnClick ) )
			selectDir = +1;
		PopItemFlag();
		PopStyleColor( 2 );
		g.IO.KeyRepeatRate = backupRepeatRate;
		g.IO.KeyRepeatDelay = backupRepeatDelay;

		ImGuiTabItem tabToScrollTo = null;
		if ( selectDir != 0 )
		{
			var tabItem = TabBarFindTabByID( tabBar, tabBar.SelectedTabId );
			if ( tabItem is not null )
			{
				int selectedOrder = tabBar.Tabs.IndexOf( tabItem );
				int targetOrder = selectedOrder + selectDir;
				for ( int guard = 0; tabToScrollTo is null && guard < tabBar.Tabs.Count + 2; guard++ )
				{
					tabToScrollTo = tabBar.Tabs[(targetOrder >= 0 && targetOrder < tabBar.Tabs.Count) ? targetOrder : Math.Clamp( selectedOrder, 0, tabBar.Tabs.Count - 1 )];
					if ( (tabToScrollTo.Flags & ImGuiTabItemFlags.Button) != 0 )
					{
						targetOrder += selectDir;
						selectedOrder += selectDir;
						tabToScrollTo = (targetOrder < 0 || targetOrder >= tabBar.Tabs.Count) ? tabToScrollTo : null;
					}
				}
			}
		}
		window.DC.CursorPos = backupCursorPos;
		tabBar.BarRect.Max.x -= scrollingButtonsWidth + 1.0f;
		return tabToScrollTo;
	}

	private static ImGuiTabItem TabBarTabListPopupButton( ImGuiTabBar tabBar )
	{
		var g = G;
		var window = g.CurrentWindow;

		float tabListPopupButtonWidth = g.FontSize + g.Style.FramePadding.y;
		var backupCursorPos = window.DC.CursorPos;
		window.DC.CursorPos = new Vector2( tabBar.BarRect.Min.x - g.Style.FramePadding.y, tabBar.BarRect.Min.y );
		tabBar.BarRect.Min.x += tabListPopupButtonWidth;

		var arrowCol = g.Style.Colors[(int)ImGuiCol.Text];
		arrowCol.w *= 0.5f;
		PushStyleColor( ImGuiCol.Text, arrowCol );
		PushStyleColor( ImGuiCol.Button, new Vector4( 0, 0, 0, 0 ) );
		bool open = BeginCombo( "##v", null, ImGuiComboFlags.NoPreview | ImGuiComboFlags.HeightLargest );
		PopStyleColor( 2 );

		ImGuiTabItem tabToSelect = null;
		if ( open )
		{
			foreach ( var tab in tabBar.Tabs )
			{
				if ( (tab.Flags & ImGuiTabItemFlags.Button) != 0 )
					continue;
				PushID( tab.ID );
				if ( Selectable( LabelText( tab.Label ?? "" ), tabBar.SelectedTabId == tab.ID ) )
					tabToSelect = tab;
				PopID();
			}
			EndCombo();
		}

		window.DC.CursorPos = backupCursorPos;
		return tabToSelect;
	}
	#endregion

	#region Tab items
	/// <summary>Create a Tab. Returns true if the Tab is selected.</summary>
	public static bool BeginTabItem( string label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None )
	{
		bool dummy = true;
		return BeginTabItemImpl( label, false, ref dummy, flags );
	}

	/// <summary>Create a Tab with a close button. When closed, <paramref name="open"/> is set to false.</summary>
	public static bool BeginTabItem( string label, ref bool open, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None )
		=> BeginTabItemImpl( label, true, ref open, flags );

	private static bool BeginTabItemImpl( string label, bool hasOpen, ref bool open, ImGuiTabItemFlags flags )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return false;

		var tabBar = g.CurrentTabBar;
		if ( tabBar is null )
		{
			Log.Warning( "ImGui: BeginTabItem() needs to be called between BeginTabBar() and EndTabBar()" );
			return false;
		}
		if ( (flags & ImGuiTabItemFlags.Button) != 0 )
			flags &= ~ImGuiTabItemFlags.Button;

		bool ret = TabItemEx( tabBar, label, hasOpen, ref open, flags );
		if ( ret && (flags & ImGuiTabItemFlags.NoPushId) == 0 )
		{
			var tab = tabBar.Tabs[tabBar.LastTabItemIdx];
			PushOverrideID( tab.ID );
		}
		return ret;
	}

	/// <summary>Only call EndTabItem() if BeginTabItem() returns true!</summary>
	public static void EndTabItem()
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return;
		var tabBar = g.CurrentTabBar;
		if ( tabBar is null || tabBar.LastTabItemIdx < 0 || tabBar.LastTabItemIdx >= tabBar.Tabs.Count )
		{
			Log.Warning( "ImGui: EndTabItem() needs to be called between BeginTabBar() and EndTabBar()" );
			return;
		}
		var tab = tabBar.Tabs[tabBar.LastTabItemIdx];
		if ( (tab.Flags & ImGuiTabItemFlags.NoPushId) == 0 )
			PopID();
	}

	/// <summary>Create a Tab behaving like a button. Return true when clicked. Cannot be selected in the tab bar.</summary>
	public static bool TabItemButton( string label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None )
	{
		var g = G;
		var window = g.CurrentWindow;
		if ( window.SkipItems )
			return false;
		var tabBar = g.CurrentTabBar;
		if ( tabBar is null )
		{
			Log.Warning( "ImGui: TabItemButton() needs to be called between BeginTabBar() and EndTabBar()" );
			return false;
		}
		bool dummy = true;
		return TabItemEx( tabBar, label, false, ref dummy, flags | ImGuiTabItemFlags.Button | ImGuiTabItemFlags.NoReorder );
	}

	/// <summary>Notify TabBar or Docking system of a closed tab/window ahead (useful to reduce visual flicker on reorderable tab bars).</summary>
	public static void SetTabItemClosed( string tabOrDockedWindowLabel )
	{
		var g = G;
		var tabBar = g.CurrentTabBar;
		if ( tabBar is null )
			return;
		int tabId = g.CurrentWindow.GetID( tabOrDockedWindowLabel );
		var tab = TabBarFindTabByID( tabBar, tabId );
		if ( tab is not null )
			tab.WantClose = true;
	}

	private static void TabBarCloseTab( ImGuiTabBar tabBar, ImGuiTabItem tab )
	{
		if ( (tab.Flags & ImGuiTabItemFlags.Button) != 0 )
			return;
		if ( (tab.Flags & (ImGuiTabItemFlags.UnsavedDocument | ImGuiTabItemFlags.NoAssumedClosure)) == 0 )
		{
			tab.WantClose = true;
			if ( tabBar.VisibleTabId == tab.ID )
			{
				tab.LastFrameVisible = -1;
				tabBar.SelectedTabId = tabBar.NextSelectedTabId = 0;
			}
		}
		else
		{
			if ( tabBar.VisibleTabId != tab.ID )
				tabBar.NextSelectedTabId = tab.ID;
		}
	}

	private static bool TabItemEx( ImGuiTabBar tabBar, string label, bool hasOpen, ref bool open, ImGuiTabItemFlags flags )
	{
		var g = G;
		var window = g.CurrentWindow;

		if ( tabBar.WantLayout )
		{
			var backupNextItemData = g.NextItemData;
			TabBarLayout( tabBar );
			g.NextItemData = backupNextItemData;
		}
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );

		if ( hasOpen && !open )
		{
			ItemAdd( new ImRect(), id, null, ImGuiItemFlags.NoNav );
			return false;
		}

		if ( !hasOpen )
			flags |= TabItemNoCloseButton;

		var tab = TabBarFindTabByID( tabBar, id );
		bool tabIsNew = false;
		if ( tab is null )
		{
			tab = new ImGuiTabItem { ID = id };
			tabBar.Tabs.Add( tab );
			tabBar.TabsAddedNew = tabIsNew = true;
		}
		tabBar.LastTabItemIdx = tabBar.Tabs.IndexOf( tab );

		var size = TabItemCalcSize( label, hasOpen || (flags & ImGuiTabItemFlags.UnsavedDocument) != 0 );
		tab.RequestedWidth = -1.0f;
		if ( (g.NextItemData.Flags & ImGuiNextItemDataFlags.HasWidth) != 0 )
			size.x = tab.RequestedWidth = g.NextItemData.Width;
		if ( tabIsNew )
			tab.Width = MathF.Max( 1.0f, size.x );
		tab.ContentWidth = size.x;
		tab.BeginOrder = tabBar.TabsActiveCount++;

		bool tabBarAppearing = tabBar.PrevFrameVisible + 1 < g.FrameCount;
		bool tabAppearing = tab.LastFrameVisible + 1 < g.FrameCount;
		bool tabJustUnsaved = (flags & ImGuiTabItemFlags.UnsavedDocument) != 0 && (tab.Flags & ImGuiTabItemFlags.UnsavedDocument) == 0;
		bool isTabButton = (flags & ImGuiTabItemFlags.Button) != 0;
		tab.LastFrameVisible = g.FrameCount;
		tab.Flags = flags;
		tab.Label = label;

		if ( !isTabButton )
		{
			if ( tabAppearing && (tabBar.Flags & ImGuiTabBarFlags.AutoSelectNewTabs) != 0 && tabBar.NextSelectedTabId == 0 )
				if ( !tabBarAppearing || tabBar.SelectedTabId == 0 )
					tabBar.NextSelectedTabId = id;
			if ( (flags & ImGuiTabItemFlags.SetSelected) != 0 && tabBar.SelectedTabId != id )
				tabBar.NextSelectedTabId = id;
		}

		bool tabContentsVisible = tabBar.VisibleTabId == id;
		if ( tabContentsVisible )
			tabBar.VisibleTabWasSubmitted = true;

		if ( !tabContentsVisible && tabBar.SelectedTabId == 0 && tabBarAppearing )
			if ( tabBar.Tabs.Count == 1 && (tabBar.Flags & ImGuiTabBarFlags.AutoSelectNewTabs) == 0 )
				tabContentsVisible = true;

		if ( tabAppearing && (!tabBarAppearing || tabIsNew) )
		{
			ItemAdd( new ImRect(), id, null, ImGuiItemFlags.NoNav );
			if ( isTabButton )
				return false;
			return tabContentsVisible;
		}

		if ( tabBar.SelectedTabId == id )
			tab.LastFrameSelected = g.FrameCount;

		var backupMainCursorPos = window.DC.CursorPos;

		bool isCentralSection = (tab.Flags & TabItemSectionMask) == 0;
		size.x = tab.Width;
		if ( isCentralSection )
			window.DC.CursorPos = tabBar.BarRect.Min + new Vector2( ImTrunc( tab.Offset - tabBar.ScrollingAnim ), 0.0f );
		else
			window.DC.CursorPos = tabBar.BarRect.Min + new Vector2( tab.Offset, 0.0f );
		var pos = window.DC.CursorPos;
		var bb = new ImRect( pos, pos + size );

		bool wantClipRect = isCentralSection && (bb.Min.x < tabBar.ScrollingRectMinX || bb.Max.x > tabBar.ScrollingRectMaxX);
		if ( wantClipRect )
			PushClipRect( new Vector2( MathF.Max( bb.Min.x, tabBar.ScrollingRectMinX ), bb.Min.y - 1 ), new Vector2( tabBar.ScrollingRectMaxX, bb.Max.y ), true );

		var backupCursorMaxPos = window.DC.CursorMaxPos;
		ItemSize( bb.Size, style.FramePadding.y );
		window.DC.CursorMaxPos = backupCursorMaxPos;

		if ( !ItemAdd( bb, id ) )
		{
			if ( wantClipRect )
				PopClipRect();
			window.DC.CursorPos = backupMainCursorPos;
			return tabContentsVisible;
		}

		int closeButtonId = hasOpen ? ImHashStr( "#CLOSE", id ) : 0;

		var buttonFlags = (isTabButton ? ImGuiButtonFlags.PressedOnClickRelease : ImGuiButtonFlags.PressedOnClick) | ImGuiButtonFlags.AllowOverlap;
		if ( g.DragDropActive )
			buttonFlags |= ImGuiButtonFlags.PressedOnDragDropHold;

		bool hovered = false, held = false, pressed = false;
		// Let the close button (submitted after us) win the hover when it was hovered last frame.
		bool closeButtonOnTop = closeButtonId != 0 && (g.HoveredIdPreviousFrame == closeButtonId || g.ActiveId == closeButtonId) && g.ActiveId != id;
		if ( !closeButtonOnTop )
			pressed = ButtonBehavior( bb, id, out hovered, out held, buttonFlags );
		if ( pressed && !isTabButton )
			tabBar.NextSelectedTabId = id;

		// Drag to reorder
		if ( held && !tabAppearing && IsMouseDragging( ImGuiMouseButton.Left ) )
		{
			if ( !g.DragDropActive && (tabBar.Flags & ImGuiTabBarFlags.Reorderable) != 0 )
			{
				if ( g.IO.MouseDelta.x < 0.0f && g.IO.MousePos.x < bb.Min.x )
					TabBarQueueReorderFromMousePos( tabBar, tab, g.IO.MousePos );
				else if ( g.IO.MouseDelta.x > 0.0f && g.IO.MousePos.x > bb.Max.x )
					TabBarQueueReorderFromMousePos( tabBar, tab, g.IO.MousePos );
			}
		}

		// Render tab shape
		var displayDrawList = window.DrawList;
		bool isHoveredAny = hovered || held || g.HoveredId == closeButtonId && closeButtonId != 0;
		var tabCol = GetColorU32Internal( (held || isHoveredAny) ? ImGuiCol.TabHovered : tabContentsVisible ? ImGuiCol.TabSelected : ImGuiCol.Tab );
		TabItemBackground( displayDrawList, bb, flags, tabCol );
		if ( tabContentsVisible && (tabBar.Flags & ImGuiTabBarFlags.DrawSelectedOverline) != 0 && style.TabBarOverlineSize > 0.0f )
		{
			var overlineCol = GetColorU32Internal( ImGuiCol.TabSelectedOverline );
			displayDrawList.AddRectFilled( bb.TL, new Vector2( bb.Max.x, bb.Min.y + style.TabBarOverlineSize ), overlineCol, style.TabRounding, ImDrawFlags.RoundCornersTop );
		}

		// Select with right mouse button (common idiom for context menus)
		bool hoveredUnblocked = IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup );
		if ( tabBar.SelectedTabId != tab.ID && hoveredUnblocked && (IsMouseClicked( ImGuiMouseButton.Right ) || IsMouseReleased( ImGuiMouseButton.Right )) && !isTabButton )
			tabBar.NextSelectedTabId = id;

		if ( (tabBar.Flags & ImGuiTabBarFlags.NoCloseWithMiddleMouseButton) != 0 )
			flags |= ImGuiTabItemFlags.NoCloseWithMiddleMouseButton;

		TabItemLabelAndCloseButton( displayDrawList, bb, tabJustUnsaved ? (flags & ~ImGuiTabItemFlags.UnsavedDocument) : flags, tabBar.FramePadding, label, id, closeButtonId, tabContentsVisible, out bool justClosed, out bool textClipped );
		if ( justClosed && hasOpen )
		{
			open = false;
			TabBarCloseTab( tabBar, tab );
		}

		if ( wantClipRect )
			PopClipRect();
		window.DC.CursorPos = backupMainCursorPos;

		// Tooltip with full label when truncated
		if ( textClipped && g.HoveredId == id && !held )
			if ( (tabBar.Flags & ImGuiTabBarFlags.NoTooltip) == 0 && (tab.Flags & ImGuiTabItemFlags.NoTooltip) == 0 )
				SetItemTooltip( "{0}", LabelText( label ) );

		if ( isTabButton )
			return pressed;
		return tabContentsVisible;
	}

	private static void TabItemBackground( ImDrawList drawList, ImRect bb, ImGuiTabItemFlags flags, Color32 col )
	{
		var g = G;
		float width = bb.Width;
		float rounding = MathF.Max( 0.0f, MathF.Min( (flags & ImGuiTabItemFlags.Button) != 0 ? g.Style.FrameRounding : g.Style.TabRounding, width * 0.5f - 1.0f ) );
		float y1 = bb.Min.y + 1.0f;
		float y2 = bb.Max.y - g.Style.TabBarBorderSize;
		drawList.AddRectFilled( new Vector2( bb.Min.x, y1 ), new Vector2( bb.Max.x, y2 ), col, rounding, ImDrawFlags.RoundCornersTop );
		if ( g.Style.TabBorderSize > 0.0f )
			drawList.AddRect( new Vector2( bb.Min.x, y1 ), new Vector2( bb.Max.x, y2 ), GetColorU32Internal( ImGuiCol.Border ), rounding, ImDrawFlags.RoundCornersTop, g.Style.TabBorderSize );
	}

	private static void TabItemLabelAndCloseButton( ImDrawList drawList, ImRect bb, ImGuiTabItemFlags flags, Vector2 framePadding, string label, int tabId, int closeButtonId, bool isContentsVisible, out bool outJustClosed, out bool outTextClipped )
	{
		var g = G;
		var labelSize = CalcTextSize( label, true );
		outJustClosed = false;
		outTextClipped = false;
		if ( bb.Width <= 1.0f )
			return;

		var textEllipsisClipBb = new ImRect( bb.Min.x + framePadding.x, bb.Min.y + framePadding.y, bb.Max.x - framePadding.x, bb.Max.y );
		outTextClipped = textEllipsisClipBb.Min.x + labelSize.x > textEllipsisClipBb.Max.x;

		float buttonSz = g.FontSize;
		var buttonPos = new Vector2( MathF.Max( bb.Min.x, bb.Max.x - framePadding.x - buttonSz ), bb.Min.y + framePadding.y );

		bool closeButtonPressed = false;
		bool closeButtonVisible = false;
		bool isHovered = g.HoveredId == tabId || (closeButtonId != 0 && g.HoveredId == closeButtonId) || g.ActiveId == tabId || (closeButtonId != 0 && g.ActiveId == closeButtonId);
		if ( closeButtonId != 0 )
		{
			if ( isContentsVisible )
				closeButtonVisible = g.Style.TabCloseButtonMinWidthSelected < 0.0f || (isHovered && bb.Width >= MathF.Max( buttonSz, g.Style.TabCloseButtonMinWidthSelected ));
			else
				closeButtonVisible = g.Style.TabCloseButtonMinWidthUnselected < 0.0f || (isHovered && bb.Width >= MathF.Max( buttonSz, g.Style.TabCloseButtonMinWidthUnselected ));
		}

		bool unsavedMarkerVisible = (flags & ImGuiTabItemFlags.UnsavedDocument) != 0 && buttonPos.x + buttonSz <= bb.Max.x && (!closeButtonVisible || !isHovered);
		if ( unsavedMarkerVisible )
		{
			var bulletBb = new ImRect( buttonPos, buttonPos + new Vector2( buttonSz, buttonSz ) );
			RenderBullet( drawList, bulletBb.Center, GetColorU32Internal( ImGuiCol.Text ) );
		}
		else if ( closeButtonVisible )
		{
			var lastItemBackup = g.LastItemData;
			if ( CloseButton( closeButtonId, buttonPos ) )
				closeButtonPressed = true;
			g.LastItemData = lastItemBackup;

			if ( isHovered && (flags & ImGuiTabItemFlags.NoCloseWithMiddleMouseButton) == 0 && IsMouseClicked( ImGuiMouseButton.Middle ) )
				closeButtonPressed = true;
		}

		if ( closeButtonVisible || unsavedMarkerVisible )
			textEllipsisClipBb.Max.x -= buttonSz * (unsavedMarkerVisible ? 0.80f : 1.0f);
		float ellipsisMaxX = textEllipsisClipBb.Max.x;

		RenderTextEllipsis( drawList, textEllipsisClipBb.Min, textEllipsisClipBb.Max, textEllipsisClipBb.Max.x, ellipsisMaxX, label, labelSize );

		outJustClosed = closeButtonPressed;
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.Table)

public static partial class ImGui
{
	private const float FLT_MIN_NORMAL = 1.17549435E-38f;
	private const float TABLE_RESIZE_HIT_HALF = 4.0f;

	internal static ImGuiTable CurrentTable
	{
		get
		{
			var g = G;
			var window = g?.CurrentWindow;
			if ( window is null )
				return null;
			int idx = window.DC.CurrentTableIdx;
			return idx >= 0 && idx < g.TablesStack.Count ? g.TablesStack[idx] : null;
		}
	}

	internal static void NewFrameTables()
	{
		var g = G;
		g.TablesStack.Clear();
		g.SplitDrawLists.Clear();
		g.TabBarStack.Clear();
	}

	#region Begin/End
	public static bool BeginTable( string strId, int columns, ImGuiTableFlags flags = ImGuiTableFlags.None, Vector2 outerSize = default, float innerWidth = 0.0f )
	{
		var g = G;
		var outerWindow = GetCurrentWindow();
		if ( outerWindow.SkipItems )
			return false;
		if ( columns <= 0 || columns > 512 )
		{
			Log.Warning( "ImGui: BeginTable() requires 1..512 columns" );
			return false;
		}

		int id = outerWindow.GetID( strId );
		bool useChildWindow = (flags & (ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY)) != 0;
		var availSize = GetContentRegionAvail();
		var actualOuterSize = CalcItemSize( outerSize, MathF.Max( availSize.x, 1.0f ), useChildWindow ? MathF.Max( availSize.y, 1.0f ) : 0.0f );
		var outerRect = new ImRect( outerWindow.DC.CursorPos, outerWindow.DC.CursorPos + actualOuterSize );

		if ( useChildWindow && IsClippedEx( outerRect, 0 ) )
		{
			ItemSize( outerRect );
			ItemAdd( outerRect, id );
			return false;
		}

		if ( !g.Tables.TryGetValue( id, out var table ) )
		{
			table = new ImGuiTable { ID = id };
			g.Tables[id] = table;
		}
		table.IsFirstFrame = table.ColumnsCount != columns;
		if ( table.ColumnsCount != columns )
		{
			table.Columns.Clear();
			for ( int i = 0; i < columns; i++ )
				table.Columns.Add( new ImGuiTableColumn { DisplayOrder = i } );
			table.ColumnsCount = columns;
		}

		// Flags fix-ups
		if ( (flags & ImGuiTableFlags.SizingMask_) == 0 )
			flags |= ((flags & ImGuiTableFlags.ScrollX) != 0 || (outerWindow.Flags & ImGuiWindowFlags.AlwaysAutoResize) != 0) ? ImGuiTableFlags.SizingFixedFit : ImGuiTableFlags.SizingStretchSame;
		if ( (flags & ImGuiTableFlags.ScrollX) == 0 && (flags & ImGuiTableFlags.ScrollY) == 0 )
		{
			// nothing
		}

		table.Flags = flags;
		table.OuterWindow = outerWindow;
		table.InnerWidth = innerWidth;
		table.IsLayoutLocked = false;
		table.IsInsideRow = false;
		table.DeclColumnsCount = 0;
		table.CurrentRow = -1;
		table.CurrentColumn = -1;
		table.RowBgColorCounter = 0;
		table.RowLines.Clear();
		table.HasHeaders = false;
		table.HeaderBottomY = float.MinValue;
		table.FreezeRowsRequest = table.FreezeColumnsRequest = 0;
		table.CellPaddingY = g.Style.CellPadding.y;
		table.ReorderColumn = -1;
		foreach ( var c in table.Columns )
		{
			c.ContentWidthPrev = c.ContentWidthThisFrame;
			c.ContentWidthThisFrame = 0f;
		}

		// Push table on the stack
		g.TablesStack.Add( table );
		int tableIdx = g.TablesStack.Count - 1;
		table.HostBackupCurrentTableIdx = outerWindow.DC.CurrentTableIdx;

		if ( useChildWindow )
		{
			var childFlags = (flags & ImGuiTableFlags.ScrollX) != 0 ? ImGuiWindowFlags.HorizontalScrollbar : ImGuiWindowFlags.None;
			BeginChildEx( null, ImHashStr( "##TableChild", id ), outerRect.Size, ImGuiChildFlags.None, childFlags | ImGuiWindowFlags.NoSavedSettings );
			table.InnerWindow = g.CurrentWindow;
			table.OuterRect = table.InnerWindow.Rect();
			table.InnerRect = table.InnerWindow.InnerRect;
			table.WorkRect = table.InnerWindow.WorkRect;
			table.InnerClipRect = table.InnerWindow.InnerClipRect;
			table.StartY = table.InnerWindow.DC.CursorPos.y;
		}
		else
		{
			table.InnerWindow = outerWindow;
			table.OuterRect = outerRect;
			table.InnerRect = outerRect;
			table.WorkRect = outerRect;
			var clip = outerWindow.ClipRect;
			table.InnerClipRect = new ImRect( MathF.Max( clip.Min.x, outerRect.Min.x ), clip.Min.y, MathF.Min( clip.Max.x, outerRect.Max.x ), clip.Max.y );
			table.StartY = outerRect.Min.y;
		}

		var inner = table.InnerWindow;
		table.InnerBackupCurrentTableIdx = inner.DC.CurrentTableIdx;
		inner.DC.CurrentTableIdx = tableIdx;
		if ( !useChildWindow )
			outerWindow.DC.CurrentTableIdx = tableIdx;

		table.HostBackupCursorMaxPos = inner.DC.CursorMaxPos;
		table.HostBackupWorkRect = inner.WorkRect;
		table.HostBackupParentWorkRect = inner.ParentWorkRect;
		table.HostBackupColumnsOffset = inner.DC.ColumnsOffset;
		table.HostBackupItemWidth = inner.DC.ItemWidth;
		table.HostBackupItemWidthStackCount = inner.DC.ItemWidthStack.Count;
		table.HostBackupCurrLineSize = inner.DC.CurrLineSize;
		table.HostBackupPrevLineSize = inner.DC.PrevLineSize;
		table.HostSkipItems = inner.SkipItems;
		inner.ParentWorkRect = table.WorkRect;

		table.RowLogicalY = table.StartY;
		table.RowPosY2 = table.StartY;
		table.FrozenBottomY = table.InnerClipRect.Min.y;
		table.ContextPopupId = ImHashStr( "##ContextMenu", id );

		PushOverrideID( id );

		// Channels: 0 = body bg, 1 = body content, 2 = frozen bg, 3 = frozen content.
		table.UsesChannels = !g.SplitDrawLists.Contains( inner.DrawList );
		if ( table.UsesChannels )
		{
			g.SplitDrawLists.Add( inner.DrawList );
			inner.DrawList.ChannelsSplit( 4 );
			inner.DrawList.ChannelsSetCurrent( 1 );
		}

		return true;
	}

	public static void EndTable()
	{
		var g = G;
		var table = CurrentTable;
		if ( table is null )
		{
			Log.Warning( "ImGui: EndTable() called without a matching BeginTable()" );
			return;
		}

		if ( table.IsInsideRow )
			TableEndRow( table );
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );

		var inner = table.InnerWindow;
		var outer = table.OuterWindow;
		bool useChild = inner != outer;

		float rowsEndY = table.RowPosY2;
		table.LastHeight = rowsEndY - table.StartY;

		// Context menu in body
		if ( (table.Flags & ImGuiTableFlags.ContextMenuInBody) != 0 && g.HoveredWindow == inner && IsMouseReleased( ImGuiMouseButton.Right ) && !IsAnyItemHovered() )
		{
			var bodyRect = new ImRect( table.ColumnsMinX, table.StartY, table.ColumnsMaxX, rowsEndY );
			if ( bodyRect.Contains( g.IO.MousePos ) )
			{
				table.ContextMenuColumn = table.HoveredColumnBody;
				OpenPopupEx( table.ContextPopupId );
			}
		}

		if ( table.UsesChannels )
		{
			inner.DrawList.ChannelsMerge();
			g.SplitDrawLists.Remove( inner.DrawList );
		}

		// Borders (drawn on top of everything)
		TableDrawBorders( table, rowsEndY );

		// Context menu
		if ( IsPopupOpen( table.ContextPopupId, ImGuiPopupFlags.None ) )
		{
			if ( BeginPopupEx( table.ContextPopupId, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings ) )
			{
				TableDrawDefaultContextMenu( table );
				EndPopup();
			}
		}

		PopID();

		// Restore host state
		inner.WorkRect = table.HostBackupWorkRect;
		inner.ParentWorkRect = table.HostBackupParentWorkRect;
		inner.DC.ColumnsOffset = table.HostBackupColumnsOffset;
		inner.DC.ItemWidth = table.HostBackupItemWidth;
		while ( inner.DC.ItemWidthStack.Count > table.HostBackupItemWidthStackCount )
			inner.DC.ItemWidthStack.RemoveAt( inner.DC.ItemWidthStack.Count - 1 );
		inner.DC.CurrLineSize = table.HostBackupCurrLineSize;
		inner.DC.PrevLineSize = table.HostBackupPrevLineSize;
		inner.SkipItems = table.HostSkipItems;
		inner.DC.CurrentTableIdx = table.InnerBackupCurrentTableIdx;
		inner.DC.IsSameLine = false;

		if ( useChild )
		{
			// Content size of the scrolling child: full columns width, logical rows height.
			inner.DC.CursorPos = new Vector2( inner.Pos.x + inner.DC.Indent, rowsEndY );
			inner.DC.CursorMaxPos = new Vector2(
				MathF.Max( table.HostBackupCursorMaxPos.x, table.WorkRect.Min.x + table.ColumnsTotalWidth ),
				MathF.Max( table.HostBackupCursorMaxPos.y, rowsEndY ) );
			inner.DC.CursorPosPrevLine = inner.DC.CursorPos;
			EndChild();
			outer.DC.CurrentTableIdx = table.HostBackupCurrentTableIdx;

			if ( (table.Flags & ImGuiTableFlags.BordersOuter) != 0 )
			{
				var r = table.OuterRect;
				var col = GetColorU32Internal( ImGuiCol.TableBorderStrong );
				if ( (table.Flags & ImGuiTableFlags.BordersOuterV) != 0 )
				{
					outer.DrawList.AddLine( r.TL, r.BL, col );
					outer.DrawList.AddLine( new Vector2( r.Max.x - 1, r.Min.y ), new Vector2( r.Max.x - 1, r.Max.y ), col );
				}
				if ( (table.Flags & ImGuiTableFlags.BordersOuterH) != 0 )
				{
					outer.DrawList.AddLine( r.TL, r.TR, col );
					outer.DrawList.AddLine( new Vector2( r.Min.x, r.Max.y - 1 ), new Vector2( r.Max.x, r.Max.y - 1 ), col );
				}
			}
		}
		else
		{
			table.OuterRect.Max.y = MathF.Max( table.OuterRect.Min.y, rowsEndY );
			outer.DC.CursorPos = table.OuterRect.Min;
			outer.DC.CursorPosPrevLine = table.OuterRect.Min;
			outer.DC.CursorMaxPos = table.HostBackupCursorMaxPos;
			ItemSize( table.OuterRect.Size );
			ItemAdd( table.OuterRect, 0 );
			outer.DC.CurrentTableIdx = table.HostBackupCurrentTableIdx;
		}

		if ( g.TablesStack.Count > 0 && g.TablesStack[^1] == table )
			g.TablesStack.RemoveAt( g.TablesStack.Count - 1 );
		table.IsFirstFrame = false;
	}
	#endregion

	#region Setup
	public static void TableSetupColumn( string label, ImGuiTableColumnFlags flags = ImGuiTableColumnFlags.None, float initWidthOrWeight = 0.0f, int userId = 0 )
	{
		var table = CurrentTable;
		if ( table is null )
		{
			Log.Warning( "ImGui: TableSetupColumn() called outside of a table" );
			return;
		}
		if ( table.IsLayoutLocked || table.DeclColumnsCount >= table.ColumnsCount )
		{
			Log.Warning( "ImGui: TableSetupColumn() called too late or too many times" );
			return;
		}

		var column = table.Columns[table.DeclColumnsCount++];
		bool flagsChanged = column.DeclFlags != flags;
		column.Name = label;
		column.UserID = userId;
		column.DeclFlags = flags;
		if ( table.IsFirstFrame || flagsChanged || column.InitWidthOrWeight != initWidthOrWeight )
		{
			column.InitWidthOrWeight = initWidthOrWeight;
			if ( table.IsFirstFrame )
			{
				column.IsUserEnabled = column.IsUserEnabledNextFrame = (flags & ImGuiTableColumnFlags.DefaultHide) == 0;
				if ( (flags & ImGuiTableColumnFlags.DefaultSort) != 0 && (table.Flags & ImGuiTableFlags.Sortable) != 0 )
				{
					column.SortOrder = 0;
					column.SortDirection = (flags & ImGuiTableColumnFlags.PreferSortDescending) != 0 ? ImGuiSortDirection.Descending : ImGuiSortDirection.Ascending;
					table.SortSpecsDirtyInternal = true;
				}
			}
		}
	}

	/// <summary>Lock columns/rows so they stay visible when scrolled.</summary>
	public static void TableSetupScrollFreeze( int cols, int rows )
	{
		var table = CurrentTable;
		if ( table is null )
			return;
		table.FreezeColumnsRequest = (table.Flags & ImGuiTableFlags.ScrollX) != 0 ? Math.Clamp( cols, 0, table.ColumnsCount ) : 0;
		table.FreezeRowsRequest = (table.Flags & ImGuiTableFlags.ScrollY) != 0 ? Math.Max( rows, 0 ) : 0;
	}
	#endregion

	#region Layout
	private static float TableGetMinColumnWidth() => MathF.Max( 1.0f, G.Style.FramePadding.x );

	private static void TableUpdateLayout( ImGuiTable table )
	{
		var g = G;
		var style = g.Style;
		table.IsLayoutLocked = true;
		var inner = table.InnerWindow;
		bool useChild = inner != table.OuterWindow;

		table.FreezeRows = useChild ? table.FreezeRowsRequest : 0;
		table.FreezeColumns = useChild ? table.FreezeColumnsRequest : 0;

		// Effective flags & enabled state
		var sizing = table.Flags & ImGuiTableFlags.SizingMask_;
		bool scrollXNoInner = (table.Flags & ImGuiTableFlags.ScrollX) != 0 && table.InnerWidth <= 0f;
		for ( int n = 0; n < table.ColumnsCount; n++ )
		{
			var c = table.Columns[n];
			if ( n >= table.DeclColumnsCount )
			{
				c.DeclFlags = ImGuiTableColumnFlags.None;
				c.Name ??= null;
			}
			var f = c.DeclFlags;
			if ( (f & ImGuiTableColumnFlags.WidthMask_) == 0 )
			{
				bool fixedPolicy = sizing == ImGuiTableFlags.SizingFixedFit || sizing == ImGuiTableFlags.SizingFixedSame || scrollXNoInner;
				f |= fixedPolicy ? ImGuiTableColumnFlags.WidthFixed : ImGuiTableColumnFlags.WidthStretch;
			}
			else if ( scrollXNoInner && (f & ImGuiTableColumnFlags.WidthStretch) != 0 )
			{
				f = (f & ~ImGuiTableColumnFlags.WidthMask_) | ImGuiTableColumnFlags.WidthFixed;
			}
			if ( (table.Flags & ImGuiTableFlags.Resizable) == 0 )
				f |= ImGuiTableColumnFlags.NoResize;
			c.Flags = f;

			if ( (table.Flags & ImGuiTableFlags.Hideable) == 0 && !c.IsUserEnabledNextFrame && c.IsUserEnabled )
				c.IsUserEnabledNextFrame = true;
			c.IsUserEnabled = c.IsUserEnabledNextFrame;
			c.IsEnabled = c.IsUserEnabled && (f & ImGuiTableColumnFlags.Disabled) == 0;
		}

		// Display order
		table.DisplayOrderToIndex.Clear();
		for ( int n = 0; n < table.ColumnsCount; n++ )
			table.DisplayOrderToIndex.Add( n );
		table.DisplayOrderToIndex.Sort( ( a, b ) =>
		{
			int r = table.Columns[a].DisplayOrder.CompareTo( table.Columns[b].DisplayOrder );
			return r != 0 ? r : a.CompareTo( b );
		} );
		for ( int d = 0; d < table.ColumnsCount; d++ )
		{
			table.Columns[table.DisplayOrderToIndex[d]].DisplayOrder = d;
			table.Columns[table.DisplayOrderToIndex[d]].DisplayIndex = d;
		}

		var enabled = new List<ImGuiTableColumn>();
		foreach ( int idx in table.DisplayOrderToIndex )
			if ( table.Columns[idx].IsEnabled )
				enabled.Add( table.Columns[idx] );

		// Padding
		float cp = style.CellPadding.x;
		bool padOuter = (table.Flags & ImGuiTableFlags.NoPadOuterX) != 0 ? false : (table.Flags & ImGuiTableFlags.PadOuterX) != 0 || (table.Flags & ImGuiTableFlags.BordersOuterV) != 0;
		bool padInner = (table.Flags & ImGuiTableFlags.NoPadInnerX) == 0;
		float minW = TableGetMinColumnWidth();

		var padL = new float[enabled.Count];
		var padR = new float[enabled.Count];
		float sumPads = 0f;
		for ( int k = 0; k < enabled.Count; k++ )
		{
			padL[k] = k == 0 ? (padOuter ? cp : 0f) : (padInner ? cp : 0f);
			padR[k] = k == enabled.Count - 1 ? (padOuter ? cp : 0f) : (padInner ? cp : 0f);
			sumPads += padL[k] + padR[k];
		}

		// Fixed widths
		float maxAutoFixed = 0f;
		foreach ( var c in enabled )
			if ( !c.IsStretch )
				maxAutoFixed = MathF.Max( maxAutoFixed, MathF.Max( c.ContentWidthPrev, minW ) );

		float sumFixed = 0f;
		float sumWeights = 0f;
		int stretchCount = 0;
		foreach ( var c in enabled )
		{
			float autoW = MathF.Max( c.ContentWidthPrev, minW );
			if ( !c.IsStretch )
			{
				float w;
				if ( c.WidthRequest > 0f )
					w = c.WidthRequest;
				else if ( c.InitWidthOrWeight > 0f )
					w = c.InitWidthOrWeight;
				else
					w = sizing == ImGuiTableFlags.SizingFixedSame ? maxAutoFixed : autoW;
				c.WidthGiven = MathF.Max( minW, ImTrunc( w ) );
				sumFixed += c.WidthGiven;
			}
			else
			{
				if ( c.StretchWeight <= 0f )
					c.StretchWeight = c.InitWidthOrWeight > 0f ? c.InitWidthOrWeight : (sizing == ImGuiTableFlags.SizingStretchProp ? autoW : 1.0f);
				sumWeights += c.StretchWeight;
				stretchCount++;
			}
		}

		float tableWidth = table.WorkRect.Width;
		if ( (table.Flags & ImGuiTableFlags.ScrollX) != 0 && table.InnerWidth > 0f )
			tableWidth = table.InnerWidth;

		if ( stretchCount > 0 )
		{
			float avail = MathF.Max( 0f, tableWidth - sumPads - sumFixed );
			float used = 0f;
			ImGuiTableColumn last = null;
			foreach ( var c in enabled )
			{
				if ( !c.IsStretch )
					continue;
				c.WidthGiven = MathF.Max( minW, ImTrunc( avail * c.StretchWeight / MathF.Max( sumWeights, 1e-6f ) ) );
				used += c.WidthGiven;
				last = c;
			}
			if ( last is not null )
				last.WidthGiven = MathF.Max( minW, last.WidthGiven + (avail - used) );
		}

		// Positions
		var inner0 = table.InnerWindow;
		float scrollX = useChild ? inner0.Scroll.x : 0f;
		float x = table.WorkRect.Min.x;
		table.FrozenRightX = float.MinValue;
		for ( int k = 0; k < enabled.Count; k++ )
		{
			var c = enabled[k];
			c.IsFrozen = k < table.FreezeColumns;
			float off = c.IsFrozen ? scrollX : 0f;
			c.MinX = x + off;
			c.WorkMinX = c.MinX + padL[k];
			c.WorkMaxX = c.WorkMinX + c.WidthGiven;
			c.MaxX = c.WorkMaxX + padR[k];
			c.ItemWidth = ImTrunc( MathF.Max( 1.0f, c.WidthGiven * 0.65f ) );
			x += padL[k] + c.WidthGiven + padR[k];
			if ( c.IsFrozen )
				table.FrozenRightX = MathF.Max( table.FrozenRightX, c.MaxX );
		}
		table.ColumnsTotalWidth = x - table.WorkRect.Min.x;

		// Fixed-only table that doesn't extend to the host width
		if ( stretchCount == 0 && !useChild && (table.Flags & ImGuiTableFlags.NoHostExtendX) != 0 )
		{
			table.OuterRect.Max.x = MathF.Min( table.OuterRect.Max.x, table.OuterRect.Min.x + table.ColumnsTotalWidth );
			table.WorkRect.Max.x = table.OuterRect.Max.x;
			table.InnerRect.Max.x = table.OuterRect.Max.x;
			table.InnerClipRect.Max.x = MathF.Min( table.InnerClipRect.Max.x, table.OuterRect.Max.x );
		}

		table.ColumnsMinX = enabled.Count > 0 ? enabled.Min( c => c.MinX ) : table.WorkRect.Min.x;
		table.ColumnsMaxX = enabled.Count > 0 ? enabled.Max( c => c.MaxX ) : table.WorkRect.Min.x;

		// Clipping per column
		var clip = table.InnerClipRect;
		foreach ( var c in table.Columns )
		{
			if ( !c.IsEnabled )
			{
				c.ClipMinX = c.ClipMaxX = clip.Min.x;
				c.IsVisibleX = false;
				continue;
			}
			float cmin = (c.Flags & ImGuiTableColumnFlags.NoClip) != 0 ? clip.Min.x : MathF.Max( c.MinX, clip.Min.x );
			float cmax = (c.Flags & ImGuiTableColumnFlags.NoClip) != 0 ? clip.Max.x : MathF.Min( c.MaxX, clip.Max.x );
			if ( !c.IsFrozen && table.FreezeColumns > 0 )
				cmin = MathF.Max( cmin, table.FrozenRightX );
			c.ClipMinX = cmin;
			c.ClipMaxX = MathF.Max( cmin, cmax );
			c.IsVisibleX = c.ClipMaxX > c.ClipMinX;
		}

		// Hovered column
		table.HoveredColumnBody = -1;
		float hoverMaxY = table.StartY + MathF.Max( table.LastHeight, g.FontSize );
		if ( g.HoveredWindow == inner && g.IO.MousePos.y >= table.StartY && g.IO.MousePos.y < hoverMaxY )
		{
			for ( int n = 0; n < table.ColumnsCount; n++ )
			{
				var c = table.Columns[n];
				if ( c.IsEnabled && g.IO.MousePos.x >= MathF.Max( c.MinX, c.ClipMinX ) && g.IO.MousePos.x < c.MaxX )
				{
					table.HoveredColumnBody = n;
					break;
				}
			}
		}

		TableUpdateBorders( table, enabled );
		TableUpdateSort( table );
	}

	private static void TableUpdateBorders( ImGuiTable table, List<ImGuiTableColumn> enabled )
	{
		var g = G;
		table.HoveredBorderColumn = -1;
		table.HeldBorderColumn = -1;
		if ( (table.Flags & ImGuiTableFlags.Resizable) == 0 || enabled.Count == 0 )
			return;

		var window = table.InnerWindow;
		bool useChild = window != table.OuterWindow;
		float y1 = useChild ? window.InnerRect.Min.y : table.StartY;
		float y2 = useChild ? window.InnerRect.Max.y : table.StartY + MathF.Max( table.LastHeight, g.FontSize + table.CellPaddingY * 2 );
		float minW = TableGetMinColumnWidth();
		float hw = MathF.Max( 2.0f, TABLE_RESIZE_HIT_HALF * g.AppliedStyleScale );

		for ( int k = 0; k < enabled.Count; k++ )
		{
			var c = enabled[k];
			bool isLast = k == enabled.Count - 1;
			if ( (c.Flags & ImGuiTableColumnFlags.NoResize) != 0 )
				continue;
			if ( isLast && (c.IsStretch || (!useChild && (table.Flags & ImGuiTableFlags.NoHostExtendX) == 0)) )
				continue;

			int colIdx = table.Columns.IndexOf( c );
			int id = ImHashInt( colIdx, table.ID ^ 0x5A5A1234 );
			var hit = new ImRect( c.MaxX - hw, y1, c.MaxX + hw, y2 );
			if ( !ItemAdd( hit, id, null, ImGuiItemFlags.NoNav ) )
				continue;
			bool pressed = ButtonBehavior( hit, id, out bool hovered, out bool held, ImGuiButtonFlags.PressedOnClick | ImGuiButtonFlags.PressedOnDoubleClick );
			if ( hovered || held )
				g.MouseCursor = ImGuiMouseCursor.ResizeEW;
			if ( hovered && g.HoveredIdTimer < 0.06f && !held )
				hovered = false;
			if ( hovered ) table.HoveredBorderColumn = colIdx;
			if ( held ) table.HeldBorderColumn = colIdx;

			if ( pressed && g.IO.MouseClickedCount[0] == 2 )
			{
				// Double-click: auto fit
				if ( c.IsStretch )
					c.StretchWeight = -1f;
				else
					c.WidthRequest = -1f;
				ClearActiveID();
				continue;
			}

			if ( held && !g.ActiveIdIsJustActivated )
			{
				float borderX = g.IO.MousePos.x - g.ActiveIdClickOffset.x + hw;
				float padR = c.MaxX - c.WorkMaxX;
				float newW = MathF.Max( minW, ImTrunc( borderX - padR - c.WorkMinX ) );
				if ( !c.IsStretch )
				{
					c.WidthRequest = newW;
				}
				else
				{
					ImGuiTableColumn next = null;
					for ( int j = k + 1; j < enabled.Count; j++ )
						if ( enabled[j].IsStretch ) { next = enabled[j]; break; }
					float sumStretchW = 0f, sumStretchWeight = 0f;
					foreach ( var s in enabled )
						if ( s.IsStretch ) { sumStretchW += s.WidthGiven; sumStretchWeight += MathF.Max( s.StretchWeight, 1e-6f ); }
					float kRatio = sumStretchWeight / MathF.Max( sumStretchW, 1f );
					if ( next is not null )
					{
						float pair = c.WidthGiven + next.WidthGiven;
						newW = Math.Clamp( newW, minW, MathF.Max( minW, pair - minW ) );
						c.StretchWeight = newW * kRatio;
						next.StretchWeight = (pair - newW) * kRatio;
					}
					else
					{
						c.StretchWeight = newW * kRatio;
					}
				}
			}
		}
	}

	private static ImGuiSortDirection TableGetColumnNextSortDirection( ImGuiTable table, ImGuiTableColumn column )
	{
		var dirs = new List<ImGuiSortDirection>();
		bool preferDesc = (column.DeclFlags & ImGuiTableColumnFlags.PreferSortDescending) != 0;
		if ( preferDesc )
		{
			if ( (column.DeclFlags & ImGuiTableColumnFlags.NoSortDescending) == 0 ) dirs.Add( ImGuiSortDirection.Descending );
			if ( (column.DeclFlags & ImGuiTableColumnFlags.NoSortAscending) == 0 ) dirs.Add( ImGuiSortDirection.Ascending );
		}
		else
		{
			if ( (column.DeclFlags & ImGuiTableColumnFlags.NoSortAscending) == 0 ) dirs.Add( ImGuiSortDirection.Ascending );
			if ( (column.DeclFlags & ImGuiTableColumnFlags.NoSortDescending) == 0 ) dirs.Add( ImGuiSortDirection.Descending );
		}
		if ( (table.Flags & ImGuiTableFlags.SortTristate) != 0 )
			dirs.Add( ImGuiSortDirection.None );
		if ( dirs.Count == 0 )
			return ImGuiSortDirection.None;
		if ( column.SortOrder < 0 || column.SortDirection == ImGuiSortDirection.None )
			return dirs[0];
		int idx = dirs.IndexOf( column.SortDirection );
		return dirs[(idx + 1) % dirs.Count];
	}

	private static void TableSetColumnSortDirection( ImGuiTable table, int columnN, ImGuiSortDirection dir, bool appendToSortSpecs )
	{
		if ( (table.Flags & ImGuiTableFlags.SortMulti) == 0 )
			appendToSortSpecs = false;
		if ( (table.Flags & ImGuiTableFlags.SortTristate) == 0 && dir == ImGuiSortDirection.None )
			dir = ImGuiSortDirection.Ascending;

		var column = table.Columns[columnN];
		int maxOrder = -1;
		if ( appendToSortSpecs )
			foreach ( var c in table.Columns )
				maxOrder = Math.Max( maxOrder, c.SortOrder );

		for ( int n = 0; n < table.ColumnsCount; n++ )
		{
			var other = table.Columns[n];
			if ( other != column && !appendToSortSpecs )
				other.SortOrder = -1;
		}

		if ( dir == ImGuiSortDirection.None )
		{
			column.SortOrder = -1;
			column.SortDirection = ImGuiSortDirection.None;
		}
		else
		{
			if ( column.SortOrder == -1 || !appendToSortSpecs )
				column.SortOrder = appendToSortSpecs ? maxOrder + 1 : 0;
			column.SortDirection = dir;
		}

		// Compact sort orders
		var sorted = table.Columns.Where( c => c.SortOrder >= 0 ).OrderBy( c => c.SortOrder ).ToList();
		for ( int i = 0; i < sorted.Count; i++ )
			sorted[i].SortOrder = i;
		table.SortSpecsDirtyInternal = true;
	}

	private static void TableUpdateSort( ImGuiTable table )
	{
		if ( (table.Flags & ImGuiTableFlags.Sortable) == 0 )
			return;

		int sortedCount = 0;
		foreach ( var c in table.Columns )
			if ( c.SortOrder >= 0 )
			{
				if ( (c.DeclFlags & ImGuiTableColumnFlags.NoSort) != 0 )
				{
					c.SortOrder = -1;
					table.SortSpecsDirtyInternal = true;
				}
				else
					sortedCount++;
			}

		if ( sortedCount > 1 && (table.Flags & ImGuiTableFlags.SortMulti) == 0 )
		{
			var keep = table.Columns.Where( c => c.SortOrder >= 0 ).OrderBy( c => c.SortOrder ).First();
			foreach ( var c in table.Columns )
				if ( c != keep ) c.SortOrder = -1;
			keep.SortOrder = 0;
			sortedCount = 1;
			table.SortSpecsDirtyInternal = true;
		}

		if ( sortedCount == 0 && (table.Flags & ImGuiTableFlags.SortTristate) == 0 )
		{
			for ( int n = 0; n < table.ColumnsCount; n++ )
			{
				var c = table.Columns[n];
				if ( c.IsEnabled && (c.DeclFlags & ImGuiTableColumnFlags.NoSort) == 0 )
				{
					c.SortOrder = 0;
					c.SortDirection = TableGetColumnNextSortDirection( table, c );
					if ( c.SortDirection == ImGuiSortDirection.None )
						c.SortDirection = ImGuiSortDirection.Ascending;
					table.SortSpecsDirtyInternal = true;
					break;
				}
			}
		}

		if ( table.SortSpecsDirtyInternal )
		{
			var sorted = new List<(int idx, ImGuiTableColumn c)>();
			for ( int n = 0; n < table.ColumnsCount; n++ )
				if ( table.Columns[n].SortOrder >= 0 )
					sorted.Add( (n, table.Columns[n]) );
			sorted.Sort( ( a, b ) => a.c.SortOrder.CompareTo( b.c.SortOrder ) );
			var specs = new ImGuiTableColumnSortSpecs[sorted.Count];
			for ( int i = 0; i < sorted.Count; i++ )
				specs[i] = new ImGuiTableColumnSortSpecs { ColumnIndex = sorted[i].idx, ColumnUserID = sorted[i].c.UserID, SortOrder = i, SortDirection = sorted[i].c.SortDirection };
			table.SortSpecs.Specs = specs;
			table.SortSpecs.SpecsCount = specs.Length;
			table.SortSpecs.SpecsDirty = true;
			table.SortSpecsDirtyInternal = false;
		}
	}
	#endregion

	#region Rows & cells
	public static void TableNextRow( ImGuiTableRowFlags rowFlags = ImGuiTableRowFlags.None, float minRowHeight = 0.0f )
	{
		var table = CurrentTable;
		if ( table is null )
		{
			Log.Warning( "ImGui: TableNextRow() called outside of a table" );
			return;
		}
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );
		if ( table.IsInsideRow )
			TableEndRow( table );

		table.LastRowFlags = table.RowFlags;
		table.RowFlags = rowFlags;
		table.CurrentRow++;
		table.RowMinHeight = minRowHeight;
		TableBeginRow( table );
	}

	private static void TableBeginRow( ImGuiTable table )
	{
		var g = G;
		var window = table.InnerWindow;
		table.IsInsideRow = true;
		table.CurrentColumn = -1;
		table.RowIsFrozen = table.CurrentRow < table.FreezeRows;
		table.RowFrozenOffset = table.RowIsFrozen ? window.Scroll.y : 0f;
		table.RowPosY1 = table.RowLogicalY + table.RowFrozenOffset;
		table.RowPosY2 = table.RowPosY1 + MathF.Max( table.RowMinHeight, table.CellPaddingY * 2.0f );
		table.RowMaxY = table.RowPosY2;
		table.RowTextBaseline = 0.0f;
		table.RowBgColor0 = default;
		table.RowBgColor1 = default;
		table.CellBgColors.Clear();

		if ( table.CurrentRow > 0 && (table.Flags & ImGuiTableFlags.BordersInnerH) != 0 && (table.LastRowFlags & ImGuiTableRowFlags.Headers) == 0 )
			if ( (table.Flags & ImGuiTableFlags.NoBordersInBody) == 0 && table.CurrentRow != table.FreezeRows )
				table.RowLines.Add( (table.RowPosY1, false) );

		// Opaque background for frozen rows so scrolled content does not show through.
		if ( table.RowIsFrozen && table.UsesChannels )
		{
			table.FrozenRowHeights.TryGetValue( table.CurrentRow, out float predicted );
			predicted = MathF.Max( predicted, table.RowPosY2 - table.RowPosY1 );
			window.DrawList.ChannelsSetCurrent( 2 );
			window.DrawList.PushClipRect( table.InnerClipRect.Min, table.InnerClipRect.Max, false );
			window.DrawList.AddRectFilled( new Vector2( table.ColumnsMinX, table.RowPosY1 ), new Vector2( MathF.Max( table.ColumnsMaxX, table.InnerClipRect.Max.x ), table.RowPosY1 + predicted ), GetColorU32Internal( ImGuiCol.WindowBg ) );
			window.DrawList.PopClipRect();
			window.DrawList.ChannelsSetCurrent( 1 );
		}
	}

	private static void TableEndRow( ImGuiTable table )
	{
		var g = G;
		var window = table.InnerWindow;
		if ( table.CurrentColumn != -1 )
			TableEndCell( table );

		table.RowPosY2 = MathF.Max( table.RowPosY2, table.RowMaxY );
		float height = table.RowPosY2 - table.RowPosY1;
		bool isHeader = (table.RowFlags & ImGuiTableRowFlags.Headers) != 0;

		if ( table.RowIsFrozen )
		{
			table.FrozenRowHeights[table.CurrentRow] = height;
			table.FrozenBottomY = MathF.Max( table.FrozenBottomY, table.RowPosY2 );
		}

		// Row background colors
		Color32 bgCol0 = table.RowBgColor0;
		if ( bgCol0.a == 0 && (table.Flags & ImGuiTableFlags.RowBg) != 0 && !isHeader )
			bgCol0 = GetColorU32Internal( (table.RowBgColorCounter & 1) != 0 ? ImGuiCol.TableRowBgAlt : ImGuiCol.TableRowBg );
		Color32 bgCol1 = table.RowBgColor1;

		bool anyBg = bgCol0.a != 0 || bgCol1.a != 0 || table.CellBgColors.Count > 0;
		bool needFrozenColumnBg = !table.RowIsFrozen && table.FreezeColumns > 0 && table.UsesChannels;
		if ( anyBg || needFrozenColumnBg )
		{
			var dl = window.DrawList;
			float clipTop = table.RowIsFrozen ? table.InnerClipRect.Min.y : MathF.Max( table.InnerClipRect.Min.y, table.FreezeRows > 0 ? table.FrozenBottomY : table.InnerClipRect.Min.y );
			var clipMin = new Vector2( table.InnerClipRect.Min.x, clipTop );
			var clipMax = table.InnerClipRect.Max;

			void DrawBgs( float minX, float maxX )
			{
				var a = new Vector2( minX, table.RowPosY1 );
				var b = new Vector2( maxX, table.RowPosY2 );
				if ( bgCol0.a != 0 ) dl.AddRectFilled( a, b, bgCol0 );
				if ( bgCol1.a != 0 ) dl.AddRectFilled( a, b, bgCol1 );
				foreach ( var (colN, col) in table.CellBgColors )
				{
					var c = table.Columns[colN];
					if ( !c.IsEnabled ) continue;
					float x1 = MathF.Max( minX, c.MinX ), x2 = MathF.Min( maxX, c.MaxX );
					if ( x2 > x1 )
						dl.AddRectFilled( new Vector2( x1, table.RowPosY1 ), new Vector2( x2, table.RowPosY2 ), col );
				}
			}

			if ( table.UsesChannels )
				dl.ChannelsSetCurrent( table.RowIsFrozen ? 2 : 0 );
			dl.PushClipRect( clipMin, clipMax, false );
			if ( anyBg )
				DrawBgs( table.ColumnsMinX, table.ColumnsMaxX );
			dl.PopClipRect();

			if ( needFrozenColumnBg )
			{
				// Frozen columns are drawn above the scrolling body: give them an opaque background.
				dl.ChannelsSetCurrent( 2 );
				dl.PushClipRect( clipMin, clipMax, false );
				float fx1 = table.ColumnsMinX, fx2 = table.FrozenRightX;
				foreach ( var c in table.Columns )
					if ( c.IsFrozen && c.IsEnabled ) fx1 = MathF.Min( fx1, c.MinX );
				if ( fx2 > fx1 )
				{
					dl.AddRectFilled( new Vector2( fx1, table.RowPosY1 ), new Vector2( fx2, table.RowPosY2 ), GetColorU32Internal( ImGuiCol.WindowBg ) );
					if ( anyBg )
						DrawBgs( fx1, fx2 );
				}
				dl.PopClipRect();
			}
			if ( table.UsesChannels )
				dl.ChannelsSetCurrent( 1 );
		}

		if ( isHeader )
		{
			table.HasHeaders = true;
			table.HeaderBottomY = MathF.Max( table.HeaderBottomY, table.RowPosY2 );
			if ( (table.Flags & (ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.BordersOuterH)) != 0 )
				table.RowLines.Add( (table.RowPosY2, true) );
		}
		else
		{
			table.RowBgColorCounter++;
		}

		// Continue layout from the logical (unscrolled) position, so frozen rows don't push content down.
		table.RowLogicalY = table.RowPosY1 - table.RowFrozenOffset + height;
		table.RowPosY2 = table.RowLogicalY;
		table.IsInsideRow = false;
		table.CurrentColumn = -1;
	}

	/// <summary>Append into the next column (or first column of next row if currently in last column). Return true when column is visible.</summary>
	public static bool TableNextColumn()
	{
		var table = CurrentTable;
		if ( table is null )
			return false;

		if ( table.IsInsideRow && table.CurrentColumn + 1 < table.ColumnsCount )
		{
			if ( table.CurrentColumn != -1 )
				TableEndCell( table );
			TableBeginCell( table, table.CurrentColumn + 1 );
		}
		else
		{
			TableNextRow();
			TableBeginCell( table, 0 );
		}
		var c = table.Columns[table.CurrentColumn];
		return c.IsEnabled && c.IsVisibleX;
	}

	/// <summary>Append into the specified column. Return true when column is visible.</summary>
	public static bool TableSetColumnIndex( int columnN )
	{
		var table = CurrentTable;
		if ( table is null || columnN < 0 || columnN >= table.ColumnsCount )
			return false;

		if ( !table.IsInsideRow )
			TableNextRow();
		if ( table.CurrentColumn != columnN )
		{
			if ( table.CurrentColumn != -1 )
				TableEndCell( table );
			TableBeginCell( table, columnN );
		}
		var c = table.Columns[columnN];
		return c.IsEnabled && c.IsVisibleX;
	}

	private static void TableBeginCell( ImGuiTable table, int columnN )
	{
		var g = G;
		var window = table.InnerWindow;
		var column = table.Columns[columnN];
		table.CurrentColumn = columnN;

		float startX = column.WorkMinX;
		float startY = table.RowPosY1 + table.CellPaddingY;
		window.DC.CursorPos = new Vector2( startX, startY );
		window.DC.CursorPosPrevLine = window.DC.CursorPos;
		window.DC.CursorMaxPos = window.DC.CursorPos;
		window.DC.ColumnsOffset = startX - window.Pos.x - window.DC.Indent;
		window.DC.CurrLineTextBaseOffset = table.RowTextBaseline;
		window.DC.CurrLineSize = Vector2.Zero;
		window.DC.PrevLineSize = Vector2.Zero;
		window.DC.IsSameLine = false;
		window.WorkRect = new ImRect( column.WorkMinX, table.WorkRect.Min.y, column.WorkMaxX, table.WorkRect.Max.y );
		window.DC.ItemWidth = column.ItemWidth;
		window.SkipItems = table.HostSkipItems || !column.IsEnabled || !column.IsVisibleX;

		if ( table.UsesChannels )
			window.DrawList.ChannelsSetCurrent( table.RowIsFrozen || column.IsFrozen ? 3 : 1 );

		float minY = table.InnerClipRect.Min.y;
		if ( !table.RowIsFrozen && table.FreezeRows > 0 )
			minY = MathF.Max( minY, table.FrozenBottomY );
		PushClipRect( new Vector2( column.ClipMinX, minY ), new Vector2( column.ClipMaxX, MathF.Max( minY, table.InnerClipRect.Max.y ) ), false );
	}

	private static void TableEndCell( ImGuiTable table )
	{
		var window = table.InnerWindow;
		var column = table.Columns[table.CurrentColumn];
		PopClipRect();

		if ( !window.SkipItems )
		{
			column.ContentWidthThisFrame = MathF.Max( column.ContentWidthThisFrame, window.DC.CursorMaxPos.x - column.WorkMinX );
			table.RowMaxY = MathF.Max( table.RowMaxY, window.DC.CursorMaxPos.y + table.CellPaddingY );
		}
		window.SkipItems = table.HostSkipItems;
	}
	#endregion

	#region Headers
	private static float TableGetHeaderRowHeight()
	{
		var g = G;
		return g.FontSize + g.Style.CellPadding.y * 2.0f;
	}

	/// <summary>Submit a row with header cells based on data provided to TableSetupColumn() + submit context menu.</summary>
	public static void TableHeadersRow()
	{
		var table = CurrentTable;
		if ( table is null )
		{
			Log.Warning( "ImGui: TableHeadersRow() called outside of a table" );
			return;
		}
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );

		float rowHeight = TableGetHeaderRowHeight();
		TableNextRow( ImGuiTableRowFlags.Headers, rowHeight );
		for ( int n = 0; n < table.ColumnsCount; n++ )
		{
			if ( !TableSetColumnIndex( n ) )
				continue;
			string name = (table.Columns[n].DeclFlags & ImGuiTableColumnFlags.NoHeaderLabel) != 0 ? "" : (table.Columns[n].Name ?? "");
			PushID( n );
			TableHeader( name );
			PopID();
		}
	}

	/// <summary>Angled headers are rendered as regular headers (text rotation is not supported).</summary>
	public static void TableAngledHeadersRow()
	{
		var table = CurrentTable;
		if ( table is null )
			return;
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );
		TableNextRow( ImGuiTableRowFlags.Headers, TableGetHeaderRowHeight() );
		for ( int n = 0; n < table.ColumnsCount; n++ )
		{
			if ( !TableSetColumnIndex( n ) )
				continue;
			if ( (table.Columns[n].DeclFlags & ImGuiTableColumnFlags.AngledHeader) == 0 )
				continue;
			PushID( n );
			TableHeader( table.Columns[n].Name ?? "" );
			PopID();
		}
	}

	/// <summary>Submit one header cell manually (rarely used).</summary>
	public static void TableHeader( string label )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;
		var table = CurrentTable;
		if ( table is null || table.CurrentColumn < 0 )
		{
			Log.Warning( "ImGui: TableHeader() needs to be called inside a table cell" );
			return;
		}

		int columnN = table.CurrentColumn;
		var column = table.Columns[columnN];
		label ??= "";
		var labelSize = CalcTextSize( label, true );
		var labelPos = window.DC.CursorPos;

		var cellR = new ImRect( column.MinX, table.RowPosY1, column.MaxX, table.RowPosY2 );
		float labelHeight = MathF.Max( labelSize.y, table.RowMinHeight - table.CellPaddingY * 2.0f );

		const float ARROW_SCALE = 0.65f;
		float wArrow = 0f, wSortText = 0f;
		string sortOrderSuffix = null;
		bool sortable = (table.Flags & ImGuiTableFlags.Sortable) != 0 && (column.DeclFlags & ImGuiTableColumnFlags.NoSort) == 0;
		if ( sortable )
		{
			wArrow = MathF.Max( g.FontSize * ARROW_SCALE + g.Style.FramePadding.x, g.FontSize * ARROW_SCALE );
			if ( column.SortOrder > 0 )
			{
				sortOrderSuffix = (column.SortOrder + 1).ToString();
				wSortText = g.Style.ItemInnerSpacing.x + CalcTextSize( sortOrderSuffix ).x;
			}
		}

		float maxPosX = labelPos.x + labelSize.x + wSortText + wArrow;
		column.ContentWidthThisFrame = MathF.Max( column.ContentWidthThisFrame, maxPosX - column.WorkMinX );

		int id = window.GetID( label );
		var bb = new ImRect( cellR.Min.x, cellR.Min.y, cellR.Max.x, MathF.Max( cellR.Max.y, cellR.Min.y + labelHeight + table.CellPaddingY * 2.0f ) );
		ItemSize( new Vector2( 0.0f, labelHeight ) );
		if ( !ItemAdd( bb, id ) )
			return;

		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, ImGuiButtonFlags.AllowOverlap );
		bool contextOpen = IsPopupOpen( table.ContextPopupId, ImGuiPopupFlags.None ) && table.ContextMenuColumn == columnN;
		bool highlightColumn = (table.Flags & ImGuiTableFlags.HighlightHoveredColumn) != 0 && table.HoveredColumnBody == columnN;

		var bgCol = GetColorU32Internal( ImGuiCol.TableHeaderBg );
		window.DrawList.AddRectFilled( bb.Min, bb.Max, bgCol );
		if ( held || hovered || contextOpen || highlightColumn )
		{
			var col = GetColorU32Internal( held ? ImGuiCol.HeaderActive : (hovered || highlightColumn) ? ImGuiCol.HeaderHovered : ImGuiCol.Header );
			window.DrawList.AddRectFilled( bb.Min, bb.Max, col );
		}

		// Drag to reorder
		if ( held && (table.Flags & ImGuiTableFlags.Reorderable) != 0 && IsMouseDragging( ImGuiMouseButton.Left ) && !g.DragDropActive && (column.DeclFlags & ImGuiTableColumnFlags.NoReorder) == 0 )
		{
			table.ReorderColumn = columnN;
			int dir = g.IO.MousePos.x < cellR.Min.x ? -1 : (g.IO.MousePos.x > cellR.Max.x ? 1 : 0);
			if ( dir != 0 )
			{
				int targetOrder = column.DisplayOrder + dir;
				if ( targetOrder >= 0 && targetOrder < table.ColumnsCount )
				{
					var other = table.Columns[table.DisplayOrderToIndex[targetOrder]];
					if ( (other.DeclFlags & ImGuiTableColumnFlags.NoReorder) == 0 )
					{
						(other.DisplayOrder, column.DisplayOrder) = (column.DisplayOrder, other.DisplayOrder);
						table.DisplayOrderToIndex[column.DisplayOrder] = columnN;
						table.DisplayOrderToIndex[other.DisplayOrder] = table.Columns.IndexOf( other );
					}
				}
			}
		}

		// Sort arrow
		float ellipsisMax = cellR.Max.x - wArrow - wSortText;
		if ( sortable )
		{
			if ( column.SortOrder != -1 && column.SortDirection != ImGuiSortDirection.None )
			{
				float x = MathF.Max( cellR.Min.x, cellR.Max.x - wArrow - wSortText );
				float y = labelPos.y;
				if ( sortOrderSuffix is not null )
				{
					var textCol = g.Style.Colors[(int)ImGuiCol.Text];
					PushStyleColor( ImGuiCol.Text, new Vector4( textCol.x, textCol.y, textCol.z, textCol.w * 0.70f ) );
					RenderText( new Vector2( x + g.Style.ItemInnerSpacing.x, y ), sortOrderSuffix );
					PopStyleColor();
					x += wSortText;
				}
				RenderArrow( window.DrawList, new Vector2( x, y + g.FontSize * (1f - ARROW_SCALE) * 0.5f ), GetColorU32Internal( ImGuiCol.Text ), column.SortDirection == ImGuiSortDirection.Ascending ? ImGuiDir.Up : ImGuiDir.Down, ARROW_SCALE );
			}

			if ( pressed && table.ReorderColumn != columnN )
			{
				var dir = TableGetColumnNextSortDirection( table, column );
				TableSetColumnSortDirection( table, columnN, dir, g.IO.KeyShift );
			}
		}

		RenderTextEllipsis( window.DrawList, labelPos, new Vector2( ellipsisMax, labelPos.y + labelHeight + g.Style.FramePadding.y ), ellipsisMax, ellipsisMax, label, labelSize );

		bool textClipped = labelSize.x > ellipsisMax - labelPos.x;
		if ( textClipped && hovered && g.ActiveId == 0 )
			SetItemTooltip( "{0}", LabelText( label ) );

		if ( IsMouseReleased( ImGuiMouseButton.Right ) && IsItemHovered() )
		{
			table.ContextMenuColumn = columnN;
			OpenPopupEx( table.ContextPopupId );
		}
	}

	private static void TableDrawDefaultContextMenu( ImGuiTable table )
	{
		bool wantSeparator = false;
		int columnN = table.ContextMenuColumn >= 0 && table.ContextMenuColumn < table.ColumnsCount ? table.ContextMenuColumn : -1;
		var column = columnN >= 0 ? table.Columns[columnN] : null;

		if ( (table.Flags & ImGuiTableFlags.Resizable) != 0 )
		{
			bool canResize = column is not null && (column.Flags & ImGuiTableColumnFlags.NoResize) == 0 && column.IsEnabled;
			if ( MenuItem( "Size column to fit", null, false, canResize ) && column is not null )
			{
				if ( column.IsStretch ) column.StretchWeight = -1f;
				else column.WidthRequest = -1f;
			}
			if ( MenuItem( "Size all columns to fit", null ) )
			{
				foreach ( var c in table.Columns )
				{
					c.WidthRequest = -1f;
					c.StretchWeight = -1f;
				}
			}
			wantSeparator = true;
		}

		if ( (table.Flags & ImGuiTableFlags.Reorderable) != 0 )
		{
			if ( MenuItem( "Reset order", null ) )
				for ( int n = 0; n < table.ColumnsCount; n++ )
					table.Columns[n].DisplayOrder = n;
			wantSeparator = true;
		}

		if ( (table.Flags & ImGuiTableFlags.Hideable) != 0 )
		{
			if ( wantSeparator )
				Separator();
			PushItemFlag( ImGuiItemFlags.AutoClosePopups, false );
			int enabledCount = table.Columns.Count( c => c.IsUserEnabled );
			for ( int n = 0; n < table.ColumnsCount; n++ )
			{
				var c = table.Columns[n];
				if ( (c.DeclFlags & ImGuiTableColumnFlags.Disabled) != 0 )
					continue;
				string name = string.IsNullOrEmpty( c.Name ) ? "<Unknown>" : LabelText( c.Name );
				bool active = (c.DeclFlags & ImGuiTableColumnFlags.NoHide) == 0 && !(enabledCount <= 1 && c.IsUserEnabled);
				PushID( n );
				if ( MenuItem( name, null, c.IsUserEnabled, active ) )
					c.IsUserEnabledNextFrame = !c.IsUserEnabled;
				PopID();
			}
			PopItemFlag();
		}
	}
	#endregion

	#region Borders
	private static void TableDrawBorders( ImGuiTable table, float rowsEndY )
	{
		var window = table.InnerWindow;
		bool useChild = window != table.OuterWindow;
		var dl = window.DrawList;
		var flags = table.Flags;
		var strong = GetColorU32Internal( ImGuiCol.TableBorderStrong );
		var light = GetColorU32Internal( ImGuiCol.TableBorderLight );

		float topY = useChild ? window.InnerRect.Min.y : table.StartY;
		float bottomY = useChild ? MathF.Min( rowsEndY, window.InnerRect.Max.y ) : rowsEndY;
		if ( useChild && (flags & ImGuiTableFlags.NoHostExtendY) == 0 )
			bottomY = MathF.Min( MathF.Max( rowsEndY, topY ), window.InnerRect.Max.y );
		if ( bottomY <= topY )
			return;

		dl.PushClipRect( table.InnerClipRect.Min, table.InnerClipRect.Max, false );

		// Horizontal row lines
		foreach ( var (y, isStrong) in table.RowLines )
			if ( y > topY && y < bottomY + 0.5f )
				dl.AddLine( new Vector2( table.ColumnsMinX, y ), new Vector2( table.ColumnsMaxX, y ), isStrong ? strong : light );

		// Inner vertical borders
		if ( (flags & ImGuiTableFlags.BordersInnerV) != 0 || table.HoveredBorderColumn != -1 || table.HeldBorderColumn != -1 )
		{
			var enabled = table.DisplayOrderToIndex.Select( i => table.Columns[i] ).Where( c => c.IsEnabled ).ToList();
			for ( int k = 0; k < enabled.Count; k++ )
			{
				var c = enabled[k];
				int colIdx = table.Columns.IndexOf( c );
				bool isLast = k == enabled.Count - 1;
				bool isHeld = table.HeldBorderColumn == colIdx;
				bool isHovered = table.HoveredBorderColumn == colIdx;
				if ( isLast && !isHeld && !isHovered )
					continue;
				if ( (flags & ImGuiTableFlags.BordersInnerV) == 0 && !isHeld && !isHovered )
					continue;
				float y2 = bottomY;
				bool bodyBorders = (flags & ImGuiTableFlags.NoBordersInBody) == 0;
				if ( !bodyBorders && !isHeld && !isHovered )
				{
					if ( !table.HasHeaders )
						continue;
					y2 = MathF.Min( bottomY, table.HeaderBottomY );
				}
				var col = isHeld ? GetColorU32Internal( ImGuiCol.SeparatorActive ) : isHovered ? GetColorU32Internal( ImGuiCol.SeparatorHovered ) : (bodyBorders ? light : strong);
				if ( table.HasHeaders && bodyBorders && !isHeld && !isHovered )
				{
					dl.AddLine( new Vector2( c.MaxX, topY ), new Vector2( c.MaxX, MathF.Min( y2, table.HeaderBottomY ) ), strong );
					dl.AddLine( new Vector2( c.MaxX, MathF.Min( y2, table.HeaderBottomY ) ), new Vector2( c.MaxX, y2 ), light );
				}
				else
				{
					dl.AddLine( new Vector2( c.MaxX, topY ), new Vector2( c.MaxX, y2 ), col, isHeld || isHovered ? 2f : 1f );
				}
			}
		}

		dl.PopClipRect();

		// Outer borders (non-scrolling tables; scrolling tables get them drawn on the host window)
		if ( !useChild )
		{
			var r = new ImRect( table.OuterRect.Min.x, topY, table.OuterRect.Max.x, bottomY );
			if ( (flags & ImGuiTableFlags.BordersOuterV) != 0 )
			{
				dl.AddLine( r.TL, r.BL, strong );
				dl.AddLine( new Vector2( r.Max.x - 1, r.Min.y ), new Vector2( r.Max.x - 1, r.Max.y ), strong );
			}
			if ( (flags & ImGuiTableFlags.BordersOuterH) != 0 )
			{
				dl.AddLine( r.TL, r.TR, strong );
				dl.AddLine( new Vector2( r.Min.x, r.Max.y - 1 ), new Vector2( r.Max.x, r.Max.y - 1 ), strong );
			}
		}
	}
	#endregion

	#region Queries
	public static ImGuiTableSortSpecs TableGetSortSpecs()
	{
		var table = CurrentTable;
		if ( table is null || (table.Flags & ImGuiTableFlags.Sortable) == 0 )
			return null;
		if ( !table.IsLayoutLocked )
			TableUpdateLayout( table );
		TableUpdateSort( table );
		return table.SortSpecs;
	}

	public static int TableGetColumnCount() => CurrentTable?.ColumnsCount ?? 0;
	public static int TableGetColumnIndex() => CurrentTable?.CurrentColumn ?? 0;
	public static int TableGetRowIndex() => CurrentTable?.CurrentRow ?? 0;
	public static int TableGetHoveredColumn() => CurrentTable?.HoveredColumnBody ?? -1;

	public static string TableGetColumnName( int columnN = -1 )
	{
		var table = CurrentTable;
		if ( table is null )
			return null;
		if ( columnN < 0 )
			columnN = table.CurrentColumn;
		if ( columnN < 0 || columnN >= table.ColumnsCount )
			return null;
		return table.Columns[columnN].Name ?? "";
	}

	public static ImGuiTableColumnFlags TableGetColumnFlags( int columnN = -1 )
	{
		var table = CurrentTable;
		if ( table is null )
			return ImGuiTableColumnFlags.None;
		if ( columnN < 0 )
			columnN = table.CurrentColumn;
		if ( columnN == table.ColumnsCount )
			return table.HoveredColumnBody == -1 ? ImGuiTableColumnFlags.IsHovered : ImGuiTableColumnFlags.None;
		if ( columnN < 0 || columnN >= table.ColumnsCount )
			return ImGuiTableColumnFlags.None;
		var c = table.Columns[columnN];
		var flags = c.DeclFlags | (c.Flags & ImGuiTableColumnFlags.WidthMask_);
		if ( c.IsEnabled ) flags |= ImGuiTableColumnFlags.IsEnabled;
		if ( c.IsEnabled && c.IsVisibleX ) flags |= ImGuiTableColumnFlags.IsVisible;
		if ( c.SortOrder >= 0 ) flags |= ImGuiTableColumnFlags.IsSorted;
		if ( table.HoveredColumnBody == columnN ) flags |= ImGuiTableColumnFlags.IsHovered;
		return flags;
	}

	/// <summary>Change user accessible enabled/disabled state of a column (applied next frame).</summary>
	public static void TableSetColumnEnabled( int columnN, bool v )
	{
		var table = CurrentTable;
		if ( table is null )
			return;
		if ( columnN < 0 )
			columnN = table.CurrentColumn;
		if ( columnN < 0 || columnN >= table.ColumnsCount )
			return;
		table.Columns[columnN].IsUserEnabledNextFrame = v;
	}

	/// <summary>Change the color of a cell, row, or column.</summary>
	public static void TableSetBgColor( ImGuiTableBgTarget target, Color32 color, int columnN = -1 )
	{
		var table = CurrentTable;
		if ( table is null || target == ImGuiTableBgTarget.None )
			return;
		switch ( target )
		{
			case ImGuiTableBgTarget.RowBg0:
				table.RowBgColor0 = color;
				break;
			case ImGuiTableBgTarget.RowBg1:
				table.RowBgColor1 = color;
				break;
			case ImGuiTableBgTarget.CellBg:
				if ( columnN < 0 )
					columnN = table.CurrentColumn;
				if ( columnN >= 0 && columnN < table.ColumnsCount && color.a != 0 )
					table.CellBgColors.Add( (columnN, color) );
				break;
		}
	}

	public static void TableSetBgColor( ImGuiTableBgTarget target, Vector4 color, int columnN = -1 )
		=> TableSetBgColor( target, GetColorU32( color ), columnN );
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.Text)

public static partial class ImGui
{
	/// <summary>Raw text without formatting. Faster than Text(), recommended for long chunks of text.</summary>
	public static void TextUnformatted( string text )
	{
		TextEx( text ?? string.Empty, false );
	}

	internal static void TextEx( string text, bool hideAfterHash )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var textPos = new Vector2( window.DC.CursorPos.x, window.DC.CursorPos.y + window.DC.CurrLineTextBaseOffset );
		float wrapPosX = window.DC.TextWrapPos;
		bool wrapEnabled = wrapPosX >= 0.0f;
		float wrapWidth = wrapEnabled ? CalcWrapWidthForPos( window.DC.CursorPos, wrapPosX ) : 0.0f;
		if ( hideAfterHash )
			text = LabelText( text );

		var textSize = CalcTextSize( text, false, wrapWidth );
		var bb = new ImRect( textPos, textPos + textSize );
		ItemSize( textSize, 0.0f );
		if ( !ItemAdd( bb, 0 ) )
			return;

		window.DrawList.AddText( 0f, bb.Min, GetColorU32Internal( ImGuiCol.Text ), text, wrapWidth );
	}

	/// <summary>Formatted text (uses .NET composite formatting: "Value: {0}").</summary>
	public static void Text( string fmt, params object[] args )
	{
		TextEx( Format( fmt, args ), false );
	}

	public static void TextColored( Vector4 col, string fmt, params object[] args )
	{
		PushStyleColor( ImGuiCol.Text, col );
		Text( fmt, args );
		PopStyleColor();
	}

	public static void TextColored( Color col, string fmt, params object[] args )
		=> TextColored( new Vector4( col.r, col.g, col.b, col.a ), fmt, args );

	public static void TextDisabled( string fmt, params object[] args )
	{
		PushStyleColor( ImGuiCol.Text, G.Style.Colors[(int)ImGuiCol.TextDisabled] );
		Text( fmt, args );
		PopStyleColor();
	}

	/// <summary>Text with word-wrapping at the end of the window (or column).</summary>
	public static void TextWrapped( string fmt, params object[] args )
	{
		var g = G;
		bool needBackup = g.CurrentWindow.DC.TextWrapPos < 0.0f;
		if ( needBackup )
			PushTextWrapPos( 0.0f );
		Text( fmt, args );
		if ( needBackup )
			PopTextWrapPos();
	}

	/// <summary>Display text+label aligned the same way as value+label widgets.</summary>
	public static void LabelText( string label, string fmt, params object[] args )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var style = g.Style;
		float w = CalcItemWidth();
		string valueText = Format( fmt, args );
		var valueSize = CalcTextSize( valueText );
		var labelSize = CalcTextSize( label, true );

		var pos = window.DC.CursorPos;
		var valueBb = new ImRect( pos, pos + new Vector2( w, valueSize.y + style.FramePadding.y * 2 ) );
		var totalBb = new ImRect( pos, pos + new Vector2( w + (labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f), MathF.Max( valueSize.y, labelSize.y ) + style.FramePadding.y * 2 ) );
		ItemSize( totalBb, style.FramePadding.y );
		if ( !ItemAdd( totalBb, 0 ) )
			return;

		RenderTextClipped( valueBb.Min + style.FramePadding, valueBb.Max, valueText, valueSize, new Vector2( 0.0f, 0.0f ) );
		if ( labelSize.x > 0.0f )
			RenderText( new Vector2( valueBb.Max.x + style.ItemInnerSpacing.x, valueBb.Min.y + style.FramePadding.y ), label );
	}

	/// <summary>Shortcut for Bullet() + Text().</summary>
	public static void BulletText( string fmt, params object[] args )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var style = g.Style;
		string text = Format( fmt, args );
		var labelSize = CalcTextSize( text, false );
		var totalSize = new Vector2( g.FontSize + (labelSize.x > 0.0f ? labelSize.x + style.FramePadding.x * 2 : 0.0f), labelSize.y );
		var pos = window.DC.CursorPos;
		pos.y += window.DC.CurrLineTextBaseOffset;
		ItemSize( totalSize, 0.0f );
		var bb = new ImRect( pos, pos + totalSize );
		if ( !ItemAdd( bb, 0 ) )
			return;

		var textCol = GetColorU32Internal( ImGuiCol.Text );
		RenderBullet( window.DrawList, bb.Min + new Vector2( style.FramePadding.x + g.FontSize * 0.5f, g.FontSize * 0.5f ), textCol );
		RenderText( bb.Min + new Vector2( g.FontSize + style.FramePadding.x * 2, 0.0f ), text, false );
	}

	/// <summary>Text separated by a horizontal line, e.g. "--- Section ---".</summary>
	public static void SeparatorText( string label )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return;

		var style = g.Style;
		var labelSize = CalcTextSize( label, true );
		var pos = window.DC.CursorPos;
		var padding = style.SeparatorTextPadding;

		float separatorThickness = style.SeparatorTextBorderSize;
		var minSize = new Vector2( labelSize.x + padding.x * 2.0f, MathF.Max( labelSize.y + padding.y * 2.0f, separatorThickness ) );
		var bb = new ImRect( pos, new Vector2( window.WorkRect.Max.x, pos.y + minSize.y ) );
		float textBaselineY = ImTrunc( (bb.Height - labelSize.y) * style.SeparatorTextAlign.y );
		ItemSize( minSize, textBaselineY );
		if ( !ItemAdd( bb, 0 ) )
			return;

		float sepY = (bb.Min.y + bb.Max.y) * 0.5f;
		float sepPos = ImLerp( bb.Min.x + padding.x, bb.Max.x - padding.x - labelSize.x, style.SeparatorTextAlign.x );
		var labelPos = new Vector2( ImFloor( sepPos ), bb.Min.y + textBaselineY );

		var sepCol = GetColorU32Internal( ImGuiCol.Separator );
		if ( labelSize.x > 0.0f )
		{
			float sep1X2 = labelPos.x - style.ItemSpacing.x;
			float sep2X1 = labelPos.x + labelSize.x + style.ItemSpacing.x;
			if ( sep1X2 > bb.Min.x && separatorThickness > 0.0f )
				window.DrawList.AddLine( new Vector2( bb.Min.x, sepY ), new Vector2( sep1X2, sepY ), sepCol, separatorThickness );
			if ( sep2X1 < bb.Max.x && separatorThickness > 0.0f )
				window.DrawList.AddLine( new Vector2( sep2X1, sepY ), new Vector2( bb.Max.x, sepY ), sepCol, separatorThickness );
			RenderText( labelPos, label );
		}
		else if ( separatorThickness > 0.0f )
		{
			window.DrawList.AddLine( new Vector2( bb.Min.x, sepY ), new Vector2( bb.Max.x, sepY ), sepCol, separatorThickness );
		}
	}

	#region Value helpers
	public static void Value( string prefix, bool b ) => Text( "{0}: {1}", prefix, b ? "true" : "false" );
	public static void Value( string prefix, int v ) => Text( "{0}: {1}", prefix, v );
	public static void Value( string prefix, uint v ) => Text( "{0}: {1}", prefix, v );
	public static void Value( string prefix, float v, string floatFormat = null )
	{
		if ( floatFormat is not null )
			Text( "{0}: {1}", prefix, v.ToString( floatFormat ) );
		else
			Text( "{0}: {1:0.000}", prefix, v );
	}
	#endregion
}

#endregion

#region ImGui (Widgets/ImGui.Tree)

public static partial class ImGui
{
	#region Tree nodes
	public static bool TreeNode( string label ) => TreeNodeEx( label, ImGuiTreeNodeFlags.None );

	/// <summary>Tree node with a separate id and formatted label.</summary>
	public static bool TreeNode( string strId, string fmt, params object[] args )
		=> TreeNodeBehavior( G.CurrentWindow.GetID( strId ), ImGuiTreeNodeFlags.None, Format( fmt, args ) );

	public static bool TreeNodeEx( string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None )
	{
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		return TreeNodeBehavior( window.GetID( label ), flags, label );
	}

	public static bool TreeNodeEx( string strId, ImGuiTreeNodeFlags flags, string fmt, params object[] args )
	{
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		return TreeNodeBehavior( window.GetID( strId ), flags, Format( fmt, args ) );
	}

	/// <summary>Set the open/collapsed state of the next TreeNode/CollapsingHeader/TabItem.</summary>
	public static void SetNextItemOpen( bool isOpen, ImGuiCond cond = ImGuiCond.None )
	{
		var g = G;
		if ( g.CurrentWindow.SkipItems )
			return;
		g.NextItemData.Flags |= ImGuiNextItemDataFlags.HasOpen;
		g.NextItemData.OpenVal = isOpen;
		g.NextItemData.OpenCond = cond != ImGuiCond.None ? cond : ImGuiCond.Always;
	}

	internal static bool TreeNodeUpdateNextOpen( int id, ImGuiTreeNodeFlags flags )
	{
		if ( (flags & ImGuiTreeNodeFlags.Leaf) != 0 )
			return true;

		var g = G;
		var window = g.CurrentWindow;
		var storage = window.DC.StateStorage;

		bool isOpen;
		if ( (g.NextItemData.Flags & ImGuiNextItemDataFlags.HasOpen) != 0 )
		{
			if ( (g.NextItemData.OpenCond & ImGuiCond.Always) != 0 )
			{
				isOpen = g.NextItemData.OpenVal;
				storage.SetInt( id, isOpen ? 1 : 0 );
			}
			else
			{
				int storedValue = storage.GetInt( id, -1 );
				if ( storedValue == -1 )
				{
					isOpen = g.NextItemData.OpenVal;
					storage.SetInt( id, isOpen ? 1 : 0 );
				}
				else
				{
					isOpen = storedValue != 0;
				}
			}
		}
		else
		{
			isOpen = storage.GetInt( id, (flags & ImGuiTreeNodeFlags.DefaultOpen) != 0 ? 1 : 0 ) != 0;
		}
		return isOpen;
	}

	internal static bool TreeNodeBehavior( int id, ImGuiTreeNodeFlags flags, string label )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		bool displayFrame = (flags & ImGuiTreeNodeFlags.Framed) != 0;
		var padding = displayFrame || (flags & ImGuiTreeNodeFlags.FramePadding) != 0 ? style.FramePadding : new Vector2( style.FramePadding.x, MathF.Min( window.DC.CurrLineTextBaseOffset, style.FramePadding.y ) );

		string labelText = LabelText( label );
		var labelSize = CalcTextSize( labelText, false );

		float textOffsetX = g.FontSize + (displayFrame ? padding.x * 3 : padding.x * 2);
		float textOffsetY = MathF.Max( padding.y, window.DC.CurrLineTextBaseOffset );
		float textWidth = g.FontSize + labelSize.x + padding.x * 2;

		float frameHeight = MathF.Max( MathF.Min( window.DC.CurrLineSize.y, g.FontSize + style.FramePadding.y * 2 ), labelSize.y + padding.y * 2 );
		bool spanAllColumns = (flags & ImGuiTreeNodeFlags.SpanAllColumns) != 0 && window.DC.CurrentTableIdx >= 0;
		var frameBb = new ImRect(
			(flags & (ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.SpanAllColumns)) != 0 ? window.WorkRect.Min.x : window.DC.CursorPos.x,
			window.DC.CursorPos.y,
			window.WorkRect.Max.x,
			window.DC.CursorPos.y + frameHeight );
		if ( spanAllColumns )
		{
			var table = CurrentTable;
			if ( table is not null )
			{
				frameBb.Min.x = table.WorkRect.Min.x;
				frameBb.Max.x = table.WorkRect.Max.x;
			}
		}
		if ( displayFrame )
		{
			float outerExtend = ImTrunc( window.WindowPadding.x * 0.5f );
			frameBb.Min.x -= outerExtend;
			frameBb.Max.x += outerExtend;
		}

		var textPos = new Vector2( window.DC.CursorPos.x + textOffsetX, window.DC.CursorPos.y + textOffsetY );
		ItemSize( new Vector2( textWidth, frameHeight ), padding.y );

		var interactBb = frameBb;
		if ( (flags & (ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.SpanAllColumns)) == 0 )
			interactBb.Max.x = frameBb.Min.x + textWidth + (labelSize.x > 0.0f ? style.ItemSpacing.x * 2.0f : 0.0f);
		if ( (flags & ImGuiTreeNodeFlags.SpanTextWidth) != 0 )
			interactBb.Max.x = frameBb.Min.x + textWidth;

		bool isLeaf = (flags & ImGuiTreeNodeFlags.Leaf) != 0;
		bool isOpen = TreeNodeUpdateNextOpen( id, flags );

		bool itemAdd = ItemAdd( interactBb, id );
		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.HasDisplayRect;
		g.LastItemData.DisplayRect = frameBb;

		if ( !itemAdd )
		{
			if ( isOpen && (flags & ImGuiTreeNodeFlags.NoTreePushOnOpen) == 0 )
				TreePushOverrideID( id );
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Openable | (isOpen ? ImGuiItemStatusFlags.Opened : ImGuiItemStatusFlags.None);
			return isOpen;
		}

		var buttonFlags = ImGuiButtonFlags.None;
		if ( (flags & ImGuiTreeNodeFlags.AllowOverlap) != 0 )
			buttonFlags |= ImGuiButtonFlags.AllowOverlap;
		if ( !isLeaf )
			buttonFlags |= ImGuiButtonFlags.PressedOnDragDropHold;

		float arrowHitX1 = textPos.x - textOffsetX - style.TouchExtraPadding.x;
		float arrowHitX2 = textPos.x - textOffsetX + (g.FontSize + padding.x * 2.0f) + style.TouchExtraPadding.x;
		bool isMouseXOverArrow = g.IO.MousePos.x >= arrowHitX1 && g.IO.MousePos.x < arrowHitX2;

		if ( (flags & (ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick)) != 0 )
			buttonFlags |= isMouseXOverArrow ? ImGuiButtonFlags.PressedOnClick : ImGuiButtonFlags.PressedOnClickRelease;
		else
			buttonFlags |= ImGuiButtonFlags.PressedOnClickRelease;
		if ( (flags & ImGuiTreeNodeFlags.OpenOnDoubleClick) != 0 )
			buttonFlags |= ImGuiButtonFlags.PressedOnDoubleClick;

		bool selected = (flags & ImGuiTreeNodeFlags.Selected) != 0;
		bool wasSelected = selected;

		bool pressed = ButtonBehavior( interactBb, id, out bool hovered, out bool held, buttonFlags );
		bool toggled = false;
		if ( !isLeaf )
		{
			if ( pressed && g.DragDropHoldJustPressedId != id )
			{
				if ( (flags & (ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick)) == 0 )
					toggled = true;
				if ( (flags & ImGuiTreeNodeFlags.OpenOnArrow) != 0 )
					toggled |= isMouseXOverArrow;
				if ( (flags & ImGuiTreeNodeFlags.OpenOnDoubleClick) != 0 && g.IO.MouseClickedCount[0] == 2 )
					toggled = true;
			}
			else if ( pressed && g.DragDropHoldJustPressedId == id )
			{
				if ( !isOpen )
					toggled = true;
			}

			if ( toggled )
			{
				isOpen = !isOpen;
				window.DC.StateStorage.SetInt( id, isOpen ? 1 : 0 );
				g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.ToggledOpen;
			}
		}

		if ( selected != wasSelected )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.ToggledSelection;

		// Render
		var textCol = GetColorU32Internal( ImGuiCol.Text );
		if ( displayFrame )
		{
			var bgCol = GetColorU32Internal( held && hovered ? ImGuiCol.HeaderActive : hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header );
			RenderFrame( frameBb.Min, frameBb.Max, bgCol, true, style.FrameRounding );
			if ( (flags & ImGuiTreeNodeFlags.Bullet) != 0 )
				RenderBullet( window.DrawList, new Vector2( textPos.x - textOffsetX * 0.60f, textPos.y + g.FontSize * 0.5f ), textCol );
			else if ( !isLeaf )
				RenderArrow( window.DrawList, new Vector2( textPos.x - textOffsetX + padding.x, textPos.y ), textCol,
					isOpen ? ((flags & ImGuiTreeNodeFlags.UpsideDownArrow) != 0 ? ImGuiDir.Up : ImGuiDir.Down) : ImGuiDir.Right, 1.0f );
			else
				textPos.x -= textOffsetX - padding.x;

			if ( (flags & ImGuiTreeNodeFlags.ClipLabelForTrailingButton) != 0 )
				frameBb.Max.x -= g.FontSize + style.FramePadding.x;
			RenderTextClipped( textPos, frameBb.Max, labelText, labelSize, Vector2.Zero, frameBb );
		}
		else
		{
			if ( hovered || selected )
			{
				var bgCol = GetColorU32Internal( held && hovered ? ImGuiCol.HeaderActive : hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header );
				RenderFrame( frameBb.Min, frameBb.Max, bgCol, false );
			}
			if ( (flags & ImGuiTreeNodeFlags.Bullet) != 0 )
				RenderBullet( window.DrawList, new Vector2( textPos.x - textOffsetX * 0.5f, textPos.y + g.FontSize * 0.5f ), textCol );
			else if ( !isLeaf )
				RenderArrow( window.DrawList, new Vector2( textPos.x - textOffsetX + padding.x, textPos.y + g.FontSize * 0.15f ), textCol,
					isOpen ? ((flags & ImGuiTreeNodeFlags.UpsideDownArrow) != 0 ? ImGuiDir.Up : ImGuiDir.Down) : ImGuiDir.Right, 0.70f );
			RenderText( textPos, labelText, false );
		}

		if ( isOpen && (flags & ImGuiTreeNodeFlags.NoTreePushOnOpen) == 0 )
			TreePushOverrideID( id );

		g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.Openable | (isOpen ? ImGuiItemStatusFlags.Opened : ImGuiItemStatusFlags.None);
		return isOpen;
	}

	/// <summary>Indent() + PushID(). Already called by TreeNode() when returning true.</summary>
	public static void TreePush( string strId )
	{
		var window = GetCurrentWindow();
		Indent();
		window.DC.TreeDepth++;
		PushID( strId );
	}

	public static void TreePush( int intId )
	{
		var window = GetCurrentWindow();
		Indent();
		window.DC.TreeDepth++;
		PushID( intId );
	}

	internal static void TreePushOverrideID( int id )
	{
		var window = GetCurrentWindow();
		Indent();
		window.DC.TreeDepth++;
		PushOverrideID( id );
	}

	/// <summary>Unindent() + PopID().</summary>
	public static void TreePop()
	{
		var window = G.CurrentWindow;
		if ( window.DC.TreeDepth <= 0 )
		{
			Log.Warning( "ImGui: TreePop() called without TreeNode/TreePush" );
			return;
		}
		Unindent();
		window.DC.TreeDepth--;
		PopID();
	}

	/// <summary>Horizontal distance preceding label when using TreeNode*() or Bullet().</summary>
	public static float GetTreeNodeToLabelSpacing()
	{
		var g = G;
		return g.FontSize + g.Style.FramePadding.x * 2.0f;
	}

	/// <summary>If returning 'true' the header is open. Doesn't indent nor push on the ID stack.</summary>
	public static bool CollapsingHeader( string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None )
	{
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		return TreeNodeBehavior( window.GetID( label ), flags | ImGuiTreeNodeFlags.CollapsingHeader, label );
	}

	/// <summary>Collapsing header with a close button. When clicked, <paramref name="visible"/> is set to false.</summary>
	public static bool CollapsingHeader( string label, ref bool visible, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;
		if ( !visible )
			return false;

		int id = window.GetID( label );
		flags |= ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.AllowOverlap | ImGuiTreeNodeFlags.ClipLabelForTrailingButton;
		bool isOpen = TreeNodeBehavior( id, flags, label );

		var lastItemBackup = g.LastItemData;
		float buttonSize = g.FontSize;
		float buttonX = MathF.Max( g.LastItemData.Rect.Min.x, g.LastItemData.Rect.Max.x - g.Style.FramePadding.x - buttonSize );
		float buttonY = g.LastItemData.Rect.Min.y + g.Style.FramePadding.y;
		int closeButtonId = ImHashStr( "#CLOSE", id );
		if ( CloseButton( closeButtonId, new Vector2( buttonX, buttonY ) ) )
			visible = false;
		g.LastItemData = lastItemBackup;

		return isOpen;
	}
	#endregion

	#region Selectable
	/// <summary>A selectable highlights when hovered, and can display another color when selected.</summary>
	public static bool Selectable( string label, bool selected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None, Vector2 sizeArg = default )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = window.GetID( label );
		var labelSize = CalcTextSize( label, true );
		var size = new Vector2( sizeArg.x != 0.0f ? sizeArg.x : labelSize.x, sizeArg.y != 0.0f ? sizeArg.y : labelSize.y );
		var pos = window.DC.CursorPos;
		pos.y += window.DC.CurrLineTextBaseOffset;
		ItemSize( size, 0.0f );

		// Fill horizontal space
		bool spanAllColumns = (flags & ImGuiSelectableFlags.SpanAllColumns) != 0;
		float minX = spanAllColumns ? window.ParentWorkRect.Min.x : pos.x;
		float maxX = spanAllColumns ? window.ParentWorkRect.Max.x : window.WorkRect.Max.x;
		if ( spanAllColumns && CurrentTable is { } table )
		{
			minX = table.WorkRect.Min.x;
			maxX = table.WorkRect.Max.x;
		}
		if ( sizeArg.x == 0.0f || (flags & ImGuiSelectableFlags.SpanAvailWidth) != 0 )
			size.x = MathF.Max( labelSize.x, maxX - minX );

		var textMin = pos;
		var textMax = new Vector2( minX + size.x, pos.y + size.y );

		var bb = new ImRect( minX, pos.y, textMax.x, textMax.y );
		if ( (flags & ImGuiSelectableFlags.NoPadWithHalfSpacing) == 0 )
		{
			float spacingX = spanAllColumns ? 0.0f : style.ItemSpacing.x;
			float spacingY = style.ItemSpacing.y;
			float spacingL = ImTrunc( spacingX * 0.50f );
			float spacingU = ImTrunc( spacingY * 0.50f );
			bb.Min.x -= spacingL;
			bb.Min.y -= spacingU;
			bb.Max.x += spacingX - spacingL;
			bb.Max.y += spacingY - spacingU;
		}

		bool disabledItem = (flags & ImGuiSelectableFlags.Disabled) != 0;
		bool disabledGlobal = (g.CurrentItemFlags & ImGuiItemFlags.Disabled) != 0;
		if ( disabledItem && !disabledGlobal )
			BeginDisabled();

		bool isVisible = ItemAdd( bb, id, null, disabledItem ? ImGuiItemFlags.Disabled : ImGuiItemFlags.None );
		if ( !isVisible )
		{
			if ( disabledItem && !disabledGlobal )
				EndDisabled();
			return false;
		}

		var buttonFlags = ImGuiButtonFlags.None;
		if ( (flags & ImGuiSelectableFlags.NoHoldingActiveID) != 0 ) buttonFlags |= ImGuiButtonFlags.NoHoldingActiveId;
		if ( (flags & ImGuiSelectableFlags.SelectOnClick) != 0 ) buttonFlags |= ImGuiButtonFlags.PressedOnClick;
		if ( (flags & ImGuiSelectableFlags.SelectOnRelease) != 0 ) buttonFlags |= ImGuiButtonFlags.PressedOnRelease;
		if ( (flags & ImGuiSelectableFlags.AllowDoubleClick) != 0 ) buttonFlags |= ImGuiButtonFlags.PressedOnClickRelease | ImGuiButtonFlags.PressedOnDoubleClick;
		if ( (flags & ImGuiSelectableFlags.AllowOverlap) != 0 || (g.LastItemData.InFlags & ImGuiItemFlags.AllowOverlap) != 0 ) buttonFlags |= ImGuiButtonFlags.AllowOverlap;

		bool wasSelected = selected;
		bool pressed = ButtonBehavior( bb, id, out bool hovered, out bool held, buttonFlags );

		if ( pressed )
			MarkItemEdited( id );
		if ( selected != wasSelected )
			g.LastItemData.StatusFlags |= ImGuiItemStatusFlags.ToggledSelection;

		// Render
		if ( (flags & ImGuiSelectableFlags.Highlight) != 0 )
			hovered = true;
		if ( hovered || selected )
		{
			var col = GetColorU32Internal( held && hovered ? ImGuiCol.HeaderActive : hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header );
			RenderFrame( bb.Min, bb.Max, col, false, 0.0f );
		}

		if ( spanAllColumns && window.DC.CurrentColumns is not null )
			PushColumnsBackground();
		RenderTextClipped( textMin, textMax, label, labelSize, style.SelectableTextAlign, bb );
		if ( spanAllColumns && window.DC.CurrentColumns is not null )
			PopColumnsBackground();

		// Automatically close popups
		if ( pressed && (window.Flags & ImGuiWindowFlags.Popup) != 0 && (flags & ImGuiSelectableFlags.NoAutoClosePopups) == 0 && (g.LastItemData.InFlags & ImGuiItemFlags.AutoClosePopups) != 0 )
			CloseCurrentPopup();

		if ( disabledItem && !disabledGlobal )
			EndDisabled();

		return pressed;
	}

	/// <summary>"bool* p_selected" variant: toggles <paramref name="selected"/> when clicked.</summary>
	public static bool Selectable( string label, ref bool selected, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None, Vector2 sizeArg = default )
	{
		if ( Selectable( label, selected, flags, sizeArg ) )
		{
			selected = !selected;
			return true;
		}
		return false;
	}
	#endregion

	#region List box
	/// <summary>Open a framed scrolling region. You can submit contents and manage your selection state however you want.</summary>
	public static bool BeginListBox( string label, Vector2 sizeArg = default )
	{
		var g = G;
		var window = GetCurrentWindow();
		if ( window.SkipItems )
			return false;

		var style = g.Style;
		int id = GetID( label );
		var labelSize = CalcTextSize( label, true );

		// Size default to hold ~7.25 items.
		var size = ImTrunc( CalcItemSize( sizeArg, CalcItemWidth(), GetTextLineHeightWithSpacing() * 7.25f + style.FramePadding.y * 2.0f ) );
		var frameSize = new Vector2( size.x, MathF.Max( size.y, labelSize.y ) );
		var frameBb = new ImRect( window.DC.CursorPos, window.DC.CursorPos + frameSize );
		var bb = new ImRect( frameBb.Min, frameBb.Max + new Vector2( labelSize.x > 0.0f ? style.ItemInnerSpacing.x + labelSize.x : 0.0f, 0.0f ) );
		g.NextItemData.ClearFlags();

		if ( !IsRectVisible( bb.Min, bb.Max ) )
		{
			ItemSize( bb.Size, style.FramePadding.y );
			ItemAdd( bb, 0, frameBb );
			g.NextWindowData.ClearFlags();
			return false;
		}

		BeginGroup();
		if ( labelSize.x > 0.0f )
		{
			var labelPos = new Vector2( frameBb.Max.x + style.ItemInnerSpacing.x, frameBb.Min.y + style.FramePadding.y );
			RenderText( labelPos, label );
			window.DC.CursorMaxPos = ImMax( window.DC.CursorMaxPos, labelPos + labelSize );
		}

		BeginChildEx( label, id, frameBb.Size, ImGuiChildFlags.FrameStyle, ImGuiWindowFlags.None );
		return true;
	}

	public static void EndListBox()
	{
		EndChild();
		EndGroup();
	}

	/// <summary>Simple list box from an array of strings. Returns true when the selection changed.</summary>
	public static bool ListBox( string label, ref int currentItem, string[] items, int heightInItems = -1 )
	{
		var g = G;
		if ( heightInItems < 0 )
			heightInItems = Math.Min( items.Length, 7 );
		float heightInItemsF = heightInItems + 0.25f;
		var size = new Vector2( 0.0f, ImTrunc( GetTextLineHeightWithSpacing() * heightInItemsF + g.Style.FramePadding.y * 2.0f ) );

		if ( !BeginListBox( label, size ) )
			return false;

		bool valueChanged = false;
		for ( int i = 0; i < items.Length; i++ )
		{
			PushID( i );
			bool itemSelected = i == currentItem;
			if ( Selectable( items[i] ?? "*Unknown item*", itemSelected ) )
			{
				currentItem = i;
				valueChanged = true;
			}
			if ( itemSelected )
				SetItemDefaultFocus();
			PopID();
		}
		EndListBox();

		if ( valueChanged )
			MarkItemEdited( g.LastItemData.ID );
		return valueChanged;
	}

	public static bool ListBox( string label, ref int currentItem, Func<int, string> getter, int itemsCount, int heightInItems = -1 )
	{
		var items = new string[itemsCount];
		for ( int i = 0; i < itemsCount; i++ )
			items[i] = getter( i );
		return ListBox( label, ref currentItem, items, heightInItems );
	}
	#endregion
}

#endregion

#region ImGuiSizeCallbackData

/// <summary>
/// Data passed to a size constraint callback set by <see cref="ImGui.SetNextWindowSizeConstraints(Vector2, Vector2, Action{ImGuiSizeCallbackData})"/>.
/// </summary>
public class ImGuiSizeCallbackData
{
	public Vector2 Pos;
	public Vector2 CurrentSize;
	public Vector2 DesiredSize;
}

#endregion

#region ImGuiStorage

/// <summary>
/// Key-value storage used by windows to persist small amounts of state (e.g. which tree nodes are open)
/// across frames, keyed by ImGui ID.
/// </summary>
public class ImGuiStorage
{
	private readonly Dictionary<int, int> _ints = new();
	private readonly Dictionary<int, float> _floats = new();
	private readonly Dictionary<int, object> _objects = new();

	public void Clear()
	{
		_ints.Clear();
		_floats.Clear();
		_objects.Clear();
	}

	public int GetInt( int key, int defaultValue = 0 ) => _ints.TryGetValue( key, out var v ) ? v : defaultValue;
	public void SetInt( int key, int value ) => _ints[key] = value;
	public bool GetBool( int key, bool defaultValue = false ) => GetInt( key, defaultValue ? 1 : 0 ) != 0;
	public void SetBool( int key, bool value ) => SetInt( key, value ? 1 : 0 );
	public float GetFloat( int key, float defaultValue = 0f ) => _floats.TryGetValue( key, out var v ) ? v : defaultValue;
	public void SetFloat( int key, float value ) => _floats[key] = value;
	public object GetObject( int key ) => _objects.TryGetValue( key, out var v ) ? v : null;
	public void SetObject( int key, object value ) => _objects[key] = value;
	public bool ContainsInt( int key ) => _ints.ContainsKey( key );

	public T GetOrCreate<T>( int key ) where T : class, new()
	{
		if ( _objects.TryGetValue( key, out var v ) && v is T typed )
			return typed;

		var created = new T();
		_objects[key] = created;
		return created;
	}
}

#endregion

#region ImRect

/// <summary>
/// An axis-aligned rectangle stored as a min and max corner, in screen pixels.
/// </summary>
public struct ImRect
{
	public Vector2 Min;
	public Vector2 Max;

	public ImRect( Vector2 min, Vector2 max )
	{
		Min = min;
		Max = max;
	}

	public ImRect( float x1, float y1, float x2, float y2 )
	{
		Min = new Vector2( x1, y1 );
		Max = new Vector2( x2, y2 );
	}

	public ImRect( Rect rect ) : this( rect.TopLeft, rect.BottomRight ) { }

	public readonly Vector2 Center => (Min + Max) * 0.5f;
	public readonly Vector2 Size => Max - Min;
	public readonly float Width => Max.x - Min.x;
	public readonly float Height => Max.y - Min.y;
	public readonly float Area => Width * Height;
	public readonly Vector2 TL => Min;
	public readonly Vector2 TR => new( Max.x, Min.y );
	public readonly Vector2 BL => new( Min.x, Max.y );
	public readonly Vector2 BR => Max;
	public readonly bool IsInverted => Min.x > Max.x || Min.y > Max.y;

	public readonly bool Contains( Vector2 p ) => p.x >= Min.x && p.y >= Min.y && p.x < Max.x && p.y < Max.y;
	public readonly bool Contains( ImRect r ) => r.Min.x >= Min.x && r.Min.y >= Min.y && r.Max.x <= Max.x && r.Max.y <= Max.y;
	public readonly bool ContainsWithPad( Vector2 p, Vector2 pad ) => p.x >= Min.x - pad.x && p.y >= Min.y - pad.y && p.x < Max.x + pad.x && p.y < Max.y + pad.y;
	public readonly bool Overlaps( ImRect r ) => r.Min.y < Max.y && r.Max.y > Min.y && r.Min.x < Max.x && r.Max.x > Min.x;

	public void Add( Vector2 p )
	{
		if ( Min.x > p.x ) Min.x = p.x;
		if ( Min.y > p.y ) Min.y = p.y;
		if ( Max.x < p.x ) Max.x = p.x;
		if ( Max.y < p.y ) Max.y = p.y;
	}

	public void Add( ImRect r )
	{
		if ( Min.x > r.Min.x ) Min.x = r.Min.x;
		if ( Min.y > r.Min.y ) Min.y = r.Min.y;
		if ( Max.x < r.Max.x ) Max.x = r.Max.x;
		if ( Max.y < r.Max.y ) Max.y = r.Max.y;
	}

	public void Expand( float amount )
	{
		Min.x -= amount; Min.y -= amount;
		Max.x += amount; Max.y += amount;
	}

	public void Expand( Vector2 amount )
	{
		Min.x -= amount.x; Min.y -= amount.y;
		Max.x += amount.x; Max.y += amount.y;
	}

	public void Translate( Vector2 d )
	{
		Min += d;
		Max += d;
	}

	public void TranslateX( float dx ) { Min.x += dx; Max.x += dx; }
	public void TranslateY( float dy ) { Min.y += dy; Max.y += dy; }

	/// <summary>Simple version, may lead to an inverted rectangle.</summary>
	public void ClipWith( ImRect r )
	{
		Min = ImGui.ImMax( Min, r.Min );
		Max = ImGui.ImMin( Max, r.Max );
	}

	/// <summary>Full version, ensure both points are fully clipped.</summary>
	public void ClipWithFull( ImRect r )
	{
		Min = ImGui.ImClamp( Min, r.Min, r.Max );
		Max = ImGui.ImClamp( Max, r.Min, r.Max );
	}

	public void Floor()
	{
		Min = new Vector2( MathF.Floor( Min.x ), MathF.Floor( Min.y ) );
		Max = new Vector2( MathF.Floor( Max.x ), MathF.Floor( Max.y ) );
	}

	public readonly Rect ToRect() => new( Min, Max - Min );

	public override readonly string ToString() => $"({Min.x:F1},{Min.y:F1})-({Max.x:F1},{Max.y:F1})";
}

#endregion

#region ImGuiCol

public enum ImGuiCol
{
	Text,
	TextDisabled,
	/// <summary>Background of normal windows</summary>
	WindowBg,
	/// <summary>Background of child windows</summary>
	ChildBg,
	/// <summary>Background of popups, menus, tooltips windows</summary>
	PopupBg,
	Border,
	BorderShadow,
	/// <summary>Background of checkbox, radio button, plot, slider, text input</summary>
	FrameBg,
	FrameBgHovered,
	FrameBgActive,
	TitleBg,
	TitleBgActive,
	TitleBgCollapsed,
	MenuBarBg,
	ScrollbarBg,
	ScrollbarGrab,
	ScrollbarGrabHovered,
	ScrollbarGrabActive,
	/// <summary>Checkbox tick and RadioButton circle</summary>
	CheckMark,
	SliderGrab,
	SliderGrabActive,
	Button,
	ButtonHovered,
	ButtonActive,
	/// <summary>Header* colors are used for CollapsingHeader, TreeNode, Selectable, MenuItem</summary>
	Header,
	HeaderHovered,
	HeaderActive,
	Separator,
	SeparatorHovered,
	SeparatorActive,
	ResizeGrip,
	ResizeGripHovered,
	ResizeGripActive,
	InputTextCursor,
	TabHovered,
	Tab,
	TabSelected,
	TabSelectedOverline,
	TabDimmed,
	TabDimmedSelected,
	TabDimmedSelectedOverline,
	PlotLines,
	PlotLinesHovered,
	PlotHistogram,
	PlotHistogramHovered,
	TableHeaderBg,
	TableBorderStrong,
	TableBorderLight,
	TableRowBg,
	TableRowBgAlt,
	TextLink,
	TextSelectedBg,
	DragDropTarget,
	NavCursor,
	NavWindowingHighlight,
	NavWindowingDimBg,
	ModalWindowDimBg,
	COUNT,

	[Obsolete( "Use ButtonHovered" )] ImGuiColButtonHovered = ButtonHovered,
	[Obsolete( "Use TabSelected" )] TabActive = TabSelected,
	[Obsolete( "Use NavCursor" )] NavHighlight = NavCursor,
}

#endregion

#region ImGuiWindowFlags

[Flags]
public enum ImGuiWindowFlags
{
	None = 0,
	NoTitleBar = 1 << 0,
	NoResize = 1 << 1,
	NoMove = 1 << 2,
	NoScrollbar = 1 << 3,
	NoScrollWithMouse = 1 << 4,
	NoCollapse = 1 << 5,
	AlwaysAutoResize = 1 << 6,
	NoBackground = 1 << 7,
	NoSavedSettings = 1 << 8,
	NoMouseInputs = 1 << 9,
	MenuBar = 1 << 10,
	HorizontalScrollbar = 1 << 11,
	NoFocusOnAppearing = 1 << 12,
	NoBringToFrontOnFocus = 1 << 13,
	AlwaysVerticalScrollbar = 1 << 14,
	AlwaysHorizontalScrollbar = 1 << 15,
	NoNavInputs = 1 << 16,
	NoNavFocus = 1 << 17,
	UnsavedDocument = 1 << 18,
	NoNav = NoNavInputs | NoNavFocus,
	NoDecoration = NoTitleBar | NoResize | NoScrollbar | NoCollapse,
	NoInputs = NoMouseInputs | NoNavInputs | NoNavFocus,

	// Internal
	ChildWindow = 1 << 24,
	Tooltip = 1 << 25,
	Popup = 1 << 26,
	Modal = 1 << 27,
	ChildMenu = 1 << 28,
}

#endregion

#region ImGuiChildFlags

[Flags]
public enum ImGuiChildFlags
{
	None = 0,
	Borders = 1 << 0,
	AlwaysUseWindowPadding = 1 << 1,
	ResizeX = 1 << 2,
	ResizeY = 1 << 3,
	AutoResizeX = 1 << 4,
	AutoResizeY = 1 << 5,
	AlwaysAutoResize = 1 << 6,
	FrameStyle = 1 << 7,
	NavFlattened = 1 << 8,
	[Obsolete( "Use Borders" )] Border = Borders,
}

#endregion

#region ImGuiItemFlags

[Flags]
public enum ImGuiItemFlags
{
	None = 0,
	NoTabStop = 1 << 0,
	NoNav = 1 << 1,
	NoNavDefaultFocus = 1 << 2,
	ButtonRepeat = 1 << 3,
	AutoClosePopups = 1 << 4,
	AllowDuplicateId = 1 << 5,

	// Internal
	Disabled = 1 << 10,
	ReadOnly = 1 << 11,
	MixedValue = 1 << 12,
	NoWindowHoverableCheck = 1 << 13,
	AllowOverlap = 1 << 14,
	Inputable = 1 << 20,
}

#endregion

#region ImGuiItemStatusFlags

[Flags]
public enum ImGuiItemStatusFlags
{
	None = 0,
	HoveredRect = 1 << 0,
	HasDisplayRect = 1 << 1,
	Edited = 1 << 2,
	ToggledSelection = 1 << 3,
	ToggledOpen = 1 << 4,
	HasDeactivated = 1 << 5,
	Deactivated = 1 << 6,
	HoveredWindow = 1 << 7,
	Visible = 1 << 8,
	HasClipRect = 1 << 9,
	Openable = 1 << 20,
	Opened = 1 << 21,
	Checkable = 1 << 22,
	Checked = 1 << 23,
	Inputable = 1 << 24,
}

#endregion

#region ImGuiInputTextFlags

[Flags]
public enum ImGuiInputTextFlags
{
	None = 0,
	CharsDecimal = 1 << 0,
	CharsHexadecimal = 1 << 1,
	CharsScientific = 1 << 2,
	CharsUppercase = 1 << 3,
	CharsNoBlank = 1 << 4,
	AllowTabInput = 1 << 5,
	EnterReturnsTrue = 1 << 6,
	EscapeClearsAll = 1 << 7,
	CtrlEnterForNewLine = 1 << 8,
	ReadOnly = 1 << 9,
	Password = 1 << 10,
	AlwaysOverwrite = 1 << 11,
	AutoSelectAll = 1 << 12,
	ParseEmptyRefVal = 1 << 13,
	DisplayEmptyRefVal = 1 << 14,
	NoHorizontalScroll = 1 << 15,
	NoUndoRedo = 1 << 16,
	ElideLeft = 1 << 17,
	CallbackCompletion = 1 << 18,
	CallbackHistory = 1 << 19,
	CallbackAlways = 1 << 20,
	CallbackCharFilter = 1 << 21,
	CallbackResize = 1 << 22,
	CallbackEdit = 1 << 23,

	// Internal
	Multiline = 1 << 26,
	MergedItem = 1 << 27,
}

#endregion

#region ImGuiTreeNodeFlags

[Flags]
public enum ImGuiTreeNodeFlags
{
	None = 0,
	Selected = 1 << 0,
	Framed = 1 << 1,
	AllowOverlap = 1 << 2,
	NoTreePushOnOpen = 1 << 3,
	NoAutoOpenOnLog = 1 << 4,
	DefaultOpen = 1 << 5,
	OpenOnDoubleClick = 1 << 6,
	OpenOnArrow = 1 << 7,
	Leaf = 1 << 8,
	Bullet = 1 << 9,
	FramePadding = 1 << 10,
	SpanAvailWidth = 1 << 11,
	SpanFullWidth = 1 << 12,
	SpanTextWidth = 1 << 13,
	SpanAllColumns = 1 << 14,
	NavLeftJumpsBackHere = 1 << 15,
	CollapsingHeader = Framed | NoTreePushOnOpen | NoAutoOpenOnLog,

	// Internal
	ClipLabelForTrailingButton = 1 << 20,
	UpsideDownArrow = 1 << 21,
}

#endregion

#region ImGuiPopupFlags

[Flags]
public enum ImGuiPopupFlags
{
	None = 0,
	MouseButtonLeft = 0,
	MouseButtonRight = 1,
	MouseButtonMiddle = 2,
	MouseButtonMask_ = 0x1F,
	MouseButtonDefault_ = 1,
	NoReopen = 1 << 5,
	NoOpenOverExistingPopup = 1 << 7,
	NoOpenOverItems = 1 << 8,
	AnyPopupId = 1 << 10,
	AnyPopupLevel = 1 << 11,
	AnyPopup = AnyPopupId | AnyPopupLevel,
}

#endregion

#region ImGuiSelectableFlags

[Flags]
public enum ImGuiSelectableFlags
{
	None = 0,
	NoAutoClosePopups = 1 << 0,
	SpanAllColumns = 1 << 1,
	AllowDoubleClick = 1 << 2,
	Disabled = 1 << 3,
	AllowOverlap = 1 << 4,
	Highlight = 1 << 5,
	[Obsolete( "Use NoAutoClosePopups" )] DontClosePopups = NoAutoClosePopups,

	// Internal
	NoHoldingActiveID = 1 << 20,
	SelectOnClick = 1 << 22,
	SelectOnRelease = 1 << 23,
	SpanAvailWidth = 1 << 24,
	SetNavIdOnHover = 1 << 25,
	NoPadWithHalfSpacing = 1 << 26,
}

#endregion

#region ImGuiComboFlags

[Flags]
public enum ImGuiComboFlags
{
	None = 0,
	PopupAlignLeft = 1 << 0,
	HeightSmall = 1 << 1,
	HeightRegular = 1 << 2,
	HeightLarge = 1 << 3,
	HeightLargest = 1 << 4,
	NoArrowButton = 1 << 5,
	NoPreview = 1 << 6,
	WidthFitPreview = 1 << 7,
	HeightMask_ = HeightSmall | HeightRegular | HeightLarge | HeightLargest,
}

#endregion

#region ImGuiTabBarFlags

[Flags]
public enum ImGuiTabBarFlags
{
	None = 0,
	Reorderable = 1 << 0,
	AutoSelectNewTabs = 1 << 1,
	TabListPopupButton = 1 << 2,
	NoCloseWithMiddleMouseButton = 1 << 3,
	NoTabListScrollingButtons = 1 << 4,
	NoTooltip = 1 << 5,
	DrawSelectedOverline = 1 << 6,
	FittingPolicyResizeDown = 1 << 7,
	FittingPolicyScroll = 1 << 8,
	FittingPolicyMask_ = FittingPolicyResizeDown | FittingPolicyScroll,
	FittingPolicyDefault_ = FittingPolicyResizeDown,
}

#endregion

#region ImGuiTabItemFlags

[Flags]
public enum ImGuiTabItemFlags
{
	None = 0,
	UnsavedDocument = 1 << 0,
	SetSelected = 1 << 1,
	NoCloseWithMiddleMouseButton = 1 << 2,
	NoPushId = 1 << 3,
	NoTooltip = 1 << 4,
	NoReorder = 1 << 5,
	Leading = 1 << 6,
	Trailing = 1 << 7,
	NoAssumedClosure = 1 << 8,
	// Internal
	Button = 1 << 21,
}

#endregion

#region ImGuiFocusedFlags

[Flags]
public enum ImGuiFocusedFlags
{
	None = 0,
	ChildWindows = 1 << 0,
	RootWindow = 1 << 1,
	AnyWindow = 1 << 2,
	NoPopupHierarchy = 1 << 3,
	DockHierarchy = 1 << 4,
	RootAndChildWindows = RootWindow | ChildWindows,
}

#endregion

#region ImGuiHoveredFlags

[Flags]
public enum ImGuiHoveredFlags
{
	None = 0,
	ChildWindows = 1 << 0,
	RootWindow = 1 << 1,
	AnyWindow = 1 << 2,
	NoPopupHierarchy = 1 << 3,
	DockHierarchy = 1 << 4,
	AllowWhenBlockedByPopup = 1 << 5,
	AllowWhenBlockedByActiveItem = 1 << 7,
	AllowWhenOverlappedByItem = 1 << 8,
	AllowWhenOverlappedByWindow = 1 << 9,
	AllowWhenDisabled = 1 << 10,
	NoNavOverride = 1 << 11,
	AllowWhenOverlapped = AllowWhenOverlappedByItem | AllowWhenOverlappedByWindow,
	RectOnly = AllowWhenBlockedByPopup | AllowWhenBlockedByActiveItem | AllowWhenOverlapped,
	RootAndChildWindows = RootWindow | ChildWindows,
	ForTooltip = 1 << 12,
	Stationary = 1 << 13,
	DelayNone = 1 << 14,
	DelayShort = 1 << 15,
	DelayNormal = 1 << 16,
	NoSharedDelay = 1 << 17,
}

#endregion

#region ImGuiDragDropFlags

[Flags]
public enum ImGuiDragDropFlags
{
	None = 0,
	SourceNoPreviewTooltip = 1 << 0,
	SourceNoDisableHover = 1 << 1,
	SourceNoHoldToOpenOthers = 1 << 2,
	SourceAllowNullID = 1 << 3,
	SourceExtern = 1 << 4,
	PayloadAutoExpire = 1 << 5,
	PayloadNoCrossContext = 1 << 6,
	PayloadNoCrossProcess = 1 << 7,
	AcceptBeforeDelivery = 1 << 10,
	AcceptNoDrawDefaultRect = 1 << 11,
	AcceptNoPreviewTooltip = 1 << 12,
	AcceptPeekOnly = AcceptBeforeDelivery | AcceptNoDrawDefaultRect,
}

#endregion

#region ImGuiDir

public enum ImGuiDir
{
	None = -1,
	Left = 0,
	Right = 1,
	Up = 2,
	Down = 3,
}

#endregion

#region ImGuiSortDirection

public enum ImGuiSortDirection
{
	None = 0,
	Ascending = 1,
	Descending = 2,
}

#endregion

#region ImGuiMouseButton

public enum ImGuiMouseButton
{
	Left = 0,
	Right = 1,
	Middle = 2,
}

#endregion

#region ImGuiMouseCursor

public enum ImGuiMouseCursor
{
	None = -1,
	Arrow = 0,
	TextInput,
	ResizeAll,
	ResizeNS,
	ResizeEW,
	ResizeNESW,
	ResizeNWSE,
	Hand,
	NotAllowed,
}

#endregion

#region ImGuiCond

[Flags]
public enum ImGuiCond
{
	None = 0,
	Always = 1 << 0,
	Once = 1 << 1,
	FirstUseEver = 1 << 2,
	Appearing = 1 << 3,
}

#endregion

#region ImGuiButtonFlags

[Flags]
public enum ImGuiButtonFlags
{
	None = 0,
	MouseButtonLeft = 1 << 0,
	MouseButtonRight = 1 << 1,
	MouseButtonMiddle = 1 << 2,
	MouseButtonMask_ = MouseButtonLeft | MouseButtonRight | MouseButtonMiddle,
	EnableNav = 1 << 3,

	// Internal
	PressedOnClick = 1 << 4,
	PressedOnClickRelease = 1 << 5,
	PressedOnClickReleaseAnywhere = 1 << 6,
	PressedOnRelease = 1 << 7,
	PressedOnDoubleClick = 1 << 8,
	PressedOnDragDropHold = 1 << 9,
	FlattenChildren = 1 << 11,
	AllowOverlap = 1 << 12,
	AlignTextBaseLine = 1 << 15,
	NoKeyModsAllowed = 1 << 16,
	NoHoldingActiveId = 1 << 17,
	NoNavFocus = 1 << 18,
	NoHoveredOnFocus = 1 << 19,
	NoSetKeyOwner = 1 << 20,
	NoTestKeyOwner = 1 << 21,
	PressedOnMask_ = PressedOnClick | PressedOnClickRelease | PressedOnClickReleaseAnywhere | PressedOnRelease | PressedOnDoubleClick | PressedOnDragDropHold,
	PressedOnDefault_ = PressedOnClickRelease,
}

#endregion

#region ImGuiSliderFlags

[Flags]
public enum ImGuiSliderFlags
{
	None = 0,
	Logarithmic = 1 << 5,
	NoRoundToFormat = 1 << 6,
	NoInput = 1 << 7,
	WrapAround = 1 << 8,
	ClampOnInput = 1 << 9,
	ClampZeroRange = 1 << 10,
	NoSpeedTweaks = 1 << 11,
	AlwaysClamp = ClampOnInput | ClampZeroRange,
	// Internal
	Vertical = 1 << 20,
	ReadOnly = 1 << 21,
}

#endregion

#region ImGuiColorEditFlags

[Flags]
public enum ImGuiColorEditFlags
{
	None = 0,
	NoAlpha = 1 << 1,
	NoPicker = 1 << 2,
	NoOptions = 1 << 3,
	NoSmallPreview = 1 << 4,
	NoInputs = 1 << 5,
	NoTooltip = 1 << 6,
	NoLabel = 1 << 7,
	NoSidePreview = 1 << 8,
	NoDragDrop = 1 << 9,
	NoBorder = 1 << 10,
	AlphaOpaque = 1 << 11,
	AlphaNoBg = 1 << 12,
	AlphaPreviewHalf = 1 << 13,
	AlphaBar = 1 << 16,
	HDR = 1 << 19,
	DisplayRGB = 1 << 20,
	DisplayHSV = 1 << 21,
	DisplayHex = 1 << 22,
	Uint8 = 1 << 23,
	Float = 1 << 24,
	PickerHueBar = 1 << 25,
	PickerHueWheel = 1 << 26,
	InputRGB = 1 << 27,
	InputHSV = 1 << 28,
	[Obsolete( "Removed in Dear ImGui 1.91.8" )] AlphaPreview = 0,

	DefaultOptions_ = Uint8 | DisplayRGB | InputRGB | PickerHueBar,
	AlphaMask_ = NoAlpha | AlphaOpaque | AlphaNoBg | AlphaPreviewHalf,
	DisplayMask_ = DisplayRGB | DisplayHSV | DisplayHex,
	DataTypeMask_ = Uint8 | Float,
	PickerMask_ = PickerHueWheel | PickerHueBar,
	InputMask_ = InputRGB | InputHSV,
}

#endregion

#region ImGuiTableFlags

[Flags]
public enum ImGuiTableFlags
{
	None = 0,
	Resizable = 1 << 0,
	Reorderable = 1 << 1,
	Hideable = 1 << 2,
	Sortable = 1 << 3,
	NoSavedSettings = 1 << 4,
	ContextMenuInBody = 1 << 5,
	RowBg = 1 << 6,
	BordersInnerH = 1 << 7,
	BordersOuterH = 1 << 8,
	BordersInnerV = 1 << 9,
	BordersOuterV = 1 << 10,
	BordersH = BordersInnerH | BordersOuterH,
	BordersV = BordersInnerV | BordersOuterV,
	BordersInner = BordersInnerV | BordersInnerH,
	BordersOuter = BordersOuterV | BordersOuterH,
	Borders = BordersInner | BordersOuter,
	NoBordersInBody = 1 << 11,
	NoBordersInBodyUntilResize = 1 << 12,
	SizingFixedFit = 1 << 13,
	SizingFixedSame = 2 << 13,
	SizingStretchProp = 3 << 13,
	SizingStretchSame = 4 << 13,
	NoHostExtendX = 1 << 16,
	NoHostExtendY = 1 << 17,
	NoKeepColumnsVisible = 1 << 18,
	PreciseWidths = 1 << 19,
	NoClip = 1 << 20,
	PadOuterX = 1 << 21,
	NoPadOuterX = 1 << 22,
	NoPadInnerX = 1 << 23,
	ScrollX = 1 << 24,
	ScrollY = 1 << 25,
	SortMulti = 1 << 26,
	SortTristate = 1 << 27,
	HighlightHoveredColumn = 1 << 28,
	SizingMask_ = SizingFixedFit | SizingFixedSame | SizingStretchProp | SizingStretchSame,
}

#endregion

#region ImGuiTableColumnFlags

[Flags]
public enum ImGuiTableColumnFlags
{
	None = 0,
	Disabled = 1 << 0,
	DefaultHide = 1 << 1,
	DefaultSort = 1 << 2,
	WidthStretch = 1 << 3,
	WidthFixed = 1 << 4,
	NoResize = 1 << 5,
	NoReorder = 1 << 6,
	NoHide = 1 << 7,
	NoClip = 1 << 8,
	NoSort = 1 << 9,
	NoSortAscending = 1 << 10,
	NoSortDescending = 1 << 11,
	NoHeaderLabel = 1 << 12,
	NoHeaderWidth = 1 << 13,
	PreferSortAscending = 1 << 14,
	PreferSortDescending = 1 << 15,
	IndentEnable = 1 << 16,
	IndentDisable = 1 << 17,
	AngledHeader = 1 << 18,
	IsEnabled = 1 << 24,
	IsVisible = 1 << 25,
	IsSorted = 1 << 26,
	IsHovered = 1 << 27,
	WidthMask_ = WidthStretch | WidthFixed,
	IndentMask_ = IndentEnable | IndentDisable,
	StatusMask_ = IsEnabled | IsVisible | IsSorted | IsHovered,
}

#endregion

#region ImGuiTableRowFlags

[Flags]
public enum ImGuiTableRowFlags
{
	None = 0,
	Headers = 1 << 0,
}

#endregion

#region ImGuiTableBgTarget

public enum ImGuiTableBgTarget
{
	None = 0,
	RowBg0 = 1,
	RowBg1 = 2,
	CellBg = 3,
}

#endregion

#region ImGuiStyleVar

public enum ImGuiStyleVar
{
	Alpha,
	DisabledAlpha,
	WindowPadding,
	WindowRounding,
	WindowBorderSize,
	WindowMinSize,
	WindowTitleAlign,
	ChildRounding,
	ChildBorderSize,
	PopupRounding,
	PopupBorderSize,
	FramePadding,
	FrameRounding,
	FrameBorderSize,
	ItemSpacing,
	ItemInnerSpacing,
	IndentSpacing,
	CellPadding,
	ScrollbarSize,
	ScrollbarRounding,
	GrabMinSize,
	GrabRounding,
	TabRounding,
	TabBorderSize,
	TabBarBorderSize,
	TabBarOverlineSize,
	TableAngledHeadersAngle,
	TableAngledHeadersTextAlign,
	ButtonTextAlign,
	SelectableTextAlign,
	SeparatorTextBorderSize,
	SeparatorTextAlign,
	SeparatorTextPadding,
	COUNT
}

#endregion

#region ImDrawFlags

[Flags]
public enum ImDrawFlags
{
	None = 0,
	Closed = 1 << 0,
	RoundCornersTopLeft = 1 << 4,
	RoundCornersTopRight = 1 << 5,
	RoundCornersBottomLeft = 1 << 6,
	RoundCornersBottomRight = 1 << 7,
	RoundCornersNone = 1 << 8,
	RoundCornersTop = RoundCornersTopLeft | RoundCornersTopRight,
	RoundCornersBottom = RoundCornersBottomLeft | RoundCornersBottomRight,
	RoundCornersLeft = RoundCornersBottomLeft | RoundCornersTopLeft,
	RoundCornersRight = RoundCornersBottomRight | RoundCornersTopRight,
	RoundCornersAll = RoundCornersTopLeft | RoundCornersTopRight | RoundCornersBottomLeft | RoundCornersBottomRight,
	RoundCornersDefault_ = RoundCornersAll,
	RoundCornersMask_ = RoundCornersAll | RoundCornersNone,
}

#endregion

#region ImGuiKey

public enum ImGuiKey
{
	None = 0,
	Tab = 512,
	LeftArrow, RightArrow, UpArrow, DownArrow,
	PageUp, PageDown, Home, End, Insert, Delete, Backspace, Space, Enter, Escape,
	LeftCtrl, LeftShift, LeftAlt, LeftSuper,
	RightCtrl, RightShift, RightAlt, RightSuper,
	Menu,
	_0, _1, _2, _3, _4, _5, _6, _7, _8, _9,
	A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
	F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
	Apostrophe, Comma, Minus, Period, Slash, Semicolon, Equal, LeftBracket, Backslash, RightBracket, GraveAccent,
	CapsLock, ScrollLock, NumLock, PrintScreen, Pause,
	Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
	KeypadDecimal, KeypadDivide, KeypadMultiply, KeypadSubtract, KeypadAdd, KeypadEnter, KeypadEqual,

	MouseLeft, MouseRight, MouseMiddle, MouseX1, MouseX2, MouseWheelX, MouseWheelY,

	COUNT,

	// Modifiers (may be combined with keys in shortcut APIs)
	ImGuiMod_None = 0,
	ImGuiMod_Ctrl = 1 << 12,
	ImGuiMod_Shift = 1 << 13,
	ImGuiMod_Alt = 1 << 14,
	ImGuiMod_Super = 1 << 15,
	ImGuiMod_Mask_ = 0xF000,
}

#endregion

#region ComponentExtensions

/// <summary>
/// Draws an ImGui inspector for any component's [Property] members.
/// </summary>
public static class ComponentExtensions
{
	private static readonly Dictionary<Type, List<PropertyDescription>> _propertyCache = new();

	private static List<PropertyDescription> GetProperties( Type type )
	{
		if ( _propertyCache.TryGetValue( type, out var props ) )
			return props;

		var typeDesc = TypeLibrary.GetType( type );
		props = typeDesc?.Properties
			.Where( p => p.HasAttribute<PropertyAttribute>() && p.CanRead && !typeof( Delegate ).IsAssignableFrom( p.PropertyType ) )
			.ToList() ?? new List<PropertyDescription>();
		_propertyCache[type] = props;
		return props;
	}

	/// <summary>
	/// Draw an editor for every [Property] of this component. If called outside of a window, a window named after the component type is created.
	/// </summary>
	public static void ImGuiInspector( this Component component )
	{
		if ( !component.IsValid() )
			return;

		var properties = GetProperties( component.GetType() );
		bool ownWindow = ImGui.G.CurrentWindow is null || ImGui.G.CurrentWindow.IsFallbackWindow;
		if ( ownWindow )
		{
			var title = TypeLibrary.GetType( component.GetType() )?.Title ?? component.GetType().Name;
			if ( !ImGui.Begin( $"{title}###{component.Id}" ) )
			{
				ImGui.End();
				return;
			}
		}

		ImGui.PushID( component.Id.GetHashCode() );
		for ( int i = 0; i < properties.Count; i++ )
		{
			// Skip event hooks (OnComponentEnabled etc.): delegates are not editable data.
			if ( typeof( Delegate ).IsAssignableFrom( properties[i].PropertyType ) )
				continue;
			ImGui.PushID( i );
			component.ImGuiProperty( properties[i] );
			ImGui.PopID();
		}
		ImGui.PopID();

		if ( ownWindow )
			ImGui.End();
	}

	/// <summary>
	/// Draw an editor for one property. Returns true if the value was changed.
	/// </summary>
	public static bool ImGuiProperty( this Component component, PropertyDescription prop )
	{
		if ( !component.IsValid() || prop is null )
			return false;

		var label = prop.Title ?? prop.Name;
		var type = prop.PropertyType;
		var range = prop.GetCustomAttribute<RangeAttribute>();
		bool readOnly = !prop.CanWrite;
		object value;
		try
		{
			value = prop.GetValue( component );
		}
		catch ( Exception )
		{
			return false;
		}

		if ( readOnly )
			ImGui.BeginDisabled();

		bool changed = false;
		object newValue = value;

		if ( type == typeof( float ) )
		{
			var v = (float)value;
			changed = range is not null
				? ImGui.SliderFloat( label, ref v, range.Min, range.Max )
				: ImGui.DragFloat( label, ref v, 0.1f );
			newValue = v;
		}
		else if ( type == typeof( double ) )
		{
			var v = (double)value;
			var f = (float)v;
			changed = range is not null
				? ImGui.SliderFloat( label, ref f, range.Min, range.Max )
				: ImGui.DragFloat( label, ref f, 0.1f );
			newValue = (double)f;
		}
		else if ( type == typeof( int ) )
		{
			var v = (int)value;
			changed = range is not null
				? ImGui.SliderInt( label, ref v, (int)range.Min, (int)range.Max )
				: ImGui.DragInt( label, ref v, 0.2f );
			newValue = v;
		}
		else if ( type == typeof( bool ) )
		{
			var v = (bool)value;
			changed = ImGui.Checkbox( label, ref v );
			newValue = v;
		}
		else if ( type == typeof( string ) )
		{
			var v = (string)value ?? string.Empty;
			changed = ImGui.InputText( label, ref v );
			newValue = v;
		}
		else if ( type == typeof( Vector2 ) )
		{
			var v = (Vector2)value;
			changed = range is not null
				? ImGui.SliderFloat2( label, ref v, range.Min, range.Max )
				: ImGui.DragFloat2( label, ref v, 0.1f );
			newValue = v;
		}
		else if ( type == typeof( Vector3 ) )
		{
			var v = (Vector3)value;
			changed = range is not null
				? ImGui.SliderFloat3( label, ref v, range.Min, range.Max )
				: ImGui.DragFloat3( label, ref v, 0.1f );
			newValue = v;
		}
		else if ( type == typeof( Vector4 ) )
		{
			var v = (Vector4)value;
			changed = range is not null
				? ImGui.SliderFloat4( label, ref v, range.Min, range.Max )
				: ImGui.DragFloat4( label, ref v, 0.1f );
			newValue = v;
		}
		else if ( type == typeof( Angles ) )
		{
			var a = (Angles)value;
			var v = new Vector3( a.pitch, a.yaw, a.roll );
			changed = ImGui.DragFloat3( label, ref v, 0.5f );
			newValue = new Angles( v.x, v.y, v.z );
		}
		else if ( type == typeof( Rotation ) )
		{
			var a = ((Rotation)value).Angles();
			var v = new Vector3( a.pitch, a.yaw, a.roll );
			changed = ImGui.DragFloat3( label, ref v, 0.5f );
			newValue = Rotation.From( new Angles( v.x, v.y, v.z ) );
		}
		else if ( type == typeof( Color ) )
		{
			var v = (Color)value;
			changed = ImGui.ColorEdit4( label, ref v );
			newValue = v;
		}
		else if ( type.IsEnum )
		{
			var names = Enum.GetNames( type );
			var values = Enum.GetValues( type );
			int current = Array.IndexOf( values, value );
			changed = ImGui.Combo( label, ref current, names );
			if ( changed && current >= 0 )
				newValue = values.GetValue( current );
		}
		else
		{
			ImGui.LabelText( label, "{0}", value?.ToString() ?? "null" );
		}

		if ( readOnly )
			ImGui.EndDisabled();

		if ( changed && !readOnly )
		{
			try
			{
				prop.SetValue( component, newValue );
			}
			catch ( Exception e )
			{
				Log.Warning( $"ImGuiInspector: could not set {prop.Name}: {e.Message}" );
				return false;
			}
		}
		return changed;
	}
}

#endregion

#region ImGuiIO

public class ImGuiIO
{
	public ImGuiIO()
	{
		for ( int i = 0; i < KeysData.Length; i++ )
			KeysData[i] = new KeyData { DownDuration = -1f, DownDurationPrev = -1f };
	}

	#region Configuration
	/// <summary>Main display size in pixels. Updated every frame from <see cref="Screen.Size"/>.</summary>
	public Vector2 DisplaySize;
	/// <summary>Time elapsed since last frame, in seconds.</summary>
	public float DeltaTime = 1.0f / 60.0f;
	/// <summary>When enabled, style sizes and font size are scaled so the UI looks the same at any resolution (reference: 1080p).</summary>
	public bool AutoScale = true;
	/// <summary>Additional global scale applied on top of <see cref="AutoScale"/>.</summary>
	public float FontGlobalScale = 1.0f;
	/// <summary>Font family used to render all text.</summary>
	public string FontName = "Roboto Mono";
	/// <summary>Font size in pixels at the reference resolution (1080p).</summary>
	public float FontSize = 15.0f;
	public int FontWeight = 400;

	public float MouseDoubleClickTime = 0.30f;
	public float MouseDoubleClickMaxDist = 6.0f;
	public float MouseDragThreshold = 6.0f;
	public float KeyRepeatDelay = 0.275f;
	public float KeyRepeatRate = 0.050f;

	public bool ConfigInputTextCursorBlink = true;
	public bool ConfigInputTextEnterKeepActive = false;
	public bool ConfigDragClickToInputText = false;
	public bool ConfigWindowsResizeFromEdges = true;
	public bool ConfigWindowsMoveFromTitleBarOnly = false;
	public float ConfigMemoryCompactTimer = 60.0f;
	/// <summary>When true, the mouse cursor shape is driven by ImGui (e.g. text beam over text inputs).</summary>
	public bool ConfigDrawCursorShape = true;
	#endregion

	#region Input (written by the backend, or by you to inject input)
	public Vector2 MousePos = new( -float.MaxValue, -float.MaxValue );
	public bool[] MouseDown = new bool[5];
	public float MouseWheel;
	public float MouseWheelH;
	public bool KeyCtrl;
	public bool KeyShift;
	public bool KeyAlt;
	public bool KeySuper;
	#endregion

	#region Output
	/// <summary>Set when ImGui wants to use the mouse; your game should not process mouse input when true.</summary>
	public bool WantCaptureMouse;
	/// <summary>Set when ImGui wants to use the keyboard (e.g. a text field is active).</summary>
	public bool WantCaptureKeyboard;
	/// <summary>Set when a text input widget is active.</summary>
	public bool WantTextInput;
	public float Framerate;
	public int MetricsRenderVertices;
	public int MetricsRenderWindows;
	public int MetricsActiveWindows;
	public Vector2 MouseDelta;
	#endregion

	#region Internal state
	internal Vector2 MousePosPrev = new( -float.MaxValue, -float.MaxValue );
	internal Vector2[] MouseClickedPos = new Vector2[5];
	internal double[] MouseClickedTime = new double[5] { -1000, -1000, -1000, -1000, -1000 };
	internal bool[] MouseClicked = new bool[5];
	internal bool[] MouseDoubleClicked = new bool[5];
	internal int[] MouseClickedCount = new int[5];
	internal int[] MouseClickedLastCount = new int[5];
	internal bool[] MouseReleased = new bool[5];
	internal bool[] MouseDownOwned = new bool[5];
	internal float[] MouseDownDuration = new float[5] { -1, -1, -1, -1, -1 };
	internal float[] MouseDownDurationPrev = new float[5] { -1, -1, -1, -1, -1 };
	internal float[] MouseDragMaxDistanceSqr = new float[5];

	internal readonly List<char> InputQueueCharacters = new();

	internal struct KeyTyped
	{
		public ImGuiKey Key;
		public bool Ctrl;
		public bool Shift;
		public bool Alt;
	}

	/// <summary>Keys typed this frame (including OS key repeats), consumed by text input widgets.</summary>
	internal readonly List<KeyTyped> InputQueueKeys = new();
	internal string PastedText;

	internal struct KeyData
	{
		public bool Down;
		public float DownDuration;
		public float DownDurationPrev;
	}

	internal readonly KeyData[] KeysData = new KeyData[(int)ImGuiKey.COUNT];
	internal readonly bool[] KeysDownFromEvents = new bool[(int)ImGuiKey.COUNT];

	private struct InputEvent
	{
		public int Type; // 0 = mouse button, 1 = wheel
		public int Button;
		public bool Down;
		public Vector2 Wheel;
	}

	private readonly List<InputEvent> _inputEvents = new();

	// Text/key events are double-buffered: anything queued during a frame is delivered at the next NewFrame,
	// so events arriving after widgets were processed are never lost.
	private readonly List<char> _pendingCharacters = new();
	private readonly List<KeyTyped> _pendingKeys = new();
	private string _pendingPaste;
	#endregion

	#region Input API
	/// <summary>Queue a mouse button change. Events are trickled so that fast clicks are never lost.</summary>
	public void AddMouseButtonEvent( int button, bool down )
	{
		if ( button < 0 || button >= MouseDown.Length )
			return;
		_inputEvents.Add( new InputEvent { Type = 0, Button = button, Down = down } );
	}

	public void AddMouseWheelEvent( float wheelX, float wheelY )
	{
		_inputEvents.Add( new InputEvent { Type = 1, Wheel = new Vector2( wheelX, wheelY ) } );
	}

	public void AddMousePosEvent( float x, float y ) => MousePos = new Vector2( x, y );

	public void AddInputCharacter( char c )
	{
		if ( c != 0 )
			_pendingCharacters.Add( c );
	}

	public void AddInputCharactersUTF8( string str )
	{
		if ( str is null ) return;
		foreach ( var c in str ) AddInputCharacter( c );
	}

	public void AddKeyEvent( ImGuiKey key, bool down )
	{
		var idx = (int)key;
		if ( idx <= 0 || idx >= KeysDownFromEvents.Length )
			return;
		KeysDownFromEvents[idx] = down;
	}

	/// <summary>Queue a typed key (including OS auto-repeat) for text editing widgets.</summary>
	public void AddKeyTyped( ImGuiKey key, bool ctrl = false, bool shift = false, bool alt = false )
	{
		if ( key == ImGuiKey.None ) return;
		_pendingKeys.Add( new KeyTyped { Key = key, Ctrl = ctrl, Shift = shift, Alt = alt } );
	}

	/// <summary>Queue text to be pasted into the active text input.</summary>
	public void AddPasteEvent( string text ) => _pendingPaste = text;

	public void ClearInputKeys()
	{
		Array.Clear( KeysDownFromEvents );
		InputQueueCharacters.Clear();
		InputQueueKeys.Clear();
		_pendingCharacters.Clear();
		_pendingKeys.Clear();
		_pendingPaste = null;
	}

	/// <summary>
	/// Apply queued mouse events. A button that changes twice in a frame leaves the second change for the next frame.
	/// </summary>
	internal void ProcessInputEvents()
	{
		InputQueueCharacters.Clear();
		InputQueueCharacters.AddRange( _pendingCharacters );
		_pendingCharacters.Clear();
		InputQueueKeys.Clear();
		InputQueueKeys.AddRange( _pendingKeys );
		_pendingKeys.Clear();
		PastedText = _pendingPaste;
		_pendingPaste = null;

		MouseWheel = 0;
		MouseWheelH = 0;
		Span<bool> changed = stackalloc bool[5];
		int consumed = 0;
		for ( ; consumed < _inputEvents.Count; consumed++ )
		{
			var e = _inputEvents[consumed];
			if ( e.Type == 0 )
			{
				if ( changed[e.Button] )
					break;
				if ( MouseDown[e.Button] != e.Down )
				{
					MouseDown[e.Button] = e.Down;
					changed[e.Button] = true;
				}
			}
			else
			{
				MouseWheelH += e.Wheel.x;
				MouseWheel += e.Wheel.y;
			}
		}
		_inputEvents.RemoveRange( 0, consumed );
	}
	#endregion
}

#endregion

#region ImGuiStyle

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

#endregion

#region ImDrawList

/// <summary>
/// A list of draw commands in screen space. Mirrors Dear ImGui's ImDrawList API. Commands are recorded
/// while widgets are submitted, then replayed through <see cref="Painter"/> at the end of the frame.
/// </summary>
public class ImDrawList
{
	internal enum CmdType
	{
		RectFilled,
		Rect,
		Line,
		Polyline,
		PolygonFilled,
		Circle,
		CircleFilled,
		Ellipse,
		EllipseFilled,
		Text,
		Image,
		BezierCubic,
		BezierQuadratic,
		RectGradient,
	}

	internal struct Cmd
	{
		public CmdType Type;
		public int ClipIdx;
		public Vector2 P1, P2, P3, P4;
		public Color C1, C2;
		public float Thickness;
		public float Rounding;
		public ImDrawFlags Flags;
		public string Text;
		public float FontSize;
		public Texture Texture;
		public Vector2[] Points;
		public bool Closed;
	}

	public ImDrawList( string name )
	{
		Name = name;
		ResetForNewFrame();
	}

	public string Name { get; }

	private readonly List<Cmd> _cmds = new();
	private List<Cmd>[] _channelStorage = new List<Cmd>[1];
	private int _currentChannel;
	private int _channelCount = 1;

	internal readonly List<ImRect> ClipRects = new();
	private readonly List<int> _clipStack = new();
	private readonly List<Vector2> _path = new();

	internal int CommandCount => _cmds.Count;

	internal static ImRect FullscreenRect => new( new Vector2( -8192f, -8192f ), new Vector2( 8192f, 8192f ) );

	internal void ResetForNewFrame()
	{
		_cmds.Clear();
		for ( int i = 1; i < _channelStorage.Length; i++ )
			_channelStorage[i]?.Clear();
		_currentChannel = 0;
		_channelCount = 1;
		ClipRects.Clear();
		_clipStack.Clear();
		_path.Clear();
		ClipRects.Add( FullscreenRect );
		_clipStack.Add( 0 );
	}

	private int CurrentClipIdx => _clipStack.Count > 0 ? _clipStack[^1] : 0;

	#region Clipping
	/// <summary>
	/// Render-level scissoring. Use ImGui.PushClipRect() instead to affect logic (hit-testing and widget culling).
	/// </summary>
	public void PushClipRect( Vector2 clipRectMin, Vector2 clipRectMax, bool intersectWithCurrentClipRect = false )
	{
		var cr = new ImRect( clipRectMin, clipRectMax );
		if ( intersectWithCurrentClipRect && ClipRects.Count > 0 )
			cr.ClipWithFull( ClipRects[CurrentClipIdx] );
		cr.Max = ImGui.ImMax( cr.Min, cr.Max );
		ClipRects.Add( cr );
		_clipStack.Add( ClipRects.Count - 1 );
	}

	public void PushClipRectFullScreen() => PushClipRect( FullscreenRect.Min, FullscreenRect.Max );

	public void PopClipRect()
	{
		if ( _clipStack.Count > 1 )
			_clipStack.RemoveAt( _clipStack.Count - 1 );
	}

	public Vector2 GetClipRectMin() => CurrentClipRect.Min;
	public Vector2 GetClipRectMax() => CurrentClipRect.Max;
	internal ImRect CurrentClipRect => ClipRects.Count > 0 ? ClipRects[CurrentClipIdx] : FullscreenRect;
	#endregion

	#region Channels
	/// <summary>
	/// Split the draw list into layers, so that items can be submitted out of order. Channel 0 holds the existing contents.
	/// </summary>
	public void ChannelsSplit( int count )
	{
		if ( count < 1 ) count = 1;
		if ( _channelStorage.Length < count )
			Array.Resize( ref _channelStorage, count );
		for ( int i = 1; i < count; i++ )
		{
			_channelStorage[i] ??= new List<Cmd>();
			_channelStorage[i].Clear();
		}
		_channelCount = count;
		_currentChannel = 0;
	}

	public void ChannelsSetCurrent( int idx )
	{
		if ( idx < 0 || idx >= _channelCount ) return;
		_currentChannel = idx;
	}

	public void ChannelsMerge()
	{
		if ( _channelCount <= 1 ) return;
		for ( int i = 1; i < _channelCount; i++ )
		{
			_cmds.AddRange( _channelStorage[i] );
			_channelStorage[i].Clear();
		}
		_channelCount = 1;
		_currentChannel = 0;
	}

	private void Add( in Cmd cmd )
	{
		var c = cmd;
		c.ClipIdx = CurrentClipIdx;
		if ( _currentChannel == 0 )
			_cmds.Add( c );
		else
			_channelStorage[_currentChannel].Add( c );
	}
	#endregion

	private static Color ToColor( Color32 c ) => new( c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f );

	#region Primitives
	public void AddLine( Vector2 p1, Vector2 p2, Color32 col, float thickness = 1.0f )
	{
		if ( col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.Line, P1 = p1, P2 = p2, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddRect( Vector2 pMin, Vector2 pMax, Color32 col, float rounding = 0.0f, ImDrawFlags flags = ImDrawFlags.None, float thickness = 1.0f )
	{
		if ( col.a == 0 || thickness <= 0f ) return;
		Add( new Cmd { Type = CmdType.Rect, P1 = pMin, P2 = pMax, C1 = ToColor( col ), Rounding = rounding, Flags = flags, Thickness = thickness } );
	}

	public void AddRectFilled( Vector2 pMin, Vector2 pMax, Color32 col, float rounding = 0.0f, ImDrawFlags flags = ImDrawFlags.None )
	{
		if ( col.a == 0 ) return;
		if ( pMax.x <= pMin.x || pMax.y <= pMin.y ) return;
		Add( new Cmd { Type = CmdType.RectFilled, P1 = pMin, P2 = pMax, C1 = ToColor( col ), Rounding = rounding, Flags = flags } );
	}

	public void AddRectFilledMultiColor( Vector2 pMin, Vector2 pMax, Color32 colUprLeft, Color32 colUprRight, Color32 colBotRight, Color32 colBotLeft )
	{
		if ( pMax.x <= pMin.x || pMax.y <= pMin.y ) return;
		if ( colUprLeft.a == 0 && colUprRight.a == 0 && colBotRight.a == 0 && colBotLeft.a == 0 ) return;

		bool vertical = colUprLeft == colUprRight && colBotLeft == colBotRight;
		bool horizontal = colUprLeft == colBotLeft && colUprRight == colBotRight;
		if ( vertical )
		{
			Add( new Cmd { Type = CmdType.RectGradient, P1 = pMin, P2 = pMax, P3 = pMin, P4 = new Vector2( pMin.x, pMax.y ), C1 = ToColor( colUprLeft ), C2 = ToColor( colBotLeft ) } );
			return;
		}
		if ( horizontal )
		{
			Add( new Cmd { Type = CmdType.RectGradient, P1 = pMin, P2 = pMax, P3 = pMin, P4 = new Vector2( pMax.x, pMin.y ), C1 = ToColor( colUprLeft ), C2 = ToColor( colUprRight ) } );
			return;
		}

		// General bilinear case: approximate with horizontal-gradient strips.
		const int strips = 12;
		var ul = ToColor( colUprLeft ); var ur = ToColor( colUprRight );
		var bl = ToColor( colBotLeft ); var br = ToColor( colBotRight );
		float h = (pMax.y - pMin.y) / strips;
		for ( int i = 0; i < strips; i++ )
		{
			float t = (i + 0.5f) / strips;
			var left = Color.Lerp( ul, bl, t );
			var right = Color.Lerp( ur, br, t );
			var a = new Vector2( pMin.x, pMin.y + h * i );
			var b = new Vector2( pMax.x, i == strips - 1 ? pMax.y : pMin.y + h * (i + 1) + 0.5f );
			Add( new Cmd { Type = CmdType.RectGradient, P1 = a, P2 = b, P3 = a, P4 = new Vector2( pMax.x, a.y ), C1 = left, C2 = right } );
		}
	}

	public void AddQuad( Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Color32 col, float thickness = 1.0f )
	{
		if ( col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.Polyline, Points = new[] { p1, p2, p3, p4 }, Closed = true, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddQuadFilled( Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Color32 col )
	{
		if ( col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.PolygonFilled, Points = new[] { p1, p2, p3, p4 }, C1 = ToColor( col ) } );
	}

	public void AddTriangle( Vector2 p1, Vector2 p2, Vector2 p3, Color32 col, float thickness = 1.0f )
	{
		if ( col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.Polyline, Points = new[] { p1, p2, p3 }, Closed = true, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddTriangleFilled( Vector2 p1, Vector2 p2, Vector2 p3, Color32 col )
	{
		if ( col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.PolygonFilled, Points = new[] { p1, p2, p3 }, C1 = ToColor( col ) } );
	}

	public void AddCircle( Vector2 center, float radius, Color32 col, int numSegments = 0, float thickness = 1.0f )
	{
		if ( col.a == 0 || radius < 0.5f ) return;
		if ( numSegments > 2 )
		{
			AddNgon( center, radius, col, numSegments, thickness );
			return;
		}
		Add( new Cmd { Type = CmdType.Circle, P1 = center, Rounding = radius, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddCircleFilled( Vector2 center, float radius, Color32 col, int numSegments = 0 )
	{
		if ( col.a == 0 || radius < 0.5f ) return;
		if ( numSegments > 2 )
		{
			AddNgonFilled( center, radius, col, numSegments );
			return;
		}
		Add( new Cmd { Type = CmdType.CircleFilled, P1 = center, Rounding = radius, C1 = ToColor( col ) } );
	}

	public void AddNgon( Vector2 center, float radius, Color32 col, int numSegments, float thickness = 1.0f )
	{
		if ( col.a == 0 || numSegments <= 2 ) return;
		var pts = NgonPoints( center, radius - 0.5f, numSegments );
		Add( new Cmd { Type = CmdType.Polyline, Points = pts, Closed = true, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddNgonFilled( Vector2 center, float radius, Color32 col, int numSegments )
	{
		if ( col.a == 0 || numSegments <= 2 ) return;
		var pts = NgonPoints( center, radius, numSegments );
		Add( new Cmd { Type = CmdType.PolygonFilled, Points = pts, C1 = ToColor( col ) } );
	}

	private static Vector2[] NgonPoints( Vector2 center, float radius, int numSegments )
	{
		var pts = new Vector2[numSegments];
		for ( int i = 0; i < numSegments; i++ )
		{
			float a = MathF.PI * 2.0f * i / numSegments;
			pts[i] = new Vector2( center.x + MathF.Cos( a ) * radius, center.y + MathF.Sin( a ) * radius );
		}
		return pts;
	}

	public void AddEllipse( Vector2 center, Vector2 radius, Color32 col, float rot = 0.0f, int numSegments = 0, float thickness = 1.0f )
	{
		if ( col.a == 0 ) return;
		if ( rot == 0f && numSegments <= 0 )
		{
			Add( new Cmd { Type = CmdType.Ellipse, P1 = center, P2 = radius, C1 = ToColor( col ), Thickness = thickness } );
			return;
		}
		Add( new Cmd { Type = CmdType.Polyline, Points = EllipsePoints( center, radius, rot, numSegments ), Closed = true, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddEllipseFilled( Vector2 center, Vector2 radius, Color32 col, float rot = 0.0f, int numSegments = 0 )
	{
		if ( col.a == 0 ) return;
		if ( rot == 0f && numSegments <= 0 )
		{
			Add( new Cmd { Type = CmdType.EllipseFilled, P1 = center, P2 = radius, C1 = ToColor( col ) } );
			return;
		}
		Add( new Cmd { Type = CmdType.PolygonFilled, Points = EllipsePoints( center, radius, rot, numSegments ), C1 = ToColor( col ) } );
	}

	private static Vector2[] EllipsePoints( Vector2 center, Vector2 radius, float rot, int numSegments )
	{
		if ( numSegments <= 0 ) numSegments = 48;
		var pts = new Vector2[numSegments];
		float cos = MathF.Cos( rot ), sin = MathF.Sin( rot );
		for ( int i = 0; i < numSegments; i++ )
		{
			float a = MathF.PI * 2f * i / numSegments;
			float x = MathF.Cos( a ) * radius.x;
			float y = MathF.Sin( a ) * radius.y;
			pts[i] = new Vector2( center.x + x * cos - y * sin, center.y + x * sin + y * cos );
		}
		return pts;
	}

	public void AddBezierCubic( Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Color32 col, float thickness, int numSegments = 0 )
	{
		if ( col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.BezierCubic, P1 = p1, P2 = p2, P3 = p3, P4 = p4, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddBezierQuadratic( Vector2 p1, Vector2 p2, Vector2 p3, Color32 col, float thickness, int numSegments = 0 )
	{
		if ( col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.BezierQuadratic, P1 = p1, P2 = p2, P3 = p3, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddPolyline( ReadOnlySpan<Vector2> points, Color32 col, ImDrawFlags flags, float thickness )
	{
		if ( col.a == 0 || points.Length < 2 ) return;
		Add( new Cmd { Type = CmdType.Polyline, Points = points.ToArray(), Closed = (flags & ImDrawFlags.Closed) != 0, C1 = ToColor( col ), Thickness = thickness } );
	}

	public void AddPolyline( Vector2[] points, int numPoints, Color32 col, ImDrawFlags flags, float thickness )
		=> AddPolyline( points.AsSpan( 0, numPoints ), col, flags, thickness );

	public void AddConvexPolyFilled( ReadOnlySpan<Vector2> points, Color32 col )
	{
		if ( col.a == 0 || points.Length < 3 ) return;
		Add( new Cmd { Type = CmdType.PolygonFilled, Points = points.ToArray(), C1 = ToColor( col ) } );
	}

	public void AddConvexPolyFilled( Vector2[] points, int numPoints, Color32 col )
		=> AddConvexPolyFilled( points.AsSpan( 0, numPoints ), col );

	/// <summary>Concave polygons are supported by the backend (even-odd fill rule).</summary>
	public void AddConcavePolyFilled( ReadOnlySpan<Vector2> points, Color32 col ) => AddConvexPolyFilled( points, col );
	#endregion

	#region Text
	public void AddText( Vector2 pos, Color32 col, string text )
		=> AddText( 0f, pos, col, text );

	/// <summary>
	/// Add text with an explicit font size (0 = current font size). Supports '\n' and optional word wrapping.
	/// </summary>
	public void AddText( float fontSize, Vector2 pos, Color32 col, string text, float wrapWidth = 0.0f, ImRect? cpuFineClipRect = null )
	{
		if ( col.a == 0 || string.IsNullOrEmpty( text ) ) return;

		var g = ImGuiContext.Current;
		float baseLine = g?.FontSize ?? 16f;
		float basePoint = g?.FontPointSize ?? 16f;
		float lineHeight = fontSize > 0f ? fontSize : baseLine;
		float pointSize = basePoint * (lineHeight / baseLine);

		if ( cpuFineClipRect.HasValue )
		{
			var r = cpuFineClipRect.Value;
			PushClipRect( r.Min, r.Max, true );
		}

		var color = ToColor( col );
		float y = pos.y;
		if ( wrapWidth > 0f )
		{
			foreach ( var line in ImGui.WrapText( text, wrapWidth, pointSize ) )
			{
				AddTextLine( new Vector2( pos.x, y ), color, line, pointSize, lineHeight );
				y += lineHeight;
			}
		}
		else
		{
			int start = 0;
			while ( start <= text.Length )
			{
				int nl = text.IndexOf( '\n', start );
				int end = nl < 0 ? text.Length : nl;
				if ( end > start )
					AddTextLine( new Vector2( pos.x, y ), color, text.Substring( start, end - start ), pointSize, lineHeight );
				if ( nl < 0 ) break;
				start = nl + 1;
				y += lineHeight;
			}
		}

		if ( cpuFineClipRect.HasValue )
			PopClipRect();
	}

	private void AddTextLine( Vector2 pos, Color color, string line, float pointSize, float lineHeight )
	{
		if ( string.IsNullOrEmpty( line ) ) return;
		// Cull lines that are entirely outside of the clip rect.
		var clip = CurrentClipRect;
		if ( pos.y > clip.Max.y || pos.y + lineHeight < clip.Min.y ) return;
		Add( new Cmd { Type = CmdType.Text, P1 = pos, C1 = color, Text = line, FontSize = pointSize } );
	}
	#endregion

	#region Image
	public void AddImage( Texture texture, Vector2 pMin, Vector2 pMax )
		=> AddImage( texture, pMin, pMax, Vector2.Zero, Vector2.One, new Color32( 255, 255, 255, 255 ) );

	public void AddImage( Texture texture, Vector2 pMin, Vector2 pMax, Vector2 uvMin, Vector2 uvMax, Color32 col )
	{
		if ( texture is null || col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.Image, Texture = texture, P1 = pMin, P2 = pMax, P3 = uvMin, P4 = uvMax, C1 = ToColor( col ) } );
	}

	public void AddImageRounded( Texture texture, Vector2 pMin, Vector2 pMax, Vector2 uvMin, Vector2 uvMax, Color32 col, float rounding, ImDrawFlags flags = ImDrawFlags.None )
	{
		if ( texture is null || col.a == 0 ) return;
		Add( new Cmd { Type = CmdType.Image, Texture = texture, P1 = pMin, P2 = pMax, P3 = uvMin, P4 = uvMax, C1 = ToColor( col ), Rounding = rounding, Flags = flags } );
	}
	#endregion

	#region Path API
	public void PathClear() => _path.Clear();
	public void PathLineTo( Vector2 pos ) => _path.Add( pos );

	public void PathLineToMergeDuplicate( Vector2 pos )
	{
		if ( _path.Count == 0 || _path[^1] != pos )
			_path.Add( pos );
	}

	public void PathFillConvex( Color32 col )
	{
		AddConvexPolyFilled( _path.ToArray(), col );
		_path.Clear();
	}

	public void PathFillConcave( Color32 col ) => PathFillConvex( col );

	public void PathStroke( Color32 col, ImDrawFlags flags = ImDrawFlags.None, float thickness = 1.0f )
	{
		AddPolyline( _path.ToArray(), col, flags, thickness );
		_path.Clear();
	}

	public void PathArcTo( Vector2 center, float radius, float aMin, float aMax, int numSegments = 0 )
	{
		if ( radius < 0.5f )
		{
			_path.Add( center );
			return;
		}
		if ( numSegments <= 0 )
			numSegments = Math.Clamp( (int)(MathF.Abs( aMax - aMin ) * radius / 4f), 4, 64 );
		for ( int i = 0; i <= numSegments; i++ )
		{
			float a = aMin + (i / (float)numSegments) * (aMax - aMin);
			_path.Add( new Vector2( center.x + MathF.Cos( a ) * radius, center.y + MathF.Sin( a ) * radius ) );
		}
	}

	/// <summary>Use precomputed angles for a 12 steps circle. aMinOf12/aMaxOf12 are in [0,12].</summary>
	public void PathArcToFast( Vector2 center, float radius, int aMinOf12, int aMaxOf12 )
	{
		PathArcTo( center, radius, aMinOf12 * MathF.PI * 2f / 12f, aMaxOf12 * MathF.PI * 2f / 12f, Math.Max( 1, aMaxOf12 - aMinOf12 ) * 2 );
	}

	public void PathEllipticalArcTo( Vector2 center, Vector2 radius, float rot, float aMin, float aMax, int numSegments = 0 )
	{
		if ( numSegments <= 0 ) numSegments = 24;
		float cos = MathF.Cos( rot ), sin = MathF.Sin( rot );
		for ( int i = 0; i <= numSegments; i++ )
		{
			float a = aMin + (i / (float)numSegments) * (aMax - aMin);
			float x = MathF.Cos( a ) * radius.x, y = MathF.Sin( a ) * radius.y;
			_path.Add( new Vector2( center.x + x * cos - y * sin, center.y + x * sin + y * cos ) );
		}
	}

	public void PathBezierCubicCurveTo( Vector2 p2, Vector2 p3, Vector2 p4, int numSegments = 0 )
	{
		var p1 = _path.Count > 0 ? _path[^1] : p2;
		if ( numSegments <= 0 ) numSegments = 24;
		for ( int i = 1; i <= numSegments; i++ )
		{
			float t = i / (float)numSegments;
			float u = 1 - t;
			float w1 = u * u * u, w2 = 3 * u * u * t, w3 = 3 * u * t * t, w4 = t * t * t;
			_path.Add( p1 * w1 + p2 * w2 + p3 * w3 + p4 * w4 );
		}
	}

	public void PathBezierQuadraticCurveTo( Vector2 p2, Vector2 p3, int numSegments = 0 )
	{
		var p1 = _path.Count > 0 ? _path[^1] : p2;
		if ( numSegments <= 0 ) numSegments = 16;
		for ( int i = 1; i <= numSegments; i++ )
		{
			float t = i / (float)numSegments;
			float u = 1 - t;
			_path.Add( p1 * (u * u) + p2 * (2 * u * t) + p3 * (t * t) );
		}
	}

	public void PathRect( Vector2 rectMin, Vector2 rectMax, float rounding = 0.0f, ImDrawFlags flags = ImDrawFlags.None )
	{
		if ( rounding < 0.5f )
		{
			_path.Add( rectMin );
			_path.Add( new Vector2( rectMax.x, rectMin.y ) );
			_path.Add( rectMax );
			_path.Add( new Vector2( rectMin.x, rectMax.y ) );
			return;
		}
		var r = Radii( flags, rounding, rectMax - rectMin );
		PathArcToFast( new Vector2( rectMin.x + r.x, rectMin.y + r.x ), r.x, 6, 9 );
		PathArcToFast( new Vector2( rectMax.x - r.y, rectMin.y + r.y ), r.y, 9, 12 );
		PathArcToFast( new Vector2( rectMax.x - r.z, rectMax.y - r.z ), r.z, 0, 3 );
		PathArcToFast( new Vector2( rectMin.x + r.w, rectMax.y - r.w ), r.w, 3, 6 );
	}
	#endregion

	#region Rendering
	/// <summary>Returns the corner radii (TL, TR, BR, BL) for a rounding amount and draw flags.</summary>
	private static Vector4 Radii( ImDrawFlags flags, float rounding, Vector2 size )
	{
		rounding = MathF.Min( rounding, MathF.Min( MathF.Abs( size.x ), MathF.Abs( size.y ) ) * 0.5f );
		if ( rounding <= 0f || (flags & ImDrawFlags.RoundCornersMask_) == ImDrawFlags.RoundCornersNone )
			return Vector4.Zero;
		if ( (flags & ImDrawFlags.RoundCornersMask_) == 0 )
			flags |= ImDrawFlags.RoundCornersAll;
		return new Vector4(
			(flags & ImDrawFlags.RoundCornersTopLeft) != 0 ? rounding : 0f,
			(flags & ImDrawFlags.RoundCornersTopRight) != 0 ? rounding : 0f,
			(flags & ImDrawFlags.RoundCornersBottomRight) != 0 ? rounding : 0f,
			(flags & ImDrawFlags.RoundCornersBottomLeft) != 0 ? rounding : 0f );
	}

	private static Painter.CornerRadii CornerRadii( ImDrawFlags flags, float rounding, Vector2 size )
	{
		var r = Radii( flags, rounding, size );
		return new Painter.CornerRadii( new Vector2( r.x ), new Vector2( r.y ), new Vector2( r.z ), new Vector2( r.w ) );
	}

	/// <summary>
	/// Replay all recorded commands into a painter.
	/// </summary>
	internal void Render( Painter painter )
	{
		ChannelsMerge();
		if ( _cmds.Count == 0 )
			return;

		int currentClip = -1;
		Painter.StateScope scope = default;
		bool hasScope = false;

		try
		{
			foreach ( var cmd in _cmds )
			{
				if ( cmd.ClipIdx != currentClip )
				{
					if ( hasScope ) scope.Dispose();
					scope = painter.Scope();
					hasScope = true;
					currentClip = cmd.ClipIdx;
					if ( currentClip != 0 )
						painter.Clip( ClipRects[currentClip].ToRect() );
				}

				var cr = ClipRects[currentClip];
				if ( cr.Width <= 0f || cr.Height <= 0f )
					continue;

				RenderCmd( painter, cmd );
			}
		}
		finally
		{
			if ( hasScope ) scope.Dispose();
		}
	}

	private static void RenderCmd( Painter painter, in Cmd cmd )
	{
		switch ( cmd.Type )
		{
			case CmdType.RectFilled:
				painter.Stroke = Stroke.None;
				painter.Fill = cmd.C1;
				painter.Rect( new Rect( cmd.P1, cmd.P2 - cmd.P1 ), CornerRadii( cmd.Flags, cmd.Rounding, cmd.P2 - cmd.P1 ) );
				break;
			case CmdType.Rect:
				painter.Fill = Fill.None;
				painter.Stroke = Stroke.Solid( cmd.C1, cmd.Thickness ).WithAlignment( Stroke.StrokeAlignment.Inside );
				painter.Rect( new Rect( cmd.P1, cmd.P2 - cmd.P1 ), CornerRadii( cmd.Flags, cmd.Rounding, cmd.P2 - cmd.P1 ) );
				break;
			case CmdType.RectGradient:
				painter.Stroke = Stroke.None;
				painter.Fill = Fill.LinearGradient( cmd.P3, cmd.P4, cmd.C1, cmd.C2 );
				painter.Rect( new Rect( cmd.P1, cmd.P2 - cmd.P1 ), 0f );
				break;
			case CmdType.Line:
				painter.Fill = Fill.None;
				painter.Stroke = Stroke.Solid( cmd.C1, cmd.Thickness );
				painter.Line( cmd.P1, cmd.P2 );
				break;
			case CmdType.Polyline:
				painter.Fill = Fill.None;
				painter.Stroke = Stroke.Solid( cmd.C1, cmd.Thickness );
				if ( cmd.Closed && cmd.Points.Length >= 3 )
					painter.Polygon( cmd.Points );
				else
					painter.Line( cmd.Points );
				break;
			case CmdType.PolygonFilled:
				painter.Stroke = Stroke.None;
				painter.Fill = cmd.C1;
				painter.Polygon( cmd.Points );
				break;
			case CmdType.Circle:
				painter.Fill = Fill.None;
				painter.Stroke = Stroke.Solid( cmd.C1, cmd.Thickness );
				painter.Circle( cmd.P1, cmd.Rounding - cmd.Thickness * 0.5f );
				break;
			case CmdType.CircleFilled:
				painter.Stroke = Stroke.None;
				painter.Fill = cmd.C1;
				painter.Circle( cmd.P1, cmd.Rounding );
				break;
			case CmdType.Ellipse:
				painter.Fill = Fill.None;
				painter.Stroke = Stroke.Solid( cmd.C1, cmd.Thickness );
				painter.Circle( cmd.P1, cmd.P2 * 2f );
				break;
			case CmdType.EllipseFilled:
				painter.Stroke = Stroke.None;
				painter.Fill = cmd.C1;
				painter.Circle( cmd.P1, cmd.P2 * 2f );
				break;
			case CmdType.BezierCubic:
				painter.Fill = Fill.None;
				painter.Stroke = Stroke.Solid( cmd.C1, cmd.Thickness );
				painter.Bezier( cmd.P1, cmd.P2, cmd.P3, cmd.P4 );
				break;
			case CmdType.BezierQuadratic:
				painter.Fill = Fill.None;
				painter.Stroke = Stroke.Solid( cmd.C1, cmd.Thickness );
				painter.Bezier( cmd.P1, cmd.P2, cmd.P3 );
				break;
			case CmdType.Text:
				{
					var g = ImGuiContext.Current;
					var font = g?.FontName ?? "Roboto Mono";
					var weight = g?.FontWeight ?? 400;
					painter.TextStyle = new TextStyle( font, cmd.FontSize, cmd.C1 ) { FontWeight = weight };
					painter.Text( cmd.Text, new Rect( cmd.P1, new Vector2( 8096f, 8096f ) ) );
					break;
				}
			case CmdType.Image:
				RenderImage( painter, cmd );
				break;
		}
	}

	private static void RenderImage( Painter painter, in Cmd cmd )
	{
		if ( !cmd.Texture.IsValid() )
			return;

		var pMin = cmd.P1;
		var pMax = cmd.P2;
		var uvMin = cmd.P3;
		var uvMax = cmd.P4;
		var target = new Rect( pMin, pMax - pMin );
		var uvSize = uvMax - uvMin;
		bool fullUv = uvMin == Vector2.Zero && uvMax == Vector2.One;
		bool rounded = cmd.Rounding > 0f;

		if ( fullUv && !rounded )
		{
			painter.Texture( cmd.Texture, target, cmd.C1 );
			return;
		}

		// Sub-rectangle of the texture: draw the whole texture scaled up and clip to the target.
		using var scope = painter.Scope();
		painter.Clip( target, rounded ? CornerRadii( cmd.Flags, cmd.Rounding, pMax - pMin ) : default );
		if ( MathF.Abs( uvSize.x ) < 1e-6f || MathF.Abs( uvSize.y ) < 1e-6f )
			return;
		var fullSize = new Vector2( (pMax.x - pMin.x) / uvSize.x, (pMax.y - pMin.y) / uvSize.y );
		var fullMin = new Vector2( pMin.x - uvMin.x * fullSize.x, pMin.y - uvMin.y * fullSize.y );
		painter.Texture( cmd.Texture, new Rect( fullMin, fullSize ), cmd.C1 );
	}
	#endregion
}

#endregion

#region ImGuiOldColumnFlags

[Flags]
public enum ImGuiOldColumnFlags
{
	None = 0,
	NoBorder = 1 << 0,
	NoResize = 1 << 1,
	NoPreserveWidths = 1 << 2,
	NoForceWithinWindow = 1 << 3,
	GrowParentContentsSize = 1 << 4,
}

#endregion

#region ImGuiPayload

/// <summary>
/// Data payload for drag and drop operations: <see cref="ImGui.AcceptDragDropPayload"/>, <see cref="ImGui.GetDragDropPayload"/>.
/// Unlike Dear ImGui, the payload holds a managed object reference instead of a copied byte buffer.
/// </summary>
public class ImGuiPayload
{
	/// <summary>The payload data, as passed to SetDragDropPayload().</summary>
	public object Data;
	/// <summary>Data type tag (short user-supplied string, 32 characters max in Dear ImGui).</summary>
	public string DataType;
	/// <summary>Source item id.</summary>
	public int SourceId;
	/// <summary>Source parent id (if available).</summary>
	public int SourceParentId;
	/// <summary>Data timestamp: the frame the payload was last set.</summary>
	public int DataFrameCount = -1;
	/// <summary>Set when AcceptDragDropPayload() was called and mouse has been hovering the target item (nb: handle overlapping drag targets).</summary>
	public bool Preview;
	/// <summary>Set when AcceptDragDropPayload() was called and mouse button is released over the target item.</summary>
	public bool Delivery;

	public void Clear()
	{
		Data = null;
		DataType = null;
		SourceId = SourceParentId = 0;
		DataFrameCount = -1;
		Preview = Delivery = false;
	}

	public bool IsDataType( string type ) => DataFrameCount != -1 && DataType == type;
	public bool IsPreview() => Preview;
	public bool IsDelivery() => Delivery;

	/// <summary>Get the payload data cast to a type, or default if it is not of that type.</summary>
	public T GetData<T>() => Data is T t ? t : default;
}

#endregion

#region ImGuiInputTextCallback

/// <summary>Callback for InputText() with the ImGuiInputTextFlags.Callback* flags. Return non-zero from a CharFilter callback to discard the character.</summary>
public delegate int ImGuiInputTextCallback( ImGuiInputTextCallbackData data );

#endregion

#region ImGuiInputTextCallbackData

/// <summary>
/// Shared state passed to an <see cref="ImGuiInputTextCallback"/>. Edit <see cref="Buf"/> via InsertChars/DeleteChars (or assign it and set BufDirty).
/// </summary>
public class ImGuiInputTextCallbackData
{
	/// <summary>One of ImGuiInputTextFlags.Callback* - the event that triggered this callback.</summary>
	public ImGuiInputTextFlags EventFlag;
	/// <summary>The flags passed to InputText().</summary>
	public ImGuiInputTextFlags Flags;
	public object UserData;

	/// <summary>CharFilter: character input. Replace it with another character, or set to 0 to discard.</summary>
	public char EventChar;
	/// <summary>Completion/History: key pressed (Tab, UpArrow or DownArrow).</summary>
	public ImGuiKey EventKey;

	private string _buf = string.Empty;
	/// <summary>Text buffer. If you replace it directly, set BufDirty = true.</summary>
	public string Buf
	{
		get => _buf;
		set
		{
			_buf = value ?? string.Empty;
			BufDirty = true;
		}
	}
	public int BufTextLen => _buf.Length;
	public int BufSize;
	public bool BufDirty;
	public int CursorPos;
	public int SelectionStart;
	public int SelectionEnd;

	internal void SetBufSilently( string text ) => _buf = text ?? string.Empty;

	public void DeleteChars( int pos, int bytesCount )
	{
		pos = Math.Clamp( pos, 0, _buf.Length );
		bytesCount = Math.Clamp( bytesCount, 0, _buf.Length - pos );
		if ( bytesCount == 0 ) return;
		_buf = _buf.Remove( pos, bytesCount );
		if ( CursorPos >= pos + bytesCount ) CursorPos -= bytesCount;
		else if ( CursorPos >= pos ) CursorPos = pos;
		SelectionStart = SelectionEnd = CursorPos;
		BufDirty = true;
	}

	public void InsertChars( int pos, string text )
	{
		if ( string.IsNullOrEmpty( text ) ) return;
		pos = Math.Clamp( pos, 0, _buf.Length );
		_buf = _buf.Insert( pos, text );
		if ( CursorPos >= pos ) CursorPos += text.Length;
		SelectionStart = SelectionEnd = CursorPos;
		BufDirty = true;
	}

	public void SelectAll()
	{
		SelectionStart = 0;
		CursorPos = SelectionEnd = _buf.Length;
	}

	public void ClearSelection() => SelectionStart = SelectionEnd = _buf.Length;
	public bool HasSelection => SelectionStart != SelectionEnd;
}

#endregion

#region ImGuiTableColumnSortSpecs

/// <summary>Sorting specification for one column of a table.</summary>
public class ImGuiTableColumnSortSpecs
{
	/// <summary>User id of the column (if specified by a TableSetupColumn() call).</summary>
	public int ColumnUserID;
	/// <summary>Index of the column.</summary>
	public int ColumnIndex;
	/// <summary>Index within parent ImGuiTableSortSpecs (always stored in order starting from 0, tables sorted on a single criteria will always have a 0 here).</summary>
	public int SortOrder;
	public ImGuiSortDirection SortDirection;
}

#endregion

#region ImGuiTableSortSpecs

/// <summary>
/// Sorting specifications for a table (often handling sort specs for a single column, occasionally more).
/// Obtained by calling TableGetSortSpecs(). When SpecsDirty is true you can sort your data, then set it back to false.
/// </summary>
public class ImGuiTableSortSpecs
{
	public ImGuiTableColumnSortSpecs[] Specs = Array.Empty<ImGuiTableColumnSortSpecs>();
	public int SpecsCount;
	/// <summary>Set to true when specs have changed since last time! Use this to sort again, then clear the flag.</summary>
	public bool SpecsDirty;
}

#endregion
