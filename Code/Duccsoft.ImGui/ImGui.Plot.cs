namespace Duccsoft.ImGui;

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
