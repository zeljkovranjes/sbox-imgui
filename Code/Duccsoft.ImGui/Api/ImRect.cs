namespace Duccsoft.ImGui;

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
