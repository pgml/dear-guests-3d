using Godot;

public partial class Equipment : Node3D
{
	[Export]
	public string EquipmentName { get; set; }

	[Export]
	public Area3D TriggerArea { get; set; }

	[Export]
	public ItemType AllowedInputType { get; set; }

	public bool CanUse { get; set; } = false;

	public PackedScene UseIndicator { get {
		return GD.Load<PackedScene>(Resources.UseIndicator);
	}}

	public DateTime DateTime { get; private set; }

	//protected UiQuickInventory QuickInventory = new();
	public PackedScene QuickInventory { get {
		return GD.Load<PackedScene>(Resources.UiQuickInventory);
	}}
	public UiQuickInventory QuickInventoryInstance = null;

	private Node3D _indicatorInstance;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint()) {
			DateTime = GD.Load<DateTime>(Resources.DateTime);
			//QuickInventory = mainUI.FindChild("QuickInventory") as UiQuickInventory;
		}

		TriggerArea.BodyEntered += _onTriggerAreaBodyEntered;
		TriggerArea.BodyExited += _onTriggerAreaBodyExited;
	}

	public override void _Process(double delta)
	{
		//if (_quickInventory.IsOpen) {
		//	_quickInventory.Position = QuickInventoryPosition();
		//}
	}

	public override void _Input(InputEvent @event)
	{
		if (!IsInstanceValid(QuickInventoryInstance)
			&& (@event.IsActionReleased("action_use"))
			&& CanUse
		) {
			QuickInventoryInstance = QuickInventory.Instantiate<UiQuickInventory>();
			GetNode("/root/MainUI").AddChild(QuickInventoryInstance);
			Vector2 position = InventoryPosition(QuickInventoryInstance);
			QuickInventoryInstance.Description = $"Use {EquipmentName}";
			QuickInventoryInstance.RestrictTypeTo = AllowedInputType;
			QuickInventoryInstance.Open(position);
		}

		if (IsInstanceValid(QuickInventoryInstance)) {
			if (@event.IsActionPressed("action_cancel")) {
				//QuickInventoryInstance.Close();
				QuickInventoryInstance.QueueFree();
			}
		}
	}

	protected float ColliderHeight()
	{
		var collisionShape = TriggerArea.FindChild("CollisionShape3D") as CollisionShape3D;
		return collisionShape.Shape switch {
			BoxShape3D => (collisionShape.Shape as BoxShape3D).Size.Y,
			CapsuleShape3D => (collisionShape.Shape as CapsuleShape3D).Height,
			CylinderShape3D => (collisionShape.Shape as CylinderShape3D).Height,
			_ => 0
		};
	}

	protected Vector2 InventoryPosition(Control uiInstance)
	{
		if (uiInstance is null) {
			return Vector2.Zero;
		}
		Vector2 position = GetViewport().GetCamera3D().UnprojectPosition(GlobalPosition);
		position.X += Position.X;
		position.Y -= uiInstance.Size.Y + Position.Z;
		return position;
	}

	private void _onTriggerAreaBodyEntered(Node3D body)
	{
		if (body is not Controller) {
			return;
		}

		var controller = body as Controller;
		if (controller.CreatureData.Node is not Actor) {
			return;
		}

		_indicatorInstance = UseIndicator.Instantiate<Node3D>();
		_indicatorInstance.Position = new Vector3(0, ColliderHeight(), 0);
		AddChild(_indicatorInstance);
		CanUse = true;
	}

	private void _onTriggerAreaBodyExited(Node3D body)
	{
		_indicatorInstance.QueueFree();
		CanUse = false;

		//if (QuickInventoryInstance.IsOpen) {
		//	QuickInventoryInstance.Close();
		//	QuickInventoryInstance.QueueFree();
		//}
	}
}
