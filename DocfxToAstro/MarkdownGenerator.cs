using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Text;
using DocfxToAstro.Models;

namespace DocfxToAstro;

internal sealed class MarkdownGenerator
{
	private readonly Logger logger;

	public MarkdownGenerator(Logger logger)
	{
		this.logger = logger;
	}

	public void GenerateMarkdownForAssemblies(in ImmutableArray<AssemblyDocumentation> assemblies,
		string baseOutputFolder,
		CancellationToken cancellationToken = default)
	{
		if (!Directory.Exists(baseOutputFolder))
		{
			Directory.CreateDirectory(baseOutputFolder);
		}

		using (Utf16ValueStringBuilder indexBuilder = ZString.CreateStringBuilder(true))
		{
			indexBuilder.AppendLine("---");
			indexBuilder.AppendLine("title: API Reference");
			indexBuilder.AppendLine("sidebar:");
			indexBuilder.AppendLine("  hidden: true");
			indexBuilder.Append("---");

			indexBuilder.WriteToFile(Path.Combine(baseOutputFolder, "index.md"));
		}

		for (int i = 0; i < assemblies.Length; i++)
		{
			GenerateAssemblyMarkdown(in assemblies.ItemRef(i), baseOutputFolder, in cancellationToken);
		}
	}

	private void GenerateAssemblyMarkdown(in AssemblyDocumentation assembly, string baseOutputFolder, in CancellationToken cancellationToken)
	{
		string outputFolder = Path.Combine(baseOutputFolder, assembly.Name);

		logger.WriteDebug($"Generating markdown for assembly: {assembly.Name} to {outputFolder}");

		if (!Directory.Exists(outputFolder))
		{
			Directory.CreateDirectory(outputFolder);
		}

		GenerateIndexForAssembly(in assembly, outputFolder, in cancellationToken);

		for (int i = 0; i < assembly.Types.Length; i++)
		{
			GenerateTypeMarkdown(in assembly.Types.ItemRef(i), outputFolder, in cancellationToken);
		}
	}

