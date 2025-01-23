using Godot;
using static IController;

public partial class Controller : CreatureController, IController
{
	private AnimationNodeStateMachinePlayback _stateMachine;

	public override void _Ready()
	{
		_setCharacterData();
		base._Ready();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CreatureData is not null) {
			base._PhysicsProcess(delta);
			Movement(delta);
		}
	}

	public void Movement(double delta)
	{
		CreatureData.CurrentState = CurrentState;
		CreatureData.Position = Position;

		// Set velocity to zero to make animations stop when
		// movement is forced
		//if (CreatureData.VelocityMultiplier == 0.0f) {
		//	Velocity = Vector3.Zero;
		//}
		//else {
			CurrentState = _stateIdle();
		//}

		if (Velocity != Vector3.Zero)	{
			CurrentState = _stateWalk();
		}

		CreatureData.CurrentState = CurrentState;
		CreatureData.IsIdle = CurrentState == _stateIdle();
		CreatureData.VelocityMultiplier = CreatureData.WalkSpeed;

		if (CreatureData.IsRunning) {
			CreatureData.VelocityMultiplier = CreatureData.RunSpeed;
		}

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

		Velocity = CreatureData.Velocity;

		MoveAndSlide();

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
		if (CharacterNode is Actor) {
			var characterNode = CharacterNode as Actor;
			CreatureData = characterNode.CreatureData;
		}
		//else if (CharacterNode is AI) {
		//	var characterNode = CharacterNode as AI;
		//	CreatureData = characterNode.CreatureData;
		//}

		CreatureData.Controller = this;
		CreatureData.Parent = GetParent() as Node3D;
		CreatureData.IsRunning = ToggleRun;
		CreatureData.DefaultWalkSpeed = DefaultWalkSpeed;
		CreatureData.DefaultRunSpeed = DefaultRunSpeed;
		CreatureData.WalkSpeed = DefaultWalkSpeed;
		CreatureData.RunSpeed = DefaultRunSpeed;
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
