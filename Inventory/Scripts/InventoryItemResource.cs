using Godot;

[GlobalClass]
public partial class InventoryItemResource : Resource
{
	[Export]
	public ItemResource ItemResource { get; set; } = new();

	[Export]
	public int Amount { get; set; } = 0;

	[Export]
	public int BeltSlot { get; set; } = -1;

	[Export]
	public int InventoryIndex { get; set; }
	public bool IsInBelt { get => BeltSlot > -1; }
}
