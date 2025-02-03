using Godot;
using System;
using System.Collections.Generic;

public partial class UiBackPackItemList : UiItemList
{
	public Dictionary<string, TitleProperties> ColumnTitles { get; set; } = new() {
		{ "Item Name", new TitleProperties(HorizontalAlignment.Left, 90)},
		{ "Weight", new TitleProperties(HorizontalAlignment.Right, 25) },
		{ "Value", new TitleProperties(HorizontalAlignment.Right, 25) },
	};

	public override void _Ready()
	{
		base._Ready();
		_buildTitleRow();
		_populateList();
		ActorInventory.InventoryUpdated += _populateList;
	}

	private void _buildTitleRow()
	{
		int i = 0;
		foreach (var (name, properties) in ColumnTitles) {
			SetColumnTitle(i, name.ToUpper());
			SetColumnTitleAlignment(i, properties.Alignment);
			SetColumnCustomMinimumWidth(i, properties.Width);
			i++;
		}
	}

	private void _populateList()
	{
		if (ActorInventory.Items is null) {
			return;
		}

		// @todo try to avoid clearing it beforehand
		// instead try to update or append
		ClearList();

		foreach (var inventoryItem in ActorInventory.Items) {
			ItemResource item = inventoryItem.ItemResource;
			TreeItem row = CreateItem(TreeRoot);
			string amount = inventoryItem.Amount > 0
				? $" ({inventoryItem.Amount.ToString()})"
				: "";

			double weight = Math.Round(item.Weight * inventoryItem.Amount, 2);

			row.SetText(0, $"{item.Name}{amount}");
			row.SetText(1, weight.ToString());
			row.SetText(2, item.Value.ToString());
			row.SetTextAlignment(1, HorizontalAlignment.Right);
			row.SetTextAlignment(2, HorizontalAlignment.Right);

			ListItems.Add(inventoryItem, row);
		}
	}
}
