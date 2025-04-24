using Godot;

public partial class ThrowComponent : Component
{
	[Export]
	public ProgressBar ForceProgress { get; set; }

	[Export]
	public HBoxContainer ThrowForceIndicator { get; set; }

	public bool CanThrow { get; set; } = false;
	public float ThrowForce { get; set; } = 0;
	public float MaxThrowForce { get; set; } = 20;
	public float ThrowIncreaseStep { get; set; } = 0.05f;

	private PhysicsObject _throwObject;
	private float _chargeProgress = 0;

	public override void _Ready()
	{
		base._Ready();

		ThrowForceIndicator.Visible = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionPressed("action_trigger") && CanThrow) {
			if (_chargeProgress <= _throwObject.MaxThrowForce) {
				_chargeProgress += _throwObject.ThrowIncreaseStep;
			}

			ThrowForceIndicator.Visible = true;
			Vector2 mousePos = ThrowForceIndicator.GetGlobalMousePosition();
			ThrowForceIndicator.GlobalPosition = mousePos - new Vector2(
				ThrowForceIndicator.Size.X / 2,
				ThrowForceIndicator.Size.Y + 2
			);
		}

		if (Input.IsActionJustReleased("action_trigger")) {
			CanThrow = false;
			ThrowForce = 0;
			_chargeProgress = 0;

			if (!_throwObject.IsInsideTree()) {
				_throwObject.QueueFree();
			}

			// Reset charge progress ui
			for (var i = 0; i < 5; i++) {
				ThrowForceIndicator.GetChild<TextureRect>(i).FlipV = false;
			}

			ThrowForceIndicator.Visible = false;
		}

		if (_throwObject is not null) {
			ForceProgress.Value = _chargeProgress;

			ProgressBar p = ForceProgress;
			double percentage = (p.Value - p.MinValue) / (p.MaxValue - p.MinValue) * 100f;

			if (ForceProgress.Value > _throwObject.MinThrowForce &&
				percentage % 20 == 0 &&
				percentage <= 100)
		 	{
				// update charge progress ui and set throw force
				for (var i = 0; i < percentage / 20; i++) {
					ThrowForceIndicator.GetChild<TextureRect>(i).FlipV = true;
					ThrowForce = _chargeProgress;
				}
			}
		}
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
					ThrowForceIndicator.Visible = false;
				}
			}

		 	if (mouseEvent.ButtonIndex == MouseButton.Right) {
				_throwObject = _instantiateThrowObject();
				//ThrowForce = _throwObject.MinThrowForce;
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
