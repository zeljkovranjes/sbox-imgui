namespace Duccsoft.ImGui;

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

internal static class ImGuiKeyNames
{
	/// <summary>
	/// Maps an <see cref="ImGuiKey"/> to the s&amp;box key name used by <c>Input.Keyboard</c> and UI button events.
	/// </summary>
	public static string ToSboxName( ImGuiKey key )
	{
		if ( key >= ImGuiKey.A && key <= ImGuiKey.Z )
			return ((char)('a' + (key - ImGuiKey.A))).ToString();
		if ( key >= ImGuiKey._0 && key <= ImGuiKey._9 )
			return ((char)('0' + (key - ImGuiKey._0))).ToString();
		if ( key >= ImGuiKey.F1 && key <= ImGuiKey.F12 )
			return "f" + (1 + (key - ImGuiKey.F1));
		if ( key >= ImGuiKey.Keypad0 && key <= ImGuiKey.Keypad9 )
			return "pad_" + (key - ImGuiKey.Keypad0);

		return key switch
		{
			ImGuiKey.Tab => "tab",
			ImGuiKey.LeftArrow => "left",
			ImGuiKey.RightArrow => "right",
			ImGuiKey.UpArrow => "up",
			ImGuiKey.DownArrow => "down",
			ImGuiKey.PageUp => "pgup",
			ImGuiKey.PageDown => "pgdn",
			ImGuiKey.Home => "home",
			ImGuiKey.End => "end",
			ImGuiKey.Insert => "ins",
			ImGuiKey.Delete => "delete",
			ImGuiKey.Backspace => "backspace",
			ImGuiKey.Space => "space",
			ImGuiKey.Enter => "enter",
			ImGuiKey.Escape => "escape",
			ImGuiKey.LeftCtrl => "lctrl",
			ImGuiKey.LeftShift => "shift",
			ImGuiKey.LeftAlt => "alt",
			ImGuiKey.LeftSuper => "lwin",
			ImGuiKey.RightCtrl => "rctrl",
			ImGuiKey.RightShift => "rshift",
			ImGuiKey.RightAlt => "ralt",
			ImGuiKey.RightSuper => "rwin",
			ImGuiKey.Menu => "app",
			ImGuiKey.Apostrophe => "'",
			ImGuiKey.Comma => ",",
			ImGuiKey.Minus => "-",
			ImGuiKey.Period => ".",
			ImGuiKey.Slash => "/",
			ImGuiKey.Semicolon => "semicolon",
			ImGuiKey.Equal => "=",
			ImGuiKey.LeftBracket => "[",
			ImGuiKey.Backslash => "\\",
			ImGuiKey.RightBracket => "]",
			ImGuiKey.GraveAccent => "`",
			ImGuiKey.CapsLock => "capslock",
			ImGuiKey.ScrollLock => "scrolllock",
			ImGuiKey.NumLock => "numlock",
			ImGuiKey.Pause => "break",
			ImGuiKey.KeypadDecimal => "pad_decimal",
			ImGuiKey.KeypadDivide => "pad_divide",
			ImGuiKey.KeypadMultiply => "pad_multiply",
			ImGuiKey.KeypadSubtract => "pad_minus",
			ImGuiKey.KeypadAdd => "pad_plus",
			ImGuiKey.KeypadEnter => "pad_enter",
			ImGuiKey.MouseLeft => "mouseleft",
			ImGuiKey.MouseRight => "mouseright",
			ImGuiKey.MouseMiddle => "mousemiddle",
			ImGuiKey.MouseX1 => "mouse4",
			ImGuiKey.MouseX2 => "mouse5",
			_ => null
		};
	}

	/// <summary>
	/// Maps an s&amp;box UI button name to an <see cref="ImGuiKey"/>.
	/// </summary>
	public static ImGuiKey FromSboxName( string name )
	{
		if ( string.IsNullOrEmpty( name ) )
			return ImGuiKey.None;

		name = name.ToLowerInvariant();
		if ( name.Length == 1 )
		{
			var c = name[0];
			if ( c >= 'a' && c <= 'z' ) return ImGuiKey.A + (c - 'a');
			if ( c >= '0' && c <= '9' ) return ImGuiKey._0 + (c - '0');
		}
		if ( name.Length >= 2 && name[0] == 'f' && int.TryParse( name.AsSpan( 1 ), out var fn ) && fn >= 1 && fn <= 12 )
			return ImGuiKey.F1 + (fn - 1);

		return name switch
		{
			"tab" => ImGuiKey.Tab,
			"left" => ImGuiKey.LeftArrow,
			"right" => ImGuiKey.RightArrow,
			"up" => ImGuiKey.UpArrow,
			"down" => ImGuiKey.DownArrow,
			"pgup" or "pageup" => ImGuiKey.PageUp,
			"pgdn" or "pagedown" => ImGuiKey.PageDown,
			"home" => ImGuiKey.Home,
			"end" => ImGuiKey.End,
			"ins" or "insert" => ImGuiKey.Insert,
			"delete" or "del" => ImGuiKey.Delete,
			"backspace" => ImGuiKey.Backspace,
			"space" => ImGuiKey.Space,
			"enter" or "return" => ImGuiKey.Enter,
			"pad_enter" => ImGuiKey.KeypadEnter,
			"escape" or "esc" => ImGuiKey.Escape,
			"ctrl" or "lctrl" => ImGuiKey.LeftCtrl,
			"rctrl" => ImGuiKey.RightCtrl,
			"shift" or "lshift" => ImGuiKey.LeftShift,
			"rshift" => ImGuiKey.RightShift,
			"alt" or "lalt" => ImGuiKey.LeftAlt,
			"ralt" => ImGuiKey.RightAlt,
			_ => ImGuiKey.None
		};
	}
}
