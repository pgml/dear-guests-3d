using Godot;

public partial class Actor : Creature
{
	[Export]
	public new CreatureData CreatureData { get; private set; }

	private World _world;
	private DirectionalLight3D _sun;

	public override void _Ready()
	{
		base._Ready();

		_world = GetTree().Root.GetNode<World>("Scene/World");
		_sun = _world.Sun;
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

		CreatureData.Direction = direction;
	}
}
