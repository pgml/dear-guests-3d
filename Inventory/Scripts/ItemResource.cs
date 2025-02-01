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
}
