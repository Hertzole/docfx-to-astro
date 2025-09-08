using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using DocfxToAstro.Helpers;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro.Models;

public sealed class NamespaceDocumentation
{
	public string Name { get; }
	public ImmutableArray<TypeDocumentation> Types { get; private set; }

	private NamespaceDocumentation(string name)
	{
		Name = name;
	}

	public static ImmutableArray<NamespaceDocumentation> FromRoots(IList<Root> roots, in CancellationToken cancellationToken = default)
	{
		return FindNamespaceItems(roots, in cancellationToken);
	}

	private static ImmutableArray<NamespaceDocumentation> FindNamespaceItems(IList<Root> roots, in CancellationToken cancellationToken)
	{
		ImmutableDictionary<string, NamespaceDocumentation>.Builder namespacesBuilder = ImmutableDictionary.CreateBuilder<string, NamespaceDocumentation>();
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

				// Skip items without a namespace
				if (string.IsNullOrEmpty(item.Namespace))
				{
					continue;
				}

				string namespaceName = item.Namespace;
				if (!namespacesBuilder.TryGetValue(namespaceName, out NamespaceDocumentation? namespaceDoc))
				{
					namespaceDoc = new NamespaceDocumentation(namespaceName);
					namespacesBuilder.Add(namespaceName, namespaceDoc);
				}

				if (!typesBuilders.TryGetValue(namespaceName, out ImmutableArray<TypeDocumentation>.Builder? typesBuilder))
				{
					typesBuilder = ImmutableArray.CreateBuilder<TypeDocumentation>();
					typesBuilders.Add(namespaceName, typesBuilder);
				}

				if (TryGetTypeDocumentation(item, references, out TypeDocumentation? typeDocumentation))
				{
					typeDocumentation.FindChildren(roots[i].Items, references);
					typesBuilder.Add(typeDocumentation);
				}
			}
		}

		ImmutableDictionary<string, NamespaceDocumentation> namespaces = namespacesBuilder.ToImmutableDictionary();
		ImmutableArray<NamespaceDocumentation>.Builder result = ImmutableArray.CreateBuilder<NamespaceDocumentation>(namespaces.Count);
		foreach ((string key, NamespaceDocumentation namespaceDoc) in namespaces)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (typesBuilders.TryGetValue(key, out ImmutableArray<TypeDocumentation>.Builder? typesBuilder))
			{
				namespaceDoc.Types = typesBuilder.ToImmutable();
			}
			else
			{
				namespaceDoc.Types = ImmutableArray<TypeDocumentation>.Empty;
			}

			result.Add(namespaceDoc);
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