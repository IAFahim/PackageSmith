using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PackageSmith.Data.Types;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public unsafe struct FixedString64
{
	private const int MaxLength = 64;
	private fixed char _chars[MaxLength];

	public readonly int Length;

	public FixedString64(string value)
	{
		Length = Math.Min(value?.Length ?? 0, MaxLength);
		for (var i = 0; i < MaxLength; i++)
		{
			_chars[i] = i < Length ? value[i] : '\0';
		}
	}

	public readonly override string ToString()
	{
		var sb = new StringBuilder(Length);
		for (var i = 0; i < Length; i++)
		{
			fixed (char* ptr = _chars)
			{
				sb.Append(ptr[i]);
			}
		}
		return sb.ToString();
	}

	public readonly bool IsEmpty => Length == 0;
}
