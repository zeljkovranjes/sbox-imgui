using Duccsoft.ImGui.Engine;

namespace Duccsoft.ImGui;

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
