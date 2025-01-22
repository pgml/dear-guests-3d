using Godot;

public partial class JumpComponent : Component
{
	[Export]
	public float JumpImpulse { get; set; } = 20f;

	[Export]
	public float JumpTestForwardDistance { get; set; } = 3f;

	[Export]
	public bool JumpLimitation { get; set; } = true;

	public Vector3 JumpOrigin { get; set; }
	public Vector3 JumpTo { get; set; }

	public async override void _Ready()
	{
		base._Ready();

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		CreatureData.JumpComponent = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(CreatureData)) {
			return;
		}

		if (Input.IsActionJustPressed("action_jump") && CreatureData.IsOnFloor) {
			Vector3 globalPosition = CreatureData.Controller.GlobalPosition;
			Vector3 facingDirection = CreatureData.FacingDirection;

			JumpOrigin = globalPosition;
			JumpTo = globalPosition + facingDirection * JumpTestForwardDistance;

			CreatureData.ShouldJump = true;
		}
		else if (CreatureData.IsOnFloor) {
			CreatureData.ShouldJump = false;
		}
	}
}
