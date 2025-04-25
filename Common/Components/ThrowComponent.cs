using Godot;

public partial class ThrowComponent : Component
{
	[Export]
	public ProgressBar ForceProgress { get; set; }

	[Export]
	public Control ThrowForceIndicator { get; set; }

	public bool CanThrow { get; set; } = false;
	public float MinThrowPercentage { get; set; } = 20;
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
		if (_throwObject is null || !_throwObject.IsThrowable) {
			return;
		}

		if (Input.IsActionPressed("action_trigger") && CanThrow) {
			if (_chargeProgress <= _throwObject.MaxThrowForce) {
				_chargeProgress += _throwObject.ThrowIncreaseStep;
			}
			_positionThrowIndicator();
		}

		if (Input.IsActionJustReleased("action_trigger")) {
			_resetChargeProgress();
		}

		if (_throwObject is not null) {
			_chargeThrow();
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
					_resetChargeProgress();
				}
			}

		 	if (mouseEvent.ButtonIndex == MouseButton.Right) {
				_throwObject = _instantiateThrowObject();
				ForceProgress.MinValue = _throwObject.MinThrowForce;
				ForceProgress.MaxValue = _throwObject.MaxThrowForce;
				// force always a specific amount of initial progress
				// otherwise if charging and throwing quickly would be just
				// dropping
				_chargeProgress = 20f / 100f * MinThrowPercentage;
				CanThrow = true;
			}
		}
	}

	/// <summary>
	/// Prepares a throw and determines the ThrowForce based on charge progress
	/// </summary>
	private void _chargeThrow()
	{
		ForceProgress.Value = _chargeProgress;

		var p = ForceProgress;
		double percentage = (p.Value - p.MinValue) / (p.MaxValue - p.MinValue) * 100f;
		percentage = Mathf.Floor(percentage);

		if (percentage % 20 == 0 && percentage <= 100) {
			// update charge progress ui and set base throw force
			for (var i = 0; i < percentage / 20; i++) {
				ThrowForceIndicator.GetChild<TextureRect>(i).FlipV = true;
				ThrowForce = _chargeProgress;
			}
		}
	}

	/// <summary>
	/// Throws the selected object
	/// </summary>
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

		// determine the throw direction from the origin
		// based on the position of the mouse cursor
		float groundY = actor.GlobalPosition.Y;
		float distanceToIntersection = (groundY - rayOrigin.Y) / rayDirection.Y;
		Vector3 hitPoint = rayOrigin + rayDirection * distanceToIntersection;
		Vector3 direction = (hitPoint - actor.GlobalPosition).Normalized();

		// throw at a roughly 45° angle
		float angleDeg = 45 + (float)GD.RandRange(-5,  5);
		float angle = Mathf.DegToRad(angleDeg);

		// add some randomness to the throw force otherwise it would always land
		// on the same spot
		ThrowForce += (float)GD.RandRange(-0.4, 0.4);

		// same with ThrowForce – add some randomness to the trajectory to vary not
		// only the distance but also the "wideness"
		Vector3 trajectory = new Vector3(
			GD.RandRange(-1, 1),
		 	Mathf.Sin(angle) * ThrowForce,
			GD.RandRange(-1, 1)
		);

		Vector3 velocity = direction * ThrowForce + trajectory;
		_throwObject.LinearVelocity = velocity;

		_updateInventory();
	}

	/// <summary>
	/// Resets a throw/charge and hides charge indicator UI
	/// </summary>
	private void _resetChargeProgress()
	{
		CanThrow = false;
		ThrowForce = 0;
		_chargeProgress = 0;

		if (!_throwObject.IsInsideTree()) {
			_throwObject.QueueFree();
		}

		for (var i = 0; i < 5; i++) {
			ThrowForceIndicator.GetChild<TextureRect>(i).FlipV = false;
		}

		ThrowForceIndicator.Visible = false;
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

	/// <summary>
	/// Updates the amount of the thrown item in the inventory
	/// </summary>
	private bool _updateInventory()
	{
		var actor = ActorData.Character<Actor>();
		var itemResource = actor.Belt.SelectedItemResource;
		int itemResourceIndex = actor.Inventory.GetItemResourceIndex(itemResource);

		return actor.Inventory.RemoveOneItem(itemResourceIndex);
	}

	/// <summary>
	/// Shows throw charge indicator and positions it at the mouse cursor
	/// </summary>
	private void _positionThrowIndicator()
	{
		ThrowForceIndicator.Visible = true;
		Vector2 mousePos = ThrowForceIndicator.GetGlobalMousePosition();

		ThrowForceIndicator.GlobalPosition = mousePos - new Vector2(
			ThrowForceIndicator.Size.X / 2,
			ThrowForceIndicator.Size.Y - 1
		);
	}
}
