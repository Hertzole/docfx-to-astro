#if !NET9_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System;

internal static class MemoryExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool StartsWith<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
	{
		return span.Length != 0 && (span[0]?.Equals(value) ?? (object?) value is null);
	}

	public static bool EndsWith<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
	{
		return span.Length != 0 && (span[^1]?.Equals(value) ?? (object?) value is null);
	}
}
#endif