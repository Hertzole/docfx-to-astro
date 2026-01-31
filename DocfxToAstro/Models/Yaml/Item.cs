using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DocfxToAstro.Helpers;
using VYaml.Annotations;

namespace DocfxToAstro.Models.Yaml;

[YamlObject]
public partial class Item
{
	public string? Uid { get; private init; }
	public string? Id { get; private init; }
	public string? Parent { get; private init; }
	public string? Name { get; private init; }
	public string? FullName { get; private init; }
	[YamlMember("type")]
	public string? TypeString { get; private init; }
	public string? Namespace { get; private init; }
	public string? Summary { get; private init; }
	public string? Remarks { get; private init; }
	public List<string>? Children { get; private init; }
	public string[]? Assemblies { get; private init; }
	public SyntaxContent? Syntax { get; private init; }
	public string[]? Inheritance { get; private init; }
	public string[]? Implements { get; private init; }
	public ExceptionDoc[]? Exceptions { get; private init; }
	public AttributeDoc[]? Attributes { get; private init; }
	public ItemSource? Source { get; private init; }

	[YamlIgnore]
	public ItemType Type
	{
		get
		{
			return TypeString switch
			{
				"Class" => ItemType.Class,
				"Struct" => ItemType.Struct,
				"Namespace" => ItemType.Namespace,
				"Delegate" => ItemType.Delegate,
				"Enum" => ItemType.Enum,
				"Interface" => ItemType.Interface,
				"Field" => ItemType.Field,
				"Property" => ItemType.Property,
				"Method" => ItemType.Method,
				"Operator" => ItemType.Operator,
				"Event" => ItemType.Event,
				"Constructor" => ItemType.Constructor,
				_ => throw new ArgumentOutOfRangeException(nameof(TypeString), TypeString, null)
			};
		}
	}
}

[YamlObject]
public readonly partial struct ItemSource
{
	public ItemSourceRemote Remote { get; private init; }
	public int StartLine { get; private init; }

	public readonly bool TryGetSourceUrl([NotNullWhen(true)] out string? url)
	{
		// Note: StartLine + 1 is actually how they do it:
		// https://github.com/dotnet/docfx/blob/c4447a95/src/Docfx.Dotnet/DotnetApiCatalog.ApiPage.cs#L133
		url = GitUtility.GetSourceUrl(new(Remote.Repo, Remote.Branch, Remote.Path, StartLine + 1));
		return url is { };
	}
}

[YamlObject]
public readonly partial struct ItemSourceRemote
{
	public string Path { get; private init; }
	public string Branch { get; private init; }
	public string Repo { get; private init; }
}