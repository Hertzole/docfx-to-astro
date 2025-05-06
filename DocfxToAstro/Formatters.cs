using System;
using System.Collections.Generic;
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

	public static string FormatSummary(string? summary, ReferenceCollection references)
	{
		if (string.IsNullOrWhiteSpace(summary))
		{
			return string.Empty;
		}

		using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
		sb.Append(summary);
		
		var firstMatches = CodeOpenTagRegex().Matches(summary);
		foreach (Match match in firstMatches)
		{
			summary = summary.Replace(match.Groups[0].Value, $"`{match.Groups[1].Value}`");
			sb.Replace(match.Groups[0].ValueSpan, $"`{match.Groups[1].Value}`");
		}
		
		sb.Replace("%60", "`");
		sb.Replace(Environment.NewLine, string.Empty);

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
				sb.Replace(match.Groups[0].ValueSpan, $"[{reference.Name}]({(isExternalLink ? string.Empty : "../")}{href.ToString().ToLowerInvariant()})");
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

		isExternalLink = false;
		return sb.AsSpan();
	}

	public static ReadOnlySpan<char> FormatType(ReadOnlySpan<char> value)
	{
		if(value.StartsWith('{') && value.EndsWith('}'))
		{
			return value.Slice(1, value.Length - 2);
		}

		return value;
	}
}