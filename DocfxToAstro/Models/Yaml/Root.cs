using System.Collections.Generic;
using VYaml.Annotations;

namespace DocfxToAstro.Models.Yaml;

[YamlObject]
public partial class Root
{
	public List<Item> Items { get; set; }
	public Reference[] References { get; set; }
}