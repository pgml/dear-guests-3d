using Godot;

public partial class UiBuildModeList : UiItemList
{
	public override void _Ready()
	{
		base._Ready();
		PopulateList();
		ActorInventory.InventoryUpdated += PopulateList;
	}

	public void PopulateList()
	{
		var items = ActorInventory.GetItemsOfType(ItemType.Equipment);

		if (items is null || !IsInstanceValid(TreeRoot)) {
			return;
		}

		// @todo try to avoid clearing it beforehand
		// instead try to update or append
		ClearList();

		foreach (var inventoryItem in items) {
			ItemResource item = inventoryItem.ItemResource;
			TreeItem row = CreateItem(TreeRoot);
			string amount = inventoryItem.Amount > 0
				? $" ({inventoryItem.Amount.ToString()})"
				: "";

			row.SetText(0, $"{item.Name}{amount}");
			row.SetMetadata(0, item);
			ListItems.Add(inventoryItem, row);
		}
	}
}
