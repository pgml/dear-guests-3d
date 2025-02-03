using Godot;

public partial class UiQuickInventoryList : UiItemList
{
	public ItemType RestrictTypeTo { get; set; }

	public override void _Ready()
	{
		base._Ready();
		PopulateList();
		ActorInventory.InventoryUpdated += PopulateList;
	}

	public void PopulateList()
	{
		var items = ActorInventory.GetItemsOfType(RestrictTypeTo);

		if (items is null) {
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
			ListItems.Add(inventoryItem, row);
		}
	}
}
