using Godot;
using System;
using System.Collections.Generic;

public partial class Inventory : Resource, ICloneable
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

		if (amount > item.MaxCarryAmount) {
			amount = item.MaxCarryAmount;
		}

		if (IsInInventory(item)) {
			int resourceIndex = GetItemResourceIndex(item);
			if (UpdateItem(resourceIndex, item, amount)) {
				wasAdded = true;
			}
		}
		else {
			Items.Add(
				new InventoryItemResource() {
					ItemResource = item,
					Amount = amount,
					InventoryIndex = Items.Count + 1
				}
			);
			wasAdded = true;
		}

		if (wasAdded) {
			EmitSignal(SignalName.InventoryUpdated);
		}

		return wasAdded;
	}

	public bool UpdateItem(
		int resourceIndex,
		ItemResource item,
		int amount
	) {
		if (!IsInInventory(item) || resourceIndex < 0) {
			return false;
		}

		var resource = Items[resourceIndex];

		if (resource.ItemResource == item) {
			resource.Amount += amount;
			if (resource.Amount > resource.ItemResource.MaxCarryAmount) {
				resource.Amount = resource.ItemResource.MaxCarryAmount;
			}
		}
		else {
			resource.ItemResource = item;
			resource.Amount = amount;
		}

		EmitSignal(SignalName.InventoryUpdated);
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

	public bool RemoveOneItem(int resourceIndex)
	{
		return RemoveItem(resourceIndex, 1);
	}

	public int GetItemResourceIndex(ItemResource item)
	{
		for (var index = 0; index < Items.Count; index++) {
			var inventoryItemResource = Items[index];
			if (inventoryItemResource.ItemResource == item) {
				return index;
			}
		}
		return -1;
	}

	public InventoryItemResource GetItemByInventoryIndex(int index)
	{
		foreach (var item in Items) {
			if (item.InventoryIndex == index) {
				return item;
			}
		}
		return null;
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

	/// <summary>
	/// Returns the amount of the item with the inventory index `index`
	/// </summary>
	public int GetItemAmount(int index)
	{
		InventoryItemResource item = GetItemByInventoryIndex(index);
		return item is null ? 0 : item.Amount;
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

	public List<InventoryItemResource> BeltItems()
	{
		var beltItems = new List<InventoryItemResource>();

		foreach (var item in Items) {
			if (item.IsInBelt) {
				beltItems.Add(item);
			}
		}

		return beltItems;
	}

	public InventoryItemResource GetBeltItemAt(int index)
	{
		foreach (var item in Items) {
			if (item.BeltSlot == index) {
				return item;
			}
		}
		return null;
	}

	public bool AttachItemToBelt(int index, int beltSlot)
	{
		var item = Items[index].ItemResource;

		if (!IsInInventory(item)) {
			return false;
		}

		var resource = Items[index];
		if (resource.ItemResource.AttachableToBelt) {
			resource.BeltSlot = beltSlot;
			EmitSignal(SignalName.InventoryUpdated);
			return true;
		}

		return false;
	}

	public bool DetachItemFromBelt(int index)
	{
		return AttachItemToBelt(index, -1);
	}

	public bool ClearBeltSlot(int index)
	{
		foreach (var item in BeltItems()) {
			if (item.BeltSlot == index) {
				item.BeltSlot = -1;
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

	[ConsoleCommand("attach_item_to_belt")]
	public bool ConsoleAttachItemToBelt(int itemResourceIndex, int beltSlot)
	{
		if (Items[itemResourceIndex] is null) {
			throw new ConsoleException($"no item at: `{itemResourceIndex}`");
		}

		return AttachItemToBelt(itemResourceIndex, beltSlot);
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

	public object Clone()
	{
		return this.MemberwiseClone();
	}
}
