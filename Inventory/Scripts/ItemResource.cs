using Godot;

public enum ItemType
{
	Any,
	Artifact,
	Beverage,
	Equipment,
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

	[Export]
	public Texture2D Sprite { get; set; }

	[Export]
	public Texture2D ListIcon { get; set; }

	[ExportCategory("Item Properties")]
	[Export]
	public string Name { get; set; }

	[Export]
	public string Description { get; set; }

	[Export]
	public float Weight { get; set; }

	[Export]
	public int MaxCarryAmount { get; set; }

	[Export]
	public float Value { get; set; }

	[Export]
	public bool AttachableToBelt { get; set; } = false;

	private static readonly string _itemResourceDir = "res://Items/";

	public static ItemResource Get(string path, bool absoluePath = false)
	{
		if (!absoluePath) {
			path = $"{ItemResource._itemResourceDir}/{path}";
		}
		return ResourceLoader.Load<ItemResource>(path);
	}

	public static T Get<T>(string path, bool absoluePath = false)
		where T : class
	{
		if (!absoluePath) {
			path = $"{ItemResource._itemResourceDir}/{path}";
		}

		return ResourceLoader.Load<T>(path);
	}

	public static bool Exists(string itemName)
	{
		string path = ItemResource._itemResourceDir;
		path = $"{path}/{itemName}";
		return ResourceLoader.Exists(path);
	}
}
