using Godot;

public partial class UiQuickInventory : Control
{
	[Export]
	public UiQuickInventoryList QuickInventoryItemList { get; set; }

	[Export]
	public ItemType RestrictTypeTo { get; set; }

	public bool IsOpen { get; set; } = false;

	public void Toggle(Vector2 position)
	{
		QuickInventoryItemList.RestrictTypeTo = RestrictTypeTo;
		Position = position;
		IsOpen = !IsOpen;
	}
}
