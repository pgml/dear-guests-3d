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

	public void Toggle(Vector2 position)
	{
		Label.Text = Description;
		QuickInventoryItemList.RestrictTypeTo = RestrictTypeTo;
		QuickInventoryItemList.PopulateList();
		Position = position;
		IsOpen = !IsOpen;
	}
}
