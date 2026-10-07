using System.Numerics;

namespace Duccsoft.ImGui;

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
