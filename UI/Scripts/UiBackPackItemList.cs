using Godot;
//using System;
using System.Collections.Generic;

public partial class UiBackPackItemList : UiItemList
{
	public Dictionary<string, TitleProperties> ColumnTitles { get; set; } = new() {
		{ "Item", new TitleProperties(HorizontalAlignment.Left, 110)},
		{ "Type", new TitleProperties(HorizontalAlignment.Right, 30) },
		{ "Amount", new TitleProperties(HorizontalAlignment.Right, 30) },
		//{ "Weight", new TitleProperties(HorizontalAlignment.Right, 25) },
		{ "Value", new TitleProperties(HorizontalAlignment.Right, 40) },
	};

	public override void _Ready()
	{
		base._Ready();
		_buildTitleRow();
		//_populateList();
		ActorInventory.InventoryUpdated += PopulateList;
	}

	private void _buildTitleRow()
	{
		ColumnTitlesVisible = true;
		Columns = ColumnTitles.Count;
		int i = 0;

		foreach (var (name, properties) in ColumnTitles) {
			SetColumnTitle(i, name.ToUpper());
			SetColumnTitleAlignment(i, properties.Alignment);
			SetColumnCustomMinimumWidth(i, properties.Width);
			i++;
		}
	}

	public void PopulateList()
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

			string itemName = item.Name;
			if (item is ArtifactResource artifact && artifact.IsSynthetic) {
				itemName += " (s)";
			}

			string amount = inventoryItem.Amount > 0
				//? $" ({inventoryItem.Amount.ToString()})"
				? inventoryItem.Amount.ToString()
				: "0";

			//double weight = Math.Round(item.Weight * inventoryItem.Amount, 2);
			var image = GD.Load<CompressedTexture2D>(Resources.UiListIcon).GetImage();
			if (inventoryItem.ItemResource.ListIcon is not null) {
				image = inventoryItem.ItemResource.ListIcon.GetImage();
			}

			if (image is not null) {
				row.SetIcon(0, ImageTexture.CreateFromImage(image));
			}

			row.SetText(0, $"   {itemName}");
			//row.SetText(1, weight.ToString());
			row.SetText(1, item.Type.ToString());
			row.SetText(2, amount);
			row.SetText(3, $"{item.Value.ToString()} ¡");
			//row.SetCustomColor(1, Color.FromString("3d000696", default));
			row.SetCustomFontSize(1, 5);

			int i = 0;
			foreach (var (name, properties) in ColumnTitles) {
				row.SetTextAlignment(i, properties.Alignment);
				i++;
			}

			ListItems.Add(inventoryItem, row);
		}
	}
}
