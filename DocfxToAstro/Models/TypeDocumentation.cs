using System;
using System.Collections.Generic;
using Cysharp.Text;
using DocfxToAstro.Helpers;
using DocfxToAstro.Models.Yaml;

namespace DocfxToAstro.Models;

public sealed class TypeDocumentation
{
	private readonly string uid;
	private readonly List<TypeDocumentation> constructors = new List<TypeDocumentation>();
	private readonly List<TypeDocumentation> fields = new List<TypeDocumentation>();
	private readonly List<TypeDocumentation> properties = new List<TypeDocumentation>();
	private readonly List<TypeDocumentation> methods = new List<TypeDocumentation>();
	private readonly List<TypeDocumentation> events = new List<TypeDocumentation>();

	public string Name { get; }
	public string FullName { get; }
	public ItemType Type { get; }
	public string? Summary { get; }
	public Link Link { get; }
	public string? Syntax { get; }
	public string? Remarks { get; }

	public string[] Inheritance { get; }
	public string[] Implements { get; }
	public Parameter[] Parameters { get; }
	public Return? Returns { get; }
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
		uid = item.Uid!;

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
			if (items[i].Parent != uid)
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
					child.FindChildren(items, references);
					break;
				case ItemType.Constructor:
					constructors.Add(child);
					child.FindChildren(items, references);
					break;
				case ItemType.Event:
					events.Add(child);
					child.FindChildren(items, references);
					break;
			}
		}
	}

	private static string[] GetReferencesArray(string[]? original, ReferenceCollection references)
	{
		if (original != null && original.Length > 0)
		{
			using Utf16ValueStringBuilder inheritanceBuilder = ZString.CreateStringBuilder(true);
			string[] result = new string[original.Length];
			for (int i = 0; i < original.Length; i++)
			{
				inheritanceBuilder.Clear();

				if (references.TryGetReference(original[i], out Reference reference))
				{
					if (string.IsNullOrWhiteSpace(reference.Href))
					{
						inheritanceBuilder.Append(reference.Name);
					}
					else
					{
						inheritanceBuilder.Append('[');
						inheritanceBuilder.Append(reference.Name);
						inheritanceBuilder.Append("](");

						ReadOnlySpan<char> href = Formatters.FormatHref(reference.Href, out bool isExternalLink);
						if (!isExternalLink)
						{
							inheritanceBuilder.Append("../");
						}

						inheritanceBuilder.Append(href.ToString().ToLowerInvariant());
						inheritanceBuilder.Append(')');
					}
				}

				result[i] = inheritanceBuilder.ToString();
			}

			return result;
		}

		return Array.Empty<string>();
	}

	private static Parameter[] GetParameters(in Item item, ReferenceCollection references)
	{
		if (item.Syntax == null || item.Syntax.Parameters == null || item.Syntax.Parameters.Length == 0)
		{
			return Array.Empty<Parameter>();
		}

		Parameter[] result = new Parameter[item.Syntax.Parameters.Length];
		for (int i = 0; i < item.Syntax.Parameters.Length; i++)
		{
			Parameter parameter = item.Syntax.Parameters[i];
			string type;

			if (references.TryGetReferenceWithLink(parameter.Type, out Reference reference))
			{
				string name = reference.Name;
				ReadOnlySpan<char> href = Formatters.FormatHref(reference.Href, out bool isExternalLink);
				if (!isExternalLink)
				{
					type = ZString.Format("[{0}](../{1})", Formatters.FormatType(name).ToString(), href.ToString().ToLowerInvariant());
				}
				else
				{
					type = ZString.Format("[{0}]({1})", Formatters.FormatType(name).ToString(), href.ToString().ToLowerInvariant());
				}
			}
			else
			{
				type = Formatters.FormatType(parameter.Type).ToString();
			}

			result[i] = new Parameter(parameter.Id, type, Formatters.FormatSummary(parameter.Description, references));
		}

		return result;
	}

	private static Return? GetReturn(in Item item, ReferenceCollection references)
	{
		if (item.Syntax == null || item.Syntax.Returns == null)
		{
			return null;
		}

		Return returns = item.Syntax.Returns.Value;
		string type;

		if (references.TryGetReferenceWithLink(returns.Type, out Reference reference))
		{
			ReadOnlySpan<char> href = Formatters.FormatHref(reference.Href, out bool isExternalLink);
			if (!isExternalLink)
			{
				type = ZString.Format("[{0}](../{1})", Formatters.FormatType(reference.Name).ToString(), href.ToString().ToLowerInvariant());
			}
			else
			{
				type = ZString.Format("[{0}]({1})", Formatters.FormatType(reference.Name).ToString(), href.ToString().ToLowerInvariant());
			}
		}
		else
		{
			type = Formatters.FormatType(returns.Type).ToString();
		}

		return new Return(type, Formatters.FormatSummary(item.Syntax.Returns.Value.Description, references));
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
	
	private static  ExceptionDocumentation[] GetExceptions(in Item item, ReferenceCollection references)
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

			result[i] = new ExceptionDocumentation(name, link, Formatters.FormatSummary(exception.Description, references));
		}

		return result;
	}
}