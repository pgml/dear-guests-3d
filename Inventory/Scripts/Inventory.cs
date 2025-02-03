using Godot;
using System.Collections.Generic;

public partial class Inventory : Resource
{
	[Signal]
	public delegate void InventoryUpdatedEventHandler();

	[Export]
	public Godot.Collections.Array<InventoryItemResource> Items { get; set; } = new();

	public Console Console { get; set; }

	public Inventory()
	{
		Console = GD.Load<Console>(Resources.Console);
		Console.AddCommands((object)this);
	}

	public bool AddItem(ItemResource item, int amount)
	{
		bool wasAdded = false;
		if (IsInInventory(item)) {
			int resourceIndex = GetResourceIndex(item);
			if (UpdateItem(resourceIndex, item, amount)) {
				wasAdded = true;
			}
		}
		else {
			Items.Add(
				new InventoryItemResource() {
					ItemResource = item,
					Amount = amount
				}
			);
			wasAdded = true;
		}

		if (wasAdded) {
			EmitSignal(SignalName.InventoryUpdated);
		}

		return wasAdded;
	}

	public bool UpdateItem(int resourceIndex, ItemResource item, int amount)
	{
		if (!IsInInventory(item) || resourceIndex < 0) {
			return false;
		}

		if (Items[resourceIndex].ItemResource == item) {
			Items[resourceIndex].Amount += amount;
		}
		else {
			Items[resourceIndex].ItemResource = item;
			Items[resourceIndex].Amount = amount;
		}

		return true;
	}

	/// <summary>
	/// Removes an item at `resourceIndex`<br />
	/// `amount` = -1 remove whole item otherwise the amount
	/// </summary>
	public bool RemoveItem(int resourceIndex, int removeByAmount = -1)
	{
		if (resourceIndex < 0) {
			return false;
		}

		// remove whole item
		if (removeByAmount < 0) {
			Items.RemoveAt(resourceIndex);
		}
		else {
			Items[resourceIndex].Amount -= removeByAmount;
			if (Items[resourceIndex].Amount <= 0) {
				Items.RemoveAt(resourceIndex);
			}
		}
		EmitSignal(SignalName.InventoryUpdated);
		return true;
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

	public List<InventoryItemResource> GetItemsOfType(ItemType type)
	{
		List<InventoryItemResource>	itemsOfType = new();
		foreach (var item in Items) {
			if (item.ItemResource.Type == type) {
				itemsOfType.Add(item);
			}
		}
		return itemsOfType;
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
	public bool ConsoleAddItem(string type, string name, string amount)
	{
		var itemPath = type switch {
			"artifact" => $"Artifacts/artifact_{name}.tres",
			"junk" => $"Junk/{name}.tres",
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

	[ConsoleCommand("add_artifact")]
	public bool ConsoleAddArtifact(string name, string amount)
	{
		return ConsoleAddItem("artifact", name, amount);
	}

	[ConsoleCommand("remove_item")]
	public bool ConsoleRemoveItem(string resourceIndex, string removeByAmount = "-1")
	{
		int index = resourceIndex.ToInt();
		int amount = removeByAmount.ToInt();
		return RemoveItem(index, amount);
	}
}
