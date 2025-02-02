using Godot;

public partial class UiInventory : Control
{
	[Export]
	public Panel InventoryBackgroud { get; set; }

	public bool IsOpen { get; set; } = false;

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_inventory")) {
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
