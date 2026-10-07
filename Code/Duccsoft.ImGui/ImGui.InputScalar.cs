using System.Numerics;

namespace Duccsoft.ImGui;

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