	private static void GenerateIndexForAssembly(in AssemblyDocumentation assembly, string outputFolder, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		Utf16ValueStringBuilder indexBuilder = ZString.CreateStringBuilder();
		try
		{
			indexBuilder.AppendLine("---");
			indexBuilder.Append("title: ");
			indexBuilder.AppendLine(assembly.Name);
			indexBuilder.Append("slug: reference/");
			indexBuilder.AppendLine(assembly.Name.ToLowerInvariant());
			indexBuilder.AppendLine("sidebar:");
			indexBuilder.AppendLine("  order: 0");
			indexBuilder.AppendLine("---");

			List<TypeDocumentation> classes = assembly.Types.Where(static x => x.Type == ItemType.Class).ToList();
			List<TypeDocumentation> structs = assembly.Types.Where(static x => x.Type == ItemType.Struct).ToList();
			List<TypeDocumentation> interfaces = assembly.Types.Where(static x => x.Type == ItemType.Interface).ToList();
			List<TypeDocumentation> enums = assembly.Types.Where(static x => x.Type == ItemType.Enum).ToList();
			List<TypeDocumentation> delegates = assembly.Types.Where(static x => x.Type == ItemType.Delegate).ToList();

			Write("Classes", classes, ref indexBuilder, in cancellationToken);
			Write("Structs", structs, ref indexBuilder, in cancellationToken);
			Write("Interfaces", interfaces, ref indexBuilder, in cancellationToken);
			Write("Enums", enums, ref indexBuilder, in cancellationToken);
			Write("Delegates", delegates, ref indexBuilder, in cancellationToken);

			indexBuilder.WriteToFile(Path.Combine(outputFolder, "index.md"));
		}
		finally
		{
			indexBuilder.Dispose();
		}

		static void Write(string header, List<TypeDocumentation> types, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
		{
			if (types.Count == 0)
			{
				return;
			}

			using Utf16ValueStringBuilder nameBuilder = ZString.CreateStringBuilder();

			types.Sort(Comparison);

			sb.Append("## ");
			sb.AppendLine(header);
			sb.AppendLine();

			sb.AppendLine("| | |");
			sb.AppendLine("| --- | --- |");
			for (int i = 0; i < types.Count; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				nameBuilder.Clear();

				TypeDocumentation type = types[i];

				nameBuilder.Append(type.Name);
				if (type.TypeParameters.Length > 0)
				{
					nameBuilder.Append("\\<");
					for (int j = 0; j < type.TypeParameters.Length; j++)
					{
						nameBuilder.Append(type.TypeParameters[j].Id);

						if (j < type.TypeParameters.Length - 1)
						{
							nameBuilder.Append(", ");
						}
					}

					nameBuilder.Append("\\>");
				}

				sb.Append("| ");
				AppendTypeWithLink(nameBuilder.AsSpan(), type.Link, ref sb);
				sb.Append(" | ");
				sb.Append(string.IsNullOrWhiteSpace(type.Summary) ? string.Empty : type.Summary);
				sb.AppendLine(" |");
			}
		}

		static int Comparison(TypeDocumentation x, TypeDocumentation y)
		{
			return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
		}
	}

	private static void GenerateTypeMarkdown(in TypeDocumentation type, string baseOutputFolder, in CancellationToken cancellationToken)
	{
		Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();

		try
		{
			AppendYamlHeader(in type, ref sb, in cancellationToken);
			AppendDefinition(in type, ref sb, in cancellationToken);
			AppendConstructors(in type, ref sb, in cancellationToken);
			AppendFields(in type, ref sb, in cancellationToken);
			AppendProperties(in type, ref sb, in cancellationToken);
			AppendMethods(in type, ref sb, in cancellationToken);
			AppendEvents(in type, ref sb, in cancellationToken);

			sb.WriteToFile(Path.Combine(baseOutputFolder, ZString.Concat(type.FullName, ".md")));
		}
		finally
		{
			sb.Dispose();
		}
	}

	private static void AppendYamlHeader(in TypeDocumentation root, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		sb.AppendLine("---");
		sb.Append("title: ");
		sb.Append(root.Name);

		switch (root.Type)
		{
			case ItemType.Class:
				sb.AppendLine(" Class");
				break;
			case ItemType.Interface:
				sb.AppendLine(" Interface");
				break;
			case ItemType.Enum:
				sb.AppendLine(" Enum");
				break;
			case ItemType.Struct:
				sb.AppendLine(" Struct");
				break;
			case ItemType.Delegate:
				sb.AppendLine(" Delegate");
				break;
		}

		sb.Append("slug: reference/");
		sb.AppendLine(root.Link.ToString(string.Empty));
		sb.AppendLine("sidebar:");
		sb.Append("  label: ");
		sb.AppendLine(root.Name);
		sb.AppendLine("---");
	}

	private static void AppendDefinition(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		sb.AppendLine("## Definition");
		sb.AppendLine();

		AppendObsoleteWarning(in type, ref sb, in cancellationToken);

		if (!string.IsNullOrWhiteSpace(type.Summary))
		{
			sb.AppendLine(type.Summary.Trim());
			sb.AppendLine();
		}

		if (!string.IsNullOrEmpty(type.Syntax))
		{
			sb.AppendLine("```csharp title=\"C#\"");
			sb.AppendLine(type.Syntax);
			sb.AppendLine("```");
			sb.AppendLine();
		}

		if (type.TypeParameters.Length > 0)
		{
			sb.AppendLine("### Type Parameters");
			sb.AppendLine();

			for (int i = 0; i < type.TypeParameters.Length; i++)
			{
				sb.Append('`');
				sb.Append(type.TypeParameters[i].Id);
				sb.AppendLine("`  ");
				sb.AppendLine(type.TypeParameters[i].Description);
				sb.AppendLine();
			}
		}

		WriteParameters(in type, ref sb, in cancellationToken);

		if (type.Inheritance.Length > 0)
		{
			sb.Append("Inheritance ");
			for (int i = 0; i < type.Inheritance.Length; i++)
			{
				AppendTypeWithLink(type.Inheritance[i].Name, type.Inheritance[i].Link, ref sb);
				if (i < type.Inheritance.Length - 1)
				{
					sb.Append(" → ");
				}
			}

			sb.AppendLine();
		}

		if (type.Implements.Length > 0)
		{
			sb.AppendLine();
			sb.Append("Implements ");
			for (int i = 0; i < type.Implements.Length; i++)
			{
				AppendTypeWithLink(type.Implements[i].Name, type.Implements[i].Link, ref sb);
				if (i < type.Implements.Length - 1)
				{
					sb.Append(", ");
				}
			}

			sb.AppendLine();
		}

		WriteRemarks(in type, ref sb, in cancellationToken);

		sb.AppendLine();
	}

	private static void AppendFields(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (type.Fields.Count == 0)
		{
			return;
		}

		sb.AppendLine("## Fields");
		sb.AppendLine();

		for (int i = 0; i < type.Fields.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			TypeDocumentation field = type.Fields[i];
			sb.AppendLine($"### {field.Name}");
			sb.AppendLine();

			AppendObsoleteWarning(in field, ref sb, in cancellationToken);

			if (!string.IsNullOrWhiteSpace(field.Summary))
			{
				sb.AppendLine(field.Summary.Trim());
				sb.AppendLine();
			}

			if (!string.IsNullOrEmpty(field.Syntax))
			{
				sb.AppendLine("```csharp title=\"C#\"");
				sb.AppendLine(field.Syntax);
				sb.AppendLine("```");
			}

			WriteRemarks(in field, ref sb, in cancellationToken);

			sb.AppendLine();
		}
	}

	private static void AppendProperties(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (type.Properties.Count == 0)
		{
			return;
		}

		sb.AppendLine("## Properties");
		sb.AppendLine();

		for (int i = 0; i < type.Properties.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			TypeDocumentation property = type.Properties[i];
			sb.AppendLine($"### {property.Name}");
			sb.AppendLine();

			AppendObsoleteWarning(in property, ref sb, in cancellationToken);

			if (!string.IsNullOrWhiteSpace(property.Summary))
			{
				sb.AppendLine(property.Summary.Trim());
				sb.AppendLine();
			}

			if (!string.IsNullOrEmpty(property.Syntax))
			{
				sb.AppendLine("```csharp title=\"C#\"");
				sb.AppendLine(property.Syntax);
				sb.AppendLine("```");
			}

			WriteRemarks(in property, ref sb, in cancellationToken);

			sb.AppendLine();
		}
	}

	private static void AppendConstructors(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (type.Constructors.Count == 0)
		{
			return;
		}

		sb.AppendLine("## Constructors");
		sb.AppendLine();

		WriteMethods(type.Constructors, ref sb, cancellationToken);
	}

	private static void AppendMethods(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (type.Methods.Count == 0)
		{
			return;
		}

		sb.AppendLine("## Methods");
		sb.AppendLine();

		WriteMethods(type.Methods, ref sb, cancellationToken);
	}

	private static void AppendEvents(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (type.Events.Count == 0)
		{
			return;
		}

		sb.AppendLine("## Events");
		sb.AppendLine();

		for (int i = 0; i < type.Events.Count; i++)
		{
			TypeDocumentation evt = type.Events[i];
			sb.Append("### ");
			sb.AppendLine(evt.Name);
			sb.AppendLine();

			AppendObsoleteWarning(in evt, ref sb, in cancellationToken);

			if (!string.IsNullOrWhiteSpace(evt.Summary))
			{
				sb.AppendLine(evt.Summary.Trim());
				sb.AppendLine();
			}

			if (!string.IsNullOrEmpty(evt.Syntax))
			{
				sb.AppendLine("```csharp title=\"C#\"");
				sb.AppendLine(evt.Syntax);
				sb.AppendLine("```");
				sb.AppendLine();
			}

			if (evt.Returns.HasValue)
			{
				sb.AppendLine("#### Event Type");
				sb.AppendLine();
				AppendTypeWithLink(evt.Returns.Value.Type.Name, evt.Returns.Value.Type.Link, ref sb);
				sb.AppendLine();
			}
		}
	}

	private static void WriteMethods(IReadOnlyList<TypeDocumentation> methods, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		for (int i = 0; i < methods.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			TypeDocumentation method = methods[i];
			sb.AppendLine($"### {method.Name}");
			sb.AppendLine();

			AppendObsoleteWarning(in method, ref sb, in cancellationToken);

			if (!string.IsNullOrWhiteSpace(method.Summary))
			{
				sb.AppendLine(method.Summary.Trim());
				sb.AppendLine();
			}

			if (!string.IsNullOrEmpty(method.Syntax))
			{
				sb.AppendLine("```csharp title=\"C#\"");
				sb.AppendLine(method.Syntax);
				sb.AppendLine("```");
				sb.AppendLine();
			}

			WriteParameters(in method, ref sb, in cancellationToken);

			if (method.Returns.HasValue)
			{
				sb.AppendLine("#### Returns");
				sb.AppendLine();

				cancellationToken.ThrowIfCancellationRequested();

				ReturnDocumentation returns = method.Returns.Value;
				AppendTypeWithLink(returns.Type.Name, returns.Type.Link, ref sb);
				if (!string.IsNullOrEmpty(returns.Summary))
				{
					sb.AppendLine("  ");
					sb.Append(returns.Summary);
				}

				sb.AppendLine();
			}

			WriteExceptions(in method, ref sb, in cancellationToken);

			WriteRemarks(in method, ref sb, in cancellationToken, "####");

			sb.AppendLine();
		}
	}

	private static void WriteParameters(in TypeDocumentation method, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		if (method.Parameters.Length == 0)
		{
			return;
		}

		sb.AppendLine("#### Parameters");
		sb.AppendLine();

		for (int j = 0; j < method.Parameters.Length; j++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			ParameterDocumentation parameter = method.Parameters[j];

			sb.Append('`');
			sb.Append(parameter.Name);
			sb.Append("` ");
			AppendTypeWithLink(parameter.Type.Name, parameter.Type.Link, ref sb);
			sb.AppendLine("  ");
			if (!string.IsNullOrEmpty(parameter.Summary))
			{
				sb.AppendLine(parameter.Summary);
			}

			sb.AppendLine();
		}
	}

	private static void WriteRemarks(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken, string header = "##")
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (string.IsNullOrWhiteSpace(type.Remarks))
		{
			return;
		}

		sb.Append(header);
		sb.AppendLine(" Remarks");
		sb.AppendLine();
		sb.AppendLine(type.Remarks);
	}

