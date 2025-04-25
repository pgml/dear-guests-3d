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

	public override void _Ready()
	{
		//await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Tools.CheckAssigned(CreatureData, "ActorData is not assigned", GetType().Name);
		Inventory = GD.Load<Inventory>(Resources.ActorInventory);
		Belt = GD.Load<Belt>(Resources.ActorBelt);

		base._Ready();

		CreatureData.Node = this;

		//_world = GetTree().Root.GetNode<World>("Scene/World");
		//_sun = _world.Sun;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!CreatureData.CanMove) {
			return;
		}
		//SunShadowSprite.RotationDegrees = new Vector3(0, _sun.RotationDegrees.X, 0);

		if (Input.IsPhysicalKeyPressed(Key.Shift)) {
			CreatureData.IsRunning = !CreatureData.IsRunning;
		}

		Vector2 input = Input.GetVector(
			"action_walk_left",
			"action_walk_right",
			"action_walk_up",
			"action_walk_down"
		);

		Vector3 direction = new() {
			X = input.X,
			Y = 0,
			Z = input.Y * Mathf.Sqrt(1.58f)
		};

		if (CreatureData is not null && !_console.IsOpen) {
			CreatureData.Direction = direction;

			CreatureData.VelocityMultiplier = CreatureData.WalkSpeed;
			if (CreatureData.IsRunning) {
				CreatureData.VelocityMultiplier = CreatureData.RunSpeed;
			}
			CreatureData.ForwardDirection = CreatureData.Direction;
			//GD.PrintS(CreatureData.Direction);
		}
	}

	public void SpawnAtPosition(Vector3 position, Scene scene)
	{
		var actor = ResourceLoader.Load<PackedScene>(Resources.Actor).Instantiate<Node3D>();
		GD.PrintS(actor, scene);
		scene.AddChild(actor);
		actor.Position = position;
	}
}
