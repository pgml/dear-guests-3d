using Godot;

public partial class UiQuickInventory : UiControl
{
	[Export]
	public UiQuickInventoryList QuickInventoryItemList { get; set; }

	[Export]
	public ItemType RestrictTypeTo { get; set; }

	[Export]
	public Label Label { get; set; }

	public string Description { get; set; }

	public override void _Ready()
	{
		base._Ready();
	}

	public void Open(Vector2 position)
	{
		Label.Text = Description;
		QuickInventoryItemList.RestrictTypeTo = RestrictTypeTo;
		QuickInventoryItemList.PopulateList();
		TreeItem firstItem = QuickInventoryItemList.GetRoot().GetChildren()[0];
		QuickInventoryItemList.GrabFocus();
		QuickInventoryItemList.SetSelected(firstItem, 0);
		firstItem.Select(0);
		Position = position;
		IsOpen = true;
	}

	public void Close()
	{
		if (QuickInventoryItemList is null) {
			return;
		}
		Label.Text = "";
		QuickInventoryItemList.RestrictTypeTo = ItemType.Any;
		Position = new Vector2(1000, 1000);
		IsOpen = false;
	}
}
