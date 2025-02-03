using Godot;

public partial class UiInventory : UiControl
{
	[Export]
	public Panel InventoryBackgroud { get; set; }

	public PackedScene QuickInventory { get {
		return GD.Load<PackedScene>(Resources.QuickInventory);
	}}

	private UiQuickInventory _quickInventory;

	public override void _Ready()
	{
		base._Ready();
		var mainUI = GetNode("/root/MainUI");
		_quickInventory = mainUI.FindChild("QuickInventory") as UiQuickInventory;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_inventory")
			&& !_quickInventory.IsOpen
		) {
			_toggleInventory();
		}
	}

	private void _toggleInventory()
	{
		float posY = !IsOpen ? 0 : Size.Y;
		Position = new Vector2(0, posY);
		IsOpen = !IsOpen;
	}
}
