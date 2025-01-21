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

		if (IsNearEdge()) {
			CreatureData.CanMove = false;
		}
		else {
			CreatureData.CanMove = true;
		}
	}

	public bool IsNearEdge()
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

		return !PhysicsServer3D.BodyTestMotion(
			Controller.GetRid(),
			bodyTestParams
		);
	}
}
