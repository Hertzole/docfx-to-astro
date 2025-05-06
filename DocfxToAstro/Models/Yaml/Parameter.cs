using VYaml.Annotations;

namespace DocfxToAstro.Models.Yaml;

[YamlObject]
public partial struct Parameter
{
	public string Id { get; set; }
	public string Type { get; set; }
	public string Description { get; set; }

	public Parameter(string id, string type, string description)
	{
		Id = id;
		Type = type;
		Description = description;
	}
}