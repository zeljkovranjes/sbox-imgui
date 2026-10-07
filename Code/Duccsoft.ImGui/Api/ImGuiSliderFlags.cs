namespace Duccsoft.ImGui;

[Flags]
public enum ImGuiSliderFlags
{
	None = 0,
	Logarithmic = 1 << 5,
	NoRoundToFormat = 1 << 6,
	NoInput = 1 << 7,
	WrapAround = 1 << 8,
	ClampOnInput = 1 << 9,
	ClampZeroRange = 1 << 10,
	NoSpeedTweaks = 1 << 11,
	AlwaysClamp = ClampOnInput | ClampZeroRange,
	// Internal
	Vertical = 1 << 20,
	ReadOnly = 1 << 21,
}
