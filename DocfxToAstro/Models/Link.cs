using System;
using Cysharp.Text;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro.Models;

public readonly record struct Link(bool IsExternalLink, string Href)
{
	public bool IsEmpty
	{
		get { return this == Empty || this == default; }
	}

	public static Link Empty
	{
		get { return new Link(false, string.Empty); }
	}

	public static Link FromReference(in Reference reference)
	{
		ReadOnlySpan<char> href = Formatters.FormatHref(reference.Href, out bool isExternalLink);
		Span<char> result = stackalloc char[href.Length];
		href.CopyTo(result);
		href.ToLowerInvariant(result);
		return new Link(isExternalLink, result.ToString());
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return ToString("../");
	}

	public string ToString(string baseLocalPath)
	{
		if (IsExternalLink)
		{
			return Href;
		}

		return ZString.Concat(baseLocalPath, Href);
	}
}