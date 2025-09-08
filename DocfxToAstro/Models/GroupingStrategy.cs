namespace DocfxToAstro.Models;

public enum GroupingStrategy
{
	/// <summary>
	/// Group documentation by assembly (default)
	/// </summary>
	Assembly,
	
	/// <summary>
	/// Group documentation by namespace
	/// </summary>
	Namespace
}