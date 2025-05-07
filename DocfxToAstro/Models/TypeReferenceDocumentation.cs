namespace DocfxToAstro.Models;

public readonly record struct TypeReferenceDocumentation
{
	public string Name { get; }
	public Link Link { get; }

	public TypeReferenceDocumentation(string name, Link link)
	{
		Name = Formatters.FormatType(name).ToString();
		Link = link;
	}
}