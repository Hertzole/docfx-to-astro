using VYaml.Annotations;

namespace DocfxToAstro.Models.Yaml;

[YamlObject]
public partial class SyntaxContent
{
	public string? Content { get; set; }
	public Parameter[]? Parameters { get; set; }
	[YamlMember("return")]
	public Return? Returns { get; set; }
	public TypeParameter[]? TypeParameters { get; set; }
}