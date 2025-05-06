using System;
using System.Linq;
using DocfxToAstro.Models;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro;

public static class TypeDocumentationExtensions
{
	public static bool TryGetObsolete(this TypeDocumentation type, out string? reason, out bool isError)
	{
		reason = string.Empty;
		isError = false;

		if (type.Attributes.Length == 0)
		{
			return false;
		}

		AttributeDoc obsoleteAttribute = type.Attributes.FirstOrDefault(static x => x.Type.Equals("System.ObsoleteAttribute", StringComparison.Ordinal));
		if (obsoleteAttribute == default)
		{
			return false;
		}

		TypeWithValue reasonArgument = obsoleteAttribute.Arguments.FirstOrDefault(static x => x.Type.Equals("System.String", StringComparison.Ordinal));
		if (reasonArgument != default)
		{
			reason = reasonArgument.Value.Trim();
		}

		TypeWithValue errorArgument = obsoleteAttribute.Arguments.FirstOrDefault(static x => x.Type.Equals("System.Boolean", StringComparison.Ordinal));
		if (errorArgument != default)
		{
			isError = bool.Parse(errorArgument.Value);
		}

		return true;
	}
}