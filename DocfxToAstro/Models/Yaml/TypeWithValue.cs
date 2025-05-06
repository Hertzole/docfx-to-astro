using VYaml.Annotations;

namespace DocfxToAstro.Models.Yaml;

[YamlObject]
public readonly partial record struct TypeWithValue
{
	public string Type { get; }
	public string Value { get; }
	
	public TypeWithValue(string type, string value)
	{
		Type = type;
		Value = value;
	}
}