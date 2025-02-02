using Godot;

public partial class Inventory : Resource
{
	[Signal]
	public delegate void InventoryUpdatedEventHandler();

	[Export]
	public Godot.Collections.Array<InventoryItemResource> Items { get; set; } = new();

	public Console Console { get {
		return GD.Load<Console>(Resources.Console);
	}}

	public Inventory()
	{
		Console.AddCommands((object)this);
	}

	public void AddItem(ItemResource item, int amount)
	{
		if (IsInInventory(item)) {
			int resourceIndex = GetResourceIndex(item);
			UpdateItem(resourceIndex, item, amount);
		}
		else {
			Items.Add(
				new InventoryItemResource() {
					ItemResource = item,
					Amount = amount
				}
			);
		}

		EmitSignal(SignalName.InventoryUpdated);
	}

	public void UpdateItem(int resourceIndex, ItemResource item, int amount)
	{
		if (!IsInInventory(item) || resourceIndex < 0) {
			return;
		}

		if (Items[resourceIndex].ItemResource == item) {
			Items[resourceIndex].Amount += amount;
		}
		else {
			Items[resourceIndex].ItemResource = item;
			Items[resourceIndex].Amount = amount;
		}
	}

	public int GetResourceIndex(ItemResource item)
	{
		for (var index = 0; index < Items.Count; index++) {
			var inventoryItemResource = Items[index];
			if (inventoryItemResource.ItemResource == item) {
				return index;
			}
		}
		return -1;
	}

	public bool IsInInventory(ItemResource item)
	{
		foreach (var inventoryItemResource in Items) {
			if (inventoryItemResource.ItemResource == item) {
				return true;
			}
		}
		return false;
	}

	[ConsoleCommand("add_item")]
	public bool AddItemByString(string type, string name, string amount)
	{
		var itemPath = type switch {
			"artifact" => $"Artifacts/artifact_{name}.tres",
			_ => null
		};

		if (itemPath is null) {
			throw new ConsoleException($"invalid item type `{type}`");
		}

		if (ItemResource.Exists(itemPath)) {
			ItemResource item = ItemResource.Get(itemPath);
			AddItem(item, amount.ToInt());
			return true;
		}

		return false;
	}
}
