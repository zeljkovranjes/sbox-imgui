using Duccsoft.ImGui.Engine;

namespace Duccsoft.ImGui;

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
