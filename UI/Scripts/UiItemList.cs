using Godot;
using System.Collections.Generic;

public struct TitleProperties
{
	public HorizontalAlignment Alignment;
	public int Width;

	public TitleProperties(HorizontalAlignment alignment, int width)
	{
		Alignment = alignment;
		Width = width;
	}
}

[Tool]
public partial class UiItemList : Tree
{
	public Dictionary<string, TitleProperties> ColumnTitles { get; set; } = new() {
		{ "Item Name", new TitleProperties(HorizontalAlignment.Left, 90)},
		{ "Weight", new TitleProperties(HorizontalAlignment.Right, 25) },
		{ "Value", new TitleProperties(HorizontalAlignment.Right, 25) },
	};

	public Inventory ActorInventory { get {
		return !Engine.IsEditorHint()
			? GD.Load<Inventory>(Resources.ActorInventory)
			: new();
	}}

	private List<TreeItem> _columns = new();

	public override void _Ready()
	{
		_buildTitleRow();
		_populateList();
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

		TreeItem root = CreateItem();
		foreach (var inventoryItem in ActorInventory.Items) {
			ItemResource item = inventoryItem.ItemResource;
			TreeItem row = CreateItem(root);
			string amount = inventoryItem.Amount > 0
				? $" ({inventoryItem.Amount.ToString()})"
				: "";

			row.SetText(0, $"{item.Name}{amount}");
			row.SetText(1, item.Weight.ToString());
			row.SetText(2, item.Value.ToString());

			row.SetTextAlignment(1, HorizontalAlignment.Right);
			row.SetTextAlignment(2, HorizontalAlignment.Right);
		}
	}
}
