using Godot;

public partial class ItemResource : Resource
{
	[Export]
	public ItemType Type { get; set; }

	[Export]
	public string Name { get; set; }

	[Export]
	public float Weight { get; set; }

	[Export]
	public int Value { get; set; }

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
