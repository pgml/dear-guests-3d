using Godot;
using static Godot.GD;
using System.Collections.Generic;

public partial class Actor : Node3D
{
	[Export]
	public Node3D Parent { get; set; }

	[Export]
	public Node ComponentsParent { get; set; }

	[Export]
	public Sprite3D SunShadowSprite { get; set; }

	public CreatureData CreatureData { get; private set; }
	public Vector3 Direction { get; set; }

	public Dictionary<string, Component> Components {
		get { return _components(); }
	}

	private World _world;
	private DirectionalLight3D _sun;

	public override void _Ready()
	{
		CreatureData = Load<CreatureData>(Resources.ActorData);
		CreatureData.Node = this;

		_world = GetTree().Root.GetNode<World>("Scene/World");
		_sun = _world.Sun;
	}

	public override void _PhysicsProcess(double delta)
	{
		CreatureData.Node = this;

		//SunShadowSprite.RotationDegrees = new Vector3(0, _sun.RotationDegrees.X, 0);

		if (Input.IsKeyPressed(Key.Shift)) {
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

	private Dictionary<string, Component> _components()
	{
		Dictionary<string, Component> components = new();
		foreach (Component component in ComponentsParent.GetChildren()) {
			components.Add(component.Name, component);
		}
		return components;
	}
}
