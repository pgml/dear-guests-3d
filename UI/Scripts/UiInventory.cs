using Godot;
using System.Linq;

public partial class UiInventory : UiControl
{
	[Export]
	public Panel InventoryBackground { get; set; }

	[Export]
	public UiBackPackItemList BackPackItemList { get; set; }

	public static AudioLibrary AudioLibrary { get {
		return GD.Load<AudioLibrary>(Resources.AudioLibrary);
	}}

	public PackedScene QuickInventory { get {
		return GD.Load<PackedScene>(Resources.UiQuickInventory);
	}}

	private UiQuickInventory _quickInventory;
	private Node _mainUi;
	private Actor _actor;
	private bool _indicesSet = false;

	//public async override void _Ready()
	//{
	//	base._Ready();
	//}

	public override void _Process(double delta)
	{
		if (!_indicesSet && _actor is not null) {
			_actor.Inventory.SetIndices();
			_indicesSet = true;
		}
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

		_actor = ActorData().Character<Actor>();

		UiBackPackItemList itemList = BackPackItemList;
		if (@event is InputEventKey key && key.IsPressed()) {
			int selectedIndex = 0;
			string pressedKey = key.AsTextKeyLabel();

			if (itemList.GetSelected() is TreeItem selectedItem && IsOpen) {
				int index = 0;
				foreach (var (invItemResource, treeItem) in itemList.ListItems.ToList()) {
					// Add to belt stuff
					if (treeItem == selectedItem) {
						int invIndex = invItemResource.InventoryIndex;

						if (@event.IsActionPressed(DGInputMap.RemoveFromBelt)) {
							_actor.Inventory.DetachItemFromBelt(invIndex);
						}

						if (InputMap.HasAction($"{DGInputMap.AddToBeltSlot}{pressedKey}")) {
							int slotIndex = pressedKey.ToInt();

							if (slotIndex <= _actor.Belt.MaxItems &&
								@event.IsActionPressed($"{DGInputMap.AddToBeltSlot}{slotIndex}"))
							{
								_actor.Inventory.ClearBeltSlot(slotIndex-1);
								_actor.Inventory.AttachItemToBelt(invIndex, slotIndex-1);
							}
						}
						selectedIndex = index;
					}
					index++;
				}

				// keep selected row selected
				var selected = itemList.GetRoot().GetChild(selectedIndex);
				itemList.SetSelected(selected, 0);
			}
		}
	}

	private void _openInventory()
	{
		if (!IsOpen) {
			Position = Vector2.Zero;
			IsOpen = true;
			RestrictPlayerMovement = true;
			ActorData().IsInventoryOpen = IsOpen;

			BackPackItemList.PopulateList();
			BackPackItemList.SelectFirstRow();
			BackPackItemList.AudioInstance = AudioLibrary.CreateAudioInstance(
				"Inventory",
				this
			);
			BackPackItemList.AudioInstance.PlayUiSound(AudioLibrary.InventoryOpen);
		}
	}

	private async void _closeInventory()
	{
		if (IsOpen) {
			Position = new Vector2(0, Size.Y);
			IsOpen = false;
			RestrictPlayerMovement = false;
			ActorData().IsInventoryOpen = IsOpen;
			var audioInstance = BackPackItemList.AudioInstance;

			if (IsInstanceValid(audioInstance)) {
				audioInstance.PlayUiSound(AudioLibrary.InventoryClose);
				await ToSignal(audioInstance.AudioUi, "finished");
				audioInstance.CallDeferred("queue_free");
			}
		}
	}
}
