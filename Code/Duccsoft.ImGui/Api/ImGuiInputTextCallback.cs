namespace Duccsoft.ImGui;

/// <summary>Callback for InputText() with the ImGuiInputTextFlags.Callback* flags. Return non-zero from a CharFilter callback to discard the character.</summary>
public delegate int ImGuiInputTextCallback( ImGuiInputTextCallbackData data );