	private static void WriteExceptions(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (type.Exceptions.Length == 0)
		{
			return;
		}

		sb.AppendLine("#### Exceptions");
		sb.AppendLine();

		for (int i = 0; i < type.Exceptions.Length; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			ExceptionDocumentation exception = type.Exceptions[i];
			AppendTypeWithLink(exception.Type.Name, exception.Type.Link, ref sb);
			sb.AppendLine("  ");
			if (!string.IsNullOrWhiteSpace(exception.Description))
			{
				sb.AppendLine(exception.Description);
			}

			sb.AppendLine();
		}
	}

	private static void AppendObsoleteWarning(in TypeDocumentation type, ref Utf16ValueStringBuilder sb, in CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (type.TryGetObsolete(out string? obsoleteReason, out bool isObsoleteError))
		{
			sb.Append(":::");
			sb.Append(isObsoleteError ? "danger" : "caution");
			sb.AppendLine("[Obsolete]");
			if (string.IsNullOrEmpty(obsoleteReason))
			{
				sb.Append("This type is obsolete");
				if (isObsoleteError)
				{
					sb.Append(" and should not be used");
				}

				sb.AppendLine(".");
			}
			else
			{
				sb.AppendLine(obsoleteReason);
			}

			sb.AppendLine(":::");
		}
	}

	private static void AppendTypeWithLink(ReadOnlySpan<char> type, in Link link, ref Utf16ValueStringBuilder sb, bool writeInCode = false)
	{
		if (!link.IsEmpty)
		{
			sb.Append('[');
		}

		if (writeInCode)
		{
			sb.Append('`');
		}

		sb.Append(type);

		if (writeInCode)
		{
			sb.Append('`');
		}

		if (!link.IsEmpty)
		{
			sb.Append(']');
			sb.Append('(');
			sb.Append(link.ToString());
			sb.Append("/)");
		}
	}
}