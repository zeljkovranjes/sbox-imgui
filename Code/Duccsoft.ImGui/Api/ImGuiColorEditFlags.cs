namespace Duccsoft.ImGui;

[Flags]
public enum ImGuiColorEditFlags
{
	None = 0,
	NoAlpha = 1 << 1,
	NoPicker = 1 << 2,
	NoOptions = 1 << 3,
	NoSmallPreview = 1 << 4,
	NoInputs = 1 << 5,
	NoTooltip = 1 << 6,
	NoLabel = 1 << 7,
	NoSidePreview = 1 << 8,
	NoDragDrop = 1 << 9,
	NoBorder = 1 << 10,
	AlphaOpaque = 1 << 11,
	AlphaNoBg = 1 << 12,
	AlphaPreviewHalf = 1 << 13,
	AlphaBar = 1 << 16,
	HDR = 1 << 19,
	DisplayRGB = 1 << 20,
	DisplayHSV = 1 << 21,
	DisplayHex = 1 << 22,
	Uint8 = 1 << 23,
	Float = 1 << 24,
	PickerHueBar = 1 << 25,
	PickerHueWheel = 1 << 26,
	InputRGB = 1 << 27,
	InputHSV = 1 << 28,
	[Obsolete( "Removed in Dear ImGui 1.91.8" )] AlphaPreview = 0,

	DefaultOptions_ = Uint8 | DisplayRGB | InputRGB | PickerHueBar,
	AlphaMask_ = NoAlpha | AlphaOpaque | AlphaNoBg | AlphaPreviewHalf,
	DisplayMask_ = DisplayRGB | DisplayHSV | DisplayHex,
	DataTypeMask_ = Uint8 | Float,
	PickerMask_ = PickerHueWheel | PickerHueBar,
	InputMask_ = InputRGB | InputHSV,
}
