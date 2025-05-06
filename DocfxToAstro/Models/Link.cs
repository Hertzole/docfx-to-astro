using Cysharp.Text;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro.Models;

public readonly record struct Link(bool IsExternalLink, string Href)
{
	public static Link Empty
	{
		get { return new Link(false, string.Empty); }
	}

	public static Link FromReference(in Reference reference)
	{
		var href = Formatters.FormatHref(reference.Href, out bool isExternalLink);
		return new Link(isExternalLink, href.ToString());
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