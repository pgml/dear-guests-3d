using Godot;

public partial class Actor : Creature
{
	[Export]
	public new CreatureData CreatureData { get; private set; }

	[Export]
	public Inventory Inventory { get; private set; }

	private World _world;
	private DirectionalLight3D _sun;

	private Console _console { get {
		return GD.Load<Console>(Resources.Console);
	}}

	public override void _Ready()
	{
		Tools.CheckAssigned(CreatureData, "ActorData is not assigned", GetType().Name);
		Inventory = GD.Load<Inventory>(Resources.ActorInventory);

		base._Ready();

		CreatureData.Node = this;

		//_world = GetTree().Root.GetNode<World>("Scene/World");
		//_sun = _world.Sun;
	}

	public override void _PhysicsProcess(double delta)
	{
		//SunShadowSprite.RotationDegrees = new Vector3(0, _sun.RotationDegrees.X, 0);

		if (Input.IsPhysicalKeyPressed(Key.Shift)) {
			CreatureData.IsRunning = !CreatureData.IsRunning;
		}

		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = new() {
			X = input.X,
			Y = 0,
			Z = input.Y * Mathf.Sqrt(1.58f)
		};

		if (CreatureData is not null && !_console.IsOpen) {
			CreatureData.Direction = direction;
		}
	}
}
