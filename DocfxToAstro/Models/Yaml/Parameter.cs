using VYaml.Annotations;

namespace DocfxToAstro.Models.Yaml;

[YamlObject]
public readonly partial record struct Parameter
{
	public string Id { get; }
	public string Type { get; }
	public string Description { get; }

	public Parameter(string id, string type, string description)
	{
		Id = id;
		Type = type;
		Description = description;
	}
}