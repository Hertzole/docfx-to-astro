namespace DocfxToAstro.Models;

public readonly record struct ParameterDocumentation(string Name, TypeReferenceDocumentation Type, string? Summary);