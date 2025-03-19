using Godot;

public partial class EdgeCheckComponent : Component
{
	[Export]
	public float BodyTestMargin { get; set; } = 0.17f;

	[Export]
	public Vector3 TestDownDistance { get; set; } = new Vector3(0, -0.6f, 0);

	[Export]
	public float TestForwardDistance { get; set; } = 0.7f;

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(CreatureData)) {
			return;
		}

		if (IsFacingEdge() && CreatureData.IsOnFloor) {
			if (CreatureData.IsOnFloor) {
				CreatureData.CanMoveAndSlide = false;
				CreatureData.CanJump = true;
				CreatureData.IsFacingEdge = true;
			}
			CreatureData.ShouldJumpForward = true;
		}
		else {
			CreatureData.IsFacingEdge = false;
			CreatureData.CanMoveAndSlide = true;
			CreatureData.CanJump = false;
		}

		if (!IsFacingEdge() && !CreatureData.IsJumping) {
			CreatureData.ShouldJumpForward = false;
		}
	}

	public bool IsFacingEdge()
	{
		Vector3 facingDirection = CreatureData.FacingDirection;
		Transform3D globalTransform = Controller.GlobalTransform;
		Vector3 forward = globalTransform.Origin + facingDirection * TestForwardDistance;
		Vector3 testPosition = forward + TestDownDistance;

		PhysicsTestMotionParameters3D bodyTestParams = new() {
			From = new Transform3D(Basis.Identity, testPosition),
			Motion = Vector3.Down,
			Margin = BodyTestMargin
		};

		return !PhysicsServer3D.BodyTestMotion(Controller.GetRid(), bodyTestParams);
	}
}
