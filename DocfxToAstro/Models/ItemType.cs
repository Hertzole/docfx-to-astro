using VYaml.Annotations;

namespace DocfxToAstro.Models;

[YamlObject]
public enum ItemType
{
	Class = 0,
	Struct = 1,
	Namespace = 2,
	Delegate = 3,
	Enum = 4,
	Interface = 5,
	Field = 6,
	Property = 7,
	Method = 8,
	Operator = 9,
	Event = 10,
	Constructor = 11,
}