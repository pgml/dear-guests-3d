using Godot;

public partial class Actor : Creature
{
	[Export]
	public new CreatureData CreatureData { get; private set; }

	[Export]
	public Inventory Inventory { get; private set; }

	[Export]
	public Belt Belt { get; private set; }

	private World _world;
	private DirectionalLight3D _sun;

	private Console _console { get {
		return GD.Load<Console>(Resources.Console);
	}}

	private Camera _camera;
	private float _cameraOffset = 0;
	private float _cameraSmoothingDelta = 0;

	public override void _Ready()
	{
		//await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Tools.CheckAssigned(CreatureData, "ActorData is not assigned", GetType().Name);

		Inventory = GD.Load<Inventory>(Resources.ActorInventory);
		Belt = GD.Load<Belt>(Resources.ActorBelt);

		_world = GetTree().CurrentScene.FindChild("World") as World;
		// store camera data
		_camera = _world.Viewport.GetCamera3D() as Camera;
		_cameraSmoothingDelta = _camera.SmoothingDelta;
		_cameraOffset = Position.Y;

		base._Ready();

		CreatureData.Node = this;
		CreatureData.CameraOffset = _cameraOffset;

		//_sun = _world.Sun;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && @event.IsActionPressed("action_use_debug")) {
			if (!CreatureData.IsMimic) {
				MorphInto("uid://bpmvfplqksh2e");
			}
			else {
				MorphBack();
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!CreatureData.CanMove || CreatureData.IsAnyUiPanelOpen()) {
			if (!CreatureData.IsBuildMoveActive) {
				CreatureData.Direction = Vector3.Zero;
				return;
			}
		}
		//SunShadowSprite.RotationDegrees = new Vector3(0, _sun.RotationDegrees.X, 0);

		if (Input.IsPhysicalKeyPressed(Key.Shift)) {
			CreatureData.IsRunning = !CreatureData.IsRunning;
		}

		Vector3 direction = GetInputDirection();

		if (CreatureData is not null && !_console.IsOpen) {
			CreatureData.Direction = direction;
			CreatureData.VelocityMultiplier = CreatureData.WalkSpeed;

			if (CreatureData.IsRunning) {
				CreatureData.VelocityMultiplier = CreatureData.RunSpeed;
			}

			CreatureData.ForwardDirection = CreatureData.Direction;
		}
	}

	public Vector3 GetInputDirection()
	{
		Vector2 input = Input.GetVector(
			"action_walk_left",
			"action_walk_right",
			"action_walk_up",
			"action_walk_down"
		);

		return new() {
			X = input.X,
			Y = 0,
			Z = input.Y * Mathf.Sqrt(1.58f)
		};
	}

	public void SpawnAtPosition(Vector3 position, Scene scene)
	{
		var actor = ResourceLoader.Load<PackedScene>(Resources.Actor).Instantiate<Node3D>();
		GD.PrintS(actor, scene);
		scene.AddChild(actor);
		actor.Position = position;
	}

	/// <summary>
	/// Morphs the actor into a `PhysicsObject`
	/// `itemPath` can be a `uid://`, `res://`
	/// </summary>
	public async void MorphInto(string itemPath)
	{
		// freeze camera for a short amount of time to make
		// transition a little bit smoother
		_camera.Freeze = true;
		// increase camera smoothing to let movement appear a bit
		// heavier since we are an object now
		_camera.SmoothingDelta = 8;

		// Hide original actor form
		CreatureData.Controller.Visible = false;

		var itemInstance = GD.Load<PackedScene>(itemPath).Instantiate<PhysicsObject>();
		itemInstance.Position = new Vector3(0, _cameraOffset, 0);

		CreatureData.IsMimic = true;
		CreatureData.MimicObject = itemInstance;
		CreatureData.Parent.AddChild(itemInstance);
		CreatureData.CameraOffset = itemInstance.Position.Y - 1.5f;

		await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
		_camera.Freeze = false;
	}

	/// <summary>
	/// Morphs actor back into its original form
	/// </summary>
	public void MorphBack()
	{
		_camera.SmoothingDelta = _cameraSmoothingDelta;
		CreatureData.Parent.RemoveChild(CreatureData.MimicObject);
		CreatureData.IsMimic = false;
		CreatureData.MimicObject = null;
		CreatureData.Controller.Visible = true;
		CreatureData.CameraOffset = _cameraOffset;
	}
}
