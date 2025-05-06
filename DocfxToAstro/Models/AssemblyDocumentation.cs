using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using DocfxToAstro.Helpers;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro.Models;

public sealed class AssemblyDocumentation
{
	public string Name { get; }
	public ImmutableArray<TypeDocumentation> Types { get; private set; }

	private AssemblyDocumentation(string name)
	{
		Name = name;
	}

	public static ImmutableArray<AssemblyDocumentation> FromRoots(IList<Root> roots, in CancellationToken cancellationToken = default)
	{
		return FindAssemblyItems(roots, in cancellationToken);
	}

	private static ImmutableArray<AssemblyDocumentation> FindAssemblyItems(IList<Root> roots, in CancellationToken cancellationToken)
	{
		ImmutableDictionary<string, AssemblyDocumentation>.Builder assembliesBuilder = ImmutableDictionary.CreateBuilder<string, AssemblyDocumentation>();
		ImmutableDictionary<string, ImmutableArray<TypeDocumentation>.Builder>.Builder typesBuilders =
			ImmutableDictionary.CreateBuilder<string, ImmutableArray<TypeDocumentation>.Builder>();

		ReferenceCollection references = new ReferenceCollection();

		for (int i = 0; i < roots.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			references.Clear();

			for (int j = 0; j < roots[i].References.Length; j++)
			{
				references.Add(roots[i].References[j].Uid, roots[i].References[j]);
			}

			for (int j = 0; j < roots[i].Items.Count; j++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				Item item = roots[i].Items[j];

				if (item.Assemblies == null || item.Assemblies.Length == 0)
				{
					continue;
				}

				string mainAssembly = item.Assemblies[0];
				if (!assembliesBuilder.TryGetValue(mainAssembly, out AssemblyDocumentation? assembly))
				{
					assembly = new AssemblyDocumentation(mainAssembly);
					assembliesBuilder.Add(mainAssembly, assembly);
				}

				if (!typesBuilders.TryGetValue(mainAssembly, out ImmutableArray<TypeDocumentation>.Builder? typesBuilder))
				{
					typesBuilder = ImmutableArray.CreateBuilder<TypeDocumentation>();
					typesBuilders.Add(mainAssembly, typesBuilder);
				}

				if (TryGetTypeDocumentation(item, references, out TypeDocumentation? typeDocumentation))
				{
					typeDocumentation.FindChildren(roots[i].Items, references);
					typesBuilder.Add(typeDocumentation);
				}
			}
		}

		ImmutableDictionary<string, AssemblyDocumentation> assemblies = assembliesBuilder.ToImmutableDictionary();
		ImmutableArray<AssemblyDocumentation>.Builder result = ImmutableArray.CreateBuilder<AssemblyDocumentation>(assemblies.Count);
		foreach ((string key, AssemblyDocumentation assembly) in assemblies)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (typesBuilders.TryGetValue(key, out ImmutableArray<TypeDocumentation>.Builder? typesBuilder))
			{
				assembly.Types = typesBuilder.ToImmutable();
			}
			else
			{
				assembly.Types = ImmutableArray<TypeDocumentation>.Empty;
			}

			result.Add(assembly);
		}

		return result.ToImmutable();
	}

	private static bool TryGetTypeDocumentation(Item item, ReferenceCollection references, [NotNullWhen(true)] out TypeDocumentation? documentation)
	{
		if (!item.Type.IsType())
		{
			documentation = null;
			return false;
		}

		documentation = new TypeDocumentation(item, references);
		return true;
	}
}