using Godot;

[GlobalClass]
public partial class InventoryItemResource : Resource
{
	[Export]
	public ItemResource ItemResource { get; set; } = new();

	[Export]
	public int Amount { get; set; } = 0;
}
