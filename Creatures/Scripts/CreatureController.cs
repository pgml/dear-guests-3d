using Godot;
using static IController;

public partial class CreatureController : CharacterBody3D
{
	[Export]
	public Node3D CharacterNode { get; set; }

	[ExportGroup("Movement")]
	[Export]
	public bool CanMove = true;

	[Export]
	public float DefaultWalkSpeed = 50.0f;

	[Export]
	public float DefaultRunSpeed = 100.0f;

	[Export]
	public Vector2 StartingDirection = new Vector2(1, 0);

	[Export]
	public bool ToggleRun = false;

	public float WalkSpeed;
	public float RunSpeed;
	public MoveState CurrentState { get; set; } = MoveState.IDLE;
	public dynamic CharacterData { get; set; }

	public float Gravity {
		get { return (float)ProjectSettings.GetSetting("physics/3d/default_gravity"); }
		set { Gravity = value; }
	}

	public float GravitySq {
		get { return Mathf.Pow(Gravity, 2); }
		set { GravitySq = value; }
	}

	public override void _PhysicsProcess(double delta)
	{
		CharacterData.IsOnFloor = IsOnFloor();
	}
}
