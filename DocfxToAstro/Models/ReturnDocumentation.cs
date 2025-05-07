namespace DocfxToAstro.Models;

public readonly record struct ReturnDocumentation
{
	public TypeReferenceDocumentation Type { get; }
	public string? Summary { get; }

	public ReturnDocumentation(TypeReferenceDocumentation type, string? summary)
	{
		Type = type;
		Summary = summary;
	}
}