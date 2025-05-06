namespace DocfxToAstro.Models;

public readonly record struct ExceptionDocumentation
{
	public string Type { get; }
	public Link Link { get; }
	public string? Description { get; }
	
	public ExceptionDocumentation(string type, Link link, string? description)
	{
		Type = type;
		Link = link;
		Description = description;
	}
}