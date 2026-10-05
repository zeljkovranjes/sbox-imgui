namespace Duccsoft.ImGui;

/// <summary>
/// Key-value storage used by windows to persist small amounts of state (e.g. which tree nodes are open)
/// across frames, keyed by ImGui ID.
/// </summary>
public class ImGuiStorage
{
	private readonly Dictionary<int, int> _ints = new();
	private readonly Dictionary<int, float> _floats = new();
	private readonly Dictionary<int, object> _objects = new();

	public void Clear()
	{
		_ints.Clear();
		_floats.Clear();
		_objects.Clear();
	}

	public int GetInt( int key, int defaultValue = 0 ) => _ints.TryGetValue( key, out var v ) ? v : defaultValue;
	public void SetInt( int key, int value ) => _ints[key] = value;
	public bool GetBool( int key, bool defaultValue = false ) => GetInt( key, defaultValue ? 1 : 0 ) != 0;
	public void SetBool( int key, bool value ) => SetInt( key, value ? 1 : 0 );
	public float GetFloat( int key, float defaultValue = 0f ) => _floats.TryGetValue( key, out var v ) ? v : defaultValue;
	public void SetFloat( int key, float value ) => _floats[key] = value;
	public object GetObject( int key ) => _objects.TryGetValue( key, out var v ) ? v : null;
	public void SetObject( int key, object value ) => _objects[key] = value;
	public bool ContainsInt( int key ) => _ints.ContainsKey( key );

	public T GetOrCreate<T>( int key ) where T : class, new()
	{
		if ( _objects.TryGetValue( key, out var v ) && v is T typed )
			return typed;

		var created = new T();
		_objects[key] = created;
		return created;
	}
}
