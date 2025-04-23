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
	private List<UiBeltItem> _beltItems = new();
	private Inventory _inventory;
	private Belt _belt;

	public override void _Ready() {}

	public override void _Process(double delta)
	{
		// All this shit should be in _Ready()
		// The reason it's here is because of the SpawnMarkers.
		// When spawning characters via the marker ActorData isn't set yet
		// @todo: make it happen - find a way for ActorData to be set
		// before UiBelt is initiated
		if (_beltItems.Count <= 0) {
			var actor = ActorData().Character<Actor>();
			_inventory = actor.Inventory;
			_belt = actor.Belt;
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
			ItemParent.AddChild(item);
			_beltItems.Add(item);
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
		for (var i = 0; i < _beltItems.Count; i++) {
			_beltItems[i].Populate(new InventoryItemResource());
		}

		foreach (var item in _inventory.BeltItems()) {
			if (_beltItems[item.BeltSlot] is null) {
				continue;
			}
			_beltItems[item.BeltSlot].Populate(item);
		}
	}
}
