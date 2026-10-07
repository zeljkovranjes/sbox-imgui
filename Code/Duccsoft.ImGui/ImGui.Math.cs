namespace Duccsoft.ImGui;

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
