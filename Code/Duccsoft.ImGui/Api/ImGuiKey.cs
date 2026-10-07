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
