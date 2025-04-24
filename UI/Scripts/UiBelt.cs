using Godot;
using System.Collections.Generic;

public partial class UiBelt : UiControl
{
	[Export]
	public HBoxContainer ItemParent { get; private set; }

	private PackedScene _uiBeltItem { get {
		return GD.Load<PackedScene>(Resources.UiBeltItem);
	}}

	/// <summary>
	/// Currently availabe and populated items in the belt
	/// </summary>
	private List<UiBeltItem> _uiBeltItems = new();
	private Inventory _inventory;
	private Belt _belt;
	private int _maxBeltSlots;

	public override void _Ready() {}

	public override void _Input(InputEvent @event)
	{
		if (ActorData().IsAnyUiPanelOpen()) {
			return;
		}

		if (@event is InputEventKey key && key.IsPressed()) {
			string pressedKey = key.AsTextKeyLabel();

			if (InputMap.HasAction($"{DGInputMap.AddToBeltSlot}{pressedKey}")) {
				int slotIndex = pressedKey.ToInt() - 1;

				if (slotIndex < _maxBeltSlots &&
					@event.IsActionPressed($"{DGInputMap.AddToBeltSlot}{pressedKey}"))
			 	{
					_deselectSlots();
					_uiBeltItems[slotIndex].IsSelected = true;
					_belt.Items[SlotItem(slotIndex).InventoryIndex] = true;
					_belt.SelectedItemResource = SlotItem(slotIndex).ItemResource;
					_belt.SelectedItemResourceIndex = SlotItem(slotIndex).InventoryIndex;
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		// All this shit should be in _Ready()
		// The reason it's here is because of the SpawnMarkers.
		// When spawning characters via the marker ActorData isn't set yet
		// @todo: make it happen - find a way for ActorData to be set
		// before UiBelt is initiated
		if (_uiBeltItems.Count <= 0) {
			var actor = ActorData().Character<Actor>();
			_inventory = actor.Inventory;
			_belt = actor.Belt;
			_maxBeltSlots = _belt.MaxItems;
			_populateSlots();

			_inventory.InventoryUpdated += _onInventoryUpdated;
		}
	}

	private void _populateSlots()
	{
		for (var i = 0; i < _belt.MaxItems; i++) {
			var item = _uiBeltItem.Instantiate<UiBeltItem>();
			int slot = i + 1;
			string shortKey = $"{slot}";

			if (InputMap.HasAction($"{DGInputMap.AddToBeltSlot}{slot}")) {
				var events = InputMap.ActionGetEvents($"{DGInputMap.AddToBeltSlot}{slot}");
				if (events[0] is InputEventKey key) {
					shortKey = key.AsTextPhysicalKeycode();
				}
			}

			if (SlotItem(i) is not null) {
				item.Populate(SlotItem(i));
			}

			item.InputLabel.Text = shortKey;
			item.InventoryIndex = SlotItem(i).InventoryIndex;
			ItemParent.AddChild(item);
			_uiBeltItems.Add(item);
		}
	}

	private InventoryItemResource SlotItem(int slot)
	{
		InventoryItemResource slotItem = new();

		foreach (var item in _inventory.BeltItems()) {
			if (item.BeltSlot == slot) {
				return item;
			}
		}

		return slotItem;
	}

	private void _onInventoryUpdated()
	{
		for (var i = 0; i < _uiBeltItems.Count; i++) {
			_uiBeltItems[i].Populate(new InventoryItemResource());
			_belt.Items.Clear();
		}

		foreach (var item in _inventory.BeltItems()) {
			if (_uiBeltItems[item.BeltSlot] is null) {
				continue;
			}

			_uiBeltItems[item.BeltSlot].Populate(item);
			_belt.Items[item.InventoryIndex] = false;
		}
	}

	private void _deselectSlots()
	{
		for (var i = 0; i < _belt.Items.Count; i++) {
			_belt.Items[i] = false;
		}

		foreach (var slot in _uiBeltItems) {
			slot.IsSelected = false;
		}
	}
}
