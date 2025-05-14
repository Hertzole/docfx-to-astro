using System;
using System.Text.RegularExpressions;
using Cysharp.Text;
using DocfxToAstro.Helpers;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro;

internal static partial class Formatters
{
	[GeneratedRegex("<xref href=\"(.*?)\"\\s?(?:data-throw-if-not-resolved=\".*?\")?><\\/xref>", RegexOptions.CultureInvariant)]
	private static partial Regex SummaryReferenceRegex();

	[GeneratedRegex("<code\\s?(?:class=\".*?\")?>(.*?)</code>", RegexOptions.CultureInvariant)]
	private static partial Regex CodeOpenTagRegex();

	[GeneratedRegex(@"\S(\s{0,1}\n\s*)\S", RegexOptions.CultureInvariant)]
	private static partial Regex InvalidNewLineRegex();

	[GeneratedRegex(@"(.*?)(?:\.html)?#(.*)_{1}(.*)", RegexOptions.CultureInvariant)]
	private static partial Regex HeaderLinkRegex();

	public static string FormatSummary(string? summary, ReferenceCollection references)
	{
		if (string.IsNullOrWhiteSpace(summary))
		{
			return string.Empty;
		}

		using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
		sb.Append(summary);

		MatchCollection firstMatches = CodeOpenTagRegex().Matches(summary);
		foreach (Match match in firstMatches)
		{
			summary = summary.Replace(match.Groups[0].Value, $"`{match.Groups[1].Value}`");
			sb.Replace(match.Groups[0].ValueSpan, $"`{match.Groups[1].Value}`");
		}

		MatchCollection newLineMatches = InvalidNewLineRegex().Matches(summary);
		foreach (Match match in newLineMatches)
		{
			sb.Replace(match.Groups[1].ValueSpan, " ");
		}

		sb.Replace("%60", "`");

		MatchCollection matches = SummaryReferenceRegex().Matches(sb.ToString());
		foreach (Match match in matches)
		{
			if (match.Groups.Count < 2)
			{
				continue;
			}

			string uid = match.Groups[1].Value;
			if (references.TryGetReferenceWithLink(uid, out Reference reference))
			{
				ReadOnlySpan<char> href = FormatHref(reference.Href, out bool isExternalLink);
				sb.Replace(match.Groups[0].ValueSpan, $"[{reference.Name}]({(isExternalLink ? string.Empty : "../")}{href.ToString().ToLowerInvariant()}/)");
			}
			else
			{
				sb.Replace(match.Groups[0].ValueSpan, $"`{uid}`");
			}
		}

		return sb.AsSpan().Trim().ToString();
	}

	public static ReadOnlySpan<char> FormatHref(ReadOnlySpan<char> href, out bool isExternalLink)
	{
		if (href.StartsWith("https://") || href.StartsWith("http://"))
		{
			isExternalLink = true;
			return href;
		}

		using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
		sb.Append(href);

		if (href.EndsWith(".html"))
		{
			sb.Remove(sb.Length - 5, 5);
		}

		sb.Replace('`', '-');

		MatchCollection headerLinkMatches = HeaderLinkRegex().Matches(sb.ToString());
		if (headerLinkMatches.Count == 1)
		{
			Match match = headerLinkMatches[0];

			sb.Clear();
			sb.Append(match.Groups[1].Value);
			sb.Append("/#");
			sb.Append(match.Groups[3].Value);
		}

		isExternalLink = false;
		return sb.AsSpan();
	}

	public static ReadOnlySpan<char> FormatType(ReadOnlySpan<char> value)
	{
		// Only run this if the value contains any of the characters
		if (!value.ContainsAny('{', '}'))
		{
			return value;
		}

		using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
		sb.Append(value);

		// Replaces types like List{{T}} with List<T>
		sb.Replace("{{", "\\<");
		sb.Replace("}}", "\\>");
		// Replaces types like {T}[] with T[]
		sb.Replace("{", string.Empty);
		sb.Replace("}", string.Empty);

		return sb.AsSpan();
	}

	public static ReadOnlySpan<char> FormatSlug(ReadOnlySpan<char> value)
	{
		if (!value.EndsWith('/'))
		{
			return value;
		}

		return value.Slice(0, value.Length - 1);
	}
}