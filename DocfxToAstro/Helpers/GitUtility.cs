using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocfxToAstro.Helpers;

// Taken from:
// https://github.com/dotnet/docfx/blob/c4447a95/src/Docfx.Common/Git/GitUtility.cs
// Copyright (c) .NET Foundation and Contributors
// Licensed under the MIT license.

public readonly record struct GitSource(string Repo, string Branch, string Path, int Line);

public static partial class GitUtility
{
	public static string? GetSourceUrl(GitSource source)
	{
		ReadOnlySpan<char> repo = source.Repo.StartsWith("git") ? GitUrlToHttps(source.Repo) : source.Repo;
		repo = repo.TrimEnd('/').TrimEnd(".git");

		if (!Uri.TryCreate(repo.ToString(), UriKind.Absolute, out var url))
			return null;

		var path = source.Path.Replace('\\', '/');

		var sourceUrl = url.Host switch
		{
			"github.com" => $"https://github.com{url.AbsolutePath}/blob/{source.Branch}/{path}{(source.Line > 0 ? $"#L{source.Line}" : null)}",
			"bitbucket.org" => $"https://bitbucket.org{url.AbsolutePath}/src/{source.Branch}/{path}{(source.Line > 0 ? $"#lines-{source.Line}" : null)}",
			_ when url.Host.EndsWith(".visualstudio.com") || url.Host == "dev.azure.com" =>
				$"https://{url.Host}{url.AbsolutePath}?path={path}&version={(IsCommit(source.Branch) ? "GC" : "GB")}{source.Branch}{(source.Line > 0 ? $"&line={source.Line}" : null)}",
			_ => null,
		};

		if (sourceUrl == null)
			return null;

		return ResolveDocfxSourceRepoUrl(sourceUrl);

		static bool IsCommit(string branch)
		{
			return branch.Length == 40 && branch.All(char.IsLetterOrDigit);
		}

		static string GitUrlToHttps(string url)
		{
			var pos = url.IndexOf('@');
			if (pos == -1) return url;
			return $"https://{url.Substring(pos + 1).Replace(":[0-9]+", "").Replace(':', '/')}";
		}
	}

	/// <summary>
	/// Rewrite path if `DOCFX_SOURCE_REPOSITORY_URL` environment variable is specified.
	/// </summary>
	private static string ResolveDocfxSourceRepoUrl(string originalUrl)
	{
		var docfxSourceRepoUrl = Environment.GetEnvironmentVariable("DOCFX_SOURCE_REPOSITORY_URL");
		if (docfxSourceRepoUrl == null)
			return originalUrl;

		if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var parsedOriginalUrl)
		 || !Uri.TryCreate(docfxSourceRepoUrl, UriKind.Absolute, out var parsedOverrideUrl)
		 || parsedOriginalUrl.Host != parsedOverrideUrl.Host)
		{
			return originalUrl;
		}

		// Parse value that defined with `{orgName}/{repoName}` format.
		var parts = parsedOverrideUrl.LocalPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length < 2)
			return originalUrl;

		string orgName = parts[0];
		string repoName = parts[1];

		switch (parsedOriginalUrl.Host)
		{
			case "github.com":
			case "bitbucket.org":
			case "dev.azure.com":
				{
					// Replace `/{orgName}/{repoName}` and remove `.git` suffix.
					var builder = new UriBuilder(parsedOriginalUrl);
					builder.Path = OrgRepoRegex().Replace(builder.Path.TrimEnd(".git").ToString(), $"/{orgName}/{repoName}");
					return builder.Uri.ToString();
				}

			// Currently other URL formats are not supported (e.g. visualstudio.com, GitHub Enterprise Server)
			default:
				return originalUrl;
		}
	}

	[GeneratedRegex("^/[^/]+/[^/]+")]
	private static partial Regex OrgRepoRegex();
}
