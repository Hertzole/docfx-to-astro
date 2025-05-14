using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DocfxToAstro.Models;
using DocfxToAstro.Models.Yaml;
using Microsoft.Extensions.Logging;
using VYaml.Serialization;

namespace DocfxToAstro;

internal static partial class Commands
{
	/// <summary>
	/// </summary>
	/// <param name="input">-i, The location of all the API files</param>
	/// <param name="output">-o, The location to put all the markdown files</param>
	/// <param name="baseSlug">The base slug to use in urls</param>
	/// <param name="dontClear">Don't clear the output location before generating files</param>
	/// <param name="verbose">Print extra information</param>
	/// <param name="cancellationToken"></param>
	public static async Task<int> Generate(string input,
		string output,
		string baseSlug = "reference",
		bool dontClear = false,
		bool verbose = false,
		CancellationToken cancellationToken = default)
	{
		using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
		});

		ILogger logger = loggerFactory.CreateLogger("docfx2astro");
		LogStarting(logger);

		string[] files = Directory.GetFiles(input, "*.yml", SearchOption.AllDirectories);
		LogFoundFiles(logger, files.Length);

		if (files.Length == 0)
		{
			logger.LogError("No .yml files found in the input directory.");
			return 1;
		}

		if (!dontClear)
		{
			ClearDirectory(output, logger);
		}

		List<Root> roots = new List<Root>(files.Length);

		for (int i = 0; i < files.Length; i++)
		{
			string fileName = Path.GetFileName(files[i]);
			if (fileName.Equals("toc.yml", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			LogReadingFile(logger, fileName);

			await using FileStream fileStream = File.OpenRead(files[i]);
			Root root = await YamlSerializer.DeserializeAsync<Root>(fileStream, YamlSerializerOptions.Standard);
			roots.Add(root);
		}

		ImmutableArray<AssemblyDocumentation> assemblies = AssemblyDocumentation.FromRoots(roots, cancellationToken);
		if (assemblies.IsDefaultOrEmpty)
		{
			logger.LogError("No assemblies found in the input files.");
			return 1;
		}

		MarkdownGenerator generator = new MarkdownGenerator(logger, baseSlug);

		generator.GenerateMarkdownForAssemblies(in assemblies, Path.GetFullPath(output), cancellationToken);

		LogSuccess(logger);
		return 0;
	}

	private static void ClearDirectory(string path, ILogger logger)
	{
		if (Directory.Exists(path))
		{
			foreach (string file in Directory.GetFiles(path))
			{
				File.Delete(file);
			}

			foreach (string directory in Directory.GetDirectories(path))
			{
				Directory.Delete(directory, true);
			}
		}

		LogClearedDirectory(logger, path);
	}

	[LoggerMessage(LogLevel.Information, "Starting docfx2astro", EventName = "Starting")]
	private static partial void LogStarting(ILogger logger);

	[LoggerMessage(LogLevel.Debug, "Found {count} files", EventName = "FoundFiles")]
	private static partial void LogFoundFiles(ILogger logger, int count);

	[LoggerMessage(LogLevel.Debug, "Cleared directory '{directory}'", EventName = "ClearedDirectory")]
	private static partial void LogClearedDirectory(ILogger logger, string directory);

	[LoggerMessage(LogLevel.Debug, "Reading file '{fileName}'", EventName = "ReadingFile")]
	private static partial void LogReadingFile(ILogger logger, string fileName);

	[LoggerMessage(LogLevel.Information, "Successfully generated markdown files!", EventName = "Done")]
	private static partial void LogSuccess(ILogger logger);
}