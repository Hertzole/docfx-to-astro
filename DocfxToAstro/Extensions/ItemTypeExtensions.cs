using DocfxToAstro.Models;

namespace DocfxToAstro;

public static class ItemTypeExtensions
{
	public static bool IsType(this ItemType itemType)
	{
		switch (itemType)
		{
			case ItemType.Class:
			case ItemType.Struct:
			case ItemType.Delegate:
			case ItemType.Enum:
			case ItemType.Interface:
				return true;
			default:
				return false;
		}
	}
}