using System;
using System.Collections.Generic;
using Cysharp.Text;
using DocfxToAstro.Helpers;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro.Models;

public sealed class TypeDocumentation
{
	private readonly List<TypeDocumentation> constructors = new List<TypeDocumentation>();
	private readonly List<TypeDocumentation> fields = new List<TypeDocumentation>();
	private readonly List<TypeDocumentation> properties = new List<TypeDocumentation>();
	private readonly List<TypeDocumentation> methods = new List<TypeDocumentation>();
	private readonly List<TypeDocumentation> events = new List<TypeDocumentation>();

	public string Uid { get; }
	public string Name { get; }
	public string FullName { get; }
	public ItemType Type { get; }
	public string? Summary { get; }
	public Link Link { get; }
	public string? Syntax { get; }
	public string? Remarks { get; }

	public TypeReferenceDocumentation[] Inheritance { get; }
	public TypeReferenceDocumentation[] Implements { get; }
	public ParameterDocumentation[] Parameters { get; }
	public ReturnDocumentation? Returns { get; }
	public TypeParameter[] TypeParameters { get; }
	public ExceptionDocumentation[] Exceptions { get; }
	public AttributeDoc[] Attributes { get; }

	public IReadOnlyList<TypeDocumentation> Constructors
	{
		get { return constructors; }
	}

	public IReadOnlyList<TypeDocumentation> Fields
	{
		get { return fields; }
	}

	public IReadOnlyList<TypeDocumentation> Properties
	{
		get { return properties; }
	}

	public IReadOnlyList<TypeDocumentation> Methods
	{
		get { return methods; }
	}

	public IReadOnlyList<TypeDocumentation> Events
	{
		get { return events; }
	}

	public TypeDocumentation(Item item, ReferenceCollection references)
	{
		Uid = item.Uid!;

		Name = item.Name!;
		FullName = item.FullName!;
		Type = item.Type;
		Summary = Formatters.FormatSummary(item.Summary, references);
		Remarks = Formatters.FormatSummary(item.Remarks, references);
		if (item.Syntax != null && !string.IsNullOrWhiteSpace(item.Syntax.Content))
		{
			Syntax = item.Syntax.Content.Trim();
		}

		Link = new Link(false, ZString.Concat(Formatters.FormatHref(item.Uid!, out _).ToString().ToLowerInvariant()));

		Inheritance = GetReferencesArray(item.Inheritance, references);
		Implements = GetReferencesArray(item.Implements, references);

		Parameters = GetParameters(in item, references);
		Returns = GetReturn(in item, references);
		TypeParameters = GetTypeParameters(in item, references);
		Exceptions = GetExceptions(in item, references);
		Attributes = item.Attributes ?? Array.Empty<AttributeDoc>();
	}

	public void FindChildren(IList<Item> items, ReferenceCollection references)
	{
		for (int i = 0; i < items.Count; i++)
		{
			// Skip if the item is not a child of this type
			if (items[i].Parent != Uid)
			{
				continue;
			}

			TypeDocumentation child = new TypeDocumentation(items[i], references);

			switch (child.Type)
			{
				case ItemType.Field:
					fields.Add(child);
					break;
				case ItemType.Property:
					properties.Add(child);
					break;
				case ItemType.Method:
					methods.Add(child);
					// Methods can have children, like return and parameters
					child.FindChildren(items, references);
					break;
				case ItemType.Constructor:
					constructors.Add(child);
					// Constructors can have children, parameters
					child.FindChildren(items, references);
					break;
				case ItemType.Event:
					events.Add(child);
					// Events can have children, like event type
					child.FindChildren(items, references);
					break;
			}
		}
	}

	private static TypeReferenceDocumentation[] GetReferencesArray(string[]? original, ReferenceCollection references)
	{
		if (original != null && original.Length > 0)
		{
			TypeReferenceDocumentation[] result = new TypeReferenceDocumentation[original.Length];
			for (int i = 0; i < original.Length; i++)
			{
				string name = original[i];
				Link link = Link.Empty;

				if (references.TryGetReferenceWithLink(original[i], out Reference reference))
				{
					link = Link.FromReference(reference);
					name = reference.Name;
				}

				result[i] = new TypeReferenceDocumentation(name, link);
			}

			return result;
		}

		return Array.Empty<TypeReferenceDocumentation>();
	}

	private static ParameterDocumentation[] GetParameters(in Item item, ReferenceCollection references)
	{
		if (item.Syntax == null || item.Syntax.Parameters == null || item.Syntax.Parameters.Length == 0)
		{
			return Array.Empty<ParameterDocumentation>();
		}

		ParameterDocumentation[] result = new ParameterDocumentation[item.Syntax.Parameters.Length];
		for (int i = 0; i < item.Syntax.Parameters.Length; i++)
		{
			Parameter parameter = item.Syntax.Parameters[i];
			string name = parameter.Type;
			Link link = Link.Empty;

			if (references.TryGetReferenceWithLink(parameter.Type, out Reference reference))
			{
				name = reference.Name;
				link = Link.FromReference(reference);
			}

			TypeReferenceDocumentation type = new TypeReferenceDocumentation(name, link);

			result[i] = new ParameterDocumentation(parameter.Id, type, parameter.Description);
		}

		return result;
	}

	private static ReturnDocumentation? GetReturn(in Item item, ReferenceCollection references)
	{
		if (item.Syntax == null || item.Syntax.Returns == null)
		{
			return null;
		}

		Return returns = item.Syntax.Returns.Value;
		string name = returns.Type;
		Link link = Link.Empty;

		if (references.TryGetReferenceWithLink(returns.Type, out Reference reference))
		{
			name = reference.Name;
			link = Link.FromReference(reference);
		}

		TypeReferenceDocumentation type = new TypeReferenceDocumentation(name, link);

		return new ReturnDocumentation(type, Formatters.FormatSummary(item.Syntax.Returns.Value.Description, references));
	}

	private static TypeParameter[] GetTypeParameters(in Item item, ReferenceCollection references)
	{
		if (item.Syntax == null || item.Syntax.TypeParameters == null || item.Syntax.TypeParameters.Length == 0)
		{
			return Array.Empty<TypeParameter>();
		}

		TypeParameter[] result = new TypeParameter[item.Syntax.TypeParameters.Length];
		for (int i = 0; i < item.Syntax.TypeParameters.Length; i++)
		{
			TypeParameter typeParameter = item.Syntax.TypeParameters[i];

			result[i] = new TypeParameter(Formatters.FormatType(typeParameter.Id).ToString(), Formatters.FormatSummary(typeParameter.Description, references));
		}

		return result;
	}

	private static ExceptionDocumentation[] GetExceptions(in Item item, ReferenceCollection references)
	{
		if (item.Exceptions == null || item.Exceptions.Length == 0)
		{
			return Array.Empty<ExceptionDocumentation>();
		}

		ExceptionDocumentation[] result = new ExceptionDocumentation[item.Exceptions.Length];
		for (int i = 0; i < item.Exceptions.Length; i++)
		{
			ExceptionDoc exception = item.Exceptions[i];
			Link link = Link.Empty;
			string name = exception.Type;
			if (references.TryGetReferenceWithLink(exception.Type, out var reference))
			{
				link = Link.FromReference(in reference);
				name = reference.Name;
			}

			result[i] = new ExceptionDocumentation(new TypeReferenceDocumentation(name, link), Formatters.FormatSummary(exception.Description, references));
		}

		return result;
	}
}