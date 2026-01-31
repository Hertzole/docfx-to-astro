using System.Collections.Generic;
using VYaml.Annotations;

namespace DocfxToAstro.Models.Yaml;

[YamlObject]
public partial class Root
{
	public required List<Item> Items { get; init; }
	public required Reference[] References { get; init; }
}
