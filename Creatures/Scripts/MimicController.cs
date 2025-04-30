using Godot;
using static IController;

public partial class MimicController : RigidBody3D
{
	public CreatureData CreatureData { get; private set; }
	public MoveState CurrentState { get; set; } = MoveState.IDLE;

	public float Gravity {
		get { return (float)ProjectSettings.GetSetting("physics/3d/default_gravity"); }
		set { Gravity = value; }
	}

	public float GravitySq {
		get { return Mathf.Pow(Gravity, 2); }
		set { GravitySq = value; }
	}

	private Vector3 _initialRotation;

	public void Movement(double delta) {}

	public override void _Ready()
	{
		_setCharacterData();
		base._Ready();

		_initialRotation = Rotation;
		SetAxisLock(PhysicsServer3D.BodyAxis.AngularX, true);
		SetAxisLock(PhysicsServer3D.BodyAxis.AngularY, true);
		SetAxisLock(PhysicsServer3D.BodyAxis.AngularZ, false);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && @event.IsActionPressed("action_use_debug")) {
			//var bolt = GD.Load<PhysicsObject>("uid://bpmvfplqksh2e");
			GD.Print("Debug use action pressed");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CreatureData is not null) {
			//GD.PrintS(CreatureData.Direction);
			if (Input.IsActionJustReleased("action_walk_left") ||
				Input.IsActionJustReleased("action_walk_right") ||
				Input.IsActionJustReleased("action_walk_up") ||
				Input.IsActionJustReleased("action_walk_down"))
			{
				GD.PrintS("Walking", CreatureData.Direction);
				if (LinearVelocity.Length() <= 2) {
					//GD.PrintS(LinearVelocity, LinearVelocity.Length());
					ApplyCentralImpulse(CreatureData.Direction);
				}
			}

			_updatePositionToParent();
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		state = PhysicsObject.LockRotationAxis(state, _initialRotation);
	}

	/// <summary>
	/// Transfers the CharacterBody3D's position to the characters root node<br />
	/// Since the CharacterBody3D is not the root node we move the position
	/// to the actual root which makes it easier to handle
	/// </summary>
	private void _updatePositionToParent()
	{
		var parentPosition = GetParent<Node3D>().Position;
		GetParent<Node3D>().Position = new Vector3(
			parentPosition.X + Position.X,
			parentPosition.Y + Position.Y,
			parentPosition.Z + Position.Z
		);
		Position = Vector3.Zero;
	}

	private void _setCharacterData()
	{
		CreatureData = _creatureData();

		if (IsInstanceValid(CreatureData)) {
			CreatureData.Parent = GetParent() as Node3D;
			//CreatureData.IsRunning = ToggleRun;
			//CreatureData.DefaultWalkSpeed = DefaultWalkSpeed;
			//CreatureData.DefaultRunSpeed = DefaultRunSpeed;
			//CreatureData.WalkSpeed = DefaultWalkSpeed;
			//CreatureData.RunSpeed = DefaultRunSpeed;
		}
	}

	private CreatureData _creatureData()
	{
		Actor characterNode = null;

		foreach (Node child in GetChildren()) {
			if (child is Actor) {
				characterNode = child as Actor;
				break;
			}
		}

		return characterNode.CreatureData;
	}
}
