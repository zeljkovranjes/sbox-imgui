namespace Duccsoft.ImGui;

[Flags]
public enum ImGuiItemFlags
{
	None = 0,
	NoTabStop = 1 << 0,
	NoNav = 1 << 1,
	NoNavDefaultFocus = 1 << 2,
	ButtonRepeat = 1 << 3,
	AutoClosePopups = 1 << 4,
	AllowDuplicateId = 1 << 5,

	// Internal
	Disabled = 1 << 10,
	ReadOnly = 1 << 11,
	MixedValue = 1 << 12,
	NoWindowHoverableCheck = 1 << 13,
	AllowOverlap = 1 << 14,
	Inputable = 1 << 20,
}
