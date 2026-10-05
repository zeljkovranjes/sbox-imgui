namespace Duccsoft.ImGui;

[Flags]
public enum ImGuiWindowFlags
{
	None = 0,
	NoTitleBar = 1 << 0,
	NoResize = 1 << 1,
	NoMove = 1 << 2,
	NoScrollbar = 1 << 3,
	NoScrollWithMouse = 1 << 4,
	NoCollapse = 1 << 5,
	AlwaysAutoResize = 1 << 6,
	NoBackground = 1 << 7,
	NoSavedSettings = 1 << 8,
	NoMouseInputs = 1 << 9,
	MenuBar = 1 << 10,
	HorizontalScrollbar = 1 << 11,
	NoFocusOnAppearing = 1 << 12,
	NoBringToFrontOnFocus = 1 << 13,
	AlwaysVerticalScrollbar = 1 << 14,
	AlwaysHorizontalScrollbar = 1 << 15,
	NoNavInputs = 1 << 16,
	NoNavFocus = 1 << 17,
	UnsavedDocument = 1 << 18,
	NoNav = NoNavInputs | NoNavFocus,
	NoDecoration = NoTitleBar | NoResize | NoScrollbar | NoCollapse,
	NoInputs = NoMouseInputs | NoNavInputs | NoNavFocus,

	// Internal
	ChildWindow = 1 << 24,
	Tooltip = 1 << 25,
	Popup = 1 << 26,
	Modal = 1 << 27,
	ChildMenu = 1 << 28,
}

[Flags]
public enum ImGuiChildFlags
{
	None = 0,
	Borders = 1 << 0,
	AlwaysUseWindowPadding = 1 << 1,
	ResizeX = 1 << 2,
	ResizeY = 1 << 3,
	AutoResizeX = 1 << 4,
	AutoResizeY = 1 << 5,
	AlwaysAutoResize = 1 << 6,
	FrameStyle = 1 << 7,
	NavFlattened = 1 << 8,
	[Obsolete( "Use Borders" )] Border = Borders,
}

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

[Flags]
public enum ImGuiItemStatusFlags
{
	None = 0,
	HoveredRect = 1 << 0,
	HasDisplayRect = 1 << 1,
	Edited = 1 << 2,
	ToggledSelection = 1 << 3,
	ToggledOpen = 1 << 4,
	HasDeactivated = 1 << 5,
	Deactivated = 1 << 6,
	HoveredWindow = 1 << 7,
	Visible = 1 << 8,
	HasClipRect = 1 << 9,
	Openable = 1 << 20,
	Opened = 1 << 21,
	Checkable = 1 << 22,
	Checked = 1 << 23,
	Inputable = 1 << 24,
}

[Flags]
public enum ImGuiInputTextFlags
{
	None = 0,
	CharsDecimal = 1 << 0,
	CharsHexadecimal = 1 << 1,
	CharsScientific = 1 << 2,
	CharsUppercase = 1 << 3,
	CharsNoBlank = 1 << 4,
	AllowTabInput = 1 << 5,
	EnterReturnsTrue = 1 << 6,
	EscapeClearsAll = 1 << 7,
	CtrlEnterForNewLine = 1 << 8,
	ReadOnly = 1 << 9,
	Password = 1 << 10,
	AlwaysOverwrite = 1 << 11,
	AutoSelectAll = 1 << 12,
	ParseEmptyRefVal = 1 << 13,
	DisplayEmptyRefVal = 1 << 14,
	NoHorizontalScroll = 1 << 15,
	NoUndoRedo = 1 << 16,
	ElideLeft = 1 << 17,
	CallbackCompletion = 1 << 18,
	CallbackHistory = 1 << 19,
	CallbackAlways = 1 << 20,
	CallbackCharFilter = 1 << 21,
	CallbackResize = 1 << 22,
	CallbackEdit = 1 << 23,

	// Internal
	Multiline = 1 << 26,
	MergedItem = 1 << 27,
}

[Flags]
public enum ImGuiTreeNodeFlags
{
	None = 0,
	Selected = 1 << 0,
	Framed = 1 << 1,
	AllowOverlap = 1 << 2,
	NoTreePushOnOpen = 1 << 3,
	NoAutoOpenOnLog = 1 << 4,
	DefaultOpen = 1 << 5,
	OpenOnDoubleClick = 1 << 6,
	OpenOnArrow = 1 << 7,
	Leaf = 1 << 8,
	Bullet = 1 << 9,
	FramePadding = 1 << 10,
	SpanAvailWidth = 1 << 11,
	SpanFullWidth = 1 << 12,
	SpanTextWidth = 1 << 13,
	SpanAllColumns = 1 << 14,
	NavLeftJumpsBackHere = 1 << 15,
	CollapsingHeader = Framed | NoTreePushOnOpen | NoAutoOpenOnLog,

