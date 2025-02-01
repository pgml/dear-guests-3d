using Godot;

public partial class Inventory : Resource
{
	[Signal]
	public delegate void InventoryUpdatedEventHandler();

	[Export]
	public Godot.Collections.Array<InventoryItemResource> Items { get; set; } = new();

	public void AddItem(ItemResource item, int amount)
	{
		Items.Add(
			new InventoryItemResource() {
				ItemResource = item,
				Amount = amount
			}
		);

		EmitSignal(SignalName.InventoryUpdated);
	}

	[ConsoleCommand]
	public bool AddItemByString(string type, string name, string amount)
	{
		var itemPath = type switch {
			"artifact" => $"Artifacts/artifact_{name}.tres",
			_ => null
		};

		if (ItemResource.Exists(itemPath)) {
			ItemResource item = ItemResource.Get(itemPath);
			AddItem(item, 1);
			return true;
		}

		return false;
	}
}
