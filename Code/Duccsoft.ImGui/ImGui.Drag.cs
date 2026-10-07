using System.Numerics;

namespace Duccsoft.ImGui;

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
