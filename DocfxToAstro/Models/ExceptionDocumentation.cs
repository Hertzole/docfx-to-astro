namespace DocfxToAstro.Models;

public readonly record struct ExceptionDocumentation
{
	public TypeReferenceDocumentation Type { get; }
	public string? Description { get; }

	public ExceptionDocumentation(TypeReferenceDocumentation type, string? description)
	{
		Type = type;
		Description = description;
	}
}