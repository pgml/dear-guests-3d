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

	// For yet unknown reasons in some occasions the character bolts
	// to the sky never to be seen again
	// this threshold ensures that the character is forced to the ground
	// when exceeded
	/// <summary>
	/// When DistanceToFloor() exceeds this threshold the character
	/// is forced to the ground
	/// </summary>
	[Export]
	public float SafeThreshold { get; set; } = 25;

	[Export]
	public bool Debug { get; set; } = false;

	public float ClimbTestForwardDistance { get; set; } = 1f;
	public Vector3 ClimbOrigin { get; set; }
	public Vector3 ClimbTo { get; set; }

	// @temporary
	[Export]
	public float InitialJumpImpulseTile = 26;
	[Export]
	public float JumpImpulsePerTile = 3;

	public async override void _Ready()
	{
		base._Ready();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (CreatureData is null) {
			return;
		}

		CreatureData.ClimbComponent = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CreatureData is null) {
			return;
		}

		if (Debug) {
			GD.PrintS(
				"ColliderHeight:", ColliderHeight(),
				"TileHeight:", ColliderHeight(true),
				"DistanceToFloor:", CreatureData.Controller.DistanceToFloor(),
				"TileDistanceToFloor:", CreatureData.Controller.DistanceToFloor(true)
			);
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
		else if (
			Input.IsActionJustReleased("action_climb")
			&& CreatureData.IsOnFloor
		) {
			CreatureData.StartClimb = true;
			Vector3 globalPosition = CreatureData.Controller.GlobalPosition;
			Vector3 facingDirection = CreatureData.FacingDirection;

			globalPosition.Y = InitialJumpImpulseTile
				+ (ColliderHeight(true) - 1) * JumpImpulsePerTile;

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

		PhysicsBody3D physicsBody = _collider().Body;
		Node parent = physicsBody.GetParent();
		double colliderHeight = 0;

		MeshInstance3D mesh = null;

		if (parent is MeshInstance3D) {
			mesh = parent as MeshInstance3D;
		}

		if (physicsBody is RigidBody3D body) {
			mesh = body.GetChild<MeshInstance3D>(0);
		}

		if (mesh is not null)	{
			Aabb aabb = mesh.Mesh.GetAabb();
			double meshElevation = mesh.Position.Y;
			double elevation = Controller.DistanceToFloor();

			colliderHeight = (aabb * mesh.GlobalTransform).Size.Y - elevation;

			if (returnTileSize) {
				colliderHeight = aabb.Size.Y / 2 - Controller.DistanceToFloor(true);
			}
		}
		else if (parent is StaticBody3D || IsInstanceValid(physicsBody)) {
			var child = physicsBody.GetChild<CollisionShape3D>(0);
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

	private (PhysicsBody3D Body, bool IsClimbable) _collider()
	{
		TestMotion testMotion = new(
			Controller.GetRid(),
			new Transform3D(
				Basis.Identity,
				Controller.GlobalTransform.Origin
			),
			CreatureData.FacingDirection,
			0.01f
		);

		if (testMotion.IsColliding) {
			var collider = testMotion.Collider<PhysicsBody3D>();
			return (collider, _bodyIsClimbable(collider));
		}

		return (null, false);
	}

	/// <summary>
	/// checks if a StaticBody3D is climbable by checking its parent or
	/// their parent is in the Climbable group<br />
	/// </summary>
	private bool _bodyIsClimbable(PhysicsBody3D body)
	{
		if (!IsInstanceValid(body)) {
			return false;
		}

		return body.GetParent().IsInGroup("Climbable")
			|| body.GetParent().GetParent().IsInGroup("Climbable");
	}

	private bool _canClimb()
	{
		return _collider().IsClimbable
			&& (ColliderHeight() >= MinClimbHeight && ColliderHeight() <= MaxClimbHeight);
	}
}
