namespace Duccsoft.ImGui;

/// <summary>
/// Data payload for drag and drop operations: <see cref="ImGui.AcceptDragDropPayload"/>, <see cref="ImGui.GetDragDropPayload"/>.
/// Unlike Dear ImGui, the payload holds a managed object reference instead of a copied byte buffer.
/// </summary>
public class ImGuiPayload
{
	/// <summary>The payload data, as passed to SetDragDropPayload().</summary>
	public object Data;
	/// <summary>Data type tag (short user-supplied string, 32 characters max in Dear ImGui).</summary>
	public string DataType;
	/// <summary>Source item id.</summary>
	public int SourceId;
	/// <summary>Source parent id (if available).</summary>
	public int SourceParentId;
	/// <summary>Data timestamp: the frame the payload was last set.</summary>
	public int DataFrameCount = -1;
	/// <summary>Set when AcceptDragDropPayload() was called and mouse has been hovering the target item (nb: handle overlapping drag targets).</summary>
	public bool Preview;
	/// <summary>Set when AcceptDragDropPayload() was called and mouse button is released over the target item.</summary>
	public bool Delivery;

	public void Clear()
	{
		Data = null;
		DataType = null;
		SourceId = SourceParentId = 0;
		DataFrameCount = -1;
		Preview = Delivery = false;
	}

	public bool IsDataType( string type ) => DataFrameCount != -1 && DataType == type;
	public bool IsPreview() => Preview;
	public bool IsDelivery() => Delivery;

	/// <summary>Get the payload data cast to a type, or default if it is not of that type.</summary>
	public T GetData<T>() => Data is T t ? t : default;
}
