using System;
using System.Collections.Generic;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro.Helpers;

public sealed class ReferenceCollection
{
	private readonly Dictionary<string, Reference> references = new Dictionary<string, Reference>();

	public void Add(string type, Reference reference)
	{
		if (!references.TryAdd(type, reference))
		{
			references[type] = reference;
		}
	}

	public void Clear()
	{
		references.Clear();
	}

	public bool TryGetReference(string type, out Reference reference)
	{
		return references.TryGetValue(type, out reference);
	}

	public bool TryGetReferenceWithLink(string type, out Reference reference)
	{
#if NET9_0_OR_GREATER
		ReadOnlySpan<char> typeSpan = type;
		var unescaped = Uri.UnescapeDataString(typeSpan);

		return references
			.GetAlternateLookup<ReadOnlySpan<char>>()
			.TryGetValue(unescaped, out reference)
			&& !string.IsNullOrWhiteSpace(reference.Href);
#else
		var unescaped = Uri.UnescapeDataString(type);

		return references
			.TryGetValue(unescaped, out reference)
			&& !string.IsNullOrWhiteSpace(reference.Href);
#endif
	}
}
