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

	public ActorData CharacterData { get; private set; }
	public Vector3 Direction { get; set; }
	public bool IsOnStairs { get; set; }

	public Dictionary<string, Component> Components {
		get { return _components(); }
	}

	private ActorData _characterData;

	private World _world;
	private DirectionalLight3D _sun;

	public override void _Ready()
	{
		_characterData = Load<ActorData>(Resources.ActorData);
		_characterData.Node = this;
		CharacterData = _characterData;

		_world = GetTree().Root.GetNode<World>("Scene/World");
		_sun = _world.Sun;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_characterData.CanMove) {
			return;
		}

		_characterData.Node = this;

		//SunShadowSprite.RotationDegrees = new Vector3(0, _sun.RotationDegrees.X, 0);

		if (Input.IsKeyPressed(Key.Shift)) {
			_characterData.IsRunning = !_characterData.IsRunning;
		}

		Vector2 input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = new();
		direction.X = input.X;
		direction.Z = input.Y * Mathf.Sqrt(1.58f);

		float gravity = _characterData.Controller.GravitySq;

		if (!_characterData.IsOnFloor) {
			direction.Y -= gravity * (float)delta;
			direction.Y = Mathf.Clamp(direction.Y, -1200, 1200);
		}

		_characterData.Direction = direction;
		_characterData.IsOnStairs = IsOnStairs;
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
