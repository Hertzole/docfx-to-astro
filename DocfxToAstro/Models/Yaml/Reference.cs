using VYaml.Annotations;

namespace DocfxToAstro.Models.Yaml;

[YamlObject]
public partial struct Reference
{
	private string name;
	
	public string Uid { get; set; }
	public string Name
	{
		get { return name;}
		set
		{
			name = value?.Replace("<", "\\<").Replace(">", "\\>") ?? string.Empty;
		}
	}
	
	public string Href { get; set; }
}