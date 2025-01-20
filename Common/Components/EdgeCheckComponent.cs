using Godot;

public partial class EdgeCheckComponent : Node
{
	[Export]
	public CharacterBody3D Controller { get; set; }

	[Export]
	public float BodyTestMargin { get; set; } = 0.17f;

	[Export]
	public Vector3 TestDownDistance { get; set; } = new Vector3(0, -0.6f, 0);

	[Export]
	public float TestForwardDistance { get; set; } = 0.7f;

	public override void _PhysicsProcess(double delta)
	{
		var characterData = (Controller as Controller).CharacterData as ActorData;

		if (IsNearEdge()) {
			characterData.CanMove = false;
		}
		else {
			characterData.CanMove = true;
		}
	}

	public bool IsNearEdge()
	{
		var characterData = (Controller as Controller).CharacterData as ActorData;
		Vector3 facingDirection = characterData.FacingDirection;
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
