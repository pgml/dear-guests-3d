using Godot;

public partial class UiInventory : UiControl
{
	[Export]
	public Panel InventoryBackgroud { get; set; }

	public PackedScene QuickInventory { get {
		return GD.Load<PackedScene>(Resources.UiQuickInventory);
	}}

	private UiQuickInventory _quickInventory;
	private Node _mainUi;

	public override void _Ready()
	{
		base._Ready();
	}

	public override void _Input(InputEvent @event)
	{
		_mainUi = GetNode("/root/MainUI");
		_quickInventory = _mainUi.FindChild("QuickInventory") as UiQuickInventory;
		var uiReplicator = _mainUi.FindChild("UiReplicator") as UiReplicator;
		bool toggleInventory = @event.IsActionPressed("toggle_inventory");

		if (toggleInventory && !ActorData().IsAnyUiPanelOpen()) {
			_openInventory();
		}
		else if (toggleInventory) {
			_closeInventory();
		}
	}

	private void _openInventory()
	{
		if (!IsOpen) {
			Position = Vector2.Zero;
			IsOpen = true;
			ActorData().IsInventoryOpen = IsOpen;
		}
	}

	private void _closeInventory()
	{
		if (IsOpen) {
			Position = new Vector2(0, Size.Y);
			IsOpen = false;
			ActorData().IsInventoryOpen = IsOpen;
		}
	}
}