	// Internal
	ClipLabelForTrailingButton = 1 << 20,
	UpsideDownArrow = 1 << 21,
}

[Flags]
public enum ImGuiPopupFlags
{
	None = 0,
	MouseButtonLeft = 0,
	MouseButtonRight = 1,
	MouseButtonMiddle = 2,
	MouseButtonMask_ = 0x1F,
	MouseButtonDefault_ = 1,
	NoReopen = 1 << 5,
	NoOpenOverExistingPopup = 1 << 7,
	NoOpenOverItems = 1 << 8,
	AnyPopupId = 1 << 10,
	AnyPopupLevel = 1 << 11,
	AnyPopup = AnyPopupId | AnyPopupLevel,
}

[Flags]
public enum ImGuiSelectableFlags
{
	None = 0,
	NoAutoClosePopups = 1 << 0,
	SpanAllColumns = 1 << 1,
	AllowDoubleClick = 1 << 2,
	Disabled = 1 << 3,
	AllowOverlap = 1 << 4,
	Highlight = 1 << 5,
	[Obsolete( "Use NoAutoClosePopups" )] DontClosePopups = NoAutoClosePopups,

	// Internal
	NoHoldingActiveID = 1 << 20,
	SelectOnClick = 1 << 22,
	SelectOnRelease = 1 << 23,
	SpanAvailWidth = 1 << 24,
	SetNavIdOnHover = 1 << 25,
	NoPadWithHalfSpacing = 1 << 26,
}

[Flags]
public enum ImGuiComboFlags
{
	None = 0,
	PopupAlignLeft = 1 << 0,
	HeightSmall = 1 << 1,
	HeightRegular = 1 << 2,
	HeightLarge = 1 << 3,
	HeightLargest = 1 << 4,
	NoArrowButton = 1 << 5,
	NoPreview = 1 << 6,
	WidthFitPreview = 1 << 7,
	HeightMask_ = HeightSmall | HeightRegular | HeightLarge | HeightLargest,
}

[Flags]
public enum ImGuiTabBarFlags
{
	None = 0,
	Reorderable = 1 << 0,
	AutoSelectNewTabs = 1 << 1,
	TabListPopupButton = 1 << 2,
	NoCloseWithMiddleMouseButton = 1 << 3,
	NoTabListScrollingButtons = 1 << 4,
	NoTooltip = 1 << 5,
	DrawSelectedOverline = 1 << 6,
	FittingPolicyResizeDown = 1 << 7,
	FittingPolicyScroll = 1 << 8,
	FittingPolicyMask_ = FittingPolicyResizeDown | FittingPolicyScroll,
	FittingPolicyDefault_ = FittingPolicyResizeDown,
}

[Flags]
public enum ImGuiTabItemFlags
{
	None = 0,
	UnsavedDocument = 1 << 0,
	SetSelected = 1 << 1,
	NoCloseWithMiddleMouseButton = 1 << 2,
	NoPushId = 1 << 3,
	NoTooltip = 1 << 4,
	NoReorder = 1 << 5,
	Leading = 1 << 6,
	Trailing = 1 << 7,
	NoAssumedClosure = 1 << 8,
	// Internal
	Button = 1 << 21,
}

[Flags]
public enum ImGuiFocusedFlags
{
	None = 0,
	ChildWindows = 1 << 0,
	RootWindow = 1 << 1,
	AnyWindow = 1 << 2,
	NoPopupHierarchy = 1 << 3,
	DockHierarchy = 1 << 4,
	RootAndChildWindows = RootWindow | ChildWindows,
}

[Flags]
public enum ImGuiHoveredFlags
{
	None = 0,
	ChildWindows = 1 << 0,
	RootWindow = 1 << 1,
	AnyWindow = 1 << 2,
	NoPopupHierarchy = 1 << 3,
	DockHierarchy = 1 << 4,
	AllowWhenBlockedByPopup = 1 << 5,
	AllowWhenBlockedByActiveItem = 1 << 7,
	AllowWhenOverlappedByItem = 1 << 8,
	AllowWhenOverlappedByWindow = 1 << 9,
	AllowWhenDisabled = 1 << 10,
	NoNavOverride = 1 << 11,
	AllowWhenOverlapped = AllowWhenOverlappedByItem | AllowWhenOverlappedByWindow,
	RectOnly = AllowWhenBlockedByPopup | AllowWhenBlockedByActiveItem | AllowWhenOverlapped,
	RootAndChildWindows = RootWindow | ChildWindows,
	ForTooltip = 1 << 12,
	Stationary = 1 << 13,
	DelayNone = 1 << 14,
	DelayShort = 1 << 15,
	DelayNormal = 1 << 16,
	NoSharedDelay = 1 << 17,
}

