using Godot;
using System;
using static IController;

public partial class Controller : CreatureController, IController
{
	[Export(PropertyHint.Flags)]
	public Layer DefaultCollisionMask { get; set; }

	[Export(PropertyHint.Flags)]
	public Layer MorphCollisionMask { get; set; }

	[Export]
	public CollisionShape3D CharacterCollider { get; set; }

	public CreatureData CreatureData { get; private set; }

	private AnimationNodeStateMachinePlayback _stateMachine;
	private Console _console { get {
		return GD.Load<Console>(Resources.Console);
	}}

	public override void _Ready()
	{
		_setCharacterData();

		CollisionMask = (uint)DefaultCollisionMask;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CreatureData is CreatureData cd) {
			base._PhysicsProcess(delta);

			// Movement when morphed into a RigidBody3D via the mimic artifact
			if (cd.IsMimic &&
				cd.MimicObject is PhysicsObject obj &&
				CreatureData.Node is Actor)
			{
				cd.IsIdle = cd.Direction == Vector3.Zero;
				obj.Move(cd);
				_updatePositionToParent(obj);
			}
			else {
				Movement(delta);
			}
		}
	}

	public void Movement(double delta)
	{
		CreatureData.IsOnFloor = IsOnFloor();
		CreatureData.CurrentState = CurrentState;
		CreatureData.Position = Position;

		if (Velocity != Vector3.Zero)	{
			CurrentState = _stateWalk();
		}
		else {
			CurrentState = _stateIdle();
		}

		CreatureData.CurrentState = CurrentState;
		CreatureData.IsIdle = CurrentState == _stateIdle();

		SlopeMovement();

		CreatureData.Velocity = new Vector3(
			CreatureData.Direction.X * CreatureData.VelocityMultiplier,
			// Apply gravity
			Velocity.Y - GravitySq * (float)delta,
			CreatureData.Direction.Z * CreatureData.VelocityMultiplier
		);

		// disallow only horizontal movement when character isn't allowed to move
		// so that MoveAndSlide still recognises jump/climb actions
		if (!CreatureData.CanMoveAndSlide) {
			_disableHorizontalMovement();
		}

		if (CreatureData.IsOnFloor) {
			CreatureData.IsJumping = false;
		}

		Jumping();
		Climbing();

		Velocity = CreatureData.Velocity;

		if (CreatureData.CanMove) {
			MoveAndSlide();
		}

		_updatePositionToParent(this);

		if (CreatureData.IsClimbing &&
			DistanceToFloor() > CreatureData.ClimbComponent.SafeThreshold)
		{
			CreatureData.CanClimb = false;
			CreatureData.ShouldClimb = false;
			GD.Print("STAAWWWP");
			return;
		}

		if (CreatureData.Direction != Vector3.Zero) {
			CreatureData.FacingDirection = CreatureData.Direction;
		}

		//SetCollider(CharacterData.Direction);
	}

	/// <summary>
	/// JumpComponent helper<br />
	///
	/// Tells <i>when</i> the character should jump.<br />
	///
	/// Jump height and distance is controlled in the
	/// JumpComponent attached to a character<br />
	///
	/// @todo make it so that it doesn't just stop but rather
	/// fall more naturally
	/// </summary>
	public void Jumping()
	{
		if (!CreatureData.JumpComponent.JumpLimitation) {
			CreatureData.CanJump = true;
		}

		// character needs the JumpComponent attached
		if (CreatureData.ShouldJump && CreatureData.CanJump) {
			CreatureData.IsJumping = true;
			CreatureData.Velocity.Y = CreatureData.JumpComponent.JumpImpulse;
		}

		if (CreatureData.IsJumping && CreatureData.ShouldJumpForward) {
			_disableHorizontalMovement();

			float velocityMultiplier = CreatureData.VelocityMultiplier;
			Vector3 facingDirection = CreatureData.FacingDirection;
			Vector3 forward = CreatureData.JumpComponent.JumpTo;
			// convert to Vector2 so that distanceTo can ignore y
			// and get more accurate results on when we have reached
			// the position we should jump to
			var from = new Vector2(GlobalPosition.X, GlobalPosition.Z);
			var to = new Vector2(forward.X, forward.Z);

			if (from.DistanceTo(to) >= 0.5f) {
				CreatureData.Velocity.X = facingDirection.X * velocityMultiplier;
				CreatureData.Velocity.Z = facingDirection.Z * velocityMultiplier;
			}
		}
	}

	public void Climbing()
	{
		if (CreatureData.IsClimbing && IsOnFloor()) {
			CreatureData.IsClimbing = false;
		}

		if (CreatureData.ShouldClimb && CreatureData.CanClimb) {
			CreatureData.IsClimbing = true;
			CreatureData.Velocity.Y = CreatureData.ClimbComponent.ClimbTo.Y;
		}

		if (CreatureData.IsClimbing) {
			_disableHorizontalMovement();
			float velocityMultiplier = CreatureData.VelocityMultiplier;
			Vector3 facingDirection = CreatureData.FacingDirection;
			CreatureData.Velocity.X = facingDirection.X * velocityMultiplier / 2;
			CreatureData.Velocity.Z = facingDirection.Z * velocityMultiplier / 2;
		}
	}

	public void SlopeMovement()
	{
		if (_isOnSlope()) {
			// Counter orthogonal slope movement seem faster
			// on vertical (in top down view) slopes
			if (CreatureData.Direction.Z != 0) {
				CreatureData.VelocityMultiplier /= Mathf.Sqrt2;
			}

			FloorConstantSpeed = true;
		}
		else {
			FloorConstantSpeed = false;
		}
	}

	/// <summary>
	/// Get the real elevation of a character<br />
	/// Due to different things like scaling and pixel size, position.Y
	/// doesn't correctly represent the characters y position
	/// </summary>
	public double CharacterElevation(bool inTileSize = false)
	{
		Sprite3D sprite = CreatureData.CharacterSprite();
		var tileSize = 32;
		float spritePosY = tileSize * sprite.PixelSize * sprite.Scale.Y;
		float parentPosY = GetParent<Node3D>().Position.Y;
		double elevation = Math.Round(parentPosY - spritePosY, 1);

		if (inTileSize) {
			float tileScale = sprite.Scale.Y + sprite.Scale.Z;
			elevation /= tileScale;
		}

		return elevation;
	}

	public double DistanceToFloor(bool inTileSize = false)
	{
		TestMotion testMotion = new(
			GetRid(),
			GetParent<Node3D>().GlobalTransform,
			Vector3.Down
		);
		double distanceToFloor = 0;

		if (testMotion.IsColliding) {
			var collider = testMotion.Collider<StaticBody3D>();
			if (collider is not null) {
				var staticBody = collider.GetParent().GetParent<Node3D>();
				if (staticBody is StaticObject) {
					distanceToFloor = CharacterElevation() - staticBody.Position.Y;
				}
			}
		}
		else {
			distanceToFloor = CharacterElevation();
		}

		if (inTileSize) {
			Sprite3D sprite = CreatureData.CharacterSprite();
			float tileScale = sprite.Scale.Y + sprite.Scale.Z;
			distanceToFloor /= tileScale;
		}

		return distanceToFloor;
	}

	public TestMotion TestMotion(Vector3 motion)
	{
		return new TestMotion(
			GetRid(),
			GetParent<Node3D>().GlobalTransform,
			motion
		);
	}

	public void SetFacingDirection(Vector2 direction)
	{
		CreatureData.AnimationComponent.SetInitialFacingDirection(direction);
	}

	public void SetDefaultCollisionMask()
	{
		CollisionMask = (uint)DefaultCollisionMask;
	}

	public void SetMorphCollisionMask()
	{
		CollisionMask = (uint)MorphCollisionMask;
	}

	/// <summary>
	/// Transfers the CharacterBody3D's position to the characters root node<br />
	/// Since the CharacterBody3D is not the root node we move the position
	/// to the actual root which makes it easier to handle
	/// </summary>
	private void _updatePositionToParent(Node3D body)
	{
		var parentPosition = GetParent<Node3D>().Position;

		GetParent<Node3D>().Position = new Vector3(
			parentPosition.X + body.Position.X,
			parentPosition.Y + body.Position.Y,
			parentPosition.Z + body.Position.Z
		);

		body.Position = Vector3.Zero;
	}

	private void _disableHorizontalMovement()
	{
		CreatureData.Velocity.X = 0;
		CreatureData.Velocity.Z = 0;
	}

	private bool _isOnSlope()
	{
		var isOnSlope = false;

		if (Mathf.IsZeroApprox(GetFloorAngle())) {
			return isOnSlope;
		}

		isOnSlope = true;

		if (GetSlideCollisionCount() > 0) {
			var collider = GetLastSlideCollision().GetCollider() as Node3D;
			if (collider.IsInGroup("Stairs")) {
				CreatureData.IsOnStairs = true;
			}
		}

		CreatureData.IsOnSlope = isOnSlope;

		return isOnSlope;
	}

	private void _setCharacterData()
	{
		CreatureData = _creatureData();

		if (IsInstanceValid(CreatureData)) {
			CreatureData.Controller = this;
			CreatureData.Parent = GetParent() as Node3D;
			CreatureData.IsRunning = ToggleRun;
			CreatureData.DefaultWalkSpeed = DefaultWalkSpeed;
			CreatureData.DefaultRunSpeed = DefaultRunSpeed;
			CreatureData.WalkSpeed = DefaultWalkSpeed;
			CreatureData.RunSpeed = DefaultRunSpeed;
		}
	}

	private CreatureData _creatureData()
	{
		var characterNode = FindChild("Character");

		if (characterNode is AI) {
			return (characterNode as AI).CreatureData;
		}

		return (characterNode as Actor).CreatureData;
	}

	private MoveState _stateIdle()
	{
		var currentState = MoveState.IDLE;

		//if (IsCharacterBoxed()) {
		//	currentState = MoveState.BOXED_IDLE;
		//}

		return currentState;
	}

	private MoveState _stateWalk()
	{
		var currentState = MoveState.WALK;

		//if (IsCharacterBoxed()) {
		//	currentState = MoveState.BOXED_WALK;
		//}

		return currentState;
	}

	private MoveState _stateJump()
	{
		return MoveState.JUMP;
	}
}
