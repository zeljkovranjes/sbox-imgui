namespace Duccsoft.ImGui;

/// <summary>
/// Draws an ImGui inspector for any component's [Property] members.
/// </summary>
public static class ComponentExtensions
{
	private static readonly Dictionary<Type, List<PropertyDescription>> _propertyCache = new();

	private static List<PropertyDescription> GetProperties( Type type )
	{
		if ( _propertyCache.TryGetValue( type, out var props ) )
			return props;

		var typeDesc = TypeLibrary.GetType( type );
		props = typeDesc?.Properties
			.Where( p => p.HasAttribute<PropertyAttribute>() && p.CanRead && !typeof( Delegate ).IsAssignableFrom( p.PropertyType ) )
			.ToList() ?? new List<PropertyDescription>();
		_propertyCache[type] = props;
		return props;
	}

	/// <summary>
	/// Draw an editor for every [Property] of this component. If called outside of a window, a window named after the component type is created.
	/// </summary>
	public static void ImGuiInspector( this Component component )
	{
		if ( !component.IsValid() )
			return;

		var properties = GetProperties( component.GetType() );
		bool ownWindow = ImGui.G.CurrentWindow is null || ImGui.G.CurrentWindow.IsFallbackWindow;
		if ( ownWindow )
		{
			var title = TypeLibrary.GetType( component.GetType() )?.Title ?? component.GetType().Name;
			if ( !ImGui.Begin( $"{title}###{component.Id}" ) )
			{
				ImGui.End();
				return;
			}
		}

		ImGui.PushID( component.Id.GetHashCode() );
		for ( int i = 0; i < properties.Count; i++ )
		{
			// Skip event hooks (OnComponentEnabled etc.): delegates are not editable data.
			if ( typeof( Delegate ).IsAssignableFrom( properties[i].PropertyType ) )
				continue;
			ImGui.PushID( i );
			component.ImGuiProperty( properties[i] );
			ImGui.PopID();
		}
		ImGui.PopID();

		if ( ownWindow )
			ImGui.End();
	}

	/// <summary>
	/// Draw an editor for one property. Returns true if the value was changed.
	/// </summary>
	public static bool ImGuiProperty( this Component component, PropertyDescription prop )
	{
		if ( !component.IsValid() || prop is null )
			return false;

		var label = prop.Title ?? prop.Name;
		var type = prop.PropertyType;
		var range = prop.GetCustomAttribute<RangeAttribute>();
		bool readOnly = !prop.CanWrite;
		object value;
		try
		{
			value = prop.GetValue( component );
		}
		catch ( Exception )
		{
			return false;
		}

		if ( readOnly )
			ImGui.BeginDisabled();

		bool changed = false;
		object newValue = value;

		if ( type == typeof( float ) )
		{
			var v = (float)value;
			changed = range is not null
				? ImGui.SliderFloat( label, ref v, range.Min, range.Max )
				: ImGui.DragFloat( label, ref v, 0.1f );
			newValue = v;
		}
		else if ( type == typeof( double ) )
		{
			var v = (double)value;
			var f = (float)v;
			changed = range is not null
				? ImGui.SliderFloat( label, ref f, range.Min, range.Max )
				: ImGui.DragFloat( label, ref f, 0.1f );
			newValue = (double)f;
		}
		else if ( type == typeof( int ) )
		{
			var v = (int)value;
			changed = range is not null
				? ImGui.SliderInt( label, ref v, (int)range.Min, (int)range.Max )
				: ImGui.DragInt( label, ref v, 0.2f );
			newValue = v;
		}
		else if ( type == typeof( bool ) )
		{
			var v = (bool)value;
			changed = ImGui.Checkbox( label, ref v );
			newValue = v;
		}
		else if ( type == typeof( string ) )
		{
			var v = (string)value ?? string.Empty;
			changed = ImGui.InputText( label, ref v );
			newValue = v;
		}
		else if ( type == typeof( Vector2 ) )
		{
			var v = (Vector2)value;
			changed = range is not null
				? ImGui.SliderFloat2( label, ref v, range.Min, range.Max )
				: ImGui.DragFloat2( label, ref v, 0.1f );
			newValue = v;
		}
		else if ( type == typeof( Vector3 ) )
		{
			var v = (Vector3)value;
			changed = range is not null
				? ImGui.SliderFloat3( label, ref v, range.Min, range.Max )
				: ImGui.DragFloat3( label, ref v, 0.1f );
			newValue = v;
		}
		else if ( type == typeof( Vector4 ) )
		{
			var v = (Vector4)value;
			changed = range is not null
				? ImGui.SliderFloat4( label, ref v, range.Min, range.Max )
				: ImGui.DragFloat4( label, ref v, 0.1f );
			newValue = v;
		}
		else if ( type == typeof( Angles ) )
		{
			var a = (Angles)value;
			var v = new Vector3( a.pitch, a.yaw, a.roll );
			changed = ImGui.DragFloat3( label, ref v, 0.5f );
			newValue = new Angles( v.x, v.y, v.z );
		}
		else if ( type == typeof( Rotation ) )
		{
			var a = ((Rotation)value).Angles();
			var v = new Vector3( a.pitch, a.yaw, a.roll );
			changed = ImGui.DragFloat3( label, ref v, 0.5f );
			newValue = Rotation.From( new Angles( v.x, v.y, v.z ) );
		}
		else if ( type == typeof( Color ) )
		{
			var v = (Color)value;
			changed = ImGui.ColorEdit4( label, ref v );
			newValue = v;
		}
		else if ( type.IsEnum )
		{
			var names = Enum.GetNames( type );
			var values = Enum.GetValues( type );
			int current = Array.IndexOf( values, value );
			changed = ImGui.Combo( label, ref current, names );
			if ( changed && current >= 0 )
				newValue = values.GetValue( current );
		}
		else
		{
			ImGui.LabelText( label, "{0}", value?.ToString() ?? "null" );
		}

		if ( readOnly )
			ImGui.EndDisabled();

		if ( changed && !readOnly )
		{
			try
			{
				prop.SetValue( component, newValue );
			}
			catch ( Exception e )
			{
				Log.Warning( $"ImGuiInspector: could not set {prop.Name}: {e.Message}" );
				return false;
			}
		}
		return changed;
	}
}
