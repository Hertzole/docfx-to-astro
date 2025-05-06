using System;
using System.Collections.Generic;
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