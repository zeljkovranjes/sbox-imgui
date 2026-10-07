using System.Globalization;
using System.Numerics;
using System.Text;

namespace Duccsoft.ImGui;

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
