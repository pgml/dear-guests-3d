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

		GD.Print(ColliderHeight(), " - ", ColliderHeight(true));

		if (Controller.RayCastFront.IsColliding() && CreatureData.IsOnFloor) {
			CreatureData.CanClimb = true;
		}

		if (!CreatureData.IsOnFloor) {
			CreatureData.CanClimb = false;
		}

		if (Input.IsActionPressed("action_climb") && CreatureData.IsOnFloor) {
			CreatureData.StartClimb = true;
		}
		else if (Input.IsActionJustReleased("action_jump") && CreatureData.IsOnFloor) {
			CreatureData.StartClimb = true;
			Vector3 globalPosition = CreatureData.Controller.GlobalPosition;
			Vector3 facingDirection = CreatureData.FacingDirection;

			globalPosition.Y = JumpImpulseTile + (ColliderHeight(true) - 1) * JumpTileAdditive;
			//globalPosition.Y = ColliderHeight() + CreatureData.JumpImpulse + JumpTileAdditive;

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
		if (!Controller.RayCastFront.IsColliding() || _collider().Body is null) {
			return 0;
		}

		StaticBody3D staticBody = _collider().Body;
		Node parent = staticBody.GetParent();

		float colliderHeight = 0;
		var sprite = CreatureData.Node.FindChild("CharacterSprite") as Sprite3D;
		float spritePosY = 32 * sprite.PixelSize * sprite.Scale.Y;
		float characterPosY = Controller.Position.Y - spritePosY;

		if (parent is MeshInstance3D)	{
			var mesh = parent as MeshInstance3D;
			Aabb aabb = mesh.Mesh.GetAabb();
			float meshHeight = aabb.Size.Y - characterPosY;
			GD.Print(meshHeight, " ", aabb.Size.Y);
			if (returnTileSize) {
				colliderHeight = meshHeight / 2;
			}
			else {
				colliderHeight = (aabb * mesh.GlobalTransform).Size.Y - characterPosY;
			}
		}
		else if (parent is StaticObject){
			var child = staticBody.GetChild<CollisionShape3D>(0);
			float childHeight = child.GlobalTransform.Origin.Y;

			colliderHeight = child.Shape switch {
				BoxShape3D => (child.Shape as BoxShape3D).Size.Y,
				CapsuleShape3D => (child.Shape as CapsuleShape3D).Height,
				_ => 0
			} - characterPosY;

			GD.Print("colliderHeight: ", colliderHeight, ", childHeight: ", childHeight);
			if (returnTileSize) {
				colliderHeight /= childHeight;
			}
		}


		return (float)System.Math.Round(colliderHeight, 1);
	}

	private (StaticBody3D Body, bool IsClimbable) _collider()
	{
		var collider = Controller.RayCastFront.GetCollider();

		if (collider is StaticBody3D) {
			var staticBody = collider as StaticBody3D;
			Node parent = staticBody.GetParent();
			bool isClimbable = parent.GetParent().IsInGroup("Climbable")
				|| parent.IsInGroup("Climbable");

			return (staticBody, isClimbable);
		}

		return (null, false);
	}

	private bool _canClimb()
	{
		return _collider().IsClimbable
			&& (ColliderHeight() >= MinClimbHeight && ColliderHeight() <= MaxClimbHeight);
	}
}
