using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocfxToAstro.Models;
using VYaml.Serialization;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro;

internal static class Commands
{
	public static async Task<int> Generate(string input, string output, bool clear = false, bool verbose = false, CancellationToken cancellationToken = default)
	{
		Logger logger = new Logger(verbose);
		string[] files = Directory.GetFiles(input, "*.yml", SearchOption.AllDirectories);

		if (files.Length == 0)
		{
			logger.WriteInfo("No .yml files found in the input directory.");
			return 1;
		}


		if (clear)
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

			await using FileStream fileStream = File.OpenRead(files[i]);
			Root root = await YamlSerializer.DeserializeAsync<Root>(fileStream, YamlSerializerOptions.Standard);
			roots.Add(root);
		}

		var assemblies = AssemblyDocumentation.FromRoots(roots, cancellationToken);
		if (assemblies.IsDefaultOrEmpty)
		{
			return 1;
		}
		
		MarkdownGenerator generator = new MarkdownGenerator(logger);

		generator.GenerateMarkdownForAssemblies(in assemblies, Path.GetFullPath(output), cancellationToken);

		return 0;
	}

	private static void ClearDirectory(string path, Logger logger)
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

		logger.WriteDebug($"Cleared directory: {path}");
	}
}