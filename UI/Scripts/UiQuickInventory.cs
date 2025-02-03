using Godot;

public partial class UiQuickInventory : UiControl
{
	[Export]
	public UiQuickInventoryList QuickInventoryItemList { get; set; }

	[Export]
	public ItemType RestrictTypeTo { get; set; }

	public override void _Ready()
	{
		base._Ready();
	}

	public void Toggle(Vector2 position)
	{
		QuickInventoryItemList.RestrictTypeTo = RestrictTypeTo;
		QuickInventoryItemList.PopulateList();
		Position = position;
		IsOpen = !IsOpen;
	}
}
