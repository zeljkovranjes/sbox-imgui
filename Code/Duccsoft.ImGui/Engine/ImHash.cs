namespace Duccsoft.ImGui.Engine;

/// <summary>
/// ID hashing shared by the facade and the window state (moved out of the facade so Engine never calls it).
/// </summary>
internal static class ImHash
{
	/// <summary>
	/// FNV-1a hash of a string, seeded. Supports "###" to reset the hash so that only the part after "###" counts.
	/// </summary>
	internal static int Str( string str, int seed )
	{
		unchecked
		{
			uint crc = (uint)seed ^ 2166136261u;
			uint seedU = crc;
			if ( str is null )
				return (int)crc;

			for ( int i = 0; i < str.Length; i++ )
			{
				char c = str[i];
				if ( c == '#' && i + 2 < str.Length && str[i + 1] == '#' && str[i + 2] == '#' )
					crc = seedU;
				crc ^= c;
				crc *= 16777619u;
			}
			int result = (int)crc;
			return result == 0 ? 1 : result;
		}
	}

	internal static int Int( int value, int seed )
	{
		unchecked
		{
			uint crc = (uint)seed ^ 2166136261u;
			for ( int i = 0; i < 4; i++ )
			{
				crc ^= (uint)((value >> (i * 8)) & 0xFF);
				crc *= 16777619u;
			}
			// Distinguish int ids from string ids of the same bytes.
			crc ^= 0x9E3779B9u;
			crc *= 16777619u;
			int result = (int)crc;
			return result == 0 ? 1 : result;
		}
	}
}
