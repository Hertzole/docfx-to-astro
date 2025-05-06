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
		return references.TryGetValue(type, out reference) && !string.IsNullOrWhiteSpace(reference.Href);
	}
}