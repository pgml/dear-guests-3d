using Godot;

public partial class Equipment : Node3D
{
	[Export]
	public Area3D TriggerArea { get; set; }

	[Export]
	public ItemType AllowedInputType { get; set; }

	public bool CanUse { get; set; } = false;

	public PackedScene UseIndicator { get {
		return GD.Load<PackedScene>(Resources.UseIndicator);
	}}

	public PackedScene QuickInventory { get {
		return GD.Load<PackedScene>(Resources.QuickInventory);
	}}

	private Node3D _indicatorInstance;
	private UiQuickInventory _quickInventory;

	public override void _Ready()
	{
		var mainUI = GetNode("/root/MainUI");
		_quickInventory = mainUI.FindChild("QuickInventory") as UiQuickInventory;

		TriggerArea.BodyEntered += _onTriggerAreaBodyEntered;
		TriggerArea.BodyExited += _onTriggerAreaBodyExited;
	}

	public override void _Process(double delta)
	{
		if (_quickInventory.IsOpen) {
			_quickInventory.Position = QuickInventoryPosition();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("action_use") && CanUse) {
			Vector2 position = QuickInventoryPosition();

			if (_quickInventory.IsOpen) {
				position = new Vector2(1000, 1000);
			}

			_quickInventory.RestrictTypeTo = AllowedInputType;
			_quickInventory.Toggle(position);
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

	protected Vector2 QuickInventoryPosition()
	{
		Vector2 position = GetViewport().GetCamera3D().UnprojectPosition(GlobalPosition);
		position.X += Position.X;
		position.Y -= _quickInventory.Size.Y + Position.Z;
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

		if (_quickInventory.IsOpen) {
			_quickInventory.Toggle(new Vector2(1000, 1000));
		}
	}
}