[Flags]
public enum ImGuiDragDropFlags
{
	None = 0,
	SourceNoPreviewTooltip = 1 << 0,
	SourceNoDisableHover = 1 << 1,
	SourceNoHoldToOpenOthers = 1 << 2,
	SourceAllowNullID = 1 << 3,
	SourceExtern = 1 << 4,
	PayloadAutoExpire = 1 << 5,
	PayloadNoCrossContext = 1 << 6,
	PayloadNoCrossProcess = 1 << 7,
	AcceptBeforeDelivery = 1 << 10,
	AcceptNoDrawDefaultRect = 1 << 11,
	AcceptNoPreviewTooltip = 1 << 12,
	AcceptPeekOnly = AcceptBeforeDelivery | AcceptNoDrawDefaultRect,
}

public enum ImGuiDir
{
	None = -1,
	Left = 0,
	Right = 1,
	Up = 2,
	Down = 3,
}

public enum ImGuiSortDirection
{
	None = 0,
	Ascending = 1,
	Descending = 2,
}

public enum ImGuiMouseButton
{
	Left = 0,
	Right = 1,
	Middle = 2,
}

public enum ImGuiMouseCursor
{
	None = -1,
	Arrow = 0,
	TextInput,
	ResizeAll,
	ResizeNS,
	ResizeEW,
	ResizeNESW,
	ResizeNWSE,
	Hand,
	NotAllowed,
}

[Flags]
public enum ImGuiCond
{
	None = 0,
	Always = 1 << 0,
	Once = 1 << 1,
	FirstUseEver = 1 << 2,
	Appearing = 1 << 3,
}

[Flags]
public enum ImGuiButtonFlags
{
	None = 0,
	MouseButtonLeft = 1 << 0,
	MouseButtonRight = 1 << 1,
	MouseButtonMiddle = 1 << 2,
	MouseButtonMask_ = MouseButtonLeft | MouseButtonRight | MouseButtonMiddle,
	EnableNav = 1 << 3,

	// Internal
	PressedOnClick = 1 << 4,
	PressedOnClickRelease = 1 << 5,
	PressedOnClickReleaseAnywhere = 1 << 6,
	PressedOnRelease = 1 << 7,
	PressedOnDoubleClick = 1 << 8,
	PressedOnDragDropHold = 1 << 9,
	FlattenChildren = 1 << 11,
	AllowOverlap = 1 << 12,
	AlignTextBaseLine = 1 << 15,
	NoKeyModsAllowed = 1 << 16,
	NoHoldingActiveId = 1 << 17,
	NoNavFocus = 1 << 18,
	NoHoveredOnFocus = 1 << 19,
	NoSetKeyOwner = 1 << 20,
	NoTestKeyOwner = 1 << 21,
	PressedOnMask_ = PressedOnClick | PressedOnClickRelease | PressedOnClickReleaseAnywhere | PressedOnRelease | PressedOnDoubleClick | PressedOnDragDropHold,
	PressedOnDefault_ = PressedOnClickRelease,
}

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

[Flags]
public enum ImGuiTableFlags
{
	None = 0,
	Resizable = 1 << 0,
	Reorderable = 1 << 1,
	Hideable = 1 << 2,
	Sortable = 1 << 3,
	NoSavedSettings = 1 << 4,
	ContextMenuInBody = 1 << 5,
	RowBg = 1 << 6,
	BordersInnerH = 1 << 7,
	BordersOuterH = 1 << 8,
	BordersInnerV = 1 << 9,
	BordersOuterV = 1 << 10,
	BordersH = BordersInnerH | BordersOuterH,
	BordersV = BordersInnerV | BordersOuterV,
	BordersInner = BordersInnerV | BordersInnerH,
	BordersOuter = BordersOuterV | BordersOuterH,
	Borders = BordersInner | BordersOuter,
	NoBordersInBody = 1 << 11,
	NoBordersInBodyUntilResize = 1 << 12,
	SizingFixedFit = 1 << 13,
	SizingFixedSame = 2 << 13,
	SizingStretchProp = 3 << 13,
	SizingStretchSame = 4 << 13,
	NoHostExtendX = 1 << 16,
	NoHostExtendY = 1 << 17,
	NoKeepColumnsVisible = 1 << 18,
	PreciseWidths = 1 << 19,
	NoClip = 1 << 20,
	PadOuterX = 1 << 21,
	NoPadOuterX = 1 << 22,
	NoPadInnerX = 1 << 23,
	ScrollX = 1 << 24,
	ScrollY = 1 << 25,
	SortMulti = 1 << 26,
	SortTristate = 1 << 27,
	HighlightHoveredColumn = 1 << 28,
	SizingMask_ = SizingFixedFit | SizingFixedSame | SizingStretchProp | SizingStretchSame,
}

