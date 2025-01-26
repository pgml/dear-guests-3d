using Godot;

public partial class ClimbComponent : Component
{
	/// <summary>
	/// Minimum tile height that a character can climb
	/// </summary>
	[Export]
	public float MinClimbHeight { get; set; } = 1.5f;

	/// <summary>
	/// Maximum tile height that a character can climb
	/// </summary>
	[Export]
	public float MaxClimbHeight { get; set; } = 6.5f;

	public float ClimbTestForwardDistance { get; set; } = 1f;
	public Vector3 ClimbOrigin { get; set; }
	public Vector3 ClimbTo { get; set; }

	// @temporary
	public float JumpImpulseTile = 30;
	public float JumpTileAdditive = 6;

	public async override void _Ready()
	{
		base._Ready();

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		CreatureData.ClimbComponent = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsInstanceValid(CreatureData)) {
			return;
		}

		if (!_canClimb()) {
			return;
		}

		if (_collider().Body is not null && CreatureData.IsOnFloor) {
			CreatureData.CanClimb = true;
		}

		if (!CreatureData.IsOnFloor) {
			CreatureData.CanClimb = false;
		}

		if (Input.IsActionPressed("action_climb") && CreatureData.IsOnFloor) {
			CreatureData.StartClimb = true;
		}
		else if (Input.IsActionJustReleased("action_climb") && CreatureData.IsOnFloor) {
			CreatureData.StartClimb = true;
			Vector3 globalPosition = CreatureData.Controller.GlobalPosition;
			Vector3 facingDirection = CreatureData.FacingDirection;

			globalPosition.Y = JumpImpulseTile + (ColliderHeight(true) - 1) * JumpTileAdditive;

			ClimbOrigin = globalPosition;
			ClimbTo = globalPosition + facingDirection * ClimbTestForwardDistance;

			CreatureData.ShouldClimb = true;
		}
		else if (CreatureData.IsOnFloor) {
			CreatureData.StartClimb = false;
			CreatureData.ShouldClimb = false;
		}
	}

	public float ColliderHeight(bool returnTileSize = false)
	{
		if (_collider().Body is null) {
			return 0;
		}

		StaticBody3D staticBody = _collider().Body;
		Node parent = staticBody.GetParent();
		//double characterElevation = Controller.CharacterElevation();
		double colliderHeight = 0;

		if (parent is MeshInstance3D)	{
			var mesh = parent as MeshInstance3D;
			Aabb aabb = mesh.Mesh.GetAabb();
			double meshElevation = mesh.Position.Y;
			double elevation = Controller.DistanceToFloor();

			colliderHeight = (aabb * mesh.GlobalTransform).Size.Y - elevation;

			if (returnTileSize) {
				colliderHeight = aabb.Size.Y / 2 - Controller.DistanceToFloor(true);
			}
		}
		else if (parent is StaticBody3D || IsInstanceValid(staticBody)) {
			var child = staticBody.GetChild<CollisionShape3D>(0);
			float childHeight = child.GlobalTransform.Origin.Y;

			colliderHeight = child.Shape switch {
				BoxShape3D => (child.Shape as BoxShape3D).Size.Y,
				CapsuleShape3D => (child.Shape as CapsuleShape3D).Height,
				_ => 0
			};

			if (returnTileSize) {
				colliderHeight /= childHeight;
			}
		}

		return (float)System.Math.Round(colliderHeight, 1);
	}

	private (StaticBody3D Body, bool IsClimbable) _collider()
	{
		PhysicsTestMotionResult3D testMotionResult = new();
		PhysicsTestMotionParameters3D testMotionParams = new() {
			From = new Transform3D(Basis.Identity, Controller.GlobalTransform.Origin + new Vector3(0, 2, 0)),
			Motion = CreatureData.FacingDirection,
		};

		bool wouldCollide = PhysicsServer3D.BodyTestMotion(
			Controller.GetRid(),
			testMotionParams,
			testMotionResult
		);

		if (wouldCollide) {
			var collider = testMotionResult.GetCollider();

			if (collider is StaticBody3D) {
				var staticBody = collider as StaticBody3D;
				return (staticBody, _bodyIsClimbable(staticBody));
			}
		}

		return (null, false);
	}

	/// <summary>
	/// checks if a StaticBody3D is climbable by checking its parent or
	/// their parent is in the Climbable group<br />
	/// </summary>
	private bool _bodyIsClimbable(StaticBody3D body)
	{
		return body.GetParent().IsInGroup("Climbable")
			|| body.GetParent().GetParent().IsInGroup("Climbable");
	}

	private bool _canClimb()
	{
		return _collider().IsClimbable
			&& (ColliderHeight() >= MinClimbHeight && ColliderHeight() <= MaxClimbHeight);
	}
}
