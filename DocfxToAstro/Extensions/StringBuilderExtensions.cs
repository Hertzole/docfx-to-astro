using System;
using System.IO;
using Cysharp.Text;

namespace DocfxToAstro;

public static class StringBuilderExtensions
{
	public static void WriteToFile(this Utf16ValueStringBuilder sb, string filePath)
	{
#if NET9_0_OR_GREATER
		File.WriteAllText(filePath, sb.AsSpan().Trim());
#else
		File.WriteAllText(filePath, sb.AsSpan().Trim().ToString());
#endif
	}
}