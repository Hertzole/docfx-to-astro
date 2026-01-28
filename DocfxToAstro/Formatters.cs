using System;
using System.Linq;
using System.Net;
using System.Text;
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

	[GeneratedRegex(
		"<pre><code\\s?(?:class=\"(.*?)\")?>(.*?)</code></pre>",
		RegexOptions.CultureInvariant | RegexOptions.Singleline
	)]
	private static partial Regex CodeOpenTagMultilineRegex();

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
			// Note: HtmlDecode is only necessary here because code blocks
			// (without being wrapped in <pre></pre>) make the characters
			// be interpreted literally.
			var decoded = WebUtility.HtmlDecode($"`{match.Groups[1].Value}`");

			summary = summary.Replace(match.Groups[0].Value, decoded);
			sb.Replace(match.Groups[0].ValueSpan, decoded);
		}

		(Match, string)[] multilineMatches = [..
			CodeOpenTagMultilineRegex()
				.Matches(summary)
				.Select(x => (x, Guid.NewGuid().ToString()))
		];

		// We avoid multiline code blocks from getting their newlines removed
		// by substituting the match with a guid before invalid newlines are removed.
		// After it, we replace the guid with what we want.
		foreach ((Match match, string guid) in multilineMatches)
		{
			summary = summary.Replace(match.Groups[0].Value, guid);
			sb.Replace(match.Groups[0].ValueSpan, guid);
		}

		MatchCollection newLineMatches = InvalidNewLineRegex().Matches(summary);
		foreach (Match match in newLineMatches)
		{
			sb.Replace(match.Groups[1].ValueSpan, " ");
		}

		foreach ((Match match, string guid) in multilineMatches)
		{
#if NET9_0_OR_GREATER
			ReadOnlySpan<char> language = match.Groups[1].ValueSpan;
			if (language.StartsWith("lang-"))
				language = language[5..];
#else
			ReadOnlySpan<char> languageSpan = match.Groups[1].ValueSpan;
			if (languageSpan.StartsWith("lang-"))
				languageSpan = languageSpan[5..];

			string language = languageSpan.ToString();
#endif
			// We are getting rid of the containing <pre> tag
			// which would have handled the decode for us.
			var decoded = WebUtility.HtmlDecode(
				$"\n```{language}"
				+ (language is "csharp" ? " title=\"C#\"" : string.Empty)
				+ $"\n{match.Groups[2].Value}\n```\n");

			sb.Replace(guid, decoded);
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
				var hrefToUse = reference.Href;

				// I don't understand why, but if we want the actual link for the type,
				// we need to get the one from spec.csharp that matches our uid.
				// At least, this is the case for direct references to subtypes (not their members).
				if (reference.SpecCSharp is { } specs && specs.Length > 0)
				{
					foreach (var spec in specs)
					{
						if (spec.Uid == reference.Uid && spec.Href is { } specHref)
						{
							hrefToUse = specHref;
							break;
						}
					}
				}

				ReadOnlySpan<char> href = FormatHref(
					hrefToUse,
					reference,
					out bool
					isExternalLink,
					out bool isMemberLink
				);
				sb.Replace(
					match.Groups[0].ValueSpan,
					$"[{reference.Name}]({(isExternalLink ? string.Empty : "../")}"
					+ $"{href.ToString().ToLowerInvariant()}{(isMemberLink ? string.Empty : '/')})"
				);
			}
			else
			{
				sb.Replace(match.Groups[0].ValueSpan, $"`{uid}`");
			}
		}

		return sb.AsSpan().Trim().ToString();
	}

	public static ReadOnlySpan<char> FormatHref(
		ReadOnlySpan<char> href,
		Reference? reference,
		out bool isExternalLink
	) => FormatHref(href, reference, out isExternalLink, out _);

	public static ReadOnlySpan<char> FormatHref(
		ReadOnlySpan<char> href,
		Reference? reference,
		out bool isExternalLink,
		out bool isMemberLink
	)
	{
		isMemberLink = false;

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
			var group3 = match.Groups[3].Value;
			if (group3 is not "" || reference is not { } value)
			{
				sb.Append(group3);
			}
			else
			{
				// Some characters are turned into `-` while some are removed from links.
				// Additionally, `-` can't appear twice in a row. We replicate that behavior here.
				// I haven't looked at the actual implementation in Astro/Starlight
				// so this is probably not 100% accurate.

				Span<char> referenceName = [..
					value.Name.Where(x => char.IsLetterOrDigit(x) || x is ' ' or ',')
					.Select(x => x is ' ' or ',' ? '-' : x)
				];

				StringBuilder linkNameCleaner = new();

				foreach (var element in referenceName)
				{
					if (element is '-' && linkNameCleaner.Length > 0 && linkNameCleaner[^1] == element)
						continue;

					linkNameCleaner.Append(element);
				}
				sb.Append(linkNameCleaner.ToString());
			}
			isMemberLink = true;
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