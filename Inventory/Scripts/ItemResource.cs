using Godot;

public enum ItemType
{
	Any,
	Artifact,
	Beverage,
	Junk,
	Tool
}

public enum ItemRarity
{
	VeryCommon,
	Common,
	Uncommon,
	Rare,
	Unique
}

public partial class ItemResource : Resource
{
	[Export]
	public ItemType Type { get; set; }

	[Export]
	public PackedScene ItemScene { get; set; }

	[ExportCategory("Item Properties")]
	[Export]
	public string Name { get; set; }

	[Export]
	public float Weight { get; set; }

	[Export]
	public int MaxCarryAmount { get; set; }

	[Export]
	public float Value { get; set; }

	private static readonly string _itemResourceDir = "res://Items/";

	public static ItemResource Get(string itemName)
	{
		string path = ItemResource._itemResourceDir;
		path = $"{path}/{itemName}";
		return ResourceLoader.Load<ItemResource>(path);
	}

	public static bool Exists(string itemName)
	{
		string path = ItemResource._itemResourceDir;
		path = $"{path}/{itemName}";
		return ResourceLoader.Exists(path);
	}
}
