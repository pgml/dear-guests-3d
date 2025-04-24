using Godot;

public partial class ThrowComponent : Component
{
	[Export]
	public ProgressBar ForceProgress { get; set; }

	public bool CanThrow { get; set; } = false;
	public float ThrowForce { get; set; } = 0;
	public float MaxThrowForce { get; set; } = 20;
	public float ThrowIncreaseStep { get; set; } = 0.05f;

	private PhysicsObject _throwObject;

	//public override void _Ready()
	//{
	//	base._Ready();
	//	ForceProgress.MaxValue = MaxThrowForce;
	//	ForceProgress.Step = ThrowIncreaseStep;
	//}

	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("action_trigger") && CanThrow) {
			if (ThrowForce <= _throwObject.MaxThrowForce) {
				ThrowForce += _throwObject.ThrowIncreaseStep;
			}
		}

		if (Input.IsActionJustReleased("action_trigger")) {
			CanThrow = false;
			ThrowForce = 0;

			if (!_throwObject.IsInsideTree()) {
				_throwObject.QueueFree();
			}
		}

		ForceProgress.Value = ThrowForce;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent &&
			mouseEvent.Pressed)
	 	{
		 	if (mouseEvent.ButtonIndex == MouseButton.Left) {
				if (ThrowForce > 0) {
					_throw();
					ThrowForce = 0;
					CanThrow = false;
				}
			}

		 	if (mouseEvent.ButtonIndex == MouseButton.Right) {
				_throwObject = _instantiateThrowObject();
				ThrowForce = _throwObject.MinThrowForce;
				ForceProgress.MinValue = _throwObject.MinThrowForce;
				ForceProgress.MaxValue = _throwObject.MaxThrowForce;
				CanThrow = true;
			}
		}
	}

	private void _throw()
	{
		var actor = ActorData.Character<Actor>();
		int itemAmount = actor.Inventory.GetItemAmount(
			actor.Belt.SelectedItemResourceIndex);

		if (_throwObject is null || itemAmount <= 0) {
			return;
		}

		GetTree().CurrentScene.AddChild(_throwObject);
		_throwObject.GlobalPosition = actor.GlobalPosition;

		Vector2 mousePos = GetViewport().GetMousePosition();
		var camera = GetViewport().GetCamera3D();

		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePos);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePos).Normalized();

		float groundY = actor.GlobalPosition.Y;
		float distanceToIntersection = (groundY - rayOrigin.Y) / rayDirection.Y;
		Vector3 hitPoint = rayOrigin + rayDirection * distanceToIntersection;

		Vector3 direction = (hitPoint - actor.GlobalPosition).Normalized();
		float angle = Mathf.DegToRad(60);
		float verticalComponent = Mathf.Sin(angle) * ThrowForce;

		Vector3 velocity = direction * ThrowForce + new Vector3(0, verticalComponent, 0);
		_throwObject.LinearVelocity = velocity;

		_updateInventory();
	}

	private PackedScene _itemScene()
	{
		var actor = ActorData.Character<Actor>();
		return actor.Belt.SelectedItemResource.ItemScene;
	}

	private PhysicsObject _instantiateThrowObject()
	{
		if (_itemScene() is null) {
			return new();
		}
		return _itemScene().Instantiate<PhysicsObject>();
	}

	private bool _updateInventory()
	{
		var actor = ActorData.Character<Actor>();
		var itemResource = actor.Belt.SelectedItemResource;
		int itemResourceIndex = actor.Inventory.GetItemResourceIndex(itemResource);

		return actor.Inventory.RemoveOneItem(itemResourceIndex);
	}
}