[Flags]
public enum ImGuiTableColumnFlags
{
	None = 0,
	Disabled = 1 << 0,
	DefaultHide = 1 << 1,
	DefaultSort = 1 << 2,
	WidthStretch = 1 << 3,
	WidthFixed = 1 << 4,
	NoResize = 1 << 5,
	NoReorder = 1 << 6,
	NoHide = 1 << 7,
	NoClip = 1 << 8,
	NoSort = 1 << 9,
	NoSortAscending = 1 << 10,
	NoSortDescending = 1 << 11,
	NoHeaderLabel = 1 << 12,
	NoHeaderWidth = 1 << 13,
	PreferSortAscending = 1 << 14,
	PreferSortDescending = 1 << 15,
	IndentEnable = 1 << 16,
	IndentDisable = 1 << 17,
	AngledHeader = 1 << 18,
	IsEnabled = 1 << 24,
	IsVisible = 1 << 25,
	IsSorted = 1 << 26,
	IsHovered = 1 << 27,
	WidthMask_ = WidthStretch | WidthFixed,
	IndentMask_ = IndentEnable | IndentDisable,
	StatusMask_ = IsEnabled | IsVisible | IsSorted | IsHovered,
}

[Flags]
public enum ImGuiTableRowFlags
{
	None = 0,
	Headers = 1 << 0,
}

public enum ImGuiTableBgTarget
{
	None = 0,
	RowBg0 = 1,
	RowBg1 = 2,
	CellBg = 3,
}

public enum ImGuiStyleVar
{
	Alpha,
	DisabledAlpha,
	WindowPadding,
	WindowRounding,
	WindowBorderSize,
	WindowMinSize,
	WindowTitleAlign,
	ChildRounding,
	ChildBorderSize,
	PopupRounding,
	PopupBorderSize,
	FramePadding,
	FrameRounding,
	FrameBorderSize,
	ItemSpacing,
	ItemInnerSpacing,
	IndentSpacing,
	CellPadding,
	ScrollbarSize,
	ScrollbarRounding,
	GrabMinSize,
	GrabRounding,
	TabRounding,
	TabBorderSize,
	TabBarBorderSize,
	TabBarOverlineSize,
	TableAngledHeadersAngle,
	TableAngledHeadersTextAlign,
	ButtonTextAlign,
	SelectableTextAlign,
	SeparatorTextBorderSize,
	SeparatorTextAlign,
	SeparatorTextPadding,
	COUNT
}

[Flags]
public enum ImDrawFlags
{
	None = 0,
	Closed = 1 << 0,
	RoundCornersTopLeft = 1 << 4,
	RoundCornersTopRight = 1 << 5,
	RoundCornersBottomLeft = 1 << 6,
	RoundCornersBottomRight = 1 << 7,
	RoundCornersNone = 1 << 8,
	RoundCornersTop = RoundCornersTopLeft | RoundCornersTopRight,
	RoundCornersBottom = RoundCornersBottomLeft | RoundCornersBottomRight,
	RoundCornersLeft = RoundCornersBottomLeft | RoundCornersTopLeft,
	RoundCornersRight = RoundCornersBottomRight | RoundCornersTopRight,
	RoundCornersAll = RoundCornersTopLeft | RoundCornersTopRight | RoundCornersBottomLeft | RoundCornersBottomRight,
	RoundCornersDefault_ = RoundCornersAll,
	RoundCornersMask_ = RoundCornersAll | RoundCornersNone,
}

internal enum ImGuiLayoutType
{
	Horizontal = 0,
	Vertical = 1,
}

internal enum ImGuiAxis
{
	None = -1,
	X = 0,
	Y = 1,
}

[Flags]
internal enum ImGuiNextWindowDataFlags
{
	None = 0,
	HasPos = 1 << 0,
	HasSize = 1 << 1,
	HasContentSize = 1 << 2,
	HasCollapsed = 1 << 3,
	HasSizeConstraint = 1 << 4,
	HasFocus = 1 << 5,
	HasBgAlpha = 1 << 6,
	HasScroll = 1 << 7,
	HasChildFlags = 1 << 8,
}

[Flags]
internal enum ImGuiNextItemDataFlags
{
	None = 0,
	HasWidth = 1 << 0,
	HasOpen = 1 << 1,
}
